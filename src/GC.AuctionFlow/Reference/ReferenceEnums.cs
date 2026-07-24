namespace GC.AuctionFlow.Reference;

/// <summary>Phase 1C profile-derived reference types only. Reserved names may exist for later phases.</summary>
public enum ReferenceType
{
    PreviousPrimaryAuctionHigh = 0,
    PreviousPrimaryAuctionLow = 1,
    PreviousPrimaryTpoPoc = 2,
    PreviousPrimaryTpoVah = 3,
    PreviousPrimaryTpoVal = 4,
    PreviousPrimaryVpoc = 5,
    PreviousPrimaryVolumeVah = 6,
    PreviousPrimaryVolumeVal = 7,

    CurrentPrimaryAuctionHigh = 8,
    CurrentPrimaryAuctionLow = 9,
    CurrentPrimaryTpoPoc = 10,
    CurrentPrimaryTpoVah = 11,
    CurrentPrimaryTpoVal = 12,
    CurrentPrimaryVpoc = 13,
    CurrentPrimaryVolumeVah = 14,
    CurrentPrimaryVolumeVal = 15,

    CompositeTpoPoc = 16,
    CompositeTpoVah = 17,
    CompositeTpoVal = 18,
    CompositeVpoc = 19,
    CompositeVolumeVah = 20,
    CompositeVolumeVal = 21,
    CompositeRangeHigh = 22,
    CompositeRangeLow = 23
}

public enum ReferenceMaturity
{
    Confirmed = 0,
    Developing = 1
}

public enum ReferenceSourceKind
{
    PreviousPrimaryAuction = 0,
    CurrentPrimaryAuction = 1,
    ConfirmedComposite = 2
}

public enum ReferenceSourceHorizon
{
    PreviousPrimaryAuction = 0,
    CurrentPrimaryAuction = 1,
    ConfirmedComposite = 2
}

/// <summary>
/// Full lifecycle enum per master spec. Phase 1C runtime emits only Fresh/Active/Expired/Retired.
/// Interaction/acceptance members are reserved — never emitted by Phase 1C.
/// </summary>
public enum ReferenceStatus
{
    Fresh = 0,
    Active = 1,
    Approaching = 2,
    Interacting = 3,
    OutsideAttemptActive = 4,
    AcceptedThrough = 5,
    Reaccepted = 6,
    Rotational = 7,
    Exhausted = 8,
    Expired = 9,
    Retired = 10
}

public enum StructuralReferenceModuleState
{
    Disabled = 0,
    AwaitingPrimary = 1,
    Ready = 2,
    Partial = 3,
    Invalid = 4
}

/// <summary>Evidence provenance for profile-derived references (descriptive only).</summary>
public enum ReferenceEvidenceTier
{
    Unknown = 0,
    ProfileDerived = 1,
    NotEvaluated = 2
}
