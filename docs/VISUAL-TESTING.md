# Visual-regression images

The workflow can render **actual WPF application controls** into PNG images on a Windows runner, at a fixed baseline size (1380×820 for MainWindow, 1080×710 for Welcome). These are **off-screen WPF renderings**, not photos, not AI-generated mockups, and not proof of real-world pixel-perfect matching.

Screens captured: Splash/Welcome, onboarding recovery/USB/final step, dashboard, USB keys, recovery, security log, settings, about, register USB and rename USB.

The renderer measures/arranges each real visual tree and creates PNGs via WPF RenderTargetBitmap. Basic checks reject all-blank images. It does not verify DPI scaling, native title bars, mouse interactions, screen readers, USB hardware or exact similarity to unavailable reference screenshots.

The following 13 captures are stored in assets/screenshots and embedded in the README after passing on Windows CI:

- splash-welcome.png
- onboarding-recovery.png
- onboarding-usb.png
- onboarding-finish.png
- dashboard.png
- usb-keys.png
- backup-access.png
- security-log.png
- settings.png
- about.png
- register-usb.png
- rename-usb.png
- revoke-usb.png

The repository README should show these images **only after the CI job confirms they exist and they have been committed to assets/screenshots/**. Concept mockups retain explicit "concept" labels.

To render locally on Windows:

    dotnet run --project tests/Brendigo.USBKey.Visual/Brendigo.USBKey.Visual.csproj --configuration Release -- assets/screenshots

Review images at 100%, 135%, 150% and 200% Windows DPI on real hardware before claiming a 1:1 match.

## Responsive and interaction regression variants

The Windows visual runner also renders the real WPF Dashboard at 930x660
and the Splash screen at 860x610. It saves dashboard-compact.png and
splash-compact.png, and checks that compact Dashboard cards reflow rather
than overlap. A third rendering, dashboard-125dpi.png, uses 120 DPI (125%).

These are WPF offscreen regression checks, not physical-monitor DPI tests.
Real Windows visual verification at 100%, 125%, 150% and 200% remains required.

The runner also verifies that the USB revoke action cannot be confirmed
until the warning checkbox is acknowledged, and that unchecking it disables
the action again. The test does not revoke a physical USB key.
