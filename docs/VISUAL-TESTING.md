# Visual-regression images

The workflow can render **actual WPF application controls** into PNG images on a Windows runner, at a fixed baseline size (1280×820 for MainWindow, 1080×710 for Welcome). These are **off-screen WPF renderings**, not photos, not AI-generated mockups, and not proof of real-world pixel-perfect matching.

Screens captured: Splash/Welcome, onboarding recovery/USB/final step, dashboard, USB keys, recovery, security log, settings, about, register USB and rename USB.

The renderer measures/arranges each real visual tree and creates PNGs via WPF RenderTargetBitmap. Basic checks reject all-blank images. It does not verify DPI scaling, native title bars, mouse interactions, screen readers, USB hardware or exact similarity to unavailable reference screenshots.

The repository README should show these images **only after the CI job confirms they exist and they have been committed to assets/screenshots/**. Concept mockups retain explicit "concept" labels.

To render locally on Windows:

    dotnet run --project tests/Brendigo.USBKey.Visual/Brendigo.USBKey.Visual.csproj --configuration Release -- assets/screenshots

Review images at 100%, 125%, 150% and 200% Windows DPI on real hardware before claiming a 1:1 match.
