namespace GC.AuctionFlow.Core;

/// <summary>Spec §9.2 capability kinds. Stable wire names via JSON string enums.</summary>
public enum CapabilityKind
{
    Trades = 0,
    BidAsk = 1,
    Footprint = 2,
    Tpo = 3,
    VolumeProfile = 4,
    LiveDom = 5,
    HistoricalDom = 6,
    Mbo = 7,
    OrderIds = 8,
    IcebergNative = 9,
    StopsClassification = 10,
    MboSweeps = 11,
    OpenInterest = 12,
    ReplayFidelity = 13
}

/// <summary>Availability axis — not fidelity, not coverage.</summary>
public enum CapabilityAvailability
{
    Unknown = 0,
    Invalid = 1,
    Unavailable = 2,
    Partial = 3,
    Available = 4
}

/// <summary>Coverage axis as flags. Live/Historical/Replay are distinct bits.</summary>
[Flags]
public enum DataCoverage
{
    None = 0,
    Live = 1,
    Historical = 2,
    Replay = 4
}

/// <summary>Computability axis (e.g. Footprint / TPO / VP).</summary>
public enum ComputabilityState
{
    Unknown = 0,
    NotComputable = 1,
    Computable = 2
}

/// <summary>Fidelity axis. Available ≠ Validated.</summary>
public enum FidelityState
{
    Unknown = 0,
    Partial = 1,
    Validated = 2
}

/// <summary>Sequence evidence axis. NoNativeSequence does not imply Invalid.</summary>
public enum SequenceEvidence
{
    Unknown = 0,
    NoNativeSequence = 1,
    LocalSequenceOnly = 2,
    NativeSequenceAvailable = 3
}

/// <summary>
/// MBO lifecycle axis. Event presence alone never reaches LifecycleValidated.
/// </summary>
public enum MboLifecycleState
{
    Unknown = 0,
    EventPresenceOnly = 1,
    FieldsObserved = 2,
    SemanticsPartiallyMapped = 3,
    LifecycleValidated = 4
}

/// <summary>
/// Distinct trade-stream concepts. Must not be double-counted as one volume source.
/// </summary>
public enum TradeStreamKind
{
    NewTrades = 0,
    CumulativeTrades = 1
}
