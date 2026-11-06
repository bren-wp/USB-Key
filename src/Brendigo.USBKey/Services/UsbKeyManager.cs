using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Brendigo.USBKey.Models;

namespace Brendigo.USBKey.Services;

/// <summary>
/// Phase 1: manages per-user USB trust and optionally locks an already unlocked session.
/// This is NOT a Windows Credential Provider and CANNOT unlock the Windows sign-in screen.
/// </summary>
public sealed class UsbKeyManager
{
    private const string TokenDirectory = ".brendigo-usb-key";
    private const string TokenFilename = "key.json";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _appFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Brendigo", "USBKey");
    private readonly string _registryPath;
    private readonly string _eventsPath;
    private RegistryState _state;
    private HashSet<Guid> _previouslySeen = new();
    private readonly Dictionary<Guid, string> _lastVerifiedDriveRoots = new();
    private readonly UsbLockMonitor _lockMonitor = new();

    public UsbKeyManager()
    {
        Directory.CreateDirectory(_appFolder);
        _registryPath = Path.Combine(_appFolder, "registry.dat");
        _eventsPath = Path.Combine(_appFolder, "events.log");
        _state = Load();
    }

    public bool AutoLock
    {
        get => _state.AutoLock;
        set
        {
            bool previous = _state.AutoLock;
            _state.AutoLock = value;
            try { Save(); }
            catch { _state.AutoLock = previous; throw; }
            ResetMonitor();
            WriteEvent(value ? "Automatsko zaključavanje uključeno." : "Automatsko zaključavanje isključeno.");
        }
    }

    public IReadOnlyList<KeyRecord> Registered => _state.Keys.AsReadOnly();
    public string EventsPath => _eventsPath;
    /// <summary>False when any removable drive or previously trusted token could not be verified.</summary>
    public bool LastScanReliable { get; private set; } = true;

    public static IReadOnlyList<string> AvailableDrives() => EnumerateDrives(out _);

    private static IReadOnlyList<string> EnumerateDrives(out bool reliable)
    {
        reliable = true;
        var drives = new List<string>();
        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            try
            {
                if (drive.IsReady && drive.DriveType == DriveType.Removable)
                    drives.Add(drive.RootDirectory.FullName);
            }
            catch (IOException) { reliable = false; }
            catch (UnauthorizedAccessException) { reliable = false; }
        }
        return drives;
    }

    public KeyRecord Enroll(string root, string name)
    {
        name = UsbEnrollmentRules.NormalizeName(name);
        // Do not silently turn any path on a USB into its drive root.
        string driveRoot = UsbEnrollmentRules.RequireDriveRoot(root);
        if (!AvailableDrives().Contains(driveRoot, StringComparer.OrdinalIgnoreCase))
            throw new IOException("Odabrani uređaj nije spreman izmjenjivi pogon.");

        if (_state.Keys.Count >= 2048)
            throw new InvalidOperationException(
                "Dosegnut je maksimalni broj registriranih USB ključeva.");
        string directory = Path.Combine(driveRoot, TokenDirectory);
        string file = Path.Combine(directory, TokenFilename);
        EnsureSafePaths(directory, file);
        UsbTokenFile token;
        if (File.Exists(file))
        {
            token = ReadToken(file) ??
                throw new IOException("Postojeća datoteka ključa nije valjana. Neće biti prebrisana.");
        }
        else
        {
            // Distinct random 256-bit secret PER stick; copying the file copies its identity.
            token = TokenCodec.CreateNew();
            Directory.CreateDirectory(directory);
            EnsureSafePaths(directory, file);
            File.SetAttributes(directory, File.GetAttributes(directory) | FileAttributes.Hidden);
            using (var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(token, JsonOptions));
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
                CryptographicOperations.ZeroMemory(bytes);
            }
            File.SetAttributes(file, File.GetAttributes(file) | FileAttributes.Hidden);
        }

        string proof = TokenCodec.CalculateProof(token);
        if (_state.Keys.Any(k => k.Id == token.Id))
            throw new InvalidOperationException(
                "Taj USB ključ već je registriran na ovom računu. Ne stvarajte njegovu kopiju.");

        var record = new KeyRecord
        {
            Id = token.Id,
            Name = name,
            ProofHex = proof,
            RegisteredUtc = DateTimeOffset.UtcNow
        };
        _state.Keys.Add(record);
        try { Save(); }
        catch { _state.Keys.Remove(record); throw; }
        WriteEvent("Registriran USB ključ: " + name);
        ResetMonitor();
        return record;
    }

    public void Rename(Guid id, string newName)
    {
        newName = UsbEnrollmentRules.NormalizeName(newName);
        var key = _state.Keys.Single(k => k.Id == id);
        string oldName = key.Name;
        key.Name = newName;
        try { Save(); } catch { key.Name = oldName; throw; }
        WriteEvent("Preimenovan USB ključ: " + oldName + " → " + newName);
    }

    public void Revoke(Guid id)
    {
        var key = _state.Keys.Single(k => k.Id == id);
        _state.Keys.Remove(key);
        try { Save(); } catch { _state.Keys.Add(key); throw; }
        ResetMonitor();
        WriteEvent("Opozvan USB ključ: " + key.Name);
    }

    public IReadOnlyList<ConnectedKey> Scan()
    {
        var connected = new List<ConnectedKey>();
        bool uncertain = false;
        IReadOnlyList<string> drives;
        bool enumerationReliable = true;
        try { drives = EnumerateDrives(out enumerationReliable); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Do not interpret a failing drive enumeration as removal of the last key.
            drives = Array.Empty<string>();
            uncertain = true;
        }
        uncertain |= !enumerationReliable;
        foreach (var root in drives)
        {
            try
            {
                string path = Path.Combine(root, TokenDirectory, TokenFilename);
                UsbTokenFile? token = ReadToken(path);
                if (token is null)
                {
                    // An existing but unreadable token is not proof of device removal.
                    // Fail open: never lock based on an uncertain scan.
                    if (File.Exists(path)) uncertain = true;
                    continue;
                }
                KeyRecord? key = _state.Keys.FirstOrDefault(k => k.Id == token.Id);
                if (key == null) continue;

                // Constant-time comparison of proofs. Neither the OS password nor a reusable
                // Windows logon token is kept on the USB drive or in this registry.
                byte[] expected = Convert.FromHexString(key.ProofHex);
                byte[] actual = Convert.FromHexString(TokenCodec.CalculateProof(token));
                bool accepted = CryptographicOperations.FixedTimeEquals(expected, actual);
                CryptographicOperations.ZeroMemory(actual);
                CryptographicOperations.ZeroMemory(expected);
                // Identical cloned token files must never appear as distinct physical keys.
                if (accepted && !connected.Any(existing => existing.Id == key.Id))
                    connected.Add(new ConnectedKey(key.Id, key.Name, root));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                or JsonException or FormatException or CryptographicException
                or ArgumentException)
            {
                // A corrupt or inaccessible USB cannot count as proof of absence.
                uncertain = true;
            }
        }

        var ids = connected.Select(k => k.Id).ToHashSet();
        // If the physical drive slot is still mounted but a once-recognized key
        // disappears, treat the scan as uncertain rather than confirming removal.
        // This also protects against an invalid or deleted key.json on a present USB.
        uncertain |= UsbScanConfidence.HasUnverifiedKnownDrive(
            _lastVerifiedDriveRoots, drives, ids);
        if (enumerationReliable)
        {
            var presentRoots = new HashSet<string>(drives, StringComparer.OrdinalIgnoreCase);
            foreach (var old in _lastVerifiedDriveRoots
                .Where(k => !presentRoots.Contains(k.Value))
                .Select(k => k.Key).ToArray())
                _lastVerifiedDriveRoots.Remove(old);
        }
        foreach (var verified in connected)
            _lastVerifiedDriveRoots[verified.Id] = verified.Drive;
        foreach (var newlySeen in connected.Where(k => !_previouslySeen.Contains(k.Id)))
        {
            var record = _state.Keys.First(k => k.Id == newlySeen.Id);
            record.LastSeenUtc = DateTimeOffset.UtcNow;
            Save();
            WriteEvent("Prepoznat USB ključ: " + record.Name);
        }
        _previouslySeen = ids;

        // Surface the same uncertainty to the UI instead of reporting a misleading
        // absence. Unknown results reset the auto-lock countdown (fail open).
        LastScanReliable = !uncertain;
        if (_lockMonitor.Observe(connected.Count, scanReliable: LastScanReliable, enabled: _state.AutoLock))
        {
            bool accepted = LockWorkStation();
            WriteEvent(accepted
                ? "Uklonjen posljednji aktivni USB ključ; zatraženo zaključavanje Windows sesije."
                : "UPOZORENJE: Windows je odbio zahtjev za zaključavanje sesije.");
        }
        return connected;
    }

    public void ResetMonitor()
    {
        _lockMonitor.Reset();
        _previouslySeen.Clear();
        _lastVerifiedDriveRoots.Clear();
    }

    public void LockNow()
    {
        WriteEvent("Korisnik je zatražio zaključavanje Windowsa.");
        if (!LockWorkStation())
            throw new InvalidOperationException("Windows nije mogao zaključati sesiju.");
    }

    public string ReadEvents() => SecurityLogStore.ReadRecent(_eventsPath);

    /// <summary>Export all currently retained events, not only the 250 visible rows.</summary>
    public int ExportEvents(string destination) =>
        SecurityLogStore.ExportRetained(_eventsPath, destination);

    private RegistryState Load()
    {
        if (!File.Exists(_registryPath)) return new RegistryState();
        if (new FileInfo(_registryPath).Length > 2 * 1024 * 1024)
            throw new InvalidDataException("USB registar premašuje sigurnosno ograničenje veličine.");
        byte[] cipher = File.ReadAllBytes(_registryPath);
        byte[] clear = ProtectedData.Unprotect(cipher, null, DataProtectionScope.CurrentUser);
        try
        {
            var data = JsonSerializer.Deserialize<RegistryState>(clear)
                ?? throw new InvalidDataException("Registar USB ključeva nije valjan.");
            if (data.Format != 1 || data.Keys == null || data.Keys.Count > 2048)
                throw new InvalidDataException("Nepodržan ili prevelik registar USB ključeva.");
            var ids = new HashSet<Guid>();
            foreach (KeyRecord key in data.Keys)
            {
                if (key is null || key.Id == Guid.Empty || !ids.Add(key.Id) ||
                    string.IsNullOrWhiteSpace(key.Name) || key.Name.Length > 64 ||
                    key.Name.Any(char.IsControl) || key.ProofHex is null ||
                    key.ProofHex.Length != 64 || !key.ProofHex.All(static c => char.IsAsciiHexDigit(c)))
                    throw new InvalidDataException(
                        "USB registar sadrži oštećeni ili duplicirani zapis. Podaci nisu automatski zamijenjeni.");
            }
            return data;
        }
        finally { CryptographicOperations.ZeroMemory(clear); }
    }

    private void Save()
    {
        byte[] clear = JsonSerializer.SerializeToUtf8Bytes(_state, JsonOptions);
        byte[] cipher;
        try { cipher = ProtectedData.Protect(clear, null, DataProtectionScope.CurrentUser); }
        finally { CryptographicOperations.ZeroMemory(clear); }
        string temp = _registryPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllBytes(temp, cipher);
            File.Move(temp, _registryPath, overwrite: true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    private static void EnsureSafePaths(string directory, string file)
    {
        // Reject simple directory junctions/symlinks on the untrusted removable medium.
        // This is a defense-in-depth check; an ordinary USB token is still copyable.
        if (Directory.Exists(directory) &&
            (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("USB mapa za ključ ne smije biti simbolička poveznica.");
        if (File.Exists(file) &&
            (File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Datoteka USB ključa ne smije biti simbolička poveznica.");
    }

    private static UsbTokenFile? ReadToken(string path)
    {
        string directory = Path.GetDirectoryName(path)
            ?? throw new IOException("Neispravna putanja USB tokena.");
        EnsureSafePaths(directory, path);
        if (!File.Exists(path)) return null;
        // Read only the strictly capped amount, even if an untrusted USB file changes
        // size between metadata lookup and reading. FileShare.Read avoids write races.
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.Read, bufferSize: 4096, FileOptions.SequentialScan);
        if (stream.Length is < 10 or > TokenCodec.MaxFileBytes) return null;
        byte[] bytes = new byte[checked((int)stream.Length)];
        try
        {
            stream.ReadExactly(bytes);
            if (stream.ReadByte() != -1) return null;
            return TokenCodec.TryParse(bytes, out var token) ? token : null;
        }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    private void WriteEvent(string message)
    {
        string safeMessage = message.Replace('\r', ' ').Replace('\n', ' ');
        // Keep local metadata bounded. The archive contains only the previous log segment.
        if (File.Exists(_eventsPath) && new FileInfo(_eventsPath).Length > 1024 * 1024)
            File.Move(_eventsPath, _eventsPath + ".previous.log", overwrite: true);
        File.AppendAllText(_eventsPath,
            DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz") + " | " +
            safeMessage + Environment.NewLine, Encoding.UTF8);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool LockWorkStation();
}
