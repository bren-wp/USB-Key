using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brendigo.USBKey;
using Brendigo.USBKey.Windows;

namespace Brendigo.USBKey.Visual;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        string output = Path.GetFullPath(args.Length > 0 ? args[0] : "visual-snapshots");
        Directory.CreateDirectory(output);

        // Use real application XAML and resources, rendered on a Windows STA thread.
        // Images are WPF-rendered UI snapshots, NOT composite mockups or photos.
        var application = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        application.InitializeComponent();
        int saved = 0;

        var welcome = new SplashWindow(openDashboard: false);
        Snapshot(welcome, output, "splash-welcome.png"); saved++;
        Visit(welcome, "RecoveryNav");
        Snapshot(welcome, output, "onboarding-recovery.png"); saved++;
        Visit(welcome, "UsbNav");
        Snapshot(welcome, output, "onboarding-usb.png"); saved++;
        Visit(welcome, "FinishNav");
        Snapshot(welcome, output, "onboarding-finish.png"); saved++;

        var window = new MainWindow(visualPreview: true);
        // Guard against WPF ContentTemplate regressions displaying type names
        // instead of accessible labels in the sidebar.
        foreach (string name in new[]
        {
            "DashboardNav", "KeysNav", "RecoveryNav",
            "SecurityNav", "SettingsNav", "AboutNav"
        })
        {
            if (window.FindName(name) is not Button nav ||
                nav.Content is not string label ||
                string.IsNullOrWhiteSpace(label) ||
                nav.ContentTemplate is null)
                throw new InvalidOperationException(
                    "Sidebar navigation must render a labeled vector icon: " + name);
        }
        Console.WriteLine("PASS: all six sidebar navigation buttons have explicit text captions.");
        Snapshot(window, output, "dashboard.png"); saved++;
        Visit(window, "KeysNav");
        Snapshot(window, output, "usb-keys.png"); saved++;
        Visit(window, "RecoveryNav");
        Snapshot(window, output, "backup-access.png"); saved++;
        Visit(window, "SecurityNav");
        Snapshot(window, output, "security-log.png"); saved++;
        Visit(window, "SettingsNav");
        Snapshot(window, output, "settings.png"); saved++;
        Visit(window, "AboutNav");
        Snapshot(window, output, "about.png"); saved++;

        var enroll = new EnrollDialog(Array.Empty<string>());
        Snapshot(enroll, output, "register-usb.png"); saved++;
        var rename = new NameDialog("Glavni USB");
        Snapshot(rename, output, "rename-usb.png"); saved++;
        var revoke = new RevokeDialog("Glavni USB");
        var acknowledge = revoke.FindName("AcknowledgeCheck") as CheckBox
            ?? throw new InvalidOperationException("Revoke acknowledgement checkbox missing.");
        var revokeButton = revoke.FindName("ConfirmRevokeButton") as Button
            ?? throw new InvalidOperationException("Revoke action button missing.");
        if (revokeButton.IsEnabled)
            throw new InvalidOperationException("Revoke must require explicit acknowledgement.");
        acknowledge.IsChecked = true;
        if (!revokeButton.IsEnabled)
            throw new InvalidOperationException("Revoke remains disabled after acknowledgement.");
        acknowledge.IsChecked = false;
        if (revokeButton.IsEnabled)
            throw new InvalidOperationException("Revoke button must disable when unchecked.");
        Console.WriteLine("PASS: revoke confirmation is gated by acknowledgement.");
        Snapshot(revoke, output, "revoke-usb.png"); saved++;

        // Additional genuine WPF renderings for small windows and 125% pixel density.
        // Do not count them as distinct screens; they are regression variants.
        welcome.Width = 860;
        welcome.Height = 610;
        Visit(welcome, "WelcomeNav");
        Snapshot(welcome, output, "splash-compact.png");

        window.Width = 930;
        window.Height = 660;
        Visit(window, "DashboardNav");
        Snapshot(window, output, "dashboard-compact.png");
        if (window.FindName("QuickActionsCard") is not Border quick ||
            Grid.GetRow(quick) != 1)
            throw new InvalidOperationException("Quick actions did not reflow below USB keys.");
        if (window.FindName("ConnectedMetricCard") is not Border connected ||
            Grid.GetRow(connected) != 1)
            throw new InvalidOperationException("Dashboard metrics did not stack in compact layout.");

        window.Width = 1280;
        window.Height = 820;
        Snapshot(window, output, "dashboard-125dpi.png", dpi: 120);
        Console.WriteLine("PASS: compact layouts and 125% WPF render scaling.");

        if (saved != 13)
            throw new InvalidOperationException($"Expected 13 screens, got {saved}.");

        Console.WriteLine($"PASS: captured {saved} actual WPF-rendered UI views in {output}.");
        return 0;
    }

    private static void Visit(Window window, string controlName)
    {
        if (window.FindName(controlName) is not Button button)
            throw new InvalidOperationException($"Missing navigation button '{controlName}'.");
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, button));
    }

    private static void Snapshot(Window window, string output, string filename, double dpi = 96)
    {
        if (window.Content is not FrameworkElement root)
            throw new InvalidOperationException("Window has no WPF visual root.");

        int width = checked((int)Math.Ceiling(window.Width));
        int height = checked((int)Math.Ceiling(window.Height));
        if (width < 400 || height < 200)
            throw new InvalidOperationException("Unexpected window dimensions.");

        root.Measure(new Size(width, height));
        root.Arrange(new Rect(0, 0, width, height));
        root.UpdateLayout();

        int pixelWidth = checked((int)Math.Round(width * dpi / 96));
        int pixelHeight = checked((int)Math.Round(height * dpi / 96));
        var image = new RenderTargetBitmap(pixelWidth, pixelHeight, dpi, dpi,
            PixelFormats.Pbgra32);
        image.Render(root);

        // Fail rather than publishing blank/unrendered images as real screenshots.
        int stride = pixelWidth * 4;
        byte[] pixels = new byte[stride * pixelHeight];
        image.CopyPixels(pixels, stride, 0);
        var colors = new HashSet<uint>();
        for (int y = 10; y < pixelHeight - 10; y += 7)
        for (int x = 10; x < pixelWidth - 10; x += 7)
        {
            int i = y * stride + x * 4;
            uint rgba = (uint)(pixels[i] | pixels[i+1] << 8 |
                pixels[i+2] << 16 | pixels[i+3] << 24);
            colors.Add(rgba);
        }
        if (colors.Count < 10)
            throw new InvalidOperationException(
                $"UI snapshot {filename} appears blank (only {colors.Count} sampled colors).");

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        string path = Path.Combine(output, filename);
        using (var stream = File.Create(path)) encoder.Save(stream);
        if (new FileInfo(path).Length < 7000)
            throw new InvalidOperationException($"UI snapshot {filename} is unexpectedly small.");

        Console.WriteLine($"PASS {filename} ({pixelWidth}x{pixelHeight} px @ {dpi}dpi, {colors.Count} sampled colors)");
    }
}
