namespace GC.AuctionFlow.Efficiency;

public sealed class AuctionEffortEvidenceVector
{
    public AuctionEffortEvidenceVector(
        decimal totalExecutedVolume,
        long tradeCount,
        int priceLevelCount,
        decimal classifiedVolume,
        decimal? askVolume,
        decimal? bidVolume,
        decimal unknownAggressorVolume,
        decimal classifiedDelta,
        decimal? absoluteClassifiedDelta,
        decimal? classifiedCvdChange,
        decimal? aggressorCoverageRatio,
        TimeSpan? observationDuration,
        TimeSpan? minimumTradeInterval,
        TimeSpan? maximumTradeInterval,
        TimeSpan? meanTradeInterval,
        TimeSpan? latestTradeInterval,
        decimal? tradesPerSecondRaw,
        decimal? contractsPerSecondRaw,
        decimal? maximumLevelExecutedVolume,
        long? maximumLevelTradeCount,
        decimal? maximumAbsoluteLevelDelta,
        int revisitedLevelCount,
        int maximumVisitCount,
        int classifiedLevelCount,
        int unknownOnlyLevelCount,
        int samePriceRatioAvailabilityCount,
        int diagonalRatioAvailabilityCount,
        int rawAskDominantLevelCount,
        int rawBidDominantLevelCount,
        int rawEqualLevelCount,
        int rawUnknownDominantLevelCount,
        int maximumConsecutiveRawAskDominanceTicks,
        int maximumConsecutiveRawBidDominanceTicks,
        int clusterPopulationSize,
        EfficiencyAvailability evidenceAvailability,
        IReadOnlyList<string> limitations)
    {
        TotalExecutedVolume = totalExecutedVolume;
        TradeCount = tradeCount;
        PriceLevelCount = priceLevelCount;
        ClassifiedVolume = classifiedVolume;
        AskVolume = askVolume;
        BidVolume = bidVolume;
        UnknownAggressorVolume = unknownAggressorVolume;
        ClassifiedDelta = classifiedDelta;
        AbsoluteClassifiedDelta = absoluteClassifiedDelta;
        ClassifiedCvdChange = classifiedCvdChange;
        AggressorCoverageRatio = aggressorCoverageRatio;
        ObservationDuration = observationDuration;
        MinimumTradeInterval = minimumTradeInterval;
        MaximumTradeInterval = maximumTradeInterval;
        MeanTradeInterval = meanTradeInterval;
        LatestTradeInterval = latestTradeInterval;
        TradesPerSecondRaw = tradesPerSecondRaw;
        ContractsPerSecondRaw = contractsPerSecondRaw;
        MaximumLevelExecutedVolume = maximumLevelExecutedVolume;
        MaximumLevelTradeCount = maximumLevelTradeCount;
        MaximumAbsoluteLevelDelta = maximumAbsoluteLevelDelta;
        RevisitedLevelCount = revisitedLevelCount;
        MaximumVisitCount = maximumVisitCount;
        ClassifiedLevelCount = classifiedLevelCount;
        UnknownOnlyLevelCount = unknownOnlyLevelCount;
        SamePriceRatioAvailabilityCount = samePriceRatioAvailabilityCount;
        DiagonalRatioAvailabilityCount = diagonalRatioAvailabilityCount;
        RawAskDominantLevelCount = rawAskDominantLevelCount;
        RawBidDominantLevelCount = rawBidDominantLevelCount;
        RawEqualLevelCount = rawEqualLevelCount;
        RawUnknownDominantLevelCount = rawUnknownDominantLevelCount;
        MaximumConsecutiveRawAskDominanceTicks = maximumConsecutiveRawAskDominanceTicks;
        MaximumConsecutiveRawBidDominanceTicks = maximumConsecutiveRawBidDominanceTicks;
        ClusterPopulationSize = clusterPopulationSize;
        EvidenceAvailability = evidenceAvailability;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public decimal TotalExecutedVolume { get; }
    public long TradeCount { get; }
    public int PriceLevelCount { get; }
    public decimal ClassifiedVolume { get; }
    public decimal? AskVolume { get; }
    public decimal? BidVolume { get; }
    public decimal UnknownAggressorVolume { get; }
    public decimal ClassifiedDelta { get; }
    public decimal? AbsoluteClassifiedDelta { get; }
    public decimal? ClassifiedCvdChange { get; }
    public decimal? AggressorCoverageRatio { get; }
    public TimeSpan? ObservationDuration { get; }
    public TimeSpan? MinimumTradeInterval { get; }
    public TimeSpan? MaximumTradeInterval { get; }
    public TimeSpan? MeanTradeInterval { get; }
    public TimeSpan? LatestTradeInterval { get; }
    public decimal? TradesPerSecondRaw { get; }
    public decimal? ContractsPerSecondRaw { get; }
    public decimal? MaximumLevelExecutedVolume { get; }
    public long? MaximumLevelTradeCount { get; }
    public decimal? MaximumAbsoluteLevelDelta { get; }
    public int RevisitedLevelCount { get; }
    public int MaximumVisitCount { get; }
    public int ClassifiedLevelCount { get; }
    public int UnknownOnlyLevelCount { get; }
    public int SamePriceRatioAvailabilityCount { get; }
    public int DiagonalRatioAvailabilityCount { get; }
    public int RawAskDominantLevelCount { get; }
    public int RawBidDominantLevelCount { get; }
    public int RawEqualLevelCount { get; }
    public int RawUnknownDominantLevelCount { get; }
    public int MaximumConsecutiveRawAskDominanceTicks { get; }
    public int MaximumConsecutiveRawBidDominanceTicks { get; }
    public int ClusterPopulationSize { get; }
    public EfficiencyAvailability EvidenceAvailability { get; }
    public IReadOnlyList<string> Limitations { get; }
}

public sealed class AuctionResultEvidenceVector
{
    public AuctionResultEvidenceVector(
        long? firstPriceTick,
        long? latestPriceTick,
        long? highPriceTick,
        long? lowPriceTick,
        long? netPriceProgressTicks,
        long? grossRangeTicks,
        long? maximumFavorableProgressTicks,
        long? maximumAdverseProgressTicks,
        long? progressRetainedTicks,
        decimal? progressRetentionRatio,
        TimeSpan? timeToMaximumFavorableProgress,
        TimeSpan? timeToLatestProgress,
        TimeSpan? timeAtMaximumExcursion,
        long? episodeReferenceDistanceStartTicks,
        long? episodeReferenceDistanceLatestTicks,
        long? maximumDistanceFromReferenceTicks,
        long? currentDistanceFromReferenceTicks,
        bool? geometricReentryObserved,
        TimeSpan? timeMaintainedInside,
        decimal? outsideTimeRatio,
        decimal? outsideVolumeRatio,
        decimal? outsideTradeRatio,
        long? localPocTick,
        long? localPocDisplacementTicks,
        long? developingTpoPocStartTick,
        long? developingTpoPocLatestTick,
        long? tpoPocMigrationTicks,
        long? developingVolumePocStartTick,
        long? developingVolumePocLatestTick,
        long? volumePocMigrationTicks,
        long? developingTpoValueLowStartTick,
        long? developingTpoValueHighStartTick,
        long? developingTpoValueLowLatestTick,
        long? developingTpoValueHighLatestTick,
        long? developingVolumeValueLowStartTick,
        long? developingVolumeValueHighStartTick,
        long? developingVolumeValueLowLatestTick,
        long? developingVolumeValueHighLatestTick,
        long? tpoValueCentroidMigrationTicks,
        long? volumeValueCentroidMigrationTicks,
        string? priceLocationAtStart,
        string? priceLocationLatest,
        EfficiencyAvailability evidenceAvailability,
        IReadOnlyList<string> limitations)
    {
        FirstPriceTick = firstPriceTick;
        LatestPriceTick = latestPriceTick;
        HighPriceTick = highPriceTick;
        LowPriceTick = lowPriceTick;
        NetPriceProgressTicks = netPriceProgressTicks;
        GrossRangeTicks = grossRangeTicks;
        MaximumFavorableProgressTicks = maximumFavorableProgressTicks;
        MaximumAdverseProgressTicks = maximumAdverseProgressTicks;
        ProgressRetainedTicks = progressRetainedTicks;
        ProgressRetentionRatio = progressRetentionRatio;
        TimeToMaximumFavorableProgress = timeToMaximumFavorableProgress;
        TimeToLatestProgress = timeToLatestProgress;
        TimeAtMaximumExcursion = timeAtMaximumExcursion;
        EpisodeReferenceDistanceStartTicks = episodeReferenceDistanceStartTicks;
        EpisodeReferenceDistanceLatestTicks = episodeReferenceDistanceLatestTicks;
        MaximumDistanceFromReferenceTicks = maximumDistanceFromReferenceTicks;
        CurrentDistanceFromReferenceTicks = currentDistanceFromReferenceTicks;
        GeometricReentryObserved = geometricReentryObserved;
        TimeMaintainedInside = timeMaintainedInside;
        OutsideTimeRatio = outsideTimeRatio;
        OutsideVolumeRatio = outsideVolumeRatio;
        OutsideTradeRatio = outsideTradeRatio;
        LocalPocTick = localPocTick;
        LocalPocDisplacementTicks = localPocDisplacementTicks;
        DevelopingTpoPocStartTick = developingTpoPocStartTick;
        DevelopingTpoPocLatestTick = developingTpoPocLatestTick;
        TpoPocMigrationTicks = tpoPocMigrationTicks;
        DevelopingVolumePocStartTick = developingVolumePocStartTick;
        DevelopingVolumePocLatestTick = developingVolumePocLatestTick;
        VolumePocMigrationTicks = volumePocMigrationTicks;
        DevelopingTpoValueLowStartTick = developingTpoValueLowStartTick;
        DevelopingTpoValueHighStartTick = developingTpoValueHighStartTick;
        DevelopingTpoValueLowLatestTick = developingTpoValueLowLatestTick;
        DevelopingTpoValueHighLatestTick = developingTpoValueHighLatestTick;
        DevelopingVolumeValueLowStartTick = developingVolumeValueLowStartTick;
        DevelopingVolumeValueHighStartTick = developingVolumeValueHighStartTick;
        DevelopingVolumeValueLowLatestTick = developingVolumeValueLowLatestTick;
        DevelopingVolumeValueHighLatestTick = developingVolumeValueHighLatestTick;
        TpoValueCentroidMigrationTicks = tpoValueCentroidMigrationTicks;
        VolumeValueCentroidMigrationTicks = volumeValueCentroidMigrationTicks;
        PriceLocationAtStart = priceLocationAtStart;
        PriceLocationLatest = priceLocationLatest;
        EvidenceAvailability = evidenceAvailability;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public long? FirstPriceTick { get; }
    public long? LatestPriceTick { get; }
    public long? HighPriceTick { get; }
    public long? LowPriceTick { get; }
    public long? NetPriceProgressTicks { get; }
    public long? GrossRangeTicks { get; }
    public long? MaximumFavorableProgressTicks { get; }
    public long? MaximumAdverseProgressTicks { get; }
    public long? ProgressRetainedTicks { get; }
    public decimal? ProgressRetentionRatio { get; }
    public TimeSpan? TimeToMaximumFavorableProgress { get; }
    public TimeSpan? TimeToLatestProgress { get; }
    public TimeSpan? TimeAtMaximumExcursion { get; }
    public long? EpisodeReferenceDistanceStartTicks { get; }
    public long? EpisodeReferenceDistanceLatestTicks { get; }
    public long? MaximumDistanceFromReferenceTicks { get; }
    public long? CurrentDistanceFromReferenceTicks { get; }
    public bool? GeometricReentryObserved { get; }
    public TimeSpan? TimeMaintainedInside { get; }
    public decimal? OutsideTimeRatio { get; }
    public decimal? OutsideVolumeRatio { get; }
    public decimal? OutsideTradeRatio { get; }
    public long? LocalPocTick { get; }
    public long? LocalPocDisplacementTicks { get; }
    public long? DevelopingTpoPocStartTick { get; }
    public long? DevelopingTpoPocLatestTick { get; }
    public long? TpoPocMigrationTicks { get; }
    public long? DevelopingVolumePocStartTick { get; }
    public long? DevelopingVolumePocLatestTick { get; }
    public long? VolumePocMigrationTicks { get; }
    public long? DevelopingTpoValueLowStartTick { get; }
    public long? DevelopingTpoValueHighStartTick { get; }
    public long? DevelopingTpoValueLowLatestTick { get; }
    public long? DevelopingTpoValueHighLatestTick { get; }
    public long? DevelopingVolumeValueLowStartTick { get; }
    public long? DevelopingVolumeValueHighStartTick { get; }
    public long? DevelopingVolumeValueLowLatestTick { get; }
    public long? DevelopingVolumeValueHighLatestTick { get; }
    public long? TpoValueCentroidMigrationTicks { get; }
    public long? VolumeValueCentroidMigrationTicks { get; }
    public string? PriceLocationAtStart { get; }
    public string? PriceLocationLatest { get; }
    public EfficiencyAvailability EvidenceAvailability { get; }
    public IReadOnlyList<string> Limitations { get; }
}

/// <summary>Descriptive raw relationships only — not an EfficiencyScore.</summary>
public sealed class AuctionEfficiencyRawRelationships
{
    public AuctionEfficiencyRawRelationships(
        decimal? netProgressPerExecutedContract,
        decimal? grossRangePerExecutedContract,
        decimal? favorableProgressPerExecutedContract,
        decimal? netProgressPerTrade,
        decimal? favorableProgressPerTrade,
        decimal? executedContractsPerTickOfGrossRange,
        decimal? classifiedAbsoluteDeltaPerTickOfGrossRange,
        decimal? classifiedAbsoluteDeltaPerTickOfFavorableProgress,
        decimal? timePerNetProgressTick,
        decimal? timePerFavorableProgressTick,
        decimal? tradesPerNetProgressTick,
        decimal? volumePerNetProgressTick)
    {
        NetProgressPerExecutedContract = netProgressPerExecutedContract;
        GrossRangePerExecutedContract = grossRangePerExecutedContract;
        FavorableProgressPerExecutedContract = favorableProgressPerExecutedContract;
        NetProgressPerTrade = netProgressPerTrade;
        FavorableProgressPerTrade = favorableProgressPerTrade;
        ExecutedContractsPerTickOfGrossRange = executedContractsPerTickOfGrossRange;
        ClassifiedAbsoluteDeltaPerTickOfGrossRange = classifiedAbsoluteDeltaPerTickOfGrossRange;
        ClassifiedAbsoluteDeltaPerTickOfFavorableProgress = classifiedAbsoluteDeltaPerTickOfFavorableProgress;
        TimePerNetProgressTick = timePerNetProgressTick;
        TimePerFavorableProgressTick = timePerFavorableProgressTick;
        TradesPerNetProgressTick = tradesPerNetProgressTick;
        VolumePerNetProgressTick = volumePerNetProgressTick;
    }

    public decimal? NetProgressPerExecutedContract { get; }
    public decimal? GrossRangePerExecutedContract { get; }
    public decimal? FavorableProgressPerExecutedContract { get; }
    public decimal? NetProgressPerTrade { get; }
    public decimal? FavorableProgressPerTrade { get; }
    public decimal? ExecutedContractsPerTickOfGrossRange { get; }
    public decimal? ClassifiedAbsoluteDeltaPerTickOfGrossRange { get; }
    public decimal? ClassifiedAbsoluteDeltaPerTickOfFavorableProgress { get; }
    public decimal? TimePerNetProgressTick { get; }
    public decimal? TimePerFavorableProgressTick { get; }
    public decimal? TradesPerNetProgressTick { get; }
    public decimal? VolumePerNetProgressTick { get; }
}
