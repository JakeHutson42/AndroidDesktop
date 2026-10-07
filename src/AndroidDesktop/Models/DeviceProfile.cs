namespace AndroidDesktop.Models;

public sealed record DeviceProfile(string Id, string Name, PrototypeOptions Runtime, string SystemImage,
    ApkSelection? SelectedApk, ApkSelection[] ApkLibrary);
