using System.IO;
using Newtonsoft.Json;
using DiscordStatusUpdater.Models;

namespace DiscordStatusUpdater.Services;

public class SettingsService
{
    private static readonly string AppDataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DiscordStatusUpdater");

    private static readonly string SettingsPath = Path.Combine(AppDataDir, "settings.json");
    private static readonly string PresetsPath = Path.Combine(AppDataDir, "presets.json");

    private static readonly string DefaultPresetsPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "Resources", "Presets", "default_presets.json");

    public AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsPath))
                return JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(SettingsPath))
                       ?? new AppSettings();
        }
        catch { /* return default below */ }
        return new AppSettings();
    }

    public void SaveSettings(AppSettings settings)
    {
        EnsureDir();
        File.WriteAllText(SettingsPath, JsonConvert.SerializeObject(settings, Formatting.Indented));
    }

    public List<ActivityPreset> LoadPresets()
    {
        try
        {
            if (File.Exists(PresetsPath))
            {
                var loaded = JsonConvert.DeserializeObject<List<ActivityPreset>>(File.ReadAllText(PresetsPath));
                if (loaded != null && loaded.Count > 0)
                    return loaded;
            }
        }
        catch { /* fall through to defaults */ }
        return LoadDefaultPresets();
    }

    public void SavePresets(List<ActivityPreset> presets)
    {
        EnsureDir();
        File.WriteAllText(PresetsPath, JsonConvert.SerializeObject(presets, Formatting.Indented));
    }

    private static List<ActivityPreset> LoadDefaultPresets()
    {
        try
        {
            if (File.Exists(DefaultPresetsPath))
            {
                var loaded = JsonConvert.DeserializeObject<List<ActivityPreset>>(
                    File.ReadAllText(DefaultPresetsPath));
                if (loaded != null) return loaded;
            }
        }
        catch { /* return empty list */ }
        return new List<ActivityPreset>();
    }

    private static void EnsureDir()
    {
        if (!Directory.Exists(AppDataDir))
            Directory.CreateDirectory(AppDataDir);
    }
}
