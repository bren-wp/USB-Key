using System.Security.Cryptography;
using System.Windows;
using Brendigo.USBKey.Services;

namespace Brendigo.USBKey.Windows;

public enum AccessDialogMode { InitialSetup, Unlock, ChangePassword }

/// <summary>Local management UI access, never a Windows sign-in provider.</summary>
public partial class AccessDialog : Window
{
    private readonly AppPasswordService _passwords;
    private readonly AccessDialogMode _mode;

    public AccessDialog(AppPasswordService passwords, AccessDialogMode mode)
    {
        _passwords = passwords ?? throw new ArgumentNullException(nameof(passwords));
        _mode = mode;
        InitializeComponent();
        if (mode == AccessDialogMode.InitialSetup)
        {
            TitleText.Text = "Početna zaštita aplikacije";
            IntroText.Text = "Upiši početnu lozinku Brendigo, zatim obavezno postavi vlastitu lozinku s najmanje 12 znakova.";
            CurrentLabel.Text = "Početna lozinka";
            InitialHint.Text = "Početna lozinka: Brendigo (samo prvo postavljanje)";
            ConfirmButton.Content = "Postavi lozinku";
        }
        else if (mode == AccessDialogMode.Unlock)
        {
            Height = 430;
            TitleText.Text = "Pristup upravljanju";
            IntroText.Text = "Unesi svoju lokalnu lozinku aplikacije. USB nadzor može raditi u pozadini dok je upravljanje zatvoreno.";
            NewPasswordPanel.Visibility = Visibility.Collapsed;
            ConfirmPanel.Visibility = Visibility.Collapsed;
            InitialHint.Visibility = Visibility.Collapsed;
            ConfirmButton.Content = "Otvori aplikaciju";
        }
        else
        {
            TitleText.Text = "Promijeni lozinku";
            IntroText.Text = "Unesi trenutačnu lozinku, a zatim novu lozinku s najmanje 12 znakova.";
            CurrentLabel.Text = "Trenutačna lozinka";
            InitialHint.Visibility = Visibility.Collapsed;
            ConfirmButton.Content = "Spremi novu lozinku";
        }
        UpdateButton();
        Loaded += (_, _) => CurrentPasswordBox.Focus();
    }

    private void PasswordChanged(object sender, RoutedEventArgs e) => UpdateButton();

    private void UpdateButton()
    {
        if (ConfirmButton is null || CurrentPasswordBox is null ||
            NewPasswordBox is null || ConfirmPasswordBox is null) return;
        ConfirmButton.IsEnabled = CurrentPasswordBox.Password.Length > 0 &&
            (_mode == AccessDialogMode.Unlock ||
             (NewPasswordBox.Password.Length > 0 && ConfirmPasswordBox.Password.Length > 0));
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "";
        string current = CurrentPasswordBox.Password;
        string next = NewPasswordBox.Password;
        string confirmation = ConfirmPasswordBox.Password;
        try
        {
            if (_mode != AccessDialogMode.Unlock && !string.Equals(next, confirmation, StringComparison.Ordinal))
                throw new ArgumentException("Nova lozinka i njezina potvrda nisu jednake.");
            switch (_mode)
            {
                case AccessDialogMode.InitialSetup:
                    _passwords.Initialize(current, next);
                    break;
                case AccessDialogMode.ChangePassword:
                    _passwords.Change(current, next);
                    break;
                case AccessDialogMode.Unlock:
                    if (!_passwords.Verify(current))
                    {
                        int seconds = (int)Math.Ceiling(_passwords.RemainingLockout.TotalSeconds);
                        StatusText.Text = seconds > 0
                            ? $"Previše pogrešnih pokušaja. Pokušaj ponovno za {seconds} sekundi."
                            : "Lozinka aplikacije nije ispravna.";
                        CurrentPasswordBox.Clear();
                        return;
                    }
                    break;
            }
            CurrentPasswordBox.Clear();
            NewPasswordBox.Clear();
            ConfirmPasswordBox.Clear();
            DialogResult = true;
        }
        catch (Exception ex) when (ex is ArgumentException or UnauthorizedAccessException
            or IOException or InvalidOperationException or CryptographicException)
        {
            StatusText.Text = ex is CryptographicException or InvalidDataException
                ? "Pristupna datoteka nije čitljiva u ovom Windows profilu."
                : ex.Message;
            CurrentPasswordBox.Clear();
        }
    }
}
