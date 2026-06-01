using System.Windows;

namespace DiscordStatusUpdater;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, ex) =>
        {
            MessageBox.Show(
                $"An unexpected error occurred:\n\n{ex.Exception.Message}",
                "Discord Status Updater — Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            ex.Handled = true;
        };
    }
}
