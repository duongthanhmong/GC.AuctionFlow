namespace GC.AuctionFlow.Episode;

/// <summary>Phase 1E observational episode states. Acceptance/re-entry reserved for Phase 1F.</summary>
public enum EpisodeState
{
    Interacting = 0,
    OutsideAttempt = 1,
    Developing = 2,
    ReentryDeveloping = 3,
    EpisodeExpired = 4,
    InvalidData = 5,

    // Reserved — never emitted in Phase 1E:
    ApproachingReference = 100,
    AcceptanceOutside = 101,
    ReacceptedInside = 102,
    ReentryFailed = 103,
    UnresolvedRotation = 104,
    TransitionedToNewBalance = 105
}

public enum EpisodeResolution
{
    None = 0,
    Expired = 1,
    InvalidData = 2,

    // Reserved — never emitted in Phase 1E:
    AcceptedOutside = 100,
    ReacceptedInside = 101,
    UnresolvedRotation = 102,
    TransitionedToNewBalance = 103
}

public enum ReferenceInteractionRole
{
    UpperBoundary = 0,
    LowerBoundary = 1,
    Centerline = 2,
    Unsupported = 3
}

public enum EpisodeInteractionDirection
{
    Unknown = 0,
    Up = 1,
    Down = 2,
    Bidirectional = 3
}

public enum EpisodeModuleState
{
    Disabled = 0,
    AwaitingReferences = 1,
    AwaitingTrades = 2,
    Ready = 3,
    Partial = 4,
    Invalid = 5
}

public enum EpisodeHistoryMode
{
    LiveOnly = 0,
    ExactHistoricalReplay = 1
}

public enum AggressorEvidenceAvailability
{
    Available = 0,
    Unavailable = 1,
    Partial = 2
}

public enum EpisodeDataQuality
{
    Unknown = 0,
    Partial = 1,
    Complete = 2,
    Invalid = 3
}

/// <summary>Geometric position of price relative to a reference zone (exact ticks).</summary>
public enum ReferenceSidePosition
{
    AtReference = 0,
    Above = 1,
    Below = 2
}

/// <summary>Admission outcome for one normalized Episode trade event. Does not create episodes.</summary>
public enum EpisodeTradeAdmissionResult
{
    Accepted = 0,
    Duplicate = 1,
    OutOfOrder = 2,
    CompatibilityMismatch = 3,
    ModuleDisabled = 4,
    InvalidEvent = 5,
    MappingFailed = 6
}
