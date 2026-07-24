namespace GC.AuctionFlow.Runtime;

/// <summary>Observed vs expected instrument comparison for ContractSnapshot.</summary>
public enum InstrumentMatchState
{
    Unknown = 0,
    Match = 1,
    Mismatch = 2
}

/// <summary>Contract calendar expiration classification (evidence-gated).</summary>
public enum ExpirationState
{
    Unknown = 0,
    Valid = 1,
    NearExpiration = 2,
    Expired = 3
}

/// <summary>
/// Roll classification. ActiveRoll requires evidence this slice does not invent —
/// unavailable evidence remains Unknown.
/// </summary>
public enum RollState
{
    Unknown = 0,
    NormalContract = 1,
    EarlyRoll = 2,
    ActiveRoll = 3,
    PostRoll = 4
}

/// <summary>Conservative runtime capability states (distinct from research claims).</summary>
public enum RuntimeCapabilityState
{
    Unknown = 0,
    Unavailable = 1,
    Partial = 2,
    Available = 3,
    Ready = 4,
    Invalid = 5,
    Blocked = 6,
    NotReady = 7,
    NotConfigured = 8,
    Recording = 9,
    Faulted = 10,
    Off = 11,
    TpoReady = 12,
    VolumeReady = 13
}

public enum ParticipationRegimePlaceholderState
{
    NotAvailable = 0
}

public enum ProfilePlaceholderState
{
    NotReady = 0,
    Partial = 1,
    Ready = 2,
    Invalid = 3
}

/// <summary>
/// Coarse Structural Reference module gate mirrored onto the runtime snapshot.
/// Detailed objects live on <see cref="StructuralReferenceSetSnapshot"/>.
/// </summary>
public enum ReferencePlaceholderState
{
    NotAvailable = 0,
    Disabled = 1,
    AwaitingPrimary = 2,
    Ready = 3,
    Partial = 4,
    Invalid = 5
}
