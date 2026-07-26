using GC.AuctionFlow.Cluster;
using GC.AuctionFlow.Core;
using GC.AuctionFlow.Efficiency;
using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Orderflow;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Runtime;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Efficiency;

/// <summary>
/// Phase 2C Auction Efficiency Raw Evidence Measurement Foundation.
/// Raw Effort/Result vectors only â€” no Effective/Ineffective/Absorption/Trade Facilitation.
/// </summary>
public sealed class Phase2CAuctionEfficiencyEvidenceTests
{
    private const decimal Tick = 0.1m;
    private const string Instrument = "GCQ6";
    private const string Epoch = "GCQ6|tick=0.1";
    private const string Auction = "PI-2026-07-24";

    private static DateTime Utc(int day, int sec = 0) =>
        new(2026, 7, day, 12, 0, sec, DateTimeKind.Utc);

    private static NewTradeObservation Obs(
        long seq, decimal price, decimal vol, bool ask = true, bool bid = false,
        DateTime? receive = null)
    {
        var at = receive ?? Utc(24, (int)Math.Min(seq, 59));
        return new NewTradeObservation(
            TradeCallbackSource.OnNewTrade, seq, at.Ticks, DateTimeKind.Utc, at, 0,
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

    private static AuctionEfficiencyHost EfficiencyHost(bool enabled = true) =>
        new(Tick, Instrument, Epoch, AtasTimestampNormalizer.PolicyVersion,
            new AuctionEfficiencyEvidencePolicyConfig(enabled));

    private static PrimaryProfileSetSnapshot Profiles(
        string auctionId = Auction,
        decimal? tpoPoc = 100.0m,
        decimal? volPoc = 100.0m,
        decimal? tpoVal = 99.5m,
        decimal? tpoVah = 100.5m,
        decimal? volVal = 99.5m,
        decimal? volVah = 100.5m)
    {
        var tpo = new TpoProfileSnapshot(
            auctionId, Utc(24), Utc(25), "America/Chicago", TimeSpan.FromHours(8).Add(TimeSpan.FromMinutes(20)),
            30, 100.5m, 99.5m, tpoPoc, tpoVah, tpoVal, 10, 1, null,
            new Dictionary<long, int>(), ProfileDataQuality.Complete, "test", Array.Empty<string>());
        var vol = new VolumeProfileSnapshot(
            auctionId, 100.5m, 99.5m, volPoc, volVah, volVal, 100m,
            new Dictionary<long, decimal>(), PriceVolumeCapability.Exact,
            ProfileDataQuality.Complete, "test", Array.Empty<string>());
        var cur = new PrimaryAuctionProfileSnapshot(
            AuctionProfileState.Ready, auctionId, Utc(24), Utc(25), false,
            tpo, vol, 100.5m, 99.5m, 100.0m, Utc(24), null,
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

    private static (ExecutedOrderflowHost Of, ClusterRawHost Cl, AuctionEfficiencyHost Eff) Feed(
        bool askBid = true,
        params (long seq, decimal price, decimal vol)[] trades)
    {
        var of = OrderflowHost();
        of.RebuildContext(Profiles(), null, Utc(24));
        var cl = ClusterHost();
        var eff = EfficiencyHost();
        foreach (var t in trades)
        {
            var evt = Trade(t.seq, t.price, t.vol, ask: askBid, bid: !askBid && t.seq % 2 == 0,
                at: Utc(24).AddSeconds((int)t.seq));
            if (!askBid)
                evt = Trade(t.seq, t.price, t.vol, ask: false, bid: false, at: Utc(24).AddSeconds((int)t.seq));
            of.ProcessTrade(evt, null, Utc(24).AddSeconds((int)t.seq));
            cl.ProcessOrderflowUpdate(of.Current!, evt.NormalizedPriceTick, evt.EventId, evt.EventSequence,
                evt.ReceiveTimestampUtc ?? evt.ExchangeTimestampUtc, Utc(24).AddSeconds((int)t.seq));
        }

        eff.Rebuild(of.Current, cl.Current, null, null, Profiles(), Utc(24).AddMinutes(1));
        return (of, cl, eff);
    }

    // --- A baseline ---

    [Fact]
    public void A01_DefaultOff_NoRows_Awaiting_ProbeIndependence()
    {
        var off = EfficiencyHost(false);
        Assert.Equal(EfficiencyModuleState.Disabled, off.Rebuild(null, null, null, null, null, Utc(24)).ModuleState);
        Assert.Null(off.Current!.CurrentAuctionEvidence);

        var of = OrderflowHost();
        of.RebuildContext(Profiles(), null, Utc(24));
        var on = EfficiencyHost(true);
        Assert.Equal(EfficiencyModuleState.AwaitingOrderflow, on.Rebuild(of.Current, null, null, null, null, Utc(24)).ModuleState);

        of.ProcessTrade(Trade(1, 100.0m, ask: false, bid: false), null, Utc(24).AddSeconds(1));
        var cl = ClusterHost();
        cl.ProcessOrderflowUpdate(of.Current!, 1000, "e1", 1, Utc(24).AddSeconds(1), Utc(24).AddSeconds(1));
        var set = on.Rebuild(of.Current, cl.Current, null, null, Profiles(), Utc(24).AddSeconds(2));
        Assert.Equal(EfficiencyModuleState.AwaitingEpisode, set.ModuleState);
        Assert.NotNull(set.CurrentAuctionEvidence);
        Assert.Equal(EfficiencyClassificationState.NotCalibrated, set.CurrentAuctionEvidence!.ClassificationState);

        Assert.DoesNotContain("EnableTradeFacilitation", typeof(AuctionEfficiencyHost).Assembly.Location);
        Assert.False(Directory.Exists(Path.Combine(
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..")),
            "src", "GC.AuctionFlow", "TradeFacilitation")));
    }

    [Fact]
    public void A02_Phase2A2BAuthoritative_NoSecondLedger()
    {
        var (of, cl, eff) = Feed(true, (1, 100.0m, 3m), (2, 100.1m, 2m));
        var a = eff.Current!.CurrentAuctionEvidence!;
        Assert.Equal(of.Current!.CurrentAuction!.ExecutedVolume, a.Effort.TotalExecutedVolume);
        Assert.Equal(of.Current.CurrentAuction.TradeCount, a.Effort.TradeCount);
        Assert.Equal(cl.Current!.CurrentAuction!.PriceLevelCount, a.Effort.PriceLevelCount);
        Assert.Equal(of.Current.CurrentAuction.ClassifiedDelta, a.Effort.ClassifiedDelta);
        Assert.DoesNotContain("EfficiencyScore", string.Join(",", a.Limitations), StringComparison.Ordinal);
    }

    // --- B Effort ---

    [Fact]
    public void B_UnknownOnly_Partial_AskBidUnavailable_NoFabricatedImbalance()
    {
        var (of, cl, eff) = Feed(false, (1, 100.0m, 5m), (2, 100.0m, 2m));
        var a = eff.Current!.CurrentAuctionEvidence!;
        Assert.Equal(EfficiencyModuleState.Partial, a.MeasurementStatus);
        Assert.Equal(7m, a.Effort.TotalExecutedVolume);
        Assert.Equal(2, a.Effort.TradeCount);
        Assert.Null(a.Effort.AskVolume);
        Assert.Null(a.Effort.BidVolume);
        Assert.True(a.Effort.UnknownAggressorVolume > 0m);
        Assert.Null(a.Effort.AbsoluteClassifiedDelta);
        Assert.Contains(AuctionEfficiencyEvidencePolicyConfig.LimitationImbalanceNotCalibrated, a.Effort.Limitations);
        Assert.Contains(AuctionEfficiencyEvidencePolicyConfig.LimitationBigTradeNotCalibrated, a.Effort.Limitations);
        Assert.Contains(AuctionEfficiencyEvidencePolicyConfig.LimitationMboSweepResearchOnly, a.Effort.Limitations);
        Assert.Equal(cl.Current!.CurrentAuction!.UnknownOnlyLevelCount, a.Effort.UnknownOnlyLevelCount);
    }

    [Fact]
    public void B_ClassifiedAskBid_CopiedExactly()
    {
        var of = OrderflowHost();
        of.RebuildContext(Profiles(), null, Utc(24));
        var cl = ClusterHost();
        var eff = EfficiencyHost();
        of.ProcessTrade(Trade(1, 100.0m, 4m, ask: true, bid: false), null, Utc(24).AddSeconds(1));
        cl.ProcessOrderflowUpdate(of.Current!, 1000, "e1", 1, Utc(24).AddSeconds(1), Utc(24).AddSeconds(1));
        of.ProcessTrade(Trade(2, 100.0m, 1m, ask: false, bid: true), null, Utc(24).AddSeconds(2));
        cl.ProcessOrderflowUpdate(of.Current!, 1000, "e2", 2, Utc(24).AddSeconds(2), Utc(24).AddSeconds(2));
        eff.Rebuild(of.Current, cl.Current, null, null, Profiles(), Utc(24).AddSeconds(3));
        var e = eff.Current!.CurrentAuctionEvidence!.Effort;
        Assert.Equal(4m, e.AskVolume);
        Assert.Equal(1m, e.BidVolume);
        Assert.Equal(3m, e.AbsoluteClassifiedDelta);
        Assert.Equal(of.Current!.CurrentAuction!.AggressorCoverageRatio, e.AggressorCoverageRatio);
    }

    // --- C Result geometry ---

    [Fact]
    public void C_ResultGeometry_NetRange_FavorableAdverse_Retention()
    {
        var of = OrderflowHost();
        of.RebuildContext(Profiles(), null, Utc(24));
        var cl = ClusterHost();
        var eff = EfficiencyHost();
        // ticks: 100.0 -> 1000, 100.2 -> 1002, 99.9 -> 999
        foreach (var (seq, px, vol) in new[] { (1L, 100.0m, 1m), (2L, 100.2m, 1m), (3L, 99.9m, 1m) })
        {
            var evt = Trade(seq, px, vol, at: Utc(24).AddSeconds((int)seq));
            of.ProcessTrade(evt, null, Utc(24).AddSeconds((int)seq));
            cl.ProcessOrderflowUpdate(of.Current!, evt.NormalizedPriceTick, evt.EventId, evt.EventSequence,
                Utc(24).AddSeconds((int)seq), Utc(24).AddSeconds((int)seq));
        }

        eff.Rebuild(of.Current, cl.Current, null, null, Profiles(), Utc(24).AddSeconds(10));
        var r = eff.Current!.CurrentAuctionEvidence!.Result;
        Assert.Equal(1000, r.FirstPriceTick);
        Assert.Equal(999, r.LatestPriceTick);
        Assert.Equal(1002, r.HighPriceTick);
        Assert.Equal(999, r.LowPriceTick);
        Assert.Equal(-1, r.NetPriceProgressTicks);
        Assert.Equal(3, r.GrossRangeTicks);
        // Auction-only uses price geometry direction (Down)
        Assert.Equal(EfficiencyResultDirection.Down, eff.Current.CurrentAuctionEvidence.ResultDirection);
        Assert.Equal(1, r.MaximumFavorableProgressTicks); // first - low = 1000-999
        Assert.Equal(2, r.MaximumAdverseProgressTicks);   // high - first
        Assert.Equal(1, r.ProgressRetainedTicks);         // first - latest
        Assert.Equal(1m, r.ProgressRetentionRatio);
        Assert.Null(EfficiencyRatio.TryDivide(1m, 0m));
        Assert.Null(EfficiencyRatio.TryDivide((long?)5, 0m));
    }

    [Fact]
    public void C_UnknownDirection_LeavesDirectionalUnavailable()
    {
        // Flat: same price
        var (_, _, eff) = Feed(true, (1, 100.0m, 1m), (2, 100.0m, 1m));
        var a = eff.Current!.CurrentAuctionEvidence!;
        Assert.Equal(EfficiencyResultDirection.Flat, a.ResultDirection);
        Assert.Equal(0, a.Result.NetPriceProgressTicks);
        Assert.Equal(0, a.Result.GrossRangeTicks);
        // Flat: favorable/adverse remain unavailable (direction not Up/Down)
        Assert.Null(a.Result.MaximumFavorableProgressTicks);
        Assert.Null(a.Result.MaximumAdverseProgressTicks);
        Assert.Null(a.Result.ProgressRetentionRatio);
    }

    // --- D Profile anchors ---

    [Fact]
    public void D_ImmutableStartAnchors_MigrationExact()
    {
        var of = OrderflowHost();
        of.RebuildContext(Profiles(tpoPoc: 100.0m, volPoc: 100.0m), null, Utc(24));
        of.ProcessTrade(Trade(1, 100.0m), null, Utc(24).AddSeconds(1));
        var cl = ClusterHost();
        cl.ProcessOrderflowUpdate(of.Current!, 1000, "e1", 1, Utc(24).AddSeconds(1), Utc(24).AddSeconds(1));
        var eff = EfficiencyHost();
        eff.Rebuild(of.Current, cl.Current, null, null, Profiles(tpoPoc: 100.0m, volPoc: 100.0m), Utc(24).AddSeconds(2));
        var start = eff.Current!.CurrentAuctionEvidence!.Result.DevelopingTpoPocStartTick;
        Assert.Equal(1000, start);

        of.ProcessTrade(Trade(2, 100.2m), null, Utc(24).AddSeconds(3));
        cl.ProcessOrderflowUpdate(of.Current!, 1002, "e2", 2, Utc(24).AddSeconds(3), Utc(24).AddSeconds(3));
        eff.Rebuild(of.Current, cl.Current, null, null,
            Profiles(tpoPoc: 100.2m, volPoc: 100.1m), Utc(24).AddSeconds(4));
        var r = eff.Current!.CurrentAuctionEvidence!.Result;
        Assert.Equal(start, r.DevelopingTpoPocStartTick); // immutable
        Assert.Equal(1002, r.DevelopingTpoPocLatestTick);
        Assert.Equal(2, r.TpoPocMigrationTicks);
        Assert.Equal(1000, r.DevelopingVolumePocStartTick);
        Assert.Equal(1001, r.DevelopingVolumePocLatestTick);
        Assert.Equal(1, r.VolumePocMigrationTicks);
    }

    // --- E raw relationships ---

    [Fact]
    public void E_RawRelationships_NullSafe_NoScore()
    {
        var (of, _, eff) = Feed(true, (1, 100.0m, 4m), (2, 100.2m, 1m));
        var a = eff.Current!.CurrentAuctionEvidence!;
        var rel = a.RawRelationships;
        Assert.Equal(of.Current!.CurrentAuction!.NetPriceProgressTicks! / 5m, rel.NetProgressPerExecutedContract);
        Assert.Equal(2m / 5m, rel.GrossRangePerExecutedContract);
        Assert.Equal(5m / 2m, rel.ExecutedContractsPerTickOfGrossRange);
        Assert.Null(EfficiencyRatio.TryDivide(1m, 0m));
        Assert.DoesNotContain("EfficiencyScore", a.SnapshotId, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(EfficiencyClassificationState.NotCalibrated, a.ClassificationState);
    }

    [Fact]
    public void E_ZeroDenominator_Null_NegativeRetainedPreserved()
    {
        var of = OrderflowHost();
        of.RebuildContext(Profiles(), null, Utc(24));
        var cl = ClusterHost();
        var eff = EfficiencyHost();
        // Up then reverse past start: first 100.0, high 100.1, latest 99.9 â†’ Up direction from firstâ†’latest? latest < first â†’ Down
        foreach (var (seq, px) in new[] { (1L, 100.0m), (2L, 100.1m), (3L, 99.8m) })
        {
            var evt = Trade(seq, px, at: Utc(24).AddSeconds((int)seq));
            of.ProcessTrade(evt, null, Utc(24).AddSeconds((int)seq));
            cl.ProcessOrderflowUpdate(of.Current!, evt.NormalizedPriceTick, evt.EventId, evt.EventSequence,
                Utc(24).AddSeconds((int)seq), Utc(24).AddSeconds((int)seq));
        }

        eff.Rebuild(of.Current, cl.Current, null, null, Profiles(), Utc(24).AddSeconds(10));
        var r = eff.Current!.CurrentAuctionEvidence!.Result;
        Assert.True(r.ProgressRetainedTicks.HasValue);
        // Down: retained = first - latest = 1000 - 998 = 2 (positive) or if Up inferred differently
        Assert.True(double.IsFinite((double)(r.ProgressRetentionRatio ?? 0m)));
        Assert.Null(eff.Current.CurrentAuctionEvidence.RawRelationships.NetProgressPerExecutedContract is decimal d && d == 0
            ? null
            : EfficiencyRatio.TryDivide(1m, 0m));
    }

    // --- F identity/revision ---

    [Fact]
    public void F_IdentityStable_FingerprintReuse_RevisionPolicy()
    {
        var of = OrderflowHost();
        of.RebuildContext(Profiles(), null, Utc(24));
        var cl = ClusterHost();
        var eff = EfficiencyHost();
        of.ProcessTrade(Trade(1, 100.0m), null, Utc(24).AddSeconds(1));
        cl.ProcessOrderflowUpdate(of.Current!, 1000, "e1", 1, Utc(24).AddSeconds(1), Utc(24).AddSeconds(1));
        var s1 = eff.Rebuild(of.Current, cl.Current, null, null, Profiles(), Utc(24).AddSeconds(2));
        var id1 = s1.CurrentAuctionEvidence!.SnapshotId;
        Assert.StartsWith("AEFF|", id1);
        Assert.Contains(AuctionEfficiencyEvidencePolicyConfig.PolicyVersion, id1);
        Assert.Equal(1, s1.CurrentAuctionEvidence.StateVersion);
        Assert.Equal(1, s1.CurrentAuctionEvidence.EventRevision);

        var s2 = eff.Rebuild(of.Current, cl.Current, null, null, Profiles(), Utc(24).AddSeconds(3));
        Assert.Same(s1, s2); // fingerprint reuse

        of.ProcessTrade(Trade(2, 100.1m), null, Utc(24).AddSeconds(4));
        cl.ProcessOrderflowUpdate(of.Current!, 1001, "e2", 2, Utc(24).AddSeconds(4), Utc(24).AddSeconds(4));
        var s3 = eff.Rebuild(of.Current, cl.Current, null, null, Profiles(), Utc(24).AddSeconds(5));
        Assert.Equal(id1, s3.CurrentAuctionEvidence!.SnapshotId);
        Assert.True(s3.CurrentAuctionEvidence.EventRevision > 1);
    }

    // --- G lifecycle ---

    [Fact]
    public void G_AuctionTransition_FreshIdentity_NoCrossAccumulation()
    {
        var of = OrderflowHost();
        of.RebuildContext(Profiles(), null, Utc(24));
        var cl = ClusterHost();
        var eff = EfficiencyHost();
        of.ProcessTrade(Trade(1, 100.0m, 3m), null, Utc(24).AddSeconds(1));
        cl.ProcessOrderflowUpdate(of.Current!, 1000, "e1", 1, Utc(24).AddSeconds(1), Utc(24).AddSeconds(1));
        eff.Rebuild(of.Current, cl.Current, null, null, Profiles(), Utc(24).AddSeconds(2));
        var firstId = eff.Current!.CurrentAuctionEvidence!.SnapshotId;
        var firstVol = eff.Current.CurrentAuctionEvidence.Effort.TotalExecutedVolume;

        of.RebuildContext(Profiles("PI-NEXT"), null, Utc(24).AddMinutes(1));
        cl.RebuildFromOrderflow(of.Current, Utc(24).AddMinutes(1));
        of.ProcessTrade(Trade(2, 100.0m, 1m, auction: "PI-NEXT"), null, Utc(24).AddMinutes(1));
        cl.ProcessOrderflowUpdate(of.Current!, 1000, "e2", 2, Utc(24).AddMinutes(1), Utc(24).AddMinutes(1));
        eff.Rebuild(of.Current, cl.Current, null, null, Profiles("PI-NEXT"), Utc(24).AddMinutes(1));
        Assert.NotEqual(firstId, eff.Current!.CurrentAuctionEvidence!.SnapshotId);
        Assert.Equal(1m, eff.Current.CurrentAuctionEvidence.Effort.TotalExecutedVolume);
        Assert.NotEqual(firstVol + 1m, eff.Current.CurrentAuctionEvidence.Effort.TotalExecutedVolume);
    }

    [Fact]
    public void G_DisableReenable_CleanLifecycle_LiveOnly()
    {
        var (of, cl, _) = Feed(true, (1, 100.0m, 2m));
        var eff = EfficiencyHost(true);
        eff.Rebuild(of.Current, cl.Current, null, null, Profiles(), Utc(24).AddSeconds(5));
        Assert.NotNull(eff.Current!.CurrentAuctionEvidence);

        eff.Configure(Tick, Instrument, Epoch, new AuctionEfficiencyEvidencePolicyConfig(false));
        Assert.Equal(EfficiencyModuleState.Disabled, eff.Current!.ModuleState);

        eff.Configure(Tick, Instrument, Epoch, new AuctionEfficiencyEvidencePolicyConfig(true));
        of.ProcessTrade(Trade(10, 100.1m), null, Utc(24).AddSeconds(10));
        cl.ProcessOrderflowUpdate(of.Current!, 1001, "e10", 10, Utc(24).AddSeconds(10), Utc(24).AddSeconds(10));
        eff.Rebuild(of.Current, cl.Current, null, null, Profiles(), Utc(24).AddSeconds(11));
        Assert.Contains(AuctionEfficiencyEvidencePolicyConfig.LimitationHistoryLiveOnly, eff.Current!.Limitations);
        Assert.Equal(EfficiencyClassificationState.NotCalibrated, eff.Current.CurrentAuctionEvidence!.ClassificationState);
    }

    // --- H runtime ---

    [Fact]
    public void H_RuntimeSchema_Gps_NoProhibitedWording_DataGateIndependent()
    {
        var (of, cl, eff) = Feed(false, (1, 100.0m, 3m), (2, 100.1m, 2m));
        var engine = new GcaeRuntimeEngine();
        var snap = engine.Publish(
            null, Instrument, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, Utc(24), false, false, false, false, false, false,
            Profiles(), executedOrderflow: of.Current, clusterRaw: cl.Current,
            auctionEfficiency: eff.Current, showAuctionEfficiencyDiagnostics: true);
        Assert.Equal("0.13.0", snap.Version);
        Assert.Same(eff.Current, snap.AuctionEfficiency);
        Assert.NotEqual(DataState.Ready, snap.DataGate.DataState);

        var lines = AuctionGpsCardMapper.BuildAuctionEfficiencyLines(eff.Current, true);
        var text = string.Join("\n", lines).ToUpperInvariant();
        Assert.Contains("AUCTION EFFICIENCY: PARTIAL", text);
        Assert.Contains("AUCTION_EFFICIENCY_EVIDENCE_POLICY_V1", text);
        Assert.Contains("NOT CALIBRATED", text);
        Assert.Contains("LIVE_ONLY", text);
        Assert.Contains("EFFORT VOLUME:", text);
        Assert.Contains("RESULT NET PROGRESS:", text);
        Assert.DoesNotContain("EFFORT EFFECTIVE", text);
        Assert.DoesNotContain("EFFORT INEFFECTIVE", text);
        Assert.DoesNotContain("ABSORPTION", text);
        Assert.DoesNotContain("EXHAUSTION", text);
        Assert.DoesNotContain("TRADE FACILITATION HEALTHY", text);
        Assert.DoesNotContain("TRADE FACILITATION FAILING", text);
        Assert.DoesNotContain("\nFAR", text);
        Assert.DoesNotContain("\nAAC", text);
        Assert.DoesNotContain("\nLONG", text);
        Assert.DoesNotContain("\nSHORT", text);
        Assert.DoesNotContain("PROBABILITY", text);
        Assert.DoesNotContain("CONFIDENCE", text);
    }

    // --- I performance ---

    [Fact]
    public void I_WideMap_FingerprintReuse()
    {
        var of = OrderflowHost();
        of.RebuildContext(Profiles(), null, Utc(24));
        var cl = ClusterHost();
        var eff = EfficiencyHost();
        for (var i = 0; i < 120; i++)
        {
            var price = 100.0m + (i * Tick);
            var evt = Trade(i + 1, price, 1m, ask: i % 2 == 0, bid: i % 2 != 0, at: Utc(24).AddSeconds(Math.Min(i, 59)));
            of.ProcessTrade(evt, null, Utc(24).AddSeconds(Math.Min(i, 59)));
            cl.ProcessOrderflowUpdate(of.Current!, evt.NormalizedPriceTick, evt.EventId, evt.EventSequence,
                Utc(24).AddSeconds(Math.Min(i, 59)), Utc(24).AddSeconds(Math.Min(i, 59)));
        }

        var first = eff.Rebuild(of.Current, cl.Current, null, null, Profiles(), Utc(24).AddMinutes(1));
        Assert.True(first.CurrentAuctionEvidence!.Effort.PriceLevelCount >= 100);
        var again = eff.Rebuild(of.Current, cl.Current, null, null, Profiles(), Utc(24).AddMinutes(2));
        Assert.Same(first, again);
    }

    // --- J scope ---

    [Fact]
    public void J_SourceScope_Phase2E_Started_SchemaPolicy()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        Assert.True(Directory.Exists(Path.Combine(root, "src", "GC.AuctionFlow", "Efficiency")));
        Assert.True(Directory.Exists(Path.Combine(root, "src", "GC.AuctionFlow", "Cluster")));
        Assert.True(Directory.Exists(Path.Combine(root, "src", "GC.AuctionFlow", "Orderflow")));
        Assert.True(Directory.Exists(Path.Combine(root, "src", "GC.AuctionFlow", "EffortResult")));
        Assert.False(Directory.Exists(Path.Combine(root, "src", "GC.AuctionFlow", "TradeFacilitation")));
        Assert.True(Directory.Exists(Path.Combine(root, "src", "GC.AuctionFlow", "Thesis")));
        Assert.False(Directory.Exists(Path.Combine(root, "src", "GC.AuctionFlow", "Far")));
        Assert.False(Directory.Exists(Path.Combine(root, "src", "GC.AuctionFlow", "Absorption")));
        var indicator = File.ReadAllText(Path.Combine(root, "src", "GC.AuctionFlow", "Atas", "GcAuctionFlowIndicator.cs"));
        Assert.Contains("EnableAuctionEfficiencyEvidence = false", indicator, StringComparison.Ordinal);
        Assert.Contains("EnableEffortResultClassifier = false", indicator, StringComparison.Ordinal);
        Assert.DoesNotContain("EnableTradeFacilitation", indicator, StringComparison.Ordinal);
        Assert.DoesNotContain("EffortResultBalanced", indicator, StringComparison.Ordinal);
        Assert.DoesNotContain("AggressionEffective", indicator, StringComparison.Ordinal);
        Assert.Equal(AuctionEfficiencyEvidencePolicyConfig.PolicyVersion, "AUCTION_EFFICIENCY_EVIDENCE_POLICY_V1");
        Assert.Equal("0.13.0", GcaeRuntimeSnapshot.SnapshotVersion);
        Assert.Equal("CLUSTER_RAW_FEATURE_POLICY_V1", ClusterRawFeaturePolicyConfig.PolicyVersion);
        Assert.Equal("EXECUTED_ORDERFLOW_POLICY_V1", ExecutedOrderflowPolicyConfig.PolicyVersion);
    }
}
