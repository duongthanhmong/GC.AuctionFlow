using GC.AuctionFlow.Core;
using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Reference;
using GC.AuctionFlow.Runtime;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Episode;

/// <summary>Phase 1E Auction Episode Observation Foundation — geometric, no Acceptance/Sweep.</summary>
public sealed class Phase1EAuctionEpisodeTests
{
    private const decimal Tick = 0.1m;
    private const string Instrument = "GCQ6";
    private const string Epoch = "GCQ6|tick=0.1";
    private const string Auction = "PI-2026-07-24";

    private static StructuralReferenceSnapshot Ref(
        ReferenceType type,
        decimal price,
        ReferenceMaturity maturity = ReferenceMaturity.Confirmed,
        ReferenceStatus status = ReferenceStatus.Active,
        string sourceId = "PI-2026-07-23")
    {
        var grid = new PriceGrid(Tick);
        var t = grid.ToTickIndex(price);
        var id = ReferenceIdentity.Build(Instrument, Epoch, sourceId, type, maturity);
        var kind = maturity == ReferenceMaturity.Confirmed && type.ToString().StartsWith("Composite", StringComparison.Ordinal)
            ? ReferenceSourceKind.ConfirmedComposite
            : (maturity == ReferenceMaturity.Developing
                ? ReferenceSourceKind.CurrentPrimaryAuction
                : ReferenceSourceKind.PreviousPrimaryAuction);
        var horizon = kind switch
        {
            ReferenceSourceKind.ConfirmedComposite => ReferenceSourceHorizon.ConfirmedComposite,
            ReferenceSourceKind.CurrentPrimaryAuction => ReferenceSourceHorizon.CurrentPrimaryAuction,
            _ => ReferenceSourceHorizon.PreviousPrimaryAuction
        };
        return new StructuralReferenceSnapshot(
            id, type, price, price, t, t,
            sourceId, kind, horizon, Utc(23),
            maturity, status, ReferenceEvidenceTier.ProfileDerived,
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
            null,
            "fp",
            1,
            Array.Empty<string>(),
            Array.Empty<string>(),
            Utc(24));

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
        var fp = "fp-" + seq;
        return new EpisodeTradeEvent(
            EpisodeIdentity.BuildEventIdentity(seq, fp),
            seq, seq * 1000, Utc(24).AddMilliseconds(seq),
            tick, price, vol, ask, bid, ask || bid,
            Instrument, Epoch, Tick, AtasTimestampNormalizer.PolicyVersion);
    }

    private static AuctionEpisodeHost Host() =>
        new(Tick, Instrument, Epoch, AtasTimestampNormalizer.PolicyVersion, new EpisodePolicyConfig(true));

    private static DateTime Utc(int day) => new(2026, 7, day, 12, 0, 0, DateTimeKind.Utc);

    // --- A. Roles ---

    [Theory]
    [InlineData(ReferenceType.PreviousPrimaryAuctionHigh, ReferenceInteractionRole.UpperBoundary)]
    [InlineData(ReferenceType.PreviousPrimaryTpoVah, ReferenceInteractionRole.UpperBoundary)]
    [InlineData(ReferenceType.CompositeRangeHigh, ReferenceInteractionRole.UpperBoundary)]
    [InlineData(ReferenceType.PreviousPrimaryAuctionLow, ReferenceInteractionRole.LowerBoundary)]
    [InlineData(ReferenceType.PreviousPrimaryTpoVal, ReferenceInteractionRole.LowerBoundary)]
    [InlineData(ReferenceType.CompositeRangeLow, ReferenceInteractionRole.LowerBoundary)]
    [InlineData(ReferenceType.PreviousPrimaryTpoPoc, ReferenceInteractionRole.Centerline)]
    [InlineData(ReferenceType.PreviousPrimaryVpoc, ReferenceInteractionRole.Centerline)]
    [InlineData(ReferenceType.CompositeTpoPoc, ReferenceInteractionRole.Centerline)]
    [InlineData(ReferenceType.CurrentPrimaryTpoPoc, ReferenceInteractionRole.Unsupported)]
    public void A_RoleMapping(ReferenceType type, ReferenceInteractionRole expected) =>
        Assert.Equal(expected, ReferenceInteractionRoleMapper.Map(type));

    [Fact]
    public void A_DevelopingExcluded_RetiredExcluded()
    {
        var developing = Ref(ReferenceType.CurrentPrimaryTpoVah, 100.2m, ReferenceMaturity.Developing);
        var retired = Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m, status: ReferenceStatus.Retired);
        var ok = Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m);
        var set = new StructuralReferenceSetSnapshot(
            StructuralReferenceModuleState.Ready, ReferencePolicyConfig.PolicyVersion,
            new[] { ok, retired }, new[] { developing }, Array.Empty<StructuralReferenceSnapshot>(),
            Array.Empty<ReferenceConfluenceGroup>(), null, "fp", 1,
            Array.Empty<string>(), Array.Empty<string>(), Utc(24));
        var eligible = AuctionEpisodeHost.SelectEligible(set);
        Assert.Single(eligible);
        Assert.Equal(ok.ReferenceId, eligible[0].ReferenceId);
    }

    // --- B/C. Creation and attempts ---

    [Fact]
    public void B_ExactTouch_CreatesInteracting()
    {
        var host = Host();
        var r = Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m);
        host.RebuildContext(Profiles(), RefSet(r), null, Utc(24));
        host.ProcessTrade(Trade(1, 100.2m), Utc(24).AddSeconds(1));
        var ep = Assert.Single(host.Current!.ActiveEpisodes);
        Assert.Equal(EpisodeState.Interacting, ep.State);
        Assert.Equal(0, ep.AttemptCount);
        Assert.Equal(EpisodeResolution.None, ep.Resolution);
    }

    [Fact]
    public void B_CrossUpper_CreatesOutsideAttempt()
    {
        var host = Host();
        var r = Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m);
        host.RebuildContext(Profiles(), RefSet(r), null, Utc(24));
        host.ProcessTrade(Trade(1, 100.3m), Utc(24).AddSeconds(1));
        var ep = Assert.Single(host.Current!.ActiveEpisodes);
        Assert.Equal(EpisodeState.OutsideAttempt, ep.State);
        Assert.Equal(1, ep.AttemptCount);
        Assert.Equal(ReferenceInteractionRole.UpperBoundary, ep.ReferenceRole);
    }

    [Fact]
    public void C_ContinuedOutside_NoExtraAttempt_ThenReentryThenSecondAttempt()
    {
        var host = Host();
        var r = Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m);
        host.RebuildContext(Profiles(), RefSet(r), null, Utc(24));
        host.ProcessTrade(Trade(1, 100.3m), Utc(24).AddSeconds(1));
        var id = host.Current!.ActiveEpisodes[0].EpisodeId;
        host.ProcessTrade(Trade(2, 100.4m), Utc(24).AddSeconds(2));
        host.ProcessTrade(Trade(3, 100.5m), Utc(24).AddSeconds(3));
        var mid = host.Current!.ActiveEpisodes[0];
        Assert.Equal(id, mid.EpisodeId);
        Assert.Equal(1, mid.AttemptCount);
        Assert.Equal(EpisodeState.Developing, mid.State);
        Assert.True(mid.MaximumCanonicalOutsideDistanceTicks >= 3);

        host.ProcessTrade(Trade(4, 100.1m), Utc(24).AddSeconds(4)); // inside
        var re = host.Current!.ActiveEpisodes[0];
        Assert.Equal(EpisodeState.ReentryDeveloping, re.State);
        Assert.Equal(1, re.AttemptCount);

        host.ProcessTrade(Trade(5, 100.3m), Utc(24).AddSeconds(5));
        var second = host.Current!.ActiveEpisodes[0];
        Assert.Equal(id, second.EpisodeId);
        Assert.Equal(2, second.AttemptCount);
        Assert.Equal(EpisodeState.OutsideAttempt, second.State);
    }

    [Fact]
    public void C_DuplicateTrade_Idempotent()
    {
        var host = Host();
        var r = Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m);
        host.RebuildContext(Profiles(), RefSet(r), null, Utc(24));
        var t = Trade(1, 100.3m);
        host.ProcessTrade(t, Utc(24).AddSeconds(1));
        var v1 = host.Current!.ActiveEpisodes[0].EventRevision;
        host.ProcessTrade(t, Utc(24).AddSeconds(2));
        Assert.Equal(v1, host.Current!.ActiveEpisodes[0].EventRevision);
        Assert.Equal(1, host.Current.ActiveEpisodes[0].AttemptCount);
    }

    [Fact]
    public void C_Centerline_NoOutsideAttempt_UsesCrossCount()
    {
        var host = Host();
        var r = Ref(ReferenceType.PreviousPrimaryTpoPoc, 100.0m);
        host.RebuildContext(Profiles(), RefSet(r), null, Utc(24));
        host.ProcessTrade(Trade(1, 100.0m), Utc(24).AddSeconds(1));
        host.ProcessTrade(Trade(2, 100.2m), Utc(24).AddSeconds(2));
        host.ProcessTrade(Trade(3, 99.8m), Utc(24).AddSeconds(3));
        var ep = Assert.Single(host.Current!.ActiveEpisodes);
        Assert.NotEqual(EpisodeState.OutsideAttempt, ep.State);
        Assert.NotEqual(EpisodeState.ReentryDeveloping, ep.State);
        Assert.Equal(0, ep.AttemptCount);
        Assert.True(ep.CrossCount >= 1 || ep.UpExcursionCount + ep.DownExcursionCount >= 1);
        Assert.Equal(EpisodeState.Developing, ep.State);
    }

    [Fact]
    public void C_ReservedStatesNeverEmitted()
    {
        var host = Host();
        var r = Ref(ReferenceType.PreviousPrimaryTpoVal, 99.8m);
        host.RebuildContext(Profiles(), RefSet(r), null, Utc(24));
        host.ProcessTrade(Trade(1, 99.7m), Utc(24).AddSeconds(1));
        host.ProcessTrade(Trade(2, 99.9m), Utc(24).AddSeconds(2));
        var ep = Assert.Single(host.Current!.ActiveEpisodes);
        Assert.DoesNotContain(ep.State, new[]
        {
            EpisodeState.ApproachingReference,
            EpisodeState.AcceptanceOutside,
            EpisodeState.ReacceptedInside,
            EpisodeState.ReentryFailed,
            EpisodeState.UnresolvedRotation,
            EpisodeState.TransitionedToNewBalance
        });
        Assert.Equal(EpisodeResolution.None, ep.Resolution);
    }

    // --- D. Metrics ---

    [Fact]
    public void D_OutsideMetrics_ExcludeInside_AggressorUnavailable()
    {
        var host = Host();
        var r = Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m);
        host.RebuildContext(Profiles(), RefSet(r), null, Utc(24));
        host.ProcessTrade(Trade(1, 100.3m, 5m, ask: true), Utc(24).AddSeconds(1));
        host.ProcessTrade(Trade(2, 100.1m, 99m, ask: true), Utc(24).AddSeconds(2)); // inside — not outside volume
        var ep = host.Current!.ActiveEpisodes[0];
        Assert.Equal(5m, ep.CanonicalOutsideExecutedVolume);
        Assert.Equal(1, ep.CanonicalOutsideTradeCount);

        var unc = Trade(3, 100.4m, 2m, ask: false, bid: false);
        // force unclassified
        unc = new EpisodeTradeEvent(unc.EventIdentity, 3, 3000, Utc(24).AddSeconds(3), unc.PriceTick, 100.4m, 2m,
            false, false, false, Instrument, Epoch, Tick, AtasTimestampNormalizer.PolicyVersion);
        host.ProcessTrade(unc, Utc(24).AddSeconds(3));
        Assert.Equal(EpisodeModuleState.Partial, host.Current!.ModuleState);
        Assert.Null(host.Current.ActiveEpisodes[0].CanonicalOutsideBidVolume);
    }

    // --- E/F. Identity, revision, dedup, lifecycle ---

    [Fact]
    public void E_EpisodeIdStable_AcrossAttempts()
    {
        var host = Host();
        var r = Ref(ReferenceType.PreviousPrimaryAuctionHigh, 100.5m);
        host.RebuildContext(Profiles(), RefSet(r), null, Utc(24));
        host.ProcessTrade(Trade(1, 100.6m), Utc(24).AddSeconds(1));
        var id = host.Current!.ActiveEpisodes[0].EpisodeId;
        host.ProcessTrade(Trade(2, 100.4m), Utc(24).AddSeconds(2));
        host.ProcessTrade(Trade(3, 100.6m), Utc(24).AddSeconds(3));
        Assert.Equal(id, host.Current!.ActiveEpisodes[0].EpisodeId);
        Assert.StartsWith("EP|", id, StringComparison.Ordinal);
        Assert.Contains(Auction, id, StringComparison.Ordinal);
        Assert.Contains(EpisodePolicyConfig.PolicyVersion, id, StringComparison.Ordinal);
    }

    [Fact]
    public void G_AuctionChange_ExpiresAndClearsLedger()
    {
        var host = Host();
        var r = Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m);
        host.RebuildContext(Profiles(Auction), RefSet(r), null, Utc(24));
        host.ProcessTrade(Trade(1, 100.3m), Utc(24).AddSeconds(1));
        Assert.Single(host.Current!.ActiveEpisodes);

        host.RebuildContext(Profiles("PI-2026-07-25"), RefSet(r), null, Utc(25));
        Assert.Empty(host.Current!.ActiveEpisodes);
        Assert.Contains(host.Current.RecentlyClosedEpisodes, e => e.State == EpisodeState.EpisodeExpired);
        Assert.Contains(host.Current.RecentlyClosedEpisodes,
            e => e.Resolution == EpisodeResolution.Expired);

        // Same event identity can process again in new auction (ledger cleared)
        host.ProcessTrade(Trade(1, 100.3m), Utc(25).AddSeconds(1));
        Assert.Single(host.Current!.ActiveEpisodes);
        Assert.Equal("PI-2026-07-25", host.Current.ActiveEpisodes[0].PrimaryAuctionId);
    }

    [Fact]
    public void F_OutOfOrderSequence_Ignored()
    {
        var host = Host();
        var r = Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m);
        host.RebuildContext(Profiles(), RefSet(r), null, Utc(24));
        host.ProcessTrade(Trade(5, 100.3m), Utc(24).AddSeconds(1));
        var rev = host.Current!.ActiveEpisodes[0].EventRevision;
        host.ProcessTrade(Trade(2, 100.4m), Utc(24).AddSeconds(2));
        Assert.Equal(rev, host.Current!.ActiveEpisodes[0].EventRevision);
    }

    [Fact]
    public void H_HistoryMode_LiveOnly()
    {
        var host = Host();
        host.RebuildContext(Profiles(), RefSet(Ref(ReferenceType.PreviousPrimaryTpoVal, 99.8m)), null, Utc(24));
        Assert.Equal(EpisodeHistoryMode.LiveOnly, host.HistoryMode);
        Assert.Contains(EpisodePolicyConfig.LimitationHistoryLiveOnly, host.Current!.Limitations);
    }

    // --- I/J. Runtime / UI / scope ---

    [Fact]
    public void I_Disabled_Awaiting_ReadyDoesNotClearDegraded()
    {
        var off = new AuctionEpisodeHost(Tick, Instrument, Epoch, AtasTimestampNormalizer.PolicyVersion,
            new EpisodePolicyConfig(false));
        Assert.Equal(EpisodeModuleState.Disabled, off.RebuildContext(Profiles(), RefSet(), null).ModuleState);

        var host = Host();
        Assert.Equal(EpisodeModuleState.AwaitingReferences, host.RebuildContext(Profiles(), null, null).ModuleState);
        var r = Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m);
        Assert.Equal(EpisodeModuleState.AwaitingTrades, host.RebuildContext(Profiles(), RefSet(r), null).ModuleState);

        host.ProcessTrade(Trade(1, 100.3m, ask: true), Utc(24).AddSeconds(1));
        Assert.True(host.Current!.ModuleState is EpisodeModuleState.Ready or EpisodeModuleState.Partial);

        var runtime = new GcaeRuntimeEngine();
        var snap = runtime.Publish(
            null, "GCQ6", DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, DateTime.UtcNow, false, false, false, false, false, false,
            Profiles(), auctionEpisodes: host.Current);
        Assert.Equal("0.9.0", snap.Version);
        Assert.NotEqual(DataState.Ready, snap.DataGate.DataState);
    }

    [Fact]
    public void J_GpsRows_NoProhibitedWording_ThesisFarNotStarted()
    {
        var host = Host();
        var r = Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m);
        host.RebuildContext(Profiles(), RefSet(r), null, Utc(24));
        host.ProcessTrade(Trade(1, 100.3m), Utc(24).AddSeconds(1));
        var runtime = new GcaeRuntimeEngine();
        var snap = runtime.Publish(
            null, "GCQ6", DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, DateTime.UtcNow, false, false, false, false, false, false,
            Profiles(), auctionEpisodes: host.Current, showAuctionEpisodeDiagnostics: true);
        var vm = AuctionGpsCardMapper.FromSnapshot(snap, true);
        var text = string.Join('\n', vm.ProfileDetailLines.Concat(vm.DiagnosticRows));
        Assert.Contains("EPISODES:", text, StringComparison.Ordinal);
        Assert.Contains("AUCTION_EPISODE_POLICY_V1", text, StringComparison.Ordinal);
        Assert.Contains("ELIGIBLE REFERENCES:", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SWEEP", text, StringComparison.Ordinal);
        Assert.DoesNotContain("FAR", text, StringComparison.Ordinal);
        Assert.DoesNotContain("AAC", text, StringComparison.Ordinal);
        Assert.DoesNotContain("AcceptanceOutside", text, StringComparison.Ordinal);
        Assert.DoesNotContain("LONG", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SHORT", text, StringComparison.Ordinal);
        Assert.Contains("MBO: BLOCKED", vm.MboLine, StringComparison.Ordinal);

        var root = FindRepoRoot();
        Assert.True(Directory.Exists(Path.Combine(root, "src", "GC.AuctionFlow", "Evidence")));
        Assert.False(Directory.Exists(Path.Combine(root, "src", "GC.AuctionFlow", "Thesis")));
        Assert.False(Directory.Exists(Path.Combine(root, "src", "GC.AuctionFlow", "Far")));
        var indicator = File.ReadAllText(Path.Combine(root, "src", "GC.AuctionFlow", "Atas", "GcAuctionFlowIndicator.cs"));
        Assert.Contains("EnableAuctionEpisodes", indicator, StringComparison.Ordinal);
        Assert.DoesNotContain("AcceptanceOutside", indicator, StringComparison.Ordinal);
        Assert.DoesNotContain("SweepDetector", indicator, StringComparison.Ordinal);
        Assert.DoesNotContain("EnableThesis", indicator, StringComparison.Ordinal);
    }

    [Fact]
    public void Policy_LimitationsRecorded()
    {
        Assert.Equal("AUCTION_EPISODE_POLICY_V1", EpisodePolicyConfig.PolicyVersion);
        Assert.Equal("INTRA_AUCTION_EPISODE_RESET_NOT_CALIBRATED", EpisodePolicyConfig.LimitationIntraAuctionResetNotCalibrated);
        Assert.Equal("APPROACH_DISTANCE_NOT_CALIBRATED", EpisodePolicyConfig.LimitationApproachDistanceNotCalibrated);
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
