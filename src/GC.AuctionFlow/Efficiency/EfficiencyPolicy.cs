namespace GC.AuctionFlow.Efficiency;

/// <summary>Phase 2C Auction Efficiency Raw Evidence — no thresholds or classifiers.</summary>
public sealed class AuctionEfficiencyEvidencePolicyConfig
{
    public const string PolicyVersion = "AUCTION_EFFICIENCY_EVIDENCE_POLICY_V1";

    public const string LimitationHistoryLiveOnly = "LIVE_ONLY_HISTORY";
    public const string LimitationMidAuction = "MID_AUCTION_COVERAGE";
    public const string LimitationAggressorPartial = "AGGRESSOR_CLASSIFICATION_PARTIAL";
    public const string LimitationAskUnavailable = "ASK_CLASSIFICATION_UNAVAILABLE";
    public const string LimitationBidUnavailable = "BID_CLASSIFICATION_UNAVAILABLE";
    public const string LimitationClassificationNotCalibrated = "AUCTION_EFFICIENCY_CLASSIFICATION_NOT_CALIBRATED";
    public const string LimitationNoEffortResult = "EFFORT_RESULT_CLASSIFICATION_NOT_AUTHORIZED";
    public const string LimitationNoTradeFacilitation = "TRADE_FACILITATION_NOT_AUTHORIZED";
    public const string LimitationNoAbsorption = "ABSORPTION_CLASSIFICATION_NOT_AUTHORIZED";
    public const string LimitationNoExhaustion = "EXHAUSTION_CLASSIFICATION_NOT_AUTHORIZED";
    public const string LimitationImbalanceNotCalibrated = "IMBALANCE_CLASSIFICATION_NOT_CALIBRATED";
    public const string LimitationStackedNotCalibrated = "STACKED_IMBALANCE_NOT_CALIBRATED";
    public const string LimitationBigTradeNotCalibrated = "BIG_TRADE_PERCENTILES_NOT_CALIBRATED";
    public const string LimitationTapeSpeedNotCalibrated = "TAPE_SPEED_CLASSIFICATION_NOT_CALIBRATED";
    public const string LimitationMboSweepResearchOnly = "MBO_SWEEP_RESEARCH_ONLY";
    public const string LimitationStopResearchOnly = "STOP_ACTIVITY_RESEARCH_ONLY";
    public const string LimitationIcebergResearchOnly = "ICEBERG_ACTIVITY_RESEARCH_ONLY";
    public const string LimitationClosePositionUnavailable = "CLOSE_POSITION_INPUT_UNAVAILABLE";
    public const string LimitationZeroDenominator = "ZERO_DENOMINATOR";
    public const string LimitationDirectionUnavailable = "RESULT_DIRECTION_UNAVAILABLE";

    public AuctionEfficiencyEvidencePolicyConfig(bool enabled = false)
    {
        Enabled = enabled;
    }

    public bool Enabled { get; }
    public string Version => PolicyVersion;
}
