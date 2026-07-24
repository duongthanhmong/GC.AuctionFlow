using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Orderflow;

namespace GC.AuctionFlow.Cluster;

internal sealed class MutableVisitState
{
    public int VisitCount { get; set; }
    public DateTime? FirstVisitAtUtc { get; set; }
    public DateTime? LastVisitAtUtc { get; set; }
    public long FirstVisitPriceTick { get; set; }
}

/// <summary>
/// Builds Cluster Raw auction measurements from Phase 2A immutable price levels.
/// Traversal policy: ascending PriceTick; consecutive dominance requires adjacent
/// traded ticks with |Δtick|=1 and identical Ask/Bid RawDominantSide (Equal/Unknown break).
/// </summary>
internal sealed class MutableClusterAuction
{
    private readonly Dictionary<long, MutableVisitState> _visits = new();
    private readonly HashSet<string> _seenEventIds = new(StringComparer.Ordinal);
    private long? _lastVisitTick;
    private string? _latestUpdatedLevelId;
    private long _stateVersion = 1;
    private long _eventRevision = 1;
    private long _appliedOrderflowEventRevision = -1;
    private string _orderflowAuctionSnapshotId = "";
    private ClusterRawAuctionSnapshot? _lastPublishedAuction;
    private readonly List<ClusterRawEpisodeSnapshot> _closedEpisodes = new();

    public MutableClusterAuction(
        string instrumentIdentity,
        string dataEpoch,
        string primaryAuctionId,
        decimal tickSize,
        string timestampPolicy,
        DateTime observationStartedAtUtc)
    {
        InstrumentIdentity = instrumentIdentity;
        DataEpoch = dataEpoch;
        PrimaryAuctionId = primaryAuctionId;
        TickSize = tickSize;
        TimestampPolicy = timestampPolicy;
        ObservationStartedAtUtc = observationStartedAtUtc;
    }

    public string InstrumentIdentity { get; }
    public string DataEpoch { get; }
    public string PrimaryAuctionId { get; }
    public decimal TickSize { get; }
    public string TimestampPolicy { get; }
    public DateTime ObservationStartedAtUtc { get; }

    public long StateVersion => _stateVersion;
    public long EventRevision => _eventRevision;
    public long AppliedOrderflowEventRevision => _appliedOrderflowEventRevision;
    public ClusterRawAuctionSnapshot? LastPublishedAuction => _lastPublishedAuction;

    public bool TryNoteAcceptedEvent(string? eventId, long priceTick, DateTime atUtc)
    {
        if (!string.IsNullOrEmpty(eventId))
        {
            if (!_seenEventIds.Add(eventId))
                return false;
        }

        if (!_visits.TryGetValue(priceTick, out var vs))
        {
            vs = new MutableVisitState
            {
                VisitCount = 1,
                FirstVisitAtUtc = atUtc,
                LastVisitAtUtc = atUtc,
                FirstVisitPriceTick = priceTick
            };
            _visits[priceTick] = vs;
            _lastVisitTick = priceTick;
            return true;
        }

        if (_lastVisitTick.HasValue && _lastVisitTick.Value == priceTick)
        {
            vs.LastVisitAtUtc = atUtc;
            _lastVisitTick = priceTick;
            return true;
        }

        // Price returned after visiting a different tick → new visit.
        vs.VisitCount++;
        vs.LastVisitAtUtc = atUtc;
        _lastVisitTick = priceTick;
        return true;
    }

    public ClusterRawAuctionSnapshot RebuildFromOrderflow(
        ExecutedOrderflowSetSnapshot orderflow,
        long? changedPriceTick,
        DateTime nowUtc)
    {
        var ofAuction = orderflow.CurrentAuction
            ?? throw new InvalidOperationException("ORDERFLOW_AUCTION_REQUIRED");

        if (!string.Equals(_orderflowAuctionSnapshotId, ofAuction.SnapshotId, StringComparison.Ordinal))
        {
            _orderflowAuctionSnapshotId = ofAuction.SnapshotId;
            _stateVersion++;
            _eventRevision++;
        }
        else if (_appliedOrderflowEventRevision == ofAuction.EventRevision
                 && _lastPublishedAuction is not null)
        {
            return _lastPublishedAuction;
        }
        else
        {
            _eventRevision++;
        }

        _appliedOrderflowEventRevision = ofAuction.EventRevision;
        var clusterId = ClusterRawIdentity.BuildAuction(ofAuction.SnapshotId);
        var levelsOf = orderflow.PriceLevels
            .OrderBy(p => p.PriceTick)
            .ToArray();

        var askAvail = new bool[levelsOf.Length];
        var bidAvail = new bool[levelsOf.Length];
        var askVol = new decimal?[levelsOf.Length];
        var bidVol = new decimal?[levelsOf.Length];
        var sides = new ClusterRawDominantSide[levelsOf.Length];
        var byTick = new Dictionary<long, int>(levelsOf.Length);

        for (var i = 0; i < levelsOf.Length; i++)
        {
            var px = levelsOf[i];
            byTick[px.PriceTick] = i;
            ResolveAskBid(px, out askAvail[i], out bidAvail[i], out askVol[i], out bidVol[i]);
            sides[i] = ResolveDominant(askAvail[i], bidAvail[i], askVol[i], bidVol[i]);
        }

        var volumes = levelsOf.Select(p => p.ExecutedVolume).ToArray();
        var tradeCounts = levelsOf.Select(p => (decimal)p.TradeCount).ToArray();
        EmpiricalMidrankV1.Compute(volumes, out var volRanks, out var volPct, out var pop);
        EmpiricalMidrankV1.Compute(tradeCounts, out var tcRanks, out var tcPct, out _);

        var absDeltaValues = new List<decimal>(levelsOf.Length);
        var absDeltaIndexMap = new List<int>(levelsOf.Length);
        for (var i = 0; i < levelsOf.Length; i++)
        {
            if (askAvail[i] && bidAvail[i])
            {
                absDeltaValues.Add(Math.Abs((askVol[i] ?? 0m) - (bidVol[i] ?? 0m)));
                absDeltaIndexMap.Add(i);
            }
        }

        var absDeltaRankByLevel = new int?[levelsOf.Length];
        var absDeltaPctByLevel = new decimal?[levelsOf.Length];
        if (absDeltaValues.Count > 0)
        {
            EmpiricalMidrankV1.Compute(absDeltaValues, out var adr, out var adp, out _);
            for (var k = 0; k < absDeltaIndexMap.Count; k++)
            {
                var li = absDeltaIndexMap[k];
                absDeltaRankByLevel[li] = adr[k];
                absDeltaPctByLevel[li] = adp[k];
            }
        }

        var runLens = ComputeConsecutiveRuns(levelsOf, sides);
        var built = new ClusterRawPriceLevelSnapshot[levelsOf.Length];
        var classifiedLevels = 0;
        var partialLevels = 0;
        var unknownOnly = 0;

        for (var i = 0; i < levelsOf.Length; i++)
        {
            var px = levelsOf[i];
            _visits.TryGetValue(px.PriceTick, out var visit);
            var visitCount = visit?.VisitCount ?? 0;
            if (visitCount == 0 && px.TradeCount > 0)
            {
                // Seed visit from Phase 2A first/last when module enabled mid-stream without event fan-out.
                visitCount = 1;
                visit = new MutableVisitState
                {
                    VisitCount = 1,
                    FirstVisitAtUtc = px.FirstEventAtUtc,
                    LastVisitAtUtc = px.LastEventAtUtc,
                    FirstVisitPriceTick = px.PriceTick
                };
                _visits[px.PriceTick] = visit;
            }

            var sameAb = ClusterRawRatio.TryCompute(askVol[i], bidVol[i], out _);
            var sameBa = ClusterRawRatio.TryCompute(bidVol[i], askVol[i], out _);

            decimal? diagAskBelow = null;
            decimal? diagBidAbove = null;
            if (byTick.TryGetValue(px.PriceTick - 1, out var belowIdx) && askAvail[i] && bidAvail[belowIdx])
                diagAskBelow = ClusterRawRatio.TryCompute(askVol[i], bidVol[belowIdx], out _);
            if (byTick.TryGetValue(px.PriceTick + 1, out var aboveIdx) && bidAvail[i] && askAvail[aboveIdx])
                diagBidAbove = ClusterRawRatio.TryCompute(bidVol[i], askVol[aboveIdx], out _);

            decimal? dominanceDiff = null;
            if (askAvail[i] && bidAvail[i])
                dominanceDiff = Math.Abs((askVol[i] ?? 0m) - (bidVol[i] ?? 0m));

            var classified = (askVol[i] ?? 0m) + (bidVol[i] ?? 0m);
            var classifiedDelta = askAvail[i] && bidAvail[i]
                ? (askVol[i] ?? 0m) - (bidVol[i] ?? 0m)
                : 0m;
            var coverage = OrderflowRatio.TryCompute(classified, px.ExecutedVolume);

            var lim = new List<string>
            {
                ClusterRawFeaturePolicyConfig.LimitationNoImbalance,
                ClusterRawFeaturePolicyConfig.LimitationNoStackedImbalance,
                ClusterRawFeaturePolicyConfig.LimitationNoExtreme,
                ClusterRawFeaturePolicyConfig.LimitationNoBigTrade,
                ClusterRawFeaturePolicyConfig.LimitationClassificationNotCalibrated,
                ClusterRawFeaturePolicyConfig.LimitationClosePositionUnavailable,
                ClusterRawFeaturePolicyConfig.LimitationHistoryLiveOnly
            };
            if (!askAvail[i]) lim.Add(ClusterRawFeaturePolicyConfig.LimitationAskUnavailable);
            if (!bidAvail[i]) lim.Add(ClusterRawFeaturePolicyConfig.LimitationBidUnavailable);
            if (px.UnknownAggressorVolume > 0m) lim.Add(ClusterRawFeaturePolicyConfig.LimitationAggressorPartial);
            if (ofAuction.CoverageMode == OrderflowCoverageMode.LiveOnlyMidAuction)
                lim.Add(ClusterRawFeaturePolicyConfig.LimitationMidAuction);

            ClusterRawAvailability avail;
            ClusterRawDataQuality levelQuality;
            if (!askAvail[i] && !bidAvail[i] && px.UnknownAggressorVolume > 0m)
            {
                unknownOnly++;
                partialLevels++;
                avail = ClusterRawAvailability.Partial;
                levelQuality = ClusterRawDataQuality.Partial;
            }
            else if (askAvail[i] && bidAvail[i] && px.UnknownAggressorVolume == 0m)
            {
                classifiedLevels++;
                avail = ClusterRawAvailability.Available;
                levelQuality = ClusterRawDataQuality.Complete;
            }
            else
            {
                partialLevels++;
                avail = ClusterRawAvailability.Partial;
                levelQuality = ClusterRawDataQuality.Partial;
            }

            long? progress = null;
            long? distance = null;
            if (visit is not null && ofAuction.LastPriceTick.HasValue)
            {
                progress = ofAuction.LastPriceTick.Value - visit.FirstVisitPriceTick;
                distance = ofAuction.LastPriceTick.Value - px.PriceTick;
            }

            var levelId = ClusterRawIdentity.BuildPriceLevel(clusterId, px.PriceTick);
            if (changedPriceTick.HasValue && changedPriceTick.Value == px.PriceTick)
                _latestUpdatedLevelId = levelId;

            built[i] = new ClusterRawPriceLevelSnapshot(
                levelId,
                ClusterRawFeaturePolicyConfig.PolicyVersion,
                clusterId,
                PrimaryAuctionId,
                px.PriceTick,
                px.DecimalPrice,
                px.ExecutedVolume,
                px.TradeCount,
                askAvail[i] ? askVol[i] : null,
                bidAvail[i] ? bidVol[i] : null,
                px.UnknownAggressorVolume,
                classified,
                classifiedDelta,
                coverage,
                sameAb,
                sameBa,
                diagAskBelow,
                diagBidAbove,
                sides[i],
                dominanceDiff,
                runLens[i],
                volRanks[i],
                absDeltaRankByLevel[i],
                tcRanks[i],
                volPct[i],
                absDeltaPctByLevel[i],
                tcPct[i],
                pop,
                visitCount,
                Math.Max(visitCount - 1, 0),
                visit?.FirstVisitAtUtc ?? px.FirstEventAtUtc,
                visit?.LastVisitAtUtc ?? px.LastEventAtUtc,
                progress,
                distance,
                _stateVersion,
                _eventRevision,
                avail,
                levelQuality,
                lim);
        }

        decimal? totalAsk = ofAuction.AggressorClassificationStatus == AggressorClassificationStatus.Unavailable
            && ofAuction.AskVolume == 0m
                ? null
                : (ofAuction.AskVolume > 0m || ofAuction.AggressorClassificationStatus == AggressorClassificationStatus.Complete
                    ? ofAuction.AskVolume
                    : (ofAuction.AskVolume == 0m && ofAuction.UnknownAggressorVolume > 0m ? null : ofAuction.AskVolume));
        decimal? totalBid = ofAuction.AggressorClassificationStatus == AggressorClassificationStatus.Unavailable
            && ofAuction.BidVolume == 0m
                ? null
                : (ofAuction.BidVolume > 0m || ofAuction.AggressorClassificationStatus == AggressorClassificationStatus.Complete
                    ? ofAuction.BidVolume
                    : (ofAuction.BidVolume == 0m && ofAuction.UnknownAggressorVolume > 0m ? null : ofAuction.BidVolume));

        if (ofAuction.UnknownAggressorVolume > 0m && ofAuction.AskVolume == 0m)
            totalAsk = null;
        if (ofAuction.UnknownAggressorVolume > 0m && ofAuction.BidVolume == 0m)
            totalBid = null;
        if (ofAuction.AggressorClassificationStatus == AggressorClassificationStatus.Complete)
        {
            totalAsk = ofAuction.AskVolume;
            totalBid = ofAuction.BidVolume;
        }

        var auctionLim = new List<string>
        {
            ClusterRawFeaturePolicyConfig.LimitationHistoryLiveOnly,
            ClusterRawFeaturePolicyConfig.LimitationClassificationNotCalibrated,
            ClusterRawFeaturePolicyConfig.LimitationNoImbalance,
            ClusterRawFeaturePolicyConfig.LimitationNoStackedImbalance,
            ClusterRawFeaturePolicyConfig.LimitationNoExtreme,
            ClusterRawFeaturePolicyConfig.LimitationNoBigTrade,
            ClusterRawFeaturePolicyConfig.LimitationNoAbsorption,
            ClusterRawFeaturePolicyConfig.LimitationNoExhaustion,
            ClusterRawFeaturePolicyConfig.LimitationNoEffortResult,
            ClusterRawFeaturePolicyConfig.LimitationNoTradeFacilitation,
            ClusterRawFeaturePolicyConfig.LimitationClosePositionUnavailable
        };
        if (ofAuction.CoverageMode == OrderflowCoverageMode.LiveOnlyMidAuction)
            auctionLim.Add(ClusterRawFeaturePolicyConfig.LimitationMidAuction);
        if (ofAuction.AggressorClassificationStatus != AggressorClassificationStatus.Complete)
            auctionLim.Add(ClusterRawFeaturePolicyConfig.LimitationAggressorPartial);

        var quality = unknownOnly > 0 || partialLevels > 0 || ofAuction.AggressorClassificationStatus != AggressorClassificationStatus.Complete
            ? ClusterRawDataQuality.Partial
            : ClusterRawDataQuality.Complete;

        var activeEps = BuildEpisodeSnapshots(orderflow.ActiveEpisodeAggregates, built, nowUtc);
        SyncClosedEpisodes(orderflow.RecentlyClosedEpisodeAggregates, built, nowUtc);

        var snap = new ClusterRawAuctionSnapshot(
            clusterId,
            ClusterRawFeaturePolicyConfig.PolicyVersion,
            ofAuction.SnapshotId,
            PrimaryAuctionId,
            InstrumentIdentity,
            DataEpoch,
            TickSize,
            TimestampPolicy,
            ofAuction.CoverageMode,
            ofAuction.ObservationStartedAtUtc,
            ofAuction.FirstEventAtUtc,
            ofAuction.LastEventAtUtc,
            built.Length,
            classifiedLevels,
            partialLevels,
            unknownOnly,
            ofAuction.ExecutedVolume,
            ofAuction.TradeCount,
            totalAsk,
            totalBid,
            ofAuction.UnknownAggressorVolume,
            ofAuction.ClassifiedDelta,
            ofAuction.LastPriceTick,
            ofAuction.HighPriceTick,
            ofAuction.LowPriceTick,
            pop,
            _latestUpdatedLevelId,
            activeEps.FirstOrDefault()?.SnapshotId,
            ClusterClassificationState.NotCalibrated,
            _stateVersion,
            _eventRevision,
            quality,
            auctionLim,
            built,
            activeEps,
            _closedEpisodes.TakeLast(ClusterRawSetSnapshot.RecentlyClosedEpisodeCapacity).ToArray());

        _lastPublishedAuction = snap;
        return snap;
    }

    public void MarkStatusChange()
    {
        _stateVersion++;
        _eventRevision++;
    }

    private IReadOnlyList<ClusterRawEpisodeSnapshot> BuildEpisodeSnapshots(
        IReadOnlyList<ExecutedOrderflowEpisodeSnapshot> source,
        ClusterRawPriceLevelSnapshot[] levels,
        DateTime nowUtc)
    {
        var list = new List<ClusterRawEpisodeSnapshot>(source.Count);
        foreach (var ep in source)
            list.Add(BuildOneEpisode(ep, levels));
        return list;
    }

    private void SyncClosedEpisodes(
        IReadOnlyList<ExecutedOrderflowEpisodeSnapshot> closed,
        ClusterRawPriceLevelSnapshot[] levels,
        DateTime nowUtc)
    {
        foreach (var ep in closed)
        {
            var id = ClusterRawIdentity.BuildEpisode(ep.SnapshotId);
            if (_closedEpisodes.Any(c => string.Equals(c.SnapshotId, id, StringComparison.Ordinal)))
                continue;
            _closedEpisodes.Add(BuildOneEpisode(ep, levels));
        }
    }

    private ClusterRawEpisodeSnapshot BuildOneEpisode(
        ExecutedOrderflowEpisodeSnapshot ep,
        ClusterRawPriceLevelSnapshot[] levels)
    {
        // Episode association is authoritative from Phase 2A; price-level cluster metrics
        // for episode scope use auction-wide levels intersecting episode tick range when available.
        var inRange = levels.AsEnumerable();
        if (ep.LowPriceTick.HasValue && ep.HighPriceTick.HasValue)
            inRange = levels.Where(l => l.PriceTick >= ep.LowPriceTick && l.PriceTick <= ep.HighPriceTick);

        var arr = inRange.ToArray();
        long? maxVolTick = arr.Length == 0 ? null : arr.OrderByDescending(l => l.ExecutedVolume).ThenBy(l => l.PriceTick).First().PriceTick;
        long? maxTcTick = arr.Length == 0 ? null : arr.OrderByDescending(l => l.TradeCount).ThenBy(l => l.PriceTick).First().PriceTick;
        long? maxAbsDeltaTick = null;
        var classifiedWithDelta = arr.Where(l => l.AskVolume.HasValue && l.BidVolume.HasValue).ToArray();
        if (classifiedWithDelta.Length > 0)
            maxAbsDeltaTick = classifiedWithDelta
                .OrderByDescending(l => Math.Abs((l.AskVolume ?? 0m) - (l.BidVolume ?? 0m)))
                .ThenBy(l => l.PriceTick)
                .First().PriceTick;

        decimal? ask = ep.UnknownAggressorVolume > 0m && ep.AskVolume == 0m ? null : ep.AskVolume;
        decimal? bid = ep.UnknownAggressorVolume > 0m && ep.BidVolume == 0m ? null : ep.BidVolume;
        if (ep.DataQuality == OrderflowDataQuality.Complete)
        {
            ask = ep.AskVolume;
            bid = ep.BidVolume;
        }

        var unknownOnly = arr.Count(l => !l.AskVolume.HasValue && !l.BidVolume.HasValue && l.UnknownAggressorVolume > 0m);
        var classified = arr.Count(l => l.AskVolume.HasValue && l.BidVolume.HasValue && l.UnknownAggressorVolume == 0m);
        var revisited = arr.Count(l => l.RevisitCount > 0);
        var maxVisit = arr.Length == 0 ? 0 : arr.Max(l => l.VisitCount);

        return new ClusterRawEpisodeSnapshot(
            ClusterRawIdentity.BuildEpisode(ep.SnapshotId),
            ClusterRawFeaturePolicyConfig.PolicyVersion,
            ep.SnapshotId,
            ep.EpisodeId,
            ep.ReferenceId,
            ep.ReferenceRole,
            ep.PrimaryAuctionId,
            ep.ObservationStartedAtUtc,
            ep.FirstEventAtUtc,
            ep.LastEventAtUtc,
            arr.Length,
            ep.ExecutedVolume,
            ep.TradeCount,
            ask,
            bid,
            ep.UnknownAggressorVolume,
            ep.ClassifiedDelta,
            ep.AggressorCoverageRatio,
            maxVolTick,
            maxAbsDeltaTick,
            maxTcTick,
            arr.Length,
            classified,
            unknownOnly,
            revisited,
            maxVisit,
            ep.FirstPriceTick,
            ep.LastPriceTick,
            ep.HighPriceTick,
            ep.LowPriceTick,
            ep.NetPriceProgressTicks,
            _stateVersion,
            _eventRevision,
            ep.DataQuality == OrderflowDataQuality.Complete ? ClusterRawAvailability.Available : ClusterRawAvailability.Partial,
            ep.DataQuality == OrderflowDataQuality.Complete ? ClusterRawDataQuality.Complete : ClusterRawDataQuality.Partial,
            new[]
            {
                ClusterRawFeaturePolicyConfig.LimitationNoAbsorption,
                ClusterRawFeaturePolicyConfig.LimitationNoTradeFacilitation,
                ClusterRawFeaturePolicyConfig.LimitationClassificationNotCalibrated,
                ClusterRawFeaturePolicyConfig.LimitationHistoryLiveOnly
            });
    }

    private static void ResolveAskBid(
        ExecutedPriceLevelSnapshot px,
        out bool askAvailable,
        out bool bidAvailable,
        out decimal? askVolume,
        out decimal? bidVolume)
    {
        var complete = px.UnknownAggressorVolume == 0m
                       && px.AskVolume + px.BidVolume == px.ExecutedVolume
                       && px.ExecutedVolume > 0m;
        askAvailable = complete || px.AskVolume > 0m;
        bidAvailable = complete || px.BidVolume > 0m;
        askVolume = askAvailable ? px.AskVolume : null;
        bidVolume = bidAvailable ? px.BidVolume : null;
    }

    private static ClusterRawDominantSide ResolveDominant(
        bool askAvailable,
        bool bidAvailable,
        decimal? ask,
        decimal? bid)
    {
        if (!askAvailable || !bidAvailable)
            return ClusterRawDominantSide.Unknown;
        var a = ask ?? 0m;
        var b = bid ?? 0m;
        if (a > b) return ClusterRawDominantSide.Ask;
        if (b > a) return ClusterRawDominantSide.Bid;
        return ClusterRawDominantSide.Equal;
    }

    /// <summary>
    /// Ascending tick traversal. Run continues only when next traded tick is exactly ±1
    /// and RawDominantSide is the same Ask or Bid (Equal/Unknown break the run).
    /// </summary>
    private static int[] ComputeConsecutiveRuns(
        ExecutedPriceLevelSnapshot[] levels,
        ClusterRawDominantSide[] sides)
    {
        var runs = new int[levels.Length];
        for (var i = 0; i < levels.Length; i++)
        {
            var side = sides[i];
            if (side is ClusterRawDominantSide.Unknown or ClusterRawDominantSide.Equal)
            {
                runs[i] = 1;
                continue;
            }

            var left = i;
            while (left > 0
                   && levels[left].PriceTick - levels[left - 1].PriceTick == 1
                   && sides[left - 1] == side)
                left--;

            var right = i;
            while (right + 1 < levels.Length
                   && levels[right + 1].PriceTick - levels[right].PriceTick == 1
                   && sides[right + 1] == side)
                right++;

            runs[i] = right - left + 1;
        }

        return runs;
    }
}
