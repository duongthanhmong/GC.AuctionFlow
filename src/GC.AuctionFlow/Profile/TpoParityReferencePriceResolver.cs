namespace GC.AuctionFlow.Profile;

/// <summary>
/// Resolves ATAS-compatible (bool + non-nullable decimal) reference-price settings into
/// an optional diagnostics-only price. Never used for TPO/POC/VA calculation.
/// </summary>
public static class TpoParityReferencePriceResolver
{
    public const string RejectDisabled = "DISABLED";
    public const string RejectNonPositive = "NONPOSITIVE";
    public const string RejectOffTick = "OFF_TICK";

    public static TpoParityReferencePriceResolution Resolve(
        bool enabled,
        decimal rawPrice,
        decimal tickSize)
    {
        if (!enabled)
            return TpoParityReferencePriceResolution.Inactive(RejectDisabled);

        if (rawPrice <= 0m)
            return TpoParityReferencePriceResolution.Rejected(RejectNonPositive, rawPrice);

        if (tickSize <= 0m)
            return TpoParityReferencePriceResolution.Rejected(RejectOffTick, rawPrice);

        var grid = new PriceGrid(tickSize);
        if (!grid.TryToTickIndex(rawPrice, out var tick))
            return TpoParityReferencePriceResolution.Rejected(RejectOffTick, rawPrice);

        var normalized = grid.ToPrice(tick);
        return TpoParityReferencePriceResolution.Accepted(normalized, rawPrice);
    }
}

public readonly struct TpoParityReferencePriceResolution
{
    private TpoParityReferencePriceResolution(bool active, decimal? normalizedPrice, decimal rawPrice, string reason)
    {
        IsActive = active;
        NormalizedPrice = normalizedPrice;
        RawPrice = rawPrice;
        Reason = reason;
    }

    public bool IsActive { get; }
    public decimal? NormalizedPrice { get; }
    public decimal RawPrice { get; }
    public string Reason { get; }

    public static TpoParityReferencePriceResolution Inactive(string reason) =>
        new(false, null, 0m, reason);

    public static TpoParityReferencePriceResolution Rejected(string reason, decimal raw) =>
        new(false, null, raw, reason);

    public static TpoParityReferencePriceResolution Accepted(decimal normalized, decimal raw) =>
        new(true, normalized, raw, "OK");
}
