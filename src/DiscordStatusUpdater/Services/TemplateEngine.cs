// File: TemplateEngine.cs
// Description: Expands {tokens} and [optional sections] in preset text using details of the detected app.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-10-09
// Last edit date: 2026-10-09
// Version: 1.0.0

using System.Text.RegularExpressions;

namespace DiscordStatusUpdater.Services;

/// <summary>Values available to templates. Empty strings mean "not known".</summary>
public sealed record TemplateContext(string App, string Process, string Title, string File, string Workspace)
{
    /// <summary>Context with every token empty, used where no app has been detected.</summary>
    public static readonly TemplateContext Empty = new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

    /// <summary>Realistic sample values for the preset editor's preview.</summary>
    public static readonly TemplateContext Sample = new(
        "Visual Studio Code", "Code", "Program.cs - my-project - Visual Studio Code", "Program.cs", "my-project");
}

/// <summary>
/// Tokens: {app} {process} {title} {file} {workspace}.
/// Square brackets mark an optional section that is dropped entirely when any token inside it is empty,
/// so "[Editing {file}]" disappears instead of leaving a dangling "Editing ".
/// </summary>
public static class TemplateEngine
{
    /// <summary>Discord rejects Details/State/tooltip text shorter than 2 or longer than 128 characters.</summary>
    public const int MinLength = 2;
    public const int MaxLength = 128;

    private static readonly Regex OptionalSection = new(@"\[([^\[\]]*)\]", RegexOptions.Compiled);
    private static readonly Regex Token = new(@"\{(\w+)\}", RegexOptions.Compiled);

    /// <summary>
    /// Renders <paramref name="template"/>. Returns null when the result is too short for Discord,
    /// so callers can simply omit the field.
    /// </summary>
    public static string? Render(string? template, TemplateContext ctx)
    {
        if (string.IsNullOrWhiteSpace(template))
            return null;

        // Resolve optional sections first: keep the inner text only if all its tokens have values.
        var text = OptionalSection.Replace(template, m =>
        {
            var inner = m.Groups[1].Value;
            return Token.Matches(inner).Any(t => Lookup(t.Groups[1].Value, ctx).Length == 0)
                ? string.Empty
                : inner;
        });

        text = Token.Replace(text, m => Lookup(m.Groups[1].Value, ctx));
        text = Regex.Replace(text, @"\s{2,}", " ").Trim();

        if (text.Length < MinLength)
            return null;

        return text.Length > MaxLength ? text[..(MaxLength - 1)] + "…" : text;
    }

    /// <summary>Unknown tokens expand to nothing so a typo never leaks "{flie}" into a public profile.</summary>
    private static string Lookup(string token, TemplateContext ctx) => token.ToLowerInvariant() switch
    {
        "app" => ctx.App,
        "process" => ctx.Process,
        "title" => ctx.Title,
        "file" => ctx.File,
        "workspace" => ctx.Workspace,
        _ => string.Empty
    };
}
