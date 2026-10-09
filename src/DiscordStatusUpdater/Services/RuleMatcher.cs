// File: RuleMatcher.cs
// Description: Picks the detection rule (and triggering process) that should drive the Discord status.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-10-09
// Last edit date: 2026-10-09
// Version: 1.0.0

using System.Text.RegularExpressions;
using DiscordStatusUpdater.Models;

namespace DiscordStatusUpdater.Services;

/// <summary>Pure matching logic, independent of how processes are enumerated.</summary>
public static class RuleMatcher
{
    /// <summary>Splits a rule's comma-separated process list into normalised names (lower case, no ".exe").</summary>
    public static IReadOnlyList<string> ParseProcessNames(string? names) =>
        (names ?? string.Empty)
            .Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeName)
            .Where(n => n.Length > 0)
            .Distinct()
            .ToList();

    /// <summary>Lower-cases and strips a trailing ".exe" so "Code.exe", "code" and "CODE" compare equal.</summary>
    public static string NormalizeName(string name)
    {
        var n = name.Trim().ToLowerInvariant();
        return n.EndsWith(".exe", StringComparison.Ordinal) ? n[..^4] : n;
    }

    /// <summary>All (rule, process) pairs that currently match, in rule order.</summary>
    public static IReadOnlyList<DetectionMatch> FindMatches(
        IEnumerable<DetectionRule> rules, IEnumerable<RunningProcess> running)
    {
        var processes = running.ToList();
        var matches = new List<DetectionMatch>();

        foreach (var rule in rules)
        {
            if (!rule.Enabled) continue;

            var names = ParseProcessNames(rule.ProcessNames);
            if (names.Count == 0) continue;

            var titleRegex = BuildTitleRegex(rule.TitleRegex);

            // Of a rule's matching processes, a focused one first, then one that has a window title,
            // then the longest-running (a stable choice, so the status does not flap between instances).
            var candidate = processes
                .Where(p => names.Contains(NormalizeName(p.Name)))
                .Where(p => titleRegex == null || titleRegex.IsMatch(p.WindowTitle))
                .OrderByDescending(p => p.IsForeground)
                .ThenByDescending(p => p.WindowTitle.Length > 0)
                .ThenBy(p => p.StartTimeUtc ?? DateTime.MaxValue)
                .FirstOrDefault();

            if (candidate != null)
                matches.Add(new DetectionMatch(rule, candidate));
        }

        return matches;
    }

    /// <summary>
    /// Chooses the single winner. With <paramref name="preferForeground"/> the app that has focus
    /// beats rule order; otherwise (or when no matching app has focus) the first rule wins.
    /// </summary>
    public static DetectionMatch? PickWinner(IReadOnlyList<DetectionMatch> matches, bool preferForeground)
    {
        if (matches.Count == 0) return null;
        if (preferForeground)
        {
            var focused = matches.FirstOrDefault(m => m.Process.IsForeground);
            if (focused != null) return focused;
        }
        return matches[0];
    }

    /// <summary>Compiles a rule's title pattern; an empty or invalid pattern means "match any title".</summary>
    private static Regex? BuildTitleRegex(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern)) return null;
        try
        {
            // A timeout guards against catastrophic patterns typed into the editor.
            return new Regex(pattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(50));
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>True when <paramref name="pattern"/> is empty or a valid regex (used for inline validation).</summary>
    public static bool IsValidTitleRegex(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern)) return true;
        try { _ = new Regex(pattern); return true; }
        catch (ArgumentException) { return false; }
    }
}
