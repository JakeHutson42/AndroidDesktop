using AndroidDesktop.Models;
using AndroidDesktop.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.IO;
using System.Text.Json;

namespace AndroidDesktop.ViewModels;

public partial class PrototypeViewModel
{
    private RecordingService _recordings = null!;
    private PlaybackService _playback = null!;
    private RecordingManifest? _loadedRecording;
    private InputGeometry? _geometry;
    private TaskCompletionSource? _recordingStarted;
    private TaskCompletionSource? _recordingStopped;
    private TaskCompletionSource? _playbackStopped;
    private bool _recordingFinalizing;
    private bool _playbackFinalizing;
    [ObservableProperty] private bool recordingActive;
    [ObservableProperty] private bool playbackActive;
    [ObservableProperty] private bool playbackPaused;
    [ObservableProperty] private string automationStatus = "Experimental: hardware input/recording acceptance remains unverified.";
    [ObservableProperty] private string recordingPath = "";
    [ObservableProperty] private int loopCount = 1;
    [ObservableProperty] private int loopDelayMs = 1000;
    [ObservableProperty] private bool infiniteLoops;
    [ObservableProperty] private bool variationsEnabled;
    [ObservableProperty] private int variationSeed = 1;
    [ObservableProperty] private int coordinatePixels;
    [ObservableProperty] private int pathPixels;
    [ObservableProperty] private int timingMs;
    [ObservableProperty] private string detectorResult = "";
    public bool AutomationEditingEnabled => CanOpen();
    private void InitializeAutomation(string? root)
    {
        root ??= Path.Combine(SettingsStore.DataRoot, "automation");
        _recordings = new(Path.Combine(root, "recordings")); _playback = new(Path.Combine(root, "runs"));
        _recordings.Failed += error => OnUi(() => { AutomationStatus = error; _viewport.Send("recordingStop", new { error }); });
        _playback.Failed += error => OnUi(() => { AutomationStatus = error; _viewport.Send("playbackStop", new { error }); });
    }
    partial void OnRecordingActiveChanged(bool value) => NotifyCommands();
    partial void OnPlaybackActiveChanged(bool value) => NotifyCommands();
    partial void OnPlaybackPausedChanged(bool value) => NotifyAutomationCommands();
    private void NotifyAutomationCommands()
    {
        StartRecordingCommand.NotifyCanExecuteChanged(); StopRecordingCommand.NotifyCanExecuteChanged();
        OpenRecordingCommand.NotifyCanExecuteChanged(); RecoverRecordingCommand.NotifyCanExecuteChanged();
        PlayRecordingCommand.NotifyCanExecuteChanged(); PausePlaybackCommand.NotifyCanExecuteChanged(); ResumePlaybackCommand.NotifyCanExecuteChanged();
        StopPlaybackCommand.NotifyCanExecuteChanged(); SaveDetectorResultCommand.NotifyCanExecuteChanged(); OnPropertyChanged(nameof(AutomationEditingEnabled));
    }
    private bool CanRecord() => CanSend() && Selection is not null && _geometry is not null && _session.State == SessionState.Running && _session.DeviceId is not null;
    private bool CanStopRecording() => RecordingActive && !Busy;
    private bool CanPlayRecording() => CanRecord() && _loadedRecording?.Complete == true;
    private bool CanPausePlayback() => PlaybackActive && !PlaybackPaused && !Busy;
    private bool CanResumePlayback() => PlaybackActive && PlaybackPaused && !Busy;
    private bool CanStopPlayback() => PlaybackActive && !Busy;
    private bool CanSaveDetectorResult() => CanOpen() && _playback?.ResultPath is not null;
    private RecordingContext CurrentContext() => new(Selection!.Apk.PackageId, Selection.Apk.VersionCode, Selection.Apk.Sha256,
        _session.DeviceId!, _session.Options.AvdName, _session.Options.ViewportMode, _geometry!, RuntimeIdentity: _session.RuntimeIdentity);
    [RelayCommand(CanExecute = nameof(CanRecord), IncludeCancelCommand = true)]
    private Task StartRecordingAsync(CancellationToken token) => RunAsync(async ct => {
        _viewport.Send("cancel"); var context = CurrentContext();
        await Task.Run(() => _recordings.StartAsync(context), ct);
        RecordingActive = true; _recordingStarted = new(TaskCreationOptions.RunContinuationsAsynchronously); _recordingStopped = new(TaskCreationOptions.RunContinuationsAsynchronously);
        RecordingPath = _recordings.ManifestPath!; _loadedRecording = null;
        _viewport.Send("recordingStart", new { id = _recordings.Manifest!.Id });
        try { await _recordingStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), ct); AutomationStatus = "Recording viewport input. Disk writes use a bounded background queue."; }
        catch { await AbortAutomationAsync("Recording start was interrupted or not acknowledged"); throw; }
    }, token);
    [RelayCommand(CanExecute = nameof(CanStopRecording))]
    private Task StopRecordingAsync() => RunAsync(async _ => {
        _viewport.Send("recordingStop");
        try { await _recordingStopped!.Task.WaitAsync(TimeSpan.FromSeconds(5)); }
        catch { await AbortAutomationAsync("Recording finalization was not acknowledged"); throw; }
    }, CancellationToken.None);
    [RelayCommand(CanExecute = nameof(CanOpen), IncludeCancelCommand = true)]
    private async Task OpenRecordingAsync(CancellationToken token)
    {
        var picker = new OpenFileDialog { Filter = "Recording manifest (manifest.json)|manifest.json", Multiselect = false };
        if (picker.ShowDialog() != true) return;
        await RunAsync(async ct => { var manifest = await Task.Run(() => PlaybackService.VerifyAsync(picker.FileName, ct), ct);
            _loadedRecording = manifest; RecordingPath = picker.FileName; AutomationStatus = $"Loaded {manifest.EventCount} events for {manifest.Context.PackageId}. Prepare the matching app state before replay."; NotifyCommands(); }, token);
    }
    [RelayCommand(CanExecute = nameof(CanOpen), IncludeCancelCommand = true)]
    private async Task RecoverRecordingAsync(CancellationToken token)
    {
        var picker = new OpenFileDialog { Filter = "Interrupted recording (manifest.json)|manifest.json", Multiselect = false };
        if (picker.ShowDialog() != true) return;
        await RunAsync(async ct => { var recovered = await Task.Run(() => _recordings.RecoverAsync(picker.FileName, ct), ct);
            if (!recovered.Complete) throw new IOException(recovered.Failure);
            _loadedRecording = recovered; RecordingPath = _recordings.ManifestPath!; AutomationStatus = "Recovered valid prefix into a new recording, with synthetic releases. Original retained; inspect before replay."; NotifyCommands(); }, token);
    }
    [RelayCommand(CanExecute = nameof(CanPlayRecording), IncludeCancelCommand = true)]
    private Task PlayRecordingAsync(CancellationToken token) => RunAsync(async ct => {
        if (VariationSeed < 0) throw new InvalidDataException("Seed must be a nonnegative integer.");
        var options = new PlaybackOptions(LoopCount, InfiniteLoops, LoopDelayMs, VariationsEnabled, (uint)VariationSeed, CoordinatePixels, PathPixels, TimingMs);
        var context = CurrentContext(); var path = RecordingPath;
        _viewport.Send("cancel");
        var manifest = await Task.Run(() => _playback.BeginAsync(path, context, options, ct), ct);
        PlaybackActive = true; PlaybackPaused = false; _playbackStopped = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _viewport.Send("playbackStart", new { id = _playback.RunId, durationMs = manifest.DurationMs, options });
        AutomationStatus = "Playback starting. Loops do not reset game state. No detector outcome is assumed.";
    }, token);
    [RelayCommand(CanExecute = nameof(CanPausePlayback))] private void PausePlayback() { _viewport.Send("playbackPause"); PlaybackPaused = true; AutomationStatus = "Pause requested; waiting for released touches and keys."; }
    [RelayCommand(CanExecute = nameof(CanResumePlayback))] private void ResumePlayback() { _viewport.Send("playbackResume"); PlaybackPaused = false; AutomationStatus = "Playback resuming."; }
    [RelayCommand(CanExecute = nameof(CanStopPlayback))]
    private Task StopPlaybackAsync() => RunAsync(async _ => {
        _viewport.Send("playbackStop");
        try { await _playbackStopped!.Task.WaitAsync(TimeSpan.FromSeconds(5)); }
        catch { await AbortAutomationAsync("Playback stop was not acknowledged"); throw; }
    }, CancellationToken.None);
    [RelayCommand(CanExecute = nameof(CanSaveDetectorResult), IncludeCancelCommand = true)]
    private Task SaveDetectorResultAsync(CancellationToken token) => RunAsync(async ct => {
        if (DetectorResult.Length > 4096) throw new InvalidDataException("Detector notes are limited to 4096 characters.");
        var result = DetectorResult; var path = Path.Combine(Path.GetDirectoryName(_playback.ResultPath!)!, "detector.json");
        await Task.Run(() => InputArchive.WriteManifestAsync(path, new { schemaVersion = 1, result, utc = DateTimeOffset.UtcNow, source = "User supplied detector observation; not inferred by Android Desktop" }), ct);
        AutomationStatus = "Detector observation saved beside the dispatch report.";
    }, token);

    private void ReceiveAutomation(string kind, JsonElement data)
    {
        if (kind == "geometry") {
            _geometry = data.Deserialize<InputGeometry>(RecordingJson.Options); NotifyAutomationCommands();
        }
        if (kind == "recordingStarted" && data.GetProperty("id").GetString() == _recordings.Manifest?.Id) {
            if (data.GetProperty("geometry").Deserialize<InputGeometry>(RecordingJson.Options) != _recordings.Manifest!.Context.Geometry)
                _recordingStarted?.TrySetException(new InvalidDataException("Display geometry changed while starting recording. Retry after rotation settles."));
            else _recordingStarted?.TrySetResult();
        }
        if (kind == "recordingBatch" && RecordingActive) _recordings.Append(data.GetProperty("id").GetString()!, data.GetProperty("events"));
        if (kind == "recordingFault" && data.GetProperty("id").GetString() == _recordings.Manifest?.Id) {
            var error = data.GetProperty("error").GetString()!; _recordingStarted?.TrySetException(new IOException(error));
            _ = AutomationCallbackAsync(() => AbortAutomationAsync(error));
        }
        if (kind == "recordingStopped" && data.GetProperty("id").GetString() == _recordings.Manifest?.Id && _recordings.Active && !_recordingFinalizing) {
            _recordingFinalizing = true;
            _ = AutomationCallbackAsync(async () => {
                try {
                    var result = await Task.Run(() => _recordings.FinishAsync(data.GetProperty("durationMs").GetDouble(), data.GetProperty("count").GetInt64(), data.GetProperty("error").GetString()));
                    _loadedRecording = result; AutomationStatus = result.Complete ? $"Saved {result.EventCount} events: {RecordingPath}" : "Incomplete recording retained: " + result.Failure;
                    _recordingStopped?.TrySetResult();
                } catch (Exception e) { _recordingStopped?.TrySetException(e); throw; }
                finally { RecordingActive = false; _recordingFinalizing = false; }
            });
        }
        if (kind == "playbackRequest" && PlaybackActive && data.GetProperty("id").GetString() == _playback.RunId) {
            _ = AutomationCallbackAsync(async () => {
                try { var batch = await Task.Run(() => _playback.NextBatchAsync(_playback.RunId!, data.GetProperty("start").GetInt64(), data.GetProperty("loop").GetInt32(), data.GetProperty("requestId").GetString()!)); _viewport.Send("playbackBatch", batch); }
                catch (Exception e) { _viewport.Send("playbackBatch", new { runId = _playback.RunId, requestId = data.GetProperty("requestId").GetString(), error = e.Message }); }
            });
        }
        if (kind == "playbackDispatch" && PlaybackActive) _playback.AppendReceipts(data.GetProperty("id").GetString()!, data.GetProperty("events"));
        if (kind == "playbackState" && data.GetProperty("id").GetString() == _playback.RunId) {
            var stage = data.GetProperty("state").GetString();
            if (stage is "paused" or "pausing") { PlaybackPaused = true; AutomationStatus = stage == "paused" ? "Paused with all input released." : "Waiting for a released-input pause boundary."; }
            if (stage == "playing") { PlaybackPaused = false; AutomationStatus = "Playing; game state is not reset between loops."; }
            if (stage == "finished" && _playback.Active && !_playbackFinalizing) {
                _playbackFinalizing = true;
                _ = AutomationCallbackAsync(async () => {
                try {
                    await Task.Run(() => _playback.FinishAsync(data.GetProperty("status").GetString()!, data.GetProperty("receipts").GetInt64(), data.GetProperty("completedLoops").GetInt32(), data.GetProperty("error").GetString()));
                    AutomationStatus = "Playback " + data.GetProperty("status").GetString() + ". Dispatch evidence: " + _playback.ResultPath + (data.GetProperty("error").ValueKind == JsonValueKind.String ? " · " + data.GetProperty("error").GetString() : "");
                    _playbackStopped?.TrySetResult();
                } catch (Exception e) { _playbackStopped?.TrySetException(e); throw; }
                finally { PlaybackActive = false; PlaybackPaused = false; _playbackFinalizing = false; }
            });
            }
        }
    }
    private async Task AutomationCallbackAsync(Func<Task> action)
    {
        try { await action(); }
        catch (Exception e) { AutomationStatus = "Automation failed: " + e.Message; AddMessage(AutomationStatus); }
    }
    private async Task AbortAutomationAsync(string reason)
    {
        if (!RecordingActive && !PlaybackActive && !_recordings.Active && !_playback.Active) return;
        _viewport.Send("recordingStop", new { error = reason }); _viewport.Send("playbackStop", new { error = reason });
        try {
            var tasks = new List<Task>();
            if (RecordingActive && _recordingStopped is not null) tasks.Add(_recordingStopped.Task);
            if (PlaybackActive && _playbackStopped is not null) tasks.Add(_playbackStopped.Task);
            await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(5));
        } catch {
            if (_recordings.Active && !_recordingFinalizing) await Task.Run(() => _recordings.FinishAsync(0, -1, reason + "; release/final receipt unverified"));
            if (_playback.Active && !_playbackFinalizing) await Task.Run(() => _playback.FinishAsync("interrupted", -1, 0, reason + "; release/final receipt unverified"));
            if (!_recordingFinalizing) RecordingActive = false;
            if (!_playbackFinalizing) PlaybackActive = false;
        }
    }
}
