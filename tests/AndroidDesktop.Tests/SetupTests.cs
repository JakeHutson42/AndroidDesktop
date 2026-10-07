using AndroidDesktop.Models;
using AndroidDesktop.Services;
using System.IO;

namespace AndroidDesktop.Tests;

public class SetupTests
{
    [Fact] public void DiscoveryWalksExecutableAncestorsIndependentlyOfWorkingDirectory()
    {
        var roots = SetupService.SearchRoots(Path.Combine(Path.GetTempPath(), "project", "src", "bin")).ToArray();
        Assert.Contains(Path.Combine(Path.GetTempPath(), "project"), roots);
        Assert.Equal(Path.Combine(Path.GetTempPath(), "project", "src", "bin"), roots[0]);
    }
    [Fact] public async Task PreparationWithoutTermsDoesNotRunInstaller()
    {
        var root = Path.Combine(Path.GetTempPath(), "prepare-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try {
            File.WriteAllText(Path.Combine(root, "python.exe"), "fixture");
            File.WriteAllText(Path.Combine(root, "run.py"), "fixture");
            var tools = new CountingTools();
            var options = new DesktopSettings().Runtime with { PythonExecutable = Path.Combine(root, "python.exe"), GatewayRoot = root };
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => new RuntimePreparationService(tools).PrepareAsync(options,
                "system-images;android-34;google_apis_playstore;x86_64", false, new Progress<string>(), CancellationToken.None));
            Assert.Contains("accept", error.Message); Assert.Equal(0, tools.Calls);
        } finally { Directory.Delete(root, true); }
    }
    [Fact] public async Task EmbeddedControllerConfigurationPersistsAcrossSettingsMigration()
    {
        var root = Path.Combine(Path.GetTempPath(), "controller-settings-" + Guid.NewGuid().ToString("N"));
        try {
            var store = new SettingsStore(root);
            var original = new DesktopSettings { Runtime = new DesktopSettings().Runtime with { DisplayTransport = "controller", ShowStandaloneWindow = false } };
            Assert.True(await store.SaveAsync(original));
            var restored = await new SettingsStore(root).LoadAsync();
            Assert.Equal("controller", restored.Runtime.DisplayTransport);
            Assert.False(restored.Runtime.ShowStandaloneWindow);
            Assert.Equal(restored.Runtime, restored.Profiles.Single(p => p.Id == restored.ActiveProfileId).Runtime);
        } finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
    [Theory]
    [InlineData("system-images;android-34;google_apis_playstore;x86_64")]
    [InlineData("system-images;android-35;google_apis;arm64-v8a")]
    public void ValidImagesAreAccepted(string image) => SetupService.ValidateImage(image);
    [Theory]
    [InlineData("../../another-profile")]
    [InlineData("system-images;android-34;../escape;x86_64")]
    [InlineData("system-images;android-34;google_apis;x86_64;extra")]
    public void InvalidImageCannotBecomeAPath(string image) => Assert.Throws<InvalidDataException>(() => SetupService.ValidateImage(image));
    [Fact] public async Task DeviceOutsideAppRootCannotRunTools()
    {
        var tools = new CountingTools();
        var setup = new SetupService(tools, new DeviceStorageService());
        await Assert.ThrowsAsync<InvalidDataException>(() => setup.CreateDeviceAsync(new() { AvdHome = Path.GetTempPath() },
            "system-images;android-34;google_apis_playstore;x86_64", new Progress<string>(), CancellationToken.None));
        Assert.Equal(0, tools.Calls);
    }
    [Fact] public void JavaArgumentsPreservePathsAndAvoidShellAndOverwriteFlags()
    {
        var root = Path.Combine(Path.GetTempPath(), "sdk tools " + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "lib"));
        var jar = Path.Combine(root, "lib", "tool.jar"); File.WriteAllText(jar, "test");
        try
        {
            var args = SetupService.JavaArguments(new() { CommandLineToolsRoot = root }, "com.android.sdklib.tool.AvdManagerCli",
                ["create", "avd", "--path", "C:/my device/data"]);
            Assert.Contains(jar, args); Assert.Contains("C:/my device/data", args);
            Assert.Equal("-Dcom.android.sdkmanager.toolsdir=" + root, args[0]);
            Assert.DoesNotContain("--force", args); Assert.DoesNotContain("cmd.exe", args);
        }
        finally { File.Delete(jar); Directory.Delete(Path.Combine(root, "lib")); Directory.Delete(root); }
    }
    [Fact] public async Task StandardInputIsClosedAndOutputDrained()
    {
        var result = await new AndroidToolService().RunWithInputAsync("powershell.exe",
            ["-NoProfile", "-Command", "[Console]::Out.Write([Console]::In.ReadToEnd())"], "no\n", TimeSpan.FromSeconds(15), CancellationToken.None);
        Assert.Equal(0, result.ExitCode); Assert.Equal("no\n", result.Output);
    }
    private sealed class CountingTools : IAndroidToolService
    {
        public int Calls;
        public Task<ToolResult> RunAsync(string executable, IEnumerable<string> arguments, TimeSpan timeout,
            CancellationToken cancellationToken, IDictionary<string, string>? environment = null)
        { Calls++; return Task.FromResult(new ToolResult(0, "", "")); }
    }
}
