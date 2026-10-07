using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using AndroidDesktop.Models;

namespace AndroidDesktop.Services;

/// <summary>Bounded batched writer shared by recordings and dispatch audit. Never waits on the input path.</summary>
public sealed class InputArchive
{
    private readonly Channel<JsonElement> _queue;
    private readonly Task _writer;
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Task Ready => _ready.Task;
    private string? _failure;
    public string? Failure => Volatile.Read(ref _failure);
    public event Action<string>? Failed;
    public InputArchive(string path, Func<JsonElement, IEnumerable<object>> expand, int capacity = 32)
    {
        _queue = Channel.CreateBounded<JsonElement>(new BoundedChannelOptions(capacity) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });
        _writer = Task.Run(async () => {
            try {
                await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 65536, FileOptions.Asynchronous);
                _ready.TrySetResult();
                await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
                await foreach (var batch in _queue.Reader.ReadAllAsync()) {
                    foreach (var row in expand(batch)) await writer.WriteLineAsync(JsonSerializer.Serialize(row, RecordingJson.Options));
                    await writer.FlushAsync();
                }
                await writer.FlushAsync(); stream.Flush(flushToDisk: true);
            } catch (Exception e) { _ready.TrySetException(e); Fail("Input archive failed: " + e.Message); }
        });
    }
    public bool TryAppend(JsonElement batch)
    {
        if (Failure is not null) return false;
        if (batch.ValueKind != JsonValueKind.Array || batch.GetArrayLength() > 256) { Fail("Input archive batch is invalid or too large."); return false; }
        if (_queue.Writer.TryWrite(batch.Clone())) return true;
        Fail("Input archive queue overflow. Capture stopped; incomplete data is not replayable. Live input stays independent."); return false;
    }
    public async Task AppendOfflineAsync(JsonElement batch, CancellationToken token)
    {
        if (Failure is not null) throw new IOException(Failure);
        await _queue.Writer.WriteAsync(batch.Clone(), token);
    }
    public void Fail(string reason) {
        if (Interlocked.CompareExchange(ref _failure, reason, null) is not null) return;
        _queue.Writer.TryComplete(); Failed?.Invoke(reason);
    }
    public async Task FinishAsync() { _queue.Writer.TryComplete(); await _writer; }

    public static async Task WriteManifestAsync<T>(string path, T manifest)
    {
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(manifest, RecordingJson.Options)); File.Move(temporary, path, overwrite: true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    public static async IAsyncEnumerable<(string Text, bool Terminated)> LinesAsync(Stream stream, [EnumeratorCancellation] CancellationToken token = default)
    {
        using var reader = new StreamReader(stream, new UTF8Encoding(false, true), leaveOpen: true);
        var buffer = new char[8192]; var line = new StringBuilder(); int read;
        while ((read = await reader.ReadAsync(buffer.AsMemory(), token)) > 0) {
            var start = 0;
            while (start < read) {
                var end = Array.IndexOf(buffer, '\n', start, read - start);
                var length = (end < 0 ? read : end) - start;
                if (line.Length + length > 8192) throw new InvalidDataException("Input archive row exceeds 8 KiB.");
                line.Append(buffer, start, length);
                if (end < 0) break;
                yield return (line.ToString().TrimEnd('\r'), true); line.Clear(); start = end + 1;
            }
        }
        if (line.Length > 0) yield return (line.ToString(), false);
    }
}
