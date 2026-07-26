namespace GC.AuctionFlow.Thesis;

public enum ThesisModuleState
{
    Disabled = 0,
    AwaitingEvidence = 1,
    Ready = 2,
    Partial = 3,
    Invalid = 4
}

public enum ThesisDirection
{
    Unknown = 0,
    Long = 1,
    Short = 2
}

public enum ThesisDataQuality
{
    Complete = 0,
    Partial = 1,
    Invalid = 2
}

/// <summary>
/// FAR state machine — 15 states per spec Chapter 26.
/// Observable states: Idle through ReentryDeveloping.
/// Armed and beyond require calibrated thresholds — never emitted in Phase 3.
/// </summary>
public enum FarState
{
    Idle = 0,
    Approaching = 1,
    EpisodeActive = 2,
    OutsideAttempt = 3,
    ReentryDeveloping = 4,
    ReacceptedInside = 5,

    // Calibrated — never emitted in Phase 3 (NotCalibrated policy):
    Armed = 100,
    MicroRetest = 101,
    StructuralRetest = 102,
    ConfirmedAttempt = 103,
    Executable = 104,
    Managing = 105,

    // Terminal:
    Invalidated = 200,
    Expired = 201,
    Completed = 202
}

/// <summary>
/// AAC state machine — 14 states per spec Chapter 27.
/// Observable states: Idle through OutsideAttempt.
/// AcceptanceDeveloping and beyond require calibrated thresholds — never emitted in Phase 3.
/// </summary>
public enum AacState
{
    Idle = 0,
    Approaching = 1,
    EpisodeActive = 2,
    OutsideAttempt = 3,
    AcceptanceDeveloping = 4,
    AcceptedOutside = 5,
    Pullback = 6,

    // Calibrated — never emitted in Phase 3 (NotCalibrated policy):
    Armed = 100,
    Executable = 101,
    Managing = 102,

    // Terminal:
    ReacceptedOldValue = 200,
    Invalidated = 201,
    Expired = 202,
    Completed = 203
}
