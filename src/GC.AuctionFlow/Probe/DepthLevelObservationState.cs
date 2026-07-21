namespace GC.AuctionFlow.Probe;

/// <summary>
/// Diagnostic last-observation state by side+price. Not an order book.
/// Does not apply volume as absolute or delta; does not auto-remove on zero.
/// VolumeMeaning = Unknown; ZeroVolumeMeaning = Unknown.
/// </summary>
public sealed class DepthLevelObservationState
{
    private readonly int _maxLevels;
    private readonly Dictionary<(DepthSide Side, decimal Price), LevelEntry> _levels = new();
    private readonly object _gate = new();

    public DepthLevelObservationState(int maxLevels)
    {
        _maxLevels = maxLevels;
    }

    public const string VolumeMeaning = DomSemanticsProbeVersions.VolumeMeaning;
    public const string ZeroVolumeMeaning = DomSemanticsProbeVersions.ZeroVolumeMeaning;

    public void Observe(DepthObservation obs)
    {
        if (obs.DerivedSide == DepthSide.Unknown)
            return;

        lock (_gate)
        {
            var key = (obs.DerivedSide, obs.Price);
            if (!_levels.TryGetValue(key, out var entry))
            {
                if (_levels.Count >= _maxLevels)
                    return;
                entry = new LevelEntry();
                _levels[key] = entry;
            }

            entry.LastObservedVolume = obs.Volume;
            entry.LastSourceTimeTicks = obs.SourceTimeTicks;
            entry.LastCallbackSource = obs.CallbackSource;
            entry.ObservationCount++;
            // Zero volume: record only — do NOT remove the level.
        }
    }

    public DepthLevelObservationStateSummary Snapshot()
    {
        lock (_gate)
        {
            var bid = _levels.Where(kv => kv.Key.Side == DepthSide.Bid).ToList();
            var ask = _levels.Where(kv => kv.Key.Side == DepthSide.Ask).ToList();
            return new DepthLevelObservationStateSummary(
                VolumeMeaning: VolumeMeaning,
                ZeroVolumeMeaning: ZeroVolumeMeaning,
                StableBookReconstruction: false,
                BidLevelCount: bid.Count,
                AskLevelCount: ask.Count,
                BidUniquePrices: bid.Select(x => x.Key.Price).Distinct().Count(),
                AskUniquePrices: ask.Select(x => x.Key.Price).Distinct().Count(),
                BidMinPrice: bid.Count == 0 ? null : bid.Min(x => x.Key.Price),
                BidMaxPrice: bid.Count == 0 ? null : bid.Max(x => x.Key.Price),
                AskMinPrice: ask.Count == 0 ? null : ask.Min(x => x.Key.Price),
                AskMaxPrice: ask.Count == 0 ? null : ask.Max(x => x.Key.Price),
                TotalObservationRecords: _levels.Values.Sum(e => e.ObservationCount));
        }
    }

    private sealed class LevelEntry
    {
        public decimal LastObservedVolume;
        public long LastSourceTimeTicks;
        public DepthCallbackSource LastCallbackSource;
        public long ObservationCount;
    }
}

public sealed record DepthLevelObservationStateSummary(
    string VolumeMeaning,
    string ZeroVolumeMeaning,
    bool StableBookReconstruction,
    int BidLevelCount,
    int AskLevelCount,
    int BidUniquePrices,
    int AskUniquePrices,
    decimal? BidMinPrice,
    decimal? BidMaxPrice,
    decimal? AskMinPrice,
    decimal? AskMaxPrice,
    long TotalObservationRecords);
