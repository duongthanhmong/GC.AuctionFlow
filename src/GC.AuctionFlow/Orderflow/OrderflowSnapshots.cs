using GC.AuctionFlow.Episode;

namespace GC.AuctionFlow.Orderflow;

public sealed class ExecutedPriceLevelSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public ExecutedPriceLevelSnapshot(
        string snapshotId,
        long priceTick,
        decimal decimalPrice,
        decimal executedVolume,
        long tradeCount,
        decimal askVolume,
        decimal bidVolume,
        decimal unknownAggressorVolume,
        decimal classifiedDelta,
        decimal? aggressorCoverageRatio,
        DateTime? firstEventAtUtc,
        DateTime? lastEventAtUtc,
        long stateVersion,
        long eventRevision,
        OrderflowDataQuality availability,
        IReadOnlyList<string> limitations)
    {
        SnapshotId = snapshotId ?? "";
        PriceTick = priceTick;
        DecimalPrice = decimalPrice;
        ExecutedVolume = executedVolume;
        TradeCount = tradeCount;
        AskVolume = askVolume;
        BidVolume = bidVolume;
        UnknownAggressorVolume = unknownAggressorVolume;
        ClassifiedDelta = classifiedDelta;
        AggressorCoverageRatio = aggressorCoverageRatio;
        FirstEventAtUtc = firstEventAtUtc;
        LastEventAtUtc = lastEventAtUtc;
        StateVersion = stateVersion;
        EventRevision = eventRevision;
        Availability = availability;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public string SnapshotId { get; }
    public long PriceTick { get; }
    public decimal DecimalPrice { get; }
    public decimal ExecutedVolume { get; }
    public long TradeCount { get; }
    public decimal AskVolume { get; }
    public decimal BidVolume { get; }
    public decimal UnknownAggressorVolume { get; }
    public decimal ClassifiedDelta { get; }
    public decimal? AggressorCoverageRatio { get; }
    public DateTime? FirstEventAtUtc { get; }
    public DateTime? LastEventAtUtc { get; }
    public long StateVersion { get; }
    public long EventRevision { get; }
    public OrderflowDataQuality Availability { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;
}

public sealed class ExecutedOrderflowEpisodeSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public ExecutedOrderflowEpisodeSnapshot(
        string snapshotId,
        string episodeId,
        string referenceId,
        ReferenceInteractionRole referenceRole,
        string primaryAuctionId,
        DateTime observationStartedAtUtc,
        DateTime? firstEventAtUtc,
        DateTime? lastEventAtUtc,
        decimal executedVolume,
        long tradeCount,
        decimal askVolume,
        decimal bidVolume,
        decimal unknownAggressorVolume,
        decimal classifiedVolume,
        decimal classifiedDelta,
        decimal? aggressorCoverageRatio,
        long? firstPriceTick,
        long? lastPriceTick,
        long? highPriceTick,
        long? lowPriceTick,
        long? netPriceProgressTicks,
        long stateVersion,
        long eventRevision,
        OrderflowDataQuality dataQuality,
        IReadOnlyList<string> limitations)
    {
        SnapshotId = snapshotId ?? "";
        EpisodeId = episodeId ?? "";
        ReferenceId = referenceId ?? "";
        ReferenceRole = referenceRole;
        PrimaryAuctionId = primaryAuctionId ?? "";
        ObservationStartedAtUtc = observationStartedAtUtc;
        FirstEventAtUtc = firstEventAtUtc;
        LastEventAtUtc = lastEventAtUtc;
        ExecutedVolume = executedVolume;
        TradeCount = tradeCount;
        AskVolume = askVolume;
        BidVolume = bidVolume;
        UnknownAggressorVolume = unknownAggressorVolume;
        ClassifiedVolume = classifiedVolume;
        ClassifiedDelta = classifiedDelta;
        AggressorCoverageRatio = aggressorCoverageRatio;
        FirstPriceTick = firstPriceTick;
        LastPriceTick = lastPriceTick;
        HighPriceTick = highPriceTick;
        LowPriceTick = lowPriceTick;
        NetPriceProgressTicks = netPriceProgressTicks;
        StateVersion = stateVersion;
        EventRevision = eventRevision;
        DataQuality = dataQuality;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public string SnapshotId { get; }
    public string EpisodeId { get; }
    public string ReferenceId { get; }
    public ReferenceInteractionRole ReferenceRole { get; }
    public string PrimaryAuctionId { get; }
    public DateTime ObservationStartedAtUtc { get; }
    public DateTime? FirstEventAtUtc { get; }
    public DateTime? LastEventAtUtc { get; }
    public decimal ExecutedVolume { get; }
    public long TradeCount { get; }
    public decimal AskVolume { get; }
    public decimal BidVolume { get; }
    public decimal UnknownAggressorVolume { get; }
    public decimal ClassifiedVolume { get; }
    public decimal ClassifiedDelta { get; }
    public decimal? AggressorCoverageRatio { get; }
    public long? FirstPriceTick { get; }
    public long? LastPriceTick { get; }
    public long? HighPriceTick { get; }
    public long? LowPriceTick { get; }
    public long? NetPriceProgressTicks { get; }
    public long StateVersion { get; }
    public long EventRevision { get; }
    public OrderflowDataQuality DataQuality { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;
}

public sealed class ExecutedOrderflowAuctionSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public ExecutedOrderflowAuctionSnapshot(
        string snapshotId,
        string policyVersion,
        string primaryAuctionId,
        string instrumentIdentity,
        string dataEpoch,
        decimal tickSize,
        string timestampPolicy,
        OrderflowCoverageMode coverageMode,
        DateTime observationStartedAtUtc,
        DateTime? firstEventAtUtc,
        DateTime? lastEventAtUtc,
        long acceptedEventCount,
        decimal executedVolume,
        long tradeCount,
        decimal askVolume,
        decimal bidVolume,
        decimal unknownAggressorVolume,
        decimal classifiedVolume,
        decimal classifiedDelta,
        decimal classifiedCvd,
        decimal? aggressorCoverageRatio,
        AggressorClassificationStatus aggressorClassificationStatus,
        TimeSpan? minimumTradeInterval,
        TimeSpan? maximumTradeInterval,
        TimeSpan? meanTradeInterval,
        TimeSpan? latestTradeInterval,
        decimal? tradesPerSecondRaw,
        long? firstPriceTick,
        long? lastPriceTick,
        long? highPriceTick,
        long? lowPriceTick,
        long? netPriceProgressTicks,
        long stateVersion,
        long eventRevision,
        OrderflowDataQuality dataQuality,
        IReadOnlyList<string> limitations)
    {
        SnapshotId = snapshotId ?? "";
        PolicyVersion = policyVersion ?? "";
        PrimaryAuctionId = primaryAuctionId ?? "";
        InstrumentIdentity = instrumentIdentity ?? "";
        DataEpoch = dataEpoch ?? "";
        TickSize = tickSize;
        TimestampPolicy = timestampPolicy ?? "";
        CoverageMode = coverageMode;
        ObservationStartedAtUtc = observationStartedAtUtc;
        FirstEventAtUtc = firstEventAtUtc;
        LastEventAtUtc = lastEventAtUtc;
        AcceptedEventCount = acceptedEventCount;
        ExecutedVolume = executedVolume;
        TradeCount = tradeCount;
        AskVolume = askVolume;
        BidVolume = bidVolume;
        UnknownAggressorVolume = unknownAggressorVolume;
        ClassifiedVolume = classifiedVolume;
        ClassifiedDelta = classifiedDelta;
        ClassifiedCvd = classifiedCvd;
        AggressorCoverageRatio = aggressorCoverageRatio;
        AggressorClassificationStatus = aggressorClassificationStatus;
        MinimumTradeInterval = minimumTradeInterval;
        MaximumTradeInterval = maximumTradeInterval;
        MeanTradeInterval = meanTradeInterval;
        LatestTradeInterval = latestTradeInterval;
        TradesPerSecondRaw = tradesPerSecondRaw;
        FirstPriceTick = firstPriceTick;
        LastPriceTick = lastPriceTick;
        HighPriceTick = highPriceTick;
        LowPriceTick = lowPriceTick;
        NetPriceProgressTicks = netPriceProgressTicks;
        StateVersion = stateVersion;
        EventRevision = eventRevision;
        DataQuality = dataQuality;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public string SnapshotId { get; }
    public string PolicyVersion { get; }
    public string PrimaryAuctionId { get; }
    public string InstrumentIdentity { get; }
    public string DataEpoch { get; }
    public decimal TickSize { get; }
    public string TimestampPolicy { get; }
    public OrderflowCoverageMode CoverageMode { get; }
    public DateTime ObservationStartedAtUtc { get; }
    public DateTime? FirstEventAtUtc { get; }
    public DateTime? LastEventAtUtc { get; }
    public long AcceptedEventCount { get; }
    public decimal ExecutedVolume { get; }
    public long TradeCount { get; }
    public decimal AskVolume { get; }
    public decimal BidVolume { get; }
    public decimal UnknownAggressorVolume { get; }
    public decimal ClassifiedVolume { get; }
    public decimal ClassifiedDelta { get; }
    public decimal ClassifiedCvd { get; }
    public decimal? AggressorCoverageRatio { get; }
    public AggressorClassificationStatus AggressorClassificationStatus { get; }
    public TimeSpan? MinimumTradeInterval { get; }
    public TimeSpan? MaximumTradeInterval { get; }
    public TimeSpan? MeanTradeInterval { get; }
    public TimeSpan? LatestTradeInterval { get; }
    public decimal? TradesPerSecondRaw { get; }
    public long? FirstPriceTick { get; }
    public long? LastPriceTick { get; }
    public long? HighPriceTick { get; }
    public long? LowPriceTick { get; }
    public long? NetPriceProgressTicks { get; }
    public long StateVersion { get; }
    public long EventRevision { get; }
    public OrderflowDataQuality DataQuality { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;
}

public sealed class ExecutedOrderflowSetSnapshot
{
    public const string SnapshotVersion = "1.0.0";
    public const int RecentlyClosedEpisodeCapacity = 64;

    public ExecutedOrderflowSetSnapshot(
        OrderflowModuleState moduleState,
        string policyVersion,
        ExecutedOrderflowAuctionSnapshot? currentAuction,
        IReadOnlyList<ExecutedPriceLevelSnapshot> priceLevels,
        IReadOnlyList<ExecutedOrderflowEpisodeSnapshot> activeEpisodeAggregates,
        IReadOnlyList<ExecutedOrderflowEpisodeSnapshot> recentlyClosedEpisodeAggregates,
        long eventsSeen,
        long eventsAccepted,
        long eventsDuplicated,
        long eventsRejected,
        string? lastRejectionReason,
        string? lastAcceptedEventId,
        long? lastAcceptedSequence,
        long onNewTradeCount,
        long onNewTradesBatchCount,
        string inputFingerprint,
        long registryRevision,
        DateTime createdAtUtc,
        DateTime lastUpdatedAtUtc,
        IReadOnlyList<string> limitations)
    {
        ModuleState = moduleState;
        PolicyVersion = policyVersion ?? "";
        CurrentAuction = currentAuction;
        PriceLevels = priceLevels ?? Array.Empty<ExecutedPriceLevelSnapshot>();
        ActiveEpisodeAggregates = activeEpisodeAggregates ?? Array.Empty<ExecutedOrderflowEpisodeSnapshot>();
        RecentlyClosedEpisodeAggregates = recentlyClosedEpisodeAggregates ?? Array.Empty<ExecutedOrderflowEpisodeSnapshot>();
        EventsSeen = eventsSeen;
        EventsAccepted = eventsAccepted;
        EventsDuplicated = eventsDuplicated;
        EventsRejected = eventsRejected;
        LastRejectionReason = lastRejectionReason;
        LastAcceptedEventId = lastAcceptedEventId;
        LastAcceptedSequence = lastAcceptedSequence;
        OnNewTradeCount = onNewTradeCount;
        OnNewTradesBatchCount = onNewTradesBatchCount;
        InputFingerprint = inputFingerprint ?? "";
        RegistryRevision = registryRevision;
        CreatedAtUtc = createdAtUtc;
        LastUpdatedAtUtc = lastUpdatedAtUtc;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public OrderflowModuleState ModuleState { get; }
    public string PolicyVersion { get; }
    public ExecutedOrderflowAuctionSnapshot? CurrentAuction { get; }
    public IReadOnlyList<ExecutedPriceLevelSnapshot> PriceLevels { get; }
    public IReadOnlyList<ExecutedOrderflowEpisodeSnapshot> ActiveEpisodeAggregates { get; }
    public IReadOnlyList<ExecutedOrderflowEpisodeSnapshot> RecentlyClosedEpisodeAggregates { get; }
    public long EventsSeen { get; }
    public long EventsAccepted { get; }
    public long EventsDuplicated { get; }
    public long EventsRejected { get; }
    public string? LastRejectionReason { get; }
    public string? LastAcceptedEventId { get; }
    public long? LastAcceptedSequence { get; }
    public long OnNewTradeCount { get; }
    public long OnNewTradesBatchCount { get; }
    public string InputFingerprint { get; }
    public long RegistryRevision { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime LastUpdatedAtUtc { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;
}
