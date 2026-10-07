using AndroidDesktop.Models;
using AndroidDesktop.Services;
using System.IO;
using System.Text.Json;

namespace AndroidDesktop.Tests;

public sealed class RecordingTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "automation-tests-" + Guid.NewGuid());
    private static readonly InputGeometry Geometry = new(720, 1280, 0);
    private static readonly RecordingContext Context = new("example.game", 1, "apk-hash", "device", "owned", "standard", Geometry);
    public RecordingTests() => Directory.CreateDirectory(_root);
    public void Dispose() { Directory.Delete(_root, true); }
    private static DeviceInputEvent Touch(long sequence, double ms, string phase, int slot = 0) => new() {
        Sequence = sequence, TimestampMs = ms, DispatchedMs = ms, Phase = phase, PointerId = slot,
        X = 72, Y = 128, NormalizedX = .1, NormalizedY = .1, Width = 720, Height = 1280, Pressure = .5 };
    private static JsonElement Batch(params DeviceInputEvent[] events) => JsonSerializer.SerializeToElement(events, RecordingJson.Options);
    private async Task<RecordingService> CompleteRecordingAsync()
    {
        var recording = new RecordingService(_root); await recording.StartAsync(Context);
        Assert.True(recording.Append(recording.Manifest!.Id, Batch(Touch(1, 0, "down"), Touch(2, 10, "move"), Touch(3, 20, "up"))));
        Assert.True((await recording.FinishAsync(30, 3)).Complete); return recording;
    }
    [Fact] public async Task RecordingFinalizesHashAndExactSequenceAndPlaybackStreamsWithoutChangingSource()
    {
        var recording = await CompleteRecordingAsync(); var manifest = await PlaybackService.VerifyAsync(recording.ManifestPath!, CancellationToken.None);
        Assert.Equal(3, manifest.EventCount); Assert.Equal(Context, manifest.Context);
        var original = await File.ReadAllTextAsync(Path.Combine(Path.GetDirectoryName(recording.ManifestPath!)!, "events.jsonl"));
        var playback = new PlaybackService(Path.Combine(_root, "runs"));
        await playback.BeginAsync(recording.ManifestPath!, Context, new(Loops: 2), CancellationToken.None);
        var first = JsonSerializer.SerializeToElement(await playback.NextBatchAsync(playback.RunId!, 0, 1, "request"));
        Assert.True(first.GetProperty("done").GetBoolean()); Assert.Equal(3, first.GetProperty("events").GetArrayLength());
        var nextLoop = JsonSerializer.SerializeToElement(await playback.NextBatchAsync(playback.RunId!, 0, 2, "request2"));
        Assert.Equal(first.GetProperty("events").ToString(), nextLoop.GetProperty("events").ToString());
        Assert.True(playback.AppendReceipts(playback.RunId!, JsonSerializer.SerializeToElement(new[] { new { dispatchedMs = 0, lateMs = 0 } })));
        await playback.FinishAsync("stopped", 1, 1);
        Assert.False(playback.Active); Assert.True(File.Exists(playback.ResultPath));
        Assert.Equal(original, await File.ReadAllTextAsync(Path.Combine(Path.GetDirectoryName(recording.ManifestPath!)!, "events.jsonl")));
    }
    [Fact] public async Task ChangedArchiveCannotReplay()
    {
        var recording = await CompleteRecordingAsync();
        await File.AppendAllTextAsync(Path.Combine(Path.GetDirectoryName(recording.ManifestPath!)!, "events.jsonl"), "changed\n");
        await Assert.ThrowsAsync<InvalidDataException>(() => PlaybackService.VerifyAsync(recording.ManifestPath!, CancellationToken.None));
    }
    [Fact] public async Task IncompleteRecordingRecoveryPreservesOriginalAndAddsReleasesForTouchAndKey()
    {
        var recording = new RecordingService(_root); await recording.StartAsync(Context);
        var key = Touch(2, 5, "down") with { Kind = "key", Key = "a" };
        recording.Append(recording.Manifest!.Id, Batch(Touch(1, 0, "down"), key));
        var interrupted = await recording.FinishAsync(6, 2, "process interruption"); Assert.False(interrupted.Complete);
        var originalPath = recording.ManifestPath!; var eventPath = Path.Combine(Path.GetDirectoryName(originalPath)!, "events.jsonl");
        await File.AppendAllTextAsync(eventPath, "{partial"); var original = await File.ReadAllTextAsync(eventPath);
        await Assert.ThrowsAsync<InvalidDataException>(() => PlaybackService.VerifyAsync(originalPath, CancellationToken.None));
        var recovery = new RecordingService(Path.Combine(_root, "recovered")); var manifest = await recovery.RecoverAsync(originalPath, CancellationToken.None);
        Assert.True(manifest.Complete); Assert.Equal(interrupted.Id, manifest.RecoveredFrom); Assert.Equal(4, manifest.EventCount);
        await PlaybackService.VerifyAsync(recovery.ManifestPath!, CancellationToken.None);
        Assert.Equal(original, await File.ReadAllTextAsync(eventPath));
    }
    [Fact] public async Task CountMismatchAndHeldPointersNeverProduceCompleteRecording()
    {
        var recording = new RecordingService(_root); await recording.StartAsync(Context);
        recording.Append(recording.Manifest!.Id, Batch(Touch(1, 0, "down")));
        Assert.False((await recording.FinishAsync(1, 2)).Complete);
    }
    [Fact] public async Task OutOfOrderOrMissingReleasesInvalidateArchive()
    {
        var recording = new RecordingService(_root); await recording.StartAsync(Context);
        recording.Append(recording.Manifest!.Id, Batch(Touch(1, 0, "up")));
        Assert.False((await recording.FinishAsync(1, 1)).Complete);
    }
    [Fact] public async Task RecoveryRejectsCorruptionInTheMiddle()
    {
        var recording = new RecordingService(_root); await recording.StartAsync(Context);
        await recording.FinishAsync(0, 0, "interrupted");
        await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(recording.ManifestPath!)!, "events.jsonl"), "{invalid}\n{partial");
        var recovery = new RecordingService(Path.Combine(_root, "recovered"));
        await Assert.ThrowsAsync<JsonException>(() => recovery.RecoverAsync(recording.ManifestPath!, CancellationToken.None));
        Assert.False(recovery.Manifest!.Complete); Assert.False(recovery.Active);
    }
    [Theory]
    [InlineData("device")][InlineData("package")][InlineData("geometry")][InlineData("runtime")]
    public void PlaybackRejectsContextDifferences(string changed)
    {
        var actual = changed switch { "device" => Context with { DeviceId = "another" }, "package" => Context with { PackageId = "different.game" },
            "geometry" => Context with { Geometry = new(1280,720,90) }, _ => Context with { RuntimeIdentity = "different-image" } };
        Assert.Throws<InvalidDataException>(() => PlaybackService.CheckContext(Context, actual));
    }
    [Theory]
    [InlineData(0,0,0,0)][InlineData(1,33,0,0)][InlineData(1,0,17,0)][InlineData(1,0,0,101)]
    public void InvalidLoopAndVariationBoundsAreRejected(int loops, int coordinates, int path, int timing) =>
        Assert.Throws<InvalidDataException>(() => new PlaybackOptions(Loops: loops, CoordinatePixels: coordinates, PathPixels: path, TimingMs: timing).Validate());
    [Fact] public void GeometryCannotChangeWhileContactsAreHeld()
    {
        var validator = new InputSequenceValidator(Geometry); validator.Add(Touch(1,0,"down"));
        Assert.Throws<InvalidDataException>(() => validator.Add(Touch(2,1,"geometry") with { Kind="geometry", Width=1280, Height=720, Orientation=90 }));
    }
    [Fact] public async Task BoundedArchiveOverflowIsExplicitAndWriterFailureIsObservable()
    {
        using var entered = new ManualResetEventSlim(); using var release = new ManualResetEventSlim();
        var archive = new InputArchive(Path.Combine(_root,"bounded.jsonl"), batch => { entered.Set(); release.Wait(); return new object[] { new { value=1 } }; }, capacity:1);
        await archive.Ready;
        try {
            Assert.True(archive.TryAppend(JsonSerializer.SerializeToElement(new[] {1}))); Assert.True(entered.Wait(5000));
            Assert.True(archive.TryAppend(JsonSerializer.SerializeToElement(new[] {2}))); Assert.False(archive.TryAppend(JsonSerializer.SerializeToElement(new[] {3})));
            Assert.Contains("overflow", archive.Failure!);
        } finally { release.Set(); await archive.FinishAsync(); }
        var broken = new InputArchive(_root, _ => []);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await broken.Ready); await broken.FinishAsync(); Assert.NotNull(broken.Failure);
    }
    [Fact] public async Task OversizedArchiveRowsAreRejectedWithoutUnboundedReadBuffer()
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(new string('x',9000)+"\n"));
        await Assert.ThrowsAsync<InvalidDataException>(async () => { await foreach (var line in InputArchive.LinesAsync(stream)) { } });
    }
}
