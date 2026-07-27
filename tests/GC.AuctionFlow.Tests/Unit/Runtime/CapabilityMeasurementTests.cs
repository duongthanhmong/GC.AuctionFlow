using GC.AuctionFlow.Core;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Runtime;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Runtime;

/// <summary>
/// Feed capability is measured, and the measurement survives to the card.
///
/// `bidAskClassificationState`, `domState` and `mboState` were hard-coded constants, so
/// the card reported a policy decision as though it were an observation: BID/ASK: UNKNOWN
/// never meant "the feed lacks aggressor side", it meant nobody had looked — while IsAsk
/// and IsBid arrived on every trade callback unread.
///
/// The distinction that matters most here is Unavailable versus Unknown. "We looked and it
/// is not there" and "we have not looked" call for opposite responses, and the card
/// originally folded both into UNKNOWN, which threw the answer away at the last step.
/// </summary>
public sealed class CapabilityMeasurementTests
{
    private static RuntimeCapabilitySnapshot Build(
        long tradesObserved, long withSide, long depthCallbacks = 0) =>
        RuntimeCapabilitySnapshotBuilder.Build(
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            instrumentIdentityAvailable: true,
            tradeObserved: tradesObserved > 0,
            lastTradeCallbackUtc: null,
            rawRecorderMasterEnabled: false,
            tradeRecordingEnabled: false,
            recorderAccepting: false,
            recorderFaulted: false,
            recorderSessionPresent: false,
            tradesObserved: tradesObserved,
            tradesWithAggressorSide: withSide,
            depthCallbacksObserved: depthCallbacks);

    // ========== A: none / some / all, with no fraction chosen ==========

    [Fact]
    public void A01_No_trades_observed_is_unknown() =>
        Assert.Equal(
            RuntimeCapabilityState.Unknown, Build(0, 0).BidAskClassificationState);

    /// <summary>
    /// The finding this instrumentation produced on a live Rithmic feed: trades arrive in
    /// volume and none carry a side. That is a fact about the feed, and it is only sayable
    /// because the count exists.
    /// </summary>
    [Fact]
    public void A02_Trades_with_no_side_is_unavailable_not_unknown() =>
        Assert.Equal(
            RuntimeCapabilityState.Unavailable, Build(5000, 0).BidAskClassificationState);

    [Fact]
    public void A03_Some_sides_is_partial() =>
        Assert.Equal(
            RuntimeCapabilityState.Partial, Build(5000, 4999).BidAskClassificationState);

    [Fact]
    public void A04_Every_trade_sided_is_available() =>
        Assert.Equal(
            RuntimeCapabilityState.Available, Build(5000, 5000).BidAskClassificationState);

    /// <summary>The limitation follows the measurement rather than being unconditional.</summary>
    [Fact]
    public void A05_The_limitation_clears_once_the_feed_proves_itself()
    {
        Assert.Contains("BIDASK_NOT_VALIDATED", Build(5000, 0).KnownLimitations);
        Assert.DoesNotContain("BIDASK_NOT_VALIDATED", Build(5000, 5000).KnownLimitations);
    }

    [Fact]
    public void A06_Dom_is_measured_from_depth_callbacks()
    {
        Assert.Equal(RuntimeCapabilityState.Unavailable, Build(1, 1).DomState);
        Assert.Equal(RuntimeCapabilityState.Partial, Build(1, 1, depthCallbacks: 1).DomState);
    }

    /// <summary>
    /// Blocked is the P0-06D operational lock, not an absence. Reporting MBO as Unavailable
    /// would say the feed lacks it, which is a claim nobody has tested.
    /// </summary>
    [Fact]
    public void A07_Mbo_stays_blocked_rather_than_unavailable()
    {
        Assert.Equal(RuntimeCapabilityState.Blocked, Build(5000, 5000).MboState);
        Assert.Contains("MBO_BLOCKED_ISOLATED_ENVIRONMENT_ONLY", Build(1, 1).KnownLimitations);
    }

    // ========== B: the card must not throw the answer away ==========

    [Fact]
    public void B01_Card_distinguishes_unavailable_from_unknown()
    {
        var unavailable = GC.AuctionFlow.UI.AuctionGpsCardMapper.BidAskLine(Build(5000, 0));
        var unknown = GC.AuctionFlow.UI.AuctionGpsCardMapper.BidAskLine(Build(0, 0));

        Assert.NotEqual(unknown, unavailable);
        Assert.Contains("UNAVAILABLE", unavailable, StringComparison.Ordinal);
        Assert.Contains("UNKNOWN", unknown, StringComparison.Ordinal);
    }

    [Fact]
    public void B02_Card_reports_dom_alongside_bid_ask() =>
        Assert.Contains(
            "DOM: PARTIAL",
            GC.AuctionFlow.UI.AuctionGpsCardMapper.BidAskLine(Build(10, 10, depthCallbacks: 3)),
            StringComparison.Ordinal);
}
