namespace GC.AuctionFlow.Foundation;

/// <summary>Deterministic, directly-measured input-integrity counts for one capture epoch.</summary>
public readonly record struct InputIntegrityCounts(
    long Accepted,
    long Duplicates,
    long OutOfOrder,
    long LateEvents,
    long NonDeduplicable,
    long MeasuredIdentityGaps,
    DateTime? LastValidEventTimeUtc)
{
    public static InputIntegrityCounts Empty => new(0, 0, 0, 0, 0, 0, null);
}

/// <summary>
/// TTS §4.2/§5.5: deterministic ordering, safe deduplication, and honest integrity counting for a
/// single capture epoch. What it reports is *directly measured* — it never calls a discontinuity in a
/// process-local counter an exchange-feed gap (audit correction / MRBS §18). It does not order events
/// for the caller by mutating them; it decides, per arrival, accept / duplicate / out-of-order / late,
/// and whether the event could be safely deduplicated at all.
/// </summary>
public sealed class InputIntegrityMeter
{
    private readonly HashSet<string> _seenIdentities = new(StringComparer.Ordinal);
    private DateTime _maxEventTimeUtc = DateTime.MinValue;
    private DateTime? _lastAcceptedEventTimeUtc;
    private long _accepted, _duplicates, _outOfOrder, _late, _nonDedup, _measuredGaps;

    /// <summary>The verdict for one observed event.</summary>
    public enum Verdict
    {
        Accepted = 0,
        DuplicateRejected = 1,
        AcceptedOutOfOrder = 2,
        AcceptedLateRevision = 3,
        AcceptedNonDeduplicable = 4,
    }

    /// <summary>
    /// Observe one canonical event. Deterministic: the same stream in the same order yields the same
    /// verdict sequence and the same counts, with no wall-clock or thread input.
    /// </summary>
    public Verdict Observe(in CanonicalInputEvent e, long lateGraceTicks = 0)
    {
        var evt = e.EventTimeUtc.ToUniversalTime();

        // Cannot be safely deduplicated: count it, accept it, but this alone must keep the epoch
        // below AnalysisReady (the host enforces that via NonDeduplicable > 0).
        if (e.Dedup == DedupCapability.NotDeduplicable)
        {
            _nonDedup++;
            _accepted++;
            Advance(evt);
            return Verdict.AcceptedNonDeduplicable;
        }

        var key = e.IdentityKey();

        // Native stable id => a repeat is a true duplicate and is rejected. A composite key must NOT
        // collapse legitimately identical prints: only a NativeStableId repeat is rejected here.
        if (e.Dedup == DedupCapability.NativeStableId && !_seenIdentities.Add(key))
        {
            _duplicates++;
            return Verdict.DuplicateRejected;
        }
        if (e.Dedup == DedupCapability.CompositeKey)
        {
            // Record the key for gap/telemetry but never reject on collision — identical prints are legal.
            _seenIdentities.Add(key);
        }

        _accepted++;

        // Ordering relative to the running max event time. Ties (equal event time) are NOT out-of-order;
        // deterministic tie-break is by (priceTicks, callback-local order) and is the caller's stable sort key.
        if (evt < _maxEventTimeUtc)
        {
            var behindTicks = (_maxEventTimeUtc - evt).Ticks;
            if (behindTicks > lateGraceTicks)
            {
                _late++;
                Advance(evt);
                return Verdict.AcceptedLateRevision;
            }
            _outOfOrder++;
            Advance(evt);
            return Verdict.AcceptedOutOfOrder;
        }

        Advance(evt);
        return Verdict.Accepted;
    }

    /// <summary>
    /// Record a directly-observed identity gap — a break in a *native, verified-stable* monotonic id
    /// (not a process-local counter). Callers pass only breaks they can actually observe on the feed.
    /// </summary>
    public void RecordMeasuredIdentityGap() => _measuredGaps++;

    private void Advance(DateTime evt)
    {
        if (evt > _maxEventTimeUtc) _maxEventTimeUtc = evt;
        _lastAcceptedEventTimeUtc = evt;
    }

    /// <summary>Deterministic tie-break key for events sharing an event time. Stable and total.</summary>
    public static (long, long, long, long) OrderingKey(in CanonicalInputEvent e) =>
        (e.EventTimeUtc.ToUniversalTime().Ticks, e.PriceTicks, (long)e.Quantity, e.CallbackLocalOrder);

    public InputIntegrityCounts Snapshot() => new(
        _accepted, _duplicates, _outOfOrder, _late, _nonDedup, _measuredGaps, _lastAcceptedEventTimeUtc);

    /// <summary>Reset for a new capture epoch (reconnect / rebuild). Keeps counting honest per-epoch.</summary>
    public void ResetForNewEpoch()
    {
        _seenIdentities.Clear();
        _maxEventTimeUtc = DateTime.MinValue;
        _lastAcceptedEventTimeUtc = null;
        _accepted = _duplicates = _outOfOrder = _late = _nonDedup = _measuredGaps = 0;
    }
}
