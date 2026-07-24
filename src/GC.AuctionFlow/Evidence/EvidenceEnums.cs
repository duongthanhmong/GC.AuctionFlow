namespace GC.AuctionFlow.Evidence;

/// <summary>Phase 1F module status. Measurement only — no resolution conclusions.</summary>
public enum EvidenceModuleState
{
    Disabled = 0,
    AwaitingEpisodes = 1,
    Ready = 2,
    Partial = 3,
    Invalid = 4
}

/// <summary>Acceptance observation maturity. Established/Failed reserved — never emitted.</summary>
public enum AcceptanceObservationState
{
    None = 0,
    Early = 1,
    Developing = 2,
    Unresolved = 3,
    Unknown = 4,

    // Reserved — never emit in Phase 1F:
    Established = 100,
    Failed = 101
}

/// <summary>Re-entry observation maturity. StableReaccepted/ReentryFailed reserved — never emitted.</summary>
public enum ReentryObservationState
{
    None = 0,
    GeometricReentry = 1,
    Developing = 2,
    Unresolved = 3,
    Unknown = 4,

    // Reserved — never emit in Phase 1F:
    StableReaccepted = 100,
    ReentryFailed = 101
}

public enum EvidenceMeasurementStatus
{
    Active = 0,
    Frozen = 1,
    Invalid = 2
}

public enum EvidenceComponentAvailability
{
    Available = 0,
    Unavailable = 1,
    NotApplicable = 2,
    NotCalibrated = 3
}
