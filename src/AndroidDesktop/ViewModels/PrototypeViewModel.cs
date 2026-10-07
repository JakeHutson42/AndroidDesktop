using AndroidDesktop.Adapters.Viewport;
using AndroidDesktop.Models;
using AndroidDesktop.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Text.Json;
using System.Windows;

namespace AndroidDesktop.ViewModels;

public partial class PrototypeViewModel : ObservableObject
{
    private readonly EmulatorSessionService _session;
    private readonly IDeviceViewport _viewport;
    private readonly EvidenceService _evidence;
    private readonly Queue<string> _messages = new();
    private CancellationTokenSource? _active;
    private bool _closing;
    private int _degrees;
    [ObservableProperty] private string state = "Stopped";
    [ObservableProperty] private string status = "Prepare Android from Setup, then use Open APK.";
    [ObservableProperty] private string operationError = "";
    [ObservableProperty] private string log = "";
    [ObservableProperty] private string media = "No media samples received.";
    [ObservableProperty] private string audioStatus = "Audio off";
    [ObservableProperty] private string displayDetails = "Display not connected";
    [ObservableProperty] private string selectedApp = "No app selected";
    [ObservableProperty] private string selectedDetails = "No app selected. Android data stays in your persistent device.";
    [ObservableProperty] private bool busy;
    [ObservableProperty] private bool inputReady;
    public ApkSelection? Selection { get; private set; }
    public System.Collections.ObjectModel.ObservableCollection<ApkSelection> Library { get; } = [];
    [ObservableProperty] private ApkSelection? librarySelection;
    public void RestoreLibrary(IEnumerable<ApkSelection> entries)
    {
        var previous = LibrarySelection;
        Library.Clear();
        foreach (var entry in entries) Library.Add(entry);
        LibrarySelection = previous is null ? null : ApkLibrary.Find(Library, previous.DeviceId, previous.Apk.PackageId);
    }
    partial void OnLibrarySelectionChanged(ApkSelection? value) => NotifyCommands();
    public Func<ApkSelection, Task<bool>> CommitSelection { get; set; } = _ => Task.FromResult(false);
    public bool CanAcceptDrop => CanOpen();
    public PrototypeViewModel(EmulatorSessionService session, IDeviceViewport viewport, EvidenceService evidence, string? automationRoot = null)
    {
        _session = session; _viewport = viewport; _evidence = evidence;
        State = DisplayState(session.State);
        session.StateChanged += s => OnUi(() => { State = DisplayState(s); if (s == SessionState.Faulted) { InputReady = false; ReleaseAllInput(); } NotifyCommands(); });
        session.Message += text => OnUi(() => AddMessage(text));
        evidence.Failed += text => OnUi(() => AddMessage(text));
        viewport.Message += Receive;
        InitializeAutomation(automationRoot);
    }
    public void RestoreSelection(ApkSelection? selection)
    {
        Selection = selection;
        SelectedApp = selection is null ? "No app selected" : string.IsNullOrWhiteSpace(selection.Apk.AppName) ? selection.Apk.PackageId : selection.Apk.AppName;
        SelectedDetails = selection is null ? "No app selected. Android data stays in your persistent device." :
            $"{selection.Apk.PackageId} · {selection.Apk.VersionName} ({selection.Apk.VersionCode})\n{selection.Apk.Path}";
        NotifyCommands();
    }
    public void AddMessage(string text)
    {
        Status = text; _messages.Enqueue($"{DateTime.Now:HH:mm:ss} {text}");
        while (_messages.Count > 60) _messages.Dequeue(); Log = string.Join("\n", _messages);
    }
    private static void OnUi(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess()) action(); else dispatcher.BeginInvoke(action);
    }
    partial void OnBusyChanged(bool value) => NotifyCommands();
    partial void OnInputReadyChanged(bool value) { NotifyCommands(); if (!value) { AudioStatus = "Audio off"; DisplayDetails = "Display not connected"; } if (!value && (RecordingActive || PlaybackActive)) _ = AutomationCallbackAsync(() => AbortAutomationAsync("Input connection unavailable")); }
    private void NotifyCommands()
    {
        StartCommand.NotifyCanExecuteChanged(); ConnectCommand.NotifyCanExecuteChanged();
        OpenApkCommand.NotifyCanExecuteChanged(); ImportFilesCommand.NotifyCanExecuteChanged();
        LaunchLibraryCommand.NotifyCanExecuteChanged(); LocateLibraryCommand.NotifyCanExecuteChanged();
        LaunchCommand.NotifyCanExecuteChanged(); RelaunchCommand.NotifyCanExecuteChanged(); RotateCommand.NotifyCanExecuteChanged();
        DeviceKeyCommand.NotifyCanExecuteChanged(); EnableAudioCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged(); ForceStopCommand.NotifyCanExecuteChanged();
        ExportSupportCommand.NotifyCanExecuteChanged();
        CaptureGestureCommand.NotifyCanExecuteChanged(); ReplayGestureCommand.NotifyCanExecuteChanged();
        NotifyAutomationCommands();
        OnPropertyChanged(nameof(CanAcceptDrop));
    }
    private bool CanOpen() => !Busy && !_closing && !RecordingActive && !PlaybackActive;
    private bool CanLiveInput() => !Busy && !_closing && !PlaybackActive && InputReady && _session.HasOwnedEmulator && _session.State is SessionState.Ready or SessionState.Running;
    private bool CanStart() => CanOpen() && !_session.HasOwnedProcessHandles && _session.State != SessionState.NotConfigured;
    private bool CanConnect() => CanOpen() && _session.HasOwnedEmulator && !_session.Options.ShowStandaloneWindow;
    private bool CanLaunch() => CanOpen() && Selection is not null;
    private bool CanLaunchLibrary() => CanOpen() && LibrarySelection is not null;
    [RelayCommand(CanExecute = nameof(CanLaunchLibrary), IncludeCancelCommand = true)]
    private Task LaunchLibraryAsync(CancellationToken token)
    {
        if (!CanLaunchLibrary()) return Task.CompletedTask;
        var entry = LibrarySelection!;
        return RunAsync(ct => OpenAsync(entry.Apk.Path, false, ct, entry.Apk.PackageId), token);
    }
    [RelayCommand(CanExecute = nameof(CanLaunchLibrary), IncludeCancelCommand = true)]
    private async Task LocateLibraryAsync(CancellationToken token)
    {
        if (!CanLaunchLibrary()) return;
        var package = LibrarySelection!.Apk.PackageId;
        var picker = new OpenFileDialog { Filter = "Standalone Android APK (*.apk)|*.apk", Multiselect = false };
        if (picker.ShowDialog() == true) await RunAsync(ct => OpenAsync(picker.FileName, false, ct, package), token);
    }
    private bool CanSend() => CanOpen() && InputReady && _session.HasOwnedEmulator && _session.State is SessionState.Ready or SessionState.Running;
    private bool CanEnableAudio() => CanLiveInput();
    private bool CanStop() => !Busy && !_closing && _session.HasOwnedProcessHandles;
    private bool CanForceStop() => !Busy && !_closing && _session.ForceStopAvailable;
    private static string DisplayState(SessionState state) => state == SessionState.NotConfigured ? "Not configured" : state.ToString();
    private async Task RunAsync(Func<CancellationToken, Task> action, CancellationToken token)
    {
        if (Busy) { AddMessage("Another operation is active. Wait or cancel it before continuing."); return; }
        OperationError = "";
        Busy = true; _active = CancellationTokenSource.CreateLinkedTokenSource(token);
        try { await action(_active.Token); }
        catch (OperationCanceledException) { AddMessage("Operation cancelled. Previous selection and Android data were retained; retry or stop the device."); }
        catch (Exception error) {
            OperationError = string.IsNullOrWhiteSpace(error.Message) ? "The operation failed. Open Diagnostics for details." : error.Message;
            AddMessage(OperationError);
            System.Diagnostics.Debug.WriteLine("Android Desktop operation failed:\n" + error);
            _evidence.TryWrite("operationError", new { type = error.GetType().FullName, message = error.Message, detail = error.ToString() });
        }
        finally { _active.Dispose(); _active = null; Busy = false; }
    }
    [RelayCommand(CanExecute = nameof(CanStart), IncludeCancelCommand = true)]
    private Task StartAsync(CancellationToken token) => RunAsync(async ct => {
        await _session.StartAsync(ct);
        if (_session.Options.ShowStandaloneWindow) AddMessage("Android is running in its own emulator window. Use that window for display and input; embedded recording/replay is unavailable in this mode.");
        else await ConnectDisplayAsync(ct);
    }, token);
    private async Task ConnectDisplayAsync(CancellationToken token)
    {
        InputReady = false; _viewport.Send("cancel"); await _viewport.DisconnectAsync();
        await _session.ConnectGatewayAsync(token); var connection = _session.Connection;
        AddMessage("Connecting Android display…"); await _viewport.ConnectAsync(connection.Port, connection.Token, token);
    }
    [RelayCommand(CanExecute = nameof(CanConnect), IncludeCancelCommand = true)]
    private Task ConnectAsync(CancellationToken token) => RunAsync(ConnectDisplayAsync, token);
    [RelayCommand(CanExecute = nameof(CanOpen), IncludeCancelCommand = true)]
    private async Task OpenApkAsync(CancellationToken token)
    {
        var picker = new OpenFileDialog { Filter = "Standalone Android APK (*.apk)|*.apk", Multiselect = false };
        if (picker.ShowDialog() == true) await ImportFilesAsync([picker.FileName], token);
    }
    [RelayCommand(CanExecute = nameof(CanOpen), IncludeCancelCommand = true)]
    public Task ImportFilesAsync(string[] files, CancellationToken token) => RunAsync(async ct => {
        var path = ApkImportService.ValidateFiles(files); await OpenAsync(path, false, ct);
    }, token);
    public Func<CancellationToken, Task>? EnsureRuntimeReady { get; set; }
    private async Task OpenAsync(string path, bool relaunch, CancellationToken token, string? expectedPackage = null)
    {
        path = ApkImportService.ValidateFiles([path]);
        if (!_session.HasOwnedProcessHandles && EnsureRuntimeReady is not null) await EnsureRuntimeReady(token);
        if (_session.State == SessionState.NotConfigured) throw new InvalidOperationException("Select Prepare Android on the Setup page before opening an APK.");
        _viewport.Send("cancel");
        if (!_session.HasOwnedEmulator) {
            if (_session.HasOwnedProcessHandles) throw new InvalidOperationException("Stop the previous session before opening an APK.");
            await _session.StartAsync(token);
        }
        if (!InputReady && !_session.Options.ShowStandaloneWindow) await ConnectDisplayAsync(token);
        var selected = await _session.ImportAsync(path, Selection, relaunch, CommitSelection, token,
            (device, package) => ApkLibrary.Find(Library, device, package) ??
                (Selection?.DeviceId == device && Selection.Apk.PackageId == package ? Selection : null), expectedPackage);
        RestoreSelection(selected);
    }
    [RelayCommand(CanExecute = nameof(CanLaunch), IncludeCancelCommand = true)]
    private Task LaunchAsync(CancellationToken token) => RunAsync(ct => OpenAsync(Selection!.Apk.Path, false, ct), token);
    [RelayCommand(CanExecute = nameof(CanLaunch), IncludeCancelCommand = true)]
    private Task RelaunchAsync(CancellationToken token) => RunAsync(ct => OpenAsync(Selection!.Apk.Path, true, ct), token);
    [RelayCommand(CanExecute = nameof(CanSend), IncludeCancelCommand = true)]
    private Task RotateAsync(CancellationToken token) => RunAsync(async ct => {
        _viewport.Send("cancel"); var next = (_degrees + 90) % 360;
        await _session.RotateAsync(next, ct); _degrees = next; _viewport.Send("rotation", _degrees);
    }, token);
    [RelayCommand(CanExecute = nameof(CanLiveInput))] private void DeviceKey(string key) => _viewport.Send("deviceKey", key);
    [RelayCommand(CanExecute = nameof(CanEnableAudio))] private void EnableAudio() => _viewport.Send("audio");
    [RelayCommand(CanExecute = nameof(CanSend))] private void CaptureGesture() => _viewport.Send("arm");
    [RelayCommand(CanExecute = nameof(CanSend))] private void ReplayGesture() => _viewport.Send("replay");
    [RelayCommand] private void ReleaseInput() => _viewport.Send("cancel");
    [RelayCommand] private void CancelOperation() { _active?.Cancel(); _viewport.Send("cancel"); }
    [RelayCommand(CanExecute = nameof(CanOpen), IncludeCancelCommand = true)]
    private async Task ExportSupportAsync(CancellationToken token)
    {
        var picker = new SaveFileDialog { Filter = "Support archive (*.zip)|*.zip", FileName = "AndroidDesktop-support.zip" };
        if (picker.ShowDialog() != true) return;
        // Capture UI state before doing archive I/O on a worker; no gameplay-path disk writes.
        var summary = new { product = "Android Desktop", version = "0.3.0", utc = DateTime.UtcNow, State,
            transport = "WebRTC provisional; hardware acceptance unverified", dotnet = Environment.Version.ToString(),
            browser = (_viewport as WebRtcViewport)?.BrowserVersion,
            package = Selection?.Apk.PackageId, packageVersion = Selection?.Apk.VersionCode, device = Selection?.DeviceId };
        var messages = Log; var directory = _evidence.DirectoryPath;
        await RunAsync(async ct => {
            await Task.Run(() => SupportExportService.ExportAsync(picker.FileName, summary, messages, directory, ct), ct);
            AddMessage("Support export finished. Review the archive before sharing.");
        }, token);
    }
    [RelayCommand(CanExecute = nameof(CanStop))]
    private Task StopAsync() => RunAsync(async ct => {
        await AbortAutomationAsync("Device stopping");
        AddMessage("Stopping Android gracefully…"); _viewport.Send("cancel"); InputReady = false;
        await _viewport.DisconnectAsync(); await _session.StopAsync(ct);
    }, CancellationToken.None);
    [RelayCommand(CanExecute = nameof(CanForceStop))]
    private Task ForceStopAsync() => RunAsync(async ct => {
        await AbortAutomationAsync("Device forcibly stopping");
        _viewport.Send("cancel"); InputReady = false; await _viewport.DisconnectAsync(); await _session.ForceStopAsync(ct);
    }, CancellationToken.None);
    public async Task<bool> CloseAsync()
    {
        _closing = true; NotifyCommands(); ReleaseAllInput();
        try {
            await AbortAutomationAsync("Application closing");
            if (Busy) {
                CancelOperation(); AddMessage("Cancelling the active operation before graceful shutdown…");
                for (var attempt = 0; Busy && attempt < 100; attempt++) await Task.Delay(100);
                if (Busy) { AddMessage("Operation is still finishing. Close again after it settles."); return false; }
            }
            if (!_session.HasOwnedProcessHandles) { await _evidence.StopAsync(); return true; }
            await StopAsync(); return !_session.HasOwnedProcessHandles;
        } finally { _closing = false; NotifyCommands(); }
    }
    public void ReleaseAllInput() => _viewport.Send("cancel");
    public event Action? ExitFullscreen;
    private void Receive(string kind, JsonElement data)
    {
        ReceiveAutomation(kind, data);
        if (kind is not ("input" or "inputBatch" or "gesture" or "recordingBatch" or "playbackDispatch") || Environment.GetEnvironmentVariable("ANDROID_DESKTOP_VERBOSE_EVIDENCE") == "1")
            _evidence.TryWrite("viewport." + kind, data);
        switch (kind) {
            case "status": AddMessage(data.GetString() ?? ""); break;
            case "inputReady": InputReady = data.GetBoolean(); break;
            case "fault": InputReady = false; _session.ReportViewportFault(data.GetString() ?? "Viewport fault"); break;
            case "disconnected": InputReady = false; AddMessage("Display disconnected. Use Connect display to recover the running device."); break;
            case "media":
                Media = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                if (data.TryGetProperty("controllerAudio", out var audio) && audio.TryGetProperty("enabled", out var enabled))
                    AudioStatus = enabled.GetBoolean() && audio.TryGetProperty("state", out var audioState) && audioState.GetString() == "running" ? "Audio on" : "Audio off";
                if (data.TryGetProperty("displayTransport", out var transport) && transport.GetString() == "webrtc" && data.TryGetProperty("muted", out var muted))
                    AudioStatus = muted.GetBoolean() ? "Audio off" : "Audio on";
                if (!InputReady) AudioStatus = "Audio off";
                break;
            case "geometry":
                if (data.TryGetProperty("width", out var width) && data.TryGetProperty("height", out var height))
                    DisplayDetails = $"{width.GetInt32()} × {height.GetInt32()}";
                AddMessage("Android display geometry: " + data); break;
            case "evidenceOverflow": _evidence.Invalidate(data.GetString() ?? "Input evidence is incomplete"); break;
            case "releaseUnverified": AddMessage(data.GetString() ?? "Input release unverified"); break;
            case "escape": ExitFullscreen?.Invoke(); break;
        }
    }
}

