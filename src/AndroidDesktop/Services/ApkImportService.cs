using AndroidDesktop.Models;
using System.IO;

namespace AndroidDesktop.Services;

public interface IApkInstallService
{
    Task<ApkMetadata> InspectAsync(string path, CancellationToken token);
    Task CheckDeviceAsync(ApkMetadata apk, CancellationToken token);
    Task<bool> IsInstalledAsync(ApkMetadata apk, CancellationToken token);
    Task InstallAsync(ApkMetadata apk, CancellationToken token);
    Task LaunchAsync(string packageId, bool relaunch, CancellationToken token);
}

public sealed class ApkImportService(IApkInstallService installer)
{
    public static string ValidateFiles(IEnumerable<string> paths)
    {
        var files = paths.ToArray();
        if (files.Length != 1) throw new InvalidDataException("Open or drop one APK or APKM bundle. Keep split files together in the original APKM bundle.");
        if (!new[] { ".apk", ".apkm" }.Contains(Path.GetExtension(files[0]), StringComparer.OrdinalIgnoreCase))
            throw new InvalidDataException("Choose an .apk or .apkm file. AAB, APKS, XAPK and separate OBB data are unsupported.");
        var path = Path.GetFullPath(files[0]);
        if (!File.Exists(path)) throw new FileNotFoundException("The APK source is missing. Select its library entry and use Locate APK / update, or use Open APK to import a file.", path);
        return path;
    }

    public async Task<ApkSelection> OpenAsync(string path, string deviceId, ApkSelection? previous, bool relaunch,
        Func<ApkSelection, Task<bool>> commit, IProgress<string> progress, CancellationToken token,
        Func<string, string, ApkSelection?>? lookup = null, string? expectedPackage = null)
    {
        path = ValidateFiles([path]);
        if (string.IsNullOrWhiteSpace(deviceId)) throw new InvalidOperationException("Device identity is unavailable. Start the application-owned device first.");
        // Keep the inspected source unchanged through adb transfer; never stage a different file under the same hash.
        await using var sourceLock = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        progress.Report("Inspecting APK identity, version and compatibility…");
        var apk = await installer.InspectAsync(path, token);
        if (expectedPackage is not null && apk.PackageId != expectedPackage)
            throw new InvalidDataException("The replacement APK belongs to another package. Use Open APK to import it separately.");
        if (lookup is not null) previous = lookup(deviceId, apk.PackageId);
        await installer.CheckDeviceAsync(apk, token);
        var unchanged = previous is not null && previous.DeviceId == deviceId && previous.Apk.PackageId == apk.PackageId &&
            previous.Apk.Sha256 == apk.Sha256 && previous.Apk.VersionCode == apk.VersionCode && previous.Apk.VersionName == apk.VersionName;
        var installed = unchanged && await installer.IsInstalledAsync(apk, token);
        DateTimeOffset installedAt;
        if (installed) { installedAt = previous!.LastInstalledUtc; progress.Report("Verified installed app; launching without reinstalling…"); }
        else
        {
            progress.Report("Installing compatible APK while preserving Android data…");
            await installer.InstallAsync(apk, token); installedAt = DateTimeOffset.UtcNow;
        }
        progress.Report(relaunch ? "Restarting the selected app…" : "Launching the selected app…");
        await installer.LaunchAsync(apk.PackageId, relaunch, token);
        var selected = new ApkSelection(apk, deviceId, installedAt, DateTimeOffset.UtcNow);
        // After dispatch, finish the short metadata commit even if cancellation arrives, avoiding a false install record.
        if (!await commit(selected)) throw new IOException("App launched, but selected APK metadata could not be saved. Previous selection retained; retry after resolving the settings error.");
        return selected;
    }
}
