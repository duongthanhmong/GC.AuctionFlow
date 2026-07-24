using GC.AuctionFlow.Core;
using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Reference;
using GC.AuctionFlow.Runtime;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Episode;

/// <summary>
/// Phase 1E live AWAITING TRADES wiring fix — Episode admission independent of TradeStreamProbe gates.
/// </summary>
public sealed class Phase1EEpisodeTradeWiringTests
{
    private const decimal Tick = 0.1m;
    private const string Instrument = "GCQ6";
    private const string Epoch = "GCQ6|tick=0.1";
    private const string Auction = "PI-2026-07-24";

    private static AuctionEpisodeHost Host(bool enabled = true) =>
        new(Tick, Instrument, Epoch, AtasTimestampNormalizer.PolicyVersion, new EpisodePolicyConfig(enabled));

    private static StructuralReferenceSnapshot Ref(ReferenceType type, decimal price)
    {
        var grid = new PriceGrid(Tick);
        var t = grid.ToTickIndex(price);
        var id = ReferenceIdentity.Build(Instrument, Epoch, "PI-2026-07-23", type, ReferenceMaturity.Confirmed);
        return new StructuralReferenceSnapshot(
            id, type, price, price, t, t,
            "PI-2026-07-23", ReferenceSourceKind.PreviousPrimaryAuction,
            ReferenceSourceHorizon.PreviousPrimaryAuction, Utc(23),
            ReferenceMaturity.Confirmed, ReferenceStatus.Active, ReferenceEvidenceTier.ProfileDerived,
            Tick, Epoch, AtasTimestampNormalizer.PolicyVersion, 1, Utc(23), Utc(23));
    }

    private static StructuralReferenceSetSnapshot RefSet(params StructuralReferenceSnapshot[] confirmed) =>
        new(
            StructuralReferenceModuleState.Ready,
            ReferencePolicyConfig.PolicyVersion,
            confirmed,
            Array.Empty<StructuralReferenceSnapshot>(),
            Array.Empty<StructuralReferenceSnapshot>(),
            Array.Empty<ReferenceConfluenceGroup>(),
            null, "fp", 1, Array.Empty<string>(), Array.Empty<string>(), Utc(24));

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

    private static EpisodeTradeEvent Trade(long seq, decimal price, decimal vol = 1m, bool ask = true, bool bid = false)
    {
        var grid = new PriceGrid(Tick);
        var tick = grid.ToTickIndex(price);
        return new EpisodeTradeEvent(
            EpisodeIdentity.BuildEventIdentity(seq, "fp-" + seq),
            seq, Utc(24).Ticks, Utc(24), tick, price, vol, ask, bid, ask || bid,
            Instrument, Epoch, Tick, AtasTimestampNormalizer.PolicyVersion);
    }

    private static NewTradeObservation Obs(long seq, decimal price) =>
        new(
            TradeCallbackSource.OnNewTradesBatch, seq, Utc(24).Ticks, DateTimeKind.Utc,
            Utc(24), 0, Instrument, "fp-" + seq, null,
            price, 1m, price, "Buy", "Trade", true, false, null, null, null);

    private static DateTime Utc(int day) => new(2026, 7, day, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void EnableAfterInit_NextTrade_LeavesAwaitingTrades()
    {
        var host = Host(enabled: false);
        host.Configure(Tick, Instrument, Epoch, new EpisodePolicyConfig(false));
        host.Configure(Tick, Instrument, Epoch, new EpisodePolicyConfig(true));
        var r = Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m);
        host.RebuildContext(Profiles(), RefSet(r), null, Utc(24));
        Assert.Equal(EpisodeModuleState.AwaitingTrades, host.Current!.ModuleState);

        host.ProcessTrade(Trade(1, 100.0m), Utc(24).AddSeconds(1)); // no touch
        Assert.True(host.Current!.ModuleState is EpisodeModuleState.Partial or EpisodeModuleState.Ready);
        Assert.Empty(host.Current.ActiveEpisodes);
        Assert.Equal(1, host.TradeEventsAccepted);
    }

    [Fact]
    public void FirstAcceptedTrade_NoInteraction_PartialWithZeroActive()
    {
        var host = Host();
        host.RebuildContext(Profiles(), RefSet(Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m)), null, Utc(24));
        host.ProcessTrade(Trade(1, 99.0m, ask: false, bid: false), Utc(24).AddSeconds(1));
        Assert.Equal(EpisodeModuleState.Partial, host.Current!.ModuleState);
        Assert.Empty(host.Current.ActiveEpisodes);
        Assert.True(host.Current.TradeEventsAccepted > 0);
    }

    [Fact]
    public void TradeOutsideAllReferences_DoesNotFabricateEpisode()
    {
        var host = Host();
        host.RebuildContext(Profiles(), RefSet(
            Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m),
            Ref(ReferenceType.PreviousPrimaryTpoVal, 99.5m)), null, Utc(24));
        host.ProcessTrade(Trade(1, 100.0m), Utc(24).AddSeconds(1));
        Assert.Empty(host.Current!.ActiveEpisodes);
        Assert.True(host.Current.ModuleState is EpisodeModuleState.Partial or EpisodeModuleState.Ready);
    }

    [Fact]
    public void ExactTouch_CreatesInteracting()
    {
        var host = Host();
        host.RebuildContext(Profiles(), RefSet(Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m)), null, Utc(24));
        host.ProcessTrade(Trade(1, 100.2m), Utc(24).AddSeconds(1));
        Assert.Single(host.Current!.ActiveEpisodes);
        Assert.Equal(EpisodeState.Interacting, host.Current.ActiveEpisodes[0].State);
    }

    [Fact]
    public void MappingReject_LeavesAwaitingTrades_ExposesReason()
    {
        var host = Host();
        host.RebuildContext(Profiles(), RefSet(Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m)), null, Utc(24));
        host.NoteMappingReject("EPISODE_TICK_MAPPING_FAILED", Utc(24));
        Assert.Equal(EpisodeModuleState.AwaitingTrades, host.Current!.ModuleState);
        Assert.Equal("EPISODE_TICK_MAPPING_FAILED", host.LastTradeRejectReason);
        Assert.Equal(0, host.TradeEventsAccepted);
        Assert.Equal(1, host.TradeEventsRejected);
    }

    [Fact]
    public void OutOfOrder_RejectedFailClosed()
    {
        var host = Host();
        host.RebuildContext(Profiles(), RefSet(Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m)), null, Utc(24));
        host.ProcessTrade(Trade(5, 100.0m), Utc(24).AddSeconds(1));
        host.ProcessTrade(Trade(2, 100.0m), Utc(24).AddSeconds(2));
        Assert.Equal(1, host.TradeEventsAccepted);
        Assert.Equal(1, host.TradeEventsRejected);
        Assert.Equal(nameof(EpisodeTradeAdmissionResult.OutOfOrder), host.LastTradeRejectReason);
        Assert.True(host.Current!.ModuleState is EpisodeModuleState.Partial or EpisodeModuleState.Ready);
    }

    [Fact]
    public void Duplicate_Idempotent_DoesNotChangeState()
    {
        var host = Host();
        host.RebuildContext(Profiles(), RefSet(Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m)), null, Utc(24));
        var t = Trade(1, 100.2m);
        host.ProcessTrade(t, Utc(24).AddSeconds(1));
        var rev = host.Current!.RegistryRevision;
        var state = host.Current.ModuleState;
        host.ProcessTrade(t, Utc(24).AddSeconds(2));
        Assert.Equal(1, host.TradeEventsDuplicate);
        Assert.Equal(rev, host.Current!.RegistryRevision);
        Assert.Equal(state, host.Current.ModuleState);
    }

    [Fact]
    public void AuctionRollover_Rebinds_AcceptsFirstNewAuctionTrade()
    {
        var host = Host();
        var r = Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m);
        host.RebuildContext(Profiles(Auction), RefSet(r), null, Utc(24));
        host.ProcessTrade(Trade(1, 100.0m), Utc(24).AddSeconds(1));
        Assert.True(host.Current!.ModuleState is EpisodeModuleState.Partial or EpisodeModuleState.Ready);

        host.RebuildContext(Profiles("PI-2026-07-25"), RefSet(r), null, Utc(25));
        Assert.Equal(EpisodeModuleState.AwaitingTrades, host.Current!.ModuleState);
        Assert.Empty(host.Current.ActiveEpisodes);

        host.ProcessTrade(Trade(1, 100.0m), Utc(25).AddSeconds(1));
        Assert.True(host.Current!.ModuleState is EpisodeModuleState.Partial or EpisodeModuleState.Ready);
        Assert.Equal("PI-2026-07-25", host.Current.PrimaryAuctionId);
    }

    [Fact]
    public void EligibleRefresh_DoesNotEraseAcceptedTradeState()
    {
        var host = Host();
        var r1 = Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m);
        host.RebuildContext(Profiles(), RefSet(r1), null, Utc(24));
        host.ProcessTrade(Trade(1, 100.0m), Utc(24).AddSeconds(1));
        Assert.True(host.Current!.ModuleState is EpisodeModuleState.Partial or EpisodeModuleState.Ready);

        var r2 = Ref(ReferenceType.PreviousPrimaryTpoVal, 99.5m);
        host.RebuildContext(Profiles(), RefSet(r1, r2), null, Utc(24).AddMinutes(1));
        Assert.True(host.Current!.ModuleState is EpisodeModuleState.Partial or EpisodeModuleState.Ready);
        Assert.Equal(2, host.Current.EligibleReferenceCount);
        Assert.True(host.Current.TradeEventsAccepted >= 1);
    }

    [Fact]
    public void OrdinaryRepublish_CannotOverwritePartialWithAwaitingTrades()
    {
        var host = Host();
        var r = Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m);
        host.RebuildContext(Profiles(), RefSet(r), null, Utc(24));
        host.ProcessTrade(Trade(1, 100.0m), Utc(24).AddSeconds(1));
        var before = host.Current!;
        Assert.True(before.ModuleState is EpisodeModuleState.Partial or EpisodeModuleState.Ready);

        // Identical fingerprint rebuild (ordinary publish path)
        var again = host.RebuildContext(Profiles(), RefSet(r), null, Utc(24).AddSeconds(2));
        Assert.True(again.ModuleState is EpisodeModuleState.Partial or EpisodeModuleState.Ready);
        Assert.NotEqual(EpisodeModuleState.AwaitingTrades, again.ModuleState);
    }

    [Fact]
    public void TryFromNewTrade_NullTick_LeavesNoAccepted()
    {
        var obs = Obs(1, 100.05m); // off-grid for 0.1 tick if TryToTickIndex rejects
        var evt = EpisodeTradeEvent.TryFromNewTrade(
            obs, Tick, Epoch, AtasTimestampNormalizer.PolicyVersion,
            _ => null);
        Assert.Null(evt);
    }

    [Fact]
    public void DiagnosticsCounters_DoNotAlterGeometry()
    {
        var host = Host();
        host.RebuildContext(Profiles(), RefSet(Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m)), null, Utc(24));
        host.ProcessTrade(Trade(1, 100.0m), Utc(24).AddSeconds(1));
        Assert.Empty(host.Current!.ActiveEpisodes);
        Assert.Equal(1, host.TradeEventsSeen);
        Assert.Equal(1, host.TradeEventsAccepted);
        Assert.Equal(0, host.TradeEventsDuplicate);
        Assert.Equal(0, host.TradeEventsRejected);
        Assert.NotNull(host.LastAcceptedTradeEventId);
        Assert.Equal(1L, host.LastAcceptedTradeSequence);
    }

    [Fact]
    public void GpsDiagnostics_ShowAdmissionCounters()
    {
        var host = Host();
        host.RebuildContext(Profiles(), RefSet(Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m)), null, Utc(24));
        host.ProcessTrade(Trade(1, 100.0m), Utc(24).AddSeconds(1));
        var lines = AuctionGpsCardMapper.BuildAuctionEpisodeLines(host.Current, showDiagnostics: true);
        var text = string.Join('\n', lines);
        Assert.Contains("EPISODE TRADE EVENTS ACCEPTED:", text, StringComparison.Ordinal);
        Assert.Contains("EPISODE TRADE EVENTS SEEN:", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SWEEP", text, StringComparison.Ordinal);
        Assert.DoesNotContain("AcceptanceOutside", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Source_OnNewTrades_AdmitsEpisode_BeforeProbeGate()
    {
        var root = FindRepoRoot();
        var src = File.ReadAllText(Path.Combine(root, "src", "GC.AuctionFlow", "Atas", "GcAuctionFlowIndicator.cs"));
        Assert.Contains("Episode admission is independent of TradeStreamProbe enable/gates", src, StringComparison.Ordinal);
        Assert.Contains("TryProcessEpisodeTrade(obs)", src, StringComparison.Ordinal);

        // Probe gate must not wrap Episode admission in OnNewTrades body order.
        var onNewTrades = src.IndexOf("protected override void OnNewTrades", StringComparison.Ordinal);
        Assert.True(onNewTrades > 0);
        var segment = src.Substring(onNewTrades, Math.Min(3500, src.Length - onNewTrades));
        var episodeAdmit = segment.IndexOf("TryProcessEpisodeTrade(obs)", StringComparison.Ordinal);
        var probeGate = segment.IndexOf("EvaluateGates(EnableTradeStreamProbe", StringComparison.Ordinal);
        Assert.True(episodeAdmit > 0 && probeGate > 0);
        Assert.True(episodeAdmit < probeGate);
    }

    [Fact]
    public void RuntimePublish_ReusesPartialSnapshot()
    {
        var host = Host();
        host.RebuildContext(Profiles(), RefSet(Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m)), null, Utc(24));
        host.ProcessTrade(Trade(1, 100.0m, ask: false, bid: false), Utc(24).AddSeconds(1));
        var runtime = new GcaeRuntimeEngine();
        var snap = runtime.Publish(
            null, "GCQ6", DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, DateTime.UtcNow, false, false, false, false, false, false,
            Profiles(), auctionEpisodes: host.Current, showAuctionEpisodeDiagnostics: true);
        Assert.Equal(EpisodeModuleState.Partial, snap.AuctionEpisodes!.ModuleState);
        Assert.True(snap.AuctionEpisodes.TradeEventsAccepted > 0);
        Assert.Empty(snap.AuctionEpisodes.ActiveEpisodes);
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
