namespace AndroidDesktop.Models;

public sealed record WindowBounds(double Left = 100, double Top = 100, double Width = 1440,
    double Height = 900, bool Maximized = false);

public sealed record DesktopSettings
{
    public int Version { get; init; } = 4;
    public string Theme { get; init; } = "Graphite";
    public WindowBounds Window { get; init; } = new();
    public PrototypeOptions Runtime { get; init; } = new()
    {
        AvdHome = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AndroidDesktop", "avd")
    };
    public string SystemImage { get; init; } = "system-images;android-34;google_apis_playstore;x86_64";
    public ApkSelection? SelectedApk { get; init; }
    public ApkSelection[] ApkLibrary { get; init; } = [];
    public string ActiveProfileId { get; init; } = "default";
    public DeviceProfile[] Profiles { get; init; } = [];
}

