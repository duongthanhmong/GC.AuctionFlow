namespace GC.AuctionFlow.Cluster;

/// <summary>Phase 2B module status. Raw cluster measurements only — no classification.</summary>
public enum ClusterRawModuleState
{
    Disabled = 0,
    AwaitingOrderflow = 1,
    Ready = 2,
    Partial = 3,
    Invalid = 4
}

/// <summary>Descriptive side comparison — not an imbalance classification.</summary>
public enum ClusterRawDominantSide
{
    Unknown = 0,
    Ask = 1,
    Bid = 2,
    Equal = 3
}

/// <summary>Classification policy is absent in Phase 2B.</summary>
public enum ClusterClassificationState
{
    NotCalibrated = 0,
    NotApplicable = 1,
    Unavailable = 2
}

public enum ClusterRawDataQuality
{
    Complete = 0,
    Partial = 1,
    Invalid = 2
}

public enum ClusterRawAvailability
{
    Available = 0,
    Partial = 1,
    Unavailable = 2
}
