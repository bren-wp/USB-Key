using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Brendigo.USBKey.Windows;

public sealed class EnrollDialog : Window
{
    private readonly ComboBox _drives;
    private readonly TextBox _name;

    public string SelectedDrive => (string)_drives.SelectedItem;
    public string KeyName => _name.Text.Trim();

    public EnrollDialog(IReadOnlyList<string> drives)
    {
        Title = "Registracija USB ključa — Brendigo USB Key";
        Width = 480; Height = 380;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brushes.White;

        var panel = new StackPanel { Margin = new Thickness(28) };
        Content = panel;

        panel.Children.Add(new TextBlock
        {
            Text = "Registriraj novi USB ključ",
            FontWeight = FontWeights.Bold, FontSize = 23, Margin = new Thickness(0, 0, 0, 10)
        });
        panel.Children.Add(new TextBlock
        {
            Text = "Odaberi izmjenjivi pogon. Svaki stick registriraj zasebno. Ako na sticku već postoji valjan Brendigo ključ, može se povezati s ovim računom.",
            TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 17),
            Foreground = Brushes.SlateGray
        });

        panel.Children.Add(new TextBlock { Text = "USB pogon", FontWeight = FontWeights.SemiBold });
        _drives = new ComboBox { Margin = new Thickness(0, 5, 0, 16), Height = 36 };
        foreach (var drive in drives) _drives.Items.Add(drive);
        _drives.SelectedIndex = drives.Count > 0 ? 0 : -1;
        panel.Children.Add(_drives);

        panel.Children.Add(new TextBlock { Text = "Naziv ključa", FontWeight = FontWeights.SemiBold });
        _name = new TextBox { Text = "Moj USB ključ", Height = 36, Padding = new Thickness(9) };
        panel.Children.Add(_name);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 22, 0, 0)
        };
        panel.Children.Add(buttons);
        var cancel = new Button { Content = "Odustani", Width = 110, Margin = new Thickness(0, 0, 10, 0),
            Style = (Style)Application.Current.FindResource("SecondaryButton") };
        cancel.Click += (_, _) => DialogResult = false;
        buttons.Children.Add(cancel);

        var accept = new Button { Content = "Registriraj", Width = 125, IsDefault = true,
            Style = (Style)Application.Current.FindResource("PrimaryButton") };
        accept.Click += (_, _) =>
        {
            if (_drives.SelectedItem is null || string.IsNullOrWhiteSpace(KeyName))
            {
                MessageBox.Show(this, "Odaberi USB pogon i upiši naziv ključa.",
                    "Nedostaju podaci", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
        };
        buttons.Children.Add(accept);
    }
}

public sealed class NameDialog : Window
{
    private readonly TextBox _name;
    public string NewName => _name.Text.Trim();

    public NameDialog(string original)
    {
        Title = "Preimenuj USB ključ";
        Width = 430; Height = 215;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new Thickness(24) };
        Content = panel;
        panel.Children.Add(new TextBlock { Text = "Novi naziv USB ključa",
            FontSize = 17, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 12) });
        _name = new TextBox { Text = original, Height = 36, Padding = new Thickness(8) };
        panel.Children.Add(_name);
        var save = new Button { Content = "Spremi naziv", Width = 145,
            HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0),
            Style = (Style)Application.Current.FindResource("PrimaryButton") };
        save.Click += (_, _) => { if (!string.IsNullOrWhiteSpace(NewName)) DialogResult = true; };
        panel.Children.Add(save);
    }
}
