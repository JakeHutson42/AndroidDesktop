using AndroidDesktop.Models;

namespace AndroidDesktop.Services;

public static class ApkLibrary
{
    public static ApkSelection? Find(IEnumerable<ApkSelection> entries, string deviceId, string packageId)
        => entries.FirstOrDefault(e => e.DeviceId == deviceId && e.Apk.PackageId == packageId);

    public static ApkSelection[] Upsert(IEnumerable<ApkSelection> entries, ApkSelection selected)
        => entries.Where(e => e.DeviceId != selected.DeviceId || e.Apk.PackageId != selected.Apk.PackageId)
            .Append(selected).ToArray();

    public static DesktopSettings Normalize(DesktopSettings settings)
    {
        var entries = settings.ApkLibrary ?? throw new System.IO.InvalidDataException("APK library is missing.");
        if (settings.SelectedApk is { } selectedEntry && (selectedEntry.Apk is null || string.IsNullOrWhiteSpace(selectedEntry.DeviceId) ||
            string.IsNullOrWhiteSpace(selectedEntry.Apk.PackageId) || string.IsNullOrWhiteSpace(selectedEntry.Apk.Path)))
            throw new System.IO.InvalidDataException("Selected APK metadata is invalid.");
        if (entries.Any(e => e is null || e.Apk is null || string.IsNullOrWhiteSpace(e.DeviceId) ||
            string.IsNullOrWhiteSpace(e.Apk.PackageId) || string.IsNullOrWhiteSpace(e.Apk.Path)))
            throw new System.IO.InvalidDataException("APK library contains invalid entries.");
        if (entries.GroupBy(e => (e.DeviceId, e.Apk.PackageId)).Any(g => g.Count() > 1))
            throw new System.IO.InvalidDataException("APK library contains duplicate identities.");
        return settings with { Version = 3, ApkLibrary = settings.SelectedApk is { } selected ? Upsert(entries, selected) : entries };
    }
}
