using GC.AuctionFlow.Profile;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Profile;

public sealed class Phase1ATimestampNormalizationTests
{
    [Fact]
    public void Unspecified_contained_UTC_does_not_double_convert()
    {
        var raw = new DateTime(2026, 7, 23, 4, 29, 0, DateTimeKind.Unspecified);
        var once = AtasTimestampNormalizer.NormalizeCandleTime(raw);
        Assert.Equal(new DateTimeOffset(2026, 7, 23, 4, 29, 0, TimeSpan.Zero), once);

        // Re-normalizing an already-UTC Kind must not shift.
        var again = AtasTimestampNormalizer.NormalizeToUtc(
            once.UtcDateTime, AtasTimestampSemantics.Utc, AtasTimestampProvenance.LiveObserved);
        Assert.Equal(once, again);
    }

    [Fact]
    public void Explicit_UTC_input_preserved()
    {
        var raw = new DateTime(2026, 7, 23, 4, 29, 0, DateTimeKind.Utc);
        var n = AtasTimestampNormalizer.NormalizeCandleTime(raw);
        Assert.Equal(DateTimeKind.Utc, n.UtcDateTime.Kind);
        Assert.Equal(4, n.UtcDateTime.Hour);
        Assert.Equal(29, n.UtcDateTime.Minute);
    }

    [Fact]
    public void Utc_0429_maps_to_ET_0029_auction_PI_2026_07_22_period_32()
    {
        var utc = new DateTimeOffset(2026, 7, 23, 4, 29, 0, TimeSpan.Zero);
        var etZone = AuctionTimezoneResolver.Resolve();
        var et = TimeZoneInfo.ConvertTimeFromUtc(utc.UtcDateTime, etZone);
        Assert.Equal(0, et.Hour);
        Assert.Equal(29, et.Minute);

        var clock = new PrimaryAuctionClock();
        var point = clock.Resolve(utc);
        Assert.Equal("PI-2026-07-22", point.AuctionId);
        Assert.Equal(32, point.TpoPeriodIndex);
    }

    [Fact]
    public void Unspecified_raw_ATAS_style_normalizes_to_period_32_not_40()
    {
        // Live defect reproduction: Unspecified wall clock that is UTC 04:29.
        var raw = new DateTime(2026, 7, 23, 4, 29, 0, DateTimeKind.Unspecified);
        var utc = AtasTimestampNormalizer.NormalizeCandleTime(raw);
        var point = new PrimaryAuctionClock().Resolve(utc);
        Assert.Equal(32, point.TpoPeriodIndex);
        Assert.NotEqual(40, point.TpoPeriodIndex);
    }

    [Fact]
    public void Exactly_0820_ET_is_period_0_of_new_auction()
    {
        // 2026-07-22 08:20 EDT = 12:20 UTC
        var utc = new DateTimeOffset(2026, 7, 22, 12, 20, 0, TimeSpan.Zero);
        var point = new PrimaryAuctionClock().Resolve(utc);
        Assert.Equal("PI-2026-07-22", point.AuctionId);
        Assert.Equal(0, point.TpoPeriodIndex);
    }

    [Fact]
    public void Immediately_before_0820_ET_belongs_to_prior_AuctionId()
    {
        // 2026-07-22 08:19:59 EDT = 12:19:59 UTC
        var utc = new DateTimeOffset(2026, 7, 22, 12, 19, 59, TimeSpan.Zero);
        var point = new PrimaryAuctionClock().Resolve(utc);
        Assert.Equal("PI-2026-07-21", point.AuctionId);
    }

    [Fact]
    public void Unknown_semantics_must_not_silently_convert()
    {
        var raw = new DateTime(2026, 7, 23, 4, 29, 0, DateTimeKind.Unspecified);
        Assert.Throws<InvalidOperationException>(() =>
            AtasTimestampNormalizer.NormalizeToUtc(raw, AtasTimestampSemantics.Unknown, AtasTimestampProvenance.Unknown));
    }

    [Fact]
    public void Old_timestamp_policy_observation_clears_ledger()
    {
        var host = new PrimaryProfileHost(0.1m);
        var utc = new DateTimeOffset(2026, 7, 22, 14, 0, 0, TimeSpan.Zero);
        var good = new ProfileBarObservation(
            0, utc, utc.AddMinutes(1), 100m, 100.1m, 100m, 100m, 1m,
            Array.Empty<PriceVolumeObservation>(), PriceVolumeCapability.Unavailable, true, true, 1,
            AtasTimestampNormalizer.PolicyVersion);
        host.UpsertBar(good, 0, utc);
        Assert.NotNull(host.Current?.CurrentAuction);

        var stale = new ProfileBarObservation(
            1, utc.AddMinutes(30), utc.AddMinutes(31), 100m, 100.1m, 100m, 100m, 1m,
            Array.Empty<PriceVolumeObservation>(), PriceVolumeCapability.Unavailable, true, true, 2,
            "LEGACY_NY_LOCAL_V0");
        host.UpsertBar(stale, 1, utc.AddMinutes(30));
        Assert.Contains(host.RevisionEvents, e => e.StartsWith("TIMESTAMP_POLICY_MISMATCH", StringComparison.Ordinal));
        Assert.Null(host.Current?.CurrentAuction);
    }
}
