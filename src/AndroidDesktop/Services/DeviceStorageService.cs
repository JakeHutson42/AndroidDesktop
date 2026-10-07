using AndroidDesktop.Models;
using System.IO;

namespace AndroidDesktop.Services;

public sealed class DeviceStorageService
{
    public string GetOrCreateIdentity(PrototypeOptions options, string devicePath)
    {
        options.Validate();
        var root = Path.GetFullPath(options.AvdHome).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(devicePath).StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Device identity must remain inside the application-owned AVD.");
        DeviceProfiles.CheckStoragePath(devicePath);
        var marker = Path.Combine(devicePath, "android-desktop-device-id");
        if (!File.Exists(marker))
        {
            using var stream = new FileStream(marker, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(stream); writer.Write(Guid.NewGuid().ToString("D"));
        }
        var id = File.ReadAllText(marker).Trim();
        if (!Guid.TryParse(id, out _)) throw new InvalidDataException("Device identity is invalid. Inspect the application-owned AVD; no data was deleted.");
        return id;
    }
    public bool Exists(PrototypeOptions options)
    {
        options.Validate();
        var ini = Path.Combine(options.AvdHome, options.AvdName + ".ini");
        if (!File.Exists(ini)) return false;
        var path = File.ReadLines(ini).FirstOrDefault(l => l.StartsWith("path=", StringComparison.Ordinal))?[5..];
        var root = Path.GetFullPath(options.AvdHome).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (path is null || !Path.GetFullPath(path).StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Existing AVD points outside this application's data directory. It will not be changed.");
        DeviceProfiles.CheckStoragePath(path);
        return File.Exists(Path.Combine(path, "config.ini"));
    }

    public string NewDevicePath(PrototypeOptions options)
    {
        options.Validate();
        var path = Path.Combine(options.AvdHome, options.AvdName + ".avd");
        if (File.Exists(Path.Combine(options.AvdHome, options.AvdName + ".ini")) || Directory.Exists(path))
            throw new InvalidOperationException("Device files already exist. Setup will not overwrite them; inspect an interrupted creation before retrying.");
        Directory.CreateDirectory(options.AvdHome);
        return path;
    }
}
