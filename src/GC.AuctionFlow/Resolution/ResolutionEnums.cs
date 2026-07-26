namespace GC.AuctionFlow.Resolution;

/// <summary>Phase 2D module status. Resolution conclusions require calibrated thresholds.</summary>
public enum ResolutionModuleState
{
    Disabled = 0,
    AwaitingEvidence = 1,
    Ready = 2,
    Partial = 3,
    Invalid = 4
}

/// <summary>
/// Acceptance resolution conclusion per EvidenceId.
/// Established/Failed require calibrated thresholds — NEVER emitted in Phase 2D (NOT_CALIBRATED).
/// </summary>
public enum AcceptanceResolutionState
{
    Unknown = 0,
    None = 1,
    Early = 2,
    Developing = 3,
    NotCalibrated = 4,

    // Reserved — require calibrated thresholds; never emit in Phase 2D:
    Established = 100,
    Failed = 101
}

/// <summary>
/// Re-entry resolution conclusion per EvidenceId.
/// StableReacceptance/ReentryFailed require calibrated thresholds — NEVER emitted in Phase 2D.
/// </summary>
public enum ReentryResolutionState
{
    Unknown = 0,
    None = 1,
    GeometricReentry = 2,
    Developing = 3,
    NotCalibrated = 4,

    // Reserved — require calibrated thresholds; never emit in Phase 2D:
    StableReacceptance = 100,
    ReentryFailed = 101
}

/// <summary>
/// Overall auction resolution conclusion. FAR/AAC require full calibration — always NotCalibrated in Phase 2D.
/// </summary>
public enum AuctionResolutionConclusion
{
    Unknown = 0,
    NotCalibrated = 1,

    // Reserved — require calibrated FAR/AAC thresholds; never emit in Phase 2D:
    FarCandidate = 100,
    AacCandidate = 101
}

public enum ResolutionDataQuality
{
    Complete = 0,
    Partial = 1,
    Invalid = 2
}
