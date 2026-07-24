using GC.AuctionFlow.Episode;

namespace GC.AuctionFlow.Orderflow;

internal sealed class MutablePriceLevel
{
    public MutablePriceLevel(long priceTick, decimal decimalPrice)
    {
        PriceTick = priceTick;
        DecimalPrice = decimalPrice;
        StateVersion = 1;
        EventRevision = 1;
    }

    public long PriceTick { get; }
    public decimal DecimalPrice { get; }
    public decimal ExecutedVolume { get; private set; }
    public long TradeCount { get; private set; }
    public decimal AskVolume { get; private set; }
    public decimal BidVolume { get; private set; }
    public decimal UnknownAggressorVolume { get; private set; }
    public DateTime? FirstEventAtUtc { get; private set; }
    public DateTime? LastEventAtUtc { get; private set; }
    public long StateVersion { get; private set; }
    public long EventRevision { get; private set; }

    public void Apply(ExecutedTradeEvent evt)
    {
        ExecutedVolume += evt.ExecutedVolume;
        TradeCount++;
        FirstEventAtUtc ??= evt.ReceiveTimestampUtc ?? evt.ExchangeTimestampUtc;
        LastEventAtUtc = evt.ReceiveTimestampUtc ?? evt.ExchangeTimestampUtc;
        switch (evt.AggressorSide)
        {
            case AggressorSide.Ask:
                AskVolume += evt.ExecutedVolume;
                break;
            case AggressorSide.Bid:
                BidVolume += evt.ExecutedVolume;
                break;
            default:
                UnknownAggressorVolume += evt.ExecutedVolume;
                break;
        }

        EventRevision++;
    }

    public ExecutedPriceLevelSnapshot ToSnapshot(string auctionSnapshotId)
    {
        var classified = AskVolume + BidVolume;
        var coverage = OrderflowRatio.TryCompute(classified, ExecutedVolume);
        var quality = UnknownAggressorVolume > 0m || !coverage.HasValue || coverage.Value < 1m
            ? OrderflowDataQuality.Partial
            : OrderflowDataQuality.Complete;
        return new ExecutedPriceLevelSnapshot(
            OrderflowIdentity.BuildPriceLevel(auctionSnapshotId, PriceTick),
            PriceTick,
            DecimalPrice,
            ExecutedVolume,
            TradeCount,
            AskVolume,
            BidVolume,
            UnknownAggressorVolume,
            AskVolume - BidVolume,
            coverage,
            FirstEventAtUtc,
            LastEventAtUtc,
            StateVersion,
            EventRevision,
            quality,
            new[]
            {
                ExecutedOrderflowPolicyConfig.LimitationNoImbalance,
                ExecutedOrderflowPolicyConfig.LimitationNoBigTrade
            });
    }
}

internal sealed class MutableEpisodeAggregate
{
    public MutableEpisodeAggregate(
        string episodeId,
        string referenceId,
        ReferenceInteractionRole referenceRole,
        string primaryAuctionId,
        DateTime nowUtc)
    {
        EpisodeId = episodeId;
        SnapshotId = OrderflowIdentity.BuildEpisode(episodeId);
        ReferenceId = referenceId;
        ReferenceRole = referenceRole;
        PrimaryAuctionId = primaryAuctionId;
        ObservationStartedAtUtc = nowUtc;
        StateVersion = 1;
        EventRevision = 1;
        ProcessedEventIds = new HashSet<string>(StringComparer.Ordinal);
    }

    public string EpisodeId { get; }
    public string SnapshotId { get; }
    public string ReferenceId { get; }
    public ReferenceInteractionRole ReferenceRole { get; }
    public string PrimaryAuctionId { get; }
    public DateTime ObservationStartedAtUtc { get; }
    public DateTime? FirstEventAtUtc { get; private set; }
    public DateTime? LastEventAtUtc { get; private set; }
    public decimal ExecutedVolume { get; private set; }
    public long TradeCount { get; private set; }
    public decimal AskVolume { get; private set; }
    public decimal BidVolume { get; private set; }
    public decimal UnknownAggressorVolume { get; private set; }
    public long? FirstPriceTick { get; private set; }
    public long? LastPriceTick { get; private set; }
    public long? HighPriceTick { get; private set; }
    public long? LowPriceTick { get; private set; }
    public long StateVersion { get; private set; }
    public long EventRevision { get; private set; }
    public HashSet<string> ProcessedEventIds { get; }
    public bool Frozen { get; private set; }

    public bool Apply(ExecutedTradeEvent evt, DateTime nowUtc)
    {
        if (Frozen)
            return false;
        if (!ProcessedEventIds.Add(evt.EventId))
            return false;

        ExecutedVolume += evt.ExecutedVolume;
        TradeCount++;
        FirstEventAtUtc ??= evt.ReceiveTimestampUtc ?? evt.ExchangeTimestampUtc;
        LastEventAtUtc = evt.ReceiveTimestampUtc ?? evt.ExchangeTimestampUtc;
        FirstPriceTick ??= evt.NormalizedPriceTick;
        LastPriceTick = evt.NormalizedPriceTick;
        HighPriceTick = HighPriceTick is long h
            ? Math.Max(h, evt.NormalizedPriceTick)
            : evt.NormalizedPriceTick;
        LowPriceTick = LowPriceTick is long l
            ? Math.Min(l, evt.NormalizedPriceTick)
            : evt.NormalizedPriceTick;

        switch (evt.AggressorSide)
        {
            case AggressorSide.Ask:
                AskVolume += evt.ExecutedVolume;
                break;
            case AggressorSide.Bid:
                BidVolume += evt.ExecutedVolume;
                break;
            default:
                UnknownAggressorVolume += evt.ExecutedVolume;
                break;
        }

        EventRevision++;
        return true;
    }

    public void Freeze()
    {
        if (Frozen) return;
        Frozen = true;
        StateVersion++;
        EventRevision++;
    }

    public ExecutedOrderflowEpisodeSnapshot ToSnapshot()
    {
        var classified = AskVolume + BidVolume;
        var coverage = OrderflowRatio.TryCompute(classified, ExecutedVolume);
        var quality = UnknownAggressorVolume > 0m ? OrderflowDataQuality.Partial : OrderflowDataQuality.Complete;
        long? progress = FirstPriceTick is long f && LastPriceTick is long la ? la - f : null;
        return new ExecutedOrderflowEpisodeSnapshot(
            SnapshotId,
            EpisodeId,
            ReferenceId,
            ReferenceRole,
            PrimaryAuctionId,
            ObservationStartedAtUtc,
            FirstEventAtUtc,
            LastEventAtUtc,
            ExecutedVolume,
            TradeCount,
            AskVolume,
            BidVolume,
            UnknownAggressorVolume,
            classified,
            AskVolume - BidVolume,
            coverage,
            FirstPriceTick,
            LastPriceTick,
            HighPriceTick,
            LowPriceTick,
            progress,
            StateVersion,
            EventRevision,
            quality,
            new[]
            {
                ExecutedOrderflowPolicyConfig.LimitationHistoryLiveOnly,
                ExecutedOrderflowPolicyConfig.LimitationNoAbsorption,
                ExecutedOrderflowPolicyConfig.LimitationNoTradeFacilitation
            });
    }
}

internal sealed class MutableAuctionAggregate
{
    private readonly Dictionary<long, MutablePriceLevel> _levels = new();
    private readonly Dictionary<string, MutableEpisodeAggregate> _episodes = new(StringComparer.Ordinal);
    private readonly HashSet<string> _processedEventIds = new(StringComparer.Ordinal);
    private DateTime? _lastTradeAt;
    private long _intervalCount;
    private double _intervalTicksSum;

    public MutableAuctionAggregate(
        string instrumentIdentity,
        string dataEpoch,
        string primaryAuctionId,
        decimal tickSize,
        string timestampPolicy,
        OrderflowCoverageMode coverageMode,
        DateTime nowUtc)
    {
        InstrumentIdentity = instrumentIdentity;
        DataEpoch = dataEpoch;
        PrimaryAuctionId = primaryAuctionId;
        TickSize = tickSize;
        TimestampPolicy = timestampPolicy;
        CoverageMode = coverageMode;
        ObservationStartedAtUtc = nowUtc;
        SnapshotId = OrderflowIdentity.BuildAuction(instrumentIdentity, dataEpoch, primaryAuctionId);
        StateVersion = 1;
        EventRevision = 1;
    }

    public string SnapshotId { get; }
    public string InstrumentIdentity { get; }
    public string DataEpoch { get; }
    public string PrimaryAuctionId { get; }
    public decimal TickSize { get; }
    public string TimestampPolicy { get; }
    public OrderflowCoverageMode CoverageMode { get; private set; }
    public DateTime ObservationStartedAtUtc { get; }
    public DateTime? FirstEventAtUtc { get; private set; }
    public DateTime? LastEventAtUtc { get; private set; }
    public long AcceptedEventCount { get; private set; }
    public decimal ExecutedVolume { get; private set; }
    public long TradeCount { get; private set; }
    public decimal AskVolume { get; private set; }
    public decimal BidVolume { get; private set; }
    public decimal UnknownAggressorVolume { get; private set; }
    public decimal ClassifiedCvd { get; private set; }
    public long? FirstPriceTick { get; private set; }
    public long? LastPriceTick { get; private set; }
    public long? HighPriceTick { get; private set; }
    public long? LowPriceTick { get; private set; }
    public TimeSpan? MinimumTradeInterval { get; private set; }
    public TimeSpan? MaximumTradeInterval { get; private set; }
    public TimeSpan? LatestTradeInterval { get; private set; }
    public long StateVersion { get; private set; }
    public long EventRevision { get; private set; }

    public IReadOnlyDictionary<string, MutableEpisodeAggregate> Episodes => _episodes;

    public bool TryApply(
        ExecutedTradeEvent evt,
        IReadOnlyDictionary<string, (string ReferenceId, ReferenceInteractionRole Role)>? episodeMeta,
        DateTime nowUtc)
    {
        if (!_processedEventIds.Add(evt.EventId))
            return false;

        var at = evt.ReceiveTimestampUtc ?? evt.ExchangeTimestampUtc;
        if (_lastTradeAt is DateTime prev && at > prev)
        {
            var interval = at - prev;
            LatestTradeInterval = interval;
            if (MinimumTradeInterval is null || interval < MinimumTradeInterval)
                MinimumTradeInterval = interval;
            if (MaximumTradeInterval is null || interval > MaximumTradeInterval)
                MaximumTradeInterval = interval;
            _intervalCount++;
            _intervalTicksSum += interval.Ticks;
        }

        _lastTradeAt = at;
        FirstEventAtUtc ??= at;
        LastEventAtUtc = at;
        AcceptedEventCount++;
        ExecutedVolume += evt.ExecutedVolume;
        TradeCount++;
        FirstPriceTick ??= evt.NormalizedPriceTick;
        LastPriceTick = evt.NormalizedPriceTick;
        HighPriceTick = HighPriceTick is long h ? Math.Max(h, evt.NormalizedPriceTick) : evt.NormalizedPriceTick;
        LowPriceTick = LowPriceTick is long l ? Math.Min(l, evt.NormalizedPriceTick) : evt.NormalizedPriceTick;

        switch (evt.AggressorSide)
        {
            case AggressorSide.Ask:
                AskVolume += evt.ExecutedVolume;
                ClassifiedCvd += evt.ExecutedVolume;
                break;
            case AggressorSide.Bid:
                BidVolume += evt.ExecutedVolume;
                ClassifiedCvd -= evt.ExecutedVolume;
                break;
            default:
                UnknownAggressorVolume += evt.ExecutedVolume;
                break;
        }

        if (!_levels.TryGetValue(evt.NormalizedPriceTick, out var level))
        {
            level = new MutablePriceLevel(evt.NormalizedPriceTick, evt.DecimalPrice);
            _levels[evt.NormalizedPriceTick] = level;
        }

        level.Apply(evt);

        foreach (var epId in evt.AssociatedEpisodeIds)
        {
            if (string.IsNullOrEmpty(epId))
                continue;
            if (!_episodes.TryGetValue(epId, out var epAgg))
            {
                episodeMeta ??= new Dictionary<string, (string, ReferenceInteractionRole)>(StringComparer.Ordinal);
                episodeMeta.TryGetValue(epId, out var meta);
                epAgg = new MutableEpisodeAggregate(
                    epId,
                    meta.ReferenceId ?? "",
                    meta.Role,
                    PrimaryAuctionId,
                    nowUtc);
                _episodes[epId] = epAgg;
            }

            epAgg.Apply(evt, nowUtc);
        }

        EventRevision++;
        return true;
    }

    public void MarkCoverage(OrderflowCoverageMode mode)
    {
        if (CoverageMode == mode)
            return;
        CoverageMode = mode;
        StateVersion++;
        EventRevision++;
    }

    public void SyncEpisodeLifecycle(AuctionEpisodeSetSnapshot? episodes, DateTime nowUtc)
    {
        if (episodes is null)
            return;

        var alive = new HashSet<string>(
            episodes.ActiveEpisodes.Select(e => e.EpisodeId),
            StringComparer.Ordinal);

        foreach (var closed in episodes.RecentlyClosedEpisodes)
        {
            if (_episodes.TryGetValue(closed.EpisodeId, out var agg))
                agg.Freeze();
        }

        foreach (var orphan in _episodes.Keys.Where(id => !alive.Contains(id) && !_episodes[id].Frozen).ToArray())
            _episodes[orphan].Freeze();
    }

    public ExecutedOrderflowAuctionSnapshot ToAuctionSnapshot()
    {
        var classified = AskVolume + BidVolume;
        var coverage = OrderflowRatio.TryCompute(classified, ExecutedVolume);
        AggressorClassificationStatus status;
        if (TradeCount == 0)
            status = AggressorClassificationStatus.Unavailable;
        else if (UnknownAggressorVolume == 0m && classified == ExecutedVolume)
            status = AggressorClassificationStatus.Complete;
        else if (classified > 0m)
            status = AggressorClassificationStatus.Partial;
        else
            status = AggressorClassificationStatus.Unavailable;

        TimeSpan? mean = _intervalCount > 0
            ? TimeSpan.FromTicks((long)(_intervalTicksSum / _intervalCount))
            : null;

        decimal? tps = null;
        if (FirstEventAtUtc is DateTime f && LastEventAtUtc is DateTime la && la > f && TradeCount > 1)
        {
            var seconds = (decimal)(la - f).TotalSeconds;
            if (seconds > 0m)
                tps = (TradeCount - 1m) / seconds;
        }

        long? progress = FirstPriceTick is long a && LastPriceTick is long b ? b - a : null;
        var quality = status == AggressorClassificationStatus.Complete
            ? OrderflowDataQuality.Complete
            : OrderflowDataQuality.Partial;

        var limitations = new List<string>
        {
            ExecutedOrderflowPolicyConfig.LimitationHistoryLiveOnly,
            ExecutedOrderflowPolicyConfig.LimitationNoImbalance,
            ExecutedOrderflowPolicyConfig.LimitationNoStackedImbalance,
            ExecutedOrderflowPolicyConfig.LimitationNoBigTrade,
            ExecutedOrderflowPolicyConfig.LimitationNoTapeSpeed,
            ExecutedOrderflowPolicyConfig.LimitationNoAbsorption,
            ExecutedOrderflowPolicyConfig.LimitationNoExhaustion,
            ExecutedOrderflowPolicyConfig.LimitationNoEffortResult,
            ExecutedOrderflowPolicyConfig.LimitationNoTradeFacilitation,
            ExecutedOrderflowPolicyConfig.LimitationCumulativeNotAuthoritative
        };
        if (CoverageMode == OrderflowCoverageMode.LiveOnlyMidAuction)
            limitations.Add(ExecutedOrderflowPolicyConfig.LimitationMidAuctionCoverage);
        if (status == AggressorClassificationStatus.Partial)
            limitations.Add(ExecutedOrderflowPolicyConfig.LimitationAggressorPartial);
        if (status == AggressorClassificationStatus.Unavailable && TradeCount > 0)
            limitations.Add(ExecutedOrderflowPolicyConfig.LimitationAggressorUnavailable);

        return new ExecutedOrderflowAuctionSnapshot(
            SnapshotId,
            ExecutedOrderflowPolicyConfig.PolicyVersion,
            PrimaryAuctionId,
            InstrumentIdentity,
            DataEpoch,
            TickSize,
            TimestampPolicy,
            CoverageMode,
            ObservationStartedAtUtc,
            FirstEventAtUtc,
            LastEventAtUtc,
            AcceptedEventCount,
            ExecutedVolume,
            TradeCount,
            AskVolume,
            BidVolume,
            UnknownAggressorVolume,
            classified,
            AskVolume - BidVolume,
            ClassifiedCvd,
            coverage,
            status,
            MinimumTradeInterval,
            MaximumTradeInterval,
            mean,
            LatestTradeInterval,
            tps,
            FirstPriceTick,
            LastPriceTick,
            HighPriceTick,
            LowPriceTick,
            progress,
            StateVersion,
            EventRevision,
            quality,
            limitations);
    }

    public IReadOnlyList<ExecutedPriceLevelSnapshot> SnapshotPriceLevels() =>
        _levels.OrderBy(kv => kv.Key).Select(kv => kv.Value.ToSnapshot(SnapshotId)).ToArray();

    public IReadOnlyList<ExecutedOrderflowEpisodeSnapshot> SnapshotActiveEpisodes() =>
        _episodes.Values.Where(e => !e.Frozen).Select(e => e.ToSnapshot())
            .OrderBy(e => e.EpisodeId, StringComparer.Ordinal).ToArray();

    public IReadOnlyList<ExecutedOrderflowEpisodeSnapshot> SnapshotClosedEpisodes() =>
        _episodes.Values.Where(e => e.Frozen).Select(e => e.ToSnapshot())
            .OrderBy(e => e.EpisodeId, StringComparer.Ordinal)
            .TakeLast(ExecutedOrderflowSetSnapshot.RecentlyClosedEpisodeCapacity)
            .ToArray();
}
