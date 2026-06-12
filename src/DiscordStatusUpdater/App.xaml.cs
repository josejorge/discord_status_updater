using System.Windows;
using DiscordStatusUpdater.Services;
using DiscordStatusUpdater.ViewModels;
using FormsApp = System.Windows.Forms.Application;

namespace DiscordStatusUpdater;

public partial class App : Application
{
    private TrayIconService? _trayService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        FormsApp.EnableVisualStyles();

        DispatcherUnhandledException += (_, ex) =>
        {
            MessageBox.Show($"An unexpected error occurred:\n\n{ex.Exception.Message}",
                "Discord Status Updater — Error", MessageBoxButton.OK, MessageBoxImage.Error);
            ex.Handled = true;
        };

        var icon = LoadTrayIcon();
        _trayService = new TrayIconService(icon);

        var window = new MainWindow();
        window.SetTrayService(_trayService);
        MainWindow = window;

        // Wire tray actions → window / VM
        _trayService.ShowWindowRequested += (_, _) => Dispatcher.Invoke(() =>
        {
            window.Show();
            window.WindowState = WindowState.Normal;
            window.Activate();
        });

        _trayService.ExitRequested += (_, _) => Dispatcher.Invoke(() =>
            window.ForceClose());

        _trayService.UpdateStatusRequested += (_, _) => Dispatcher.Invoke(() =>
        {
            var vm = (MainViewModel)window.DataContext;
            if (vm.UpdateActivityCommand.CanExecute(null))
                vm.UpdateActivityCommand.Execute(null);
        });

        _trayService.ClearStatusRequested += (_, _) => Dispatcher.Invoke(() =>
        {
            var vm = (MainViewModel)window.DataContext;
            if (vm.ClearActivityCommand.CanExecute(null))
                vm.ClearActivityCommand.Execute(null);
        });

        // Start minimized if the user requested it
        var vm2 = (MainViewModel)window.DataContext;
        if (vm2.StartMinimized)
            window.WindowState = WindowState.Minimized;

        window.Show();
        // If Minimized + MinimizeToTray, OnStateChanged in MainWindow will hide to tray
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayService?.Dispose();
        base.OnExit(e);
    }

    private static System.Drawing.Icon? LoadTrayIcon()
    {
        try
        {
            var stream = GetResourceStream(new Uri("pack://application:,,,/Assets/icon.ico"))?.Stream;
            return stream != null ? new System.Drawing.Icon(stream) : null;
        }
        catch { return null; }
    }
}
