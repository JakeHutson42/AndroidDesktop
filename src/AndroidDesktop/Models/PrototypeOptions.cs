using System.IO;
using System.Text.Json;

namespace AndroidDesktop.Models;

public sealed record PrototypeOptions
{
    public string ProfileId { get; init; } = "default";
    public int? MemoryMb { get; init; }
    public int? CpuCores { get; init; }
    public string SdkRoot { get; init; } = "";
    public string CommandLineToolsRoot { get; init; } = "";
    public string JavaExecutable { get; init; } = "";
    public string PythonExecutable { get; init; } = "";
    public string GatewayRoot { get; init; } = "";
    public string AvdHome { get; init; } = "%LOCALAPPDATA%/AndroidDesktop/avd";
    public string AvdName { get; init; } = "AndroidDesktop_Phase0";
    public string EvidenceRoot { get; init; } = "%LOCALAPPDATA%/AndroidDesktop/phase0-evidence";
    public int EmulatorPort { get; init; } = 5580;
    public int GrpcPort { get; init; } = 8554;
    public int GatewayPort { get; init; } = 8087;
    public bool ShowStandaloneWindow { get; init; }
    public string DisplayTransport { get; init; } = "controller";
    public string ViewportMode { get; init; } = "standard";
    public string? DiscoveryDirectory { get; init; }
    public string Serial => $"emulator-{EmulatorPort}";
    public string Adb => Path.Combine(SdkRoot, "platform-tools", "adb.exe");
    public string Emulator => Path.Combine(SdkRoot, "emulator", "emulator.exe");

    public static async Task<PrototypeOptions> LoadAsync(string path)
    {
        var json = Environment.ExpandEnvironmentVariables(await File.ReadAllTextAsync(path));
        var options = JsonSerializer.Deserialize<PrototypeOptions>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("Empty Phase 0 configuration.");
        options.Validate();
        return options;
    }

    public void Validate()
    {
        if (EmulatorPort < 5554 || EmulatorPort > 5682 || EmulatorPort % 2 != 0)
            throw new InvalidDataException("EmulatorPort must be an even port from 5554 through 5682.");
        if (GrpcPort is < 1024 or > 65535 || GatewayPort is < 1024 or > 65535 || GrpcPort == GatewayPort ||
            new[] { GrpcPort, GatewayPort }.Any(p => p == EmulatorPort || p == EmulatorPort + 1))
            throw new InvalidDataException("Use distinct, non-reserved emulator, gRPC and gateway ports.");
        if (ViewportMode is not ("standard" or "composition")) throw new InvalidDataException("ViewportMode must be standard or composition.");
        if (DisplayTransport is not ("webrtc" or "controller")) throw new InvalidDataException("Unsupported display transport.");
        if (!System.Text.RegularExpressions.Regex.IsMatch(AvdName, "^[A-Za-z0-9_-]+$")) throw new InvalidDataException("Invalid AVD name.");
        if (MemoryMb is < 512 or > 8192 || CpuCores is < 1 or > 8)
            throw new InvalidDataException("Use 512–8192 MiB guest RAM and 1–8 virtual CPU cores. Blank preserves the existing AVD configuration.");
        var expectedHome = Path.GetFullPath(AndroidDesktop.Services.DeviceProfiles.Home(ProfileId));
        if (!Path.GetFullPath(AvdHome).TrimEnd(Path.DirectorySeparatorChar).Equals(expectedHome, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"The profile must use its isolated application-owned AVD home: {expectedHome}");
        if (ProfileId != "default" && AvdName != "AndroidDesktop_" + ProfileId)
            throw new InvalidDataException("The profile must use its stable AVD name.");
        AndroidDesktop.Services.DeviceProfiles.CheckStoragePath(AvdHome);
    }
}
