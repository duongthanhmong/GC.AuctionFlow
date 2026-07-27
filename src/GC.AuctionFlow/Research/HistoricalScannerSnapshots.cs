using GC.AuctionFlow.Reference;

namespace GC.AuctionFlow.Research;

/// <summary>
/// What the Historical Scanner has collected, and how far the unlock protocol has got.
///
/// Deliberately absent: any threshold, percentile, base rate or verdict. This snapshot
/// answers "what has been observed and what is still missing", never "what does it mean".
/// </summary>
public sealed class HistoricalScannerSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public HistoricalScannerSnapshot(
        HistoricalScannerState state,
        string policyVersion,
        int rowsCollected,
        int admissibleRows,
        int rowsDroppedToCapacity,
        IReadOnlyDictionary<ReferenceType, int> rowsByReferenceType,
        IReadOnlyDictionary<DatasetRowAdmissibility, int> rowsByAdmissibility,
        CalibrationProtocol protocol,
        IReadOnlyList<ScannerStudy> studies,
        DateTime datasetStartedAtUtc,
        DateTime lastUpdatedAtUtc,
        IReadOnlyList<string> limitations,
        BarReplayState barReplayState = BarReplayState.Disabled,
        int barDerivedRows = 0,
        int barsWalked = 0,
        int volatilityObservations = 0)
    {
        State = state;
        PolicyVersion = policyVersion ?? "";
        RowsCollected = rowsCollected;
        AdmissibleRows = admissibleRows;
        RowsDroppedToCapacity = rowsDroppedToCapacity;
        RowsByReferenceType = rowsByReferenceType ?? new Dictionary<ReferenceType, int>();
        RowsByAdmissibility = rowsByAdmissibility ?? new Dictionary<DatasetRowAdmissibility, int>();
        Protocol = protocol;
        Studies = studies ?? Array.Empty<ScannerStudy>();
        DatasetStartedAtUtc = datasetStartedAtUtc;
        LastUpdatedAtUtc = lastUpdatedAtUtc;
        Limitations = limitations ?? Array.Empty<string>();
        BarReplayState = barReplayState;
        BarDerivedRows = barDerivedRows;
        BarsWalked = barsWalked;
        VolatilityObservations = volatilityObservations;
    }

    public HistoricalScannerState State { get; }

    public string PolicyVersion { get; }

    /// <summary>
    /// Every row ever folded, independent of how many are retained.
    ///
    /// Tracked separately from the retained list for the same reason the price memory
    /// ledger does: a dataset that saw 3000 episodes and reports 2048 because of a
    /// retention bound is a quiet lie about sample size, and sample size is exactly what
    /// a calibration decision would rest on.
    /// </summary>
    public int RowsCollected { get; }

    public int AdmissibleRows { get; }

    /// <summary>Rows evicted by the retention bound. Reported, never silent.</summary>
    public int RowsDroppedToCapacity { get; }

    public IReadOnlyDictionary<ReferenceType, int> RowsByReferenceType { get; }

    public IReadOnlyDictionary<DatasetRowAdmissibility, int> RowsByAdmissibility { get; }

    public CalibrationProtocol Protocol { get; }

    public IReadOnlyList<ScannerStudy> Studies { get; }

    public DateTime DatasetStartedAtUtc { get; }

    public DateTime LastUpdatedAtUtc { get; }

    public IReadOnlyList<string> Limitations { get; }

    public BarReplayState BarReplayState { get; }

    /// <summary>
    /// Rows measured from completed bars.
    ///
    /// Kept apart from <see cref="RowsCollected"/> and never summed with it. A bar-derived
    /// row has no intra-bar ordering, so it cannot answer an acceptance or re-entry
    /// question that a live episode row can; adding the two would produce a larger set
    /// that is less sound, which is the same error as pooling across a missing
    /// stratification axis.
    /// </summary>
    public int BarDerivedRows { get; }

    public int BarsWalked { get; }

    /// <summary>
    /// Completed periods whose realized range has been recorded.
    ///
    /// Raw material for a boundary decision that happens outside this build. It is not a
    /// count of anything calibrated.
    /// </summary>
    public int VolatilityObservations { get; }

    /// <summary>
    /// Always false while the protocol runs outside this DLL.
    ///
    /// Surfaced on the snapshot so that a change of mind about it has to be a visible
    /// edit against a tested value rather than a quiet consequence of something else.
    /// </summary>
    public bool AnyStateUnlocked => Protocol.UnlockPermitted;
}
