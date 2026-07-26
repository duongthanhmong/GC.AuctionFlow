namespace GC.AuctionFlow.Thesis;

public enum ThesisContractModuleState
{
    Disabled = 0,
    AwaitingMaturity = 1,
    Ready = 2,
    Partial = 3,
    Invalid = 4
}

/// <summary>Thesis family. FAR and AAC are the only authorized families.</summary>
public enum ThesisFamily
{
    Unknown = 0,
    Far = 1,
    Aac = 2
}

/// <summary>
/// Auction horizon a thesis component is anchored to (v1.2 §11.1).
/// Unavailable is the honest default until the multi-horizon map is authorized.
/// </summary>
public enum ThesisHorizonSource
{
    Unavailable = 0,
    MicroExecutionEpisode = 1,
    LocalBalance = 2,
    IntradayAuction = 3,
    DailyAuction = 4,
    MultiDayComposite = 5,
    WeeklyOrMultiWeek = 6,
    MonthlyOrMultiMonth = 7
}

/// <summary>
/// Which of the five thesis horizons a declaration refers to (v1.2 §11.2).
/// All five must be declared for a contract to be complete.
/// </summary>
public enum ThesisHorizonRole
{
    Context = 0,
    Thesis = 1,
    Trigger = 2,
    Management = 3,
    Target = 4
}

/// <summary>
/// Source-of-move provenance (v1.2 §11.3).
/// A micro trigger must never promote a thesis to multi-session.
/// </summary>
public enum SourceOfMove
{
    Unknown = 0,
    LocalBalanceBreakout = 1,
    DailyCompositeEdge = 2,
    StructuralLevel = 3,
    EventImpulse = 4,
    ShortLivedLiquidationImpulse = 5
}

/// <summary>
/// Invalidation dimensions. v1.2 §32.2 defines four; v1.3 §11.3 adds Evidence
/// as an independent fifth dimension because it fires earlier than Auction
/// invalidation and does not require acceptance to have formed.
/// </summary>
public enum InvalidationDimension
{
    Price = 0,
    Auction = 1,
    Time = 2,
    Context = 3,
    Evidence = 4
}

/// <summary>
/// Per-dimension invalidation state.
/// Triggered requires calibrated thresholds and is therefore reserved.
/// </summary>
public enum InvalidationDimensionState
{
    Unknown = 0,
    NotCalibrated = 1,
    Monitoring = 2,
    // Reserved — calibration required before any of these can be emitted.
    Triggered = 100,
    Cleared = 101
}

/// <summary>
/// Structured missing-evidence declaration (v1.3 G-THE-001).
/// A thesis that cannot state what it lacks is not a valid thesis.
/// Free-text is deliberately not supported.
/// </summary>
public enum MissingEvidenceKind
{
    None = 0,
    AcceptanceResolutionNotCalibrated = 1,
    ReentryResolutionNotCalibrated = 2,
    OldValueReclaimNotObserved = 3,
    TradeFacilitationNotCalibrated = 4,
    EffortResultNotCalibrated = 5,
    SignalMaturityNotCalibrated = 6,
    AggressorClassificationUnavailable = 7,
    TargetSpaceUnavailable = 8,
    PriceLocationUnavailable = 9,
    HorizonMapUnavailable = 10,
    MboBlocked = 11
}

/// <summary>Overall contract completeness. Executable is reserved.</summary>
public enum ThesisContractState
{
    Unknown = 0,
    Incomplete = 1,
    NotCalibrated = 2,
    // Reserved — calibration required before any of these can be emitted.
    Complete = 100,
    Executable = 101,
    // Terminal:
    Invalidated = 200,
    Expired = 201
}

public enum ThesisContractDataQuality
{
    Complete = 0,
    Partial = 1,
    Invalid = 2
}
