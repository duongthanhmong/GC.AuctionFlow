using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Orderflow;

namespace GC.AuctionFlow.Cluster;

public sealed class ClusterRawPriceLevelSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public ClusterRawPriceLevelSnapshot(
        string levelId,
        string policyVersion,
        string clusterRawSnapshotId,
        string primaryAuctionId,
        long priceTick,
        decimal decimalPrice,
        decimal executedVolume,
        long tradeCount,
        decimal? askVolume,
        decimal? bidVolume,
        decimal unknownAggressorVolume,
        decimal classifiedVolume,
        decimal classifiedDelta,
        decimal? aggressorCoverageRatio,
        decimal? samePriceAskToBidRatio,
        decimal? samePriceBidToAskRatio,
        decimal? diagonalAskToBidBelowRatio,
        decimal? diagonalBidToAskAboveRatio,
        ClusterRawDominantSide rawDominantSide,
        decimal? rawDominanceDifference,
        int consecutiveRawDominanceTicks,
        int volumeRank,
        int? absoluteDeltaRank,
        int tradeCountRank,
        decimal? empiricalVolumePercentile,
        decimal? empiricalAbsoluteDeltaPercentile,
        decimal? empiricalTradeCountPercentile,
        int volumeRankPopulation,
        int visitCount,
        int revisitCount,
        DateTime? firstVisitAtUtc,
        DateTime? lastVisitAtUtc,
        long? priceProgressSinceFirstVisitTicks,
        long? distanceFromLatestPriceTicks,
        long stateVersion,
        long eventRevision,
        ClusterRawAvailability availability,
        ClusterRawDataQuality dataQuality,
        IReadOnlyList<string> limitations)
    {
        LevelId = levelId ?? "";
        PolicyVersion = policyVersion ?? ClusterRawFeaturePolicyConfig.PolicyVersion;
        ClusterRawSnapshotId = clusterRawSnapshotId ?? "";
        PrimaryAuctionId = primaryAuctionId ?? "";
        PriceTick = priceTick;
        DecimalPrice = decimalPrice;
        ExecutedVolume = executedVolume;
        TradeCount = tradeCount;
        AskVolume = askVolume;
        BidVolume = bidVolume;
        UnknownAggressorVolume = unknownAggressorVolume;
        ClassifiedVolume = classifiedVolume;
        ClassifiedDelta = classifiedDelta;
        AggressorCoverageRatio = aggressorCoverageRatio;
        SamePriceAskToBidRatio = samePriceAskToBidRatio;
        SamePriceBidToAskRatio = samePriceBidToAskRatio;
        DiagonalAskToBidBelowRatio = diagonalAskToBidBelowRatio;
        DiagonalBidToAskAboveRatio = diagonalBidToAskAboveRatio;
        RawDominantSide = rawDominantSide;
        RawDominanceDifference = rawDominanceDifference;
        ConsecutiveRawDominanceTicks = consecutiveRawDominanceTicks;
        VolumeRank = volumeRank;
        AbsoluteDeltaRank = absoluteDeltaRank;
        TradeCountRank = tradeCountRank;
        EmpiricalVolumePercentile = empiricalVolumePercentile;
        EmpiricalAbsoluteDeltaPercentile = empiricalAbsoluteDeltaPercentile;
        EmpiricalTradeCountPercentile = empiricalTradeCountPercentile;
        VolumeRankPopulation = volumeRankPopulation;
        VisitCount = visitCount;
        RevisitCount = revisitCount;
        FirstVisitAtUtc = firstVisitAtUtc;
        LastVisitAtUtc = lastVisitAtUtc;
        PriceProgressSinceFirstVisitTicks = priceProgressSinceFirstVisitTicks;
        DistanceFromLatestPriceTicks = distanceFromLatestPriceTicks;
        StateVersion = stateVersion;
        EventRevision = eventRevision;
        Availability = availability;
        DataQuality = dataQuality;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public string LevelId { get; }
    public string PolicyVersion { get; }
    public string ClusterRawSnapshotId { get; }
    public string PrimaryAuctionId { get; }
    public long PriceTick { get; }
    public decimal DecimalPrice { get; }
    public decimal ExecutedVolume { get; }
    public long TradeCount { get; }
    public decimal? AskVolume { get; }
    public decimal? BidVolume { get; }
    public decimal UnknownAggressorVolume { get; }
    public decimal ClassifiedVolume { get; }
    public decimal ClassifiedDelta { get; }
    public decimal? AggressorCoverageRatio { get; }
    public decimal? SamePriceAskToBidRatio { get; }
    public decimal? SamePriceBidToAskRatio { get; }
    public decimal? DiagonalAskToBidBelowRatio { get; }
    public decimal? DiagonalBidToAskAboveRatio { get; }
    public ClusterRawDominantSide RawDominantSide { get; }
    public decimal? RawDominanceDifference { get; }
    public int ConsecutiveRawDominanceTicks { get; }
    public int VolumeRank { get; }
    public int? AbsoluteDeltaRank { get; }
    public int TradeCountRank { get; }
    public decimal? EmpiricalVolumePercentile { get; }
    public decimal? EmpiricalAbsoluteDeltaPercentile { get; }
    public decimal? EmpiricalTradeCountPercentile { get; }
    public int VolumeRankPopulation { get; }
    public int VisitCount { get; }
    public int RevisitCount { get; }
    public DateTime? FirstVisitAtUtc { get; }
    public DateTime? LastVisitAtUtc { get; }
    public long? PriceProgressSinceFirstVisitTicks { get; }
    public long? DistanceFromLatestPriceTicks { get; }
    public long StateVersion { get; }
    public long EventRevision { get; }
    public ClusterRawAvailability Availability { get; }
    public ClusterRawDataQuality DataQuality { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;
}

public sealed class ClusterRawEpisodeSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public ClusterRawEpisodeSnapshot(
        string snapshotId,
        string policyVersion,
        string episodeOrderflowSnapshotId,
        string episodeId,
        string referenceId,
        ReferenceInteractionRole referenceRole,
        string primaryAuctionId,
        DateTime observationStartedAtUtc,
        DateTime? firstEventAtUtc,
        DateTime? lastEventAtUtc,
        int priceLevelCount,
        decimal executedVolume,
        long tradeCount,
        decimal? askVolume,
        decimal? bidVolume,
        decimal unknownAggressorVolume,
        decimal classifiedDelta,
        decimal? aggressorCoverageRatio,
        long? maximumVolumePriceTick,
        long? maximumAbsoluteDeltaPriceTick,
        long? maximumTradeCountPriceTick,
        int volumeRankPopulation,
        int classifiedLevelCount,
        int unknownOnlyLevelCount,
        int revisitedLevelCount,
        int maximumVisitCount,
        long? firstPriceTick,
        long? lastPriceTick,
        long? highPriceTick,
        long? lowPriceTick,
        long? netPriceProgressTicks,
        long stateVersion,
        long eventRevision,
        ClusterRawAvailability availability,
        ClusterRawDataQuality dataQuality,
        IReadOnlyList<string> limitations)
    {
        SnapshotId = snapshotId ?? "";
        PolicyVersion = policyVersion ?? ClusterRawFeaturePolicyConfig.PolicyVersion;
        EpisodeOrderflowSnapshotId = episodeOrderflowSnapshotId ?? "";
        EpisodeId = episodeId ?? "";
        ReferenceId = referenceId ?? "";
        ReferenceRole = referenceRole;
        PrimaryAuctionId = primaryAuctionId ?? "";
        ObservationStartedAtUtc = observationStartedAtUtc;
        FirstEventAtUtc = firstEventAtUtc;
        LastEventAtUtc = lastEventAtUtc;
        PriceLevelCount = priceLevelCount;
        ExecutedVolume = executedVolume;
        TradeCount = tradeCount;
        AskVolume = askVolume;
        BidVolume = bidVolume;
        UnknownAggressorVolume = unknownAggressorVolume;
        ClassifiedDelta = classifiedDelta;
        AggressorCoverageRatio = aggressorCoverageRatio;
        MaximumVolumePriceTick = maximumVolumePriceTick;
        MaximumAbsoluteDeltaPriceTick = maximumAbsoluteDeltaPriceTick;
        MaximumTradeCountPriceTick = maximumTradeCountPriceTick;
        VolumeRankPopulation = volumeRankPopulation;
        ClassifiedLevelCount = classifiedLevelCount;
        UnknownOnlyLevelCount = unknownOnlyLevelCount;
        RevisitedLevelCount = revisitedLevelCount;
        MaximumVisitCount = maximumVisitCount;
        FirstPriceTick = firstPriceTick;
        LastPriceTick = lastPriceTick;
        HighPriceTick = highPriceTick;
        LowPriceTick = lowPriceTick;
        NetPriceProgressTicks = netPriceProgressTicks;
        StateVersion = stateVersion;
        EventRevision = eventRevision;
        Availability = availability;
        DataQuality = dataQuality;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public string SnapshotId { get; }
    public string PolicyVersion { get; }
    public string EpisodeOrderflowSnapshotId { get; }
    public string EpisodeId { get; }
    public string ReferenceId { get; }
    public ReferenceInteractionRole ReferenceRole { get; }
    public string PrimaryAuctionId { get; }
    public DateTime ObservationStartedAtUtc { get; }
    public DateTime? FirstEventAtUtc { get; }
    public DateTime? LastEventAtUtc { get; }
    public int PriceLevelCount { get; }
    public decimal ExecutedVolume { get; }
    public long TradeCount { get; }
    public decimal? AskVolume { get; }
    public decimal? BidVolume { get; }
    public decimal UnknownAggressorVolume { get; }
    public decimal ClassifiedDelta { get; }
    public decimal? AggressorCoverageRatio { get; }
    public long? MaximumVolumePriceTick { get; }
    public long? MaximumAbsoluteDeltaPriceTick { get; }
    public long? MaximumTradeCountPriceTick { get; }
    public int VolumeRankPopulation { get; }
    public int ClassifiedLevelCount { get; }
    public int UnknownOnlyLevelCount { get; }
    public int RevisitedLevelCount { get; }
    public int MaximumVisitCount { get; }
    public long? FirstPriceTick { get; }
    public long? LastPriceTick { get; }
    public long? HighPriceTick { get; }
    public long? LowPriceTick { get; }
    public long? NetPriceProgressTicks { get; }
    public long StateVersion { get; }
    public long EventRevision { get; }
    public ClusterRawAvailability Availability { get; }
    public ClusterRawDataQuality DataQuality { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;
}

public sealed class ClusterRawAuctionSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public ClusterRawAuctionSnapshot(
        string snapshotId,
        string policyVersion,
        string orderflowAuctionSnapshotId,
        string primaryAuctionId,
        string instrumentIdentity,
        string dataEpoch,
        decimal tickSize,
        string timestampPolicy,
        OrderflowCoverageMode coverageMode,
        DateTime observationStartedAtUtc,
        DateTime? firstEventAtUtc,
        DateTime? lastEventAtUtc,
        int priceLevelCount,
        int classifiedLevelCount,
        int partialLevelCount,
        int unknownOnlyLevelCount,
        decimal totalExecutedVolume,
        long totalTradeCount,
        decimal? totalAskVolume,
        decimal? totalBidVolume,
        decimal totalUnknownAggressorVolume,
        decimal totalClassifiedDelta,
        long? latestPriceTick,
        long? highPriceTick,
        long? lowPriceTick,
        int populationSize,
        string? latestUpdatedLevelId,
        string? latestEpisodeSnapshotId,
        ClusterClassificationState classificationState,
        long stateVersion,
        long eventRevision,
        ClusterRawDataQuality dataQuality,
        IReadOnlyList<string> limitations,
        IReadOnlyList<ClusterRawPriceLevelSnapshot> priceLevels,
        IReadOnlyList<ClusterRawEpisodeSnapshot> activeEpisodeSnapshots,
        IReadOnlyList<ClusterRawEpisodeSnapshot> recentlyClosedEpisodeSnapshots)
    {
        SnapshotId = snapshotId ?? "";
        PolicyVersion = policyVersion ?? ClusterRawFeaturePolicyConfig.PolicyVersion;
        OrderflowAuctionSnapshotId = orderflowAuctionSnapshotId ?? "";
        PrimaryAuctionId = primaryAuctionId ?? "";
        InstrumentIdentity = instrumentIdentity ?? "";
        DataEpoch = dataEpoch ?? "";
        TickSize = tickSize;
        TimestampPolicy = timestampPolicy ?? "";
        CoverageMode = coverageMode;
        ObservationStartedAtUtc = observationStartedAtUtc;
        FirstEventAtUtc = firstEventAtUtc;
        LastEventAtUtc = lastEventAtUtc;
        PriceLevelCount = priceLevelCount;
        ClassifiedLevelCount = classifiedLevelCount;
        PartialLevelCount = partialLevelCount;
        UnknownOnlyLevelCount = unknownOnlyLevelCount;
        TotalExecutedVolume = totalExecutedVolume;
        TotalTradeCount = totalTradeCount;
        TotalAskVolume = totalAskVolume;
        TotalBidVolume = totalBidVolume;
        TotalUnknownAggressorVolume = totalUnknownAggressorVolume;
        TotalClassifiedDelta = totalClassifiedDelta;
        LatestPriceTick = latestPriceTick;
        HighPriceTick = highPriceTick;
        LowPriceTick = lowPriceTick;
        PopulationSize = populationSize;
        LatestUpdatedLevelId = latestUpdatedLevelId;
        LatestEpisodeSnapshotId = latestEpisodeSnapshotId;
        ClassificationState = classificationState;
        StateVersion = stateVersion;
        EventRevision = eventRevision;
        DataQuality = dataQuality;
        Limitations = limitations ?? Array.Empty<string>();
        PriceLevels = priceLevels ?? Array.Empty<ClusterRawPriceLevelSnapshot>();
        ActiveEpisodeSnapshots = activeEpisodeSnapshots ?? Array.Empty<ClusterRawEpisodeSnapshot>();
        RecentlyClosedEpisodeSnapshots = recentlyClosedEpisodeSnapshots ?? Array.Empty<ClusterRawEpisodeSnapshot>();
    }

    public string SnapshotId { get; }
    public string PolicyVersion { get; }
    public string OrderflowAuctionSnapshotId { get; }
    public string PrimaryAuctionId { get; }
    public string InstrumentIdentity { get; }
    public string DataEpoch { get; }
    public decimal TickSize { get; }
    public string TimestampPolicy { get; }
    public OrderflowCoverageMode CoverageMode { get; }
    public DateTime ObservationStartedAtUtc { get; }
    public DateTime? FirstEventAtUtc { get; }
    public DateTime? LastEventAtUtc { get; }
    public int PriceLevelCount { get; }
    public int ClassifiedLevelCount { get; }
    public int PartialLevelCount { get; }
    public int UnknownOnlyLevelCount { get; }
    public decimal TotalExecutedVolume { get; }
    public long TotalTradeCount { get; }
    public decimal? TotalAskVolume { get; }
    public decimal? TotalBidVolume { get; }
    public decimal TotalUnknownAggressorVolume { get; }
    public decimal TotalClassifiedDelta { get; }
    public long? LatestPriceTick { get; }
    public long? HighPriceTick { get; }
    public long? LowPriceTick { get; }
    public int PopulationSize { get; }
    public string? LatestUpdatedLevelId { get; }
    public string? LatestEpisodeSnapshotId { get; }
    public ClusterClassificationState ClassificationState { get; }
    public long StateVersion { get; }
    public long EventRevision { get; }
    public ClusterRawDataQuality DataQuality { get; }
    public IReadOnlyList<string> Limitations { get; }
    public IReadOnlyList<ClusterRawPriceLevelSnapshot> PriceLevels { get; }
    public IReadOnlyList<ClusterRawEpisodeSnapshot> ActiveEpisodeSnapshots { get; }
    public IReadOnlyList<ClusterRawEpisodeSnapshot> RecentlyClosedEpisodeSnapshots { get; }
    public string Version => SnapshotVersion;
}

public sealed class ClusterRawSetSnapshot
{
    public const string SnapshotVersion = "1.0.0";
    public const int RecentlyClosedEpisodeCapacity = 64;

    public ClusterRawSetSnapshot(
        ClusterRawModuleState moduleState,
        string policyVersion,
        ClusterRawAuctionSnapshot? currentAuction,
        string rankMethod,
        long inputOrderflowEventRevision,
        long inputOrderflowStateVersion,
        string? lastSourceEventId,
        long? lastSourceSequence,
        long rejectedStaleRevisionCount,
        string? lastRejectionReason,
        ClusterRawInputFingerprint? inputFingerprint,
        DateTime createdAtUtc,
        DateTime lastUpdatedAtUtc,
        IReadOnlyList<string> limitations)
    {
        ModuleState = moduleState;
        PolicyVersion = policyVersion ?? ClusterRawFeaturePolicyConfig.PolicyVersion;
        CurrentAuction = currentAuction;
        RankMethod = rankMethod ?? ClusterRawFeaturePolicyConfig.RankMethod;
        InputOrderflowEventRevision = inputOrderflowEventRevision;
        InputOrderflowStateVersion = inputOrderflowStateVersion;
        LastSourceEventId = lastSourceEventId;
        LastSourceSequence = lastSourceSequence;
        RejectedStaleRevisionCount = rejectedStaleRevisionCount;
        LastRejectionReason = lastRejectionReason;
        InputFingerprint = inputFingerprint;
        CreatedAtUtc = createdAtUtc;
        LastUpdatedAtUtc = lastUpdatedAtUtc;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public ClusterRawModuleState ModuleState { get; }
    public string PolicyVersion { get; }
    public ClusterRawAuctionSnapshot? CurrentAuction { get; }
    public string RankMethod { get; }
    public long InputOrderflowEventRevision { get; }
    public long InputOrderflowStateVersion { get; }
    public string? LastSourceEventId { get; }
    public long? LastSourceSequence { get; }
    public long RejectedStaleRevisionCount { get; }
    public string? LastRejectionReason { get; }
    public ClusterRawInputFingerprint? InputFingerprint { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime LastUpdatedAtUtc { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;
}
