// File: ShippedDataTests.cs
// Description: Integrity tests for the shipped default_rules.json / default_presets.json (typos, dangling references).
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-10-09
// Last edit date: 2026-10-09
// Version: 1.0.0

using System.Text.RegularExpressions;
using DiscordStatusUpdater.Models;
using DiscordStatusUpdater.Services;
using Newtonsoft.Json;
using Xunit;

namespace DiscordStatusUpdater.Tests;

/// <summary>
/// The defaults are hand-edited JSON, so a typo (wrong preset name, truncated Application ID) would only show
/// up at runtime on someone's profile. These tests read the real files from the source tree.
/// </summary>
public class ShippedDataTests
{
    private static readonly string PresetsDir = FindPresetsDir();

    private static readonly List<DetectionRule> Rules =
        JsonConvert.DeserializeObject<List<DetectionRule>>(File.ReadAllText(Path.Combine(PresetsDir, "default_rules.json")))!;

    private static readonly List<ActivityPreset> Presets =
        JsonConvert.DeserializeObject<List<ActivityPreset>>(File.ReadAllText(Path.Combine(PresetsDir, "default_presets.json")))!;

    /// <summary>Walks up from the test binary to the repo root (the folder holding the .sln).</summary>
    private static string FindPresetsDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !dir.GetFiles("*.sln").Any()) dir = dir.Parent;
        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, "src", "DiscordStatusUpdater", "Resources", "Presets");
    }

    [Fact]
    public void RuleIds_AreUnique() =>
        Assert.Equal(Rules.Count, Rules.Select(r => r.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());

    [Fact]
    public void PresetNames_AreUnique() =>
        Assert.Equal(Presets.Count, Presets.Select(p => p.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());

    [Fact]
    public void EveryRule_PointsAtAnExistingPreset()
    {
        var names = Presets.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var r in Rules)
            Assert.True(names.Contains(r.PresetName), $"Rule '{r.Name}' references missing preset '{r.PresetName}'");
    }

    [Fact]
    public void EveryRule_HasAName_AndAtLeastOneProcess()
    {
        foreach (var r in Rules)
        {
            Assert.False(string.IsNullOrWhiteSpace(r.Name), $"Rule {r.Id} has no name");
            Assert.NotEmpty(RuleMatcher.ParseProcessNames(r.ProcessNames));
        }
    }

    [Fact]
    public void ApplicationIds_AreEmptyOrDiscordSnowflakes()
    {
        // Snowflakes are 17-20 digit numbers; a truncated paste would be shorter.
        foreach (var r in Rules.Where(r => r.ApplicationId.Length > 0))
            Assert.Matches(new Regex(@"^\d{17,20}$"), r.ApplicationId);
    }

    [Fact]
    public void ApplicationIds_AreNotSharedBetweenRules()
    {
        var ids = Rules.Where(r => r.ApplicationId.Length > 0).Select(r => r.ApplicationId).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void RulesWithOwnApplication_LeaveLargeImageKeyEmpty_SoDiscordShowsTheAppIcon()
    {
        foreach (var r in Rules.Where(r => r.ApplicationId.Length > 0))
        {
            var preset = Presets.First(p => p.Name == r.PresetName);
            Assert.True(string.IsNullOrEmpty(preset.LargeImageKey), $"Preset '{preset.Name}' sets an image key");
        }
    }

    [Theory]
    [InlineData("tabularis")]
    [InlineData("rdm")]
    [InlineData("onecommander")]
    [InlineData("vmware")]
    public void PrivacySensitiveTools_UseFixedTextOnly(string ruleId)
    {
        // Their window titles expose connection/host/folder/VM names, so the shipped presets must not echo titles.
        var rule = Rules.First(r => r.Id == ruleId);
        var preset = Presets.First(p => p.Name == rule.PresetName);
        foreach (var text in new[] { preset.Details, preset.State, preset.LargeImageText })
            Assert.DoesNotMatch(new Regex(@"\{(title|file|workspace)\}", RegexOptions.IgnoreCase), text);
    }

    [Fact]
    public void ShippedPresets_RenderWithinDiscordLimits_ForASampleContext()
    {
        foreach (var p in Presets)
            foreach (var t in new[] { p.Details, p.State })
            {
                var text = TemplateEngine.Render(t, TemplateContext.Sample);
                Assert.True(text == null || text.Length is >= TemplateEngine.MinLength and <= TemplateEngine.MaxLength);
            }
    }
}
