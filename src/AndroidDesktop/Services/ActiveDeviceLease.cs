using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace AndroidDesktop.Services;

/// <summary>Exclusive per-user device lease, including crash-surviving child identities.</summary>
public sealed class ActiveDeviceLease : IDisposable
{
    public sealed record Owner(int Pid, long StartedUtcTicks);
    private readonly FileStream _file;
    private readonly List<Owner> _owners = [];
    private ActiveDeviceLease(FileStream file) { _file = file; }
    public static ActiveDeviceLease Acquire(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        FileStream file;
        try { file = new(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
        catch (IOException e) { throw new InvalidOperationException("Another Android Desktop session owns the active device. Stop it before starting a profile.", e); }
        try {
            if (file.Length > 65536) throw new InvalidDataException("Device ownership record is invalid. Inspect it before continuing; no device was stopped.");
            if (file.Length != 0) {
                var previous = JsonSerializer.Deserialize<Owner[]>(file) ?? throw new InvalidDataException("Device ownership record is empty.");
                foreach (var owner in previous) {
                    if (owner.Pid <= 0 || owner.StartedUtcTicks <= 0) throw new InvalidDataException("Invalid device ownership record.");
                    try {
                        using var process = Process.GetProcessById(owner.Pid);
                        if (!process.HasExited && process.StartTime.ToUniversalTime().Ticks == owner.StartedUtcTicks)
                            throw new InvalidOperationException("A previous Android Desktop host or owned child is still running. Close it gracefully before starting another profile; no process was terminated.");
                    } catch (ArgumentException) { /* The recorded PID no longer exists. */ }
                }
            }
            var lease = new ActiveDeviceLease(file);
            using var host = Process.GetCurrentProcess(); lease.Track(host); return lease;
        } catch { file.Dispose(); throw; }
    }
    public void Track(Process process)
    {
        var owner = new Owner(process.Id, process.StartTime.ToUniversalTime().Ticks);
        if (!_owners.Contains(owner)) _owners.Add(owner);
        _file.Position = 0; _file.SetLength(0); JsonSerializer.Serialize(_file, _owners); _file.Flush(true);
    }
    public void Dispose()
    {
        // Clear only after the session has confirmed its children stopped.
        try { _file.SetLength(0); _file.Flush(true); } finally { _file.Dispose(); }
    }
}
