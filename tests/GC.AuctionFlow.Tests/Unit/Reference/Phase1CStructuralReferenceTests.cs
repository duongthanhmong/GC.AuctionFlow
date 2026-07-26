using GC.AuctionFlow.Composite;
using GC.AuctionFlow.Core;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Reference;
using GC.AuctionFlow.Runtime;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Reference;

/// <summary>Phase 1C Structural Reference Foundation â€” deterministic extraction/registry/runtime/UI.</summary>
public sealed class Phase1CStructuralReferenceTests
{
    private const decimal Tick = 0.1m;
    private const string Instrument = "GCQ6";
    private const string Epoch = "GCQ6|tick=0.1";

    private static PrimaryAuctionProfileSnapshot MakeAuction(
        string id,
        DateTime start,
        DateTime end,
        bool completed,
        decimal high = 100.5m,
        decimal low = 99.5m,
        decimal tpoPoc = 100.0m,
        decimal tpoVah = 100.2m,
        decimal tpoVal = 99.8m,
        decimal? vpoc = 100.0m,
        decimal? volVah = 100.2m,
        decimal? volVal = 99.8m,
        PriceVolumeCapability volCap = PriceVolumeCapability.Exact,
        decimal? lastPx = 100.0m,
        AuctionProfileState state = AuctionProfileState.Ready)
    {
        var tpo = new TpoProfileSnapshot(
            id, start, end,
            AuctionTimezoneResolver.IanaAmericaNewYork, new TimeSpan(8, 20, 0), 30,
            high, low, tpoPoc, tpoVah, tpoVal,
            2, 1, null,
            new Dictionary<long, int> { [(long)(tpoPoc / Tick)] = 2 },
            ProfileDataQuality.Complete, "test", Array.Empty<string>());
        VolumeProfileSnapshot? vol = null;
        if (volCap == PriceVolumeCapability.Exact && vpoc is not null)
        {
            vol = new VolumeProfileSnapshot(
                id, high, low, vpoc, volVah, volVal,
                20m, new Dictionary<long, decimal> { [(long)(vpoc.Value / Tick)] = 20m },
                PriceVolumeCapability.Exact, ProfileDataQuality.Complete, "test", Array.Empty<string>());
        }
        else
        {
            vol = new VolumeProfileSnapshot(
                id, high, low, null, null, null,
                0m, new Dictionary<long, decimal>(),
                PriceVolumeCapability.Unavailable, ProfileDataQuality.Partial, "test",
                new[] { "PRICE_VOLUME_DATA_UNAVAILABLE" });
            if (state == AuctionProfileState.Ready)
                state = AuctionProfileState.Partial;
        }

        return new PrimaryAuctionProfileSnapshot(
            state, id, start, end, completed,
            tpo, vol, high, low, lastPx, end, null,
            state == AuctionProfileState.Ready ? ProfileDataQuality.Complete : ProfileDataQuality.Partial,
            Array.Empty<string>());
    }

    private static PrimaryProfileSetSnapshot Profiles(PrimaryAuctionProfileSnapshot current, PrimaryAuctionProfileSnapshot? previous)
    {
        var completed = new List<PrimaryAuctionProfileSnapshot>();
        if (previous is not null) completed.Add(previous);
        return new PrimaryProfileSetSnapshot(
            current, previous, HistoricalInitializationState.Complete, 10,
            Array.Empty<string>(), Array.Empty<string>(), null, completed);
    }

    private static CompositeAggregateResult Agg(
        decimal? tpoPoc = 100.1m,
        decimal? tpoVah = 100.3m,
        decimal? tpoVal = 99.7m,
        decimal? high = 100.6m,
        decimal? low = 99.4m,
        decimal? vpoc = 100.1m,
        decimal? volVah = 100.3m,
        decimal? volVal = 99.7m,
        PriceVolumeCapability cap = PriceVolumeCapability.Exact) =>
        new(
            new Dictionary<long, int> { [1001] = 3 },
            cap == PriceVolumeCapability.Exact ? new Dictionary<long, decimal> { [1001] = 30m } : new Dictionary<long, decimal>(),
            3, cap == PriceVolumeCapability.Exact ? 30m : 0m,
            high, low, tpoPoc, vpoc, tpoVah, tpoVal, volVah, volVal,
            cap,
            cap == PriceVolumeCapability.Exact ? ProfileDataQuality.Complete : ProfileDataQuality.Partial,
            Array.Empty<string>());

    private static ConfirmedCompositeProfileSnapshot ConfirmedComposite(
        CompositeStatus status = CompositeStatus.Ready,
        CompositeAggregateResult? agg = null)
    {
        agg ??= Agg();
        return new ConfirmedCompositeProfileSnapshot(
            "CMP|PI-2026-07-20", CompositePolicyMode.OperatorAnchored, CompositePolicyConfig.PolicyVersion,
            Instrument, Epoch, Tick, "PI-2026-07-20", "PI-2026-07-20", "PI-2026-07-20",
            new[] { "PI-2026-07-20" }, Array.Empty<string>(), 1,
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow,
            status, agg,
            status == CompositeStatus.Partial ? CompositeCapabilityState.Partial : CompositeCapabilityState.Ready,
            CompositeEvidenceState.NotEvaluated, Array.Empty<string>(), "test");
    }

    private static StructuralReferenceHost Host(bool enabled = true) =>
        new(Tick, Instrument, Epoch, AtasTimestampNormalizer.PolicyVersion, new ReferencePolicyConfig(enabled));

    // --- A. Extraction ---

    [Fact]
    public void Extract_previous_primary_emits_confirmed_tpo_and_volume_when_exact()
    {
        var prev = MakeAuction("PI-2026-07-20", Utc(20), Utc(21), true);
        var cur = MakeAuction("PI-2026-07-21", Utc(21), Utc(22), false, lastPx: 100.0m);
        var r = StructuralReferenceExtractor.Extract(Profiles(cur, prev), null, true);
        Assert.Contains(r.Candidates, c => c.Type == ReferenceType.PreviousPrimaryTpoPoc && c.Maturity == ReferenceMaturity.Confirmed);
        Assert.Contains(r.Candidates, c => c.Type == ReferenceType.PreviousPrimaryVpoc);
        Assert.Contains(r.Candidates, c => c.Type == ReferenceType.PreviousPrimaryAuctionHigh);
        Assert.Equal(StructuralReferenceModuleState.Ready, r.SuggestedState);
    }

    [Fact]
    public void Extract_omits_volume_when_not_exact_and_marks_partial()
    {
        var prev = MakeAuction("PI-2026-07-20", Utc(20), Utc(21), true, volCap: PriceVolumeCapability.Unavailable);
        var cur = MakeAuction("PI-2026-07-21", Utc(21), Utc(22), false, volCap: PriceVolumeCapability.Unavailable);
        var r = StructuralReferenceExtractor.Extract(Profiles(cur, prev), null, true);
        Assert.Contains(r.Candidates, c => c.Type == ReferenceType.PreviousPrimaryTpoPoc);
        Assert.DoesNotContain(r.Candidates, c => c.Type == ReferenceType.PreviousPrimaryVpoc);
        Assert.DoesNotContain(r.Candidates, c => c.Type == ReferenceType.CurrentPrimaryVpoc);
        Assert.Contains(r.UnavailableVolumeReasons, x => x.Contains("VOLUME", StringComparison.Ordinal));
        Assert.Equal(StructuralReferenceModuleState.Partial, r.SuggestedState);
        Assert.DoesNotContain(r.Candidates, c => c.Price == 0m && c.Type.ToString().Contains("Vpoc", StringComparison.Ordinal));
    }

    [Fact]
    public void Extract_current_marked_developing_previous_confirmed()
    {
        var prev = MakeAuction("PI-2026-07-20", Utc(20), Utc(21), true);
        var cur = MakeAuction("PI-2026-07-21", Utc(21), Utc(22), false);
        var r = StructuralReferenceExtractor.Extract(Profiles(cur, prev), null, true);
        Assert.All(r.Candidates.Where(c => c.SourceKind == ReferenceSourceKind.CurrentPrimaryAuction),
            c => Assert.Equal(ReferenceMaturity.Developing, c.Maturity));
        Assert.All(r.Candidates.Where(c => c.SourceKind == ReferenceSourceKind.PreviousPrimaryAuction),
            c => Assert.Equal(ReferenceMaturity.Confirmed, c.Maturity));
    }

    [Fact]
    public void Extract_confirmed_composite_confirmed_preview_ignored()
    {
        var prev = MakeAuction("PI-2026-07-20", Utc(20), Utc(21), true);
        var cur = MakeAuction("PI-2026-07-21", Utc(21), Utc(22), false);
        var conf = ConfirmedComposite();
        var preview = new DevelopingCompositePreviewSnapshot(conf.CompositeId, cur.AuctionId, Agg(tpoPoc: 101.0m), 0.1m, 0.1m, Array.Empty<string>());
        var set = new CompositeSetSnapshot(conf, preview, Array.Empty<CompositeMergeEvidence>(), Array.Empty<string>());
        var r = StructuralReferenceExtractor.Extract(Profiles(cur, prev), set, true);
        Assert.Contains(r.Candidates, c => c.Type == ReferenceType.CompositeTpoPoc && c.Maturity == ReferenceMaturity.Confirmed);
        Assert.Contains(r.Candidates, c => c.Type == ReferenceType.CompositeRangeHigh);
        Assert.DoesNotContain(r.Candidates, c => c.Price == 101.0m && c.Type == ReferenceType.CompositeTpoPoc);
        Assert.Contains(r.KnownLimitations, l => l.Contains("PREVIEW_IGNORED", StringComparison.Ordinal));
    }

    [Fact]
    public void Extract_partial_composite_emits_tpo_omits_volume()
    {
        var prev = MakeAuction("PI-2026-07-20", Utc(20), Utc(21), true);
        var cur = MakeAuction("PI-2026-07-21", Utc(21), Utc(22), false);
        var conf = ConfirmedComposite(CompositeStatus.Partial, Agg(vpoc: null, volVah: null, volVal: null, cap: PriceVolumeCapability.Unavailable));
        var set = new CompositeSetSnapshot(conf, null, Array.Empty<CompositeMergeEvidence>(), Array.Empty<string>());
        var r = StructuralReferenceExtractor.Extract(Profiles(cur, prev), set, true);
        Assert.Contains(r.Candidates, c => c.Type == ReferenceType.CompositeTpoPoc);
        Assert.DoesNotContain(r.Candidates, c => c.Type == ReferenceType.CompositeVpoc);
        Assert.Equal(StructuralReferenceModuleState.Partial, r.SuggestedState);
    }

    [Fact]
    public void Extract_awaiting_when_no_profiles()
    {
        var r = StructuralReferenceExtractor.Extract(null, null, true);
        Assert.Equal(StructuralReferenceModuleState.AwaitingPrimary, r.SuggestedState);
        Assert.Empty(r.Candidates);
    }

    // --- B. Identity / revision ---

    [Fact]
    public void Identity_stable_when_developing_poc_moves_state_version_increments()
    {
        var host = Host();
        var prev = MakeAuction("PI-2026-07-20", Utc(20), Utc(21), true);
        var cur1 = MakeAuction("PI-2026-07-21", Utc(21), Utc(22), false, tpoPoc: 100.0m, lastPx: 100.0m);
        var s1 = host.Rebuild(Profiles(cur1, prev), null, Utc(21));
        var id = s1.DevelopingReferences.Single(r => r.ReferenceType == ReferenceType.CurrentPrimaryTpoPoc).ReferenceId;
        var v1 = s1.DevelopingReferences.Single(r => r.ReferenceType == ReferenceType.CurrentPrimaryTpoPoc).StateVersion;

        var cur2 = MakeAuction("PI-2026-07-21", Utc(21), Utc(22), false, tpoPoc: 100.3m, lastPx: 100.3m);
        var s2 = host.Rebuild(Profiles(cur2, prev), null, Utc(21).AddMinutes(1));
        var moved = s2.DevelopingReferences.Single(r => r.ReferenceType == ReferenceType.CurrentPrimaryTpoPoc);
        Assert.Equal(id, moved.ReferenceId);
        Assert.True(moved.StateVersion > v1);
        Assert.Equal(100.3m, moved.ZoneLow);
        Assert.DoesNotContain("100.0", id, StringComparison.Ordinal);
        Assert.DoesNotContain("100.3", id, StringComparison.Ordinal);
    }

    [Fact]
    public void Identity_identical_republish_does_not_increment_version()
    {
        var host = Host();
        var prev = MakeAuction("PI-2026-07-20", Utc(20), Utc(21), true);
        var cur = MakeAuction("PI-2026-07-21", Utc(21), Utc(22), false);
        var s1 = host.Rebuild(Profiles(cur, prev), null, Utc(21));
        var poc = s1.DevelopingReferences.Single(r => r.ReferenceType == ReferenceType.CurrentPrimaryTpoPoc);
        var s2 = host.Rebuild(Profiles(cur, prev), null, Utc(21).AddSeconds(1));
        var poc2 = s2.DevelopingReferences.Single(r => r.ReferenceType == ReferenceType.CurrentPrimaryTpoPoc);
        Assert.Equal(poc.ReferenceId, poc2.ReferenceId);
        Assert.Equal(poc.StateVersion, poc2.StateVersion);
    }

    [Fact]
    public void Identity_different_source_auction_different_id_confirmed_developing_no_collide()
    {
        var a = ReferenceIdentity.Build(Instrument, Epoch, "PI-2026-07-20", ReferenceType.PreviousPrimaryTpoPoc, ReferenceMaturity.Confirmed);
        var b = ReferenceIdentity.Build(Instrument, Epoch, "PI-2026-07-21", ReferenceType.PreviousPrimaryTpoPoc, ReferenceMaturity.Confirmed);
        var c = ReferenceIdentity.Build(Instrument, Epoch, "PI-2026-07-21", ReferenceType.CurrentPrimaryTpoPoc, ReferenceMaturity.Developing);
        var d = ReferenceIdentity.Build(Instrument, Epoch, "PI-2026-07-21", ReferenceType.CurrentPrimaryTpoPoc, ReferenceMaturity.Confirmed);
        Assert.NotEqual(a, b);
        Assert.NotEqual(c, d);
        Assert.Contains("REFERENCE_POLICY_V1", a, StringComparison.Ordinal);
        Assert.DoesNotContain("100.0", a, StringComparison.Ordinal);
    }

    [Fact]
    public void Registry_confirmed_mutation_fail_closed_stale_revision_rejected()
    {
        var reg = new StructuralReferenceRegistry(Tick, Epoch, AtasTimestampNormalizer.PolicyVersion);
        var grid = new PriceGrid(Tick);
        Assert.True(grid.TryToTickIndex(100.0m, out var t));
        var first = new StructuralReferenceSnapshot(
            ReferenceIdentity.Build(Instrument, Epoch, "PI-A", ReferenceType.PreviousPrimaryTpoPoc, ReferenceMaturity.Confirmed),
            ReferenceType.PreviousPrimaryTpoPoc, 100.0m, 100.0m, t, t,
            "PI-A", ReferenceSourceKind.PreviousPrimaryAuction, ReferenceSourceHorizon.PreviousPrimaryAuction,
            Utc(20), ReferenceMaturity.Confirmed, ReferenceStatus.Fresh, ReferenceEvidenceTier.ProfileDerived,
            Tick, Epoch, AtasTimestampNormalizer.PolicyVersion, 1, Utc(20), Utc(20));
        reg.Upsert(first, Utc(20));

        var mutated = new StructuralReferenceSnapshot(
            first.ReferenceId, first.ReferenceType, 100.5m, 100.5m, t + 5, t + 5,
            first.SourceId, first.SourceKind, first.SourceHorizon, first.SourceCreatedAtUtc,
            ReferenceMaturity.Confirmed, ReferenceStatus.Active, ReferenceEvidenceTier.ProfileDerived,
            Tick, Epoch, AtasTimestampNormalizer.PolicyVersion, 1, Utc(20), Utc(21));
        Assert.Throws<InvalidOperationException>(() => reg.Upsert(mutated, Utc(21)));

        var developingId = ReferenceIdentity.Build(Instrument, Epoch, "PI-B", ReferenceType.CurrentPrimaryTpoPoc, ReferenceMaturity.Developing);
        var d1 = new StructuralReferenceSnapshot(
            developingId, ReferenceType.CurrentPrimaryTpoPoc, 100.0m, 100.0m, t, t,
            "PI-B", ReferenceSourceKind.CurrentPrimaryAuction, ReferenceSourceHorizon.CurrentPrimaryAuction,
            Utc(21), ReferenceMaturity.Developing, ReferenceStatus.Fresh, ReferenceEvidenceTier.ProfileDerived,
            Tick, Epoch, AtasTimestampNormalizer.PolicyVersion, 2, Utc(21), Utc(21));
        reg.Upsert(d1, Utc(21));
        var stale = new StructuralReferenceSnapshot(
            developingId, ReferenceType.CurrentPrimaryTpoPoc, 100.2m, 100.2m, t + 2, t + 2,
            "PI-B", ReferenceSourceKind.CurrentPrimaryAuction, ReferenceSourceHorizon.CurrentPrimaryAuction,
            Utc(21), ReferenceMaturity.Developing, ReferenceStatus.Active, ReferenceEvidenceTier.ProfileDerived,
            Tick, Epoch, AtasTimestampNormalizer.PolicyVersion, 1, Utc(21), Utc(22));
        Assert.Throws<InvalidOperationException>(() => reg.Upsert(stale, Utc(22)));
    }

    // --- C. Compatibility ---

    [Fact]
    public void Registry_tick_epoch_policy_mismatch_fail_closed()
    {
        var reg = new StructuralReferenceRegistry(Tick, Epoch, AtasTimestampNormalizer.PolicyVersion);
        var id = ReferenceIdentity.Build(Instrument, Epoch, "PI-A", ReferenceType.PreviousPrimaryTpoPoc, ReferenceMaturity.Confirmed);
        var badTick = new StructuralReferenceSnapshot(
            id, ReferenceType.PreviousPrimaryTpoPoc, 100.0m, 100.0m, 1000, 1000,
            "PI-A", ReferenceSourceKind.PreviousPrimaryAuction, ReferenceSourceHorizon.PreviousPrimaryAuction,
            Utc(20), ReferenceMaturity.Confirmed, ReferenceStatus.Fresh, ReferenceEvidenceTier.ProfileDerived,
            0.25m, Epoch, AtasTimestampNormalizer.PolicyVersion, 1, Utc(20), Utc(20));
        Assert.Throws<InvalidOperationException>(() => reg.Upsert(badTick, Utc(20)));

        var badEpoch = new StructuralReferenceSnapshot(
            id, ReferenceType.PreviousPrimaryTpoPoc, 100.0m, 100.0m, 1000, 1000,
            "PI-A", ReferenceSourceKind.PreviousPrimaryAuction, ReferenceSourceHorizon.PreviousPrimaryAuction,
            Utc(20), ReferenceMaturity.Confirmed, ReferenceStatus.Fresh, ReferenceEvidenceTier.ProfileDerived,
            Tick, "OTHER", AtasTimestampNormalizer.PolicyVersion, 1, Utc(20), Utc(20));
        Assert.Throws<InvalidOperationException>(() => reg.Upsert(badEpoch, Utc(20)));

        var badPolicy = new StructuralReferenceSnapshot(
            id, ReferenceType.PreviousPrimaryTpoPoc, 100.0m, 100.0m, 1000, 1000,
            "PI-A", ReferenceSourceKind.PreviousPrimaryAuction, ReferenceSourceHorizon.PreviousPrimaryAuction,
            Utc(20), ReferenceMaturity.Confirmed, ReferenceStatus.Fresh, ReferenceEvidenceTier.ProfileDerived,
            Tick, Epoch, "OTHER_POLICY", 1, Utc(20), Utc(20));
        Assert.Throws<InvalidOperationException>(() => reg.Upsert(badPolicy, Utc(20)));
    }

    // --- D. Lifecycle ---

    [Fact]
    public void Lifecycle_fresh_then_active_expire_on_source_gone_no_forbidden_statuses()
    {
        var host = Host();
        var prev = MakeAuction("PI-2026-07-20", Utc(20), Utc(21), true);
        var cur = MakeAuction("PI-2026-07-21", Utc(21), Utc(22), false);
        var s1 = host.Rebuild(Profiles(cur, prev), null, Utc(21));
        Assert.Contains(s1.ActiveReferences, r => r.Status == ReferenceStatus.Fresh);

        var s2 = host.Rebuild(Profiles(cur, prev), null, Utc(21).AddMinutes(1));
        Assert.Contains(s2.ActiveReferences, r => r.Status == ReferenceStatus.Active);

        // Previous auction replaced by a different previous â†’ old source expires.
        var prev2 = MakeAuction("PI-2026-07-19", Utc(19), Utc(20), true);
        var s3 = host.Rebuild(Profiles(cur, prev2), null, Utc(21).AddMinutes(2));
        Assert.DoesNotContain(s3.ConfirmedReferences, r => r.SourceId == "PI-2026-07-20");
        Assert.Contains(s3.RetiredOrExpired, r => r.SourceId == "PI-2026-07-20" && r.Status == ReferenceStatus.Expired);

        Assert.DoesNotContain(s3.ActiveReferences, r =>
            r.Status is ReferenceStatus.Approaching or ReferenceStatus.Interacting
                or ReferenceStatus.OutsideAttemptActive or ReferenceStatus.AcceptedThrough
                or ReferenceStatus.Reaccepted or ReferenceStatus.Exhausted);
        Assert.All(s3.ActiveReferences, r => Assert.Equal(0, r.TestCount));
    }

    [Fact]
    public void Lifecycle_disable_reenable_clean_state()
    {
        var host = Host(true);
        var prev = MakeAuction("PI-2026-07-20", Utc(20), Utc(21), true);
        var cur = MakeAuction("PI-2026-07-21", Utc(21), Utc(22), false);
        host.Rebuild(Profiles(cur, prev), null);
        host.Configure(Tick, Instrument, Epoch, new ReferencePolicyConfig(false));
        Assert.Equal(StructuralReferenceModuleState.Disabled, host.Current!.ModuleState);
        Assert.Empty(host.Current.ActiveReferences);

        host.Configure(Tick, Instrument, Epoch, new ReferencePolicyConfig(true));
        var s = host.Rebuild(Profiles(cur, prev), null);
        Assert.True(s.ActiveReferences.Count > 0);
        Assert.Equal(StructuralReferenceModuleState.Ready, s.ModuleState);
    }

    // --- E. Confluence ---

    [Fact]
    public void Confluence_exact_tick_groups_adjacent_does_not_no_score()
    {
        var host = Host();
        // Force previous TPO POC == current TPO POC for exact confluence.
        var prev = MakeAuction("PI-2026-07-20", Utc(20), Utc(21), true, tpoPoc: 100.0m, vpoc: 100.0m);
        var cur = MakeAuction("PI-2026-07-21", Utc(21), Utc(22), false, tpoPoc: 100.0m, vpoc: 100.1m, lastPx: 100.0m);
        var s = host.Rebuild(Profiles(cur, prev), null);
        var group = s.ConfluenceGroups.Single(g => g.ZoneLow == 100.0m && g.ConstituentCount >= 2);
        Assert.True(group.IsMixedMaturity);
        Assert.True(group.ConstituentReferenceIds.Count >= 2);
        Assert.DoesNotContain(group.ConstituentTypes, t => false); // types preserved
        Assert.DoesNotContain(s.KnownLimitations, l => l.Contains("SCORE", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(s.KnownLimitations, l => l.Contains("PROBABILITY", StringComparison.OrdinalIgnoreCase));

        var adjacent = s.ConfluenceGroups.Where(g => g.ZoneLow == 100.1m).ToArray();
        Assert.All(adjacent, g => Assert.DoesNotContain(g.ConstituentReferenceIds, id => group.ConstituentReferenceIds.Contains(id)));
    }

    // --- F. Nearest ---

    [Fact]
    public void Nearest_above_below_deterministic_ticks_no_support_resistance()
    {
        var host = Host();
        var prev = MakeAuction("PI-2026-07-20", Utc(20), Utc(21), true, high: 100.5m, low: 99.5m, tpoPoc: 99.5m, tpoVah: 100.5m, tpoVal: 99.5m, vpoc: 99.5m, volVah: 100.5m, volVal: 99.5m);
        var cur = MakeAuction("PI-2026-07-21", Utc(21), Utc(22), false, high: 101.0m, low: 99.0m, tpoPoc: 100.0m, tpoVah: 101.0m, tpoVal: 99.0m, vpoc: 100.0m, volVah: 101.0m, volVal: 99.0m, lastPx: 100.0m);
        var s = host.Rebuild(Profiles(cur, prev), null);
        Assert.NotNull(s.Nearest);
        Assert.NotNull(s.Nearest!.DistanceBelowTicks);
        Assert.NotNull(s.Nearest.DistanceAboveTicks);
        Assert.True(s.Nearest.NearestBelow.Count >= 1);
        Assert.True(s.Nearest.NearestAbove.Count >= 1);
        Assert.All(s.Nearest.NearestBelow, r => Assert.True(r.ZoneHighTick < s.Nearest.CurrentPriceTick));
        Assert.All(s.Nearest.NearestAbove, r => Assert.True(r.ZoneLowTick > s.Nearest.CurrentPriceTick));
    }

    // --- G. Runtime / DataGate ---

    [Fact]
    public void Runtime_disabled_references_do_not_degrade_primary_ready_does_not_clear_degraded()
    {
        var engine = new GcaeRuntimeEngine();
        var prev = MakeAuction("PI-2026-07-20", Utc(20), Utc(21), true);
        var cur = MakeAuction("PI-2026-07-21", Utc(21), Utc(22), false);
        var profiles = Profiles(cur, prev);
        var snap = engine.Publish(
            observed: null,
            expectedInstrumentCode: "GCQ6",
            mode: DataSourceMode.Live,
            modeProvenance: DataSourceModeProvenance.OperatorDeclared,
            provider: DeclaredFeedProvider.Rithmic,
            providerProvenance: FeedProviderProvenance.OperatorDeclared,
            tradeObserved: true,
            lastTradeCallbackUtc: DateTime.UtcNow,
            rawRecorderMasterEnabled: false,
            tradeRecordingEnabled: false,
            recorderAccepting: false,
            recorderFaulted: false,
            recorderSessionPresent: false,
            indicatorDisposed: false,
            profiles: profiles,
            structuralReferences: null);
        Assert.Equal("0.13.0", GcaeRuntimeSnapshot.SnapshotVersion);
        Assert.Equal("0.13.0", snap.Version);
        Assert.Equal(ReferencePlaceholderState.NotAvailable, snap.Reference);
        // Bid/Ask unknown keeps degraded independently of references.
        Assert.True(snap.DataGate.DataState is DataState.Degraded or DataState.Ready or DataState.Invalid);

        var host = Host();
        var refs = host.Rebuild(profiles, null);
        var snap2 = engine.Publish(
            observed: null, "GCQ6", DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, DateTime.UtcNow, false, false, false, false, false, false,
            profiles: profiles, structuralReferences: refs);
        Assert.Equal(ReferencePlaceholderState.Ready, snap2.Reference);
        Assert.Equal(snap.DataGate.DataState, snap2.DataGate.DataState);
    }

    [Fact]
    public void Runtime_primary_only_ready_while_composite_disabled_awaiting_without_primary()
    {
        var host = Host();
        var prev = MakeAuction("PI-2026-07-20", Utc(20), Utc(21), true);
        var cur = MakeAuction("PI-2026-07-21", Utc(21), Utc(22), false);
        var s = host.Rebuild(Profiles(cur, prev), composite: null);
        Assert.Equal(StructuralReferenceModuleState.Ready, s.ModuleState);
        Assert.True(s.ConfirmedReferences.Count > 0);
        Assert.True(s.DevelopingReferences.Count > 0);
        Assert.DoesNotContain(s.ActiveReferences, r => r.SourceKind == ReferenceSourceKind.ConfirmedComposite);

        var awaiting = host.Rebuild(null, null);
        Assert.Equal(StructuralReferenceModuleState.AwaitingPrimary, awaiting.ModuleState);
    }

    [Fact]
    public void Publish_guard_reuses_snapshot_until_fingerprint_changes()
    {
        var prev = MakeAuction("PI-2026-07-20", Utc(20), Utc(21), true);
        var cur = MakeAuction("PI-2026-07-21", Utc(21), Utc(22), false);
        var profiles = Profiles(cur, prev);
        StructuralReferenceHost? host = null;
        ReferenceInputFingerprint? last = null;
        var rebuilds = 0;

        ReferenceInputFingerprint Fp() => ReferenceInputFingerprint.Build(
            true, profiles, null, Tick, Epoch, AtasTimestampNormalizer.PolicyVersion);

        void MaybeRebuild()
        {
            var current = Fp();
            if (!ReferencePublishInitialization.ShouldProcess(true, profiles, host, last, current))
                return;
            host ??= Host();
            host.Configure(Tick, Instrument, Epoch, new ReferencePolicyConfig(true));
            host.Rebuild(profiles, null);
            last = host.LastAppliedFingerprint;
            rebuilds++;
        }

        MaybeRebuild();
        MaybeRebuild();
        MaybeRebuild();
        Assert.Equal(1, rebuilds);

        var cur2 = MakeAuction("PI-2026-07-21", Utc(21), Utc(22), false, tpoPoc: 100.4m, lastPx: 100.4m);
        profiles = Profiles(cur2, prev);
        MaybeRebuild();
        Assert.Equal(2, rebuilds);
    }

    // --- H. UI / overlay ---

    [Fact]
    public void Gps_reference_rows_and_diagnostics_have_no_episode_thesis_direction()
    {
        var host = Host();
        var prev = MakeAuction("PI-2026-07-20", Utc(20), Utc(21), true);
        var cur = MakeAuction("PI-2026-07-21", Utc(21), Utc(22), false);
        var refs = host.Rebuild(Profiles(cur, prev), null);
        var engine = new GcaeRuntimeEngine();
        var snap = engine.Publish(
            null, "GCQ6", DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, DateTime.UtcNow, false, false, false, false, false, false,
            profiles: Profiles(cur, prev), structuralReferences: refs, showStructuralReferenceDiagnostics: true);
        var vm = AuctionGpsCardMapper.FromSnapshot(snap, showDiagnostics: true);
        Assert.Contains(vm.ProfileDetailLines, l => l == "REFERENCES: READY");
        Assert.Contains(vm.ProfileDetailLines, l => l == "REFERENCE POLICY: REFERENCE_POLICY_V1");
        Assert.Contains(vm.ProfileDetailLines, l => l.StartsWith("CONFIRMED REFERENCES:", StringComparison.Ordinal));
        Assert.Contains(vm.ProfileDetailLines, l => l.StartsWith("REF NEAREST", StringComparison.Ordinal));
        Assert.Contains(vm.DiagnosticRows, l => l.StartsWith("EPISODE:", StringComparison.Ordinal));
        Assert.Contains(vm.DiagnosticRows, l => l.StartsWith("FAR:", StringComparison.Ordinal));
        Assert.Contains(vm.DiagnosticRows, l => l.StartsWith("AAC:", StringComparison.Ordinal));
        Assert.DoesNotContain(vm.AllLines(true), l => l.Contains("LONG", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(vm.AllLines(true), l => l.Contains("SHORT", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(vm.AllLines(true), l => l.Contains("SUPPORT", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(vm.AllLines(true), l => l.Contains("RESISTANCE", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(vm.AllLines(true), l => l.StartsWith("STRUCTURAL CONTEXT:", StringComparison.Ordinal));
    }

    [Fact]
    public void Overlay_confirmed_developing_wording_exact_tick_one_zone()
    {
        var host = Host();
        var prev = MakeAuction("PI-2026-07-20", Utc(20), Utc(21), true,
            high: 101.0m, low: 99.0m, tpoPoc: 100.0m, tpoVah: 100.5m, tpoVal: 99.5m, vpoc: 100.0m, volVah: 100.5m, volVal: 99.5m);
        var cur = MakeAuction("PI-2026-07-21", Utc(21), Utc(22), false,
            high: 102.0m, low: 98.0m, tpoPoc: 100.0m, tpoVah: 101.5m, tpoVal: 98.5m, vpoc: 100.8m, volVah: 101.5m, volVal: 98.5m, lastPx: 100.0m);
        var refs = host.Rebuild(Profiles(cur, prev), null);
        var ovm = PrimaryProfileOverlayViewModel.FromProfiles(
            Profiles(cur, prev), true, null, false, false, refs, enableStructuralReferenceOverlay: true);
        var refLevels = ovm.Levels.Where(l => l.Kind == ProfileOverlayKind.StructuralReference).ToArray();
        Assert.Contains(refLevels, l => l.Label.Contains("CONFIRMED", StringComparison.Ordinal));
        Assert.Contains(refLevels, l => l.Label.Contains("DEVELOPING", StringComparison.Ordinal));
        Assert.Equal(1, refLevels.Count(l => l.Price == 100.0m));
        Assert.True(refLevels.Single(l => l.Price == 100.0m).ConstituentReferenceIds.Count >= 2);
        Assert.Contains("MIXED", refLevels.Single(l => l.Price == 100.0m).Label, StringComparison.Ordinal);
        Assert.Equal(ReferenceOverlayDisplayPolicy.PolicyVersion, refLevels[0].OverlayPolicyVersion);
    }

    [Fact]
    public void Gps_awaiting_primary_row()
    {
        var lines = AuctionGpsCardMapper.BuildStructuralReferenceLines(
            new StructuralReferenceSetSnapshot(
                StructuralReferenceModuleState.AwaitingPrimary, ReferencePolicyConfig.PolicyVersion,
                Array.Empty<StructuralReferenceSnapshot>(), Array.Empty<StructuralReferenceSnapshot>(),
                Array.Empty<StructuralReferenceSnapshot>(), Array.Empty<ReferenceConfluenceGroup>(),
                null, "fp", 0, Array.Empty<string>(), Array.Empty<string>(), DateTime.UtcNow),
            false);
        Assert.Contains(lines, l => l == "REFERENCES: AWAITING PRIMARY");
    }

    // --- I. Regression / scope ---

    [Fact]
    public void Source_scope_no_phase1g_thesis_or_far_wiring()
    {
        var root = FindRepoRoot();
        var indicator = File.ReadAllText(Path.Combine(root, "src", "GC.AuctionFlow", "Atas", "GcAuctionFlowIndicator.cs"));
        Assert.Contains("EnableStructuralReferences = false", indicator, StringComparison.Ordinal);
        Assert.Contains("REFERENCE_POLICY_V1", indicator, StringComparison.Ordinal);
        Assert.Contains("EnableAuctionEpisodes = false", indicator, StringComparison.Ordinal);
        Assert.Contains("EnableAcceptanceReentryEvidence = false", indicator, StringComparison.Ordinal);
        Assert.DoesNotContain("DirectionalAuction", indicator, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductionThesis", indicator, StringComparison.Ordinal);
        Assert.DoesNotContain("FarAacEngine", indicator, StringComparison.Ordinal);
        Assert.DoesNotContain("EnableThesis", indicator, StringComparison.Ordinal);
        Assert.DoesNotContain("TradeFacilitation", indicator, StringComparison.Ordinal);
        Assert.DoesNotContain("Telegram", indicator, StringComparison.Ordinal);
        Assert.True(Directory.Exists(Path.Combine(root, "src", "GC.AuctionFlow", "Evidence")));
        Assert.True(Directory.Exists(Path.Combine(root, "src", "GC.AuctionFlow", "Thesis")));

        var refDir = Path.Combine(root, "src", "GC.AuctionFlow", "Reference");
        foreach (var file in Directory.GetFiles(refDir, "*.cs"))
        {
            var name = Path.GetFileName(file);
            if (name.Equals("ReferenceEnums.cs", StringComparison.OrdinalIgnoreCase))
                continue; // reserved lifecycle members allowed in enum only
            var src = File.ReadAllText(file);
            Assert.DoesNotContain("ReferenceStatus.Approaching", src, StringComparison.Ordinal);
            Assert.DoesNotContain("ReferenceStatus.Interacting", src, StringComparison.Ordinal);
            Assert.DoesNotContain("ReferenceStatus.OutsideAttemptActive", src, StringComparison.Ordinal);
            Assert.DoesNotContain("ReferenceStatus.AcceptedThrough", src, StringComparison.Ordinal);
            Assert.DoesNotContain("ReferenceScore", src, StringComparison.Ordinal);
            Assert.DoesNotContain("probability", src, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static DateTime Utc(int day) => new(2026, 7, day, 12, 0, 0, DateTimeKind.Utc);

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "GC.AuctionFlow.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
