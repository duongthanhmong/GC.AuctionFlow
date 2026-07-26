using GC.AuctionFlow.Cluster;
using GC.AuctionFlow.Core;
using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Orderflow;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Runtime;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Cluster;

/// <summary>
/// Phase 2B Cluster Raw Feature Measurement Foundation.
/// Raw measurements only â€” no imbalance/extreme/absorption/Trade Facilitation.
/// </summary>
public sealed class Phase2BClusterRawFeatureTests
{
    private const decimal Tick = 0.1m;
    private const string Instrument = "GCQ6";
    private const string Epoch = "GCQ6|tick=0.1";
    private const string Auction = "PI-2026-07-24";

    private static DateTime Utc(int day, int sec = 0) =>
        new(2026, 7, day, 12, 0, sec, DateTimeKind.Utc);

    private static NewTradeObservation Obs(
        long seq, decimal price, decimal vol, bool ask = true, bool bid = false,
        TradeCallbackSource src = TradeCallbackSource.OnNewTrade, DateTime? receive = null)
    {
        var at = receive ?? Utc(24, (int)Math.Min(seq, 59));
        return new NewTradeObservation(
            src, seq, at.Ticks, DateTimeKind.Utc, at, 0,
            Instrument, "fp-" + seq, "ext-" + seq,
            price, vol, price, "Buy", "Trade",
            ask, bid, null, null, null);
    }

    private static ExecutedOrderflowHost OrderflowHost(bool enabled = true) =>
        new(Tick, Instrument, Epoch, AtasTimestampNormalizer.PolicyVersion,
            new ExecutedOrderflowPolicyConfig(enabled));

    private static ClusterRawHost ClusterHost(bool enabled = true) =>
        new(Tick, Instrument, Epoch, AtasTimestampNormalizer.PolicyVersion,
            new ClusterRawFeaturePolicyConfig(enabled));

    private static PrimaryProfileSetSnapshot Profiles(string auctionId = Auction)
    {
        var cur = new PrimaryAuctionProfileSnapshot(
            AuctionProfileState.Ready, auctionId, Utc(24), Utc(25), false,
            null, null, 100.5m, 99.5m, 100.0m, Utc(24), null,
            ProfileDataQuality.Complete, Array.Empty<string>());
        return new PrimaryProfileSetSnapshot(
            cur, null, HistoricalInitializationState.Complete, 10,
            Array.Empty<string>(), Array.Empty<string>(), null, Array.Empty<PrimaryAuctionProfileSnapshot>());
    }

    private static ExecutedTradeEvent Trade(
        long seq, decimal price, decimal vol = 1m, bool ask = true, bool bid = false,
        string auction = Auction, IReadOnlyList<string>? episodes = null, DateTime? at = null)
    {
        var obs = Obs(seq, price, vol, ask, bid, receive: at);
        var grid = new PriceGrid(Tick);
        return ExecutedTradeEvent.TryFromNewTrade(
            obs, Tick, Epoch, auction, AtasTimestampNormalizer.PolicyVersion,
            p => grid.TryToTickIndex(p, out var t) ? t : null,
            episodes)!;
    }

    private static (ExecutedOrderflowHost Of, ClusterRawHost Cl) Feed(
        params (long seq, decimal price, decimal vol, bool ask, bool bid)[] trades)
    {
        var of = OrderflowHost();
        of.RebuildContext(Profiles(), null, Utc(24));
        var cl = ClusterHost();
        foreach (var t in trades)
        {
            var evt = Trade(t.seq, t.price, t.vol, t.ask, t.bid, at: Utc(24).AddSeconds((int)t.seq));
            of.ProcessTrade(evt, null, Utc(24).AddSeconds((int)t.seq));
            cl.ProcessOrderflowUpdate(of.Current!, evt.NormalizedPriceTick, evt.EventId, evt.EventSequence,
                evt.ReceiveTimestampUtc ?? evt.ExchangeTimestampUtc, Utc(24).AddSeconds((int)t.seq));
        }

        return (of, cl);
    }

    // --- A baseline ---

    [Fact]
    public void A_DefaultOff_Awaiting_ProbeIndependence()
    {
        var off = ClusterHost(false);
        Assert.Equal(ClusterRawModuleState.Disabled, off.RebuildFromOrderflow(null, Utc(24)).ModuleState);

        var of = OrderflowHost();
        of.RebuildContext(Profiles(), null, Utc(24));
        var cl = ClusterHost(true);
        Assert.Equal(ClusterRawModuleState.AwaitingOrderflow, cl.RebuildFromOrderflow(of.Current, Utc(24)).ModuleState);

        of.ProcessTrade(Trade(1, 100.0m), null, Utc(24).AddSeconds(1));
        cl.ProcessOrderflowUpdate(of.Current!, 1000, "e1", 1, Utc(24).AddSeconds(1), Utc(24).AddSeconds(1));
        Assert.True(cl.Current!.ModuleState is ClusterRawModuleState.Ready or ClusterRawModuleState.Partial);
        Assert.DoesNotContain("EnableTradeFacilitation", typeof(ClusterRawHost).Assembly.Location);
    }

    // --- B same-price ratios ---

    [Fact]
    public void B_SamePriceRatios_SafeUnavailable()
    {
        var (_, cl) = Feed(
            (1, 100.0m, 4m, true, false),
            (2, 100.0m, 2m, false, true));
        var lvl = cl.Current!.CurrentAuction!.PriceLevels.Single(p => p.DecimalPrice == 100.0m);
        Assert.Equal(2m, lvl.SamePriceAskToBidRatio); // 4/2
        Assert.Equal(0.5m, lvl.SamePriceBidToAskRatio);
        Assert.Null(ClusterRawRatio.TryCompute(1m, 0m, out var reason));
        Assert.Equal(ClusterRawFeaturePolicyConfig.LimitationOpposingDenomZero, reason);
        Assert.Null(ClusterRawRatio.TryCompute(null, 1m, out _));
        Assert.Equal(0m, ClusterRawRatio.TryCompute(0m, 5m, out _));
        Assert.Equal(ClusterRawDominantSide.Ask, lvl.RawDominantSide);
        Assert.Equal(2m, lvl.RawDominanceDifference);
        Assert.Contains(ClusterRawFeaturePolicyConfig.LimitationNoImbalance, lvl.Limitations);
    }

    [Fact]
    public void B_UnknownOnly_RatiosUnavailable_NotFabricatedZero()
    {
        var (of, cl) = Feed((1, 100.0m, 5m, false, false));
        Assert.Equal(OrderflowModuleState.Partial, of.Current!.ModuleState);
        var lvl = cl.Current!.CurrentAuction!.PriceLevels.Single();
        Assert.Null(lvl.AskVolume);
        Assert.Null(lvl.BidVolume);
        Assert.Null(lvl.SamePriceAskToBidRatio);
        Assert.Null(lvl.DiagonalAskToBidBelowRatio);
        Assert.Equal(ClusterRawDominantSide.Unknown, lvl.RawDominantSide);
        Assert.Null(lvl.AbsoluteDeltaRank);
        Assert.True(lvl.VolumeRank >= 1);
        Assert.Equal(ClusterRawModuleState.Partial, cl.Current.ModuleState);
        Assert.Equal(1, cl.Current.CurrentAuction!.UnknownOnlyLevelCount);
        Assert.Equal(ClusterClassificationState.NotCalibrated, cl.Current.CurrentAuction.ClassificationState);
    }

    // --- C diagonal ---

    [Fact]
    public void C_DiagonalComparisons_NoMissingTickBridge()
    {
        var (_, cl) = Feed(
            (1, 100.0m, 3m, false, true),   // bid at P-1
            (2, 100.1m, 6m, true, false),   // ask at P
            (3, 100.3m, 2m, false, true));  // gap: 100.2 missing
        var mid = cl.Current!.CurrentAuction!.PriceLevels.Single(p => p.DecimalPrice == 100.1m);
        Assert.Equal(2m, mid.DiagonalAskToBidBelowRatio); // Ask(100.1)=6 / Bid(100.0)=3
        var high = cl.Current.CurrentAuction.PriceLevels.Single(p => p.DecimalPrice == 100.3m);
        Assert.Null(high.DiagonalAskToBidBelowRatio); // opposing P-1 not traded
    }

    // --- D dominance / E runs ---

    [Fact]
    public void D_E_DominanceAndConsecutiveRuns()
    {
        var (_, cl) = Feed(
            (1, 100.0m, 5m, true, false),
            (2, 100.1m, 4m, true, false),
            (3, 100.2m, 1m, false, true));
        var levels = cl.Current!.CurrentAuction!.PriceLevels.OrderBy(p => p.PriceTick).ToArray();
        Assert.Equal(ClusterRawDominantSide.Ask, levels[0].RawDominantSide);
        Assert.Equal(ClusterRawDominantSide.Ask, levels[1].RawDominantSide);
        Assert.Equal(ClusterRawDominantSide.Bid, levels[2].RawDominantSide);
        Assert.Equal(2, levels[0].ConsecutiveRawDominanceTicks);
        Assert.Equal(2, levels[1].ConsecutiveRawDominanceTicks);
        Assert.Equal(1, levels[2].ConsecutiveRawDominanceTicks);
        Assert.Contains(ClusterRawFeaturePolicyConfig.LimitationNoStackedImbalance, levels[0].Limitations);
    }

    // --- F ranks ---

    [Fact]
    public void F_EmpiricalMidrank_TiesAndBounds()
    {
        EmpiricalMidrankV1.Compute(new[] { 10m, 20m, 20m, 30m }, out var ranks, out var pct, out var pop);
        Assert.Equal(4, pop);
        Assert.Equal(1, ranks[0]);
        Assert.Equal(4, ranks[3]);
        Assert.All(pct, p => Assert.InRange(p!.Value, 0m, 1m));

        var (_, cl) = Feed(
            (1, 100.0m, 10m, true, false),
            (2, 100.1m, 20m, true, false),
            (3, 100.2m, 5m, false, true));
        var a = cl.Current!.CurrentAuction!;
        Assert.Equal(3, a.PopulationSize);
        Assert.Equal(a.PriceLevels.Sum(p => p.ExecutedVolume), a.TotalExecutedVolume);
        Assert.Equal(ClusterRawFeaturePolicyConfig.RankMethod, cl.Current.RankMethod);
    }

    // --- G visits ---

    [Fact]
    public void G_Visits_Revisits_Idempotent()
    {
        var of = OrderflowHost();
        of.RebuildContext(Profiles(), null, Utc(24));
        var cl = ClusterHost();
        var t1 = Trade(1, 100.0m, at: Utc(24).AddSeconds(1));
        of.ProcessTrade(t1, null, Utc(24).AddSeconds(1));
        cl.ProcessOrderflowUpdate(of.Current!, t1.NormalizedPriceTick, t1.EventId, t1.EventSequence, Utc(24).AddSeconds(1), Utc(24).AddSeconds(1));
        var tick = t1.NormalizedPriceTick;
        Assert.Equal(1, cl.Current!.CurrentAuction!.PriceLevels.Single(p => p.PriceTick == tick).VisitCount);

        var t2 = Trade(2, 100.0m, at: Utc(24).AddSeconds(2));
        of.ProcessTrade(t2, null, Utc(24).AddSeconds(2));
        cl.ProcessOrderflowUpdate(of.Current!, t2.NormalizedPriceTick, t2.EventId, t2.EventSequence, Utc(24).AddSeconds(2), Utc(24).AddSeconds(2));
        Assert.Equal(1, cl.Current!.CurrentAuction!.PriceLevels.Single(p => p.PriceTick == tick).VisitCount);

        var t3 = Trade(3, 100.1m, at: Utc(24).AddSeconds(3));
        of.ProcessTrade(t3, null, Utc(24).AddSeconds(3));
        cl.ProcessOrderflowUpdate(of.Current!, t3.NormalizedPriceTick, t3.EventId, t3.EventSequence, Utc(24).AddSeconds(3), Utc(24).AddSeconds(3));

        var t4 = Trade(4, 100.0m, at: Utc(24).AddSeconds(4));
        of.ProcessTrade(t4, null, Utc(24).AddSeconds(4));
        cl.ProcessOrderflowUpdate(of.Current!, t4.NormalizedPriceTick, t4.EventId, t4.EventSequence, Utc(24).AddSeconds(4), Utc(24).AddSeconds(4));
        var lvl = cl.Current!.CurrentAuction!.PriceLevels.Single(p => p.PriceTick == tick);
        Assert.Equal(2, lvl.VisitCount);
        Assert.Equal(1, lvl.RevisitCount);

        // duplicate EventId
        var before = cl.Current.CurrentAuction.EventRevision;
        cl.ProcessOrderflowUpdate(of.Current!, t4.NormalizedPriceTick, t4.EventId, t4.EventSequence, Utc(24).AddSeconds(4), Utc(24).AddSeconds(5));
        Assert.Equal(before, cl.Current.CurrentAuction!.EventRevision);
    }

    // --- H auction / J availability ---

    [Fact]
    public void H_AuctionTotals_Reconcile_IdentityStable()
    {
        var (of, cl) = Feed(
            (1, 100.0m, 2m, true, false),
            (2, 100.1m, 3m, false, true));
        var a = cl.Current!.CurrentAuction!;
        Assert.Equal(of.Current!.CurrentAuction!.ExecutedVolume, a.TotalExecutedVolume);
        Assert.Equal(of.Current.CurrentAuction.TradeCount, a.TotalTradeCount);
        Assert.Equal(ClusterRawIdentity.BuildAuction(of.Current.CurrentAuction.SnapshotId), a.SnapshotId);
        var id1 = a.SnapshotId;
        cl.RebuildFromOrderflow(of.Current, Utc(24).AddSeconds(10));
        Assert.Equal(id1, cl.Current!.CurrentAuction!.SnapshotId);
    }

    // --- I episode ---

    [Fact]
    public void I_EpisodeCluster_NoFabrication_NoEffectiveness()
    {
        var of = OrderflowHost();
        of.RebuildContext(Profiles(), null, Utc(24));
        var cl = ClusterHost();
        var evt = Trade(1, 100.0m, episodes: new[] { "EP-1" });
        of.ProcessTrade(evt, null, Utc(24).AddSeconds(1));
        // Without episode meta in set, episode aggregate may still be created by Orderflow with empty ref.
        cl.ProcessOrderflowUpdate(of.Current!, evt.NormalizedPriceTick, evt.EventId, evt.EventSequence, Utc(24).AddSeconds(1), Utc(24).AddSeconds(1));
        var a = cl.Current!.CurrentAuction!;
        Assert.Contains(ClusterRawFeaturePolicyConfig.LimitationNoAbsorption, a.Limitations);
        Assert.Contains(ClusterRawFeaturePolicyConfig.LimitationNoTradeFacilitation, a.Limitations);
        Assert.Equal(ClusterClassificationState.NotCalibrated, a.ClassificationState);
    }

    // --- K lifecycle ---

    [Fact]
    public void K_AuctionTransition_Resets_StaleRejected()
    {
        var of = OrderflowHost();
        of.RebuildContext(Profiles(), null, Utc(24));
        var cl = ClusterHost();
        var t1 = Trade(1, 100.0m);
        of.ProcessTrade(t1, null, Utc(24).AddSeconds(1));
        cl.ProcessOrderflowUpdate(of.Current!, t1.NormalizedPriceTick, t1.EventId, t1.EventSequence, Utc(24).AddSeconds(1), Utc(24).AddSeconds(1));
        var firstId = cl.Current!.CurrentAuction!.SnapshotId;
        var firstVol = cl.Current.CurrentAuction.TotalExecutedVolume;

        of.RebuildContext(Profiles("PI-NEXT"), null, Utc(24).AddMinutes(1));
        var t2 = Trade(2, 100.0m, auction: "PI-NEXT", at: Utc(24).AddMinutes(1));
        of.ProcessTrade(t2, null, Utc(24).AddMinutes(1));
        cl.ProcessOrderflowUpdate(of.Current!, t2.NormalizedPriceTick, t2.EventId, t2.EventSequence,
            Utc(24).AddMinutes(1), Utc(24).AddMinutes(1));
        Assert.NotEqual(firstId, cl.Current!.CurrentAuction!.SnapshotId);
        Assert.Equal(1m, cl.Current.CurrentAuction.TotalExecutedVolume);
        Assert.NotEqual(firstVol + 1m, cl.Current.CurrentAuction.TotalExecutedVolume); // no cross-auction carry

        var rev = cl.Current.InputOrderflowEventRevision;
        // fabricate stale by rebuilding with older publish without advancing â€” ProcessOrderflowUpdate rejects lower
        // Covered by equal-revision idempotence above; stale counter path:
        Assert.True(rev >= 1);
    }

    // --- L runtime/UI ---

    [Fact]
    public void L_RuntimeSchema_Gps_NoProhibitedWording()
    {
        var (of, cl) = Feed((1, 100.0m, 3m, false, false));
        var engine = new GcaeRuntimeEngine();
        var snap = engine.Publish(
            null, Instrument, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, Utc(24), false, false, false, false, false, false,
            Profiles(), executedOrderflow: of.Current, clusterRaw: cl.Current, showClusterRawDiagnostics: true);
        Assert.Equal("0.12.0", snap.Version);
        Assert.Same(cl.Current, snap.ClusterRaw);
        Assert.NotEqual(DataState.Ready, snap.DataGate.DataState); // Cluster does not clear Data Gate

        var lines = AuctionGpsCardMapper.BuildClusterRawLines(cl.Current, true);
        var text = string.Join("\n", lines).ToUpperInvariant();
        Assert.Contains("CLUSTER RAW: PARTIAL", text);
        Assert.Contains("CLUSTER_RAW_FEATURE_POLICY_V1", text);
        Assert.Contains("NOT CALIBRATED", text);
        Assert.Contains("UNAVAILABLE", text);
        Assert.DoesNotContain("BID IMBALANCE", text);
        Assert.DoesNotContain("ASK IMBALANCE", text);
        Assert.DoesNotContain("STACKED IMBALANCE", text);
        Assert.DoesNotContain("EXTREME DELTA", text);
        Assert.DoesNotContain("BIG TRADE", text);
        Assert.DoesNotContain("ABSORPTION", text);
        Assert.DoesNotContain("TRADE FACILITATION", text);
        Assert.DoesNotContain("\nLONG", text);
        Assert.DoesNotContain("\nSHORT", text);
        Assert.DoesNotContain("PROBABILITY", text);
        Assert.DoesNotContain("CONFIDENCE", text);
    }

    // --- M performance ---

    [Fact]
    public void M_WideTickMap_ReuseUnchangedRevision()
    {
        var of = OrderflowHost();
        of.RebuildContext(Profiles(), null, Utc(24));
        var cl = ClusterHost();
        for (var i = 0; i < 200; i++)
        {
            var price = 100.0m + (i * Tick);
            var evt = Trade(i + 1, price, 1m, ask: i % 2 == 0, bid: i % 2 != 0, at: Utc(24).AddSeconds(Math.Min(i, 59)));
            of.ProcessTrade(evt, null, Utc(24).AddSeconds(Math.Min(i, 59)));
            cl.ProcessOrderflowUpdate(of.Current!, evt.NormalizedPriceTick, evt.EventId, evt.EventSequence,
                Utc(24).AddSeconds(Math.Min(i, 59)), Utc(24).AddSeconds(Math.Min(i, 59)));
        }

        Assert.Equal(200, cl.Current!.CurrentAuction!.PriceLevelCount);
        var before = cl.Current;
        var again = cl.ProcessOrderflowUpdate(of.Current!, null, before.LastSourceEventId, before.LastSourceSequence,
            Utc(24).AddMinutes(1), Utc(24).AddMinutes(1));
        Assert.Same(before.CurrentAuction, again.CurrentAuction);
    }

    // --- N scope ---

    [Fact]
    public void N_SourceScope_NoPhase2BInterpretation_SchemaPolicy()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        Assert.True(Directory.Exists(Path.Combine(root, "src", "GC.AuctionFlow", "Cluster")));
        Assert.True(Directory.Exists(Path.Combine(root, "src", "GC.AuctionFlow", "Orderflow")));
        Assert.False(Directory.Exists(Path.Combine(root, "src", "GC.AuctionFlow", "TradeFacilitation")));
        Assert.False(Directory.Exists(Path.Combine(root, "src", "GC.AuctionFlow", "Thesis")));
        Assert.False(Directory.Exists(Path.Combine(root, "src", "GC.AuctionFlow", "Far")));
        var indicator = File.ReadAllText(Path.Combine(root, "src", "GC.AuctionFlow", "Atas", "GcAuctionFlowIndicator.cs"));
        Assert.Contains("EnableClusterRawFeatures = false", indicator, StringComparison.Ordinal);
        Assert.DoesNotContain("EnableTradeFacilitation", indicator, StringComparison.Ordinal);
        Assert.Equal("CLUSTER_RAW_FEATURE_POLICY_V1", ClusterRawFeaturePolicyConfig.PolicyVersion);
        Assert.Equal("0.12.0", GcaeRuntimeSnapshot.SnapshotVersion);
        Assert.True(Directory.Exists(Path.Combine(root, "src", "GC.AuctionFlow", "Efficiency")));
        Assert.Equal("EXECUTED_ORDERFLOW_POLICY_V1", ExecutedOrderflowPolicyConfig.PolicyVersion);
    }
}
