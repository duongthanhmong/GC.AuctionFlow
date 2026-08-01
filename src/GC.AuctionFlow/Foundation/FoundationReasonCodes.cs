namespace GC.AuctionFlow.Foundation;

/// <summary>
/// Deterministic machine-readable reason codes for the M1 input foundation (TTS §7.1, §5.7).
/// Free text may explain but never replaces a code. These are additive to the existing
/// <c>DataGateReasonCodes</c>; they cover input-integrity and lifecycle causes only.
/// </summary>
public static class FoundationReasonCodes
{
    // ---- contract / tick / version (hard blocks) ----
    public const string ContractIdentityMissing = "FND_CONTRACT_IDENTITY_MISSING";
    public const string TickSizeInvalid = "FND_TICK_SIZE_INVALID";
    public const string VersionMissing = "FND_VERSION_MISSING";
    public const string DataSchemaVersionMissing = "FND_DATA_SCHEMA_VERSION_MISSING";

    // ---- session template (session-dependent block) ----
    public const string SessionTemplateUnknown = "FND_SESSION_TEMPLATE_UNKNOWN";
    public const string SessionTemplateReviewRequired = "FND_SESSION_TEMPLATE_REVIEW_REQUIRED";
    public const string SessionTemplateNotApprovedForProduction = "FND_SESSION_TEMPLATE_NOT_APPROVED_FOR_PRODUCTION";

    // ---- input integrity ----
    public const string DuplicateRejected = "FND_DUPLICATE_REJECTED";
    public const string OutOfOrderObserved = "FND_OUT_OF_ORDER_OBSERVED";
    public const string LateEventRevision = "FND_LATE_EVENT_REVISION";
    public const string DedupNotPossible = "FND_DEDUP_NOT_POSSIBLE";
    public const string EventTimeProvenanceDegraded = "FND_EVENT_TIME_PROVENANCE_DEGRADED";

    // ---- capability distinction (never assume historical==live) ----
    public const string HistoricalTradeCapabilityUnproven = "FND_HISTORICAL_TRADE_CAPABILITY_UNPROVEN";
    public const string HistoricalCandleNotTradeParity = "FND_HISTORICAL_CANDLE_NOT_TRADE_PARITY";

    // ---- lifecycle / warm-up / recovery ----
    public const string WarmupIncomplete = "FND_WARMUP_INCOMPLETE";
    public const string Rebuilding = "FND_REBUILDING";
    public const string RecoveringAfterReconnect = "FND_RECOVERING_AFTER_RECONNECT";
    public const string UnrecoveredGapBlocksReady = "FND_UNRECOVERED_GAP_BLOCKS_READY";
    public const string RequiredModuleRebuilding = "FND_REQUIRED_MODULE_REBUILDING";
    public const string CaptureEpochChanged = "FND_CAPTURE_EPOCH_CHANGED";
    public const string StaleReadinessInvalidatedByIdentity = "FND_STALE_READINESS_INVALIDATED_BY_IDENTITY";
    public const string RecorderSegmentCorruptFailedClosed = "FND_RECORDER_SEGMENT_CORRUPT_FAILED_CLOSED";
    public const string IndicatorStopped = "FND_INDICATOR_STOPPED";

    // ---- config governance ----
    public const string UnapprovedParameterCannotUnlockReady = "FND_UNAPPROVED_PARAMETER_CANNOT_UNLOCK_READY";
}
