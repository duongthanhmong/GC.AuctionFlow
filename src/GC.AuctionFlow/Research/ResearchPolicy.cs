namespace GC.AuctionFlow.Research;

/// <summary>
/// Phase 5A Historical Scanner (v1.2 §46, v1.3 §14.1).
///
/// This module performs step one of the six-step unlock protocol and nothing else: it
/// collects raw features so that rule versions can be re-run later without re-collecting
/// data (v1.2 §46.3). It derives no threshold, unlocks no `[C]` state, and reaches no
/// verdict about any reference, episode or day.
///
/// There is no numeric constant here that gates a decision. The only numbers are
/// retention capacities, which bound memory and are named as such.
/// </summary>
public sealed class HistoricalScannerPolicyConfig
{
    public const string PolicyVersion = "HISTORICAL_SCANNER_POLICY_V1";

    public const string LimitationNoThresholdDerived = "NO_THRESHOLD_DERIVED_BY_SCANNER";
    public const string LimitationSampleCriteriaNotRegistered = "SAMPLE_CRITERIA_NOT_PRE_REGISTERED";
    public const string LimitationStratificationIncomplete = "STRATIFICATION_AXES_INCOMPLETE";
    public const string LimitationNoOutOfSampleValidation = "OUT_OF_SAMPLE_VALIDATION_NOT_RUN";
    public const string LimitationNoLiveShadow = "LIVE_SHADOW_NOT_RUN";
    public const string LimitationNoDecisionLogEntry = "DECISION_LOG_ENTRY_ABSENT";
    public const string LimitationLiveOnly = "LIVE_ONLY_HISTORY";
    public const string LimitationDatasetStartsAtIndicatorStart = "DATASET_STARTS_AT_INDICATOR_START";
    public const string LimitationResearchOnly = "RESEARCH_ONLY_NOT_A_PRODUCTION_INPUT";

    /// <summary>
    /// Dataset rows retained in memory before the oldest is dropped.
    ///
    /// A retention bound, not a sample size. Deciding how many rows a study needs is a
    /// research governance act — see <see cref="CalibrationProtocol"/>.
    /// </summary>
    public const int RowCapacity = 2048;

    public HistoricalScannerPolicyConfig(bool enabled = false)
    {
        Enabled = enabled;
    }

    public bool Enabled { get; }

    public string Version => PolicyVersion;

    /// <summary>
    /// Every limitation this module always carries.
    ///
    /// All six are permanent as far as this module is concerned. Steps two through six
    /// of the protocol happen outside the DLL, so nothing the scanner does at runtime can
    /// retire any of them.
    /// </summary>
    public static IReadOnlyList<string> StandingLimitations { get; } = new[]
    {
        LimitationResearchOnly,
        LimitationNoThresholdDerived,
        LimitationSampleCriteriaNotRegistered,
        LimitationNoOutOfSampleValidation,
        LimitationNoLiveShadow,
        LimitationNoDecisionLogEntry,
        LimitationLiveOnly,
        LimitationDatasetStartsAtIndicatorStart,
    };
}
