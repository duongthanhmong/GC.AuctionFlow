using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Reference;

namespace GC.AuctionFlow.Research;

/// <summary>
/// Phase 5A Historical Scanner host (v1.2 §46, v1.3 §14.1).
///
/// Folds closed episodes into a raw-feature dataset. Like the price memory ledger this
/// host is stateful across rebuilds by design — a dataset that forgets between snapshots
/// is not a dataset — and like it, the accumulation is LIVE_ONLY: rows begin at indicator
/// start and pre-start activity is never reconstructed.
///
/// It performs step one of the six-step unlock protocol. It derives no threshold and
/// unlocks no `[C]` state, and the reason it cannot is structural rather than a matter of
/// restraint: steps two through six happen in a research process with a DECISION_LOG,
/// outside this build entirely.
/// </summary>
public sealed class HistoricalScannerHost
{
    private readonly List<EpisodeDatasetRecord> _rows = new();
    private readonly HashSet<string> _foldedEpisodeIds = new(StringComparer.Ordinal);
    private readonly Dictionary<ReferenceType, int> _byReferenceType = new();
    private readonly Dictionary<DatasetRowAdmissibility, int> _byAdmissibility = new();

    private HistoricalScannerPolicyConfig _policy;
    private HistoricalScannerSnapshot? _published;
    private DateTime _datasetStartedAtUtc;
    private int _rowsCollected;
    private int _admissibleRows;
    private int _rowsDropped;

    private readonly EpisodeDatasetStore _store;
    private Task<IReadOnlyList<PersistedEpisodeRow>>? _loading;
    private int _loadMerged;
    private int _recoveredRows;

    public HistoricalScannerHost(
        HistoricalScannerPolicyConfig? policy = null,
        EpisodeDatasetStore? store = null)
    {
        _policy = policy ?? new HistoricalScannerPolicyConfig(enabled: false);
        _store = store ?? new EpisodeDatasetStore();
    }

    /// <summary>Rows recovered from previous sessions, once the background load lands.</summary>
    public int RecoveredRows => Volatile.Read(ref _recoveredRows);

    public HistoricalScannerSnapshot? Current => _published;

    public HistoricalScannerPolicyConfig Policy => _policy;

    /// <summary>Retained rows, oldest first. Bounded by the retention capacity.</summary>
    public IReadOnlyList<EpisodeDatasetRecord> Rows => _rows;

    public void Configure(HistoricalScannerPolicyConfig policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        _policy = policy;
        if (!_policy.Enabled)
        {
            Reset();
            _published = DisabledSnapshot(DateTime.UtcNow);
        }
    }

    /// <summary>
    /// Folds every newly closed episode into the dataset.
    ///
    /// Only closed episodes are folded. An episode still developing has no terminal state
    /// to record, and folding it early would put a row in the dataset whose outcome is
    /// whatever it happened to be at snapshot time — a measurement of the publish
    /// schedule rather than of the market.
    /// </summary>
    public void Rebuild(
        AuctionEpisodeSetSnapshot? episodes,
        DateTime? nowUtc = null,
        HistoricalBarReplayHost? replay = null,
        VolatilityRegimeHost? volatility = null,
        ParticipationRegimeHost? participation = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;

        if (!_policy.Enabled)
        {
            _published = DisabledSnapshot(now);
            return;
        }

        if (_datasetStartedAtUtc == default)
            _datasetStartedAtUtc = now;

        // Recovery is dispatched once and merged whenever it completes. Reading on this
        // thread would put disk work back on an ATAS callback, which is the defect that
        // corrupted a chart earlier today.
        _loading ??= _store.LoadAsync();
        if (Volatile.Read(ref _loadMerged) == 0 && _loading.IsCompletedSuccessfully)
        {
            Volatile.Write(ref _loadMerged, 1);
            foreach (var persisted in _loading.Result)
            {
                // Recovered rows seed the dedup set so a re-observed episode is not
                // counted twice, and count towards the sample without re-entering the
                // retained list — the file is the retained list now.
                if (_foldedEpisodeIds.Add(persisted.EpisodeId))
                {
                    _rowsCollected++;
                    if (persisted.Admissibility == DatasetRowAdmissibility.Admissible)
                        _admissibleRows++;
                    _recoveredRows++;
                }
            }
        }

        var newRows = new List<EpisodeDatasetRecord>();

        // Folding is never gated on recovery landing. It was, briefly, and that made
        // whether a module collected anything depend on disk latency. Duplicate rows are
        // the file's problem and the store rejects them at the boundary.
        if (episodes is not null)
        {
            foreach (var episode in episodes.RecentlyClosedEpisodes)
            {
                if (string.IsNullOrEmpty(episode.EpisodeId))
                    continue;

                // The closed-episode list is a rolling window, so the same episode is
                // seen on many publishes. Folding it twice would double-count it into
                // every distribution built on this dataset.
                if (!_foldedEpisodeIds.Add(episode.EpisodeId))
                    continue;

                var row = EpisodeDatasetRecord.FromEpisode(episode);
                Fold(row);
                newRows.Add(row);
            }
        }

        // Persisted after folding, so a row that survives dedup is the one that reaches
        // disk. The call queues and returns; the write happens in the background.
        if (newRows.Count > 0)
            _store.Append(newRows);

        // Bar-derived rows count towards the module being alive but never towards the
        // episode dataset, which is what the calibration protocol reads.
        var barRows = replay?.RowsMeasured ?? 0;

        var state = _rowsCollected > 0 || barRows > 0
            ? HistoricalScannerState.Collecting
            : HistoricalScannerState.AwaitingEpisodes;

        var protocol = CalibrationProtocol.Evaluate(
            hasCollectedRows: _rowsCollected > 0,
            volatilityRegimeCanStratify: volatility?.CanStratify ?? false,
            participationRegimeCanStratify: participation?.CanStratify ?? false);

        var limitations = new List<string>(HistoricalScannerPolicyConfig.StandingLimitations);
        if (protocol.MissingAxes.Count > 0)
            limitations.Add(HistoricalScannerPolicyConfig.LimitationStratificationIncomplete);

        _published = new HistoricalScannerSnapshot(
            state,
            HistoricalScannerPolicyConfig.PolicyVersion,
            _rowsCollected,
            _admissibleRows,
            _rowsDropped,
            new Dictionary<ReferenceType, int>(_byReferenceType),
            new Dictionary<DatasetRowAdmissibility, int>(_byAdmissibility),
            protocol,
            ScannerStudyRegistry.Evaluate(_admissibleRows),
            _datasetStartedAtUtc,
            now,
            limitations,
            replay?.State ?? BarReplayState.Disabled,
            barRows,
            replay?.BarsWalked ?? 0,
            volatility?.ObservationsSeen ?? 0,
            _recoveredRows);
    }

    private void Fold(EpisodeDatasetRecord row)
    {
        _rows.Add(row);
        _rowsCollected++;

        if (row.Admissibility == DatasetRowAdmissibility.Admissible)
            _admissibleRows++;

        _byReferenceType[row.ReferenceType] =
            _byReferenceType.TryGetValue(row.ReferenceType, out var byType) ? byType + 1 : 1;
        _byAdmissibility[row.Admissibility] =
            _byAdmissibility.TryGetValue(row.Admissibility, out var byAdm) ? byAdm + 1 : 1;

        // Evict oldest first. The counts above are never rewound: they describe what was
        // observed, and retention is a memory bound, not a revision of history.
        while (_rows.Count > HistoricalScannerPolicyConfig.RowCapacity)
        {
            _rows.RemoveAt(0);
            _rowsDropped++;
        }
    }

    private void Reset()
    {
        _rows.Clear();
        _foldedEpisodeIds.Clear();
        _byReferenceType.Clear();
        _byAdmissibility.Clear();
        _datasetStartedAtUtc = default;
        _rowsCollected = 0;
        _admissibleRows = 0;
        _rowsDropped = 0;
    }

    private static HistoricalScannerSnapshot DisabledSnapshot(DateTime now) =>
        new(
            HistoricalScannerState.Disabled,
            HistoricalScannerPolicyConfig.PolicyVersion,
            rowsCollected: 0,
            admissibleRows: 0,
            rowsDroppedToCapacity: 0,
            new Dictionary<ReferenceType, int>(),
            new Dictionary<DatasetRowAdmissibility, int>(),
            CalibrationProtocol.Evaluate(hasCollectedRows: false),
            ScannerStudyRegistry.Evaluate(admissibleRows: 0),
            datasetStartedAtUtc: now,
            lastUpdatedAtUtc: now,
            HistoricalScannerPolicyConfig.StandingLimitations);
}
