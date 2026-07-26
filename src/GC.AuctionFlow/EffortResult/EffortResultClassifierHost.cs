using GC.AuctionFlow.Efficiency;

namespace GC.AuctionFlow.EffortResult;

/// <summary>
/// Phase 2E Effort vs Result Classifier host.
/// Consumes Phase 2C AuctionEfficiencyEvidenceSetSnapshot immutably.
/// All calibrated classification states are NOT_CALIBRATED — thresholds not yet determined.
/// FAR/AAC/Thesis/Entry/Risk: NOT AUTHORIZED in Phase 2E.
/// </summary>
public sealed class EffortResultClassifierHost
{
    private EffortResultClassifierPolicyConfig _policy;
    private EffortResultClassificationSetSnapshot? _published;
    private EffortResultInputFingerprint? _lastFingerprint;
    private DateTime _createdAtUtc;
    private readonly List<EffortResultClassificationSnapshot> _closedList = new();
    private readonly HashSet<string> _closedIds = new(StringComparer.Ordinal);

    public EffortResultClassifierHost(EffortResultClassifierPolicyConfig? policy = null)
    {
        _policy = policy ?? new EffortResultClassifierPolicyConfig(enabled: false);
    }

    public EffortResultClassificationSetSnapshot? Current => _published;
    public EffortResultClassifierPolicyConfig Policy => _policy;
    public EffortResultInputFingerprint? LastAppliedFingerprint => _lastFingerprint;

    public void Configure(EffortResultClassifierPolicyConfig policy)
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
    /// Rebuild classifier from current efficiency evidence set.
    /// Fingerprint-gated: returns cached when efficiency unchanged.
    /// All classification states → NotCalibrated (no thresholds calibrated in Phase 2E).
    /// </summary>
    public EffortResultClassificationSetSnapshot Rebuild(
        AuctionEfficiencyEvidenceSetSnapshot? efficiency,
        DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;

        if (!_policy.Enabled)
        {
            ResetInternal();
            _published = DisabledSnapshot(now);
            return _published;
        }

        var fp = BuildFingerprint(efficiency);
        if (_lastFingerprint.HasValue && _lastFingerprint.Value.Equals(fp) && _published is not null)
            return _published;

        if (efficiency is null || efficiency.ModuleState == EfficiencyModuleState.Disabled)
        {
            _published = StatusSnapshot(EffortResultModuleState.AwaitingEfficiency, now);
            _lastFingerprint = fp;
            return _published;
        }

        if (efficiency.ModuleState == EfficiencyModuleState.Invalid)
        {
            _published = StatusSnapshot(EffortResultModuleState.Invalid, now,
                new[] { "EFFICIENCY_INPUT_INVALID" });
            _lastFingerprint = fp;
            return _published;
        }

        if (_createdAtUtc == default)
            _createdAtUtc = now;

        // Map current auction evidence.
        EffortResultClassificationSnapshot? currentAuction = null;
        if (efficiency.CurrentAuctionEvidence is { } curEff)
            currentAuction = MapToClassification(curEff, now);

        // Map active episode evidence.
        var activeEpisodes = new List<EffortResultClassificationSnapshot>(efficiency.ActiveEpisodeEvidence.Count);
        long maxRev = -1L;
        EffortResultClassificationSnapshot? latest = currentAuction;
        int ready = 0, partial = 0;

        if (currentAuction is not null)
        {
            if (currentAuction.EventRevision >= maxRev)
            {
                maxRev = currentAuction.EventRevision;
                latest = currentAuction;
            }
            CountQuality(currentAuction, ref ready, ref partial);
        }

        foreach (var ep in efficiency.ActiveEpisodeEvidence)
        {
            var cls = MapToClassification(ep, now);
            activeEpisodes.Add(cls);
            if (cls.EventRevision >= maxRev)
            {
                maxRev = cls.EventRevision;
                latest = cls;
            }
            CountQuality(cls, ref ready, ref partial);
        }

        // Track recently-closed from efficiency closed list (capped, no duplicates).
        foreach (var ep in efficiency.RecentlyClosedEpisodeEvidence)
        {
            var clsId = EffortResultIdentity.BuildFromEfficiencyId(ep.SnapshotId);
            if (_closedIds.Add(clsId))
            {
                _closedList.Add(MapToClassification(ep, now));
                if (_closedList.Count > EffortResultClassificationSetSnapshot.RecentlyClosedCapacity)
                {
                    var removed = _closedList[0];
                    _closedList.RemoveAt(0);
                    _closedIds.Remove(removed.ClassificationId);
                }
            }
        }

        // Determine module state.
        EffortResultModuleState state;
        var hasAny = currentAuction is not null || activeEpisodes.Count > 0;
        if (!hasAny)
            state = EffortResultModuleState.AwaitingEfficiency;
        else if (partial > 0
                 || efficiency.ModuleState == EfficiencyModuleState.Partial
                 || efficiency.ModuleState == EfficiencyModuleState.AwaitingEpisode
                 || efficiency.ModuleState == EfficiencyModuleState.AwaitingOrderflow)
            state = EffortResultModuleState.Partial;
        else
            state = EffortResultModuleState.Ready;

        _published = new EffortResultClassificationSetSnapshot(
            state,
            EffortResultClassifierPolicyConfig.PolicyVersion,
            currentAuction,
            activeEpisodes,
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

    private static EffortResultClassificationSnapshot MapToClassification(
        AuctionEfficiencyEvidenceSnapshot eff, DateTime nowUtc)
    {
        // All classification states → NotCalibrated in Phase 2E.
        // None of the spec-defined states (EffortResultBalanced, AggressionEffective, etc.)
        // can be emitted until thresholds are explicitly calibrated.
        const EffortResultClassificationState classification = EffortResultClassificationState.NotCalibrated;

        var quality = eff.DataQuality switch
        {
            EfficiencyDataQuality.Invalid => EffortResultDataQuality.Invalid,
            EfficiencyDataQuality.Partial => EffortResultDataQuality.Partial,
            _                            => EffortResultDataQuality.Complete
        };

        var clsId = EffortResultIdentity.BuildFromEfficiencyId(eff.SnapshotId);

        var lim = new List<string>(eff.Limitations.Count + 8)
        {
            EffortResultClassifierPolicyConfig.LimitationNotCalibrated,
            EffortResultClassifierPolicyConfig.LimitationBalancedNotCalibrated,
            EffortResultClassifierPolicyConfig.LimitationEffectiveNotCalibrated,
            EffortResultClassifierPolicyConfig.LimitationIneffectiveNotCalibrated,
            EffortResultClassifierPolicyConfig.LimitationAbsorptionNotCalibrated,
            EffortResultClassifierPolicyConfig.LimitationExhaustionNotCalibrated,
            EffortResultClassifierPolicyConfig.LimitationFacilitationHealthyNotCalibrated,
            EffortResultClassifierPolicyConfig.LimitationFacilitationFailingNotCalibrated
        };
        foreach (var l in eff.Limitations)
            lim.Add(l);

        return new EffortResultClassificationSnapshot(
            clsId,
            EffortResultClassifierPolicyConfig.PolicyVersion,
            eff.SnapshotId,
            eff.PrimaryAuctionId,
            eff.EpisodeId,
            eff.ReferenceId,
            eff.ScopeType,
            classification,
            quality,
            eff.StateVersion,
            eff.EventRevision,
            nowUtc,
            lim);
    }

    private static void CountQuality(EffortResultClassificationSnapshot cls, ref int ready, ref int partial)
    {
        switch (cls.DataQuality)
        {
            case EffortResultDataQuality.Complete: ready++; break;
            case EffortResultDataQuality.Partial:  partial++; break;
        }
    }

    // --- fingerprint ---

    private EffortResultInputFingerprint BuildFingerprint(AuctionEfficiencyEvidenceSetSnapshot? efficiency) =>
        new EffortResultInputFingerprint(
            _policy.Enabled,
            efficiency?.InputFingerprint?.ToString() ?? "",
            EffortResultClassifierPolicyConfig.PolicyVersion);

    // --- status helpers ---

    private void ResetInternal()
    {
        _closedList.Clear();
        _closedIds.Clear();
    }

    private static IReadOnlyList<string> BuildSetLimitations() => new[]
    {
        EffortResultClassifierPolicyConfig.LimitationNotCalibrated,
        EffortResultClassifierPolicyConfig.LimitationBalancedNotCalibrated,
        EffortResultClassifierPolicyConfig.LimitationEffectiveNotCalibrated,
        EffortResultClassifierPolicyConfig.LimitationIneffectiveNotCalibrated,
        EffortResultClassifierPolicyConfig.LimitationAbsorptionNotCalibrated,
        EffortResultClassifierPolicyConfig.LimitationExhaustionNotCalibrated,
        EffortResultClassifierPolicyConfig.LimitationFacilitationHealthyNotCalibrated,
        EffortResultClassifierPolicyConfig.LimitationFacilitationFailingNotCalibrated,
        EffortResultClassifierPolicyConfig.LimitationLiveOnly,
        EffortResultClassifierPolicyConfig.LimitationNoFarAac,
        EffortResultClassifierPolicyConfig.LimitationNoThesisEntry,
        EffortResultClassifierPolicyConfig.LimitationNoOverlayAlerts
    };

    private EffortResultClassificationSetSnapshot DisabledSnapshot(DateTime now) =>
        new EffortResultClassificationSetSnapshot(
            EffortResultModuleState.Disabled,
            EffortResultClassifierPolicyConfig.PolicyVersion,
            null,
            Array.Empty<EffortResultClassificationSnapshot>(),
            Array.Empty<EffortResultClassificationSnapshot>(),
            null, 0, 0, now, now,
            new[] { "MODULE_DISABLED" });

    private EffortResultClassificationSetSnapshot StatusSnapshot(
        EffortResultModuleState state, DateTime now, string[]? extra = null)
    {
        if (_createdAtUtc == default) _createdAtUtc = now;
        var lim = new List<string>
        {
            EffortResultClassifierPolicyConfig.LimitationNotCalibrated,
            EffortResultClassifierPolicyConfig.LimitationLiveOnly
        };
        if (extra is not null) lim.AddRange(extra);
        return new EffortResultClassificationSetSnapshot(
            state,
            EffortResultClassifierPolicyConfig.PolicyVersion,
            null,
            Array.Empty<EffortResultClassificationSnapshot>(),
            _closedList.ToArray(),
            null, 0, 0,
            _createdAtUtc, now,
            lim);
    }
}
