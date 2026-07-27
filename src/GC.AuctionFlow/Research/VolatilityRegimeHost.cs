using GC.AuctionFlow.Profile;

namespace GC.AuctionFlow.Research;

/// <summary>
/// Accumulates completed-period realized ranges and classifies them against registered
/// boundaries.
///
/// Stateful across rebuilds by design, like the price memory ledger and the episode
/// dataset: a sample that forgets itself between snapshots is not a sample.
///
/// It publishes the observations and the observed extremes. It does not publish a
/// percentile, a mean or a suggested boundary — those are the research process's job, and
/// producing one here would be the DLL choosing the threshold under another name.
/// </summary>
public sealed class VolatilityRegimeHost
{
    /// <summary>Observations retained. A memory bound, not a sample-size judgement.</summary>
    public const int ObservationCapacity = 4096;

    private readonly List<VolatilityObservation> _observations = new();
    private readonly HashSet<int> _foldedPeriods = new();

    private bool _enabled;
    private int _observationsSeen;
    private int _observationsDropped;
    private VolatilityRegimeBoundaries _boundaries = VolatilityRegimeBoundaries.Unregistered;

    public VolatilityRegimeHost(bool enabled = false)
    {
        _enabled = enabled;
    }

    public VolatilityRegimeBoundaries Boundaries => _boundaries;

    public IReadOnlyList<VolatilityObservation> Observations => _observations;

    /// <summary>Every period ever folded, independent of how many are retained.</summary>
    public int ObservationsSeen => _observationsSeen;

    public int ObservationsDroppedToCapacity => _observationsDropped;

    /// <summary>Smallest realized range observed, or null before anything was observed.</summary>
    public long? MinimumRangeTicks { get; private set; }

    /// <summary>Largest realized range observed, or null before anything was observed.</summary>
    public long? MaximumRangeTicks { get; private set; }

    /// <summary>
    /// True once boundaries are registered and observations exist to place.
    ///
    /// This is what `G-CAL-002` step 2 needs from this axis. Until it is true the protocol
    /// stays blocked — but now blocked on a decision somebody can make, rather than on a
    /// classifier that does not exist.
    /// </summary>
    public bool CanStratify => _boundaries.CanStratify && _observations.Count > 0;

    public void Configure(bool enabled)
    {
        if (_enabled == enabled)
            return;

        _enabled = enabled;
        if (!enabled)
            Reset();
    }

    /// <summary>Applies boundaries decided outside this build.</summary>
    public void RegisterBoundaries(VolatilityRegimeBoundaries boundaries) =>
        _boundaries = boundaries ?? VolatilityRegimeBoundaries.Unregistered;

    /// <summary>
    /// Folds newly completed periods.
    ///
    /// Only completed periods. A period still forming has a high and low that both still
    /// move, so its range would measure how far into the period the snapshot happened to
    /// land rather than what the market did.
    /// </summary>
    public void Rebuild(IReadOnlyList<CompletedTpoPeriodSnapshot>? completedPeriods)
    {
        if (!_enabled || completedPeriods is null || completedPeriods.Count == 0)
            return;

        foreach (var period in completedPeriods)
        {
            if (!_foldedPeriods.Add(period.PeriodIndex))
                continue;

            var observation = VolatilityObservation.TryFrom(period);
            if (observation is null)
                continue;

            _observations.Add(observation);
            _observationsSeen++;

            var range = observation.RealizedRangeTicks;
            MinimumRangeTicks = MinimumRangeTicks is null ? range : Math.Min(MinimumRangeTicks.Value, range);
            MaximumRangeTicks = MaximumRangeTicks is null ? range : Math.Max(MaximumRangeTicks.Value, range);

            while (_observations.Count > ObservationCapacity)
            {
                _observations.RemoveAt(0);
                _observationsDropped++;
            }
        }
    }

    /// <summary>
    /// Counts observations per bucket.
    ///
    /// Empty when nothing is registered. An unregistered axis reporting "3000 periods, all
    /// Unavailable" invites the reader to treat Unavailable as a fourth regime.
    /// </summary>
    public IReadOnlyDictionary<VolatilityRegimeBucket, int> CountsByBucket()
    {
        var counts = new Dictionary<VolatilityRegimeBucket, int>();
        if (!CanStratify)
            return counts;

        foreach (var observation in _observations)
        {
            var bucket = _boundaries.Classify(observation.RealizedRangeTicks);
            counts[bucket] = counts.TryGetValue(bucket, out var existing) ? existing + 1 : 1;
        }

        return counts;
    }

    private void Reset()
    {
        _observations.Clear();
        _foldedPeriods.Clear();
        _observationsSeen = 0;
        _observationsDropped = 0;
        MinimumRangeTicks = null;
        MaximumRangeTicks = null;
        _boundaries = VolatilityRegimeBoundaries.Unregistered;
    }
}
