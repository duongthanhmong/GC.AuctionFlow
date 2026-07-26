using GC.AuctionFlow.Core;
using GC.AuctionFlow.Maturity;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Runtime;
using GC.AuctionFlow.Thesis;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Maturity;

/// <summary>
/// Phase 3B Signal Maturity (v1.2 §29, v1.3 §9).
/// Lifecycle capped at Candidate; Fast/Standard/Confirmed NOT CALIBRATED.
/// Expected-behaviour contract declared, deadline NOT CALIBRATED.
/// </summary>
public sealed class Phase3BSignalMaturityTests
{
    private static DateTime Utc(int sec = 0) =>
        new(2026, 7, 27, 12, 0, sec, DateTimeKind.Utc);

    // ---------- helpers ----------

    private static FarThesisSnapshot Far(
        FarState state,
        string id = "FAR-1",
        ThesisDirection dir = ThesisDirection.Long,
        ThesisDataQuality q = ThesisDataQuality.Complete,
        bool notCalibrated = false,
        long stateVersion = 1L,
        long eventRevision = 1L) =>
        new FarThesisSnapshot(
            id, FarThesisPolicyConfig.PolicyVersion,
            evidenceId: "EV-1", episodeId: "EP-1",
            primaryAuctionId: "PI-1", referenceId: "REF-1",
            direction: dir, farState: state, notCalibrated: notCalibrated,
            attemptCount: 1, dataQuality: q,
            stateVersion: stateVersion, eventRevision: eventRevision,
            observedAtUtc: Utc(), limitations: Array.Empty<string>());

    private static AacThesisSnapshot Aac(
        AacState state,
        string id = "AAC-1",
        ThesisDirection dir = ThesisDirection.Short,
        ThesisDataQuality q = ThesisDataQuality.Complete,
        bool notCalibrated = false,
        long stateVersion = 1L,
        long eventRevision = 1L) =>
        new AacThesisSnapshot(
            id, AacThesisPolicyConfig.PolicyVersion,
            evidenceId: "EV-2", episodeId: "EP-2",
            primaryAuctionId: "PI-1", referenceId: "REF-2",
            direction: dir, aacState: state, notCalibrated: notCalibrated,
            attemptCount: 1, dataQuality: q,
            stateVersion: stateVersion, eventRevision: eventRevision,
            observedAtUtc: Utc(), limitations: Array.Empty<string>());

    private static FarThesisSetSnapshot FarSet(
        ThesisModuleState state = ThesisModuleState.Ready,
        params FarThesisSnapshot[] theses) =>
        new FarThesisSetSnapshot(
            state, FarThesisPolicyConfig.PolicyVersion,
            theses, Array.Empty<FarThesisSnapshot>(),
            theses.Length > 0 ? theses[^1] : null,
            0, 0, Utc(), Utc(1), Array.Empty<string>());

    private static AacThesisSetSnapshot AacSet(
        ThesisModuleState state = ThesisModuleState.Ready,
        params AacThesisSnapshot[] theses) =>
        new AacThesisSetSnapshot(
            state, AacThesisPolicyConfig.PolicyVersion,
            theses, Array.Empty<AacThesisSnapshot>(),
            theses.Length > 0 ? theses[^1] : null,
            0, 0, Utc(), Utc(1), Array.Empty<string>());

    private static SignalMaturityHost EnabledHost() =>
        new SignalMaturityHost(new SignalMaturityPolicyConfig(enabled: true));

    // ========== A: Policy ==========

    [Fact]
    public void A01_PolicyVersion_is_signal_maturity_v1() =>
        Assert.Equal("SIGNAL_MATURITY_POLICY_V1", SignalMaturityPolicyConfig.PolicyVersion);

    [Fact]
    public void A02_Module_default_is_disabled() =>
        Assert.False(new SignalMaturityPolicyConfig().Enabled);

    [Fact]
    public void A03_Limitation_constants_are_stable()
    {
        Assert.Equal("SIGNAL_MATURITY_THRESHOLDS_NOT_CALIBRATED", SignalMaturityPolicyConfig.LimitationNotCalibrated);
        Assert.Equal("FAST_MATURITY_SHADOW_ONLY", SignalMaturityPolicyConfig.LimitationFastShadowOnly);
        Assert.Equal("EXPECTED_BEHAVIOR_DEADLINE_NOT_CALIBRATED", SignalMaturityPolicyConfig.LimitationDeadlineNotCalibrated);
        Assert.Equal("RETEST_DISCRIMINATION_NOT_CALIBRATED", SignalMaturityPolicyConfig.LimitationRetestNotCalibrated);
        Assert.Equal("LIVE_ONLY_HISTORY", SignalMaturityPolicyConfig.LimitationLiveOnly);
    }

    [Fact]
    public void A04_Fast_is_shadow_only_by_default()
    {
        Assert.True(SignalMaturityPolicyConfig.FastShadowOnlyDefault);
        Assert.True(new SignalMaturityPolicyConfig(enabled: true).FastShadowOnly);
    }

    // ========== B: Enum calibration gates ==========

    [Fact]
    public void B01_MaturityLevel_calibrated_values_are_reserved_above_99()
    {
        Assert.Equal(1, (int)SignalMaturityLevel.NotCalibrated);
        Assert.True((int)SignalMaturityLevel.Fast >= 100);
        Assert.True((int)SignalMaturityLevel.Standard >= 100);
        Assert.True((int)SignalMaturityLevel.Confirmed >= 100);
    }

    [Fact]
    public void B02_Lifecycle_observable_states_are_below_100()
    {
        Assert.True((int)AnalysisLifecycleState.Observation < 100);
        Assert.True((int)AnalysisLifecycleState.Approaching < 100);
        Assert.True((int)AnalysisLifecycleState.EpisodeActive < 100);
        Assert.True((int)AnalysisLifecycleState.Candidate < 100);
        Assert.True((int)AnalysisLifecycleState.NotCalibrated < 100);
    }

    [Fact]
    public void B03_Lifecycle_calibrated_states_are_reserved()
    {
        Assert.Equal(100, (int)AnalysisLifecycleState.Armed);
        Assert.Equal(101, (int)AnalysisLifecycleState.Executable);
        Assert.Equal(102, (int)AnalysisLifecycleState.Managing);
    }

    [Fact]
    public void B04_Retest_discrimination_states_are_reserved()
    {
        Assert.Equal(3, (int)RetestObservationState.NotCalibrated);
        Assert.True((int)RetestObservationState.MicroRetest >= 100);
        Assert.True((int)RetestObservationState.StructuralRetest >= 100);
        Assert.True((int)RetestObservationState.SecondAttempt >= 100);
    }

    [Fact]
    public void B05_ExpectedBehavior_covers_all_six_kdk_scenarios()
    {
        // v1.3 §9.3 — six expected-behaviour rows.
        Assert.Equal(7, Enum.GetValues<ExpectedBehaviorContractKind>().Length); // 6 + Unknown
    }

    // ========== C: Disabled / reset ==========

    [Fact]
    public void C01_Disabled_host_publishes_disabled_snapshot()
    {
        var host = new SignalMaturityHost(new SignalMaturityPolicyConfig(enabled: false));
        var set = host.Rebuild(FarSet(), AacSet(), Utc());
        Assert.Equal(MaturityModuleState.Disabled, set.ModuleState);
        Assert.Empty(set.ActiveCandidates);
        Assert.Contains("MODULE_DISABLED", set.Limitations);
    }

    [Fact]
    public void C02_Configure_disabled_clears_published_to_disabled()
    {
        var host = EnabledHost();
        host.Rebuild(FarSet(ThesisModuleState.Ready, Far(FarState.ReentryDeveloping)), null, Utc());
        host.Configure(new SignalMaturityPolicyConfig(enabled: false));
        Assert.Equal(MaturityModuleState.Disabled, host.Current!.ModuleState);
    }

    [Fact]
    public void C03_Reset_clears_current()
    {
        var host = EnabledHost();
        host.Rebuild(FarSet(ThesisModuleState.Ready, Far(FarState.ReentryDeveloping)), null, Utc());
        host.Reset();
        Assert.Null(host.Current);
    }

    [Fact]
    public void C04_Configure_null_throws() =>
        Assert.Throws<ArgumentNullException>(() => EnabledHost().Configure(null!));

    // ========== D: Awaiting / invalid ==========

    [Fact]
    public void D01_Null_inputs_yield_awaiting_thesis()
    {
        var set = EnabledHost().Rebuild(null, null, Utc());
        Assert.Equal(MaturityModuleState.AwaitingThesis, set.ModuleState);
        Assert.Contains(SignalMaturityPolicyConfig.LimitationNotCalibrated, set.Limitations);
        Assert.Contains(SignalMaturityPolicyConfig.LimitationLiveOnly, set.Limitations);
    }

    [Fact]
    public void D02_Disabled_thesis_inputs_yield_awaiting_thesis()
    {
        var set = EnabledHost().Rebuild(
            FarSet(ThesisModuleState.Disabled), AacSet(ThesisModuleState.Disabled), Utc());
        Assert.Equal(MaturityModuleState.AwaitingThesis, set.ModuleState);
    }

    [Fact]
    public void D03_Invalid_thesis_input_yields_invalid()
    {
        var set = EnabledHost().Rebuild(FarSet(ThesisModuleState.Invalid), null, Utc());
        Assert.Equal(MaturityModuleState.Invalid, set.ModuleState);
        Assert.Contains("THESIS_INPUT_INVALID", set.Limitations);
    }

    [Fact]
    public void D04_Empty_ready_thesis_set_yields_awaiting_thesis()
    {
        var set = EnabledHost().Rebuild(FarSet(ThesisModuleState.Ready), null, Utc());
        Assert.Equal(MaturityModuleState.AwaitingThesis, set.ModuleState);
    }

    // ========== E: FAR lifecycle mapping ==========

    [Theory]
    [InlineData(FarState.Idle, AnalysisLifecycleState.Observation)]
    [InlineData(FarState.Approaching, AnalysisLifecycleState.Approaching)]
    [InlineData(FarState.EpisodeActive, AnalysisLifecycleState.EpisodeActive)]
    [InlineData(FarState.OutsideAttempt, AnalysisLifecycleState.EpisodeActive)]
    [InlineData(FarState.ReentryDeveloping, AnalysisLifecycleState.Candidate)]
    [InlineData(FarState.ReacceptedInside, AnalysisLifecycleState.Candidate)]
    [InlineData(FarState.Invalidated, AnalysisLifecycleState.Invalidated)]
    [InlineData(FarState.Expired, AnalysisLifecycleState.Expired)]
    [InlineData(FarState.Completed, AnalysisLifecycleState.Completed)]
    public void E01_Far_state_maps_to_lifecycle(FarState far, AnalysisLifecycleState expected)
    {
        var set = EnabledHost().Rebuild(FarSet(ThesisModuleState.Ready, Far(far)), null, Utc());
        Assert.Equal(expected, set.ActiveCandidates[0].LifecycleState);
    }

    [Fact]
    public void E02_Far_reaccepted_inside_is_capped_at_candidate_and_flagged()
    {
        var set = EnabledHost().Rebuild(
            FarSet(ThesisModuleState.Ready, Far(FarState.ReacceptedInside)), null, Utc());
        var sm = set.ActiveCandidates[0];
        // Would be Armed once maturity is calibrated — must stay Candidate.
        Assert.Equal(AnalysisLifecycleState.Candidate, sm.LifecycleState);
        Assert.Contains(MaturityBlockingReason.ThesisStateNotCalibrated, sm.BlockingReasons);
    }

    [Fact]
    public void E03_Far_calibrated_state_falls_back_to_not_calibrated()
    {
        var set = EnabledHost().Rebuild(
            FarSet(ThesisModuleState.Ready, Far(FarState.Armed)), null, Utc());
        Assert.Equal(AnalysisLifecycleState.NotCalibrated, set.ActiveCandidates[0].LifecycleState);
    }

    [Fact]
    public void E04_Far_family_and_direction_are_carried()
    {
        var set = EnabledHost().Rebuild(
            FarSet(ThesisModuleState.Ready, Far(FarState.ReentryDeveloping, dir: ThesisDirection.Long)), null, Utc());
        var sm = set.ActiveCandidates[0];
        Assert.Equal("FAR", sm.ThesisFamily);
        Assert.Equal(ThesisDirection.Long, sm.Direction);
        Assert.Equal("SM:FAR-1", sm.SnapshotId);
    }

    // ========== F: AAC lifecycle mapping ==========

    [Theory]
    [InlineData(AacState.Idle, AnalysisLifecycleState.Observation)]
    [InlineData(AacState.Approaching, AnalysisLifecycleState.Approaching)]
    [InlineData(AacState.EpisodeActive, AnalysisLifecycleState.EpisodeActive)]
    [InlineData(AacState.OutsideAttempt, AnalysisLifecycleState.EpisodeActive)]
    [InlineData(AacState.AcceptanceDeveloping, AnalysisLifecycleState.Candidate)]
    [InlineData(AacState.AcceptedOutside, AnalysisLifecycleState.Candidate)]
    [InlineData(AacState.Pullback, AnalysisLifecycleState.Candidate)]
    [InlineData(AacState.ReacceptedOldValue, AnalysisLifecycleState.Invalidated)]
    [InlineData(AacState.Invalidated, AnalysisLifecycleState.Invalidated)]
    [InlineData(AacState.Expired, AnalysisLifecycleState.Expired)]
    [InlineData(AacState.Completed, AnalysisLifecycleState.Completed)]
    public void F01_Aac_state_maps_to_lifecycle(AacState aac, AnalysisLifecycleState expected)
    {
        var set = EnabledHost().Rebuild(null, AacSet(ThesisModuleState.Ready, Aac(aac)), Utc());
        Assert.Equal(expected, set.ActiveCandidates[0].LifecycleState);
    }

    [Fact]
    public void F02_Aac_calibrated_state_falls_back_to_not_calibrated()
    {
        var set = EnabledHost().Rebuild(null, AacSet(ThesisModuleState.Ready, Aac(AacState.Executable)), Utc());
        Assert.Equal(AnalysisLifecycleState.NotCalibrated, set.ActiveCandidates[0].LifecycleState);
    }

    [Fact]
    public void F03_Aac_family_is_carried()
    {
        var set = EnabledHost().Rebuild(null, AacSet(ThesisModuleState.Ready, Aac(AacState.AcceptedOutside)), Utc());
        Assert.Equal("AAC", set.ActiveCandidates[0].ThesisFamily);
    }

    [Fact]
    public void F04_Far_and_Aac_both_present_produce_two_scopes()
    {
        var set = EnabledHost().Rebuild(
            FarSet(ThesisModuleState.Ready, Far(FarState.ReentryDeveloping)),
            AacSet(ThesisModuleState.Ready, Aac(AacState.AcceptanceDeveloping)),
            Utc());
        Assert.Equal(2, set.ActiveCandidates.Count);
        Assert.Contains(set.ActiveCandidates, s => s.ThesisFamily == "FAR");
        Assert.Contains(set.ActiveCandidates, s => s.ThesisFamily == "AAC");
    }

    // ========== G: Expected behaviour contract (v1.3 §9.3) ==========

    [Theory]
    [InlineData(FarState.ReentryDeveloping, ExpectedBehaviorContractKind.FarReentry)]
    [InlineData(FarState.ReacceptedInside, ExpectedBehaviorContractKind.FarRetest)]
    [InlineData(FarState.EpisodeActive, ExpectedBehaviorContractKind.Unknown)]
    public void G01_Far_expected_behavior(FarState far, ExpectedBehaviorContractKind expected)
    {
        var set = EnabledHost().Rebuild(FarSet(ThesisModuleState.Ready, Far(far)), null, Utc());
        Assert.Equal(expected, set.ActiveCandidates[0].ExpectedBehavior);
    }

    [Theory]
    [InlineData(AacState.AcceptanceDeveloping, ExpectedBehaviorContractKind.AacEarly)]
    [InlineData(AacState.AcceptedOutside, ExpectedBehaviorContractKind.NewValueContinuation)]
    [InlineData(AacState.Pullback, ExpectedBehaviorContractKind.AacRetest)]
    [InlineData(AacState.EpisodeActive, ExpectedBehaviorContractKind.Unknown)]
    public void G02_Aac_expected_behavior(AacState aac, ExpectedBehaviorContractKind expected)
    {
        var set = EnabledHost().Rebuild(null, AacSet(ThesisModuleState.Ready, Aac(aac)), Utc());
        Assert.Equal(expected, set.ActiveCandidates[0].ExpectedBehavior);
    }

    [Fact]
    public void G03_Expected_behavior_deadline_is_never_fabricated()
    {
        var set = EnabledHost().Rebuild(
            FarSet(ThesisModuleState.Ready, Far(FarState.ReentryDeveloping)), null, Utc());
        var sm = set.ActiveCandidates[0];
        Assert.Null(sm.ExpectedBehaviorDeadlineUtc);
        Assert.Contains(SignalMaturityPolicyConfig.LimitationDeadlineNotCalibrated, sm.Limitations);
        Assert.Contains(MaturityBlockingReason.ExpectedBehaviorDeadlineNotCalibrated, sm.BlockingReasons);
    }

    // ========== H: NOT_CALIBRATED invariants ==========

    [Fact]
    public void H01_MaturityLevel_is_always_not_calibrated()
    {
        foreach (var far in Enum.GetValues<FarState>())
        {
            var set = EnabledHost().Rebuild(FarSet(ThesisModuleState.Ready, Far(far)), null, Utc());
            Assert.Equal(SignalMaturityLevel.NotCalibrated, set.ActiveCandidates[0].MaturityLevel);
        }
    }

    [Fact]
    public void H02_Fast_standard_confirmed_counts_are_always_zero()
    {
        var set = EnabledHost().Rebuild(
            FarSet(ThesisModuleState.Ready, Far(FarState.ReacceptedInside)),
            AacSet(ThesisModuleState.Ready, Aac(AacState.AcceptedOutside)),
            Utc());
        Assert.Equal(0, set.FastCount);
        Assert.Equal(0, set.StandardCount);
        Assert.Equal(0, set.ConfirmedCount);
    }

    [Fact]
    public void H03_Fast_shadow_only_flag_is_always_true()
    {
        var set = EnabledHost().Rebuild(
            FarSet(ThesisModuleState.Ready, Far(FarState.ReentryDeveloping)), null, Utc());
        Assert.True(set.FastShadowOnly);
        Assert.Contains(SignalMaturityPolicyConfig.LimitationFastShadowOnly, set.Limitations);
    }

    [Fact]
    public void H04_Retest_discrimination_is_not_calibrated()
    {
        var set = EnabledHost().Rebuild(
            FarSet(ThesisModuleState.Ready, Far(FarState.ReacceptedInside)), null, Utc());
        Assert.Equal(RetestObservationState.NotCalibrated, set.ActiveCandidates[0].RetestObservation);
        Assert.Contains(SignalMaturityPolicyConfig.LimitationRetestNotCalibrated, set.ActiveCandidates[0].Limitations);
    }

    [Fact]
    public void H05_MicroConfirmation_is_never_asserted()
    {
        var set = EnabledHost().Rebuild(
            FarSet(ThesisModuleState.Ready, Far(FarState.ReentryDeveloping)), null, Utc());
        Assert.False(set.ActiveCandidates[0].MicroConfirmationObserved);
    }

    [Fact]
    public void H06_Every_candidate_is_flagged_not_calibrated()
    {
        var set = EnabledHost().Rebuild(
            FarSet(ThesisModuleState.Ready, Far(FarState.ReentryDeveloping)),
            AacSet(ThesisModuleState.Ready, Aac(AacState.AcceptanceDeveloping)),
            Utc());
        Assert.All(set.ActiveCandidates, s => Assert.True(s.NotCalibrated));
    }

    [Fact]
    public void H07_BlockingReasons_are_never_empty()
    {
        var set = EnabledHost().Rebuild(
            FarSet(ThesisModuleState.Ready, Far(FarState.ReentryDeveloping)), null, Utc());
        Assert.NotEmpty(set.ActiveCandidates[0].BlockingReasons);
        Assert.Contains(MaturityBlockingReason.ThresholdsNotCalibrated, set.ActiveCandidates[0].BlockingReasons);
    }

    [Fact]
    public void H08_Set_limitations_bar_entry_plan_and_risk_sizing()
    {
        var set = EnabledHost().Rebuild(
            FarSet(ThesisModuleState.Ready, Far(FarState.ReentryDeveloping)), null, Utc());
        Assert.Contains(SignalMaturityPolicyConfig.LimitationNoEntryPlan, set.Limitations);
        Assert.Contains(SignalMaturityPolicyConfig.LimitationNoRiskSizing, set.Limitations);
    }

    // ========== I: Module state + fingerprint gating ==========

    [Fact]
    public void I01_Ready_when_complete_quality()
    {
        var set = EnabledHost().Rebuild(
            FarSet(ThesisModuleState.Ready, Far(FarState.ReentryDeveloping)), null, Utc());
        Assert.Equal(MaturityModuleState.Ready, set.ModuleState);
    }

    [Fact]
    public void I02_Partial_when_thesis_quality_partial()
    {
        var set = EnabledHost().Rebuild(
            FarSet(ThesisModuleState.Ready, Far(FarState.ReentryDeveloping, q: ThesisDataQuality.Partial)),
            null, Utc());
        Assert.Equal(MaturityModuleState.Partial, set.ModuleState);
    }

    [Fact]
    public void I03_Partial_when_thesis_module_partial()
    {
        var set = EnabledHost().Rebuild(
            FarSet(ThesisModuleState.Partial, Far(FarState.ReentryDeveloping)), null, Utc());
        Assert.Equal(MaturityModuleState.Partial, set.ModuleState);
    }

    [Fact]
    public void I04_Unchanged_input_returns_cached_instance()
    {
        var host = EnabledHost();
        var far = FarSet(ThesisModuleState.Ready, Far(FarState.ReentryDeveloping));
        var a = host.Rebuild(far, null, Utc());
        var b = host.Rebuild(far, null, Utc(5));
        Assert.Same(a, b);
    }

    [Fact]
    public void I05_Changed_input_rebuilds()
    {
        var host = EnabledHost();
        var a = host.Rebuild(FarSet(ThesisModuleState.Ready, Far(FarState.EpisodeActive)), null, Utc());
        var changed = new FarThesisSetSnapshot(
            ThesisModuleState.Ready, FarThesisPolicyConfig.PolicyVersion,
            new[] { Far(FarState.ReentryDeveloping) }, Array.Empty<FarThesisSnapshot>(),
            null, 0, 0, Utc(), Utc(9), Array.Empty<string>());
        var b = host.Rebuild(changed, null, Utc(9));
        Assert.NotSame(a, b);
        Assert.Equal(AnalysisLifecycleState.Candidate, b.ActiveCandidates[0].LifecycleState);
    }

    [Fact]
    public void I06_CandidateCount_counts_only_candidate_lifecycle()
    {
        var set = EnabledHost().Rebuild(
            FarSet(ThesisModuleState.Ready,
                Far(FarState.ReentryDeveloping, id: "FAR-1"),
                Far(FarState.EpisodeActive, id: "FAR-2")),
            null, Utc());
        Assert.Equal(2, set.ActiveCandidates.Count);
        Assert.Equal(1, set.CandidateCount);
    }

    [Fact]
    public void I07_LatestUpdated_tracks_highest_event_revision()
    {
        var set = EnabledHost().Rebuild(
            FarSet(ThesisModuleState.Ready,
                Far(FarState.EpisodeActive, id: "FAR-1", eventRevision: 1L),
                Far(FarState.ReentryDeveloping, id: "FAR-2", eventRevision: 7L)),
            null, Utc());
        Assert.Equal("SM:FAR-2", set.LatestUpdated!.SnapshotId);
    }

    // ========== J: Runtime wiring ==========

    private static GcaeRuntimeSnapshot PublishWith(SignalMaturitySetSnapshot? maturity, bool diag = false)
    {
        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(0.1m, 14));
        return engine.Publish(
            new ObservedInstrumentSnapshot("GCU6", "GCU6-ID", "GCU6", "COMEX",
                new DateTime(2026, 8, 27), 0.1m, "GC", "GCU6", "COMEX", 0.1m, null),
            "GCU7",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            tradeObserved: false, lastTradeCallbackUtc: null,
            rawRecorderMasterEnabled: false, tradeRecordingEnabled: true,
            recorderAccepting: false, recorderFaulted: false, recorderSessionPresent: false,
            indicatorDisposed: false, timestampUtc: Utc(),
            signalMaturity: maturity,
            showSignalMaturityDiagnostics: diag);
    }

    [Fact]
    public void J01_SnapshotVersion_is_0_18_0() =>
        Assert.Equal("0.18.0", GcaeRuntimeSnapshot.SnapshotVersion);

    [Fact]
    public void J02_Maturity_defaults_to_null_on_snapshot() =>
        Assert.Null(PublishWith(null).SignalMaturity);

    [Fact]
    public void J03_Maturity_set_is_carried_on_snapshot()
    {
        var set = EnabledHost().Rebuild(
            FarSet(ThesisModuleState.Ready, Far(FarState.ReentryDeveloping)), null, Utc());
        var snap = PublishWith(set);
        Assert.NotNull(snap.SignalMaturity);
        Assert.Equal(MaturityModuleState.Ready, snap.SignalMaturity!.ModuleState);
    }

    [Fact]
    public void J04_Maturity_limitations_merge_into_known_limitations()
    {
        var set = EnabledHost().Rebuild(
            FarSet(ThesisModuleState.Ready, Far(FarState.ReentryDeveloping)), null, Utc());
        var snap = PublishWith(set);
        Assert.Contains(SignalMaturityPolicyConfig.LimitationNotCalibrated, snap.KnownLimitations);
        Assert.Contains(SignalMaturityPolicyConfig.LimitationFastShadowOnly, snap.KnownLimitations);
    }

    // ========== K: GPS card ==========

    [Fact]
    public void K01_GpsCard_has_14_diagnostic_rows()
    {
        var vm = AuctionGpsCardMapper.FromSnapshot(PublishWith(null), showDiagnostics: true);
        Assert.Equal(14, vm.DiagnosticRows.Count);
    }

    [Fact]
    public void K02_GpsCard_shows_maturity_not_available_when_null()
    {
        var vm = AuctionGpsCardMapper.FromSnapshot(PublishWith(null), showDiagnostics: true);
        Assert.Contains("MATURITY: NOT AVAILABLE", vm.DiagnosticRows);
    }

    [Fact]
    public void K03_GpsCard_shows_maturity_module_state()
    {
        var set = EnabledHost().Rebuild(
            FarSet(ThesisModuleState.Ready, Far(FarState.ReentryDeveloping)), null, Utc());
        var vm = AuctionGpsCardMapper.FromSnapshot(PublishWith(set), showDiagnostics: true);
        Assert.Contains("MATURITY: READY", vm.DiagnosticRows);
    }

    [Fact]
    public void K04_MaturityLines_null_set_is_empty() =>
        Assert.Empty(AuctionGpsCardMapper.BuildSignalMaturityLines(null, false));

    [Fact]
    public void K05_MaturityLines_disabled_is_single_row()
    {
        var host = new SignalMaturityHost(new SignalMaturityPolicyConfig(enabled: false));
        var set = host.Rebuild(null, null, Utc());
        var rows = AuctionGpsCardMapper.BuildSignalMaturityLines(set, false);
        Assert.Single(rows);
        Assert.Equal("MATURITY: DISABLED", rows[0]);
    }

    [Fact]
    public void K06_MaturityLines_awaiting_declares_not_calibrated()
    {
        var set = EnabledHost().Rebuild(null, null, Utc());
        var rows = AuctionGpsCardMapper.BuildSignalMaturityLines(set, false);
        Assert.Contains("MATURITY: AWAITING THESIS", rows);
        Assert.Contains("MATURITY LEVEL: NOT CALIBRATED", rows);
    }

    [Fact]
    public void K07_MaturityLines_ready_declares_not_calibrated_and_shadow_only()
    {
        var set = EnabledHost().Rebuild(
            FarSet(ThesisModuleState.Ready, Far(FarState.ReentryDeveloping)), null, Utc());
        var rows = AuctionGpsCardMapper.BuildSignalMaturityLines(set, false);
        Assert.Contains("MATURITY LEVEL: NOT CALIBRATED", rows);
        Assert.Contains("FAST MODE: SHADOW ONLY", rows);
        Assert.Contains("EXPECTED BEHAVIOR DEADLINE: NOT CALIBRATED", rows);
    }

    [Fact]
    public void K08_MaturityLines_never_emit_calibrated_level_wording()
    {
        var set = EnabledHost().Rebuild(
            FarSet(ThesisModuleState.Ready, Far(FarState.ReacceptedInside)),
            AacSet(ThesisModuleState.Ready, Aac(AacState.AcceptedOutside)),
            Utc());
        var text = string.Join(" | ", AuctionGpsCardMapper.BuildSignalMaturityLines(set, true));
        Assert.DoesNotContain("MATURITY LEVEL: FAST", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MATURITY LEVEL: STANDARD", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MATURITY LEVEL: CONFIRMED", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ARMED", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EXECUTABLE", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void K09_MaturityLines_diagnostics_add_rows()
    {
        var set = EnabledHost().Rebuild(
            FarSet(ThesisModuleState.Ready, Far(FarState.ReentryDeveloping)), null, Utc());
        var plain = AuctionGpsCardMapper.BuildSignalMaturityLines(set, false);
        var diag = AuctionGpsCardMapper.BuildSignalMaturityLines(set, true);
        Assert.True(diag.Count > plain.Count);
        Assert.Contains(diag, r => r.StartsWith("MATURITY ID:", StringComparison.Ordinal));
    }

    // ========== L: scope guards (v1.3) ==========

    [Fact]
    public void L01_No_entry_price_or_stop_fields_exist()
    {
        var props = typeof(SignalMaturitySnapshot).GetProperties().Select(p => p.Name).ToArray();
        Assert.DoesNotContain(props, n => n.Contains("Entry", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(props, n => n.Contains("Stop", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(props, n => n.Contains("Target", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(props, n => n.Contains("Size", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void L02_No_gex_surface_in_maturity_module()
    {
        var names = typeof(SignalMaturitySnapshot).GetProperties().Select(p => p.Name)
            .Concat(typeof(SignalMaturitySetSnapshot).GetProperties().Select(p => p.Name));
        Assert.DoesNotContain(names, n => n.Contains("Gex", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void L03_No_score_or_probability_fields()
    {
        var props = typeof(SignalMaturitySnapshot).GetProperties().Select(p => p.Name).ToArray();
        Assert.DoesNotContain(props, n => n.Contains("Score", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(props, n => n.Contains("Probability", StringComparison.OrdinalIgnoreCase));
    }
}
