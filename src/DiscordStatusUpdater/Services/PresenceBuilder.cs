// File: PresenceBuilder.cs
// Description: Converts an ActivityPreset (with optional template context) into a Discord RichPresence object.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-10-09
// Last edit date: 2026-10-09
// Version: 1.0.0

using DiscordRPC;
using DiscordStatusUpdater.Models;

namespace DiscordStatusUpdater.Services;

/// <summary>Shared by the manual "Update Status" button and the automatic detection path.</summary>
public static class PresenceBuilder
{
    /// <param name="preset">Source of all text, image and button fields; text may contain template tokens.</param>
    /// <param name="ctx">Values for the tokens. <see cref="TemplateContext.Empty"/> for manual updates.</param>
    /// <param name="startUtc">Elapsed-timer start; null means "now". Ignored if the preset hides elapsed time.</param>
    public static RichPresence Build(ActivityPreset preset, TemplateContext ctx, DateTime? startUtc = null)
    {
        var presence = new RichPresence
        {
            Details = TemplateEngine.Render(preset.Details, ctx),
            State = TemplateEngine.Render(preset.State, ctx)
        };

        if (!string.IsNullOrWhiteSpace(preset.LargeImageKey) || !string.IsNullOrWhiteSpace(preset.SmallImageKey))
        {
            presence.Assets = new Assets
            {
                LargeImageKey = NullIfEmpty(preset.LargeImageKey),
                LargeImageText = TemplateEngine.Render(preset.LargeImageText, ctx),
                SmallImageKey = NullIfEmpty(preset.SmallImageKey),
                SmallImageText = TemplateEngine.Render(preset.SmallImageText, ctx)
            };
        }

        if (preset.ShowElapsedTime)
            presence.Timestamps = startUtc.HasValue ? new Timestamps(startUtc.Value) : Timestamps.Now;

        var buttons = new List<Button>();
        if (!string.IsNullOrWhiteSpace(preset.Button1Label) && IsValidUrl(preset.Button1Url))
            buttons.Add(new Button { Label = preset.Button1Label.Trim(), Url = preset.Button1Url.Trim() });
        if (!string.IsNullOrWhiteSpace(preset.Button2Label) && IsValidUrl(preset.Button2Url))
            buttons.Add(new Button { Label = preset.Button2Label.Trim(), Url = preset.Button2Url.Trim() });
        if (buttons.Count > 0)
            presence.Buttons = buttons.ToArray();

        return presence;
    }

    public static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    public static bool IsValidUrl(string? url) =>
        !string.IsNullOrWhiteSpace(url) &&
        (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
         url.StartsWith("http://", StringComparison.OrdinalIgnoreCase));
}
