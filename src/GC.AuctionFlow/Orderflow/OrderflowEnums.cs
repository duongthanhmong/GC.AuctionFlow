namespace GC.AuctionFlow.Orderflow;

/// <summary>Phase 2A module status. Raw features only — no interpretation.</summary>
public enum OrderflowModuleState
{
    Disabled = 0,
    AwaitingTrades = 1,
    Ready = 2,
    Partial = 3,
    Invalid = 4
}

public enum AggressorSide
{
    Unknown = 0,
    Ask = 1,
    Bid = 2
}

public enum AggressorClassificationStatus
{
    Unavailable = 0,
    Partial = 1,
    Complete = 2
}

public enum OrderflowCoverageMode
{
    Unknown = 0,
    LiveOnlyMidAuction = 1,
    LiveOnlyFromAuctionStart = 2,
    HistoricalExact = 3
}

public enum OrderflowDataQuality
{
    Complete = 0,
    Partial = 1,
    Invalid = 2
}

public enum OrderflowSourceCallbackKind
{
    OnNewTrade = 0,
    OnNewTradesBatch = 1
}
