using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Brendigo.USBKey.Windows;

/// <summary>
/// Informational onboarding. It intentionally does not change Windows sign-in or require a USB.
/// </summary>
public partial class SplashWindow : Window
{
    private int _step;
    private readonly bool _openDashboard;

    public SplashWindow(bool openDashboard = true)
    {
        _openDashboard = openDashboard;
        InitializeComponent();
        ShowStep(0);
    }

    private void ShowStep(int index)
    {
        _step = Math.Clamp(index, 0, 3);
        StepNumber.Text = $"KORAK {_step + 1} OD 4";

        StackPanel[] pages = { WelcomePage, RecoveryPage, UsbPage, FinishPage };
        Button[] nav = { WelcomeNav, RecoveryNav, UsbNav, FinishNav };
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].Visibility = i == _step ? Visibility.Visible : Visibility.Collapsed;
            nav[i].Background = i == _step
                ? new SolidColorBrush(Color.FromRgb(15, 107, 255))
                : Brushes.Transparent;
        }

        BackButton.IsEnabled = _step != 0;
        NextButton.Content = _step == 3
            ? _openDashboard ? "Otvori nadzornu ploču" : "Zatvori vodič"
            : "Nastavi";
    }

    private void Step_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && int.TryParse(button.Tag as string, out int index))
            ShowStep(index);
    }

    private void Back_Click(object sender, RoutedEventArgs e) => ShowStep(_step - 1);

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        if (_step != 3) { ShowStep(_step + 1); return; }

        if (_openDashboard)
        {
            var main = new MainWindow();
            Application.Current.MainWindow = main;
            main.Show();
        }

        Close();
    }

    private void SignIn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:signinoptions")
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Nije moguće otvoriti Windows postavke",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
