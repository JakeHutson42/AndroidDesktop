using AndroidDesktop.Models;
using System.IO;
using System.Text.Json;

namespace AndroidDesktop.Services;

public sealed class SettingsStore(string directory)
{
    private readonly SemaphoreSlim _writes = new(1, 1);
    private readonly JsonSerializerOptions _json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    private bool _readOnly;
    private bool _recovered;
    public event Action<string>? Failed;
    public string FilePath => Path.Combine(directory, "settings.json");
    public static string DataRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AndroidDesktop");

    public async Task<DesktopSettings> LoadAsync()
    {
        foreach (var path in new[] { FilePath, FilePath + ".bak" })
        {
            if (!File.Exists(path)) continue;
            try
            {
                using var document = JsonDocument.Parse(await File.ReadAllTextAsync(path));
                var version = document.RootElement.EnumerateObject().FirstOrDefault(p => p.Name.Equals("version", StringComparison.OrdinalIgnoreCase));
                if (version.Value.ValueKind == JsonValueKind.Number && version.Value.GetInt32() > 4)
                {
                    _readOnly = true;
                    Failed?.Invoke("Settings belong to a newer application version. This session will not overwrite them.");
                    return new();
                }
                var settings = document.RootElement.Deserialize<DesktopSettings>(_json) ?? throw new InvalidDataException("Empty settings.");
                if (settings.Runtime is null) throw new InvalidDataException("Runtime settings are missing.");
                settings.Runtime.Validate();
                if (settings.Window is null || settings.Theme is null || settings.SystemImage is null) throw new InvalidDataException("Incomplete settings.");
                _recovered = path.EndsWith(".bak", StringComparison.Ordinal);
                if (_recovered) Failed?.Invoke("Recovered settings from the last-known-good backup.");
                return DeviceProfiles.Normalize(settings);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidDataException or ArgumentException)
            { if (path == FilePath) _recovered = true; Failed?.Invoke("Could not load settings: " + error.Message); }
        }
        return new();
    }

    public async Task<bool> SaveAsync(DesktopSettings settings)
    {
        await _writes.WaitAsync();
        string? temporary = null;
        try
        {
            if (_readOnly) throw new InvalidOperationException("Newer settings are protected; changes cannot be saved by this version.");
            settings.Runtime.Validate();
            Directory.CreateDirectory(directory);
            temporary = FilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
            {
                await JsonSerializer.SerializeAsync(stream, DeviceProfiles.Normalize(settings), _json);
                await stream.FlushAsync(); stream.Flush(flushToDisk: true);
            }
            if (File.Exists(FilePath)) File.Replace(temporary, FilePath, _recovered ? null : FilePath + ".bak");
            else File.Move(temporary, FilePath);
            temporary = null; _recovered = false;
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        { Failed?.Invoke("Settings could not be saved: " + error.Message); return false; }
        finally
        {
            if (temporary is not null) { try { File.Delete(temporary); } catch (Exception error) when (error is IOException or UnauthorizedAccessException) { } }
            _writes.Release();
        }
    }
}
