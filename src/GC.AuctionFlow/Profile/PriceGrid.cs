namespace GC.AuctionFlow.Profile;

/// <summary>
/// Tick-safe price normalization. Profile dictionaries key by integer tick index.
/// Alignment: after MidpointRounding.AwayFromZero, absolute residual in tick-units must be
/// &lt;= (AlignmentTolerance / TickSize). Default tolerance = TickSize * 1e-8 (seed).
/// </summary>
public sealed class PriceGrid
{
    public const decimal DefaultAlignmentToleranceFraction = 0.00000001m;

    public PriceGrid(decimal tickSize, decimal alignmentToleranceFraction = DefaultAlignmentToleranceFraction)
    {
        if (tickSize <= 0m)
            throw new ArgumentOutOfRangeException(nameof(tickSize), "Tick size must be positive.");
        if (alignmentToleranceFraction < 0m || alignmentToleranceFraction >= 0.5m)
            throw new ArgumentOutOfRangeException(nameof(alignmentToleranceFraction));

        TickSize = tickSize;
        AlignmentTolerance = tickSize * alignmentToleranceFraction;
    }

    public decimal TickSize { get; }
    public decimal AlignmentTolerance { get; }

    public long ToTickIndex(decimal price)
    {
        var scaled = price / TickSize;
        var rounded = decimal.Round(scaled, 0, MidpointRounding.AwayFromZero);
        var residualTicks = Math.Abs(scaled - rounded);
        var toleranceTicks = AlignmentTolerance / TickSize;
        if (residualTicks > toleranceTicks)
            throw new ArgumentException($"Price {price} is not alignable to tick size {TickSize} (residualTicks={residualTicks}).");
        return (long)rounded;
    }

    public bool TryToTickIndex(decimal price, out long tickIndex)
    {
        try
        {
            tickIndex = ToTickIndex(price);
            return true;
        }
        catch
        {
            tickIndex = 0;
            return false;
        }
    }

    public decimal ToPrice(long tickIndex) => tickIndex * TickSize;

    public decimal Normalize(decimal price) => ToPrice(ToTickIndex(price));
}
