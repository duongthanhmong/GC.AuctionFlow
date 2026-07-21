using Aos.LevelEngine.Session;
using Xunit;

namespace Aos.LevelEngine.Tests;

public class TradingDateTests
{
    private static readonly TimeZoneInfo Et = ExchangeClock.Resolve("America/New_York");

    [Fact]
    public void At_1759_ET_TradingDate_is_same_calendar_day()
    {
        // 2026-07-20 17:59 ET = 21:59 UTC (EDT)
        var t = EtUtc.Of(2026, 7, 20, 17, 59);
        Assert.Equal(new DateOnly(2026, 7, 20), TradingDate.Of(t, Et));
    }

    [Fact]
    public void At_1800_ET_TradingDate_is_next_calendar_day()
    {
        var t = EtUtc.Of(2026, 7, 20, 18, 0);
        Assert.Equal(new DateOnly(2026, 7, 21), TradingDate.Of(t, Et));
    }

    [Fact]
    public void Monday_2252_ET_TradingDate_is_Tuesday()
    {
        // Probe runtime case: 2026-07-21T02:52Z = Mon 20/07 22:52 ET
        var t = DateTime.SpecifyKind(new DateTime(2026, 7, 21, 2, 52, 0), DateTimeKind.Utc);
        Assert.Equal(new DateOnly(2026, 7, 21), TradingDate.Of(t, Et));
        // Sanity: same instant via ET helper
        Assert.Equal(new DateOnly(2026, 7, 21), TradingDate.Of(EtUtc.Of(2026, 7, 20, 22, 52), Et));
    }

    [Fact]
    public void Prior_of_Monday_skips_weekend_to_Friday_via_chart_dates()
    {
        var monday = new DateOnly(2026, 7, 20);
        var friday = new DateOnly(2026, 7, 17);
        var withData = new[] { friday, monday };
        Assert.Equal(friday, TradingDate.Prior(monday, withData));
    }

    [Fact]
    public void FixedProfile_session_mismatch_emits_guard_signal()
    {
        // Probe wrongly expects Friday; FixedProfile scaled = Sun 18:00 ET → TradingDate Monday
        var expectedPrior = new DateOnly(2026, 7, 17); // Friday
        var scaledUtc = DateTime.SpecifyKind(new DateTime(2026, 7, 19, 22, 0, 0), DateTimeKind.Utc);
        var result = SessionAlignment.CompareLastDay(expectedPrior, scaledUtc, Et);

        Assert.Equal(SessionAlignment.Misaligned, result.Alignment);
        Assert.Equal(new DateOnly(2026, 7, 20), result.FixedProfileTradingDate);
        Assert.True(result.EmitDataQualityEvent);
        Assert.Contains("expected=2026-07-17", result.Detail);
        Assert.Contains("observed=2026-07-20", result.Detail);
    }

    [Fact]
    public void FixedProfile_aligned_when_LastDay_matches_prior()
    {
        // At Mon 22:52 ET → resolved Tue 21; prior Mon 20; FP scaled Sun 18:00 ET → Mon 20
        var expectedPrior = new DateOnly(2026, 7, 20);
        var scaledUtc = DateTime.SpecifyKind(new DateTime(2026, 7, 19, 22, 0, 0), DateTimeKind.Utc);
        var result = SessionAlignment.CompareLastDay(expectedPrior, scaledUtc, Et);

        Assert.Equal(SessionAlignment.Aligned, result.Alignment);
        Assert.False(result.EmitDataQualityEvent);
    }
}
