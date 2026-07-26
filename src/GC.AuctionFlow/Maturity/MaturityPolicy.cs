namespace GC.AuctionFlow.Maturity;

/// <summary>
/// Phase 3B Signal Maturity — policy config and limitation constants.
/// v1.2 §29, v1.3 §9. No thresholds are defined here by design (v1.3 G-CAL-001).
/// </summary>
public sealed class SignalMaturityPolicyConfig
{
    public const string PolicyVersion = "SIGNAL_MATURITY_POLICY_V1";

    public const string LimitationNotCalibrated = "SIGNAL_MATURITY_THRESHOLDS_NOT_CALIBRATED";
    public const string LimitationFastNotCalibrated = "SIGNAL_MATURITY_FAST_NOT_CALIBRATED";
    public const string LimitationStandardNotCalibrated = "SIGNAL_MATURITY_STANDARD_NOT_CALIBRATED";
    public const string LimitationConfirmedNotCalibrated = "SIGNAL_MATURITY_CONFIRMED_NOT_CALIBRATED";
    public const string LimitationFastShadowOnly = "FAST_MATURITY_SHADOW_ONLY";
    public const string LimitationRetestNotCalibrated = "RETEST_DISCRIMINATION_NOT_CALIBRATED";
    public const string LimitationDeadlineNotCalibrated = "EXPECTED_BEHAVIOR_DEADLINE_NOT_CALIBRATED";
    public const string LimitationMicroConfirmationNotCalibrated = "MICRO_CONFIRMATION_NOT_CALIBRATED";
    public const string LimitationTargetSpaceUnavailable = "TARGET_SPACE_UNAVAILABLE";
    public const string LimitationLiveOnly = "LIVE_ONLY_HISTORY";
    public const string LimitationNoEntryPlan = "SIGNAL_MATURITY_NO_ENTRY_PLAN";
    public const string LimitationNoRiskSizing = "SIGNAL_MATURITY_NO_RISK_SIZING";

    /// <summary>
    /// FAST deployment guardrail (v1.2 §29.5, v1.3 §9.4).
    /// FAST is shadow-only by default and may not produce action alerts,
    /// executable plans or risk sizing until the evidence gate is passed.
    /// </summary>
    public const bool FastShadowOnlyDefault = true;

    public SignalMaturityPolicyConfig(bool enabled = false)
    {
        Enabled = enabled;
    }

    public bool Enabled { get; }
    public string Version => PolicyVersion;
    public bool FastShadowOnly => FastShadowOnlyDefault;
}
