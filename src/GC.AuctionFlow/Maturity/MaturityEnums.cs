namespace GC.AuctionFlow.Maturity;

public enum MaturityModuleState
{
    Disabled = 0,
    AwaitingThesis = 1,
    Ready = 2,
    Partial = 3,
    Invalid = 4
}

/// <summary>
/// Signal maturity level (v1.2 §29.2, v1.3 §9.1).
/// Fast/Standard/Confirmed require calibrated criteria — never emitted in Phase 3B.
/// </summary>
public enum SignalMaturityLevel
{
    Unknown = 0,
    NotCalibrated = 1,
    // Reserved — calibration required before any of these can be emitted.
    Fast = 100,
    Standard = 101,
    Confirmed = 102
}

/// <summary>
/// Analysis lifecycle (v1.2 §29.1).
/// Armed and beyond require calibrated maturity criteria — never emitted in Phase 3B.
/// </summary>
public enum AnalysisLifecycleState
{
    Observation = 0,
    Approaching = 1,
    EpisodeActive = 2,
    Candidate = 3,
    NotCalibrated = 4,
    // Reserved — calibration required before any of these can be emitted.
    Armed = 100,
    Executable = 101,
    Managing = 102,
    // Terminal:
    Completed = 200,
    Invalidated = 201,
    Expired = 202
}

/// <summary>
/// Expected behaviour contract (v1.3 §9.3 — KDK Ch 63).
/// Declares, at candidate creation, what the market MUST do if the thesis is correct.
/// </summary>
public enum ExpectedBehaviorContractKind
{
    Unknown = 0,
    /// <summary>FAR re-entry: price enters old value and does NOT quickly restore the outside area.</summary>
    FarReentry = 1,
    /// <summary>FAR retest: the test builds no acceptance outside; opposing flow produces result.</summary>
    FarRetest = 2,
    /// <summary>AAC early: price holds outside the reference; local POC/value does NOT return inside.</summary>
    AacEarly = 3,
    /// <summary>AAC retest: old-value reclaim attempt fails; price restores the accepted area.</summary>
    AacRetest = 4,
    /// <summary>New value continuation: new value area holds and POC keeps migrating.</summary>
    NewValueContinuation = 5,
    /// <summary>Rotation: price builds no value outside the boundary and returns to the centre.</summary>
    Rotation = 6
}

/// <summary>
/// Retest observation (v1.3 §9.2).
/// Micro / structural / second-attempt discrimination requires calibrated thresholds.
/// </summary>
public enum RetestObservationState
{
    Unknown = 0,
    NotObserved = 1,
    RetestObserved = 2,
    NotCalibrated = 3,
    // Reserved — calibration required before any of these can be emitted.
    MicroRetest = 100,
    StructuralRetest = 101,
    SecondAttempt = 102
}

/// <summary>Why a maturity level could not be determined. Never empty in Phase 3B.</summary>
public enum MaturityBlockingReason
{
    None = 0,
    ThresholdsNotCalibrated = 1,
    RetestDiscriminationNotCalibrated = 2,
    ExpectedBehaviorDeadlineNotCalibrated = 3,
    MicroConfirmationNotCalibrated = 4,
    TargetSpaceUnavailable = 5,
    PriceLocationUnavailable = 6,
    ThesisStateNotCalibrated = 7,
    FastShadowOnly = 8,
    LowQualityLocation = 9,
    /// <summary>Measured: no target lies ahead. Distinct from TargetSpaceUnavailable,
    /// which means the space could not be measured at all.</summary>
    NoRemainingTargetSpace = 10
}

public enum MaturityDataQuality
{
    Complete = 0,
    Partial = 1,
    Invalid = 2
}

/// <summary>
/// Location gate outcome (v1.3 §10, guards G-LOC-001..003).
///
/// v1.2 §2.3 states that orderflow only has meaning in Context and Location.
/// This gate is what enforces the Location half of that claim.
/// </summary>
public enum LocationGateOutcome
{
    Unknown = 0,

    /// <summary>
    /// G-LOC-003: price location is unavailable, so no candidate may be produced.
    /// Enforced — this is a pure availability check.
    /// </summary>
    BlockedLocationUnavailable = 1,

    /// <summary>
    /// G-LOC-001: mid-value or at-POC. The worst location for FAR/AAC; the candidate
    /// is allowed to exist but may never mature to Confirmed.
    /// </summary>
    AllowedLowQuality = 2,

    /// <summary>At a value-area boundary — where episodes legitimately open.</summary>
    AllowedBoundary = 3,

    /// <summary>Outside value — where FAR (failure) and AAC (acceptance) live.</summary>
    AllowedOutside = 4,

    /// <summary>
    /// G-LOC-002: no remaining target space, hard veto regardless of score.
    /// Reserved — RemainingTargetSpace does not exist until the Target Engine
    /// (Phase 3E), so this outcome can never be reached yet.
    /// </summary>
    BlockedNoTargetSpace = 100
}
