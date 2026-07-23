using GC.AuctionFlow.Profile;

namespace GC.AuctionFlow.Tests.Unit.Profile;

/// <summary>
/// Test-only independent Classic TPO oracle. Does not call ClassicTpoEngine, PocSelector,
/// ValueAreaCalculator, or reuse engine mutable period structures.
/// Matching the engine proves internal consistency — not ATAS built-in parity.
/// </summary>
public static class ClassicTpoOracle
{
    public static SortedDictionary<long, int> BuildSortedCounts(
        string auctionId,
        DateTime auctionStartUtc,
        DateTime auctionEndUtc,
        IReadOnlyList<ProfileBarObservation> bars,
        PrimaryAuctionClock clock,
        PriceGrid grid)
    {
        // period -> unique ticks (independent HashSet instances)
        var byPeriod = new Dictionary<int, HashSet<long>>();

        foreach (var bar in bars)
        {
            if (bar.StartUtc.UtcDateTime < auctionStartUtc || bar.StartUtc.UtcDateTime >= auctionEndUtc)
                continue;

            var point = clock.Resolve(bar.StartUtc);
            if (!string.Equals(point.AuctionId, auctionId, StringComparison.Ordinal))
                continue;

            if (!grid.TryToTickIndex(bar.High, out var high) || !grid.TryToTickIndex(bar.Low, out var low))
                continue;

            if (high < low)
                (low, high) = (high, low);

            if (!byPeriod.TryGetValue(point.TpoPeriodIndex, out var set))
            {
                set = new HashSet<long>();
                byPeriod[point.TpoPeriodIndex] = set;
            }

            for (var t = low; t <= high; t++)
                set.Add(t);
        }

        var counts = new SortedDictionary<long, int>();
        foreach (var set in byPeriod.Values)
        {
            foreach (var tick in set)
            {
                counts.TryGetValue(tick, out var c);
                counts[tick] = c + 1;
            }
        }

        return counts;
    }

    public static IReadOnlyList<decimal> MaxCountPrices(SortedDictionary<long, int> counts, PriceGrid grid)
    {
        if (counts.Count == 0)
            return Array.Empty<decimal>();
        var max = counts.Values.Max();
        return counts.Where(kv => kv.Value == max).Select(kv => grid.ToPrice(kv.Key)).ToArray();
    }

    public static bool DistributionsEqual(
        IReadOnlyDictionary<long, int> engine,
        SortedDictionary<long, int> oracle)
    {
        if (engine.Count != oracle.Count)
            return false;
        foreach (var kv in oracle)
        {
            if (!engine.TryGetValue(kv.Key, out var c) || c != kv.Value)
                return false;
        }

        return true;
    }
}
