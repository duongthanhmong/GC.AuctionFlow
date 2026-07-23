namespace GC.AuctionFlow.Profile;

public sealed class VolumeProfileSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public VolumeProfileSnapshot(
        string auctionId,
        decimal? profileHigh,
        decimal? profileLow,
        decimal? volumePoc,
        decimal? volumeVah,
        decimal? volumeVal,
        decimal totalExecutedVolume,
        IReadOnlyDictionary<long, decimal> priceLevelVolumes,
        PriceVolumeCapability priceVolumeCapability,
        ProfileDataQuality dataQuality,
        string provenance,
        IReadOnlyList<string> knownLimitations)
    {
        AuctionId = auctionId;
        ProfileHigh = profileHigh;
        ProfileLow = profileLow;
        VolumePoc = volumePoc;
        VolumeVah = volumeVah;
        VolumeVal = volumeVal;
        TotalExecutedVolume = totalExecutedVolume;
        PriceLevelVolumes = priceLevelVolumes;
        PriceVolumeCapability = priceVolumeCapability;
        DataQuality = dataQuality;
        Provenance = provenance;
        KnownLimitations = knownLimitations ?? Array.Empty<string>();
    }

    public string AuctionId { get; }
    public decimal? ProfileHigh { get; }
    public decimal? ProfileLow { get; }
    public decimal? VolumePoc { get; }
    public decimal? VolumeVah { get; }
    public decimal? VolumeVal { get; }
    public decimal TotalExecutedVolume { get; }
    public IReadOnlyDictionary<long, decimal> PriceLevelVolumes { get; }
    public PriceVolumeCapability PriceVolumeCapability { get; }
    public ProfileDataQuality DataQuality { get; }
    public string Provenance { get; }
    public IReadOnlyList<string> KnownLimitations { get; }
    public string Version => SnapshotVersion;
}

/// <summary>
/// Exact executed volume-at-price only. Never smears bar TotalVolume across High-Low.
/// </summary>
public static class VolumeProfileEngine
{
    public static VolumeProfileSnapshot Build(
        string auctionId,
        DateTime auctionStartUtc,
        DateTime auctionEndUtc,
        IReadOnlyList<ProfileBarObservation> bars,
        PriceGrid grid,
        decimal valueAreaFraction,
        long? previousVolumePocTick)
    {
        var limitations = new List<string>
        {
            "NO_VOLUME_SMEARING_FROM_BAR_TOTAL",
            "BIDASK_NOT_INFERRED_FROM_TOTAL_VOLUME",
            "POC_TIE_POLICY=" + PocSelector.TiePolicyId
        };

        var relevant = bars
            .Where(b => !(b.EndUtc.UtcDateTime < auctionStartUtc || b.StartUtc.UtcDateTime >= auctionEndUtc))
            .OrderBy(b => b.BarIndex)
            .ToList();

        if (relevant.Count == 0)
        {
            return new VolumeProfileSnapshot(
                auctionId, null, null, null, null, null, 0m,
                new Dictionary<long, decimal>(),
                PriceVolumeCapability.Unknown,
                ProfileDataQuality.Unknown,
                "VolumeProfileEngine/empty",
                limitations);
        }

        var anyExact = relevant.Any(b => b.PriceVolumeCapability == PriceVolumeCapability.Exact && b.PriceVolumes.Count > 0);
        if (!anyExact)
        {
            limitations.Add("PRICE_VOLUME_DATA_UNAVAILABLE");
            limitations.Add("VOLUME_PROFILE_UNAVAILABLE");
            return new VolumeProfileSnapshot(
                auctionId, null, null, null, null, null, 0m,
                new Dictionary<long, decimal>(),
                PriceVolumeCapability.Unavailable,
                ProfileDataQuality.Partial,
                "VolumeProfileEngine/unavailable",
                limitations);
        }

        // Replace-by-bar: last observation for each bar index wins (caller should already dedupe).
        var byBar = new Dictionary<int, ProfileBarObservation>();
        foreach (var bar in relevant)
            byBar[bar.BarIndex] = bar;

        var volumes = new Dictionary<long, decimal>();
        long? hi = null, lo = null;
        decimal total = 0m;

        foreach (var bar in byBar.Values.OrderBy(b => b.BarIndex))
        {
            if (bar.PriceVolumeCapability != PriceVolumeCapability.Exact)
                continue;

            foreach (var pv in bar.PriceVolumes)
            {
                if (!grid.TryToTickIndex(pv.Price, out var tick))
                {
                    limitations.Add("PV_PRICE_NOT_ALIGNABLE:" + bar.BarIndex);
                    continue;
                }

                volumes.TryGetValue(tick, out var cur);
                volumes[tick] = cur + pv.ExecutedVolume;
                total += pv.ExecutedVolume;
                hi = hi is null ? tick : Math.Max(hi.Value, tick);
                lo = lo is null ? tick : Math.Min(lo.Value, tick);
            }
        }

        if (volumes.Count == 0)
        {
            limitations.Add("PRICE_VOLUME_DATA_UNAVAILABLE");
            return new VolumeProfileSnapshot(
                auctionId, null, null, null, null, null, 0m,
                volumes,
                PriceVolumeCapability.Unavailable,
                ProfileDataQuality.Partial,
                "VolumeProfileEngine/empty-levels",
                limitations);
        }

        var pocTick = PocSelector.SelectPocTick(volumes, previousVolumePocTick);
        var va = ValueAreaCalculator.Calculate(volumes, pocTick, valueAreaFraction, grid);

        return new VolumeProfileSnapshot(
            auctionId,
            grid.ToPrice(hi!.Value),
            grid.ToPrice(lo!.Value),
            grid.ToPrice(pocTick),
            va.Vah,
            va.Val,
            total,
            volumes,
            PriceVolumeCapability.Exact,
            ProfileDataQuality.Complete,
            "VolumeProfileEngine/v1",
            limitations.Distinct(StringComparer.Ordinal).ToArray());
    }
}
