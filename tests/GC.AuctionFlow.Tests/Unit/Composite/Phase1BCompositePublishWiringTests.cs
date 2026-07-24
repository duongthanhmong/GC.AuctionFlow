using GC.AuctionFlow.Composite;
using GC.AuctionFlow.Core;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Runtime;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Composite;

/// <summary>
/// Wiring-level regression for Phase 1B Composite publish init + operator config fingerprint.
/// </summary>
public sealed class Phase1BCompositePublishWiringTests
{
    private const decimal Tick = 0.1m;

    private static PrimaryProfileSetSnapshot ReadyProfiles(params string[] completedIds)
    {
        if (completedIds.Length == 0)
            completedIds = ["PI-2026-07-20"];

        var completed = new List<PrimaryAuctionProfileSnapshot>();
        PrimaryAuctionProfileSnapshot? current = null;
        for (var i = 0; i < completedIds.Length; i++)
        {
            var id = completedIds[i];
            var day = DateOnly.Parse(id.AsSpan("PI-".Length));
            var start = new DateTime(day.Year, day.Month, day.Day, 12, 0, 0, DateTimeKind.Utc);
            var end = start.AddDays(1);
            var snap = MakeAuction(id, start, end, completed: true);
            completed.Add(snap);
            current = snap;
        }

        return new PrimaryProfileSetSnapshot(
            current, completed.Count > 1 ? completed[^2] : null,
            HistoricalInitializationState.Complete, 10,
            Array.Empty<string>(), Array.Empty<string>(), null, completed);
    }

    private static PrimaryAuctionProfileSnapshot MakeAuction(string id, DateTime start, DateTime end, bool completed)
    {
        var tpo = new TpoProfileSnapshot(
            id, start, end,
            AuctionTimezoneResolver.IanaAmericaNewYork, new TimeSpan(8, 20, 0), 30,
            100.5m, 99.5m, 100.0m, 100.2m, 99.8m,
            2, 1, null,
            new Dictionary<long, int> { [1000] = 2 },
            ProfileDataQuality.Complete, "test", Array.Empty<string>());
        var vol = new VolumeProfileSnapshot(
            id, 100.5m, 99.5m, 100.0m, 100.2m, 99.8m,
            20m, new Dictionary<long, decimal> { [1000] = 20m },
            PriceVolumeCapability.Exact, ProfileDataQuality.Complete, "test", Array.Empty<string>());
        return new PrimaryAuctionProfileSnapshot(
            AuctionProfileState.Ready, id, start, end, completed,
            tpo, vol, 100.5m, 99.5m, 100.0m, end, null, ProfileDataQuality.Complete, Array.Empty<string>());
    }

    private sealed class TradeStyleCompositePublisher
    {
        private readonly PrimaryProfileSetSnapshot _profiles;
        public CompositeProfileHost? Host;
        public CompositeOperatorConfiguration? LastApplied;
        public int RebuildCount { get; private set; }
        public bool EnableComposite = true;
        public string? Anchor = "";
        public bool IncludeThroughLatest = true;
        public IReadOnlyList<string> Excluded = Array.Empty<string>();
        public bool Preview;
        public bool Shadow;

        public TradeStyleCompositePublisher(PrimaryProfileSetSnapshot profiles) => _profiles = profiles;

        public CompositePolicyConfig BuildPolicy() =>
            new(
                CompositePolicyMode.OperatorAnchored,
                Anchor,
                IncludeThroughLatest,
                Excluded,
                Preview,
                Shadow);

        public CompositeSetSnapshot? Publish()
        {
            if (!EnableComposite)
            {
                Host = null;
                LastApplied = null;
                return null;
            }

            var policy = BuildPolicy();
            var fingerprint = CompositeOperatorConfiguration.FromPolicy(policy);
            if (CompositePublishInitialization.ShouldProcess(
                    EnableComposite,
                    _profiles,
                    Host,
                    LastApplied,
                    fingerprint))
            {
                RebuildCount++;
                Host ??= new CompositeProfileHost(Tick, 0.70m, policy);
                Host.Configure(Tick, 0.70m, "GCQ6", "GCQ6|tick=0.1", policy);
                var developing = _profiles.CurrentAuction is { IsCompleted: false } cur ? cur : null;
                Host.Rebuild(_profiles.CompletedAuctions, developing);
                if (Host.Current is not null)
                    LastApplied = CompositeOperatorConfiguration.FromPolicy(policy);
            }

            return Host?.Current;
        }
    }

    [Fact]
    public void Guarded_publish_blank_anchor_initializes_awaiting_once()
    {
        var pub = new TradeStyleCompositePublisher(ReadyProfiles());
        pub.Anchor = "";
        var snap = pub.Publish();
        Assert.NotNull(snap);
        Assert.Equal(CompositeStatus.AwaitingAnchor, snap!.Confirmed.CompositeStatus);
        Assert.Null(snap.Confirmed.AnchorAuctionId);
        Assert.Equal(1, pub.RebuildCount);
        Assert.Equal(
            new[] { "COMPOSITE: AWAITING ANCHOR", "COMPOSITE POLICY: OPERATOR ANCHORED" },
            AuctionGpsCardMapper.BuildCompositeLines(snap, false).ToArray());
    }

    [Fact]
    public void Blank_to_nonblank_anchor_propagates_on_trade_style_publish()
    {
        var profiles = ReadyProfiles("PI-2026-07-20", "PI-2026-07-21");
        var pub = new TradeStyleCompositePublisher(profiles) { Anchor = "" };
        var awaiting = pub.Publish();
        Assert.Equal(1, pub.RebuildCount);
        Assert.Null(awaiting!.Confirmed.AnchorAuctionId);

        pub.Anchor = "PI-2026-07-20";
        var after = pub.Publish();
        Assert.Equal(2, pub.RebuildCount);
        Assert.Equal("PI-2026-07-20", after!.Confirmed.AnchorAuctionId);
        Assert.True(after.Confirmed.CompositeStatus is CompositeStatus.Ready or CompositeStatus.Partial);

        var diag = AuctionGpsCardMapper.BuildCompositeLines(after, showDiagnostics: true);
        Assert.Contains(diag, r => r == "COMPOSITE ANCHOR: PI-2026-07-20");
        Assert.DoesNotContain(diag, r => r == "COMPOSITE ANCHOR: —");
    }

    [Fact]
    public void Unchanged_configuration_reuses_without_rebuild()
    {
        var pub = new TradeStyleCompositePublisher(ReadyProfiles()) { Anchor = "PI-2026-07-20" };
        var first = pub.Publish();
        var id = first!.Confirmed.CompositeId;
        for (var i = 0; i < 5; i++)
            pub.Publish();
        Assert.Equal(1, pub.RebuildCount);
        Assert.Equal(id, pub.Host!.Current!.Confirmed.CompositeId);
    }

    [Fact]
    public void Normalized_equivalent_anchor_does_not_rebuild()
    {
        var pub = new TradeStyleCompositePublisher(ReadyProfiles()) { Anchor = "PI-2026-07-20" };
        pub.Publish();
        Assert.Equal(1, pub.RebuildCount);

        pub.Anchor = "  PI-2026-07-20  ";
        pub.Publish();
        Assert.Equal(1, pub.RebuildCount);
        Assert.Equal(
            CompositeOperatorConfiguration.FromPolicy(new CompositePolicyConfig(CompositePolicyMode.OperatorAnchored, "PI-2026-07-20")),
            CompositeOperatorConfiguration.FromPolicy(new CompositePolicyConfig(CompositePolicyMode.OperatorAnchored, "  PI-2026-07-20  ")));
    }

    [Fact]
    public void Include_through_latest_toggle_rebuilds_once()
    {
        var profiles = ReadyProfiles("PI-2026-07-20", "PI-2026-07-21");
        var pub = new TradeStyleCompositePublisher(profiles)
        {
            Anchor = "PI-2026-07-20",
            IncludeThroughLatest = true
        };
        pub.Publish();
        Assert.Equal(1, pub.RebuildCount);

        pub.IncludeThroughLatest = false;
        pub.Publish();
        Assert.Equal(2, pub.RebuildCount);
        pub.Publish();
        Assert.Equal(2, pub.RebuildCount);
    }

    [Fact]
    public void Exclusion_change_rebuilds_equivalent_order_does_not()
    {
        var profiles = ReadyProfiles("PI-2026-07-20", "PI-2026-07-21", "PI-2026-07-22");
        var pub = new TradeStyleCompositePublisher(profiles)
        {
            Anchor = "PI-2026-07-20",
            Excluded = new[] { "PI-2026-07-22" }
        };
        pub.Publish();
        Assert.Equal(1, pub.RebuildCount);

        // Semantically equivalent set (policy orders/distincts) — rebuild must not fire.
        pub.Excluded = new[] { "PI-2026-07-22", "PI-2026-07-22" };
        pub.Publish();
        Assert.Equal(1, pub.RebuildCount);

        pub.Excluded = new[] { "PI-2026-07-21" };
        pub.Publish();
        Assert.Equal(2, pub.RebuildCount);
    }

    [Fact]
    public void Preview_flag_change_rebuilds_without_mutating_confirmed_membership()
    {
        var profiles = ReadyProfiles("PI-2026-07-20", "PI-2026-07-21");
        // Developing current separate from completed.
        var developing = MakeAuction(
            "PI-2026-07-22",
            new DateTime(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc),
            completed: false);
        var set = new PrimaryProfileSetSnapshot(
            developing, profiles.CompletedAuctions[^1],
            HistoricalInitializationState.Complete, 10,
            Array.Empty<string>(), Array.Empty<string>(), null, profiles.CompletedAuctions);

        var pub = new TradeStyleCompositePublisher(set)
        {
            Anchor = "PI-2026-07-20",
            Preview = false
        };
        var confirmedOff = pub.Publish()!;
        Assert.Null(confirmedOff.Preview);
        var confirmedId = confirmedOff.Confirmed.CompositeId;
        var membership = confirmedOff.Confirmed.IncludedAuctionIds.ToArray();

        pub.Preview = true;
        var withPreview = pub.Publish()!;
        Assert.Equal(2, pub.RebuildCount);
        Assert.Equal(confirmedId, withPreview.Confirmed.CompositeId);
        Assert.Equal(membership, withPreview.Confirmed.IncludedAuctionIds.ToArray());
        Assert.NotNull(withPreview.Preview);
    }

    [Fact]
    public void Shadow_flag_change_rebuilds_and_stays_not_calibrated()
    {
        var pub = new TradeStyleCompositePublisher(ReadyProfiles("PI-2026-07-20", "PI-2026-07-21"))
        {
            Anchor = "PI-2026-07-20",
            Shadow = false
        };
        var before = pub.Publish()!;
        var id = before.Confirmed.CompositeId;

        pub.Shadow = true;
        var after = pub.Publish()!;
        Assert.Equal(2, pub.RebuildCount);
        Assert.Equal(id, after.Confirmed.CompositeId);
        Assert.Equal(CompositeEvidenceState.NotCalibrated, after.Confirmed.EvidenceState);
    }

    [Fact]
    public void Composite_disabled_clears_fingerprint_and_reenable_rebuilds()
    {
        var pub = new TradeStyleCompositePublisher(ReadyProfiles()) { Anchor = "PI-2026-07-20" };
        pub.Publish();
        Assert.Equal(1, pub.RebuildCount);
        Assert.NotNull(pub.LastApplied);

        pub.EnableComposite = false;
        Assert.Null(pub.Publish());
        Assert.Null(pub.LastApplied);
        Assert.Null(pub.Host);
        Assert.Empty(AuctionGpsCardMapper.BuildCompositeLines(null, false));

        pub.EnableComposite = true;
        pub.Publish();
        Assert.Equal(2, pub.RebuildCount);
    }

    [Fact]
    public void Anchor_change_propagates_while_global_data_degraded()
    {
        var profiles = ReadyProfiles("PI-2026-07-20", "PI-2026-07-21");
        var pub = new TradeStyleCompositePublisher(profiles) { Anchor = "" };
        pub.Publish();
        pub.Anchor = "PI-2026-07-20";
        var snap = pub.Publish()!;

        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(Tick, 14));
        var observed = new ObservedInstrumentSnapshot(
            "GCQ6", "id", "GCQ6", "COMEX", new DateTime(2026, 8, 27), Tick, "GC", "GCQ6", "COMEX", Tick, null);
        var runtime = engine.Publish(
            observed, "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, DateTime.UtcNow, false, true, false, false, false, false,
            profiles: profiles, composite: snap, showCompositeDiagnostics: true);

        Assert.Equal(DataState.Degraded, runtime.DataGate.DataState);
        var vm = AuctionGpsCardMapper.FromSnapshot(runtime, false);
        Assert.Contains(vm.ProfileDetailLines, l => l == "COMPOSITE ANCHOR: PI-2026-07-20");
        Assert.DoesNotContain(vm.ProfileDetailLines, l => l == "COMPOSITE ANCHOR: —");
    }

    [Fact]
    public void Anchor_not_loaded_retains_requested_id()
    {
        var pub = new TradeStyleCompositePublisher(ReadyProfiles("PI-2026-07-21"))
        {
            Anchor = "PI-2026-07-20"
        };
        var snap = pub.Publish()!;
        Assert.Equal("PI-2026-07-20", snap.Confirmed.AnchorAuctionId);
        Assert.NotNull(snap.Confirmed.AnchorAuctionId);
        // Current host: empty included set with nonblank anchor → Building + ANCHOR= limitation
        // (ANCHOR_NOT_LOADED path reserved when included non-empty but missing anchor).
        Assert.True(
            snap.Confirmed.CompositeStatus is CompositeStatus.AwaitingAnchor or CompositeStatus.Building);
        Assert.Contains(
            snap.Confirmed.KnownLimitations,
            l => l.Contains("PI-2026-07-20", StringComparison.Ordinal)
                 || l.StartsWith("ANCHOR_NOT_LOADED:", StringComparison.Ordinal));

        var diag = AuctionGpsCardMapper.BuildCompositeLines(snap, true);
        Assert.Contains(diag, r => r == "COMPOSITE ANCHOR: PI-2026-07-20");
        Assert.DoesNotContain(diag, r => r == "COMPOSITE ANCHOR: —");
    }

    [Fact]
    public void Awaiting_row_present_while_global_data_degraded()
    {
        var pub = new TradeStyleCompositePublisher(ReadyProfiles()) { Anchor = "" };
        var snap = pub.Publish();
        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(Tick, 14));
        var observed = new ObservedInstrumentSnapshot(
            "GCQ6", "id", "GCQ6", "COMEX", new DateTime(2026, 8, 27), Tick, "GC", "GCQ6", "COMEX", Tick, null);
        var runtime = engine.Publish(
            observed, "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, DateTime.UtcNow, false, true, false, false, false, false,
            profiles: ReadyProfiles(), composite: snap);

        Assert.Equal(DataState.Degraded, runtime.DataGate.DataState);
        var vm = AuctionGpsCardMapper.FromSnapshot(runtime, false);
        Assert.Contains(vm.ProfileDetailLines, l => l == "COMPOSITE: AWAITING ANCHOR");
    }

    [Fact]
    public void Indicator_ensure_uses_fingerprint_should_process()
    {
        var root = FindRepoRoot();
        var src = File.ReadAllText(Path.Combine(root, "src", "GC.AuctionFlow", "Atas", "GcAuctionFlowIndicator.cs"));
        Assert.Contains("EnsureCompositeSnapshotInitializedForPublish", src, StringComparison.Ordinal);
        Assert.Contains("CompositePublishInitialization.ShouldProcess", src, StringComparison.Ordinal);
        Assert.Contains("_lastAppliedCompositeConfiguration", src, StringComparison.Ordinal);
        Assert.Contains("CompositeOperatorConfiguration.FromPolicy", src, StringComparison.Ordinal);

        var ensureIdx = src.IndexOf("EnsureCompositeSnapshotInitializedForPublish();", StringComparison.Ordinal);
        var readIdx = src.IndexOf(
            "var composite = EnableCompositeProfile ? _compositeHost?.Current : null;",
            ensureIdx >= 0 ? ensureIdx : 0,
            StringComparison.Ordinal);
        Assert.True(ensureIdx > 0 && readIdx > ensureIdx);
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
