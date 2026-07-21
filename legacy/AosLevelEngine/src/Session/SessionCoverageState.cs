namespace Aos.LevelEngine.Session;

/// <summary>
/// RTH coverage classification — never conflate truncated history edge with a holiday.
/// </summary>
public enum SessionCoverageState
{
    /// <summary>Bars cover [RthStart, RthEnd) within profile tolerance.</summary>
    COMPLETE,
    /// <summary>Some RTH data present but abnormal gaps — exclude from Weekly/Composite/nPOC.</summary>
    PARTIAL_COVERAGE,
    /// <summary>No data AND day sits strictly between two COMPLETE sessions (holiday/weekend evidence).</summary>
    NON_TRADING_DAY,
    /// <summary>Before EarliestUsableTradingDate or at history edge — system must say "don't know".</summary>
    UNKNOWN_COVERAGE
}

public enum AlignmentStatus
{
    ALIGNED,
    MISALIGNED,
    UNVERIFIABLE,
    NOT_APPLICABLE
}

/// <summary>Period-identity source cross-check (FixedProfile LastDay/Week, session candles, …).</summary>
public sealed class PeriodIdentityAlignment
{
    public required string SourceName { get; init; }
    public DateOnly? ExpectedTradingDate { get; init; }
    public DateOnly? ObservedTradingDate { get; init; }
    public required AlignmentStatus AlignmentStatus { get; init; }
    public string? Detail { get; init; }
}
