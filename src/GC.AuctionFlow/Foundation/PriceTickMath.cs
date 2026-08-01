namespace GC.AuctionFlow.Foundation;

/// <summary>
/// TTS §5.2: price is converted to integer ticks exactly once at the adapter boundary. A float/decimal
/// display price must never become an internal identity or state-boundary key. This is the single
/// deterministic conversion used by the foundation.
/// </summary>
public static class PriceTickMath
{
    /// <summary>True when the tick size is a usable positive, finite step. Zero/negative/NaN blocks analysis.</summary>
    public static bool IsValidTickSize(decimal tickSize) => tickSize > 0m;

    /// <summary>
    /// Deterministic price → integer ticks. Rounds half away from zero so the mapping is stable and
    /// sign-symmetric. Throws on an invalid tick size — the caller must have gated on
    /// <see cref="IsValidTickSize"/> and raised <c>FND_TICK_SIZE_INVALID</c> first.
    /// </summary>
    public static long ToTicks(decimal price, decimal tickSize)
    {
        if (!IsValidTickSize(tickSize))
            throw new ArgumentOutOfRangeException(nameof(tickSize), tickSize, "tick size must be > 0");
        return (long)decimal.Round(price / tickSize, 0, MidpointRounding.AwayFromZero);
    }

    /// <summary>Inverse for display only. Never used as an identity/boundary key.</summary>
    public static decimal ToDisplayPrice(long priceTicks, decimal tickSize) => priceTicks * tickSize;
}
