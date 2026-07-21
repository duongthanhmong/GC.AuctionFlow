namespace Aos.LevelEngine.Session;

/// <summary>
/// Builds SessionCoverageState map and EarliestUsableTradingDate from chart bar times.
/// </summary>
public sealed class SessionCoverageAnalyzer
{
    private readonly TradingDateResolver _resolver;

    public SessionCoverageAnalyzer(TradingDateResolver resolver) => _resolver = resolver;

    public sealed class Result
    {
        public required IReadOnlyDictionary<DateOnly, SessionCoverageState> ByTradingDate { get; init; }
        public DateOnly? EarliestUsableTradingDate { get; init; }
        public IReadOnlyList<DateOnly> CompletedRthSessions { get; init; } = Array.Empty<DateOnly>();
        public int CompletedOvernightSessionCount { get; init; }
    }

    /// <summary>
    /// <paramref name="barTimesUtc"/> — exchange times as UTC (SpecifyKind applied inside).
    /// Optional <paramref name="barIntervalMinutes"/> for expected RTH bar count (seed from chart TF; default 1 = M1).
    /// </summary>
    public Result Analyze(IReadOnlyList<DateTime> barTimesUtc, int barIntervalMinutes = 1)
    {
        // seed value, subject to sensitivity test — interval from chart TF
        if (barIntervalMinutes < 1) barIntervalMinutes = 1;

        var rthStart = _resolver.RthStartLocalTime;
        var rthEnd = _resolver.RthEndLocalTime;
        var expectedMinutes = (int)(rthEnd.ToTimeSpan() - rthStart.ToTimeSpan()).TotalMinutes;
        var expectedBars = Math.Max(1, expectedMinutes / barIntervalMinutes);
        var minBarsForComplete = Math.Max(1,
            expectedBars - (_resolver.PartialCoverageMaxMissingMinutes / barIntervalMinutes));

        // Count RTH bars per TradingDate
        var rthCounts = new Dictionary<DateOnly, int>();
        var overnightDates = new HashSet<DateOnly>();
        DateOnly? minTd = null, maxTd = null;

        foreach (var raw in barTimesUtc)
        {
            var utc = DateTime.SpecifyKind(raw, DateTimeKind.Utc);
            var td = _resolver.ResolveTradingDate(utc);
            if (minTd is null || td < minTd) minTd = td;
            if (maxTd is null || td > maxTd) maxTd = td;

            var local = TimeZoneInfo.ConvertTimeFromUtc(utc, _resolver.TradingTimeZone);
            var tod = TimeOnly.FromDateTime(local);
            if (tod >= rthStart && tod < rthEnd)
            {
                rthCounts.TryGetValue(td, out var n);
                rthCounts[td] = n + 1;
            }

            // Overnight belonging to TradingDate td: local in [rollover, 24) on prev cal day OR [0, rthStart) on td
            if (tod >= _resolver.TradingSessionRolloverLocalTime || tod < rthStart)
                overnightDates.Add(td);
        }

        if (minTd is null || maxTd is null)
        {
            return new Result
            {
                ByTradingDate = new Dictionary<DateOnly, SessionCoverageState>(),
                EarliestUsableTradingDate = null
            };
        }

        // First pass: COMPLETE / PARTIAL / provisional empty
        var provisional = new Dictionary<DateOnly, SessionCoverageState>();
        for (var d = minTd.Value; d <= maxTd.Value; d = d.AddDays(1))
        {
            rthCounts.TryGetValue(d, out var count);
            if (count >= minBarsForComplete)
                provisional[d] = SessionCoverageState.COMPLETE;
            else if (count > 0)
                provisional[d] = SessionCoverageState.PARTIAL_COVERAGE;
            else
                provisional[d] = SessionCoverageState.UNKNOWN_COVERAGE; // refine below
        }

        var completed = provisional
            .Where(kv => kv.Value == SessionCoverageState.COMPLETE)
            .Select(kv => kv.Key)
            .OrderBy(x => x)
            .ToList();

        DateOnly? earliestUsable = completed.Count > 0 ? completed[0] : null;

        // Second pass: empty days → NON_TRADING only if strictly between two COMPLETE; else UNKNOWN
        var final = new Dictionary<DateOnly, SessionCoverageState>();
        foreach (var (d, state) in provisional)
        {
            if (state != SessionCoverageState.UNKNOWN_COVERAGE)
            {
                final[d] = state;
                continue;
            }

            if (earliestUsable is not null && d < earliestUsable.Value)
            {
                final[d] = SessionCoverageState.UNKNOWN_COVERAGE;
                continue;
            }

            var hasCompleteBefore = completed.Any(c => c < d);
            var hasCompleteAfter = completed.Any(c => c > d);
            if (hasCompleteBefore && hasCompleteAfter)
                final[d] = SessionCoverageState.NON_TRADING_DAY;
            else
                final[d] = SessionCoverageState.UNKNOWN_COVERAGE;
        }

        // Overnight sessions: TradingDates with overnight bars that also have COMPLETE prior RTH
        // or overnight data toward a COMPLETE day — count distinct overnight TradingDates with data
        // where the overnight window is "completed" if we have bars and TradingDate's RTH is COMPLETE
        // or prior day COMPLETE. Simplified: count overnightDates ∩ (completed ∪ resolved with bars before RTH).
        var completedOvernight = overnightDates
            .Where(td =>
            {
                // Overnight for td is usable when we have overnight bars and td is COMPLETE or prior COMPLETE exists
                if (!overnightDates.Contains(td)) return false;
                return completed.Contains(td) || completed.Any(c => c == td.AddDays(-1) || c < td);
            })
            .Distinct()
            .Count();

        return new Result
        {
            ByTradingDate = final,
            EarliestUsableTradingDate = earliestUsable,
            CompletedRthSessions = completed,
            CompletedOvernightSessionCount = completedOvernight
        };
    }

    /// <summary>COMPLETE sessions only — Weekly/Composite/nPOC must use this set.</summary>
    public static bool IsUsableForMultiSession(SessionCoverageState state) =>
        state == SessionCoverageState.COMPLETE;
}
