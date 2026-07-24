using GC.AuctionFlow.Composite;
using GC.AuctionFlow.Profile;

namespace GC.AuctionFlow.Directional;

/// <summary>Descriptive price-vs-value location. Exact tick equality. No S/R wording.</summary>
public static class PriceLocationClassifier
{
    public static PriceValueLocation Classify(
        decimal? price,
        decimal? val,
        decimal? vah,
        decimal? poc,
        decimal tickSize)
    {
        if (price is null || val is null || vah is null || tickSize <= 0m)
            return PriceValueLocation.Unavailable;

        var grid = new PriceGrid(tickSize);
        if (!grid.TryToTickIndex(price.Value, out var px)
            || !grid.TryToTickIndex(val.Value, out var lo)
            || !grid.TryToTickIndex(vah.Value, out var hi))
            return PriceValueLocation.Unavailable;

        if (lo > hi)
            return PriceValueLocation.Unavailable;

        if (poc is decimal p && grid.TryToTickIndex(p, out var pocTick) && px == pocTick)
            return PriceValueLocation.AtPoc;
        if (px == hi)
            return PriceValueLocation.AtValueHigh;
        if (px == lo)
            return PriceValueLocation.AtValueLow;
        if (px > hi)
            return PriceValueLocation.AboveValue;
        if (px < lo)
            return PriceValueLocation.BelowValue;
        return PriceValueLocation.InsideValue;
    }

    public static ProfileLocationContextSnapshot Build(
        decimal? currentPrice,
        PrimaryAuctionProfileSnapshot? currentPrimary,
        PrimaryAuctionProfileSnapshot? previousPrimary,
        CompositeSetSnapshot? composite,
        decimal tickSize)
    {
        var curTpo = currentPrimary?.TpoProfile;
        var prevTpo = previousPrimary?.TpoProfile;
        var conf = composite?.Confirmed;

        var primaryTpo = Classify(currentPrice, curTpo?.TpoVal, curTpo?.TpoVah, curTpo?.TpoPoc, tickSize);
        var previousTpo = Classify(currentPrice, prevTpo?.TpoVal, prevTpo?.TpoVah, prevTpo?.TpoPoc, tickSize);

        var primaryVol = PriceValueLocation.Unavailable;
        if (currentPrimary?.VolumeProfile?.PriceVolumeCapability == PriceVolumeCapability.Exact)
        {
            primaryVol = Classify(
                currentPrice,
                currentPrimary.VolumeProfile.VolumeVal,
                currentPrimary.VolumeProfile.VolumeVah,
                currentPrimary.VolumeProfile.VolumePoc,
                tickSize);
        }

        var compositeTpo = PriceValueLocation.Unavailable;
        var compositeVol = PriceValueLocation.Unavailable;
        if (conf is not null
            && conf.CompositeStatus is CompositeStatus.Ready or CompositeStatus.Partial)
        {
            compositeTpo = Classify(currentPrice, conf.TpoVal, conf.TpoVah, conf.TpoPoc, tickSize);
            if (conf.Aggregate?.PriceVolumeCapability == PriceVolumeCapability.Exact)
            {
                compositeVol = Classify(currentPrice, conf.VolumeVal, conf.VolumeVah, conf.VolumePoc, tickSize);
            }
        }

        return new ProfileLocationContextSnapshot(
            primaryTpo,
            primaryVol,
            previousTpo,
            compositeTpo,
            compositeVol);
    }
}
