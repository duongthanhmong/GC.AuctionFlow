using GC.AuctionFlow.Profile;

namespace GC.AuctionFlow.Research;

/// <summary>
/// Accumulates per-period participation measures and classifies them against registered
/// boundaries.
///
/// Stateful across rebuilds, like the volatility host and the episode dataset. It
/// publishes the observations and which clock buckets still lack a registration; it never
/// publishes a percentile, because producing one would be the DLL choosing the threshold
/// under another name.
/// </summary>
public sealed class ParticipationRegimeHost
{
    /// <summary>Observations retained. A memory bound, not a sample-size judgement.</summary>
    public const int ObservationCapacity = 4096;

    private readonly List<ParticipationObservation> _observations = new();
    private readonly HashSet<int> _foldedPeriods = new();
    private readonly HashSet<int> _observedClockBuckets = new();

    private bool _enabled;
    private int _observationsSeen;
    private int _observationsDropped;
    private ParticipationRegimeBoundaries _boundaries = ParticipationRegimeBoundaries.Unregistered;

    public ParticipationRegimeHost(bool enabled = false)
    {
        _enabled = enabled;
    }

    public ParticipationRegimeBoundaries Boundaries => _boundaries;

    public IReadOnlyList<ParticipationObservation> Observations => _observations;

    /// <summary>Every period ever folded, independent of how many are retained.</summary>
    public int ObservationsSeen => _observationsSeen;

    public int ObservationsDroppedToCapacity => _observationsDropped;

    /// <summary>
    /// Clock buckets that have been observed but have no registered boundaries.
    ///
    /// This is the actionable list. An axis that just says "not calibrated" leaves nobody
    /// knowing what to do; this names exactly which slots of the auction clock still need a
    /// decision.
    /// </summary>
    public IReadOnlyList<int> UnregisteredClockBuckets =>
        _observedClockBuckets.Where(b => !_boundaries.IsRegistered(b)).OrderBy(b => b).ToArray();

    /// <summary>
    /// True only when every observed clock bucket is registered.
    ///
    /// Partial registration is not enough. Stratifying some periods and leaving the rest
    /// Unavailable produces a distribution over a silently self-selected subset, which is
    /// worse than no distribution because it looks like one.
    /// </summary>
    public bool CanStratify => _observations.Count > 0 && UnregisteredClockBuckets.Count == 0;

    public void Configure(bool enabled)
    {
        if (_enabled == enabled)
            return;

        _enabled = enabled;
        if (!enabled)
            Reset();
    }

    public void RegisterBoundaries(ParticipationRegimeBoundaries boundaries) =>
        _boundaries = boundaries ?? ParticipationRegimeBoundaries.Unregistered;

    /// <summary>
    /// Folds newly completed periods.
    ///
    /// Volume comes from the volume profile restricted to the period's price range, which
    /// is the closest the profile gets to per-period executed volume without a trade feed.
    /// Trade count stays null unless the feed reports it — §10.4 lists it as a feature, and
    /// substituting zero would make every period look like it had no trades.
    /// </summary>
    public void Rebuild(
        IReadOnlyList<CompletedTpoPeriodSnapshot>? completedPeriods,
        VolumeProfileSnapshot? volumeProfile)
    {
        if (!_enabled || completedPeriods is null || completedPeriods.Count == 0)
            return;

        foreach (var period in completedPeriods)
        {
            if (!_foldedPeriods.Add(period.PeriodIndex))
                continue;

            var range = period.PeriodHighTick - period.PeriodLowTick;
            if (range < 0)
                continue;

            decimal volume = 0m;

            // The volume profile keys executed volume by tick and carries no per-level
            // trade count, so trade count stays null. §10.4 lists it as a feature and it is
            // simply not observable at this layer; substituting zero would make every
            // period look like it had no trades.
            int? tradeCount = null;

            if (volumeProfile?.PriceLevelVolumes is { Count: > 0 } levels)
            {
                foreach (var (priceTick, executed) in levels)
                {
                    if (priceTick < period.PeriodLowTick || priceTick > period.PeriodHighTick)
                        continue;

                    volume += executed;
                }
            }

            _observations.Add(new ParticipationObservation(
                period.PeriodIndex,
                clockBucket: period.PeriodIndex,
                period.PeriodEndUtc,
                volume,
                tradeCount,
                range,
                period.PeriodEndUtc - period.PeriodStartUtc));

            _observedClockBuckets.Add(period.PeriodIndex);
            _observationsSeen++;

            while (_observations.Count > ObservationCapacity)
            {
                _observations.RemoveAt(0);
                _observationsDropped++;
            }
        }
    }

    /// <summary>
    /// Counts observations per bucket. Empty unless every observed clock bucket is
    /// registered — see <see cref="CanStratify"/>.
    /// </summary>
    public IReadOnlyDictionary<ParticipationRegimeBucket, int> CountsByBucket()
    {
        var counts = new Dictionary<ParticipationRegimeBucket, int>();
        if (!CanStratify)
            return counts;

        foreach (var observation in _observations)
        {
            var bucket = _boundaries.Classify(observation);
            counts[bucket] = counts.TryGetValue(bucket, out var existing) ? existing + 1 : 1;
        }

        return counts;
    }

    private void Reset()
    {
        _observations.Clear();
        _foldedPeriods.Clear();
        _observedClockBuckets.Clear();
        _observationsSeen = 0;
        _observationsDropped = 0;
        _boundaries = ParticipationRegimeBoundaries.Unregistered;
    }
}
