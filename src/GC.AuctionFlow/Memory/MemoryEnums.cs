namespace GC.AuctionFlow.Memory;

public enum MemoryModuleState
{
    Disabled = 0,
    AwaitingEpisodes = 1,
    Ready = 2,
    Partial = 3,
    Invalid = 4
}

/// <summary>
/// Outcome of one test against a reference (v1.3 §13, KDK Ch 27).
///
/// Whether a test HELD the reference or BROKE it needs calibrated acceptance and
/// re-entry resolution, so those verdicts stay reserved. What is observable is
/// whether the test is still running and how it terminated mechanically.
/// </summary>
public enum ReferenceTestOutcome
{
    Unknown = 0,

    /// <summary>The episode against this reference is still open.</summary>
    InProgress = 1,

    /// <summary>Episode expired without a calibrated resolution.</summary>
    Expired = 2,

    /// <summary>Episode ended on invalid data.</summary>
    InvalidData = 3,

    /// <summary>Test concluded, but held-vs-broken requires calibration.</summary>
    NotCalibrated = 4,

    // Reserved — calibration required before any of these can be emitted.

    /// <summary>Reference held: price was rejected and did not accept beyond it.</summary>
    HeldRejected = 100,

    /// <summary>Reference broke: acceptance was established beyond it.</summary>
    Broken = 101
}

/// <summary>
/// Whether repeated testing has changed a reference's significance.
///
/// v1.3 G-REF-001: there is NO one-directional rule that a level tested many times
/// becomes weaker, or stronger. KDK Ch 27 is explicit that liquidity can be
/// replenished between tests. Every verdict here is therefore reserved, and the
/// engine records raw counts instead of inferring.
/// </summary>
public enum ReferenceStrengthState
{
    Unknown = 0,

    /// <summary>Always emitted. Test count is recorded; significance is not inferred.</summary>
    NotCalibrated = 1,

    // Reserved — calibration required before any of these can be emitted.
    Strengthening = 100,
    Weakening = 101,
    Stable = 102
}

/// <summary>
/// Whether passive liquidity was replenished between tests (KDK Ch 27).
/// This is Tier-3 advertised liquidity and needs MBO, which is BLOCKED.
/// </summary>
public enum LiquidityReplenishmentObservability
{
    /// <summary>MBO unavailable — cannot observe. Never inferred from price alone.</summary>
    Unavailable = 0,

    // Reserved — requires an active MBO subscription.
    Observed = 100,
    NotObserved = 101
}

public enum MemoryDataQuality
{
    Complete = 0,
    Partial = 1,
    Invalid = 2
}
