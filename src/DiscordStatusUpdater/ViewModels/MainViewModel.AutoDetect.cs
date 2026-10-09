// File: MainViewModel.AutoDetect.cs
// Description: Auto-detect half of the main view-model: rules editing, detector lifecycle, live status and navigation.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-10-09
// Last edit date: 2026-10-09
// Version: 1.0.0

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using DiscordRPC;
using DiscordStatusUpdater.Models;
using DiscordStatusUpdater.Services;

namespace DiscordStatusUpdater.ViewModels;

/// <summary>One row of the dashboard's "Detected now" list.</summary>
public sealed record MatchItem(string Name, string Title, bool IsWinner);

/// <summary>One entry of the "add rule from a running app" picker.</summary>
public sealed record RunningAppItem(string ProcessName, string WindowTitle)
{
    public string Display => $"{ProcessName}   ·   {WindowTitle}";
}

public sealed partial class MainViewModel
{
    /// <summary>Discord throttles activity updates; text-only refreshes are spaced at least this far apart.</summary>
    private static readonly TimeSpan MinRefreshInterval = TimeSpan.FromSeconds(15);

    /// <summary>Delay before rule edits are written to disk, so typing does not hit the disk per keystroke.</summary>
    private static readonly TimeSpan RulesSaveDelay = TimeSpan.FromMilliseconds(600);

    private AutoDetectService? _detector;
    private DispatcherTimer? _uiTimer;

    // Rules are read by the detector's background thread, so it gets an immutable snapshot.
    private volatile IReadOnlyList<DetectionRule> _rulesSnapshot = Array.Empty<DetectionRule>();
    private DateTime? _rulesDirtySince;

    // What auto-detect most recently decided / sent.
    private DetectionMatch? _winner;
    private string? _lastAutoKey;
    private DateTime _lastPushUtc = DateTime.MinValue;
    private (string AppId, RichPresence Presence, string Key)? _pendingPush;
    private bool _autoPushed;

    // ── Settings backing fields ──────────────────────────────────────────────
    private bool _autoDetectEnabled;
    private bool _preferFocusedApp = true;
    private bool _clearWhenIdle = true;
    private bool _shareWindowTitles = true;
    private int _pollIntervalSeconds = 3;

    // ── Navigation ───────────────────────────────────────────────────────────
    private string _currentPage = "Dashboard";

    // ── Rules editing ────────────────────────────────────────────────────────
    private DetectionRule? _selectedRule;
    private RunningAppItem? _selectedRunningApp;
    private bool _regexValid = true;

    // ── Live card ────────────────────────────────────────────────────────────
    private string _liveAppName = "Nothing detected";
    private string _liveDetails = string.Empty;
    private string _liveState = string.Empty;
    private string _liveElapsed = string.Empty;
    private string _liveSource = "Waiting for a tracked app to start…";
    private string _liveInitials = "?";

    public ObservableCollection<DetectionRule> Rules { get; } = new();
    public ObservableCollection<MatchItem> Matches { get; } = new();
    public ObservableCollection<RunningAppItem> RunningApps { get; } = new();

    public ICommand NavigateCommand { get; private set; } = null!;
    public ICommand NewRuleCommand { get; private set; } = null!;
    public ICommand DeleteRuleCommand { get; private set; } = null!;
    public ICommand MoveRuleUpCommand { get; private set; } = null!;
    public ICommand MoveRuleDownCommand { get; private set; } = null!;
    public ICommand RefreshRunningAppsCommand { get; private set; } = null!;
    public ICommand AddRuleFromAppCommand { get; private set; } = null!;
    public ICommand ToggleAutoDetectCommand { get; private set; } = null!;

    // ── Properties: navigation ───────────────────────────────────────────────
    public string CurrentPage
    {
        get => _currentPage;
        set { _currentPage = value; OnPropertyChanged(); }
    }

    // ── Properties: auto-detect settings ─────────────────────────────────────
    public bool AutoDetectEnabled
    {
        get => _autoDetectEnabled;
        set
        {
            if (_autoDetectEnabled == value) return;
            _autoDetectEnabled = value;
            OnPropertyChanged();
            QuickSave(s => s.AutoDetectEnabled = value);
            ApplyDetectorState();
        }
    }

    public bool PreferFocusedApp
    {
        get => _preferFocusedApp;
        set { _preferFocusedApp = value; OnPropertyChanged(); QuickSave(s => s.PreferFocusedApp = value); }
    }

    public bool ClearWhenIdle
    {
        get => _clearWhenIdle;
        set { _clearWhenIdle = value; OnPropertyChanged(); QuickSave(s => s.ClearWhenIdle = value); }
    }

    public bool ShareWindowTitles
    {
        get => _shareWindowTitles;
        set
        {
            _shareWindowTitles = value;
            OnPropertyChanged();
            QuickSave(s => s.ShareWindowTitles = value);
            _lastAutoKey = null; // re-render the current status with/without file names
            RefreshLive();
            if (AutoDetectEnabled) ApplyWinner(force: true);
        }
    }

    public int PollIntervalSeconds
    {
        get => _pollIntervalSeconds;
        set
        {
            _pollIntervalSeconds = Math.Clamp(value, 1, 30);
            OnPropertyChanged();
            QuickSave(s => s.PollIntervalSeconds = _pollIntervalSeconds);
            if (AutoDetectEnabled) ApplyDetectorState(); // restart with the new interval
        }
    }

    // ── Properties: rules editing ────────────────────────────────────────────
    public DetectionRule? SelectedRule
    {
        get => _selectedRule;
        set
        {
            if (ReferenceEquals(_selectedRule, value)) return;
            _selectedRule = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedRule));
            RegexValid = value == null || RuleMatcher.IsValidTitleRegex(value.TitleRegex);
        }
    }

    public bool HasSelectedRule => _selectedRule != null;

    public RunningAppItem? SelectedRunningApp
    {
        get => _selectedRunningApp;
        set { _selectedRunningApp = value; OnPropertyChanged(); }
    }

    /// <summary>False when the selected rule's title pattern is not a valid regular expression.</summary>
    public bool RegexValid
    {
        get => _regexValid;
        private set { _regexValid = value; OnPropertyChanged(); }
    }

    // ── Properties: live card ────────────────────────────────────────────────
    public bool HasDetection => _winner != null;
    public string LiveAppName { get => _liveAppName; private set { _liveAppName = value; OnPropertyChanged(); } }
    public string LiveDetails { get => _liveDetails; private set { _liveDetails = value; OnPropertyChanged(); } }
    public string LiveState { get => _liveState; private set { _liveState = value; OnPropertyChanged(); } }
    public string LiveElapsed { get => _liveElapsed; private set { _liveElapsed = value; OnPropertyChanged(); } }
    public string LiveSource { get => _liveSource; private set { _liveSource = value; OnPropertyChanged(); } }
    public string LiveInitials { get => _liveInitials; private set { _liveInitials = value; OnPropertyChanged(); } }

    // ── Properties: preset editor preview (uses sample values for tokens) ────
    public string PreviewDetails => TemplateEngine.Render(Details, TemplateContext.Sample) ?? string.Empty;
    public string PreviewState => TemplateEngine.Render(State, TemplateContext.Sample) ?? string.Empty;

    // ── Setup ────────────────────────────────────────────────────────────────
    private void InitializeAutoDetect()
    {
        var settings = _settingsService.LoadSettings();
        _autoDetectEnabled = settings.AutoDetectEnabled;
        _preferFocusedApp = settings.PreferFocusedApp;
        _clearWhenIdle = settings.ClearWhenIdle;
        _shareWindowTitles = settings.ShareWindowTitles;
        _pollIntervalSeconds = Math.Clamp(settings.PollIntervalSeconds, 1, 30);

        NavigateCommand = new RelayCommand(p => CurrentPage = p?.ToString() ?? "Dashboard");
        NewRuleCommand = new RelayCommand(NewRule);
        DeleteRuleCommand = new RelayCommand(DeleteRule, () => SelectedRule != null);
        MoveRuleUpCommand = new RelayCommand(() => MoveRule(-1), () => SelectedRule != null && Rules.IndexOf(SelectedRule) > 0);
        MoveRuleDownCommand = new RelayCommand(() => MoveRule(1), () => SelectedRule != null && Rules.IndexOf(SelectedRule) < Rules.Count - 1);
        RefreshRunningAppsCommand = new RelayCommand(RefreshRunningApps);
        AddRuleFromAppCommand = new RelayCommand(AddRuleFromApp, () => SelectedRunningApp != null);
        ToggleAutoDetectCommand = new RelayCommand(() => AutoDetectEnabled = !AutoDetectEnabled);

        _detector = new AutoDetectService(() => _rulesSnapshot, () => PreferFocusedApp);
        _detector.DetectionChanged += (_, m) => DispatchAsync(() => OnDetectionChanged(m));
        _detector.DetectionRefreshed += (_, m) => DispatchAsync(() => OnDetectionRefreshed(m));
        _detector.Scanned += (_, e) => DispatchAsync(() => OnScanned(e.Matches, e.Winner));

        // One-second heartbeat: elapsed clock, throttled presence pushes and debounced rule saving.
        _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _uiTimer.Tick += (_, _) => OnUiTick();
        _uiTimer.Start();

        RefreshRunningApps();
        ApplyDetectorState();
    }

    private void LoadRulesFromDisk()
    {
        var presets = Presets.ToList();
        var rules = _settingsService.LoadRules(presets, out var presetsChanged);

        if (presetsChanged)
        {
            foreach (var p in presets.Where(p => Presets.All(x => x.Name != p.Name)))
                Presets.Add(p);
            _settingsService.SavePresets(Presets.ToList());
        }

        foreach (var rule in rules)
            AddRuleToCollection(rule);

        SelectedRule = Rules.FirstOrDefault();
        PublishRulesSnapshot();
    }

    private void AddRuleToCollection(DetectionRule rule)
    {
        rule.PropertyChanged += OnRulePropertyChanged;
        Rules.Add(rule);
    }

    private void OnRulePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DetectionRule.TitleRegex) && ReferenceEquals(sender, SelectedRule))
            RegexValid = RuleMatcher.IsValidTitleRegex(SelectedRule!.TitleRegex);

        MarkRulesDirty();
    }

    // ── Detector lifecycle ───────────────────────────────────────────────────
    private void ApplyDetectorState()
    {
        if (_detector == null) return;

        if (AutoDetectEnabled)
        {
            _detector.Start(TimeSpan.FromSeconds(PollIntervalSeconds));
            StatusBarMessage = "Auto-detect on — watching for your apps.";
        }
        else
        {
            _detector.Stop();
            _winner = null;
            _pendingPush = null;
            _lastAutoKey = null;
            Matches.Clear();
            if (_autoPushed)
            {
                try { _rpcService.ClearPresence(); } catch { /* not connected */ }
                _autoPushed = false;
            }
            RefreshLive();
            StatusBarMessage = "Auto-detect off.";
        }
        OnPropertyChanged(nameof(AutoDetectStatusText));
    }

    public string AutoDetectStatusText => AutoDetectEnabled ? "Auto-detect is ON" : "Auto-detect is OFF";

    private void DisposeAutoDetect()
    {
        _uiTimer?.Stop();
        _detector?.Dispose();
        FlushRules();
    }

    // ── Detector events (UI thread) ──────────────────────────────────────────
    private void OnScanned(IReadOnlyList<DetectionMatch> matches, DetectionMatch? winner)
    {
        if (!AutoDetectEnabled) return;

        Matches.Clear();
        foreach (var m in matches)
            Matches.Add(new MatchItem(
                m.Rule.Name,
                string.IsNullOrWhiteSpace(m.Process.WindowTitle) ? m.Process.Name : m.Process.WindowTitle,
                winner != null && ReferenceEquals(m, winner)));
    }

    private void OnDetectionChanged(DetectionMatch? match)
    {
        if (!AutoDetectEnabled) return;
        _winner = match;
        RefreshLive();

        if (match == null)
        {
            // Nothing tracked is running any more.
            _pendingPush = null;
            _lastAutoKey = null;
            if (_autoPushed && ClearWhenIdle)
            {
                try { _rpcService.ClearPresence(); } catch { /* not connected */ }
                _autoPushed = false;
                StatusBarMessage = "No tracked app running — status cleared.";
            }
            return;
        }

        ApplyWinner(force: true);
    }

    private void OnDetectionRefreshed(DetectionMatch match)
    {
        if (!AutoDetectEnabled) return;
        _winner = match;
        RefreshLive();
        ApplyWinner(force: false);
    }

    /// <summary>After a (re)connect, resend the current auto status that the new connection has not seen.</summary>
    private void OnConnectedForAutoDetect()
    {
        if (!AutoDetectEnabled || _winner == null) return;
        _lastAutoKey = null;
        ApplyWinner(force: true);
    }

    // ── Applying a detection to Discord ──────────────────────────────────────
    private void ApplyWinner(bool force)
    {
        var match = _winner;
        if (match == null) return;

        var rule = match.Rule;
        var preset = Presets.FirstOrDefault(p => string.Equals(p.Name, rule.PresetName, StringComparison.OrdinalIgnoreCase));
        if (preset == null)
        {
            StatusBarMessage = $"Rule \"{rule.Name}\" points to a preset that does not exist — pick one in Auto-Detect.";
            return;
        }

        var appId = !string.IsNullOrWhiteSpace(rule.ApplicationId) ? rule.ApplicationId.Trim() : ApplicationId.Trim();
        if (string.IsNullOrWhiteSpace(appId))
        {
            StatusBarMessage = "Enter a Discord Application ID on the Dashboard to start auto-detect.";
            return;
        }

        var presence = PresenceBuilder.Build(preset, BuildContext(match), match.Process.StartTimeUtc);
        var key = $"{appId}|{rule.Id}|{match.Process.Pid}|{presence.Details}|{presence.State}";
        if (key == _lastAutoKey) return;

        // Switching apps is pushed straight away; a mere title change waits for the rate-limit window.
        if (!force && DateTime.UtcNow - _lastPushUtc < MinRefreshInterval)
        {
            _pendingPush = (appId, presence, key);
            return;
        }

        PushAuto(appId, presence, key, rule.Name);
    }

    private void PushAuto(string appId, RichPresence presence, string key, string ruleName)
    {
        try
        {
            PushPresence(appId, presence);
            _lastAutoKey = key;
            _pendingPush = null;
            _autoPushed = true;
            StatusBarMessage = $"Auto-detect: showing \"{ruleName}\".";
        }
        catch (Exception ex)
        {
            StatusBarMessage = $"Auto-detect error: {ex.Message}";
        }
    }

    /// <summary>Sends a presence, (re)connecting first if the live connection uses a different Application ID.</summary>
    private void PushPresence(string appId, RichPresence presence)
    {
        if (!string.Equals(_rpcService.CurrentApplicationId, appId, StringComparison.Ordinal))
        {
            IsConnected = false;
            IsConnecting = true;
            SetStatus("Connecting...", "#F0B232");
            _rpcService.Connect(appId);
        }

        _rpcService.SetPresence(presence);
        _lastPushUtc = DateTime.UtcNow;
    }

    private TemplateContext BuildContext(DetectionMatch match)
    {
        if (!ShareWindowTitles)
            return new TemplateContext(match.Rule.Name, match.Process.Name, string.Empty, string.Empty, string.Empty);

        var parsed = TitleParser.Parse(match.Process.WindowTitle);
        return new TemplateContext(
            match.Rule.Name, match.Process.Name, match.Process.WindowTitle, parsed.File, parsed.Workspace);
    }

    // ── Live card ────────────────────────────────────────────────────────────
    private void RefreshLive()
    {
        OnPropertyChanged(nameof(HasDetection));
        var match = _winner;

        if (match == null)
        {
            LiveAppName = "Nothing detected";
            LiveDetails = string.Empty;
            LiveState = string.Empty;
            LiveElapsed = string.Empty;
            LiveInitials = "?";
            LiveSource = AutoDetectEnabled
                ? "Waiting for a tracked app to start…"
                : "Turn on auto-detect to follow your apps.";
            return;
        }

        var preset = Presets.FirstOrDefault(p => string.Equals(p.Name, match.Rule.PresetName, StringComparison.OrdinalIgnoreCase));
        var ctx = BuildContext(match);

        LiveAppName = match.Rule.Name;
        LiveDetails = preset == null ? string.Empty : TemplateEngine.Render(preset.Details, ctx) ?? string.Empty;
        LiveState = preset == null ? string.Empty : TemplateEngine.Render(preset.State, ctx) ?? string.Empty;
        LiveInitials = Initials(match.Rule.Name);
        LiveSource = preset == null
            ? $"Preset \"{match.Rule.PresetName}\" is missing"
            : string.IsNullOrWhiteSpace(match.Rule.ApplicationId)
                ? $"Preset: {preset.Name}"
                : $"Preset: {preset.Name}  ·  own Discord application";
        UpdateElapsed();
    }

    private void UpdateElapsed()
    {
        var start = _winner?.Process.StartTimeUtc;
        if (start == null) { LiveElapsed = string.Empty; return; }

        var t = DateTime.UtcNow - start.Value;
        if (t < TimeSpan.Zero) t = TimeSpan.Zero;
        LiveElapsed = t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00} elapsed" : $"{t.Minutes:00}:{t.Seconds:00} elapsed";
    }

    private static string Initials(string name)
    {
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length switch
        {
            0 => "?",
            1 => words[0][..Math.Min(2, words[0].Length)].ToUpperInvariant(),
            _ => $"{char.ToUpperInvariant(words[0][0])}{char.ToUpperInvariant(words[1][0])}"
        };
    }

    // ── One-second heartbeat ─────────────────────────────────────────────────
    private void OnUiTick()
    {
        if (_winner != null) UpdateElapsed();

        // Flush a title refresh that was held back by the rate limit.
        if (_pendingPush is { } pending && DateTime.UtcNow - _lastPushUtc >= MinRefreshInterval)
            PushAuto(pending.AppId, pending.Presence, pending.Key, _winner?.Rule.Name ?? string.Empty);

        if (_rulesDirtySince is { } since && DateTime.UtcNow - since >= RulesSaveDelay)
            FlushRules();
    }

    // ── Rules editing ────────────────────────────────────────────────────────
    private void NewRule()
    {
        var rule = new DetectionRule
        {
            Name = "New rule",
            PresetName = Presets.FirstOrDefault()?.Name ?? string.Empty
        };
        AddRuleToCollection(rule);
        SelectedRule = rule;
        MarkRulesDirty();
    }

    private void DeleteRule()
    {
        if (SelectedRule == null) return;
        var index = Rules.IndexOf(SelectedRule);
        SelectedRule.PropertyChanged -= OnRulePropertyChanged;
        Rules.Remove(SelectedRule);
        SelectedRule = Rules.Count == 0 ? null : Rules[Math.Min(index, Rules.Count - 1)];
        MarkRulesDirty();
    }

    private void MoveRule(int delta)
    {
        if (SelectedRule == null) return;
        var from = Rules.IndexOf(SelectedRule);
        var to = from + delta;
        if (to < 0 || to >= Rules.Count) return;

        var rule = SelectedRule;
        Rules.Move(from, to);
        SelectedRule = rule; // Move raises a reset in some bindings; keep the selection
        MarkRulesDirty();
    }

    private void RefreshRunningApps()
    {
        var apps = ProcessScanner.ListWindowedApps();
        RunningApps.Clear();
        foreach (var a in apps)
            RunningApps.Add(new RunningAppItem(a.Name, a.WindowTitle));
    }

    /// <summary>Creates a rule plus a generic preset for the app picked in the running-apps list.</summary>
    private void AddRuleFromApp()
    {
        if (SelectedRunningApp == null) return;

        var process = SelectedRunningApp.ProcessName;
        var presetName = $"Auto: {process}";

        if (Presets.All(p => p.Name != presetName))
        {
            Presets.Add(new ActivityPreset
            {
                Name = presetName,
                Details = "Using {app}",
                State = "[{workspace}]",
                LargeImageText = process,
                ShowElapsedTime = true
            });
            _settingsService.SavePresets(Presets.ToList());
        }

        var rule = new DetectionRule { Name = process, ProcessNames = process, PresetName = presetName };
        AddRuleToCollection(rule);
        SelectedRule = rule;
        MarkRulesDirty();
        StatusBarMessage = $"Rule for \"{process}\" added. Edit its preset on the Presets page.";
    }

    // ── Rules persistence ────────────────────────────────────────────────────
    private void MarkRulesDirty()
    {
        PublishRulesSnapshot();
        _rulesDirtySince = DateTime.UtcNow;
    }

    private void PublishRulesSnapshot() => _rulesSnapshot = Rules.ToArray();

    private void FlushRules()
    {
        if (_rulesDirtySince == null) return;
        _rulesDirtySince = null;
        try { _settingsService.SaveRules(Rules.ToList()); }
        catch (Exception ex) { StatusBarMessage = $"Could not save rules: {ex.Message}"; }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    /// <summary>Marshals to the UI thread without blocking the detector's background thread.</summary>
    private static void DispatchAsync(Action a) =>
        Application.Current?.Dispatcher.BeginInvoke(a);
}
