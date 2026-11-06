namespace Brendigo.USBKey.Models;

public sealed class KeyRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string ProofHex { get; set; } = "";
    public DateTimeOffset RegisteredUtc { get; set; }
    public DateTimeOffset? LastSeenUtc { get; set; }
}

public sealed class RegistryState
{
    public int Format { get; set; } = 1;
    public bool AutoLock { get; set; }
    public List<KeyRecord> Keys { get; set; } = new();
}

public sealed class UsbTokenFile
{
    public int Format { get; set; } = 1;
    public Guid Id { get; set; }
    public string Token { get; set; } = "";
}

public sealed record ConnectedKey(Guid Id, string Name, string Drive);

public sealed class KeyRow
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string Status { get; init; } = "";
    public string Registered { get; init; } = "";
    public string LastSeen { get; init; } = "";
}
