namespace GC.AuctionFlow.Plar;

public enum PlarModuleState
{
    Disabled = 0,
    AwaitingReferences = 1,
    Ready = 2,
    Partial = 3,
    Invalid = 4
}

/// <summary>Direction the path is projected in. Derived from thesis direction.</summary>
public enum PathDirection
{
    Unknown = 0,
    Up = 1,
    Down = 2
}

/// <summary>
/// What a structural level does on the path (v1.2 §33.1-33.2, v1.3 §7.4 G-FAR-006).
///
/// POC is deliberately <see cref="TargetAndBarrier"/>: KDK Ch 64 lists it as a valid FAR
/// target, and KDK Ch 19 Rule 4 warns it can break the rotation path before the opposite
/// edge is reached. Modelling it only as a target is the mistake G-FAR-006 names.
/// </summary>
public enum PathObstacleRole
{
    Unknown = 0,

    /// <summary>A destination the auction may rotate to.</summary>
    Target = 1,

    /// <summary>Something in the way that must be resolved first.</summary>
    Barrier = 2,

    /// <summary>Both — reaching it is progress, but it may also stop the move.</summary>
    TargetAndBarrier = 3
}

/// <summary>
/// How likely a barrier is to be penetrated.
/// Requires reaction history and adjacent-build research (v1.2 §33.2.1), neither of
/// which is calibrated, so only NotCalibrated is ever emitted.
/// </summary>
public enum BarrierPermeability
{
    Unknown = 0,
    NotCalibrated = 1,
    // Reserved — calibration required before any of these can be emitted.
    LowFriction = 100,
    ModerateFriction = 101,
    HighFriction = 102
}

/// <summary>
/// Whether target space could be measured at all.
/// Distinguishes "no room" from "cannot tell", which G-LOC-002 depends on.
/// </summary>
public enum TargetSpaceAvailability
{
    /// <summary>No usable reference set — space is unknown, NOT zero.</summary>
    Unavailable = 0,

    /// <summary>References exist and at least one target lies ahead.</summary>
    Available = 1,

    /// <summary>References exist but none lie ahead — genuinely no room.</summary>
    NoTargetAhead = 2
}

public enum PlarDataQuality
{
    Complete = 0,
    Partial = 1,
    Invalid = 2
}
