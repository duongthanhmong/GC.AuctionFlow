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
/// The answer is currently step two, not step three, and the reason is worth stating
/// plainly: a distribution must be stratified by ReferenceType x ParticipationRegime x
/// VolatilityRegime, and only the first of those three classifiers exists. Pooling
/// across the two missing axes would yield a distribution that looks complete, and a
/// threshold derived from it would be exactly the coder-chosen number `G-CAL-001` bans.
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
    public static CalibrationProtocol Evaluate(bool hasCollectedRows)
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

            new StratificationAxisStatus(
                StratificationAxis.VolatilityRegime,
                StratificationAxisAvailability.NotImplemented,
                "no volatility regime classifier exists in this build"),
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
