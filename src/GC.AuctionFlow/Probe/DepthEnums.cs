namespace GC.AuctionFlow.Probe;

public enum DepthCallbackSource
{
    MarketDepthChanged = 0,
    MarketDepthsBatch = 1,
    BestBidAskChanged = 2,
    SnapshotPull = 3
}

public enum DepthSide
{
    Unknown = 0,
    Bid = 1,
    Ask = 2
}

/// <summary>Indicator MarketDataArg has no action field — Unknown only in P0-05.</summary>
public enum DepthUpdateAction
{
    Unknown = 0
}

public enum DepthLifecycleMarker
{
    Attached = 0,
    Removed = 1,
    ReAdded = 2,
    OperatorDeclaredReconnect = 3
}

/// <summary>ObservedNative only if a genuine ATAS/feed reset marker is observed (none in P0-05 API).</summary>
public enum DepthResetEvidence
{
    NotObserved = 0,
    Suspected = 1,
    ObservedNative = 2
}

public enum DeclaredFeedProvider
{
    Unknown = 0,
    Rithmic = 1,
    Other = 2
}

public enum FeedProviderProvenance
{
    Unknown = 0,
    OperatorDeclared = 1,
    ObservedApi = 2
}

public static class DomSemanticsProbeVersions
{
    public const string ProbeVersion = "0.0.5";
    public const string DomSemanticsProbeSchemaVersion = "1.0.0";
    public const string ContinuityDisclaimer =
        "Internal capture continuity does not establish exchange-feed completeness.";
    public const string VolumeMeaning = "Unknown";
    public const string ZeroVolumeMeaning = "Unknown";
}
