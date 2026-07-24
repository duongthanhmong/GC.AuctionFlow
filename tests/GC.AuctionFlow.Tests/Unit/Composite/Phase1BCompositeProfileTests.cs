using GC.AuctionFlow.Composite;
using GC.AuctionFlow.Core;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Runtime;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Composite;

public sealed class Phase1BCompositeProfileTests
{
    private static readonly PriceGrid Grid = new(0.1m);
    private static readonly PrimaryAuctionClockConfig Cfg = new();

    private static DateTimeOffset EtToUtc(int y, int m, int d, int hh, int mm, int ss = 0)
    {
        var local = new DateTime(y, m, d, hh, mm, ss, DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, AuctionTimezoneResolver.Resolve()));
    }

    private static CompositeAuctionContribution Contrib(
        string id, DateTime start, DateTime end, DateOnly local,
        IReadOnlyDictionary<long, int> tpo,
        IReadOnlyDictionary<long, decimal>? vol = null,
        PriceVolumeCapability cap = PriceVolumeCapability.Exact,
        bool completed = true,
        string epoch = "E1",
        decimal tick = 0.1m,
        string version = "v1")
    {
        decimal? hi = tpo.Count > 0 ? Grid.ToPrice(tpo.Keys.Max()) : null;
        decimal? lo = tpo.Count > 0 ? Grid.ToPrice(tpo.Keys.Min()) : null;
        var volumes = vol ?? new Dictionary<long, decimal>();
        return new CompositeAuctionContribution(
            id, start, end, local, completed, hi, lo,
            tpo.Count > 0 ? Grid.ToPrice(tpo.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).First().Key) : null,
            volumes.Count > 0 ? Grid.ToPrice(volumes.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).First().Key) : null,
            hi, lo, hi, lo,
            tpo.Values.Sum(),
            volumes.Values.Sum(),
            tpo, volumes, cap, ProfileDataQuality.Complete, Array.Empty<string>(),
            "1.0.0", AtasTimestampNormalizer.PolicyVersion, "test", version, "GCQ6", epoch, tick);
    }

    private static PrimaryAuctionProfileSnapshot PrimaryFromContrib(CompositeAuctionContribution c)
    {
        var tpo = new TpoProfileSnapshot(
            c.AuctionId, c.AuctionStartUtc, c.AuctionEndUtc,
            Cfg.TimezoneId, Cfg.AnchorLocalTime, Cfg.PeriodMinutes,
            c.ProfileHigh, c.ProfileLow, c.TpoPoc, c.TpoVah, c.TpoVal,
            c.TotalTpoCount, 1, null, c.PriceLevelTpoCounts,
            c.DataQuality, "test", c.KnownLimitations);
        VolumeProfileSnapshot? vol = null;
        if (c.PriceVolumeCapability == PriceVolumeCapability.Exact)
        {
            vol = new VolumeProfileSnapshot(
                c.AuctionId, c.ProfileHigh, c.ProfileLow, c.VolumePoc, c.VolumeVah, c.VolumeVal,
                c.TotalExecutedVolume, c.PriceLevelVolumes, c.PriceVolumeCapability,
                c.DataQuality, "test", Array.Empty<string>());
        }
        else
        {
            vol = new VolumeProfileSnapshot(
                c.AuctionId, null, null, null, null, null, 0m,
                new Dictionary<long, decimal>(), PriceVolumeCapability.Unavailable,
                ProfileDataQuality.Partial, "test", new[] { "PRICE_VOLUME_DATA_UNAVAILABLE" });
        }

        return new PrimaryAuctionProfileSnapshot(
            AuctionProfileState.Ready, c.AuctionId, c.AuctionStartUtc, c.AuctionEndUtc, c.IsCompleted,
            tpo, vol, c.ProfileHigh, c.ProfileLow, c.TpoPoc, c.AuctionEndUtc, null, c.DataQuality, Array.Empty<string>());
    }

    [Fact]
    public void Ledger_add_replace_duplicate_and_reject_developing()
    {
        var ledger = new CompletedAuctionLedger();
        var a = Contrib("PI-2026-07-20", DateTime.UtcNow, DateTime.UtcNow.AddHours(24), new DateOnly(2026, 7, 20),
            new Dictionary<long, int> { [1000] = 1 }, version: "v1");
        Assert.True(ledger.Upsert(a));
        Assert.False(ledger.Upsert(a)); // same version
        var a2 = Contrib("PI-2026-07-20", DateTime.UtcNow, DateTime.UtcNow.AddHours(24), new DateOnly(2026, 7, 20),
            new Dictionary<long, int> { [1000] = 2 }, version: "v2");
        Assert.True(ledger.Upsert(a2));
        Assert.Equal(1, ledger.Count);
        Assert.Equal(2, ledger.AllOrdered()[0].PriceLevelTpoCounts[1000]);

        var dev = Contrib("PI-2026-07-21", DateTime.UtcNow, DateTime.UtcNow.AddHours(24), new DateOnly(2026, 7, 21),
            new Dictionary<long, int> { [1000] = 1 }, completed: false);
        Assert.False(ledger.Upsert(dev));
    }

    [Fact]
    public void Ledger_epoch_mismatch_throws()
    {
        var ledger = new CompletedAuctionLedger();
        ledger.Upsert(Contrib("PI-2026-07-20", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), new DateOnly(2026, 7, 20),
            new Dictionary<long, int> { [1000] = 1 }, epoch: "E1", version: "v1"));
        Assert.Throws<InvalidOperationException>(() =>
            ledger.Upsert(Contrib("PI-2026-07-20", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), new DateOnly(2026, 7, 20),
                new Dictionary<long, int> { [1000] = 1 }, epoch: "E2", version: "v2")));
    }

    [Fact]
    public void Ledger_tick_mismatch_and_lower_version_rejected()
    {
        var ledger = new CompletedAuctionLedger();
        Assert.True(ledger.Upsert(Contrib("PI-2026-07-20", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), new DateOnly(2026, 7, 20),
            new Dictionary<long, int> { [1000] = 1 }, tick: 0.1m, version: "v2")));
        Assert.Throws<InvalidOperationException>(() =>
            ledger.Upsert(Contrib("PI-2026-07-21", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), new DateOnly(2026, 7, 21),
                new Dictionary<long, int> { [1000] = 1 }, tick: 0.25m, version: "v1")));
        Assert.False(ledger.Upsert(Contrib("PI-2026-07-20", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), new DateOnly(2026, 7, 20),
            new Dictionary<long, int> { [1000] = 9 }, tick: 0.1m, version: "v1")));
        Assert.Equal(1, ledger.AllOrdered()[0].PriceLevelTpoCounts[1000]);
    }

    [Fact]
    public void Merge_evidence_poc_displacement_and_outside_share()
    {
        var grid = new PriceGrid(0.1m);
        Assert.Equal(5L, CompositeMergeEvidenceCalculator.DisplacementTicks(100.0m, 100.5m, grid));
        Assert.Null(CompositeMergeEvidenceCalculator.DisplacementTicks(100.0m, null, grid));
        var share = CompositeMergeEvidenceCalculator.OutsideShare(
            new Dictionary<long, int> { [1000] = 1, [1010] = 3 }, 1000, 1005);
        Assert.Equal(0.75m, share);
        Assert.Null(CompositeMergeEvidenceCalculator.OutsideShare(new Dictionary<long, int>(), 0, 1));
    }

    [Fact]
    public void Preview_disabled_removes_preview()
    {
        var c1 = Contrib("PI-2026-07-20", new DateTime(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 21, 12, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 7, 20),
            new Dictionary<long, int> { [1000] = 5 }, new Dictionary<long, decimal> { [1000] = 50m });
        var developing = Contrib("PI-2026-07-21", new DateTime(2026, 7, 21, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 7, 21),
            new Dictionary<long, int> { [2000] = 9 }, new Dictionary<long, decimal> { [2000] = 90m }, completed: false);

        var host = new CompositeProfileHost(0.1m);
        host.Configure(0.1m, 0.70m, "GCQ6", "E1",
            new CompositePolicyConfig(CompositePolicyMode.OperatorAnchored, "PI-2026-07-20", true,
                enableDevelopingCompositePreview: true));
        Assert.NotNull(host.Rebuild(new[] { PrimaryFromContrib(c1) }, PrimaryFromContrib(developing)).Preview);

        host.Configure(0.1m, 0.70m, "GCQ6", "E1",
            new CompositePolicyConfig(CompositePolicyMode.OperatorAnchored, "PI-2026-07-20", true,
                enableDevelopingCompositePreview: false));
        Assert.Null(host.Rebuild(new[] { PrimaryFromContrib(c1) }, PrimaryFromContrib(developing)).Preview);
    }

    [Fact]
    public void Aggregation_order_independent_and_preserves_totals()
    {
        var a = Contrib("PI-2026-07-20", new DateTime(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 21, 12, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 7, 20),
            new Dictionary<long, int> { [1000] = 2, [1001] = 1 },
            new Dictionary<long, decimal> { [1000] = 10m, [1001] = 5m });
        var b = Contrib("PI-2026-07-21", new DateTime(2026, 7, 21, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 7, 21),
            new Dictionary<long, int> { [1001] = 3, [1002] = 1 },
            new Dictionary<long, decimal> { [1001] = 7m, [1002] = 3m });

        var r1 = CompositeAggregator.Aggregate(new[] { a, b }, 0.1m, 0.70m);
        var r2 = CompositeAggregator.Aggregate(new[] { b, a }, 0.1m, 0.70m);
        Assert.Equal(r1.TotalTpoCount, r2.TotalTpoCount);
        Assert.Equal(r1.TotalExecutedVolume, r2.TotalExecutedVolume);
        Assert.Equal(7, r1.TotalTpoCount); // 2+1+3+1
        Assert.Equal(25m, r1.TotalExecutedVolume);
        Assert.Equal(4, r1.TpoCounts[1001]);
        Assert.Equal(r1.TpoPoc, r2.TpoPoc);
        Assert.Equal(r1.VolumePoc, r2.VolumePoc);
    }

    [Fact]
    public void Aggregation_partial_when_volume_unavailable()
    {
        var a = Contrib("PI-2026-07-20", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), new DateOnly(2026, 7, 20),
            new Dictionary<long, int> { [1000] = 1 },
            new Dictionary<long, decimal> { [1000] = 10m });
        var b = Contrib("PI-2026-07-21", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), new DateOnly(2026, 7, 21),
            new Dictionary<long, int> { [1000] = 1 },
            cap: PriceVolumeCapability.Unavailable);
        var r = CompositeAggregator.Aggregate(new[] { a, b }, 0.1m, 0.70m);
        Assert.Equal(ProfileDataQuality.Partial, r.DataQuality);
        Assert.NotNull(r.TpoPoc);
        Assert.NotNull(r.VolumePoc); // from exact contribution only
    }

    [Fact]
    public void Operator_anchor_awaiting_and_ready()
    {
        var host = new CompositeProfileHost(0.1m, 0.70m, new CompositePolicyConfig(CompositePolicyMode.OperatorAnchored));
        host.Configure(0.1m, 0.70m, "GCQ6", "E1", new CompositePolicyConfig(CompositePolicyMode.OperatorAnchored, anchorAuctionId: null));
        var empty = host.Rebuild(Array.Empty<PrimaryAuctionProfileSnapshot>(), null);
        Assert.Equal(CompositeStatus.AwaitingAnchor, empty.Confirmed.CompositeStatus);

        var c1 = Contrib("PI-2026-07-20", new DateTime(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 21, 12, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 7, 20),
            new Dictionary<long, int> { [1000] = 2 },
            new Dictionary<long, decimal> { [1000] = 10m });
        var c2 = Contrib("PI-2026-07-21", new DateTime(2026, 7, 21, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 7, 21),
            new Dictionary<long, int> { [1000] = 1 },
            new Dictionary<long, decimal> { [1000] = 5m });
        host.Configure(0.1m, 0.70m, "GCQ6", "E1",
            new CompositePolicyConfig(CompositePolicyMode.OperatorAnchored, "PI-2026-07-20", true));
        var set = host.Rebuild(new[] { PrimaryFromContrib(c1), PrimaryFromContrib(c2) }, null);
        Assert.Equal(CompositeStatus.Ready, set.Confirmed.CompositeStatus);
        Assert.Equal(2, set.Confirmed.CompletedContributionCount);
        Assert.Contains("PI-2026-07-20", set.Confirmed.CompositeId);
        Assert.Null(set.Preview);
    }

    [Fact]
    public void Exclusion_and_deterministic_composite_id()
    {
        var c1 = Contrib("PI-2026-07-20", new DateTime(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 21, 12, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 7, 20),
            new Dictionary<long, int> { [1000] = 1 }, new Dictionary<long, decimal> { [1000] = 1m });
        var c2 = Contrib("PI-2026-07-21", new DateTime(2026, 7, 21, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 7, 21),
            new Dictionary<long, int> { [1000] = 1 }, new Dictionary<long, decimal> { [1000] = 1m });
        var c3 = Contrib("PI-2026-07-22", new DateTime(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 7, 22),
            new Dictionary<long, int> { [1000] = 1 }, new Dictionary<long, decimal> { [1000] = 1m });

        var host = new CompositeProfileHost(0.1m);
        host.Configure(0.1m, 0.70m, "GCQ6", "E1",
            new CompositePolicyConfig(CompositePolicyMode.OperatorAnchored, "PI-2026-07-20", true,
                excludedAuctionIds: new[] { "PI-2026-07-21" }));
        var set = host.Rebuild(new[] { PrimaryFromContrib(c1), PrimaryFromContrib(c2), PrimaryFromContrib(c3) }, null);
        Assert.Equal(new[] { "PI-2026-07-20", "PI-2026-07-22" }, set.Confirmed.IncludedAuctionIds);
        var id1 = set.Confirmed.CompositeId;
        var set2 = host.Rebuild(new[] { PrimaryFromContrib(c3), PrimaryFromContrib(c1), PrimaryFromContrib(c2) }, null);
        Assert.Equal(id1, set2.Confirmed.CompositeId);
    }

    [Fact]
    public void Preview_does_not_mutate_confirmed()
    {
        var c1 = Contrib("PI-2026-07-20", new DateTime(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 21, 12, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 7, 20),
            new Dictionary<long, int> { [1000] = 5 }, new Dictionary<long, decimal> { [1000] = 50m });
        var developing = Contrib("PI-2026-07-21", new DateTime(2026, 7, 21, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 7, 21),
            new Dictionary<long, int> { [2000] = 9 }, new Dictionary<long, decimal> { [2000] = 90m }, completed: false);

        var host = new CompositeProfileHost(0.1m);
        host.Configure(0.1m, 0.70m, "GCQ6", "E1",
            new CompositePolicyConfig(CompositePolicyMode.OperatorAnchored, "PI-2026-07-20", true,
                enableDevelopingCompositePreview: true));
        var set = host.Rebuild(new[] { PrimaryFromContrib(c1) }, PrimaryFromContrib(developing));
        Assert.Equal(1, set.Confirmed.CompletedContributionCount);
        Assert.Equal(100.0m, set.Confirmed.TpoPoc);
        Assert.NotNull(set.Preview);
        Assert.Equal(set.Confirmed.CompositeId, set.Preview!.BaseCompositeId);
        Assert.Contains(2000, set.Preview.Aggregate.TpoCounts.Keys);
        Assert.DoesNotContain(2000, set.Confirmed.Aggregate!.TpoCounts.Keys);
    }

    [Fact]
    public void Merge_evidence_overlap_and_zero_width_null()
    {
        Assert.Null(CompositeMergeEvidenceCalculator.ValueAreaOverlapRatio(100m, 100m, 100m, 101m));
        var ov = CompositeMergeEvidenceCalculator.ValueAreaOverlapRatio(100m, 110m, 105m, 115m);
        Assert.NotNull(ov);
        Assert.True(ov > 0m && ov < 1m);
        Assert.Equal(0m, CompositeMergeEvidenceCalculator.ValueAreaOverlapRatio(100m, 110m, 120m, 130m));
    }

    [Fact]
    public void Shadow_evidence_not_calibrated_by_default()
    {
        var policy = new CompositePolicyConfig(CompositePolicyMode.OperatorAnchored, "PI-2026-07-20",
            enableShadowEvidence: true);
        Assert.Equal(CompositeEvidenceState.NotCalibrated,
            CompositeMergeEvidenceCalculator.EvaluateShadow(policy, Array.Empty<CompositeMergeEvidence>()));
    }

    [Fact]
    public void History_gap_reporting()
    {
        var a = Contrib("PI-2026-07-20", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), new DateOnly(2026, 7, 20),
            new Dictionary<long, int> { [1000] = 1 });
        var b = Contrib("PI-2026-07-22", DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(3), new DateOnly(2026, 7, 22),
            new Dictionary<long, int> { [1000] = 1 });
        var gaps = CompositeProfileHost.ReportHistoryGaps(new[] { a, b });
        Assert.Contains(gaps, g => g.StartsWith("MISSING_HISTORY_GAP:", StringComparison.Ordinal));
    }

    [Fact]
    public void Gps_awaiting_ready_partial_and_no_thesis_rows()
    {
        var awaiting = new CompositeSetSnapshot(
            new ConfirmedCompositeProfileSnapshot(
                "CMP|AWAITING_ANCHOR", CompositePolicyMode.OperatorAnchored, CompositePolicyConfig.PolicyVersion,
                "GCQ6", "E1", 0.1m, null, null, null, Array.Empty<string>(), Array.Empty<string>(), 0, null, null,
                CompositeStatus.AwaitingAnchor, null, CompositeCapabilityState.AwaitingAnchor,
                CompositeEvidenceState.NotEvaluated, new[] { "COMPOSITE_AWAITING_ANCHOR" }, "test"),
            null, Array.Empty<CompositeMergeEvidence>(), Array.Empty<string>());
        var rows = AuctionGpsCardMapper.BuildCompositeLines(awaiting, false);
        Assert.Contains(rows, r => r == "COMPOSITE: AWAITING ANCHOR");
        Assert.Contains(rows, r => r == "COMPOSITE POLICY: OPERATOR ANCHORED");

        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(0.1m, 14));
        var observed = new ObservedInstrumentSnapshot(
            "GCQ6", "id", "GCQ6", "COMEX", new DateTime(2026, 8, 27), 0.1m, "GC", "GCQ6", "COMEX", 0.1m, null);
        var snap = engine.Publish(
            observed, "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, DateTime.UtcNow, false, true, false, false, false, false,
            profiles: null, composite: awaiting);
        var vm = AuctionGpsCardMapper.FromSnapshot(snap, false);
        Assert.Contains(vm.ProfileDetailLines, l => l.StartsWith("COMPOSITE:", StringComparison.Ordinal));
        Assert.DoesNotContain(vm.ProfileDetailLines, l => l.Contains("THESIS", StringComparison.Ordinal));
        Assert.DoesNotContain(vm.AllLines(false), l => l.StartsWith("STRUCTURAL CONTEXT:", StringComparison.Ordinal));
        Assert.Equal(DataState.Degraded, snap.DataGate.DataState); // composite ready not forced here; awaiting + no profile
    }

    [Fact]
    public void Composite_ready_does_not_force_global_data_ready()
    {
        var agg = CompositeAggregator.Aggregate(new[]
        {
            Contrib("PI-2026-07-20", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), new DateOnly(2026, 7, 20),
                new Dictionary<long, int> { [1000] = 2 }, new Dictionary<long, decimal> { [1000] = 10m })
        }, 0.1m, 0.70m);
        var confirmed = new ConfirmedCompositeProfileSnapshot(
            "CMP|X", CompositePolicyMode.OperatorAnchored, CompositePolicyConfig.PolicyVersion,
            "GCQ6", "E1", 0.1m, "PI-2026-07-20", "PI-2026-07-20", "PI-2026-07-20",
            new[] { "PI-2026-07-20" }, Array.Empty<string>(), 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(1),
            CompositeStatus.Ready, agg, CompositeCapabilityState.Ready, CompositeEvidenceState.NotCalibrated,
            Array.Empty<string>(), "test");
        var set = new CompositeSetSnapshot(confirmed, null, Array.Empty<CompositeMergeEvidence>(), Array.Empty<string>());

        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(0.1m, 14));
        var observed = new ObservedInstrumentSnapshot(
            "GCQ6", "id", "GCQ6", "COMEX", new DateTime(2026, 8, 27), 0.1m, "GC", "GCQ6", "COMEX", 0.1m, null);
        var snap = engine.Publish(
            observed, "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, DateTime.UtcNow, false, true, false, false, false, false,
            profiles: null, composite: set);
        Assert.NotEqual(DataState.Ready, snap.DataGate.DataState);
    }

    [Fact]
    public void Overlay_confirmed_vs_preview_labels()
    {
        var agg = CompositeAggregator.Aggregate(new[]
        {
            Contrib("PI-2026-07-20", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), new DateOnly(2026, 7, 20),
                new Dictionary<long, int> { [1000] = 2 }, new Dictionary<long, decimal> { [1000] = 10m })
        }, 0.1m, 0.70m);
        var confirmed = new ConfirmedCompositeProfileSnapshot(
            "CMP|X", CompositePolicyMode.OperatorAnchored, CompositePolicyConfig.PolicyVersion,
            "GCQ6", "E1", 0.1m, "PI-2026-07-20", "PI-2026-07-20", "PI-2026-07-20",
            new[] { "PI-2026-07-20" }, Array.Empty<string>(), 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(1),
            CompositeStatus.Ready, agg, CompositeCapabilityState.Ready, CompositeEvidenceState.NotEvaluated,
            Array.Empty<string>(), "test");
        var preview = new DevelopingCompositePreviewSnapshot("CMP|X", "PI-2026-07-21", agg, 0.1m, 0.1m, Array.Empty<string>());
        var set = new CompositeSetSnapshot(confirmed, preview, Array.Empty<CompositeMergeEvidence>(), Array.Empty<string>());
        var vm = PrimaryProfileOverlayViewModel.FromProfiles(null, false, set, enableCompositeOverlay: true, showCompositePreview: true);
        Assert.Contains(vm.Levels, l => l.Label.StartsWith("Confirmed Composite", StringComparison.Ordinal));
        Assert.Contains(vm.Levels, l => l.Label.StartsWith("Preview Composite", StringComparison.Ordinal));
    }

    [Fact]
    public void Indicator_defaults_composite_off_mbo_off()
    {
        var root = FindRepoRoot();
        var src = File.ReadAllText(Path.Combine(root, "src", "GC.AuctionFlow", "Atas", "GcAuctionFlowIndicator.cs"));
        Assert.Contains("EnableCompositeProfile = false", src, StringComparison.Ordinal);
        Assert.Contains("EnableMboLifecycleProbe = false", src, StringComparison.Ordinal);
        Assert.Contains("EnsureCompositeSnapshotInitializedForPublish", src, StringComparison.Ordinal);
        Assert.Contains("CompositePublishInitialization.ShouldProcess", src, StringComparison.Ordinal);
        Assert.Contains("EnableAuctionEpisodes = false", src, StringComparison.Ordinal);
        Assert.Contains("EnableStructuralReferences = false", src, StringComparison.Ordinal);
        Assert.DoesNotContain("DirectionalAuction", src, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductionThesis", src, StringComparison.Ordinal);
        Assert.DoesNotContain("FarAac", src, StringComparison.Ordinal);
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

        throw new InvalidOperationException("repo root not found");
    }
}
