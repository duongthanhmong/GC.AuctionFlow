namespace GC.AuctionFlow.Core;

/// <summary>Declared / observed feed mode. Unknown must remain explicit.</summary>
public enum DataSourceMode
{
    Unknown = 0,
    Live = 1,
    HistoricalLoad = 2,
    Replay = 3,
    Simulated = 4
}

/// <summary>Provenance for <see cref="DataSourceMode"/>.</summary>
public enum DataSourceModeProvenance
{
    Unknown = 0,
    OperatorDeclared = 1,
    ObservedApi = 2,
    Inferred = 3
}

/// <summary>Provenance for capability observations and evidence facts.</summary>
public enum EvidenceProvenance
{
    Unknown = 0,
    OperatorDeclared = 1,
    ObservedApi = 2,
    Inferred = 3
}
