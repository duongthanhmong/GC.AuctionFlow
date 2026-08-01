namespace GC.AuctionFlow.Foundation;

/// <summary>
/// TTS §5.7 / §4.5 capability + data-quality snapshot for the M1 input foundation. Immutable; every
/// published runtime analysis references one. It distinguishes historical from live capability, exposes
/// exactly what integrity is measured, and carries the gate verdict. It adds no market classification.
/// </summary>
public sealed record FoundationCapabilitySnapshot(
    string CapabilitySnapshotId,
    string SourceId,
    string ContractId,
    bool ContractIdentityAvailable,
    decimal TickSize,
    bool TickSizeValid,
    string SessionTemplateId,
    string SessionTemplateVersion,
    SessionTemplateStatus SessionTemplateStatus,
    CapabilityAvailability HistoricalTradeCapability,
    CapabilityAvailability LiveTradeCapability,
    DedupCapability DedupCapability,
    long ClassifiedCount,
    long UnknownAggressorCount,
    long DuplicateCount,
    long OutOfOrderCount,
    long LateEventCount,
    long NonDeduplicableCount,
    long MeasuredIdentityGapCount,
    bool HasUnrecoveredGap,
    DateTime? LastValidEventTimeUtc,
    LifecycleState LifecycleState,
    ModuleReadiness RequiredModuleReadiness,
    ModuleReadiness OptionalModuleReadiness,
    DataState DataState,
    VersionStamp Versions,
    IReadOnlyList<string> ReasonCodes)
{
    /// <summary>
    /// Acquisition-gate DataState (SC-007 retained: Ready|Degraded|Invalid). A five-state operator
    /// view, if ever needed, is an explicit projection of this — never a silent replacement.
    /// </summary>
    public static DataState ToDataState(LifecycleState s) => s switch
    {
        LifecycleState.AnalysisReady => DataState.Ready,
        LifecycleState.Degraded or LifecycleState.Recovering or
        LifecycleState.HistoricalWarmup or LifecycleState.Rebuilding => DataState.Degraded,
        _ => DataState.Invalid, // ColdStart, Invalid, Stopped
    };
}

/// <summary>Local mirror of the acquisition DataState (SC-007) so Foundation carries no Core dependency
/// direction inversion. Values match <c>GC.AuctionFlow.Core.DataState</c> exactly.</summary>
public enum DataState
{
    Invalid = 0,
    Degraded = 1,
    Ready = 2,
}
