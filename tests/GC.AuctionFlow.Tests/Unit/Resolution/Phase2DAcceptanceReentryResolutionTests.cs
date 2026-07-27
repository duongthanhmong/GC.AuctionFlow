using GC.AuctionFlow.Core;
using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Evidence;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Reference;
using GC.AuctionFlow.Resolution;
using GC.AuctionFlow.Runtime;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Resolution;

/// <summary>
/// Phase 2D Acceptance/Re-entry Resolution.
/// Resolution conclusions require calibrated thresholds â€” all produce NOT CALIBRATED in Phase 2D.
/// Established/Failed/StableReacceptance/ReentryFailed/FAR/AAC: NOT AUTHORIZED.
/// </summary>
public sealed class Phase2DAcceptanceReentryResolutionTests
{
    private const string EvidenceId = "AREV|EP-TEST-01|ACCEPTANCE_REENTRY_EVIDENCE_POLICY_V1";
    private const string EpisodeId = "EP-TEST-01";
    private const string Auction = "PI-2026-07-24";
    private const string ReferenceId = "REF-VAH|PI-2026-07-23";

    private static DateTime Utc(int sec = 0) =>
        new(2026, 7, 24, 12, 0, sec, DateTimeKind.Utc);

    // â”€â”€â”€ minimal vector factories â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static AcceptanceEvidenceVector NullAccVec() =>
        new AcceptanceEvidenceVector(
            TimeSpan.Zero,              // outsideTime
            TimeSpan.Zero,              // totalObservedTime
            null,                       // outsideTimeRatio
            0m,                         // outsideExecutedVolume
            0m,                         // totalObservedExecutedVolume
            null,                       // outsideVolumeRatio
            0L,                         // outsideTradeCount
            0L,                         // totalObservedTradeCount
            null,                       // outsideTradeCountRatio
            0,                          // outsideSegmentCount
            0,                          // attemptCount
            0L,                         // maximumOutsideDistanceTicks
            null,                       // currentDistanceFromReferenceTicks
            null,                       // outsideBidVolume
            null,                       // outsideAskVolume
            null,                       // outsideDelta
            AggressorEvidenceAvailability.Unavailable,
            null,                       // localPocTick
            null,                       // localPocDisplacementTicks
            null,                       // latestSide
            0,                          // consecutiveOutsideEvents
            null,                       // elapsedSinceLastInsideEvent
            null,                       // outsideCloseRatio
            null,                       // tpoCountOutside
            null,                       // valueCentroidDisplacement
            null,                       // oldValueReclaimFailure
            null,                       // retestHoldQuality
            null,                       // localValueLow
            null,                       // localValueHigh
            Array.Empty<string>());

    private static ReentryEvidenceVector NullReentryVec() =>
        new ReentryEvidenceVector(
            false,                      // geometricReentryObserved
            null,                       // firstGeometricReentryAt
            null,                       // reentrySpeed
            0L,                         // maximumDistanceReturnedInsideTicks
            null,                       // currentDistanceInsideTicks
            TimeSpan.Zero,              // timeMaintainedInside
            0m,                         // insideExecutedVolumeAfterReentry
            0L,                         // insideTradeCountAfterReentry
            null,                       // insideBidVolumeAfterReentry
            null,                       // insideAskVolumeAfterReentry
            null,                       // insideDeltaAfterReentry
            AggressorEvidenceAvailability.Unavailable,
            0,                          // subsequentReferenceTestCount
            0,                          // outsideReattemptCount
            null,                       // latestSide
            null,                       // localPocTick
            null,                       // localPocRelativeToReference
            null,                       // localPocChangeSinceReentry
            null,                       // oppositeAggressionQuality
            null,                       // localValueRebuildInside
            null,                       // oldDirectionAggressionEffectiveness
            null,                       // stableReacceptance
            null,                       // reentryFailure
            Array.Empty<string>());

    // â”€â”€â”€ snapshot factories â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static AcceptanceReentryEvidenceSnapshot EvidenceSnap(
        AcceptanceObservationState accObs = AcceptanceObservationState.None,
        ReentryObservationState reentryObs = ReentryObservationState.None,
        EpisodeDataQuality quality = EpisodeDataQuality.Complete,
        string? evidenceId = null,
        long stateVer = 1L,
        long eventRev = 1L)
    {
        return new AcceptanceReentryEvidenceSnapshot(
            evidenceId ?? EvidenceId,
            AcceptanceReentryEvidencePolicyConfig.PolicyVersion,
            EpisodeId,
            Auction,
            ReferenceId,
            ReferenceType.PreviousPrimaryTpoVah,
            ReferenceInteractionRole.UpperBoundary,
            referencePriceTick: 1000L,
            referencePrice: 100.0m,
            canonicalOutsideDirection: ReferenceSidePosition.Above,
            episodeState: EpisodeState.ReentryDeveloping,
            episodeResolution: EpisodeResolution.None,
            measurementStatus: EvidenceMeasurementStatus.Active,
            acceptanceObservationState: accObs,
            reentryObservationState: reentryObs,
            startedAtUtc: Utc(0),
            lastUpdatedAtUtc: Utc(10),
            firstOutsideAtUtc: Utc(1),
            firstGeometricReentryAtUtc: Utc(5),
            lastOutsideAtUtc: Utc(4),
            lastInsideAtUtc: Utc(9),
            totalObservedDuration: TimeSpan.FromSeconds(10),
            totalObservedExecutedVolume: 50m,
            totalObservedTradeCount: 20L,
            acceptance: NullAccVec(),
            reentry: NullReentryVec(),
            stateVersion: stateVer,
            eventRevision: eventRev,
            dataQuality: quality,
            evidenceAvailability: new Dictionary<string, EvidenceComponentAvailability>(),
            limitations: Array.Empty<string>(),
            directionalProvenance: null);
    }

    private static AcceptanceReentryEvidenceSetSnapshot EvidenceSetSnap(
        EvidenceModuleState state,
        AcceptanceReentryEvidenceSnapshot[] active)
    {
        var last = active.Length > 0 ? active[active.Length - 1] : null;
        return new AcceptanceReentryEvidenceSetSnapshot(
            state,
            AcceptanceReentryEvidencePolicyConfig.PolicyVersion,
            Auction,
            active,
            Array.Empty<AcceptanceReentryEvidenceSnapshot>(),
            last,
            new Dictionary<AcceptanceObservationState, int>(),
            new Dictionary<ReentryObservationState, int>(),
            availableComponentCount: 0,
            unavailableComponentCount: 0,
            inputFingerprint: "fp-test",
            registryRevision: 1L,
            createdAtUtc: Utc(0),
            lastUpdatedAtUtc: Utc(10),
            limitations: Array.Empty<string>());
    }

    // Convenience overloads
    private static AcceptanceReentryEvidenceSetSnapshot EvidenceSetSnap(
        EvidenceModuleState state = EvidenceModuleState.Ready) =>
        EvidenceSetSnap(state, Array.Empty<AcceptanceReentryEvidenceSnapshot>());

    private static AcceptanceReentryEvidenceSetSnapshot EvidenceSetSnap(
        AcceptanceReentryEvidenceSnapshot active) =>
        EvidenceSetSnap(EvidenceModuleState.Ready, new[] { active });

    private static AcceptanceReentryEvidenceSetSnapshot EvidenceSetSnap(
        EvidenceModuleState state,
        AcceptanceReentryEvidenceSnapshot active) =>
        EvidenceSetSnap(state, new[] { active });

    private static AcceptanceReentryEvidenceSetSnapshot EvidenceSetSnap(
        AcceptanceReentryEvidenceSnapshot active1,
        AcceptanceReentryEvidenceSnapshot active2) =>
        EvidenceSetSnap(EvidenceModuleState.Ready, new[] { active1, active2 });

    // â”€â”€â”€ A. Disabled / null evidence â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void A01_DefaultOff_Returns_Disabled()
    {
        var host = new AuctionResolutionHost();
        var snap = host.Rebuild(null);
        Assert.Equal(ResolutionModuleState.Disabled, snap.ModuleState);
        Assert.Empty(snap.ActiveResolutions);
        Assert.Null(snap.LatestUpdated);
    }

    [Fact]
    public void A02_NullEvidence_Returns_AwaitingEvidence()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var snap = host.Rebuild(null);
        Assert.Equal(ResolutionModuleState.AwaitingEvidence, snap.ModuleState);
    }

    [Fact]
    public void A03_DisabledEvidence_Returns_AwaitingEvidence()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var snap = host.Rebuild(EvidenceSetSnap(EvidenceModuleState.Disabled));
        Assert.Equal(ResolutionModuleState.AwaitingEvidence, snap.ModuleState);
    }

    [Fact]
    public void A04_InvalidEvidence_Returns_Invalid()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var snap = host.Rebuild(EvidenceSetSnap(EvidenceModuleState.Invalid));
        Assert.Equal(ResolutionModuleState.Invalid, snap.ModuleState);
    }

    [Fact]
    public void A05_Configure_Disabled_Clears_And_Returns_Disabled()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        host.Rebuild(EvidenceSetSnap(EvidenceSnap()));
        host.Configure(new AuctionResolutionPolicyConfig(false));
        Assert.Equal(ResolutionModuleState.Disabled, host.Current?.ModuleState);
    }

    // â”€â”€â”€ B. Acceptance observation â†’ resolution mapping â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void B01_AcceptanceNone_Maps_To_None()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var snap = host.Rebuild(EvidenceSetSnap(EvidenceSnap(accObs: AcceptanceObservationState.None)));
        Assert.Equal(AcceptanceResolutionState.None, snap.LatestUpdated!.AcceptanceResolution);
    }

    [Fact]
    public void B02_AcceptanceEarly_Maps_To_Early()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var snap = host.Rebuild(EvidenceSetSnap(EvidenceSnap(accObs: AcceptanceObservationState.Early)));
        Assert.Equal(AcceptanceResolutionState.Early, snap.LatestUpdated!.AcceptanceResolution);
    }

    [Fact]
    public void B03_AcceptanceDeveloping_Maps_To_Developing()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var snap = host.Rebuild(EvidenceSetSnap(EvidenceSnap(accObs: AcceptanceObservationState.Developing)));
        Assert.Equal(AcceptanceResolutionState.Developing, snap.LatestUpdated!.AcceptanceResolution);
    }

    [Fact]
    public void B04_AcceptanceUnresolved_Maps_To_NotCalibrated()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var snap = host.Rebuild(EvidenceSetSnap(EvidenceSnap(accObs: AcceptanceObservationState.Unresolved)));
        Assert.Equal(AcceptanceResolutionState.NotCalibrated, snap.LatestUpdated!.AcceptanceResolution);
    }

    [Fact]
    public void B05_AcceptanceEstablished_Reserved_Maps_To_NotCalibrated()
    {
        // Phase 1F never emits Established; defensive guard ensures NotCalibrated if it somehow arrives.
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var snap = host.Rebuild(EvidenceSetSnap(EvidenceSnap(accObs: AcceptanceObservationState.Established)));
        Assert.Equal(AcceptanceResolutionState.NotCalibrated, snap.LatestUpdated!.AcceptanceResolution);
    }

    [Fact]
    public void B06_AcceptanceFailed_Reserved_Maps_To_NotCalibrated()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var snap = host.Rebuild(EvidenceSetSnap(EvidenceSnap(accObs: AcceptanceObservationState.Failed)));
        Assert.Equal(AcceptanceResolutionState.NotCalibrated, snap.LatestUpdated!.AcceptanceResolution);
    }

    // â”€â”€â”€ C. Re-entry observation â†’ resolution mapping â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void C01_ReentryNone_Maps_To_None()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var snap = host.Rebuild(EvidenceSetSnap(EvidenceSnap(reentryObs: ReentryObservationState.None)));
        Assert.Equal(ReentryResolutionState.None, snap.LatestUpdated!.ReentryResolution);
    }

    [Fact]
    public void C02_ReentryGeometric_Maps_To_GeometricReentry()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var snap = host.Rebuild(EvidenceSetSnap(EvidenceSnap(reentryObs: ReentryObservationState.GeometricReentry)));
        Assert.Equal(ReentryResolutionState.GeometricReentry, snap.LatestUpdated!.ReentryResolution);
    }

    [Fact]
    public void C03_ReentryDeveloping_Maps_To_Developing()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var snap = host.Rebuild(EvidenceSetSnap(EvidenceSnap(reentryObs: ReentryObservationState.Developing)));
        Assert.Equal(ReentryResolutionState.Developing, snap.LatestUpdated!.ReentryResolution);
    }

    [Fact]
    public void C04_ReentryUnresolved_Maps_To_NotCalibrated()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var snap = host.Rebuild(EvidenceSetSnap(EvidenceSnap(reentryObs: ReentryObservationState.Unresolved)));
        Assert.Equal(ReentryResolutionState.NotCalibrated, snap.LatestUpdated!.ReentryResolution);
    }

    [Fact]
    public void C05_ReentryStableReaccepted_Reserved_Maps_To_NotCalibrated()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var snap = host.Rebuild(EvidenceSetSnap(EvidenceSnap(reentryObs: ReentryObservationState.StableReaccepted)));
        Assert.Equal(ReentryResolutionState.NotCalibrated, snap.LatestUpdated!.ReentryResolution);
    }

    [Fact]
    public void C06_ReentryFailed_Reserved_Maps_To_NotCalibrated()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var snap = host.Rebuild(EvidenceSetSnap(EvidenceSnap(reentryObs: ReentryObservationState.ReentryFailed)));
        Assert.Equal(ReentryResolutionState.NotCalibrated, snap.LatestUpdated!.ReentryResolution);
    }

    // â”€â”€â”€ D. Conclusion, policy, limitations â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void D01_Conclusion_Always_NotCalibrated()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var snap = host.Rebuild(EvidenceSetSnap(EvidenceSnap(
            accObs: AcceptanceObservationState.Developing,
            reentryObs: ReentryObservationState.GeometricReentry)));
        Assert.Equal(AuctionResolutionConclusion.NotCalibrated, snap.LatestUpdated!.Conclusion);
    }

    [Fact]
    public void D02_PolicyVersion_IsCorrect()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var snap = host.Rebuild(EvidenceSetSnap(EvidenceSnap()));
        Assert.Equal(AuctionResolutionPolicyConfig.PolicyVersion, snap.PolicyVersion);
        Assert.Equal(AuctionResolutionPolicyConfig.PolicyVersion, snap.LatestUpdated!.PolicyVersion);
    }

    [Fact]
    public void D03_ResolutionId_Format()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var snap = host.Rebuild(EvidenceSetSnap(EvidenceSnap(evidenceId: EvidenceId)));
        var expectedId = ResolutionIdentity.BuildFromEvidenceId(EvidenceId);
        Assert.Equal(expectedId, snap.LatestUpdated!.ResolutionId);
        Assert.StartsWith("ARES|", snap.LatestUpdated!.ResolutionId, StringComparison.Ordinal);
        Assert.EndsWith(AuctionResolutionPolicyConfig.PolicyVersion, snap.LatestUpdated!.ResolutionId, StringComparison.Ordinal);
    }

    [Fact]
    public void D04_SetSnapshot_Limitations_Contain_NotCalibrated()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var snap = host.Rebuild(EvidenceSetSnap(EvidenceSnap()));
        Assert.Contains(AuctionResolutionPolicyConfig.LimitationNotCalibrated, snap.Limitations);
        Assert.Contains(AuctionResolutionPolicyConfig.LimitationEstablishedNotCalibrated, snap.Limitations);
        Assert.Contains(AuctionResolutionPolicyConfig.LimitationFarNotCalibrated, snap.Limitations);
        Assert.Contains(AuctionResolutionPolicyConfig.LimitationAacNotCalibrated, snap.Limitations);
    }

    [Fact]
    public void D05_Individual_Snapshot_Carries_NotCalibrated_Limitations()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var snap = host.Rebuild(EvidenceSetSnap(EvidenceSnap()));
        var res = snap.LatestUpdated!;
        Assert.Contains(AuctionResolutionPolicyConfig.LimitationNotCalibrated, res.Limitations);
        Assert.Contains(AuctionResolutionPolicyConfig.LimitationFarNotCalibrated, res.Limitations);
        Assert.Contains(AuctionResolutionPolicyConfig.LimitationAacNotCalibrated, res.Limitations);
    }

    [Fact]
    public void D06_InputObservationStates_Preserved()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var snap = host.Rebuild(EvidenceSetSnap(EvidenceSnap(
            accObs: AcceptanceObservationState.Developing,
            reentryObs: ReentryObservationState.GeometricReentry)));
        var res = snap.LatestUpdated!;
        Assert.Equal(AcceptanceObservationState.Developing, res.InputAcceptanceObservation);
        Assert.Equal(ReentryObservationState.GeometricReentry, res.InputReentryObservation);
    }

    // â”€â”€â”€ E. Module state â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void E01_Ready_When_Active_Evidence_Ready()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var snap = host.Rebuild(EvidenceSetSnap(
            EvidenceModuleState.Ready,
            EvidenceSnap(quality: EpisodeDataQuality.Complete)));
        Assert.Equal(ResolutionModuleState.Ready, snap.ModuleState);
    }

    [Fact]
    public void E02_Partial_When_Evidence_Partial()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var snap = host.Rebuild(EvidenceSetSnap(
            EvidenceModuleState.Partial,
            EvidenceSnap(quality: EpisodeDataQuality.Partial)));
        Assert.Equal(ResolutionModuleState.Partial, snap.ModuleState);
    }

    [Fact]
    public void E03_AwaitingEvidence_When_No_Active()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var snap = host.Rebuild(EvidenceSetSnap(EvidenceModuleState.Ready));
        Assert.Equal(ResolutionModuleState.AwaitingEvidence, snap.ModuleState);
    }

    [Fact]
    public void E04_Multiple_Active_All_Mapped()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var ev1 = EvidenceSnap(accObs: AcceptanceObservationState.Early,
            evidenceId: "AREV|EP-01|POLICY", stateVer: 1, eventRev: 10);
        var ev2 = EvidenceSnap(accObs: AcceptanceObservationState.Developing,
            evidenceId: "AREV|EP-02|POLICY", stateVer: 1, eventRev: 20);
        var snap = host.Rebuild(EvidenceSetSnap(ev1, ev2));
        Assert.Equal(2, snap.ActiveResolutions.Count);
        Assert.Contains(snap.ActiveResolutions,
            r => r.AcceptanceResolution == AcceptanceResolutionState.Early);
        Assert.Contains(snap.ActiveResolutions,
            r => r.AcceptanceResolution == AcceptanceResolutionState.Developing);
    }

    // â”€â”€â”€ F. Fingerprint gate â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void F01_SameFingerprint_Returns_Cached()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var ev = EvidenceSetSnap(EvidenceSnap());
        var snap1 = host.Rebuild(ev, Utc(1));
        var snap2 = host.Rebuild(ev, Utc(2));
        // Same evidence fingerprint â†’ same cached snapshot object
        Assert.Same(snap1, snap2);
    }

    [Fact]
    public void F02_DifferentRegistryRevision_Triggers_Rebuild()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var ev1 = EvidenceSetSnap(EvidenceSnap(accObs: AcceptanceObservationState.Early));
        // ev2 with different registryRevision
        var ev2 = new AcceptanceReentryEvidenceSetSnapshot(
            EvidenceModuleState.Ready,
            AcceptanceReentryEvidencePolicyConfig.PolicyVersion,
            Auction,
            new[] { EvidenceSnap(accObs: AcceptanceObservationState.Developing) },
            Array.Empty<AcceptanceReentryEvidenceSnapshot>(),
            EvidenceSnap(accObs: AcceptanceObservationState.Developing),
            new Dictionary<AcceptanceObservationState, int>(),
            new Dictionary<ReentryObservationState, int>(),
            availableComponentCount: 0,
            unavailableComponentCount: 0,
            inputFingerprint: "fp-test",
            registryRevision: 2L,   // changed from 1 â†’ forces rebuild
            createdAtUtc: Utc(0),
            lastUpdatedAtUtc: Utc(10),
            limitations: Array.Empty<string>());

        var snap1 = host.Rebuild(ev1);
        var snap2 = host.Rebuild(ev2);
        Assert.NotSame(snap1, snap2);
        Assert.Equal(AcceptanceResolutionState.Developing, snap2.LatestUpdated!.AcceptanceResolution);
    }

    // â”€â”€â”€ G. Identity and data tracing â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void G01_EvidenceId_EpisodeId_PrimaryAuction_Carried()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var snap = host.Rebuild(EvidenceSetSnap(EvidenceSnap()));
        var res = snap.LatestUpdated!;
        Assert.Equal(EvidenceId, res.EvidenceId);
        Assert.Equal(EpisodeId, res.EpisodeId);
        Assert.Equal(Auction, res.PrimaryAuctionId);
        Assert.Equal(ReferenceId, res.ReferenceId);
    }

    [Fact]
    public void G02_StateVersion_EventRevision_Carried()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var snap = host.Rebuild(EvidenceSetSnap(EvidenceSnap(stateVer: 42L, eventRev: 99L)));
        var res = snap.LatestUpdated!;
        Assert.Equal(42L, res.StateVersion);
        Assert.Equal(99L, res.EventRevision);
    }

    [Fact]
    public void G03_DataQuality_Complete_Snapshot()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var snap = host.Rebuild(EvidenceSetSnap(EvidenceSnap(quality: EpisodeDataQuality.Complete)));
        Assert.Equal(ResolutionDataQuality.Complete, snap.LatestUpdated!.DataQuality);
    }

    [Fact]
    public void G04_DataQuality_Partial_Snapshot()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var snap = host.Rebuild(EvidenceSetSnap(
            EvidenceModuleState.Partial,
            EvidenceSnap(quality: EpisodeDataQuality.Partial)));
        Assert.Equal(ResolutionDataQuality.Partial, snap.LatestUpdated!.DataQuality);
    }

    // â”€â”€â”€ H. GPS card rows â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void H01_Disabled_GPS_Row()
    {
        var disabled = new AuctionResolutionSetSnapshot(
            ResolutionModuleState.Disabled, AuctionResolutionPolicyConfig.PolicyVersion,
            Array.Empty<AuctionResolutionSnapshot>(), Array.Empty<AuctionResolutionSnapshot>(),
            null, 0, 0, Utc(), Utc(), new[] { "MODULE_DISABLED" });

        var rows = AuctionGpsCardMapper.BuildAuctionResolutionLines(disabled, false);
        Assert.Contains("RESOLUTION: DISABLED", rows);
    }

    [Fact]
    public void H02_AwaitingEvidence_GPS_Row()
    {
        var awaiting = new AuctionResolutionSetSnapshot(
            ResolutionModuleState.AwaitingEvidence, AuctionResolutionPolicyConfig.PolicyVersion,
            Array.Empty<AuctionResolutionSnapshot>(), Array.Empty<AuctionResolutionSnapshot>(),
            null, 0, 0, Utc(), Utc(), Array.Empty<string>());

        var rows = AuctionGpsCardMapper.BuildAuctionResolutionLines(awaiting, false);
        Assert.Contains("RESOLUTION: AWAITING EVIDENCE", rows);
        Assert.Contains("RESOLUTION CONCLUSION: NOT CALIBRATED", rows);
    }

    [Fact]
    public void H03_Ready_GPS_Rows()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var snap = host.Rebuild(EvidenceSetSnap(
            EvidenceModuleState.Ready,
            EvidenceSnap(
                accObs: AcceptanceObservationState.Early,
                reentryObs: ReentryObservationState.GeometricReentry)));
        var rows = AuctionGpsCardMapper.BuildAuctionResolutionLines(snap, false);
        Assert.Contains(rows, r => r.StartsWith("RESOLUTION: READY", StringComparison.Ordinal));
        Assert.Contains("ACCEPTANCE RESOLUTION: EARLY", rows);
        Assert.Contains("REENTRY RESOLUTION: GEOMETRICREENTRY", rows);
        Assert.Contains("RESOLUTION CONCLUSION: NOT CALIBRATED", rows);
    }

    [Fact]
    public void H04_Diagnostics_Rows_When_ShowDiagnostics()
    {
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        var snap = host.Rebuild(EvidenceSetSnap(EvidenceSnap(stateVer: 7L, eventRev: 13L)));
        var rows = AuctionGpsCardMapper.BuildAuctionResolutionLines(snap, true);
        Assert.Contains(rows, r => r.StartsWith("RESOLUTION ID:", StringComparison.Ordinal));
        Assert.Contains(rows, r => r.Contains("RESOLUTION STATE VER: 7", StringComparison.Ordinal));
        Assert.Contains(rows, r => r.Contains("RESOLUTION EVENT REV: 13", StringComparison.Ordinal));
    }

    [Fact]
    public void H05_Null_SetSnapshot_Returns_Empty()
    {
        var rows = AuctionGpsCardMapper.BuildAuctionResolutionLines(null, false);
        Assert.Empty(rows);
    }

    // â”€â”€â”€ I. RuntimeSnapshot carries resolution â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void I01_RuntimeEngine_Resolution_Snapshot_In_Published()
    {
        var engine = new GcaeRuntimeEngine();
        var host = new AuctionResolutionHost(new AuctionResolutionPolicyConfig(true));
        host.Rebuild(EvidenceSetSnap(EvidenceSnap(accObs: AcceptanceObservationState.Developing)));
        var resolution = host.Current;

        var snap = engine.Publish(
            observed: null,
            expectedInstrumentCode: "GCQ6",
            mode: DataSourceMode.Live,
            modeProvenance: DataSourceModeProvenance.OperatorDeclared,
            provider: DeclaredFeedProvider.Rithmic,
            providerProvenance: FeedProviderProvenance.OperatorDeclared,
            tradeObserved: false,
            lastTradeCallbackUtc: null,
            rawRecorderMasterEnabled: false,
            tradeRecordingEnabled: false,
            recorderAccepting: false,
            recorderFaulted: false,
            recorderSessionPresent: false,
            indicatorDisposed: false,
            auctionResolution: resolution,
            showAuctionResolutionDiagnostics: false);

        Assert.NotNull(snap.AuctionResolution);
        Assert.Equal(ResolutionModuleState.Ready, snap.AuctionResolution!.ModuleState);
        Assert.Equal("0.25.0", snap.Version);
    }

    [Fact]
    public void I02_RuntimeEngine_No_Resolution_Returns_Null()
    {
        var engine = new GcaeRuntimeEngine();
        var snap = engine.Publish(
            observed: null,
            expectedInstrumentCode: "GCQ6",
            mode: DataSourceMode.Live,
            modeProvenance: DataSourceModeProvenance.OperatorDeclared,
            provider: DeclaredFeedProvider.Rithmic,
            providerProvenance: FeedProviderProvenance.OperatorDeclared,
            tradeObserved: false,
            lastTradeCallbackUtc: null,
            rawRecorderMasterEnabled: false,
            tradeRecordingEnabled: false,
            recorderAccepting: false,
            recorderFaulted: false,
            recorderSessionPresent: false,
            indicatorDisposed: false);

        Assert.Null(snap.AuctionResolution);
    }

    [Fact]
    public void I03_RuntimeSnapshot_Version_Bumped_To_0_11_0()
    {
        Assert.Equal("0.25.0", GcaeRuntimeSnapshot.SnapshotVersion);
    }

    // â”€â”€â”€ J. ResolutionIdentity â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void J01_BuildFromEvidenceId_Format()
    {
        var id = ResolutionIdentity.BuildFromEvidenceId("AREV|EP-01|POLICY_V1");
        Assert.StartsWith("ARES|", id, StringComparison.Ordinal);
        Assert.EndsWith(AuctionResolutionPolicyConfig.PolicyVersion, id, StringComparison.Ordinal);
        // The middle segment (sanitized evidence ID) must not contain | characters
        var withoutPrefix = id.Substring("ARES|".Length);
        var withoutSuffix = withoutPrefix.Substring(0, withoutPrefix.Length
            - 1 - AuctionResolutionPolicyConfig.PolicyVersion.Length);
        Assert.DoesNotContain("|", withoutSuffix, StringComparison.Ordinal);
    }

    [Fact]
    public void J02_BuildFromEvidenceId_EmptyInput_Uses_Unknown()
    {
        var id = ResolutionIdentity.BuildFromEvidenceId("");
        Assert.StartsWith("ARES|", id, StringComparison.Ordinal);
        Assert.Contains("Unknown", id, StringComparison.Ordinal);
    }

    // â”€â”€â”€ K. ResolutionInputFingerprint â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void K01_SameValues_AreEqual()
    {
        var f1 = new ResolutionInputFingerprint(true, 5L, "fp", AuctionResolutionPolicyConfig.PolicyVersion);
        var f2 = new ResolutionInputFingerprint(true, 5L, "fp", AuctionResolutionPolicyConfig.PolicyVersion);
        Assert.True(f1.Equals(f2));
    }

    [Fact]
    public void K02_DifferentRevision_NotEqual()
    {
        var f1 = new ResolutionInputFingerprint(true, 5L, "fp", AuctionResolutionPolicyConfig.PolicyVersion);
        var f2 = new ResolutionInputFingerprint(true, 6L, "fp", AuctionResolutionPolicyConfig.PolicyVersion);
        Assert.False(f1.Equals(f2));
    }

    [Fact]
    public void K03_DifferentFingerprint_NotEqual()
    {
        var f1 = new ResolutionInputFingerprint(true, 5L, "fp-a", AuctionResolutionPolicyConfig.PolicyVersion);
        var f2 = new ResolutionInputFingerprint(true, 5L, "fp-b", AuctionResolutionPolicyConfig.PolicyVersion);
        Assert.False(f1.Equals(f2));
    }

    [Fact]
    public void K04_EnabledFalse_DifferentFrom_EnabledTrue()
    {
        var f1 = new ResolutionInputFingerprint(true, 5L, "fp", AuctionResolutionPolicyConfig.PolicyVersion);
        var f2 = new ResolutionInputFingerprint(false, 5L, "fp", AuctionResolutionPolicyConfig.PolicyVersion);
        Assert.False(f1.Equals(f2));
    }
}
