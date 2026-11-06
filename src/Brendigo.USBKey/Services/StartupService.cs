using System.IO;
using Microsoft.Win32;

namespace Brendigo.USBKey.Services;

public static class StartupService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "BrendigoUSBKey";

    public static bool IsEnabled()
    {
        string? executable = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executable)) return false;
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
        string? stored = key?.GetValue(ValueName) as string;
        return string.Equals(stored, "\"" + executable + "\" --autostart",
            StringComparison.OrdinalIgnoreCase);
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
        string root = Path.GetPathRoot(executable) ??
            throw new IOException("Nije moguće utvrditi lokaciju izvršne datoteke.");
        DriveType driveType = new DriveInfo(root).DriveType;
        if (driveType is DriveType.Removable or DriveType.Network)
            throw new InvalidOperationException(
                "Automatsko pokretanje zahtijeva stalnu lokalnu lokaciju aplikacije, a ne USB ili mrežni disk.");
        using var run = Registry.CurrentUser.CreateSubKey(RunKey);
        run.SetValue(ValueName, "\"" + executable + "\" --autostart", RegistryValueKind.String);
    }
}
