using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using DiscordRPC;
using DiscordStatusUpdater.Models;
using DiscordStatusUpdater.Services;

namespace DiscordStatusUpdater.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly DiscordRpcService _rpcService;
    private readonly SettingsService _settingsService;

    // ── Connection ──────────────────────────────────────────────────────────
    private string _applicationId = string.Empty;
    private bool _isConnected;
    private bool _isConnecting;
    private string _connectionStatusText = "Not connected";
    private string _connectionStatusColor = "#F23F43";

    // ── Preset ──────────────────────────────────────────────────────────────
    private ActivityPreset? _selectedPreset;
    private string _currentPresetName = string.Empty;

    // ── Activity fields ──────────────────────────────────────────────────────
    private string _details = string.Empty;
    private string _state = string.Empty;
    private string _largeImageKey = string.Empty;
    private string _largeImageText = string.Empty;
    private string _smallImageKey = string.Empty;
    private string _smallImageText = string.Empty;
    private bool _showElapsedTime = true;
    private string _button1Label = string.Empty;
    private string _button1Url = string.Empty;
    private string _button2Label = string.Empty;
    private string _button2Url = string.Empty;

    // ── Status bar ───────────────────────────────────────────────────────────
    private string _statusBarMessage = "Ready — enter your Discord Application ID to get started.";

    // ── Properties ───────────────────────────────────────────────────────────
    public string ApplicationId
    {
        get => _applicationId;
        set { _applicationId = value; OnPropertyChanged(); }
    }

    public bool IsConnected
    {
        get => _isConnected;
        private set
        {
            _isConnected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanEditApplicationId));
        }
    }

    public bool IsConnecting
    {
        get => _isConnecting;
        private set
        {
            _isConnecting = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanEditApplicationId));
        }
    }

    public bool CanEditApplicationId => !IsConnected && !IsConnecting;

    public string ConnectionStatusText
    {
        get => _connectionStatusText;
        private set { _connectionStatusText = value; OnPropertyChanged(); }
    }

    public string ConnectionStatusColor
    {
        get => _connectionStatusColor;
        private set { _connectionStatusColor = value; OnPropertyChanged(); }
    }

    public ObservableCollection<ActivityPreset> Presets { get; } = new();

    public ActivityPreset? SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            if (_selectedPreset == value) return;
            _selectedPreset = value;
            OnPropertyChanged();
            if (value != null) ApplyPreset(value);
        }
    }

    public string CurrentPresetName
    {
        get => _currentPresetName;
        set { _currentPresetName = value; OnPropertyChanged(); }
    }

    public string Details
    {
        get => _details;
        set { _details = value; OnPropertyChanged(); }
    }

    public string State
    {
        get => _state;
        set { _state = value; OnPropertyChanged(); }
    }

    public string LargeImageKey
    {
        get => _largeImageKey;
        set { _largeImageKey = value; OnPropertyChanged(); }
    }

    public string LargeImageText
    {
        get => _largeImageText;
        set { _largeImageText = value; OnPropertyChanged(); }
    }

    public string SmallImageKey
    {
        get => _smallImageKey;
        set { _smallImageKey = value; OnPropertyChanged(); }
    }

    public string SmallImageText
    {
        get => _smallImageText;
        set { _smallImageText = value; OnPropertyChanged(); }
    }

    public bool ShowElapsedTime
    {
        get => _showElapsedTime;
        set { _showElapsedTime = value; OnPropertyChanged(); }
    }

    public string Button1Label
    {
        get => _button1Label;
        set { _button1Label = value; OnPropertyChanged(); }
    }

    public string Button1Url
    {
        get => _button1Url;
        set { _button1Url = value; OnPropertyChanged(); }
    }

    public string Button2Label
    {
        get => _button2Label;
        set { _button2Label = value; OnPropertyChanged(); }
    }

    public string Button2Url
    {
        get => _button2Url;
        set { _button2Url = value; OnPropertyChanged(); }
    }

    public string StatusBarMessage
    {
        get => _statusBarMessage;
        private set { _statusBarMessage = value; OnPropertyChanged(); }
    }

    // ── Commands ─────────────────────────────────────────────────────────────
    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand UpdateActivityCommand { get; }
    public ICommand ClearActivityCommand { get; }
    public ICommand SavePresetCommand { get; }
    public ICommand DeletePresetCommand { get; }
    public ICommand NewPresetCommand { get; }
    public ICommand OpenDevPortalCommand { get; }

    // ── Constructor ──────────────────────────────────────────────────────────
    public MainViewModel()
    {
        _rpcService = new DiscordRpcService();
        _settingsService = new SettingsService();

        _rpcService.Connected += OnRpcConnected;
        _rpcService.Disconnected += OnRpcDisconnected;
        _rpcService.ErrorOccurred += OnRpcError;

        ConnectCommand = new RelayCommand(
            Connect,
            () => !IsConnected && !IsConnecting && !string.IsNullOrWhiteSpace(ApplicationId));

        DisconnectCommand = new RelayCommand(
            Disconnect,
            () => IsConnected || IsConnecting);

        UpdateActivityCommand = new RelayCommand(
            UpdateActivity,
            () => IsConnected);

        ClearActivityCommand = new RelayCommand(
            ClearActivity,
            () => IsConnected);

        SavePresetCommand = new RelayCommand(
            SavePreset,
            () => !string.IsNullOrWhiteSpace(CurrentPresetName));

        DeletePresetCommand = new RelayCommand(
            DeletePreset,
            () => SelectedPreset != null);

        NewPresetCommand = new RelayCommand(NewPreset);

        OpenDevPortalCommand = new RelayCommand(
            () => OpenUrl("https://discord.com/developers/applications"));

        LoadData();
    }

    // ── Data loading ─────────────────────────────────────────────────────────
    private void LoadData()
    {
        var settings = _settingsService.LoadSettings();
        ApplicationId = settings.ApplicationId;

        foreach (var preset in _settingsService.LoadPresets())
            Presets.Add(preset);

        if (Presets.Count == 0) return;

        var last = Presets.FirstOrDefault(p => p.Name == settings.LastPresetName)
                   ?? Presets[0];
        SelectedPreset = last;
        CurrentPresetName = last.Name;
    }

    // ── Connection ────────────────────────────────────────────────────────────
    private void Connect()
    {
        try
        {
            IsConnecting = true;
            SetStatus("Connecting...", "#F0B232");
            StatusBarMessage = "Connecting to Discord...";
            _rpcService.Connect(ApplicationId.Trim());

            var s = _settingsService.LoadSettings();
            s.ApplicationId = ApplicationId.Trim();
            _settingsService.SaveSettings(s);
        }
        catch (Exception ex)
        {
            IsConnecting = false;
            SetStatus("Connection failed", "#F23F43");
            StatusBarMessage = $"Connection failed: {ex.Message}";
        }
    }

    private void Disconnect()
    {
        _rpcService.Disconnect();
        IsConnected = false;
        IsConnecting = false;
        SetStatus("Not connected", "#F23F43");
        StatusBarMessage = "Disconnected.";
    }

    // ── Activity ─────────────────────────────────────────────────────────────
    private void UpdateActivity()
    {
        try
        {
            _rpcService.SetPresence(BuildPresence());
            StatusBarMessage = "Activity updated!";
        }
        catch (Exception ex)
        {
            StatusBarMessage = $"Error: {ex.Message}";
        }
    }

    private void ClearActivity()
    {
        try
        {
            _rpcService.ClearPresence();
            StatusBarMessage = "Activity cleared.";
        }
        catch (Exception ex)
        {
            StatusBarMessage = $"Error: {ex.Message}";
        }
    }

    private RichPresence BuildPresence()
    {
        var presence = new RichPresence
        {
            Details = NullIfEmpty(Details),
            State = NullIfEmpty(State)
        };

        if (!string.IsNullOrWhiteSpace(LargeImageKey) || !string.IsNullOrWhiteSpace(SmallImageKey))
        {
            presence.Assets = new Assets
            {
                LargeImageKey = NullIfEmpty(LargeImageKey),
                LargeImageText = NullIfEmpty(LargeImageText),
                SmallImageKey = NullIfEmpty(SmallImageKey),
                SmallImageText = NullIfEmpty(SmallImageText)
            };
        }

        if (ShowElapsedTime)
            presence.Timestamps = Timestamps.Now;

        var buttons = new List<DiscordRPC.Button>();
        if (!string.IsNullOrWhiteSpace(Button1Label) && IsValidUrl(Button1Url))
            buttons.Add(new DiscordRPC.Button { Label = Button1Label.Trim(), Url = Button1Url.Trim() });
        if (!string.IsNullOrWhiteSpace(Button2Label) && IsValidUrl(Button2Url))
            buttons.Add(new DiscordRPC.Button { Label = Button2Label.Trim(), Url = Button2Url.Trim() });
        if (buttons.Count > 0)
            presence.Buttons = buttons.ToArray();

        return presence;
    }

    // ── Presets ───────────────────────────────────────────────────────────────
    private void SavePreset()
    {
        var preset = new ActivityPreset
        {
            Name = CurrentPresetName.Trim(),
            Details = Details,
            State = State,
            LargeImageKey = LargeImageKey,
            LargeImageText = LargeImageText,
            SmallImageKey = SmallImageKey,
            SmallImageText = SmallImageText,
            ShowElapsedTime = ShowElapsedTime,
            Button1Label = Button1Label,
            Button1Url = Button1Url,
            Button2Label = Button2Label,
            Button2Url = Button2Url
        };

        var existing = Presets.Select((p, i) => (p, i)).FirstOrDefault(x => x.p.Name == preset.Name);
        if (existing.p != null)
        {
            Presets[existing.i] = preset;
        }
        else
        {
            Presets.Add(preset);
        }

        SelectedPreset = preset;
        _settingsService.SavePresets(Presets.ToList());
        SaveLastPreset(preset.Name);
        StatusBarMessage = $"Preset \"{preset.Name}\" saved.";
    }

    private void DeletePreset()
    {
        if (SelectedPreset == null) return;
        var name = SelectedPreset.Name;
        Presets.Remove(SelectedPreset);
        SelectedPreset = Presets.Count > 0 ? Presets[0] : null;
        CurrentPresetName = SelectedPreset?.Name ?? string.Empty;
        _settingsService.SavePresets(Presets.ToList());
        StatusBarMessage = $"Preset \"{name}\" deleted.";
    }

    private void NewPreset()
    {
        _selectedPreset = null;
        OnPropertyChanged(nameof(SelectedPreset));
        CurrentPresetName = "New Preset";
        Details = string.Empty;
        State = string.Empty;
        LargeImageKey = string.Empty;
        LargeImageText = string.Empty;
        SmallImageKey = string.Empty;
        SmallImageText = string.Empty;
        ShowElapsedTime = true;
        Button1Label = string.Empty;
        Button1Url = string.Empty;
        Button2Label = string.Empty;
        Button2Url = string.Empty;
        StatusBarMessage = "New preset ready. Fill in the fields and click Save.";
    }

    private void ApplyPreset(ActivityPreset p)
    {
        CurrentPresetName = p.Name;
        Details = p.Details;
        State = p.State;
        LargeImageKey = p.LargeImageKey;
        LargeImageText = p.LargeImageText;
        SmallImageKey = p.SmallImageKey;
        SmallImageText = p.SmallImageText;
        ShowElapsedTime = p.ShowElapsedTime;
        Button1Label = p.Button1Label;
        Button1Url = p.Button1Url;
        Button2Label = p.Button2Label;
        Button2Url = p.Button2Url;
        SaveLastPreset(p.Name);
    }

    // ── RPC event handlers ────────────────────────────────────────────────────
    private void OnRpcConnected(object? sender, EventArgs e)
    {
        Dispatch(() =>
        {
            IsConnected = true;
            IsConnecting = false;
            SetStatus("Connected", "#23A559");
            StatusBarMessage = "Connected to Discord!";
        });
    }

    private void OnRpcDisconnected(object? sender, EventArgs e)
    {
        Dispatch(() =>
        {
            IsConnected = false;
            IsConnecting = false;
            SetStatus("Not connected", "#F23F43");
            StatusBarMessage = "Disconnected from Discord.";
        });
    }

    private void OnRpcError(object? sender, string message)
    {
        Dispatch(() =>
        {
            IsConnecting = false;
            SetStatus("Error", "#F23F43");
            StatusBarMessage = $"Discord error: {message}";
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void SetStatus(string text, string color)
    {
        ConnectionStatusText = text;
        ConnectionStatusColor = color;
    }

    private void SaveLastPreset(string name)
    {
        var s = _settingsService.LoadSettings();
        s.LastPresetName = name;
        _settingsService.SaveSettings(s);
    }

    private static void Dispatch(Action a) =>
        Application.Current?.Dispatcher.Invoke(a);

    private static string? NullIfEmpty(string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static bool IsValidUrl(string url) =>
        !string.IsNullOrWhiteSpace(url) &&
        (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
         url.StartsWith("http://", StringComparison.OrdinalIgnoreCase));

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { /* ignore */ }
    }

    // ── INotifyPropertyChanged ────────────────────────────────────────────────
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void Dispose() => _rpcService.Dispose();
}
