namespace GC.AuctionFlow.Episode;

/// <summary>
/// Read-only geometric measurement observation emitted by Phase 1E on admitted trades.
/// Phase 1F consumes this feed; it must not mutate Episode State or Resolution.
/// </summary>
public sealed class EpisodeMeasurementEvent
{
    public EpisodeMeasurementEvent(
        string episodeId,
        string eventIdentity,
        long localMonotonicSequence,
        DateTime eventUtc,
        long priceTick,
        decimal volume,
        bool isAsk,
        bool isBid,
        bool aggressorClassified,
        string referenceId,
        ReferenceInteractionRole referenceRole,
        long referencePriceTick,
        ReferenceSidePosition side,
        EpisodeState stateBefore,
        EpisodeState stateAfter,
        int attemptCount,
        long distanceFromReferenceTicks,
        bool attemptIncremented,
        bool outsideSegmentOpened,
        bool outsideSegmentClosed,
        bool geometricReentryObserved,
        bool episodeCreated,
        long eventRevision)
    {
        EpisodeId = episodeId ?? "";
        EventIdentity = eventIdentity ?? "";
        LocalMonotonicSequence = localMonotonicSequence;
        EventUtc = eventUtc;
        PriceTick = priceTick;
        Volume = volume;
        IsAsk = isAsk;
        IsBid = isBid;
        AggressorClassified = aggressorClassified;
        ReferenceId = referenceId ?? "";
        ReferenceRole = referenceRole;
        ReferencePriceTick = referencePriceTick;
        Side = side;
        StateBefore = stateBefore;
        StateAfter = stateAfter;
        AttemptCount = attemptCount;
        DistanceFromReferenceTicks = distanceFromReferenceTicks;
        AttemptIncremented = attemptIncremented;
        OutsideSegmentOpened = outsideSegmentOpened;
        OutsideSegmentClosed = outsideSegmentClosed;
        GeometricReentryObserved = geometricReentryObserved;
        EpisodeCreated = episodeCreated;
        EventRevision = eventRevision;
    }

    public string EpisodeId { get; }
    public string EventIdentity { get; }
    public long LocalMonotonicSequence { get; }
    public DateTime EventUtc { get; }
    public long PriceTick { get; }
    public decimal Volume { get; }
    public bool IsAsk { get; }
    public bool IsBid { get; }
    public bool AggressorClassified { get; }
    public string ReferenceId { get; }
    public ReferenceInteractionRole ReferenceRole { get; }
    public long ReferencePriceTick { get; }
    public ReferenceSidePosition Side { get; }
    public EpisodeState StateBefore { get; }
    public EpisodeState StateAfter { get; }
    public int AttemptCount { get; }
    public long DistanceFromReferenceTicks { get; }
    public bool AttemptIncremented { get; }
    public bool OutsideSegmentOpened { get; }
    public bool OutsideSegmentClosed { get; }
    public bool GeometricReentryObserved { get; }
    public bool EpisodeCreated { get; }
    public long EventRevision { get; }
}
