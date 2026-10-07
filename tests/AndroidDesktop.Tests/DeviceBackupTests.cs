using AndroidDesktop.Models;
using AndroidDesktop.Services;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace AndroidDesktop.Tests;

public sealed class DeviceBackupTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "backup-tests-" + Guid.NewGuid().ToString("N"));
    private readonly BackupTarget target;
    private readonly ShutdownReceipt receipt;
    private readonly string disk, parent;
    private static readonly IProgress<string> Progress = new SilentProgress();
    public DeviceBackupTests()
    {
        var options = new DesktopSettings().Runtime; var home = Path.Combine(root, "device"); parent = Path.Combine(root, "backups");
        var device = Path.Combine(home, options.AvdName + ".avd"); Directory.CreateDirectory(device); Directory.CreateDirectory(parent);
        var id = Guid.NewGuid().ToString("D"); File.WriteAllText(Path.Combine(device, "android-desktop-device-id"), id);
        File.WriteAllText(Path.Combine(home, options.AvdName + ".ini"), "path=" + device);
        File.WriteAllText(Path.Combine(device, "config.ini"), "fixture config; no Android device"); disk = Path.Combine(device, "userdata.img"); File.WriteAllText(disk, "saved progress fixture");
        var selection = new ApkSelection(new("fixture.apk", "example.game", 26, [], "hash"), id, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);
        target = new(home, "default", options.AvdName, id, "fixture runtime signature", new("default", "Default device", options, new DesktopSettings().SystemImage, selection, [selection]));
        receipt = new(target.ProfileId, id, target.RuntimeIdentity, DateTimeOffset.UtcNow);
    }
    private DeviceBackupService Service => new(_ => long.MaxValue);
    private Task<string> Backup() => Service.BackupAsync(target, receipt, parent, Progress, default);
    [Fact] public async Task BackupVerifiesAllBytesAndRestorePreservesOriginalsAndMetadata()
    {
        var backup = await Backup(); var manifest = await Service.VerifyAsync(backup, default); Assert.Equal(4, manifest.Files.Length);
        Assert.Equal("saved progress fixture", File.ReadAllText(disk)); File.WriteAllText(disk, "new progress after backup");
        DeviceProfile? committed = null;
        await Service.RestoreAsync(target, receipt, backup, profile => { committed = profile; return Task.FromResult(true); }, Progress, default);
        Assert.Equal("saved progress fixture", File.ReadAllText(disk)); Assert.Equal(target.DeviceId, committed!.SelectedApk!.DeviceId);
        var original = Assert.Single(Directory.GetDirectories(root, "device.original-*"));
        Assert.Equal("new progress after backup", File.ReadAllText(Path.Combine(original, target.AvdName + ".avd", "userdata.img")));
        Assert.False(DeviceBackupService.HasPendingRestore(target.Home)); Assert.True(Directory.Exists(backup));
    }
    [Theory][InlineData("prepared")][InlineData("original-preserved")][InlineData("device-installed")][InlineData("metadata-committed")]
    public async Task EveryInterruptedCommitRecoversOriginalDiskAndMetadata(string point)
    {
        var backup = await Backup(); File.WriteAllText(disk, "original retained state"); DeviceProfile persisted = target.Profile;
        var service = new DeviceBackupService(_ => long.MaxValue, checkpoint => { if (checkpoint == point) throw new IOException("simulated interruption"); });
        await Assert.ThrowsAsync<IOException>(() => service.RestoreAsync(target, receipt, backup, profile => { persisted = profile; return Task.FromResult(true); }, Progress, default));
        Assert.True(DeviceBackupService.HasPendingRestore(target.Home));
        await Service.RecoverAsync(target.Home, target.ProfileId, profile => { persisted = profile; return Task.FromResult(true); });
        Assert.Equal("original retained state", File.ReadAllText(disk)); Assert.Equal(JsonSerializer.Serialize(target.Profile), JsonSerializer.Serialize(persisted));
        Assert.False(DeviceBackupService.HasPendingRestore(target.Home)); Assert.True(Directory.Exists(backup));
    }
    [Fact] public async Task FailedMetadataCommitRollsBackDisksAndKeepsJournalUntilRecoverySaveSucceeds()
    {
        var backup = await Backup(); File.WriteAllText(disk, "before restore");
        await Assert.ThrowsAsync<IOException>(() => Service.RestoreAsync(target, receipt, backup, _ => Task.FromResult(false), Progress, default));
        Assert.Equal("before restore", File.ReadAllText(disk)); Assert.True(DeviceBackupService.HasPendingRestore(target.Home));
        await Service.RecoverAsync(target.Home, "default", _ => Task.FromResult(true)); Assert.False(DeviceBackupService.HasPendingRestore(target.Home));
    }
    [Fact] public async Task CorruptionAndExtraFilesCannotVerifyOrChangeOriginals()
    {
        var backup = await Backup(); var copy = Path.Combine(backup, "data", target.AvdName + ".avd", "userdata.img"); File.WriteAllText(copy, "corrupt");
        await Assert.ThrowsAsync<InvalidDataException>(() => Service.VerifyAsync(backup, default));
        await Assert.ThrowsAsync<InvalidDataException>(() => Service.RestoreAsync(target, receipt, backup, _ => throw new Exception("must not commit"), Progress, default));
        Assert.Equal("saved progress fixture", File.ReadAllText(disk));
        File.WriteAllText(copy, "saved progress fixture"); File.WriteAllText(Path.Combine(backup, "data", "extra"), "extra");
        await Assert.ThrowsAsync<InvalidDataException>(() => Service.VerifyAsync(backup, default));
    }
    [Fact] public async Task IncompatibleRuntimeOrDeviceIsRejectedBeforeCommit()
    {
        var backup = await Backup(); var wrong = target with { RuntimeIdentity = "different image/emulator" };
        await Assert.ThrowsAsync<InvalidDataException>(() => Service.RestoreAsync(wrong, receipt with { RuntimeIdentity = wrong.RuntimeIdentity }, backup, _ => throw new Exception("must not commit"), Progress, default));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service.BackupAsync(target, receipt with { DeviceId = Guid.NewGuid().ToString() }, parent, Progress, default));
        Assert.Equal("saved progress fixture", File.ReadAllText(disk));
    }
    [Fact] public async Task TraversalAndUnsupportedManifestAreRejected()
    {
        var backup = await Backup(); var manifest = await Service.VerifyAsync(backup, default); var path = Path.Combine(backup, "manifest.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(manifest with { Files = [new("../escape", 0, new string('A', 64))] }));
        await Assert.ThrowsAsync<InvalidDataException>(() => Service.VerifyAsync(backup, default));
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(manifest with { SchemaVersion = 99 }));
        await Assert.ThrowsAsync<InvalidDataException>(() => Service.VerifyAsync(backup, default));
    }
    [Fact] public async Task LowSpaceCancellationLockedSourceAndOverlapPreserveOriginals()
    {
        await Assert.ThrowsAsync<IOException>(() => new DeviceBackupService(_ => 0).BackupAsync(target, receipt, parent, Progress, default));
        using (File.Open(disk, FileMode.Open, FileAccess.Write, FileShare.None))
            await Assert.ThrowsAsync<IOException>(() => Backup());
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Service.BackupAsync(target, receipt, parent, Progress, cancel.Token));
        await Assert.ThrowsAsync<InvalidDataException>(() => Service.BackupAsync(target, receipt, target.Home, Progress, default));
        var backup = await Backup(); await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Service.RestoreAsync(target, receipt, backup, _ => throw new Exception("must not commit"), Progress, cancel.Token));
        Assert.Equal("saved progress fixture", File.ReadAllText(disk)); Assert.False(DeviceBackupService.HasPendingRestore(target.Home));
    }
    [Fact] public async Task PendingJournalBlocksAnotherRestoreAndBadJournalPreservesCopies()
    {
        var backup = await Backup(); File.WriteAllText(DeviceBackupService.JournalPath(target.Home), "corrupt journal");
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service.RestoreAsync(target, receipt, backup, _ => Task.FromResult(true), Progress, default));
        await Assert.ThrowsAsync<JsonException>(() => Service.RecoverAsync(target.Home, target.ProfileId, _ => Task.FromResult(true)));
        Assert.Equal("saved progress fixture", File.ReadAllText(disk)); Assert.True(DeviceBackupService.HasPendingRestore(target.Home));
    }
    [Fact] public async Task StreamingCopyAndVerificationMeasureFixtureWorkload()
    {
        await File.WriteAllBytesAsync(disk, new byte[16 * 1024 * 1024]); var watch = Stopwatch.StartNew(); var backup = await Backup();
        await Service.VerifyAsync(backup, default); watch.Stop(); Assert.True(watch.Elapsed < TimeSpan.FromSeconds(30));
        var report = Path.Combine(Environment.CurrentDirectory, "docs", "phase-7"); Directory.CreateDirectory(report);
        await File.WriteAllTextAsync(Path.Combine(report, "backup-performance.json"), JsonSerializer.Serialize(new { payloadBytes = 16 * 1024 * 1024, copyPlusTwoVerificationsMs = watch.Elapsed.TotalMilliseconds,
            streamingBufferBytes = 65536, maximumFileHandles = DeviceBackupService.MaximumFiles, runtime = Environment.Version.ToString(), androidConsistencyVerified = false }));
    }
    [Fact] public async Task CancellationDuringStagingAndSourceLocksLeaveNoCommittedRestore()
    {
        var backup = await Backup(); using var cancelled = new CancellationTokenSource();
        var progress = new CallbackProgress(_ => cancelled.Cancel());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Service.RestoreAsync(target, receipt, backup, _ => throw new Exception("must not commit"), progress, cancelled.Token));
        Assert.Equal("saved progress fixture", File.ReadAllText(disk)); Assert.False(DeviceBackupService.HasPendingRestore(target.Home));
        Assert.NotEmpty(Directory.GetDirectories(root, "device.restore-stage-*"));
    }
    [Fact] public async Task LockArtifactsAndDirectoryLinksCannotBecomeCompletedBackups()
    {
        var lockFile = Path.Combine(target.Home, "running.lock"); File.WriteAllText(lockFile, "fixture lock");
        await Assert.ThrowsAsync<IOException>(() => Backup()); File.Delete(lockFile);
        var link = Path.Combine(target.Home, "linked-data");
        // Junction creation needs no symbolic-link privilege on ordinary Windows installations.
        (await new AndroidToolService().RunAsync("cmd.exe", ["/c", "mklink", "/J", link, parent], TimeSpan.FromSeconds(10), default)).RequireSuccess();
        try { await Assert.ThrowsAsync<InvalidDataException>(() => Backup()); }
        finally { Directory.Delete(link); }
        Assert.Empty(Directory.GetDirectories(parent, "AndroidDesktop-backup-*"));
    }
    [Fact] public async Task SessionWithoutGracefulReceiptCannotOfferDeviceBackup()
    {
        var session = new EmulatorSessionService(new DesktopSettings().Runtime, new AndroidToolService(), new EvidenceService());
        Assert.Null(session.LastGracefulShutdown);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.BackupTargetAsync(target.Profile, default));
    }
    private sealed class CallbackProgress(Action<string> action) : IProgress<string> { public void Report(string message) => action(message); }
    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
    private sealed class SilentProgress : IProgress<string> { public void Report(string message) { } }
}

