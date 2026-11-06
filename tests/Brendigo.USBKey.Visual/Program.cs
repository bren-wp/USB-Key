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

        if (saved != 12)
            throw new InvalidOperationException($"Expected 12 screens, got {saved}.");

        Console.WriteLine($"PASS: captured {saved} actual WPF-rendered UI views in {output}.");
        return 0;
    }

    private static void Visit(Window window, string controlName)
    {
        if (window.FindName(controlName) is not Button button)
            throw new InvalidOperationException($"Missing navigation button '{controlName}'.");
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, button));
    }

    private static void Snapshot(Window window, string output, string filename)
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

        var image = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        image.Render(root);

        // Fail rather than publishing blank/unrendered images as real screenshots.
        int stride = width * 4;
        byte[] pixels = new byte[stride * height];
        image.CopyPixels(pixels, stride, 0);
        var colors = new HashSet<uint>();
        for (int y = 10; y < height - 10; y += 23)
        for (int x = 10; x < width - 10; x += 23)
        {
            int i = y * stride + x * 4;
            uint rgba = (uint)(pixels[i] | pixels[i+1] << 8 |
                pixels[i+2] << 16 | pixels[i+3] << 24);
            colors.Add(rgba);
        }
        if (colors.Count < 12)
            throw new InvalidOperationException(
                $"UI snapshot {filename} appears blank (only {colors.Count} sampled colors).");

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        string path = Path.Combine(output, filename);
        using (var stream = File.Create(path)) encoder.Save(stream);
        if (new FileInfo(path).Length < 7000)
            throw new InvalidOperationException($"UI snapshot {filename} is unexpectedly small.");

        Console.WriteLine($"PASS {filename} ({width}x{height}, {colors.Count} sampled colors)");
    }
}
