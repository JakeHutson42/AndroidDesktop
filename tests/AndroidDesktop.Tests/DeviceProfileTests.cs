using AndroidDesktop.Models;
using AndroidDesktop.Services;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace AndroidDesktop.Tests;

public sealed class DeviceProfileTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "profile-tests-" + Guid.NewGuid().ToString("N"));
    private static DesktopSettings Legacy => new() { Version = 3, Theme = "Arctic", SelectedApk = new(new("game.apk", "example.game", 26, [], "hash"), "existing-device", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch) };
    private static DeviceProfile NewProfile => DeviceProfiles.Create("Test device", new DesktopSettings().Runtime, new DesktopSettings().SystemImage);
    [Fact] public async Task LegacyDefaultRetainsExactMetadataPathsAndConfigWithoutCreatingDeviceFiles()
    {
        Directory.CreateDirectory(root); var old = Legacy; var original = JsonSerializer.Serialize(old);
        var store = new SettingsStore(root); await File.WriteAllTextAsync(store.FilePath, original);
        var migrated = await store.LoadAsync(); var profile = Assert.Single(migrated.Profiles);
        Assert.Equal(4, migrated.Version); Assert.Equal("default", profile.Id); Assert.Equal(old.Runtime, profile.Runtime);
        Assert.Equal(old.SystemImage, profile.SystemImage); Assert.Equal(old.Theme, migrated.Theme);
        Assert.Equal(JsonSerializer.Serialize(old.SelectedApk), JsonSerializer.Serialize(profile.SelectedApk));
        Assert.Single(profile.ApkLibrary); Assert.Null(profile.Runtime.MemoryMb); Assert.Null(profile.Runtime.CpuCores);
        Assert.True(await store.SaveAsync(migrated)); Assert.Equal(original, await File.ReadAllTextAsync(store.FilePath + ".bak"));
        Assert.Equal(2, Directory.GetFiles(root).Length); Assert.Empty(Directory.GetDirectories(root));
    }
    [Fact] public async Task SwitchingAndRoundTripKeepEachSelectionLibraryImageAndResourceBudget()
    {
        var state = DeviceProfiles.Normalize(Legacy); var profile = NewProfile;
        state = state with { Profiles = state.Profiles.Append(profile).ToArray() };
        var switched = DeviceProfiles.Switch(state, profile.Id);
        Assert.Null(switched.SelectedApk); Assert.Empty(switched.ApkLibrary); Assert.Equal(profile.Runtime, switched.Runtime);
        var newSelection = Legacy.SelectedApk! with { DeviceId = "new-device" };
        switched = switched with { SelectedApk = newSelection, ApkLibrary = [newSelection], Runtime = switched.Runtime with { MemoryMb = 4096, CpuCores = 4 } };
        var store = new SettingsStore(root); Assert.True(await store.SaveAsync(switched)); var reloaded = await store.LoadAsync();
        Assert.Equal(profile.Id, reloaded.ActiveProfileId); Assert.Equal(4096, reloaded.Runtime.MemoryMb);
        var back = DeviceProfiles.Switch(reloaded, "default");
        Assert.Equal(state.Runtime, back.Runtime); Assert.Equal("existing-device", back.SelectedApk!.DeviceId); Assert.Single(back.ApkLibrary);
        var again = DeviceProfiles.Switch(back, profile.Id); Assert.Equal("new-device", again.SelectedApk!.DeviceId); Assert.Equal(4, again.Runtime.CpuCores);
    }
    [Fact] public void NewProfilesHaveSeparatePathsAndStableIdsRegardlessOfDisplayNameAndSerial()
    {
        var first = NewProfile; var second = NewProfile;
        Assert.NotEqual(first.Id, second.Id); Assert.NotEqual(first.Runtime.AvdHome, second.Runtime.AvdHome);
        Assert.Equal(first.Runtime.Serial, second.Runtime.Serial); Assert.Equal(first.Runtime.ProfileId, first.Id);
        Assert.Equal(first.Runtime.AvdHome, (first with { Name = "Renamed" }).Runtime.AvdHome);
        Assert.False(Directory.Exists(first.Runtime.AvdHome)); Assert.Empty(first.ApkLibrary);
    }
    [Theory][InlineData("../escape")][InlineData("DEFAULT")][InlineData("not-a-guid")]
    public void InvalidProfileIdentitiesCannotCreatePaths(string id) => Assert.Throws<InvalidDataException>(() => DeviceProfiles.Home(id));
    [Fact] public void WrongHomeAvdNameAndBoundsAreRejectedBeforeTools()
    {
        var profile = NewProfile;
        Assert.Throws<InvalidDataException>(() => (profile.Runtime with { AvdHome = new DesktopSettings().Runtime.AvdHome }).Validate());
        Assert.Throws<InvalidDataException>(() => (profile.Runtime with { AvdName = "another" }).Validate());
        Assert.Throws<InvalidDataException>(() => (profile.Runtime with { MemoryMb = 8193 }).Validate());
        Assert.Throws<InvalidDataException>(() => (profile.Runtime with { CpuCores = 0 }).Validate());
        Assert.Throws<InvalidDataException>(() => DeviceProfiles.Create("", profile.Runtime, profile.SystemImage));
        Assert.Throws<InvalidDataException>(() => DeviceProfiles.Switch(DeviceProfiles.Normalize(Legacy), "unknown"));
        Assert.Empty(EmulatorSessionService.ResourceArguments(new DesktopSettings().Runtime));
        Assert.Equal(new[] { "-memory", "2048", "-cores", "2" }, EmulatorSessionService.ResourceArguments(profile.Runtime));
    }
    [Fact] public async Task InvalidProfilePrimaryRecoversBackupAndLockedWriteKeepsActiveProfile()
    {
        var store = new SettingsStore(root); var state = DeviceProfiles.Normalize(Legacy); var profile = NewProfile;
        state = state with { Profiles = state.Profiles.Append(profile).ToArray() }; Assert.True(await store.SaveAsync(state)); Assert.True(await store.SaveAsync(state));
        var before = await File.ReadAllTextAsync(store.FilePath);
        using (File.Open(store.FilePath, FileMode.Open, FileAccess.Read, FileShare.None)) Assert.False(await store.SaveAsync(DeviceProfiles.Switch(state, profile.Id)));
        Assert.Equal(before, await File.ReadAllTextAsync(store.FilePath));
        await File.WriteAllTextAsync(store.FilePath, "{\"Version\":4,\"Profiles\":null}");
        var loaded = await store.LoadAsync(); Assert.Equal("default", loaded.ActiveProfileId); Assert.Equal(2, loaded.Profiles.Length);
        Assert.True(await store.SaveAsync(loaded)); Assert.Equal(before, await File.ReadAllTextAsync(store.FilePath + ".bak"));
    }
    [Fact] public void DuplicateOrMismatchedProfileRecordsAreRejected()
    {
        var state = DeviceProfiles.Normalize(Legacy); var profile = NewProfile;
        Assert.Throws<InvalidDataException>(() => DeviceProfiles.Normalize(state with { Profiles = [profile, profile] }));
        Assert.Throws<InvalidDataException>(() => DeviceProfiles.Normalize(state with { Profiles = [profile with { Runtime = state.Runtime }] }));
    }
    [Fact] public void ActiveLeaseExcludesOtherSessionsAndStaleChildRecordsFailClosed()
    {
        Directory.CreateDirectory(root); var path = Path.Combine(root, "active.lock");
        using (var lease = ActiveDeviceLease.Acquire(path)) Assert.Throws<InvalidOperationException>(() => ActiveDeviceLease.Acquire(path));
        using (ActiveDeviceLease.Acquire(path)) { }
        using var host = Process.GetCurrentProcess();
        File.WriteAllText(path, JsonSerializer.Serialize(new[] { new ActiveDeviceLease.Owner(host.Id, host.StartTime.ToUniversalTime().Ticks) }));
        Assert.Throws<InvalidOperationException>(() => ActiveDeviceLease.Acquire(path));
        var record = File.ReadAllText(path); Assert.Contains(host.Id.ToString(), record);
        // A stale PID with a different start identity is not mistaken for the prior child.
        File.WriteAllText(path, JsonSerializer.Serialize(new[] { new ActiveDeviceLease.Owner(host.Id, 1) }));
        using (ActiveDeviceLease.Acquire(path)) { }
        File.WriteAllText(path, "corrupt"); Assert.Throws<JsonException>(() => ActiveDeviceLease.Acquire(path)); Assert.Equal("corrupt", File.ReadAllText(path));
    }
    [Fact] public async Task BoundedProfileMetadataWorkloadRetainsAllLibrariesAndSwitches()
    {
        var state = DeviceProfiles.Normalize(Legacy);
        var profiles = new List<DeviceProfile>(state.Profiles);
        for (var i = 1; i < DeviceProfiles.MaximumProfiles; i++) {
            var profile = NewProfile;
            var entries = Enumerable.Range(0, 100).Select(j => Legacy.SelectedApk! with {
                DeviceId = profile.Id, Apk = Legacy.SelectedApk!.Apk with { PackageId = "example.app" + j } }).ToArray();
            profiles.Add(profile with { SelectedApk = entries[0], ApkLibrary = entries });
        }
        state = state with { Profiles = profiles.ToArray() }; var store = new SettingsStore(root);
        var watch = Stopwatch.StartNew(); Assert.True(await store.SaveAsync(state)); var loaded = await store.LoadAsync();
        foreach (var profile in profiles) {
            loaded = DeviceProfiles.Switch(loaded, profile.Id);
            Assert.Equal(profile.ApkLibrary.Length, loaded.ApkLibrary.Length);
        }
        watch.Stop(); Assert.True(watch.Elapsed < TimeSpan.FromSeconds(15), watch.Elapsed.ToString());
        var reportDirectory = Path.Combine(Environment.CurrentDirectory, "docs", "phase-6"); Directory.CreateDirectory(reportDirectory);
        await File.WriteAllTextAsync(Path.Combine(reportDirectory, "profile-performance.json"), JsonSerializer.Serialize(new {
            profiles = 32, totalLibraryEntries = profiles.Sum(p => p.ApkLibrary.Length), saveLoadAnd32SwitchesMs = watch.Elapsed.TotalMilliseconds,
            bytes = new FileInfo(store.FilePath).Length, runtime = Environment.Version.ToString(), androidConnected = false }));
    }
    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
}
