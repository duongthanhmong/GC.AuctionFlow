using Aos.LevelEngine.Interactions;
using Aos.LevelEngine.Profiles;

namespace Aos.LevelEngine.Session;

/// <summary>
/// RotationR lifecycle for Interaction Engine.
/// ResetTicks = max(floor, R) and ExitDistance = R — fail-closed until AVAILABLE.
/// Never invent a default R.
/// </summary>
public enum RotationRAvailabilityStatus
{
    /// <summary>Before FreezeTime (profile) on current TradingDate — R not frozen yet.</summary>
    NOT_YET_AVAILABLE,
    /// <summary>At/after FreezeTime but M1 volatility-window bars insufficient / wrong TF.</summary>
    INSUFFICIENT,
    /// <summary>Enough M1 bars; R computed and frozen.</summary>
    AVAILABLE
}

public sealed class RotationRAvailabilityResult
{
    public required RotationRAvailabilityStatus Status { get; init; }
    public required DateOnly TradingDate { get; init; }
    public required TimeOnly FreezeTimeLocal { get; init; }
    public int BarsInVolatilityWindow { get; init; }
    public RotationRState? FrozenState { get; init; }
    public string? Reason { get; init; }

    /// <summary>Interaction Engine must refuse to start when false.</summary>
    public bool AllowsInteractionEngine => Status == RotationRAvailabilityStatus.AVAILABLE
                                           && FrozenState is { IsFrozen: true };

    public int? RotationRTicks =>
        AllowsInteractionEngine ? FrozenState!.RotationRTicks : null;
}

public static class RotationRAvailability
{
    /// <summary>
    /// Evaluate RotationR status. <paramref name="asOfExchangeUtc"/> = evaluation instant (usually newest bar / clock).
    /// <paramref name="m1TrueRangesInWindow"/> = M1 true ranges (ticks) inside [VolatilityWindowStart, VolatilityWindowEnd).
    /// Empty list after freeze time → INSUFFICIENT (no default R).
    /// </summary>
    public static RotationRAvailabilityResult Evaluate(
        InstrumentProfile profile,
        TradingDateResolver resolver,
        DateOnly tradingDate,
        DateTime asOfExchangeUtc,
        IReadOnlyList<decimal> m1TrueRangesInWindow,
        string? chartTimeFrame,
        bool macroEventInVolWindow = false)
    {
        var freezeLocal = InstrumentProfileLoader.ParseTime(profile.Rotation.FreezeTime);
        var asOfUtc = DateTime.SpecifyKind(asOfExchangeUtc, DateTimeKind.Utc);
        var asOfTd = resolver.ResolveTradingDate(asOfUtc);
        var local = TimeZoneInfo.ConvertTimeFromUtc(asOfUtc, resolver.TradingTimeZone);
        var localDate = DateOnly.FromDateTime(local);
        var tod = TimeOnly.FromDateTime(local);

        // Before freeze on the TradingDate's RTH calendar morning: NOT_YET_AVAILABLE
        // Freeze applies on calendar day == tradingDate (FreezeTime 09:15 < rollover 18:00).
        var pastFreeze = asOfTd > tradingDate
                         || (asOfTd == tradingDate && localDate == tradingDate && tod >= freezeLocal);

        if (!pastFreeze)
        {
            return new RotationRAvailabilityResult
            {
                Status = RotationRAvailabilityStatus.NOT_YET_AVAILABLE,
                TradingDate = tradingDate,
                FreezeTimeLocal = freezeLocal,
                BarsInVolatilityWindow = m1TrueRangesInWindow.Count,
                Reason =
                    $"asOf local {localDate} {tod:HH\\:mm} ET < FreezeTime {freezeLocal:HH\\:mm} on TradingDate {tradingDate}."
            };
        }

        var req = profile.HistoryCapabilities.RotationR;
        var looksM1 = chartTimeFrame is not null &&
                      (chartTimeFrame.Contains("M1", StringComparison.OrdinalIgnoreCase)
                       || chartTimeFrame.Equals("1", StringComparison.OrdinalIgnoreCase));
        if (req.RequireChartTimeframeM1 && !looksM1)
        {
            return new RotationRAvailabilityResult
            {
                Status = RotationRAvailabilityStatus.INSUFFICIENT,
                TradingDate = tradingDate,
                FreezeTimeLocal = freezeLocal,
                BarsInVolatilityWindow = m1TrueRangesInWindow.Count,
                Reason = $"Chart TimeFrame='{chartTimeFrame}' is not M1 — no default RotationR."
            };
        }

        if (m1TrueRangesInWindow.Count == 0)
        {
            return new RotationRAvailabilityResult
            {
                Status = RotationRAvailabilityStatus.INSUFFICIENT,
                TradingDate = tradingDate,
                FreezeTimeLocal = freezeLocal,
                BarsInVolatilityWindow = 0,
                Reason =
                    $"Past FreezeTime {freezeLocal:HH\\:mm} but no M1 true ranges in volatility window — " +
                    "INSUFFICIENT; Interaction Engine FAIL-CLOSED (no default R)."
            };
        }

        var freezeAt = DateTime.SpecifyKind(
            tradingDate.ToDateTime(freezeLocal), DateTimeKind.Unspecified);
        // Convert freeze wall-clock ET → UTC for storage
        var freezeUtc = TimeZoneInfo.ConvertTimeToUtc(freezeAt, resolver.TradingTimeZone);
        var frozen = RotationRCalculator.Freeze(profile, m1TrueRangesInWindow, freezeUtc, macroEventInVolWindow);

        return new RotationRAvailabilityResult
        {
            Status = RotationRAvailabilityStatus.AVAILABLE,
            TradingDate = tradingDate,
            FreezeTimeLocal = freezeLocal,
            BarsInVolatilityWindow = m1TrueRangesInWindow.Count,
            FrozenState = frozen,
            Reason =
                $"RotationR={frozen.RotationRTicks} frozen at {freezeLocal:HH\\:mm} ET; {frozen.RSourceBreakdown}"
        };
    }

    /// <summary>FAIL-CLOSED gate for Interaction Engine — never substitute a default R.</summary>
    public static void EnsureAvailableForInteraction(RotationRAvailabilityResult result)
    {
        if (result.AllowsInteractionEngine) return;
        throw new InvalidOperationException(
            $"FAIL-CLOSED: Interaction Engine requires RotationR AVAILABLE (frozen). " +
            $"Status={result.Status}. {result.Reason} " +
            "ResetTicks=max(floor,R) and ExitDistance=R — defaults forbidden.");
    }
}
