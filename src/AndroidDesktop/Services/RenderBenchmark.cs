using AndroidDesktop.Adapters.Viewport;
using AndroidDesktop.ViewModels;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Threading;

namespace AndroidDesktop.Services;

public static class RenderBenchmark
{
    public static async Task RunAsync(string mode, string path, int seconds, MainWindow window, DesktopViewModel desktop,
        WebRtcViewport viewport, EmulatorSessionService session, IAndroidToolService tools, string? diagnosticApk = null, string workload = "home")
    {
        if (seconds is < 10 or > 600) throw new ArgumentOutOfRangeException(nameof(seconds));
        var samples = new List<object>();
        var guestFrames = new SortedSet<long>(); string layer = "";
        var gpuSamples = new List<string>();
        object? dpiChecks = null, hoverChecks = null, inputStats = null;

        var previous = new Dictionary<(int, long), (double Cpu, long Timestamp)>();
        string? error = null; string graphics = ""; string renderer = ""; bool ready = false, retained = false, restored = false;
        var dispatcherDelays = new List<double>(); uint nativeDpi = 0; bool nativeFocus = false;
        bool nativeTouch = false, nativeKey = false, installedDiagnostic = false, nativeInputTestRan = false; string inputEvidence = "", preservedPackage = "";
        var startupWatch = Stopwatch.StartNew(); double startupSeconds = 0, shutdownSeconds = 0;
        bool darkMargins = false, rotationCommandEnabled = false;
        var rotations = new List<object>();
        var pointerRotations = new List<object>();
        object? saveReopen = null;
        long? savedTouchesBeforeInput = null;
        bool focusReleaseKey = false, focusReleaseTouch = false;
        var shutdownUiIntervals = new List<double>();
        try
        {
            desktop.Device.PersistStartupTimings = false;
            await desktop.Device.StartCommand.ExecuteAsync(null);
            startupSeconds = startupWatch.Elapsed.TotalSeconds;
            ready = session.HasOwnedEmulator && (mode == "standalone" || desktop.Device.InputReady);
            if (!ready) throw new InvalidOperationException(desktop.Device.OperationError.Length > 0 ? desktop.Device.OperationError : desktop.Device.Log);
            // Same workload for all transports: Home, with no APK installation or game consent changes.
            (await tools.RunAsync(session.Options.Adb, ["-s", session.Options.Serial, "shell", "input", "keyevent", "KEYCODE_HOME"], TimeSpan.FromSeconds(10), CancellationToken.None)).RequireSuccess();
            if (workload == "diagnostic") {
                var existing = await tools.RunAsync(session.Options.Adb, ["-s",session.Options.Serial,"shell","pm","path","com.androiddesktop.diagnostic"], TimeSpan.FromSeconds(10), CancellationToken.None);
                if (!existing.Output.Contains("package:")) {
                    if (diagnosticApk is null) throw new InvalidOperationException("Diagnostic workload requires the repository APK.");
                    (await tools.RunAsync(session.Options.Adb,["-s",session.Options.Serial,"install",Path.GetFullPath(diagnosticApk)],TimeSpan.FromSeconds(60),CancellationToken.None)).RequireSuccess(); installedDiagnostic = true;
                }
                (await tools.RunAsync(session.Options.Adb,["-s",session.Options.Serial,"shell","am","start","-n","com.androiddesktop.diagnostic/.DiagnosticActivity"],TimeSpan.FromSeconds(10),CancellationToken.None)).RequireSuccess();
            }
            if (workload == "game") {
                (await tools.RunAsync(session.Options.Adb, ["-s", session.Options.Serial, "shell", "monkey", "-p", "com.gamehivecorp.taptitans2", "-c", "android.intent.category.LAUNCHER", "1"], TimeSpan.FromSeconds(15), CancellationToken.None)).RequireSuccess();
            }
            await Task.Delay(15000);
            if (workload == "game") {
                var capture = await tools.RunAsync(session.Options.Adb, ["-s", session.Options.Serial, "shell", "screencap", "-p", "/sdcard/AndroidDesktop-benchmark.png"], TimeSpan.FromSeconds(10), CancellationToken.None);
                capture.RequireSuccess();
                (await tools.RunAsync(session.Options.Adb, ["-s", session.Options.Serial, "pull", "/sdcard/AndroidDesktop-benchmark.png", path + ".game.png"], TimeSpan.FromSeconds(10), CancellationToken.None)).RequireSuccess();
                await tools.RunAsync(session.Options.Adb, ["-s", session.Options.Serial, "shell", "rm", "/sdcard/AndroidDesktop-benchmark.png"], TimeSpan.FromSeconds(10), CancellationToken.None);
            }

            graphics = (await tools.RunAsync(session.Options.Adb, ["-s", session.Options.Serial, "shell", "dumpsys", "SurfaceFlinger"], TimeSpan.FromSeconds(15), CancellationToken.None)).RequireSuccess();
            graphics = string.Join('\n', graphics.Split('\n').Where(l => l.Contains("GLES:") || l.Contains("GL_RENDERER") || l.Contains("Vulkan")));
            var package = await tools.RunAsync(session.Options.Adb, ["-s", session.Options.Serial, "shell", "dumpsys", "package", "com.gamehivecorp.taptitans2"], TimeSpan.FromSeconds(10), CancellationToken.None);
            preservedPackage = string.Join('\n', package.Output.Split('\n').Where(line => line.Contains("versionName=") || line.Contains("firstInstallTime=") || line.Contains("lastUpdateTime=")));
            renderer = session.RendererLog;
            if (workload is "game" or "diagnostic") {
                var layers = (await tools.RunAsync(session.Options.Adb, ["-s", session.Options.Serial, "shell", "dumpsys", "SurfaceFlinger", "--list"], TimeSpan.FromSeconds(10), CancellationToken.None)).RequireSuccess();
                var packageName = workload == "game" ? "com.gamehivecorp.taptitans2" : "com.androiddesktop.diagnostic";
                var candidates = layers.Split('\n').Select(l=>l.Trim()).Where(l=>l.Contains(packageName) && !l.Contains("ActivityRecord") && !l.Contains("InputSink") && !l.Contains("Bounds for") && !l.Contains("animation-leash"))
                    .OrderByDescending(l=>l.StartsWith(packageName + "/") || l.StartsWith("SurfaceView[")).ToArray();
                foreach (var candidate in candidates) {
                    var quotedCandidate = "'" + candidate.Replace("'", "'\"'\"'") + "'";
                    var probe = await tools.RunAsync(session.Options.Adb,["-s",session.Options.Serial,"shell","dumpsys SurfaceFlinger --latency " + quotedCandidate],TimeSpan.FromSeconds(5),CancellationToken.None);
                    if (probe.Output.Split('\n').Count(l=>l.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries) is { Length:3 } f && long.TryParse(f[1],out var t) && t>0 && t<long.MaxValue) >= 3) { layer=candidate; break; }
                }
                if (layer != "") await tools.RunAsync(session.Options.Adb,["-s",session.Options.Serial,"shell","dumpsys","SurfaceFlinger","--latency-clear"],TimeSpan.FromSeconds(10),CancellationToken.None);

            }
            var clock = Stopwatch.StartNew();

            while (clock.Elapsed.TotalSeconds < seconds)
            {
                var identities = session.NativeWindowOwners().Concat([new ActiveDeviceLease.Owner(Environment.ProcessId, Process.GetCurrentProcess().StartTime.ToUniversalTime().Ticks)]).ToArray();
                var ids = identities.Select(o => o.Pid).Concat(ProcessTree.Descendants([Environment.ProcessId])).Distinct();
                var values = new List<object>();
                foreach (var id in ids)
                {
                    try {
                        using var process = Process.GetProcessById(id);
                        var key = (id, process.StartTime.ToUniversalTime().Ticks);
                        var cpu = process.TotalProcessorTime.TotalMilliseconds; var now = Stopwatch.GetTimestamp();
                        if (previous.TryGetValue(key, out var last)) {
                            var elapsed = Stopwatch.GetElapsedTime(last.Timestamp, now).TotalMilliseconds;
                            values.Add(new { name = process.ProcessName, id, cpuPercentOfMachine = Math.Max(0, (cpu - last.Cpu) / elapsed / Environment.ProcessorCount * 100), workingSetMiB = process.WorkingSet64 / 1048576d });
                        }
                        previous[key] = (cpu, now);
                    } catch (Exception e) when (e is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception) { }
                }
                dispatcherDelays.Add(await Task.Run(async () => {
                    var dispatch = Stopwatch.StartNew();
                    await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Input);
                    return dispatch.Elapsed.TotalMilliseconds;
                }));
                if (layer != "") {
                    var quoted = "'" + layer.Replace("'", "'\"'\"'") + "'";
                    var latency = await tools.RunAsync(session.Options.Adb, ["-s", session.Options.Serial, "shell", "dumpsys SurfaceFlinger --latency " + quoted], TimeSpan.FromSeconds(5), CancellationToken.None);
                    foreach (var line in latency.Output.Split('\n')) {
                        var fields = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                        if (fields.Length == 3 && long.TryParse(fields[1], out var present) && present > 0 && present < long.MaxValue) guestFrames.Add(present);
                    }
                }
                if (File.Exists("C:/Windows/System32/nvidia-smi.exe")) {
                    var gpu = await tools.RunAsync("C:/Windows/System32/nvidia-smi.exe",["--query-gpu=timestamp,utilization.gpu,utilization.memory,memory.used,power.draw","--format=csv,noheader,nounits"],TimeSpan.FromSeconds(5),CancellationToken.None);
                    if (gpu.ExitCode == 0) gpuSamples.Add(gpu.Output.Trim());
                }
                samples.Add(new { seconds = clock.Elapsed.TotalSeconds, processes = values });
                await Task.Delay(1000);
            }
            if (mode == "native") {
                dpiChecks = await window.CheckNativeDpiAsync(viewport.NativeHost!);
                retained = await window.CheckNativeHostAsync(viewport.NativeHost!);
                nativeDpi = viewport.NativeHost!.ChildDpi; nativeFocus = viewport.NativeHost.FocusNative();
                darkMargins = viewport.NativeHost.BackgroundMatchesTheme();
                rotationCommandEnabled = desktop.Device.RotateCommand.CanExecute(null);
                (await tools.RunAsync(session.Options.Adb, ["-s", session.Options.Serial, "shell", "am", "start", "-a", "android.settings.SETTINGS"], TimeSpan.FromSeconds(10), CancellationToken.None)).RequireSuccess();
                for (var quarterTurn = 1; quarterTurn <= 4; quarterTurn++) {
                    await desktop.Device.RotateCommand.ExecuteAsync(null);
                    if (desktop.Device.OperationError.Length > 0) throw new InvalidOperationException(desktop.Device.OperationError);
                    await Task.Delay(700);
                    var orientation = (await tools.RunAsync(session.Options.Adb, ["-s", session.Options.Serial, "shell", "dumpsys", "input"], TimeSpan.FromSeconds(10), CancellationToken.None)).RequireSuccess();
                    rotations.Add(new { requestedDegrees = quarterTurn * 90 % 360,
                        orientation = string.Join('\n', orientation.Split('\n').Where(line => line.Contains("Viewport INTERNAL: displayId=0"))),
                        geometryMatches = viewport.NativeHost.GeometryMatches(), attached = viewport.NativeHost.Attached });
                }
                await session.NativeDeviceKeyAsync("home");
                var diagnostic = await tools.RunAsync(session.Options.Adb, ["-s", session.Options.Serial, "shell", "pm", "path", "com.androiddesktop.diagnostic"], TimeSpan.FromSeconds(10), CancellationToken.None);
                if (!diagnostic.Output.Contains("package:") && diagnosticApk is not null) {
                    (await tools.RunAsync(session.Options.Adb, ["-s", session.Options.Serial, "install", Path.GetFullPath(diagnosticApk)], TimeSpan.FromSeconds(60), CancellationToken.None)).RequireSuccess();
                    installedDiagnostic = true;
                    diagnostic = await tools.RunAsync(session.Options.Adb, ["-s", session.Options.Serial, "shell", "pm", "path", "com.androiddesktop.diagnostic"], TimeSpan.FromSeconds(10), CancellationToken.None);
                }
                if (diagnostic.ExitCode == 0 && diagnostic.Output.Contains("package:")) {
                    if (installedDiagnostic) (await tools.RunAsync(session.Options.Adb,["-s",session.Options.Serial,"shell","am","force-stop","com.androiddesktop.diagnostic"],TimeSpan.FromSeconds(10),CancellationToken.None)).RequireSuccess();
                    (await tools.RunAsync(session.Options.Adb, ["-s", session.Options.Serial, "shell", "am", "start", "-n", "com.androiddesktop.diagnostic/.DiagnosticActivity"], TimeSpan.FromSeconds(10), CancellationToken.None)).RequireSuccess();
                    nativeInputTestRan = true;
                    await Task.Delay(1500);
                    var before = (await tools.RunAsync(session.Options.Adb, ["-s", session.Options.Serial, "logcat", "-d", "-t", "2000", "-v", "raw", "Phase0Diagnostic:I", "*:S"], TimeSpan.FromSeconds(10), CancellationToken.None)).RequireSuccess();
                    var initialCreated = before.Split('\n').Where(l=>l.Trim().StartsWith('{')).Select(l=>JsonSerializer.Deserialize<JsonElement>(l)).LastOrDefault(r=>r.GetProperty("kind").GetString()=="created");
                    if (initialCreated.ValueKind == JsonValueKind.Object) savedTouchesBeforeInput = initialCreated.GetProperty("data").GetProperty("savedTouches").GetInt64();
                    window.Activate(); await Task.Delay(150);
                    await viewport.NativeHost.DiagnosticInputAsync(); await Task.Delay(1500);
                    hoverChecks = await window.CheckTabHoverAsync(viewport.NativeHost!);
                    viewport.NativeHost.DiagnosticKey(0x1e, true); await Task.Delay(100);
                    viewport.NativeHost.DiagnosticLoseFocus(); await Task.Delay(150);
                    await viewport.NativeHost.DiagnosticPointerAsync(.3,.4, focusRelease:true);
                    for (var quarterTurn = 1; quarterTurn <= 4; quarterTurn++) {
                        await desktop.Device.RotateCommand.ExecuteAsync(null); await Task.Delay(700);
                        await viewport.NativeHost.DiagnosticPointerAsync(.2,.3);
                        var rotationLog = (await tools.RunAsync(session.Options.Adb,["-s",session.Options.Serial,"logcat","-d","-t","2000","-v","raw","Phase0Diagnostic:I","*:S"],TimeSpan.FromSeconds(10),CancellationToken.None)).RequireSuccess();
                        var lastTouch = rotationLog.Split('\n').LastOrDefault(l=>l.Contains("\"kind\":\"touch\""));
                        pointerRotations.Add(new { requestedDegrees = quarterTurn*90%360, lastTouch, geometry=viewport.NativeHost.GeometryMatches() });
                    }
                    var after = (await tools.RunAsync(session.Options.Adb, ["-s", session.Options.Serial, "logcat", "-d", "-t", "2000", "-v", "raw", "Phase0Diagnostic:I", "*:S"], TimeSpan.FromSeconds(10), CancellationToken.None)).RequireSuccess();
                    inputEvidence = string.Join('\n', after.Split('\n').Where(line => !before.Contains(line, StringComparison.Ordinal) && (line.Contains("\"kind\":\"touch\"") || line.Contains("\"kind\":\"key\""))));
                    nativeTouch = inputEvidence.Contains("\"actionMasked\":0") && inputEvidence.Contains("\"actionMasked\":1");
                    var records = inputEvidence.Split('\n').Where(l=>l.Trim().StartsWith('{')).Select(l=>JsonSerializer.Deserialize<JsonElement>(l)).ToArray();
                    bool KeyPair(int keyCode) => records.Where(r=>r.GetProperty("kind").GetString()=="key" && r.GetProperty("data").GetProperty("keyCode").GetInt32()==keyCode)
                        .Select(r=>r.GetProperty("data").GetProperty("action").GetInt32()).Distinct().Order().SequenceEqual(new[]{0,1});
                    nativeKey = KeyPair(20); focusReleaseKey = KeyPair(29);
                    focusReleaseTouch = records.Where(r=>r.GetProperty("kind").GetString()=="touch").Count(r=>r.GetProperty("data").GetProperty("actionMasked").GetInt32()==1) >= 6;
                }
                inputStats = viewport.NativeHost.Input is { } input ? new { input.Sent, input.PeakQueue, input.MaxQueueDelayMs } : null;
                if (nativeInputTestRan && installedDiagnostic) {
                    if (savedTouchesBeforeInput is null) throw new InvalidOperationException("Fresh diagnostic save baseline was not captured.");
                    var entries = inputEvidence.Split('\n').Where(l=>l.Trim().StartsWith('{')).Select(l=>JsonSerializer.Deserialize<JsonElement>(l)).ToArray();
                    var receivedTouches = savedTouchesBeforeInput.Value + entries.Count(r=>r.GetProperty("kind").GetString()=="touch" && r.GetProperty("data").GetProperty("actionMasked").GetInt32()==0);
                    await Task.Delay(500); await session.NativeDeviceKeyAsync("home");
                    var preference = (await tools.RunAsync(session.Options.Adb,["-s",session.Options.Serial,"shell","run-as","com.androiddesktop.diagnostic","cat","shared_prefs/DiagnosticActivity.xml"],TimeSpan.FromSeconds(10),CancellationToken.None)).RequireSuccess();
                    var savedXml = System.Xml.Linq.XDocument.Parse(preference);
                    var expectedTouches = long.Parse(savedXml.Descendants("long").Single(e=>e.Attribute("name")?.Value=="touches").Attribute("value")!.Value,System.Globalization.CultureInfo.InvariantCulture);
                    var restartStop = Stopwatch.StartNew(); await desktop.Device.StopCommand.ExecuteAsync(null);
                    var stopSeconds = restartStop.Elapsed.TotalSeconds;
                    if (session.HasOwnedProcessHandles || session.LastGracefulShutdown is null) throw new InvalidOperationException("Save reopen requires confirmed graceful shutdown.");
                    await desktop.Device.StartCommand.ExecuteAsync(null);
                    if (!desktop.Device.InputReady) throw new InvalidOperationException("Save reopen start failed.");
                    (await tools.RunAsync(session.Options.Adb,["-s",session.Options.Serial,"shell","am","start","-n","com.androiddesktop.diagnostic/.DiagnosticActivity"],TimeSpan.FromSeconds(10),CancellationToken.None)).RequireSuccess();
                    await Task.Delay(1500);
                    var reopened = (await tools.RunAsync(session.Options.Adb,["-s",session.Options.Serial,"logcat","-d","-t","2000","-v","raw","Phase0Diagnostic:I","*:S"],TimeSpan.FromSeconds(10),CancellationToken.None)).RequireSuccess();
                    var reopenedCreated = reopened.Split('\n').Where(l=>l.Trim().StartsWith('{')).Select(l=>JsonSerializer.Deserialize<JsonElement>(l)).Last(r=>r.GetProperty("kind").GetString()=="created");
                    var actualTouches = reopenedCreated.GetProperty("data").GetProperty("savedTouches").GetInt64();
                    saveReopen = new { receivedTouches, expectedTouches, actualTouches, passed=expectedTouches==actualTouches, stopSeconds, note="Private diagnostic app preference survived a cold restart. This does not inspect private game saves." };
                }

                await session.NativeDeviceKeyAsync("home");
                if (installedDiagnostic) {
                    (await tools.RunAsync(session.Options.Adb, ["-s", session.Options.Serial, "uninstall", "com.androiddesktop.diagnostic"], TimeSpan.FromSeconds(30), CancellationToken.None)).RequireSuccess(); installedDiagnostic = false;
                }
            }
        }
        catch (Exception failure) { error = SupportExportService.Redact(failure.Message); renderer = session.RendererLog; }
        finally {
            if (installedDiagnostic && session.HasOwnedEmulator) {
                try { (await tools.RunAsync(session.Options.Adb, ["-s", session.Options.Serial, "uninstall", "com.androiddesktop.diagnostic"], TimeSpan.FromSeconds(30), CancellationToken.None)).RequireSuccess(); }
                catch (Exception failure) { error = (error ?? "") + " Diagnostic cleanup: " + SupportExportService.Redact(failure.Message); }
            }
            try { var shutdownWatch = Stopwatch.StartNew(); double lastTick = 0;
                var shutdownTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Input,
                    (_, _) => { var now = shutdownWatch.Elapsed.TotalMilliseconds; shutdownUiIntervals.Add(now - lastTick); lastTick = now; }, window.Dispatcher);
                shutdownTimer.Start();
                try { if (session.HasOwnedProcessHandles) await desktop.Device.StopCommand.ExecuteAsync(null); } finally { shutdownTimer.Stop(); }
                shutdownSeconds = shutdownWatch.Elapsed.TotalSeconds; restored = viewport.NativeHost is null || !viewport.NativeHost.Attached; }
            catch (Exception failure) { error = (error ?? "") + " Shutdown: " + SupportExportService.Redact(failure.Message); }
        }
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new { mode, workload, requestedSampleSeconds = seconds,
            ready, error, graphics, rendererLog = renderer, samples, dispatcherDelayMs = dispatcherDelays, retainedDuringResizeAndNavigation = retained,
            nativeDetached = restored, nativeDpi, nativeFocus, nativeInputTestRan, nativeTouch, nativeKey, nativeKeyboardSubmitted = viewport.NativeHost?.DiagnosticKeyboardSubmitted, inputEvidence, preservedPackage, resizeCount = viewport.NativeHost?.ResizeCount,
            startupSeconds, shutdownSeconds, shutdownUiIntervalsMs = shutdownUiIntervals, darkMargins, rotationCommandEnabled, rotations,
            shutdownDiagnostics = session.ShutdownDiagnostics,
            dpiChecks, hoverChecks, inputStats, gpuSamples, saveReopen, pointerRotations, focusReleaseKey, focusReleaseTouch, guestLayer = layer, guestPresentTimestampsNs = guestFrames,

            forwardedKeyDowns = viewport.NativeHost?.ForwardedKeyDowns, forwardedKeyUps = viewport.NativeHost?.ForwardedKeyUps,
            keyboardSuppression = viewport.NativeHost?.DiagnosticKeyboardSuppression,
            hiddenBeforeNativeAttach = viewport.NativeHost is not null && !viewport.NativeHost.VisibleBeforeAttach,
            visibleDetachedEvents = viewport.NativeHost?.VisibleDetachedEvents, startupProgressAtReady = desktop.Device.StartupProgress,
            gracefulStop = session.LastGracefulShutdown is not null, ownedProcessHandlesAfterStop = session.HasOwnedProcessHandles,
            limitations = "Guest SurfaceFlinger timestamps measure Android buffer presentation, not Windows displayed FPS. Synthetic pointer and scan-code tests do not replace physical keyboard/IME testing. Only recorded monitor entries verify physical DPI. Dispatcher delay and input queue delay are not input-to-visible latency. A game launch alone does not prove gameplay; inspect its screenshot." }, new JsonSerializerOptions { WriteIndented = true }));
    }
}

