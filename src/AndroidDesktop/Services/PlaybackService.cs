using AndroidDesktop.Models;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace AndroidDesktop.Services;

/// <summary>Streams bounded input batches to the viewport scheduler and archives actual dispatch receipts.</summary>
public sealed class PlaybackService(string root)
{
    private FileStream? _source;
    private IAsyncEnumerator<(string Text, bool Terminated)>? _reader;
    private InputArchive? _audit;
    private readonly SemaphoreSlim _requests = new(1, 1);
    private long _cursor, _receipts;
    private string? _directory;
    private RecordingManifest? _recording;
    private object? _parameters;
    public string? RunId { get; private set; }
    public bool Active => _source is not null;
    public string? ResultPath { get; private set; }
    public event Action<string>? Failed;
    public static void CheckContext(RecordingContext expected, RecordingContext actual)
    {
        if (expected != actual) throw new InvalidDataException("Recording package/version/checksum, device, protocol/configuration or geometry differs. Restore the matching session before replay; no coordinates are guessed.");
    }
    public static async Task<RecordingManifest> VerifyAsync(string path, CancellationToken token)
    {
        var (manifest, events) = await RecordingService.ReadManifestAsync(path, false, token);
        await using var stream = new FileStream(events, FileMode.Open, FileAccess.Read, FileShare.Read);
        await VerifyEventsAsync(manifest, stream, token);
        return manifest;
    }
    private static async Task VerifyEventsAsync(RecordingManifest manifest, FileStream stream, CancellationToken token)
    {
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream, token));
        if (!hash.Equals(manifest.EventsSha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Recording event checksum changed.");
        stream.Position = 0; var validator = new InputSequenceValidator(manifest.Context.Geometry);
        await foreach (var line in InputArchive.LinesAsync(stream, token)) {
            if (!line.Terminated) throw new InvalidDataException("Recording has an unfinished final row.");
            validator.Add(JsonSerializer.Deserialize<DeviceInputEvent>(line.Text, RecordingJson.Options) ?? throw new InvalidDataException("Invalid event."));
        }
        if (!validator.Released || validator.Count != manifest.EventCount || validator.TimestampMs > manifest.DurationMs || validator.Count == 0)
            throw new InvalidDataException("Recording contact sequence/count/duration is incomplete.");
        stream.Position = 0;
    }
    public async Task<RecordingManifest> BeginAsync(string path, RecordingContext actual, PlaybackOptions options, CancellationToken token)
    {
        if (Active) throw new InvalidOperationException("Another playback is active.");
        options.Validate();
        var (manifest, events) = await RecordingService.ReadManifestAsync(path, false, token); CheckContext(manifest.Context, actual);
        var source = new FileStream(events, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.Asynchronous);
        try { await VerifyEventsAsync(manifest, source, token); }
        catch { await source.DisposeAsync(); throw; }
        var id = Guid.NewGuid().ToString("N"); var directory = Path.Combine(root, "run-" + id);
        var parameters = new { schemaVersion = 1, id, recordingId = manifest.Id, recordingSha256 = manifest.EventsSha256, createdUtc = DateTimeOffset.UtcNow,
            context = actual, options, variationAlgorithm = "mulberry32-v1; per-contact translation + sinusoidal path + monotonic bounded timestamp jitter", status = "running", hardwareReceptionVerified = false };
        try { Directory.CreateDirectory(directory); await InputArchive.WriteManifestAsync(Path.Combine(directory, "run.json"), parameters); }
        catch { await source.DisposeAsync(); throw; }
        _source = source; _recording = manifest; _parameters = parameters; _directory = directory; RunId = id; _cursor = 0; _receipts = 0;
        _reader = InputArchive.LinesAsync(_source).GetAsyncEnumerator();
        _audit = new InputArchive(Path.Combine(directory, "dispatch.jsonl"), batch => {
            var rows = batch.EnumerateArray().Select(e => (object)e.Clone()).ToArray(); Interlocked.Add(ref _receipts, rows.Length); return rows;
        });
        _audit.Failed += reason => Failed?.Invoke(reason);
        try { await _audit.Ready; }
        catch { await FinishAsync("faulted", 0, 0, "Dispatch archive could not be created"); throw; }
        return manifest;
    }
    public async Task<object> NextBatchAsync(string id, long start, int loop, string requestId)
    {
        await _requests.WaitAsync();
        try {
            if (!Active || id != RunId || loop < 1) throw new InvalidOperationException("Playback session is no longer active.");
            if (start == 0 && _cursor != 0) { await _reader!.DisposeAsync(); _source!.Position = 0; _reader = InputArchive.LinesAsync(_source).GetAsyncEnumerator(); _cursor = 0; }
            if (start != _cursor) throw new InvalidDataException("Out-of-order playback batch request.");
            var events = new List<DeviceInputEvent>(128);
            while (events.Count < 128 && _cursor < _recording!.EventCount) {
                if (!await _reader!.MoveNextAsync()) throw new InvalidDataException("Recording ended unexpectedly.");
                events.Add(JsonSerializer.Deserialize<DeviceInputEvent>(_reader.Current.Text, RecordingJson.Options)!); _cursor++;
            }
            return new { runId = id, requestId, events, done = _cursor == _recording!.EventCount };
        } finally { _requests.Release(); }
    }
    public bool AppendReceipts(string id, JsonElement events) => id == RunId && _audit?.TryAppend(events) == true;
    public async Task FinishAsync(string status, long expectedReceipts, int completedLoops, string? error = null)
    {
        await _requests.WaitAsync();
        try {
            if (!Active) return;
            if (error is not null) _audit!.Fail(error);
            await _audit!.FinishAsync();
            var failure = _audit.Failure;
            if (_receipts != expectedReceipts && error is null) failure ??= "Dispatch receipt count mismatch; run evidence incomplete.";
            ResultPath = Path.Combine(_directory!, "result.json");
            await InputArchive.WriteManifestAsync(ResultPath, new { schemaVersion = 1, RunId, parameters = _parameters, status,
                completedLoops, dispatchedEvents = _receipts, expectedReceipts, failure, completeEvidence = failure is null, hardwareReceptionVerified = false });
        } finally {
            if (_reader is not null) await _reader.DisposeAsync(); _reader = null;
            if (_source is not null) await _source.DisposeAsync(); _source = null; _audit = null;
            _requests.Release();
        }
    }
}
