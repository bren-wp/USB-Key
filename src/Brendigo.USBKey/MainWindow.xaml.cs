using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Brendigo.USBKey.Models;
using Brendigo.USBKey.Services;
using Brendigo.USBKey.Windows;

namespace Brendigo.USBKey;

public partial class MainWindow : Window
{
    private readonly UsbKeyManager _manager;
    private readonly DispatcherTimer _timer;
    private string _page = "Dashboard";

    public MainWindow()
    {
        InitializeComponent();
        try { _manager = new UsbKeyManager(); }
        catch (Exception ex)
        {
            MessageBox.Show("Sigurnosni registar nije moguće otvoriti. Program neće mijenjati Windowsovu prijavu.\n\n" +
                ex.Message, "Brendigo USB Key", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }

        AutoLockCheck.IsChecked = _manager.AutoLock;
        StartupCheck.IsChecked = StartupService.IsEnabled();
        ShowPage("Dashboard");
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _timer.Tick += (_, _) => RefreshStatus();
        _timer.Start();
        RefreshStatus();
    }

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string page)
            ShowPage(page);
    }

    private void ShowPage(string page)
    {
        _page = page;
        DashboardPage.Visibility = page == "Dashboard" ? Visibility.Visible : Visibility.Collapsed;
        KeysPage.Visibility = page == "Keys" ? Visibility.Visible : Visibility.Collapsed;
        RecoveryPage.Visibility = page == "Recovery" ? Visibility.Visible : Visibility.Collapsed;
        SecurityPage.Visibility = page == "Security" ? Visibility.Visible : Visibility.Collapsed;
        SettingsPage.Visibility = page == "Settings" ? Visibility.Visible : Visibility.Collapsed;
        AboutPage.Visibility = page == "About" ? Visibility.Visible : Visibility.Collapsed;

        (string title, string subtitle) = page switch
        {
            "Keys" => ("USB ključevi", "Dodaj, pregledaj, preimenuj ili opozovi pristup pojedinog sticka."),
            "Recovery" => ("Rezervni pristup", "Ako USB zakaže, Windowsova uobičajena prijava ostaje dostupna."),
            "Security" => ("Sigurnosni dnevnik", "Lokalni pregled rada i promjena USB ključeva."),
            "Settings" => ("Postavke", "Prilagodi zaključavanje i pokretanje aplikacije."),
            "About" => ("O programu", "Brendigo USB Key — podaci ostaju na ovom računalu."),
            _ => ("Sigurnosna nadzorna ploča", "Pregled registriranih i trenutačno spojenih USB ključeva.")
        };
        PageTitle.Text = title;
        PageSubtitle.Text = subtitle;
        foreach (var button in new[] { DashboardNav, KeysNav, RecoveryNav, SecurityNav, SettingsNav, AboutNav })
            button.Background = (button.Tag as string) == page
                ? new SolidColorBrush(Color.FromRgb(15, 107, 255))
                : Brushes.Transparent;
        if (page == "Security") EventLogBox.Text = _manager.ReadEvents();
    }

    private void RefreshStatus()
    {
        try
        {
            var connected = _manager.Scan();
            var connectedIds = connected.Select(k => k.Id).ToHashSet();
            var rows = _manager.Registered.Select(k => new KeyRow
            {
                Id = k.Id,
                Name = k.Name,
                Status = connectedIds.Contains(k.Id) ? "● Spojen" : "○ Nije spojen",
                Registered = k.RegisteredUtc.ToLocalTime().ToString("dd.MM.yyyy."),
                LastSeen = k.LastSeenUtc?.ToLocalTime().ToString("dd.MM.yyyy. HH:mm") ?? "Nikad"
            }).ToList();

            // Preserve selected row after periodic refresh.
            Guid? selected = (KeysGrid.SelectedItem as KeyRow)?.Id;
            KeysGrid.ItemsSource = rows;
            RecentKeys.ItemsSource = rows;
            if (selected.HasValue) KeysGrid.SelectedItem = rows.FirstOrDefault(r => r.Id == selected.Value);

            RegisteredCount.Text = rows.Count.ToString();
            ConnectedCount.Text = connected.Count.ToString();
            AutoLockState.Text = _manager.AutoLock ? "Uključeno" : "Isključeno";
            StatusMessage.Text = connected.Count > 0
                ? "Prepoznati USB ključevi: " + string.Join(", ", connected.Select(k => k.Name))
                : rows.Count > 0
                    ? "Nijedan registrirani USB ključ trenutačno nije spojen."
                    : "Još nema registriranih ključeva. Za početak umetni USB stick.";
            if (_page == "Security") EventLogBox.Text = _manager.ReadEvents();
        }
        catch (Exception ex)
        {
            _timer.Stop();
            MessageBox.Show("Provjera USB ključeva nije uspjela. Nadzor je privremeno zaustavljen.\n\n" +
                ex.Message, "Brendigo USB Key", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Enroll_Click(object sender, RoutedEventArgs e)
    {
        var drives = UsbKeyManager.AvailableDrives();
        if (drives.Count == 0)
        {
            MessageBox.Show(this,
                "Nije pronađen spreman izmjenjivi USB pogon. Umetni običan USB stick i pokušaj ponovno.",
                "USB uređaj nije pronađen", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var dialog = new EnrollDialog(drives) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        try
        {
            _manager.Enroll(dialog.SelectedDrive, dialog.KeyName);
            RefreshStatus();
            ShowPage("Keys");
            MessageBox.Show(this, "USB ključ uspješno je registriran. Registriraj drugi fizički stick za rezervu.",
                "Registracija uspješna", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { ShowError("Registracija nije uspjela", ex); }
    }

    private void Rename_Click(object sender, RoutedEventArgs e)
    {
        if (KeysGrid.SelectedItem is not KeyRow item) { RequireSelection(); return; }
        var dialog = new NameDialog(item.Name) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        try { _manager.Rename(item.Id, dialog.NewName); RefreshStatus(); }
        catch (Exception ex) { ShowError("Preimenovanje nije uspjelo", ex); }
    }

    private void Revoke_Click(object sender, RoutedEventArgs e)
    {
        if (KeysGrid.SelectedItem is not KeyRow item) { RequireSelection(); return; }
        var confirmation = MessageBox.Show(this,
            "Opozvati ključ \"" + item.Name + "\" na ovom računalu?\n\n" +
            "USB datoteka ostat će na sticku, ali ovaj Windows račun više joj neće vjerovati. " +
            "Provjeri svoj rezervni način prijave prije opoziva.",
            "Potvrda opoziva", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirmation != MessageBoxResult.Yes) return;
        try { _manager.Revoke(item.Id); RefreshStatus(); }
        catch (Exception ex) { ShowError("Opoziv nije uspio", ex); }
    }

    private void ManualRefresh_Click(object sender, RoutedEventArgs e) => RefreshStatus();

    private void LockNow_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, "Windows će se odmah zaključati. Provjeri da poznaješ Windows lozinku ili Hello PIN. Nastaviti?",
            "Zaključavanje Windowsa", MessageBoxButton.YesNo,
            MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try { _manager.LockNow(); }
        catch (Exception ex) { ShowError("Zaključavanje nije uspjelo", ex); }
    }

    private void TestRecovery_Click(object sender, RoutedEventArgs e) => LockNow_Click(sender, e);

    private void OpenSignInSettings_Click(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("ms-settings:signinoptions") { UseShellExecute = true }); }
        catch (Exception ex) { ShowError("Nije moguće otvoriti Windows postavke", ex); }
    }

    private void OpenDataFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", "/select,\"" + _manager.EventsPath + "\"")
            { UseShellExecute = true });
        }
        catch (Exception ex) { ShowError("Nije moguće otvoriti mapu zapisnika", ex); }
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (AutoLockCheck.IsChecked == true && !_manager.AutoLock)
            {
                if (MessageBox.Show(this,
                    "Prije uključivanja potvrdi da znaš svoju Windows lozinku ili Hello PIN. " +
                    "Aplikacija zaključava sesiju nakon uklanjanja zadnjeg aktivnog USB ključa, " +
                    "ali ga još NE može koristiti za otključavanje Windows prijave.\n\nUključiti?",
                    "Sigurnosna potvrda", MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
                {
                    AutoLockCheck.IsChecked = false;
                    return;
                }
            }
            StartupService.SetEnabled(StartupCheck.IsChecked == true);
            _manager.AutoLock = AutoLockCheck.IsChecked == true;
            RefreshStatus();
            MessageBox.Show(this, "Postavke su spremljene.", "Brendigo USB Key",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { ShowError("Spremanje postavki nije uspjelo", ex); }
    }

    private void ShowWelcome_Click(object sender, RoutedEventArgs e)
    {
        var welcome = new SplashWindow(openDashboard: false) { Owner = this };
        welcome.ShowDialog();
    }

    private void RequireSelection() =>
        MessageBox.Show(this, "Prvo odaberi USB ključ na popisu.", "Brendigo USB Key",
            MessageBoxButton.OK, MessageBoxImage.Information);

    private void ShowError(string title, Exception ex) =>
        MessageBox.Show(this, ex.Message, title, MessageBoxButton.OK, MessageBoxImage.Error);
}
