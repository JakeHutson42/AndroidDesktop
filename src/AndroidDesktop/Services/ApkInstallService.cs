using AndroidDesktop.Models;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Globalization;

namespace AndroidDesktop.Services;

public sealed class ApkInstallService(IAndroidToolService tools, PrototypeOptions options) : IApkInstallService
{
    private Task<ToolResult> AdbAsync(CancellationToken token, params string[] arguments) =>
        tools.RunAsync(options.Adb, new[] { "-s", options.Serial }.Concat(arguments), TimeSpan.FromMinutes(2), token);

    public async Task<ApkMetadata> InspectAsync(string path, CancellationToken token)
    {
        path = ApkImportService.ValidateFiles([path]);
        string[] abis;
        using (var zip = ZipFile.OpenRead(path))
        {
            if (zip.GetEntry("AndroidManifest.xml") is null) throw new InvalidDataException("The APK has no Android manifest.");
            abis = zip.Entries.Select(e => e.FullName.Split('/')).Where(p => p.Length >= 3 && p[0] == "lib" && p[^1].EndsWith(".so"))
                .Select(p => p[1]).Distinct(StringComparer.Ordinal).Order().ToArray();
        }
        async Task<string> Analyze(params string[] args) => (await tools.RunAsync(options.JavaExecutable,
            new[] { "-Dcom.android.sdklib.toolsdir=" + options.CommandLineToolsRoot, "-classpath",
                Path.Combine(options.CommandLineToolsRoot, "lib", "apkanalyzer-classpath.jar"), "com.android.tools.apk.analyzer.ApkAnalyzerCli" }
                .Concat(args).Append(path), TimeSpan.FromSeconds(30), token)).RequireSuccess();
        var package = await Analyze("manifest", "application-id");
        if (!Regex.IsMatch(package, @"^[A-Za-z][A-Za-z0-9_]*(\.[A-Za-z0-9_]+)+$")) throw new InvalidDataException("APK Analyzer returned an invalid package identifier.");
        var minimum = int.Parse(await Analyze("manifest", "min-sdk"), System.Globalization.CultureInfo.InvariantCulture);
        var versionCode = long.Parse(await Analyze("manifest", "version-code"), CultureInfo.InvariantCulture);
        var versionName = await Analyze("manifest", "version-name");
        var manifest = await Analyze("manifest", "print");
        if (Regex.IsMatch(manifest, "\\bsplit\\s*=|isSplitRequired\\s*=\\s*\"true\"|isFeatureSplit\\s*=\\s*\"true\""))
            throw new InvalidDataException("This APK belongs to a split package. Select a standalone APK; split sets and separate OBB data are unsupported.");
        var xml = XDocument.Parse(manifest);
        XNamespace android = "http://schemas.android.com/apk/res/android";
        var major = (string?)xml.Root?.Attribute(android + "versionCodeMajor");
        if (major is not null) versionCode = (long.Parse(major, CultureInfo.InvariantCulture) << 32) | (versionCode & uint.MaxValue);
        var name = (string?)xml.Root?.Element("application")?.Attribute(android + "label") ?? package;
        if (name.StartsWith("@string/", StringComparison.Ordinal))
        {
            try { name = await Analyze("resources", "value", "--config", "default", "--name", name[8..], "--type", "string"); }
            catch (Exception error) when (error is InvalidOperationException or FormatException) { name = package; }
        }
        if (string.IsNullOrWhiteSpace(name) || name.StartsWith('@')) name = package;
        await using var file = File.OpenRead(path);
        var checksum = Convert.ToHexString(await SHA256.HashDataAsync(file, token));
        return new(path, package, minimum, abis, checksum, name, versionCode, versionName);
    }

    public static void CheckCompatibility(ApkMetadata apk, int sdk, IEnumerable<string> supportedAbis)
    {
        if (apk.MinimumSdk > sdk) throw new InvalidDataException($"APK requires API {apk.MinimumSdk}; device is API {sdk}.");
        if (apk.NativeAbis.Length > 0 && !apk.NativeAbis.Intersect(supportedAbis, StringComparer.Ordinal).Any())
            throw new InvalidDataException($"APK ABIs ({string.Join(", ", apk.NativeAbis)}) do not match the device’s reported supported ABIs. ARM translation must be demonstrated on this image.");
    }

    public async Task CheckDeviceAsync(ApkMetadata apk, CancellationToken token)
    {
        var sdk = int.Parse((await AdbAsync(token, "shell", "getprop", "ro.build.version.sdk")).RequireSuccess());
        var abis = (await AdbAsync(token, "shell", "getprop", "ro.product.cpu.abilist")).RequireSuccess().Split(',');
        CheckCompatibility(apk, sdk, abis);
    }
    public async Task<bool> IsInstalledAsync(ApkMetadata apk, CancellationToken token)
    {
        ValidatePackage(apk.PackageId);
        var path = await AdbAsync(token, "shell", "pm", "path", "--user", "0", apk.PackageId);
        // Android's pm path returns 1 with empty output when the package is absent.
        if (path.ExitCode == 1 && string.IsNullOrWhiteSpace(path.Output) && string.IsNullOrWhiteSpace(path.Error)) return false;
        if (path.ExitCode != 0) throw new InvalidOperationException("Could not check the installed package: " + path.Error + path.Output);
        if (!path.Output.Split('\n').Any(line => line.TrimStart().StartsWith("package:", StringComparison.Ordinal))) return false;
        var details = (await AdbAsync(token, "shell", "dumpsys", "package", apk.PackageId)).RequireSuccess();
        // Match version details only in the package section, not disabled-system-package history.
        var section = Regex.Match(details, @"Package \[" + Regex.Escape(apk.PackageId) + @"\].*?(?=\n\s*Package \[|Hidden system packages:|Dexopt state:|Compiler stats:|$)", RegexOptions.Singleline).Value;
        var code = Regex.Match(section, @"\bversionCode=(\d+)").Groups[1].Value;
        var name = Regex.Match(section, @"\bversionName=([^\r\n]*)").Groups[1].Value.Trim();
        return long.TryParse(code, out var installedCode) && installedCode == apk.VersionCode && name == apk.VersionName;
    }
    public async Task InstallAsync(ApkMetadata apk, CancellationToken token)
    {
        var install = await AdbAsync(token, "install", "-r", apk.Path);
        var text = install.Output + install.Error;
        if (install.ExitCode != 0 || !install.Output.Contains("Success", StringComparison.Ordinal))
        {
            var reason = text.Contains("UPDATE_INCOMPATIBLE") ? "Signature conflict. Existing package data was not removed." :
                text.Contains("VERSION_DOWNGRADE") ? "Android rejected a downgrade. Existing package data was not removed." :
                text.Contains("MISSING_SPLIT") ? "APK requires missing split files; only standalone APKs are supported." :
                text.Contains("INSUFFICIENT_STORAGE") ? "Android device storage is insufficient. Existing application data was retained." :
                text.Contains("NO_MATCHING_ABIS") || text.Contains("OLDER_SDK") ? "Android rejected this APK's device compatibility. Existing application data was retained." :
                "Android rejected the installation; no uninstall or data reset was attempted.";
            throw new InvalidOperationException(reason + "\n" + text);
        }
    }
    public async Task InstallAndLaunchAsync(ApkMetadata apk, CancellationToken token)
    {
        await CheckDeviceAsync(apk, token); await InstallAsync(apk, token); await LaunchAsync(apk.PackageId, false, token);
    }
    public async Task LaunchAsync(string packageId, bool relaunch, CancellationToken token)
    {
        ValidatePackage(packageId);
        // Resolve the launcher explicitly; avoid monkey's randomized event runner.
        var resolved = (await AdbAsync(token, "shell", "cmd", "package", "resolve-activity", "--brief",
            "--user", "0", "-a", "android.intent.action.MAIN", "-c", "android.intent.category.LAUNCHER", packageId)).RequireSuccess();
        var component = resolved.Split('\n').Select(l => l.Trim()).LastOrDefault(l => l.Contains('/'));
        if (component is null || !component.StartsWith(packageId + "/", StringComparison.Ordinal) || !Regex.IsMatch(component, @"^[A-Za-z0-9_.]+/[A-Za-z0-9_.$]+$"))
            throw new InvalidOperationException("Installed APK has no resolvable launcher activity.");
        if (relaunch) (await AdbAsync(token, "shell", "am", "force-stop", "--user", "0", packageId)).RequireSuccess();
        var launch = (await AdbAsync(token, "shell", "am", "start", "--user", "0", "-W", "-n", component)).RequireSuccess();
        if (launch.Contains("Error:", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException(launch);
    }
    private static void ValidatePackage(string package)
    {
        if (!Regex.IsMatch(package, @"^[A-Za-z][A-Za-z0-9_]*(\.[A-Za-z0-9_]+)+$")) throw new InvalidDataException("Invalid Android package identifier.");
    }
}
