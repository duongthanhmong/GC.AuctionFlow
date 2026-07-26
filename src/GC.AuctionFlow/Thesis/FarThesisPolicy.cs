namespace GC.AuctionFlow.Thesis;

public sealed class FarThesisPolicyConfig
{
    public const string PolicyVersion = "FAR_THESIS_POLICY_V1";
    public const string LimitationNotCalibrated       = "FAR_THESIS_NOT_CALIBRATED";
    public const string LimitationArmedNotCalibrated  = "FAR_ARMED_NOT_CALIBRATED";
    public const string LimitationExecutableNotCalibrated = "FAR_EXECUTABLE_NOT_CALIBRATED";
    public const string LimitationManagingNotCalibrated   = "FAR_MANAGING_NOT_CALIBRATED";
    public const string LimitationCompletedNotCalibrated  = "FAR_COMPLETED_NOT_CALIBRATED";
    public const string LimitationReacceptedInsideNotCalibrated = "FAR_REACCEPTED_INSIDE_NOT_CALIBRATED";
    public const string LimitationLiveOnly            = "LIVE_ONLY_HISTORY";
    public const string LimitationThesisNotAuthorized = "FAR_THESIS_NOT_AUTHORIZED";
    public const string LimitationEntryNotAuthorized  = "FAR_ENTRY_NOT_AUTHORIZED";
    public const string LimitationRiskNotAuthorized   = "FAR_RISK_NOT_AUTHORIZED";
    public const string LimitationTwoAttemptNotCalibrated = "FAR_TWO_ATTEMPT_NOT_CALIBRATED";

    public FarThesisPolicyConfig(bool enabled = false) { Enabled = enabled; }
    public bool Enabled { get; }
    public string Version => PolicyVersion;
}
