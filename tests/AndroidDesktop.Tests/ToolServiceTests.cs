using AndroidDesktop.Services;
using System.IO;

namespace AndroidDesktop.Tests;
public class ToolServiceTests
{
    private static string PowerShell => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");
    [Fact] public void ArgumentListKeepsPathsAndMetacharactersAsSingleArguments()
    {
        var info = AndroidToolService.CreateStartInfo("adb.exe", ["-s", "emulator-5580", "install", "C:/game & tests/one.apk"]);
        Assert.False(info.UseShellExecute);
        Assert.Equal("C:/game & tests/one.apk", info.ArgumentList[3]);
    }
    [Fact] public async Task OutputAndErrorAreDrainedConcurrentlyAndBounded()
    {
        var result = await new AndroidToolService().RunAsync(PowerShell,
            ["-NoProfile", "-Command", "for ($i=0;$i -lt 2000;$i++) { [Console]::Out.WriteLine('x'*200); [Console]::Error.WriteLine('y'*200) }"],
            TimeSpan.FromSeconds(20), CancellationToken.None);
        Assert.Equal(0,result.ExitCode);
        Assert.InRange(result.Output.Length,1,262144); Assert.InRange(result.Error.Length,1,262144);
    }
    [Fact] public async Task TimeoutReapsItsOwnedTool()
    {
        var path = Path.Combine(Path.GetTempPath(), "phase0-tool-"+Guid.NewGuid()+".txt");
        try {
            await Assert.ThrowsAsync<TimeoutException>(() => new AndroidToolService().RunAsync(PowerShell,
                ["-NoProfile", "-Command", "[IO.File]::WriteAllText($env:PHASE0_PID_FILE, [string]$PID); Start-Sleep -Seconds 30"],
                TimeSpan.FromSeconds(3), CancellationToken.None, new Dictionary<string,string> { ["PHASE0_PID_FILE"] = path }));
            Assert.True(File.Exists(path));
            var id = int.Parse(await File.ReadAllTextAsync(path));
            Assert.Throws<ArgumentException>(() => System.Diagnostics.Process.GetProcessById(id));
        } finally { if (File.Exists(path)) File.Delete(path); }
    }
}
