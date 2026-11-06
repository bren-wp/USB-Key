using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Brendigo.USBKey.Models;
using Brendigo.USBKey.Services;

int checks = 0;
void Check(bool valid, string description)
{
    if (!valid) throw new InvalidOperationException("TEST FAILED: " + description);
    Console.WriteLine("PASS " + description);
    checks++;
}

var first = TokenCodec.CreateNew();
var second = TokenCodec.CreateNew();
Check(first.Id != second.Id, "Different devices have different identifiers");
Check(first.Token != second.Token, "Different devices have independent random secrets");
Check(Convert.FromBase64String(first.Token).Length == 32, "Secret contains 256 bits");
Check(TokenCodec.CalculateProof(first).Length == 64, "SHA-256 proof is 32 bytes");
Check(TokenCodec.CalculateProof(first) != TokenCodec.CalculateProof(second), "Proofs differ");
var clone = new UsbTokenFile { Id = first.Id, Format = first.Format, Token = first.Token };
Check(TokenCodec.CalculateProof(first) == TokenCodec.CalculateProof(clone),
    "Cloned token keeps same proof (documented limitation)");

byte[] data = JsonSerializer.SerializeToUtf8Bytes(first);
Check(TokenCodec.TryParse(data, out var parsed) && parsed!.Id == first.Id, "Valid JSON token parses");
Check(parsed != null && TokenCodec.CalculateProof(parsed) == TokenCodec.CalculateProof(first),
    "Parsed token produces same proof");
Check(!TokenCodec.TryParse(Encoding.UTF8.GetBytes("{broken"), out _), "Malformed JSON rejected");
Check(!TokenCodec.TryParse(new byte[TokenCodec.MaxFileBytes + 1], out _),
    "Oversized token rejected");
Check(!TokenCodec.TryParse(Encoding.UTF8.GetBytes(""), out _), "Empty token rejected");

var invalid = new UsbTokenFile { Id = Guid.Empty, Token = first.Token };
Check(!TokenCodec.TryParse(JsonSerializer.SerializeToUtf8Bytes(invalid), out _),
    "Empty identifier rejected");
invalid.Id = Guid.NewGuid();
invalid.Token = first.Token.Insert(10, " ");
Check(!TokenCodec.TryParse(JsonSerializer.SerializeToUtf8Bytes(invalid), out _),
    "Noncanonical Base64 rejected");
invalid.Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
Check(!TokenCodec.TryParse(JsonSerializer.SerializeToUtf8Bytes(invalid), out _),
    "Short secret rejected");


var monitor = new UsbLockMonitor();
Check(!monitor.Observe(0, true, true), "Cannot auto-lock before a valid USB was seen");
Check(!monitor.Observe(2, true, true), "Two recognized USB sticks arm monitoring");
Check(monitor.Armed && monitor.AbsentScans == 0, "Monitoring arms only after valid USB presence");
Check(!monitor.Observe(1, true, true), "Removing one of two USBs cannot lock");
Check(!monitor.Observe(0, true, true), "First reliable absent scan does not lock");
Check(!monitor.Observe(0, false, true), "Uncertain scan must not lock");
Check(monitor.AbsentScans == 0, "Uncertain scan resets absence countdown");
Check(!monitor.Observe(0, true, true), "Reliable absence count restarts");
Check(!monitor.Observe(0, true, true), "Second reliable absence does not lock");
Check(monitor.Observe(0, true, true), "Third reliable absence requests a single lock");
Check(!monitor.Observe(0, true, true), "No repeated lock without re-arming");
Check(!monitor.Observe(1, true, true), "Returned key re-arms after lock");
Check(!monitor.Observe(0, true, false), "Disabling auto-lock never locks");
Check(!monitor.Armed, "Disabling auto-lock disarms monitor");
monitor.Observe(1, true, true);
monitor.Reset();
Check(!monitor.Observe(0, true, true), "Settings change resets monitoring");
Check(!monitor.Observe(0, false, true), "No lock on an unreadable device after reset");

// Onboarding completion must be independent of USB enrollment and Windows credentials.
string welcomeFolder = Path.Combine(Path.GetTempPath(), "brendigo-welcome-test-" + Guid.NewGuid().ToString("N"));
try
{
    Check(!OnboardingState.IsCompletedAt(welcomeFolder), "Unseen welcome starts incomplete");
    OnboardingState.MarkCompletedAt(welcomeFolder);
    Check(OnboardingState.IsCompletedAt(welcomeFolder), "Finished welcome is remembered without USB registration");
    OnboardingState.MarkCompletedAt(welcomeFolder);
    Check(OnboardingState.IsCompletedAt(welcomeFolder), "Finishing twice is safe");
    File.WriteAllText(Path.Combine(welcomeFolder, "welcome.completed"), "garbage");
    Check(!OnboardingState.IsCompletedAt(welcomeFolder), "Corrupt non-security welcome marker is rejected");
}
finally
{
    if (Directory.Exists(welcomeFolder)) Directory.Delete(welcomeFolder, recursive: true);
}

// A full export must include the last rotation, and must not overwrite either source.
string logFolder = Path.Combine(Path.GetTempPath(), "brendigo-log-test-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(logFolder);
try
{
    string current = Path.Combine(logFolder, "events.log");
    string archive = current + ".previous.log";
    File.WriteAllText(archive, "2026-11-06 | Arhiva: prethodni događaj\n");
    File.WriteAllText(current, "2026-11-07 | Novi događaj\n");
    string recent = SecurityLogStore.ReadRecent(current);
    Check(recent.IndexOf("Novi događaj", StringComparison.Ordinal) <
          recent.IndexOf("prethodni događaj", StringComparison.Ordinal),
          "Security Log displays newest retained events first");
    Check(SecurityLogStore.ReadRecent(current, 1).Contains("Novi događaj"),
          "Security Log enforces recent row limit");
    string output = Path.Combine(logFolder, "izvoz.txt");
    int exported = SecurityLogStore.ExportRetained(current, output);
    string all = File.ReadAllText(output);
    Check(exported == 2 && all.Contains("prethodni događaj") &&
          all.Contains("Novi događaj"), "Export includes current and retained archived events");
    Check(all.IndexOf("prethodni događaj", StringComparison.Ordinal) <
          all.IndexOf("Novi događaj", StringComparison.Ordinal),
          "Export preserves chronological order");
    bool rejectedOverwrite = false;
    try { SecurityLogStore.ExportRetained(current, archive); }
    catch (IOException) { rejectedOverwrite = true; }
    Check(rejectedOverwrite, "Export cannot overwrite the source log archive");
    Check(File.ReadAllText(archive).Contains("prethodni događaj"),
          "Rejected export does not modify the archive");
}
finally
{
    Directory.Delete(logFolder, recursive: true);
}


// A previously recognized physical drive must not trigger auto-lock while
// it is still mounted if its token file is missing, corrupt or replaced.
var knownRoots = new Dictionary<Guid, string> {
    [first.Id] = "X:\\",
    [second.Id] = "Y:\\"
};
Check(!UsbScanConfidence.HasUnverifiedKnownDrive(knownRoots,
    new[] { "X:\\", "Y:\\" }, new[] { first.Id, second.Id }),
    "Both verified USB drives are reliable");
Check(UsbScanConfidence.HasUnverifiedKnownDrive(knownRoots,
    new[] { "X:\\", "Y:\\" }, new[] { second.Id }),
    "A known mounted USB with unreadable token is uncertain");
Check(!UsbScanConfidence.HasUnverifiedKnownDrive(knownRoots,
    new[] { "Y:\\" }, new[] { second.Id }),
    "Actually removed USB root is reliable absence");
Check(UsbScanConfidence.HasUnverifiedKnownDrive(knownRoots,
    new[] { "x:\\", "y:\\" }, new[] { second.Id }),
    "Known drive comparisons are case insensitive");
Check(!UsbScanConfidence.HasUnverifiedKnownDrive(knownRoots,
    Array.Empty<string>(), Array.Empty<Guid>()),
    "Absence of all previously mounted USB roots is not an I/O error");
Check(UsbScanConfidence.HasUnverifiedKnownDrive(knownRoots,
    new[] { "X:\\", "Y:\\" }, Array.Empty<Guid>()),
    "No verified USB but known mounted roots is uncertain");
var stayConnected = new UsbLockMonitor();
stayConnected.Observe(1, scanReliable: true, enabled: true);
Check(!stayConnected.Observe(0, scanReliable: false, enabled: true),
    "Unverifiable mounted USB does not auto-lock");
Check(!stayConnected.Observe(0, scanReliable: true, enabled: true),
    "After an uncertain scan a fresh reliable absence count begins");

Console.WriteLine($"All {checks} USB security smoke checks passed.");
