namespace OfficeClock.Models;

public sealed class DayRecord
{
    public DateTime Date { get; init; }
    public DateTime In { get; init; }
    public DateTime Out { get; init; }
    public string? Reason { get; init; }

    // NFR-4: clock changes must not produce a negative total.
    public TimeSpan Total => Out > In ? Out - In : TimeSpan.Zero;

    public string DateText => Date.ToString("yyyy-MM-dd");
    public string DayText => Date.ToString("ddd");
    public string InText => In.ToString("HH:mm");
    public string OutText => Reason is null ? "Running" : Out.ToString("HH:mm");
    public string TotalText => Format(Total);

    public static string Format(TimeSpan t) => $"{(int)t.TotalHours}:{t.Minutes:00}";
}
