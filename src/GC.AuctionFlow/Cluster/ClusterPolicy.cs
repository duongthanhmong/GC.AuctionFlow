namespace GC.AuctionFlow.Cluster;

/// <summary>Phase 2B Cluster Raw Feature Measurement — no thresholds.</summary>
public sealed class ClusterRawFeaturePolicyConfig
{
    public const string PolicyVersion = "CLUSTER_RAW_FEATURE_POLICY_V1";
    public const string RankMethod = "EMPIRICAL_MIDRANK_V1";

    public const string LimitationHistoryLiveOnly = "LIVE_ONLY_HISTORY";
    public const string LimitationMidAuction = "MID_AUCTION_COVERAGE";
    public const string LimitationAskUnavailable = "ASK_CLASSIFICATION_UNAVAILABLE";
    public const string LimitationBidUnavailable = "BID_CLASSIFICATION_UNAVAILABLE";
    public const string LimitationOpposingNotTraded = "OPPOSING_LEVEL_NOT_TRADED";
    public const string LimitationOpposingDenomZero = "OPPOSING_DENOMINATOR_ZERO";
    public const string LimitationAggressorPartial = "AGGRESSOR_CLASSIFICATION_PARTIAL";
    public const string LimitationClosePositionUnavailable = "CLOSE_POSITION_INPUT_UNAVAILABLE";
    public const string LimitationNoImbalance = "IMBALANCE_CLASSIFICATION_NOT_AUTHORIZED";
    public const string LimitationNoStackedImbalance = "STACKED_IMBALANCE_NOT_AUTHORIZED";
    public const string LimitationNoExtreme = "EXTREME_CLASSIFICATION_NOT_AUTHORIZED";
    public const string LimitationNoBigTrade = "BIG_TRADE_CLASSIFICATION_NOT_AUTHORIZED";
    public const string LimitationNoAbsorption = "ABSORPTION_CLASSIFICATION_NOT_AUTHORIZED";
    public const string LimitationNoExhaustion = "EXHAUSTION_CLASSIFICATION_NOT_AUTHORIZED";
    public const string LimitationNoEffortResult = "EFFORT_RESULT_NOT_AUTHORIZED";
    public const string LimitationNoTradeFacilitation = "TRADE_FACILITATION_NOT_AUTHORIZED";
    public const string LimitationClassificationNotCalibrated = "CLUSTER_CLASSIFICATION_NOT_CALIBRATED";

    public ClusterRawFeaturePolicyConfig(bool enabled = false)
    {
        Enabled = enabled;
    }

    public bool Enabled { get; }
    public string Version => PolicyVersion;
}
