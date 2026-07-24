using GC.AuctionFlow.Profile;

namespace GC.AuctionFlow.Composite;

/// <summary>Deterministic composite TPO/Volume aggregation over tick indices. Reuses Phase 1A POC/VA.</summary>
public static class CompositeAggregator
{
    public static CompositeAggregateResult Aggregate(
        IReadOnlyList<CompositeAuctionContribution> included,
        decimal tickSize,
        decimal valueAreaFraction,
        long? previousCompositeTpoPocTick = null,
        long? previousCompositeVolumePocTick = null)
    {
        if (included is null) throw new ArgumentNullException(nameof(included));
        var grid = new PriceGrid(tickSize);
        var limitations = new List<string>
        {
            "COMPOSITE_AGGREGATION_V1",
            "POC_TIE_POLICY=" + PocSelector.TiePolicyId,
            "NO_VOLUME_SMEARING",
            "NO_HARD_N_DAY_MERGE"
        };

        if (included.Count == 0)
        {
            return new CompositeAggregateResult(
                new Dictionary<long, int>(),
                new Dictionary<long, decimal>(),
                0, 0m, null, null, null, null, null, null, null, null,
                PriceVolumeCapability.Unavailable,
                ProfileDataQuality.Unknown,
                limitations);
        }

        var tpo = new Dictionary<long, int>();
        var vol = new Dictionary<long, decimal>();
        long? hi = null, lo = null;
        var totalTpo = 0;
        var totalVol = 0m;
        var anyExact = false;
        var anyUnavailable = false;
        var allExact = true;

        foreach (var c in included.OrderBy(x => x.AuctionStartUtc).ThenBy(x => x.AuctionId, StringComparer.Ordinal))
        {
            foreach (var kv in c.PriceLevelTpoCounts)
            {
                tpo.TryGetValue(kv.Key, out var n);
                tpo[kv.Key] = n + kv.Value;
                totalTpo += kv.Value;
                hi = hi is null ? kv.Key : Math.Max(hi.Value, kv.Key);
                lo = lo is null ? kv.Key : Math.Min(lo.Value, kv.Key);
            }

            if (c.PriceVolumeCapability == PriceVolumeCapability.Exact)
            {
                anyExact = true;
                foreach (var kv in c.PriceLevelVolumes)
                {
                    vol.TryGetValue(kv.Key, out var v);
                    vol[kv.Key] = v + kv.Value;
                    totalVol += kv.Value;
                    hi = hi is null ? kv.Key : Math.Max(hi.Value, kv.Key);
                    lo = lo is null ? kv.Key : Math.Min(lo.Value, kv.Key);
                }
            }
            else
            {
                anyUnavailable = true;
                allExact = false;
                limitations.Add("VOLUME_UNAVAILABLE:" + c.AuctionId);
            }
        }

        decimal? tpoPoc = null, tpoVah = null, tpoVal = null;
        decimal? volPoc = null, volVah = null, volVal = null;
        decimal? profileHigh = hi is long h ? grid.ToPrice(h) : null;
        decimal? profileLow = lo is long l ? grid.ToPrice(l) : null;

        if (tpo.Count > 0)
        {
            var poc = PocSelector.SelectDetailed(
                tpo.ToDictionary(kv => kv.Key, kv => (decimal)kv.Value),
                previousCompositeTpoPocTick);
            tpoPoc = grid.ToPrice(poc.PocTick);
            var va = ValueAreaCalculator.Calculate(
                tpo.ToDictionary(kv => kv.Key, kv => (decimal)kv.Value),
                poc.PocTick,
                valueAreaFraction,
                grid);
            tpoVah = va.Vah;
            tpoVal = va.Val;
        }

        var volCap = allExact && anyExact ? PriceVolumeCapability.Exact
            : anyExact ? PriceVolumeCapability.Exact // still exact for available portion; Partial quality below
            : PriceVolumeCapability.Unavailable;

        if (anyExact && vol.Count > 0)
        {
            // If some contributions lack exact volume, quality is Partial but POC uses available exact volume only.
            if (anyUnavailable)
            {
                volCap = PriceVolumeCapability.Exact;
                limitations.Add("COMPOSITE_VOLUME_PARTIAL_CONTRIBUTIONS");
            }

            var vp = PocSelector.SelectDetailed(vol, previousCompositeVolumePocTick);
            volPoc = grid.ToPrice(vp.PocTick);
            var vva = ValueAreaCalculator.Calculate(vol, vp.PocTick, valueAreaFraction, grid);
            volVah = vva.Vah;
            volVal = vva.Val;
        }
        else
        {
            volCap = PriceVolumeCapability.Unavailable;
        }

        var quality = tpo.Count == 0 ? ProfileDataQuality.Unknown
            : anyUnavailable ? ProfileDataQuality.Partial
            : ProfileDataQuality.Complete;

        return new CompositeAggregateResult(
            tpo.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key, kv => kv.Value),
            vol.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key, kv => kv.Value),
            totalTpo,
            totalVol,
            profileHigh,
            profileLow,
            tpoPoc,
            volPoc,
            tpoVah,
            tpoVal,
            volVah,
            volVal,
            volCap,
            quality,
            limitations.Distinct(StringComparer.Ordinal).ToArray());
    }
}

public sealed class CompositeAggregateResult
{
    public CompositeAggregateResult(
        IReadOnlyDictionary<long, int> tpoCounts,
        IReadOnlyDictionary<long, decimal> volumes,
        int totalTpoCount,
        decimal totalExecutedVolume,
        decimal? profileHigh,
        decimal? profileLow,
        decimal? tpoPoc,
        decimal? volumePoc,
        decimal? tpoVah,
        decimal? tpoVal,
        decimal? volumeVah,
        decimal? volumeVal,
        PriceVolumeCapability priceVolumeCapability,
        ProfileDataQuality dataQuality,
        IReadOnlyList<string> knownLimitations)
    {
        TpoCounts = tpoCounts;
        Volumes = volumes;
        TotalTpoCount = totalTpoCount;
        TotalExecutedVolume = totalExecutedVolume;
        ProfileHigh = profileHigh;
        ProfileLow = profileLow;
        TpoPoc = tpoPoc;
        VolumePoc = volumePoc;
        TpoVah = tpoVah;
        TpoVal = tpoVal;
        VolumeVah = volumeVah;
        VolumeVal = volumeVal;
        PriceVolumeCapability = priceVolumeCapability;
        DataQuality = dataQuality;
        KnownLimitations = knownLimitations;
    }

    public IReadOnlyDictionary<long, int> TpoCounts { get; }
    public IReadOnlyDictionary<long, decimal> Volumes { get; }
    public int TotalTpoCount { get; }
    public decimal TotalExecutedVolume { get; }
    public decimal? ProfileHigh { get; }
    public decimal? ProfileLow { get; }
    public decimal? TpoPoc { get; }
    public decimal? VolumePoc { get; }
    public decimal? TpoVah { get; }
    public decimal? TpoVal { get; }
    public decimal? VolumeVah { get; }
    public decimal? VolumeVal { get; }
    public PriceVolumeCapability PriceVolumeCapability { get; }
    public ProfileDataQuality DataQuality { get; }
    public IReadOnlyList<string> KnownLimitations { get; }
}
