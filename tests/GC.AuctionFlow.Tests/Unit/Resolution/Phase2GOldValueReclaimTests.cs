using GC.AuctionFlow.Core;
using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Evidence;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Reference;
using GC.AuctionFlow.Resolution;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Resolution;

/// <summary>
/// Phase 2G Old Value Reclaim Test — v1.3 §6.2 / G-ACC-005, KDK Ch 18.
///
/// This is the FAR-vs-AAC decision axis. Three states must be distinguishable:
/// no attempt, attempted-and-held (FAR), attempted-and-failed (AAC).
/// The two outcomes stay calibration-gated; only "attempted at all" is observable.
/// </summary>
public sealed class Phase2GOldValueReclaimTests
{
    private const string Auction = "PI-2026-07-27";
    private const decimal Tick = 0.1m;

    private static DateTime Utc(int sec = 0) =>
        new(2026, 7, 27, 12, 0, sec, DateTimeKind.Utc);

    // ---------- helpers ----------

    private static AcceptanceReentryEvidenceSnapshot MakeEvidence(
        ReentryObservationState reentryObs,
        AcceptanceObservationState acceptObs = AcceptanceObservationState.None,
        string evidenceId = "EV-1",
        int attemptCount = 1,
        TimeSpan? timeMaintainedInside = null,
        long maxDistanceInside = 0L,
        long? currentDistanceInside = null,
        bool? localValueRebuild = null,
        decimal insideVolumeAfterReentry = 0m,
        TimeSpan? elapsedSinceInside = null)
    {
        var grid = new PriceGrid(Tick);
        var refPrice = 2400.0m;
        var refTick = grid.ToTickIndex(refPrice);

        var acceptance = new AcceptanceEvidenceVector(
            TimeSpan.Zero, TimeSpan.Zero, null,       // outsideTime, totalObservedTime, outsideTimeRatio
            0m, 0m, null,                             // outsideExecutedVolume, total, ratio
            0L, 0L, null,                             // outsideTradeCount, total, ratio
            0,                                        // outsideSegmentCount
            attemptCount,                             // attemptCount
            0L, null,                                 // maximumOutsideDistanceTicks, currentDistanceFromReferenceTicks
            null, null, null,                         // outsideBid/Ask/Delta
            AggressorEvidenceAvailability.Unavailable,
            null, null,                               // localPocTick, localPocDisplacementTicks
            null, 0,                                  // latestSide, consecutiveOutsideEvents
            elapsedSinceInside,                       // elapsedSinceLastInsideEvent  <-- attempt window
            null, null, null,                         // outsideCloseRatio, tpoCountOutside, valueCentroidDisplacement
            null,                                     // oldValueReclaimFailure (dead field, superseded by Phase 2G)
            null, null, null,                         // retestHoldQuality, localValueLow, localValueHigh
            Array.Empty<string>());

        var reentry = new ReentryEvidenceVector(
            reentryObs != ReentryObservationState.None,  // geometricReentryObserved
            null, null,                                  // firstGeometricReentryAt, reentrySpeed
            maxDistanceInside,
            currentDistanceInside,
            timeMaintainedInside ?? TimeSpan.Zero,
            insideVolumeAfterReentry,
            0L,                                          // insideTradeCountAfterReentry
            null, null, null,                            // insideBid/Ask/Delta
            AggressorEvidenceAvailability.Unavailable,
            0, 0,                                        // subsequentReferenceTestCount, outsideReattemptCount
            null, null, null, null,                      // latestSide, localPocTick, relative, changeSinceReentry
            null,                                        // oppositeAggressionQuality
            localValueRebuild,                           // localValueRebuildInside
            null, null, null,                            // oldDirectionAggressionEffectiveness, stableReacceptance, reentryFailure
            Array.Empty<string>());

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
            canonicalOutsideDirection: ReferenceSidePosition.Above,
            episodeState: EpisodeState.OutsideAttempt,
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
            acceptance: acceptance,
            reentry: reentry,
            stateVersion: 1L,
            eventRevision: 1L,
            dataQuality: EpisodeDataQuality.Complete,
            evidenceAvailability: new Dictionary<string, EvidenceComponentAvailability>(),
            limitations: Array.Empty<string>(),
            directionalProvenance: null);
    }

    private static AcceptanceReentryEvidenceSetSnapshot MakeSet(
        params AcceptanceReentryEvidenceSnapshot[] ev) =>
        new(
            EvidenceModuleState.Ready,
            AcceptanceReentryEvidencePolicyConfig.PolicyVersion,
            Auction,
            ev,
            Array.Empty<AcceptanceReentryEvidenceSnapshot>(),
            ev.Length > 0 ? ev[^1] : null,
            new Dictionary<AcceptanceObservationState, int>(),
            new Dictionary<ReentryObservationState, int>(),
            availableComponentCount: 1,
            unavailableComponentCount: 0,
            inputFingerprint: "fp-2g",
            registryRevision: 1L,
            createdAtUtc: Utc(0),
            lastUpdatedAtUtc: Utc(1),
            limitations: Array.Empty<string>());

    private static AuctionResolutionHost EnabledHost() =>
        new AuctionResolutionHost(new AuctionResolutionPolicyConfig(enabled: true));

    private static OldValueReclaimObservation Reclaim(
        ReentryObservationState obs, params object[] _)
    {
        var set = EnabledHost().Rebuild(MakeSet(MakeEvidence(obs)), Utc());
        return set.ActiveResolutions[0].OldValueReclaim;
    }

    // ========== A: Enum shape ==========

    /// <summary>
    /// v1.3 G-ACC-005: three states must exist. A nullable bool cannot express
    /// "not attempted" and "attempted, outcome unknown" as different facts.
    /// </summary>
    [Fact]
    public void A01_Three_reclaim_outcomes_are_representable()
    {
        Assert.Equal(0, (int)OldValueReclaimState.Unknown);
        Assert.Equal(1, (int)OldValueReclaimState.NotAttempted);
        Assert.Equal(2, (int)OldValueReclaimState.AttemptedOutcomeNotCalibrated);
        Assert.True((int)OldValueReclaimState.AttemptedAndHeld >= 100);
        Assert.True((int)OldValueReclaimState.AttemptedAndFailed >= 100);
    }

    [Fact]
    public void A02_Held_and_failed_are_distinct_states()
    {
        // FAR needs "held"; AAC needs "failed". Collapsing them loses the axis.
        Assert.NotEqual(OldValueReclaimState.AttemptedAndHeld, OldValueReclaimState.AttemptedAndFailed);
    }

    [Fact]
    public void A03_Limitation_constants_are_stable()
    {
        Assert.Equal("OLD_VALUE_RECLAIM_OUTCOME_NOT_CALIBRATED", AuctionResolutionPolicyConfig.LimitationReclaimNotCalibrated);
        Assert.Equal("OLD_VALUE_RECLAIM_HELD_NOT_CALIBRATED", AuctionResolutionPolicyConfig.LimitationReclaimHeldNotCalibrated);
        Assert.Equal("OLD_VALUE_RECLAIM_FAILED_NOT_CALIBRATED", AuctionResolutionPolicyConfig.LimitationReclaimFailedNotCalibrated);
        Assert.Equal("OLD_VALUE_RECLAIM_WINDOW_NOT_CALIBRATED", AuctionResolutionPolicyConfig.LimitationReclaimWindowNotCalibrated);
    }

    // ========== B: Derivation from evidence ==========

    [Fact]
    public void B01_No_reentry_observation_is_not_attempted() =>
        Assert.Equal(OldValueReclaimState.NotAttempted,
            Reclaim(ReentryObservationState.None).State);

    [Fact]
    public void B02_Unknown_reentry_observation_is_unknown() =>
        Assert.Equal(OldValueReclaimState.Unknown,
            Reclaim(ReentryObservationState.Unknown).State);

    [Theory]
    [InlineData(ReentryObservationState.GeometricReentry)]
    [InlineData(ReentryObservationState.Developing)]
    [InlineData(ReentryObservationState.Unresolved)]
    public void B03_Any_reentry_observation_is_attempted_but_uncalibrated(ReentryObservationState obs) =>
        Assert.Equal(OldValueReclaimState.AttemptedOutcomeNotCalibrated, Reclaim(obs).State);

    /// <summary>
    /// v1.3 G-DISC-002: geometric re-entry is geometry only. It must not be
    /// promoted to "held", which is what would support FAR.
    /// </summary>
    [Fact]
    public void B04_Geometric_reentry_is_never_promoted_to_held()
    {
        var r = Reclaim(ReentryObservationState.GeometricReentry);
        Assert.NotEqual(OldValueReclaimState.AttemptedAndHeld, r.State);
        Assert.NotEqual(OldValueReclaimState.AttemptedAndFailed, r.State);
        Assert.True(r.IsObservableOnly);
    }

    [Fact]
    public void B05_Reserved_outcomes_are_never_emitted()
    {
        foreach (var obs in Enum.GetValues<ReentryObservationState>())
        {
            var r = Reclaim(obs);
            Assert.True((int)r.State < 100,
                "reclaim emitted a calibrated verdict for " + obs);
        }
    }

    // ========== C: Measurements ==========

    [Fact]
    public void C01_Attempted_carries_raw_measurements()
    {
        var ev = MakeEvidence(
            ReentryObservationState.GeometricReentry,
            attemptCount: 3,
            timeMaintainedInside: TimeSpan.FromSeconds(42),
            maxDistanceInside: 17L,
            currentDistanceInside: 5L,
            localValueRebuild: true,
            insideVolumeAfterReentry: 250m,
            elapsedSinceInside: TimeSpan.FromSeconds(8));

        var r = EnabledHost().Rebuild(MakeSet(ev), Utc()).ActiveResolutions[0].OldValueReclaim;

        Assert.Equal(3, r.AttemptCount);
        Assert.Equal(TimeSpan.FromSeconds(42), r.TimeMaintainedInside);
        Assert.Equal(17L, r.MaximumDistanceReturnedInsideTicks);
        Assert.Equal(5L, r.CurrentDistanceInsideTicks);
        Assert.True(r.LocalValueRebuildInside);
        Assert.Equal(250m, r.InsideExecutedVolumeAfterReentry);
        Assert.Equal(TimeSpan.FromSeconds(8), r.ElapsedSinceLastInsideEvent);
    }

    /// <summary>
    /// G-ACC-005 requires a time bound. Reporting how long since price was last
    /// inside is the observable half; the deciding window length is not calibrated.
    /// </summary>
    [Fact]
    public void C02_Attempt_window_is_present_but_its_length_is_not_calibrated()
    {
        var ev = MakeEvidence(ReentryObservationState.Developing,
            elapsedSinceInside: TimeSpan.FromMinutes(2));
        var r = EnabledHost().Rebuild(MakeSet(ev), Utc()).ActiveResolutions[0].OldValueReclaim;

        Assert.Equal(TimeSpan.FromMinutes(2), r.ElapsedSinceLastInsideEvent);
        Assert.Contains(AuctionResolutionPolicyConfig.LimitationReclaimWindowNotCalibrated, r.Limitations);
    }

    /// <summary>
    /// v1.3 G-ACC-003: unavailable is null, never 0. Reporting measurements for a
    /// reclaim that was never attempted would imply an attempt that never happened.
    /// </summary>
    [Fact]
    public void C03_Not_attempted_reports_null_measurements_not_zero()
    {
        var r = Reclaim(ReentryObservationState.None);
        Assert.Equal(0, r.AttemptCount);
        Assert.Null(r.TimeMaintainedInside);
        Assert.Null(r.MaximumDistanceReturnedInsideTicks);
        Assert.Null(r.CurrentDistanceInsideTicks);
        Assert.Null(r.LocalValueRebuildInside);
        Assert.Null(r.InsideExecutedVolumeAfterReentry);
        Assert.Null(r.ElapsedSinceLastInsideEvent);
    }

    [Fact]
    public void C04_Unknown_also_reports_null_measurements()
    {
        var r = Reclaim(ReentryObservationState.Unknown);
        Assert.Null(r.TimeMaintainedInside);
        Assert.Null(r.ElapsedSinceLastInsideEvent);
    }

    [Fact]
    public void C05_Unavailable_local_value_rebuild_stays_null()
    {
        var ev = MakeEvidence(ReentryObservationState.GeometricReentry, localValueRebuild: null);
        var r = EnabledHost().Rebuild(MakeSet(ev), Utc()).ActiveResolutions[0].OldValueReclaim;
        Assert.Null(r.LocalValueRebuildInside);
    }

    // ========== D: Limitations ==========

    [Fact]
    public void D01_Every_reclaim_carries_all_four_limitations()
    {
        foreach (var obs in Enum.GetValues<ReentryObservationState>())
        {
            var r = Reclaim(obs);
            Assert.Contains(AuctionResolutionPolicyConfig.LimitationReclaimNotCalibrated, r.Limitations);
            Assert.Contains(AuctionResolutionPolicyConfig.LimitationReclaimHeldNotCalibrated, r.Limitations);
            Assert.Contains(AuctionResolutionPolicyConfig.LimitationReclaimFailedNotCalibrated, r.Limitations);
            Assert.Contains(AuctionResolutionPolicyConfig.LimitationReclaimWindowNotCalibrated, r.Limitations);
        }
    }

    // ========== E: Resolution snapshot integration ==========

    [Fact]
    public void E01_Reclaim_is_always_present_on_a_resolution()
    {
        var set = EnabledHost().Rebuild(MakeSet(MakeEvidence(ReentryObservationState.None)), Utc());
        Assert.NotNull(set.ActiveResolutions[0].OldValueReclaim);
    }

    [Fact]
    public void E02_Conclusion_stays_not_calibrated_even_with_a_reclaim_attempt()
    {
        var set = EnabledHost().Rebuild(
            MakeSet(MakeEvidence(ReentryObservationState.GeometricReentry)), Utc());
        var r = set.ActiveResolutions[0];
        Assert.Equal(OldValueReclaimState.AttemptedOutcomeNotCalibrated, r.OldValueReclaim.State);
        // The axis is measured, but FAR/AAC still cannot be concluded.
        Assert.Equal(AuctionResolutionConclusion.NotCalibrated, r.Conclusion);
    }

    [Fact]
    public void E03_Multiple_evidence_scopes_each_get_their_own_reclaim()
    {
        var set = EnabledHost().Rebuild(MakeSet(
            MakeEvidence(ReentryObservationState.None, evidenceId: "EV-A"),
            MakeEvidence(ReentryObservationState.GeometricReentry, evidenceId: "EV-B")), Utc());

        Assert.Equal(2, set.ActiveResolutions.Count);
        Assert.Contains(set.ActiveResolutions,
            x => x.OldValueReclaim.State == OldValueReclaimState.NotAttempted);
        Assert.Contains(set.ActiveResolutions,
            x => x.OldValueReclaim.State == OldValueReclaimState.AttemptedOutcomeNotCalibrated);
    }

    // ========== F: GPS card ==========

    [Fact]
    public void F01_ReclaimLines_null_is_empty() =>
        Assert.Empty(AuctionGpsCardMapper.BuildOldValueReclaimLines(null, false));

    [Fact]
    public void F02_ReclaimLines_show_state()
    {
        var rows = AuctionGpsCardMapper.BuildOldValueReclaimLines(
            Reclaim(ReentryObservationState.None), false);
        Assert.Contains("OLD VALUE RECLAIM: NOTATTEMPTED", rows);
    }

    [Fact]
    public void F03_ReclaimLines_declare_outcome_not_calibrated_when_attempted()
    {
        var rows = AuctionGpsCardMapper.BuildOldValueReclaimLines(
            Reclaim(ReentryObservationState.GeometricReentry), false);
        Assert.Contains("RECLAIM OUTCOME: NOT CALIBRATED", rows);
    }

    [Fact]
    public void F04_ReclaimLines_diagnostics_show_raw_measurements()
    {
        var ev = MakeEvidence(ReentryObservationState.Developing,
            timeMaintainedInside: TimeSpan.FromSeconds(30), maxDistanceInside: 12L);
        var r = EnabledHost().Rebuild(MakeSet(ev), Utc()).ActiveResolutions[0].OldValueReclaim;
        var rows = AuctionGpsCardMapper.BuildOldValueReclaimLines(r, true);

        Assert.Contains(rows, x => x.StartsWith("RECLAIM DWELL INSIDE: 30.0s", StringComparison.Ordinal));
        Assert.Contains(rows, x => x.StartsWith("RECLAIM MAX DEPTH: 12 ticks", StringComparison.Ordinal));
        Assert.Contains("RECLAIM WINDOW: NOT CALIBRATED", rows);
    }

    [Fact]
    public void F05_ReclaimLines_report_unavailable_not_zero()
    {
        var rows = AuctionGpsCardMapper.BuildOldValueReclaimLines(
            Reclaim(ReentryObservationState.None), true);
        Assert.Contains("RECLAIM DWELL INSIDE: unavailable", rows);
        Assert.Contains("RECLAIM MAX DEPTH: unavailable", rows);
        Assert.Contains("RECLAIM LOCAL VALUE REBUILD: unavailable", rows);
    }

    [Fact]
    public void F06_ReclaimLines_never_emit_a_calibrated_verdict()
    {
        foreach (var obs in Enum.GetValues<ReentryObservationState>())
        {
            var text = string.Join(" | ",
                AuctionGpsCardMapper.BuildOldValueReclaimLines(Reclaim(obs), true));
            Assert.DoesNotContain("ATTEMPTEDANDHELD", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ATTEMPTEDANDFAILED", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    // ========== G: Scope guards ==========

    /// <summary>
    /// The reclaim observation is measurement only. It must not carry a verdict
    /// shortcut such as a boolean "reclaimed" flag, which is the shape v1.3
    /// G-ACC-005 rejects.
    /// </summary>
    [Fact]
    public void G01_No_boolean_verdict_shortcut_exists()
    {
        var bools = typeof(OldValueReclaimObservation).GetProperties()
            .Where(p => p.PropertyType == typeof(bool))
            .Select(p => p.Name)
            .ToArray();
        // IsObservableOnly is a derived guard, not a verdict.
        Assert.Equal(new[] { "IsObservableOnly" }, bools);
    }

    [Fact]
    public void G02_No_threshold_surface_on_the_observation()
    {
        foreach (var p in typeof(OldValueReclaimObservation).GetProperties())
        {
            Assert.DoesNotContain("Threshold", p.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Score", p.Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
