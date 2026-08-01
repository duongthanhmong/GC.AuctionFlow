namespace GC.AuctionFlow.Foundation;

/// <summary>
/// M1 deterministic input foundation. These types translate Technical Translation Spec §5, §9-12
/// and MRBS §§1-3, 18, 24-26, 39-40, 44 into the smallest coherent runtime contract that gates
/// readiness truthfully. They add NO market classification; they only decide whether the input is
/// good enough for the (default-off) analysis modules to be trusted, and expose exactly what is
/// measured versus unobservable. Nothing here calibrates a parameter or enables an analysis module.
/// </summary>
internal static class FoundationDocMarker { }

/// <summary>TTS §10.1 lifecycle. The only states that may host a confirmed downstream publish is
/// <see cref="AnalysisReady"/>; every other state blocks confirmation.</summary>
public enum LifecycleState
{
    ColdStart = 0,
    HistoricalWarmup = 1,
    Rebuilding = 2,
    AnalysisReady = 3,
    Degraded = 4,
    Recovering = 5,
    Invalid = 6,
    Stopped = 7,
}

/// <summary>TTS §5.2 DataPhase. Historical and Live capabilities are never assumed equal.</summary>
public enum DataPhase
{
    Historical = 0,
    Replay = 1,
    Live = 2,
}

/// <summary>TTS §5.2 MarketEventKind.</summary>
public enum InputEventKind
{
    Trade = 0,
    Quote = 1,
    Depth = 2,
    SessionMarker = 3,
    Correction = 4,
}

/// <summary>
/// How an event may be deduplicated. Drives whether overlap can be resolved safely (TTS §4.2/§5.4).
/// <see cref="NotDeduplicable"/> means unsafe overlap must NOT promote the engine to AnalysisReady.
/// </summary>
public enum DedupCapability
{
    /// <summary>A verified-stable native/exchange identity for this event type.</summary>
    NativeStableId = 0,
    /// <summary>A documented deterministic composite key; may collide on legitimately identical prints.</summary>
    CompositeKey = 1,
    /// <summary>No safe identity; overlap cannot be resolved without risking double count.</summary>
    NotDeduplicable = 2,
}

/// <summary>
/// Provenance of <see cref="CanonicalInputEvent.EventTimeUtc"/> — whether the source time can safely
/// be declared UTC. The ATAS timestamp is not blindly declared UTC (TTS §5.3).
/// </summary>
public enum SourceTimeKind
{
    /// <summary>Source declared the timestamp as UTC and it is trusted as market/event time.</summary>
    DeclaredUtc = 0,
    /// <summary>Only the adapter receive time is trustworthy; event time is a best-effort copy.</summary>
    AdapterReceiveOnly = 1,
    /// <summary>Time provenance is unknown; event-time ordering is degraded.</summary>
    Unknown = 2,
}

/// <summary>
/// Session-template approval (TTS §9.1 + audit correction 3.1). An unapproved/unknown template blocks
/// session-dependent outputs. Nothing here selects venue hours — that is CONF-001, an owner decision.
/// </summary>
public enum SessionTemplateStatus
{
    UnknownTemplate = 0,
    ReviewRequired = 1,
    Proposed = 2,
    UnderReview = 3,
    ApprovedForResearch = 4,
    ApprovedForProduction = 5,
    Rejected = 6,
    Replaced = 7,
    Deferred = 8,
}

/// <summary>Parameter provenance (TTS §9.1). OBSERVED_METADATA is authoritative instrument data.</summary>
public enum ParameterSourceType
{
    KDK_CONVENTION = 0,
    PROPOSED_SEED = 1,
    DATA_DERIVED = 2,
    OBSERVED_METADATA = 3,
}

/// <summary>Parameter approval (TTS §9.1). Only ApprovedForProduction may back a production decision.</summary>
public enum ParameterApprovalStatus
{
    Proposed = 0,
    UnderReview = 1,
    ApprovedForResearch = 2,
    ApprovedForProduction = 3,
    Rejected = 4,
    Replaced = 5,
    Deferred = 6,
}

/// <summary>Per-capability availability, published separately for historical and live (TTS §5.6, DQ-004).</summary>
public enum CapabilityAvailability
{
    Unknown = 0,
    Unavailable = 1,
    Unproven = 2,
    Available = 3,
}

/// <summary>Readiness of a required/optional module for the current cutoff.</summary>
public enum ModuleReadiness
{
    NotReady = 0,
    Rebuilding = 1,
    Ready = 2,
    Unavailable = 3,
}
