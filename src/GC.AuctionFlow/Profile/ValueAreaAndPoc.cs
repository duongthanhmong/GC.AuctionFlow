namespace GC.AuctionFlow.Profile;

/// <summary>
/// POC tie policy (deterministic, independent of dictionary iteration order):
/// 1) closest to profile midpoint
/// 2) closest to previous published POC when available
/// 3) lower normalized tick index
/// </summary>
public static class PocSelector
{
    public const string TiePolicyId = "MIDPOINT_THEN_PREVPOC_THEN_LOWER_TICK_V1";

    public static long SelectPocTick(
        IReadOnlyDictionary<long, decimal> contributionsByTick,
        long? previousPocTick = null) =>
        SelectDetailed(contributionsByTick, previousPocTick).PocTick;

    public static PocSelectionResult SelectDetailed(
        IReadOnlyDictionary<long, decimal> contributionsByTick,
        long? previousPocTick = null)
    {
        if (contributionsByTick is null || contributionsByTick.Count == 0)
            throw new ArgumentException("No contributions for POC selection.");

        var max = contributionsByTick.Values.Max();
        var candidates = contributionsByTick
            .Where(kv => kv.Value == max)
            .Select(kv => kv.Key)
            .OrderBy(t => t)
            .ToList();

        var minTick = contributionsByTick.Keys.Min();
        var maxTick = contributionsByTick.Keys.Max();
        var mid = (minTick + maxTick) / 2.0m;

        if (candidates.Count == 1)
        {
            return new PocSelectionResult(candidates[0], max, candidates, mid, previousPocTick, "UNIQUE_MAX");
        }

        var byMid = candidates
            .OrderBy(t => Math.Abs(t - mid))
            .ThenBy(t => previousPocTick is long prev ? Math.Abs(t - prev) : 0L)
            .ThenBy(t => t)
            .ToList();

        var bestMidDist = Math.Abs(byMid[0] - mid);
        var midWinners = byMid.Where(t => Math.Abs(t - mid) == bestMidDist).OrderBy(t => t).ToList();
        if (midWinners.Count == 1)
            return new PocSelectionResult(midWinners[0], max, candidates, mid, previousPocTick, "MIDPOINT");

        if (previousPocTick is long prevPoc)
        {
            var byPrev = midWinners
                .OrderBy(t => Math.Abs(t - prevPoc))
                .ThenBy(t => t)
                .ToList();
            var bestPrevDist = Math.Abs(byPrev[0] - prevPoc);
            var prevWinners = byPrev.Where(t => Math.Abs(t - prevPoc) == bestPrevDist).OrderBy(t => t).ToList();
            if (prevWinners.Count == 1)
                return new PocSelectionResult(prevWinners[0], max, candidates, mid, previousPocTick, "PREV_POC");
            return new PocSelectionResult(prevWinners[0], max, candidates, mid, previousPocTick, "LOWER_TICK");
        }

        return new PocSelectionResult(midWinners[0], max, candidates, mid, previousPocTick, "LOWER_TICK");
    }
}

/// <summary>
/// Deterministic adjacent-expand Value Area from POC.
/// ValueAreaFraction default 0.70 is a conventional configurable default, not a predictive GC edge.
/// Expansion walks one normalized tick at a time; empty ticks contribute zero but keep adjacency.
/// </summary>
public static class ValueAreaCalculator
{
    public static ValueAreaResult Calculate(
        IReadOnlyDictionary<long, decimal> contributionsByTick,
        long pocTick,
        decimal valueAreaFraction,
        PriceGrid grid)
    {
        if (grid is null) throw new ArgumentNullException(nameof(grid));
        if (valueAreaFraction <= 0m || valueAreaFraction > 1m)
            throw new ArgumentOutOfRangeException(nameof(valueAreaFraction));
        if (contributionsByTick is null || contributionsByTick.Count == 0)
            throw new ArgumentException("No contributions for value area.");
        if (!contributionsByTick.ContainsKey(pocTick))
            throw new ArgumentException("POC tick missing from contributions.");

        var total = contributionsByTick.Values.Sum();
        if (total <= 0m)
            return new ValueAreaResult(grid.ToPrice(pocTick), grid.ToPrice(pocTick), pocTick, pocTick, 0m, total);

        var target = total * valueAreaFraction;
        var minTick = contributionsByTick.Keys.Min();
        var maxTick = contributionsByTick.Keys.Max();
        var low = pocTick;
        var high = pocTick;
        var cumulative = contributionsByTick[pocTick];

        while (cumulative < target && (low > minTick || high < maxTick))
        {
            var canBelow = low > minTick;
            var canAbove = high < maxTick;
            var below = canBelow && contributionsByTick.TryGetValue(low - 1, out var bv) ? bv : 0m;
            var above = canAbove && contributionsByTick.TryGetValue(high + 1, out var av) ? av : 0m;

            if (canBelow && canAbove && below == above)
            {
                low--;
                high++;
                cumulative += below + above;
            }
            else if (canAbove && (!canBelow || above > below))
            {
                high++;
                cumulative += above;
            }
            else if (canBelow)
            {
                low--;
                cumulative += below;
            }
            else
            {
                break;
            }
        }

        return new ValueAreaResult(
            grid.ToPrice(high),
            grid.ToPrice(low),
            high,
            low,
            cumulative,
            total);
    }
}

public sealed class ValueAreaResult
{
    public ValueAreaResult(decimal vah, decimal val, long vahTick, long valTick, decimal cumulative, decimal total)
    {
        Vah = vah;
        Val = val;
        VahTick = vahTick;
        ValTick = valTick;
        CumulativeContribution = cumulative;
        TotalContribution = total;
    }

    public decimal Vah { get; }
    public decimal Val { get; }
    public long VahTick { get; }
    public long ValTick { get; }
    public decimal CumulativeContribution { get; }
    public decimal TotalContribution { get; }
}
