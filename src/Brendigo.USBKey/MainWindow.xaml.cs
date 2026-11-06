using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Navigation;
using Microsoft.Win32;
using Brendigo.USBKey.Models;
using Brendigo.USBKey.Services;
using Brendigo.USBKey.Windows;

namespace Brendigo.USBKey;

public partial class MainWindow : Window
{
    private readonly UsbKeyManager _manager;
    private readonly DispatcherTimer _timer;
    private TrayController? _tray;
    private bool _exitRequested;
    private string _rawEvents = "";
    private string _page = "Dashboard";

    public MainWindow() : this(visualPreview: false) { }

    // Only used by the Windows visual regression runner; no tray or USB polling.
    public MainWindow(bool visualPreview)
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
        SizeChanged += (_, _) => UpdateResponsiveLayout();
        LayoutRoot.SizeChanged += (_, _) => UpdateResponsiveLayout();
        Loaded += (_, _) => UpdateResponsiveLayout();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _timer.Tick += (_, _) => RefreshStatus();
        if (!visualPreview)
        {
            _timer.Start();
            RefreshStatus();
            _tray = new TrayController(ShowFromTray, LockFromTray, ExitFromTray);
            Application.Current.SessionEnding += OnWindowsSessionEnding;
        }
        else
        {
            RegisteredCount.Text = _manager.Registered.Count.ToString();
            RecoveryKeysStatus.Text = _manager.Registered.Count < 2
                ? "Za rezervu preporučujemo najmanje dva neovisna USB sticka."
                : $"Registriranih USB ključeva: {_manager.Registered.Count}.";
            ConnectedCount.Text = "0";
            AutoLockState.Text = _manager.AutoLock ? "Uključeno" : "Isključeno";
            StatusMessage.Text = "Nema priključenih USB ključeva na testnom računalu.";
        }
    }

    private void ShowFromTray()
    {
        ShowInTaskbar = true;
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void LockFromTray()
    {
        if (MessageBox.Show("Zaključati Windows sada? Za povratak će trebati Windows lozinka ili Hello.",
            "Brendigo USB Key", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try { _manager.LockNow(); }
        catch (Exception ex) { ShowError("Zaključavanje nije uspjelo", ex); }
    }

    private void ExitFromTray()
    {
        _exitRequested = true;
        Close();
    }

    private void OnWindowsSessionEnding(object sender, SessionEndingCancelEventArgs e)
        => _exitRequested = true;

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_exitRequested && _tray != null &&
            !Application.Current.Dispatcher.HasShutdownStarted)
        {
            e.Cancel = true;
            Hide();
            _tray.ShowBackgroundHint();
            return;
        }

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        Application.Current.SessionEnding -= OnWindowsSessionEnding;
        _timer.Stop();
        _tray?.Dispose();
        base.OnClosed(e);
    }

    private void UpdateResponsiveLayout()
    {
        // RenderTargetBitmap sizes the WPF root without showing a native Window.
        // Use the arranged root width for reliable desktop and responsive previews.
        double width = LayoutRoot.ActualWidth > 0 ? LayoutRoot.ActualWidth : Width;
        SidebarColumn.Width = new GridLength(width < 1090 ? 206 : 242);
        LocalStatusBadge.Visibility = width < 1110
            ? Visibility.Collapsed : Visibility.Visible;

        // Reflow all metric cards and the quick-actions panel rather than
        // squeezing important controls into unreadable columns on small windows.
        bool compact = width < 1150;
        for (int i = 0; i < DashboardMetricsGrid.ColumnDefinitions.Count; i++)
            DashboardMetricsGrid.ColumnDefinitions[i].Width =
                compact && i > 0 ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        Grid.SetColumn(RegisteredMetricCard, 0);
        Grid.SetRow(RegisteredMetricCard, 0);
        Grid.SetColumn(ConnectedMetricCard, compact ? 0 : 1);
        Grid.SetRow(ConnectedMetricCard, compact ? 1 : 0);
        Grid.SetColumn(AutoLockMetricCard, compact ? 0 : 2);
        Grid.SetRow(AutoLockMetricCard, compact ? 2 : 0);

        DashboardPanelsGrid.ColumnDefinitions[1].Width =
            compact ? new GridLength(0) : new GridLength(290);
        Grid.SetColumn(KeysSummaryCard, 0);
        Grid.SetRow(KeysSummaryCard, 0);
        Grid.SetColumn(QuickActionsCard, compact ? 0 : 1);
        Grid.SetRow(QuickActionsCard, compact ? 1 : 0);
    }

    private void KeysGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        bool selected = KeysGrid.SelectedItem is KeyRow;
        RenameKeyButton.IsEnabled = selected;
        RevokeKeyButton.IsEnabled = selected;
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
        if (page == "Security") RefreshSecurityLog();
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

            KeysEmptyHint.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            RegisteredCount.Text = rows.Count.ToString();
            RecoveryKeysStatus.Text = rows.Count switch
            {
                0 => "Nema registriranih USB ključeva. Preporučujemo dva zasebna sticka.",
                1 => "Registriran je 1 USB. Dodaj još jedan fizički stick kao rezervu.",
                _ => $"Registriranih USB ključeva: {rows.Count}. Čuvaj rezervni stick na odvojenoj lokaciji."
            };
            ConnectedCount.Text = connected.Count.ToString();
            AutoLockState.Text = _manager.AutoLock ? "Uključeno" : "Isključeno";
            StatusMessage.Text = connected.Count > 0
                ? "Prepoznati USB ključevi: " + string.Join(", ", connected.Select(k => k.Name))
                : rows.Count > 0
                    ? "Nijedan registrirani USB ključ trenutačno nije spojen."
                    : "Još nema registriranih ključeva. Za početak umetni USB stick.";
            if (_page == "Security") RefreshSecurityLog();
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
        // Let the registration dialog remain open while a USB is inserted, then refresh.
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
        var confirmation = new RevokeDialog(item.Name) { Owner = this };
        if (confirmation.ShowDialog() != true) return;
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

    private void RefreshSecurityLog()
    {
        try
        {
            _rawEvents = _manager.ReadEvents();
            ApplySecurityLogFilter();
        }
        catch (Exception ex)
        {
            EventCountText.Text = "Greška pri čitanju dnevnika";
            EventLogBox.Text = "Nije moguće učitati sigurnosni dnevnik: " + ex.Message;
        }
    }

    private void ApplySecurityLogFilter()
    {
        string query = EventSearchBox.Text.Trim();
        var lines = _rawEvents
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line != "Još nema sigurnosnih događaja.");
        if (query.Length > 0)
            lines = lines.Where(line =>
                line.Contains(query, StringComparison.CurrentCultureIgnoreCase));
        var filtered = lines.ToArray();
        EventCountText.Text = filtered.Length == 0
            ? "Nema događaja za prikaz."
            : $"Prikazano događaja: {filtered.Length}";
        EventLogBox.Text = filtered.Length == 0
            ? "Nema sigurnosnih događaja koji odgovaraju kriteriju."
            : string.Join(Environment.NewLine, filtered);
    }

    private void EventSearch_TextChanged(object sender, TextChangedEventArgs e)
        => ApplySecurityLogFilter();

    private void RefreshLog_Click(object sender, RoutedEventArgs e) => RefreshSecurityLog();

    private void ExportLog_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RefreshSecurityLog();
            if (string.IsNullOrWhiteSpace(_rawEvents) ||
                _rawEvents == "Još nema sigurnosnih događaja.")
            {
                MessageBox.Show(this, "Nema događaja za izvoz.", "Sigurnosni dnevnik",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (MessageBox.Show(this,
                "Izvoz uključuje i prethodnu arhivu sačuvanih događaja, a ne samo " +
                "250 redaka na ekranu. Sadrži nazive USB ključeva i vremena događaja. " +
                "TXT datoteka neće biti šifrirana. Želiš li nastaviti?",
                "Privatnost izvoza", MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            var dialog = new SaveFileDialog
            {
                Title = "Izvoz sigurnosnog dnevnika",
                FileName = $"Brendigo-USB-Key-Dnevnik-{DateTime.Now:yyyy-MM-dd}.txt",
                Filter = "Tekstualna datoteka (*.txt)|*.txt",
                DefaultExt = ".txt",
                AddExtension = true,
                OverwritePrompt = true
            };
            if (dialog.ShowDialog(this) != true) return;
            int exported = _manager.ExportEvents(dialog.FileName);
            MessageBox.Show(this,
                $"Izvezeno je {exported} sačuvanih događaja (uključujući posljednju arhivu).",
                "Brendigo USB Key",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { ShowError("Izvoz dnevnika nije uspio", ex); }
    }

    private void OpenDataFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string folder = Path.GetDirectoryName(_manager.EventsPath)
                ?? throw new IOException("Mapa zapisnika nije dostupna.");
            Process.Start(new ProcessStartInfo("explorer.exe", "\"" + folder + "\"")
            { UseShellExecute = true });
        }
        catch (Exception ex) { ShowError("Nije moguće otvoriti mapu zapisnika", ex); }
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        bool originalAutoLock = _manager.AutoLock;
        bool originalStartup = StartupService.IsEnabled();
        bool desiredAutoLock = AutoLockCheck.IsChecked == true;
        bool desiredStartup = StartupCheck.IsChecked == true;

        if (desiredAutoLock && !originalAutoLock &&
            MessageBox.Show(this,
                "Potvrdi da znaš Windows lozinku ili Hello PIN. USB trenutno ne može " +
                "otključati zaslon za prijavu. Automatsko zaključavanje reagira samo " +
                "dok Brendigo USB Key radi. Želiš li uključiti zaštitu?",
                "Sigurnosna potvrda", MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            AutoLockCheck.IsChecked = originalAutoLock;
            StartupCheck.IsChecked = originalStartup;
            return;
        }

        try
        {
            if (originalAutoLock != desiredAutoLock)
                _manager.AutoLock = desiredAutoLock;
            if (originalStartup != desiredStartup)
                StartupService.SetEnabled(desiredStartup);

            RefreshStatus();
            MessageBox.Show(this, "Postavke su spremljene.", "Brendigo USB Key",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            // Best-effort rollback if the second setting could not be written.
            // Always reflect actual persisted values rather than the requested ones.
            var rollbackErrors = new List<string>();
            try
            {
                if (_manager.AutoLock != originalAutoLock)
                    _manager.AutoLock = originalAutoLock;
            }
            catch (Exception rollback) { rollbackErrors.Add(rollback.Message); }
            try
            {
                if (StartupService.IsEnabled() != originalStartup)
                    StartupService.SetEnabled(originalStartup);
            }
            catch (Exception rollback) { rollbackErrors.Add(rollback.Message); }

            AutoLockCheck.IsChecked = _manager.AutoLock;
            try { StartupCheck.IsChecked = StartupService.IsEnabled(); }
            catch { StartupCheck.IsChecked = originalStartup; }

            string details = rollbackErrors.Count == 0 ? "" :
                "\n\nDio promjena nije bilo moguće vratiti:\n" + string.Join("\n", rollbackErrors);
            ShowError("Spremanje postavki nije uspjelo",
                new InvalidOperationException(ex.Message + details, ex));
        }
    }

    private void OpenBrendigoWebsite_Click(object sender, RequestNavigateEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); }
        catch (Exception ex) { ShowError("Nije moguće otvoriti Brendigo web stranicu", ex); }
        e.Handled = true;
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
