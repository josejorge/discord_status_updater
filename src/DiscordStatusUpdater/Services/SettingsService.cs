// File: SettingsService.cs
// Description: Reads and writes settings, presets and detection rules as JSON under %AppData%\DiscordStatusUpdater.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-06-01
// Last edit date: 2026-10-09
// Version: 2.0.1

using System.IO;
using System.Windows;
using Newtonsoft.Json;
using DiscordStatusUpdater.Models;

namespace DiscordStatusUpdater.Services;

public class SettingsService
{
    /// <summary>
    /// Optional override for where settings live (portable installs, automated UI tests). Note that
    /// Windows resolves the real %AppData% from the user profile, not from the APPDATA variable.
    /// </summary>
    public const string DataDirEnvVar = "DISCORD_STATUS_UPDATER_DATA";

    private static readonly string AppDataDir =
        Environment.GetEnvironmentVariable(DataDirEnvVar) is { Length: > 0 } custom
            ? custom
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DiscordStatusUpdater");

    private static readonly string SettingsPath = Path.Combine(AppDataDir, "settings.json");
    private static readonly string PresetsPath = Path.Combine(AppDataDir, "presets.json");
    private static readonly string RulesPath = Path.Combine(AppDataDir, "rules.json");

    // The defaults are WPF Resources compiled into the assembly (they are not copied next to the exe).
    private const string DefaultPresetsUri = "pack://application:,,,/Resources/Presets/default_presets.json";
    private const string DefaultRulesUri = "pack://application:,,,/Resources/Presets/default_rules.json";

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

    /// <summary>
    /// Loads the user's detection rules. On first run (no rules.json yet) the shipped rules are returned
    /// and any shipped presets they need are merged into <paramref name="presets"/>, so users upgrading
    /// from a version without auto-detect get working rules without losing their own presets
    /// (only presets the shipped rules reference are merged, never ones the user deleted).
    /// </summary>
    public List<DetectionRule> LoadRules(List<ActivityPreset> presets, out bool presetsChanged)
    {
        presetsChanged = false;
        try
        {
            if (File.Exists(RulesPath))
            {
                var loaded = JsonConvert.DeserializeObject<List<DetectionRule>>(File.ReadAllText(RulesPath));
                if (loaded != null) return loaded;
            }
        }
        catch { /* fall through to defaults */ }

        var rules = ReadResource<List<DetectionRule>>(DefaultRulesUri) ?? new List<DetectionRule>();

        // Only first-run merging: a preset the user later deletes must not come back on every launch.
        var needed = rules.Select(r => r.PresetName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var shipped in LoadDefaultPresets().Where(p => needed.Contains(p.Name)))
        {
            if (presets.Any(p => string.Equals(p.Name, shipped.Name, StringComparison.OrdinalIgnoreCase))) continue;
            presets.Add(shipped);
            presetsChanged = true;
        }

        return rules;
    }

    public void SaveRules(List<DetectionRule> rules)
    {
        EnsureDir();
        File.WriteAllText(RulesPath, JsonConvert.SerializeObject(rules, Formatting.Indented));
    }

    private static List<ActivityPreset> LoadDefaultPresets() =>
        ReadResource<List<ActivityPreset>>(DefaultPresetsUri) ?? new List<ActivityPreset>();

    /// <summary>Deserialises a JSON file embedded as a WPF resource; null if missing or malformed.</summary>
    private static T? ReadResource<T>(string packUri) where T : class
    {
        try
        {
            var info = Application.GetResourceStream(new Uri(packUri));
            if (info == null) return null;
            using var reader = new StreamReader(info.Stream);
            return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
        }
        catch { return null; }
    }

    private static void EnsureDir()
    {
        if (!Directory.Exists(AppDataDir))
            Directory.CreateDirectory(AppDataDir);
    }
}
