namespace GC.AuctionFlow.Core;

/// <summary>P0-03 capability schema identity constants.</summary>
public static class CapabilitySchemaVersions
{
    public const string SchemaVersion = "1.0.0";
    public const string ProbeVersionPlaceholder = "0.0.5";
}

/// <summary>Stable limitation / invariant codes (not scores).</summary>
public static class KnownLimitationCodes
{
    public const string NoNativeSequence = "NO_NATIVE_SEQUENCE";
    public const string LocalCaptureNotExchangeCompleteness = "LOCAL_CAPTURE_NOT_EXCHANGE_COMPLETENESS";
    public const string MboEventPresenceNotLifecycle = "MBO_EVENT_PRESENCE_NOT_LIFECYCLE";
    public const string AvailableNotValidated = "AVAILABLE_NOT_VALIDATED";
    public const string UnknownModeNotLive = "UNKNOWN_MODE_NOT_LIVE";
    public const string ModeWithoutProvenanceUnresolved = "MODE_WITHOUT_PROVENANCE_UNRESOLVED";
    public const string DistinctNewAndCumulativeTrades = "DISTINCT_NEW_AND_CUMULATIVE_TRADES";
    public const string IndicatorMenuPresenceNotCapabilityEvidence = "INDICATOR_MENU_PRESENCE_NOT_CAPABILITY_EVIDENCE";
    public const string EsEvidenceNotGcProof = "ES_EVIDENCE_NOT_GC_PROOF";
    public const string GcRuntimeCapabilityUnproven = "GC_RUNTIME_CAPABILITY_UNPROVEN";
}
