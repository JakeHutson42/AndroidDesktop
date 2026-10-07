using AndroidDesktop.Models;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AndroidDesktop.Services;

public sealed class EmulatorSessionService(PrototypeOptions options, IAndroidToolService tools, EvidenceService evidence)
{
    private readonly SemaphoreSlim _operation = new(1, 1);
    private OwnedProcess? _emulator, _gateway;
    private ActiveDeviceLease? _lease;
    private HttpClient? _http;
    private string? _token;
    private string? _deviceId;
    public string? DeviceId => _deviceId;
    public string RuntimeIdentity { get; private set; } = "";
    public ShutdownReceipt? LastGracefulShutdown { get; private set; }
    private string _backupRuntimeIdentity = "";
    private List<ActiveDeviceLease.Owner> _shutdownChildren = [];
    private bool _stopFailed;
    public bool ForceStopAvailable => _stopFailed && HasOwnedProcessHandles;
    public PrototypeOptions Options => options;
    public SessionState State { get; private set; } = string.IsNullOrWhiteSpace(options.SdkRoot) ? SessionState.NotConfigured : SessionState.Stopped;
    public bool HasOwnedEmulator => _emulator is not null && !_emulator.Process.HasExited;
    public bool HasOwnedProcessHandles => _emulator is not null || _gateway is not null;
    public event Action<SessionState>? StateChanged;
    public event Action<string>? Message;
    public void Configure(PrototypeOptions updated)
    {
        if (HasOwnedProcessHandles || _operation.CurrentCount == 0) throw new InvalidOperationException("Stop the device before changing its setup.");
        updated.Validate(); options = updated;
        _deviceId = null; RuntimeIdentity = "";
        LastGracefulShutdown = null;
        _backupRuntimeIdentity = "";
        SetState(string.IsNullOrWhiteSpace(options.SdkRoot) ? SessionState.NotConfigured : SessionState.Stopped);
    }
    public (int Port, string Token) Connection => _token is null ? throw new InvalidOperationException("Gateway is not ready.") : (options.GatewayPort, _token);

    private void SetState(SessionState state) { State = state; StateChanged?.Invoke(state); evidence.TryWrite("session", state.ToString()); }
    private void Report(string message) => Message?.Invoke(message);
    private async Task OperationAsync(Func<CancellationToken, Task> action, CancellationToken token, bool retainReadyOnFailure = false)
    {
        if (!await _operation.WaitAsync(0, token)) throw new InvalidOperationException("Another session operation is active.");
        var previous = State;
        try { await action(token); }
        catch (Exception error) {
            if (State == SessionState.Stopping && HasOwnedProcessHandles) _stopFailed = true;
            SetState(retainReadyOnFailure && State != SessionState.Faulted && HasOwnedEmulator && previous is SessionState.Ready or SessionState.Running ? previous : SessionState.Faulted);
            Report(error.Message);
            if (!HasOwnedProcessHandles) { ReleaseLease(); await evidence.StopAsync(); }
            throw;
        }
        finally { _operation.Release(); }
    }

    public Task StartAsync(CancellationToken token) => OperationAsync(async ct =>
    {
        if (_emulator is not null) throw new InvalidOperationException("Stop the previous owned session before starting another.");
        _stopFailed = false;
        LastGracefulShutdown = null;
        RuntimeIdentity = ""; _backupRuntimeIdentity = ""; _shutdownChildren = [];
        options.Validate();
        if (DeviceBackupService.HasPendingRestore(options.AvdHome)) throw new InvalidOperationException("A restore transaction is pending. Use Recover interrupted restore in Setup before starting this device.");
        foreach (var file in new[] { options.Adb, options.Emulator })
            if (!File.Exists(file)) throw new FileNotFoundException("Missing Android SDK tool. Follow docs/phase-0/RUNBOOK.md.", file);
        var iniPath = Path.Combine(options.AvdHome, options.AvdName + ".ini");
        if (!File.Exists(iniPath)) throw new FileNotFoundException("Create the Phase 0 AVD using the runbook; other AVD profiles are never modified.", iniPath);
        var avdIni = ReadProperties(iniPath);
        if (!avdIni.TryGetValue("path", out var devicePath) || !Path.GetFullPath(devicePath).StartsWith(
            Path.GetFullPath(options.AvdHome).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("AVD .ini must point to a device directory inside the application-owned AVD home.");
        _lease ??= ActiveDeviceLease.Acquire(Path.Combine(SettingsStore.DataRoot, "active-device.lock"));
        _deviceId = new DeviceStorageService().GetOrCreateIdentity(options, devicePath);
        foreach (var port in new[] { options.EmulatorPort, options.EmulatorPort + 1, options.GrpcPort, options.GatewayPort }) EnsurePortAvailable(port);
        evidence.Start(options);
        Report("Phase 0 evidence directory: " + evidence.DirectoryPath);
        SetState(SessionState.Starting);
        var acceleration = await tools.RunAsync(options.Emulator, ["-accel-check"], TimeSpan.FromSeconds(20), ct);
        evidence.TryWrite("acceleration", acceleration);
        acceleration.RequireSuccess();
        var version = await tools.RunAsync(options.Emulator, ["-version"], TimeSpan.FromSeconds(15), ct);
        evidence.TryWrite("emulatorVersion", version);
        var configuration = await File.ReadAllTextAsync(Path.Combine(devicePath, "config.ini"), ct);
        evidence.TryWrite("avdConfiguration", configuration);
        var runtimeIdentity = new Dictionary<string, string> { ["emulatorVersion"] = SupportExportService.Redact(version.RequireSuccess()),
            ["avdConfigurationSha256"] = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(configuration))) };
        if (options.MemoryMb is { } memory) runtimeIdentity["guestMemoryMb"] = memory.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (options.CpuCores is { } cores) runtimeIdentity["guestCpuCores"] = cores.ToString(System.Globalization.CultureInfo.InvariantCulture);
        runtimeIdentity["displayTransport"] = options.DisplayTransport;
        Report("Starting persistent Android device; waiting for sys.boot_completed…");
        var args = new List<string> { "-avd", options.AvdName, "-port", options.EmulatorPort.ToString(),
            "-grpc", options.GrpcPort.ToString(), "-grpc-use-token", "-gpu", "auto", "-no-snapshot-load" };
        if (!options.ShowStandaloneWindow) args.Add("-no-window");
        // Embedded audio owns playback; avoid a second native host output path.
        if (!options.ShowStandaloneWindow && options.DisplayTransport == "controller") args.Add("-no-audio");
        args.AddRange(ResourceArguments(options));
        // No -wipe-data. Cold boot makes the baseline explicit; persistent device disks remain intact.
        _emulator = new OwnedProcess(AndroidToolService.CreateStartInfo(options.Emulator, args,
            new Dictionary<string, string> { ["ANDROID_AVD_HOME"] = options.AvdHome }));
        _lease.Track(_emulator.Process);
        Monitor(_emulator, "Emulator");
        evidence.SetOwnedRoots(Environment.ProcessId, _emulator.Id);
        using var bootDeadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        bootDeadline.CancelAfter(TimeSpan.FromMinutes(3));
        try { while (true)
        {
            if (_emulator.Process.HasExited) throw new InvalidOperationException("Emulator exited before boot:\n" + _emulator.Tail);
            var boot = await tools.RunAsync(options.Adb, ["-s", options.Serial, "shell", "getprop", "sys.boot_completed"],
                TimeSpan.FromSeconds(5), bootDeadline.Token);
            if (boot.ExitCode == 0 && boot.Output.Trim() == "1") break;
            await Task.Delay(1000, bootDeadline.Token);
        } } catch (OperationCanceledException) when (!ct.IsCancellationRequested) {
            throw new TimeoutException("Android did not boot within three minutes. Stop the owned device, check acceleration, storage and image compatibility in Setup, then retry. Device disks are retained.");
        }
        foreach (var prop in new[] { "ro.build.version.sdk", "ro.product.cpu.abilist", "ro.build.fingerprint" }) {
            var value = (await tools.RunAsync(options.Adb, ["-s", options.Serial, "shell", "getprop", prop], TimeSpan.FromSeconds(10), ct)).RequireSuccess();
            runtimeIdentity[prop] = value; evidence.TryWrite(prop, value);
        }
        RuntimeIdentity = JsonSerializer.Serialize(runtimeIdentity);
        try { _backupRuntimeIdentity = await BackupIdentityAsync(ct); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { Report("Backup compatibility could not be established: " + error.Message); }
        SetState(SessionState.Ready);
        Report(options.DisplayTransport == "controller" ? "Android booted. Connect the embedded controller display." : "Android booted. Connect display to probe the provisional RTC transport.");
    }, token);

    public static IEnumerable<string> ResourceArguments(PrototypeOptions options)
    {
        options.Validate();
        if (options.MemoryMb is { } memory) { yield return "-memory"; yield return memory.ToString(System.Globalization.CultureInfo.InvariantCulture); }
        if (options.CpuCores is { } cores) { yield return "-cores"; yield return cores.ToString(System.Globalization.CultureInfo.InvariantCulture); }
    }

    public Task ConnectGatewayAsync(CancellationToken token) => OperationAsync(async ct =>
    {
        if (!HasOwnedEmulator) throw new InvalidOperationException("Start the owned emulator first.");
        await StopGatewayAsync(ct);
        if (!File.Exists(options.PythonExecutable) || !File.Exists(Path.Combine(options.GatewayRoot, "run.py")))
            throw new FileNotFoundException("Prepare the pinned Python gateway and set its paths in phase0.local.json.");
        var gatewayVersions = await tools.RunAsync(options.PythonExecutable, ["-B", Path.Combine(options.GatewayRoot, "run.py"), "--self-test"], TimeSpan.FromSeconds(15), ct);
        gatewayVersions.RequireSuccess(); evidence.TryWrite("gatewayVersions", gatewayVersions.Output);
        var discovery = await FindDiscoveryAsync(ct);
        _token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var environment = new Dictionary<string, string> {
            ["ANDROID_DESKTOP_SESSION_TOKEN"] = _token, ["ANDROID_DESKTOP_DISCOVERY_FILE"] = discovery,
            ["ANDROID_DESKTOP_GATEWAY_PORT"] = options.GatewayPort.ToString(), ["PYTHONUNBUFFERED"] = "1" };
        var info = AndroidToolService.CreateStartInfo(options.PythonExecutable, ["-B", Path.Combine(options.GatewayRoot, "run.py")], environment);
        info.WorkingDirectory = options.GatewayRoot;
        _gateway = new OwnedProcess(info);
        _lease!.Track(_gateway.Process);
        Monitor(_gateway, "Gateway");
        evidence.SetOwnedRoots(Environment.ProcessId, _emulator!.Id, _gateway.Id);
        _http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{options.GatewayPort}/"), Timeout = TimeSpan.FromSeconds(15) };
        _http.DefaultRequestHeaders.Authorization = new("Bearer", _token);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(25));
        while (true)
        {
            if (_gateway.Process.HasExited) throw new InvalidOperationException("Gateway failed:\n" + _gateway.Tail);
            try {
                using var response = await _http.GetAsync("api/v1/emulator/status", deadline.Token);
                response.EnsureSuccessStatusCode();
                using var statusDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync(deadline.Token));
                evidence.TryWrite("emulatorStatus", statusDocument.RootElement.Clone());
                break;
            }
            catch (HttpRequestException) { await Task.Delay(500, deadline.Token); }
        }
        Report(options.DisplayTransport == "controller" ? "Checking authenticated controller frames…" : "Invoking RTC service; an unimplemented response fails this candidate.");
        using var probe = await _http.GetAsync(options.DisplayTransport == "controller" ? "probe-controller" : "probe", ct);
        var probeText = await probe.Content.ReadAsStringAsync(ct);
        evidence.TryWrite("rtcProbe", new { status = (int)probe.StatusCode, body = probeText });
        if (!probe.IsSuccessStatusCode) throw new InvalidOperationException("Display service probe failed.\n" + probeText);
        SetState(SessionState.Ready);
        Report(options.DisplayTransport == "controller" ? "Controller accepted. Embedded display is limited to 15 fps; enable audio for the separate PCM stream." : "RTC request accepted. Video, audio and input still require end-to-end validation.");
    }, token);

    public async Task<ApkSelection> ImportAsync(string apkPath, ApkSelection? previous, bool relaunch,
        Func<ApkSelection, Task<bool>> commit, CancellationToken token,
        Func<string, string, ApkSelection?>? lookup = null, string? expectedPackage = null)
    {
        ApkSelection? selected = null;
        await OperationAsync(async ct => {
            if (!HasOwnedEmulator || State is not (SessionState.Ready or SessionState.Running)) throw new InvalidOperationException("Device must be ready to open an APK.");
            SetState(SessionState.Installing);
            using var installer = new ApkInstallService(tools, options);
            var importer = new ApkImportService(installer);
            // Direct progress preserves session message ordering without a second UI queue.
            selected = await importer.OpenAsync(apkPath, _deviceId!, previous, relaunch, commit, new SessionProgress(Report), ct, lookup, expectedPackage);
            evidence.TryWrite("apk", selected);
            SetState(SessionState.Running);
            Report($"Running {selected.Apk.AppName} ({selected.Apk.PackageId}).");
        }, token, retainReadyOnFailure: true);
        return selected!;
    }
    private sealed class SessionProgress(Action<string> report) : IProgress<string> { public void Report(string value) => report(value); }

    public Task RotateAsync(int degrees, CancellationToken token) => OperationAsync(async ct =>
    {
        if (_http is null || !HasOwnedEmulator) throw new InvalidOperationException("Connect the display first.");
        using var body = new StringContent(JsonSerializer.Serialize(new { degrees }), Encoding.UTF8, "application/json");
        using var response = await _http.PostAsync("rotate", body, ct);
        response.EnsureSuccessStatusCode();
        evidence.TryWrite("rotationRequested", degrees);
    }, token, retainReadyOnFailure: true);

    public Task StopAsync(CancellationToken token) => OperationAsync(async ct =>
    {
        LastGracefulShutdown = null;
        var wasRunning = HasOwnedEmulator && !string.IsNullOrWhiteSpace(RuntimeIdentity);
        var descendants = ProcessTree.Descendants(new[] { _emulator, _gateway }.Where(p => p is not null && !p.Process.HasExited).Select(p => p!.Id).ToArray());
        var owners = _shutdownChildren;
        foreach (var id in descendants) {
            try { using var process = System.Diagnostics.Process.GetProcessById(id); var owner = new ActiveDeviceLease.Owner(id, process.StartTime.ToUniversalTime().Ticks);
                if (!owners.Contains(owner)) owners.Add(owner); _lease?.Track(process); }
            catch (ArgumentException) { }
        }
        SetState(SessionState.Stopping);
        await StopGatewayAsync(ct);
        if (HasOwnedEmulator)
        {
            Report("Requesting graceful emulator shutdown…");
            (await tools.RunAsync(options.Adb, ["-s", options.Serial, "emu", "kill"], TimeSpan.FromSeconds(10), ct)).RequireSuccess();
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
            deadline.CancelAfter(TimeSpan.FromSeconds(30));
            try { await _emulator!.WaitAsync(deadline.Token); }
            catch (OperationCanceledException) { throw new TimeoutException("Emulator did not stop in 30 seconds. It remains owned; Force stop is an explicit recovery action."); }
        }
        foreach (var owner in owners) {
            try { using var process = System.Diagnostics.Process.GetProcessById(owner.Pid);
                if (!process.HasExited && process.StartTime.ToUniversalTime().Ticks == owner.StartedUtcTicks)
                    throw new IOException("An owned device descendant remains running. Backup consistency is unconfirmed."); }
            catch (ArgumentException) { }
        }
        _emulator?.Dispose(); _emulator = null; _shutdownChildren = [];
        if (wasRunning && !string.IsNullOrWhiteSpace(_backupRuntimeIdentity)) LastGracefulShutdown = new(options.ProfileId, _deviceId!, _backupRuntimeIdentity, DateTimeOffset.UtcNow);
        ReleaseLease();
        _stopFailed = false;
        SetState(SessionState.Stopped); Report("Stopped. Device disk data retained; save integrity still needs a reopen test.");
        await evidence.StopAsync();
    }, token);

    public Task ForceStopAsync(CancellationToken token) => OperationAsync(async ct =>
    {
        LastGracefulShutdown = null;
        if (!ForceStopAvailable) throw new InvalidOperationException("Try graceful Stop first. Force stop is available only after shutdown fails or times out.");
        if (_gateway is not null) { await _gateway.ForceStopAsync(); _gateway.Dispose(); _gateway = null; }
        _http?.Dispose(); _http = null; _token = null;
        if (_emulator is not null) { await _emulator.ForceStopAsync(); _emulator.Dispose(); _emulator = null; }
        foreach (var owner in _shutdownChildren) {
            try { using var process = System.Diagnostics.Process.GetProcessById(owner.Pid);
                if (!process.HasExited && process.StartTime.ToUniversalTime().Ticks == owner.StartedUtcTicks) { process.Kill(true); await process.WaitForExitAsync(ct); } }
            catch (ArgumentException) { }
        }
        _shutdownChildren = [];
        ReleaseLease();
        _stopFailed = false;
        SetState(SessionState.Stopped); Report("Owned processes forcibly stopped. Validate game saves before continuing.");
        await evidence.StopAsync();
    }, token);

    private void ReleaseLease() { var lease = _lease; _lease = null; lease?.Dispose(); }
    private async Task<string> BackupIdentityAsync(CancellationToken token)
    {
        var path = Path.Combine(options.AvdHome, options.AvdName + ".ini");
        var device = ReadProperties(path).GetValueOrDefault("path") ?? throw new InvalidDataException("Device path is missing.");
        var config = ReadProperties(Path.Combine(device, "config.ini"));
        var image = config.GetValueOrDefault("image.sysdir.1") ?? throw new InvalidDataException("Device system-image dependency is missing.");
        var package = Path.Combine(options.SdkRoot, image, "package.xml");
        async Task<string> Hash(string file) { await using var stream = File.OpenRead(file); return Convert.ToHexString(await SHA256.HashDataAsync(stream, token)); }
        return JsonSerializer.Serialize(new { runtime = RuntimeIdentity, configurationSha256 = await Hash(Path.Combine(device, "config.ini")),
            emulatorSha256 = await Hash(options.Emulator), imagePackageSha256 = await Hash(package) });
    }
    public async Task<BackupTarget> BackupTargetAsync(DeviceProfile profile, CancellationToken token)
    {
        if (HasOwnedProcessHandles || LastGracefulShutdown is null || profile.Id != options.ProfileId || profile.Runtime != options)
            throw new InvalidOperationException("Start and gracefully stop the active profile before backup or restore. Forced/unknown shutdown cannot establish consistency.");
        var identity = await BackupIdentityAsync(token);
        return new(options.AvdHome, options.ProfileId, options.AvdName, _deviceId!, identity, profile);
    }
    public void InvalidateShutdownReceipt() => LastGracefulShutdown = null;
    public void ReportViewportFault(string message) { if (HasOwnedEmulator) SetState(SessionState.Faulted); Report(message); }
    private void Monitor(OwnedProcess process, string name)
    {
        _ = Task.Run(async () => {
            try {
                await process.WaitAsync(CancellationToken.None);
                if (State is not (SessionState.Stopped or SessionState.Stopping) &&
                    (ReferenceEquals(_emulator, process) || ReferenceEquals(_gateway, process)))
                { SetState(SessionState.Faulted); Report(name + " exited. Stop or reconnect the owned session; device data is retained."); }
            } catch (ObjectDisposedException) { }
        });
    }

    private async Task StopGatewayAsync(CancellationToken token)
    {
        if (_gateway is not null)
        {
            try {
                if (!_gateway.Process.HasExited && _http is not null) {
                    using var response = await _http.PostAsync("shutdown", null, token);
                }
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
                deadline.CancelAfter(TimeSpan.FromSeconds(5));
                await _gateway.WaitAsync(deadline.Token);
            }
            catch (Exception e) when (e is HttpRequestException or OperationCanceledException) {
                throw new TimeoutException("Gateway could not shut down gracefully. Try Stop; if that fails, explicitly Force stop the owned session. Device data is retained.", e);
            }
            _gateway.Dispose(); _gateway = null;
        }
        _http?.Dispose(); _http = null; _token = null;
    }

    private async Task<string> FindDiscoveryAsync(CancellationToken token)
    {
        var directories = new[] { options.DiscoveryDirectory,
            Path.Combine(Path.GetTempPath(), "avd", "running"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp", "avd", "running"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".android", "avd", "running") };
        for (var attempt = 0; attempt < 10; attempt++)
        {
            foreach (var directory in directories.Where(d => d is not null && Directory.Exists(d)))
            foreach (var file in Directory.EnumerateFiles(directory!, "*.ini"))
            {
                if (_emulator is null || File.GetLastWriteTimeUtc(file) < _emulator.Process.StartTime.ToUniversalTime().AddSeconds(-2)) continue;
                var props = ReadProperties(file);
                if (props.GetValueOrDefault("grpc.port") == options.GrpcPort.ToString() &&
                    !string.IsNullOrWhiteSpace(props.GetValueOrDefault("grpc.token"))) return file;
            }
            await Task.Delay(500, token);
        }
        throw new FileNotFoundException("No token-authenticated emulator discovery file found. Set DiscoveryDirectory to the emulator’s avd/running directory; see the runbook.");
    }
    private static Dictionary<string, string> ReadProperties(string path)
    {
        var properties = new Dictionary<string, string>();
        foreach (var line in File.ReadLines(path).Where(l => !l.TrimStart().StartsWith('#') && l.Contains('='))) {
            var parts = line.Split('=', 2); properties[parts[0].Trim()] = parts[1].Trim();
        }
        return properties;
    }
    private static void EnsurePortAvailable(int port)
    {
        var listener = new TcpListener(IPAddress.Loopback, port);
        try { listener.Start(); } catch (SocketException) { throw new InvalidOperationException($"Port {port} is already occupied; choose unused ports. Existing devices are not stopped."); }
        finally { listener.Stop(); }
    }
}
