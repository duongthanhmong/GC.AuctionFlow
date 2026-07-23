namespace GC.AuctionFlow.Profile;

/// <summary>Resolves IANA America/New_York with Windows Eastern Standard Time fallback. Never uses machine-local TZ implicitly.</summary>
public static class AuctionTimezoneResolver
{
    public const string IanaAmericaNewYork = "America/New_York";
    public const string WindowsEastern = "Eastern Standard Time";

    public static TimeZoneInfo Resolve(string? preferredId = null)
    {
        var id = string.IsNullOrWhiteSpace(preferredId) ? IanaAmericaNewYork : preferredId.Trim();
        if (TryFind(id, out var tz))
            return tz!;

        if (string.Equals(id, IanaAmericaNewYork, StringComparison.OrdinalIgnoreCase)
            && TryFind(WindowsEastern, out tz))
            return tz!;

        if (string.Equals(id, WindowsEastern, StringComparison.OrdinalIgnoreCase)
            && TryFind(IanaAmericaNewYork, out tz))
            return tz!;

        throw new TimeZoneNotFoundException(
            $"Unable to resolve timezone '{id}'. Tried IANA '{IanaAmericaNewYork}' and Windows '{WindowsEastern}'.");
    }

    private static bool TryFind(string id, out TimeZoneInfo? tz)
    {
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(id);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            tz = null;
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            tz = null;
            return false;
        }
    }
}

/// <summary>Production Classic TPO / primary auction clock configuration (seeds, subject to sensitivity test).</summary>
public sealed class PrimaryAuctionClockConfig
{
    public const int DefaultPeriodMinutes = 30;
    public static readonly TimeSpan DefaultAnchorLocalTime = new(8, 20, 0);
    public const decimal DefaultValueAreaFraction = 0.70m; // conventional configurable default, not GC edge

    public PrimaryAuctionClockConfig(
        string timezoneId = AuctionTimezoneResolver.IanaAmericaNewYork,
        TimeSpan? anchorLocalTime = null,
        int periodMinutes = DefaultPeriodMinutes,
        decimal valueAreaFraction = DefaultValueAreaFraction)
    {
        if (periodMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(periodMinutes));
        if (valueAreaFraction <= 0m || valueAreaFraction > 1m)
            throw new ArgumentOutOfRangeException(nameof(valueAreaFraction));

        TimezoneId = timezoneId;
        AnchorLocalTime = anchorLocalTime ?? DefaultAnchorLocalTime;
        PeriodMinutes = periodMinutes;
        ValueAreaFraction = valueAreaFraction;
        TimeZone = AuctionTimezoneResolver.Resolve(TimezoneId);
    }

    public string TimezoneId { get; }
    public TimeSpan AnchorLocalTime { get; }
    public int PeriodMinutes { get; }
    public decimal ValueAreaFraction { get; }
    public TimeZoneInfo TimeZone { get; }
}

public sealed class AuctionClockPoint
{
    public AuctionClockPoint(
        string auctionId,
        DateTime auctionStartUtc,
        DateTime auctionEndUtc,
        DateOnly localAuctionDate,
        int tpoPeriodIndex,
        DateTime tpoPeriodStartUtc,
        DateTime tpoPeriodEndUtc,
        bool isCurrentDevelopingPeriod)
    {
        AuctionId = auctionId;
        AuctionStartUtc = auctionStartUtc;
        AuctionEndUtc = auctionEndUtc;
        LocalAuctionDate = localAuctionDate;
        TpoPeriodIndex = tpoPeriodIndex;
        TpoPeriodStartUtc = tpoPeriodStartUtc;
        TpoPeriodEndUtc = tpoPeriodEndUtc;
        IsCurrentDevelopingPeriod = isCurrentDevelopingPeriod;
    }

    public string AuctionId { get; }
    public DateTime AuctionStartUtc { get; }
    public DateTime AuctionEndUtc { get; }
    public DateOnly LocalAuctionDate { get; }
    public int TpoPeriodIndex { get; }
    public DateTime TpoPeriodStartUtc { get; }
    public DateTime TpoPeriodEndUtc { get; }
    public bool IsCurrentDevelopingPeriod { get; }
}

/// <summary>
/// Primary Intraday auction clock: 08:20 America/New_York → next 08:20.
/// Accepts UTC DateTimeOffset only (via Resolve). Machine-local TZ independent.
/// </summary>
public sealed class PrimaryAuctionClock
{
    private readonly PrimaryAuctionClockConfig _config;

    public PrimaryAuctionClock(PrimaryAuctionClockConfig? config = null)
    {
        _config = config ?? new PrimaryAuctionClockConfig();
    }

    public PrimaryAuctionClockConfig Config => _config;

    /// <summary>Resolve auction/TPO period for a UTC instant. Non-UTC offsets are converted to UTC once.</summary>
    public AuctionClockPoint Resolve(DateTimeOffset utcTimestamp)
    {
        var utc = utcTimestamp.ToUniversalTime().UtcDateTime;
        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, _config.TimeZone);
        var localDate = DateOnly.FromDateTime(local);
        var anchorTime = TimeOnly.FromTimeSpan(_config.AnchorLocalTime);

        DateOnly auctionDate;
        if (TimeOnly.FromDateTime(local) >= anchorTime)
            auctionDate = localDate;
        else
            auctionDate = localDate.AddDays(-1);

        var startLocal = auctionDate.ToDateTime(anchorTime);
        var endLocal = auctionDate.AddDays(1).ToDateTime(anchorTime);

        var startUtc = LocalToUtc(startLocal);
        var endUtc = LocalToUtc(endLocal);

        var elapsed = utc - startUtc;
        if (elapsed < TimeSpan.Zero)
            elapsed = TimeSpan.Zero;

        var periodTicks = TimeSpan.FromMinutes(_config.PeriodMinutes);
        var index = (int)(elapsed.Ticks / periodTicks.Ticks);
        var periodStartUtc = startUtc + TimeSpan.FromTicks(periodTicks.Ticks * index);
        var periodEndUtc = periodStartUtc + periodTicks;
        if (periodEndUtc > endUtc)
            periodEndUtc = endUtc;

        var auctionId = $"PI-{auctionDate:yyyy-MM-dd}";
        return new AuctionClockPoint(
            auctionId,
            startUtc,
            endUtc,
            auctionDate,
            index,
            periodStartUtc,
            periodEndUtc,
            isCurrentDevelopingPeriod: utc < periodEndUtc);
    }

    private DateTime LocalToUtc(DateTime unspecifiedLocal)
    {
        // Ambiguous (fall-back): choose the earlier offset occurrence (standard ATAS/session conservative).
        if (_config.TimeZone.IsAmbiguousTime(unspecifiedLocal))
        {
            var offsets = _config.TimeZone.GetAmbiguousTimeOffsets(unspecifiedLocal);
            var earlier = offsets.Min();
            return DateTime.SpecifyKind(unspecifiedLocal - earlier, DateTimeKind.Utc);
        }

        // Invalid (spring-forward): bump forward to next valid local time.
        if (_config.TimeZone.IsInvalidTime(unspecifiedLocal))
        {
            var adjusted = unspecifiedLocal.AddHours(1);
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(adjusted, DateTimeKind.Unspecified), _config.TimeZone);
        }

        return TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(unspecifiedLocal, DateTimeKind.Unspecified),
            _config.TimeZone);
    }
}
