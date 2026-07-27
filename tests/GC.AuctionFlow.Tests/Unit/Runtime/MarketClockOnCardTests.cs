using GC.AuctionFlow.Core;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Runtime;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Runtime;

/// <summary>
/// The clock measurement must reach the card.
///
/// This project's recurring defect is a value the engine computes correctly and the display
/// layer then replaces with something of its own — five instances so far, none of which had
/// a test. A measurement nobody can see is the same as no measurement, and this one exists
/// to retire an assumption stamped on every recorded frame.
/// </summary>
public sealed class MarketClockOnCardTests
{
    private static GcaeRuntimeSnapshot Publish(string? summary) =>
        new GcaeRuntimeEngine().Publish(
            null, "GCQ6", DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            tradeObserved: true, lastTradeCallbackUtc: null,
            rawRecorderMasterEnabled: false, tradeRecordingEnabled: false,
            recorderAccepting: false, recorderFaulted: false, recorderSessionPresent: false,
            indicatorDisposed: false,
            marketClockSummary: summary);

    /// <summary>
    /// Read through the card, the way the operator's eyes travel — asserting on the helper
    /// is what let a hard-coded MBO row ship beside a correct helper.
    /// </summary>
    private static string Row(string? summary) =>
        AuctionGpsCardMapper.FromSnapshot(Publish(summary), true)
            .AllLines(includeDiagnostics: true)
            .Single(l => l.StartsWith("MARKET CLOCK:", StringComparison.Ordinal));

    [Fact]
    public void A01_The_measured_alignment_reaches_the_card()
    {
        var probe = new MarketClockProbe();
        var utc = new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);
        probe.Observe(utc, utc);

        Assert.Equal("MARKET CLOCK: " + probe.Describe(), Row(probe.Describe()));
        Assert.Contains("UTC", Row(probe.Describe()));
    }

    /// <summary>
    /// A non-zero offset is the finding worth having: it would mean every frame stamped
    /// `SourceTimeKindUnspecified` has been read wrong. It must not be flattened to "UTC".
    /// </summary>
    [Fact]
    public void A02_An_offset_is_shown_as_an_offset_not_flattened_to_utc()
    {
        var probe = new MarketClockProbe();
        var utc = new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);
        probe.Observe(utc.AddHours(-5), utc);

        var row = Row(probe.Describe());
        Assert.Contains("UTC-5", row);
        Assert.DoesNotContain("NOT SAMPLED", row);
    }

    [Fact]
    public void A03_No_sample_says_so_rather_than_claiming_utc()
    {
        var row = Row(null);
        Assert.Equal("MARKET CLOCK: NOT SAMPLED", row);
    }

    [Fact]
    public void A04_An_unstable_clock_is_not_reported_as_settled()
    {
        var probe = new MarketClockProbe();
        var utc = new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);
        probe.Observe(utc, utc);
        probe.Observe(utc.AddHours(4), utc);

        var row = Row(probe.Describe());
        Assert.Contains("UNSTABLE", row);
    }
}
