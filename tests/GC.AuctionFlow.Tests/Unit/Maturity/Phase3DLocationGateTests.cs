using GC.AuctionFlow.Directional;
using GC.AuctionFlow.Maturity;
using GC.AuctionFlow.Thesis;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Maturity;

/// <summary>
/// Phase 3D Location Gate — v1.3 §10, guards G-LOC-001..003.
///
/// v1.2 §2.3 asserts that orderflow only has meaning in Context and Location, but
/// until this phase no module enforced the Location half. The gate is purely
/// positional: no threshold, no direction, no score.
/// </summary>
public sealed class Phase3DLocationGateTests
{
    private static DateTime Utc(int sec = 0) =>
        new(2026, 7, 27, 12, 0, sec, DateTimeKind.Utc);

    // ---------- helpers ----------

    private static FarThesisSnapshot Far(FarState state, string id = "FAR-1") =>
        new(id, FarThesisPolicyConfig.PolicyVersion,
            evidenceId: "EV-1", episodeId: "EP-1",
            primaryAuctionId: "PI-1", referenceId: "REF-1",
            direction: ThesisDirection.Long, farState: state, notCalibrated: false,
            attemptCount: 1, dataQuality: ThesisDataQuality.Complete,
            stateVersion: 1L, eventRevision: 1L,
            observedAtUtc: Utc(), limitations: Array.Empty<string>());

    private static FarThesisSetSnapshot FarSet(params FarThesisSnapshot[] t) =>
        new(ThesisModuleState.Ready, FarThesisPolicyConfig.PolicyVersion,
            t, Array.Empty<FarThesisSnapshot>(), t.Length > 0 ? t[^1] : null,
            0, 0, Utc(), Utc(1), Array.Empty<string>());

    private static ProfileLocationContextSnapshot Location(
        PriceValueLocation volume,
        PriceValueLocation tpo = PriceValueLocation.Unavailable) =>
        new(currentPrimaryTpo: tpo,
            currentPrimaryVolume: volume,
            previousPrimaryTpo: PriceValueLocation.Unavailable,
            confirmedCompositeTpo: PriceValueLocation.Unavailable,
            confirmedCompositeVolume: PriceValueLocation.Unavailable);

    private static SignalMaturityHost EnabledHost() =>
        new(new SignalMaturityPolicyConfig(enabled: true));

    private static SignalMaturitySnapshot Gate(
        PriceValueLocation location,
        FarState state = FarState.ReentryDeveloping) =>
        EnabledHost()
            .Rebuild(FarSet(Far(state)), null, Location(location), Utc())
            .ActiveCandidates[0];

    // ========== A: Enum shape ==========

    [Fact]
    public void A01_Target_space_veto_is_reserved()
    {
        // G-LOC-002 cannot be evaluated until the Target Engine (Phase 3E).
        Assert.True((int)LocationGateOutcome.BlockedNoTargetSpace >= 100);
    }

    [Fact]
    public void A02_Observable_outcomes_are_below_100()
    {
        Assert.True((int)LocationGateOutcome.BlockedLocationUnavailable < 100);
        Assert.True((int)LocationGateOutcome.AllowedLowQuality < 100);
        Assert.True((int)LocationGateOutcome.AllowedBoundary < 100);
        Assert.True((int)LocationGateOutcome.AllowedOutside < 100);
    }

    [Fact]
    public void A03_Limitation_constants_are_stable()
    {
        Assert.Equal("PRICE_LOCATION_UNAVAILABLE", SignalMaturityPolicyConfig.LimitationLocationUnavailable);
        Assert.Equal("LOW_QUALITY_LOCATION_MID_VALUE", SignalMaturityPolicyConfig.LimitationLowQualityLocation);
        Assert.Equal("REMAINING_TARGET_SPACE_NOT_AVAILABLE", SignalMaturityPolicyConfig.LimitationTargetSpaceNotAvailable);
    }

    // ========== B: Gate mapping ==========

    [Theory]
    [InlineData(PriceValueLocation.Unavailable, LocationGateOutcome.BlockedLocationUnavailable)]
    [InlineData(PriceValueLocation.InsideValue, LocationGateOutcome.AllowedLowQuality)]
    [InlineData(PriceValueLocation.AtPoc, LocationGateOutcome.AllowedLowQuality)]
    [InlineData(PriceValueLocation.AtValueHigh, LocationGateOutcome.AllowedBoundary)]
    [InlineData(PriceValueLocation.AtValueLow, LocationGateOutcome.AllowedBoundary)]
    [InlineData(PriceValueLocation.AboveValue, LocationGateOutcome.AllowedOutside)]
    [InlineData(PriceValueLocation.BelowValue, LocationGateOutcome.AllowedOutside)]
    public void B01_Location_maps_to_gate_outcome(
        PriceValueLocation loc, LocationGateOutcome expected) =>
        Assert.Equal(expected, Gate(loc).LocationGate);

    [Fact]
    public void B02_Observed_location_is_carried_for_audit() =>
        Assert.Equal(PriceValueLocation.AboveValue, Gate(PriceValueLocation.AboveValue).ObservedLocation);

    /// <summary>Volume profile reflects executed activity, so it wins over TPO.</summary>
    [Fact]
    public void B03_Volume_location_is_preferred_over_tpo()
    {
        var set = EnabledHost().Rebuild(
            FarSet(Far(FarState.ReentryDeveloping)), null,
            Location(volume: PriceValueLocation.AboveValue, tpo: PriceValueLocation.InsideValue),
            Utc());
        Assert.Equal(PriceValueLocation.AboveValue, set.ActiveCandidates[0].ObservedLocation);
    }

    [Fact]
    public void B04_Tpo_location_is_used_when_volume_is_unavailable()
    {
        var set = EnabledHost().Rebuild(
            FarSet(Far(FarState.ReentryDeveloping)), null,
            Location(volume: PriceValueLocation.Unavailable, tpo: PriceValueLocation.BelowValue),
            Utc());
        Assert.Equal(PriceValueLocation.BelowValue, set.ActiveCandidates[0].ObservedLocation);
    }

    // ========== C: G-LOC-003 — no location, no candidate ==========

    [Fact]
    public void C01_Unavailable_location_blocks_the_candidate_lifecycle()
    {
        var sm = Gate(PriceValueLocation.Unavailable, FarState.ReentryDeveloping);
        // Would be Candidate on thesis state alone; the gate holds it back.
        Assert.NotEqual(AnalysisLifecycleState.Candidate, sm.LifecycleState);
        Assert.Equal(AnalysisLifecycleState.EpisodeActive, sm.LifecycleState);
    }

    [Fact]
    public void C02_Blocked_scope_is_reported_not_silently_dropped()
    {
        var set = EnabledHost().Rebuild(
            FarSet(Far(FarState.ReentryDeveloping)), null,
            Location(PriceValueLocation.Unavailable), Utc());
        // Dropping the scope would hide the block from the operator.
        Assert.Single(set.ActiveCandidates);
        Assert.Equal(0, set.CandidateCount);
    }

    [Fact]
    public void C03_Blocked_location_is_explained()
    {
        var sm = Gate(PriceValueLocation.Unavailable);
        Assert.Contains(MaturityBlockingReason.PriceLocationUnavailable, sm.BlockingReasons);
        Assert.Contains(SignalMaturityPolicyConfig.LimitationLocationUnavailable, sm.Limitations);
    }

    [Fact]
    public void C04_Null_location_context_blocks_too()
    {
        var set = EnabledHost().Rebuild(FarSet(Far(FarState.ReentryDeveloping)), null, null, Utc());
        var sm = set.ActiveCandidates[0];
        Assert.Equal(LocationGateOutcome.BlockedLocationUnavailable, sm.LocationGate);
        Assert.NotEqual(AnalysisLifecycleState.Candidate, sm.LifecycleState);
    }

    [Fact]
    public void C05_Gate_does_not_disturb_terminal_states()
    {
        // A thesis that already ended must not be resurrected or re-labelled by the gate.
        foreach (var st in new[] { FarState.Invalidated, FarState.Expired, FarState.Completed })
        {
            var sm = Gate(PriceValueLocation.Unavailable, st);
            Assert.True((int)sm.LifecycleState >= 200, st + " was altered by the gate");
        }
    }

    // ========== D: G-LOC-001 — mid-value is low quality ==========

    [Theory]
    [InlineData(PriceValueLocation.InsideValue)]
    [InlineData(PriceValueLocation.AtPoc)]
    public void D01_Mid_value_is_flagged_low_quality(PriceValueLocation loc)
    {
        var sm = Gate(loc);
        Assert.Contains(MaturityBlockingReason.LowQualityLocation, sm.BlockingReasons);
        Assert.Contains(SignalMaturityPolicyConfig.LimitationLowQualityLocation, sm.Limitations);
    }

    /// <summary>
    /// G-LOC-001 says low-quality location may never reach Confirmed. Confirmed is
    /// itself calibration-gated, so the invariant must hold from both directions.
    /// </summary>
    [Fact]
    public void D02_Low_quality_location_can_never_be_confirmed()
    {
        var sm = Gate(PriceValueLocation.InsideValue);
        Assert.NotEqual(SignalMaturityLevel.Confirmed, sm.MaturityLevel);
        Assert.Equal(SignalMaturityLevel.NotCalibrated, sm.MaturityLevel);
    }

    [Fact]
    public void D03_Low_quality_still_allows_the_candidate()
    {
        // Mid-value is the worst location, not an impossible one.
        var sm = Gate(PriceValueLocation.InsideValue, FarState.ReentryDeveloping);
        Assert.Equal(AnalysisLifecycleState.Candidate, sm.LifecycleState);
    }

    [Theory]
    [InlineData(PriceValueLocation.AtValueHigh)]
    [InlineData(PriceValueLocation.AboveValue)]
    public void D04_Boundary_and_outside_are_not_low_quality(PriceValueLocation loc)
    {
        var sm = Gate(loc);
        Assert.DoesNotContain(MaturityBlockingReason.LowQualityLocation, sm.BlockingReasons);
        Assert.DoesNotContain(SignalMaturityPolicyConfig.LimitationLowQualityLocation, sm.Limitations);
    }

    // ========== E: G-LOC-002 — target-space veto is unavailable ==========

    [Fact]
    public void E01_Target_space_veto_is_declared_unavailable_not_passed()
    {
        // Silently treating "cannot evaluate" as "passed" is the failure mode here.
        var sm = Gate(PriceValueLocation.AboveValue);
        Assert.Contains(SignalMaturityPolicyConfig.LimitationTargetSpaceNotAvailable, sm.Limitations);
        Assert.Contains(MaturityBlockingReason.TargetSpaceUnavailable, sm.BlockingReasons);
    }

    [Fact]
    public void E02_No_scope_ever_reports_the_target_space_veto()
    {
        foreach (var loc in Enum.GetValues<PriceValueLocation>())
            Assert.NotEqual(LocationGateOutcome.BlockedNoTargetSpace, Gate(loc).LocationGate);
    }

    // ========== F: Purity of the gate ==========

    /// <summary>
    /// The gate is positional only: it must not vary with thesis direction.
    /// </summary>
    [Fact]
    public void F01_Gate_is_independent_of_thesis_direction()
    {
        var longThesis = new FarThesisSnapshot(
            "FAR-L", FarThesisPolicyConfig.PolicyVersion, "EV", "EP", "PI", "REF",
            ThesisDirection.Long, FarState.ReentryDeveloping, false, 1,
            ThesisDataQuality.Complete, 1L, 1L, Utc(), Array.Empty<string>());
        var shortThesis = new FarThesisSnapshot(
            "FAR-S", FarThesisPolicyConfig.PolicyVersion, "EV", "EP", "PI", "REF",
            ThesisDirection.Short, FarState.ReentryDeveloping, false, 1,
            ThesisDataQuality.Complete, 1L, 1L, Utc(), Array.Empty<string>());

        var set = EnabledHost().Rebuild(
            FarSet(longThesis, shortThesis), null,
            Location(PriceValueLocation.AboveValue), Utc());

        Assert.Equal(2, set.ActiveCandidates.Count);
        Assert.Single(set.ActiveCandidates.Select(x => x.LocationGate).Distinct());
    }

    [Fact]
    public void F02_Gate_never_unlocks_a_maturity_level()
    {
        foreach (var loc in Enum.GetValues<PriceValueLocation>())
            Assert.Equal(SignalMaturityLevel.NotCalibrated, Gate(loc).MaturityLevel);
    }

    [Fact]
    public void F03_Location_change_invalidates_the_fingerprint()
    {
        var host = EnabledHost();
        var far = FarSet(Far(FarState.ReentryDeveloping));
        var a = host.Rebuild(far, null, Location(PriceValueLocation.AboveValue), Utc());
        var b = host.Rebuild(far, null, Location(PriceValueLocation.InsideValue), Utc(1));
        Assert.NotSame(a, b);
        Assert.NotEqual(a.ActiveCandidates[0].LocationGate, b.ActiveCandidates[0].LocationGate);
    }

    [Fact]
    public void F04_Same_location_returns_cached_instance()
    {
        var host = EnabledHost();
        var far = FarSet(Far(FarState.ReentryDeveloping));
        var loc = Location(PriceValueLocation.AboveValue);
        Assert.Same(host.Rebuild(far, null, loc, Utc()), host.Rebuild(far, null, loc, Utc(5)));
    }

    // ========== G: GPS card ==========

    [Fact]
    public void G01_Gps_shows_location_and_gate()
    {
        var set = EnabledHost().Rebuild(
            FarSet(Far(FarState.ReentryDeveloping)), null,
            Location(PriceValueLocation.AboveValue), Utc());
        var rows = AuctionGpsCardMapper.BuildSignalMaturityLines(set, false);
        Assert.Contains("LOCATION: ABOVEVALUE", rows);
        Assert.Contains("LOCATION GATE: ALLOWEDOUTSIDE", rows);
    }

    [Fact]
    public void G02_Gps_declares_target_space_veto_unavailable()
    {
        var set = EnabledHost().Rebuild(
            FarSet(Far(FarState.ReentryDeveloping)), null,
            Location(PriceValueLocation.AboveValue), Utc());
        Assert.Contains("TARGET SPACE VETO: NOT AVAILABLE",
            AuctionGpsCardMapper.BuildSignalMaturityLines(set, false));
    }

    [Fact]
    public void G03_Gps_shows_the_block_when_location_is_unavailable()
    {
        var set = EnabledHost().Rebuild(
            FarSet(Far(FarState.ReentryDeveloping)), null,
            Location(PriceValueLocation.Unavailable), Utc());
        var rows = AuctionGpsCardMapper.BuildSignalMaturityLines(set, false);
        Assert.Contains("LOCATION GATE: BLOCKEDLOCATIONUNAVAILABLE", rows);
        Assert.Contains("CANDIDATES: 0", rows);
    }
}
