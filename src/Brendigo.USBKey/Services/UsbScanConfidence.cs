namespace Brendigo.USBKey.Services;

/// <summary>
/// Recognizes a previously trusted physical-drive slot that remains mounted while
/// its expected token is no longer verifiable. This never proves hardware identity:
/// volume letters may be reused, so a cautious false negative (no lock) is preferred
/// to accidentally locking Windows on a transient token failure.
/// </summary>
public static class UsbScanConfidence
{
    public static bool HasUnverifiedKnownDrive(
        IReadOnlyDictionary<Guid, string> lastKnownRoots,
        IEnumerable<string> currentRoots,
        IEnumerable<Guid> verifiedIds)
    {
        ArgumentNullException.ThrowIfNull(lastKnownRoots);
        ArgumentNullException.ThrowIfNull(currentRoots);
        ArgumentNullException.ThrowIfNull(verifiedIds);
        var roots = new HashSet<string>(currentRoots, StringComparer.OrdinalIgnoreCase);
        var verified = new HashSet<Guid>(verifiedIds);
        return lastKnownRoots.Any(entry =>
            !verified.Contains(entry.Key) && roots.Contains(entry.Value));
    }
}
