using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Reference;

namespace GC.AuctionFlow.Evidence;

public sealed class AcceptanceEvidenceVector
{
    public AcceptanceEvidenceVector(
        TimeSpan outsideTime,
        TimeSpan totalObservedTime,
        decimal? outsideTimeRatio,
        decimal outsideExecutedVolume,
        decimal totalObservedExecutedVolume,
        decimal? outsideVolumeRatio,
        long outsideTradeCount,
        long totalObservedTradeCount,
        decimal? outsideTradeCountRatio,
        int outsideSegmentCount,
        int attemptCount,
        long maximumOutsideDistanceTicks,
        long? currentDistanceFromReferenceTicks,
        decimal? outsideBidVolume,
        decimal? outsideAskVolume,
        decimal? outsideDelta,
        AggressorEvidenceAvailability aggressorEvidenceAvailability,
        long? localPocTick,
        long? localPocDisplacementTicks,
        ReferenceSidePosition? latestSide,
        int consecutiveOutsideEvents,
        TimeSpan? elapsedSinceLastInsideEvent,
        decimal? outsideCloseRatio,
        int? tpoCountOutside,
        long? valueCentroidDisplacement,
        bool? oldValueReclaimFailure,
        string? retestHoldQuality,
        long? localValueLow,
        long? localValueHigh,
        IReadOnlyList<string> unavailableReasons)
    {
        OutsideTime = outsideTime;
        TotalObservedTime = totalObservedTime;
        OutsideTimeRatio = outsideTimeRatio;
        OutsideExecutedVolume = outsideExecutedVolume;
        TotalObservedExecutedVolume = totalObservedExecutedVolume;
        OutsideVolumeRatio = outsideVolumeRatio;
        OutsideTradeCount = outsideTradeCount;
        TotalObservedTradeCount = totalObservedTradeCount;
        OutsideTradeCountRatio = outsideTradeCountRatio;
        OutsideSegmentCount = outsideSegmentCount;
        AttemptCount = attemptCount;
        MaximumOutsideDistanceTicks = maximumOutsideDistanceTicks;
        CurrentDistanceFromReferenceTicks = currentDistanceFromReferenceTicks;
        OutsideBidVolume = outsideBidVolume;
        OutsideAskVolume = outsideAskVolume;
        OutsideDelta = outsideDelta;
        AggressorEvidenceAvailability = aggressorEvidenceAvailability;
        LocalPocTick = localPocTick;
        LocalPocDisplacementTicks = localPocDisplacementTicks;
        LatestSide = latestSide;
        ConsecutiveOutsideEvents = consecutiveOutsideEvents;
        ElapsedSinceLastInsideEvent = elapsedSinceLastInsideEvent;
        OutsideCloseRatio = outsideCloseRatio;
        TpoCountOutside = tpoCountOutside;
        ValueCentroidDisplacement = valueCentroidDisplacement;
        OldValueReclaimFailure = oldValueReclaimFailure;
        RetestHoldQuality = retestHoldQuality;
        LocalValueLow = localValueLow;
        LocalValueHigh = localValueHigh;
        UnavailableReasons = unavailableReasons ?? Array.Empty<string>();
    }

    public TimeSpan OutsideTime { get; }
    public TimeSpan TotalObservedTime { get; }
    public decimal? OutsideTimeRatio { get; }
    public decimal OutsideExecutedVolume { get; }
    public decimal TotalObservedExecutedVolume { get; }
    public decimal? OutsideVolumeRatio { get; }
    public long OutsideTradeCount { get; }
    public long TotalObservedTradeCount { get; }
    public decimal? OutsideTradeCountRatio { get; }
    public int OutsideSegmentCount { get; }
    public int AttemptCount { get; }
    public long MaximumOutsideDistanceTicks { get; }
    public long? CurrentDistanceFromReferenceTicks { get; }
    public decimal? OutsideBidVolume { get; }
    public decimal? OutsideAskVolume { get; }
    public decimal? OutsideDelta { get; }
    public AggressorEvidenceAvailability AggressorEvidenceAvailability { get; }
    public long? LocalPocTick { get; }
    public long? LocalPocDisplacementTicks { get; }
    public ReferenceSidePosition? LatestSide { get; }
    public int ConsecutiveOutsideEvents { get; }
    public TimeSpan? ElapsedSinceLastInsideEvent { get; }
    public decimal? OutsideCloseRatio { get; }
    public int? TpoCountOutside { get; }
    public long? ValueCentroidDisplacement { get; }
    public bool? OldValueReclaimFailure { get; }
    public string? RetestHoldQuality { get; }
    public long? LocalValueLow { get; }
    public long? LocalValueHigh { get; }
    public IReadOnlyList<string> UnavailableReasons { get; }
}

public sealed class ReentryEvidenceVector
{
    public ReentryEvidenceVector(
        bool geometricReentryObserved,
        DateTime? firstGeometricReentryAt,
        TimeSpan? reentrySpeed,
        long maximumDistanceReturnedInsideTicks,
        long? currentDistanceInsideTicks,
        TimeSpan timeMaintainedInside,
        decimal insideExecutedVolumeAfterReentry,
        long insideTradeCountAfterReentry,
        decimal? insideBidVolumeAfterReentry,
        decimal? insideAskVolumeAfterReentry,
        decimal? insideDeltaAfterReentry,
        AggressorEvidenceAvailability aggressorEvidenceAvailability,
        int subsequentReferenceTestCount,
        int outsideReattemptCount,
        ReferenceSidePosition? latestSide,
        long? localPocTick,
        long? localPocRelativeToReference,
        long? localPocChangeSinceReentry,
        bool? oppositeAggressionQuality,
        bool? localValueRebuildInside,
        bool? oldDirectionAggressionEffectiveness,
        bool? stableReacceptance,
        bool? reentryFailure,
        IReadOnlyList<string> unavailableReasons)
    {
        GeometricReentryObserved = geometricReentryObserved;
        FirstGeometricReentryAt = firstGeometricReentryAt;
        ReentrySpeed = reentrySpeed;
        MaximumDistanceReturnedInsideTicks = maximumDistanceReturnedInsideTicks;
        CurrentDistanceInsideTicks = currentDistanceInsideTicks;
        TimeMaintainedInside = timeMaintainedInside;
        InsideExecutedVolumeAfterReentry = insideExecutedVolumeAfterReentry;
        InsideTradeCountAfterReentry = insideTradeCountAfterReentry;
        InsideBidVolumeAfterReentry = insideBidVolumeAfterReentry;
        InsideAskVolumeAfterReentry = insideAskVolumeAfterReentry;
        InsideDeltaAfterReentry = insideDeltaAfterReentry;
        AggressorEvidenceAvailability = aggressorEvidenceAvailability;
        SubsequentReferenceTestCount = subsequentReferenceTestCount;
        OutsideReattemptCount = outsideReattemptCount;
        LatestSide = latestSide;
        LocalPocTick = localPocTick;
        LocalPocRelativeToReference = localPocRelativeToReference;
        LocalPocChangeSinceReentry = localPocChangeSinceReentry;
        OppositeAggressionQuality = oppositeAggressionQuality;
        LocalValueRebuildInside = localValueRebuildInside;
        OldDirectionAggressionEffectiveness = oldDirectionAggressionEffectiveness;
        StableReacceptance = stableReacceptance;
        ReentryFailure = reentryFailure;
        UnavailableReasons = unavailableReasons ?? Array.Empty<string>();
    }

    public bool GeometricReentryObserved { get; }
    public DateTime? FirstGeometricReentryAt { get; }
    public TimeSpan? ReentrySpeed { get; }
    public long MaximumDistanceReturnedInsideTicks { get; }
    public long? CurrentDistanceInsideTicks { get; }
    public TimeSpan TimeMaintainedInside { get; }
    public decimal InsideExecutedVolumeAfterReentry { get; }
    public long InsideTradeCountAfterReentry { get; }
    public decimal? InsideBidVolumeAfterReentry { get; }
    public decimal? InsideAskVolumeAfterReentry { get; }
    public decimal? InsideDeltaAfterReentry { get; }
    public AggressorEvidenceAvailability AggressorEvidenceAvailability { get; }
    public int SubsequentReferenceTestCount { get; }
    public int OutsideReattemptCount { get; }
    public ReferenceSidePosition? LatestSide { get; }
    public long? LocalPocTick { get; }
    public long? LocalPocRelativeToReference { get; }
    public long? LocalPocChangeSinceReentry { get; }
    public bool? OppositeAggressionQuality { get; }
    public bool? LocalValueRebuildInside { get; }
    public bool? OldDirectionAggressionEffectiveness { get; }
    public bool? StableReacceptance { get; }
    public bool? ReentryFailure { get; }
    public IReadOnlyList<string> UnavailableReasons { get; }
}
