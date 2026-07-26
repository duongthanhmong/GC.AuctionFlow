namespace GC.AuctionFlow.Thesis;

public sealed class AacThesisPolicyConfig
{
    public const string PolicyVersion = "AAC_THESIS_POLICY_V1";
    public const string LimitationNotCalibrated          = "AAC_THESIS_NOT_CALIBRATED";
    public const string LimitationArmedNotCalibrated     = "AAC_ARMED_NOT_CALIBRATED";
    public const string LimitationExecutableNotCalibrated = "AAC_EXECUTABLE_NOT_CALIBRATED";
    public const string LimitationManagingNotCalibrated  = "AAC_MANAGING_NOT_CALIBRATED";
    public const string LimitationCompletedNotCalibrated = "AAC_COMPLETED_NOT_CALIBRATED";
    public const string LimitationAcceptedOutsideNotCalibrated = "AAC_ACCEPTED_OUTSIDE_NOT_CALIBRATED";
    public const string LimitationPullbackNotCalibrated  = "AAC_PULLBACK_NOT_CALIBRATED";
    public const string LimitationLiveOnly               = "LIVE_ONLY_HISTORY";
    public const string LimitationThesisNotAuthorized    = "AAC_THESIS_NOT_AUTHORIZED";
    public const string LimitationEntryNotAuthorized     = "AAC_ENTRY_NOT_AUTHORIZED";
    public const string LimitationRiskNotAuthorized      = "AAC_RISK_NOT_AUTHORIZED";

    public AacThesisPolicyConfig(bool enabled = false) { Enabled = enabled; }
    public bool Enabled { get; }
    public string Version => PolicyVersion;
}
