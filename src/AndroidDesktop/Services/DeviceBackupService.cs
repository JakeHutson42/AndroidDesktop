using AndroidDesktop.Models;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace AndroidDesktop.Services;

/// <summary>Same-profile stopped-device backup/restore. The caller must hold the exclusive device lease.</summary>
public sealed class DeviceBackupService
{
    public const int MaximumFiles = 4096;
    private const int MaximumManifestBytes = 4 * 1024 * 1024;
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private readonly Func<string, long> _freeSpace;
    private readonly Action<string>? _checkpoint;
    public DeviceBackupService(Func<string, long>? freeSpace = null, Action<string>? checkpoint = null)
    {
        _freeSpace = freeSpace ?? (path => new DriveInfo(Path.GetPathRoot(Path.GetFullPath(path))!).AvailableFreeSpace);
        _checkpoint = checkpoint;
    }
    public static string JournalPath(string home) => Full(home) + ".restore.json";
    public static bool HasPendingRestore(string home) => File.Exists(JournalPath(home));
    private static string Full(string path) => Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
    private static bool Inside(string path, string root) => Full(path).Equals(Full(root), StringComparison.OrdinalIgnoreCase) ||
        Full(path).StartsWith(Full(root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    public static void CheckNoLinks(string path)
    {
        for (var current = Full(path); current is not null; current = Path.GetDirectoryName(current))
            if ((Directory.Exists(current) || File.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Backup and device storage cannot contain symbolic links or junctions.");
    }
    private static string FilePath(string root, string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || relative.Length > 1024 || relative.Contains('\\') || relative.Contains(':') ||
            relative.Split('/').Any(p => p.Length == 0 || p is "." or ".." || p.EndsWith('.') || p.EndsWith(' ') || p.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
            throw new InvalidDataException("Invalid backup file path.");
        var path = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
        if (!Inside(path, root) || path.Equals(Full(root), StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Backup path escapes its root.");
        CheckNoLinks(path); return path;
    }
    private static string[] Files(string root)
    {
        CheckNoLinks(root); var paths = new List<string>(); var pending = new Stack<string>(); pending.Push(Full(root));
        var visited = 0;
        while (pending.TryPop(out var directory)) {
            if (++visited > MaximumFiles * 2) throw new InvalidDataException("Device directory inventory is too large.");
            foreach (var entry in Directory.EnumerateFileSystemEntries(directory)) {
                CheckNoLinks(entry);
                if (Path.GetFileName(entry).EndsWith(".lock", StringComparison.OrdinalIgnoreCase)) throw new IOException("Device lock files remain. Confirm shutdown before continuing.");
                if (Directory.Exists(entry)) {
                    if (visited + pending.Count >= MaximumFiles * 2) throw new InvalidDataException("Device directory inventory is too large.");
                    pending.Push(entry);
                }
                else { if (paths.Count >= MaximumFiles) throw new InvalidDataException("Device file inventory exceeds 4096 files."); paths.Add(Path.GetRelativePath(root, entry).Replace('\\', '/')); }
            }
        }
        return paths.Order(StringComparer.OrdinalIgnoreCase).ToArray();
    }
    private void CheckSpace(string path, long bytes)
    {
        if (bytes < 0 || _freeSpace(path) < checked(bytes + 64L * 1024 * 1024)) throw new IOException("Insufficient free space for staging and verification. Originals retained.");
    }
    private static async Task<T> ReadAsync<T>(string path, CancellationToken token)
    {
        CheckNoLinks(path);
        if (new FileInfo(path).Length > MaximumManifestBytes) throw new InvalidDataException("Backup metadata exceeds 4 MiB.");
        return JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(path, token), Json) ?? throw new InvalidDataException("Missing backup metadata.");
    }
    private static async Task WriteAsync<T>(string path, T value)
    {
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try {
            await using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None)) {
                await JsonSerializer.SerializeAsync(file, value, Json); await file.FlushAsync(); file.Flush(true);
                if (file.Length > MaximumManifestBytes) throw new InvalidDataException("Backup metadata exceeds 4 MiB.");
            }
            File.Move(temp, path, true);
        } finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    private static void Receipt(BackupTarget target, ShutdownReceipt receipt)
    {
        if (receipt.ProfileId != target.ProfileId || receipt.DeviceId != target.DeviceId || receipt.RuntimeIdentity != target.RuntimeIdentity ||
            string.IsNullOrWhiteSpace(receipt.RuntimeIdentity) || receipt.StoppedUtc > DateTimeOffset.UtcNow)
            throw new InvalidOperationException("A matching confirmed graceful shutdown is required. Start and stop this profile normally first.");
    }
    private static void Compatible(BackupTarget target, DeviceBackupManifest manifest)
    {
        if (manifest.ProfileId != target.ProfileId || manifest.DeviceId != target.DeviceId || manifest.AvdName != target.AvdName || Full(manifest.Home) != Full(target.Home) ||
            manifest.RuntimeIdentity != target.RuntimeIdentity || manifest.Profile.Runtime != target.Profile.Runtime || manifest.Profile.SystemImage != target.Profile.SystemImage)
            throw new InvalidDataException("Backup device/profile, runtime, image or configuration is incompatible. No original files were changed.");
    }
    private static async Task<string> HashAsync(Stream stream, CancellationToken token) => Convert.ToHexString(await SHA256.HashDataAsync(stream, token));
    public async Task<string> BackupAsync(BackupTarget target, ShutdownReceipt receipt, string parent, IProgress<string> progress, CancellationToken token)
    {
        Receipt(target, receipt); if (HasPendingRestore(target.Home)) throw new InvalidOperationException("Recover the pending restore first.");
        parent = Full(parent); CheckNoLinks(parent); CheckNoLinks(target.Home);
        if (!Directory.Exists(parent) || Inside(parent, target.Home) || Inside(target.Home, parent)) throw new InvalidDataException("Choose an existing backup directory separate from device storage.");
        var id = Guid.NewGuid().ToString("N"); var stage = Path.Combine(parent, ".backup-stage-" + id); var completed = Path.Combine(parent, "AndroidDesktop-backup-" + id);
        var handles = new List<(string Relative, FileStream Stream)>();
        try {
            var files = Files(target.Home); long bytes = 0;
            foreach (var relative in files) {
                token.ThrowIfCancellationRequested(); var stream = new FileStream(FilePath(target.Home, relative), FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
                handles.Add((relative, stream)); bytes = checked(bytes + stream.Length);
            }
            CheckSpace(parent, bytes); Directory.CreateDirectory(Path.Combine(stage, "data")); var inventory = new List<BackupFile>();
            foreach (var (relative, source) in handles) {
                token.ThrowIfCancellationRequested(); progress.Report("Copying " + relative);
                var destination = FilePath(Path.Combine(stage, "data"), relative); Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                await using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, FileOptions.Asynchronous)) {
                    await source.CopyToAsync(output, 65536, token); await output.FlushAsync(token); output.Flush(true);
                }
                source.Position = 0; inventory.Add(new(relative, source.Length, await HashAsync(source, token)));
            }
            if (!files.SequenceEqual(Files(target.Home), StringComparer.OrdinalIgnoreCase)) throw new IOException("Device inventory changed during backup.");
            var manifest = new DeviceBackupManifest(1, id, DateTimeOffset.UtcNow, target.ProfileId, Full(target.Home), target.DeviceId, target.AvdName,
                target.RuntimeIdentity, target.Profile, receipt, inventory.ToArray(), true);
            await WriteAsync(Path.Combine(stage, "manifest.json"), manifest);
            await VerifyAsync(stage, token); token.ThrowIfCancellationRequested(); Directory.Move(stage, completed);
            progress.Report("Backup verified. Android saved-progress acceptance remains unverified."); return completed;
        } finally { foreach (var handle in handles) await handle.Stream.DisposeAsync(); }
        // Partial staging is intentionally retained for inspection, never presented as completed.
    }
    public async Task<DeviceBackupManifest> VerifyAsync(string directory, CancellationToken token)
    {
        directory = Full(directory); var manifest = await ReadAsync<DeviceBackupManifest>(Path.Combine(directory, "manifest.json"), token);
        if (manifest.SchemaVersion != 1 || !Guid.TryParseExact(manifest.Id, "N", out _) || !Guid.TryParse(manifest.DeviceId, out _) || !manifest.Verified ||
            manifest.Profile is null || manifest.Shutdown is null || manifest.Files is null || manifest.Files.Length is < 1 or > MaximumFiles ||
            manifest.Profile.Id != manifest.ProfileId || manifest.Profile.Runtime is null || manifest.AvdName != manifest.Profile.Runtime.AvdName ||
            manifest.Files.Any(f => f is null || f.Bytes < 0 || f.Sha256 is null || !System.Text.RegularExpressions.Regex.IsMatch(f.Sha256, "^[0-9A-F]{64}$")) ||
            manifest.Files.Select(f => f.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count() != manifest.Files.Length)
            throw new InvalidDataException("Invalid or incomplete backup manifest.");
        Receipt(new(directory, manifest.ProfileId, manifest.AvdName, manifest.DeviceId, manifest.RuntimeIdentity, manifest.Profile), manifest.Shutdown);
        manifest.Profile.Runtime.Validate(); SetupService.ValidateImage(manifest.Profile.SystemImage);
        ApkLibrary.Normalize(new DesktopSettings { SelectedApk = manifest.Profile.SelectedApk, ApkLibrary = manifest.Profile.ApkLibrary });
        var data = Path.Combine(directory, "data"); var actual = Files(data);
        if (!actual.SequenceEqual(manifest.Files.Select(f => f.Path).Order(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase))
            throw new InvalidDataException("Backup inventory differs from its manifest.");
        foreach (var file in manifest.Files) {
            await using var stream = new FileStream(FilePath(data, file.Path), FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.Asynchronous);
            if (stream.Length != file.Bytes || await HashAsync(stream, token) != file.Sha256) throw new InvalidDataException("Backup checksum/size mismatch: " + file.Path);
        }
        var index = FilePath(data, manifest.AvdName + ".ini");
        var devicePath = (await File.ReadAllLinesAsync(index, token)).FirstOrDefault(l => l.StartsWith("path=", StringComparison.Ordinal))?[5..];
        if (devicePath is null || !Inside(devicePath, manifest.Home) || Full(devicePath) == Full(manifest.Home)) throw new InvalidDataException("Backup AVD index points outside its original device storage.");
        var marker = Path.GetRelativePath(manifest.Home, devicePath).Replace('\\', '/') + "/android-desktop-device-id";
        if ((await File.ReadAllTextAsync(FilePath(data, marker), token)).Trim() != manifest.DeviceId)
            throw new InvalidDataException("Backup device identity is missing or changed.");
        return manifest;
    }
    private static (string Stage, string Original) RestorePaths(string home, string id)
    {
        if (!Guid.TryParseExact(id, "N", out _)) throw new InvalidDataException("Invalid restore transaction identity.");
        return (Full(home) + ".restore-stage-" + id, Full(home) + ".original-" + id);
    }
    public async Task RestoreAsync(BackupTarget target, ShutdownReceipt receipt, string backup, Func<DeviceProfile, Task<bool>> commit,
        IProgress<string> progress, CancellationToken token)
    {
        Receipt(target, receipt); CheckNoLinks(target.Home); CheckNoLinks(backup);
        if (HasPendingRestore(target.Home)) throw new InvalidOperationException("Recover the pending restore first.");
        if (Inside(backup, target.Home) || Inside(target.Home, backup)) throw new InvalidDataException("Backup and device paths overlap.");
        var manifest = await VerifyAsync(backup, token); Compatible(target, manifest);
        var id = Guid.NewGuid().ToString("N"); var paths = RestorePaths(target.Home, id); CheckSpace(Path.GetDirectoryName(Full(target.Home))!, manifest.Files.Sum(f => f.Bytes));
        Directory.CreateDirectory(paths.Stage);
        foreach (var file in manifest.Files) {
            token.ThrowIfCancellationRequested(); progress.Report("Staging " + file.Path);
            var destination = FilePath(paths.Stage, file.Path); Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            await using var input = new FileStream(FilePath(Path.Combine(backup, "data"), file.Path), FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.Asynchronous);
            await using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, FileOptions.Asynchronous);
            await input.CopyToAsync(output, 65536, token); await output.FlushAsync(token); output.Flush(true);
        }
        foreach (var file in manifest.Files) {
            await using var stream = File.OpenRead(FilePath(paths.Stage, file.Path));
            if (stream.Length != file.Bytes || await HashAsync(stream, token) != file.Sha256) throw new InvalidDataException("Restored staging checksum mismatch.");
        }
        token.ThrowIfCancellationRequested(); var journal = new RestoreJournal(1, id, target.ProfileId, target.Profile, "prepared");
        await WriteAsync(JournalPath(target.Home), journal); _checkpoint?.Invoke("prepared");
        // Once journaled, complete or roll back without cancellation, retaining every original.
        Directory.Move(target.Home, paths.Original); _checkpoint?.Invoke("original-preserved");
        Directory.Move(paths.Stage, target.Home); _checkpoint?.Invoke("device-installed");
        if (!await commit(manifest.Profile)) {
            await RecoverAsync(target.Home, target.ProfileId, commit); throw new IOException("Restore metadata commit failed. Originals restored; recovery staging retained.");
        }
        _checkpoint?.Invoke("metadata-committed");
        await WriteAsync(JournalPath(target.Home), journal with { State = "committed" });
        File.Move(JournalPath(target.Home), Full(target.Home) + ".restore-completed-" + id + ".json");
        progress.Report("Restore completed. Original device retained at " + paths.Original);
    }
    public async Task RecoverAsync(string home, string profileId, Func<DeviceProfile, Task<bool>> commit)
    {
        CheckNoLinks(home); var journal = await ReadAsync<RestoreJournal>(JournalPath(home), CancellationToken.None);
        if (journal.SchemaVersion != 1 || journal.ProfileId != profileId || journal.Before is null || journal.Before.Id != profileId || journal.State is not ("prepared" or "committed"))
            throw new InvalidDataException("Invalid restore journal. Files retained for investigation.");
        var paths = RestorePaths(home, journal.Id); CheckNoLinks(paths.Stage); CheckNoLinks(paths.Original);
        if (journal.State == "committed" && (!Directory.Exists(home) || !Directory.Exists(paths.Original)))
            throw new IOException("Committed restore copies are missing; recovery refused.");
        if (journal.State == "prepared") {
            if (Directory.Exists(paths.Original)) {
                if (Directory.Exists(home)) {
                    if (Directory.Exists(paths.Stage)) throw new IOException("Ambiguous restore state; all copies retained for investigation.");
                    Directory.Move(home, paths.Stage);
                }
                Directory.Move(paths.Original, home);
            } else if (!Directory.Exists(home)) throw new IOException("Original device directory is missing. Recovery refused.");
            if (!await commit(journal.Before)) throw new IOException("Recovery metadata could not be saved. Device startup remains blocked.");
        }
        File.Move(JournalPath(home), Full(home) + ".restore-recovered-" + journal.Id + ".json");
    }
}
