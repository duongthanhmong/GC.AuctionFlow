using System.Globalization;
using System.Text;

namespace GC.AuctionFlow.Profile;

/// <summary>Required ATAS built-in TPO benchmark checklist. Operator must confirm before assigning defects to GCAE.</summary>
public static class TpoAtasBenchmarkChecklist
{
    public const string StatusUnconfirmed = "UNCONFIRMED";

    public static readonly IReadOnlyList<string> RequiredSettings = new[]
    {
        "Mode: Volume Profile & TPO",
        "External period: Custom",
        "Begin/end corresponding to 08:20 America/New_York",
        "Sub period: 30 minutes",
        "Prices per row: 1",
        "Value Area: 0.70",
        "Initial Balance: OFF",
        "Filter: OFF",
        "Same GCQ6 contract",
        "Same chart history",
        "Same current auction"
    };
}

/// <summary>Compresses sorted period indices: 5,6,7,8,11 → "5-8,11".</summary>
public static class PeriodIndexCompressor
{
    public static string Compress(IReadOnlyList<int> sortedDistinct)
    {
        if (sortedDistinct is null || sortedDistinct.Count == 0)
            return "—";

        var sb = new StringBuilder();
        var i = 0;
        while (i < sortedDistinct.Count)
        {
            var start = sortedDistinct[i];
            var end = start;
            var j = i + 1;
            while (j < sortedDistinct.Count && sortedDistinct[j] == end + 1)
            {
                end = sortedDistinct[j];
                j++;
            }

            if (sb.Length > 0)
                sb.Append(',');
            if (start == end)
                sb.Append(start.ToString(CultureInfo.InvariantCulture));
            else
                sb.Append(start.ToString(CultureInfo.InvariantCulture))
                    .Append('-')
                    .Append(end.ToString(CultureInfo.InvariantCulture));
            i = j;
        }

        return sb.ToString();
    }
}

public sealed class TpoTargetPriceDiagnostic
{
    public TpoTargetPriceDiagnostic(
        string label,
        decimal price,
        long tickIndex,
        int tpoCount,
        int rankAmongLevels,
        int belowMaxBy,
        IReadOnlyList<int> contributingPeriodIndices,
        string compressedPeriods,
        int completedPeriodContributionCount,
        bool developingPeriodContributed,
        int? firstContributingPeriod,
        int? lastContributingPeriod,
        bool insideTpoValueArea)
    {
        Label = label;
        Price = price;
        TickIndex = tickIndex;
        TpoCount = tpoCount;
        RankAmongLevels = rankAmongLevels;
        BelowMaxBy = belowMaxBy;
        ContributingPeriodIndices = contributingPeriodIndices;
        CompressedPeriods = compressedPeriods;
        CompletedPeriodContributionCount = completedPeriodContributionCount;
        DevelopingPeriodContributed = developingPeriodContributed;
        FirstContributingPeriod = firstContributingPeriod;
        LastContributingPeriod = lastContributingPeriod;
        InsideTpoValueArea = insideTpoValueArea;
    }

    public string Label { get; }
    public decimal Price { get; }
    public long TickIndex { get; }
    public int TpoCount { get; }
    public int RankAmongLevels { get; }
    public int BelowMaxBy { get; }
    public IReadOnlyList<int> ContributingPeriodIndices { get; }
    public string CompressedPeriods { get; }
    public int CompletedPeriodContributionCount { get; }
    public bool DevelopingPeriodContributed { get; }
    public int? FirstContributingPeriod { get; }
    public int? LastContributingPeriod { get; }
    public bool InsideTpoValueArea { get; }

    public IReadOnlyList<string> ToGpsRows()
    {
        if (Label == "SELECTED_POC")
        {
            return new[]
            {
                "SELECTED POC COUNT: " + TpoCount.ToString(CultureInfo.InvariantCulture),
                "SELECTED POC PERIODS: " + CompressedPeriods
            };
        }

        var px = Price.ToString("0.0", CultureInfo.InvariantCulture);
        return new[]
        {
            "REFERENCE PRICE: " + px,
            "REFERENCE COUNT: " + TpoCount.ToString(CultureInfo.InvariantCulture),
            "REFERENCE RANK: " + RankAmongLevels.ToString(CultureInfo.InvariantCulture),
            "REFERENCE BELOW MAX BY: " + BelowMaxBy.ToString(CultureInfo.InvariantCulture),
            "REFERENCE PERIODS: " + CompressedPeriods,
            "REFERENCE DEVELOPING CONTRIB: " + (DevelopingPeriodContributed ? "YES" : "NO"),
            "REFERENCE IN TPO VA: " + (InsideTpoValueArea ? "YES" : "NO")
        };
    }
}

public sealed class TpoLedgerAuditDiagnostic
{
    public TpoLedgerAuditDiagnostic(
        int ledgerBarCount,
        int expectedWallClockSlots,
        int missingM5Slots,
        IReadOnlyList<DateTimeOffset> missingM5SlotStartsUtc,
        int duplicateBarIndexObservations,
        IReadOnlyList<int> revisedBarIndices,
        IReadOnlyList<string> rejectedBars,
        int currentFormingBarIndex,
        bool currentFormingBarPresent)
    {
        LedgerBarCount = ledgerBarCount;
        ExpectedWallClockSlots = expectedWallClockSlots;
        MissingM5Slots = missingM5Slots;
        MissingM5SlotStartsUtc = missingM5SlotStartsUtc;
        DuplicateBarIndexObservations = duplicateBarIndexObservations;
        RevisedBarIndices = revisedBarIndices;
        RejectedBars = rejectedBars;
        CurrentFormingBarIndex = currentFormingBarIndex;
        CurrentFormingBarPresent = currentFormingBarPresent;
    }

    public int LedgerBarCount { get; }
    public int ExpectedWallClockSlots { get; }
    public int MissingM5Slots { get; }
    public IReadOnlyList<DateTimeOffset> MissingM5SlotStartsUtc { get; }
    public int DuplicateBarIndexObservations { get; }
    public IReadOnlyList<int> RevisedBarIndices { get; }
    public IReadOnlyList<string> RejectedBars { get; }
    public int CurrentFormingBarIndex { get; }
    public bool CurrentFormingBarPresent { get; }

    public IReadOnlyList<string> ToGpsRows()
    {
        var revised = RevisedBarIndices.Count == 0
            ? "—"
            : string.Join(",", RevisedBarIndices.Take(16).Select(i => i.ToString(CultureInfo.InvariantCulture)))
              + (RevisedBarIndices.Count > 16 ? ",..." : "");
        return new[]
        {
            "LEDGER BARS: " + LedgerBarCount.ToString(CultureInfo.InvariantCulture),
            "EXPECTED WALL-CLOCK SLOTS: " + ExpectedWallClockSlots.ToString(CultureInfo.InvariantCulture),
            "MISSING M5 SLOTS: " + MissingM5Slots.ToString(CultureInfo.InvariantCulture),
            "DUPLICATE BAR INDICES: " + DuplicateBarIndexObservations.ToString(CultureInfo.InvariantCulture),
            "REVISED BAR INDICES: " + revised,
            "REJECTED BARS: " + RejectedBars.Count.ToString(CultureInfo.InvariantCulture)
        };
    }
}

/// <summary>
/// Bounded Classic TPO parity / distribution forensic diagnostic for one auction.
/// No file I/O. Version bump when diagnostic fields change.
/// </summary>
public sealed class TpoParityDiagnostic
{
    public const string DiagnosticVersion = "TPO_PARITY_DIAG_V2";

    public TpoParityDiagnostic(
        string auctionId,
        DateTime auctionStartUtc,
        DateTime auctionEndUtc,
        decimal tickSize,
        int lastProcessedBar,
        string timestampPolicyVersion,
        bool developingPeriodIncludedInPocAndVa,
        int? developingPeriodIndex,
        int completedPeriodCount,
        int emptyPeriodSlots,
        int observedPeriodCount,
        int totalTpoCount,
        int maxTpoCount,
        IReadOnlyList<decimal> maxTiedPrices,
        decimal? profileMidpoint,
        decimal? previousPublishedPoc,
        decimal? selectedTpoPoc,
        string pocResolutionStep,
        decimal? completedOnlyTpoPoc,
        int completedOnlyMaxCount,
        IReadOnlyList<decimal> completedOnlyMaxTiedPrices,
        IReadOnlyList<TpoPeriodDiagnostic> periods,
        IReadOnlyList<(long Tick, int Count)> sortedCounts,
        TpoTargetPriceDiagnostic? selectedPocTarget = null,
        TpoTargetPriceDiagnostic? referenceTarget = null,
        TpoLedgerAuditDiagnostic? ledgerAudit = null,
        decimal? operatorReferencePrice = null,
        string atasBenchmarkConfirmation = TpoAtasBenchmarkChecklist.StatusUnconfirmed,
        string? referencePriceRejectReason = null)
    {
        AuctionId = auctionId;
        AuctionStartUtc = auctionStartUtc;
        AuctionEndUtc = auctionEndUtc;
        TickSize = tickSize;
        LastProcessedBar = lastProcessedBar;
        TimestampPolicyVersion = timestampPolicyVersion;
        DevelopingPeriodIncludedInPocAndVa = developingPeriodIncludedInPocAndVa;
        DevelopingPeriodIndex = developingPeriodIndex;
        CompletedPeriodCount = completedPeriodCount;
        EmptyPeriodSlots = emptyPeriodSlots;
        ObservedPeriodCount = observedPeriodCount;
        TotalTpoCount = totalTpoCount;
        MaxTpoCount = maxTpoCount;
        MaxTiedPrices = maxTiedPrices;
        ProfileMidpoint = profileMidpoint;
        PreviousPublishedPoc = previousPublishedPoc;
        SelectedTpoPoc = selectedTpoPoc;
        PocResolutionStep = pocResolutionStep;
        CompletedOnlyTpoPoc = completedOnlyTpoPoc;
        CompletedOnlyMaxCount = completedOnlyMaxCount;
        CompletedOnlyMaxTiedPrices = completedOnlyMaxTiedPrices;
        Periods = periods;
        SortedCounts = sortedCounts;
        SelectedPocTarget = selectedPocTarget;
        ReferenceTarget = referenceTarget;
        LedgerAudit = ledgerAudit;
        OperatorReferencePrice = operatorReferencePrice;
        AtasBenchmarkConfirmation = atasBenchmarkConfirmation;
        ReferencePriceRejectReason = referencePriceRejectReason;
    }

    public string AuctionId { get; }
    public DateTime AuctionStartUtc { get; }
    public DateTime AuctionEndUtc { get; }
    public decimal TickSize { get; }
    public int LastProcessedBar { get; }
    public string TimestampPolicyVersion { get; }
    public bool DevelopingPeriodIncludedInPocAndVa { get; }
    public int? DevelopingPeriodIndex { get; }
    public int CompletedPeriodCount { get; }
    public int EmptyPeriodSlots { get; }
    public int ObservedPeriodCount { get; }
    public int TotalTpoCount { get; }
    public int MaxTpoCount { get; }
    public IReadOnlyList<decimal> MaxTiedPrices { get; }
    public decimal? ProfileMidpoint { get; }
    public decimal? PreviousPublishedPoc { get; }
    public decimal? SelectedTpoPoc { get; }
    public string PocResolutionStep { get; }
    public decimal? CompletedOnlyTpoPoc { get; }
    public int CompletedOnlyMaxCount { get; }
    public IReadOnlyList<decimal> CompletedOnlyMaxTiedPrices { get; }
    public IReadOnlyList<TpoPeriodDiagnostic> Periods { get; }
    public IReadOnlyList<(long Tick, int Count)> SortedCounts { get; }
    public TpoTargetPriceDiagnostic? SelectedPocTarget { get; }
    public TpoTargetPriceDiagnostic? ReferenceTarget { get; }
    public TpoLedgerAuditDiagnostic? LedgerAudit { get; }
    public decimal? OperatorReferencePrice { get; }
    public string AtasBenchmarkConfirmation { get; }
    /// <summary>When reference was requested but rejected (NONPOSITIVE/OFF_TICK). Null when OK or disabled.</summary>
    public string? ReferencePriceRejectReason { get; }
    public string Version => DiagnosticVersion;

    public TpoParityDiagnostic WithLedgerAndReference(
        TpoLedgerAuditDiagnostic? ledger,
        TpoTargetPriceDiagnostic? referenceTarget,
        decimal? operatorReferencePrice,
        string? referencePriceRejectReason = null) =>
        new(
            AuctionId, AuctionStartUtc, AuctionEndUtc, TickSize, LastProcessedBar, TimestampPolicyVersion,
            DevelopingPeriodIncludedInPocAndVa, DevelopingPeriodIndex, CompletedPeriodCount, EmptyPeriodSlots,
            ObservedPeriodCount, TotalTpoCount, MaxTpoCount, MaxTiedPrices, ProfileMidpoint, PreviousPublishedPoc,
            SelectedTpoPoc, PocResolutionStep, CompletedOnlyTpoPoc, CompletedOnlyMaxCount, CompletedOnlyMaxTiedPrices,
            Periods, SortedCounts, SelectedPocTarget, referenceTarget, ledger, operatorReferencePrice,
            AtasBenchmarkConfirmation, referencePriceRejectReason);

    public IReadOnlyList<string> ToGpsDiagnosticRows()
    {
        var ties = MaxTiedPrices.Count == 0
            ? "—"
            : string.Join(",", MaxTiedPrices.Select(p => p.ToString("0.0", CultureInfo.InvariantCulture)));
        var rows = new List<string>
        {
            "TPO MAX COUNT: " + MaxTpoCount.ToString(CultureInfo.InvariantCulture),
            "TPO MAX TIES: " + ties,
            "TPO POC POLICY: " + PocSelector.TiePolicyId + " / " + PocResolutionStep,
            "TPO DEVELOPING INCLUDED: " + (DevelopingPeriodIncludedInPocAndVa ? "YES" : "NO"),
            "OBSERVED PERIODS: " + ObservedPeriodCount.ToString(CultureInfo.InvariantCulture),
            "EMPTY PERIOD SLOTS: " + EmptyPeriodSlots.ToString(CultureInfo.InvariantCulture),
            "TPO COMPLETED-ONLY POC: " + (CompletedOnlyTpoPoc?.ToString("0.0", CultureInfo.InvariantCulture) ?? "—")
        };

        if (SelectedPocTarget is not null)
            rows.AddRange(SelectedPocTarget.ToGpsRows());
        if (ReferenceTarget is not null)
            rows.AddRange(ReferenceTarget.ToGpsRows());
        else if (!string.IsNullOrEmpty(ReferencePriceRejectReason)
                 && !string.Equals(ReferencePriceRejectReason, TpoParityReferencePriceResolver.RejectDisabled, StringComparison.Ordinal))
        {
            rows.Add("REFERENCE PRICE: INVALID");
            rows.Add("REFERENCE REJECT: " + ReferencePriceRejectReason);
        }

        if (LedgerAudit is not null)
            rows.AddRange(LedgerAudit.ToGpsRows());

        rows.Add("ATAS BENCHMARK SETTINGS: " + AtasBenchmarkConfirmation);
        rows.Add("TPO DIAGNOSTIC VERSION: " + DiagnosticVersion);
        return rows;
    }
}

public sealed class TpoPeriodDiagnostic
{
    public TpoPeriodDiagnostic(
        int periodIndex,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        DateTime periodStartEt,
        DateTime periodEndEt,
        bool isCompleted,
        bool isDeveloping,
        int? firstBarIndex,
        int? lastBarIndex,
        long? lowTick,
        long? highTick,
        int uniquePriceLevels,
        int barRevisionHints,
        bool touchedPeriodBoundary,
        int contributingBarCount = 0,
        IReadOnlyList<int>? sortedContributingBarIndices = null,
        DateTimeOffset? firstRawCandleStartUtc = null,
        DateTimeOffset? lastRawCandleStartUtc = null,
        decimal? periodLow = null,
        decimal? periodHigh = null,
        int duplicateBarObservations = 0,
        int missingExpectedM5Slots = 0,
        IReadOnlyList<string>? rejectedBars = null,
        IReadOnlyList<int>? boundaryTouchingBarIndices = null)
    {
        PeriodIndex = periodIndex;
        PeriodStartUtc = periodStartUtc;
        PeriodEndUtc = periodEndUtc;
        PeriodStartEt = periodStartEt;
        PeriodEndEt = periodEndEt;
        IsCompleted = isCompleted;
        IsDeveloping = isDeveloping;
        FirstBarIndex = firstBarIndex;
        LastBarIndex = lastBarIndex;
        LowTick = lowTick;
        HighTick = highTick;
        UniquePriceLevels = uniquePriceLevels;
        BarRevisionHints = barRevisionHints;
        TouchedPeriodBoundary = touchedPeriodBoundary;
        ContributingBarCount = contributingBarCount;
        SortedContributingBarIndices = sortedContributingBarIndices ?? Array.Empty<int>();
        FirstRawCandleStartUtc = firstRawCandleStartUtc;
        LastRawCandleStartUtc = lastRawCandleStartUtc;
        PeriodLow = periodLow;
        PeriodHigh = periodHigh;
        DuplicateBarObservations = duplicateBarObservations;
        MissingExpectedM5Slots = missingExpectedM5Slots;
        RejectedBars = rejectedBars ?? Array.Empty<string>();
        BoundaryTouchingBarIndices = boundaryTouchingBarIndices ?? Array.Empty<int>();
    }

    public int PeriodIndex { get; }
    public DateTime PeriodStartUtc { get; }
    public DateTime PeriodEndUtc { get; }
    public DateTime PeriodStartEt { get; }
    public DateTime PeriodEndEt { get; }
    public bool IsCompleted { get; }
    public bool IsDeveloping { get; }
    public int? FirstBarIndex { get; }
    public int? LastBarIndex { get; }
    public long? LowTick { get; }
    public long? HighTick { get; }
    public int UniquePriceLevels { get; }
    public int BarRevisionHints { get; }
    public bool TouchedPeriodBoundary { get; }
    public int ContributingBarCount { get; }
    public IReadOnlyList<int> SortedContributingBarIndices { get; }
    public DateTimeOffset? FirstRawCandleStartUtc { get; }
    public DateTimeOffset? LastRawCandleStartUtc { get; }
    public decimal? PeriodLow { get; }
    public decimal? PeriodHigh { get; }
    public int DuplicateBarObservations { get; }
    public int MissingExpectedM5Slots { get; }
    public IReadOnlyList<string> RejectedBars { get; }
    public IReadOnlyList<int> BoundaryTouchingBarIndices { get; }
}

public sealed class PocSelectionResult
{
    public PocSelectionResult(
        long pocTick,
        decimal maxCount,
        IReadOnlyList<long> tiedTicks,
        decimal midpointTick,
        long? previousPocTick,
        string resolutionStep)
    {
        PocTick = pocTick;
        MaxCount = maxCount;
        TiedTicks = tiedTicks;
        MidpointTick = midpointTick;
        PreviousPocTick = previousPocTick;
        ResolutionStep = resolutionStep;
    }

    public long PocTick { get; }
    public decimal MaxCount { get; }
    public IReadOnlyList<long> TiedTicks { get; }
    public decimal MidpointTick { get; }
    public long? PreviousPocTick { get; }
    public string ResolutionStep { get; }
}

/// <summary>Builds target-price forensic rows without mutating production POC.</summary>
public static class TpoTargetPriceDiagnosticFactory
{
    public static TpoTargetPriceDiagnostic? TryBuild(
        string label,
        decimal price,
        PriceGrid grid,
        IReadOnlyDictionary<long, int> counts,
        IReadOnlyDictionary<long, IReadOnlyList<int>> tickToPeriods,
        int? developingPeriodIndex,
        decimal? tpoVal,
        decimal? tpoVah,
        int maxCount)
    {
        if (!grid.TryToTickIndex(price, out var tick))
            return null;

        counts.TryGetValue(tick, out var count);
        var rank = 1;
        foreach (var c in counts.Values)
        {
            if (c > count)
                rank++;
        }

        tickToPeriods.TryGetValue(tick, out var periods);
        periods ??= Array.Empty<int>();
        var sorted = periods.Distinct().OrderBy(p => p).ToArray();
        var completed = developingPeriodIndex is int dev
            ? sorted.Count(p => p != dev)
            : sorted.Length;
        var developingContrib = developingPeriodIndex is int d && sorted.Contains(d);
        var inVa = tpoVal is decimal lo && tpoVah is decimal hi && price >= lo && price <= hi;

        return new TpoTargetPriceDiagnostic(
            label,
            grid.ToPrice(tick),
            tick,
            count,
            rank,
            Math.Max(0, maxCount - count),
            sorted,
            PeriodIndexCompressor.Compress(sorted),
            completed,
            developingContrib,
            sorted.Length > 0 ? sorted[0] : null,
            sorted.Length > 0 ? sorted[^1] : null,
            inVa);
    }
}

/// <summary>Wall-clock M5 slot expectations for Classic TPO period forensics (seed: 5-minute chart).</summary>
public static class TpoM5SlotForensics
{
    public const int ChartMinutes = 5; // seed value, subject to sensitivity test — M5 chart assumption for slot audit

    public static int CountExpectedSlots(DateTime periodStartUtc, DateTime periodEndUtc, DateTime evaluationUtc, bool isCompleted)
    {
        var end = isCompleted ? periodEndUtc : (evaluationUtc < periodEndUtc ? evaluationUtc : periodEndUtc);
        if (end <= periodStartUtc)
            return 0;
        var slots = 0;
        for (var t = periodStartUtc; t < end; t = t.AddMinutes(ChartMinutes))
            slots++;
        return slots;
    }

    public static IReadOnlyList<DateTime> ExpectedSlotStarts(DateTime periodStartUtc, DateTime periodEndUtc, DateTime evaluationUtc, bool isCompleted)
    {
        var end = isCompleted ? periodEndUtc : (evaluationUtc < periodEndUtc ? evaluationUtc : periodEndUtc);
        var list = new List<DateTime>();
        for (var t = periodStartUtc; t < end; t = t.AddMinutes(ChartMinutes))
            list.Add(t);
        return list;
    }

    public static int CountMissingSlots(
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        DateTime evaluationUtc,
        bool isCompleted,
        IReadOnlyCollection<DateTimeOffset> observedBarStartsUtc)
    {
        var expected = ExpectedSlotStarts(periodStartUtc, periodEndUtc, evaluationUtc, isCompleted);
        var observed = new HashSet<DateTime>(observedBarStartsUtc.Select(s => s.UtcDateTime));
        return expected.Count(e => !observed.Contains(e));
    }
}
