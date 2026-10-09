// File: TitleParser.cs
// Description: Splits an editor-style window title ("file - folder - App") into a file and a workspace part.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-10-09
// Last edit date: 2026-10-09
// Version: 1.0.0

using System.Text.RegularExpressions;

namespace DiscordStatusUpdater.Services;

/// <summary>
/// Most development tools title their window "&lt;file&gt; - &lt;workspace&gt; - &lt;App name&gt;"
/// (VS Code) or "&lt;solution&gt; - &lt;App name&gt;" (Visual Studio). This turns that into two tokens
/// templates can use. Titles that do not follow the pattern simply yield sensible fallbacks.
/// </summary>
public static class TitleParser
{
    // Segments are separated by a hyphen, en dash or em dash with spaces on both sides.
    // Requiring the spaces keeps names such as "my-project" or "a-b.cs" in one piece.
    private static readonly Regex SegmentSeparator = new(@"\s[-–—]\s", RegexOptions.Compiled);

    // Editors prefix or suffix unsaved/modified documents with a dot, bullet or asterisk.
    private static readonly char[] DirtyMarkers = { '●', '•', '*', '·' };

    public readonly record struct ParsedTitle(string File, string Workspace);

    public static ParsedTitle Parse(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return new ParsedTitle(string.Empty, string.Empty);

        var parts = SegmentSeparator.Split(title)
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToList();

        // The last segment is the application's own name ("Visual Studio Code"), which we already know.
        if (parts.Count > 1)
            parts.RemoveAt(parts.Count - 1);

        if (parts.Count == 0)
            return new ParsedTitle(string.Empty, string.Empty);

        // A lone segment is a workspace/solution name, not a file.
        if (parts.Count == 1)
            return new ParsedTitle(string.Empty, Clean(parts[0]));

        return new ParsedTitle(Clean(parts[0]), Clean(parts[^1]));
    }

    /// <summary>Removes unsaved-changes markers from both ends of a segment.</summary>
    private static string Clean(string segment) => segment.Trim(DirtyMarkers).Trim();
}
