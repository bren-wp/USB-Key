using System.Text;

namespace Brendigo.USBKey.Services;

/// <summary>
/// Local, plaintext diagnostic log. No Windows password, PIN or USB secret is stored here.
/// The archive covers only the single retained rotation, not an unlimited audit history.
/// </summary>
public static class SecurityLogStore
{
    private const long MaxReadableFile = 4L * 1024 * 1024;

    public static string ReadRecent(string activePath, int maxEvents = 250)
    {
        if (maxEvents is < 1 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(maxEvents));

        var lines = ExistingSources(activePath)
            .SelectMany(ReadLinesShared)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .TakeLast(maxEvents)
            .Reverse().ToArray();
        return lines.Length == 0
            ? "Još nema sigurnosnih događaja."
            : string.Join(Environment.NewLine, lines);
    }

    public static int ExportRetained(string activePath, string destination)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        string canonical = Path.GetFullPath(destination);
        string current = Path.GetFullPath(activePath);
        string archive = Path.GetFullPath(activePath + ".previous.log");
        if (string.Equals(canonical, current, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(canonical, archive, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Izvoz ne smije prepisati izvorni sigurnosni zapisnik.");

        string[] sources = ExistingSources(activePath).ToArray();
        if (sources.Length == 0)
            throw new InvalidOperationException("Nema sigurnosnih događaja za izvoz.");

        // Validate all paths before creating a new destination.
        foreach (string source in sources)
        {
            if (new FileInfo(source).Length > MaxReadableFile)
                throw new IOException("Sigurnosni dnevnik premašuje ograničenje izvoza.");
        }

        // Use a temporary file in the selected directory; a partially written
        // export must never replace an existing user document.
        string temporary = canonical + "." + Guid.NewGuid().ToString("N") + ".tmp";
        int count = 0;
        try
        {
            using (var writer = new StreamWriter(
                new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None),
                new UTF8Encoding(false)))
            {
                writer.WriteLine("Brendigo USB Key — lokalni sigurnosni dnevnik");
                writer.WriteLine("Ovo je nešifrirani izvoz sačuvanih događaja (aktivni zapisnik i posljednja arhiva).");
                writer.WriteLine("Autor: Brendigo | https://brendigo.com");
                writer.WriteLine();
                foreach (string source in sources)
                    foreach (string line in ReadLinesShared(source))
                    {
                        writer.WriteLine(line);
                        if (!string.IsNullOrWhiteSpace(line)) count++;
                    }
            }
            File.Move(temporary, canonical, overwrite: true);
            return count;
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private static IEnumerable<string> ExistingSources(string activePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activePath);
        string archive = activePath + ".previous.log";
        if (File.Exists(archive)) yield return archive;
        if (File.Exists(activePath)) yield return activePath;
    }

    private static IEnumerable<string> ReadLinesShared(string path)
    {
        if (new FileInfo(path).Length > MaxReadableFile)
            throw new IOException("Sigurnosni dnevnik premašuje dopuštenu veličinu.");

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        while (reader.ReadLine() is string line)
            yield return line;
    }
}
