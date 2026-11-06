using System.Security.Cryptography;
using System.Text.Json;

namespace Brendigo.USBKey.Services;

/// <summary>
/// Optional, per-Windows-user UI management password. NOT a Windows credential,
/// disk encryption key, hardware-backed authentication or security boundary
/// against malware running as this Windows user.
/// </summary>
public sealed class AppPasswordService
{
    // One-time shared bootstrap phrase requested by the product owner.
    // It MUST be replaced before any USB management controls are shown.
    public const string InitialPassword = "Brendigo";
    private const int WorkFactor = 210_000;
    private const int MaxFailures = 5;
    private const int MaxCredentialSize = 4096;
    private readonly string _path;
    private int _failures;
    private DateTimeOffset _lockedUntil;

    private sealed class Credential
    {
        public int Format { get; set; } = 1;
        public int Iterations { get; set; } = WorkFactor;
        public string Salt { get; set; } = "";
        public string Hash { get; set; } = "";
    }

    public AppPasswordService(string? dataFolder = null)
    {
        string folder = dataFolder ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Brendigo", "USBKey");
        Directory.CreateDirectory(folder);
        _path = Path.Combine(folder, "access.dat");
    }

    public bool IsConfigured => File.Exists(_path);
    public TimeSpan RemainingLockout
    {
        get
        {
            var remaining = _lockedUntil - DateTimeOffset.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }

    public static string? ValidateNew(string? password)
    {
        if (password is null || password.Length is < 12 or > 128)
            return "Nova lozinka mora imati između 12 i 128 znakova.";
        if (password.Any(char.IsControl))
            return "Lozinka ne smije sadržavati kontrolne znakove.";
        if (string.Equals(password, InitialPassword, StringComparison.Ordinal))
            return "Početna lozinka Brendigo nije dopuštena kao trajna lozinka.";
        return null;
    }

    public void Initialize(string initialPassword, string newPassword)
    {
        if (IsConfigured)
            throw new InvalidOperationException("Lozinka aplikacije već je postavljena.");
        if (!string.Equals(initialPassword, InitialPassword, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("Početna lozinka nije ispravna.");
        string? error = ValidateNew(newPassword);
        if (error != null) throw new ArgumentException(error, nameof(newPassword));
        WriteCredential(newPassword, replace: false);
    }

    public bool Verify(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        if (RemainingLockout > TimeSpan.Zero) return false;
        if (!IsConfigured) return false;

        Credential credential = ReadCredential();
        byte[] salt = Convert.FromBase64String(credential.Salt);
        byte[] expected = Convert.FromBase64String(credential.Hash);
        byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt,
            credential.Iterations, HashAlgorithmName.SHA256, 32);
        try
        {
            bool valid = CryptographicOperations.FixedTimeEquals(expected, actual);
            if (valid)
            {
                _failures = 0;
                _lockedUntil = default;
            }
            else if (++_failures >= MaxFailures)
            {
                _lockedUntil = DateTimeOffset.UtcNow.AddSeconds(45);
                _failures = 0;
            }
            return valid;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(salt);
            CryptographicOperations.ZeroMemory(expected);
            CryptographicOperations.ZeroMemory(actual);
        }
    }

    public void Change(string currentPassword, string newPassword)
    {
        string? error = ValidateNew(newPassword);
        if (error != null) throw new ArgumentException(error, nameof(newPassword));
        if (string.Equals(currentPassword, newPassword, StringComparison.Ordinal))
            throw new ArgumentException("Nova lozinka mora biti različita od postojeće.", nameof(newPassword));
        if (!Verify(currentPassword))
            throw new UnauthorizedAccessException("Trenutačna lozinka nije ispravna.");
        WriteCredential(newPassword, replace: true);
        _failures = 0;
        _lockedUntil = default;
    }

    private Credential ReadCredential()
    {
        byte[] cipher = File.ReadAllBytes(_path);
        if (cipher.Length is < 16 or > MaxCredentialSize)
            throw new InvalidDataException("Neispravna datoteka pristupa aplikaciji.");
        byte[] clear = ProtectedData.Unprotect(cipher, null, DataProtectionScope.CurrentUser);
        try
        {
            Credential record = JsonSerializer.Deserialize<Credential>(clear)
                ?? throw new InvalidDataException("Neispravni pristupni podaci.");
            if (record.Format != 1 || record.Iterations != WorkFactor ||
                record.Salt.Length != 44 || record.Hash.Length != 44 ||
                Convert.FromBase64String(record.Salt).Length != 32 ||
                Convert.FromBase64String(record.Hash).Length != 32)
                throw new InvalidDataException("Nepodržan ili neispravan format pristupnih podataka.");
            return record;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(clear);
            CryptographicOperations.ZeroMemory(cipher);
        }
    }

    private void WriteCredential(string password, bool replace)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(32);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, WorkFactor,
            HashAlgorithmName.SHA256, 32);
        byte[] clear = Array.Empty<byte>();
        byte[] cipher = Array.Empty<byte>();
        try
        {
            var record = new Credential
            {
                Salt = Convert.ToBase64String(salt),
                Hash = Convert.ToBase64String(hash)
            };
            clear = JsonSerializer.SerializeToUtf8Bytes(record);
            cipher = ProtectedData.Protect(clear, null, DataProtectionScope.CurrentUser);
            string temporary = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(temporary, cipher);
                File.Move(temporary, _path, overwrite: replace);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(salt);
            CryptographicOperations.ZeroMemory(hash);
            CryptographicOperations.ZeroMemory(clear);
            CryptographicOperations.ZeroMemory(cipher);
        }
    }
}
