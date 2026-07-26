using GC.AuctionFlow.Core;
using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Evidence;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Reference;
using GC.AuctionFlow.Runtime;
using GC.AuctionFlow.Thesis;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Thesis;

/// <summary>
/// Phase 3A FAR + AAC Thesis State Machine Foundation.
/// Observable states only: EpisodeActive, OutsideAttempt, ReentryDeveloping, Pullback.
/// Armed/Executable/Managing/Completed → NOT CALIBRATED.
/// FAR legacy alias: "Sweep-Reclaim Reversal" (log/UI only).
/// AAC legacy alias: "Break-Accept-Retest Continuation" (log/UI only).
/// </summary>
public sealed class Phase3FarAacThesisTests
{
    private const string Auction = "PI-2026-07-26";
    private const decimal Tick = 0.1m;
    private const string Epoch = "GCQ6|tick=0.1";
    private const string Instrument = "GCQ6";

    private static DateTime Utc(int sec = 0) =>
        new(2026, 7, 26, 12, 0, sec, DateTimeKind.Utc);

    // ---------- helper: build minimal evidence snapshot ----------

    private static AcceptanceReentryEvidenceSnapshot MakeEvidence(
        string evidenceId,
        EpisodeState episodeState,
        ReferenceSidePosition? outsideDir,
        AcceptanceObservationState acceptObs = AcceptanceObservationState.None,
        ReentryObservationState reentryObs = ReentryObservationState.None,
        long stateVersion = 1L,
        long eventRevision = 1L)
    {
        var grid = new PriceGrid(Tick);
        var refPrice = 2400.0m;
        var refTick = grid.ToTickIndex(refPrice);
        return new AcceptanceReentryEvidenceSnapshot(
            evidenceId,
            AcceptanceReentryEvidencePolicyConfig.PolicyVersion,
            episodeId: "EP-1",
            primaryAuctionId: Auction,
            referenceId: "REF-1",
            referenceType: ReferenceType.PreviousPrimaryTpoPoc,
            referenceRole: ReferenceInteractionRole.UpperBoundary,
            referencePriceTick: refTick,
            referencePrice: refPrice,
            canonicalOutsideDirection: outsideDir,
            episodeState: episodeState,
            episodeResolution: EpisodeResolution.None,
            measurementStatus: EvidenceMeasurementStatus.Active,
            acceptanceObservationState: acceptObs,
            reentryObservationState: reentryObs,
            startedAtUtc: Utc(0),
            lastUpdatedAtUtc: Utc(1),
            firstOutsideAtUtc: null,
            firstGeometricReentryAtUtc: null,
            lastOutsideAtUtc: null,
            lastInsideAtUtc: null,
            totalObservedDuration: TimeSpan.Zero,
            totalObservedExecutedVolume: 0m,
            totalObservedTradeCount: 0L,
            acceptance: new AcceptanceEvidenceVector(
                TimeSpan.Zero, TimeSpan.Zero, null, 0m, 0m, null, 0L, 0L, null, 0, 0, 0L, null,
                null, null, null, AggressorEvidenceAvailability.Unavailable, null, null, null, 0, null, null, null, null, null, null, null, null,
                Array.Empty<string>()),
            reentry: new ReentryEvidenceVector(
                false, null, null, 0L, null, TimeSpan.Zero, 0m, 0L,
                null, null, null, AggressorEvidenceAvailability.Unavailable, 0, 0, null,
                null, null, null, null, null, null, null, null,
                Array.Empty<string>()),
            stateVersion: stateVersion,
            eventRevision: eventRevision,
            dataQuality: EpisodeDataQuality.Complete,
            evidenceAvailability: new Dictionary<string, EvidenceComponentAvailability>(),
            limitations: Array.Empty<string>(),
            directionalProvenance: null);
    }

    private static AcceptanceReentryEvidenceSetSnapshot MakeEvidenceSet(
        params AcceptanceReentryEvidenceSnapshot[] activeEvidence) =>
        new(
            EvidenceModuleState.Ready,
            AcceptanceReentryEvidencePolicyConfig.PolicyVersion,
            Auction,
            activeEvidence,
            Array.Empty<AcceptanceReentryEvidenceSnapshot>(),
            activeEvidence.Length > 0 ? activeEvidence[^1] : null,
            new Dictionary<AcceptanceObservationState, int>(),
            new Dictionary<ReentryObservationState, int>(),
            availableComponentCount: 1,
            unavailableComponentCount: 0,
            inputFingerprint: "fp-test",
            registryRevision: 1L,
            createdAtUtc: Utc(0),
            lastUpdatedAtUtc: Utc(1),
            limitations: Array.Empty<string>());

    // ========== A: Policy & identity ==========

    [Fact]
    public void A01_FarPolicyVersion_is_FAR_THESIS_POLICY_V1()
    {
        Assert.Equal("FAR_THESIS_POLICY_V1", FarThesisPolicyConfig.PolicyVersion);
    }

    [Fact]
    public void A02_AacPolicyVersion_is_AAC_THESIS_POLICY_V1()
    {
        Assert.Equal("AAC_THESIS_POLICY_V1", AacThesisPolicyConfig.PolicyVersion);
    }

    [Fact]
    public void A03_FarThesisId_format_FAR_pipe_evidenceId_pipe_policy()
    {
        var id = ThesisIdentity.BuildFarId("EV|GCQ6|001");
        Assert.StartsWith("FAR|", id, StringComparison.Ordinal);
        Assert.Contains("FAR_THESIS_POLICY_V1", id, StringComparison.Ordinal);
    }

    [Fact]
    public void A04_AacThesisId_format_AAC_pipe_evidenceId_pipe_policy()
    {
        var id = ThesisIdentity.BuildAacId("EV|GCQ6|001");
        Assert.StartsWith("AAC|", id, StringComparison.Ordinal);
        Assert.Contains("AAC_THESIS_POLICY_V1", id, StringComparison.Ordinal);
    }

    [Fact]
    public void A05_ThesisId_sanitizes_pipe_in_evidenceId()
    {
        var far = ThesisIdentity.BuildFarId("EV|SOME|ID");
        Assert.DoesNotContain("EV|SOME|ID", far, StringComparison.Ordinal);
        Assert.Contains("EV_SOME_ID", far, StringComparison.Ordinal);

        var aac = ThesisIdentity.BuildAacId("EV|SOME|ID");
        Assert.DoesNotContain("EV|SOME|ID", aac, StringComparison.Ordinal);
        Assert.Contains("EV_SOME_ID", aac, StringComparison.Ordinal);
    }

    [Fact]
    public void A06_ThesisId_handles_null_evidenceId()
    {
        var far = ThesisIdentity.BuildFarId(null!);
        Assert.StartsWith("FAR|Unknown|", far, StringComparison.Ordinal);

        var aac = ThesisIdentity.BuildAacId(null!);
        Assert.StartsWith("AAC|Unknown|", aac, StringComparison.Ordinal);
    }

    [Fact]
    public void A07_FarSnapshotVersion_is_1_0_0()
    {
        Assert.Equal("1.0.0", FarThesisSnapshot.SnapshotVersion);
        Assert.Equal("1.0.0", FarThesisSetSnapshot.SnapshotVersion);
    }

    [Fact]
    public void A08_AacSnapshotVersion_is_1_0_0()
    {
        Assert.Equal("1.0.0", AacThesisSnapshot.SnapshotVersion);
        Assert.Equal("1.0.0", AacThesisSetSnapshot.SnapshotVersion);
    }

    // ========== B: FAR host disabled ==========

    [Fact]
    public void B01_FarHost_Disabled_produces_Disabled_snapshot()
    {
        var host = new FarThesisHost(new FarThesisPolicyConfig(enabled: false));
        var set = host.Rebuild(null, Utc());
        Assert.Equal(ThesisModuleState.Disabled, set.ModuleState);
    }

    [Fact]
    public void B02_FarHost_Disabled_snapshot_has_MODULE_DISABLED_limitation()
    {
        var host = new FarThesisHost(new FarThesisPolicyConfig(enabled: false));
        var set = host.Rebuild(null, Utc());
        Assert.Contains("MODULE_DISABLED", set.Limitations);
    }

    [Fact]
    public void B03_FarHost_Disabled_has_no_active_theses()
    {
        var host = new FarThesisHost(new FarThesisPolicyConfig(enabled: false));
        var set = host.Rebuild(null, Utc());
        Assert.Empty(set.ActiveTheses);
        Assert.Equal(0, set.ArmableCount);
        Assert.Equal(0, set.ExecutableCount);
    }

    // ========== C: FAR host awaiting evidence ==========

    [Fact]
    public void C01_FarHost_null_evidence_produces_AwaitingEvidence()
    {
        var host = new FarThesisHost(new FarThesisPolicyConfig(enabled: true));
        var set = host.Rebuild(null, Utc());
        Assert.Equal(ThesisModuleState.AwaitingEvidence, set.ModuleState);
    }

    [Fact]
    public void C02_FarHost_disabled_evidence_produces_AwaitingEvidence()
    {
        var host = new FarThesisHost(new FarThesisPolicyConfig(enabled: true));
        var disabled = new AcceptanceReentryEvidenceSetSnapshot(
            EvidenceModuleState.Disabled,
            AcceptanceReentryEvidencePolicyConfig.PolicyVersion,
            Auction,
            Array.Empty<AcceptanceReentryEvidenceSnapshot>(),
            Array.Empty<AcceptanceReentryEvidenceSnapshot>(),
            null,
            new Dictionary<AcceptanceObservationState, int>(),
            new Dictionary<ReentryObservationState, int>(),
            0, 0, "", 0L, Utc(), Utc(), Array.Empty<string>());
        var set = host.Rebuild(disabled, Utc());
        Assert.Equal(ThesisModuleState.AwaitingEvidence, set.ModuleState);
    }

    [Fact]
    public void C03_FarHost_awaiting_has_not_calibrated_limitation()
    {
        var host = new FarThesisHost(new FarThesisPolicyConfig(enabled: true));
        var set = host.Rebuild(null, Utc());
        Assert.Contains(FarThesisPolicyConfig.LimitationNotCalibrated, set.Limitations);
    }

    // ========== D: FAR state mapping from EpisodeState ==========

    [Fact]
    public void D01_FarHost_Interacting_maps_to_EpisodeActive()
    {
        var host = new FarThesisHost(new FarThesisPolicyConfig(enabled: true));
        var ev = MakeEvidence("EV-001", EpisodeState.Interacting, ReferenceSidePosition.Below);
        var set = host.Rebuild(MakeEvidenceSet(ev), Utc());
        Assert.Single(set.ActiveTheses);
        Assert.Equal(FarState.EpisodeActive, set.ActiveTheses[0].FarState);
        Assert.False(set.ActiveTheses[0].NotCalibrated);
    }

    [Fact]
    public void D02_FarHost_OutsideAttempt_maps_to_OutsideAttempt()
    {
        var host = new FarThesisHost(new FarThesisPolicyConfig(enabled: true));
        var ev = MakeEvidence("EV-002", EpisodeState.OutsideAttempt, ReferenceSidePosition.Below,
            reentryObs: ReentryObservationState.None);
        var set = host.Rebuild(MakeEvidenceSet(ev), Utc());
        Assert.Equal(FarState.OutsideAttempt, set.ActiveTheses[0].FarState);
    }

    [Fact]
    public void D03_FarHost_Developing_maps_to_OutsideAttempt()
    {
        var host = new FarThesisHost(new FarThesisPolicyConfig(enabled: true));
        var ev = MakeEvidence("EV-003", EpisodeState.Developing, ReferenceSidePosition.Below,
            reentryObs: ReentryObservationState.None);
        var set = host.Rebuild(MakeEvidenceSet(ev), Utc());
        Assert.Equal(FarState.OutsideAttempt, set.ActiveTheses[0].FarState);
    }

    [Fact]
    public void D04_FarHost_OutsideAttempt_plus_GeometricReentry_maps_to_ReentryDeveloping()
    {
        var host = new FarThesisHost(new FarThesisPolicyConfig(enabled: true));
        var ev = MakeEvidence("EV-004", EpisodeState.OutsideAttempt, ReferenceSidePosition.Below,
            reentryObs: ReentryObservationState.GeometricReentry);
        var set = host.Rebuild(MakeEvidenceSet(ev), Utc());
        Assert.Equal(FarState.ReentryDeveloping, set.ActiveTheses[0].FarState);
        Assert.False(set.ActiveTheses[0].NotCalibrated);
    }

    [Fact]
    public void D05_FarHost_OutsideAttempt_plus_ReentryDevelopingObs_maps_to_ReentryDeveloping()
    {
        var host = new FarThesisHost(new FarThesisPolicyConfig(enabled: true));
        var ev = MakeEvidence("EV-005", EpisodeState.OutsideAttempt, ReferenceSidePosition.Below,
            reentryObs: ReentryObservationState.Developing);
        var set = host.Rebuild(MakeEvidenceSet(ev), Utc());
        Assert.Equal(FarState.ReentryDeveloping, set.ActiveTheses[0].FarState);
    }

    [Fact]
    public void D06_FarHost_ReentryDeveloping_episode_state_is_NotCalibrated_ReentryDeveloping()
    {
        // Phase 3 cap: ReacceptedInside requires Phase 2D StableReacceptance (NotCalibrated).
        var host = new FarThesisHost(new FarThesisPolicyConfig(enabled: true));
        var ev = MakeEvidence("EV-006", EpisodeState.ReentryDeveloping, ReferenceSidePosition.Below);
        var set = host.Rebuild(MakeEvidenceSet(ev), Utc());
        Assert.Equal(FarState.ReentryDeveloping, set.ActiveTheses[0].FarState);
        Assert.True(set.ActiveTheses[0].NotCalibrated);
    }

    [Fact]
    public void D07_FarHost_EpisodeExpired_maps_to_Expired()
    {
        var host = new FarThesisHost(new FarThesisPolicyConfig(enabled: true));
        var ev = MakeEvidence("EV-007", EpisodeState.EpisodeExpired, ReferenceSidePosition.Below);
        var set = host.Rebuild(MakeEvidenceSet(ev), Utc());
        Assert.Equal(FarState.Expired, set.ActiveTheses[0].FarState);
    }

    [Fact]
    public void D08_FarHost_Idle_default_for_unknown_episode_state()
    {
        var host = new FarThesisHost(new FarThesisPolicyConfig(enabled: true));
        // ApproachingReference (reserved, value 100) not normally emitted — maps to Idle
        var ev = MakeEvidence("EV-008", (EpisodeState)100, null);
        var set = host.Rebuild(MakeEvidenceSet(ev), Utc());
        Assert.Equal(FarState.Idle, set.ActiveTheses[0].FarState);
    }

    // ========== E: FAR direction mapping ==========

    [Fact]
    public void E01_FarHost_OutsideBelow_is_Long_thesis()
    {
        // FAR Long: price breaks below reference, fails, re-enters → long thesis.
        var host = new FarThesisHost(new FarThesisPolicyConfig(enabled: true));
        var ev = MakeEvidence("EV-E01", EpisodeState.OutsideAttempt, ReferenceSidePosition.Below);
        var set = host.Rebuild(MakeEvidenceSet(ev), Utc());
        Assert.Equal(ThesisDirection.Long, set.ActiveTheses[0].Direction);
    }

    [Fact]
    public void E02_FarHost_OutsideAbove_is_Short_thesis()
    {
        // FAR Short: price breaks above reference, fails, re-enters → short thesis.
        var host = new FarThesisHost(new FarThesisPolicyConfig(enabled: true));
        var ev = MakeEvidence("EV-E02", EpisodeState.OutsideAttempt, ReferenceSidePosition.Above);
        var set = host.Rebuild(MakeEvidenceSet(ev), Utc());
        Assert.Equal(ThesisDirection.Short, set.ActiveTheses[0].Direction);
    }

    [Fact]
    public void E03_FarHost_null_outside_direction_is_Unknown()
    {
        var host = new FarThesisHost(new FarThesisPolicyConfig(enabled: true));
        var ev = MakeEvidence("EV-E03", EpisodeState.Interacting, null);
        var set = host.Rebuild(MakeEvidenceSet(ev), Utc());
        Assert.Equal(ThesisDirection.Unknown, set.ActiveTheses[0].Direction);
    }

    // ========== F: FAR fingerprint gating ==========

    [Fact]
    public void F01_FarHost_same_evidence_fingerprint_returns_same_snapshot_instance()
    {
        var host = new FarThesisHost(new FarThesisPolicyConfig(enabled: true));
        var evSet = MakeEvidenceSet(
            MakeEvidence("EV-F01", EpisodeState.Interacting, ReferenceSidePosition.Below));
        var set1 = host.Rebuild(evSet, Utc(0));
        var set2 = host.Rebuild(evSet, Utc(1));
        Assert.Same(set1, set2);
    }

    [Fact]
    public void F02_FarHost_Reset_then_rebuild_returns_new_snapshot()
    {
        var host = new FarThesisHost(new FarThesisPolicyConfig(enabled: true));
        var evSet = MakeEvidenceSet(
            MakeEvidence("EV-F02", EpisodeState.Interacting, ReferenceSidePosition.Below));
        var set1 = host.Rebuild(evSet, Utc(0));
        host.Reset();
        var set2 = host.Rebuild(evSet, Utc(1));
        Assert.NotSame(set1, set2);
    }

    // ========== G: FAR ArmableCount/ExecutableCount always 0 ==========

    [Fact]
    public void G01_FarHost_ArmableCount_always_zero()
    {
        var host = new FarThesisHost(new FarThesisPolicyConfig(enabled: true));
        var ev = MakeEvidence("EV-G01", EpisodeState.OutsideAttempt, ReferenceSidePosition.Below);
        var set = host.Rebuild(MakeEvidenceSet(ev), Utc());
        Assert.Equal(0, set.ArmableCount);
    }

    [Fact]
    public void G02_FarHost_ExecutableCount_always_zero()
    {
        var host = new FarThesisHost(new FarThesisPolicyConfig(enabled: true));
        var ev = MakeEvidence("EV-G02", EpisodeState.OutsideAttempt, ReferenceSidePosition.Below);
        var set = host.Rebuild(MakeEvidenceSet(ev), Utc());
        Assert.Equal(0, set.ExecutableCount);
    }

    // ========== H: FAR NotCalibrated limitations in set ==========

    [Fact]
    public void H01_FarThesisSet_has_all_required_not_calibrated_limitations()
    {
        var host = new FarThesisHost(new FarThesisPolicyConfig(enabled: true));
        var ev = MakeEvidence("EV-H01", EpisodeState.Interacting, ReferenceSidePosition.Below);
        var set = host.Rebuild(MakeEvidenceSet(ev), Utc());
        Assert.Contains(FarThesisPolicyConfig.LimitationNotCalibrated, set.Limitations);
        Assert.Contains(FarThesisPolicyConfig.LimitationArmedNotCalibrated, set.Limitations);
        Assert.Contains(FarThesisPolicyConfig.LimitationExecutableNotCalibrated, set.Limitations);
        Assert.Contains(FarThesisPolicyConfig.LimitationReacceptedInsideNotCalibrated, set.Limitations);
        Assert.Contains(FarThesisPolicyConfig.LimitationTwoAttemptNotCalibrated, set.Limitations);
        Assert.Contains(FarThesisPolicyConfig.LimitationLiveOnly, set.Limitations);
    }

    [Fact]
    public void H02_FarThesisSnapshot_has_not_calibrated_limitation()
    {
        var host = new FarThesisHost(new FarThesisPolicyConfig(enabled: true));
        var ev = MakeEvidence("EV-H02", EpisodeState.Interacting, ReferenceSidePosition.Below);
        var set = host.Rebuild(MakeEvidenceSet(ev), Utc());
        var snap = set.ActiveTheses[0];
        Assert.Contains(FarThesisPolicyConfig.LimitationNotCalibrated, snap.Limitations);
        Assert.Contains(FarThesisPolicyConfig.LimitationEntryNotAuthorized, snap.Limitations);
        Assert.Contains(FarThesisPolicyConfig.LimitationRiskNotAuthorized, snap.Limitations);
    }

    // ========== I: AAC disabled ==========

    [Fact]
    public void I01_AacHost_Disabled_produces_Disabled_snapshot()
    {
        var host = new AacThesisHost(new AacThesisPolicyConfig(enabled: false));
        var set = host.Rebuild(null, Utc());
        Assert.Equal(ThesisModuleState.Disabled, set.ModuleState);
    }

    [Fact]
    public void I02_AacHost_Disabled_has_MODULE_DISABLED_limitation()
    {
        var host = new AacThesisHost(new AacThesisPolicyConfig(enabled: false));
        var set = host.Rebuild(null, Utc());
        Assert.Contains("MODULE_DISABLED", set.Limitations);
    }

    // ========== J: AAC state mapping ==========

    [Fact]
    public void J01_AacHost_Interacting_maps_to_EpisodeActive()
    {
        var host = new AacThesisHost(new AacThesisPolicyConfig(enabled: true));
        var ev = MakeEvidence("EV-J01", EpisodeState.Interacting, ReferenceSidePosition.Above);
        var set = host.Rebuild(MakeEvidenceSet(ev), Utc());
        Assert.Single(set.ActiveTheses);
        Assert.Equal(AacState.EpisodeActive, set.ActiveTheses[0].AacState);
        Assert.False(set.ActiveTheses[0].NotCalibrated);
    }

    [Fact]
    public void J02_AacHost_OutsideAttempt_no_acceptance_maps_to_OutsideAttempt_not_calibrated_false()
    {
        var host = new AacThesisHost(new AacThesisPolicyConfig(enabled: true));
        var ev = MakeEvidence("EV-J02", EpisodeState.OutsideAttempt, ReferenceSidePosition.Above,
            acceptObs: AcceptanceObservationState.Early);
        var set = host.Rebuild(MakeEvidenceSet(ev), Utc());
        Assert.Equal(AacState.OutsideAttempt, set.ActiveTheses[0].AacState);
        Assert.False(set.ActiveTheses[0].NotCalibrated);
    }

    [Fact]
    public void J03_AacHost_OutsideAttempt_with_AcceptanceDeveloping_is_NotCalibrated()
    {
        // AcceptanceDeveloping requires calibrated thresholds — stays at OutsideAttempt + NotCalibrated.
        var host = new AacThesisHost(new AacThesisPolicyConfig(enabled: true));
        var ev = MakeEvidence("EV-J03", EpisodeState.OutsideAttempt, ReferenceSidePosition.Above,
            acceptObs: AcceptanceObservationState.Developing);
        var set = host.Rebuild(MakeEvidenceSet(ev), Utc());
        Assert.Equal(AacState.OutsideAttempt, set.ActiveTheses[0].AacState);
        Assert.True(set.ActiveTheses[0].NotCalibrated);
    }

    [Fact]
    public void J04_AacHost_ReentryDeveloping_maps_to_Pullback_NotCalibrated()
    {
        // Geometric re-entry alone does NOT invalidate AAC. Invalidation requires
        // StableReacceptance, which is calibration-gated (v1.3 §8.2 / G-AAC-001).
        var host = new AacThesisHost(new AacThesisPolicyConfig(enabled: true));
        var ev = MakeEvidence("EV-J04", EpisodeState.ReentryDeveloping, ReferenceSidePosition.Above);
        var set = host.Rebuild(MakeEvidenceSet(ev), Utc());
        Assert.Equal(AacState.Pullback, set.ActiveTheses[0].AacState);
        Assert.True(set.ActiveTheses[0].NotCalibrated);
    }

    [Fact]
    public void J04b_AacHost_never_invalidates_on_geometry_alone()
    {
        // G-DISC-002: GeometricReentry must not be promoted to StableReacceptance,
        // therefore no evidence-observable episode state may yield Invalidated.
        var host = new AacThesisHost(new AacThesisPolicyConfig(enabled: true));
        foreach (var st in new[]
        {
            EpisodeState.Interacting,
            EpisodeState.OutsideAttempt,
            EpisodeState.Developing,
            EpisodeState.ReentryDeveloping
        })
        {
            host.Reset();
            var ev = MakeEvidence("EV-J04b-" + st, st, ReferenceSidePosition.Above);
            var set = host.Rebuild(MakeEvidenceSet(ev), Utc());
            Assert.NotEqual(AacState.Invalidated, set.ActiveTheses[0].AacState);
            Assert.NotEqual(AacState.ReacceptedOldValue, set.ActiveTheses[0].AacState);
        }
    }

    [Fact]
    public void J05_AacHost_EpisodeExpired_maps_to_Expired()
    {
        var host = new AacThesisHost(new AacThesisPolicyConfig(enabled: true));
        var ev = MakeEvidence("EV-J05", EpisodeState.EpisodeExpired, ReferenceSidePosition.Above);
        var set = host.Rebuild(MakeEvidenceSet(ev), Utc());
        Assert.Equal(AacState.Expired, set.ActiveTheses[0].AacState);
    }

    [Fact]
    public void J06_AacHost_Developing_episode_maps_to_OutsideAttempt()
    {
        var host = new AacThesisHost(new AacThesisPolicyConfig(enabled: true));
        var ev = MakeEvidence("EV-J06", EpisodeState.Developing, ReferenceSidePosition.Above);
        var set = host.Rebuild(MakeEvidenceSet(ev), Utc());
        Assert.Equal(AacState.OutsideAttempt, set.ActiveTheses[0].AacState);
    }

    // ========== K: AAC direction mapping — opposite of FAR ==========

    [Fact]
    public void K01_AacHost_OutsideAbove_is_Long_thesis()
    {
        // AAC Long: break ABOVE reference → continuation upward = Long.
        var host = new AacThesisHost(new AacThesisPolicyConfig(enabled: true));
        var ev = MakeEvidence("EV-K01", EpisodeState.OutsideAttempt, ReferenceSidePosition.Above);
        var set = host.Rebuild(MakeEvidenceSet(ev), Utc());
        Assert.Equal(ThesisDirection.Long, set.ActiveTheses[0].Direction);
    }

    [Fact]
    public void K02_AacHost_OutsideBelow_is_Short_thesis()
    {
        // AAC Short: break BELOW reference → continuation downward = Short.
        var host = new AacThesisHost(new AacThesisPolicyConfig(enabled: true));
        var ev = MakeEvidence("EV-K02", EpisodeState.OutsideAttempt, ReferenceSidePosition.Below);
        var set = host.Rebuild(MakeEvidenceSet(ev), Utc());
        Assert.Equal(ThesisDirection.Short, set.ActiveTheses[0].Direction);
    }

    [Fact]
    public void K03_AacHost_null_direction_is_Unknown()
    {
        var host = new AacThesisHost(new AacThesisPolicyConfig(enabled: true));
        var ev = MakeEvidence("EV-K03", EpisodeState.Interacting, null);
        var set = host.Rebuild(MakeEvidenceSet(ev), Utc());
        Assert.Equal(ThesisDirection.Unknown, set.ActiveTheses[0].Direction);
    }

    // ========== L: AAC NotCalibrated limitations ==========

    [Fact]
    public void L01_AacThesisSet_has_all_required_not_calibrated_limitations()
    {
        var host = new AacThesisHost(new AacThesisPolicyConfig(enabled: true));
        var ev = MakeEvidence("EV-L01", EpisodeState.OutsideAttempt, ReferenceSidePosition.Above);
        var set = host.Rebuild(MakeEvidenceSet(ev), Utc());
        Assert.Contains(AacThesisPolicyConfig.LimitationNotCalibrated, set.Limitations);
        Assert.Contains(AacThesisPolicyConfig.LimitationArmedNotCalibrated, set.Limitations);
        Assert.Contains(AacThesisPolicyConfig.LimitationAcceptedOutsideNotCalibrated, set.Limitations);
        Assert.Contains(AacThesisPolicyConfig.LimitationPullbackNotCalibrated, set.Limitations);
        Assert.Contains(AacThesisPolicyConfig.LimitationLiveOnly, set.Limitations);
    }

    [Fact]
    public void L02_AacThesisSnapshot_has_not_calibrated_limitation()
    {
        var host = new AacThesisHost(new AacThesisPolicyConfig(enabled: true));
        var ev = MakeEvidence("EV-L02", EpisodeState.OutsideAttempt, ReferenceSidePosition.Above);
        var set = host.Rebuild(MakeEvidenceSet(ev), Utc());
        var snap = set.ActiveTheses[0];
        Assert.Contains(AacThesisPolicyConfig.LimitationNotCalibrated, snap.Limitations);
        Assert.Contains(AacThesisPolicyConfig.LimitationEntryNotAuthorized, snap.Limitations);
        Assert.Contains(AacThesisPolicyConfig.LimitationRiskNotAuthorized, snap.Limitations);
    }

    // ========== M: AAC fingerprint gating ==========

    [Fact]
    public void M01_AacHost_same_fingerprint_returns_same_snapshot()
    {
        var host = new AacThesisHost(new AacThesisPolicyConfig(enabled: true));
        var evSet = MakeEvidenceSet(
            MakeEvidence("EV-M01", EpisodeState.OutsideAttempt, ReferenceSidePosition.Above));
        var set1 = host.Rebuild(evSet, Utc(0));
        var set2 = host.Rebuild(evSet, Utc(1));
        Assert.Same(set1, set2);
    }

    [Fact]
    public void M02_AacHost_Reset_then_rebuild_returns_new_snapshot()
    {
        var host = new AacThesisHost(new AacThesisPolicyConfig(enabled: true));
        var evSet = MakeEvidenceSet(
            MakeEvidence("EV-M02", EpisodeState.OutsideAttempt, ReferenceSidePosition.Above));
        var set1 = host.Rebuild(evSet, Utc(0));
        host.Reset();
        var set2 = host.Rebuild(evSet, Utc(1));
        Assert.NotSame(set1, set2);
    }

    // ========== N: GPS card UI lines ==========

    [Fact]
    public void N01_BuildFarThesisLines_null_returns_empty()
    {
        var lines = AuctionGpsCardMapper.BuildFarThesisLines(null, false);
        Assert.Empty(lines);
    }

    [Fact]
    public void N02_BuildFarThesisLines_Disabled_returns_DISABLED_line()
    {
        var set = new FarThesisSetSnapshot(
            ThesisModuleState.Disabled, FarThesisPolicyConfig.PolicyVersion,
            Array.Empty<FarThesisSnapshot>(), Array.Empty<FarThesisSnapshot>(),
            null, 0, 0, Utc(), Utc(), new[] { "MODULE_DISABLED" });
        var lines = AuctionGpsCardMapper.BuildFarThesisLines(set, false);
        Assert.Single(lines);
        Assert.Equal("FAR THESIS: DISABLED", lines[0]);
    }

    [Fact]
    public void N03_BuildAacThesisLines_null_returns_empty()
    {
        var lines = AuctionGpsCardMapper.BuildAacThesisLines(null, false);
        Assert.Empty(lines);
    }

    [Fact]
    public void N04_BuildAacThesisLines_Disabled_returns_DISABLED_line()
    {
        var set = new AacThesisSetSnapshot(
            ThesisModuleState.Disabled, AacThesisPolicyConfig.PolicyVersion,
            Array.Empty<AacThesisSnapshot>(), Array.Empty<AacThesisSnapshot>(),
            null, 0, 0, Utc(), Utc(), new[] { "MODULE_DISABLED" });
        var lines = AuctionGpsCardMapper.BuildAacThesisLines(set, false);
        Assert.Single(lines);
        Assert.Equal("AAC THESIS: DISABLED", lines[0]);
    }

    [Fact]
    public void N05_BuildFarThesisLines_AwaitingEvidence_shows_policy_and_not_calibrated()
    {
        var host = new FarThesisHost(new FarThesisPolicyConfig(enabled: true));
        var set = host.Rebuild(null, Utc());
        var lines = AuctionGpsCardMapper.BuildFarThesisLines(set, false);
        Assert.Contains(lines, l => l.Contains("FAR_THESIS_POLICY_V1", StringComparison.Ordinal));
        Assert.Contains(lines, l => l.Contains("NOT CALIBRATED", StringComparison.Ordinal));
    }

    [Fact]
    public void N06_BuildAacThesisLines_Ready_shows_state_and_direction()
    {
        var host = new AacThesisHost(new AacThesisPolicyConfig(enabled: true));
        var ev = MakeEvidence("EV-N06", EpisodeState.OutsideAttempt, ReferenceSidePosition.Above);
        var set = host.Rebuild(MakeEvidenceSet(ev), Utc());
        var lines = AuctionGpsCardMapper.BuildAacThesisLines(set, false);
        Assert.Contains(lines, l => l.Contains("OUTSIDEATTEMPT", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(lines, l => l.Contains("LONG", StringComparison.OrdinalIgnoreCase));
    }

    // ========== O: GPS card diagnostic rows — FAR + AAC rows added ==========

    [Fact]
    public void O01_GpsCard_diagnostics_has_FAR_and_AAC_rows()
    {
        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(0.1m, 14));
        var snap = engine.Publish(
            new ObservedInstrumentSnapshot("GCU6", "GCU6-ID", "GCU6", "COMEX",
                new DateTime(2026, 8, 27), 0.1m, "GC", "GCU6", "COMEX", 0.1m, null),
            "GCU7",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            tradeObserved: false, lastTradeCallbackUtc: null,
            rawRecorderMasterEnabled: false, tradeRecordingEnabled: true,
            recorderAccepting: false, recorderFaulted: false, recorderSessionPresent: false,
            indicatorDisposed: false, timestampUtc: Utc());

        var vm = AuctionGpsCardMapper.FromSnapshot(snap, showDiagnostics: true);
        Assert.Equal(21, vm.DiagnosticRows.Count);
        Assert.Contains(vm.DiagnosticRows, r => r.StartsWith("FAR:", StringComparison.Ordinal));
        Assert.Contains(vm.DiagnosticRows, r => r.StartsWith("AAC:", StringComparison.Ordinal));
    }

    [Fact]
    public void O02_GpsCard_diagnostics_AllLines_count_delta_is_21()
    {
        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(0.1m, 14));
        var snap = engine.Publish(
            new ObservedInstrumentSnapshot("GCU6", "GCU6-ID", "GCU6", "COMEX",
                new DateTime(2026, 8, 27), 0.1m, "GC", "GCU6", "COMEX", 0.1m, null),
            "GCU7",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            tradeObserved: false, lastTradeCallbackUtc: null,
            rawRecorderMasterEnabled: false, tradeRecordingEnabled: true,
            recorderAccepting: false, recorderFaulted: false, recorderSessionPresent: false,
            indicatorDisposed: false, timestampUtc: Utc());

        var vm = AuctionGpsCardMapper.FromSnapshot(snap, showDiagnostics: true);
        Assert.Equal(vm.AllLines(false).Count + 21, vm.AllLines(true).Count);
    }

    // ========== P: Schema version bumped to 0.16.0 ==========

    [Fact]
    public void P01_SnapshotVersion_is_0_24_0()
    {
        Assert.Equal("0.24.0", GcaeRuntimeSnapshot.SnapshotVersion);
        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(0.1m, 14));
        var snap = engine.Publish(
            null, "GCQ6",
            DataSourceMode.Unknown, DataSourceModeProvenance.Unknown,
            DeclaredFeedProvider.Unknown, FeedProviderProvenance.Unknown,
            false, null, false, false, false, false, false, false,
            timestampUtc: Utc());
        Assert.Equal("0.24.0", snap.Version);
    }

    // ========== Q: FAR/AAC both wired into runtime publish ==========

    [Fact]
    public void Q01_Publish_with_farThesis_snapshot_stored()
    {
        var engine = new GcaeRuntimeEngine();
        var farSet = new FarThesisSetSnapshot(
            ThesisModuleState.AwaitingEvidence, FarThesisPolicyConfig.PolicyVersion,
            Array.Empty<FarThesisSnapshot>(), Array.Empty<FarThesisSnapshot>(),
            null, 0, 0, Utc(), Utc(),
            new[] { FarThesisPolicyConfig.LimitationNotCalibrated });

        var snap = engine.Publish(
            null, "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            false, null, false, false, false, false, false, false,
            timestampUtc: Utc(),
            farThesis: farSet);

        Assert.NotNull(snap.FarThesis);
        Assert.Equal(ThesisModuleState.AwaitingEvidence, snap.FarThesis!.ModuleState);
    }

    [Fact]
    public void Q02_Publish_with_aacThesis_snapshot_stored()
    {
        var engine = new GcaeRuntimeEngine();
        var aacSet = new AacThesisSetSnapshot(
            ThesisModuleState.AwaitingEvidence, AacThesisPolicyConfig.PolicyVersion,
            Array.Empty<AacThesisSnapshot>(), Array.Empty<AacThesisSnapshot>(),
            null, 0, 0, Utc(), Utc(),
            new[] { AacThesisPolicyConfig.LimitationNotCalibrated });

        var snap = engine.Publish(
            null, "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            false, null, false, false, false, false, false, false,
            timestampUtc: Utc(),
            aacThesis: aacSet);

        Assert.NotNull(snap.AacThesis);
        Assert.Equal(ThesisModuleState.AwaitingEvidence, snap.AacThesis!.ModuleState);
    }

    // ========== R: Source scope assertion ==========

    [Fact]
    public void R01_SourceScope_Phase3A_Thesis_directory_exists()
    {
        var root = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
                "src", "GC.AuctionFlow", "Thesis"));
        Assert.True(Directory.Exists(root),
            $"Expected Thesis source directory at: {root}");
        var far = Path.Combine(root, "FarThesisHost.cs");
        var aac = Path.Combine(root, "AacThesisHost.cs");
        Assert.True(File.Exists(far), $"FarThesisHost.cs not found at: {far}");
        Assert.True(File.Exists(aac), $"AacThesisHost.cs not found at: {aac}");
    }

    [Fact]
    public void R02_SourceScope_indicator_has_EnableFarThesis_and_EnableAacThesis_settings()
    {
        var indicator = File.ReadAllText(
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
                "src", "GC.AuctionFlow", "Atas", "GcAuctionFlowIndicator.cs")));
        Assert.Contains("EnableFarThesis = false", indicator, StringComparison.Ordinal);
        Assert.Contains("EnableAacThesis = false", indicator, StringComparison.Ordinal);
        Assert.Contains("FAR_THESIS_POLICY_V1", indicator, StringComparison.Ordinal);
        Assert.Contains("AAC_THESIS_POLICY_V1", indicator, StringComparison.Ordinal);
    }
}
