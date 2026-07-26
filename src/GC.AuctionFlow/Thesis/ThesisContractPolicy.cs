namespace GC.AuctionFlow.Thesis;

/// <summary>
/// Phase 3C Thesis Contract + multi-dimensional invalidation.
/// v1.2 §32 + §11.1-11.3, v1.3 §11. No thresholds by design (v1.3 G-CAL-001).
/// </summary>
public sealed class ThesisContractPolicyConfig
{
    public const string PolicyVersion = "THESIS_CONTRACT_POLICY_V1";

    public const string LimitationNotCalibrated = "THESIS_CONTRACT_THRESHOLDS_NOT_CALIBRATED";
    public const string LimitationCompleteNotCalibrated = "THESIS_CONTRACT_COMPLETE_NOT_CALIBRATED";
    public const string LimitationExecutableNotAuthorized = "THESIS_CONTRACT_EXECUTABLE_NOT_AUTHORIZED";

    public const string LimitationPriceInvalidationNotCalibrated = "PRICE_INVALIDATION_NOT_CALIBRATED";
    public const string LimitationAuctionInvalidationNotCalibrated = "AUCTION_INVALIDATION_NOT_CALIBRATED";
    public const string LimitationTimeInvalidationNotCalibrated = "TIME_INVALIDATION_NOT_CALIBRATED";
    public const string LimitationContextInvalidationNotCalibrated = "CONTEXT_INVALIDATION_NOT_CALIBRATED";
    public const string LimitationEvidenceInvalidationNotCalibrated = "EVIDENCE_INVALIDATION_NOT_CALIBRATED";

    public const string LimitationExpiryNotCalibrated = "THESIS_EXPIRY_DURATION_NOT_CALIBRATED";
    public const string LimitationHorizonMapUnavailable = "THESIS_HORIZON_MAP_UNAVAILABLE";
    public const string LimitationNoProtectiveStop = "THESIS_CONTRACT_NO_PROTECTIVE_STOP";
    public const string LimitationNoTargetPath = "THESIS_CONTRACT_NO_TARGET_PATH";
    public const string LimitationNoRiskSizing = "THESIS_CONTRACT_NO_RISK_SIZING";
    public const string LimitationLiveOnly = "LIVE_ONLY_HISTORY";

    /// <summary>
    /// v1.2 §32.3: analytical invalidation is not a protective hard stop.
    /// Phase 3C models invalidation only; stop calculation (§32.4) needs
    /// volatility/MAE distributions and is not authorized here.
    /// </summary>
    public const bool ProtectiveStopAuthorized = false;

    /// <summary>All five dimensions of v1.3 §11.3 must be declared.</summary>
    public const int RequiredInvalidationDimensions = 5;

    /// <summary>All five horizons of v1.2 §11.2 must be declared.</summary>
    public const int RequiredHorizonRoles = 5;

    public ThesisContractPolicyConfig(bool enabled = false)
    {
        Enabled = enabled;
    }

    public bool Enabled { get; }
    public string Version => PolicyVersion;
}
