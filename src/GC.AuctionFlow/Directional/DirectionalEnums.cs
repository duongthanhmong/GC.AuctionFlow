namespace GC.AuctionFlow.Directional;

/// <summary>Production directional auction state. Descriptive only — no Long/Short/Buy/Sell.</summary>
public enum DirectionalAuctionState
{
    UpDiscovery = 0,
    DownDiscovery = 1,
    UpRotation = 2,
    DownRotation = 3,
    Balance = 4,
    Transition = 5,
    Conflicted = 6,
    Unknown = 7
}

public enum MigrationDirection
{
    Higher = 0,
    Lower = 1,
    Unchanged = 2,
    Mixed = 3,
    Unavailable = 4
}

public enum ValueRelationship
{
    FullyAbove = 0,
    FullyBelow = 1,
    OverlappingHigher = 2,
    OverlappingLower = 3,
    Inside = 4,
    Outside = 5,
    Equal = 6,
    Overlapping = 7,
    Unavailable = 8
}

public enum PriceValueLocation
{
    AboveValue = 0,
    AtValueHigh = 1,
    InsideValue = 2,
    AtPoc = 3,
    AtValueLow = 4,
    BelowValue = 5,
    Unavailable = 6
}

public enum OneTimeFramingState
{
    Unknown = 0,
    DevelopingUp = 1,
    ConfirmedUp = 2,
    DevelopingDown = 3,
    ConfirmedDown = 4,
    Broken = 5,
    Mixed = 6
}

public enum DirectionalHorizon
{
    StructuralMultiDay = 0,
    TacticalCurrentPrimary = 1,
    IntradayPersistence = 2,
    Execution = 3
}

public enum DirectionalMaturity
{
    Confirmed = 0,
    Developing = 1,
    Unavailable = 2
}

public enum DirectionalModuleState
{
    Disabled = 0,
    AwaitingProfile = 1,
    Ready = 2,
    Partial = 3,
    Invalid = 4
}

public enum ExecutionContextAvailability
{
    NotAvailable = 0,
    Unknown = 1
}
