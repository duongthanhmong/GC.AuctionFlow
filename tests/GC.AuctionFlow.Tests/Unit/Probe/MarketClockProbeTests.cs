using GC.AuctionFlow.Probe;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Probe;

/// <summary>
/// The measurement that replaces `ATAS_CANDLE_TIME_UTC_V1`.
///
/// That note said ATAS wall-clock timestamps with `Kind: Unspecified` are really UTC, and
/// it was written from inspection rather than measurement. Every recorded frame carries
/// `SourceTimeKindUnspecified` on the strength of it. `Indicator.MarketTime` makes the
/// question answerable, so it gets answered.
/// </summary>
public sealed class MarketClockProbeTests
{
    private static readonly DateTime Utc = new(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);

    private static MarketClockProbe Probe(params TimeSpan[] offsets)
    {
        var probe = new MarketClockProbe();
        foreach (var offset in offsets)
            probe.Observe(Utc + offset, Utc);
        return probe;
    }

    [Fact]
    public void A01_Nothing_sampled_reports_nothing()
    {
        var probe = new MarketClockProbe();
        Assert.Equal(MarketClockAlignment.NotSampled, probe.Alignment);
        Assert.Null(probe.ReportedOffset);
        Assert.Equal("NOT SAMPLED", probe.Describe());
    }

    [Fact]
    public void A02_A_clock_sitting_on_utc_is_reported_as_utc()
    {
        var probe = Probe(TimeSpan.Zero, TimeSpan.FromMilliseconds(3), TimeSpan.FromMilliseconds(-4));
        Assert.Equal(MarketClockAlignment.Utc, probe.Alignment);
        Assert.Equal(TimeSpan.Zero, probe.ReportedOffset);
        Assert.Contains("UTC", probe.Describe());
    }

    /// <summary>
    /// The two clocks cannot be read atomically, so a small disagreement is the instrument,
    /// not the market. Treating it as an offset would report a finding that does not exist.
    /// </summary>
    [Fact]
    public void A03_Sampling_jitter_is_not_mistaken_for_an_offset()
    {
        var probe = Probe(TimeSpan.FromMilliseconds(MarketClockProbe.SamplingJitterMilliseconds - 1));
        Assert.Equal(MarketClockAlignment.Utc, probe.Alignment);
    }

    [Fact]
    public void B01_A_stable_non_zero_offset_is_reported_as_an_offset()
    {
        var probe = Probe(TimeSpan.FromHours(-5), TimeSpan.FromHours(-5).Add(TimeSpan.FromMilliseconds(6)));
        Assert.Equal(MarketClockAlignment.FixedOffset, probe.Alignment);
        Assert.Equal(TimeSpan.FromHours(-5), probe.ReportedOffset);
        Assert.Contains("UTC-5", probe.Describe());
    }

    [Fact]
    public void B02_A_half_hour_exchange_offset_survives_reporting()
    {
        var probe = Probe(TimeSpan.FromMinutes(330));
        Assert.Equal(TimeSpan.FromMinutes(330), probe.ReportedOffset);
        Assert.Contains("UTC+5:30", probe.Describe());
    }

    /// <summary>
    /// An offset that does not land on the grid real exchanges use is a finding. Rounding
    /// it onto the grid would erase exactly the thing worth knowing.
    /// </summary>
    [Fact]
    public void B03_An_offset_off_the_grid_is_reported_as_itself_not_rounded()
    {
        var odd = TimeSpan.FromMinutes(7);
        var probe = Probe(odd);
        Assert.Equal(odd, probe.ReportedOffset);
    }

    /// <summary>
    /// A wandering clock is neither UTC nor a fixed offset, and calling it either would be
    /// a guess dressed as a measurement.
    /// </summary>
    [Fact]
    public void C01_A_drifting_clock_is_reported_as_unstable_not_averaged()
    {
        var probe = Probe(TimeSpan.Zero, TimeSpan.FromHours(3), TimeSpan.FromHours(-3));
        Assert.Equal(MarketClockAlignment.Unstable, probe.Alignment);
        Assert.Null(probe.ReportedOffset);
        Assert.Contains("UNSTABLE", probe.Describe());
        Assert.Equal(TimeSpan.FromHours(6), probe.OffsetSpread);
    }

    /// <summary>
    /// The comparison must be of the two wall-clock readings. Converting `MarketTime` by
    /// its `Kind` first would assume the answer the probe exists to measure — and the Kind
    /// the platform reports is the very thing under suspicion.
    /// </summary>
    [Fact]
    public void C02_The_declared_kind_does_not_change_the_measurement()
    {
        var reading = Utc.AddHours(-5);

        var asUnspecified = new MarketClockProbe();
        asUnspecified.Observe(DateTime.SpecifyKind(reading, DateTimeKind.Unspecified), Utc);

        var asLocal = new MarketClockProbe();
        asLocal.Observe(DateTime.SpecifyKind(reading, DateTimeKind.Local), Utc);

        var asUtc = new MarketClockProbe();
        asUtc.Observe(DateTime.SpecifyKind(reading, DateTimeKind.Utc), Utc);

        Assert.Equal(asUnspecified.LastOffset, asLocal.LastOffset);
        Assert.Equal(asUnspecified.LastOffset, asUtc.LastOffset);
        Assert.Equal(TimeSpan.FromHours(-5), asUnspecified.LastOffset);
    }

    [Fact]
    public void C03_A_default_reading_is_not_a_sample()
    {
        var probe = new MarketClockProbe();
        probe.Observe(default, Utc);
        probe.Observe(Utc, default);
        Assert.Equal(0, probe.Samples);
        Assert.Equal(MarketClockAlignment.NotSampled, probe.Alignment);
    }
}
