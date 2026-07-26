namespace GC.AuctionFlow.Facilitation;

/// <summary>Phase 2F Trade Facilitation module state.</summary>
public enum TradeFacilitationModuleState
{
    Disabled = 0,
    AwaitingEfficiency = 1,
    Ready = 2,
    Partial = 3,
    Invalid = 4
}

/// <summary>
/// Trade Facilitation classification output states (spec §23.4).
/// Calibrated states (100+) never emitted until thresholds explicitly calibrated — always NotCalibrated.
/// </summary>
public enum TradeFacilitationClassificationState
{
    Unknown = 0,

    /// <summary>No calibrated thresholds — always emitted in Phase 2F.</summary>
    NotCalibrated = 1,

    // Reserved — calibration required before any of these can be emitted.
    Healthy = 100,
    Failing = 101
}

public enum TradeFacilitationDataQuality
{
    Complete = 0,
    Partial = 1,
    Invalid = 2
}

/// <summary>
/// Alignment of a facilitation component with the auction direction being attempted
/// (v1.3 §5.2, KDK Ch 31).
///
/// This is a pure sign comparison, so it is observable without calibration:
/// whether the migration is LARGE ENOUGH to matter is the calibrated part.
/// </summary>
public enum FacilitationComponentAlignment
{
    /// <summary>Component measurement not available.</summary>
    Unavailable = 0,

    /// <summary>Direction of the attempt is unknown, so alignment is undefined.</summary>
    Unknown = 1,

    /// <summary>Migration follows the attempted direction.</summary>
    Aligned = 2,

    /// <summary>Migration runs against the attempted direction.</summary>
    Opposed = 3,

    /// <summary>No migration.</summary>
    Flat = 4
}

/// <summary>
/// The four components a facilitation verdict requires (v1.3 §5.2, G-TF-002).
/// Healthy/Failing may not be emitted until all four are present or explicitly
/// marked unavailable.
/// </summary>
public enum FacilitationComponent
{
    /// <summary>Aggression and volume are present.</summary>
    Activity = 0,

    /// <summary>Range expanded / favorable progress achieved.</summary>
    Progress = 1,

    /// <summary>POC / value area migrated with the attempt.</summary>
    Structure = 2,

    /// <summary>Pullbacks held; trade continued in the new area.</summary>
    Maintenance = 3
}
