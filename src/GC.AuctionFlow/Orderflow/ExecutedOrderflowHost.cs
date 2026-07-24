using System.Globalization;
using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Profile;

namespace GC.AuctionFlow.Orderflow;

public readonly struct OrderflowInputFingerprint : IEquatable<OrderflowInputFingerprint>
{
    public OrderflowInputFingerprint(
        bool enabled,
        string primaryAuctionId,
        string policyVersion,
        decimal tickSize,
        string dataEpoch,
        string timestampPolicyVersion)
    {
        Enabled = enabled;
        PrimaryAuctionId = primaryAuctionId ?? "";
        PolicyVersion = policyVersion ?? "";
        TickSize = tickSize;
        DataEpoch = dataEpoch ?? "";
        TimestampPolicyVersion = timestampPolicyVersion ?? "";
    }

    public bool Enabled { get; }
    public string PrimaryAuctionId { get; }
    public string PolicyVersion { get; }
    public decimal TickSize { get; }
    public string DataEpoch { get; }
    public string TimestampPolicyVersion { get; }

    public static OrderflowInputFingerprint Build(
        bool enabled,
        string? primaryAuctionId,
        decimal tickSize,
        string dataEpoch,
        string timestampPolicyVersion) =>
        new(
            enabled,
            primaryAuctionId ?? "",
            ExecutedOrderflowPolicyConfig.PolicyVersion,
            tickSize,
            dataEpoch,
            timestampPolicyVersion);

    public bool Equals(OrderflowInputFingerprint other) =>
        Enabled == other.Enabled
        && string.Equals(PrimaryAuctionId, other.PrimaryAuctionId, StringComparison.Ordinal)
        && string.Equals(PolicyVersion, other.PolicyVersion, StringComparison.Ordinal)
        && TickSize == other.TickSize
        && string.Equals(DataEpoch, other.DataEpoch, StringComparison.Ordinal)
        && string.Equals(TimestampPolicyVersion, other.TimestampPolicyVersion, StringComparison.Ordinal);

    public override bool Equals(object? obj) =>
        obj is OrderflowInputFingerprint other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(Enabled, PrimaryAuctionId, PolicyVersion, TickSize, DataEpoch, TimestampPolicyVersion);

    public override string ToString() =>
        Enabled + "|" + PrimaryAuctionId + "|" + PolicyVersion + "|" +
        TickSize.ToString(CultureInfo.InvariantCulture) + "|" + DataEpoch + "|" + TimestampPolicyVersion;
}

public static class OrderflowPublishInitialization
{
    public static bool ShouldProcess(
        bool enable,
        ExecutedOrderflowHost? host,
        OrderflowInputFingerprint? lastApplied,
        OrderflowInputFingerprint current)
    {
        if (!enable)
            return false;
        if (host is null || host.Current is null || lastApplied is null)
            return true;
        return !lastApplied.Value.Equals(current);
    }
}

/// <summary>
/// Phase 2A Executed Orderflow raw feature host.
/// Consumes NewTrade-normalized events only. Never mutates Episode/Evidence/Profile.
/// Cumulative callbacks are not authoritative for executed totals.
/// </summary>
public sealed class ExecutedOrderflowHost
{
    private ExecutedOrderflowPolicyConfig _policy;
    private decimal _tickSize;
    private string _instrumentIdentity = "Unknown";
    private string _contractEpoch = "Unknown";
    private string _timestampPolicyVersion;
    private MutableAuctionAggregate? _auction;
    private ExecutedOrderflowSetSnapshot? _published;
    private OrderflowInputFingerprint? _lastFingerprint;
    private DateTime _createdAtUtc;
    private string _primaryAuctionId = "";
    private bool _observedAnyTradeInPriorAuction;
    private long _registryRevision;
    private long _eventsSeen;
    private long _eventsAccepted;
    private long _eventsDuplicated;
    private long _eventsRejected;
    private string? _lastRejectionReason;
    private string? _lastAcceptedEventId;
    private long? _lastAcceptedSequence;
    private long _onNewTradeCount;
    private long _onNewTradesBatchCount;
    private long _lastCommittedSequence = -1;

    public ExecutedOrderflowHost(
        decimal tickSize,
        string instrumentIdentity,
        string contractEpoch,
        string? timestampPolicyVersion = null,
        ExecutedOrderflowPolicyConfig? policy = null)
    {
        if (tickSize <= 0m) throw new ArgumentOutOfRangeException(nameof(tickSize));
        _tickSize = tickSize;
        _instrumentIdentity = string.IsNullOrWhiteSpace(instrumentIdentity) ? "Unknown" : instrumentIdentity;
        _contractEpoch = string.IsNullOrWhiteSpace(contractEpoch) ? "Unknown" : contractEpoch;
        _timestampPolicyVersion = string.IsNullOrWhiteSpace(timestampPolicyVersion)
            ? AtasTimestampNormalizer.PolicyVersion
            : timestampPolicyVersion;
        _policy = policy ?? new ExecutedOrderflowPolicyConfig(enabled: false);
    }

    public ExecutedOrderflowSetSnapshot? Current => _published;
    public OrderflowInputFingerprint? LastAppliedFingerprint => _lastFingerprint;
    public ExecutedOrderflowPolicyConfig Policy => _policy;

    public void Configure(
        decimal tickSize,
        string instrumentIdentity,
        string contractEpoch,
        ExecutedOrderflowPolicyConfig policy,
        string? timestampPolicyVersion = null)
    {
        if (tickSize <= 0m) throw new ArgumentOutOfRangeException(nameof(tickSize));
        if (policy is null) throw new ArgumentNullException(nameof(policy));
        var epochChanged = !string.Equals(_contractEpoch, contractEpoch, StringComparison.Ordinal)
                           || tickSize != _tickSize;
        _tickSize = tickSize;
        _instrumentIdentity = string.IsNullOrWhiteSpace(instrumentIdentity) ? "Unknown" : instrumentIdentity;
        _contractEpoch = string.IsNullOrWhiteSpace(contractEpoch) ? "Unknown" : contractEpoch;
        _timestampPolicyVersion = string.IsNullOrWhiteSpace(timestampPolicyVersion)
            ? AtasTimestampNormalizer.PolicyVersion
            : timestampPolicyVersion;
        var wasEnabled = _policy.Enabled;
        _policy = policy;
        if (!_policy.Enabled)
        {
            ResetInternal();
            _published = DisabledSnapshot(DateTime.UtcNow);
            _lastFingerprint = null;
        }
        else if (!wasEnabled || epochChanged)
        {
            ResetInternal();
        }
    }

    public void Reset()
    {
        ResetInternal();
        _published = null;
        _lastFingerprint = null;
        _createdAtUtc = default;
    }

    private void ResetInternal()
    {
        if (_auction is not null)
            _observedAnyTradeInPriorAuction = _auction.TradeCount > 0 || _observedAnyTradeInPriorAuction;
        _auction = null;
        _primaryAuctionId = "";
        _lastCommittedSequence = -1;
        _registryRevision++;
    }

    public ExecutedOrderflowSetSnapshot RebuildContext(
        PrimaryProfileSetSnapshot? profiles,
        AuctionEpisodeSetSnapshot? episodes,
        DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        var auctionId = profiles?.CurrentAuction?.AuctionId ?? episodes?.PrimaryAuctionId ?? "";
        var fingerprint = OrderflowInputFingerprint.Build(
            _policy.Enabled, auctionId, _tickSize, _contractEpoch, _timestampPolicyVersion);

        if (!_policy.Enabled)
        {
            ResetInternal();
            _published = DisabledSnapshot(now);
            _lastFingerprint = fingerprint;
            return _published;
        }

        if (!string.Equals(_timestampPolicyVersion, AtasTimestampNormalizer.PolicyVersion, StringComparison.Ordinal))
        {
            _published = StatusSnapshot(OrderflowModuleState.Invalid, fingerprint, now,
                new[] { "TIMESTAMP_POLICY_MISMATCH" });
            _lastFingerprint = fingerprint;
            return _published;
        }

        EnsureAuction(auctionId, now);
        _auction?.SyncEpisodeLifecycle(episodes, now);
        _published = BuildSetSnapshot(fingerprint, now);
        _lastFingerprint = fingerprint;
        return _published;
    }

    public ExecutedOrderflowSetSnapshot ProcessTrade(
        ExecutedTradeEvent evt,
        AuctionEpisodeSetSnapshot? episodes,
        DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        if (!_policy.Enabled)
            return _published ?? DisabledSnapshot(now);

        _eventsSeen++;
        if (evt.SourceCallbackKind == OrderflowSourceCallbackKind.OnNewTrade)
            _onNewTradeCount++;
        else
            _onNewTradesBatchCount++;

        if (evt.ExecutedVolume < 0m || evt.TickSize != _tickSize)
        {
            _eventsRejected++;
            _lastRejectionReason = "ORDERFLOW_EVENT_INCOMPATIBLE";
            return PublishCurrent(now, episodes);
        }

        if (!string.Equals(evt.DataEpoch, _contractEpoch, StringComparison.Ordinal))
        {
            _eventsRejected++;
            _lastRejectionReason = "ORDERFLOW_EPOCH_MISMATCH";
            return PublishCurrent(now, episodes);
        }

        if (evt.EventSequence < _lastCommittedSequence)
        {
            _eventsRejected++;
            _lastRejectionReason = "ORDERFLOW_STALE_SEQUENCE";
            return PublishCurrent(now, episodes);
        }

        EnsureAuction(evt.PrimaryAuctionId, now);
        if (_auction is null)
        {
            _eventsRejected++;
            _lastRejectionReason = "ORDERFLOW_AUCTION_UNAVAILABLE";
            return PublishCurrent(now, episodes);
        }

        var meta = BuildEpisodeMeta(episodes);
        var accepted = _auction.TryApply(evt, meta, now);
        if (!accepted)
        {
            _eventsDuplicated++;
            return PublishCurrent(now, episodes);
        }

        _eventsAccepted++;
        _lastAcceptedEventId = evt.EventId;
        _lastAcceptedSequence = evt.EventSequence;
        _lastCommittedSequence = evt.EventSequence;
        _auction.SyncEpisodeLifecycle(episodes, now);
        return PublishCurrent(now, episodes);
    }

    private void EnsureAuction(string auctionId, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(auctionId))
            auctionId = "Unknown";

        if (_auction is not null && string.Equals(_primaryAuctionId, auctionId, StringComparison.Ordinal))
            return;

        var continuous = _auction is not null || _observedAnyTradeInPriorAuction;
        if (_auction is not null)
            _observedAnyTradeInPriorAuction = true;

        var coverage = continuous
            ? OrderflowCoverageMode.LiveOnlyFromAuctionStart
            : OrderflowCoverageMode.LiveOnlyMidAuction;

        _auction = new MutableAuctionAggregate(
            _instrumentIdentity,
            _contractEpoch,
            auctionId,
            _tickSize,
            _timestampPolicyVersion,
            coverage,
            nowUtc);
        _primaryAuctionId = auctionId;
        _lastCommittedSequence = -1;
        _registryRevision++;
    }

    private static IReadOnlyDictionary<string, (string ReferenceId, ReferenceInteractionRole Role)> BuildEpisodeMeta(
        AuctionEpisodeSetSnapshot? episodes)
    {
        var map = new Dictionary<string, (string, ReferenceInteractionRole)>(StringComparer.Ordinal);
        if (episodes is null)
            return map;
        foreach (var ep in episodes.ActiveEpisodes.Concat(episodes.RecentlyClosedEpisodes))
            map[ep.EpisodeId] = (ep.ReferenceId, ep.ReferenceRole);
        return map;
    }

    private ExecutedOrderflowSetSnapshot PublishCurrent(DateTime now, AuctionEpisodeSetSnapshot? episodes)
    {
        var fingerprint = _lastFingerprint ?? OrderflowInputFingerprint.Build(
            true, _primaryAuctionId, _tickSize, _contractEpoch, _timestampPolicyVersion);
        _auction?.SyncEpisodeLifecycle(episodes, now);
        _published = BuildSetSnapshot(fingerprint, now);
        return _published;
    }

    private ExecutedOrderflowSetSnapshot BuildSetSnapshot(OrderflowInputFingerprint fingerprint, DateTime now)
    {
        if (_createdAtUtc == default)
            _createdAtUtc = now;

        var limitations = new List<string>
        {
            ExecutedOrderflowPolicyConfig.LimitationHistoryLiveOnly,
            ExecutedOrderflowPolicyConfig.LimitationCumulativeNotAuthoritative
        };

        OrderflowModuleState status;
        ExecutedOrderflowAuctionSnapshot? auctionSnap = null;
        IReadOnlyList<ExecutedPriceLevelSnapshot> levels = Array.Empty<ExecutedPriceLevelSnapshot>();
        IReadOnlyList<ExecutedOrderflowEpisodeSnapshot> activeEps = Array.Empty<ExecutedOrderflowEpisodeSnapshot>();
        IReadOnlyList<ExecutedOrderflowEpisodeSnapshot> closedEps = Array.Empty<ExecutedOrderflowEpisodeSnapshot>();

        if (_auction is null || _auction.TradeCount == 0)
        {
            status = OrderflowModuleState.AwaitingTrades;
        }
        else
        {
            auctionSnap = _auction.ToAuctionSnapshot();
            levels = _auction.SnapshotPriceLevels();
            activeEps = _auction.SnapshotActiveEpisodes();
            closedEps = _auction.SnapshotClosedEpisodes();
            limitations.AddRange(auctionSnap.Limitations);

            if (auctionSnap.AggressorClassificationStatus == AggressorClassificationStatus.Complete
                && auctionSnap.CoverageMode != OrderflowCoverageMode.LiveOnlyMidAuction)
                status = OrderflowModuleState.Ready;
            else
                status = OrderflowModuleState.Partial;
        }

        return new ExecutedOrderflowSetSnapshot(
            status,
            _policy.Version,
            auctionSnap,
            levels,
            activeEps,
            closedEps,
            _eventsSeen,
            _eventsAccepted,
            _eventsDuplicated,
            _eventsRejected,
            _lastRejectionReason,
            _lastAcceptedEventId,
            _lastAcceptedSequence,
            _onNewTradeCount,
            _onNewTradesBatchCount,
            fingerprint.ToString(),
            _registryRevision,
            _createdAtUtc,
            now,
            limitations.Distinct(StringComparer.Ordinal).ToArray());
    }

    private ExecutedOrderflowSetSnapshot DisabledSnapshot(DateTime now) =>
        StatusSnapshot(OrderflowModuleState.Disabled, default, now, new[] { "ORDERFLOW_DISABLED" });

    private ExecutedOrderflowSetSnapshot StatusSnapshot(
        OrderflowModuleState status,
        OrderflowInputFingerprint fingerprint,
        DateTime now,
        IReadOnlyList<string> limitations)
    {
        if (_createdAtUtc == default)
            _createdAtUtc = now;
        return new ExecutedOrderflowSetSnapshot(
            status,
            _policy.Version,
            null,
            Array.Empty<ExecutedPriceLevelSnapshot>(),
            Array.Empty<ExecutedOrderflowEpisodeSnapshot>(),
            Array.Empty<ExecutedOrderflowEpisodeSnapshot>(),
            _eventsSeen,
            _eventsAccepted,
            _eventsDuplicated,
            _eventsRejected,
            _lastRejectionReason,
            _lastAcceptedEventId,
            _lastAcceptedSequence,
            _onNewTradeCount,
            _onNewTradesBatchCount,
            fingerprint.ToString(),
            _registryRevision,
            _createdAtUtc,
            now,
            limitations);
    }
}
