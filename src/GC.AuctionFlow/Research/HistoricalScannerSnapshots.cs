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
        IReadOnlyList<string> limitations)
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

    /// <summary>
    /// Always false while the protocol runs outside this DLL.
    ///
    /// Surfaced on the snapshot so that a change of mind about it has to be a visible
    /// edit against a tested value rather than a quiet consequence of something else.
    /// </summary>
    public bool AnyStateUnlocked => Protocol.UnlockPermitted;
}
