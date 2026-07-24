using System.Globalization;
using GC.AuctionFlow.Orderflow;
using GC.AuctionFlow.Profile;

namespace GC.AuctionFlow.Cluster;

public readonly struct ClusterRawInputFingerprint : IEquatable<ClusterRawInputFingerprint>
{
    public ClusterRawInputFingerprint(
        bool enabled,
        string primaryAuctionId,
        string policyVersion,
        decimal tickSize,
        string dataEpoch,
        string timestampPolicyVersion,
        long orderflowEventRevision)
    {
        Enabled = enabled;
        PrimaryAuctionId = primaryAuctionId ?? "";
        PolicyVersion = policyVersion ?? "";
        TickSize = tickSize;
        DataEpoch = dataEpoch ?? "";
        TimestampPolicyVersion = timestampPolicyVersion ?? "";
        OrderflowEventRevision = orderflowEventRevision;
    }

    public bool Enabled { get; }
    public string PrimaryAuctionId { get; }
    public string PolicyVersion { get; }
    public decimal TickSize { get; }
    public string DataEpoch { get; }
    public string TimestampPolicyVersion { get; }
    public long OrderflowEventRevision { get; }

    public static ClusterRawInputFingerprint Build(
        bool enabled,
        string? primaryAuctionId,
        decimal tickSize,
        string dataEpoch,
        string timestampPolicyVersion,
        long orderflowEventRevision) =>
        new(
            enabled,
            primaryAuctionId ?? "",
            ClusterRawFeaturePolicyConfig.PolicyVersion,
            tickSize,
            dataEpoch,
            timestampPolicyVersion,
            orderflowEventRevision);

    public bool Equals(ClusterRawInputFingerprint other) =>
        Enabled == other.Enabled
        && string.Equals(PrimaryAuctionId, other.PrimaryAuctionId, StringComparison.Ordinal)
        && string.Equals(PolicyVersion, other.PolicyVersion, StringComparison.Ordinal)
        && TickSize == other.TickSize
        && string.Equals(DataEpoch, other.DataEpoch, StringComparison.Ordinal)
        && string.Equals(TimestampPolicyVersion, other.TimestampPolicyVersion, StringComparison.Ordinal)
        && OrderflowEventRevision == other.OrderflowEventRevision;

    public override bool Equals(object? obj) =>
        obj is ClusterRawInputFingerprint other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(Enabled, PrimaryAuctionId, PolicyVersion, TickSize, DataEpoch,
            TimestampPolicyVersion, OrderflowEventRevision);

    public override string ToString() =>
        Enabled + "|" + PrimaryAuctionId + "|" + PolicyVersion + "|" +
        TickSize.ToString(CultureInfo.InvariantCulture) + "|" + DataEpoch + "|" +
        TimestampPolicyVersion + "|" + OrderflowEventRevision.ToString(CultureInfo.InvariantCulture);
}

public static class ClusterRawPublishInitialization
{
    public static bool ShouldProcess(
        bool enable,
        ClusterRawHost? host,
        ClusterRawInputFingerprint? lastApplied,
        ClusterRawInputFingerprint current)
    {
        if (!enable)
            return false;
        if (host is null || host.Current is null || lastApplied is null)
            return true;
        return !lastApplied.Value.Equals(current);
    }
}

/// <summary>
/// Phase 2B Cluster Raw Feature host.
/// Consumes Phase 2A immutable snapshots + optional changed-tick fan-out.
/// Does not remap ATAS trades or mutate Orderflow/Episode/Evidence.
/// </summary>
public sealed class ClusterRawHost
{
    private ClusterRawFeaturePolicyConfig _policy;
    private decimal _tickSize;
    private string _instrumentIdentity = "Unknown";
    private string _contractEpoch = "Unknown";
    private string _timestampPolicyVersion;
    private MutableClusterAuction? _auction;
    private ClusterRawSetSnapshot? _published;
    private ClusterRawInputFingerprint? _lastFingerprint;
    private DateTime _createdAtUtc;
    private string _primaryAuctionId = "";
    private long _rejectedStale;
    private string? _lastRejectionReason;
    private string? _lastSourceEventId;
    private long? _lastSourceSequence;
    private long _lastAppliedOrderflowRevision = -1;

    public ClusterRawHost(
        decimal tickSize,
        string instrumentIdentity,
        string contractEpoch,
        string? timestampPolicyVersion = null,
        ClusterRawFeaturePolicyConfig? policy = null)
    {
        if (tickSize <= 0m) throw new ArgumentOutOfRangeException(nameof(tickSize));
        _tickSize = tickSize;
        _instrumentIdentity = string.IsNullOrWhiteSpace(instrumentIdentity) ? "Unknown" : instrumentIdentity;
        _contractEpoch = string.IsNullOrWhiteSpace(contractEpoch) ? "Unknown" : contractEpoch;
        _timestampPolicyVersion = string.IsNullOrWhiteSpace(timestampPolicyVersion)
            ? AtasTimestampNormalizer.PolicyVersion
            : timestampPolicyVersion;
        _policy = policy ?? new ClusterRawFeaturePolicyConfig(enabled: false);
    }

    public ClusterRawSetSnapshot? Current => _published;
    public ClusterRawInputFingerprint? LastAppliedFingerprint => _lastFingerprint;
    public ClusterRawFeaturePolicyConfig Policy => _policy;

    public void Configure(
        decimal tickSize,
        string instrumentIdentity,
        string contractEpoch,
        ClusterRawFeaturePolicyConfig policy,
        string? timestampPolicyVersion = null)
    {
        if (tickSize <= 0m) throw new ArgumentOutOfRangeException(nameof(tickSize));
        if (policy is null) throw new ArgumentNullException(nameof(policy));
        _tickSize = tickSize;
        _instrumentIdentity = string.IsNullOrWhiteSpace(instrumentIdentity) ? "Unknown" : instrumentIdentity;
        _contractEpoch = string.IsNullOrWhiteSpace(contractEpoch) ? "Unknown" : contractEpoch;
        _timestampPolicyVersion = string.IsNullOrWhiteSpace(timestampPolicyVersion)
            ? AtasTimestampNormalizer.PolicyVersion
            : timestampPolicyVersion;
        _policy = policy;
        if (!_policy.Enabled)
        {
            _auction = null;
            _published = DisabledSnapshot(DateTime.UtcNow);
            _lastFingerprint = null;
            _lastAppliedOrderflowRevision = -1;
        }
    }

    public void Reset()
    {
        _auction = null;
        _published = null;
        _lastFingerprint = null;
        _createdAtUtc = default;
        _primaryAuctionId = "";
        _rejectedStale = 0;
        _lastRejectionReason = null;
        _lastSourceEventId = null;
        _lastSourceSequence = null;
        _lastAppliedOrderflowRevision = -1;
    }

    public ClusterRawSetSnapshot RebuildFromOrderflow(
        ExecutedOrderflowSetSnapshot? orderflow,
        DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        if (!_policy.Enabled)
        {
            _auction = null;
            _published = DisabledSnapshot(now);
            return _published;
        }

        if (!string.Equals(_timestampPolicyVersion, AtasTimestampNormalizer.PolicyVersion, StringComparison.Ordinal))
        {
            _published = StatusSnapshot(ClusterRawModuleState.Invalid, now,
                new[] { "TIMESTAMP_POLICY_MISMATCH" });
            return _published;
        }

        if (orderflow is null
            || orderflow.ModuleState is OrderflowModuleState.Disabled or OrderflowModuleState.AwaitingTrades
            || orderflow.CurrentAuction is null
            || orderflow.CurrentAuction.TradeCount <= 0)
        {
            _auction = null;
            _lastAppliedOrderflowRevision = -1;
            _published = StatusSnapshot(ClusterRawModuleState.AwaitingOrderflow, now,
                new[] { ClusterRawFeaturePolicyConfig.LimitationHistoryLiveOnly });
            return _published;
        }

        if (orderflow.ModuleState == OrderflowModuleState.Invalid)
        {
            _published = StatusSnapshot(ClusterRawModuleState.Invalid, now,
                new[] { "ORDERFLOW_INPUT_INVALID" });
            return _published;
        }

        EnsureAuction(orderflow.CurrentAuction, now);
        var auctionSnap = _auction!.RebuildFromOrderflow(orderflow, changedPriceTick: null, now);
        _published = BuildSetSnapshot(orderflow, auctionSnap, now);
        _lastFingerprint = ClusterRawInputFingerprint.Build(
            true, _primaryAuctionId, _tickSize, _contractEpoch, _timestampPolicyVersion,
            orderflow.CurrentAuction.EventRevision);
        _lastAppliedOrderflowRevision = orderflow.CurrentAuction.EventRevision;
        return _published;
    }

    /// <summary>
    /// Version-gated update after Phase 2A accepts a trade. Pass changed tick for visit tracking.
    /// Does not admit trades independently — Orderflow remains authoritative.
    /// </summary>
    public ClusterRawSetSnapshot ProcessOrderflowUpdate(
        ExecutedOrderflowSetSnapshot orderflow,
        long? changedPriceTick,
        string? sourceEventId,
        long? sourceSequence,
        DateTime? eventAtUtc = null,
        DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        if (!_policy.Enabled)
            return _published ?? DisabledSnapshot(now);

        if (orderflow.CurrentAuction is null || orderflow.CurrentAuction.TradeCount <= 0)
            return RebuildFromOrderflow(orderflow, now);

        // Auction / epoch / tick identity change clears applied revision before stale checks.
        EnsureAuction(orderflow.CurrentAuction, now);

        var ofRev = orderflow.CurrentAuction.EventRevision;
        if (ofRev < _lastAppliedOrderflowRevision)
        {
            _rejectedStale++;
            _lastRejectionReason = "CLUSTER_STALE_ORDERFLOW_REVISION";
            return _published ?? RebuildFromOrderflow(orderflow, now);
        }

        if (ofRev == _lastAppliedOrderflowRevision && _published?.CurrentAuction is not null)
            return _published;

        if (changedPriceTick.HasValue)
        {
            var at = eventAtUtc
                     ?? orderflow.CurrentAuction.LastEventAtUtc
                     ?? now;
            _auction!.TryNoteAcceptedEvent(sourceEventId, changedPriceTick.Value, at);
        }

        _lastSourceEventId = sourceEventId;
        _lastSourceSequence = sourceSequence;
        var auctionSnap = _auction!.RebuildFromOrderflow(orderflow, changedPriceTick, now);
        _published = BuildSetSnapshot(orderflow, auctionSnap, now);
        _lastFingerprint = ClusterRawInputFingerprint.Build(
            true, _primaryAuctionId, _tickSize, _contractEpoch, _timestampPolicyVersion, ofRev);
        _lastAppliedOrderflowRevision = ofRev;
        return _published;
    }

    private void EnsureAuction(ExecutedOrderflowAuctionSnapshot ofAuction, DateTime nowUtc)
    {
        if (_auction is not null
            && string.Equals(_primaryAuctionId, ofAuction.PrimaryAuctionId, StringComparison.Ordinal)
            && string.Equals(_contractEpoch, ofAuction.DataEpoch, StringComparison.Ordinal)
            && _tickSize == ofAuction.TickSize)
            return;

        _auction = new MutableClusterAuction(
            ofAuction.InstrumentIdentity,
            ofAuction.DataEpoch,
            ofAuction.PrimaryAuctionId,
            ofAuction.TickSize,
            ofAuction.TimestampPolicy,
            ofAuction.ObservationStartedAtUtc);
        _primaryAuctionId = ofAuction.PrimaryAuctionId;
        _lastAppliedOrderflowRevision = -1;
    }

    private ClusterRawSetSnapshot BuildSetSnapshot(
        ExecutedOrderflowSetSnapshot orderflow,
        ClusterRawAuctionSnapshot auction,
        DateTime now)
    {
        if (_createdAtUtc == default)
            _createdAtUtc = now;

        var state = ResolveModuleState(orderflow, auction);
        var lim = new List<string>(auction.Limitations);
        if (!lim.Contains(ClusterRawFeaturePolicyConfig.LimitationClassificationNotCalibrated, StringComparer.Ordinal))
            lim.Add(ClusterRawFeaturePolicyConfig.LimitationClassificationNotCalibrated);

        return new ClusterRawSetSnapshot(
            state,
            ClusterRawFeaturePolicyConfig.PolicyVersion,
            auction,
            ClusterRawFeaturePolicyConfig.RankMethod,
            orderflow.CurrentAuction?.EventRevision ?? 0,
            orderflow.CurrentAuction?.StateVersion ?? 0,
            _lastSourceEventId,
            _lastSourceSequence,
            _rejectedStale,
            _lastRejectionReason,
            _lastFingerprint,
            _createdAtUtc,
            now,
            lim);
    }

    private static ClusterRawModuleState ResolveModuleState(
        ExecutedOrderflowSetSnapshot orderflow,
        ClusterRawAuctionSnapshot auction)
    {
        if (orderflow.ModuleState == OrderflowModuleState.Invalid)
            return ClusterRawModuleState.Invalid;
        if (auction.PriceLevelCount <= 0)
            return ClusterRawModuleState.AwaitingOrderflow;
        if (auction.UnknownOnlyLevelCount > 0
            || auction.PartialLevelCount > 0
            || auction.CoverageMode == OrderflowCoverageMode.LiveOnlyMidAuction
            || orderflow.ModuleState == OrderflowModuleState.Partial
            || auction.DataQuality != ClusterRawDataQuality.Complete)
            return ClusterRawModuleState.Partial;
        return ClusterRawModuleState.Ready;
    }

    private ClusterRawSetSnapshot DisabledSnapshot(DateTime now) =>
        StatusSnapshot(ClusterRawModuleState.Disabled, now, Array.Empty<string>());

    private ClusterRawSetSnapshot StatusSnapshot(
        ClusterRawModuleState state,
        DateTime now,
        IReadOnlyList<string> limitations)
    {
        if (_createdAtUtc == default)
            _createdAtUtc = now;
        return new ClusterRawSetSnapshot(
            state,
            ClusterRawFeaturePolicyConfig.PolicyVersion,
            null,
            ClusterRawFeaturePolicyConfig.RankMethod,
            0,
            0,
            _lastSourceEventId,
            _lastSourceSequence,
            _rejectedStale,
            _lastRejectionReason,
            _lastFingerprint,
            _createdAtUtc,
            now,
            limitations);
    }
}
