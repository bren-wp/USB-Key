using System.Windows;

namespace Brendigo.USBKey.Windows;

/// <summary>
/// UI confirmation only. The actual key revocation is carried out by UsbKeyManager
/// after the caller receives DialogResult == true. No Windows login is modified.
/// </summary>
public partial class RevokeDialog : Window
{
    public RevokeDialog(string keyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyName);
        InitializeComponent();
        KeyNameText.Text = keyName;
    }

    private void Acknowledge_Changed(object sender, RoutedEventArgs e)
        => ConfirmRevokeButton.IsEnabled = AcknowledgeCheck.IsChecked == true;

    private void ConfirmRevoke_Click(object sender, RoutedEventArgs e)
    {
        if (AcknowledgeCheck.IsChecked != true) return;
        DialogResult = true;
    }
}
