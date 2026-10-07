using AndroidDesktop.Models;
using AndroidDesktop.Services;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace AndroidDesktop.Tests;

public sealed class ApkLibraryTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "library-tests-" + Guid.NewGuid().ToString("N"));
    private static ApkSelection Entry(string package = "example.one", string device = "device-a")
        => new(new("fixture.apk", package, 26, [], "hash", "Fixture", 1, "1.0"), device, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);
    [Fact] public async Task VersionTwoSelectionMigratesAndPreservesOriginalAndDeviceFiles()
    {
        Directory.CreateDirectory(root);
        var store = new SettingsStore(root);
        var old = new DesktopSettings { Version = 2, Theme = "Arctic", SelectedApk = Entry(), Window = new(15, 25, 950, 750), Runtime = new DesktopSettings().Runtime };
        Directory.CreateDirectory(Path.Combine(root, "avd"));
        var disk = Path.Combine(root, "avd", "userdata.img"); await File.WriteAllTextAsync(disk, "preserve Android data");
        var original = JsonSerializer.Serialize(old); await File.WriteAllTextAsync(store.FilePath, original);
        var migrated = await store.LoadAsync();
        Assert.Equal(4, migrated.Version); Assert.Single(migrated.ApkLibrary);
        Assert.Equal(JsonSerializer.Serialize(old.SelectedApk), JsonSerializer.Serialize(migrated.ApkLibrary[0]));
        Assert.Equal(old.Window, migrated.Window); Assert.Equal(old.Runtime, migrated.Runtime); Assert.Equal(old.Theme, migrated.Theme);
        Assert.True(await store.SaveAsync(migrated)); Assert.Equal(original, await File.ReadAllTextAsync(store.FilePath + ".bak"));
        Assert.Equal("preserve Android data", await File.ReadAllTextAsync(disk));
    }
    [Fact] public void UpdateRetainsOtherPackagesAndDevices()
    {
        var original = Entry(); var other = Entry("example.two"); var device = Entry(device: "device-b");
        var updated = original with { Apk = original.Apk with { VersionCode = 2, Path = "moved.apk" } };
        var result = ApkLibrary.Upsert([original, other, device], updated);
        Assert.Equal(3, result.Length); Assert.Same(updated, ApkLibrary.Find(result, "device-a", "example.one"));
        Assert.Same(other, ApkLibrary.Find(result, "device-a", "example.two")); Assert.Same(device, ApkLibrary.Find(result, "device-b", "example.one"));
        Assert.Equal("fixture.apk", original.Apk.Path);
    }
    [Fact] public async Task CorruptLibraryRecoversPreviousCommitAndFailedWriteRetainsIt()
    {
        var store = new SettingsStore(root); var old = new DesktopSettings { SelectedApk = Entry() };
        Assert.True(await store.SaveAsync(old)); Assert.True(await store.SaveAsync(old with { Theme = "Arctic" }));
        await File.WriteAllTextAsync(store.FilePath, "{\"Version\":3,\"ApkLibrary\":null}");
        var recovered = await store.LoadAsync(); Assert.Single(recovered.ApkLibrary);
        var backup = await File.ReadAllTextAsync(store.FilePath + ".bak");
        using (File.Open(store.FilePath, FileMode.Open, FileAccess.Read, FileShare.None))
            Assert.False(await store.SaveAsync(recovered with { ApkLibrary = [Entry("example.two")] }));
        Assert.Equal(backup, await File.ReadAllTextAsync(store.FilePath + ".bak"));
        Assert.True(await store.SaveAsync(recovered)); Assert.Equal(backup, await File.ReadAllTextAsync(store.FilePath + ".bak"));
    }
    [Fact] public async Task SwitchingUsesMatchingEntryAndRelocationRejectsAnotherPackage()
    {
        Directory.CreateDirectory(root); var path = Path.Combine(root, "source.apk"); await File.WriteAllTextAsync(path, "fake inspector fixture");
        var matching = Entry() with { Apk = Entry().Apk with { Path = path } }; var different = Entry("example.two");
        var installer = new Installer(matching.Apk); var importer = new ApkImportService(installer);
        var selected = await importer.OpenAsync(path, "device-a", different, false, _ => Task.FromResult(true), new SilentProgress(), default,
            (device, package) => ApkLibrary.Find([matching, different], device, package), "example.one");
        Assert.Equal(0, installer.Installs); Assert.Equal(1, installer.Queries); Assert.Equal(matching.LastInstalledUtc, selected.LastInstalledUtc);
        await Assert.ThrowsAsync<InvalidDataException>(() => importer.OpenAsync(path, "device-a", matching, false, _ => throw new Exception("must not commit"), new SilentProgress(), default, expectedPackage: "example.wrong"));
        Assert.Equal(1, installer.Launches);
    }
    [Fact] public async Task ThousandEntryRoundTripAndLookupRetainEveryIdentity()
    {
        var entries = Enumerable.Range(0, 1000).Select(i => Entry("example.app" + i)).ToArray(); var store = new SettingsStore(root);
        var watch = Stopwatch.StartNew(); Assert.True(await store.SaveAsync(new() { ApkLibrary = entries })); var loaded = await store.LoadAsync();
        foreach (var entry in entries) Assert.NotNull(ApkLibrary.Find(loaded.ApkLibrary, entry.DeviceId, entry.Apk.PackageId));
        watch.Stop(); Assert.Equal(1000, loaded.ApkLibrary.Length);
        // Generous regression ceiling for bounded metadata operations, not a gameplay budget.
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(15), watch.Elapsed.ToString());
        Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "docs", "phase-5"));
        await File.WriteAllTextAsync(Path.Combine(Environment.CurrentDirectory, "docs", "phase-5", "library-performance.json"), JsonSerializer.Serialize(new { entries = 1000, saveLoadAndThousandLookupsMs = watch.Elapsed.TotalMilliseconds, bytes = new FileInfo(store.FilePath).Length, runtime = Environment.Version.ToString(), androidConnected = false }));
    }
    private sealed class SilentProgress : IProgress<string> { public void Report(string value) { } }
    private sealed class Installer(ApkMetadata apk) : IApkInstallService
    {
        public int Installs, Queries, Launches;
        public Task<ApkMetadata> InspectAsync(string path, CancellationToken token) => Task.FromResult(apk);
        public Task CheckDeviceAsync(ApkMetadata value, CancellationToken token) => Task.CompletedTask;
        public Task<bool> IsInstalledAsync(ApkMetadata value, CancellationToken token) { Queries++; return Task.FromResult(true); }
        public Task InstallAsync(ApkMetadata value, CancellationToken token) { Installs++; return Task.CompletedTask; }
        public Task LaunchAsync(string package, bool relaunch, CancellationToken token) { Launches++; return Task.CompletedTask; }
    }
    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
}


