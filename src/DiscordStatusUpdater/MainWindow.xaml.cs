using System.ComponentModel;
using System.Windows;
using DiscordStatusUpdater.Services;
using DiscordStatusUpdater.ViewModels;

namespace DiscordStatusUpdater;

public partial class MainWindow : Window
{
    private TrayIconService? _trayService;
    private bool _forceClose;

    public MainWindow()
    {
        InitializeComponent();
    }

    public void SetTrayService(TrayIconService service)
    {
        _trayService = service;
    }

    public void ForceClose()
    {
        _forceClose = true;
        Close();
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        if (WindowState == WindowState.Minimized && Vm.MinimizeToTray)
        {
            Hide();
            _trayService?.ShowBalloonTip("Running in the system tray. Double-click the icon to open.");
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_forceClose && Vm.MinimizeToTray)
        {
            // Treat window close as "hide to tray" unless this is a real exit request
            e.Cancel = true;
            Hide();
            _trayService?.ShowBalloonTip("Still running. Use the tray menu to exit.");
            return;
        }

        Vm.Dispose();
        base.OnClosing(e);
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (Vm.MinimizeToTray)
        {
            Hide();
            _trayService?.ShowBalloonTip("Minimized to tray.");
        }
        else
        {
            WindowState = WindowState.Minimized;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        _forceClose = true;
        Application.Current.Shutdown();
    }

    private MainViewModel Vm => (MainViewModel)DataContext;
}
