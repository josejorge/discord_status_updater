// File: RunningProcess.cs
// Description: Lightweight snapshot of a running process (name, window title, focus state) used by the rule matcher.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-10-09
// Last edit date: 2026-10-09
// Version: 1.0.0

namespace DiscordStatusUpdater.Models;

/// <summary>
/// Immutable snapshot of one process. Kept free of <c>System.Diagnostics.Process</c> so the matcher
/// can be unit-tested with hand-made data.
/// </summary>
/// <param name="Name">Process name without ".exe".</param>
/// <param name="Pid">Process id.</param>
/// <param name="WindowTitle">Title of the app's window (the focused window when the app has focus).</param>
/// <param name="StartTimeUtc">When the process started, or null if Windows denied access.</param>
/// <param name="IsForeground">True when this process owns the currently focused window.</param>
public sealed record RunningProcess(
    string Name,
    int Pid,
    string WindowTitle,
    DateTime? StartTimeUtc,
    bool IsForeground);

/// <summary>The outcome of matching: which rule fired and the process that triggered it.</summary>
public sealed record DetectionMatch(DetectionRule Rule, RunningProcess Process);
