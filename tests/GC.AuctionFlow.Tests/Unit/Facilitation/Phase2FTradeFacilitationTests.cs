using GC.AuctionFlow.Core;
using GC.AuctionFlow.Efficiency;
using GC.AuctionFlow.Facilitation;
using GC.AuctionFlow.Orderflow;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Runtime;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Facilitation;

/// <summary>
/// Phase 2F Trade Facilitation Index Foundation.
/// TradeFacilitationIndex = normalized relationship of direction-consistent effort and achieved auction progress.
/// All calibrated classification states → NotCalibrated.
/// FAR/AAC: NOT AUTHORIZED in Phase 2F.
/// </summary>
public sealed class Phase2FTradeFacilitationTests
{
    private const decimal Tick = 0.1m;
    private const string Instrument = "GCQ6";
    private const string Epoch = "GCQ6|tick=0.1";
    private const string Auction = "PI-2026-07-25";

    private static DateTime Utc(int sec = 0) =>
        new(2026, 7, 25, 12, 0, sec, DateTimeKind.Utc);

    // ---------- A: Policy & identity ----------

    [Fact]
    public void A01_PolicyVersion_is_TRADE_FACILITATION_POLICY_V1()
    {
        Assert.Equal("TRADE_FACILITATION_POLICY_V1", TradeFacilitationPolicyConfig.PolicyVersion);
    }

    [Fact]
    public void A02_SnapshotVersion_is_1_0_0()
    {
        Assert.Equal("1.0.0", TradeFacilitationSnapshot.SnapshotVersion);
        Assert.Equal("1.0.0", TradeFacilitationSetSnapshot.SnapshotVersion);
    }

    [Fact]
    public void A03_LimitationNotCalibrated_constant_is_correct()
    {
        Assert.Equal(
            "TRADE_FACILITATION_INDEX_THRESHOLDS_NOT_CALIBRATED",
            TradeFacilitationPolicyConfig.LimitationNotCalibrated);
    }

    [Fact]
    public void A04_LimitationHealthy_constant_defined()
    {
        Assert.False(string.IsNullOrEmpty(TradeFacilitationPolicyConfig.LimitationHealthyNotCalibrated));
    }

    [Fact]
    public void A05_LimitationFailing_constant_defined()
    {
        Assert.False(string.IsNullOrEmpty(TradeFacilitationPolicyConfig.LimitationFailingNotCalibrated));
    }

    [Fact]
    public void A06_LimitationNoFarAac_constant_defined()
    {
        Assert.False(string.IsNullOrEmpty(TradeFacilitationPolicyConfig.LimitationNoFarAac));
    }

    // ---------- B: Disabled module ----------

    [Fact]
    public void B01_Disabled_host_produces_Disabled_snapshot()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: false));
        var set = host.Rebuild(null);
        Assert.Equal(TradeFacilitationModuleState.Disabled, set.ModuleState);
    }

    [Fact]
    public void B02_Disabled_snapshot_has_MODULE_DISABLED_limitation()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: false));
        var set = host.Rebuild(null);
        Assert.Contains("MODULE_DISABLED", set.Limitations);
    }

    [Fact]
    public void B03_Disabled_snapshot_has_no_CurrentAuction()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: false));
        var set = host.Rebuild(null);
        Assert.Null(set.CurrentAuctionFacilitation);
        Assert.Empty(set.ActiveEpisodeFacilitations);
    }

    [Fact]
    public void B04_Configure_disabled_produces_Disabled_state()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: false));
        host.Configure(new TradeFacilitationPolicyConfig(enabled: false));
        Assert.NotNull(host.Current);
        Assert.Equal(TradeFacilitationModuleState.Disabled, host.Current!.ModuleState);
    }

    // ---------- C: AwaitingEfficiency ----------

    [Fact]
    public void C01_Null_efficiency_produces_AwaitingEfficiency()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var set = host.Rebuild(null);
        Assert.Equal(TradeFacilitationModuleState.AwaitingEfficiency, set.ModuleState);
    }

    [Fact]
    public void C02_Disabled_efficiency_produces_AwaitingEfficiency()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var effHost = MakeEfficiencyHost(false);
        var set = host.Rebuild(effHost.Current);
        Assert.Equal(TradeFacilitationModuleState.AwaitingEfficiency, set.ModuleState);
    }

    [Fact]
    public void C03_AwaitingEfficiency_has_NOT_CALIBRATED_limitation()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var set = host.Rebuild(null);
        Assert.Contains(TradeFacilitationPolicyConfig.LimitationNotCalibrated, set.Limitations);
    }

    [Fact]
    public void C04_Invalid_efficiency_produces_Invalid_state()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var set = host.Rebuild(MakeInvalidEfficiencySet());
        Assert.Equal(TradeFacilitationModuleState.Invalid, set.ModuleState);
    }

    // ---------- D: NotCalibrated classification state ----------

    [Fact]
    public void D01_All_snapshots_produce_NotCalibrated_classification()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var set = host.Rebuild(BuildReadyEfficiency());

        if (set.CurrentAuctionFacilitation is not null)
            Assert.Equal(TradeFacilitationClassificationState.NotCalibrated,
                set.CurrentAuctionFacilitation.Classification);

        foreach (var ep in set.ActiveEpisodeFacilitations)
            Assert.Equal(TradeFacilitationClassificationState.NotCalibrated, ep.Classification);
    }

    [Fact]
    public void D02_NotCalibrated_enum_value_is_1()
    {
        Assert.Equal(1, (int)TradeFacilitationClassificationState.NotCalibrated);
    }

    [Fact]
    public void D03_Healthy_calibrated_state_enum_value_is_100()
    {
        Assert.Equal(100, (int)TradeFacilitationClassificationState.Healthy);
    }

    [Fact]
    public void D04_Failing_calibrated_state_enum_value_is_101()
    {
        Assert.Equal(101, (int)TradeFacilitationClassificationState.Failing);
    }

    [Fact]
    public void D05_Healthy_and_Failing_never_emitted()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var set = host.Rebuild(MakeSingleAuctionEfficiency(
            EfficiencyResultDirection.Up, 300m, 100m, 400m, 20));

        var allCls = new List<TradeFacilitationClassificationState>();
        if (set.CurrentAuctionFacilitation is not null)
            allCls.Add(set.CurrentAuctionFacilitation.Classification);
        allCls.AddRange(set.ActiveEpisodeFacilitations.Select(s => s.Classification));
        allCls.AddRange(set.RecentlyClosedFacilitations.Select(s => s.Classification));

        foreach (var cls in allCls)
            Assert.True(cls == TradeFacilitationClassificationState.NotCalibrated
                        || cls == TradeFacilitationClassificationState.Unknown,
                $"Calibrated state unexpectedly emitted: {cls}");
    }

    // ---------- E: Set-level policy propagation ----------

    [Fact]
    public void E01_PolicyVersion_propagated_to_set_snapshot()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var set = host.Rebuild(BuildReadyEfficiency());
        Assert.Equal(TradeFacilitationPolicyConfig.PolicyVersion, set.PolicyVersion);
    }

    [Fact]
    public void E02_Set_limitations_include_all_NOT_CALIBRATED_strings_after_processing()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var set = host.Rebuild(MakeSingleAuctionEfficiency(
            EfficiencyResultDirection.Up, 100m, 50m, 150m, 10));
        Assert.Contains(TradeFacilitationPolicyConfig.LimitationNotCalibrated, set.Limitations);
        Assert.Contains(TradeFacilitationPolicyConfig.LimitationHealthyNotCalibrated, set.Limitations);
        Assert.Contains(TradeFacilitationPolicyConfig.LimitationFailingNotCalibrated, set.Limitations);
        Assert.Contains(TradeFacilitationPolicyConfig.LimitationLiveOnly, set.Limitations);
    }

    [Fact]
    public void E03_Set_limitations_include_NoFarAac_after_processing()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var set = host.Rebuild(MakeSingleAuctionEfficiency(
            EfficiencyResultDirection.Up, 100m, 50m, 150m, 10));
        Assert.Contains(TradeFacilitationPolicyConfig.LimitationNoFarAac, set.Limitations);
    }

    // ---------- F: Fingerprint caching ----------

    [Fact]
    public void F01_Identical_efficiency_input_returns_cached_snapshot()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var efficiency = BuildReadyEfficiency();
        var set1 = host.Rebuild(efficiency);
        var set2 = host.Rebuild(efficiency);
        Assert.Same(set1, set2);
    }

    [Fact]
    public void F02_Reset_then_rebuild_returns_new_snapshot()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var set1 = host.Rebuild(null);
        host.Reset();
        var set2 = host.Rebuild(null);
        Assert.NotSame(set1, set2);
    }

    [Fact]
    public void F03_Reset_clears_current()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        host.Rebuild(BuildReadyEfficiency());
        host.Reset();
        Assert.Null(host.Current);
    }

    // ---------- G: Set-level counts ----------

    [Fact]
    public void G01_RecentlyClosedCapacity_is_64()
    {
        Assert.Equal(64, TradeFacilitationSetSnapshot.RecentlyClosedCapacity);
    }

    [Fact]
    public void G02_ReadyCount_and_PartialCount_non_negative()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var set = host.Rebuild(BuildReadyEfficiency());
        Assert.True(set.ReadyCount >= 0);
        Assert.True(set.PartialCount >= 0);
    }

    [Fact]
    public void G03_PolicyVersion_on_individual_snapshot()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var set = host.Rebuild(MakeSingleAuctionEfficiency(
            EfficiencyResultDirection.Up, 100m, 50m, 150m, 10));
        if (set.CurrentAuctionFacilitation is not null)
            Assert.Equal(TradeFacilitationPolicyConfig.PolicyVersion,
                set.CurrentAuctionFacilitation.PolicyVersion);
    }

    // ---------- H: Raw index component computation ----------

    [Fact]
    public void H01_Direction_Up_uses_AskVolume_as_direction_consistent_effort()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var set = host.Rebuild(MakeSingleAuctionEfficiency(
            EfficiencyResultDirection.Up, 300m, 100m, 400m, 20));

        var snap = set.CurrentAuctionFacilitation;
        Assert.NotNull(snap);
        Assert.Equal(300m, snap!.DirectionConsistentEffortVolume);
    }

    [Fact]
    public void H02_Direction_Down_uses_BidVolume_as_direction_consistent_effort()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var set = host.Rebuild(MakeSingleAuctionEfficiency(
            EfficiencyResultDirection.Down, 100m, 250m, 350m, 15));

        var snap = set.CurrentAuctionFacilitation;
        Assert.NotNull(snap);
        Assert.Equal(250m, snap!.DirectionConsistentEffortVolume);
    }

    [Fact]
    public void H03_Direction_Unknown_yields_null_direction_consistent_effort()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var set = host.Rebuild(MakeSingleAuctionEfficiency(
            EfficiencyResultDirection.Unknown, 100m, 100m, 200m, null));

        var snap = set.CurrentAuctionFacilitation;
        Assert.NotNull(snap);
        Assert.Null(snap!.DirectionConsistentEffortVolume);
    }

    [Fact]
    public void H04_DirectionConsistentEffortRatio_computed_correctly()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var set = host.Rebuild(MakeSingleAuctionEfficiency(
            EfficiencyResultDirection.Up, 300m, 100m, 400m, 20));

        var snap = set.CurrentAuctionFacilitation;
        Assert.NotNull(snap);
        // 300 / 400 = 0.75
        Assert.Equal(0.75m, snap!.DirectionConsistentEffortRatio);
    }

    [Fact]
    public void H05_AchievedFavorableProgressTicks_mapped_from_result()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var set = host.Rebuild(MakeSingleAuctionEfficiency(
            EfficiencyResultDirection.Up, 300m, 100m, 400m, 12));

        var snap = set.CurrentAuctionFacilitation;
        Assert.NotNull(snap);
        Assert.Equal(12L, snap!.AchievedFavorableProgressTicks);
    }

    [Fact]
    public void H06_FavorableProgressPerDirectionUnit_computed_correctly()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var set = host.Rebuild(MakeSingleAuctionEfficiency(
            EfficiencyResultDirection.Up, 100m, 50m, 150m, 50));

        var snap = set.CurrentAuctionFacilitation;
        Assert.NotNull(snap);
        // 50 ticks / 100 contracts = 0.5
        Assert.Equal(0.5m, snap!.FavorableProgressPerDirectionUnit);
    }

    [Fact]
    public void H07_Null_favorableProgress_yields_null_FavorableProgressPerDirectionUnit()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var set = host.Rebuild(MakeSingleAuctionEfficiency(
            EfficiencyResultDirection.Up, 200m, 100m, 300m, null));

        var snap = set.CurrentAuctionFacilitation;
        Assert.NotNull(snap);
        Assert.Null(snap!.FavorableProgressPerDirectionUnit);
    }

    [Fact]
    public void H08_Individual_snapshot_has_NOT_CALIBRATED_limitation()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var set = host.Rebuild(MakeSingleAuctionEfficiency(
            EfficiencyResultDirection.Up, 300m, 100m, 400m, 20));

        var snap = set.CurrentAuctionFacilitation;
        Assert.NotNull(snap);
        Assert.Contains(TradeFacilitationPolicyConfig.LimitationNotCalibrated, snap!.Limitations);
    }

    [Fact]
    public void H09_Unknown_direction_adds_DirectionUnavailable_limitation()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var set = host.Rebuild(MakeSingleAuctionEfficiency(
            EfficiencyResultDirection.Unknown, 100m, 100m, 200m, null));

        var snap = set.CurrentAuctionFacilitation;
        Assert.NotNull(snap);
        Assert.Contains(TradeFacilitationPolicyConfig.LimitationDirectionUnavailable, snap!.Limitations);
    }

    [Fact]
    public void H10_SnapshotId_prefixed_with_TF_colon()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var set = host.Rebuild(MakeSingleAuctionEfficiency(
            EfficiencyResultDirection.Up, 100m, 50m, 150m, 10));

        var snap = set.CurrentAuctionFacilitation;
        Assert.NotNull(snap);
        Assert.StartsWith("TF:", snap!.SnapshotId, StringComparison.Ordinal);
    }

    // ---------- I: Runtime integration ----------

    [Fact]
    public void I01_RuntimeSnapshot_SnapshotVersion_is_0_26_0()
    {
        Assert.Equal("0.26.0", GcaeRuntimeSnapshot.SnapshotVersion);
    }

    [Fact]
    public void I02_Runtime_Publish_accepts_null_tradeFacilitation()
    {
        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(0.1m, 14));
        var snap = engine.Publish(
            null, "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, Utc(), false, true, false, false, false, false,
            timestampUtc: Utc(),
            tradeFacilitation: null);
        Assert.Null(snap.TradeFacilitation);
    }

    [Fact]
    public void I03_Runtime_Publish_passes_tradeFacilitation_through()
    {
        var tfHost = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: false));
        var tfSet = tfHost.Rebuild(null);
        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(0.1m, 14));
        var snap = engine.Publish(
            null, "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, Utc(), false, true, false, false, false, false,
            timestampUtc: Utc(),
            tradeFacilitation: tfSet);
        Assert.Same(tfSet, snap.TradeFacilitation);
    }

    [Fact]
    public void I04_Runtime_Publish_tradeFacilitation_limitations_merged()
    {
        var tfHost = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var tfSet = tfHost.Rebuild(null);
        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(0.1m, 14));
        var snap = engine.Publish(
            null, "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, Utc(), false, true, false, false, false, false,
            timestampUtc: Utc(),
            tradeFacilitation: tfSet);
        Assert.Contains(TradeFacilitationPolicyConfig.LimitationNotCalibrated, snap.KnownLimitations);
    }

    // ---------- J: GPS diagnostic row ----------

    [Fact]
    public void J01_GPS_diagnostic_row_count_is_current()
    {
        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(0.1m, 14));
        var snap = engine.Publish(
            null, "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, Utc(), false, true, false, false, false, false,
            timestampUtc: Utc());
        var vm = AuctionGpsCardMapper.FromSnapshot(snap, showDiagnostics: true);
        Assert.Equal(25, vm.DiagnosticRows.Count);
    }

    [Fact]
    public void J02_GPS_has_TRADE_FACILITATION_row_when_showDiagnostics_true()
    {
        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(0.1m, 14));
        var snap = engine.Publish(
            null, "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, Utc(), false, true, false, false, false, false,
            timestampUtc: Utc());
        var vm = AuctionGpsCardMapper.FromSnapshot(snap, showDiagnostics: true);
        Assert.Contains(vm.DiagnosticRows,
            r => r.StartsWith("TRADE FACILITATION:", StringComparison.Ordinal));
    }

    [Fact]
    public void J03_GPS_TRADE_FACILITATION_row_says_NOT_AVAILABLE_when_null()
    {
        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(0.1m, 14));
        var snap = engine.Publish(
            null, "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, Utc(), false, true, false, false, false, false,
            timestampUtc: Utc(),
            tradeFacilitation: null);
        var vm = AuctionGpsCardMapper.FromSnapshot(snap, showDiagnostics: true);
        var tfRow = vm.DiagnosticRows.FirstOrDefault(r => r.StartsWith("TRADE FACILITATION:", StringComparison.Ordinal));
        Assert.NotNull(tfRow);
        Assert.Contains("NOT AVAILABLE", tfRow!, StringComparison.Ordinal);
    }

    [Fact]
    public void J04_BuildTradeFacilitationLines_null_returns_empty()
    {
        var lines = AuctionGpsCardMapper.BuildTradeFacilitationLines(null, false);
        Assert.Empty(lines);
    }

    [Fact]
    public void J05_BuildTradeFacilitationLines_Disabled_returns_DISABLED_row()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: false));
        var set = host.Rebuild(null);
        var lines = AuctionGpsCardMapper.BuildTradeFacilitationLines(set, false);
        Assert.Contains(lines, l => l.Contains("DISABLED", StringComparison.Ordinal));
    }

    [Fact]
    public void J06_BuildTradeFacilitationLines_AwaitingEfficiency_has_relevant_row()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var set = host.Rebuild(null);
        var lines = AuctionGpsCardMapper.BuildTradeFacilitationLines(set, false);
        Assert.True(lines.Count > 0);
        Assert.Contains(lines, l => l.Contains("AWAITING", StringComparison.Ordinal)
                                    || l.Contains("TRADE FACILITATION:", StringComparison.Ordinal));
    }

    [Fact]
    public void J07_BuildTradeFacilitationLines_shows_NOT_CALIBRATED_index_row()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var set = host.Rebuild(null);
        var lines = AuctionGpsCardMapper.BuildTradeFacilitationLines(set, false);
        Assert.Contains(lines, l => l.Contains("NOT CALIBRATED", StringComparison.Ordinal));
    }

    [Fact]
    public void J08_GPS_TRADE_FACILITATION_row_shows_AWAITINGEFFICIENCY_for_enabled_no_data()
    {
        var tfHost = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var tfSet = tfHost.Rebuild(null);
        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(0.1m, 14));
        var snap = engine.Publish(
            null, "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, Utc(), false, true, false, false, false, false,
            timestampUtc: Utc(),
            tradeFacilitation: tfSet);
        var vm = AuctionGpsCardMapper.FromSnapshot(snap, showDiagnostics: true);
        var tfRow = vm.DiagnosticRows.FirstOrDefault(r => r.StartsWith("TRADE FACILITATION:", StringComparison.Ordinal));
        Assert.NotNull(tfRow);
        Assert.Contains("AWAITINGEFFICIENCY", tfRow!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void J09_AllLines_difference_matches_the_diagnostic_row_count()
    {
        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(0.1m, 14));
        var snap = engine.Publish(
            null, "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, Utc(), false, true, false, false, false, false,
            timestampUtc: Utc());
        var vm = AuctionGpsCardMapper.FromSnapshot(snap, showDiagnostics: true);
        var withDiag = vm.AllLines(true).Count;
        var withoutDiag = vm.AllLines(false).Count;
        Assert.Equal(25, withDiag - withoutDiag);
    }

    // ---------- K: Enums ----------

    [Fact]
    public void K01_TradeFacilitationModuleState_Disabled_is_0()
    {
        Assert.Equal(0, (int)TradeFacilitationModuleState.Disabled);
    }

    [Fact]
    public void K02_TradeFacilitationModuleState_AwaitingEfficiency_is_1()
    {
        Assert.Equal(1, (int)TradeFacilitationModuleState.AwaitingEfficiency);
    }

    [Fact]
    public void K03_TradeFacilitationDataQuality_Complete_is_0()
    {
        Assert.Equal(0, (int)TradeFacilitationDataQuality.Complete);
    }

    [Fact]
    public void K04_DataQuality_partial_efficiency_maps_to_partial()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var set = host.Rebuild(MakeSingleAuctionEfficiency(
            EfficiencyResultDirection.Up, 100m, null, 100m, 5,
            dataQuality: EfficiencyDataQuality.Partial));

        var snap = set.CurrentAuctionFacilitation;
        Assert.NotNull(snap);
        Assert.Equal(TradeFacilitationDataQuality.Partial, snap!.DataQuality);
    }

    // ---------- Helpers ----------

    private static AuctionEfficiencyHost MakeEfficiencyHost(bool enabled) =>
        new AuctionEfficiencyHost(Tick, Instrument, Epoch,
            AtasTimestampNormalizer.PolicyVersion,
            new AuctionEfficiencyEvidencePolicyConfig(enabled));

    private static AuctionEfficiencyEvidenceSetSnapshot BuildReadyEfficiency()
    {
        var effHost = new AuctionEfficiencyHost(Tick, Instrument, Epoch,
            AtasTimestampNormalizer.PolicyVersion,
            new AuctionEfficiencyEvidencePolicyConfig(enabled: true));
        effHost.Configure(Tick, Instrument, Epoch,
            new AuctionEfficiencyEvidencePolicyConfig(enabled: true),
            AtasTimestampNormalizer.PolicyVersion);
        effHost.Rebuild(null, null, null, null, null);
        var current = effHost.Current;
        if (current is not null) return current;

        return new AuctionEfficiencyEvidenceSetSnapshot(
            EfficiencyModuleState.AwaitingOrderflow,
            AuctionEfficiencyEvidencePolicyConfig.PolicyVersion,
            null,
            Array.Empty<AuctionEfficiencyEvidenceSnapshot>(),
            Array.Empty<AuctionEfficiencyEvidenceSnapshot>(),
            null,
            0, 0, 0,
            null, 0L, null,
            Utc(), Utc(),
            Array.Empty<string>());
    }

    private static AuctionEfficiencyEvidenceSetSnapshot MakeInvalidEfficiencySet() =>
        new AuctionEfficiencyEvidenceSetSnapshot(
            EfficiencyModuleState.Invalid,
            AuctionEfficiencyEvidencePolicyConfig.PolicyVersion,
            null,
            Array.Empty<AuctionEfficiencyEvidenceSnapshot>(),
            Array.Empty<AuctionEfficiencyEvidenceSnapshot>(),
            null,
            0, 0, 0,
            null, 0L, null,
            Utc(), Utc(),
            new[] { "INVALID_INPUT" });

    private static AuctionEfficiencyEvidenceSetSnapshot MakeSingleAuctionEfficiency(
        EfficiencyResultDirection direction,
        decimal? askVolume,
        decimal? bidVolume,
        decimal classifiedVolume,
        long? favorableProgressTicks,
        EfficiencyDataQuality dataQuality = EfficiencyDataQuality.Complete)
    {
        var effort = new AuctionEffortEvidenceVector(
            totalExecutedVolume: classifiedVolume,
            tradeCount: 100L,
            priceLevelCount: 10,
            classifiedVolume: classifiedVolume,
            askVolume: askVolume,
            bidVolume: bidVolume,
            unknownAggressorVolume: 0m,
            classifiedDelta: (askVolume ?? 0m) - (bidVolume ?? 0m),
            absoluteClassifiedDelta: Math.Abs((askVolume ?? 0m) - (bidVolume ?? 0m)),
            classifiedCvdChange: null,
            aggressorCoverageRatio: null,
            observationDuration: null,
            minimumTradeInterval: null,
            maximumTradeInterval: null,
            meanTradeInterval: null,
            latestTradeInterval: null,
            tradesPerSecondRaw: null,
            contractsPerSecondRaw: null,
            maximumLevelExecutedVolume: null,
            maximumLevelTradeCount: null,
            maximumAbsoluteLevelDelta: null,
            revisitedLevelCount: 0,
            maximumVisitCount: 1,
            classifiedLevelCount: 10,
            unknownOnlyLevelCount: 0,
            samePriceRatioAvailabilityCount: 0,
            diagonalRatioAvailabilityCount: 0,
            rawAskDominantLevelCount: 0,
            rawBidDominantLevelCount: 0,
            rawEqualLevelCount: 0,
            rawUnknownDominantLevelCount: 0,
            maximumConsecutiveRawAskDominanceTicks: 0,
            maximumConsecutiveRawBidDominanceTicks: 0,
            clusterPopulationSize: 10,
            evidenceAvailability: EfficiencyAvailability.Available,
            limitations: Array.Empty<string>());

        var result = new AuctionResultEvidenceVector(
            firstPriceTick: 10000L,
            latestPriceTick: 10010L,
            highPriceTick: 10020L,
            lowPriceTick: 9980L,
            netPriceProgressTicks: favorableProgressTicks,
            grossRangeTicks: 40L,
            maximumFavorableProgressTicks: favorableProgressTicks,
            maximumAdverseProgressTicks: 5L,
            progressRetainedTicks: favorableProgressTicks,
            progressRetentionRatio: 1.0m,
            timeToMaximumFavorableProgress: null,
            timeToLatestProgress: null,
            timeAtMaximumExcursion: null,
            episodeReferenceDistanceStartTicks: null,
            episodeReferenceDistanceLatestTicks: null,
            maximumDistanceFromReferenceTicks: null,
            currentDistanceFromReferenceTicks: null,
            geometricReentryObserved: null,
            timeMaintainedInside: null,
            outsideTimeRatio: null,
            outsideVolumeRatio: null,
            outsideTradeRatio: null,
            localPocTick: null,
            localPocDisplacementTicks: null,
            developingTpoPocStartTick: null,
            developingTpoPocLatestTick: null,
            tpoPocMigrationTicks: null,
            developingVolumePocStartTick: null,
            developingVolumePocLatestTick: null,
            volumePocMigrationTicks: null,
            developingTpoValueLowStartTick: null,
            developingTpoValueHighStartTick: null,
            developingTpoValueLowLatestTick: null,
            developingTpoValueHighLatestTick: null,
            developingVolumeValueLowStartTick: null,
            developingVolumeValueHighStartTick: null,
            developingVolumeValueLowLatestTick: null,
            developingVolumeValueHighLatestTick: null,
            tpoValueCentroidMigrationTicks: null,
            volumeValueCentroidMigrationTicks: null,
            priceLocationAtStart: null,
            priceLocationLatest: null,
            evidenceAvailability: EfficiencyAvailability.Available,
            limitations: Array.Empty<string>());

        var rels = new AuctionEfficiencyRawRelationships(
            null, null, null, null, null, null, null, null, null, null, null, null);

        var measurementStatus = dataQuality == EfficiencyDataQuality.Complete
            ? EfficiencyModuleState.Ready
            : EfficiencyModuleState.Partial;

        var effSnap = new AuctionEfficiencyEvidenceSnapshot(
            snapshotId: "EFF|" + Auction + "|current",
            policyVersion: AuctionEfficiencyEvidencePolicyConfig.PolicyVersion,
            scopeType: EfficiencyScopeType.CurrentPrimaryAuction,
            primaryAuctionId: Auction,
            episodeId: null,
            referenceId: null,
            referenceRole: null,
            instrumentIdentity: Instrument,
            dataEpoch: Epoch,
            tickSize: Tick,
            timestampPolicy: AtasTimestampNormalizer.PolicyVersion,
            measurementStatus: measurementStatus,
            classificationState: EfficiencyClassificationState.NotCalibrated,
            observationStartedAtUtc: Utc(),
            firstInputAtUtc: Utc(),
            lastInputAtUtc: Utc(30),
            coverageMode: OrderflowCoverageMode.LiveOnlyFromAuctionStart,
            resultDirection: direction,
            effort: effort,
            result: result,
            rawRelationships: rels,
            stateVersion: 1L,
            eventRevision: 1L,
            dataQuality: dataQuality,
            availability: EfficiencyAvailability.Available,
            limitations: Array.Empty<string>(),
            inputFingerprint: "fp1",
            isFrozen: false);

        var setModuleState = dataQuality == EfficiencyDataQuality.Complete
            ? EfficiencyModuleState.AwaitingEpisode
            : EfficiencyModuleState.Partial;

        return new AuctionEfficiencyEvidenceSetSnapshot(
            moduleState: setModuleState,
            policyVersion: AuctionEfficiencyEvidencePolicyConfig.PolicyVersion,
            currentAuctionEvidence: effSnap,
            activeEpisodeEvidence: Array.Empty<AuctionEfficiencyEvidenceSnapshot>(),
            recentlyClosedEpisodeEvidence: Array.Empty<AuctionEfficiencyEvidenceSnapshot>(),
            latestUpdatedEvidence: effSnap,
            readyCount: 0,
            partialCount: dataQuality == EfficiencyDataQuality.Partial ? 1 : 0,
            invalidCount: 0,
            inputFingerprint: null,
            rejectedStaleCount: 0L,
            lastRejectionReason: null,
            createdAtUtc: Utc(),
            lastUpdatedAtUtc: Utc(30),
            limitations: Array.Empty<string>());
    }
}
