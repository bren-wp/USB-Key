using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Brendigo.USBKey.Models;

namespace Brendigo.USBKey.Services;

/// <summary>
/// Portable token format; not a hardware-backed credential. Token bytes on an ordinary
/// USB can be copied, therefore this MUST NOT authorize Windows interactive logon.
/// </summary>
public static class TokenCodec
{
    public const int MaxFileBytes = 2048;

    public static UsbTokenFile CreateNew() => new()
    {
        Format = 1,
        Id = Guid.NewGuid(),
        Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
    };

    public static bool TryParse(ReadOnlySpan<byte> data, out UsbTokenFile? token)
    {
        token = null;
        if (data.Length is < 10 or > MaxFileBytes) return false;

        try
        {
            var parsed = JsonSerializer.Deserialize<UsbTokenFile>(data);
            if (parsed is null || parsed.Format != 1 || parsed.Id == Guid.Empty ||
                parsed.Token is null || parsed.Token.Length != 44) return false;

            byte[] decoded = Convert.FromBase64String(parsed.Token);
            try
            {
                if (decoded.Length != 32 ||
                    !string.Equals(Convert.ToBase64String(decoded), parsed.Token,
                        StringComparison.Ordinal)) return false;
            }
            finally { CryptographicOperations.ZeroMemory(decoded); }

            token = parsed;
            return true;
        }
        catch (Exception ex) when (ex is JsonException or FormatException or ArgumentException)
        {
            return false;
        }
    }

    public static string CalculateProof(UsbTokenFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        byte[]? secret = null;
        byte[]? combined = null;
        try
        {
            secret = Convert.FromBase64String(file.Token);
            if (file.Format != 1 || file.Id == Guid.Empty || secret.Length != 32)
                throw new ArgumentException("Neispravan USB token.", nameof(file));

            byte[] prefix = Encoding.UTF8.GetBytes("Brendigo-USB-Key/v1/" +
                file.Id.ToString("N") + "/");
            combined = new byte[prefix.Length + secret.Length];
            Buffer.BlockCopy(prefix, 0, combined, 0, prefix.Length);
            Buffer.BlockCopy(secret, 0, combined, prefix.Length, secret.Length);
            return Convert.ToHexString(SHA256.HashData(combined));
        }
        finally
        {
            if (secret != null) CryptographicOperations.ZeroMemory(secret);
            if (combined != null) CryptographicOperations.ZeroMemory(combined);
        }
    }
}
