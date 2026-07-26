using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Evidence;

namespace GC.AuctionFlow.Resolution;

/// <summary>
/// Phase 2D Acceptance/Re-entry Resolution host.
/// Consumes Phase 1F AcceptanceReentryEvidenceSetSnapshot immutably.
/// Maps observation states to resolution states; all calibrated conclusions are NOT_CALIBRATED.
/// FAR/AAC/Thesis/Entry/Risk: NOT AUTHORIZED in Phase 2D.
/// </summary>
public sealed class AuctionResolutionHost
{
    private AuctionResolutionPolicyConfig _policy;
    private AuctionResolutionSetSnapshot? _published;
    private readonly List<AuctionResolutionSnapshot> _closedList = new();
    private readonly HashSet<string> _closedIds = new(StringComparer.Ordinal);
    private DateTime _createdAtUtc;
    private ResolutionInputFingerprint? _lastFingerprint;

    public AuctionResolutionHost(AuctionResolutionPolicyConfig? policy = null)
    {
        _policy = policy ?? new AuctionResolutionPolicyConfig(enabled: false);
    }

    public AuctionResolutionSetSnapshot? Current => _published;
    public AuctionResolutionPolicyConfig Policy => _policy;
    public ResolutionInputFingerprint? LastAppliedFingerprint => _lastFingerprint;

    /// <summary>
    /// Reconfigure policy. When disabled, clears state and publishes a Disabled snapshot.
    /// </summary>
    public void Configure(AuctionResolutionPolicyConfig policy)
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

    /// <summary>Hard reset — clears all state and published snapshot.</summary>
    public void Reset()
    {
        ResetInternal();
        _published = null;
        _lastFingerprint = null;
        _createdAtUtc = default;
    }

    /// <summary>
    /// Rebuild resolution from current evidence snapshot.
    /// Fingerprint-gated: returns cached snapshot when evidence unchanged.
    /// </summary>
    public AuctionResolutionSetSnapshot Rebuild(
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

        // Fingerprint check — skip rebuild if evidence unchanged.
        var fp = BuildFingerprint(evidence);
        if (_lastFingerprint.HasValue && _lastFingerprint.Value.Equals(fp) && _published is not null)
            return _published;

        if (evidence is null || evidence.ModuleState == EvidenceModuleState.Disabled)
        {
            _published = StatusSnapshot(ResolutionModuleState.AwaitingEvidence, now);
            _lastFingerprint = fp;
            return _published;
        }

        if (evidence.ModuleState == EvidenceModuleState.Invalid)
        {
            _published = StatusSnapshot(ResolutionModuleState.Invalid, now,
                new[] { "EVIDENCE_INPUT_INVALID" });
            _lastFingerprint = fp;
            return _published;
        }

        // Map active evidence → resolution snapshots.
        var active = new List<AuctionResolutionSnapshot>(evidence.ActiveEvidence.Count);
        long maxRev = -1L;
        AuctionResolutionSnapshot? latest = null;
        int ready = 0, partial = 0;

        foreach (var ev in evidence.ActiveEvidence)
        {
            var res = MapToResolution(ev, now);
            active.Add(res);
            if (res.EventRevision >= maxRev)
            {
                maxRev = res.EventRevision;
                latest = res;
            }
            switch (res.DataQuality)
            {
                case ResolutionDataQuality.Complete: ready++; break;
                case ResolutionDataQuality.Partial: partial++; break;
            }
        }

        // Track recently-closed from evidence recently-closed list (capped, no duplicates).
        foreach (var ev in evidence.RecentlyClosedEvidence)
        {
            var resId = ResolutionIdentity.BuildFromEvidenceId(ev.EvidenceId);
            if (_closedIds.Add(resId))
            {
                _closedList.Add(MapToResolution(ev, now));
                if (_closedList.Count > AuctionResolutionSetSnapshot.RecentlyClosedCapacity)
                {
                    var removed = _closedList[0];
                    _closedList.RemoveAt(0);
                    _closedIds.Remove(removed.ResolutionId);
                }
            }
        }

        // Determine module state.
        ResolutionModuleState state;
        if (active.Count == 0)
            state = ResolutionModuleState.AwaitingEvidence;
        else if (partial > 0 || evidence.ModuleState == EvidenceModuleState.Partial)
            state = ResolutionModuleState.Partial;
        else
            state = ResolutionModuleState.Ready;

        if (_createdAtUtc == default)
            _createdAtUtc = now;

        _published = new AuctionResolutionSetSnapshot(
            state,
            AuctionResolutionPolicyConfig.PolicyVersion,
            active,
            _closedList.ToArray(),
            latest,
            ready,
            partial,
            _createdAtUtc,
            now,
            BuildSetLimitations());

        _lastFingerprint = fp;
        return _published;
    }

    // --- mapping ---

    private static AuctionResolutionSnapshot MapToResolution(
        AcceptanceReentryEvidenceSnapshot ev, DateTime nowUtc)
    {
        // Acceptance: Early/Developing pass through; Unresolved → NotCalibrated; Established/Failed never
        // emitted by Phase 1F but handle defensively as NotCalibrated.
        var accRes = ev.AcceptanceObservationState switch
        {
            AcceptanceObservationState.None         => AcceptanceResolutionState.None,
            AcceptanceObservationState.Early        => AcceptanceResolutionState.Early,
            AcceptanceObservationState.Developing   => AcceptanceResolutionState.Developing,
            AcceptanceObservationState.Unresolved   => AcceptanceResolutionState.NotCalibrated,
            AcceptanceObservationState.Unknown      => AcceptanceResolutionState.Unknown,
            AcceptanceObservationState.Established  => AcceptanceResolutionState.NotCalibrated,  // reserved path
            AcceptanceObservationState.Failed       => AcceptanceResolutionState.NotCalibrated,  // reserved path
            _                                       => AcceptanceResolutionState.Unknown
        };

        // Re-entry: GeometricReentry/Developing pass through; Unresolved → NotCalibrated.
        var reentryRes = ev.ReentryObservationState switch
        {
            ReentryObservationState.None              => ReentryResolutionState.None,
            ReentryObservationState.GeometricReentry  => ReentryResolutionState.GeometricReentry,
            ReentryObservationState.Developing        => ReentryResolutionState.Developing,
            ReentryObservationState.Unresolved        => ReentryResolutionState.NotCalibrated,
            ReentryObservationState.Unknown           => ReentryResolutionState.Unknown,
            ReentryObservationState.StableReaccepted  => ReentryResolutionState.NotCalibrated,   // reserved path
            ReentryObservationState.ReentryFailed     => ReentryResolutionState.NotCalibrated,   // reserved path
            _                                         => ReentryResolutionState.Unknown
        };

        // FAR/AAC overall conclusion: always NotCalibrated in Phase 2D — calibrated thresholds required.
        const AuctionResolutionConclusion conclusion = AuctionResolutionConclusion.NotCalibrated;

        var quality = ev.DataQuality switch
        {
            EpisodeDataQuality.Invalid  => ResolutionDataQuality.Invalid,
            EpisodeDataQuality.Partial  => ResolutionDataQuality.Partial,
            _                           => ResolutionDataQuality.Complete
        };

        var resId = ResolutionIdentity.BuildFromEvidenceId(ev.EvidenceId);

        var lim = new List<string>(ev.Limitations.Count + 6)
        {
            AuctionResolutionPolicyConfig.LimitationNotCalibrated,
            AuctionResolutionPolicyConfig.LimitationEstablishedNotCalibrated,
            AuctionResolutionPolicyConfig.LimitationFailedNotCalibrated,
            AuctionResolutionPolicyConfig.LimitationStableReentryNotCalibrated,
            AuctionResolutionPolicyConfig.LimitationReentryFailedNotCalibrated,
            AuctionResolutionPolicyConfig.LimitationFarNotCalibrated,
            AuctionResolutionPolicyConfig.LimitationAacNotCalibrated
        };
        foreach (var l in ev.Limitations)
            lim.Add(l);

        return new AuctionResolutionSnapshot(
            resId,
            AuctionResolutionPolicyConfig.PolicyVersion,
            ev.EvidenceId,
            ev.EpisodeId,
            ev.PrimaryAuctionId,
            ev.ReferenceId,
            accRes,
            reentryRes,
            conclusion,
            ev.AcceptanceObservationState,
            ev.ReentryObservationState,
            ev.StateVersion,
            ev.EventRevision,
            quality,
            nowUtc,
            lim);
    }

    // --- fingerprint ---

    private ResolutionInputFingerprint BuildFingerprint(AcceptanceReentryEvidenceSetSnapshot? evidence) =>
        new ResolutionInputFingerprint(
            _policy.Enabled,
            evidence?.RegistryRevision ?? -1L,
            evidence?.InputFingerprint ?? "",
            AuctionResolutionPolicyConfig.PolicyVersion);

    // --- status helpers ---

    private void ResetInternal()
    {
        _closedList.Clear();
        _closedIds.Clear();
    }

    private static IReadOnlyList<string> BuildSetLimitations() => new[]
    {
        AuctionResolutionPolicyConfig.LimitationNotCalibrated,
        AuctionResolutionPolicyConfig.LimitationEstablishedNotCalibrated,
        AuctionResolutionPolicyConfig.LimitationFailedNotCalibrated,
        AuctionResolutionPolicyConfig.LimitationStableReentryNotCalibrated,
        AuctionResolutionPolicyConfig.LimitationReentryFailedNotCalibrated,
        AuctionResolutionPolicyConfig.LimitationFarNotCalibrated,
        AuctionResolutionPolicyConfig.LimitationAacNotCalibrated,
        AuctionResolutionPolicyConfig.LimitationLiveOnly,
        AuctionResolutionPolicyConfig.LimitationNoFarAac,
        AuctionResolutionPolicyConfig.LimitationNoThesisEntry,
        AuctionResolutionPolicyConfig.LimitationNoOverlayAlerts,
        AuctionResolutionPolicyConfig.LimitationOutsideCloseRatioUnavailable,
        AuctionResolutionPolicyConfig.LimitationTpoOutsideCountUnavailable,
        AuctionResolutionPolicyConfig.LimitationLocalValueRebuildUnavailable,
        AuctionResolutionPolicyConfig.LimitationOppositeAggressionNotAuthorized
    };

    private AuctionResolutionSetSnapshot DisabledSnapshot(DateTime now) =>
        new AuctionResolutionSetSnapshot(
            ResolutionModuleState.Disabled,
            AuctionResolutionPolicyConfig.PolicyVersion,
            Array.Empty<AuctionResolutionSnapshot>(),
            Array.Empty<AuctionResolutionSnapshot>(),
            null, 0, 0, now, now,
            new[] { "MODULE_DISABLED" });

    private AuctionResolutionSetSnapshot StatusSnapshot(
        ResolutionModuleState state, DateTime now, string[]? extra = null)
    {
        if (_createdAtUtc == default) _createdAtUtc = now;
        var lim = new List<string>
        {
            AuctionResolutionPolicyConfig.LimitationNotCalibrated,
            AuctionResolutionPolicyConfig.LimitationLiveOnly
        };
        if (extra is not null) lim.AddRange(extra);
        return new AuctionResolutionSetSnapshot(
            state,
            AuctionResolutionPolicyConfig.PolicyVersion,
            Array.Empty<AuctionResolutionSnapshot>(),
            _closedList.ToArray(),
            null, 0, 0,
            _createdAtUtc, now,
            lim);
    }
}
