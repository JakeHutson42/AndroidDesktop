using AndroidDesktop.Models;
using AndroidDesktop.Services;
using System.IO;
using System.Text.Json;

namespace AndroidDesktop.Tests;

public sealed class SettingsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "android-desktop-settings-" + Guid.NewGuid().ToString("N"));
    public SettingsTests() => Directory.CreateDirectory(_root);
    [Fact] public async Task SavesAreSerializedAndKeepPreviousCommit()
    {
        var store = new SettingsStore(_root);
        Assert.True(await store.SaveAsync(new() { Theme = "Arctic" }));
        var writes = Enumerable.Range(0, 10).Select(i => store.SaveAsync(new() { Theme = "Palette" + i })).ToArray();
        Assert.All(await Task.WhenAll(writes), Assert.True);
        var loaded = await store.LoadAsync();
        Assert.Equal("Palette9", loaded.Theme);
        var backup = JsonSerializer.Deserialize<DesktopSettings>(await File.ReadAllTextAsync(store.FilePath + ".bak"));
        Assert.Equal("Palette8", backup!.Theme);
        Assert.Empty(Directory.GetFiles(_root, "*.tmp"));
    }
    [Fact] public async Task CorruptPrimaryRecoversBackupWithoutReplacingItWithCorruption()
    {
        var store = new SettingsStore(_root);
        await store.SaveAsync(new() { Theme = "Arctic" }); await store.SaveAsync(new() { Theme = "Crimson" });
        await File.WriteAllTextAsync(store.FilePath, "broken JSON");
        var errors = new List<string>(); store.Failed += errors.Add;
        var loaded = await store.LoadAsync(); Assert.Equal("Arctic", loaded.Theme);
        Assert.True(await store.SaveAsync(loaded with { Theme = "Ultraviolet" }));
        Assert.Contains(errors, text => text.Contains("Recovered"));
        Assert.Contains("Arctic", await File.ReadAllTextAsync(store.FilePath + ".bak"));
    }
    [Fact] public async Task LegacyVersionMigratesAndPreservesItsOriginalAsBackup()
    {
        var store = new SettingsStore(_root);
        await File.WriteAllTextAsync(store.FilePath, "{\"version\":0,\"theme\":\"Crimson\"}");
        var loaded = await store.LoadAsync(); Assert.Equal(4, loaded.Version); Assert.Equal("Crimson", loaded.Theme);
        Assert.True(await store.SaveAsync(loaded));
        Assert.Contains("\"version\":0", await File.ReadAllTextAsync(store.FilePath + ".bak"));
    }
    [Fact] public async Task FutureSchemaIsNeverOverwritten()
    {
        var store = new SettingsStore(_root);
        const string future = "{\"version\":99,\"newData\":\"retain\"}";
        await File.WriteAllTextAsync(store.FilePath, future);
        await store.LoadAsync(); Assert.False(await store.SaveAsync(new()));
        Assert.Equal(future, await File.ReadAllTextAsync(store.FilePath));
    }
    [Fact] public async Task PhaseOneMigratesWithoutChangingThemeBoundsOrRuntime()
    {
        var store = new SettingsStore(_root);
        var old = new DesktopSettings { Version = 1, Theme = "Arctic", Window = new(200, 150, 1000, 700, true), Runtime = new DesktopSettings().Runtime with { SdkRoot = "C:/existing SDK" } };
        await File.WriteAllTextAsync(store.FilePath, JsonSerializer.Serialize(old));
        var migrated = await store.LoadAsync(); Assert.Equal(4, migrated.Version); Assert.Null(migrated.SelectedApk);
        Assert.Equal(old.Theme, migrated.Theme); Assert.Equal(old.Window, migrated.Window); Assert.Equal(old.Runtime, migrated.Runtime);
        await store.SaveAsync(migrated); Assert.Equal(1, JsonSerializer.Deserialize<DesktopSettings>(await File.ReadAllTextAsync(store.FilePath + ".bak"))!.Version);
    }
    [Fact] public async Task SaveFailureIsObservableAndLeavesCommittedSettingsIntact()
    {
        var store = new SettingsStore(_root); await store.SaveAsync(new() { Theme = "Arctic" });
        var errors = new List<string>(); store.Failed += errors.Add;
        using (File.Open(store.FilePath, FileMode.Open, FileAccess.Read, FileShare.None))
            Assert.False(await store.SaveAsync(new() { Theme = "Crimson" }));
        Assert.Equal("Arctic", (await store.LoadAsync()).Theme); Assert.Single(errors);
        Assert.Empty(Directory.GetFiles(_root, "*.tmp"));
    }
    public void Dispose()
    {
        foreach (var file in Directory.GetFiles(_root)) File.Delete(file);
        Directory.Delete(_root);
    }
}


