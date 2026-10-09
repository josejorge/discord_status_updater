// File: ProcessScanner.cs
// Description: Enumerates running processes (name, window title, start time, focus) via System.Diagnostics and Win32.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-10-09
// Last edit date: 2026-10-09
// Version: 1.0.0

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using DiscordStatusUpdater.Models;

namespace DiscordStatusUpdater.Services;

/// <summary>Reads live process information. All reads are best-effort: protected processes are skipped.</summary>
public static class ProcessScanner
{
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int maxCount);

    /// <summary>
    /// Snapshots only the processes whose normalised name is in <paramref name="wantedNames"/>.
    /// Reading titles and start times is comparatively expensive, so everything else is skipped.
    /// </summary>
    public static IReadOnlyList<RunningProcess> Scan(ISet<string> wantedNames)
    {
        var result = new List<RunningProcess>();
        if (wantedNames.Count == 0) return result;

        var (foregroundPid, foregroundTitle) = GetForeground();

        foreach (var p in Process.GetProcesses())
        {
            try
            {
                if (!wantedNames.Contains(RuleMatcher.NormalizeName(p.ProcessName))) continue;

                var isForeground = p.Id == foregroundPid;
                // Electron apps (VS Code, Wave) own many processes but only one has a window. When the app
                // has focus, the focused window's title is the freshest; otherwise use its main window.
                var title = isForeground && foregroundTitle.Length > 0 ? foregroundTitle : p.MainWindowTitle;

                result.Add(new RunningProcess(p.ProcessName, p.Id, title ?? string.Empty, TryStartTime(p), isForeground));
            }
            catch
            {
                // Process exited mid-scan or access denied: not interesting for presence.
            }
            finally
            {
                p.Dispose();
            }
        }

        return result;
    }

    /// <summary>
    /// Lists apps that currently show a window, one per process name, for the "add from running app" picker.
    /// </summary>
    public static IReadOnlyList<RunningProcess> ListWindowedApps()
    {
        var apps = new Dictionary<string, RunningProcess>(StringComparer.OrdinalIgnoreCase);
        var (foregroundPid, _) = GetForeground();

        foreach (var p in Process.GetProcesses())
        {
            try
            {
                if (p.MainWindowHandle == IntPtr.Zero || string.IsNullOrWhiteSpace(p.MainWindowTitle)) continue;
                if (p.Id == Environment.ProcessId) continue; // do not offer ourselves
                if (apps.ContainsKey(p.ProcessName)) continue;

                apps[p.ProcessName] = new RunningProcess(
                    p.ProcessName, p.Id, p.MainWindowTitle, TryStartTime(p), p.Id == foregroundPid);
            }
            catch { /* skip inaccessible processes */ }
            finally { p.Dispose(); }
        }

        return apps.Values.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Process id and window title of whatever currently has keyboard focus.</summary>
    private static (int Pid, string Title) GetForeground()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return (-1, string.Empty);

        GetWindowThreadProcessId(hwnd, out var pid);
        var sb = new StringBuilder(512);
        GetWindowText(hwnd, sb, sb.Capacity);
        return ((int)pid, sb.ToString());
    }

    private static DateTime? TryStartTime(Process p)
    {
        try { return p.StartTime.ToUniversalTime(); }
        catch { return null; } // access denied for elevated/system processes
    }
}
