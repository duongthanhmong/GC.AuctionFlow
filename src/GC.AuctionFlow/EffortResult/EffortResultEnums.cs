namespace GC.AuctionFlow.EffortResult;

/// <summary>Phase 2E Effort vs Result Classifier module state.</summary>
public enum EffortResultModuleState
{
    Disabled = 0,
    AwaitingEfficiency = 1,
    Ready = 2,
    Partial = 3,
    Invalid = 4
}

/// <summary>
/// Effort vs Result classification output states (spec Chapter 23.3).
/// All calibrated states (100+) emit NotCalibrated until thresholds explicitly calibrated.
/// </summary>
public enum EffortResultClassificationState
{
    Unknown = 0,

    /// <summary>Phase 2E: no calibrated thresholds → always emitted as NotCalibrated.</summary>
    NotCalibrated = 4,

    // Reserved — calibration required before any of these can be emitted.
    EffortResultBalanced = 100,
    AggressionEffective = 101,
    AggressionIneffective = 102,
    PotentialPassiveAbsorption = 103,
    PotentialExhaustion = 104,
    TradeFacilitationHealthy = 105,
    TradeFacilitationFailing = 106
}

public enum EffortResultDataQuality
{
    Complete = 0,
    Partial = 1,
    Invalid = 2
}
