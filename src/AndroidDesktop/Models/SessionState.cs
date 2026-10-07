namespace AndroidDesktop.Models;
public enum SessionState { NotConfigured, Stopped, Starting, Ready, Installing, Running, Stopping, Faulted }

public sealed record ApkMetadata(string Path, string PackageId, int MinimumSdk, string[] NativeAbis, string Sha256,
    string AppName = "", long VersionCode = 0, string VersionName = "");

public sealed record ApkSelection(ApkMetadata Apk, string DeviceId, DateTimeOffset LastInstalledUtc, DateTimeOffset LastLaunchedUtc);

public sealed record SemanticInputEvent(long Sequence, double TimestampMs, int PointerId, string Phase,
    int X, int Y, double NormalizedX, double NormalizedY, int Width, int Height, int Orientation,
    double Pressure, string Source, double DispatchedMs);
