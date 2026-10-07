using AndroidDesktop.Models;
using AndroidDesktop.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;

namespace AndroidDesktop.ViewModels;

public partial class DesktopViewModel : ObservableObject
{
    private readonly SettingsStore _store;
    private readonly SetupService _setup;
    private readonly EmulatorSessionService _session;
    private readonly ThemeService _themes = new();
    private readonly System.Windows.Threading.DispatcherTimer _saveTimer = new() { Interval = TimeSpan.FromMilliseconds(600) };
    private DesktopSettings _settings = new();
    private bool _initialized;
    private bool _automaticStartup;
    private readonly bool _persistent;
    private readonly SemaphoreSlim _saveOperations = new(1, 1);
    [ObservableProperty] private string theme = "Graphite";
    [ObservableProperty] private string persistenceStatus = "Loading settings…";
    [ObservableProperty] private string setupStatus = "Checking installed prerequisites…";
    [ObservableProperty] private string setupReport = "";
    [ObservableProperty] private bool setupBusy;
    [ObservableProperty] private bool acceptedSdkTerms;
    [ObservableProperty] private string sdkRoot = "";
    [ObservableProperty] private string commandLineToolsRoot = "";
    [ObservableProperty] private string javaExecutable = "";
    [ObservableProperty] private string pythonExecutable = "";
    [ObservableProperty] private string gatewayRoot = "";
    [ObservableProperty] private string systemImage = "system-images;android-34;google_apis_playstore;x86_64";
    [ObservableProperty] private string newProfileName = "";
    [ObservableProperty] private DeviceProfile? selectedProfile;
    [ObservableProperty] private string activeProfileName = "Default device";
    [ObservableProperty] private int? memoryMb;
    [ObservableProperty] private int? cpuCores;
    [ObservableProperty] private bool showStandaloneWindow;
    [ObservableProperty] private bool useControllerDisplay;
    [ObservableProperty] private string backupPath = "";
    [ObservableProperty] private string backupStatus = "Gracefully stop the active device before backup or restore. Backups can contain Android accounts and private data.";
    public System.Collections.ObjectModel.ObservableCollection<DeviceProfile> Profiles { get; } = [];
    partial void OnSelectedProfileChanged(DeviceProfile? value) => SwitchProfileCommand.NotifyCanExecuteChanged();
    public PrototypeViewModel Device { get; }
    public IReadOnlyList<string> Themes => _themes.Themes;
    public string DataRoot => SettingsStore.DataRoot;
    public bool SetupEditingEnabled => CanSetup();
    public bool CanAcceptSdkTerms => !SetupBusy;
    public event Action<WindowBounds>? RestoreBounds;
    public event Action<PrototypeOptions>? RuntimeConfigured;
    public event Action<bool>? ShowSetup;

    public async Task StartHomeAsync()
    {
        _automaticStartup = true;
        if (!_setup.Ready(ReadOptions(), SystemImage.Trim())) { ShowSetup?.Invoke(true); return; }
        ShowSetup?.Invoke(false);
        await Device.StartCommand.ExecuteAsync(null);
    }

    public DesktopViewModel(PrototypeViewModel device, SettingsStore store, SetupService setup, EmulatorSessionService session, bool persistent = true)
    {
        Device = device; _store = store; _setup = setup; _session = session; _persistent = persistent;
        Device.EnsureRuntimeReady = async token => {
            var options = _setup.Discover(ReadOptions());
            if (!_setup.Ready(options, SystemImage.Trim())) {
                var progress = new Progress<string>(message => { SetupStatus = message; Device.AddMessage(message); });
                options = await new RuntimePreparationService(new AndroidToolService()).PrepareAsync(options, SystemImage.Trim(), AcceptedSdkTerms, progress, token);
                ApplyFields(options);
                if (!await CommitRuntimeAsync(options, SystemImage.Trim())) throw new IOException("Could not save Android setup. Retry preparation.");
                await _setup.CreateDeviceAsync(options, SystemImage.Trim(), progress, token);
            }
            else if (options != _session.Options) {
                ApplyFields(options);
                if (!await CommitRuntimeAsync(options, SystemImage.Trim())) throw new IOException("Could not save the detected Android installation.");
            }
            SetupStatus = "Android is ready. Drop an APK on the Device page to play.";
        };
        Device.Busy = true;
        store.Failed += message => {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher is null || dispatcher.CheckAccess()) PersistenceStatus = message;
            else dispatcher.BeginInvoke(() => PersistenceStatus = message);
        };
        _saveTimer.Tick += async (_, _) => { _saveTimer.Stop(); await SaveAsync(); };
        device.PropertyChanged += (_, e) => { if (e.PropertyName is nameof(Device.Busy) or nameof(Device.State) or nameof(Device.RecordingActive) or nameof(Device.PlaybackActive)) NotifySetupCommands(); };
    }
    public async Task InitializeAsync(PrototypeOptions? imported = null)
    {
        if (_persistent) _settings = await _store.LoadAsync();
        _settings = DeviceProfiles.Normalize(_settings);
        // Older defaults used the experimental RTC service, which current emulator
        // token scopes reject. Normal startup uses the authenticated controller API.
        if (imported is null && _settings.Runtime.DisplayTransport == "webrtc")
            _settings = DeviceProfiles.Capture(_settings with { Runtime = _settings.Runtime with { DisplayTransport = "controller" } });
        if (imported is not null) _settings = DeviceProfiles.Switch(_settings, imported.ProfileId) with { Runtime = imported };
        Theme = _settings.Theme == "Cyberpunk" ? "Graphite" : Themes.Contains(_settings.Theme) ? _settings.Theme : "Graphite";
        _themes.Apply(Theme); RestoreBounds?.Invoke(_settings.Window);
        PrototypeOptions options;
        try { options = await Task.Run(() => _setup.Discover(_settings.Runtime)); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException) {
            // A stale/inaccessible SDK must not prevent users correcting it from Setup.
            options = _settings.Runtime;
            SetupStatus = "Dependency discovery failed; correct paths in Setup and recheck: " + error.Message;
        }
        ApplyFields(options); SystemImage = _settings.SystemImage;
        RuntimeConfigured?.Invoke(options); _session.Configure(options); _settings = _settings with { Runtime = options };
        Device.RestoreSelection(_settings.SelectedApk);
        Device.RestoreLibrary(_settings.ApkLibrary);
        RefreshProfiles();
        _initialized = true;
        Device.Busy = false;
        if (PersistenceStatus == "Loading settings…") PersistenceStatus = "Settings ready";
        NotifySetupCommands();
        if (_persistent && SetupService.RuntimeAvailable(options, SystemImage)) await PrepareAndroidCommand.ExecuteAsync(null);
        else SetupStatus = "First time? Read the Android SDK terms, then select Prepare Android. We'll handle the downloads and device automatically.";
    }
    private void ApplyFields(PrototypeOptions options)
    {
        SdkRoot = options.SdkRoot; CommandLineToolsRoot = options.CommandLineToolsRoot; JavaExecutable = options.JavaExecutable;
        PythonExecutable = options.PythonExecutable; GatewayRoot = options.GatewayRoot;
        MemoryMb = options.MemoryMb; CpuCores = options.CpuCores; ShowStandaloneWindow = options.ShowStandaloneWindow;
        UseControllerDisplay = options.DisplayTransport == "controller";
    }
    private PrototypeOptions ReadOptions() => _settings.Runtime with
    {
        SdkRoot = Environment.ExpandEnvironmentVariables(SdkRoot.Trim()), CommandLineToolsRoot = Environment.ExpandEnvironmentVariables(CommandLineToolsRoot.Trim()),
        JavaExecutable = Environment.ExpandEnvironmentVariables(JavaExecutable.Trim()), PythonExecutable = Environment.ExpandEnvironmentVariables(PythonExecutable.Trim()),
        GatewayRoot = Environment.ExpandEnvironmentVariables(GatewayRoot.Trim()), MemoryMb = MemoryMb, CpuCores = CpuCores,
        ShowStandaloneWindow = ShowStandaloneWindow, DisplayTransport = UseControllerDisplay ? "controller" : "webrtc"
    };
    partial void OnThemeChanged(string value)
    {
        _themes.Apply(value);
        _settings = _settings with { Theme = value }; ScheduleSave();
    }
    public void UpdateBounds(WindowBounds bounds) { _settings = _settings with { Window = bounds }; ScheduleSave(); }
    private void ScheduleSave() { if (!_initialized || !_persistent) return; _saveTimer.Stop(); _saveTimer.Start(); }
    public async Task FlushAsync() { _saveTimer.Stop(); if (_initialized && _persistent) await SaveAsync(); }
    public async Task<bool> CommitSelectionAsync(ApkSelection selected)
    {
        _saveTimer.Stop();
        await _saveOperations.WaitAsync();
        try {
            var candidate = DeviceProfiles.Capture(_settings with { SelectedApk = selected, ApkLibrary = ApkLibrary.Upsert(_settings.ApkLibrary, selected) });
            if (_persistent && !await _store.SaveAsync(candidate)) return false;
            // Merge into the latest in-memory settings so a resize/theme change during I/O is retained.
            _settings = _settings with { SelectedApk = selected, ApkLibrary = candidate.ApkLibrary, Profiles = candidate.Profiles };
            Device.RestoreLibrary(candidate.ApkLibrary);
            PersistenceStatus = "Selected app saved"; ScheduleSave(); return true;
        } finally { _saveOperations.Release(); }
    }
    private async Task<bool> SaveAsync()
    {
        if (!_persistent) return true;
        await _saveOperations.WaitAsync();
        try {
            var saved = await _store.SaveAsync(_settings);
            if (saved) PersistenceStatus = "Settings saved";
            return saved;
        } finally { _saveOperations.Release(); }
    }
    private bool CanSetup() => _initialized && !SetupBusy && !Device.Busy && !Device.RecordingActive && !Device.PlaybackActive && !_session.HasOwnedProcessHandles;
    private void NotifySetupCommands()
    {
        OnPropertyChanged(nameof(SetupEditingEnabled));
        CheckSetupCommand.NotifyCanExecuteChanged(); SaveSetupCommand.NotifyCanExecuteChanged(); CreateDeviceCommand.NotifyCanExecuteChanged();
        PrepareAndroidCommand.NotifyCanExecuteChanged();
        AddProfileCommand.NotifyCanExecuteChanged(); SwitchProfileCommand.NotifyCanExecuteChanged();
        BackupDeviceCommand.NotifyCanExecuteChanged(); RestoreDeviceCommand.NotifyCanExecuteChanged();
        VerifyBackupCommand.NotifyCanExecuteChanged(); RecoverRestoreCommand.NotifyCanExecuteChanged();
    }
    partial void OnSetupBusyChanged(bool value) { Device.Busy = value; OnPropertyChanged(nameof(CanAcceptSdkTerms)); NotifySetupCommands(); }
    private async Task SetupOperationAsync(Func<IProgress<string>, CancellationToken, Task> operation, CancellationToken token)
    {
        if (!CanSetup()) { SetupStatus = "Stop the current device and finish active operations before changing setup or profiles."; return; }
        SetupBusy = true;
        var progress = new Progress<string>(message => SetupStatus = message);
        try { await operation(progress, token); }
        catch (OperationCanceledException) { SetupStatus = "Setup cancelled. Saved paths and existing device data are retained. Recheck to resume."; }
        catch (Exception error) { SetupStatus = error.Message; }
        finally { SetupBusy = false; }
    }
    [RelayCommand(CanExecute = nameof(CanSetup), IncludeCancelCommand = true)]
    private async Task PrepareAndroidAsync(CancellationToken token)
    {
        var prepared = false;
        await SetupOperationAsync(async (_, ct) => {
            await Device.EnsureRuntimeReady!(ct);
            ct.ThrowIfCancellationRequested();
            SetupReport = "Android runtime installed. Your apps and device data are kept between launches.";
            prepared = true;
        }, token);
        if (prepared && _automaticStartup && !token.IsCancellationRequested) {
            ShowSetup?.Invoke(false);
            await Device.StartCommand.ExecuteAsync(null);
        }
    }
    [RelayCommand(CanExecute = nameof(CanSetup), IncludeCancelCommand = true)]
    private Task CheckSetupAsync(CancellationToken token) => SetupOperationAsync(async (progress, ct) =>
    {
        SetupReport = await _setup.CheckAsync(ReadOptions(), SystemImage.Trim(), progress, ct);
        SetupStatus = _setup.Ready(ReadOptions(), SystemImage.Trim())
            ? "Android is ready. Drop an APK on the Device page to play."
            : "Select Prepare Android to install missing components and create your device automatically.";
    }, token);
    [RelayCommand(CanExecute = nameof(CanSetup))]
    private Task SaveSetupAsync() => SetupOperationAsync(async (_, _) =>
    {
        var options = ReadOptions(); options.Validate(); SetupService.ValidateImage(SystemImage.Trim());
        if (await CommitRuntimeAsync(options, SystemImage.Trim())) SetupStatus = "Setup paths and resource settings saved. Recheck prerequisites before creating the device.";
    }, CancellationToken.None);
    [RelayCommand(CanExecute = nameof(CanSetup), IncludeCancelCommand = true)]
    private Task CreateDeviceAsync(CancellationToken token) => SetupOperationAsync(async (progress, ct) =>
    {
        var options = ReadOptions(); options.Validate(); SetupService.ValidateImage(SystemImage.Trim());
        if (!await CommitRuntimeAsync(options, SystemImage.Trim())) throw new IOException("Save setup paths successfully before creating a device.");
        await _setup.CreateDeviceAsync(options, SystemImage.Trim(), progress, ct);
    }, token);
    [RelayCommand] private void CancelSetup() { PrepareAndroidCommand.Cancel(); CheckSetupCommand.Cancel(); CreateDeviceCommand.Cancel(); }
    private async Task<bool> CommitRuntimeAsync(PrototypeOptions options, string image)
    {
        if (image != _settings.SystemImage && new DeviceStorageService().Exists(_settings.Runtime))
            throw new InvalidOperationException("An existing device keeps its system image. Add a separate profile to use a different image.");
        await _saveOperations.WaitAsync();
        try {
            var candidate = DeviceProfiles.Capture(_settings with { Runtime = options, SystemImage = image });
            if (_persistent && !await _store.SaveAsync(candidate)) return false;
            _session.Configure(options);
            RuntimeConfigured?.Invoke(options);
            _settings = _settings with { Runtime = options, SystemImage = image, Profiles = candidate.Profiles };
            RefreshProfiles(); ScheduleSave(); return true;
        } finally { _saveOperations.Release(); }
    }
    private void RefreshProfiles()
    {
        _settings = DeviceProfiles.Capture(_settings);
        var selectedId = SelectedProfile?.Id ?? _settings.ActiveProfileId;
        Profiles.Clear(); foreach (var profile in _settings.Profiles) Profiles.Add(profile);
        SelectedProfile = Profiles.FirstOrDefault(p => p.Id == selectedId);
        ActiveProfileName = Profiles.Single(p => p.Id == _settings.ActiveProfileId).Name;
    }
    private bool CanSwitchProfile() => CanSetup() && SelectedProfile is not null && SelectedProfile.Id != _settings.ActiveProfileId;
    [RelayCommand(CanExecute = nameof(CanSetup))]
    private Task AddProfileAsync() => SetupOperationAsync(async (_, _) =>
    {
        await _saveOperations.WaitAsync();
        try {
            if (_settings.Profiles.Length >= DeviceProfiles.MaximumProfiles) throw new InvalidOperationException("At most 32 device profiles are supported.");
            var profile = DeviceProfiles.Create(NewProfileName, _settings.Runtime, _settings.SystemImage);
            var candidate = DeviceProfiles.Capture(_settings with { Profiles = _settings.Profiles.Append(profile).ToArray() });
            if (_persistent && !await _store.SaveAsync(candidate)) throw new IOException("Profile could not be saved. Previous profiles retained.");
            _settings = _settings with { Profiles = candidate.Profiles }; RefreshProfiles(); SelectedProfile = Profiles.Single(p => p.Id == profile.Id);
            NewProfileName = ""; SetupStatus = "Profile saved. Switch to it, check prerequisites, then create its Android device."; ScheduleSave();
        } finally { _saveOperations.Release(); }
    }, CancellationToken.None);
    [RelayCommand(CanExecute = nameof(CanSwitchProfile))]
    private Task SwitchProfileAsync()
    {
        if (!CanSwitchProfile()) return Task.CompletedTask;
        var id = SelectedProfile!.Id;
        return SetupOperationAsync(async (_, _) => {
            await _saveOperations.WaitAsync();
            try {
                var candidate = DeviceProfiles.Switch(_settings, id);
                if (candidate.Runtime.ViewportMode != _session.Options.ViewportMode)
                    throw new InvalidOperationException("Profiles must use the current display mode. Restart with the required display configuration first.");
                if (_persistent && !await _store.SaveAsync(candidate)) throw new IOException("Profile switch could not be saved. The active profile was retained.");
                Device.ReleaseAllInput(); Device.InputReady = false; _session.Configure(candidate.Runtime);
                _settings = candidate with { Theme = _settings.Theme, Window = _settings.Window };
                ApplyFields(candidate.Runtime); SystemImage = candidate.SystemImage; SetupReport = "";
                Device.RestoreSelection(candidate.SelectedApk); Device.RestoreLibrary(candidate.ApkLibrary); RefreshProfiles();
                Device.AddMessage("Active profile: " + ActiveProfileName + ". Start or create its device from Setup.");
                SetupStatus = "Profile switched. Existing device files and app data were retained."; ScheduleSave();
            } finally { _saveOperations.Release(); }
        }, CancellationToken.None);
    }
    public async Task CancelAndWaitAsync()
    {
        CancelSetup();
        if (PrepareAndroidCommand.ExecutionTask is { } prepare) await prepare;
        if (CheckSetupCommand.ExecutionTask is { } check) await check;
        if (CreateDeviceCommand.ExecutionTask is { } create) await create;
        if (SaveSetupCommand.ExecutionTask is { } save) await save;
        if (AddProfileCommand.ExecutionTask is { } add) await add;
        if (SwitchProfileCommand.ExecutionTask is { } profile) await profile;
        BackupDeviceCommand.Cancel(); VerifyBackupCommand.Cancel(); RestoreDeviceCommand.Cancel();
        if (BackupDeviceCommand.ExecutionTask is { } backup) await backup;
        if (VerifyBackupCommand.ExecutionTask is { } verify) await verify;
        if (RestoreDeviceCommand.ExecutionTask is { } restore) await restore;
        if (RecoverRestoreCommand.ExecutionTask is { } recover) await recover;
    }
    private bool CanBackup() => CanSetup() && _session.LastGracefulShutdown?.ProfileId == _settings.ActiveProfileId && !DeviceBackupService.HasPendingRestore(_settings.Runtime.AvdHome);
    private bool CanRecoverRestore() => CanSetup() && DeviceBackupService.HasPendingRestore(_settings.Runtime.AvdHome);
    private DeviceProfile ActiveProfile() => DeviceProfiles.Capture(_settings).Profiles.Single(p => p.Id == _settings.ActiveProfileId);
    private async Task BackupOperationAsync(Func<DeviceBackupService, IProgress<string>, CancellationToken, Task> operation, CancellationToken token)
    {
        if (!CanSetup()) return;
        SetupBusy = true;
        var progress = new Progress<string>(message => BackupStatus = message);
        try {
            await Task.Run(async () => {
                using var lease = ActiveDeviceLease.Acquire(Path.Combine(SettingsStore.DataRoot, "active-device.lock"));
                await operation(new DeviceBackupService(), progress, token);
            }, token);
        } catch (OperationCanceledException) { BackupStatus = "Cancelled before commit. Originals and any staging files retained."; }
        catch (Exception error) { BackupStatus = error.Message + " Original copies retained; recover a pending restore before startup."; }
        finally { SetupBusy = false; }
    }
    [RelayCommand(CanExecute = nameof(CanBackup), IncludeCancelCommand = true)]
    private async Task BackupDeviceAsync(CancellationToken token)
    {
        if (!CanBackup()) return;
        var picker = new Microsoft.Win32.OpenFolderDialog { Title = "Choose a separate directory for a new device backup" };
        if (picker.ShowDialog() != true) return;
        var profile = ActiveProfile(); string? result = null;
        await BackupOperationAsync(async (service, progress, ct) => {
            var target = await _session.BackupTargetAsync(profile, ct);
            result = await service.BackupAsync(target, _session.LastGracefulShutdown!, picker.FolderName, progress, ct);
        }, token);
        if (result is not null) BackupPath = result;
    }
    [RelayCommand(CanExecute = nameof(CanSetup), IncludeCancelCommand = true)]
    private async Task VerifyBackupAsync(CancellationToken token)
    {
        if (!CanSetup()) return;
        var picker = new Microsoft.Win32.OpenFolderDialog { Title = "Select the completed backup directory" };
        if (picker.ShowDialog() != true) return;
        BackupPath = picker.FolderName;
        await BackupOperationAsync(async (service, progress, ct) => {
            await service.VerifyAsync(picker.FolderName, ct); progress.Report("Backup file inventory and checksums verified; Android saved-progress acceptance remains unverified.");
        }, token);
    }
    private async Task<bool> CommitRestoredProfileAsync(DeviceProfile profile)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess()) return await dispatcher.InvokeAsync(() => CommitRestoredProfileAsync(profile)).Task.Unwrap();
        if (profile.Id != _settings.ActiveProfileId || profile.Runtime.ProfileId != profile.Id) return false;
        await _saveOperations.WaitAsync();
        try {
            var candidate = DeviceProfiles.Normalize(_settings with { Runtime = profile.Runtime, SystemImage = profile.SystemImage,
                SelectedApk = profile.SelectedApk, ApkLibrary = profile.ApkLibrary });
            if (_persistent && !await _store.SaveAsync(candidate)) return false;
            _settings = candidate with { Theme = _settings.Theme, Window = _settings.Window };
            _session.Configure(profile.Runtime); ApplyFields(profile.Runtime); SystemImage = profile.SystemImage;
            Device.RestoreSelection(profile.SelectedApk); Device.RestoreLibrary(candidate.ApkLibrary); RefreshProfiles(); ScheduleSave(); return true;
        } finally { _saveOperations.Release(); }
    }
    [RelayCommand(CanExecute = nameof(CanBackup), IncludeCancelCommand = true)]
    private async Task RestoreDeviceAsync(CancellationToken token)
    {
        if (!CanBackup()) return;
        var picker = new Microsoft.Win32.OpenFolderDialog { Title = "Select a compatible backup to restore (original device retained)" };
        if (picker.ShowDialog() != true) return;
        BackupPath = picker.FolderName; var profile = ActiveProfile();
        await BackupOperationAsync(async (service, progress, ct) => {
            var target = await _session.BackupTargetAsync(profile, ct);
            await service.RestoreAsync(target, _session.LastGracefulShutdown!, picker.FolderName, CommitRestoredProfileAsync, progress, ct);
        }, token);
        _session.InvalidateShutdownReceipt(); NotifySetupCommands();
    }
    [RelayCommand(CanExecute = nameof(CanRecoverRestore))]
    private Task RecoverRestoreAsync()
    {
        if (!CanRecoverRestore()) return Task.CompletedTask;
        var home = _settings.Runtime.AvdHome; var id = _settings.ActiveProfileId;
        return BackupOperationAsync(async (service, progress, _) => {
            await service.RecoverAsync(home, id, CommitRestoredProfileAsync);
            _session.InvalidateShutdownReceipt(); progress.Report("Restore recovery finished. Start and gracefully stop the profile before another backup.");
        }, CancellationToken.None);
    }
}

