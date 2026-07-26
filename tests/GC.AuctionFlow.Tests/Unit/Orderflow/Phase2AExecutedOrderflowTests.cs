using GC.AuctionFlow.Core;
using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Orderflow;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Runtime;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Orderflow;

/// <summary>
/// Phase 2A Executed Orderflow Raw Feature Foundation.
/// Raw totals only â€” no imbalance, absorption, Trade Facilitation, FAR/AAC, or Thesis.
/// </summary>
public sealed class Phase2AExecutedOrderflowTests
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

    private static ExecutedOrderflowHost Host(bool enabled = true) =>
        new(Tick, Instrument, Epoch, AtasTimestampNormalizer.PolicyVersion,
            new ExecutedOrderflowPolicyConfig(enabled));

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

    // --- A/H admission ---

    [Fact]
    public void A_DefaultOff_Disabled_ProbeOffDoesNotBlock()
    {
        var off = Host(false);
        Assert.Equal(OrderflowModuleState.Disabled, off.RebuildContext(Profiles(), null, Utc(24)).ModuleState);

        var on = Host(true);
        Assert.Equal(OrderflowModuleState.AwaitingTrades, on.RebuildContext(Profiles(), null, Utc(24)).ModuleState);
        on.ProcessTrade(Trade(1, 100.0m), null, Utc(24).AddSeconds(1));
        Assert.True(on.Current!.ModuleState is OrderflowModuleState.Ready or OrderflowModuleState.Partial);
        Assert.Equal(1m, on.Current.CurrentAuction!.ExecutedVolume);
    }

    // --- B aggressor ---

    [Fact]
    public void B_AskBidUnknown_ClassifiedDelta_Coverage()
    {
        var host = Host();
        host.RebuildContext(Profiles(), null, Utc(24));
        host.ProcessTrade(Trade(1, 100.0m, 2m, ask: true, bid: false), null, Utc(24).AddSeconds(1));
        host.ProcessTrade(Trade(2, 100.1m, 3m, ask: false, bid: true), null, Utc(24).AddSeconds(2));
        host.ProcessTrade(Trade(3, 100.2m, 5m, ask: false, bid: false), null, Utc(24).AddSeconds(3));
        var a = host.Current!.CurrentAuction!;
        Assert.Equal(10m, a.ExecutedVolume);
        Assert.Equal(2m, a.AskVolume);
        Assert.Equal(3m, a.BidVolume);
        Assert.Equal(5m, a.UnknownAggressorVolume);
        Assert.Equal(-1m, a.ClassifiedDelta); // 2 - 3
        Assert.Equal(-1m, a.ClassifiedCvd);
        Assert.Equal(0.5m, a.AggressorCoverageRatio);
        Assert.Equal(AggressorClassificationStatus.Partial, a.AggressorClassificationStatus);
        Assert.Equal(OrderflowModuleState.Partial, host.Current.ModuleState);
        Assert.Null(OrderflowRatio.TryCompute(1m, 0m));
    }

    [Fact]
    public void B_CompleteClassification_CanReady_WhenFromAuctionStart()
    {
        var host = Host();
        // First auction trade establishes mid coverage; transition then FromAuctionStart.
        host.RebuildContext(Profiles("PI-A"), null, Utc(24));
        host.ProcessTrade(Trade(1, 100.0m, auction: "PI-A"), null, Utc(24).AddSeconds(1));
        host.RebuildContext(Profiles("PI-B"), null, Utc(24).AddMinutes(1));
        host.ProcessTrade(Trade(2, 100.0m, 1m, ask: true, auction: "PI-B"), null, Utc(24).AddMinutes(1).AddSeconds(1));
        host.ProcessTrade(Trade(3, 100.1m, 1m, ask: false, bid: true, auction: "PI-B"), null, Utc(24).AddMinutes(1).AddSeconds(2));
        var a = host.Current!.CurrentAuction!;
        Assert.Equal(OrderflowCoverageMode.LiveOnlyFromAuctionStart, a.CoverageMode);
        Assert.Equal(AggressorClassificationStatus.Complete, a.AggressorClassificationStatus);
        Assert.Equal(OrderflowModuleState.Ready, host.Current.ModuleState);
        Assert.Equal(0m, a.ClassifiedDelta);
    }

    // --- C auction aggregate / CVD reset ---

    [Fact]
    public void C_Duplicate_Revision_AuctionReset_Cvd()
    {
        var host = Host();
        host.RebuildContext(Profiles(), null, Utc(24));
        var t1 = Trade(1, 100.0m, 4m, ask: true);
        host.ProcessTrade(t1, null, Utc(24).AddSeconds(1));
        var er = host.Current!.CurrentAuction!.EventRevision;
        var sv = host.Current.CurrentAuction.StateVersion;
        host.ProcessTrade(t1, null, Utc(24).AddSeconds(2)); // duplicate
        Assert.Equal(er, host.Current.CurrentAuction!.EventRevision);
        Assert.Equal(1, host.Current.EventsDuplicated);
        Assert.Equal(4m, host.Current.CurrentAuction.ExecutedVolume);

        host.ProcessTrade(Trade(2, 100.2m, 1m, ask: true), null, Utc(24).AddSeconds(3));
        Assert.True(host.Current.CurrentAuction!.EventRevision > er);
        Assert.Equal(5m, host.Current.CurrentAuction.ClassifiedCvd);

        var republish = host.RebuildContext(Profiles(), null, Utc(24).AddSeconds(4));
        Assert.Equal(host.Current.CurrentAuction.EventRevision, republish.CurrentAuction!.EventRevision);
        Assert.Equal(host.Current.CurrentAuction.StateVersion, republish.CurrentAuction.StateVersion);

        host.RebuildContext(Profiles("PI-NEXT"), null, Utc(25));
        host.ProcessTrade(Trade(10, 100.0m, 2m, ask: false, bid: true, auction: "PI-NEXT"), null, Utc(25).AddSeconds(1));
        Assert.Equal(2m, host.Current!.CurrentAuction!.ExecutedVolume);
        Assert.Equal(-2m, host.Current.CurrentAuction.ClassifiedCvd); // reset
        Assert.Equal("PI-NEXT", host.Current.CurrentAuction.PrimaryAuctionId);
    }

    // --- D price ledger ---

    [Fact]
    public void D_PriceLedger_ByTick_NoImbalance()
    {
        var host = Host();
        host.RebuildContext(Profiles(), null, Utc(24));
        host.ProcessTrade(Trade(1, 100.0m, 2m, ask: true), null, Utc(24).AddSeconds(1));
        host.ProcessTrade(Trade(2, 100.0m, 1m, ask: false, bid: true), null, Utc(24).AddSeconds(2));
        host.ProcessTrade(Trade(3, 100.1m, 3m, ask: true), null, Utc(24).AddSeconds(3));
        Assert.Equal(2, host.Current!.PriceLevels.Count);
        var lvl = host.Current.PriceLevels[0];
        Assert.Equal(3m, lvl.ExecutedVolume);
        Assert.Equal(2m, lvl.AskVolume);
        Assert.Equal(1m, lvl.BidVolume);
        Assert.Equal(1m, lvl.ClassifiedDelta);
        Assert.DoesNotContain(host.Current.Limitations, l => l.Contains("IMBALANCE_DETECTED", StringComparison.Ordinal));
        Assert.Contains(ExecutedOrderflowPolicyConfig.LimitationNoImbalance, host.Current.Limitations);
    }

    // --- E episode aggregate ---

    [Fact]
    public void E_EpisodeAggregate_StableId_DoesNotMutateEpisode()
    {
        var host = Host();
        host.RebuildContext(Profiles(), null, Utc(24));
        var epId = "EP|TEST|1";
        host.ProcessTrade(Trade(1, 100.0m, 2m, episodes: new[] { epId }), null, Utc(24).AddSeconds(1));
        host.ProcessTrade(Trade(2, 100.1m, 1m, episodes: new[] { epId }), null, Utc(24).AddSeconds(2));
        var snap = Assert.Single(host.Current!.ActiveEpisodeAggregates);
        Assert.Equal(OrderflowIdentity.BuildEpisode(epId), snap.SnapshotId);
        Assert.Equal(3m, snap.ExecutedVolume);
        Assert.Equal(2, snap.TradeCount);
        Assert.Contains(ExecutedOrderflowPolicyConfig.LimitationNoAbsorption, snap.Limitations);
        Assert.Contains(ExecutedOrderflowPolicyConfig.LimitationNoTradeFacilitation, snap.Limitations);
    }

    // --- F timing ---

    [Fact]
    public void F_Timing_Intervals_NoTapeLabel()
    {
        var host = Host();
        host.RebuildContext(Profiles(), null, Utc(24));
        host.ProcessTrade(Trade(1, 100.0m, at: Utc(24, 0)), null, Utc(24));
        host.ProcessTrade(Trade(2, 100.0m, at: Utc(24, 2)), null, Utc(24).AddSeconds(2));
        host.ProcessTrade(Trade(3, 100.0m, at: Utc(24, 5)), null, Utc(24).AddSeconds(5));
        var a = host.Current!.CurrentAuction!;
        Assert.Equal(TimeSpan.FromSeconds(2), a.MinimumTradeInterval);
        Assert.Equal(TimeSpan.FromSeconds(3), a.MaximumTradeInterval);
        Assert.Equal(TimeSpan.FromSeconds(3), a.LatestTradeInterval);
        Assert.DoesNotContain("FAST TAPE", string.Join(' ', host.Current.Limitations), StringComparison.OrdinalIgnoreCase);
    }

    // --- G coverage ---

    [Fact]
    public void G_MidAuctionCoverage_ThenNextAuctionFromStart()
    {
        var host = Host();
        host.RebuildContext(Profiles("PI-1"), null, Utc(24));
        host.ProcessTrade(Trade(1, 100.0m, auction: "PI-1"), null, Utc(24).AddSeconds(1));
        Assert.Equal(OrderflowCoverageMode.LiveOnlyMidAuction, host.Current!.CurrentAuction!.CoverageMode);

        host.RebuildContext(Profiles("PI-2"), null, Utc(25));
        host.ProcessTrade(Trade(2, 100.0m, auction: "PI-2"), null, Utc(25).AddSeconds(1));
        Assert.Equal(OrderflowCoverageMode.LiveOnlyFromAuctionStart, host.Current!.CurrentAuction!.CoverageMode);
        Assert.Contains(ExecutedOrderflowPolicyConfig.LimitationHistoryLiveOnly, host.Current.Limitations);
    }

    // --- H runtime ---

    [Fact]
    public void H_ReadyDoesNotClearDegraded_Schema080()
    {
        var host = Host();
        host.RebuildContext(Profiles(), null, Utc(24));
        host.ProcessTrade(Trade(1, 100.0m, ask: true), null, Utc(24).AddSeconds(1));
        host.ProcessTrade(Trade(2, 100.0m, ask: false, bid: true), null, Utc(24).AddSeconds(2));

        var runtime = new GcaeRuntimeEngine();
        var snap = runtime.Publish(
            null, "GCQ6", DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, DateTime.UtcNow, false, false, false, false, false, false,
            Profiles(), executedOrderflow: host.Current);
        Assert.Equal("0.21.0", snap.Version);
        Assert.NotEqual(DataState.Ready, snap.DataGate.DataState);
        Assert.Same(host.Current, snap.ExecutedOrderflow);
    }

    // --- I UI / scope ---

    [Fact]
    public void I_GpsRows_ClassifiedDelta_NoProhibited_Phase2BNotStarted()
    {
        var host = Host();
        host.RebuildContext(Profiles(), null, Utc(24));
        host.ProcessTrade(Trade(1, 100.0m, 2m, ask: true), null, Utc(24).AddSeconds(1));
        host.ProcessTrade(Trade(2, 100.0m, 1m, ask: false, bid: false), null, Utc(24).AddSeconds(2));

        var runtime = new GcaeRuntimeEngine();
        var snap = runtime.Publish(
            null, "GCQ6", DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, DateTime.UtcNow, false, false, false, false, false, false,
            Profiles(), executedOrderflow: host.Current, showExecutedOrderflowDiagnostics: true);
        var vm = AuctionGpsCardMapper.FromSnapshot(snap, true);
        var text = string.Join('\n', vm.ProfileDetailLines.Concat(vm.DiagnosticRows));
        Assert.Contains("ORDERFLOW:", text, StringComparison.Ordinal);
        Assert.Contains("EXECUTED_ORDERFLOW_POLICY_V1", text, StringComparison.Ordinal);
        Assert.Contains("CLASSIFIED DELTA:", text, StringComparison.Ordinal);
        Assert.Contains("ORDERFLOW HISTORY: LIVE_ONLY", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ABSORPTION", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EXHAUSTION", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TRADE FACILITATION HEALTHY", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TRADE FACILITATION FAILING", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PROBABILITY", text, StringComparison.Ordinal);
        Assert.Contains("MBO: BLOCKED", vm.MboLine, StringComparison.Ordinal);

        var root = FindRepoRoot();
        Assert.True(Directory.Exists(Path.Combine(root, "src", "GC.AuctionFlow", "Orderflow")));
        Assert.True(Directory.Exists(Path.Combine(root, "src", "GC.AuctionFlow", "Cluster")));
        Assert.False(Directory.Exists(Path.Combine(root, "src", "GC.AuctionFlow", "TradeFacilitation")));
        Assert.True(Directory.Exists(Path.Combine(root, "src", "GC.AuctionFlow", "Thesis")));
        Assert.False(Directory.Exists(Path.Combine(root, "src", "GC.AuctionFlow", "Far")));
        var indicator = File.ReadAllText(Path.Combine(root, "src", "GC.AuctionFlow", "Atas", "GcAuctionFlowIndicator.cs"));
        Assert.Contains("EnableExecutedOrderflow = false", indicator, StringComparison.Ordinal);
        Assert.Contains("EnableClusterRawFeatures = false", indicator, StringComparison.Ordinal);
        Assert.Contains("EnableTradeFacilitation", indicator, StringComparison.Ordinal);
        Assert.DoesNotContain("ImbalanceThreshold", indicator, StringComparison.Ordinal);
        Assert.DoesNotContain("EnableThesis ", indicator, StringComparison.Ordinal);
    }

    [Fact]
    public void Policy_Version()
    {
        Assert.Equal("EXECUTED_ORDERFLOW_POLICY_V1", ExecutedOrderflowPolicyConfig.PolicyVersion);
        Assert.Equal("0.21.0", GcaeRuntimeSnapshot.SnapshotVersion);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "GC.AuctionFlow.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repo root not found.");
    }
}
