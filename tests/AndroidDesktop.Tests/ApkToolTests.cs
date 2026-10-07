using AndroidDesktop.Models;
using AndroidDesktop.Services;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

namespace AndroidDesktop.Tests;

public sealed class ApkToolTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), "metadata " + Guid.NewGuid().ToString("N") + ".apk");
    private ApkMetadata Apk => new(_path, "example.game", 26, [], "hash", "Game", 3, "1.3");
    public ApkToolTests()
    {
        using var zip = ZipFile.Open(_path, ZipArchiveMode.Create);
        zip.CreateEntry("AndroidManifest.xml"); zip.CreateEntry("lib/arm64-v8a/game.so");
    }
    [Fact] public async Task OfficialAnalyzerCommandsReadIdentityVersionLabelAbiAndHash()
    {
        var tools = new FakeTools();
        var apk = await new ApkInstallService(tools, new()).InspectAsync(_path, CancellationToken.None);
        Assert.Equal("example.game", apk.PackageId); Assert.Equal("Test Game", apk.AppName);
        Assert.Equal(3, apk.VersionCode); Assert.Equal("1.3", apk.VersionName); Assert.Equal(26, apk.MinimumSdk);
        Assert.Equal(["arm64-v8a"], apk.NativeAbis); Assert.Equal(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(_path))), apk.Sha256);
        Assert.All(tools.Calls, args => Assert.Contains("com.android.tools.apk.analyzer.ApkAnalyzerCli", args));
    }
    [Fact] public async Task SplitManifestIsRejectedBeforeAnyInstallation()
    {
        var tools = new FakeTools { Split = true };
        await Assert.ThrowsAsync<InvalidDataException>(() => new ApkInstallService(tools, new()).InspectAsync(_path, CancellationToken.None));
        Assert.DoesNotContain(tools.Calls, args => args.Contains("install"));
    }
    [Theory][InlineData("3", "1.3", true)][InlineData("4", "1.3", false)][InlineData("3", "changed", false)]
    public async Task InstalledShortcutRequiresActualPackageVersion(string code, string name, bool expected)
    {
        var tools = new FakeTools { Code = code, Name = name };
        Assert.Equal(expected, await new ApkInstallService(tools, new()).IsInstalledAsync(Apk, CancellationToken.None));
        Assert.All(tools.Calls, args => Assert.Equal(["-s", "emulator-5580"], args.Take(2)));
    }
    [Fact] public async Task MissingPackageExitOneIsNotMistakenForTransportFailure()
    {
        var tools = new FakeTools { Missing = true };
        Assert.False(await new ApkInstallService(tools, new()).IsInstalledAsync(Apk, CancellationToken.None)); Assert.Single(tools.Calls);
    }
    [Fact] public async Task FailedInstalledQueryDoesNotPermitAutomaticReinstall()
    {
        var tools = new FakeTools { Offline = true };
        await Assert.ThrowsAsync<InvalidOperationException>(() => new ApkInstallService(tools, new()).IsInstalledAsync(Apk, CancellationToken.None));
        Assert.Single(tools.Calls);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task LaunchAndRelaunchUseExplicitPackageWithoutDataDeletion(bool relaunch)
    {
        var tools = new FakeTools();
        await new ApkInstallService(tools, new()).LaunchAsync(Apk.PackageId, relaunch, CancellationToken.None);
        Assert.Equal(relaunch, tools.Calls.Any(args => args.Contains("force-stop")));
        Assert.DoesNotContain(tools.Calls, args => args.Contains("clear") || args.Contains("uninstall"));
        Assert.All(tools.Calls, args => Assert.Equal(["-s", "emulator-5580"], args.Take(2)));
        Assert.Contains("example.game/.Main", tools.Calls.Last());
    }
    [Theory][InlineData("INSUFFICIENT_STORAGE")][InlineData("NO_MATCHING_ABIS")][InlineData("OLDER_SDK")]
    public async Task InstallationFailureNeverUsesDataDeletingRecovery(string error)
    {
        var tools = new FakeTools { InstallError = error };
        await Assert.ThrowsAsync<InvalidOperationException>(() => new ApkInstallService(tools, new()).InstallAsync(Apk, CancellationToken.None));
        Assert.Equal(["-s", "emulator-5580", "install", "-r", _path], tools.Calls.Single());
    }
    public void Dispose() => File.Delete(_path);
    private sealed class FakeTools : IAndroidToolService
    {
        public List<string[]> Calls { get; } = [];
        public string Code = "3", Name = "1.3";
        public bool Missing, Offline, Split;
        public string? InstallError;
        public Task<ToolResult> RunAsync(string executable, IEnumerable<string> arguments, TimeSpan timeout,
            CancellationToken cancellationToken, IDictionary<string, string>? environment = null)
        {
            var args = arguments.ToArray(); Calls.Add(args);
            ToolResult result;
            if (args.Contains("application-id")) result = new(0, "example.game", "");
            else if (args.Contains("min-sdk")) result = new(0, "26", "");
            else if (args.Contains("version-code")) result = new(0, "3", "");
            else if (args.Contains("version-name")) result = new(0, "1.3", "");
            else if (args.Contains("print")) result = new(0, "<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\"" + (Split ? " split=\"part\"" : "") + "><application android:label=\"@string/app_name\"/></manifest>", "");
            else if (args.Contains("resources")) result = new(0, "Test Game", "");
            else if (args.Contains("path")) result = Offline ? new(1, "", "device offline") : Missing ? new(1, "", "") : new(0, "package:/data/app/example.game/base.apk\n", "");
            else if (args.Contains("dumpsys")) result = new(0, $"Packages:\n  Package [example.game] (abc):\n    versionCode={Code} minSdk=26 targetSdk=34\n    versionName={Name}\nHidden system packages:\n  Package [example.game] (old):\n    versionCode=3\n    versionName=1.3\n", "");
            else if (args.Contains("resolve-activity")) result = new(0, "example.game/.Main", "");
            else if (args.Contains("install")) result = InstallError is null ? new(0, "Success", "") : new(1, "", "INSTALL_FAILED_" + InstallError);
            else result = new(0, "Status: ok", "");
            return Task.FromResult(result);
        }
    }
}
