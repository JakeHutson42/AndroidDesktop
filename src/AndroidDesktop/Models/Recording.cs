using System.IO;
using System.Text.Json;

namespace AndroidDesktop.Models;

public sealed record InputGeometry(int Width, int Height, int Orientation)
{
    public void Validate() { if (Width is < 1 or > 16384 || Height is < 1 or > 16384 || Orientation is not (0 or 90 or 180 or 270)) throw new InvalidDataException("Invalid recording geometry."); }
}
public sealed record DeviceInputEvent
{
    public long Sequence { get; init; }
    public double TimestampMs { get; init; }
    public string Kind { get; init; } = "touch";
    public string Phase { get; init; } = "";
    public int PointerId { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public double NormalizedX { get; init; }
    public double NormalizedY { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public int Orientation { get; init; }
    public double Pressure { get; init; }
    public string Key { get; init; } = "";
    public int KeyCode { get; init; }
    public int CodeType { get; init; }
    public string Source { get; init; } = "live";
    public double DispatchedMs { get; init; }
    [System.Text.Json.Serialization.JsonIgnore] public InputGeometry Geometry => new(Width, Height, Orientation);
}
public sealed record RecordingContext(string PackageId, long VersionCode, string ApkSha256, string DeviceId,
    string AvdName, string ViewportMode, InputGeometry Geometry,
    string ProtocolCommit = "edd3526160771a72bb964317bf420be44208ab26", string RuntimeIdentity = "");
public sealed record RecordingManifest
{
    public int SchemaVersion { get; init; } = 1;
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
    public required RecordingContext Context { get; init; }
    public bool Complete { get; init; }
    public long EventCount { get; init; }
    public double DurationMs { get; init; }
    public string EventsSha256 { get; init; } = "";
    public string? Failure { get; init; }
    public string? RecoveredFrom { get; init; }
    public string Clock { get; init; } = "Browser performance.now relative to recording start; dispatch timestamps use the same clock";
    public string Capabilities { get; init; } = "10 touch slots; pressure 1..1024; up/cancel pressure 0 (not Android ACTION_CANCEL); string/evdev keyboard; actual decoded dimensions; orientation label requires hardware verification";
}
public sealed record PlaybackOptions(int Loops = 1, bool Infinite = false, int LoopDelayMs = 1000,
    bool Variations = false, uint Seed = 1, int CoordinatePixels = 0, int PathPixels = 0, int TimingMs = 0)
{
    public void Validate() {
        if (Loops is < 1 or > 10000 || LoopDelayMs is < 0 or > 60000 || CoordinatePixels is < 0 or > 32 || PathPixels is < 0 or > 16 || TimingMs is < 0 or > 100)
            throw new InvalidDataException("Loops must be 1–10000, delay 0–60000 ms; variation limits are coordinate 32 px, path 16 px and timing 100 ms.");
    }
}
public static class RecordingJson
{
    public static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
}

/// <summary>One streaming validator; no recording-sized cache. Rejects incomplete contact sequences.</summary>
public sealed class InputSequenceValidator(InputGeometry initial)
{
    public InputGeometry Geometry { get; private set; } = initial;
    public Dictionary<int, DeviceInputEvent> Pointers { get; } = [];
    public Dictionary<string, DeviceInputEvent> Keys { get; } = [];
    public long Count { get; private set; }
    public long Sequence { get; private set; }
    public double TimestampMs { get; private set; }
    public bool Released => Pointers.Count == 0 && Keys.Count == 0;
    public void Add(DeviceInputEvent e)
    {
        e.Geometry.Validate();
        if (e.Sequence != Sequence + 1 || !double.IsFinite(e.TimestampMs) || e.TimestampMs < TimestampMs || e.TimestampMs > 86400000 || !double.IsFinite(e.DispatchedMs) || e.DispatchedMs < 0)
            throw new InvalidDataException("Recording sequence/timestamps are invalid or incomplete.");
        if (e.Kind == "geometry") {
            if (!Released) throw new InvalidDataException("Geometry changed with held input.");
            Geometry = e.Geometry;
        } else {
            if (e.Geometry != Geometry) throw new InvalidDataException("Input does not match the recorded geometry.");
            if (e.Kind == "touch") {
                if (e.PointerId is < 0 or > 9 || e.X < 0 || e.X >= e.Width || e.Y < 0 || e.Y >= e.Height || !double.IsFinite(e.Pressure) || e.Pressure is < 0 or > 1 ||
                    !double.IsFinite(e.NormalizedX) || !double.IsFinite(e.NormalizedY) || e.NormalizedX is < 0 or > 1 || e.NormalizedY is < 0 or > 1 ||
                    Math.Abs(Math.Min(e.Width - 1, (int)(e.NormalizedX * e.Width)) - e.X) > 1 || Math.Abs(Math.Min(e.Height - 1, (int)(e.NormalizedY * e.Height)) - e.Y) > 1)
                    throw new InvalidDataException("Invalid touch coordinates/pressure/pointer slot.");
                if (e.Phase == "down") { if (!Pointers.TryAdd(e.PointerId, e)) throw new InvalidDataException("Duplicate touch down."); }
                else if (e.Phase == "move") { if (!Pointers.ContainsKey(e.PointerId)) throw new InvalidDataException("Move without down."); Pointers[e.PointerId] = e; }
                else if (e.Phase is "up" or "cancel") { if (!Pointers.Remove(e.PointerId)) throw new InvalidDataException("Release without down."); }
                else throw new InvalidDataException("Invalid touch phase.");
            } else if (e.Kind == "key") {
                if (e.Key.Length > 32 || (e.Key.Length == 0 && (e.KeyCode is < 1 or > 255 || e.CodeType != 1))) throw new InvalidDataException("Invalid keyboard event.");
                if (e.Key.Length == 0 && e.Phase != "press") throw new InvalidDataException("Evdev controls support atomic presses only in this adapter.");
                var identity = e.Key.Length > 0 ? e.Key : $"{e.CodeType}:{e.KeyCode}";
                if (e.Phase == "down") { if (!Keys.TryAdd(identity, e)) throw new InvalidDataException("Duplicate key down."); }
                else if (e.Phase is "up" or "cancel") { if (!Keys.Remove(identity)) throw new InvalidDataException("Key release without down."); }
                else if (e.Phase != "press") throw new InvalidDataException("Invalid key phase.");
            } else throw new InvalidDataException("Unknown input event kind.");
        }
        Sequence = e.Sequence; TimestampMs = e.TimestampMs; Count++;
    }
}
