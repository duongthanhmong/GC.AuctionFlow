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
