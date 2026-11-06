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

Console.WriteLine($"All {checks} USB security smoke checks passed.");
