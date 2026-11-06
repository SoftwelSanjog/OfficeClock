namespace OfficeClock.Services;

public static class BootTimeProvider
{
    /// FR-5: boot time if the app started within 10 minutes of boot, else now.
    /// Never earlier than today's midnight (FR-16: days are split at midnight).
    public static DateTime CheckIn(DateTime now)
    {
        var boot = now - TimeSpan.FromMilliseconds(Environment.TickCount64);
        var t = now - boot <= TimeSpan.FromMinutes(10) ? boot : now;
        return t < now.Date ? now.Date : t;
    }
}
