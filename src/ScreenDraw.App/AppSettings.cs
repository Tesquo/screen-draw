using System.Text.Json;
using ScreenDraw.Core.Diagnostics;

namespace ScreenDraw.App;

internal sealed class AppSettings
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public string SnapshotFolder { get; set; } = DefaultFolder;

    public static string DefaultFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
        "Screen Draw");

    public static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ScreenDraw",
        "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), Options);
                if (loaded is not null && !string.IsNullOrWhiteSpace(loaded.SnapshotFolder))
                {
                    return loaded;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            Log.Write($"settings: could not read {FilePath} ({ex.Message}); using defaults");
        }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Options));
            Log.Write($"settings: saved snapshot folder '{SnapshotFolder}'");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Write($"settings: could not write {FilePath} ({ex.Message})");
        }
    }
}
