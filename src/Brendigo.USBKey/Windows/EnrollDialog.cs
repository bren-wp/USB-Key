using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brendigo.USBKey.Services;

namespace Brendigo.USBKey.Windows;

/// <summary>USB enrollment only; Windows credentials are never requested.</summary>
public sealed class EnrollDialog : Window
{
    private static readonly Brush Navy = new SolidColorBrush(Color.FromRgb(11, 45, 92));
    private static readonly Brush Muted = new SolidColorBrush(Color.FromRgb(81, 112, 145));
    private readonly ComboBox _drives;
    private readonly TextBox _name;
    private readonly TextBlock _status;

    public string SelectedDrive => _drives.SelectedItem as string ?? "";
    public string KeyName => _name.Text.Trim();

    public EnrollDialog(IReadOnlyList<string> drives)
    {
        Title = "Registracija USB-a — Brendigo USB Key";
        Icon = BitmapFrame.Create(new Uri("pack://application:,,,/Assets/brendigo-usb-key.ico"));
        Width = 610;
        Height = 525;
        MinWidth = 520;
        MinHeight = 490;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(242, 246, 250));
        FontFamily = new FontFamily("Segoe UI");

        var layout = new Grid
        {
            Background = new SolidColorBrush(Color.FromRgb(242, 246, 250))
        };
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Content = layout;

        var header = new Border
        {
            Background = Navy,
            Padding = new Thickness(26, 19, 26, 19)
        };
        var hero = new StackPanel { Orientation = Orientation.Horizontal };
        header.Child = hero;
        hero.Children.Add(new Image
        {
            Source = BitmapFrame.Create(new Uri("pack://application:,,,/Assets/brendigo-usb-key.ico")),
            Width = 61, Height = 61, Stretch = Stretch.Uniform,
            Margin = new Thickness(0, 0, 17, 0)
        });
        var headerText = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        headerText.Children.Add(new TextBlock
        {
            Text = "Registriraj novi USB", Foreground = Brushes.White,
            FontSize = 24, FontWeight = FontWeights.Bold
        });
        headerText.Children.Add(new TextBlock
        {
            Text = "Brendigo USB Key · lokalna registracija",
            Foreground = new SolidColorBrush(Color.FromRgb(163, 226, 236)),
            FontSize = 13, Margin = new Thickness(0, 5, 0, 0)
        });
        hero.Children.Add(headerText);
        Grid.SetRow(header, 0);
        layout.Children.Add(header);

        var content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        Grid.SetRow(content, 1);
        layout.Children.Add(content);
        var fields = new StackPanel { Margin = new Thickness(26, 25, 26, 18) };
        content.Content = fields;

        fields.Children.Add(new TextBlock
        {
            Text = "Odaberi fizički USB stick i dodijeli mu jedinstven naziv.",
            FontSize = 14, Foreground = Muted,
            TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 20)
        });

        fields.Children.Add(new TextBlock
        {
            Text = "USB pogon", FontWeight = FontWeights.SemiBold,
            Foreground = Navy, FontSize = 14
        });

        var driveRow = new Grid { Margin = new Thickness(0, 7, 0, 7) };
        driveRow.ColumnDefinitions.Add(new ColumnDefinition());
        driveRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        fields.Children.Add(driveRow);

        _drives = new ComboBox
        {
            Height = 43, FontSize = 15,
            Padding = new Thickness(10, 4, 10, 4),
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(185, 211, 233))
        };
        System.Windows.Automation.AutomationProperties.SetName(_drives, "Odaberi izmjenjivi USB pogon");
        driveRow.Children.Add(_drives);
        var refresh = new Button
        {
            Content = "Osvježi",
            Width = 108, Margin = new Thickness(10, 0, 0, 0),
            Style = (Style)Application.Current.FindResource("SecondaryButton")
        };
        refresh.Click += (_, _) => RefreshDrives();
        Grid.SetColumn(refresh, 1);
        driveRow.Children.Add(refresh);

        _status = new TextBlock
        {
            Foreground = Muted, FontSize = 12, Margin = new Thickness(0, 0, 0, 18)
        };
        fields.Children.Add(_status);

        fields.Children.Add(new TextBlock
        {
            Text = "Naziv USB ključa", FontWeight = FontWeights.SemiBold,
            Foreground = Navy, FontSize = 14
        });

        _name = new TextBox
        {
            Text = "Moj USB ključ", Height = 43,
            Padding = new Thickness(11, 8, 11, 8),
            FontSize = 15, Margin = new Thickness(0, 7, 0, 17),
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(185, 211, 233)),
            MaxLength = 64
        };
        System.Windows.Automation.AutomationProperties.SetName(_name, "Jedinstven naziv USB ključa");
        fields.Children.Add(_name);

        fields.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(230, 243, 255)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(184, 219, 248)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(11),
            Padding = new Thickness(14),
            Child = new TextBlock
            {
                Text = "Svaki fizički stick registriraj zasebno. Datoteka običnog USB ključa može se kopirati; ovime se ne omogućuje prijava u zaključani Windows.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = Navy, FontSize = 12.5
            }
        });

        var footer = new Border
        {
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(215, 231, 243)),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(26, 17, 26, 17)
        };
        Grid.SetRow(footer, 2);
        layout.Children.Add(footer);

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        footer.Child = actions;
        var cancel = new Button
        {
            Content = "Odustani", Width = 120, IsCancel = true,
            Margin = new Thickness(0, 0, 10, 0),
            Style = (Style)Application.Current.FindResource("SecondaryButton")
        };
        cancel.Click += (_, _) => DialogResult = false;
        actions.Children.Add(cancel);

        var register = new Button
        {
            Content = "Registriraj USB", Width = 173, IsDefault = true,
            Style = (Style)Application.Current.FindResource("PrimaryButton")
        };
        register.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(SelectedDrive) || KeyName.Length is < 1 or > 64)
            {
                MessageBox.Show(this, "Odaberi USB pogon i naziv od 1 do 64 znaka.",
                    "Provjera podataka", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
        };
        actions.Children.Add(register);
        Populate(drives);
    }

    private void RefreshDrives()
    {
        try { Populate(UsbKeyManager.AvailableDrives()); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _status.Text = "Osvježavanje USB popisa nije uspjelo: " + ex.Message;
        }
    }

    private void Populate(IReadOnlyList<string> drives)
    {
        string? old = _drives.SelectedItem as string;
        _drives.Items.Clear();
        foreach (string drive in drives) _drives.Items.Add(drive);
        if (old != null && _drives.Items.Contains(old))
            _drives.SelectedItem = old;
        else if (drives.Count > 0)
            _drives.SelectedIndex = 0;

        _status.Text = drives.Count == 0
            ? "Nema spremnih izmjenjivih USB pogona. Umetni stick pa odaberi Osvježi."
            : $"Pronađeni spremni izmjenjivi pogoni: {drives.Count}.";
    }
}

public sealed class NameDialog : Window
{
    private static readonly Brush Navy = new SolidColorBrush(Color.FromRgb(11, 45, 92));
    private static readonly Brush Muted = new SolidColorBrush(Color.FromRgb(81, 112, 145));
    private readonly TextBox _name;
    public string NewName => _name.Text.Trim();

    public NameDialog(string original)
    {
        Title = "Preimenuj USB ključ — Brendigo USB Key";
        Icon = BitmapFrame.Create(new Uri("pack://application:,,,/Assets/brendigo-usb-key.ico"));
        Width = 480; Height = 270;
        MinWidth = 420; MinHeight = 255;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(242, 246, 250));
        FontFamily = new FontFamily("Segoe UI");

        var root = new Grid
        {
            Background = new SolidColorBrush(Color.FromRgb(242, 246, 250))
        };
        var panel = new StackPanel { Margin = new Thickness(26) };
        root.Children.Add(panel);
        Content = root;

        panel.Children.Add(new TextBlock
        {
            Text = "Preimenuj registrirani USB",
            FontSize = 23, FontWeight = FontWeights.Bold, Foreground = Navy,
            Margin = new Thickness(0, 0, 0, 10)
        });
        panel.Children.Add(new TextBlock
        {
            Text = "Promjena naziva ne mijenja token niti prava pristupa.",
            Foreground = Muted, TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 15)
        });
        _name = new TextBox
        {
            Text = original, MaxLength = 64,
            Height = 43, Padding = new Thickness(10, 9, 10, 9),
            FontSize = 15, BorderBrush = new SolidColorBrush(Color.FromRgb(185, 211, 233))
        };
        System.Windows.Automation.AutomationProperties.SetName(_name, "Novi naziv USB ključa");
        panel.Children.Add(_name);

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 20, 0, 0)
        };
        panel.Children.Add(actions);
        var cancel = new Button
        {
            Content = "Odustani", IsCancel = true,
            Style = (Style)Application.Current.FindResource("SecondaryButton"),
            Width = 110, Margin = new Thickness(0, 0, 10, 0)
        };
        cancel.Click += (_, _) => DialogResult = false;
        actions.Children.Add(cancel);
        var save = new Button
        {
            Content = "Spremi naziv", IsDefault = true, Width = 146,
            Style = (Style)Application.Current.FindResource("PrimaryButton")
        };
        save.Click += (_, _) =>
        {
            if (NewName.Length is >= 1 and <= 64)
                DialogResult = true;
            else
                MessageBox.Show(this, "Naziv mora sadržavati od 1 do 64 znaka.",
                    "Provjera naziva", MessageBoxButton.OK, MessageBoxImage.Warning);
        };
        actions.Children.Add(save);
    }
}
