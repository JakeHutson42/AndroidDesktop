using AndroidDesktop.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Channels;

namespace AndroidDesktop.Services;

/// <summary>Bounded Phase 0 measurements, not a product recording service.</summary>
public sealed class EvidenceService
{
    private Channel<string>? _queue;
    private Task? _writer, _sampler;
    private Task? _failureMarker;
    private CancellationTokenSource? _sampling;
    private int[] _roots = [];
    private int _failed;
    public string? DirectoryPath { get; private set; }
    public event Action<string>? Failed;
    public void Invalidate(string reason) => Fail(reason);
    public void SetOwnedRoots(params int[] ids) => _roots = ids;

    public void Start(PrototypeOptions options)
    {
        if (_writer is not null) return;
        DirectoryPath = Path.Combine(options.EvidenceRoot, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(DirectoryPath);
        _failed = 0;
        _failureMarker = null;
        _queue = Channel.CreateBounded<string>(new BoundedChannelOptions(4096) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });
        var path = Path.Combine(DirectoryPath, "measurements.jsonl");
        _writer = Task.Run(async () => {
            try {
                await using var stream = new StreamWriter(path, append: false);
                await foreach (var line in _queue.Reader.ReadAllAsync()) await stream.WriteLineAsync(line);
                await stream.FlushAsync();
            } catch (Exception e) { Fail("Evidence write failed: " + e.Message); }
        });
        SetOwnedRoots(Environment.ProcessId);
        TryWrite("reference", new { Environment.OSVersion, runtime = Environment.Version.ToString(),
            Environment.ProcessorCount, architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            Stopwatch.Frequency, configuration = options.ViewportMode, options.ShowStandaloneWindow,
            notes = "CPU/GPU model, GPU utilization and visible-response latency require the runbook’s hardware measurements" });
        _sampling = new CancellationTokenSource();
        _sampler = Task.Run(() => SampleAsync(_sampling.Token));
    }

    public bool TryWrite(string kind, object? data)
    {
        if (_queue is null || Volatile.Read(ref _failed) != 0) return false;
        var safe = SupportExportService.SanitizeJson(JsonSerializer.SerializeToElement(data));
        var line = JsonSerializer.Serialize(new { utc = DateTime.UtcNow, hostTicks = Stopwatch.GetTimestamp(), kind, data = safe });
        if (_queue.Writer.TryWrite(line)) return true;
        Fail("Evidence queue overflow. Measurement capture stopped; this run is incomplete. Gameplay input remains independent.");
        return false;
    }
    private void Fail(string reason)
    {
        reason = SupportExportService.Redact(reason);
        if (Interlocked.Exchange(ref _failed, 1) != 0) return;
        _queue?.Writer.TryComplete();
        Failed?.Invoke(reason);
        if (DirectoryPath is not null) {
            _failureMarker = Task.Run(async () => { try { await File.WriteAllTextAsync(Path.Combine(DirectoryPath, "INCOMPLETE.txt"), reason); } catch { /* UI reports failure even if the disk is unavailable. */ } });
        }
    }
    private async Task SampleAsync(CancellationToken token)
    {
        var previous = new Dictionary<int, (TimeSpan Cpu, long Ticks)>();
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try {
            while (await timer.WaitForNextTickAsync(token)) {
                var rows = new List<object>();
                var ids = ProcessTree.Descendants(_roots);
                foreach (var id in ids) {
                    try {
                        using var p = Process.GetProcessById(id); p.Refresh();
                        var cpu = p.TotalProcessorTime; var now = Stopwatch.GetTimestamp();
                        var percent = previous.TryGetValue(id, out var last)
                            ? (cpu-last.Cpu).TotalSeconds / Stopwatch.GetElapsedTime(last.Ticks, now).TotalSeconds * 100 / Environment.ProcessorCount
                            : (double?)null;
                        previous[id] = (cpu, now);
                        rows.Add(new { id, name = p.ProcessName, p.WorkingSet64, p.PrivateMemorySize64, cpuPercentOfMachine = percent });
                    } catch (Exception e) when (e is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception) { }
                }
                foreach (var id in previous.Keys.Except(ids).ToArray()) previous.Remove(id);
                TryWrite("processes", rows);
            }
        } catch (OperationCanceledException) { }
        catch (Exception e) { Fail("Process measurement failed: " + e.Message); }
    }
    public async Task StopAsync()
    {
        _sampling?.Cancel();
        if (_sampler is not null) await _sampler;
        _queue?.Writer.TryComplete();
        if (_writer is not null) await _writer;
        if (_failureMarker is not null) await _failureMarker;
        _writer = null; _sampler = null; _queue = null;
        _sampling?.Dispose(); _sampling = null;
    }
}

internal static class ProcessTree
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Entry
    {
        public uint Size, Usage, ProcessId; public UIntPtr DefaultHeap; public uint ModuleId, Threads, ParentId;
        public int Priority; public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string Executable;
    }
    [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint pid);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern bool Process32FirstW(IntPtr snapshot, ref Entry entry);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern bool Process32NextW(IntPtr snapshot, ref Entry entry);
    [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr handle);
    public static HashSet<int> Descendants(int[] roots)
    {
        var ids = roots.ToHashSet(); var parents = new Dictionary<int,int>();
        var snapshot = CreateToolhelp32Snapshot(2, 0);
        if (snapshot == new IntPtr(-1)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        try {
            var entry = new Entry { Size = (uint)Marshal.SizeOf<Entry>(), Executable = "" };
            if (Process32FirstW(snapshot, ref entry)) do { parents[(int)entry.ProcessId] = (int)entry.ParentId; } while (Process32NextW(snapshot, ref entry));
        } finally { CloseHandle(snapshot); }
        bool changed;
        do { changed = false; foreach (var pair in parents) if (ids.Contains(pair.Value) && ids.Add(pair.Key)) changed = true; } while (changed);
        return ids;
    }
}
