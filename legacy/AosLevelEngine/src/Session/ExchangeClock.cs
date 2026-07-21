using System.Globalization;

namespace Aos.LevelEngine.Session;

/// <summary>
/// Exchange-clock helpers. Profile Timezone = America/New_York (DST-aware).
/// TimestampExchange is treated as explicit UTC via SpecifyKind — Kind is not trusted
/// for conversion (ATAS runtime: Kind=Unspecified, values match UTC).
/// Never hard-code UTC offsets.
/// </summary>
public static class ExchangeClock
{
    public static TimeZoneInfo Resolve(string ianaOrWindowsId)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(ianaOrWindowsId); }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        }
    }

    public static DateTime ToExchangeLocal(DateTime timestampExchange, TimeZoneInfo tz)
    {
        var utc = DateTime.SpecifyKind(timestampExchange, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
    }

    public static TimeOnly TimeOfDay(DateTime timestampExchange, TimeZoneInfo tz) =>
        TimeOnly.FromDateTime(ToExchangeLocal(timestampExchange, tz));

    public static DateOnly DateOf(DateTime timestampExchange, TimeZoneInfo tz) =>
        DateOnly.FromDateTime(ToExchangeLocal(timestampExchange, tz));

    public static bool IsAtOrAfter(DateTime timestampExchange, TimeZoneInfo tz, string hhmm)
    {
        var t = TimeOfDay(timestampExchange, tz);
        var boundary = TimeOnly.ParseExact(hhmm, "HH:mm", CultureInfo.InvariantCulture);
        return t >= boundary;
    }

    public static bool IsBefore(DateTime timestampExchange, TimeZoneInfo tz, string hhmm)
    {
        var t = TimeOfDay(timestampExchange, tz);
        var boundary = TimeOnly.ParseExact(hhmm, "HH:mm", CultureInfo.InvariantCulture);
        return t < boundary;
    }

    /// <summary>RTH = [09:30, 16:00) ET.</summary>
    public static bool IsRth(DateTime timestampExchange, TimeZoneInfo tz, string start, string end)
    {
        var t = TimeOfDay(timestampExchange, tz);
        var a = TimeOnly.ParseExact(start, "HH:mm", CultureInfo.InvariantCulture);
        var b = TimeOnly.ParseExact(end, "HH:mm", CultureInfo.InvariantCulture);
        return t >= a && t < b;
    }

    /// <summary>Overnight = [18:00 prior calendar day, 09:30) ET.</summary>
    public static bool IsOvernight(DateTime timestampExchange, TimeZoneInfo tz, string start, string end)
    {
        var t = TimeOfDay(timestampExchange, tz);
        var a = TimeOnly.ParseExact(start, "HH:mm", CultureInfo.InvariantCulture); // 18:00
        var b = TimeOnly.ParseExact(end, "HH:mm", CultureInfo.InvariantCulture);   // 09:30
        return t >= a || t < b;
    }
}
