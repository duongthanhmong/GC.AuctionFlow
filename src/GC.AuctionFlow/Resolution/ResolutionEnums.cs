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

/// <summary>
/// Old-value reclaim test (v1.3 §6.2, G-ACC-005; KDK Ch 18).
/// This is the FAR-vs-AAC decision axis: a reclaim that is attempted AND held
/// supports FAR; a reclaim that fails or is only fleeting supports AAC; no attempt
/// at all means the auction is still Unresolved.
///
/// A nullable bool is explicitly insufficient — "not attempted" and "attempted but
/// outcome unknown" are different facts and must not collapse to null.
/// </summary>
public enum OldValueReclaimState
{
    /// <summary>Re-entry evidence unavailable — cannot say whether an attempt occurred.</summary>
    Unknown = 0,

    /// <summary>Observable: no reclaim attempt has been made.</summary>
    NotAttempted = 1,

    /// <summary>
    /// Observable: a reclaim attempt occurred, but held-vs-failed requires
    /// calibrated dwell/volume/structure thresholds.
    /// </summary>
    AttemptedOutcomeNotCalibrated = 2,

    // Reserved — calibration required before any of these can be emitted.

    /// <summary>Reclaim attempted and sustained — supports FAR.</summary>
    AttemptedAndHeld = 100,

    /// <summary>Reclaim attempted and failed, or held only fleetingly — supports AAC.</summary>
    AttemptedAndFailed = 101
}
