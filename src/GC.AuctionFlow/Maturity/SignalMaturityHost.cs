using GC.AuctionFlow.Directional;
using GC.AuctionFlow.Plar;
using GC.AuctionFlow.Thesis;

namespace GC.AuctionFlow.Maturity;

/// <summary>
/// Phase 3B Signal Maturity host.
/// Consumes Phase 3A FAR and AAC thesis set snapshots immutably.
/// Derives analysis lifecycle position and declares the expected-behaviour contract.
/// Maturity level is always NotCalibrated — Fast/Standard/Confirmed thresholds
/// are not calibrated (v1.2 §29, v1.3 §9, Calibration Ledger v1.3 §14).
/// </summary>
public sealed class SignalMaturityHost
{
    private const string FamilyFar = "FAR";
    private const string FamilyAac = "AAC";

    private SignalMaturityPolicyConfig _policy;
    private SignalMaturitySetSnapshot? _published;
    private string? _lastFingerprintKey;
    private DateTime _createdAtUtc;
    private readonly List<SignalMaturitySnapshot> _closedList = new();
    private readonly HashSet<string> _closedIds = new(StringComparer.Ordinal);

    public SignalMaturityHost(SignalMaturityPolicyConfig? policy = null)
    {
        _policy = policy ?? new SignalMaturityPolicyConfig(enabled: false);
    }

    public SignalMaturitySetSnapshot? Current => _published;
    public SignalMaturityPolicyConfig Policy => _policy;

    public void Configure(SignalMaturityPolicyConfig policy)
    {
        if (policy is null) throw new ArgumentNullException(nameof(policy));
        _policy = policy;
        if (!_policy.Enabled)
        {
            ResetInternal();
            _published = DisabledSnapshot(DateTime.UtcNow);
            _lastFingerprintKey = null;
        }
    }

    public void Reset()
    {
        ResetInternal();
        _published = null;
        _lastFingerprintKey = null;
        _createdAtUtc = default;
    }

    /// <summary>
    /// Rebuild from current FAR and AAC thesis sets.
    /// Fingerprint-gated: returns cached when both thesis inputs are unchanged.
    /// </summary>
    public SignalMaturitySetSnapshot Rebuild(
        FarThesisSetSnapshot? far,
        AacThesisSetSnapshot? aac,
        ProfileLocationContextSnapshot? location = null,
        AuctionPathSnapshot? path = null,
        DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;

        if (!_policy.Enabled)
        {
            ResetInternal();
            _published = DisabledSnapshot(now);
            return _published;
        }

        var fp = BuildFingerprintKey(far, aac, location, path);
        if (_lastFingerprintKey is not null
            && string.Equals(_lastFingerprintKey, fp, StringComparison.Ordinal)
            && _published is not null)
            return _published;

        var farUsable = far is not null && far.ModuleState != ThesisModuleState.Disabled;
        var aacUsable = aac is not null && aac.ModuleState != ThesisModuleState.Disabled;

        if (!farUsable && !aacUsable)
        {
            _published = StatusSnapshot(MaturityModuleState.AwaitingThesis, now);
            _lastFingerprintKey = fp;
            return _published;
        }

        if ((far is not null && far.ModuleState == ThesisModuleState.Invalid)
            || (aac is not null && aac.ModuleState == ThesisModuleState.Invalid))
        {
            _published = StatusSnapshot(MaturityModuleState.Invalid, now,
                new[] { "THESIS_INPUT_INVALID" });
            _lastFingerprintKey = fp;
            return _published;
        }

        if (_createdAtUtc == default)
            _createdAtUtc = now;

        var loc = ResolveLocation(location);

        var active = new List<SignalMaturitySnapshot>();
        long maxRev = -1L;
        SignalMaturitySnapshot? latest = null;
        int candidates = 0, partial = 0;

        if (farUsable)
        {
            foreach (var t in far!.ActiveTheses)
                Accumulate(MapFar(t, loc, path, now), active, ref maxRev, ref latest, ref candidates, ref partial);
            foreach (var t in far.RecentlyClosedTheses)
                AccumulateClosed(MapFar(t, loc, path, now));
        }

        if (aacUsable)
        {
            foreach (var t in aac!.ActiveTheses)
                Accumulate(MapAac(t, loc, path, now), active, ref maxRev, ref latest, ref candidates, ref partial);
            foreach (var t in aac.RecentlyClosedTheses)
                AccumulateClosed(MapAac(t, loc, path, now));
        }

        MaturityModuleState state;
        if (active.Count == 0)
            state = MaturityModuleState.AwaitingThesis;
        else if (partial > 0
                 || (far is not null && far.ModuleState == ThesisModuleState.Partial)
                 || (aac is not null && aac.ModuleState == ThesisModuleState.Partial)
                 || (far is not null && far.ModuleState == ThesisModuleState.AwaitingEvidence)
                 || (aac is not null && aac.ModuleState == ThesisModuleState.AwaitingEvidence))
            state = MaturityModuleState.Partial;
        else
            state = MaturityModuleState.Ready;

        _published = new SignalMaturitySetSnapshot(
            state,
            SignalMaturityPolicyConfig.PolicyVersion,
            active,
            _closedList.ToArray(),
            latest,
            candidates,
            0, 0, 0,
            SignalMaturityPolicyConfig.FastShadowOnlyDefault,
            _createdAtUtc,
            now,
            BuildSetLimitations());

        _lastFingerprintKey = fp;
        return _published;
    }

    // --- mapping ---

    private static SignalMaturitySnapshot MapFar(
        FarThesisSnapshot t, PriceValueLocation location,
        AuctionPathSnapshot? path, DateTime nowUtc)
    {
        var (lifecycle, notCalibrated) = MapFarLifecycle(t.FarState);
        var expected = MapFarExpectedBehavior(t.FarState);
        return Build(t.ThesisId, FamilyFar, t.EvidenceId, t.EpisodeId, t.ReferenceId,
            t.Direction, lifecycle, expected, notCalibrated || t.NotCalibrated,
            location, path, MapQuality(t.DataQuality), t.StateVersion, t.EventRevision, nowUtc);
    }

    private static SignalMaturitySnapshot MapAac(
        AacThesisSnapshot t, PriceValueLocation location,
        AuctionPathSnapshot? path, DateTime nowUtc)
    {
        var (lifecycle, notCalibrated) = MapAacLifecycle(t.AacState);
        var expected = MapAacExpectedBehavior(t.AacState);
        return Build(t.ThesisId, FamilyAac, t.EvidenceId, t.EpisodeId, t.ReferenceId,
            t.Direction, lifecycle, expected, notCalibrated || t.NotCalibrated,
            location, path, MapQuality(t.DataQuality), t.StateVersion, t.EventRevision, nowUtc);
    }

    private static (AnalysisLifecycleState state, bool notCalibrated) MapFarLifecycle(FarState s) => s switch
    {
        FarState.Idle              => (AnalysisLifecycleState.Observation, false),
        FarState.Approaching       => (AnalysisLifecycleState.Approaching, false),
        FarState.EpisodeActive     => (AnalysisLifecycleState.EpisodeActive, false),
        FarState.OutsideAttempt    => (AnalysisLifecycleState.EpisodeActive, false),
        FarState.ReentryDeveloping => (AnalysisLifecycleState.Candidate, false),
        // ReacceptedInside would be Armed once maturity is calibrated — capped at Candidate.
        FarState.ReacceptedInside  => (AnalysisLifecycleState.Candidate, true),
        FarState.Invalidated       => (AnalysisLifecycleState.Invalidated, false),
        FarState.Expired           => (AnalysisLifecycleState.Expired, false),
        FarState.Completed         => (AnalysisLifecycleState.Completed, false),
        // Calibrated FAR states must never reach here in Phase 3.
        _                          => (AnalysisLifecycleState.NotCalibrated, true)
    };

    private static (AnalysisLifecycleState state, bool notCalibrated) MapAacLifecycle(AacState s) => s switch
    {
        AacState.Idle                 => (AnalysisLifecycleState.Observation, false),
        AacState.Approaching          => (AnalysisLifecycleState.Approaching, false),
        AacState.EpisodeActive        => (AnalysisLifecycleState.EpisodeActive, false),
        AacState.OutsideAttempt       => (AnalysisLifecycleState.EpisodeActive, false),
        AacState.AcceptanceDeveloping => (AnalysisLifecycleState.Candidate, false),
        // AcceptedOutside / Pullback would be Armed once calibrated — capped at Candidate.
        AacState.AcceptedOutside      => (AnalysisLifecycleState.Candidate, true),
        AacState.Pullback             => (AnalysisLifecycleState.Candidate, true),
        AacState.ReacceptedOldValue   => (AnalysisLifecycleState.Invalidated, false),
        AacState.Invalidated          => (AnalysisLifecycleState.Invalidated, false),
        AacState.Expired              => (AnalysisLifecycleState.Expired, false),
        AacState.Completed            => (AnalysisLifecycleState.Completed, false),
        // Calibrated AAC states must never reach here in Phase 3.
        _                             => (AnalysisLifecycleState.NotCalibrated, true)
    };

    private static ExpectedBehaviorContractKind MapFarExpectedBehavior(FarState s) => s switch
    {
        FarState.ReentryDeveloping => ExpectedBehaviorContractKind.FarReentry,
        FarState.ReacceptedInside  => ExpectedBehaviorContractKind.FarRetest,
        _                          => ExpectedBehaviorContractKind.Unknown
    };

    private static ExpectedBehaviorContractKind MapAacExpectedBehavior(AacState s) => s switch
    {
        AacState.AcceptanceDeveloping => ExpectedBehaviorContractKind.AacEarly,
        AacState.AcceptedOutside      => ExpectedBehaviorContractKind.NewValueContinuation,
        AacState.Pullback             => ExpectedBehaviorContractKind.AacRetest,
        _                             => ExpectedBehaviorContractKind.Unknown
    };

    private static MaturityDataQuality MapQuality(ThesisDataQuality q) => q switch
    {
        ThesisDataQuality.Invalid => MaturityDataQuality.Invalid,
        ThesisDataQuality.Partial => MaturityDataQuality.Partial,
        _                         => MaturityDataQuality.Complete
    };

    private static SignalMaturitySnapshot Build(
        string thesisId, string family, string evidenceId, string episodeId, string referenceId,
        ThesisDirection direction, AnalysisLifecycleState lifecycle,
        ExpectedBehaviorContractKind expected, bool notCalibrated,
        PriceValueLocation location,
        AuctionPathSnapshot? path,
        MaturityDataQuality quality, long stateVersion, long eventRevision, DateTime nowUtc)
    {
        // v1.3 §10 location gate, evaluated BEFORE the lifecycle is finalised.
        var gate = DeriveLocationGate(location, path);

        // G-LOC-003 / G-LOC-002: without a location there is no Context, and with no
        // room ahead there is nothing to trade toward. Either way there is no candidate.
        // The scope is still reported — silently dropping it would hide the block.
        if (gate is LocationGateOutcome.BlockedLocationUnavailable
                 or LocationGateOutcome.BlockedNoTargetSpace
            && lifecycle == AnalysisLifecycleState.Candidate)
            lifecycle = AnalysisLifecycleState.EpisodeActive;

        var blocking = new List<MaturityBlockingReason>
        {
            MaturityBlockingReason.ThresholdsNotCalibrated,
            MaturityBlockingReason.RetestDiscriminationNotCalibrated,
            MaturityBlockingReason.ExpectedBehaviorDeadlineNotCalibrated,
            MaturityBlockingReason.MicroConfirmationNotCalibrated,
            // TargetSpaceUnavailable is NOT unconditional any more: since Phase 3E the
            // PLAR path can actually measure the space, so the reason is added below
            // only when it genuinely cannot be measured.
            MaturityBlockingReason.FastShadowOnly
        };
        if (notCalibrated)
            blocking.Add(MaturityBlockingReason.ThesisStateNotCalibrated);
        if (gate == LocationGateOutcome.BlockedLocationUnavailable)
            blocking.Add(MaturityBlockingReason.PriceLocationUnavailable);
        if (gate == LocationGateOutcome.BlockedNoTargetSpace)
            blocking.Add(MaturityBlockingReason.NoRemainingTargetSpace);
        // G-LOC-001: mid-value / at-POC may exist but may never mature to Confirmed.
        if (gate == LocationGateOutcome.AllowedLowQuality)
            blocking.Add(MaturityBlockingReason.LowQualityLocation);

        var lim = new List<string>
        {
            SignalMaturityPolicyConfig.LimitationNotCalibrated,
            SignalMaturityPolicyConfig.LimitationFastNotCalibrated,
            SignalMaturityPolicyConfig.LimitationStandardNotCalibrated,
            SignalMaturityPolicyConfig.LimitationConfirmedNotCalibrated,
            SignalMaturityPolicyConfig.LimitationFastShadowOnly,
            SignalMaturityPolicyConfig.LimitationRetestNotCalibrated,
            SignalMaturityPolicyConfig.LimitationDeadlineNotCalibrated,
            SignalMaturityPolicyConfig.LimitationMicroConfirmationNotCalibrated,
            SignalMaturityPolicyConfig.LimitationTargetSpaceUnavailable,
            SignalMaturityPolicyConfig.LimitationLiveOnly,
        };
        // Only claim the veto is unmeasurable when it genuinely is (no PLAR path).
        if (path is null || path.TargetSpaceAvailability == TargetSpaceAvailability.Unavailable)
        {
            lim.Add(SignalMaturityPolicyConfig.LimitationTargetSpaceNotAvailable);
            blocking.Add(MaturityBlockingReason.TargetSpaceUnavailable);
        }
        if (gate == LocationGateOutcome.BlockedLocationUnavailable)
            lim.Add(SignalMaturityPolicyConfig.LimitationLocationUnavailable);
        if (gate == LocationGateOutcome.AllowedLowQuality)
            lim.Add(SignalMaturityPolicyConfig.LimitationLowQualityLocation);

        return new SignalMaturitySnapshot(
            BuildSnapshotId(thesisId),
            SignalMaturityPolicyConfig.PolicyVersion,
            thesisId,
            family,
            evidenceId,
            episodeId,
            referenceId,
            direction,
            lifecycle,
            SignalMaturityLevel.NotCalibrated,
            expected,
            // Deadline duration is NOT CALIBRATED — never fabricate one.
            null,
            // Micro vs structural retest discrimination is NOT CALIBRATED.
            RetestObservationState.NotCalibrated,
            location,
            gate,
            false,
            true,
            blocking,
            quality,
            stateVersion,
            eventRevision,
            nowUtc,
            lim);
    }

    // --- accumulation ---

    private void Accumulate(
        SignalMaturitySnapshot sm, List<SignalMaturitySnapshot> active,
        ref long maxRev, ref SignalMaturitySnapshot? latest,
        ref int candidates, ref int partial)
    {
        active.Add(sm);
        if (sm.EventRevision >= maxRev)
        {
            maxRev = sm.EventRevision;
            latest = sm;
        }
        if (sm.LifecycleState == AnalysisLifecycleState.Candidate)
            candidates++;
        if (sm.DataQuality == MaturityDataQuality.Partial)
            partial++;
    }

    private void AccumulateClosed(SignalMaturitySnapshot sm)
    {
        if (!_closedIds.Add(sm.SnapshotId)) return;
        _closedList.Add(sm);
        if (_closedList.Count > SignalMaturitySetSnapshot.RecentlyClosedCapacity)
        {
            var removed = _closedList[0];
            _closedList.RemoveAt(0);
            _closedIds.Remove(removed.SnapshotId);
        }
    }

    // --- helpers ---

    private static string BuildSnapshotId(string thesisId) => "SM:" + thesisId;

    private static string BuildFingerprintKey(
        FarThesisSetSnapshot? far, AacThesisSetSnapshot? aac,
        ProfileLocationContextSnapshot? location,
        AuctionPathSnapshot? path) =>
        SignalMaturityPolicyConfig.PolicyVersion
        + "|F:" + (far?.ModuleState.ToString() ?? "") + ":" + (far?.LastUpdatedAtUtc.Ticks.ToString() ?? "")
        + "|A:" + (aac?.ModuleState.ToString() ?? "") + ":" + (aac?.LastUpdatedAtUtc.Ticks.ToString() ?? "")
        + "|L:" + ResolveLocation(location).ToString()
        + "|T:" + (path?.TargetSpaceAvailability.ToString() ?? "")
        + ":" + (path?.RemainingTargetSpaceTicks?.ToString() ?? "");

    private static IReadOnlyList<string> BuildSetLimitations() => new[]
    {
        SignalMaturityPolicyConfig.LimitationNotCalibrated,
        SignalMaturityPolicyConfig.LimitationFastNotCalibrated,
        SignalMaturityPolicyConfig.LimitationStandardNotCalibrated,
        SignalMaturityPolicyConfig.LimitationConfirmedNotCalibrated,
        SignalMaturityPolicyConfig.LimitationFastShadowOnly,
        SignalMaturityPolicyConfig.LimitationDeadlineNotCalibrated,
        SignalMaturityPolicyConfig.LimitationLiveOnly,
        SignalMaturityPolicyConfig.LimitationNoEntryPlan,
        SignalMaturityPolicyConfig.LimitationNoRiskSizing
    };

    private void ResetInternal()
    {
        _closedList.Clear();
        _closedIds.Clear();
    }

    private SignalMaturitySetSnapshot DisabledSnapshot(DateTime now) =>
        new SignalMaturitySetSnapshot(
            MaturityModuleState.Disabled,
            SignalMaturityPolicyConfig.PolicyVersion,
            Array.Empty<SignalMaturitySnapshot>(),
            Array.Empty<SignalMaturitySnapshot>(),
            null, 0, 0, 0, 0,
            SignalMaturityPolicyConfig.FastShadowOnlyDefault,
            now, now,
            new[] { "MODULE_DISABLED" });

    private SignalMaturitySetSnapshot StatusSnapshot(
        MaturityModuleState state, DateTime now, string[]? extra = null)
    {
        if (_createdAtUtc == default) _createdAtUtc = now;
        var lim = new List<string>
        {
            SignalMaturityPolicyConfig.LimitationNotCalibrated,
            SignalMaturityPolicyConfig.LimitationFastShadowOnly,
            SignalMaturityPolicyConfig.LimitationLiveOnly
        };
        if (extra is not null) lim.AddRange(extra);
        return new SignalMaturitySetSnapshot(
            state,
            SignalMaturityPolicyConfig.PolicyVersion,
            Array.Empty<SignalMaturitySnapshot>(),
            _closedList.ToArray(),
            null, 0, 0, 0, 0,
            SignalMaturityPolicyConfig.FastShadowOnlyDefault,
            _createdAtUtc, now,
            lim);
    }

    /// <summary>
    /// Which of the five available locations the gate uses.
    /// The current primary auction is what a live thesis is transacting in, and the
    /// volume profile is preferred over TPO because it reflects executed activity
    /// rather than time distribution.
    /// </summary>
    private static PriceValueLocation ResolveLocation(ProfileLocationContextSnapshot? location)
    {
        if (location is null) return PriceValueLocation.Unavailable;
        return location.CurrentPrimaryVolume != PriceValueLocation.Unavailable
            ? location.CurrentPrimaryVolume
            : location.CurrentPrimaryTpo;
    }

    /// <summary>
    /// v1.3 §10 location gate. Purely positional — no threshold, no direction.
    /// BlockedNoTargetSpace (G-LOC-002) is unreachable until the Target Engine exists.
    /// </summary>
    private static LocationGateOutcome DeriveLocationGate(
        PriceValueLocation location, AuctionPathSnapshot? path)
    {
        // G-LOC-002: no room ahead is a hard veto, and it outranks position quality —
        // a perfect location with nowhere to go is still not tradeable.
        // NoTargetAhead is a measured fact; Unavailable means we could not measure and
        // must NOT be treated as a pass.
        if (path?.TargetSpaceAvailability == TargetSpaceAvailability.NoTargetAhead)
            return LocationGateOutcome.BlockedNoTargetSpace;

        return MapPosition(location);
    }

    private static LocationGateOutcome MapPosition(PriceValueLocation location) => location switch
    {
        PriceValueLocation.Unavailable => LocationGateOutcome.BlockedLocationUnavailable,
        PriceValueLocation.InsideValue => LocationGateOutcome.AllowedLowQuality,
        PriceValueLocation.AtPoc       => LocationGateOutcome.AllowedLowQuality,
        PriceValueLocation.AtValueHigh => LocationGateOutcome.AllowedBoundary,
        PriceValueLocation.AtValueLow  => LocationGateOutcome.AllowedBoundary,
        PriceValueLocation.AboveValue  => LocationGateOutcome.AllowedOutside,
        PriceValueLocation.BelowValue  => LocationGateOutcome.AllowedOutside,
        _                              => LocationGateOutcome.Unknown
    };
}
