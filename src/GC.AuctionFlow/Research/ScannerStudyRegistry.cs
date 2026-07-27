namespace GC.AuctionFlow.Research;

/// <summary>
/// One of the nine first-workload studies, and what it is waiting on.
/// </summary>
public sealed class ScannerStudy
{
    public ScannerStudy(
        ScannerStudyKind kind,
        ScannerStudyState state,
        string blockedOn,
        int rowsCollected)
    {
        Kind = kind;
        State = state;
        BlockedOn = blockedOn;
        RowsCollected = rowsCollected;
    }

    public ScannerStudyKind Kind { get; }

    public ScannerStudyState State { get; }

    /// <summary>The specific missing thing, not a generic "not calibrated".</summary>
    public string BlockedOn { get; }

    /// <summary>Rows collected against this study. Never compared to a required count.</summary>
    public int RowsCollected { get; }
}

/// <summary>
/// The first low-cost scanner workload (v1.2 §46.7).
///
/// These nine were chosen because they run on Profile, trades, bid/ask and timestamps,
/// so they do not require months of DOM/MBO history first. Several are nonetheless
/// blocked, and by different things — this reports which, per study, rather than
/// flattening it all into one unhelpful state.
///
/// No study reports a result. `G-FAST-001` requires sample criteria to be fixed before
/// the distribution is looked at, and those criteria are a research governance decision
/// made outside this build. A study that has enough rows therefore reaches
/// AwaitingSampleCriteria and stops there, which is the honest terminus.
/// </summary>
public static class ScannerStudyRegistry
{
    /// <summary>
    /// Evaluates every study against what this build provides.
    /// </summary>
    /// <param name="admissibleRows">Dataset rows admissible as raw features.</param>
    public static IReadOnlyList<ScannerStudy> Evaluate(int admissibleRows)
    {
        // Each study names its own missing prerequisite. Where a prerequisite exists but
        // is itself calibration-gated, the study is blocked rather than collecting —
        // building a distribution on a gated input calibrates one gate with another.
        var definitions = new (ScannerStudyKind Kind, string? Blocker)[]
        {
            // Needs a historical initial-balance width distribution. The profile emits IB
            // width, but nothing accumulates it across auctions.
            (ScannerStudyKind.IbWidthPercentile, "no cross-auction IB width accumulator"),

            // The classifier exists (Phase 1H) and is research-only, so its labels can be
            // counted. Counting labels is not calibrating them.
            (ScannerStudyKind.DayStructureDistribution, null),

            (ScannerStudyKind.IbExtremeRevisitOutcome,
                "IBExtreme end-of-day labelling not implemented"),

            // Composite merge tolerance is a Phase 1B policy input, not an observation.
            (ScannerStudyKind.CompositeMergeTolerance,
                "merge tolerance is an operator input, not an observable"),

            (ScannerStudyKind.LvnHvnBaseRate, "LVN/HVN node detection not implemented"),

            (ScannerStudyKind.ThinParticipationEffect,
                "ThinParticipationLabel is THIN_PARTICIPATION_NOT_CALIBRATED"),

            (ScannerStudyKind.OneTimeFramingPersistence,
                "OneTimeFramingState Confirmed is OTF_CONFIRMATION_NOT_CALIBRATED"),

            // Settlement proximity is a tag, not a signal, so tagging episodes by it is
            // observation rather than inference.
            (ScannerStudyKind.PreSettlementEpisodeOutcome, null),

            (ScannerStudyKind.AdjacentBuildBarrierOutcome,
                "adjacent-build detection not implemented"),
        };

        var studies = new List<ScannerStudy>(definitions.Length);
        foreach (var (kind, blocker) in definitions)
        {
            if (blocker is not null)
            {
                studies.Add(new ScannerStudy(
                    kind, ScannerStudyState.AwaitingPrerequisite, blocker, rowsCollected: 0));
                continue;
            }

            // Rows exist and nothing upstream is missing — so the study is only ever as
            // far as "someone must register the sample criteria", and that someone is not
            // this code.
            studies.Add(admissibleRows > 0
                ? new ScannerStudy(
                    kind,
                    ScannerStudyState.AwaitingSampleCriteria,
                    HistoricalScannerPolicyConfig.LimitationSampleCriteriaNotRegistered,
                    admissibleRows)
                : new ScannerStudy(
                    kind, ScannerStudyState.Collecting, "no admissible rows yet", rowsCollected: 0));
        }

        return studies;
    }
}
