using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AndroidDesktop.Services;

/// <summary>Opt-in, bounded export. Never copies settings, discovery files, APKs or Android disks.</summary>
public static class SupportExportService
{
    private const long MaximumEvidenceBytes = 8 * 1024 * 1024;
    private static readonly HashSet<string> SafeKinds = ["reference", "session", "processes", "viewport.media", "viewport.geometry", "rotationRequested", "emulatorVersion", "acceleration", "gatewayVersions", "ro.build.version.sdk", "ro.product.cpu.abilist", "ro.build.fingerprint"];
    public static JsonElement SanitizeJson(JsonElement source)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer)) {
            void Write(JsonElement element) {
                if (element.ValueKind == JsonValueKind.Object) {
                    writer.WriteStartObject();
                    foreach (var property in element.EnumerateObject()) {
                        writer.WritePropertyName(property.Name);
                        if (Regex.IsMatch(property.Name, "(?i)token|password|authorization|credential|cookie|secret|discovery|(?:^|_)path$")) writer.WriteStringValue("[redacted]");
                        else Write(property.Value);
                    }
                    writer.WriteEndObject();
                } else if (element.ValueKind == JsonValueKind.Array) {
                    writer.WriteStartArray(); foreach (var item in element.EnumerateArray()) Write(item); writer.WriteEndArray();
                } else if (element.ValueKind == JsonValueKind.String) writer.WriteStringValue(Redact(element.GetString()!));
                else element.WriteTo(writer);
            }
            Write(source);
        }
        using var document = JsonDocument.Parse(buffer.ToArray()); return document.RootElement.Clone();
    }
    public static string Redact(string value)
    {
        if (value.Length > 32768) value = value[..32768] + " [truncated]";
        // Free-form errors may contain headers, JWTs or the emulator discovery credentials.
        value = Regex.Replace(value, @"(?i)(bearer\s+|(?:token|password|authorization|credential|cookie|secret)[\w. -]*[\""']?\s*[:=]\s*[\""']?)[^\s,;\""']+", "$1[redacted]");
        value = Regex.Replace(value, @"\b[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\b|\b[0-9a-fA-F]{64}\b", "[redacted]");
        value = Regex.Replace(value, @"(?i)[a-z]:[\\/][^\r\n\""<>]*|(?:/Users/|/home/|/tmp/)[^\r\n\""<>]*", "[local path]");
        return value.Length <= 32768 ? value : value[..32768] + " [truncated]";
    }
    public static async Task ExportAsync(string destination, object summary, string log, string? evidenceDirectory, CancellationToken token)
    {
        // CreateNew prevents a partial overwrite of a previous export. Commit via a unique sibling temp.
        var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true)) {
                async Task Write(string name, string text) {
                    await using var writer = new StreamWriter(zip.CreateEntry(name).Open());
                    await writer.WriteAsync(text.AsMemory(), token);
                }
                await Write("summary.json", JsonSerializer.Serialize(SanitizeJson(JsonSerializer.SerializeToElement(summary)), new JsonSerializerOptions { WriteIndented = true }));
                await Write("errors.txt", Redact(log));
                await Write("README.txt", "Candidate support evidence. Android media, saves and performance are unverified unless accompanied by a completed hardware report. Tokens, source paths, settings, discovery files and device disks are excluded. Review before sharing.\n");
                var path = evidenceDirectory is null ? null : Path.Combine(evidenceDirectory, "measurements.jsonl");
                var truncated = false;
                if (path is not null && File.Exists(path)) {
                    await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(file);
                    await using var output = new StreamWriter(zip.CreateEntry("measurements.jsonl").Open());
                    long bytes = 0; string? line;
                    while ((line = await reader.ReadLineAsync(token)) is not null) {
                        bytes += System.Text.Encoding.UTF8.GetByteCount(line);
                        if (bytes > MaximumEvidenceBytes) { truncated = true; break; }
                        if (line.Length > 65536) continue;
                        try {
                            using var document = JsonDocument.Parse(line);
                            if (SafeKinds.Contains(document.RootElement.GetProperty("kind").GetString() ?? ""))
                                await output.WriteLineAsync(JsonSerializer.Serialize(SanitizeJson(document.RootElement)).AsMemory(), token);
                        } catch (JsonException) { /* An active writer can leave one partial final row. */ }
                    }
                }
                if (truncated) await Write("TRUNCATED.txt", "Evidence export reached the 8 MiB scan limit; local measurements are retained.");
                if (evidenceDirectory is not null && File.Exists(Path.Combine(evidenceDirectory, "INCOMPLETE.txt")))
                    await Write("INCOMPLETE.txt", "The local evidence service reported a failure. This run cannot establish acceptance.");
            }
            File.Move(temporary, destination, overwrite: true);
        } finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
