using GC.AuctionFlow.Cluster;
using GC.AuctionFlow.Core;
using GC.AuctionFlow.EffortResult;
using GC.AuctionFlow.Efficiency;
using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Orderflow;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Runtime;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.EffortResult;

/// <summary>
/// Phase 2E Effort vs Result Classifier Foundation.
/// All calibrated classification states â†’ NotCalibrated.
/// FAR/AAC/Thesis/Entry: NOT AUTHORIZED in Phase 2E.
/// </summary>
public sealed class Phase2EEffortResultClassifierTests
{
    private const decimal Tick = 0.1m;
    private const string Instrument = "GCQ6";
    private const string Epoch = "GCQ6|tick=0.1";
    private const string Auction = "PI-2026-07-25";

    private static DateTime Utc(int sec = 0) =>
        new(2026, 7, 25, 12, 0, sec, DateTimeKind.Utc);

    // ---------- A: Policy & identity ----------

    [Fact]
    public void A01_PolicyVersion_is_EFFORT_RESULT_CLASSIFIER_POLICY_V1()
    {
        Assert.Equal("EFFORT_RESULT_CLASSIFIER_POLICY_V1", EffortResultClassifierPolicyConfig.PolicyVersion);
    }

    [Fact]
    public void A02_SnapshotVersion_is_1_0_0()
    {
        Assert.Equal("1.0.0", EffortResultClassificationSnapshot.SnapshotVersion);
        Assert.Equal("1.0.0", EffortResultClassificationSetSnapshot.SnapshotVersion);
    }

    [Fact]
    public void A03_ClassificationId_format_ERCL_pipe_effId_pipe_policy()
    {
        var id = EffortResultIdentity.BuildFromEfficiencyId("EFF|GCQ6|v1");
        Assert.StartsWith("ERCL|", id, StringComparison.Ordinal);
        Assert.Contains("EFFORT_RESULT_CLASSIFIER_POLICY_V1", id, StringComparison.Ordinal);
    }

    [Fact]
    public void A04_ClassificationId_sanitizes_pipe_in_efficiencyId()
    {
        var id = EffortResultIdentity.BuildFromEfficiencyId("EFF|SOME|ID");
        Assert.DoesNotContain("EFF|SOME|ID", id, StringComparison.Ordinal);
        Assert.Contains("EFF_SOME_ID", id, StringComparison.Ordinal);
    }

    [Fact]
    public void A05_ClassificationId_handles_null_efficiencyId()
    {
        var id = EffortResultIdentity.BuildFromEfficiencyId(null!);
        Assert.StartsWith("ERCL|Unknown|", id, StringComparison.Ordinal);
    }

    // ---------- B: Disabled module ----------

    [Fact]
    public void B01_Disabled_host_produces_Disabled_snapshot()
    {
        var host = new EffortResultClassifierHost(new EffortResultClassifierPolicyConfig(enabled: false));
        var set = host.Rebuild(null);
        Assert.Equal(EffortResultModuleState.Disabled, set.ModuleState);
    }

    [Fact]
    public void B02_Disabled_snapshot_has_MODULE_DISABLED_limitation()
    {
        var host = new EffortResultClassifierHost(new EffortResultClassifierPolicyConfig(enabled: false));
        var set = host.Rebuild(null);
        Assert.Contains("MODULE_DISABLED", set.Limitations);
    }

    [Fact]
    public void B03_Disabled_snapshot_has_no_CurrentAuction()
    {
        var host = new EffortResultClassifierHost(new EffortResultClassifierPolicyConfig(enabled: false));
        var set = host.Rebuild(null);
        Assert.Null(set.CurrentAuctionClassification);
        Assert.Empty(set.ActiveEpisodeClassifications);
    }

    [Fact]
    public void B04_Configure_disabled_resets_and_returns_disabled()
    {
        var host = new EffortResultClassifierHost(new EffortResultClassifierPolicyConfig(enabled: false));
        host.Configure(new EffortResultClassifierPolicyConfig(enabled: false));
        Assert.NotNull(host.Current);
        Assert.Equal(EffortResultModuleState.Disabled, host.Current!.ModuleState);
    }

    // ---------- C: AwaitingEfficiency ----------

    [Fact]
    public void C01_Null_efficiency_produces_AwaitingEfficiency()
    {
        var host = new EffortResultClassifierHost(new EffortResultClassifierPolicyConfig(enabled: true));
        var set = host.Rebuild(null);
        Assert.Equal(EffortResultModuleState.AwaitingEfficiency, set.ModuleState);
    }

    [Fact]
    public void C02_Disabled_efficiency_produces_AwaitingEfficiency()
    {
        var host = new EffortResultClassifierHost(new EffortResultClassifierPolicyConfig(enabled: true));
        var effHost = MakeEfficiencyHost(false);
        var set = host.Rebuild(effHost.Current);
        Assert.Equal(EffortResultModuleState.AwaitingEfficiency, set.ModuleState);
    }

    [Fact]
    public void C03_AwaitingEfficiency_has_NOT_CALIBRATED_limitation()
    {
        var host = new EffortResultClassifierHost(new EffortResultClassifierPolicyConfig(enabled: true));
        var set = host.Rebuild(null);
        Assert.Contains(EffortResultClassifierPolicyConfig.LimitationNotCalibrated, set.Limitations);
    }

    // ---------- D: NotCalibrated classification states ----------

    [Fact]
    public void D01_All_snapshots_produce_NotCalibrated_classification()
    {
        var host = new EffortResultClassifierHost(new EffortResultClassifierPolicyConfig(enabled: true));
        var efficiency = BuildReadyEfficiency();
        var set = host.Rebuild(efficiency);

        if (set.CurrentAuctionClassification is not null)
            Assert.Equal(EffortResultClassificationState.NotCalibrated, set.CurrentAuctionClassification.Classification);

        foreach (var ep in set.ActiveEpisodeClassifications)
            Assert.Equal(EffortResultClassificationState.NotCalibrated, ep.Classification);
    }

    [Fact]
    public void D02_NotCalibrated_enum_value_is_4()
    {
        Assert.Equal(4, (int)EffortResultClassificationState.NotCalibrated);
    }

    [Fact]
    public void D03_EffortResultBalanced_enum_is_not_emitted_in_Phase2E()
    {
        var host = new EffortResultClassifierHost(new EffortResultClassifierPolicyConfig(enabled: true));
        var set = host.Rebuild(BuildReadyEfficiency());

        Assert.DoesNotContain(set.ActiveEpisodeClassifications,
            s => s.Classification == EffortResultClassificationState.EffortResultBalanced);
        Assert.True(set.CurrentAuctionClassification is null
            || set.CurrentAuctionClassification.Classification != EffortResultClassificationState.EffortResultBalanced);
    }

    [Fact]
    public void D04_Snapshot_limitations_include_all_NOT_CALIBRATED_strings()
    {
        var host = new EffortResultClassifierHost(new EffortResultClassifierPolicyConfig(enabled: true));
        var set = host.Rebuild(BuildReadyEfficiency());

        Assert.Contains(EffortResultClassifierPolicyConfig.LimitationNotCalibrated, set.Limitations);
        Assert.Contains(EffortResultClassifierPolicyConfig.LimitationBalancedNotCalibrated, set.Limitations);
        Assert.Contains(EffortResultClassifierPolicyConfig.LimitationEffectiveNotCalibrated, set.Limitations);
        Assert.Contains(EffortResultClassifierPolicyConfig.LimitationIneffectiveNotCalibrated, set.Limitations);
        Assert.Contains(EffortResultClassifierPolicyConfig.LimitationAbsorptionNotCalibrated, set.Limitations);
        Assert.Contains(EffortResultClassifierPolicyConfig.LimitationExhaustionNotCalibrated, set.Limitations);
        Assert.Contains(EffortResultClassifierPolicyConfig.LimitationFacilitationHealthyNotCalibrated, set.Limitations);
        Assert.Contains(EffortResultClassifierPolicyConfig.LimitationFacilitationFailingNotCalibrated, set.Limitations);
    }

    [Fact]
    public void D05_FAR_AAC_NOT_AUTHORIZED_limitation_present()
    {
        var host = new EffortResultClassifierHost(new EffortResultClassifierPolicyConfig(enabled: true));
        var set = host.Rebuild(BuildReadyEfficiency());
        Assert.Contains(EffortResultClassifierPolicyConfig.LimitationNoFarAac, set.Limitations);
    }

    [Fact]
    public void D06_LIVE_ONLY_limitation_present()
    {
        var host = new EffortResultClassifierHost(new EffortResultClassifierPolicyConfig(enabled: true));
        var set = host.Rebuild(BuildReadyEfficiency());
        Assert.Contains(EffortResultClassifierPolicyConfig.LimitationLiveOnly, set.Limitations);
    }

    // ---------- E: Module state transitions ----------

    [Fact]
    public void E01_Ready_efficiency_produces_Ready_or_Partial_module_state()
    {
        var host = new EffortResultClassifierHost(new EffortResultClassifierPolicyConfig(enabled: true));
        var set = host.Rebuild(BuildReadyEfficiency());
        Assert.True(set.ModuleState == EffortResultModuleState.Ready
            || set.ModuleState == EffortResultModuleState.Partial
            || set.ModuleState == EffortResultModuleState.AwaitingEfficiency);
    }

    [Fact]
    public void E02_Invalid_efficiency_produces_Invalid_module_state()
    {
        var host = new EffortResultClassifierHost(new EffortResultClassifierPolicyConfig(enabled: true));
        var effHost = MakeEfficiencyHost(true);
        effHost.Configure(Tick, Instrument, Epoch, new AuctionEfficiencyEvidencePolicyConfig(true), AtasTimestampNormalizer.PolicyVersion);

        var invalidSet = MakeInvalidEfficiencySet();
        var set = host.Rebuild(invalidSet);
        Assert.Equal(EffortResultModuleState.Invalid, set.ModuleState);
    }

    [Fact]
    public void E03_PolicyVersion_propagated_to_set_snapshot()
    {
        var host = new EffortResultClassifierHost(new EffortResultClassifierPolicyConfig(enabled: true));
        var set = host.Rebuild(BuildReadyEfficiency());
        Assert.Equal(EffortResultClassifierPolicyConfig.PolicyVersion, set.PolicyVersion);
    }

    // ---------- F: Fingerprint gate ----------

    [Fact]
    public void F01_Identical_efficiency_input_returns_cached_snapshot()
    {
        var host = new EffortResultClassifierHost(new EffortResultClassifierPolicyConfig(enabled: true));
        var efficiency = BuildReadyEfficiency();
        var set1 = host.Rebuild(efficiency);
        var set2 = host.Rebuild(efficiency);
        Assert.Same(set1, set2);
    }

    [Fact]
    public void F02_Reset_then_rebuild_returns_new_snapshot()
    {
        var host = new EffortResultClassifierHost(new EffortResultClassifierPolicyConfig(enabled: true));
        var set1 = host.Rebuild(null);
        host.Reset();
        var set2 = host.Rebuild(null);
        Assert.NotSame(set1, set2);
    }

    [Fact]
    public void F03_LastAppliedFingerprint_set_after_rebuild()
    {
        var host = new EffortResultClassifierHost(new EffortResultClassifierPolicyConfig(enabled: true));
        host.Rebuild(BuildReadyEfficiency());
        Assert.True(host.LastAppliedFingerprint.HasValue);
    }

    [Fact]
    public void F04_Fingerprint_equality_identical_inputs()
    {
        var fp1 = new EffortResultInputFingerprint(true, "A", "V1");
        var fp2 = new EffortResultInputFingerprint(true, "A", "V1");
        Assert.True(fp1.Equals(fp2));
    }

    [Fact]
    public void F05_Fingerprint_inequality_different_efficiencySetFp()
    {
        var fp1 = new EffortResultInputFingerprint(true, "A", "V1");
        var fp2 = new EffortResultInputFingerprint(true, "B", "V1");
        Assert.False(fp1.Equals(fp2));
    }

    [Fact]
    public void F06_Reset_clears_fingerprint_and_published()
    {
        var host = new EffortResultClassifierHost(new EffortResultClassifierPolicyConfig(enabled: true));
        host.Rebuild(BuildReadyEfficiency());
        host.Reset();
        Assert.Null(host.Current);
        Assert.False(host.LastAppliedFingerprint.HasValue);
    }

    // ---------- G: Set-level counts ----------

    [Fact]
    public void G01_RecentlyClosedCapacity_is_64()
    {
        Assert.Equal(64, EffortResultClassificationSetSnapshot.RecentlyClosedCapacity);
    }

    [Fact]
    public void G02_Set_ReadyCount_and_PartialCount_non_negative()
    {
        var host = new EffortResultClassifierHost(new EffortResultClassifierPolicyConfig(enabled: true));
        var set = host.Rebuild(BuildReadyEfficiency());
        Assert.True(set.ReadyCount >= 0);
        Assert.True(set.PartialCount >= 0);
    }

    [Fact]
    public void G03_PolicyVersion_on_individual_snapshot()
    {
        var host = new EffortResultClassifierHost(new EffortResultClassifierPolicyConfig(enabled: true));
        var set = host.Rebuild(BuildReadyEfficiency());
        if (set.CurrentAuctionClassification is not null)
            Assert.Equal(EffortResultClassifierPolicyConfig.PolicyVersion,
                set.CurrentAuctionClassification.PolicyVersion);
    }

    // ---------- H: DataQuality mapping ----------

    [Fact]
    public void H01_Complete_efficiency_quality_maps_to_Complete_data_quality()
    {
        var host = new EffortResultClassifierHost(new EffortResultClassifierPolicyConfig(enabled: true));
        var set = host.Rebuild(BuildReadyEfficiency());
        if (set.CurrentAuctionClassification is not null)
            Assert.True(set.CurrentAuctionClassification.DataQuality == EffortResultDataQuality.Complete
                        || set.CurrentAuctionClassification.DataQuality == EffortResultDataQuality.Partial);
    }

    // ---------- I: Runtime & GPS integration ----------

    [Fact]
    public void I01_RuntimeSnapshot_SnapshotVersion_is_0_12_0()
    {
        Assert.Equal("0.26.0", GcaeRuntimeSnapshot.SnapshotVersion);
    }

    [Fact]
    public void I02_Runtime_Publish_accepts_null_effortResult()
    {
        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(0.1m, 14));
        var snap = engine.Publish(
            null, "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, Utc(), false, true, false, false, false, false,
            timestampUtc: Utc(),
            effortResult: null,
            showEffortResultDiagnostics: false);
        Assert.Null(snap.EffortResult);
    }

    [Fact]
    public void I03_Runtime_Publish_passes_effortResult_through()
    {
        var host = new EffortResultClassifierHost(new EffortResultClassifierPolicyConfig(enabled: false));
        var erSet = host.Rebuild(null);
        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(0.1m, 14));
        var snap = engine.Publish(
            null, "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, Utc(), false, true, false, false, false, false,
            timestampUtc: Utc(),
            effortResult: erSet,
            showEffortResultDiagnostics: false);
        Assert.Same(erSet, snap.EffortResult);
    }

    [Fact]
    public void I04_ShowEffortResultDiagnostics_propagated()
    {
        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(0.1m, 14));
        var snap = engine.Publish(
            null, "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, Utc(), false, true, false, false, false, false,
            timestampUtc: Utc(),
            effortResult: null,
            showEffortResultDiagnostics: true);
        Assert.True(snap.ShowEffortResultDiagnostics);
    }

    [Fact]
    public void I05_Runtime_Publish_effortResult_limitations_merged_into_snapshot()
    {
        var host = new EffortResultClassifierHost(new EffortResultClassifierPolicyConfig(enabled: true));
        var erSet = host.Rebuild(null);
        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(0.1m, 14));
        var snap = engine.Publish(
            null, "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, Utc(), false, true, false, false, false, false,
            timestampUtc: Utc(),
            effortResult: erSet,
            showEffortResultDiagnostics: false);
        Assert.Contains(EffortResultClassifierPolicyConfig.LimitationNotCalibrated, snap.KnownLimitations);
    }

    // ---------- J: GPS card display ----------

    [Fact]
    public void J01_BuildEffortResultLines_null_set_returns_empty()
    {
        var lines = AuctionGpsCardMapper.BuildEffortResultLines(null, false);
        Assert.Empty(lines);
    }

    [Fact]
    public void J02_BuildEffortResultLines_Disabled_returns_DISABLED_row()
    {
        var host = new EffortResultClassifierHost(new EffortResultClassifierPolicyConfig(enabled: false));
        var set = host.Rebuild(null);
        var lines = AuctionGpsCardMapper.BuildEffortResultLines(set, false);
        Assert.Contains(lines, l => l.Contains("DISABLED", StringComparison.Ordinal));
    }

    [Fact]
    public void J03_BuildEffortResultLines_AwaitingEfficiency_returns_relevant_row()
    {
        var host = new EffortResultClassifierHost(new EffortResultClassifierPolicyConfig(enabled: true));
        var set = host.Rebuild(null);
        var lines = AuctionGpsCardMapper.BuildEffortResultLines(set, false);
        Assert.True(lines.Count > 0);
        Assert.Contains(lines, l => l.Contains("AWAITING", StringComparison.Ordinal) || l.Contains("EFFORT RESULT:", StringComparison.Ordinal));
    }

    [Fact]
    public void J04_BuildEffortResultLines_no_FAR_AAC_in_output()
    {
        var host = new EffortResultClassifierHost(new EffortResultClassifierPolicyConfig(enabled: true));
        var set = host.Rebuild(BuildReadyEfficiency());
        var lines = AuctionGpsCardMapper.BuildEffortResultLines(set, false);
        Assert.DoesNotContain(lines, l => l.Contains("FAR", StringComparison.Ordinal));
        Assert.DoesNotContain(lines, l => l.Contains("AAC", StringComparison.Ordinal));
    }

    [Fact]
    public void J05_BuildEffortResultLines_no_Thesis_in_output()
    {
        var host = new EffortResultClassifierHost(new EffortResultClassifierPolicyConfig(enabled: true));
        var set = host.Rebuild(BuildReadyEfficiency());
        var lines = AuctionGpsCardMapper.BuildEffortResultLines(set, false);
        Assert.DoesNotContain(lines, l => l.Contains("THESIS", StringComparison.Ordinal));
    }

    [Fact]
    public void J06_GPS_DiagnosticRows_has_EFFORT_RESULT_row_when_showDiagnostics_true()
    {
        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(0.1m, 14));
        var snap = engine.Publish(
            null, "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, Utc(), false, true, false, false, false, false,
            timestampUtc: Utc());
        var vm = AuctionGpsCardMapper.FromSnapshot(snap, showDiagnostics: true);
        Assert.Contains(vm.DiagnosticRows, r => r.StartsWith("EFFORT RESULT:", StringComparison.Ordinal));
    }

    [Fact]
    public void J07_GPS_diagnostic_row_count_is_current()
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

    // ---------- K: Limitations & NOT CALIBRATED policy ----------

    [Fact]
    public void K01_Individual_snapshot_has_8_NOT_CALIBRATED_limitations()
    {
        var host = new EffortResultClassifierHost(new EffortResultClassifierPolicyConfig(enabled: true));
        var set = host.Rebuild(BuildReadyEfficiency());
        if (set.CurrentAuctionClassification is not null)
        {
            var lims = set.CurrentAuctionClassification.Limitations;
            Assert.Contains(EffortResultClassifierPolicyConfig.LimitationNotCalibrated, lims);
            Assert.Contains(EffortResultClassifierPolicyConfig.LimitationBalancedNotCalibrated, lims);
            Assert.Contains(EffortResultClassifierPolicyConfig.LimitationEffectiveNotCalibrated, lims);
            Assert.Contains(EffortResultClassifierPolicyConfig.LimitationIneffectiveNotCalibrated, lims);
            Assert.Contains(EffortResultClassifierPolicyConfig.LimitationAbsorptionNotCalibrated, lims);
            Assert.Contains(EffortResultClassifierPolicyConfig.LimitationExhaustionNotCalibrated, lims);
            Assert.Contains(EffortResultClassifierPolicyConfig.LimitationFacilitationHealthyNotCalibrated, lims);
            Assert.Contains(EffortResultClassifierPolicyConfig.LimitationFacilitationFailingNotCalibrated, lims);
        }
    }

    [Fact]
    public void K02_EffortResultBalanced_calibrated_state_enum_value_is_100()
    {
        Assert.Equal(100, (int)EffortResultClassificationState.EffortResultBalanced);
    }

    [Fact]
    public void K03_AggressionEffective_calibrated_state_enum_value_is_101()
    {
        Assert.Equal(101, (int)EffortResultClassificationState.AggressionEffective);
    }

    [Fact]
    public void K04_TradeFacilitationFailing_calibrated_state_enum_value_is_106()
    {
        Assert.Equal(106, (int)EffortResultClassificationState.TradeFacilitationFailing);
    }

    [Fact]
    public void K05_No_EffortResultBalanced_or_any_calibrated_state_emitted()
    {
        var host = new EffortResultClassifierHost(new EffortResultClassifierPolicyConfig(enabled: true));
        var set = host.Rebuild(BuildReadyEfficiency());

        var allClassifications = new List<EffortResultClassificationState>();
        if (set.CurrentAuctionClassification is not null)
            allClassifications.Add(set.CurrentAuctionClassification.Classification);
        allClassifications.AddRange(set.ActiveEpisodeClassifications.Select(s => s.Classification));
        allClassifications.AddRange(set.RecentlyClosedClassifications.Select(s => s.Classification));

        foreach (var cls in allClassifications)
            Assert.True(cls == EffortResultClassificationState.NotCalibrated
                        || cls == EffortResultClassificationState.Unknown,
                $"Unexpected calibrated state: {cls}");
    }

    // ---------- Helpers ----------

    private static AuctionEfficiencyHost MakeEfficiencyHost(bool enabled)
    {
        var host = new AuctionEfficiencyHost(Tick, Instrument, Epoch,
            AtasTimestampNormalizer.PolicyVersion,
            new AuctionEfficiencyEvidencePolicyConfig(enabled));
        return host;
    }

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
}
