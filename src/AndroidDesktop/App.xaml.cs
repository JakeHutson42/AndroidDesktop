using AndroidDesktop.Adapters.Viewport;
using AndroidDesktop.Models;
using AndroidDesktop.Services;
using AndroidDesktop.ViewModels;
using System.IO;
using System.Windows;
namespace AndroidDesktop;
public partial class App : Application
{
 private Mutex? _instance;
 protected override async void OnStartup(StartupEventArgs e)
 {
  base.OnStartup(e);
  new ThemeService().Apply("Graphite");
  var key = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(SettingsStore.DataRoot.ToUpperInvariant())));
  _instance = new Mutex(true, "Local\\AndroidDesktop-" + key, out var first);
  if (!first) { _instance.Dispose(); _instance = null; MessageBox.Show("Android Desktop is already open. Use that window to switch profiles; only one device may be active.", "Android Desktop"); Shutdown(); return; }
  var startup = System.Diagnostics.Stopwatch.StartNew();
  PrototypeOptions? imported = null; string? warning = null;
  var smoke = e.Args.Length >= 2 && e.Args[0] == "--smoke-test";
  var embeddedTest = e.Args.Length >= 3 && e.Args[0] == "--embedded-test";
  var startDevice = e.Args.Contains("--start-device");
  var config = smoke || embeddedTest || startDevice ? null : e.Args.FirstOrDefault();
  if (config is not null) try { imported = await PrototypeOptions.LoadAsync(config); } catch (Exception error) { warning = "Configuration import failed: " + error.Message; }
  var options = new DesktopSettings().Runtime;
  if (smoke && e.Args.Contains("--composition")) options = options with { ViewportMode = "composition" };
  var tools = new AndroidToolService(); var evidence = new EvidenceService();
  var session = new EmulatorSessionService(options, tools, evidence);
  var viewport = new WebRtcViewport(options.ViewportMode, smoke ? Path.GetFullPath(e.Args[1]) + ".webview" : null);
  var viewModel = new PrototypeViewModel(session, viewport, evidence);
  var desktop = new DesktopViewModel(viewModel, new SettingsStore(SettingsStore.DataRoot), new SetupService(tools, new DeviceStorageService()), session, persistent: !smoke);
  desktop.RuntimeConfigured += configured => { viewport.ConfigureMode(configured.ViewportMode); viewport.ConfigureTransport(configured.DisplayTransport); };
  viewModel.CommitSelection = desktop.CommitSelectionAsync;
  if (smoke) viewModel.Busy = false;
  var window = new MainWindow(desktop, viewport); MainWindow = window;
  var smokeStarted = false;
  window.ContentRendered += async (_, _) => {
   var shellMs = startup.Elapsed.TotalMilliseconds;
   viewModel.AddMessage($"Shell content rendered in {shellMs:0} ms; this is not an emulator/media performance result.");
   if (!smoke || smokeStarted) return;
   smokeStarted = true;
   object result;
   try {
    await desktop.InitializeAsync(options);
    await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    if (!e.Args.Contains("--shell-only")) await viewport.InitializeAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(20));
    var shellChecks = await window.CheckShellSmokeAsync(e.Args.Contains("--theme-preview") ? e.Args[1] + ".views" : null);
    result = new { shellRendered = true, shellMilliseconds = shellMs, packagedViewportLoaded = !e.Args.Contains("--shell-only"), shellChecks, browserVersion = viewport.BrowserVersion,
       viewportMode = options.ViewportMode, androidConnected = false, mediaVerified = false };
   } catch (Exception failure) {
    result = new { shellRendered = true, packagedViewportLoaded = false, error = failure.ToString() };
   }
   await File.WriteAllTextAsync(e.Args[1], System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
   window.Close();
  };
  window.Show(); if (warning is not null) viewModel.AddMessage(warning);
  if (!smoke) try {
   await desktop.InitializeAsync(imported);
   if (embeddedTest) {
    object report;
    try {
     if (session.Options.ShowStandaloneWindow || session.Options.DisplayTransport != "controller") throw new InvalidOperationException("Configure embedded controller mode before this explicit real-device test.");
     await viewModel.StartCommand.ExecuteAsync(null);
     if (!viewModel.InputReady) throw new InvalidOperationException(viewModel.Log);
     if (e.Args.Contains("--apk-test")) {
      await Task.Delay(2000);
      await viewport.CaptureDecodedFrameAsync(Path.GetFullPath(e.Args[1]) + ".startup.frame.png");
     }
     viewModel.CommitSelection = _ => Task.FromResult(true); // Test APK must not replace persistent library metadata.
     await viewModel.ImportFilesAsync([Path.GetFullPath(e.Args[2])], CancellationToken.None);
     if (viewModel.Selection is null) throw new InvalidOperationException(viewModel.Log);
     if (e.Args.Contains("--apk-test")) {
      await Task.Delay(15000);
      var package = viewModel.Selection.Apk.PackageId;
      var installed = (await tools.RunAsync(session.Options.Adb, ["-s", session.Options.Serial, "shell", "pm", "path", package], TimeSpan.FromSeconds(15), CancellationToken.None)).RequireSuccess();
      var activity = (await tools.RunAsync(session.Options.Adb, ["-s", session.Options.Serial, "shell", "dumpsys", "activity", "activities"], TimeSpan.FromSeconds(15), CancellationToken.None)).RequireSuccess();
      var process = await tools.RunAsync(session.Options.Adb, ["-s", session.Options.Serial, "shell", "pidof", package], TimeSpan.FromSeconds(15), CancellationToken.None);
      await viewport.CapturePreviewAsync(Path.GetFullPath(e.Args[1]) + ".png");
      await viewport.CaptureDecodedFrameAsync(Path.GetFullPath(e.Args[1]) + ".frame.png");
      var screen = "/data/local/tmp/android-desktop-test-" + Environment.ProcessId + ".png";
      (await tools.RunAsync(session.Options.Adb, ["-s", session.Options.Serial, "shell", "screencap", "-p", screen], TimeSpan.FromSeconds(15), CancellationToken.None)).RequireSuccess();
      (await tools.RunAsync(session.Options.Adb, ["-s", session.Options.Serial, "pull", screen, Path.GetFullPath(e.Args[1]) + ".android.png"], TimeSpan.FromSeconds(15), CancellationToken.None)).RequireSuccess();
      await tools.RunAsync(session.Options.Adb, ["-s", session.Options.Serial, "shell", "rm", screen], TimeSpan.FromSeconds(15), CancellationToken.None);
      await tools.RunAsync(session.Options.Adb, ["-s", session.Options.Serial, "shell", "input", "keyevent", "KEYCODE_HOME"], TimeSpan.FromSeconds(15), CancellationToken.None);
      await Task.Delay(3000);
      await viewport.CaptureDecodedFrameAsync(Path.GetFullPath(e.Args[1]) + ".home.png");
      await File.WriteAllTextAsync(Path.GetFullPath(e.Args[1]) + ".activity.txt", activity);
      report = new { embeddedFramesAndInputReady = viewModel.InputReady, package, installed = installed.Trim(), processRunning = process.ExitCode == 0, processId = process.Output.Trim(), apk = viewModel.Selection.Apk, gameplayVerified = false };
     } else {
     await Task.Delay(2000);
     var before = (await tools.RunAsync(session.Options.Adb, ["-s", session.Options.Serial, "logcat", "-d", "-t", "500", "-v", "raw", "Phase0Diagnostic:I", "*:S"], TimeSpan.FromSeconds(15), CancellationToken.None)).RequireSuccess();
     await viewport.DiagnosticTapAsync(); await Task.Delay(1000);
     if(e.Args.Contains("--audio-test")) { viewModel.EnableAudioCommand.Execute(null); await viewport.DiagnosticTapAsync(); await Task.Delay(5000); }
     await viewport.CapturePreviewAsync(Path.GetFullPath(e.Args[1]) + ".png");
     var logcat = await tools.RunAsync(session.Options.Adb, ["-s", session.Options.Serial, "logcat", "-d", "-t", "500", "-v", "raw", "Phase0Diagnostic:I", "*:S"], TimeSpan.FromSeconds(15), CancellationToken.None);
     var log = logcat.RequireSuccess();
     await File.WriteAllTextAsync(Path.GetFullPath(e.Args[1]) + ".diagnostic.txt", log);
     var connected = viewModel.InputReady;
     var media = viewModel.Media;
     await viewModel.StopCommand.ExecuteAsync(null);
     report = new { embeddedFramesAndInputReady = connected, media, diagnosticPackage = viewModel.Selection.Apk.PackageId,
       gracefulStop = session.LastGracefulShutdown is not null, separateEmulatorWindow = false,
       newTouchReceived = System.Text.RegularExpressions.Regex.Matches(log, "\"kind\":\"touch\"").Count > System.Text.RegularExpressions.Regex.Matches(before, "\"kind\":\"touch\"").Count,
       gamePerformanceVerified = false, audioVerified = false };
     }
    } catch (Exception error) { report = new { embeddedFramesAndInputReady = false, error = SupportExportService.Redact(error.Message) }; }
    finally { if (session.HasOwnedProcessHandles) await viewModel.StopCommand.ExecuteAsync(null); }
    await File.WriteAllTextAsync(Path.GetFullPath(e.Args[1]), System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    window.Close();
   }
   else await desktop.StartHomeAsync();
  } catch (Exception error) { viewModel.Busy = false; viewModel.AddMessage("Setup initialization failed: " + error.Message); }
 }
 protected override void OnExit(ExitEventArgs e) { if (_instance is not null) { _instance.ReleaseMutex(); _instance.Dispose(); } base.OnExit(e); }
}
