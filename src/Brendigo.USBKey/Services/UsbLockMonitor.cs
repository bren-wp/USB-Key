namespace Brendigo.USBKey.Services;

/// <summary>
/// Conservative auto-lock state machine: only confirmed absence of ALL registered
/// keys can trigger a lock request. Unknown USB scan results must never lock.
/// This is not a Windows sign-in/authentication mechanism.
/// </summary>
public sealed class UsbLockMonitor
{
    public const int RequiredAbsentScans = 3;

    public bool Armed { get; private set; }
    public int AbsentScans { get; private set; }

    public bool Observe(int presentKeys, bool scanReliable, bool enabled)
    {
        if (presentKeys < 0)
            throw new ArgumentOutOfRangeException(nameof(presentKeys));

        if (!enabled)
        {
            Reset();
            return false;
        }

        if (presentKeys > 0)
        {
            Armed = true;
            AbsentScans = 0;
            return false;
        }

        if (!scanReliable)
        {
            // A transient USB I/O failure is not evidence that the device was removed.
            AbsentScans = 0;
            return false;
        }

        if (!Armed) return false;
        AbsentScans++;
        if (AbsentScans < RequiredAbsentScans) return false;

        Reset(); // Do not lock repeatedly until a valid device has returned.
        return true;
    }

    public void Reset()
    {
        Armed = false;
        AbsentScans = 0;
    }
}
