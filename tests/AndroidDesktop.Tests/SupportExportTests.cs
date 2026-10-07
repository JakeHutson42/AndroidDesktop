using AndroidDesktop.Services;
using System.IO;
using System.IO.Compression;

namespace AndroidDesktop.Tests;
public class SupportExportTests
{
    [Fact] public void StructuredSanitizationKeepsValidJsonAndMeasurementsWhileRemovingSecrets()
    {
        var safe = SupportExportService.SanitizeJson(System.Text.Json.JsonSerializer.SerializeToElement(new {
            frameMs = 16.6, nested = new { token = "private-value", discovery = "private-file", path = "C:/Users/private/file" },
            error = "Failed C:/Users/private/file" }));
        Assert.Equal(16.6, safe.GetProperty("frameMs").GetDouble());
        Assert.DoesNotContain("private", safe.ToString());
        Assert.Equal("[redacted]", safe.GetProperty("nested").GetProperty("token").GetString());
    }
    [Theory]
    [InlineData("Bearer session-credential", "session-credential")]
    [InlineData("grpc.token=private-discovery-value", "private-discovery-value")]
    [InlineData("Failed C:\\Users\\Someone\\private game.apk", "Someone")]
    [InlineData("secret: abcdef", "abcdef")]
    [InlineData("{\"grpc.token\":\"private-discovery\"}", "private-discovery")]
    public void RedactsSecretsAndPersonalPaths(string input, string prohibited) => Assert.DoesNotContain(prohibited, SupportExportService.Redact(input));

    [Fact] public async Task ExportExcludesCredentialsApkEventsAndSettingsAndMarksIncompleteEvidence()
    {
        var root = Path.Combine(Path.GetTempPath(), "support-test-" + Guid.NewGuid()); Directory.CreateDirectory(root);
        try {
            await File.WriteAllTextAsync(Path.Combine(root, "measurements.jsonl"), "{\"kind\":\"apk\",\"data\":\"private-source\"}\n{\"kind\":\"session\",\"data\":\"Ready\"}\n{\"kind\":\"emulatorStatus\",\"data\":\"private-discovery\"}\n");
            await File.WriteAllTextAsync(Path.Combine(root, "INCOMPLETE.txt"), "private-path");
            var path = Path.Combine(root, "report.zip");
            await SupportExportService.ExportAsync(path, new { version = "0.3.0" }, "Bearer super-secret", root, CancellationToken.None);
            using var archive = ZipFile.OpenRead(path);
            Assert.NotNull(archive.GetEntry("INCOMPLETE.txt"));
            foreach (var entry in archive.Entries) {
                using var reader = new StreamReader(entry.Open()); var text = reader.ReadToEnd();
                Assert.DoesNotContain("private-", text); Assert.DoesNotContain("super-secret", text);
            }
            using var measurements = new StreamReader(archive.GetEntry("measurements.jsonl")!.Open());
            Assert.Contains("Ready", measurements.ReadToEnd());
        } finally { Directory.Delete(root, true); }
    }
    [Fact] public async Task CancelledExportPreservesExistingArchive()
    {
        var root = Path.Combine(Path.GetTempPath(), "support-test-" + Guid.NewGuid()); Directory.CreateDirectory(root);
        var path = Path.Combine(root, "report.zip"); await File.WriteAllTextAsync(path, "previous archive");
        try {
            using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => SupportExportService.ExportAsync(path, new { }, "", null, cancelled.Token));
            Assert.Equal("previous archive", await File.ReadAllTextAsync(path));
            Assert.Single(Directory.GetFiles(root));
        } finally { Directory.Delete(root, true); }
    }
}
