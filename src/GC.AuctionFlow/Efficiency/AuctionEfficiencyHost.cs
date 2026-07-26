using GC.AuctionFlow.Cluster;
using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Evidence;
using GC.AuctionFlow.Orderflow;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Reference;

namespace GC.AuctionFlow.Efficiency;

public static class EfficiencyPublishInitialization
{
    public static bool ShouldProcess(
        bool enable,
        AuctionEfficiencyHost? host,
        EfficiencyInputFingerprint? lastApplied,
        EfficiencyInputFingerprint current)
    {
        if (!enable)
            return false;
        if (host is null || host.Current is null || lastApplied is null)
            return true;
        return !lastApplied.Value.Equals(current);
    }
}

/// <summary>
/// Phase 2C Auction Efficiency Raw Evidence host.
/// Consumes Phase 2A/2B/1E/1F/Profile immutably. No classification.
/// </summary>
public sealed class AuctionEfficiencyHost
{
    private AuctionEfficiencyEvidencePolicyConfig _policy;
    private decimal _tickSize;
    private string _instrumentIdentity = "Unknown";
    private string _contractEpoch = "Unknown";
    private string _timestampPolicyVersion;
    private AuctionEfficiencyEvidenceSetSnapshot? _published;
    private EfficiencyInputFingerprint? _lastFingerprint;
    private DateTime _createdAtUtc;
    private long _rejectedStale;
    private string? _lastRejectionReason;

    // Immutable start anchors per evidence id
    private readonly Dictionary<string, ProfileStartAnchors> _anchors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, MutableEvidenceState> _states = new(StringComparer.Ordinal);
    private readonly List<AuctionEfficiencyEvidenceSnapshot> _closedEpisodes = new();
    private string _primaryAuctionId = "";

    public AuctionEfficiencyHost(
        decimal tickSize,
        string instrumentIdentity,
        string contractEpoch,
        string? timestampPolicyVersion = null,
        AuctionEfficiencyEvidencePolicyConfig? policy = null)
    {
        if (tickSize <= 0m) throw new ArgumentOutOfRangeException(nameof(tickSize));
        _tickSize = tickSize;
        _instrumentIdentity = string.IsNullOrWhiteSpace(instrumentIdentity) ? "Unknown" : instrumentIdentity;
        _contractEpoch = string.IsNullOrWhiteSpace(contractEpoch) ? "Unknown" : contractEpoch;
        _timestampPolicyVersion = string.IsNullOrWhiteSpace(timestampPolicyVersion)
            ? AtasTimestampNormalizer.PolicyVersion
            : timestampPolicyVersion;
        _policy = policy ?? new AuctionEfficiencyEvidencePolicyConfig(enabled: false);
    }

    public AuctionEfficiencyEvidenceSetSnapshot? Current => _published;
    public EfficiencyInputFingerprint? LastAppliedFingerprint => _lastFingerprint;
    public AuctionEfficiencyEvidencePolicyConfig Policy => _policy;

    public void Configure(
        decimal tickSize,
        string instrumentIdentity,
        string contractEpoch,
        AuctionEfficiencyEvidencePolicyConfig policy,
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

    private void ResetInternal()
    {
        _anchors.Clear();
        _states.Clear();
        _closedEpisodes.Clear();
        _primaryAuctionId = "";
        _rejectedStale = 0;
        _lastRejectionReason = null;
    }

    public AuctionEfficiencyEvidenceSetSnapshot Rebuild(
        ExecutedOrderflowSetSnapshot? orderflow,
        ClusterRawSetSnapshot? cluster,
        AuctionEpisodeSetSnapshot? episodes,
        AcceptanceReentryEvidenceSetSnapshot? evidence,
        PrimaryProfileSetSnapshot? profiles,
        DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        if (!_policy.Enabled)
        {
            ResetInternal();
            _published = DisabledSnapshot(now);
            return _published;
        }

        if (!string.Equals(_timestampPolicyVersion, AtasTimestampNormalizer.PolicyVersion, StringComparison.Ordinal))
        {
            _published = StatusSnapshot(EfficiencyModuleState.Invalid, now, new[] { "TIMESTAMP_POLICY_MISMATCH" });
            return _published;
        }

        var fp = BuildFingerprint(orderflow, cluster, episodes, evidence, profiles);
        if (_lastFingerprint.HasValue && _lastFingerprint.Value.Equals(fp) && _published is not null)
            return _published;

        if (orderflow?.CurrentAuction is null
            || orderflow.ModuleState is OrderflowModuleState.Disabled or OrderflowModuleState.AwaitingTrades
            || orderflow.CurrentAuction.TradeCount <= 0)
        {
            _published = StatusSnapshot(EfficiencyModuleState.AwaitingOrderflow, now,
                new[] { AuctionEfficiencyEvidencePolicyConfig.LimitationHistoryLiveOnly });
            _lastFingerprint = fp;
            return _published;
        }

        if (orderflow.ModuleState == OrderflowModuleState.Invalid)
        {
            _published = StatusSnapshot(EfficiencyModuleState.Invalid, now, new[] { "ORDERFLOW_INPUT_INVALID" });
            _lastFingerprint = fp;
            return _published;
        }

        var auctionId = orderflow.CurrentAuction.PrimaryAuctionId;
        if (!string.Equals(_primaryAuctionId, auctionId, StringComparison.Ordinal))
        {
            FreezeAllActive();
            _anchors.Clear();
            _states.Clear();
            _primaryAuctionId = auctionId;
        }

        var auctionSnap = BuildAuctionEvidence(orderflow, cluster, profiles, evidence, fp.ToString(), now);
        var activeEps = BuildActiveEpisodeEvidence(orderflow, cluster, episodes, evidence, profiles, fp.ToString(), now);
        SyncClosedEpisodes(episodes);

        var latest = auctionSnap;
        foreach (var ep in activeEps)
        {
            if (latest is null || ep.EventRevision >= latest.EventRevision)
                latest = ep;
        }

        var ready = 0;
        var partial = 0;
        var invalid = 0;
        void Count(AuctionEfficiencyEvidenceSnapshot? s)
        {
            if (s is null) return;
            switch (s.MeasurementStatus)
            {
                case EfficiencyModuleState.Ready: ready++; break;
                case EfficiencyModuleState.Partial: partial++; break;
                case EfficiencyModuleState.Invalid: invalid++; break;
            }
        }
        Count(auctionSnap);
        foreach (var e in activeEps) Count(e);

        EfficiencyModuleState moduleState;
        if (auctionSnap is null)
            moduleState = EfficiencyModuleState.AwaitingOrderflow;
        else if (activeEps.Count == 0)
            moduleState = EfficiencyModuleState.AwaitingEpisode;
        else if (invalid > 0 && ready == 0 && partial == 0)
            moduleState = EfficiencyModuleState.Invalid;
        else if (partial > 0 || auctionSnap.MeasurementStatus == EfficiencyModuleState.Partial)
            moduleState = EfficiencyModuleState.Partial;
        else if (ready > 0)
            moduleState = EfficiencyModuleState.Ready;
        else
            moduleState = EfficiencyModuleState.Partial;

        if (_createdAtUtc == default)
            _createdAtUtc = now;

        var lim = new List<string>
        {
            AuctionEfficiencyEvidencePolicyConfig.LimitationHistoryLiveOnly,
            AuctionEfficiencyEvidencePolicyConfig.LimitationClassificationNotCalibrated,
            AuctionEfficiencyEvidencePolicyConfig.LimitationNoEffortResult,
            AuctionEfficiencyEvidencePolicyConfig.LimitationNoTradeFacilitation,
            AuctionEfficiencyEvidencePolicyConfig.LimitationNoAbsorption,
            AuctionEfficiencyEvidencePolicyConfig.LimitationNoExhaustion,
            AuctionEfficiencyEvidencePolicyConfig.LimitationImbalanceNotCalibrated,
            AuctionEfficiencyEvidencePolicyConfig.LimitationStackedNotCalibrated,
            AuctionEfficiencyEvidencePolicyConfig.LimitationBigTradeNotCalibrated,
            AuctionEfficiencyEvidencePolicyConfig.LimitationTapeSpeedNotCalibrated,
            AuctionEfficiencyEvidencePolicyConfig.LimitationMboSweepResearchOnly,
            AuctionEfficiencyEvidencePolicyConfig.LimitationClosePositionUnavailable
        };
        if (orderflow.CurrentAuction.CoverageMode == OrderflowCoverageMode.LiveOnlyMidAuction)
            lim.Add(AuctionEfficiencyEvidencePolicyConfig.LimitationMidAuction);

        _published = new AuctionEfficiencyEvidenceSetSnapshot(
            moduleState,
            AuctionEfficiencyEvidencePolicyConfig.PolicyVersion,
            auctionSnap,
            activeEps,
            _closedEpisodes.TakeLast(AuctionEfficiencyEvidenceSetSnapshot.RecentlyClosedEpisodeCapacity).ToArray(),
            latest,
            ready,
            partial,
            invalid,
            fp,
            _rejectedStale,
            _lastRejectionReason,
            _createdAtUtc,
            now,
            lim);
        _lastFingerprint = fp;
        return _published;
    }

    private void FreezeAllActive()
    {
        foreach (var kv in _states.ToArray())
        {
            if (kv.Value.LastPublished is { } snap && !snap.IsFrozen)
            {
                var frozen = CloneFrozen(snap);
                if (snap.ScopeType != EfficiencyScopeType.CurrentPrimaryAuction)
                    _closedEpisodes.Add(frozen);
                _states[kv.Key] = kv.Value with { LastPublished = frozen, IsFrozen = true };
            }
        }
    }

    private void SyncClosedEpisodes(AuctionEpisodeSetSnapshot? episodes)
    {
        if (episodes is null) return;
        foreach (var ep in episodes.RecentlyClosedEpisodes)
        {
            var id = EfficiencyIdentity.BuildEpisode(ep.EpisodeId);
            if (_states.TryGetValue(id, out var st) && st.LastPublished is { } snap && !snap.IsFrozen)
            {
                var frozen = CloneFrozen(snap);
                if (!_closedEpisodes.Any(c => string.Equals(c.SnapshotId, frozen.SnapshotId, StringComparison.Ordinal)))
                    _closedEpisodes.Add(frozen);
                _states[id] = st with { LastPublished = frozen, IsFrozen = true };
            }
        }
    }

    private static AuctionEfficiencyEvidenceSnapshot CloneFrozen(AuctionEfficiencyEvidenceSnapshot s) =>
        new(
            s.SnapshotId, s.PolicyVersion, EfficiencyScopeType.ClosedEpisode, s.PrimaryAuctionId,
            s.EpisodeId, s.ReferenceId, s.ReferenceRole, s.InstrumentIdentity, s.DataEpoch, s.TickSize,
            s.TimestampPolicy, s.MeasurementStatus, s.ClassificationState, s.ObservationStartedAtUtc,
            s.FirstInputAtUtc, s.LastInputAtUtc, s.CoverageMode, s.ResultDirection, s.Effort, s.Result,
            s.RawRelationships, s.StateVersion, s.EventRevision, s.DataQuality, s.Availability,
            s.Limitations, s.InputFingerprint, isFrozen: true);

    private AuctionEfficiencyEvidenceSnapshot BuildAuctionEvidence(
        ExecutedOrderflowSetSnapshot orderflow,
        ClusterRawSetSnapshot? cluster,
        PrimaryProfileSetSnapshot? profiles,
        AcceptanceReentryEvidenceSetSnapshot? evidence,
        string fpText,
        DateTime now)
    {
        var ofA = orderflow.CurrentAuction!;
        var id = EfficiencyIdentity.BuildAuction(ofA.SnapshotId);
        var anchors = EnsureAnchors(id, profiles, now);
        var effort = BuildEffort(ofA, cluster?.CurrentAuction, orderflow.PriceLevels);
        var direction = ResolveDirection(ofA.FirstPriceTick, ofA.LastPriceTick, null);
        var result = BuildResult(
            ofA.FirstPriceTick, ofA.LastPriceTick, ofA.HighPriceTick, ofA.LowPriceTick,
            ofA.NetPriceProgressTicks, direction, null, evidence?.LatestUpdatedEvidence, anchors, profiles);
        var rel = BuildRelationships(effort, result, ofA.FirstEventAtUtc, ofA.LastEventAtUtc);
        var status = ResolveStatus(effort, result, hasEpisode: false);
        var quality = status == EfficiencyModuleState.Ready ? EfficiencyDataQuality.Complete : EfficiencyDataQuality.Partial;
        var avail = effort.EvidenceAvailability == EfficiencyAvailability.Available
                    && result.EvidenceAvailability == EfficiencyAvailability.Available
            ? EfficiencyAvailability.Available
            : EfficiencyAvailability.Partial;

        return PublishEvidence(
            id, EfficiencyScopeType.CurrentPrimaryAuction, ofA.PrimaryAuctionId, null, null, null,
            ofA, effort, result, rel, direction, status, quality, avail, fpText, now);
    }

    private IReadOnlyList<AuctionEfficiencyEvidenceSnapshot> BuildActiveEpisodeEvidence(
        ExecutedOrderflowSetSnapshot orderflow,
        ClusterRawSetSnapshot? cluster,
        AuctionEpisodeSetSnapshot? episodes,
        AcceptanceReentryEvidenceSetSnapshot? evidence,
        PrimaryProfileSetSnapshot? profiles,
        string fpText,
        DateTime now)
    {
        if (episodes is null || orderflow.CurrentAuction is null)
            return Array.Empty<AuctionEfficiencyEvidenceSnapshot>();

        var list = new List<AuctionEfficiencyEvidenceSnapshot>();
        var ofByEp = orderflow.ActiveEpisodeAggregates
            .GroupBy(e => e.EpisodeId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        var clByEp = cluster?.CurrentAuction?.ActiveEpisodeSnapshots
            .GroupBy(e => e.EpisodeId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal)
            ?? new Dictionary<string, ClusterRawEpisodeSnapshot>(StringComparer.Ordinal);
        var evByEp = evidence?.ActiveEvidence
            .GroupBy(e => e.EpisodeId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal)
            ?? new Dictionary<string, AcceptanceReentryEvidenceSnapshot>(StringComparer.Ordinal);

        foreach (var ep in episodes.ActiveEpisodes)
        {
            if (!ofByEp.TryGetValue(ep.EpisodeId, out var ofEp))
                continue;

            clByEp.TryGetValue(ep.EpisodeId, out var clEp);
            evByEp.TryGetValue(ep.EpisodeId, out var evEp);

            var id = EfficiencyIdentity.BuildEpisode(ep.EpisodeId);
            var anchors = EnsureAnchors(id, profiles, now);
            var effort = BuildEffortFromEpisode(ofEp, clEp, cluster?.CurrentAuction);
            var direction = MapEpisodeDirection(ep.InteractionDirection);
            var result = BuildResult(
                ofEp.FirstPriceTick, ofEp.LastPriceTick, ofEp.HighPriceTick, ofEp.LowPriceTick,
                ofEp.NetPriceProgressTicks, direction, ep, evEp, anchors, profiles);
            var rel = BuildRelationships(effort, result, ofEp.FirstEventAtUtc, ofEp.LastEventAtUtc);
            var status = ResolveStatus(effort, result, hasEpisode: true);
            if (ep.ReferenceRole == ReferenceInteractionRole.Centerline)
                status = EfficiencyModuleState.Partial;
            var quality = status == EfficiencyModuleState.Ready ? EfficiencyDataQuality.Complete : EfficiencyDataQuality.Partial;
            var avail = effort.EvidenceAvailability == EfficiencyAvailability.Available
                        && result.EvidenceAvailability != EfficiencyAvailability.Unavailable
                ? (status == EfficiencyModuleState.Ready ? EfficiencyAvailability.Available : EfficiencyAvailability.Partial)
                : EfficiencyAvailability.Partial;

            list.Add(PublishEvidence(
                id, EfficiencyScopeType.ActiveEpisode, ep.PrimaryAuctionId, ep.EpisodeId,
                ep.ReferenceId, ep.ReferenceRole, orderflow.CurrentAuction, effort, result, rel,
                direction, status, quality, avail, fpText, now));
        }

        return list;
    }

    private AuctionEfficiencyEvidenceSnapshot PublishEvidence(
        string id,
        EfficiencyScopeType scope,
        string primaryAuctionId,
        string? episodeId,
        string? referenceId,
        ReferenceInteractionRole? role,
        ExecutedOrderflowAuctionSnapshot ofAuction,
        AuctionEffortEvidenceVector effort,
        AuctionResultEvidenceVector result,
        AuctionEfficiencyRawRelationships rel,
        EfficiencyResultDirection direction,
        EfficiencyModuleState status,
        EfficiencyDataQuality quality,
        EfficiencyAvailability avail,
        string fpText,
        DateTime now)
    {
        if (!_states.TryGetValue(id, out var st))
        {
            st = new MutableEvidenceState(1, 1, now, null, false);
        }
        else if (st.IsFrozen)
        {
            return st.LastPublished!;
        }
        else
        {
            var prev = st.LastPublished;
            var metricChanged = prev is null
                || prev.Effort.TotalExecutedVolume != effort.TotalExecutedVolume
                || prev.Effort.TradeCount != effort.TradeCount
                || prev.Result.NetPriceProgressTicks != result.NetPriceProgressTicks
                || prev.Result.GrossRangeTicks != result.GrossRangeTicks
                || prev.Result.ProgressRetainedTicks != result.ProgressRetainedTicks;
            var statusChanged = prev is null
                || prev.MeasurementStatus != status
                || prev.Availability != avail
                || prev.DataQuality != quality
                || prev.CoverageMode != ofAuction.CoverageMode;

            if (!metricChanged && !statusChanged && prev is not null && string.Equals(prev.InputFingerprint, fpText, StringComparison.Ordinal))
                return prev;

            var stateVer = st.StateVersion;
            var eventRev = st.EventRevision;
            if (metricChanged) eventRev++;
            if (statusChanged)
            {
                stateVer++;
                if (!metricChanged) eventRev++;
            }

            st = new MutableEvidenceState(stateVer, eventRev, st.ObservationStartedAtUtc, prev, false);
        }

        var lim = new List<string>(effort.Limitations);
        foreach (var l in result.Limitations)
        {
            if (!lim.Contains(l, StringComparer.Ordinal))
                lim.Add(l);
        }
        lim.Add(AuctionEfficiencyEvidencePolicyConfig.LimitationClassificationNotCalibrated);
        lim.Add(AuctionEfficiencyEvidencePolicyConfig.LimitationNoEffortResult);
        lim.Add(AuctionEfficiencyEvidencePolicyConfig.LimitationNoTradeFacilitation);

        var snap = new AuctionEfficiencyEvidenceSnapshot(
            id,
            AuctionEfficiencyEvidencePolicyConfig.PolicyVersion,
            scope,
            primaryAuctionId,
            episodeId,
            referenceId,
            role,
            ofAuction.InstrumentIdentity,
            ofAuction.DataEpoch,
            ofAuction.TickSize,
            ofAuction.TimestampPolicy,
            status,
            EfficiencyClassificationState.NotCalibrated,
            st.ObservationStartedAtUtc,
            ofAuction.FirstEventAtUtc,
            ofAuction.LastEventAtUtc,
            ofAuction.CoverageMode,
            direction,
            effort,
            result,
            rel,
            st.StateVersion,
            st.EventRevision,
            quality,
            avail,
            lim,
            fpText,
            isFrozen: false);

        _states[id] = st with { LastPublished = snap };
        return snap;
    }

    private ProfileStartAnchors EnsureAnchors(string evidenceId, PrimaryProfileSetSnapshot? profiles, DateTime now)
    {
        if (_anchors.TryGetValue(evidenceId, out var existing))
            return existing;

        var cur = profiles?.CurrentAuction;
        long? tpoPoc = ToTick(cur?.TpoProfile?.TpoPoc);
        long? volPoc = ToTick(cur?.VolumeProfile?.VolumePoc);
        long? tpoLow = ToTick(cur?.TpoProfile?.TpoVal);
        long? tpoHigh = ToTick(cur?.TpoProfile?.TpoVah);
        long? volLow = ToTick(cur?.VolumeProfile?.VolumeVal);
        long? volHigh = ToTick(cur?.VolumeProfile?.VolumeVah);
        var a = new ProfileStartAnchors(tpoPoc, volPoc, tpoLow, tpoHigh, volLow, volHigh, now);
        _anchors[evidenceId] = a;
        return a;
    }

    private long? ToTick(decimal? price)
    {
        if (!price.HasValue || _tickSize <= 0m) return null;
        return (long)decimal.Round(price.Value / _tickSize, MidpointRounding.AwayFromZero);
    }

    private static EfficiencyResultDirection ResolveDirection(long? first, long? latest, EpisodeInteractionDirection? epDir)
    {
        if (epDir == EpisodeInteractionDirection.Up) return EfficiencyResultDirection.Up;
        if (epDir == EpisodeInteractionDirection.Down) return EfficiencyResultDirection.Down;
        if (!first.HasValue || !latest.HasValue) return EfficiencyResultDirection.Unknown;
        if (latest > first) return EfficiencyResultDirection.Up;
        if (latest < first) return EfficiencyResultDirection.Down;
        return EfficiencyResultDirection.Flat;
    }

    private static EfficiencyResultDirection MapEpisodeDirection(EpisodeInteractionDirection d) =>
        d switch
        {
            EpisodeInteractionDirection.Up => EfficiencyResultDirection.Up,
            EpisodeInteractionDirection.Down => EfficiencyResultDirection.Down,
            _ => EfficiencyResultDirection.Unknown
        };

    private static EfficiencyModuleState ResolveStatus(
        AuctionEffortEvidenceVector effort,
        AuctionResultEvidenceVector result,
        bool hasEpisode)
    {
        if (effort.TotalExecutedVolume <= 0m || effort.TradeCount <= 0)
            return EfficiencyModuleState.AwaitingOrderflow;
        if (effort.AskVolume is null || effort.BidVolume is null
            || effort.UnknownAggressorVolume > 0m
            || !result.FirstPriceTick.HasValue
            || !result.LatestPriceTick.HasValue
            || result.MaximumFavorableProgressTicks is null && hasEpisode)
            return EfficiencyModuleState.Partial;
        if (!hasEpisode)
            return EfficiencyModuleState.Partial; // auction-only is Partial until Episode when required; still publish
        return EfficiencyModuleState.Ready;
    }

    private AuctionEffortEvidenceVector BuildEffort(
        ExecutedOrderflowAuctionSnapshot ofA,
        ClusterRawAuctionSnapshot? clA,
        IReadOnlyList<ExecutedPriceLevelSnapshot> levels)
    {
        decimal? ask = ofA.UnknownAggressorVolume > 0m && ofA.AskVolume == 0m ? null : ofA.AskVolume;
        decimal? bid = ofA.UnknownAggressorVolume > 0m && ofA.BidVolume == 0m ? null : ofA.BidVolume;
        if (ofA.AggressorClassificationStatus == AggressorClassificationStatus.Complete)
        {
            ask = ofA.AskVolume;
            bid = ofA.BidVolume;
        }

        decimal? absDelta = ask.HasValue && bid.HasValue ? Math.Abs(ask.Value - bid.Value) : null;
        if (ofA.UnknownAggressorVolume > 0m && ofA.AskVolume == 0m && ofA.BidVolume == 0m)
            absDelta = null;

        TimeSpan? duration = null;
        if (ofA.FirstEventAtUtc.HasValue && ofA.LastEventAtUtc.HasValue)
            duration = ofA.LastEventAtUtc.Value - ofA.FirstEventAtUtc.Value;

        decimal? cps = null;
        if (duration is { TotalSeconds: > 0 })
            cps = ofA.ExecutedVolume / (decimal)duration.Value.TotalSeconds;

        var maxVol = levels.Count == 0 ? (decimal?)null : levels.Max(l => l.ExecutedVolume);
        var maxTrades = levels.Count == 0 ? (long?)null : levels.Max(l => l.TradeCount);

        int samePriceAvail = 0, diagAvail = 0, askDom = 0, bidDom = 0, equalDom = 0, unkDom = 0;
        int maxAskRun = 0, maxBidRun = 0, revisited = 0, maxVisit = 0, classifiedLevels = 0, unknownOnly = 0;
        decimal? maxAbsLevelDelta = null;
        var pop = clA?.PopulationSize ?? levels.Count;

        if (clA is not null)
        {
            classifiedLevels = clA.ClassifiedLevelCount;
            unknownOnly = clA.UnknownOnlyLevelCount;
            foreach (var lvl in clA.PriceLevels)
            {
                if (lvl.SamePriceAskToBidRatio.HasValue || lvl.SamePriceBidToAskRatio.HasValue) samePriceAvail++;
                if (lvl.DiagonalAskToBidBelowRatio.HasValue || lvl.DiagonalBidToAskAboveRatio.HasValue) diagAvail++;
                switch (lvl.RawDominantSide)
                {
                    case ClusterRawDominantSide.Ask:
                        askDom++;
                        maxAskRun = Math.Max(maxAskRun, lvl.ConsecutiveRawDominanceTicks);
                        break;
                    case ClusterRawDominantSide.Bid:
                        bidDom++;
                        maxBidRun = Math.Max(maxBidRun, lvl.ConsecutiveRawDominanceTicks);
                        break;
                    case ClusterRawDominantSide.Equal: equalDom++; break;
                    default: unkDom++; break;
                }
                if (lvl.RevisitCount > 0) revisited++;
                maxVisit = Math.Max(maxVisit, lvl.VisitCount);
                if (lvl.AskVolume.HasValue && lvl.BidVolume.HasValue)
                {
                    var ad = Math.Abs(lvl.AskVolume.Value - lvl.BidVolume.Value);
                    maxAbsLevelDelta = maxAbsLevelDelta.HasValue ? Math.Max(maxAbsLevelDelta.Value, ad) : ad;
                }
            }
        }
        else
        {
            unknownOnly = levels.Count(l => l.UnknownAggressorVolume > 0m && l.AskVolume == 0m && l.BidVolume == 0m);
        }

        var lim = new List<string>
        {
            AuctionEfficiencyEvidencePolicyConfig.LimitationHistoryLiveOnly,
            AuctionEfficiencyEvidencePolicyConfig.LimitationImbalanceNotCalibrated,
            AuctionEfficiencyEvidencePolicyConfig.LimitationStackedNotCalibrated,
            AuctionEfficiencyEvidencePolicyConfig.LimitationBigTradeNotCalibrated,
            AuctionEfficiencyEvidencePolicyConfig.LimitationTapeSpeedNotCalibrated,
            AuctionEfficiencyEvidencePolicyConfig.LimitationMboSweepResearchOnly,
            AuctionEfficiencyEvidencePolicyConfig.LimitationStopResearchOnly,
            AuctionEfficiencyEvidencePolicyConfig.LimitationIcebergResearchOnly
        };
        if (!ask.HasValue) lim.Add(AuctionEfficiencyEvidencePolicyConfig.LimitationAskUnavailable);
        if (!bid.HasValue) lim.Add(AuctionEfficiencyEvidencePolicyConfig.LimitationBidUnavailable);
        if (ofA.UnknownAggressorVolume > 0m) lim.Add(AuctionEfficiencyEvidencePolicyConfig.LimitationAggressorPartial);
        if (ofA.CoverageMode == OrderflowCoverageMode.LiveOnlyMidAuction)
            lim.Add(AuctionEfficiencyEvidencePolicyConfig.LimitationMidAuction);

        var avail = !ask.HasValue || !bid.HasValue || ofA.UnknownAggressorVolume > 0m
            ? EfficiencyAvailability.Partial
            : EfficiencyAvailability.Available;

        return new AuctionEffortEvidenceVector(
            ofA.ExecutedVolume, ofA.TradeCount, levels.Count, ofA.ClassifiedVolume,
            ask, bid, ofA.UnknownAggressorVolume, ofA.ClassifiedDelta, absDelta,
            ofA.ClassifiedCvd, ofA.AggressorCoverageRatio, duration,
            ofA.MinimumTradeInterval, ofA.MaximumTradeInterval, ofA.MeanTradeInterval, ofA.LatestTradeInterval,
            ofA.TradesPerSecondRaw, cps, maxVol, maxTrades, maxAbsLevelDelta,
            revisited, maxVisit, classifiedLevels, unknownOnly, samePriceAvail, diagAvail,
            askDom, bidDom, equalDom, unkDom, maxAskRun, maxBidRun, pop, avail, lim);
    }

    private AuctionEffortEvidenceVector BuildEffortFromEpisode(
        ExecutedOrderflowEpisodeSnapshot ofEp,
        ClusterRawEpisodeSnapshot? clEp,
        ClusterRawAuctionSnapshot? clAuction)
    {
        decimal? ask = ofEp.UnknownAggressorVolume > 0m && ofEp.AskVolume == 0m ? null : ofEp.AskVolume;
        decimal? bid = ofEp.UnknownAggressorVolume > 0m && ofEp.BidVolume == 0m ? null : ofEp.BidVolume;
        if (ofEp.DataQuality == OrderflowDataQuality.Complete)
        {
            ask = ofEp.AskVolume;
            bid = ofEp.BidVolume;
        }

        decimal? absDelta = ask.HasValue && bid.HasValue ? Math.Abs(ask.Value - bid.Value) : null;
        TimeSpan? duration = null;
        if (ofEp.FirstEventAtUtc.HasValue && ofEp.LastEventAtUtc.HasValue)
            duration = ofEp.LastEventAtUtc.Value - ofEp.FirstEventAtUtc.Value;

        var lim = new List<string>
        {
            AuctionEfficiencyEvidencePolicyConfig.LimitationHistoryLiveOnly,
            AuctionEfficiencyEvidencePolicyConfig.LimitationImbalanceNotCalibrated,
            AuctionEfficiencyEvidencePolicyConfig.LimitationBigTradeNotCalibrated
        };
        if (!ask.HasValue) lim.Add(AuctionEfficiencyEvidencePolicyConfig.LimitationAskUnavailable);
        if (!bid.HasValue) lim.Add(AuctionEfficiencyEvidencePolicyConfig.LimitationBidUnavailable);

        return new AuctionEffortEvidenceVector(
            ofEp.ExecutedVolume, ofEp.TradeCount, clEp?.PriceLevelCount ?? 0, ofEp.ClassifiedVolume,
            ask, bid, ofEp.UnknownAggressorVolume, ofEp.ClassifiedDelta, absDelta,
            null, ofEp.AggressorCoverageRatio, duration,
            null, null, null, null, null, null,
            null, null,
            clEp?.MaximumAbsoluteDeltaPriceTick is not null ? absDelta : null,
            clEp?.RevisitedLevelCount ?? 0, clEp?.MaximumVisitCount ?? 0,
            clEp?.ClassifiedLevelCount ?? 0, clEp?.UnknownOnlyLevelCount ?? 0,
            0, 0, 0, 0, 0, 0, 0, 0, clAuction?.PopulationSize ?? 0,
            ask.HasValue && bid.HasValue ? EfficiencyAvailability.Available : EfficiencyAvailability.Partial,
            lim);
    }

    private AuctionResultEvidenceVector BuildResult(
        long? first, long? latest, long? high, long? low, long? netProgress,
        EfficiencyResultDirection direction,
        AuctionEpisodeSnapshot? episode,
        AcceptanceReentryEvidenceSnapshot? evidence,
        ProfileStartAnchors anchors,
        PrimaryProfileSetSnapshot? profiles)
    {
        long? gross = high.HasValue && low.HasValue ? high.Value - low.Value : null;
        long? fav = null, adv = null, retained = null;
        if (first.HasValue && high.HasValue && low.HasValue)
        {
            if (direction == EfficiencyResultDirection.Up)
            {
                fav = high.Value - first.Value;
                adv = first.Value - low.Value;
                if (latest.HasValue) retained = latest.Value - first.Value;
            }
            else if (direction == EfficiencyResultDirection.Down)
            {
                fav = first.Value - low.Value;
                adv = high.Value - first.Value;
                if (latest.HasValue) retained = first.Value - latest.Value;
            }
        }

        decimal? retentionRatio = null;
        if (retained.HasValue && fav.HasValue)
            retentionRatio = EfficiencyRatio.TryDivide((decimal)retained.Value, fav.Value);

        long? tpoLatest = ToTick(profiles?.CurrentAuction?.TpoProfile?.TpoPoc);
        long? volLatest = ToTick(profiles?.CurrentAuction?.VolumeProfile?.VolumePoc);
        long? tpoLowL = ToTick(profiles?.CurrentAuction?.TpoProfile?.TpoVal);
        long? tpoHighL = ToTick(profiles?.CurrentAuction?.TpoProfile?.TpoVah);
        long? volLowL = ToTick(profiles?.CurrentAuction?.VolumeProfile?.VolumeVal);
        long? volHighL = ToTick(profiles?.CurrentAuction?.VolumeProfile?.VolumeVah);

        long? tpoMig = DiffAbs(anchors.TpoPocStart, tpoLatest);
        long? volMig = DiffAbs(anchors.VolumePocStart, volLatest);
        long? tpoCentStart = Centroid(anchors.TpoValueLowStart, anchors.TpoValueHighStart);
        long? tpoCentLatest = Centroid(tpoLowL, tpoHighL);
        long? volCentStart = Centroid(anchors.VolumeValueLowStart, anchors.VolumeValueHighStart);
        long? volCentLatest = Centroid(volLowL, volHighL);

        var lim = new List<string>
        {
            AuctionEfficiencyEvidencePolicyConfig.LimitationClosePositionUnavailable,
            AuctionEfficiencyEvidencePolicyConfig.LimitationHistoryLiveOnly
        };
        if (direction == EfficiencyResultDirection.Unknown)
            lim.Add(AuctionEfficiencyEvidencePolicyConfig.LimitationDirectionUnavailable);

        var avail = first.HasValue && latest.HasValue
            ? (direction == EfficiencyResultDirection.Unknown ? EfficiencyAvailability.Partial : EfficiencyAvailability.Available)
            : EfficiencyAvailability.Unavailable;

        return new AuctionResultEvidenceVector(
            first, latest, high, low, netProgress, gross, fav, adv, retained, retentionRatio,
            null, null, null,
            episode is null || !first.HasValue ? null : Math.Abs(first.Value - episode.ReferencePriceTick),
            episode is null ? null : Math.Abs((latest ?? first ?? 0) - episode.ReferencePriceTick),
            episode?.MaximumCanonicalOutsideDistanceTicks
                ?? (episode is null ? null : Math.Max(episode.MaximumAboveDistanceTicks, episode.MaximumBelowDistanceTicks)),
            evidence?.Acceptance.CurrentDistanceFromReferenceTicks,
            evidence?.Reentry.GeometricReentryObserved,
            evidence?.Reentry.TimeMaintainedInside > TimeSpan.Zero ? evidence.Reentry.TimeMaintainedInside : null,
            evidence?.Acceptance.OutsideTimeRatio,
            evidence?.Acceptance.OutsideVolumeRatio,
            evidence?.Acceptance.OutsideTradeCountRatio,
            evidence?.Acceptance.LocalPocTick,
            evidence?.Acceptance.LocalPocDisplacementTicks,
            anchors.TpoPocStart, tpoLatest, tpoMig,
            anchors.VolumePocStart, volLatest, volMig,
            anchors.TpoValueLowStart, anchors.TpoValueHighStart, tpoLowL, tpoHighL,
            anchors.VolumeValueLowStart, anchors.VolumeValueHighStart, volLowL, volHighL,
            DiffAbs(tpoCentStart, tpoCentLatest),
            DiffAbs(volCentStart, volCentLatest),
            null, null, avail, lim);
    }

    private static long? DiffAbs(long? a, long? b) =>
        a.HasValue && b.HasValue ? Math.Abs(b.Value - a.Value) : null;

    private static long? Centroid(long? low, long? high) =>
        low.HasValue && high.HasValue ? (low.Value + high.Value) / 2 : null;

    private static AuctionEfficiencyRawRelationships BuildRelationships(
        AuctionEffortEvidenceVector effort,
        AuctionResultEvidenceVector result,
        DateTime? firstAt,
        DateTime? lastAt)
    {
        TimeSpan? duration = null;
        if (firstAt.HasValue && lastAt.HasValue)
            duration = lastAt.Value - firstAt.Value;

        var net = result.NetPriceProgressTicks;
        var gross = result.GrossRangeTicks;
        var fav = result.MaximumFavorableProgressTicks;
        var absDelta = effort.AbsoluteClassifiedDelta;

        return new AuctionEfficiencyRawRelationships(
            EfficiencyRatio.TryDivide(net, effort.TotalExecutedVolume),
            EfficiencyRatio.TryDivide(gross, effort.TotalExecutedVolume),
            EfficiencyRatio.TryDivide(fav, effort.TotalExecutedVolume),
            EfficiencyRatio.TryDivide(net, effort.TradeCount),
            EfficiencyRatio.TryDivide(fav, effort.TradeCount),
            EfficiencyRatio.TryDivide(effort.TotalExecutedVolume, gross),
            absDelta.HasValue ? EfficiencyRatio.TryDivide(absDelta.Value, gross) : null,
            absDelta.HasValue ? EfficiencyRatio.TryDivide(absDelta.Value, fav) : null,
            duration.HasValue && net.HasValue ? EfficiencyRatio.TryDivide(duration.Value, net.Value) : null,
            duration.HasValue && fav.HasValue ? EfficiencyRatio.TryDivide(duration.Value, fav.Value) : null,
            EfficiencyRatio.TryDivide((decimal)effort.TradeCount, net),
            EfficiencyRatio.TryDivide(effort.TotalExecutedVolume, net));
    }

    private EfficiencyInputFingerprint BuildFingerprint(
        ExecutedOrderflowSetSnapshot? orderflow,
        ClusterRawSetSnapshot? cluster,
        AuctionEpisodeSetSnapshot? episodes,
        AcceptanceReentryEvidenceSetSnapshot? evidence,
        PrimaryProfileSetSnapshot? profiles)
    {
        var ofA = orderflow?.CurrentAuction;
        var clA = cluster?.CurrentAuction;
        var epKey = episodes is null
            ? ""
            : string.Join(",", episodes.ActiveEpisodes.Select(e => e.EpisodeId + ":" + e.EventRevision));
        var evKey = evidence is null
            ? ""
            : string.Join(",", evidence.ActiveEvidence.Select(e => e.EvidenceId + ":" + e.EventRevision));
        var pf = profiles?.CurrentAuction;
        var pfKey = pf is null
            ? ""
            : (pf.AuctionId + ":" + (pf.TpoProfile?.TpoPoc?.ToString() ?? "") + ":" + (pf.VolumeProfile?.VolumePoc?.ToString() ?? ""));

        return new EfficiencyInputFingerprint(
            _policy.Enabled,
            ofA?.SnapshotId ?? "",
            ofA?.EventRevision ?? 0,
            ofA?.StateVersion ?? 0,
            clA?.SnapshotId ?? "",
            clA?.EventRevision ?? 0,
            clA?.StateVersion ?? 0,
            epKey, evKey, pfKey,
            ofA?.PrimaryAuctionId ?? "",
            _contractEpoch,
            _tickSize,
            _timestampPolicyVersion,
            AuctionEfficiencyEvidencePolicyConfig.PolicyVersion);
    }

    private AuctionEfficiencyEvidenceSetSnapshot DisabledSnapshot(DateTime now) =>
        StatusSnapshot(EfficiencyModuleState.Disabled, now, Array.Empty<string>());

    private AuctionEfficiencyEvidenceSetSnapshot StatusSnapshot(
        EfficiencyModuleState state,
        DateTime now,
        IReadOnlyList<string> limitations)
    {
        if (_createdAtUtc == default)
            _createdAtUtc = now;
        return new AuctionEfficiencyEvidenceSetSnapshot(
            state, AuctionEfficiencyEvidencePolicyConfig.PolicyVersion, null,
            Array.Empty<AuctionEfficiencyEvidenceSnapshot>(),
            Array.Empty<AuctionEfficiencyEvidenceSnapshot>(),
            null, 0, 0, state == EfficiencyModuleState.Invalid ? 1 : 0,
            _lastFingerprint, _rejectedStale, _lastRejectionReason,
            _createdAtUtc, now, limitations);
    }

    private readonly record struct ProfileStartAnchors(
        long? TpoPocStart,
        long? VolumePocStart,
        long? TpoValueLowStart,
        long? TpoValueHighStart,
        long? VolumeValueLowStart,
        long? VolumeValueHighStart,
        DateTime CapturedAtUtc);

    private readonly record struct MutableEvidenceState(
        long StateVersion,
        long EventRevision,
        DateTime ObservationStartedAtUtc,
        AuctionEfficiencyEvidenceSnapshot? LastPublished,
        bool IsFrozen);
}
