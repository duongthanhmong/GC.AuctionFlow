namespace GC.AuctionFlow.Orderflow;

/// <summary>Phase 2A Executed Orderflow raw feature policy. No interpretation thresholds.</summary>
public sealed class ExecutedOrderflowPolicyConfig
{
    public const string PolicyVersion = "EXECUTED_ORDERFLOW_POLICY_V1";

    public const string LimitationHistoryLiveOnly = "ORDERFLOW HISTORY: LIVE_ONLY";
    public const string LimitationMidAuctionCoverage = "ORDERFLOW_COVERAGE_MID_AUCTION";
    public const string LimitationAggressorPartial = "AGGRESSOR_CLASSIFICATION_PARTIAL";
    public const string LimitationAggressorUnavailable = "AGGRESSOR_CLASSIFICATION_UNAVAILABLE";
    public const string LimitationNoImbalance = "IMBALANCE_CLASSIFICATION_NOT_AUTHORIZED";
    public const string LimitationNoStackedImbalance = "STACKED_IMBALANCE_NOT_AUTHORIZED";
    public const string LimitationNoBigTrade = "BIG_TRADE_CLASSIFICATION_NOT_AUTHORIZED";
    public const string LimitationNoTapeSpeed = "TAPE_SPEED_INTERPRETATION_NOT_AUTHORIZED";
    public const string LimitationNoAbsorption = "ABSORPTION_CLASSIFICATION_NOT_AUTHORIZED";
    public const string LimitationNoExhaustion = "EXHAUSTION_CLASSIFICATION_NOT_AUTHORIZED";
    public const string LimitationNoEffortResult = "EFFORT_RESULT_NOT_AUTHORIZED";
    public const string LimitationNoTradeFacilitation = "TRADE_FACILITATION_NOT_AUTHORIZED";
    public const string LimitationCumulativeNotAuthoritative = "CUMULATIVE_CALLBACKS_NOT_AUTHORITATIVE_FOR_EXECUTED_TOTALS";
    public const string LimitationPrimaryAuctionChanged = "PRIMARY_AUCTION_CHANGED";

    public ExecutedOrderflowPolicyConfig(bool enabled = false)
    {
        Enabled = enabled;
    }

    public bool Enabled { get; }
    public string Version => PolicyVersion;
}
