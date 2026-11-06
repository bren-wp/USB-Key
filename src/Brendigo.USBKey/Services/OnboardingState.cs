using System.Text;

namespace Brendigo.USBKey.Services;

/// <summary>
/// Remembers only whether the informational welcome wizard was completed.
/// This marker is NOT a security credential and cannot authorize USB/Windows login.
/// </summary>
public static class OnboardingState
{
    public static string AppFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Brendigo", "USBKey");

    private const string Marker = "welcome.completed";

    public static bool IsCompleted() => IsCompletedAt(AppFolder);
    public static void MarkCompleted() => MarkCompletedAt(AppFolder);

    public static bool IsCompletedAt(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        string markerPath = Path.Combine(folder, Marker);
        try
        {
            return File.Exists(markerPath) &&
                new FileInfo(markerPath).Length <= 16 &&
                File.ReadAllText(markerPath, Encoding.ASCII).Trim() == "1";
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    public static void MarkCompletedAt(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        Directory.CreateDirectory(folder);
        string markerPath = Path.Combine(folder, Marker);
        string temporary = markerPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, "1\n", Encoding.ASCII);
            File.Move(temporary, markerPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
