namespace GC.AuctionFlow.Resolution;

/// <summary>
/// Phase 2D Acceptance/Re-entry Resolution — ACCEPTANCE_REENTRY_RESOLUTION_POLICY_V1.
/// All calibrated conclusions (Established/Failed/StableReacceptance/ReentryFailed/FAR/AAC)
/// are NOT CALIBRATED and must never be emitted in Phase 2D.
/// </summary>
public sealed class AuctionResolutionPolicyConfig
{
    public const string PolicyVersion = "ACCEPTANCE_REENTRY_RESOLUTION_POLICY_V1";

    public const string LimitationNotCalibrated = "ACCEPTANCE_REENTRY_RESOLUTION_NOT_CALIBRATED";
    public const string LimitationEstablishedNotCalibrated = "ACCEPTANCE_ESTABLISHED_THRESHOLD_NOT_CALIBRATED";
    public const string LimitationFailedNotCalibrated = "ACCEPTANCE_FAILED_THRESHOLD_NOT_CALIBRATED";
    public const string LimitationStableReentryNotCalibrated = "STABLE_REENTRY_THRESHOLD_NOT_CALIBRATED";
    public const string LimitationReentryFailedNotCalibrated = "REENTRY_FAILED_THRESHOLD_NOT_CALIBRATED";
    public const string LimitationFarNotCalibrated = "FAR_CONCLUSION_NOT_CALIBRATED";
    public const string LimitationAacNotCalibrated = "AAC_CONCLUSION_NOT_CALIBRATED";
    public const string LimitationLiveOnly = "LIVE_ONLY_HISTORY";
    public const string LimitationNoFarAac = "FAR_AAC_RESOLUTION_NOT_AUTHORIZED";
    public const string LimitationNoThesisEntry = "THESIS_ENTRY_RISK_NOT_AUTHORIZED";
    public const string LimitationNoOverlayAlerts = "OVERLAY_ALERTS_NOT_AUTHORIZED";
    public const string LimitationOutsideCloseRatioUnavailable = "OUTSIDE_CLOSE_RATIO_UNAVAILABLE";
    public const string LimitationTpoOutsideCountUnavailable = "TPO_OUTSIDE_COUNT_UNAVAILABLE";
    public const string LimitationLocalValueRebuildUnavailable = "LOCAL_VALUE_REBUILD_UNAVAILABLE";
    public const string LimitationOppositeAggressionNotAuthorized = "OPPOSITE_AGGRESSION_INTERPRETATION_NOT_AUTHORIZED";

    public AuctionResolutionPolicyConfig(bool enabled = false)
    {
        Enabled = enabled;
    }

    public bool Enabled { get; }
    public string Version => PolicyVersion;
}
