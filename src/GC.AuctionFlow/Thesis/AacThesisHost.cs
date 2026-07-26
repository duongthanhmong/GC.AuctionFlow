using GC.AuctionFlow.Evidence;
using GC.AuctionFlow.Episode;

namespace GC.AuctionFlow.Thesis;

/// <summary>
/// Phase 3 AAC (Acceptance-Continuation) Thesis host.
/// Consumes Phase 1F AcceptanceReentryEvidenceSetSnapshot immutably.
/// Observable states: Idle, EpisodeActive, OutsideAttempt, Pullback.
/// AcceptanceDeveloping and beyond require calibrated thresholds — NOT CALIBRATED in Phase 3.
/// AAC Long: break above reference, sustained outside, continuation upward.
/// AAC Short: break below reference, sustained outside, continuation downward.
/// Re-entry (ReentryDeveloping) → Pullback + NotCalibrated. Geometric re-entry alone does NOT
/// invalidate AAC: invalidation requires StableReacceptance, which is calibration-gated
/// (v1.3 §8.2, G-AAC-001; KDK Ch 65 "một bóng nến quay vào chưa đủ").
/// </summary>
public sealed class AacThesisHost
{
    private AacThesisPolicyConfig _policy;
    private AacThesisSetSnapshot? _published;
    private AacThesisInputFingerprint? _lastFingerprint;
    private DateTime _createdAtUtc;
    private readonly List<AacThesisSnapshot> _closedList = new();
    private readonly HashSet<string> _closedIds = new(StringComparer.Ordinal);

    public AacThesisHost(AacThesisPolicyConfig? policy = null)
    {
        _policy = policy ?? new AacThesisPolicyConfig(enabled: false);
    }

    public AacThesisSetSnapshot? Current => _published;
    public AacThesisPolicyConfig Policy => _policy;
    public AacThesisInputFingerprint? LastAppliedFingerprint => _lastFingerprint;

    public void Configure(AacThesisPolicyConfig policy)
    {
        if (policy is null) throw new ArgumentNullException(nameof(policy));
        _policy = policy;
        if (!_policy.Enabled)
        {
            ResetInternal();
            _published = DisabledSnapshot(DateTime.UtcNow);
            _lastFingerprint = null;
        }
    }

    public void Reset()
    {
        ResetInternal();
        _published = null;
        _lastFingerprint = null;
        _createdAtUtc = default;
    }

    /// <summary>
    /// Rebuild AAC thesis observations from current evidence set.
    /// Fingerprint-gated: returns cached when evidence unchanged.
    /// All AcceptanceDeveloping/AcceptedOutside/Pullback/Armed+ states → NotCalibrated.
    /// ReentryDeveloping → Pullback + NotCalibrated (geometric re-entry is not reacceptance).
    /// </summary>
    public AacThesisSetSnapshot Rebuild(
        AcceptanceReentryEvidenceSetSnapshot? evidence,
        DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;

        if (!_policy.Enabled)
        {
            ResetInternal();
            _published = DisabledSnapshot(now);
            return _published;
        }

        var fp = BuildFingerprint(evidence);
        if (_lastFingerprint.HasValue && _lastFingerprint.Value.Equals(fp) && _published is not null)
            return _published;

        if (evidence is null || evidence.ModuleState == EvidenceModuleState.Disabled)
        {
            _published = StatusSnapshot(ThesisModuleState.AwaitingEvidence, now);
            _lastFingerprint = fp;
            return _published;
        }

        if (evidence.ModuleState == EvidenceModuleState.Invalid)
        {
            _published = StatusSnapshot(ThesisModuleState.Invalid, now,
                new[] { "EVIDENCE_INPUT_INVALID" });
            _lastFingerprint = fp;
            return _published;
        }

        if (_createdAtUtc == default)
            _createdAtUtc = now;

        var active = new List<AacThesisSnapshot>(evidence.ActiveEvidence.Count);
        long maxRev = -1L;
        AacThesisSnapshot? latest = null;

        foreach (var ev in evidence.ActiveEvidence)
        {
            var snap = MapToAacThesis(ev, now);
            active.Add(snap);
            if (snap.EventRevision >= maxRev)
            {
                maxRev = snap.EventRevision;
                latest = snap;
            }
        }

        foreach (var ev in evidence.RecentlyClosedEvidence)
        {
            var thesisId = ThesisIdentity.BuildAacId(ev.EvidenceId);
            if (_closedIds.Add(thesisId))
            {
                _closedList.Add(MapToAacThesis(ev, now));
                if (_closedList.Count > AacThesisSetSnapshot.RecentlyClosedCapacity)
                {
                    var removed = _closedList[0];
                    _closedList.RemoveAt(0);
                    _closedIds.Remove(removed.ThesisId);
                }
            }
        }

        var state = active.Count == 0
            ? ThesisModuleState.AwaitingEvidence
            : evidence.ModuleState == EvidenceModuleState.Partial
                ? ThesisModuleState.Partial
                : ThesisModuleState.Ready;

        _published = new AacThesisSetSnapshot(
            state,
            AacThesisPolicyConfig.PolicyVersion,
            active,
            _closedList.ToArray(),
            latest,
            armableCount: 0,
            executableCount: 0,
            _createdAtUtc,
            now,
            BuildSetLimitations());

        _lastFingerprint = fp;
        return _published;
    }

    // --- mapping ---

    private static AacThesisSnapshot MapToAacThesis(
        AcceptanceReentryEvidenceSnapshot ev, DateTime nowUtc)
    {
        var thesisId = ThesisIdentity.BuildAacId(ev.EvidenceId);
        var direction = DeriveDirection(ev);
        var (aacState, notCalibrated) = DeriveAacState(ev);

        var quality = ev.DataQuality switch
        {
            EpisodeDataQuality.Invalid  => ThesisDataQuality.Invalid,
            EpisodeDataQuality.Partial  => ThesisDataQuality.Partial,
            _                           => ThesisDataQuality.Complete
        };

        var lim = new List<string>(ev.Limitations.Count + 7)
        {
            AacThesisPolicyConfig.LimitationNotCalibrated,
            AacThesisPolicyConfig.LimitationArmedNotCalibrated,
            AacThesisPolicyConfig.LimitationExecutableNotCalibrated,
            AacThesisPolicyConfig.LimitationAcceptedOutsideNotCalibrated,
            AacThesisPolicyConfig.LimitationPullbackNotCalibrated,
            AacThesisPolicyConfig.LimitationThesisNotAuthorized,
            AacThesisPolicyConfig.LimitationEntryNotAuthorized,
            AacThesisPolicyConfig.LimitationRiskNotAuthorized,
            AacThesisPolicyConfig.LimitationLiveOnly
        };
        foreach (var l in ev.Limitations)
            lim.Add(l);

        return new AacThesisSnapshot(
            thesisId,
            AacThesisPolicyConfig.PolicyVersion,
            ev.EvidenceId,
            ev.EpisodeId,
            ev.PrimaryAuctionId,
            ev.ReferenceId,
            direction,
            aacState,
            notCalibrated,
            attemptCount: 0,
            quality,
            ev.StateVersion,
            ev.EventRevision,
            nowUtc,
            lim);
    }

    private static ThesisDirection DeriveDirection(AcceptanceReentryEvidenceSnapshot ev)
    {
        // AAC Long: break ABOVE reference → continuation upward = Long thesis
        // AAC Short: break BELOW reference → continuation downward = Short thesis
        // Opposite of FAR (which re-enters back inside).
        return ev.CanonicalOutsideDirection switch
        {
            ReferenceSidePosition.Above => ThesisDirection.Long,
            ReferenceSidePosition.Below => ThesisDirection.Short,
            _ => ThesisDirection.Unknown
        };
    }

    private static (AacState state, bool notCalibrated) DeriveAacState(
        AcceptanceReentryEvidenceSnapshot ev)
    {
        // Map from Phase 1F evidence observable states to AAC state machine.
        // AcceptanceDeveloping and beyond require calibrated acceptance thresholds.
        // Re-entry into old value (ReentryDeveloping) → AAC thesis invalidated.
        switch (ev.EpisodeState)
        {
            case EpisodeState.Interacting:
                return (AacState.EpisodeActive, false);

            case EpisodeState.OutsideAttempt:
            case EpisodeState.Developing:
                // AcceptanceDeveloping state requires calibrated volume/time thresholds.
                // In Phase 3: remain at OutsideAttempt; flag NotCalibrated if acceptance is building.
                if (ev.AcceptanceObservationState >= AcceptanceObservationState.Developing)
                    return (AacState.OutsideAttempt, true);
                return (AacState.OutsideAttempt, false);

            case EpisodeState.ReentryDeveloping:
                // Price has geometrically re-entered old value. This is NOT yet reacceptance:
                // AAC invalidation requires ReentryResolutionState.StableReacceptance, which is
                // calibration-gated. Report Pullback and flag NotCalibrated rather than closing
                // the thesis on geometry alone (v1.3 G-DISC-002 / G-AAC-001).
                return (AacState.Pullback, true);

            case EpisodeState.EpisodeExpired:
            case EpisodeState.InvalidData:
                return (AacState.Expired, false);

            default:
                return (AacState.Idle, false);
        }
    }

    // --- fingerprint ---

    private AacThesisInputFingerprint BuildFingerprint(
        AcceptanceReentryEvidenceSetSnapshot? evidence) =>
        new AacThesisInputFingerprint(
            _policy.Enabled,
            evidence?.RegistryRevision ?? -1L,
            evidence?.InputFingerprint ?? "",
            AacThesisPolicyConfig.PolicyVersion);

    // --- status helpers ---

    private void ResetInternal()
    {
        _closedList.Clear();
        _closedIds.Clear();
    }

    private static IReadOnlyList<string> BuildSetLimitations() => new[]
    {
        AacThesisPolicyConfig.LimitationNotCalibrated,
        AacThesisPolicyConfig.LimitationArmedNotCalibrated,
        AacThesisPolicyConfig.LimitationExecutableNotCalibrated,
        AacThesisPolicyConfig.LimitationManagingNotCalibrated,
        AacThesisPolicyConfig.LimitationCompletedNotCalibrated,
        AacThesisPolicyConfig.LimitationAcceptedOutsideNotCalibrated,
        AacThesisPolicyConfig.LimitationPullbackNotCalibrated,
        AacThesisPolicyConfig.LimitationThesisNotAuthorized,
        AacThesisPolicyConfig.LimitationEntryNotAuthorized,
        AacThesisPolicyConfig.LimitationRiskNotAuthorized,
        AacThesisPolicyConfig.LimitationLiveOnly
    };

    private AacThesisSetSnapshot DisabledSnapshot(DateTime now) =>
        new AacThesisSetSnapshot(
            ThesisModuleState.Disabled,
            AacThesisPolicyConfig.PolicyVersion,
            Array.Empty<AacThesisSnapshot>(),
            Array.Empty<AacThesisSnapshot>(),
            null, 0, 0, now, now,
            new[] { "MODULE_DISABLED" });

    private AacThesisSetSnapshot StatusSnapshot(
        ThesisModuleState state, DateTime now, string[]? extra = null)
    {
        if (_createdAtUtc == default) _createdAtUtc = now;
        var lim = new List<string>
        {
            AacThesisPolicyConfig.LimitationNotCalibrated,
            AacThesisPolicyConfig.LimitationThesisNotAuthorized,
            AacThesisPolicyConfig.LimitationLiveOnly
        };
        if (extra is not null) lim.AddRange(extra);
        return new AacThesisSetSnapshot(
            state,
            AacThesisPolicyConfig.PolicyVersion,
            Array.Empty<AacThesisSnapshot>(),
            _closedList.ToArray(),
            null, 0, 0,
            _createdAtUtc, now,
            lim);
    }
}
