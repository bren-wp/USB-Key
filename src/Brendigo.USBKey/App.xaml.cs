using System.Threading;
using System.Windows;
using Brendigo.USBKey.Windows;
using Brendigo.USBKey.Services;

namespace Brendigo.USBKey;

public partial class App : Application
{
    private Mutex? _singleInstance;

    protected override void OnStartup(StartupEventArgs e)
    {
        _singleInstance = new Mutex(true, @"Local\BrendigoUSBKey.OnlyOneUI", out bool isFirst);
        if (!isFirst)
        {
            _singleInstance.Dispose();
            _singleInstance = null;
            MessageBox.Show("Brendigo USB Key već radi. Otvori ga dvoklikom na ikonu u Windows obavijesnom području.", "Brendigo USB Key",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show("Dogodila se neočekivana pogreška. Windowsova prijava nije promijenjena.\n\n" +
                args.Exception.Message, "Brendigo USB Key", MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };
        base.OnStartup(e);

        // Auto-start should enable USB monitoring immediately, without blocking
        // on onboarding. A manual launch opens the four-step welcome screen.
        bool autoStart = e.Args.Any(arg =>
            string.Equals(arg, "--autostart", StringComparison.OrdinalIgnoreCase));
        // A previously configured account must never be blocked by onboarding.
        // The USB registry is only a marker for opening the dashboard, never proof of login.
        bool existingConfiguration = File.Exists(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Brendigo", "USBKey", "registry.dat"));
        if (autoStart || existingConfiguration || OnboardingState.IsCompleted())
        {
            var dashboard = new MainWindow();
            MainWindow = dashboard;
            dashboard.Show();
            if (autoStart)
            {
                dashboard.WindowState = WindowState.Minimized;
                dashboard.ShowInTaskbar = false;
                dashboard.Hide();
            }
        }
        else
        {
            var welcome = new SplashWindow();
            MainWindow = welcome;
            welcome.Show();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_singleInstance != null)
        {
            _singleInstance.ReleaseMutex();
            _singleInstance.Dispose();
        }
        base.OnExit(e);
    }
}
