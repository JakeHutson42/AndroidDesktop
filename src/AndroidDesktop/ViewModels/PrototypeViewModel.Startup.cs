using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Threading;
using AndroidDesktop.Services;

namespace AndroidDesktop.ViewModels;

public partial class PrototypeViewModel
{
    [ObservableProperty] private bool isStarting;
    [ObservableProperty] private double startupProgress;
    [ObservableProperty] private string startupTiming = "Estimating startup time…";
    [ObservableProperty] private string startupTitle = "Starting Android";
    [ObservableProperty] private string startupMessage = "Checking your device and hardware acceleration";
    public StartupStep[] StartupSteps { get; } = [new("Check device and acceleration"), new("Start the emulator"), new("Boot Android"), new("Connect the display")];
    public bool PersistStartupTimings { get; set; } = true;
    private readonly Stopwatch _startupClock = new();
    private DispatcherTimer? _startupTimer;
    private int _startupStage;
    private double _stageStart;
    private readonly double[] _observedStages = new double[4];
    // Initial estimate comes from this device's measured cold-boot trials. Successful
    // runs replace it with a rolling estimate, keyed to the device and transport.
    private double[] _stageEstimates = [.5, 2, 18, .5];
    private string TimingPath => Path.Combine(SettingsStore.DataRoot, "startup-timing-" +
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(
            _session.Options.ProfileId + ":" + _session.Options.DisplayTransport + ":" + _session.Options.MemoryMb + ":" + _session.Options.CpuCores)))[..16] + ".json");
    private void BeginStartup()
    {
        if (IsStarting) return;
        try { var saved = JsonSerializer.Deserialize<double[]>(File.ReadAllText(TimingPath));
            if (saved is { Length: 4 } && saved.All(x => double.IsFinite(x) && x > 0 && x < 180)) _stageEstimates = saved; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { }
        Array.Clear(_observedStages); _startupStage = 0; _stageStart = 0;
        StartupTitle = "Starting Android"; StartupProgress = 0; IsStarting = true; _startupClock.Restart();
        UpdateSteps();
        _startupTimer ??= new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Background,
            (_, _) => UpdateStartupProgress(), System.Windows.Application.Current.Dispatcher);
        _startupTimer.Start(); UpdateStartupProgress();
    }
    private void SetStartupStage(int stage)
    {
        if (!IsStarting || stage <= _startupStage || stage > 3) return;
        _observedStages[_startupStage] = Math.Max(.1, _startupClock.Elapsed.TotalSeconds - _stageStart);
        _stageStart = _startupClock.Elapsed.TotalSeconds; _startupStage = stage;
        UpdateSteps(); UpdateStartupProgress();
    }
    private void UpdateSteps() {
        StartupMessage = _startupStage switch { 0 => "Checking your device and hardware acceleration", 1 => "Starting the emulator", 2 => "Waiting for Android to finish starting", _ => "Connecting Android to your viewport" };
        for (var i = 0; i < StartupSteps.Length; i++) StartupSteps[i].State = i < _startupStage ? "Complete" : i == _startupStage ? "Current" : "Waiting";
    }
    private void UpdateStartupProgress()
    {
        var elapsed = _startupClock.Elapsed.TotalSeconds - _stageStart;
        // Advance within the current stage, but never claim a stage has finished
        // until its real event arrives. A slow boot pauses rather than inventing progress.
        var before = _stageEstimates.Take(_startupStage).Sum();
        var fraction = Math.Min(.92, elapsed / _stageEstimates[_startupStage]);
        StartupProgress = Math.Max(StartupProgress, 100 * (before + fraction * _stageEstimates[_startupStage]) / _stageEstimates.Sum());
        var remaining = Math.Max(0, _stageEstimates.Skip(_startupStage).Sum() - elapsed);
        StartupTiming = elapsed > _stageEstimates[_startupStage] * 1.25
            ? $"Taking longer than usual · {_startupClock.Elapsed.TotalSeconds:0}s elapsed"
            : $"About {Math.Max(1, (int)Math.Ceiling(remaining))}s remaining · {_startupClock.Elapsed.TotalSeconds:0}s elapsed";
    }
    private void EndStartup(bool success)
    {
        if (!IsStarting) return;
        _startupTimer?.Stop(); _startupClock.Stop();
        if (!success) { IsStarting = false; return; }
        _observedStages[_startupStage] = Math.Max(.1, _startupClock.Elapsed.TotalSeconds - _stageStart);
        foreach (var step in StartupSteps) step.State = "Complete";
        StartupProgress = 100; StartupTitle = "Android is ready"; StartupTiming = $"Ready in {_startupClock.Elapsed.TotalSeconds:0}s";
        for (var i = 0; i < 4; i++) if (_observedStages[i] > 0) _stageEstimates[i] = .6 * _stageEstimates[i] + .4 * _observedStages[i];
        if (PersistStartupTimings) {
            try { Directory.CreateDirectory(SettingsStore.DataRoot); File.WriteAllText(TimingPath, JsonSerializer.Serialize(_stageEstimates)); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        }
        _ = RevealViewportAsync();
    }
    private async Task RevealViewportAsync() { await Task.Delay(350); if (!_startupClock.IsRunning) IsStarting = false; }
}

public partial class StartupStep(string title) : ObservableObject
{
    public string Title { get; } = title;
    [ObservableProperty] private string state = "Waiting";
}
