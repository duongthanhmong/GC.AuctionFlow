namespace GC.AuctionFlow.Research;

/// <summary>
/// One stratification axis and whether it can key a distribution.
/// </summary>
public sealed class StratificationAxisStatus
{
    public StratificationAxisStatus(
        StratificationAxis axis,
        StratificationAxisAvailability availability,
        string reason)
    {
        Axis = axis;
        Availability = availability;
        Reason = reason;
    }

    public StratificationAxis Axis { get; }

    public StratificationAxisAvailability Availability { get; }

    /// <summary>Why the axis is where it is. Reported, not inferred by the reader.</summary>
    public string Reason { get; }

    public bool CanStratify => Availability == StratificationAxisAvailability.Available;
}

/// <summary>
/// The mandatory unlock protocol of v1.3 §14.1, reported as a position rather than a
/// verdict.
///
/// `G-CAL-001` forbids unlocking any `[C]` state with a threshold chosen by whoever
/// wrote the code. `G-CAL-002` replaces that with six ordered steps. The scanner performs
/// step one; steps two through six happen outside this DLL, in a research process with a
/// DECISION_LOG. So this type exists to answer one question honestly: given what the
/// build can actually do today, which step is the protocol stuck at?
///
/// The answer is currently step two. A distribution must be stratified by ReferenceType x
/// ParticipationRegime x VolatilityRegime. ReferenceType is emitted on every episode;
/// VolatilityRegime now has a classifier but no registered boundaries; ParticipationRegime
/// rests on a label that is itself calibration-gated. Pooling across an axis that cannot
/// key anything would yield a distribution that looks complete, and a threshold derived
/// from it would be exactly the coder-chosen number `G-CAL-001` bans.
/// </summary>
public sealed class CalibrationProtocol
{
    public const int TotalSteps = 6;

    private CalibrationProtocol(
        IReadOnlyList<StratificationAxisStatus> axes,
        CalibrationProtocolStep blockedAt,
        string blockedReason,
        int stepsSatisfied)
    {
        StratificationAxes = axes;
        BlockedAt = blockedAt;
        BlockedReason = blockedReason;
        StepsSatisfied = stepsSatisfied;
    }

    public IReadOnlyList<StratificationAxisStatus> StratificationAxes { get; }

    /// <summary>The first step that cannot be satisfied by this build.</summary>
    public CalibrationProtocolStep BlockedAt { get; }

    public string BlockedReason { get; }

    /// <summary>Steps fully satisfied. Never <see cref="TotalSteps"/> from inside the DLL.</summary>
    public int StepsSatisfied { get; }

    /// <summary>
    /// Always false. Kept as an explicit property rather than an absent one so that any
    /// future attempt to unlock a state has to change a value that tests assert on.
    /// </summary>
    public bool UnlockPermitted => StepsSatisfied >= TotalSteps;

    public IReadOnlyList<StratificationAxis> MissingAxes =>
        StratificationAxes.Where(a => !a.CanStratify).Select(a => a.Axis).ToArray();

    /// <summary>
    /// Evaluates the protocol against what the build provides.
    /// </summary>
    /// <param name="hasCollectedRows">Whether the scanner has any dataset rows at all.</param>
    public static CalibrationProtocol Evaluate(
        bool hasCollectedRows,
        bool volatilityRegimeCanStratify = false)
    {
        var axes = new[]
        {
            new StratificationAxisStatus(
                StratificationAxis.ReferenceType,
                StratificationAxisAvailability.Available,
                "ReferenceType is emitted on every episode"),

            // ThinParticipationLabel exists but is itself calibration-gated, so it cannot
            // key a distribution used to calibrate something else — that is circular.
            new StratificationAxisStatus(
                StratificationAxis.ParticipationRegime,
                StratificationAxisAvailability.ImplementedButNotCalibrated,
                "ThinParticipationLabel is THIN_PARTICIPATION_NOT_CALIBRATED; "
                + "using it as a key would calibrate one gate with another"),

            // The classifier now exists. Its boundaries are registered from outside the
            // build rather than derived here: boundaries computed from the observed sample
            // would move as it grew, which re-chooses the sample criteria after seeing
            // results (G-FAST-001) and makes step 4's out-of-sample validation impossible,
            // because the out-of-sample data would have helped build the bins judging it.
            new StratificationAxisStatus(
                StratificationAxis.VolatilityRegime,
                volatilityRegimeCanStratify
                    ? StratificationAxisAvailability.Available
                    : StratificationAxisAvailability.ImplementedButNotCalibrated,
                volatilityRegimeCanStratify
                    ? "realized range per completed period, against registered boundaries"
                    : "realized range is collected, but no boundaries are registered in "
                      + "the DECISION_LOG yet"),
        };

        if (!hasCollectedRows)
        {
            return new CalibrationProtocol(
                axes,
                CalibrationProtocolStep.CollectRawFeatures,
                "no dataset rows collected yet",
                stepsSatisfied: 0);
        }

        var missing = axes.Where(a => !a.CanStratify).ToArray();
        if (missing.Length > 0)
        {
            return new CalibrationProtocol(
                axes,
                CalibrationProtocolStep.StratifiedDistribution,
                "cannot stratify by " + string.Join(" and ", missing.Select(a => a.Axis))
                + "; pooling across a missing axis yields a distribution that looks "
                + "complete and is not",
                stepsSatisfied: 1);
        }

        // Reachable only once both classifiers exist. Step three is then the wall, and it
        // is not one the DLL can climb: sample criteria must be registered in advance by
        // a research process, and choosing them here after seeing rows is precisely what
        // G-FAST-001 forbids.
        return new CalibrationProtocol(
            axes,
            CalibrationProtocolStep.PreRegisteredSampleCriteria,
            "sample criteria must be registered in advance and outside this build",
            stepsSatisfied: 2);
    }
}
