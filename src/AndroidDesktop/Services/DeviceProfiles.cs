using AndroidDesktop.Models;
using System.IO;

namespace AndroidDesktop.Services;

public static class DeviceProfiles
{
    public const int MaximumProfiles = 32;
    public static string Home(string id) => id == "default" ? Path.Combine(SettingsStore.DataRoot, "avd") :
        Path.Combine(SettingsStore.DataRoot, "profiles", ValidateId(id), "avd");
    public static void CheckStoragePath(string path)
    {
        var root = Path.GetFullPath(SettingsStore.DataRoot).TrimEnd(Path.DirectorySeparatorChar);
        var current = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
        if (!current.Equals(root, StringComparison.OrdinalIgnoreCase) && !current.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Device storage must remain inside the application's data root.");
        while (current.Length >= root.Length)
        {
            if ((Directory.Exists(current) || File.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Device storage cannot use symbolic links or junctions. Existing files were retained.");
            if (current.Equals(root, StringComparison.OrdinalIgnoreCase)) break;
            current = Path.GetDirectoryName(current)!;
        }
    }
    public static string ValidateId(string id)
    {
        if (id != "default" && (!Guid.TryParseExact(id, "N", out var parsed) || parsed.ToString("N") != id))
            throw new InvalidDataException("Invalid device profile identity.");
        return id;
    }
    public static DeviceProfile Create(string name, PrototypeOptions dependencies, string image)
    {
        name = name.Trim(); ValidateName(name); SetupService.ValidateImage(image);
        var id = Guid.NewGuid().ToString("N");
        var options = dependencies with { ProfileId = id, AvdHome = Home(id), AvdName = "AndroidDesktop_" + id,
            MemoryMb = 2048, CpuCores = 2, EvidenceRoot = Path.Combine(SettingsStore.DataRoot, "profiles", id, "evidence"), DiscoveryDirectory = null };
        options.Validate(); return new(id, name, options, image, null, []);
    }
    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 64 || name.Any(char.IsControl))
            throw new InvalidDataException("Profile names must contain 1–64 characters without control characters.");
    }
    public static DesktopSettings Normalize(DesktopSettings settings)
    {
        if (settings.Runtime is null || settings.SystemImage is null) throw new InvalidDataException("Active profile configuration is missing.");
        settings.Runtime.Validate(); SetupService.ValidateImage(settings.SystemImage);
        settings = ApkLibrary.Normalize(settings);
        var profiles = settings.Profiles ?? throw new InvalidDataException("Device profiles are missing.");
        if (profiles.Length == 0)
        {
            if (settings.ActiveProfileId != "default" || settings.Runtime.ProfileId != "default")
                throw new InvalidDataException("The active device profile is missing.");
            profiles = [new("default", "Default device", settings.Runtime, settings.SystemImage, settings.SelectedApk, settings.ApkLibrary)];
        }
        if (profiles.Length > MaximumProfiles || profiles.Any(p => p is null) || profiles.Select(p => p.Id).Distinct().Count() != profiles.Length)
            throw new InvalidDataException("Invalid or duplicate device profiles.");
        for (var index = 0; index < profiles.Length; index++)
        {
            var profile = profiles[index];
            if (profile.Runtime is null || profile.SystemImage is null || profile.Id is null || profile.Name is null)
                throw new InvalidDataException("Incomplete device profile.");
            ValidateId(profile.Id); ValidateName(profile.Name); profile.Runtime.Validate(); SetupService.ValidateImage(profile.SystemImage);
            if (profile.Runtime.ProfileId != profile.Id) throw new InvalidDataException("Profile and runtime identities differ.");
            var normalized = ApkLibrary.Normalize(settings with { SelectedApk = profile.SelectedApk, ApkLibrary = profile.ApkLibrary });
            // Clone the array so validation/migration never mutates its caller's snapshots.
            if (index == 0) profiles = profiles.ToArray();
            profiles[index] = profile with { ApkLibrary = normalized.ApkLibrary };
        }
        if (!profiles.Any(p => p.Id == settings.ActiveProfileId) || settings.Runtime.ProfileId != settings.ActiveProfileId)
            throw new InvalidDataException("Active profile does not match the runtime.");
        // The top-level active snapshot retains compatibility with the existing services.
        return Capture(settings with { Version = 4, Profiles = profiles });
    }
    public static DesktopSettings Capture(DesktopSettings settings) => settings with
    {
        Version = 4,
        Profiles = settings.Profiles.Select(p => p.Id == settings.ActiveProfileId ? p with
        { Runtime = settings.Runtime, SystemImage = settings.SystemImage, SelectedApk = settings.SelectedApk, ApkLibrary = settings.ApkLibrary } : p).ToArray()
    };
    public static DesktopSettings Switch(DesktopSettings settings, string id)
    {
        settings = Normalize(settings);
        var target = settings.Profiles.SingleOrDefault(p => p.Id == id) ?? throw new InvalidDataException("Unknown device profile.");
        return settings with { ActiveProfileId = target.Id, Runtime = target.Runtime, SystemImage = target.SystemImage,
            SelectedApk = target.SelectedApk, ApkLibrary = target.ApkLibrary };
    }
}
