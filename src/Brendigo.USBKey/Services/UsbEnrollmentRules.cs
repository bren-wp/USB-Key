using System.IO;

namespace Brendigo.USBKey.Services;

/// <summary>
/// Validates only a plain Windows removable DRIVE ROOT, never an arbitrary
/// directory or a drive-relative path. Physical Removable status is checked
/// separately by UsbKeyManager immediately before enrollment.
/// </summary>
public static class UsbEnrollmentRules
{
    public static string RequireDriveRoot(string? requested)
    {
        if (string.IsNullOrWhiteSpace(requested))
            throw new ArgumentException("Odaberi USB pogon.", nameof(requested));

        // No UNC, device namespaces, relative paths, or supplied subdirectories.
        string path = requested.Trim();
        if (path.Length != 3 ||
            !char.IsAsciiLetter(path[0]) || path[1] != ':' ||
            path[2] != '\\')
            throw new IOException("Dopušteno je odabrati samo korijen USB pogona (npr. E:\\).");
        return char.ToUpperInvariant(path[0]) + ":\\";
    }

    public static string NormalizeName(string? value)
    {
        string name = value?.Trim() ?? string.Empty;
        if (name.Length is < 1 or > 64 || name.Any(char.IsControl))
            throw new ArgumentException(
                "Naziv mora sadržavati 1–64 znaka bez kontrolnih znakova.", nameof(value));
        return name;
    }

    public static bool IsValidName(string? value)
    {
        string name = value?.Trim() ?? string.Empty;
        return name.Length is >= 1 and <= 64 && !name.Any(char.IsControl);
    }
}
