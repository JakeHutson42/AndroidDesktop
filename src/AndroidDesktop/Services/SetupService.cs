using AndroidDesktop.Models;
using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Text.RegularExpressions;

namespace AndroidDesktop.Services;

public sealed class SetupService(IAndroidToolService tools, DeviceStorageService storage)
{
    public static IEnumerable<string> SearchRoots(string start)
    {
        for (var directory = new DirectoryInfo(Path.GetFullPath(start)); directory is not null; directory = directory.Parent)
            yield return directory.FullName;
    }
    public bool Ready(PrototypeOptions options, string image) => RuntimeAvailable(options, image) && storage.Exists(options);
    public static bool RuntimeAvailable(PrototypeOptions options, string image) =>
        File.Exists(options.Adb) && File.Exists(options.Emulator) && File.Exists(options.JavaExecutable)
        && File.Exists(options.PythonExecutable) && File.Exists(Path.Combine(options.GatewayRoot, "run.py")) && ImageExists(options, image);
    public PrototypeOptions Discover(PrototypeOptions current)
    {
        static string Existing(string current, IEnumerable<string?> candidates, Func<string, bool> test)
            => test(current) ? current : candidates.FirstOrDefault(s => !string.IsNullOrEmpty(s) && test(s)) ?? current;
        var roots = SearchRoots(AppContext.BaseDirectory).Concat(SearchRoots(Environment.CurrentDirectory)).Distinct().ToArray();
        var managed = Path.Combine(SettingsStore.DataRoot, "runtime-components");
        var sdk = Existing(current.SdkRoot, new[] { Path.Combine(managed, "sdk"), Environment.GetEnvironmentVariable("ANDROID_HOME"),
            Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk") }.Concat(roots.Select(r => Path.Combine(r, "artifacts", "android-sdk"))),
            p => File.Exists(Path.Combine(p, "platform-tools", "adb.exe")) || Directory.Exists(Path.Combine(p, "cmdline-tools")));
        var commandRoots = Directory.Exists(Path.Combine(sdk, "cmdline-tools")) ? Directory.EnumerateDirectories(Path.Combine(sdk, "cmdline-tools")).OrderDescending().ToArray() : [];
        var commands = Existing(current.CommandLineToolsRoot, new[] { Path.Combine(managed, "command-tools-23", "cmdline-tools"), Path.Combine(sdk, "cmdline-tools", "latest") }.Concat(commandRoots), p => Directory.Exists(Path.Combine(p, "lib")));
        var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        var javaRoots = new[] { Path.Combine(managed, "java") }.Concat(roots.Select(r => Path.Combine(r, "artifacts", "android-setup", "java")));
        var java = Existing(current.JavaExecutable, new[] { javaHome is null ? null : Path.Combine(javaHome, "bin", "java.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Android", "Android Studio", "jbr", "bin", "java.exe") }.Concat(javaRoots.Where(Directory.Exists).SelectMany(r => Directory.EnumerateFiles(r, "java.exe", SearchOption.AllDirectories))), File.Exists);
        // Packaged dependencies take precedence over stale developer paths after an upgrade.
        var packagedGateway = Path.Combine(AppContext.BaseDirectory, "gateway");
        var packagedPython = Path.Combine(AppContext.BaseDirectory, "runtime", "python.exe");
        var gateway = File.Exists(Path.Combine(packagedGateway, "run.py")) ? packagedGateway : Existing(current.GatewayRoot, roots.Select(r => Path.Combine(r, "tools", "gateway")), p => File.Exists(Path.Combine(p, "run.py")));
        var python = File.Exists(packagedPython) ? packagedPython : Existing(current.PythonExecutable, [Path.Combine(gateway, ".venv", "Scripts", "python.exe")], File.Exists);
        return current with { SdkRoot = sdk, CommandLineToolsRoot = commands, JavaExecutable = java, GatewayRoot = gateway, PythonExecutable = python };
    }

    public async Task<string> CheckAsync(PrototypeOptions options, string image, IProgress<string> progress, CancellationToken token)
    {
        options.Validate(); ValidateImage(image);
        var results = new List<string>();
        foreach (var (name, path) in new[] { ("ADB", options.Adb), ("Emulator", options.Emulator), ("Java", options.JavaExecutable), ("Gateway Python", options.PythonExecutable), ("Gateway", Path.Combine(options.GatewayRoot, "run.py")) })
            results.Add(File.Exists(path) ? name + ": found" : name + ": missing — configure the path below");
        try { results.Add("WebView2: " + CoreWebView2Environment.GetAvailableBrowserVersionString()); }
        catch (WebView2RuntimeNotFoundException) { results.Add("WebView2: missing — install Microsoft's Evergreen Runtime"); }
        var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(options.AvdHome))!);
        results.Add($"Device storage: {drive.AvailableFreeSpace / 1073741824d:0.0} GiB free");
        results.Add(ImageExists(options, image) ? "Selected system image: installed" : "Selected system image: missing — install using the official SDK Manager");
        results.Add(storage.Exists(options) ? "Persistent device: ready" : "Persistent device: not created");
        if (File.Exists(options.JavaExecutable) && Directory.Exists(Path.Combine(options.CommandLineToolsRoot, "lib")))
        {
            progress.Report("Checking Java and SDK command-line tool compatibility…");
            var sdkCheck = await tools.RunAsync(options.JavaExecutable, JavaArguments(options, "com.android.sdklib.tool.sdkmanager.SdkManagerCli", ["--version", "--sdk_root=" + options.SdkRoot]), TimeSpan.FromSeconds(30), token, JavaEnvironment(options));
            results.Add(sdkCheck.ExitCode == 0 ? "Java / SDK tools: compatible (" + sdkCheck.Output.Trim() + ")" : "Java / SDK tools: failed — " + sdkCheck.Error.Trim());
        }
        if (File.Exists(options.PythonExecutable) && File.Exists(Path.Combine(options.GatewayRoot, "run.py")))
        {
            progress.Report("Checking the prepared gateway dependencies…");
            var python = await tools.RunAsync(options.PythonExecutable, ["-B", "-c", "import aiohttp, grpc; import sys; sys.path.insert(0, sys.argv[1]); import emulator_controller_pb2, rtc_service_v2_pb2", Path.Combine(options.GatewayRoot, "videobridge_gateway", "proto")], TimeSpan.FromSeconds(15), token);
            results.Add(python.ExitCode == 0 ? "Gateway dependencies: ready (transport remains provisional)" : "Gateway dependencies: incomplete — repair the application installation or prepared developer gateway. " + python.Error.Trim());
        }
        if (File.Exists(options.Emulator))
        {
            progress.Report("Checking Android hardware acceleration…");
            var acceleration = await tools.RunAsync(options.Emulator, ["-accel-check"], TimeSpan.FromSeconds(20), token);
            results.Add("Acceleration: " + (acceleration.ExitCode == 0 ? "available" : "unavailable — enable Windows Hypervisor Platform / firmware virtualization and reboot, then recheck") + "\n" + acceleration.Output.Trim() + acceleration.Error.Trim());
        }
        results.Add("Download size: not known until official SDK Manager resolves the selected packages. No downloads have started.");
        return string.Join("\n\n", results);
    }

    public async Task CreateDeviceAsync(PrototypeOptions options, string image, IProgress<string> progress, CancellationToken token)
    {
        options.Validate(); ValidateImage(image);
        if (DeviceBackupService.HasPendingRestore(options.AvdHome)) throw new InvalidOperationException("Recover the interrupted restore before creating or changing this device.");
        if (storage.Exists(options)) { progress.Report("Existing application-owned device retained."); return; }
        if (!ImageExists(options, image)) throw new InvalidOperationException("Install the selected system image and accept its licenses using the official SDK Manager first.");
        if (!File.Exists(options.JavaExecutable)) throw new FileNotFoundException("Select a compatible Java executable.");
        if (new DriveInfo(Path.GetPathRoot(options.AvdHome)!).AvailableFreeSpace < 4L * 1024 * 1024 * 1024)
            throw new IOException("Less than 4 GiB free for device creation. Free storage and retry; actual game requirements may be larger.");
        (await tools.RunAsync(options.Emulator, ["-accel-check"], TimeSpan.FromSeconds(20), token)).RequireSuccess();
        var path = storage.NewDevicePath(options);
        progress.Report("Creating the persistent application-owned device…");
        (await tools.RunWithInputAsync(options.JavaExecutable, JavaArguments(options, "com.android.sdklib.tool.AvdManagerCli",
            ["create", "avd", "--name", options.AvdName, "--package", image, "--device", "pixel_2", "--path", path]), "no\n", TimeSpan.FromMinutes(2), token,
            JavaEnvironment(options))).RequireSuccess();
        if (!storage.Exists(options)) throw new IOException("Device creation did not produce a complete AVD. Inspect setup output before retrying.");
        progress.Report("Device created. Paths saved; start it from the Device page.");
    }

    public static void ValidateImage(string image)
    {
        if (!Regex.IsMatch(image, "^system-images;android-[0-9]+;[A-Za-z0-9_-]+;[A-Za-z0-9_-]+$")) throw new InvalidDataException("Enter a valid SDK system-image package identifier.");
    }
    private static bool ImageExists(PrototypeOptions options, string image) => File.Exists(Path.Combine(options.SdkRoot, image.Replace(';', Path.DirectorySeparatorChar), "package.xml"));
    public static string[] JavaArguments(PrototypeOptions options, string mainClass, string[] arguments)
    {
        var library = Path.Combine(options.CommandLineToolsRoot, "lib");
        if (!Directory.Exists(library)) throw new DirectoryNotFoundException("SDK command-line tools are missing. Install them using the official setup guide.");
        var jars = Directory.EnumerateFiles(library, "*.jar", SearchOption.AllDirectories).Order().ToArray();
        if (jars.Length == 0) throw new FileNotFoundException("No SDK tool libraries found.");
        return new[] { "-Dcom.android.sdkmanager.toolsdir=" + options.CommandLineToolsRoot, "-cp", string.Join(Path.PathSeparator, jars), mainClass }.Concat(arguments).ToArray();
    }
    private static Dictionary<string, string> JavaEnvironment(PrototypeOptions options) => new()
    {
        ["ANDROID_HOME"] = options.SdkRoot, ["ANDROID_AVD_HOME"] = options.AvdHome,
        ["JAVA_HOME"] = Path.GetDirectoryName(Path.GetDirectoryName(options.JavaExecutable))!
    };
}
