namespace GC.AuctionFlow.Imbalance;

public enum ImbalanceModuleState
{
    Disabled = 0,
    AwaitingCluster = 1,
    Ready = 2,
    Partial = 3,
    Invalid = 4
}

/// <summary>
/// How the asymmetry was measured (KDK Ch 25).
///
/// Same-price compares Bid and Ask at one price. It is easy to read but does not
/// reflect how market orders lift or hit through the ladder.
///
/// Diagonal compares Ask at one price against Bid one tick lower (or the reverse),
/// which is the structure matching actually produces.
/// </summary>
public enum ImbalanceComparisonMode
{
    SamePrice = 0,
    Diagonal = 1
}

/// <summary>
/// Whether a level qualifies as an imbalance.
///
/// KDK Ch 25 requires BOTH a ratio rule and a minimum-volume rule, and says both must
/// suit the product, the session and the bar type. Neither is calibrated, so no
/// qualification verdict can be emitted.
/// </summary>
public enum ImbalanceQualification
{
    Unknown = 0,

    /// <summary>Aggressor classification unavailable — the ratio cannot be formed.</summary>
    Unavailable = 1,

    /// <summary>Ratio computed, but the qualifying rule is not calibrated.</summary>
    NotCalibrated = 2,

    // Reserved — calibration required before any of these can be emitted.
    AskImbalance = 100,
    BidImbalance = 101,
    NotImbalanced = 102
}

/// <summary>
/// Stacked imbalance (KDK Ch 25): several adjacent levels imbalanced the same way.
/// The run length is observable; how many levels CONSTITUTE a stack is calibrated.
/// </summary>
public enum StackedImbalanceState
{
    Unknown = 0,
    Unavailable = 1,
    NotCalibrated = 2,

    // Reserved — calibration required before any of these can be emitted.
    AskStacked = 100,
    BidStacked = 101,
    NotStacked = 102
}

/// <summary>
/// Where the asymmetry occurred (KDK Ch 25 "Vị trí").
///
/// This is the one genuinely new measurement in this phase. Imbalance mid-value may be
/// nothing more than part of a rotation; imbalance at a boundary inside an episode is
/// more notable, especially alongside acceptance or re-entry.
/// </summary>
public enum ImbalanceLocationContext
{
    /// <summary>Price location unavailable — significance cannot be contextualised.</summary>
    Unavailable = 0,

    /// <summary>Mid-value or at POC: likely part of a rotation.</summary>
    MidValue = 1,

    /// <summary>At a value-area boundary: more notable.</summary>
    ValueBoundary = 2,

    /// <summary>Outside value: where acceptance and failure are decided.</summary>
    OutsideValue = 3
}

public enum ImbalanceDataQuality
{
    Complete = 0,
    Partial = 1,
    Invalid = 2
}
