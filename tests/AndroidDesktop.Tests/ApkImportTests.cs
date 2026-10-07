using AndroidDesktop.Models;
using AndroidDesktop.Services;
using System.IO;

namespace AndroidDesktop.Tests;

public sealed class ApkImportTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), "import test " + Guid.NewGuid().ToString("N") + ".apk");
    public ApkImportTests() => File.WriteAllText(_path, "fixture: fake inspector supplies metadata");
    private ApkMetadata Apk => new(_path, "example.game", 26, [], "hash", "Test Game", 3, "1.3");
    private ApkSelection Previous => new(Apk, "device-a", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(-1));
    private static readonly IProgress<string> Progress = new SilentProgress();
    [Theory]
    [InlineData("game.aab")][InlineData("game.apks")][InlineData("game.xapk")][InlineData("game.obb")]
    public void UnsupportedFormatsExplainTheBoundary(string file)
        => Assert.Contains("standalone", Assert.Throws<InvalidDataException>(() => ApkImportService.ValidateFiles([file])).Message);
    [Fact] public void MultipleFilesAndMissingSourcesAreRejected()
    {
        Assert.Throws<InvalidDataException>(() => ApkImportService.ValidateFiles([_path, _path]));
        Assert.Throws<InvalidDataException>(() => ApkImportService.ValidateFiles([]));
        Assert.Throws<FileNotFoundException>(() => ApkImportService.ValidateFiles([_path + ".missing.apk"]));
        Assert.Equal(_path, ApkImportService.ValidateFiles([_path]));
    }
    [Fact] public async Task UnchangedInstalledAppLaunchesWithoutInstallAndKeepsInstallTime()
    {
        var previous = Previous; var fake = new FakeInstaller(Apk) { Installed = true }; ApkSelection? committed = null;
        var result = await new ApkImportService(fake).OpenAsync(_path, "device-a", previous, false,
            selected => { committed = selected; return Task.FromResult(true); }, Progress, CancellationToken.None);
        Assert.Equal(["inspect", "compatibility", "query", "launch"], fake.Calls);
        Assert.Equal(previous.LastInstalledUtc, result.LastInstalledUtc); Assert.Equal(result, committed);
    }
    [Fact] public async Task AbsentOrWrongInstalledVersionReinstalls()
    {
        var fake = new FakeInstaller(Apk) { Installed = false };
        var result = await new ApkImportService(fake).OpenAsync(_path, "device-a", Previous, false, _ => Task.FromResult(true), Progress, CancellationToken.None);
        Assert.Equal(["inspect", "compatibility", "query", "install", "launch"], fake.Calls);
        Assert.True(result.LastInstalledUtc > Previous.LastInstalledUtc);
    }
    [Theory][InlineData(true)][InlineData(false)]
    public async Task ChangedSourceOrDifferentDeviceCannotUseInstalledShortcut(bool changed)
    {
        var fake = new FakeInstaller(Apk) { Installed = true };
        var previous = changed ? Previous with { Apk = Apk with { Sha256 = "old" } } : Previous with { DeviceId = "old-device" };
        await new ApkImportService(fake).OpenAsync(_path, "device-a", previous, false, _ => Task.FromResult(true), Progress, CancellationToken.None);
        Assert.Equal(["inspect", "compatibility", "install", "launch"], fake.Calls);
    }
    [Theory][InlineData("compatibility")][InlineData("install")][InlineData("launch")]
    public async Task FailedOperationCannotCommitNewSelection(string stage)
    {
        var old = Previous; var persisted = old; var fake = new FakeInstaller(Apk) { FailAt = stage };
        await Assert.ThrowsAsync<InvalidOperationException>(() => new ApkImportService(fake).OpenAsync(_path, "device-a", old, false,
            selected => { persisted = selected; return Task.FromResult(true); }, Progress, CancellationToken.None));
        Assert.Equal(old, persisted);
        if (stage == "compatibility") Assert.DoesNotContain("install", fake.Calls);
        if (stage == "install") Assert.DoesNotContain("launch", fake.Calls);
    }
    [Fact] public async Task FailedPersistenceRetainsPriorSettings()
    {
        var root = Path.Combine(Path.GetTempPath(), "selection-store-" + Guid.NewGuid().ToString("N"));
        var store = new SettingsStore(root); var old = Previous;
        try {
            await store.SaveAsync(new() { SelectedApk = old });
            var committedJson = await File.ReadAllTextAsync(store.FilePath);
            using (File.Open(store.FilePath, FileMode.Open, FileAccess.Read, FileShare.None))
                await Assert.ThrowsAsync<IOException>(() => new ApkImportService(new FakeInstaller(Apk)).OpenAsync(_path, "device-a", old, false,
                    selected => store.SaveAsync(new() { SelectedApk = selected }), Progress, CancellationToken.None));
            Assert.Equal(committedJson, await File.ReadAllTextAsync(store.FilePath));
            Assert.Equal(old.Apk.PackageId, (await store.LoadAsync()).SelectedApk!.Apk.PackageId);
        } finally { foreach (var file in Directory.GetFiles(root)) File.Delete(file); Directory.Delete(root); }
    }
    [Fact] public async Task SourceCannotChangeBetweenInspectionAndTransfer()
    {
        var fake = new FakeInstaller(Apk) { OnInspect = () => Assert.Throws<IOException>(() => File.WriteAllText(_path, "changed")) };
        await new ApkImportService(fake).OpenAsync(_path, "device-a", null, false, _ => Task.FromResult(true), Progress, CancellationToken.None);
    }
    [Fact] public async Task CancelledInstallCannotCommitAndReleasesSourceLock()
    {
        var fake = new FakeInstaller(Apk) { CancelInstall = true }; var commits = 0;
        await Assert.ThrowsAsync<OperationCanceledException>(() => new ApkImportService(fake).OpenAsync(_path, "device-a", Previous, false,
            _ => { commits++; return Task.FromResult(true); }, Progress, CancellationToken.None));
        Assert.Equal(0, commits); Assert.DoesNotContain("launch", fake.Calls); File.WriteAllText(_path, "available");
    }
    [Fact] public async Task ReplacingPackageAndRelaunchStayScopedToSelectedPackage()
    {
        var fake = new FakeInstaller(Apk with { PackageId = "example.other" });
        var result = await new ApkImportService(fake).OpenAsync(_path, "device-a", Previous, true, _ => Task.FromResult(true), Progress, CancellationToken.None);
        Assert.Equal("example.other", result.Apk.PackageId); Assert.True(fake.Relaunch);
        Assert.Equal("example.other", fake.LaunchedPackage);
    }
    public void Dispose() => File.Delete(_path);
    private sealed class SilentProgress : IProgress<string> { public void Report(string value) { } }
    private sealed class FakeInstaller(ApkMetadata apk) : IApkInstallService
    {
        public List<string> Calls { get; } = [];
        public bool Installed, CancelInstall, Relaunch;
        public string? FailAt, LaunchedPackage;
        public Action? OnInspect;
        private void Call(string stage) { Calls.Add(stage); if (FailAt == stage) throw new InvalidOperationException(stage + " failed"); }
        public Task<ApkMetadata> InspectAsync(string path, CancellationToken token) { Call("inspect"); OnInspect?.Invoke(); return Task.FromResult(apk); }
        public Task CheckDeviceAsync(ApkMetadata value, CancellationToken token) { Call("compatibility"); return Task.CompletedTask; }
        public Task<bool> IsInstalledAsync(ApkMetadata value, CancellationToken token) { Call("query"); return Task.FromResult(Installed); }
        public Task InstallAsync(ApkMetadata value, CancellationToken token) { Call("install"); if (CancelInstall) throw new OperationCanceledException(); return Task.CompletedTask; }
        public Task LaunchAsync(string package, bool relaunch, CancellationToken token) { Call("launch"); LaunchedPackage = package; Relaunch = relaunch; return Task.CompletedTask; }
    }
}
