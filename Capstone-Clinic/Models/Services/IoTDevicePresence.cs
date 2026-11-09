namespace Capstone_Clinic.Services;

public sealed class IoTDevicePresence
{
    private long _lastSeenUnixMs;

    public void MarkSeen()
    {
        Interlocked.Exchange(
            ref _lastSeenUnixMs,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    public bool IsOnline(TimeSpan? window = null)
    {
        var lastSeen = Interlocked.Read(ref _lastSeenUnixMs);
        if (lastSeen == 0)
        {
            return false;
        }

        var allowed = window ?? TimeSpan.FromSeconds(90);
        var elapsed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - lastSeen;
        return elapsed <= allowed.TotalMilliseconds;
    }
}
