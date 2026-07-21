using System.Globalization;
using Aos.LevelEngine.Profiles;

namespace Aos.LevelEngine.Session;

/// <summary>
/// Single system-invariant TradingDate resolver. All modules MUST use this — never invent session dates.
/// Version stamps every manifest / level provenance; bump when rules change.
/// </summary>
public sealed class TradingDateResolver
{
    /// <summary>Bump when ResolveTradingDate / coverage rules change.</summary>
    public const string TradingDateResolverVersion = "1.0.0";

    private readonly TimeZoneInfo _tz;
    private readonly TimeOnly _rollover;
    private readonly TimeOnly _rthStart;
    private readonly TimeOnly _rthEnd;
    private readonly int _partialMaxMissingMinutes;

    public TimeZoneInfo TradingTimeZone => _tz;
    public TimeOnly TradingSessionRolloverLocalTime => _rollover;
    public TimeOnly RthStartLocalTime => _rthStart;
    public TimeOnly RthEndLocalTime => _rthEnd;
    /// <summary>seed value, subject to sensitivity test — from profile.</summary>
    public int PartialCoverageMaxMissingMinutes => _partialMaxMissingMinutes;

    public TradingDateResolver(InstrumentProfile profile)
    {
        var id = profile.TradingSessionIdentity
            ?? throw new InstrumentProfileLoadException(
                "FAIL STARTUP: TradingSessionIdentity section required.");

        _tz = ExchangeClock.Resolve(id.TradingTimeZone);
        _rollover = InstrumentProfileLoader.ParseTime(id.TradingSessionRolloverLocalTime);
        _rthStart = InstrumentProfileLoader.ParseTime(id.RthStartLocalTime);
        _rthEnd = InstrumentProfileLoader.ParseTime(id.RthEndLocalTime);
        // seed value, subject to sensitivity test
        _partialMaxMissingMinutes = id.PartialCoverageMaxMissingMinutes;
    }

    /// <summary>For unit tests that construct params without a full profile.</summary>
    public TradingDateResolver(
        TimeZoneInfo tradingTimeZone,
        TimeOnly tradingSessionRolloverLocalTime,
        TimeOnly rthStartLocalTime,
        TimeOnly rthEndLocalTime,
        int partialCoverageMaxMissingMinutes)
    {
        _tz = tradingTimeZone;
        _rollover = tradingSessionRolloverLocalTime;
        _rthStart = rthStartLocalTime;
        _rthEnd = rthEndLocalTime;
        _partialMaxMissingMinutes = partialCoverageMaxMissingMinutes;
    }

    public static TradingDateResolver FromProfile(InstrumentProfile profile) => new(profile);

    /// <summary>
    /// ResolveTradingDate(exchangeTimeUtc) — Explicit UTC via SpecifyKind; Kind is not trusted.
    /// </summary>
    public DateOnly ResolveTradingDate(DateTime exchangeTimeUtc)
    {
        var utc = DateTime.SpecifyKind(exchangeTimeUtc, DateTimeKind.Utc);
        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, _tz);
        var calendar = DateOnly.FromDateTime(local);
        var tod = TimeOnly.FromDateTime(local);
        return tod >= _rollover ? calendar.AddDays(1) : calendar;
    }

    /// <summary>
    /// Prior trading date among COMPLETE sessions only (weekends/holidays skipped via data).
    /// </summary>
    public DateOnly? PriorTradingDate(DateOnly tradingDate, IEnumerable<DateOnly> completedTradingDates)
    {
        DateOnly? best = null;
        foreach (var d in completedTradingDates)
        {
            if (d >= tradingDate) continue;
            if (best is null || d > best) best = d;
        }
        return best;
    }

    public string Format(DateOnly? d) =>
        d?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "(none)";
}
