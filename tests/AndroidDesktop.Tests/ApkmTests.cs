using AndroidDesktop.Services;
using System.IO;
using System.IO.Compression;

namespace AndroidDesktop.Tests;
public class ApkmTests
{
    [Theory]
    [InlineData("../split.apk")]
    [InlineData("nested/split.apk")]
    [InlineData("nested\\split.apk")]
    [InlineData("BASE.apk")]
    public void UnsafeOrAmbiguousBundleIsRejectedBeforeExtraction(string split) => WithBundle(split, (archive, output) => {
        Assert.Throws<InvalidDataException>(() => ApkInstallService.ExtractBundle(archive, output));
        Assert.False(Directory.Exists(output));
    });
    [Fact] public void FlatBundleExtractsApksAndKeepsOriginalSource() => WithBundle("split_config.x86_64.apk", (archive, output) => {
        Assert.Equal(archive, ApkImportService.ValidateFiles([archive]));
        var files = ApkInstallService.ExtractBundle(archive, output);
        Assert.Equal(2, files.Length);
        Assert.True(files.All(File.Exists)); Assert.True(File.Exists(archive));
    });
    private static void WithBundle(string split, Action<string, string> test)
    {
        var root = Path.Combine(Path.GetTempPath(), "apkm-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try {
            var archive = Path.Combine(root, "app.apkm");
            using (var zip = ZipFile.Open(archive, ZipArchiveMode.Create)) {
                zip.CreateEntry("base.apk"); zip.CreateEntry(split); zip.CreateEntry("info.json");
            }
            test(archive, Path.Combine(root, "extracted"));
        } finally { Directory.Delete(root, true); }
    }
}
