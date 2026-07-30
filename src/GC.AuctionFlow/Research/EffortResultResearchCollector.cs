using GC.AuctionFlow.Efficiency;

namespace GC.AuctionFlow.Research;

/// <summary>
/// The bridge that was missing: closed-episode Effort/Result evidence into the research
/// corpus (WP01A).
///
/// The Historical Scanner collects episode geometry, and the Efficiency module computes the
/// Effort and Result vectors, and until now nothing connected them — <c>Research/</c>
/// referenced neither <c>Efficiency</c> nor <c>EffortResult</c>. The collection machinery,
/// the persistence discipline, the stratification axes and the calibration protocol all
/// already existed. This host is the join, and nothing more.
///
/// What it is not:
///
/// <list type="bullet">
/// <item><description>
/// not a classifier — it reads <see cref="AuctionEfficiencyEvidenceSnapshot"/> directly and
/// never reconstructs raw measurements from a classification snapshot, which would be a
/// measurement of the classifier rather than of the market;
/// </description></item>
/// <item><description>
/// not a normalizer — no percentile, z-score, ranking, bucketing or threshold. Choosing one
/// before the corpus exists is what `G-CAL-001` forbids;
/// </description></item>
/// <item><description>
/// not an outcome labeller — the Result vector is what was observable when the episode
/// closed. A follow-through label needs its own horizon and identity and is not collected.
/// </description></item>
/// </list>
///
/// Like the scanner, this host is stateful across rebuilds by design and its accumulation is
/// LIVE_ONLY: observations begin at indicator start and pre-start activity is never
/// reconstructed.
/// </summary>
public sealed class EffortResultResearchCollector
{
    private readonly List<EffortResultResearchRecord> _observations = new();
    private readonly HashSet<string> _collectedIds = new(StringComparer.Ordinal);
    private readonly EpisodeDatasetStore _store;

    private EffortResultResearchPolicyConfig _policy;
    private Task<IReadOnlyList<EffortResultResearchRecord>>? _loading;
    private int _loadMerged;
    private int _recoveredObservations;

    private DateTime _startedAtUtc;
    private int _observationsCollected;
    private int _observationsDropped;
    private int _rejectedNotClosedEpisode;
    private int _rejectedNotFrozen;
    private int _rejectedNoIdentity;

    public EffortResultResearchCollector(
        EffortResultResearchPolicyConfig? policy = null,
        EpisodeDatasetStore? store = null)
    {
        _policy = policy ?? new EffortResultResearchPolicyConfig(enabled: false);
        _store = store ?? new EpisodeDatasetStore();
    }

    public EffortResultResearchPolicyConfig Policy => _policy;

    /// <summary>Retained observations, oldest first. Bounded by the retention capacity.</summary>
    public IReadOnlyList<EffortResultResearchRecord> Observations => _observations;

    /// <summary>Distinct observations seen this session, including those since evicted.</summary>
    public int ObservationsCollected => _observationsCollected;

    /// <summary>Observations evicted to stay inside the retention bound.</summary>
    public int ObservationsDropped => _observationsDropped;

    /// <summary>
    /// Observations recovered from previous sessions, once the background load lands.
    ///
    /// Named separately from the session total for the same reason the scanner does it: six
    /// rows earned today and six restored from disk are the same number and not the same
    /// fact.
    /// </summary>
    public int RecoveredObservations => Volatile.Read(ref _recoveredObservations);

    public int RejectedNotClosedEpisode => _rejectedNotClosedEpisode;

    public int RejectedNotFrozen => _rejectedNotFrozen;

    public int RejectedNoStableIdentity => _rejectedNoIdentity;

    /// <summary>When this collector first ran. Default until it does.</summary>
    public DateTime StartedAtUtc => _startedAtUtc;

    public void Configure(EffortResultResearchPolicyConfig policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        _policy = policy;

        // Disabling PAUSES collection. It does not erase the corpus.
        //
        // Rebuild returns early while disabled, and that is the whole of the effect. The
        // observations, the dedup ids, the recovery state, the counters and the start
        // timestamp all survive, because they describe what was observed and a toggle is not
        // an observation. Keeping the object alive while clearing its state would have been
        // the same defect wearing a longer lifetime.
    }

    /// <summary>
    /// Collects every newly closed, frozen efficiency observation.
    ///
    /// The closed-episode list is a rolling window, so the same observation is seen on many
    /// publishes; collecting it twice would weight every distribution built on this corpus by
    /// the publish schedule. In-session deduplication happens here, and cross-session
    /// deduplication happens at the file boundary — the same division the episode dataset
    /// arrived at after a restart inflated it to 5,126 lines for 2,098 distinct rows.
    ///
    /// Collection is never gated on recovery landing. Gating it would make whether a module
    /// collects anything depend on disk latency, which was tried once and reverted.
    /// </summary>
    public void Rebuild(AuctionEfficiencyEvidenceSetSnapshot? efficiency, DateTime? nowUtc = null)
    {
        if (!_policy.Enabled)
            return;

        var now = nowUtc ?? DateTime.UtcNow;
        if (_startedAtUtc == default)
            _startedAtUtc = now;

        // Dispatched once and merged whenever it completes. Reading on this thread would put
        // disk work on an ATAS callback, which is the defect that corrupted a chart.
        _loading ??= _store.LoadEffortResultResearchAsync();
        if (Volatile.Read(ref _loadMerged) == 0 && _loading.IsCompletedSuccessfully)
        {
            Volatile.Write(ref _loadMerged, 1);
            foreach (var persisted in _loading.Result)
            {
                if (_collectedIds.Add(persisted.ObservationId))
                {
                    _observationsCollected++;
                    _recoveredObservations++;
                }
            }
        }

        if (efficiency is null)
            return;

        List<EffortResultResearchRecord>? fresh = null;

        foreach (var snapshot in efficiency.RecentlyClosedEpisodeEvidence)
        {
            // Cheap identity check before admission, so a repeated publish costs a set
            // lookup rather than a record construction.
            if (snapshot is not null
                && snapshot.ScopeType == EfficiencyScopeType.ClosedEpisode
                && snapshot.IsFrozen
                && _collectedIds.Contains(EffortResultResearchProjection.BuildObservationId(snapshot)))
            {
                continue;
            }

            var record = EffortResultResearchProjection.TryProject(snapshot, now, out var rejection);
            if (record is null)
            {
                switch (rejection)
                {
                    case EffortResultResearchRejection.NotClosedEpisode:
                        _rejectedNotClosedEpisode++;
                        break;
                    case EffortResultResearchRejection.NotFrozen:
                        _rejectedNotFrozen++;
                        break;
                    default:
                        _rejectedNoIdentity++;
                        break;
                }

                continue;
            }

            if (!_collectedIds.Add(record.ObservationId))
                continue;

            Fold(record);
            (fresh ??= new List<EffortResultResearchRecord>()).Add(record);
        }

        // Persisted after folding, so a row that survives dedup is the one that reaches disk.
        // The call queues and returns; the write happens on the store's own thread.
        if (fresh is not null)
            _store.AppendEffortResultResearch(fresh);
    }

    private void Fold(EffortResultResearchRecord record)
    {
        _observations.Add(record);
        _observationsCollected++;

        // Evict oldest first. The counts above are never rewound: they describe what was
        // observed, and retention is a memory bound, not a revision of history.
        while (_observations.Count > EffortResultResearchPolicyConfig.ObservationCapacity)
        {
            _observations.RemoveAt(0);
            _observationsDropped++;
        }
    }

}
