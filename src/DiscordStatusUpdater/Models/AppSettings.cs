// File: AppSettings.cs
// Description: Persisted application settings (Discord Application ID, tray/startup toggles, auto-detect options).
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-06-01
// Last edit date: 2026-10-09
// Version: 1.1.0

namespace DiscordStatusUpdater.Models;

public class AppSettings
{
    public string ApplicationId { get; set; } = string.Empty;
    public string LastPresetName { get; set; } = string.Empty;
    public bool MinimizeToTray { get; set; } = true;
    public bool StartMinimized { get; set; } = false;
    public bool OpenWithWindows { get; set; } = false;

    // ── Automatic detection ──────────────────────────────────────────────────

    /// <summary>Master switch: watch running apps and drive the status from the detection rules.</summary>
    public bool AutoDetectEnabled { get; set; } = false;

    /// <summary>How often the process list is scanned, in seconds.</summary>
    public int PollIntervalSeconds { get; set; } = 3;

    /// <summary>When several rules match, prefer the app that currently has keyboard focus.</summary>
    public bool PreferFocusedApp { get; set; } = true;

    /// <summary>Clear the status when none of the rules match any running app.</summary>
    public bool ClearWhenIdle { get; set; } = true;

    /// <summary>Allow file/workspace names taken from window titles to appear in the status.</summary>
    public bool ShareWindowTitles { get; set; } = true;
}
