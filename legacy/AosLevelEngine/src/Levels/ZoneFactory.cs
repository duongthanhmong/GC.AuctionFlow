using Aos.LevelEngine.Profiles;

namespace Aos.LevelEngine.Levels;

public static class ZoneFactory
{
    public static FrozenZone Create(
        InstrumentProfile profile,
        LevelTypeKind kind,
        LevelBatch batch,
        decimal sourcePrice,
        DateTime frozenAtExchange,
        DateTime frozenAtUtc,
        string? sourceSession)
    {
        var mid = profile.NormalizeToTick(sourcePrice);
        var width = profile.WidthTicksFor(kind);
        var tick = profile.TickSize;
        var lowerTicks = width / 2;
        var upperTicks = width - lowerTicks;
        var lower = mid - lowerTicks * tick;
        var upper = mid + upperTicks * tick;
        return new FrozenZone(
            Guid.NewGuid(),
            kind,
            batch,
            InstrumentProfile.FamilyFor(kind),
            sourceSession,
            mid,
            mid,
            lower,
            upper,
            width,
            frozenAtExchange,
            frozenAtUtc,
            InstrumentProfile.StructuralFor(kind),
            profile.LevelEngine.ExhaustedPolicy);
    }
}

public static class VolumeProfile
{
    public readonly record struct ValueArea(decimal Poc, decimal Vah, decimal Val, long TotalVolume);

    public static ValueArea? Compute(
        IReadOnlyDictionary<decimal, long> volumeByPrice,
        InstrumentProfile profile)
    {
        if (volumeByPrice.Count == 0) return null;
        if (profile.LevelEngine.ValueAreaAlgorithm != ValueAreaAlgorithm.EXPAND_FROM_POC_V1)
            throw new InvalidOperationException($"Unsupported ValueAreaAlgorithm: {profile.LevelEngine.ValueAreaAlgorithm}");

        // seed value, subject to sensitivity test (LevelEngine.ValueAreaVolumePercent)
        var valueAreaPercent = profile.LevelEngine.ValueAreaVolumePercent;
        if (valueAreaPercent is <= 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(valueAreaPercent));

        var levels = volumeByPrice
            .Where(kv => kv.Value > 0)
            .Select(kv => (Price: profile.NormalizeToTick(kv.Key), Vol: kv.Value))
            .GroupBy(x => x.Price)
            .Select(g => (Price: g.Key, Vol: g.Sum(x => x.Vol)))
            .OrderBy(x => x.Price)
            .ToList();
        if (levels.Count == 0) return null;

        var total = levels.Sum(x => x.Vol);
        var maxVol = levels.Max(x => x.Vol);
        var candidates = levels.Select((x, i) => (x, i)).Where(t => t.x.Vol == maxVol).ToList();
        var pocIdx = profile.LevelEngine.PocTieBreak switch
        {
            PocTieBreak.LOWEST_PRICE => candidates.MinBy(t => t.x.Price).i,
            PocTieBreak.HIGHEST_PRICE => candidates.MaxBy(t => t.x.Price).i,
            _ => throw new InvalidOperationException("Unknown PocTieBreak")
        };

        var target = (long)Math.Ceiling(total * (decimal)valueAreaPercent);
        var cum = levels[pocIdx].Vol;
        var lo = pocIdx;
        var hi = pocIdx;
        while (cum < target && (lo > 0 || hi < levels.Count - 1))
        {
            var volBelow = lo > 0 ? levels[lo - 1].Vol : -1;
            var volAbove = hi < levels.Count - 1 ? levels[hi + 1].Vol : -1;
            if (volAbove >= volBelow) { hi++; cum += levels[hi].Vol; }
            else { lo--; cum += levels[lo].Vol; }
        }

        return new ValueArea(levels[pocIdx].Price, levels[hi].Price, levels[lo].Price, total);
    }
}

public static class ClusterBuilder
{
    public static IReadOnlyList<LevelCluster> AssignClusters(
        IReadOnlyList<FrozenZone> zones,
        InstrumentProfile profile)
    {
        var tick = profile.TickSize;
        // seed value, subject to sensitivity test (Zones.ClusterGapTicks)
        var gap = profile.Zones.ClusterGapTicks * tick;
        // seed value, subject to sensitivity test (Zones.ClusterMaxTicks)
        var maxTicks = profile.Zones.ClusterMaxTicks;

        var ordered = zones.OrderBy(z => z.ZoneLower).ToList();
        var clusters = new List<LevelCluster>();
        var i = 0;
        while (i < ordered.Count)
        {
            var members = new List<FrozenZone> { ordered[i] };
            var upper = ordered[i].ZoneUpper;
            var j = i + 1;
            while (j < ordered.Count && ordered[j].ZoneLower - upper <= gap)
            {
                members.Add(ordered[j]);
                if (ordered[j].ZoneUpper > upper) upper = ordered[j].ZoneUpper;
                j++;
            }

            var lower = members.Min(m => m.ZoneLower);
            var widthTicks = (int)decimal.Round((upper - lower) / tick, 0, MidpointRounding.AwayFromZero);
            var overwide = widthTicks > maxTicks;
            var id = Guid.NewGuid();
            foreach (var m in members)
                m.AssignCluster(id, overwide);

            // Confluence = distinct IndependentSourceFamily in cluster
            var families = members.Select(m => m.SourceFamily).Distinct().Count();
            foreach (var m in members)
                m.SetConfluenceCount(families);

            clusters.Add(new LevelCluster
            {
                ClusterId = id,
                MemberLevelIds = members.Select(m => m.LevelId).ToList(),
                ClusterLower = lower,
                ClusterUpper = upper,
                WidthTicks = widthTicks,
                IsOverwide = overwide
            });
            i = j;
        }

        return clusters;
    }
}

/// <summary>nPOC active only if no trade with TimestampExchange ever landed in its zone since formation.</summary>
public static class NpocActivity
{
    public static bool RemainsActive(
        decimal zoneLower,
        decimal zoneUpper,
        IEnumerable<(DateTime TimestampExchange, decimal Price)> tradesSinceFormation) =>
        !tradesSinceFormation.Any(t => t.Price >= zoneLower && t.Price <= zoneUpper);
}
