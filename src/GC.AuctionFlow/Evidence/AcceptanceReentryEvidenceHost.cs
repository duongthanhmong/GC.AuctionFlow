using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Profile;

namespace GC.AuctionFlow.Evidence;

/// <summary>
/// Acceptance/Re-entry Evidence Measurement host.
/// Consumes Episode measurement events and snapshots. Never mutates Episode.
/// </summary>
public sealed class AcceptanceReentryEvidenceHost
{
    private AcceptanceReentryEvidencePolicyConfig _policy;
    private decimal _tickSize;
    private string _contractEpoch = "Unknown";
    private string _timestampPolicyVersion;
    private readonly AcceptanceReentryEvidenceRegistry _registry;
    private AcceptanceReentryEvidenceSetSnapshot? _published;
    private EvidenceInputFingerprint? _lastFingerprint;
    private DateTime _createdAtUtc;

    public AcceptanceReentryEvidenceHost(
        decimal tickSize,
        string contractEpoch,
        string? timestampPolicyVersion = null,
        AcceptanceReentryEvidencePolicyConfig? policy = null)
    {
        if (tickSize <= 0m) throw new ArgumentOutOfRangeException(nameof(tickSize));
        _tickSize = tickSize;
        _contractEpoch = string.IsNullOrWhiteSpace(contractEpoch) ? "Unknown" : contractEpoch;
        _timestampPolicyVersion = string.IsNullOrWhiteSpace(timestampPolicyVersion)
            ? AtasTimestampNormalizer.PolicyVersion
            : timestampPolicyVersion;
        _policy = policy ?? new AcceptanceReentryEvidencePolicyConfig(enabled: false);
        _registry = new AcceptanceReentryEvidenceRegistry();
    }

    public AcceptanceReentryEvidenceSetSnapshot? Current => _published;
    public EvidenceInputFingerprint? LastAppliedFingerprint => _lastFingerprint;
    public AcceptanceReentryEvidencePolicyConfig Policy => _policy;

    public void Configure(
        decimal tickSize,
        string contractEpoch,
        AcceptanceReentryEvidencePolicyConfig policy,
        string? timestampPolicyVersion = null)
    {
        if (tickSize <= 0m) throw new ArgumentOutOfRangeException(nameof(tickSize));
        if (policy is null) throw new ArgumentNullException(nameof(policy));
        _tickSize = tickSize;
        _contractEpoch = string.IsNullOrWhiteSpace(contractEpoch) ? "Unknown" : contractEpoch;
        _timestampPolicyVersion = string.IsNullOrWhiteSpace(timestampPolicyVersion)
            ? AtasTimestampNormalizer.PolicyVersion
            : timestampPolicyVersion;
        _policy = policy;
        if (!_policy.Enabled)
        {
            _registry.Reset();
            _published = DisabledSnapshot(DateTime.UtcNow);
            _lastFingerprint = null;
        }
    }

    public void Reset()
    {
        _registry.Reset();
        _published = null;
        _lastFingerprint = null;
        _createdAtUtc = default;
    }

    public AcceptanceReentryEvidenceSetSnapshot RebuildContext(
        AuctionEpisodeSetSnapshot? episodes,
        DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        var fingerprint = EvidenceInputFingerprint.Build(
            _policy.Enabled, episodes, _tickSize, _contractEpoch, _timestampPolicyVersion);

        if (!_policy.Enabled)
        {
            _registry.Reset();
            _published = DisabledSnapshot(now);
            _lastFingerprint = fingerprint;
            return _published;
        }

        if (!string.Equals(_timestampPolicyVersion, AtasTimestampNormalizer.PolicyVersion, StringComparison.Ordinal))
        {
            _published = StatusSnapshot(EvidenceModuleState.Invalid, fingerprint, now,
                new[] { "TIMESTAMP_POLICY_MISMATCH" });
            _lastFingerprint = fingerprint;
            return _published;
        }

        _registry.SyncFromEpisodeSet(episodes, now);
        _published = BuildSetSnapshot(fingerprint, now, episodes);
        _lastFingerprint = fingerprint;
        return _published;
    }

    public AcceptanceReentryEvidenceSetSnapshot ProcessMeasurementEvents(
        IReadOnlyList<EpisodeMeasurementEvent> events,
        AuctionEpisodeSetSnapshot? episodes,
        DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        if (!_policy.Enabled)
            return _published ?? DisabledSnapshot(now);

        var byId = episodes?.ActiveEpisodes
            .Concat(episodes.RecentlyClosedEpisodes ?? Array.Empty<AuctionEpisodeSnapshot>())
            .GroupBy(e => e.EpisodeId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal)
            ?? new Dictionary<string, AuctionEpisodeSnapshot>(StringComparer.Ordinal);

        foreach (var evt in events ?? Array.Empty<EpisodeMeasurementEvent>())
        {
            byId.TryGetValue(evt.EpisodeId, out var ep);
            _registry.ProcessMeasurementEvent(evt, ep, now);
        }

        var fingerprint = _lastFingerprint ?? EvidenceInputFingerprint.Build(
            true, episodes, _tickSize, _contractEpoch, _timestampPolicyVersion);
        _published = BuildSetSnapshot(fingerprint, now, episodes);
        return _published;
    }

    private AcceptanceReentryEvidenceSetSnapshot BuildSetSnapshot(
        EvidenceInputFingerprint fingerprint,
        DateTime now,
        AuctionEpisodeSetSnapshot? episodes)
    {
        if (_createdAtUtc == default)
            _createdAtUtc = now;

        var limitations = new List<string>
        {
            AcceptanceReentryEvidencePolicyConfig.LimitationHistoryLiveOnly,
            AcceptanceReentryEvidencePolicyConfig.LimitationStableReacceptanceNotCalibrated
        };

        var active = _registry.SnapshotActive(now);
        var closed = _registry.SnapshotClosed();

        EvidenceModuleState status;
        if (episodes is null
            || episodes.ModuleState is EpisodeModuleState.Disabled
                or EpisodeModuleState.AwaitingReferences
                or EpisodeModuleState.AwaitingTrades)
        {
            status = EvidenceModuleState.AwaitingEpisodes;
        }
        else if (active.Count == 0 && episodes.ActiveEpisodes.Count == 0)
        {
            status = EvidenceModuleState.AwaitingEpisodes;
        }
        else if (active.Count == 0 && episodes.ActiveEpisodes.Count > 0)
        {
            // Episodes exist but evidence not yet created from events — still awaiting measurement events.
            status = EvidenceModuleState.AwaitingEpisodes;
        }
        else
        {
            var anyCenterlineOnly = active.All(a => a.ReferenceRole == ReferenceInteractionRole.Centerline);
            var anyAggressorGap = active.Any(a =>
                a.Acceptance.AggressorEvidenceAvailability == AggressorEvidenceAvailability.Unavailable
                || a.Reentry.AggressorEvidenceAvailability == AggressorEvidenceAvailability.Unavailable);
            if (anyCenterlineOnly || anyAggressorGap)
            {
                status = EvidenceModuleState.Partial;
                if (anyCenterlineOnly)
                    limitations.Add(AcceptanceReentryEvidencePolicyConfig.LimitationCenterlineNotApplicable);
            }
            else
                status = EvidenceModuleState.Ready;
        }

        var accCounts = active.GroupBy(a => a.AcceptanceObservationState)
            .ToDictionary(g => g.Key, g => g.Count());
        var reCounts = active.GroupBy(a => a.ReentryObservationState)
            .ToDictionary(g => g.Key, g => g.Count());

        var available = 0;
        var unavailable = 0;
        foreach (var a in active)
        {
            foreach (var kv in a.EvidenceAvailability)
            {
                if (kv.Value == EvidenceComponentAvailability.Available) available++;
                else unavailable++;
            }
        }

        return new AcceptanceReentryEvidenceSetSnapshot(
            status,
            _policy.Version,
            _registry.PrimaryAuctionId,
            active,
            closed,
            _registry.LatestUpdated,
            accCounts,
            reCounts,
            available,
            unavailable,
            fingerprint.ToString(),
            _registry.RegistryRevision,
            _createdAtUtc,
            now,
            limitations.Distinct(StringComparer.Ordinal).ToArray());
    }

    private AcceptanceReentryEvidenceSetSnapshot DisabledSnapshot(DateTime now) =>
        StatusSnapshot(EvidenceModuleState.Disabled, default, now, new[] { "EVIDENCE_DISABLED" });

    private AcceptanceReentryEvidenceSetSnapshot StatusSnapshot(
        EvidenceModuleState status,
        EvidenceInputFingerprint fingerprint,
        DateTime now,
        IReadOnlyList<string> limitations)
    {
        if (_createdAtUtc == default)
            _createdAtUtc = now;
        return new AcceptanceReentryEvidenceSetSnapshot(
            status,
            _policy.Version,
            _registry.PrimaryAuctionId,
            Array.Empty<AcceptanceReentryEvidenceSnapshot>(),
            Array.Empty<AcceptanceReentryEvidenceSnapshot>(),
            null,
            new Dictionary<AcceptanceObservationState, int>(),
            new Dictionary<ReentryObservationState, int>(),
            0, 0,
            fingerprint.ToString(),
            _registry.RegistryRevision,
            _createdAtUtc,
            now,
            limitations);
    }
}
