namespace GC.AuctionFlow.Profile;

public sealed class TpoProfileSnapshot
{
    public const string SnapshotVersion = "1.0.1";

    /// <summary>
    /// Production policy: developing TPO period IS included in letter counts used for
    /// TPO POC and TPO Value Area. Documented for ATAS parity comparison — do not mix
    /// silently with completed-only benchmarks.
    /// </summary>
    public const bool DevelopingPeriodIncludedInPocAndVa = true;

    public TpoProfileSnapshot(
        string auctionId,
        DateTime startUtc,
        DateTime endUtc,
        string anchorTimezone,
        TimeSpan anchorLocalTime,
        int periodMinutes,
        decimal? profileHigh,
        decimal? profileLow,
        decimal? tpoPoc,
        decimal? tpoVah,
        decimal? tpoVal,
        int totalTpoCount,
        int completedPeriodCount,
        int? developingPeriodIndex,
        IReadOnlyDictionary<long, int> priceLevelTpoCounts,
        ProfileDataQuality dataQuality,
        string provenance,
        IReadOnlyList<string> knownLimitations,
        TpoParityDiagnostic? parityDiagnostic = null)
    {
        AuctionId = auctionId;
        StartUtc = startUtc;
        EndUtc = endUtc;
        AnchorTimezone = anchorTimezone;
        AnchorLocalTime = anchorLocalTime;
        PeriodMinutes = periodMinutes;
        ProfileHigh = profileHigh;
        ProfileLow = profileLow;
        TpoPoc = tpoPoc;
        TpoVah = tpoVah;
        TpoVal = tpoVal;
        TotalTpoCount = totalTpoCount;
        CompletedPeriodCount = completedPeriodCount;
        DevelopingPeriodIndex = developingPeriodIndex;
        PriceLevelTpoCounts = priceLevelTpoCounts;
        DataQuality = dataQuality;
        Provenance = provenance;
        KnownLimitations = knownLimitations ?? Array.Empty<string>();
        ParityDiagnostic = parityDiagnostic;
    }

    public string AuctionId { get; }
    public DateTime StartUtc { get; }
    public DateTime EndUtc { get; }
    public string AnchorTimezone { get; }
    public TimeSpan AnchorLocalTime { get; }
    public int PeriodMinutes { get; }
    public decimal? ProfileHigh { get; }
    public decimal? ProfileLow { get; }
    public decimal? TpoPoc { get; }
    public decimal? TpoVah { get; }
    public decimal? TpoVal { get; }
    public int TotalTpoCount { get; }
    public int CompletedPeriodCount { get; }
    public int? DevelopingPeriodIndex { get; }
    public IReadOnlyDictionary<long, int> PriceLevelTpoCounts { get; }
    public ProfileDataQuality DataQuality { get; }
    public string Provenance { get; }
    public IReadOnlyList<string> KnownLimitations { get; }
    public TpoParityDiagnostic? ParityDiagnostic { get; }
    public string Version => SnapshotVersion;

    public TpoProfileSnapshot WithParityDiagnostic(TpoParityDiagnostic? diagnostic) =>
        new(
            AuctionId, StartUtc, EndUtc, AnchorTimezone, AnchorLocalTime, PeriodMinutes,
            ProfileHigh, ProfileLow, TpoPoc, TpoVah, TpoVal, TotalTpoCount, CompletedPeriodCount,
            DevelopingPeriodIndex, PriceLevelTpoCounts, DataQuality, Provenance, KnownLimitations, diagnostic);
}

/// <summary>
/// Classic TPO: each normalized price between period low/high receives at most one letter per TPO period.
/// Period ownership uses bar StartUtc only (not LastTime). Auction end exclusive. Developing period included in POC/VA.
/// </summary>
public static class ClassicTpoEngine
{
    public static TpoProfileSnapshot Build(
        string auctionId,
        DateTime auctionStartUtc,
        DateTime auctionEndUtc,
        IReadOnlyList<ProfileBarObservation> bars,
        PrimaryAuctionClock clock,
        PriceGrid grid,
        decimal valueAreaFraction,
        long? previousTpoPocTick,
        DateTimeOffset evaluationUtc,
        int lastProcessedBar = -1,
        decimal? parityReferencePrice = null,
        IReadOnlyDictionary<int, int>? barRevisionCounts = null)
    {
        var cfg = clock.Config;
        var etZone = cfg.TimeZone;
        var limitations = new List<string>
        {
            "OTF_NOT_IMPLEMENTED",
            "DEVELOPING_PERIOD_NOT_USED_FOR_OTF",
            "DEVELOPING_PERIOD_INCLUDED_IN_POC_AND_VA=true",
            "PERIOD_OWNERSHIP=BAR_START_UTC",
            "AUCTION_END_EXCLUSIVE",
            "POC_TIE_POLICY=" + PocSelector.TiePolicyId
        };

        if (bars.Count == 0)
        {
            return new TpoProfileSnapshot(
                auctionId, auctionStartUtc, auctionEndUtc,
                cfg.TimezoneId, cfg.AnchorLocalTime, cfg.PeriodMinutes,
                null, null, null, null, null,
                0, 0, null,
                new Dictionary<long, int>(),
                ProfileDataQuality.Unknown,
                "ClassicTpoEngine/empty",
                limitations);
        }

        var periodTouches = new Dictionary<int, HashSet<long>>();
        var periodCompleted = new Dictionary<int, bool>();
        var periodMeta = new Dictionary<int, AuctionClockPoint>();
        var periodBars = new Dictionary<int, List<ProfileBarObservation>>();
        var periodRejected = new Dictionary<int, List<string>>();
        var periodBoundaryBars = new Dictionary<int, List<int>>();
        var engineRejected = new List<string>();
        long? auctionHigh = null;
        long? auctionLow = null;

        foreach (var bar in bars.OrderBy(b => b.BarIndex))
        {
            if (bar.StartUtc.UtcDateTime < auctionStartUtc || bar.StartUtc.UtcDateTime >= auctionEndUtc)
                continue;

            var point = clock.Resolve(bar.StartUtc);
            if (!string.Equals(point.AuctionId, auctionId, StringComparison.Ordinal))
                continue;

            if (!periodBars.TryGetValue(point.TpoPeriodIndex, out var barList))
            {
                barList = new List<ProfileBarObservation>();
                periodBars[point.TpoPeriodIndex] = barList;
                periodMeta[point.TpoPeriodIndex] = point;
                periodRejected[point.TpoPeriodIndex] = new List<string>();
                periodBoundaryBars[point.TpoPeriodIndex] = new List<int>();
            }

            if (!grid.TryToTickIndex(bar.High, out var highTick) || !grid.TryToTickIndex(bar.Low, out var lowTick))
            {
                var reason = "BAR_PRICE_NOT_ALIGNABLE:" + bar.BarIndex;
                limitations.Add(reason);
                engineRejected.Add(reason);
                periodRejected[point.TpoPeriodIndex].Add(reason);
                continue;
            }

            if (highTick < lowTick)
                (lowTick, highTick) = (highTick, lowTick);

            auctionHigh = auctionHigh is null ? highTick : Math.Max(auctionHigh.Value, highTick);
            auctionLow = auctionLow is null ? lowTick : Math.Min(auctionLow.Value, lowTick);

            if (!periodTouches.TryGetValue(point.TpoPeriodIndex, out var set))
            {
                set = new HashSet<long>();
                periodTouches[point.TpoPeriodIndex] = set;
            }

            for (var t = lowTick; t <= highTick; t++)
                set.Add(t);

            barList.Add(bar);
            if (bar.StartUtc.UtcDateTime == point.TpoPeriodStartUtc
                || bar.StartUtc.UtcDateTime == point.TpoPeriodEndUtc)
                periodBoundaryBars[point.TpoPeriodIndex].Add(bar.BarIndex);

            var completed = evaluationUtc.UtcDateTime >= point.TpoPeriodEndUtc;
            periodCompleted[point.TpoPeriodIndex] = completed;
        }

        var counts = AggregateCounts(periodTouches, developingOnlyExclude: null);

        int? developingIndex = null;
        var completedCount = 0;
        foreach (var kv in periodCompleted)
        {
            if (kv.Value) completedCount++;
            else developingIndex = developingIndex is null ? kv.Key : Math.Max(developingIndex.Value, kv.Key);
        }

        if (periodTouches.Count > 0)
        {
            var maxPeriod = periodTouches.Keys.Max();
            if (!periodCompleted.TryGetValue(maxPeriod, out var done) || !done)
                developingIndex = maxPeriod;
        }

        var emptySlots = 0;
        if (periodTouches.Count > 0)
        {
            var minP = periodTouches.Keys.Min();
            var maxP = periodTouches.Keys.Max();
            for (var p = minP; p <= maxP; p++)
            {
                if (!periodTouches.ContainsKey(p))
                    emptySlots++;
            }
        }

        decimal? poc = null, vah = null, val = null, hi = null, lo = null;
        PocSelectionResult? pocSel = null;
        if (counts.Count > 0)
        {
            pocSel = PocSelector.SelectDetailed(
                counts.ToDictionary(kv => kv.Key, kv => (decimal)kv.Value),
                previousTpoPocTick);
            poc = grid.ToPrice(pocSel.PocTick);
            var va = ValueAreaCalculator.Calculate(
                counts.ToDictionary(kv => kv.Key, kv => (decimal)kv.Value),
                pocSel.PocTick,
                valueAreaFraction,
                grid);
            vah = va.Vah;
            val = va.Val;
            hi = grid.ToPrice(auctionHigh!.Value);
            lo = grid.ToPrice(auctionLow!.Value);
        }

        decimal? completedOnlyPoc = null;
        var completedOnlyMax = 0;
        IReadOnlyList<decimal> completedOnlyTies = Array.Empty<decimal>();
        if (developingIndex is int devEx)
        {
            var completedCounts = AggregateCounts(periodTouches, developingOnlyExclude: devEx);
            if (completedCounts.Count > 0)
            {
                var cSel = PocSelector.SelectDetailed(
                    completedCounts.ToDictionary(kv => kv.Key, kv => (decimal)kv.Value),
                    previousTpoPocTick);
                completedOnlyPoc = grid.ToPrice(cSel.PocTick);
                completedOnlyMax = (int)cSel.MaxCount;
                completedOnlyTies = cSel.TiedTicks.Select(grid.ToPrice).ToArray();
            }
        }
        else if (counts.Count > 0 && pocSel is not null)
        {
            completedOnlyPoc = poc;
            completedOnlyMax = (int)pocSel.MaxCount;
            completedOnlyTies = pocSel.TiedTicks.Select(grid.ToPrice).ToArray();
        }

        var tickToPeriods = BuildTickToPeriods(periodTouches);
        var periodDiags = new List<TpoPeriodDiagnostic>();
        foreach (var idx in periodBars.Keys.OrderBy(i => i))
        {
            var meta = periodMeta[idx];
            periodTouches.TryGetValue(idx, out var set);
            set ??= new HashSet<long>();
            var isDev = developingIndex == idx;
            var isDone = periodCompleted.TryGetValue(idx, out var c) && c;
            var contrib = periodBars[idx].OrderBy(b => b.BarIndex).ToList();
            var starts = contrib.Select(b => b.StartUtc).ToList();
            var dupStarts = starts.GroupBy(s => s.UtcDateTime).Count(g => g.Count() > 1);
            var missing = TpoM5SlotForensics.CountMissingSlots(
                meta.TpoPeriodStartUtc, meta.TpoPeriodEndUtc, evaluationUtc.UtcDateTime, isDone, starts);
            var revisions = 0;
            if (barRevisionCounts is not null)
            {
                foreach (var b in contrib)
                {
                    if (barRevisionCounts.TryGetValue(b.BarIndex, out var rc))
                        revisions += rc;
                }
            }

            periodDiags.Add(new TpoPeriodDiagnostic(
                idx,
                meta.TpoPeriodStartUtc,
                meta.TpoPeriodEndUtc,
                TimeZoneInfo.ConvertTimeFromUtc(meta.TpoPeriodStartUtc, etZone),
                TimeZoneInfo.ConvertTimeFromUtc(meta.TpoPeriodEndUtc, etZone),
                isDone,
                isDev,
                contrib.Count > 0 ? contrib[0].BarIndex : null,
                contrib.Count > 0 ? contrib[^1].BarIndex : null,
                set.Count > 0 ? set.Min() : null,
                set.Count > 0 ? set.Max() : null,
                set.Count,
                revisions,
                periodBoundaryBars[idx].Count > 0,
                contrib.Count,
                contrib.Select(b => b.BarIndex).Distinct().OrderBy(i => i).ToArray(),
                contrib.Count > 0 ? contrib[0].StartUtc : null,
                contrib.Count > 0 ? contrib[^1].StartUtc : null,
                set.Count > 0 ? grid.ToPrice(set.Min()) : null,
                set.Count > 0 ? grid.ToPrice(set.Max()) : null,
                dupStarts,
                missing,
                periodRejected[idx].ToArray(),
                periodBoundaryBars[idx].Distinct().OrderBy(i => i).ToArray()));
        }

        var sorted = counts.OrderBy(kv => kv.Key).Select(kv => (kv.Key, kv.Value)).ToArray();
        var maxCount = counts.Count == 0 ? 0 : counts.Values.Max();
        var maxTies = counts.Where(kv => kv.Value == maxCount).Select(kv => grid.ToPrice(kv.Key)).OrderBy(p => p).ToArray();
        decimal? midPrice = auctionHigh is long ah && auctionLow is long al
            ? (grid.ToPrice(ah) + grid.ToPrice(al)) / 2m
            : null;

        TpoTargetPriceDiagnostic? selectedTarget = null;
        if (poc is decimal selectedPx)
        {
            selectedTarget = TpoTargetPriceDiagnosticFactory.TryBuild(
                "SELECTED_POC", selectedPx, grid, counts, tickToPeriods, developingIndex, val, vah, maxCount);
        }

        TpoTargetPriceDiagnostic? referenceTarget = null;
        if (parityReferencePrice is decimal refPx)
        {
            referenceTarget = TpoTargetPriceDiagnosticFactory.TryBuild(
                "REFERENCE", refPx, grid, counts, tickToPeriods, developingIndex, val, vah, maxCount);
        }

        var diag = new TpoParityDiagnostic(
            auctionId,
            auctionStartUtc,
            auctionEndUtc,
            grid.TickSize,
            lastProcessedBar >= 0 ? lastProcessedBar : bars.Max(b => b.BarIndex),
            AtasTimestampNormalizer.PolicyVersion,
            TpoProfileSnapshot.DevelopingPeriodIncludedInPocAndVa,
            developingIndex,
            completedCount,
            emptySlots,
            periodTouches.Count,
            counts.Values.Sum(),
            maxCount,
            maxTies,
            midPrice,
            previousTpoPocTick is long ppt ? grid.ToPrice(ppt) : null,
            poc,
            pocSel?.ResolutionStep ?? "NONE",
            completedOnlyPoc,
            completedOnlyMax,
            completedOnlyTies,
            periodDiags,
            sorted,
            selectedTarget,
            referenceTarget,
            ledgerAudit: null,
            operatorReferencePrice: parityReferencePrice);

        var totalTpo = counts.Values.Sum();
        return new TpoProfileSnapshot(
            auctionId, auctionStartUtc, auctionEndUtc,
            cfg.TimezoneId, cfg.AnchorLocalTime, cfg.PeriodMinutes,
            hi, lo, poc, vah, val,
            totalTpo, completedCount, developingIndex,
            counts,
            counts.Count > 0 ? ProfileDataQuality.Complete : ProfileDataQuality.Unknown,
            "ClassicTpoEngine/v1",
            limitations.Distinct(StringComparer.Ordinal).ToArray(),
            diag);
    }

    private static Dictionary<long, IReadOnlyList<int>> BuildTickToPeriods(Dictionary<int, HashSet<long>> periodTouches)
    {
        var map = new Dictionary<long, List<int>>();
        foreach (var kv in periodTouches.OrderBy(k => k.Key))
        {
            foreach (var tick in kv.Value)
            {
                if (!map.TryGetValue(tick, out var list))
                {
                    list = new List<int>();
                    map[tick] = list;
                }

                list.Add(kv.Key);
            }
        }

        return map.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<int>)kv.Value);
    }

    private static Dictionary<long, int> AggregateCounts(
        Dictionary<int, HashSet<long>> periodTouches,
        int? developingOnlyExclude)
    {
        var counts = new Dictionary<long, int>();
        foreach (var kv in periodTouches)
        {
            if (developingOnlyExclude is int ex && kv.Key == ex)
                continue;
            foreach (var tick in kv.Value)
            {
                counts.TryGetValue(tick, out var c);
                counts[tick] = c + 1;
            }
        }

        return counts;
    }
}
