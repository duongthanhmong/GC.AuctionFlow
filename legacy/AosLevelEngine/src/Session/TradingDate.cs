using System.Globalization;
using Aos.LevelEngine.Session;

namespace Aos.LevelEngine.Session;

/// <summary>
/// Thin helpers retained for call-site clarity. Prefer <see cref="TradingDateResolver"/>.
/// </summary>
public static class TradingDate
{
    public static DateOnly Of(DateTime timestampExchange, TimeZoneInfo tz, TimeOnly? sessionRolloverEt = null)
    {
        var rollover = sessionRolloverEt ?? new TimeOnly(18, 0); // seed — prefer profile via TradingDateResolver
        var utc = DateTime.SpecifyKind(timestampExchange, DateTimeKind.Utc);
        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
        var calendar = DateOnly.FromDateTime(local);
        var tod = TimeOnly.FromDateTime(local);
        return tod >= rollover ? calendar.AddDays(1) : calendar;
    }

    public static DateOnly? Prior(DateOnly tradingDate, IEnumerable<DateOnly> tradingDatesWithData)
    {
        DateOnly? best = null;
        foreach (var d in tradingDatesWithData)
        {
            if (d >= tradingDate) continue;
            if (best is null || d > best) best = d;
        }
        return best;
    }

    public static string Format(DateOnly? d) =>
        d?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "(none)";
}
