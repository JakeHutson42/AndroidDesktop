using AndroidDesktop.Models;
using AndroidDesktop.Services;
using System.IO;
using System.Text.Json;

namespace AndroidDesktop.Tests;
public class EvidenceTests
{
    [Fact] public async Task MeasurementWriterFlushesProcessSamplesAndMarksInvalidInputEvidence()
    {
        var root = Path.Combine(Path.GetTempPath(),"phase0-evidence-test-"+Guid.NewGuid());
        var evidence = new EvidenceService();
        try {
            evidence.Start(new PrototypeOptions { EvidenceRoot = root });
            Assert.True(evidence.TryWrite("test",new { value = 1 }));
            await Task.Delay(1200);
            evidence.Invalidate("deliberate input evidence overflow");
            await evidence.StopAsync();
            var rows = File.ReadAllLines(Path.Combine(evidence.DirectoryPath!,"measurements.jsonl"))
                .Select(line=>JsonDocument.Parse(line).RootElement.Clone()).ToArray();
            Assert.Contains(rows,row=>row.GetProperty("kind").GetString()=="processes");
            Assert.Contains(rows,row=>row.GetProperty("kind").GetString()=="test");
            Assert.False(evidence.TryWrite("after-stop",null));
            Assert.True(File.Exists(Path.Combine(evidence.DirectoryPath!,"INCOMPLETE.txt")));
        } finally {
            await evidence.StopAsync();
            // Temporary test data only; no device storage is involved.
            var resolved = Path.GetFullPath(root);
            Assert.StartsWith(Path.GetFullPath(Path.GetTempPath()), resolved, StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith("phase0-evidence-test-", Path.GetFileName(resolved), StringComparison.Ordinal);
            if (Directory.Exists(resolved)) Directory.Delete(resolved,recursive:true);
        }
    }
}
