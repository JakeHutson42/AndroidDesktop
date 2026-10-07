namespace AndroidDesktop.Models;

public sealed record ShutdownReceipt(string ProfileId, string DeviceId, string RuntimeIdentity, DateTimeOffset StoppedUtc);
public sealed record BackupFile(string Path, long Bytes, string Sha256);
public sealed record BackupTarget(string Home, string ProfileId, string AvdName, string DeviceId, string RuntimeIdentity, DeviceProfile Profile);
public sealed record DeviceBackupManifest(int SchemaVersion, string Id, DateTimeOffset CreatedUtc, string ProfileId, string Home,
    string DeviceId, string AvdName, string RuntimeIdentity, DeviceProfile Profile, ShutdownReceipt Shutdown,
    BackupFile[] Files, bool Verified, string Consistency = "graceful-host-shutdown; Android saved-progress acceptance unverified");
public sealed record RestoreJournal(int SchemaVersion, string Id, string ProfileId, DeviceProfile Before, string State);
