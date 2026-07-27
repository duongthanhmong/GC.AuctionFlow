namespace GC.AuctionFlow.Research;

/// <summary>
/// Coarse module gate for the Historical Scanner.
/// </summary>
public enum HistoricalScannerState
{
    Disabled = 0,
    AwaitingEpisodes = 1,

    /// <summary>Rows are accumulating. Says nothing about whether they are enough.</summary>
    Collecting = 2,
    InvalidData = 3
}

/// <summary>
/// The six steps of the mandatory unlock protocol (v1.3 §14.1 `G-CAL-002`).
///
/// The scanner performs step one. It reports which step the protocol is blocked at so
/// that "not calibrated" is a position in a process rather than a permanent shrug.
/// </summary>
public enum CalibrationProtocolStep
{
    /// <summary>Historical Scanner collects raw features.</summary>
    CollectRawFeatures = 1,

    /// <summary>Empirical distribution stratified by ReferenceType x Participation x Volatility.</summary>
    StratifiedDistribution = 2,

    /// <summary>Sample criteria fixed in advance (`G-FAST-001`).</summary>
    PreRegisteredSampleCriteria = 3,

    /// <summary>Out-of-sample validation.</summary>
    OutOfSampleValidation = 4,

    /// <summary>Live Shadow.</summary>
    LiveShadow = 5,

    /// <summary>Written to DECISION_LOG with a hash of the source data.</summary>
    DecisionLogEntry = 6
}

/// <summary>
/// The three axes a distribution must be stratified by before any threshold may be
/// derived from it (`G-CAL-002` step 2).
/// </summary>
public enum StratificationAxis
{
    ReferenceType = 0,
    ParticipationRegime = 1,
    VolatilityRegime = 2
}

/// <summary>
/// Whether an axis can actually stratify anything yet.
///
/// An axis that does not exist is not the same as an axis with one bucket. Pooling
/// across a missing axis produces a distribution that looks complete and is not, which
/// is the failure mode `G-CAL-001` exists to prevent.
/// </summary>
public enum StratificationAxisAvailability
{
    /// <summary>No classifier for this axis exists in the build.</summary>
    NotImplemented = 0,

    /// <summary>A classifier exists but its own output is calibration-gated.</summary>
    ImplementedButNotCalibrated = 1,

    /// <summary>Usable as a stratification key.</summary>
    Available = 2
}

/// <summary>
/// The first low-cost scanner workload (v1.2 §46.7).
///
/// These nine studies were chosen because they run on Profile, trades, bid/ask and
/// timestamps — no waiting months for DOM/MBO history.
/// </summary>
public enum ScannerStudyKind
{
    IbWidthPercentile = 0,
    DayStructureDistribution = 1,
    IbExtremeRevisitOutcome = 2,
    CompositeMergeTolerance = 3,
    LvnHvnBaseRate = 4,
    ThinParticipationEffect = 5,
    OneTimeFramingPersistence = 6,
    PreSettlementEpisodeOutcome = 7,
    AdjacentBuildBarrierOutcome = 8
}

/// <summary>
/// How far a study has got.
///
/// There is deliberately no Calibrated member. A study that has produced a threshold is
/// no longer the scanner's business — it belongs to the DECISION_LOG, and adding the
/// member here would let a future edit shortcut steps four through six.
/// </summary>
public enum ScannerStudyState
{
    /// <summary>Blocked: an input the study needs does not exist in this build.</summary>
    AwaitingPrerequisite = 0,

    /// <summary>Rows are being collected against this study.</summary>
    Collecting = 1,

    /// <summary>
    /// Enough rows exist to compute a distribution, but the sample criteria that would
    /// make it admissible were never registered. `G-FAST-001` forbids choosing them now.
    /// </summary>
    AwaitingSampleCriteria = 2
}

/// <summary>
/// Why a dataset row may be inadmissible for calibration.
/// </summary>
public enum DatasetRowAdmissibility
{
    /// <summary>Usable as a raw feature row. Not a claim that any study may use it.</summary>
    Admissible = 0,

    /// <summary>The episode ended for a data reason rather than a market one.</summary>
    InvalidData = 1,

    /// <summary>Bid/ask was unknown, so aggressor-derived features are absent.</summary>
    AggressorEvidenceMissing = 2,

    /// <summary>The episode was open when the indicator started or stopped.</summary>
    TruncatedByLiveOnlyWindow = 3
}
