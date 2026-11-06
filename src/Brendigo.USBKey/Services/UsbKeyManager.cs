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
    private const int MaxTokenFileBytes = 2048;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _appFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Brendigo", "USBKey");
    private readonly string _registryPath;
    private readonly string _eventsPath;
    private RegistryState _state;
    private HashSet<Guid> _previouslySeen = new();
    private bool _armed;
    private int _missedScans;

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
            _state.AutoLock = value;
            Save();
            ResetMonitor();
            WriteEvent(value ? "Automatsko zaključavanje uključeno." : "Automatsko zaključavanje isključeno.");
        }
    }

    public IReadOnlyList<KeyRecord> Registered => _state.Keys.AsReadOnly();
    public string EventsPath => _eventsPath;

    public static IReadOnlyList<string> AvailableDrives()
    {
        var drives = new List<string>();
        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            try
            {
                if (drive.IsReady && drive.DriveType == DriveType.Removable)
                    drives.Add(drive.RootDirectory.FullName);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        return drives;
    }

    public KeyRecord Enroll(string root, string name)
    {
        name = name.Trim();
        if (name.Length is < 1 or > 64 || name.Any(char.IsControl))
            throw new ArgumentException("Naziv mora sadržavati 1–64 znaka bez kontrolnih znakova.");

        // Do not allow arbitrary user-supplied paths or the system drive.
        string driveRoot = Path.GetPathRoot(Path.GetFullPath(root))
            ?? throw new IOException("Neispravna lokacija USB uređaja.");
        if (!AvailableDrives().Contains(driveRoot, StringComparer.OrdinalIgnoreCase))
            throw new IOException("Odabrani uređaj nije spreman izmjenjivi pogon.");

        string directory = Path.Combine(driveRoot, TokenDirectory);
        string file = Path.Combine(directory, TokenFilename);
        UsbTokenFile token;
        if (File.Exists(file))
        {
            token = ReadToken(file) ??
                throw new IOException("Postojeća datoteka ključa nije valjana. Neće biti prebrisana.");
        }
        else
        {
            // Distinct random 256-bit secret PER stick; copying the file copies its identity.
            token = new UsbTokenFile
            {
                Id = Guid.NewGuid(),
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            };
            Directory.CreateDirectory(directory);
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

        string proof = GetProof(token);
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
        newName = newName.Trim();
        if (newName.Length is < 1 or > 64 || newName.Any(char.IsControl))
            throw new ArgumentException("Naziv mora sadržavati 1–64 znaka bez kontrolnih znakova.");
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
        foreach (var root in AvailableDrives())
        {
            try
            {
                string path = Path.Combine(root, TokenDirectory, TokenFilename);
                UsbTokenFile? token = ReadToken(path);
                if (token == null) continue;
                KeyRecord? key = _state.Keys.FirstOrDefault(k => k.Id == token.Id);
                if (key == null) continue;

                // Constant-time comparison of proofs. Neither the OS password nor a reusable
                // Windows logon token is kept on the USB drive or in this registry.
                byte[] expected = Convert.FromHexString(key.ProofHex);
                byte[] actual = Convert.FromHexString(GetProof(token));
                bool accepted = CryptographicOperations.FixedTimeEquals(expected, actual);
                CryptographicOperations.ZeroMemory(actual);
                if (accepted) connected.Add(new ConnectedKey(key.Id, key.Name, root));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                or JsonException or FormatException or CryptographicException
                or ArgumentException)
            {
                // A corrupt, untrusted, or removed drive is never a valid key.
            }
        }

        var ids = connected.Select(k => k.Id).ToHashSet();
        foreach (var newlySeen in connected.Where(k => !_previouslySeen.Contains(k.Id)))
        {
            var record = _state.Keys.First(k => k.Id == newlySeen.Id);
            record.LastSeenUtc = DateTimeOffset.UtcNow;
            Save();
            WriteEvent("Prepoznat USB ključ: " + record.Name);
        }
        _previouslySeen = ids;

        // Arm only after a valid key was actually present in THIS app session.
        // Two missed polls protect against transient drive enumeration failures.
        if (connected.Count > 0)
        {
            _armed = true;
            _missedScans = 0;
        }
        else if (_armed && _state.AutoLock && ++_missedScans >= 2)
        {
            _armed = false;
            _missedScans = 0;
            WriteEvent("Uklonjen posljednji aktivni USB ključ; Windows je zaključan.");
            if (!LockWorkStation())
                WriteEvent("UPOZORENJE: Windows nije potvrdio zahtjev za zaključavanje.");
        }
        return connected;
    }

    public void ResetMonitor()
    {
        _armed = false;
        _missedScans = 0;
        _previouslySeen.Clear();
    }

    public void LockNow()
    {
        WriteEvent("Korisnik je zatražio zaključavanje Windowsa.");
        if (!LockWorkStation())
            throw new InvalidOperationException("Windows nije mogao zaključati sesiju.");
    }

    public string ReadEvents()
    {
        if (!File.Exists(_eventsPath)) return "Još nema sigurnosnih događaja.";
        return string.Join(Environment.NewLine, File.ReadLines(_eventsPath).TakeLast(250).Reverse());
    }

    private RegistryState Load()
    {
        if (!File.Exists(_registryPath)) return new RegistryState();
        byte[] cipher = File.ReadAllBytes(_registryPath);
        byte[] clear = ProtectedData.Unprotect(cipher, null, DataProtectionScope.CurrentUser);
        try
        {
            var data = JsonSerializer.Deserialize<RegistryState>(clear)
                ?? throw new InvalidDataException("Registar USB ključeva nije valjan.");
            if (data.Format != 1 || data.Keys == null)
                throw new InvalidDataException("Nepodržan format registra.");
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

    private static UsbTokenFile? ReadToken(string path)
    {
        if (!File.Exists(path)) return null;
        var info = new FileInfo(path);
        if (info.Length is < 10 or > MaxTokenFileBytes) return null;
        byte[] content = File.ReadAllBytes(path);
        try
        {
            var token = JsonSerializer.Deserialize<UsbTokenFile>(content);
            if (token == null || token.Format != 1 || token.Id == Guid.Empty)
                return null;
            if (Convert.FromBase64String(token.Token).Length != 32) return null;
            return token;
        }
        finally { CryptographicOperations.ZeroMemory(content); }
    }

    private static string GetProof(UsbTokenFile file)
    {
        byte[] secret = Convert.FromBase64String(file.Token);
        byte[] prefix = Encoding.UTF8.GetBytes("Brendigo-USB-Key/v1/" + file.Id.ToString("N") + "/");
        byte[] combined = new byte[prefix.Length + secret.Length];
        prefix.CopyTo(combined, 0);
        secret.CopyTo(combined, prefix.Length);
        byte[] hash = SHA256.HashData(combined);
        CryptographicOperations.ZeroMemory(secret);
        CryptographicOperations.ZeroMemory(combined);
        return Convert.ToHexString(hash);
    }

    private void WriteEvent(string message)
    {
        string safeMessage = message.Replace('\r', ' ').Replace('\n', ' ');
        File.AppendAllText(_eventsPath,
            DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz") + " | " +
            safeMessage + Environment.NewLine, Encoding.UTF8);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool LockWorkStation();
}
