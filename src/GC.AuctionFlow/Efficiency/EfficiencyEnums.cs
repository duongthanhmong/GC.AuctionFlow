namespace GC.AuctionFlow.Efficiency;

/// <summary>Phase 2C module status. Raw evidence only — no Effort/Result classification.</summary>
public enum EfficiencyModuleState
{
    Disabled = 0,
    AwaitingOrderflow = 1,
    AwaitingEpisode = 2,
    Ready = 3,
    Partial = 4,
    Invalid = 5
}

public enum EfficiencyScopeType
{
    CurrentPrimaryAuction = 0,
    ActiveEpisode = 1,
    ClosedEpisode = 2
}

/// <summary>Descriptive geometry only — not buying/selling success.</summary>
public enum EfficiencyResultDirection
{
    Unknown = 0,
    Up = 1,
    Down = 2,
    Flat = 3
}

public enum EfficiencyClassificationState
{
    NotCalibrated = 0,
    NotApplicable = 1,
    Unavailable = 2
}

public enum EfficiencyDataQuality
{
    Complete = 0,
    Partial = 1,
    Invalid = 2
}

public enum EfficiencyAvailability
{
    Available = 0,
    Partial = 1,
    Unavailable = 2
}
