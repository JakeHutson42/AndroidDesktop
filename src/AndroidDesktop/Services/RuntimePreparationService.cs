using AndroidDesktop.Models;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;

namespace AndroidDesktop.Services;

public sealed class RuntimePreparationService(IAndroidToolService tools)
{
    public async Task<PrototypeOptions> PrepareAsync(PrototypeOptions options, string image, bool acceptedTerms,
        IProgress<string> progress, CancellationToken token)
    {
        options.Validate(); SetupService.ValidateImage(image);
        if (!File.Exists(options.PythonExecutable) || !File.Exists(Path.Combine(options.GatewayRoot, "run.py")))
            throw new InvalidOperationException("The application installation is incomplete. Install the packaged Android Desktop application, which includes its display runtime.");
        if (SetupService.RuntimeAvailable(options, image)) return options;
        if (!acceptedTerms) throw new InvalidOperationException("Read and accept the Android SDK terms below, then select Prepare Android. No paths are needed.");
        var root = Path.Combine(SettingsStore.DataRoot, "runtime-components");
        Directory.CreateDirectory(root);
        using var lease = ActiveDeviceLease.Acquire(Path.Combine(root, "installation.lock"));
        if (new DriveInfo(Path.GetPathRoot(root)!).AvailableFreeSpace < 12L * 1024 * 1024 * 1024)
            throw new IOException("Android preparation needs at least 12 GB of free disk space. Free space and retry.");
        if (!File.Exists(options.JavaExecutable))
        {
            var javaRoot = Path.Combine(root, "java");
            await ExtractAsync("https://github.com/adoptium/temurin21-binaries/releases/download/jdk-21.0.12.1%2B1/OpenJDK21U-jdk_x64_windows_hotspot_21.0.12.1_1.zip",
                "F9D6E191AB098C0D416E7D588A24420A8621CD2F4720DAB2459B8B7B2D2D8B4E", javaRoot, "Java", progress, token);
            options = options with { JavaExecutable = Directory.EnumerateFiles(javaRoot, "java.exe", SearchOption.AllDirectories).Single(p => Path.GetFileName(Path.GetDirectoryName(p)) == "bin") };
        }
        if (!Directory.Exists(Path.Combine(options.CommandLineToolsRoot, "lib")))
        {
            var commandRoot = Path.Combine(root, "command-tools-23");
            await ExtractAsync("https://dl.google.com/android/repository/commandlinetools-win-16111833_latest.zip",
                "E5885E2E59038C0778A85FC19F01DE7CE7C567C105553F7617B936B1E0E87B2B", commandRoot, "Android tools", progress, token);
            options = options with { CommandLineToolsRoot = Path.Combine(commandRoot, "cmdline-tools") };
        }
        if (string.IsNullOrWhiteSpace(options.SdkRoot)) options = options with { SdkRoot = Path.Combine(root, "sdk") };
        Directory.CreateDirectory(options.SdkRoot);
        var environment = new Dictionary<string, string> { ["JAVA_HOME"] = Path.GetDirectoryName(Path.GetDirectoryName(options.JavaExecutable))!, ["ANDROID_HOME"] = options.SdkRoot, ["ANDROID_AVD_HOME"] = options.AvdHome };
        progress.Report("Accepting the Android SDK terms you selected…");
        (await tools.RunWithInputAsync(options.JavaExecutable, SetupService.JavaArguments(options, "com.android.sdklib.tool.sdkmanager.SdkManagerCli",
            ["--sdk_root=" + options.SdkRoot, "--licenses"]), string.Concat(Enumerable.Repeat("y\n", 100)), TimeSpan.FromMinutes(5), token, environment)).RequireSuccess();
        progress.Report("Installing Android 14 and the emulator. This can download several GB; keep the app open…");
        (await tools.RunWithInputAsync(options.JavaExecutable, SetupService.JavaArguments(options, "com.android.sdklib.tool.sdkmanager.SdkManagerCli",
            ["--sdk_root=" + options.SdkRoot, "--channel=0", "platform-tools", "emulator", image, "build-tools;36.0.0"]),
            string.Concat(Enumerable.Repeat("y\n", 100)), TimeSpan.FromMinutes(90), token, environment)).RequireSuccess();
        if (!SetupService.RuntimeAvailable(options, image)) throw new IOException("Android installation did not finish. Select Prepare Android again to resume.");
        return options;
    }

    private static async Task ExtractAsync(string url, string hash, string destination, string name, IProgress<string> progress, CancellationToken token)
    {
        // Only complete, checksum-verified archives become an installed component.
        if (File.Exists(Path.Combine(destination, ".complete"))) return;
        var stage = destination + "-" + Guid.NewGuid().ToString("N");
        var archive = stage + ".zip";
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            await using (var output = File.Create(archive))
            await using (var input = await response.Content.ReadAsStreamAsync(token))
            {
                var buffer = new byte[81920]; long received = 0; int count; long reported = -1;
                while ((count = await input.ReadAsync(buffer, token)) != 0)
                {
                    await output.WriteAsync(buffer.AsMemory(0, count), token); received += count;
                    var mb = received / 1048576;
                    if (mb != reported) { reported = mb; progress.Report($"Downloading {name}: {mb} MB" + (response.Content.Headers.ContentLength is { } size ? $" of {size / 1048576} MB" : "")); }
                }
            }
            await using (var input = File.OpenRead(archive))
                if (Convert.ToHexString(await SHA256.HashDataAsync(input, token)) != hash) throw new InvalidDataException(name + " download failed verification. Retry preparation.");
            progress.Report("Preparing " + name + "…");
            await Task.Run(() => ZipFile.ExtractToDirectory(archive, stage), token);
            token.ThrowIfCancellationRequested();
            File.WriteAllText(Path.Combine(stage, ".complete"), hash);
            if (Directory.Exists(destination)) throw new IOException("An incomplete runtime directory already exists: " + destination);
            Directory.Move(stage, destination);
        }
        finally { if (File.Exists(archive)) File.Delete(archive); if (Directory.Exists(stage)) Directory.Delete(stage, true); }
    }
}
