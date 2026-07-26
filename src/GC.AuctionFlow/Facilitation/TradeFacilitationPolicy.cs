namespace GC.AuctionFlow.Facilitation;

/// <summary>Phase 2F Trade Facilitation — policy config and limitation constants.</summary>
public sealed class TradeFacilitationPolicyConfig
{
    public const string PolicyVersion = "TRADE_FACILITATION_POLICY_V1";

    public const string LimitationNotCalibrated = "TRADE_FACILITATION_INDEX_THRESHOLDS_NOT_CALIBRATED";
    public const string LimitationHealthyNotCalibrated = "TRADE_FACILITATION_HEALTHY_THRESHOLD_NOT_CALIBRATED";
    public const string LimitationFailingNotCalibrated = "TRADE_FACILITATION_FAILING_THRESHOLD_NOT_CALIBRATED";
    public const string LimitationDirectionUnavailable = "TRADE_FACILITATION_DIRECTION_UNAVAILABLE";
    public const string LimitationEffortUnavailable = "TRADE_FACILITATION_DIRECTION_CONSISTENT_EFFORT_UNAVAILABLE";
    public const string LimitationProgressUnavailable = "TRADE_FACILITATION_FAVORABLE_PROGRESS_UNAVAILABLE";
    public const string LimitationComponentsIncomplete = "TRADE_FACILITATION_COMPONENTS_INCOMPLETE";
    public const string LimitationStructureUnavailable = "TRADE_FACILITATION_STRUCTURE_UNAVAILABLE";
    public const string LimitationMaintenanceUnavailable = "TRADE_FACILITATION_MAINTENANCE_UNAVAILABLE";
    public const string LimitationLiveOnly = "LIVE_ONLY_HISTORY";
    public const string LimitationNoFarAac = "TRADE_FACILITATION_NO_FAR_AAC_SIGNALS";

    /// <summary>
    /// v1.3 §5.2 G-TF-002: Activity, Progress, Structure and Maintenance must all be
    /// accounted for before Healthy/Failing can ever be emitted.
    /// </summary>
    public const int RequiredComponents = 4;
    public TradeFacilitationPolicyConfig(bool enabled = false)
    {
        Enabled = enabled;
    }

    public bool Enabled { get; }
    public string Version => PolicyVersion;
}
