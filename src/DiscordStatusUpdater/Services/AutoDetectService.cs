// File: AutoDetectService.cs
// Description: Background poller that watches running apps and reports debounced changes of the winning rule.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-10-09
// Last edit date: 2026-10-09
// Version: 1.0.1

using DiscordStatusUpdater.Models;

namespace DiscordStatusUpdater.Services;

/// <summary>
/// Polls the process list on a background thread and raises events only when something meaningful changes.
/// Events fire on a thread-pool thread; subscribers must marshal to the UI themselves.
/// </summary>
public sealed class AutoDetectService : IDisposable
{
    /// <summary>A change must be seen on this many consecutive scans before it is reported.</summary>
    private const int StableScansRequired = 2;

    private readonly Func<IReadOnlyList<DetectionRule>> _rules;
    private readonly Func<bool> _preferForeground;
    private readonly Func<ISet<string>, IReadOnlyList<RunningProcess>> _scanner;
    private CancellationTokenSource? _cts;

    // Debounce state: the candidate being confirmed and how many scans in a row it has been seen.
    private string? _pendingKey;
    private int _pendingCount;
    private string? _committedKey;
    private DetectionMatch? _committed;

    /// <summary>The (debounced) winner changed; null means nothing matches any more.</summary>
    public event EventHandler<DetectionMatch?>? DetectionChanged;

    /// <summary>The winner is unchanged but its window title changed (e.g. a different file is open).</summary>
    public event EventHandler<DetectionMatch>? DetectionRefreshed;

    /// <summary>Every scan, with all currently matching rules (winner included) for the dashboard list.</summary>
    public event EventHandler<(IReadOnlyList<DetectionMatch> Matches, DetectionMatch? Winner)>? Scanned;

    public bool IsRunning => _cts != null;

    /// <param name="scanner">Process source; defaults to the real scanner, replaced by a fake in tests.</param>
    public AutoDetectService(
        Func<IReadOnlyList<DetectionRule>> rules,
        Func<bool> preferForeground,
        Func<ISet<string>, IReadOnlyList<RunningProcess>>? scanner = null)
    {
        _rules = rules;
        _preferForeground = preferForeground;
        _scanner = scanner ?? ProcessScanner.Scan;
    }

    public void Start(TimeSpan interval)
    {
        Stop();
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => LoopAsync(interval, _cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _pendingKey = null;
        _pendingCount = 0;
        _committedKey = null;
        _committed = null;
    }

    private async Task LoopAsync(TimeSpan interval, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(interval);
        do
        {
            try { ScanOnce(); }
            catch { /* a failed scan must never kill the watcher; try again next tick */ }
        }
        while (await WaitNextAsync(timer, ct));
    }

    private static async Task<bool> WaitNextAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try { return await timer.WaitForNextTickAsync(ct); }
        catch (OperationCanceledException) { return false; }
    }

    /// <summary>Runs one scan and applies the debounce rules. Public so tests/UI can force a refresh.</summary>
    public void ScanOnce()
    {
        var rules = _rules();
        var wanted = rules
            .Where(r => r.Enabled)
            .SelectMany(r => RuleMatcher.ParseProcessNames(r.ProcessNames))
            .ToHashSet();

        var running = _scanner(wanted);
        var matches = RuleMatcher.FindMatches(rules, running);
        var winner = RuleMatcher.PickWinner(matches, _preferForeground());

        Scanned?.Invoke(this, (matches, winner));

        // Identity of the winner: which rule fired for which process (null key = idle).
        var key = winner == null ? null : $"{winner.Rule.Id}:{winner.Process.Pid}";

        if (key == _committedKey)
        {
            // Same winner. Report a title change without waiting, then drop any half-confirmed switch.
            _pendingKey = null;
            _pendingCount = 0;
            if (winner != null && _committed != null && winner.Process.WindowTitle != _committed.Process.WindowTitle)
            {
                _committed = winner;
                DetectionRefreshed?.Invoke(this, winner);
            }
            return;
        }

        // A different winner: require it to hold for a couple of scans so brief focus changes
        // and short-lived helper windows do not make the status flap.
        if (key == _pendingKey)
            _pendingCount++;
        else
        {
            _pendingKey = key;
            _pendingCount = 1;
        }

        if (_pendingCount < StableScansRequired) return;

        _committedKey = key;
        _committed = winner;
        _pendingKey = null;
        _pendingCount = 0;
        DetectionChanged?.Invoke(this, winner);
    }

    public void Dispose() => Stop();
}
