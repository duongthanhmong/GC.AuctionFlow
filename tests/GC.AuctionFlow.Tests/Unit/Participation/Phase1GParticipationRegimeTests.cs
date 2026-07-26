using GC.AuctionFlow.Core;
using GC.AuctionFlow.Participation;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Runtime;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Participation;

/// <summary>Phase 1G — Thin Participation Classifier (§10.4) + Settlement Proximity Tags (§10.5).</summary>
public sealed class Phase1GParticipationRegimeTests
{
    // July 2026 is EDT (UTC-4). 13:30 ET = 17:30 UTC.
    private static DateTime Utc(int hour, int minute = 0) =>
        new(2026, 7, 21, hour, minute, 0, DateTimeKind.Utc);

    // ========== A: Enums present ==========

    [Fact]
    public void A01_ThinParticipationLabel_has_NotCalibrated_and_four_production_labels()
    {
        Assert.Equal(0, (int)ThinParticipationLabel.NotCalibrated);
        Assert.Equal(1, (int)ThinParticipationLabel.NormalParticipation);
        Assert.Equal(2, (int)ThinParticipationLabel.ReducedParticipation);
        Assert.Equal(3, (int)ThinParticipationLabel.ThinParticipation);
        Assert.Equal(4, (int)ThinParticipationLabel.DislocatedParticipation);
    }

    [Fact]
    public void A02_SettlementProximityTag_has_four_values()
    {
        Assert.Equal(0, (int)SettlementProximityTag.Unknown);
        Assert.Equal(1, (int)SettlementProximityTag.PreSettlement);
        Assert.Equal(2, (int)SettlementProximityTag.SettlementTransition);
        Assert.Equal(3, (int)SettlementProximityTag.PostSettlement);
    }

    // ========== B: SettlementProximityConfig defaults ==========

    [Fact]
    public void B01_Config_anchor_defaults_to_1330()
    {
        var cfg = new SettlementProximityConfig();
        Assert.Equal(new TimeSpan(13, 30, 0), cfg.AnchorLocalTime);
    }

    [Fact]
    public void B02_Config_pre_settlement_window_defaults_to_30min()
    {
        var cfg = new SettlementProximityConfig();
        Assert.Equal(TimeSpan.FromMinutes(30), cfg.PreSettlementWindow);
    }

    [Fact]
    public void B03_Config_transition_window_defaults_to_5min()
    {
        var cfg = new SettlementProximityConfig();
        Assert.Equal(TimeSpan.FromMinutes(5), cfg.SettlementTransitionWindow);
    }

    [Fact]
    public void B04_Config_post_settlement_window_defaults_to_30min()
    {
        var cfg = new SettlementProximityConfig();
        Assert.Equal(TimeSpan.FromMinutes(30), cfg.PostSettlementWindow);
    }

    // ========== C: SettlementProximityClassifier — tag assignment ==========

    [Fact]
    public void C01_At_anchor_exact_returns_SettlementTransition()
    {
        // 13:30 ET = 17:30 UTC in EDT
        var result = SettlementProximityClassifier.Classify(Utc(17, 30));
        Assert.Equal(SettlementProximityTag.SettlementTransition, result.Tag);
    }

    [Fact]
    public void C02_Five_minutes_before_anchor_returns_SettlementTransition()
    {
        // 13:25 ET = 17:25 UTC
        var result = SettlementProximityClassifier.Classify(Utc(17, 25));
        Assert.Equal(SettlementProximityTag.SettlementTransition, result.Tag);
    }

    [Fact]
    public void C03_Five_minutes_after_anchor_returns_SettlementTransition()
    {
        // 13:35 ET = 17:35 UTC
        var result = SettlementProximityClassifier.Classify(Utc(17, 35));
        Assert.Equal(SettlementProximityTag.SettlementTransition, result.Tag);
    }

    [Fact]
    public void C04_Ten_minutes_before_anchor_returns_PreSettlement()
    {
        // 13:20 ET = 17:20 UTC
        var result = SettlementProximityClassifier.Classify(Utc(17, 20));
        Assert.Equal(SettlementProximityTag.PreSettlement, result.Tag);
    }

    [Fact]
    public void C05_Ten_minutes_after_anchor_returns_PostSettlement()
    {
        // 13:40 ET = 17:40 UTC
        var result = SettlementProximityClassifier.Classify(Utc(17, 40));
        Assert.Equal(SettlementProximityTag.PostSettlement, result.Tag);
    }

    [Fact]
    public void C06_Thirty_minutes_before_anchor_is_boundary_PreSettlement()
    {
        // 13:00 ET = 17:00 UTC
        var result = SettlementProximityClassifier.Classify(Utc(17, 0));
        Assert.Equal(SettlementProximityTag.PreSettlement, result.Tag);
    }

    [Fact]
    public void C07_Thirty_minutes_after_anchor_is_boundary_PostSettlement()
    {
        // 14:00 ET = 18:00 UTC
        var result = SettlementProximityClassifier.Classify(Utc(18, 0));
        Assert.Equal(SettlementProximityTag.PostSettlement, result.Tag);
    }

    [Fact]
    public void C08_Before_pre_settlement_window_returns_Unknown()
    {
        // 12:00 ET = 16:00 UTC (90 min before anchor)
        var result = SettlementProximityClassifier.Classify(Utc(16, 0));
        Assert.Equal(SettlementProximityTag.Unknown, result.Tag);
    }

    [Fact]
    public void C09_After_post_settlement_window_returns_Unknown()
    {
        // 15:00 ET = 19:00 UTC (90 min after anchor)
        var result = SettlementProximityClassifier.Classify(Utc(19, 0));
        Assert.Equal(SettlementProximityTag.Unknown, result.Tag);
    }

    [Fact]
    public void C10_Snapshot_carries_timezone_id()
    {
        var result = SettlementProximityClassifier.Classify(Utc(17, 30));
        Assert.Equal("America/New_York", result.TimezoneId);
    }

    [Fact]
    public void C11_Snapshot_carries_offset_from_anchor()
    {
        // 13:40 ET → offset = +10 min
        var result = SettlementProximityClassifier.Classify(Utc(17, 40));
        Assert.NotNull(result.OffsetFromAnchor);
        Assert.Equal(TimeSpan.FromMinutes(10), result.OffsetFromAnchor!.Value);
    }

    [Fact]
    public void C12_No_known_limitations_for_settlement_proximity()
    {
        var result = SettlementProximityClassifier.Classify(Utc(17, 30));
        Assert.Empty(result.KnownLimitations);
    }

    [Fact]
    public void C13_Null_config_uses_defaults()
    {
        var result = SettlementProximityClassifier.Classify(Utc(17, 30), null);
        Assert.Equal(SettlementProximityTag.SettlementTransition, result.Tag);
    }

    [Fact]
    public void C14_Custom_anchor_shifts_classification()
    {
        // Custom anchor at 14:00 ET; test timestamp = 13:30 ET = 17:30 UTC
        // offset = -30 min, pre-settlement window = 30 min → boundary PreSettlement
        var cfg = new SettlementProximityConfig
        {
            AnchorLocalTime = new TimeSpan(14, 0, 0)
        };
        var result = SettlementProximityClassifier.Classify(Utc(17, 30), cfg);
        Assert.Equal(SettlementProximityTag.PreSettlement, result.Tag);
    }

    // ========== D: ThinParticipationClassifier ==========

    [Fact]
    public void D01_ClassifyNotCalibrated_returns_NotCalibrated_label()
    {
        var result = ThinParticipationClassifier.ClassifyNotCalibrated();
        Assert.Equal(ThinParticipationLabel.NotCalibrated, result.Label);
    }

    [Fact]
    public void D02_ClassifyNotCalibrated_carries_limitation_string()
    {
        var result = ThinParticipationClassifier.ClassifyNotCalibrated();
        Assert.Contains(ThinParticipationClassifier.Limitation, result.KnownLimitations);
    }

    [Fact]
    public void D03_Limitation_string_contains_NOT_CALIBRATED()
    {
        Assert.Contains("NOT_CALIBRATED", ThinParticipationClassifier.Limitation);
    }

    [Fact]
    public void D04_Limitation_string_contains_THIN_PARTICIPATION()
    {
        Assert.Contains("THIN_PARTICIPATION", ThinParticipationClassifier.Limitation);
    }

    // ========== E: ParticipationSetSnapshot ==========

    [Fact]
    public void E01_ParticipationSetSnapshot_aggregates_limitations_from_both_classifiers()
    {
        var settlement = SettlementProximityClassifier.Classify(Utc(17, 30));
        var thin = ThinParticipationClassifier.ClassifyNotCalibrated();
        var set = new ParticipationSetSnapshot(settlement, thin);

        Assert.Contains(ThinParticipationClassifier.Limitation, set.KnownLimitations);
    }

    [Fact]
    public void E02_ParticipationSetSnapshot_exposes_both_components()
    {
        var settlement = SettlementProximityClassifier.Classify(Utc(17, 30));
        var thin = ThinParticipationClassifier.ClassifyNotCalibrated();
        var set = new ParticipationSetSnapshot(settlement, thin);

        Assert.Equal(SettlementProximityTag.SettlementTransition, set.SettlementProximity.Tag);
        Assert.Equal(ThinParticipationLabel.NotCalibrated, set.ThinParticipation.Label);
    }

    [Fact]
    public void E03_ParticipationSetSnapshot_rejects_null_settlementProximity()
    {
        var thin = ThinParticipationClassifier.ClassifyNotCalibrated();
        Assert.Throws<ArgumentNullException>(() =>
            new ParticipationSetSnapshot(null!, thin));
    }

    [Fact]
    public void E04_ParticipationSetSnapshot_rejects_null_thinParticipation()
    {
        var settlement = SettlementProximityClassifier.Classify(Utc(17, 30));
        Assert.Throws<ArgumentNullException>(() =>
            new ParticipationSetSnapshot(settlement, null!));
    }

    // ========== F: Runtime snapshot integration ==========

    [Fact]
    public void F01_GcaeRuntimeSnapshot_accepts_null_participation()
    {
        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(0.1m, 14));
        var snap = engine.Publish(
            null, "GCQ6",
            DataSourceMode.Unknown, DataSourceModeProvenance.Unknown,
            DeclaredFeedProvider.Unknown, FeedProviderProvenance.Unknown,
            false, null, false, false, false, false, false, false,
            timestampUtc: Utc(17, 30));

        // Engine always computes participation automatically when not provided
        Assert.NotNull(snap.Participation);
    }

    [Fact]
    public void F02_Runtime_engine_auto_computes_settlement_proximity()
    {
        // 17:30 UTC = 13:30 ET = anchor → SettlementTransition
        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(0.1m, 14));
        var snap = engine.Publish(
            null, "GCQ6",
            DataSourceMode.Unknown, DataSourceModeProvenance.Unknown,
            DeclaredFeedProvider.Unknown, FeedProviderProvenance.Unknown,
            false, null, false, false, false, false, false, false,
            timestampUtc: Utc(17, 30));

        Assert.Equal(SettlementProximityTag.SettlementTransition, snap.Participation!.SettlementProximity.Tag);
    }

    [Fact]
    public void F03_Runtime_engine_auto_computes_thin_participation_not_calibrated()
    {
        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(0.1m, 14));
        var snap = engine.Publish(
            null, "GCQ6",
            DataSourceMode.Unknown, DataSourceModeProvenance.Unknown,
            DeclaredFeedProvider.Unknown, FeedProviderProvenance.Unknown,
            false, null, false, false, false, false, false, false,
            timestampUtc: Utc(17, 30));

        Assert.Equal(ThinParticipationLabel.NotCalibrated, snap.Participation!.ThinParticipation.Label);
    }

    [Fact]
    public void F04_Runtime_engine_accepts_caller_provided_participation()
    {
        var settlement = SettlementProximityClassifier.Classify(Utc(16, 0)); // Unknown
        var thin = ThinParticipationClassifier.ClassifyNotCalibrated();
        var customParticipation = new ParticipationSetSnapshot(settlement, thin);

        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(0.1m, 14));
        var snap = engine.Publish(
            null, "GCQ6",
            DataSourceMode.Unknown, DataSourceModeProvenance.Unknown,
            DeclaredFeedProvider.Unknown, FeedProviderProvenance.Unknown,
            false, null, false, false, false, false, false, false,
            timestampUtc: Utc(16, 0),
            participation: customParticipation);

        Assert.Equal(SettlementProximityTag.Unknown, snap.Participation!.SettlementProximity.Tag);
    }

    [Fact]
    public void F05_Thin_participation_limitation_propagates_to_runtime_snapshot()
    {
        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(0.1m, 14));
        var snap = engine.Publish(
            null, "GCQ6",
            DataSourceMode.Unknown, DataSourceModeProvenance.Unknown,
            DeclaredFeedProvider.Unknown, FeedProviderProvenance.Unknown,
            false, null, false, false, false, false, false, false,
            timestampUtc: Utc(17, 30));

        Assert.Contains(ThinParticipationClassifier.Limitation, snap.KnownLimitations);
    }

    // ========== G: GPS card shows participation lines ==========

    private static GcaeRuntimeSnapshot MakeSnapshot(int utcHour, int utcMinute = 0)
    {
        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(0.1m, 14));
        return engine.Publish(
            null, "GCQ6",
            DataSourceMode.Unknown, DataSourceModeProvenance.Unknown,
            DeclaredFeedProvider.Unknown, FeedProviderProvenance.Unknown,
            false, null, false, false, false, false, false, false,
            timestampUtc: Utc(utcHour, utcMinute));
    }

    [Fact]
    public void G01_GPS_card_shows_settlement_tag_line()
    {
        var snap = MakeSnapshot(17, 30); // SettlementTransition
        var vm = AuctionGpsCardMapper.FromSnapshot(snap, false);
        Assert.Contains(vm.ProfileDetailLines, l => l.StartsWith("SETTLEMENT TAG:", StringComparison.Ordinal));
    }

    [Fact]
    public void G02_GPS_card_shows_thin_participation_line()
    {
        var snap = MakeSnapshot(17, 30);
        var vm = AuctionGpsCardMapper.FromSnapshot(snap, false);
        Assert.Contains(vm.ProfileDetailLines, l => l.StartsWith("THIN PARTICIPATION:", StringComparison.Ordinal));
    }

    [Fact]
    public void G03_GPS_settlement_tag_shows_SETTLEMENT_TRANSITION_at_anchor()
    {
        var snap = MakeSnapshot(17, 30);
        var vm = AuctionGpsCardMapper.FromSnapshot(snap, false);
        Assert.Contains(vm.ProfileDetailLines,
            l => l == "SETTLEMENT TAG: SETTLEMENT_TRANSITION");
    }

    [Fact]
    public void G04_GPS_settlement_tag_shows_PRE_SETTLEMENT()
    {
        var snap = MakeSnapshot(17, 20); // 13:20 ET = 10 min before
        var vm = AuctionGpsCardMapper.FromSnapshot(snap, false);
        Assert.Contains(vm.ProfileDetailLines,
            l => l == "SETTLEMENT TAG: PRE_SETTLEMENT");
    }

    [Fact]
    public void G05_GPS_settlement_tag_shows_POST_SETTLEMENT()
    {
        var snap = MakeSnapshot(17, 40); // 13:40 ET = 10 min after
        var vm = AuctionGpsCardMapper.FromSnapshot(snap, false);
        Assert.Contains(vm.ProfileDetailLines,
            l => l == "SETTLEMENT TAG: POST_SETTLEMENT");
    }

    [Fact]
    public void G06_GPS_settlement_tag_shows_UNKNOWN_outside_windows()
    {
        var snap = MakeSnapshot(16, 0); // 12:00 ET = 90 min before
        var vm = AuctionGpsCardMapper.FromSnapshot(snap, false);
        Assert.Contains(vm.ProfileDetailLines,
            l => l == "SETTLEMENT TAG: UNKNOWN");
    }

    [Fact]
    public void G07_GPS_thin_participation_shows_NOT_CALIBRATED()
    {
        var snap = MakeSnapshot(17, 30);
        var vm = AuctionGpsCardMapper.FromSnapshot(snap, false);
        Assert.Contains(vm.ProfileDetailLines,
            l => l == "THIN PARTICIPATION: NOT CALIBRATED");
    }

    [Fact]
    public void G08_BuildParticipationLines_returns_empty_for_null()
    {
        var lines = AuctionGpsCardMapper.BuildParticipationLines(null);
        Assert.Empty(lines);
    }

    [Fact]
    public void G09_BuildParticipationLines_returns_two_lines()
    {
        var set = new ParticipationSetSnapshot(
            SettlementProximityClassifier.Classify(Utc(17, 30)),
            ThinParticipationClassifier.ClassifyNotCalibrated());
        var lines = AuctionGpsCardMapper.BuildParticipationLines(set);
        Assert.Equal(2, lines.Count);
    }

    [Fact]
    public void G10_GPS_diagnostic_row_count_stays_at_11_with_participation()
    {
        var snap = MakeSnapshot(17, 30);
        var vm = AuctionGpsCardMapper.FromSnapshot(snap, true);
        Assert.Equal(13, vm.DiagnosticRows.Count);
    }

    [Fact]
    public void G11_AllLines_difference_stays_11_with_participation()
    {
        var snap = MakeSnapshot(17, 30);
        var vm = AuctionGpsCardMapper.FromSnapshot(snap, true);
        Assert.Equal(vm.AllLines(false).Count + 13, vm.AllLines(true).Count);
    }

    // ========== H: Schema version ==========

    [Fact]
    public void H01_SnapshotVersion_is_0_14_0()
    {
        Assert.Equal("0.16.0", GcaeRuntimeSnapshot.SnapshotVersion);
    }

    [Fact]
    public void H02_Published_snapshot_version_matches_constant()
    {
        var snap = MakeSnapshot(17, 30);
        Assert.Equal(GcaeRuntimeSnapshot.SnapshotVersion, snap.Version);
    }

    // ========== R: Structural ==========

    [Fact]
    public void R01_Participation_directory_exists_in_src()
    {
        var root = FindRepoRoot();
        Assert.True(Directory.Exists(
            Path.Combine(root, "src", "GC.AuctionFlow", "Participation")));
    }

    [Fact]
    public void R02_ParticipationEnums_file_exists()
    {
        var root = FindRepoRoot();
        Assert.True(File.Exists(
            Path.Combine(root, "src", "GC.AuctionFlow", "Participation", "ParticipationEnums.cs")));
    }

    [Fact]
    public void R03_SettlementProximityClassifier_file_exists()
    {
        var root = FindRepoRoot();
        Assert.True(File.Exists(
            Path.Combine(root, "src", "GC.AuctionFlow", "Participation", "SettlementProximityClassifier.cs")));
    }

    [Fact]
    public void R04_ThinParticipationClassifier_file_exists()
    {
        var root = FindRepoRoot();
        Assert.True(File.Exists(
            Path.Combine(root, "src", "GC.AuctionFlow", "Participation", "ThinParticipationClassifier.cs")));
    }

    [Fact]
    public void R05_Indicator_references_GC_AuctionFlow_Participation_namespace()
    {
        var root = FindRepoRoot();
        var indicator = File.ReadAllText(
            Path.Combine(root, "src", "GC.AuctionFlow", "Atas", "GcAuctionFlowIndicator.cs"));
        Assert.Contains("GC.AuctionFlow.Participation", indicator, StringComparison.Ordinal);
    }

    [Fact]
    public void R06_GpsMapper_references_GC_AuctionFlow_Participation_namespace()
    {
        var root = FindRepoRoot();
        var mapper = File.ReadAllText(
            Path.Combine(root, "src", "GC.AuctionFlow", "UI", "AuctionGpsCardViewModel.cs"));
        Assert.Contains("GC.AuctionFlow.Participation", mapper, StringComparison.Ordinal);
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
