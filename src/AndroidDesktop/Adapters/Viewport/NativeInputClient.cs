using Android.Emulation.Control;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using System.Diagnostics;

namespace AndroidDesktop.Adapters.Viewport;

/// <summary>Input only. Native Qt remains the GPU presentation surface.</summary>
public sealed class NativeInputClient : IAsyncDisposable
{
    private readonly GrpcChannel? _channel;
    private readonly EmulatorController.EmulatorControllerClient _client;
    private readonly Metadata _headers;
    private readonly LinkedList<Pending> _queue = new();
    private readonly HashSet<int> _keys = [];
    private readonly SemaphoreSlim _signal = new(0);
    private readonly Task _worker;
    private bool _closing, _failed, _touch;
    private int _x, _y;
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int Sent { get; private set; }
    public int PeakQueue { get; private set; }
    public double MaxQueueDelayMs { get; private set; }
    public event Action<string>? Fault;
    private sealed record Pending(object Value, long Time, bool Move);
    private NativeInputClient(int port, string token)
    {
        _channel = GrpcChannel.ForAddress($"http://127.0.0.1:{port}");
        _client = new(_channel);
        _headers = new Metadata { { "authorization", "Bearer " + token } };
        _worker = Task.Run(PumpAsync);
    }
    internal NativeInputClient(EmulatorController.EmulatorControllerClient client, int width, int height)
    {
        _client = client; _headers = new Metadata(); Width = width; Height = height; _worker = Task.Run(PumpAsync);
    }
    public static async Task<NativeInputClient> ConnectAsync(int port, string token, CancellationToken ct)
    {
        var input = new NativeInputClient(port, token);
        try {
            var config = await input._client.getDisplayConfigurationsAsync(new Empty(), input._headers, DateTime.UtcNow.AddSeconds(5), ct);
            var display = config.Displays.FirstOrDefault(d => d.Display == 0) ?? throw new InvalidOperationException("Primary emulator display is unavailable.");
            input.Width = checked((int)display.Width); input.Height = checked((int)display.Height);
            if (input.Width <= 0 || input.Height <= 0) throw new InvalidOperationException("Invalid emulator display size.");
            return input;
        } catch { await input.DisposeAsync(); throw; }
    }
    public void Key(int key, bool down)
    {
        lock (_queue) {
            if (_closing || _failed) return;
            if (down) _keys.Add(key); else _keys.Remove(key);
            Enqueue(new KeyboardEvent { CodeType = KeyboardEvent.Types.KeyCodeType.Win, KeyCode = key,
                EventType = down ? KeyboardEvent.Types.KeyEventType.Keydown : KeyboardEvent.Types.KeyEventType.Keyup });
        }
    }
    public void Pointer(double x, double y, int rotation, bool down, bool move = false)
    {
        lock (_queue) {
            if (_closing || _failed) return;
            var mapped = Map(x, y, Width, Height, rotation);
            _x = mapped.X; _y = mapped.Y; _touch = down;
            Enqueue(TouchMessage(down), move: move);
        }
    }
    public static (int X, int Y) Map(double x, double y, int width, int height, int rotation)
    {
        // Android's controller accepts physical panel coordinates, before rotation.
        (x, y) = ((rotation % 360 + 360) % 360) switch {
            90 => (1 - y, x), 180 => (1 - x, 1 - y), 270 => (y, 1 - x), _ => (x, y)
        };
        return ((int)Math.Round(Math.Clamp(x, 0, 1) * (width - 1)), (int)Math.Round(Math.Clamp(y, 0, 1) * (height - 1)));
    }
    private TouchEvent TouchMessage(bool down) => new() { Touches = { new Touch { X = _x, Y = _y, Identifier = 0, Pressure = down ? 1 : 0 } } };
    public void ReleaseAll()
    {
        lock (_queue) { if (!_closing && !_failed) ReleaseLocked(); }
    }
    private void ReleaseLocked()
    {
        foreach (var key in _keys) Enqueue(new KeyboardEvent { CodeType = KeyboardEvent.Types.KeyCodeType.Win, KeyCode = key, EventType = KeyboardEvent.Types.KeyEventType.Keyup }, true);
        _keys.Clear();
        if (_touch) { Enqueue(TouchMessage(false), true); _touch = false; }
    }
    private void Enqueue(object value, bool release = false, bool move = false)
    {
        if (move && _queue.Last is { Value.Move: true } last) {
            last.Value = new Pending(value, Stopwatch.GetTimestamp(), true); return;
        }
        if (!release && _queue.Count >= 128) {
            // Preserve queued edges and append releases; stop accepting input until reconnect.
            _failed = true; ReleaseLocked();
            Fault?.Invoke("Native input queue stalled. Reconnect the display; controller display remains available.");
            return;
        }
        _queue.AddLast(new Pending(value, Stopwatch.GetTimestamp(), move));
        PeakQueue = Math.Max(PeakQueue, _queue.Count); _signal.Release();
    }
    private async Task PumpAsync()
    {
        while (true) {
            await _signal.WaitAsync();
            Pending? pending;
            lock (_queue) { pending = _queue.First?.Value; if (pending is not null) _queue.RemoveFirst(); if (pending is null && _closing) return; }
            if (pending is null) continue;
            MaxQueueDelayMs = Math.Max(MaxQueueDelayMs, Stopwatch.GetElapsedTime(pending.Time).TotalMilliseconds);
            try {
                if (pending.Value is KeyboardEvent key) await _client.sendKeyAsync(key, _headers, DateTime.UtcNow.AddSeconds(2));
                else await _client.sendTouchAsync((TouchEvent)pending.Value, _headers, DateTime.UtcNow.AddSeconds(2));
                Sent++;
            } catch (Exception error) when (error is RpcException or ObjectDisposedException) {
                var notify = false;
                lock (_queue) { if (!_failed) { _failed = true; _queue.Clear(); ReleaseLocked(); notify = true; } }
                if (notify) Fault?.Invoke("Native input connection failed. Reconnect the display or select controller display.");
            }
        }
    }
    public async ValueTask DisposeAsync()
    {
        lock (_queue) { if (_closing) return; ReleaseLocked(); _closing = true; _signal.Release(); }
        try { await _worker.WaitAsync(TimeSpan.FromSeconds(5)); }
        catch (TimeoutException) { _channel?.Dispose(); await _worker; }
        finally { _channel?.Dispose(); _signal.Dispose(); }
    }
}
