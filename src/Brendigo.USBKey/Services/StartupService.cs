using System.IO;
using Microsoft.Win32;

namespace Brendigo.USBKey.Services;

public static class StartupService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "BrendigoUSBKey";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
        return key?.GetValue(ValueName) is string;
    }

    public static void SetEnabled(bool enabled)
    {
        if (!enabled)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            key?.DeleteValue(ValueName, throwOnMissingValue: false);
            return;
        }

        string executable = Environment.ProcessPath
            ?? throw new InvalidOperationException("Nije moguće utvrditi lokaciju aplikacije.");
        string fileName = Path.GetFileName(executable);
        bool supportedName = string.Equals(fileName, "Brendigo-USB-Key-Portable.exe",
            StringComparison.OrdinalIgnoreCase) ||
            string.Equals(fileName, "Brendigo.USBKey.exe", StringComparison.OrdinalIgnoreCase);
        if (!supportedName)
            throw new InvalidOperationException(
                "Automatsko pokretanje omogućite tek u objavljenoj Windows aplikaciji.");
        using var run = Registry.CurrentUser.CreateSubKey(RunKey);
        run.SetValue(ValueName, "\"" + executable + "\" --autostart", RegistryValueKind.String);
    }
}
