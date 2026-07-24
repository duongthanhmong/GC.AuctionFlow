namespace GC.AuctionFlow.Evidence;

/// <summary>Phase 1F Acceptance/Re-entry Evidence Measurement policy. No resolution thresholds.</summary>
public sealed class AcceptanceReentryEvidencePolicyConfig
{
    public const string PolicyVersion = "ACCEPTANCE_REENTRY_EVIDENCE_POLICY_V1";

    public const string LimitationHistoryLiveOnly = "EVIDENCE HISTORY: LIVE_ONLY";
    public const string LimitationCenterlineNotApplicable = "CENTERLINE_ACCEPTANCE_GEOMETRY_NOT_APPLICABLE";
    public const string LimitationOutsideCloseUnavailable = "OUTSIDE_CLOSE_FEED_UNAVAILABLE";
    public const string LimitationTpoOutsideUnavailable = "TPO_OUTSIDE_WINDOW_UNAVAILABLE";
    public const string LimitationLocalValueNotAuthorized = "LOCAL_VALUE_POLICY_NOT_AUTHORIZED";
    public const string LimitationOldValueReclaimNotCalibrated = "OLD_VALUE_RECLAIM_THRESHOLD_NOT_CALIBRATED";
    public const string LimitationRetestHoldNotCalibrated = "RETEST_HOLD_POLICY_NOT_CALIBRATED";
    public const string LimitationOppositeAggressionNotAuthorized = "OPPOSITE_AGGRESSION_INTERPRETATION_NOT_AUTHORIZED";
    public const string LimitationLocalValueRebuildNotAuthorized = "LOCAL_VALUE_REBUILD_POLICY_NOT_AUTHORIZED";
    public const string LimitationOldDirectionRequiresOrderflow = "OLD_DIRECTION_EFFECTIVENESS_REQUIRES_ORDERFLOW_PHASE";
    public const string LimitationStableReacceptanceNotCalibrated = "STABLE_REACCEPTANCE_THRESHOLD_NOT_CALIBRATED";
    public const string LimitationPrimaryAuctionChanged = "PRIMARY_AUCTION_CHANGED";

    public AcceptanceReentryEvidencePolicyConfig(bool enabled = false)
    {
        Enabled = enabled;
    }

    public bool Enabled { get; }
    public string Version => PolicyVersion;
}
