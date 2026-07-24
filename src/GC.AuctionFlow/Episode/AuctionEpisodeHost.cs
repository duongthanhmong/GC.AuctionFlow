using GC.AuctionFlow.Directional;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Reference;

namespace GC.AuctionFlow.Episode;

/// <summary>
/// Auction Episode Observation host. Consumes Confirmed References + live trades.
/// Profile/Composite/Reference/Directional must not consume Episode state.
/// </summary>
public sealed class AuctionEpisodeHost
{
    private EpisodePolicyConfig _policy;
    private decimal _tickSize;
    private string _contractIdentity = "Unknown";
    private string _contractEpoch = "Unknown";
    private string _timestampPolicyVersion;
    private AuctionEpisodeRegistry _registry;
    private AuctionEpisodeSetSnapshot? _published;
    private EpisodeInputFingerprint? _lastFingerprint;
    private DateTime _createdAtUtc;
    private IReadOnlyList<StructuralReferenceSnapshot> _eligible = Array.Empty<StructuralReferenceSnapshot>();
    private string? _directionalProvenance;
    private string _tradeStreamEpoch = "live";

    private long _tradeEventsSeen;
    private long _tradeEventsAccepted;
    private long _tradeEventsDuplicate;
    private long _tradeEventsRejected;
    private string? _lastTradeRejectReason;
    private string? _lastAcceptedTradeEventId;
    private long? _lastAcceptedTradeSequence;

    public AuctionEpisodeHost(
        decimal tickSize,
        string contractIdentity,
        string contractEpoch,
        string? timestampPolicyVersion = null,
        EpisodePolicyConfig? policy = null)
    {
        if (tickSize <= 0m) throw new ArgumentOutOfRangeException(nameof(tickSize));
        _tickSize = tickSize;
        _contractIdentity = string.IsNullOrWhiteSpace(contractIdentity) ? "Unknown" : contractIdentity;
        _contractEpoch = string.IsNullOrWhiteSpace(contractEpoch) ? "Unknown" : contractEpoch;
        _timestampPolicyVersion = string.IsNullOrWhiteSpace(timestampPolicyVersion)
            ? AtasTimestampNormalizer.PolicyVersion
            : timestampPolicyVersion;
        _policy = policy ?? new EpisodePolicyConfig(enabled: false);
        _registry = new AuctionEpisodeRegistry(_tickSize, _timestampPolicyVersion);
        _registry.Configure(_tickSize, _contractIdentity, _contractEpoch, _timestampPolicyVersion);
    }

    public AuctionEpisodeSetSnapshot? Current => _published;
    public EpisodePolicyConfig Policy => _policy;
    public EpisodeInputFingerprint? LastAppliedFingerprint => _lastFingerprint;
    public EpisodeHistoryMode HistoryMode => EpisodeHistoryMode.LiveOnly;
    public long TradeEventsSeen => _tradeEventsSeen;
    public long TradeEventsAccepted => _tradeEventsAccepted;
    public long TradeEventsDuplicate => _tradeEventsDuplicate;
    public long TradeEventsRejected => _tradeEventsRejected;
    public string? LastTradeRejectReason => _lastTradeRejectReason;
    public string? LastAcceptedTradeEventId => _lastAcceptedTradeEventId;
    public long? LastAcceptedTradeSequence => _lastAcceptedTradeSequence;

    public void Configure(
        decimal tickSize,
        string contractIdentity,
        string contractEpoch,
        EpisodePolicyConfig policy,
        string? timestampPolicyVersion = null)
    {
        if (tickSize <= 0m) throw new ArgumentOutOfRangeException(nameof(tickSize));
        if (policy is null) throw new ArgumentNullException(nameof(policy));

        _tickSize = tickSize;
        _contractIdentity = string.IsNullOrWhiteSpace(contractIdentity) ? "Unknown" : contractIdentity;
        _contractEpoch = string.IsNullOrWhiteSpace(contractEpoch) ? "Unknown" : contractEpoch;
        _timestampPolicyVersion = string.IsNullOrWhiteSpace(timestampPolicyVersion)
            ? AtasTimestampNormalizer.PolicyVersion
            : timestampPolicyVersion;
        _policy = policy;
        _registry.Configure(_tickSize, _contractIdentity, _contractEpoch, _timestampPolicyVersion);

        if (!_policy.Enabled)
        {
            _registry.Reset();
            _eligible = Array.Empty<StructuralReferenceSnapshot>();
            ResetAdmissionDiagnostics();
            _published = DisabledSnapshot(DateTime.UtcNow);
            _lastFingerprint = null;
        }
    }

    public void Reset()
    {
        _registry.Reset();
        _eligible = Array.Empty<StructuralReferenceSnapshot>();
        _published = null;
        _lastFingerprint = null;
        _createdAtUtc = default;
        _directionalProvenance = null;
        ResetAdmissionDiagnostics();
    }

    /// <summary>
    /// Non-event context sync: auction transition, eligible confirmed references, optional directional provenance.
    /// Does not process trades. Eligible refresh must not erase accepted-trade state within the same auction.
    /// </summary>
    public AuctionEpisodeSetSnapshot RebuildContext(
        PrimaryProfileSetSnapshot? profiles,
        StructuralReferenceSetSnapshot? references,
        DirectionalContextSetSnapshot? directional,
        DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        var auctionId = profiles?.CurrentAuction?.AuctionId ?? "";
        var fingerprint = EpisodeInputFingerprint.Build(
            _policy.Enabled,
            auctionId,
            references,
            directional,
            _tradeStreamEpoch,
            _tickSize,
            _contractEpoch,
            _timestampPolicyVersion,
            _policy.Version);

        if (!_policy.Enabled)
        {
            _registry.Reset();
            _eligible = Array.Empty<StructuralReferenceSnapshot>();
            ResetAdmissionDiagnostics();
            _published = DisabledSnapshot(now);
            _lastFingerprint = fingerprint;
            return _published;
        }

        if (!string.Equals(_timestampPolicyVersion, AtasTimestampNormalizer.PolicyVersion, StringComparison.Ordinal))
        {
            _published = InvalidSnapshot(fingerprint, now, "TIMESTAMP_POLICY_MISMATCH");
            _lastFingerprint = fingerprint;
            return _published;
        }

        if (string.IsNullOrWhiteSpace(auctionId))
        {
            _eligible = Array.Empty<StructuralReferenceSnapshot>();
            _published = StatusSnapshot(EpisodeModuleState.AwaitingReferences, fingerprint, now,
                new[] { "AWAITING_PRIMARY_AUCTION" });
            _lastFingerprint = fingerprint;
            return _published;
        }

        _registry.OnPrimaryAuctionChanged(auctionId, now);
        _eligible = SelectEligible(references);
        _registry.SyncEligibleReferences(_eligible, now);
        _directionalProvenance = BuildDirectionalProvenance(directional);

        _published = BuildSetSnapshot(fingerprint, now);
        _lastFingerprint = fingerprint;
        return _published;
    }

    public AuctionEpisodeSetSnapshot ProcessTrade(EpisodeTradeEvent evt, DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        if (!_policy.Enabled)
            return _published ?? DisabledSnapshot(now);

        _tradeEventsSeen++;
        if (evt is null)
        {
            RecordReject(EpisodeTradeAdmissionResult.InvalidEvent, "INVALID_EVENT");
            _published = BuildSetSnapshot(_lastFingerprint ?? default, now);
            return _published;
        }

        var result = _registry.ProcessTrade(evt, _eligible, _directionalProvenance, now);
        ApplyAdmissionResult(result, evt);
        var fingerprint = _lastFingerprint ?? EpisodeInputFingerprint.Build(
            true, _registry.PrimaryAuctionId, null, null, _tradeStreamEpoch,
            _tickSize, _contractEpoch, _timestampPolicyVersion);
        _published = BuildSetSnapshot(fingerprint, now);
        return _published;
    }

    /// <summary>Drain Phase 1E → Phase 1F read-only measurement events after ProcessTrade.</summary>
    public IReadOnlyList<EpisodeMeasurementEvent> DrainMeasurementEvents() =>
        _registry.DrainMeasurementEvents();

    /// <summary>Record a mapper/normalization failure without fabricating an episode.</summary>
    public AuctionEpisodeSetSnapshot NoteMappingReject(string reason, DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        if (!_policy.Enabled)
            return _published ?? DisabledSnapshot(now);

        _tradeEventsSeen++;
        RecordReject(EpisodeTradeAdmissionResult.MappingFailed, reason);
        var fingerprint = _lastFingerprint ?? default;
        _published = BuildSetSnapshot(fingerprint, now);
        return _published;
    }

    public static IReadOnlyList<StructuralReferenceSnapshot> SelectEligible(
        StructuralReferenceSetSnapshot? references)
    {
        if (references is null
            || references.ModuleState is StructuralReferenceModuleState.Disabled
                or StructuralReferenceModuleState.AwaitingPrimary
                or StructuralReferenceModuleState.Invalid)
            return Array.Empty<StructuralReferenceSnapshot>();

        return references.ConfirmedReferences
            .Where(r => r.Maturity == ReferenceMaturity.Confirmed)
            .Where(r => r.Status is ReferenceStatus.Fresh or ReferenceStatus.Active)
            .Where(r => ReferenceInteractionRoleMapper.IsPhase1EEligibleType(r.ReferenceType))
            .Where(r => r.ZoneLowTick == r.ZoneHighTick)
            .OrderBy(r => r.ZoneLowTick)
            .ThenBy(r => r.ReferenceId, StringComparer.Ordinal)
            .ToArray();
    }

    private void ApplyAdmissionResult(EpisodeTradeAdmissionResult result, EpisodeTradeEvent evt)
    {
        switch (result)
        {
            case EpisodeTradeAdmissionResult.Accepted:
                _tradeEventsAccepted++;
                _lastAcceptedTradeEventId = evt.EventIdentity;
                _lastAcceptedTradeSequence = evt.LocalMonotonicSequence;
                _lastTradeRejectReason = null;
                break;
            case EpisodeTradeAdmissionResult.Duplicate:
                _tradeEventsDuplicate++;
                break;
            default:
                RecordReject(result, result.ToString());
                break;
        }
    }

    private void RecordReject(EpisodeTradeAdmissionResult result, string reason)
    {
        _tradeEventsRejected++;
        _lastTradeRejectReason = reason;
        _ = result;
    }

    private void ResetAdmissionDiagnostics()
    {
        _tradeEventsSeen = 0;
        _tradeEventsAccepted = 0;
        _tradeEventsDuplicate = 0;
        _tradeEventsRejected = 0;
        _lastTradeRejectReason = null;
        _lastAcceptedTradeEventId = null;
        _lastAcceptedTradeSequence = null;
    }

    private AuctionEpisodeSetSnapshot BuildSetSnapshot(EpisodeInputFingerprint fingerprint, DateTime now)
    {
        if (_createdAtUtc == default)
            _createdAtUtc = now;

        var limitations = new List<string>
        {
            EpisodePolicyConfig.LimitationHistoryLiveOnly,
            EpisodePolicyConfig.LimitationIntraAuctionResetNotCalibrated,
            EpisodePolicyConfig.LimitationApproachDistanceNotCalibrated,
            EpisodePolicyConfig.LimitationDevelopingNotAuthorized,
            EpisodePolicyConfig.LimitationLocalValueNotAuthorized
        };

        var active = _registry.SnapshotActive(_tickSize);
        var closed = _registry.SnapshotClosed();
        var counts = active
            .GroupBy(e => e.State)
            .ToDictionary(g => g.Key, g => g.Count());

        EpisodeModuleState status;
        if (_eligible.Count == 0)
            status = EpisodeModuleState.AwaitingReferences;
        else if (!_registry.AnyTradeObserved)
            status = EpisodeModuleState.AwaitingTrades;
        else if (_registry.AnyAggressorUnavailable)
        {
            status = EpisodeModuleState.Partial;
            limitations.Add("AGGRESSOR_CLASSIFICATION_UNAVAILABLE");
        }
        else
            status = EpisodeModuleState.Ready;

        return new AuctionEpisodeSetSnapshot(
            status,
            _policy.Version,
            EpisodeHistoryMode.LiveOnly,
            _registry.PrimaryAuctionId,
            _eligible.Count,
            active.Select(a => a.ReferenceId).Distinct(StringComparer.Ordinal).Count(),
            active,
            closed,
            _registry.LatestUpdated,
            counts,
            fingerprint.ToString(),
            _registry.RegistryRevision,
            _createdAtUtc,
            now,
            limitations.Distinct(StringComparer.Ordinal).ToArray(),
            _tradeEventsSeen,
            _tradeEventsAccepted,
            _tradeEventsDuplicate,
            _tradeEventsRejected,
            _lastTradeRejectReason,
            _lastAcceptedTradeEventId,
            _lastAcceptedTradeSequence);
    }

    private static string? BuildDirectionalProvenance(DirectionalContextSetSnapshot? d)
    {
        if (d is null || d.Status == DirectionalModuleState.Disabled)
            return null;
        return d.Status + "|" + d.StructuralContext.State + "|" + d.TacticalContext.State
               + "|V=" + d.SnapshotVersionToken;
    }

    private AuctionEpisodeSetSnapshot DisabledSnapshot(DateTime now) =>
        StatusSnapshot(EpisodeModuleState.Disabled, default, now, new[] { "EPISODES_DISABLED" });

    private AuctionEpisodeSetSnapshot InvalidSnapshot(EpisodeInputFingerprint fp, DateTime now, string reason) =>
        StatusSnapshot(EpisodeModuleState.Invalid, fp, now, new[] { reason });

    private AuctionEpisodeSetSnapshot StatusSnapshot(
        EpisodeModuleState status,
        EpisodeInputFingerprint fingerprint,
        DateTime now,
        IReadOnlyList<string> limitations)
    {
        if (_createdAtUtc == default)
            _createdAtUtc = now;
        return new AuctionEpisodeSetSnapshot(
            status,
            _policy.Version,
            EpisodeHistoryMode.LiveOnly,
            _registry.PrimaryAuctionId,
            _eligible.Count,
            0,
            Array.Empty<AuctionEpisodeSnapshot>(),
            Array.Empty<AuctionEpisodeSnapshot>(),
            null,
            new Dictionary<EpisodeState, int>(),
            fingerprint.ToString(),
            _registry.RegistryRevision,
            _createdAtUtc,
            now,
            limitations,
            _tradeEventsSeen,
            _tradeEventsAccepted,
            _tradeEventsDuplicate,
            _tradeEventsRejected,
            _lastTradeRejectReason,
            _lastAcceptedTradeEventId,
            _lastAcceptedTradeSequence);
    }
}
