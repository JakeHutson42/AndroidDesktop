using AndroidDesktop.Models;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace AndroidDesktop.Services;

public sealed class RecordingService(string root)
{
    private InputArchive? _archive;
    private InputSequenceValidator? _validator;
    public RecordingManifest? Manifest { get; private set; }
    public string? ManifestPath { get; private set; }
    public bool Active => _archive is not null;
    public event Action<string>? Failed;
    public async Task StartAsync(RecordingContext context, string? recoveredFrom = null)
    {
        if (Active) throw new InvalidOperationException("Another recording is active.");
        context.Geometry.Validate();
        var manifest = new RecordingManifest { Context = context, RecoveredFrom = recoveredFrom };
        var directory = Path.Combine(root, "recording-" + manifest.Id); Directory.CreateDirectory(directory);
        ManifestPath = Path.Combine(directory, "manifest.json"); Manifest = manifest;
        await InputArchive.WriteManifestAsync(ManifestPath, manifest);
        _validator = new(context.Geometry);
        _archive = new InputArchive(Path.Combine(directory, "events.jsonl"), batch => {
            var events = batch.Deserialize<DeviceInputEvent[]>(RecordingJson.Options) ?? throw new InvalidDataException("Empty input batch.");
            foreach (var e in events) _validator.Add(e);
            return events.Cast<object>();
        });
        _archive.Failed += reason => Failed?.Invoke(reason);
        try { await _archive.Ready; }
        catch { await _archive.FinishAsync(); _archive = null; throw; }
    }
    public bool Append(string id, JsonElement batch) => Manifest?.Id == id && _archive?.TryAppend(batch) == true;
    public void Invalidate(string reason) => _archive?.Fail(reason);
    public async Task<RecordingManifest> FinishAsync(double durationMs, long expectedCount, string? interruption = null)
    {
        var archive = _archive ?? throw new InvalidOperationException("No recording is active.");
        if (interruption is not null) archive.Fail(interruption);
        await archive.FinishAsync();
        _archive = null;
        var failure = archive.Failure;
        if (failure is null && (_validator!.Count == 0 || _validator.Count != expectedCount || !_validator.Released || !double.IsFinite(durationMs) || durationMs < _validator.TimestampMs || durationMs > 86400000)) failure = "Recording counts, duration or held input did not finalize safely.";
        var path = Path.Combine(Path.GetDirectoryName(ManifestPath!)!, "events.jsonl");
        string hash = "";
        if (File.Exists(path)) { await using var stream = File.OpenRead(path); hash = Convert.ToHexString(await SHA256.HashDataAsync(stream)); }
        var final = Manifest! with { Complete = failure is null, Failure = failure, EventCount = _validator!.Count,
            DurationMs = double.IsFinite(durationMs) ? Math.Max(durationMs, _validator.TimestampMs) : _validator.TimestampMs, EventsSha256 = hash };
        try { await InputArchive.WriteManifestAsync(ManifestPath!, final); Manifest = final; return final; }
        finally { _archive = null; }
    }
    public static async Task<(RecordingManifest Manifest, string Events)> ReadManifestAsync(string path, bool allowIncomplete, CancellationToken token)
    {
        if (new FileInfo(path).Length > 65536) throw new InvalidDataException("Recording manifest exceeds 64 KiB.");
        var manifest = JsonSerializer.Deserialize<RecordingManifest>(await File.ReadAllTextAsync(path, token), RecordingJson.Options) ?? throw new InvalidDataException("Empty recording manifest.");
        if (manifest.SchemaVersion != 1 || manifest.Context is null || !Guid.TryParse(manifest.Id, out _) || manifest.EventCount < 0 || !double.IsFinite(manifest.DurationMs) || manifest.DurationMs < 0)
            throw new InvalidDataException("Unsupported or invalid recording manifest.");
        manifest.Context.Geometry.Validate();
        if (!allowIncomplete && !manifest.Complete) throw new InvalidDataException("Recording is incomplete. Use Recover interrupted to create a separate recovered copy.");
        return (manifest, Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!, "events.jsonl"));
    }
    public async Task<RecordingManifest> RecoverAsync(string path, CancellationToken token)
    {
        var (source, events) = await ReadManifestAsync(path, true, token);
        if (source.Complete) throw new InvalidOperationException("This recording is already complete.");
        await StartAsync(source.Context, source.Id);
        var validator = new InputSequenceValidator(source.Context.Geometry); var batch = new List<DeviceInputEvent>();
        try {
            await using var stream = File.OpenRead(events);
            await foreach (var line in InputArchive.LinesAsync(stream, token)) {
                // Only an unterminated final row is discarded; middle corruption is an error.
                if (!line.Terminated) break;
                var e = JsonSerializer.Deserialize<DeviceInputEvent>(line.Text, RecordingJson.Options) ?? throw new InvalidDataException("Invalid recovery row.");
                validator.Add(e); batch.Add(e);
                if (batch.Count == 128) { await AppendRecoveryAsync(batch, token); batch.Clear(); }
            }
            foreach (var held in validator.Pointers.Values.Concat(validator.Keys.Values).ToArray()) {
                var release = held with { Sequence = validator.Sequence + 1, TimestampMs = validator.TimestampMs, Phase = "cancel", Source = "recovery", Pressure = 0 };
                validator.Add(release); batch.Add(release);
            }
            if (batch.Count > 0) await AppendRecoveryAsync(batch, token);
            return await FinishAsync(validator.TimestampMs, validator.Count);
        } catch {
            if (Active) await FinishAsync(validator.TimestampMs, validator.Count, "Recovery interrupted or invalid; original recording retained."); throw;
        }
    }
    private async Task AppendRecoveryAsync(List<DeviceInputEvent> batch, CancellationToken token)
    {
        // Offline recovery may wait for archival capacity; live capture never waits.
        var data = JsonSerializer.SerializeToElement(batch, RecordingJson.Options);
        await _archive!.AppendOfflineAsync(data, token);
    }
}
