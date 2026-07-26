namespace GC.AuctionFlow.EffortResult;

/// <summary>Phase 2E Effort vs Result Classifier policy — no calibrated thresholds in Phase 2E.</summary>
public sealed class EffortResultClassifierPolicyConfig
{
    public const string PolicyVersion = "EFFORT_RESULT_CLASSIFIER_POLICY_V1";

    // All classification outputs NOT_CALIBRATED — thresholds not yet determined.
    public const string LimitationNotCalibrated = "EFFORT_RESULT_CLASSIFIER_NOT_CALIBRATED";
    public const string LimitationBalancedNotCalibrated = "EFFORT_RESULT_BALANCED_THRESHOLD_NOT_CALIBRATED";
    public const string LimitationEffectiveNotCalibrated = "AGGRESSION_EFFECTIVE_THRESHOLD_NOT_CALIBRATED";
    public const string LimitationIneffectiveNotCalibrated = "AGGRESSION_INEFFECTIVE_THRESHOLD_NOT_CALIBRATED";
    public const string LimitationAbsorptionNotCalibrated = "POTENTIAL_PASSIVE_ABSORPTION_NOT_CALIBRATED";
    public const string LimitationExhaustionNotCalibrated = "POTENTIAL_EXHAUSTION_NOT_CALIBRATED";
    public const string LimitationFacilitationHealthyNotCalibrated = "TRADE_FACILITATION_HEALTHY_NOT_CALIBRATED";
    public const string LimitationFacilitationFailingNotCalibrated = "TRADE_FACILITATION_FAILING_NOT_CALIBRATED";
    public const string LimitationLiveOnly = "LIVE_ONLY_HISTORY";
    public const string LimitationNoFarAac = "FAR_AAC_NOT_AUTHORIZED";
    public const string LimitationNoThesisEntry = "THESIS_ENTRY_NOT_AUTHORIZED";
    public const string LimitationNoOverlayAlerts = "OVERLAY_ALERTS_NOT_AUTHORIZED";

    public EffortResultClassifierPolicyConfig(bool enabled = false)
    {
        Enabled = enabled;
    }

    public bool Enabled { get; }
    public string Version => PolicyVersion;
}
