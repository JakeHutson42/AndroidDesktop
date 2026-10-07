using AndroidDesktop.Models;
using AndroidDesktop.Services;
using System.IO;
using System.Text.Json;

// Explicit local acceptance harness: uses the application's services and preserves all device data.
var root = Path.GetFullPath(args[0]);
if (System.Diagnostics.Process.GetProcessesByName("AndroidDesktop").Length != 0)
    throw new InvalidOperationException("Close Android Desktop before this setup/acceptance harness writes its settings.");
var report = new Dictionary<string, object?>();
var store = new SettingsStore(SettingsStore.DataRoot);
var settings = await store.LoadAsync();
var java = Directory.EnumerateDirectories(Path.Combine(root, "artifacts/android-setup/java"))
    .Single(p => File.Exists(Path.Combine(p, "bin/java.exe")));
var options = settings.Runtime with {
    SdkRoot = Path.Combine(root, "artifacts/android-sdk"),
    CommandLineToolsRoot = Path.Combine(root, "artifacts/android-sdk/cmdline-tools/latest"),
    JavaExecutable = Path.Combine(java, "bin/java.exe"),
    GatewayRoot = Path.Combine(root, "tools/gateway"),
    PythonExecutable = Path.Combine(root, "tools/gateway/.venv/Scripts/python.exe"),
    EvidenceRoot = Path.Combine(root, "artifacts/android-setup/evidence"),
    MemoryMb = 2048, CpuCores = 2, ShowStandaloneWindow = args.Contains("--standalone"),
    DisplayTransport = args.Contains("--controller") ? "controller" : "webrtc"
};
options.Validate();
var tools = new AndroidToolService();
var progress = new Progress<string>(Console.WriteLine);
using (var lease = ActiveDeviceLease.Acquire(Path.Combine(SettingsStore.DataRoot, "active-device.lock"))) {
    await new SetupService(tools, new DeviceStorageService()).CreateDeviceAsync(options,
        "system-images;android-34;google_apis_playstore;x86_64", progress, CancellationToken.None);
    if (!await store.SaveAsync(settings with { Runtime = options, SystemImage = "system-images;android-34;google_apis_playstore;x86_64" }))
        throw new IOException("Could not persist Android setup.");
}
await File.WriteAllTextAsync(Path.Combine(root, "android.local.json"), JsonSerializer.Serialize(options, new JsonSerializerOptions { WriteIndented = true }));
report["configured"] = true;
var session = new EmulatorSessionService(options, tools, new EvidenceService());
session.Message += Console.WriteLine;
var nativeViewport = new NoEmbeddedViewport();
var nativeViewModel = options.ShowStandaloneWindow ? new AndroidDesktop.ViewModels.PrototypeViewModel(
    session, nativeViewport, new EvidenceService()) { CommitSelection = _ => Task.FromResult(true) } : null;
try {
    if (nativeViewModel is null) await session.StartAsync(CancellationToken.None);
    else {
        await nativeViewModel.StartCommand.ExecuteAsync(null);
        if (session.State != SessionState.Ready) throw new InvalidOperationException(nativeViewModel.Log);
        report["appStartCommand"] = true;
    }
    report["androidBooted"] = true;
    report["runtimeIdentity"] = session.RuntimeIdentity;
    var diagnostic = Path.Combine(root, "diagnostic-apk/build/Phase0Diagnostic.apk");
    if (File.Exists(diagnostic)) {
        try {
            ApkSelection selected;
            if (nativeViewModel is null) selected = await session.ImportAsync(diagnostic, null, true, _ => Task.FromResult(true), CancellationToken.None);
            else {
                await nativeViewModel.ImportFilesAsync([diagnostic], CancellationToken.None);
                selected = nativeViewModel.Selection ?? throw new InvalidOperationException(nativeViewModel.Log);
                report["appImportCommand"] = true;
                report["embeddedConnections"] = nativeViewport.Connections;
            }
            report["diagnosticInstalledAndLaunched"] = selected.Apk.PackageId;
        } catch (Exception error) { report["diagnosticFailure"] = SupportExportService.Redact(error.Message); }
    }
    if (!options.ShowStandaloneWindow) {
        try { await session.ConnectGatewayAsync(CancellationToken.None); report["rtcProbe"] = "passed"; }
        catch (Exception error) { report["rtcProbe"] = "failed: " + SupportExportService.Redact(error.Message); }
    } else report["displayMode"] = "native emulator window; embedded RTC bypassed explicitly";
} catch (Exception error) { report["androidBooted"] = false; report["bootFailure"] = SupportExportService.Redact(error.Message); }
finally {
    try { await session.StopAsync(CancellationToken.None); report["gracefulStop"] = session.LastGracefulShutdown is not null; }
    catch (Exception error) {
        report["stopFailure"] = SupportExportService.Redact(error.Message);
        if (session.ForceStopAvailable) await session.ForceStopAsync(CancellationToken.None);
    }
    Directory.CreateDirectory(Path.Combine(root, "docs/android-installation"));
    await File.WriteAllTextAsync(Path.Combine(root, options.ShowStandaloneWindow ? "docs/android-installation/acceptance-standalone.json" : options.DisplayTransport == "controller" ? "docs/android-installation/acceptance-controller.json" : "docs/android-installation/acceptance.json"),
        JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
}
Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
Environment.ExitCode = report.GetValueOrDefault("androidBooted") is true ? 0 : 1;

sealed class NoEmbeddedViewport : AndroidDesktop.Adapters.Viewport.IDeviceViewport
{
    public int Connections { get; private set; }
    public System.Windows.FrameworkElement Control => null!;
    public event Action<string, JsonElement>? Message { add { } remove { } }
    public Task ConnectAsync(int port, string token, CancellationToken cancellationToken) {
        Connections++; throw new InvalidOperationException("Native-window mode must not request embedded RTC.");
    }
    public void Send(string kind, object? data = null) { }
    public Task DisconnectAsync() => Task.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
