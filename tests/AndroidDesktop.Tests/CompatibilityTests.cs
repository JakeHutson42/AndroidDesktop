using AndroidDesktop.Models;
using AndroidDesktop.Services;
using System.IO;

namespace AndroidDesktop.Tests;
public class CompatibilityTests
{
    [Fact] public void ArmIsAcceptedWhenDeviceReportsSupport()
    {
        var apk = new ApkMetadata("game.apk", "example.game", 26, ["arm64-v8a"], "hash");
        ApkInstallService.CheckCompatibility(apk, 34, ["x86_64", "arm64-v8a"]);
    }
    [Fact] public void AbiMismatchIsRejected()
    {
        var apk = new ApkMetadata("game.apk", "example.game", 26, ["arm64-v8a"], "hash");
        Assert.Throws<InvalidDataException>(() => ApkInstallService.CheckCompatibility(apk, 34, ["x86_64"]));
    }
    [Fact] public void ManagedOnlyApkRequiresNoNativeAbi()
        => ApkInstallService.CheckCompatibility(new("game.apk", "example.game", 26, [], "hash"), 34, ["x86_64"]);
    [Fact] public void MinimumSdkIsEnforced()
        => Assert.Throws<InvalidDataException>(() => ApkInstallService.CheckCompatibility(new("game.apk", "example.game", 35, [], "hash"), 34, ["x86_64"]));
    [Theory]
    [InlineData("INSTALL_FAILED_UPDATE_INCOMPATIBLE")]
    [InlineData("INSTALL_FAILED_VERSION_DOWNGRADE")]
    [InlineData("INSTALL_FAILED_MISSING_SPLIT")]
    public async Task FailedInstallNeverUninstallsOrLaunches(string error)
    {
        var fake = new FakeTools(error);
        var installer = new ApkInstallService(fake, new PrototypeOptions());
        await Assert.ThrowsAsync<InvalidOperationException>(() => installer.InstallAndLaunchAsync(new("game.apk", "example.game", 26, [], "hash"), CancellationToken.None));
        Assert.Equal(3, fake.Calls.Count);
        Assert.All(fake.Calls, args => Assert.Equal(new[] { "-s", "emulator-5580" }, args.Take(2)));
        Assert.DoesNotContain(fake.Calls, args => args.Contains("uninstall") || args.Contains("clear") || args.Contains("start"));
    }
    [Fact] public void PortsAndAppOwnedDeviceRootAreEnforced()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AndroidDesktop", "avd");
        new PrototypeOptions { AvdHome = root }.Validate();
        Assert.Throws<InvalidDataException>(() => new PrototypeOptions { AvdHome = root, EmulatorPort = 5581 }.Validate());
        Assert.Throws<InvalidDataException>(() => new PrototypeOptions { AvdHome = root, GatewayPort = 8554 }.Validate());
        Assert.Throws<InvalidDataException>(() => new PrototypeOptions { AvdHome = Path.GetTempPath() }.Validate());
    }
    private sealed class FakeTools(string error) : IAndroidToolService
    {
        public List<string[]> Calls { get; } = [];
        public Task<ToolResult> RunAsync(string executable, IEnumerable<string> arguments, TimeSpan timeout,
            CancellationToken cancellationToken, IDictionary<string,string>? environment = null)
        {
            var args = arguments.ToArray(); Calls.Add(args);
            return Task.FromResult(args.Contains("ro.build.version.sdk") ? new ToolResult(0,"34","") :
                args.Contains("ro.product.cpu.abilist") ? new ToolResult(0,"x86_64","") : new ToolResult(1,"",error));
        }
    }
}
