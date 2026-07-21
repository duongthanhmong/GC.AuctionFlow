using Aos.LevelEngine.Session;

namespace Aos.LevelEngine.Session;

/// <summary>Compatibility wrapper — prefer <see cref="PeriodIdentityGuard"/>.</summary>
public static class SessionAlignment
{
    public const string Aligned = "ALIGNED";
    public const string Misaligned = "MISALIGNED";
    public const string Unknown = "UNKNOWN";

    public readonly record struct Result(
        string Alignment,
        DateOnly? FixedProfileTradingDate,
        DateOnly? ExpectedLastDayTradingDate,
        bool EmitDataQualityEvent,
        string? Detail);

    public static Result CompareLastDay(
        DateOnly? expectedPriorTradingDate,
        DateTime? fixedProfileScaledTimeUtc,
        TimeZoneInfo tz,
        TimeOnly? sessionRolloverEt = null)
    {
        var resolver = new TradingDateResolver(
            tz,
            sessionRolloverEt ?? new TimeOnly(18, 0),
            new TimeOnly(9, 30),
            new TimeOnly(16, 0),
            partialCoverageMaxMissingMinutes: 30);

        var g = PeriodIdentityGuard.Evaluate(
            "FixedProfile.LastDay", expectedPriorTradingDate, fixedProfileScaledTimeUtc, resolver);

        var alignment = g.Alignment.AlignmentStatus switch
        {
            AlignmentStatus.ALIGNED => Aligned,
            AlignmentStatus.MISALIGNED => Misaligned,
            _ => Unknown
        };

        return new Result(
            alignment,
            g.Alignment.ObservedTradingDate,
            g.Alignment.ExpectedTradingDate,
            g.EmitDataQualityEvent,
            g.Alignment.Detail);
    }
}
