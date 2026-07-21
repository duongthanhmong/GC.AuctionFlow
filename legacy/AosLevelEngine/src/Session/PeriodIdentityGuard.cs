using Aos.LevelEngine.Session;

namespace Aos.LevelEngine.Session;

/// <summary>
/// Alignment guard for any external period-identity source (FixedProfile, session candles, …).
/// </summary>
public static class PeriodIdentityGuard
{
    public const string SessionDefinitionMismatch = "SESSION_DEFINITION_MISMATCH";

    public readonly record struct GuardResult(
        PeriodIdentityAlignment Alignment,
        bool EmitDataQualityEvent,
        bool AllowLevelCreation);

    public static GuardResult Evaluate(
        string sourceName,
        DateOnly? expectedTradingDate,
        DateTime? observedPeriodTimestampUtc,
        TradingDateResolver resolver)
    {
        if (expectedTradingDate is null || observedPeriodTimestampUtc is null)
        {
            var unverifiable = new PeriodIdentityAlignment
            {
                SourceName = sourceName,
                ExpectedTradingDate = expectedTradingDate,
                ObservedTradingDate = null,
                AlignmentStatus = AlignmentStatus.UNVERIFIABLE,
                Detail = "Missing expected and/or observed period timestamp."
            };
            // UNVERIFIABLE → capability unusable for precise session identity; do not fail whole engine
            return new GuardResult(unverifiable, false, AllowLevelCreation: false);
        }

        var observed = resolver.ResolveTradingDate(observedPeriodTimestampUtc.Value);
        if (observed == expectedTradingDate.Value)
        {
            return new GuardResult(new PeriodIdentityAlignment
            {
                SourceName = sourceName,
                ExpectedTradingDate = expectedTradingDate,
                ObservedTradingDate = observed,
                AlignmentStatus = AlignmentStatus.ALIGNED
            }, false, true);
        }

        var mis = new PeriodIdentityAlignment
        {
            SourceName = sourceName,
            ExpectedTradingDate = expectedTradingDate,
            ObservedTradingDate = observed,
            AlignmentStatus = AlignmentStatus.MISALIGNED,
            Detail =
                $"expected={resolver.Format(expectedTradingDate)} observed={resolver.Format(observed)} " +
                $"scaledUtc={DateTime.SpecifyKind(observedPeriodTimestampUtc.Value, DateTimeKind.Utc):o}"
        };
        return new GuardResult(mis, EmitDataQualityEvent: true, AllowLevelCreation: false);
    }
}
