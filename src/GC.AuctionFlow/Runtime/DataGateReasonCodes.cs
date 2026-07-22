namespace GC.AuctionFlow.Runtime;

/// <summary>Stable DataGate reason codes for P0-08A (deterministic, not localized).</summary>
public static class DataGateReasonCodes
{
    public const string InstrumentMismatch = "INSTRUMENT_MISMATCH";
    public const string InstrumentUnknown = "INSTRUMENT_UNKNOWN";
    public const string TickSizeInvalid = "TICK_SIZE_INVALID";
    public const string TickSizeMismatch = "TICK_SIZE_MISMATCH";
    public const string ContractExpired = "CONTRACT_EXPIRED";
    public const string ProviderModeConflict = "PROVIDER_MODE_CONFLICT";
    public const string IndicatorDisposed = "INDICATOR_DISPOSED";
    public const string IdentityCorruption = "IDENTITY_CORRUPTION";
    public const string ProfileNotReady = "PROFILE_NOT_READY";
    public const string BidAskUnknownOrPartial = "BIDASK_UNKNOWN_OR_PARTIAL";
    public const string RollStateUnknown = "ROLL_STATE_UNKNOWN";
    public const string TradeNotObserved = "TRADE_NOT_OBSERVED";
    public const string SourceProvenanceIncomplete = "SOURCE_PROVENANCE_INCOMPLETE";
}
