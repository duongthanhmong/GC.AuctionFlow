namespace GC.AuctionFlow.Research;

/// <summary>
/// Participation regime bucket. The four outputs of v1.2 §10.4.
/// </summary>
public enum ParticipationRegimeBucket
{
    /// <summary>No boundaries registered for this observation's clock bucket.</summary>
    Unavailable = 0,
    Normal = 1,
    Reduced = 2,
    Thin = 3,
    Dislocated = 4
}

/// <summary>
/// One completed period's raw participation measures.
///
/// v1.2 §10.4 lists volume, trade count, range, depth, spread, executed-volume density and
/// duration. Depth and spread need a bid/ask feed; when it is absent they are null rather
/// than zero, because zero spread is a claim and no spread is an absence.
///
/// The clock bucket is carried on every observation because §10.4 asks for volume
/// percentile *by clock bucket*. An overnight period and a cash-session period are not
/// comparable on volume, and pooling them would produce a distribution whose shape is
/// mostly the session calendar.
/// </summary>
public sealed class ParticipationObservation
{
    public ParticipationObservation(
        int periodIndex,
        int clockBucket,
        DateTime periodEndUtc,
        decimal executedVolume,
        int? tradeCount,
        long rangeTicks,
        TimeSpan duration)
    {
        PeriodIndex = periodIndex;
        ClockBucket = clockBucket;
        PeriodEndUtc = periodEndUtc;
        ExecutedVolume = executedVolume;
        TradeCount = tradeCount;
        RangeTicks = rangeTicks;
        Duration = duration;
    }

    public int PeriodIndex { get; }

    /// <summary>
    /// Which slot of the auction clock this period occupies. The engine's own TPO period
    /// index, so no new notion of time is invented.
    /// </summary>
    public int ClockBucket { get; }

    public DateTime PeriodEndUtc { get; }
    public decimal ExecutedVolume { get; }

    /// <summary>Null when the feed reported no per-trade counts.</summary>
    public int? TradeCount { get; }

    public long RangeTicks { get; }
    public TimeSpan Duration { get; }

    /// <summary>
    /// Executed volume per tick of range. Null when the period did not move: dividing by a
    /// zero range would report infinite density for a period that simply stood still.
    /// </summary>
    public decimal? ExecutedVolumeDensity =>
        RangeTicks > 0 ? ExecutedVolume / RangeTicks : null;
}

/// <summary>
/// Participation boundaries, registered per clock bucket from outside the build.
///
/// Same reasoning as the volatility axis: boundaries derived from the observed sample
/// would move as it grew, re-choosing sample criteria after seeing results
/// (`G-FAST-001`), restratifying past periods retroactively, and making `G-CAL-002` step 4
/// impossible. The existing production limitation says the same thing in fewer words —
/// `THIN_PARTICIPATION_PERCENTILE_THRESHOLDS_NOT_CALIBRATED`.
///
/// Registration is per clock bucket, not global, because §10.4 requires it. A single
/// global triple would be simpler and would mean an overnight period and a cash-session
/// period are judged against the same volume, which is the error the clause exists to
/// prevent.
/// </summary>
public sealed class ParticipationRegimeBoundaries
{
    private readonly Dictionary<int, (decimal Thin, decimal Reduced, decimal Dislocated)> _byClockBucket;

    private ParticipationRegimeBoundaries(
        Dictionary<int, (decimal, decimal, decimal)> byClockBucket,
        string decisionLogReference)
    {
        _byClockBucket = byClockBucket;
        DecisionLogReference = decisionLogReference;
    }

    /// <summary>
    /// Where the decision is recorded. Required — see `G-CAL-002` step 6.
    /// </summary>
    public string DecisionLogReference { get; }

    public IReadOnlyCollection<int> RegisteredClockBuckets => _byClockBucket.Keys;

    /// <summary>Nothing registered.</summary>
    public static ParticipationRegimeBoundaries Unregistered { get; } =
        new(new Dictionary<int, (decimal, decimal, decimal)>(), "");

    /// <summary>
    /// Registers one clock bucket.
    /// </summary>
    /// <param name="thinBelow">Executed volume below this is Thin.</param>
    /// <param name="reducedBelow">Executed volume below this, but not Thin, is Reduced.</param>
    /// <param name="dislocatedAtOrAbove">
    /// Executed volume at or above this is Dislocated. §10.4 treats dislocation as its own
    /// regime rather than the top of Normal: an event-driven period is not a busy normal
    /// one, and a thin overnight auction can still carry real information after a shock.
    /// </param>
    public ParticipationRegimeBoundaries Register(
        int clockBucket,
        decimal thinBelow,
        decimal reducedBelow,
        decimal dislocatedAtOrAbove,
        string decisionLogReference)
    {
        if (string.IsNullOrWhiteSpace(decisionLogReference))
            return this;

        // Refused rather than reordered. Repairing a mis-registered triple would hide a
        // governance failure while still producing buckets somebody would trust.
        if (thinBelow <= 0m || thinBelow >= reducedBelow || reducedBelow >= dislocatedAtOrAbove)
            return this;

        var next = new Dictionary<int, (decimal, decimal, decimal)>(_byClockBucket)
        {
            [clockBucket] = (thinBelow, reducedBelow, dislocatedAtOrAbove),
        };

        return new ParticipationRegimeBoundaries(next, decisionLogReference.Trim());
    }

    public bool IsRegistered(int clockBucket) => _byClockBucket.ContainsKey(clockBucket);

    /// <summary>
    /// Places one observation.
    ///
    /// <see cref="ParticipationRegimeBucket.Unavailable"/> when this observation's clock
    /// bucket has no registration — never a fallback to another bucket's boundaries, which
    /// would judge an overnight period against cash-session volume.
    /// </summary>
    public ParticipationRegimeBucket Classify(ParticipationObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);

        if (!_byClockBucket.TryGetValue(observation.ClockBucket, out var bounds))
            return ParticipationRegimeBucket.Unavailable;

        var volume = observation.ExecutedVolume;

        if (volume >= bounds.Dislocated) return ParticipationRegimeBucket.Dislocated;
        if (volume < bounds.Thin) return ParticipationRegimeBucket.Thin;
        return volume < bounds.Reduced
            ? ParticipationRegimeBucket.Reduced
            : ParticipationRegimeBucket.Normal;
    }
}
