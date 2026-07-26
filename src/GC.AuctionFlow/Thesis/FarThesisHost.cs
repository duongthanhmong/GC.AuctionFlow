using GC.AuctionFlow.Evidence;
using GC.AuctionFlow.Episode;

namespace GC.AuctionFlow.Thesis;

/// <summary>
/// Phase 3 FAR (Failed Auction Re-entry) Thesis host.
/// Consumes Phase 1F AcceptanceReentryEvidenceSetSnapshot immutably.
/// Observable states: Idle, EpisodeActive, OutsideAttempt, ReentryDeveloping.
/// Armed and beyond require calibrated thresholds — NOT CALIBRATED in Phase 3.
/// FAR Long: outside below reference, fails, re-enters above.
/// FAR Short: outside above reference, fails, re-enters below (symmetric).
/// Two-Attempt Failure variant tracked via AttemptCount — NOT CALIBRATED.
/// </summary>
public sealed class FarThesisHost
{
    private FarThesisPolicyConfig _policy;
    private FarThesisSetSnapshot? _published;
    private FarThesisInputFingerprint? _lastFingerprint;
    private DateTime _createdAtUtc;
    private readonly List<FarThesisSnapshot> _closedList = new();
    private readonly HashSet<string> _closedIds = new(StringComparer.Ordinal);

    public FarThesisHost(FarThesisPolicyConfig? policy = null)
    {
        _policy = policy ?? new FarThesisPolicyConfig(enabled: false);
    }

    public FarThesisSetSnapshot? Current => _published;
    public FarThesisPolicyConfig Policy => _policy;
    public FarThesisInputFingerprint? LastAppliedFingerprint => _lastFingerprint;

    public void Configure(FarThesisPolicyConfig policy)
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
    /// Rebuild FAR thesis observations from current evidence set.
    /// Fingerprint-gated: returns cached when evidence unchanged.
    /// All Armed/Executable/Managing/Completed states → NotCalibrated.
    /// </summary>
    public FarThesisSetSnapshot Rebuild(
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

        var active = new List<FarThesisSnapshot>(evidence.ActiveEvidence.Count);
        long maxRev = -1L;
        FarThesisSnapshot? latest = null;

        foreach (var ev in evidence.ActiveEvidence)
        {
            var snap = MapToFarThesis(ev, now);
            active.Add(snap);
            if (snap.EventRevision >= maxRev)
            {
                maxRev = snap.EventRevision;
                latest = snap;
            }
        }

        foreach (var ev in evidence.RecentlyClosedEvidence)
        {
            var thesisId = ThesisIdentity.BuildFarId(ev.EvidenceId);
            if (_closedIds.Add(thesisId))
            {
                _closedList.Add(MapToFarThesis(ev, now));
                if (_closedList.Count > FarThesisSetSnapshot.RecentlyClosedCapacity)
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

        _published = new FarThesisSetSnapshot(
            state,
            FarThesisPolicyConfig.PolicyVersion,
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

    private static FarThesisSnapshot MapToFarThesis(
        AcceptanceReentryEvidenceSnapshot ev, DateTime nowUtc)
    {
        var thesisId = ThesisIdentity.BuildFarId(ev.EvidenceId);
        var direction = DeriveDirection(ev);
        var (farState, notCalibrated) = DeriveFarState(ev);

        var quality = ev.DataQuality switch
        {
            EpisodeDataQuality.Invalid  => ThesisDataQuality.Invalid,
            EpisodeDataQuality.Partial  => ThesisDataQuality.Partial,
            _                           => ThesisDataQuality.Complete
        };

        var lim = new List<string>(ev.Limitations.Count + 6)
        {
            FarThesisPolicyConfig.LimitationNotCalibrated,
            FarThesisPolicyConfig.LimitationArmedNotCalibrated,
            FarThesisPolicyConfig.LimitationExecutableNotCalibrated,
            FarThesisPolicyConfig.LimitationReacceptedInsideNotCalibrated,
            FarThesisPolicyConfig.LimitationThesisNotAuthorized,
            FarThesisPolicyConfig.LimitationEntryNotAuthorized,
            FarThesisPolicyConfig.LimitationRiskNotAuthorized,
            FarThesisPolicyConfig.LimitationLiveOnly
        };
        foreach (var l in ev.Limitations)
            lim.Add(l);

        return new FarThesisSnapshot(
            thesisId,
            FarThesisPolicyConfig.PolicyVersion,
            ev.EvidenceId,
            ev.EpisodeId,
            ev.PrimaryAuctionId,
            ev.ReferenceId,
            direction,
            farState,
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
        // FAR Long: outside attempt BELOW reference → re-entering up (Long thesis)
        // FAR Short: outside attempt ABOVE reference → re-entering down (Short thesis)
        return ev.CanonicalOutsideDirection switch
        {
            ReferenceSidePosition.Below => ThesisDirection.Long,
            ReferenceSidePosition.Above => ThesisDirection.Short,
            _ => ThesisDirection.Unknown
        };
    }

    private static (FarState state, bool notCalibrated) DeriveFarState(
        AcceptanceReentryEvidenceSnapshot ev)
    {
        // Map from Phase 1F evidence observable states to FAR state machine.
        // ReacceptedInside and Armed+ require calibrated thresholds (Phase 2D StableReacceptance)
        // → always NotCalibrated in Phase 3.
        switch (ev.EpisodeState)
        {
            case EpisodeState.Interacting:
                return (FarState.EpisodeActive, false);

            case EpisodeState.OutsideAttempt:
            case EpisodeState.Developing:
                // Advance to ReentryDeveloping if re-entry observation confirms it.
                if (ev.ReentryObservationState == ReentryObservationState.GeometricReentry
                    || ev.ReentryObservationState == ReentryObservationState.Developing)
                    return (FarState.ReentryDeveloping, false);
                return (FarState.OutsideAttempt, false);

            case EpisodeState.ReentryDeveloping:
                // Observable maximum in Phase 3.
                // ReacceptedInside requires Phase 2D StableReacceptance (NotCalibrated).
                return (FarState.ReentryDeveloping, true);

            case EpisodeState.EpisodeExpired:
            case EpisodeState.InvalidData:
                return (FarState.Expired, false);

            default:
                return (FarState.Idle, false);
        }
    }

    // --- fingerprint ---

    private FarThesisInputFingerprint BuildFingerprint(
        AcceptanceReentryEvidenceSetSnapshot? evidence) =>
        new FarThesisInputFingerprint(
            _policy.Enabled,
            evidence?.RegistryRevision ?? -1L,
            evidence?.InputFingerprint ?? "",
            FarThesisPolicyConfig.PolicyVersion);

    // --- status helpers ---

    private void ResetInternal()
    {
        _closedList.Clear();
        _closedIds.Clear();
    }

    private static IReadOnlyList<string> BuildSetLimitations() => new[]
    {
        FarThesisPolicyConfig.LimitationNotCalibrated,
        FarThesisPolicyConfig.LimitationArmedNotCalibrated,
        FarThesisPolicyConfig.LimitationExecutableNotCalibrated,
        FarThesisPolicyConfig.LimitationManagingNotCalibrated,
        FarThesisPolicyConfig.LimitationCompletedNotCalibrated,
        FarThesisPolicyConfig.LimitationReacceptedInsideNotCalibrated,
        FarThesisPolicyConfig.LimitationTwoAttemptNotCalibrated,
        FarThesisPolicyConfig.LimitationThesisNotAuthorized,
        FarThesisPolicyConfig.LimitationEntryNotAuthorized,
        FarThesisPolicyConfig.LimitationRiskNotAuthorized,
        FarThesisPolicyConfig.LimitationLiveOnly
    };

    private FarThesisSetSnapshot DisabledSnapshot(DateTime now) =>
        new FarThesisSetSnapshot(
            ThesisModuleState.Disabled,
            FarThesisPolicyConfig.PolicyVersion,
            Array.Empty<FarThesisSnapshot>(),
            Array.Empty<FarThesisSnapshot>(),
            null, 0, 0, now, now,
            new[] { "MODULE_DISABLED" });

    private FarThesisSetSnapshot StatusSnapshot(
        ThesisModuleState state, DateTime now, string[]? extra = null)
    {
        if (_createdAtUtc == default) _createdAtUtc = now;
        var lim = new List<string>
        {
            FarThesisPolicyConfig.LimitationNotCalibrated,
            FarThesisPolicyConfig.LimitationThesisNotAuthorized,
            FarThesisPolicyConfig.LimitationLiveOnly
        };
        if (extra is not null) lim.AddRange(extra);
        return new FarThesisSetSnapshot(
            state,
            FarThesisPolicyConfig.PolicyVersion,
            Array.Empty<FarThesisSnapshot>(),
            _closedList.ToArray(),
            null, 0, 0,
            _createdAtUtc, now,
            lim);
    }
}
