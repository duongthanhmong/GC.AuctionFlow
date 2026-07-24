using GC.AuctionFlow.Profile;

namespace GC.AuctionFlow.Composite;

/// <summary>Merge-evidence metrics only — no automatic Production merge/close.</summary>
public sealed class CompositeMergeEvidence
{
    public const string EvidenceVersion = "1.0.0";

    public CompositeMergeEvidence(
        string leftAuctionId,
        string rightAuctionId,
        decimal? tpoValueOverlapRatio,
        decimal? volumeValueOverlapRatio,
        decimal? profileRangeOverlapRatio,
        long? tpoPocDisplacementTicks,
        long? volumePocDisplacementTicks,
        long? tpoValueCentroidDisplacementTicks,
        long? volumeValueCentroidDisplacementTicks,
        long? compositeRangeExpansionUpTicks,
        long? compositeRangeExpansionDownTicks,
        decimal? outsideCompositeTpoShare,
        decimal? outsideCompositeVolumeShare,
        decimal? contributionTpoShare,
        decimal? contributionVolumeShare,
        PriceVolumeCapability priceVolumeCapability,
        ProfileDataQuality dataQuality,
        IReadOnlyList<string> knownLimitations,
        string provenance)
    {
        LeftAuctionId = leftAuctionId;
        RightAuctionId = rightAuctionId;
        TpoValueOverlapRatio = tpoValueOverlapRatio;
        VolumeValueOverlapRatio = volumeValueOverlapRatio;
        ProfileRangeOverlapRatio = profileRangeOverlapRatio;
        TpoPocDisplacementTicks = tpoPocDisplacementTicks;
        VolumePocDisplacementTicks = volumePocDisplacementTicks;
        TpoValueCentroidDisplacementTicks = tpoValueCentroidDisplacementTicks;
        VolumeValueCentroidDisplacementTicks = volumeValueCentroidDisplacementTicks;
        CompositeRangeExpansionUpTicks = compositeRangeExpansionUpTicks;
        CompositeRangeExpansionDownTicks = compositeRangeExpansionDownTicks;
        OutsideCompositeTpoShare = outsideCompositeTpoShare;
        OutsideCompositeVolumeShare = outsideCompositeVolumeShare;
        ContributionTpoShare = contributionTpoShare;
        ContributionVolumeShare = contributionVolumeShare;
        PriceVolumeCapability = priceVolumeCapability;
        ParticipationQuality = ProfileDataQuality.Unknown; // deferred classifier
        ThinContributionFlag = null; // Unknown until later classifier
        DataQuality = dataQuality;
        KnownLimitations = knownLimitations ?? Array.Empty<string>();
        Provenance = provenance;
    }

    public string LeftAuctionId { get; }
    public string RightAuctionId { get; }
    public decimal? TpoValueOverlapRatio { get; }
    public decimal? VolumeValueOverlapRatio { get; }
    public decimal? ProfileRangeOverlapRatio { get; }
    public long? TpoPocDisplacementTicks { get; }
    public long? VolumePocDisplacementTicks { get; }
    public long? TpoValueCentroidDisplacementTicks { get; }
    public long? VolumeValueCentroidDisplacementTicks { get; }
    public long? CompositeRangeExpansionUpTicks { get; }
    public long? CompositeRangeExpansionDownTicks { get; }
    public decimal? OutsideCompositeTpoShare { get; }
    public decimal? OutsideCompositeVolumeShare { get; }
    public decimal? ContributionTpoShare { get; }
    public decimal? ContributionVolumeShare { get; }
    public PriceVolumeCapability PriceVolumeCapability { get; }
    public ProfileDataQuality ParticipationQuality { get; }
    public bool? ThinContributionFlag { get; }
    public ProfileDataQuality DataQuality { get; }
    public IReadOnlyList<string> KnownLimitations { get; }
    public string Provenance { get; }
    public string Version => EvidenceVersion;
}

public static class CompositeMergeEvidenceCalculator
{
    public static decimal? ValueAreaOverlapRatio(decimal? aVal, decimal? aVah, decimal? bVal, decimal? bVah)
    {
        if (aVal is null || aVah is null || bVal is null || bVah is null)
            return null;
        var aLo = Math.Min(aVal.Value, aVah.Value);
        var aHi = Math.Max(aVal.Value, aVah.Value);
        var bLo = Math.Min(bVal.Value, bVah.Value);
        var bHi = Math.Max(bVal.Value, bVah.Value);
        var aWidth = aHi - aLo;
        var bWidth = bHi - bLo;
        if (aWidth <= 0m || bWidth <= 0m)
            return null; // zero-width — unavailable, not zero
        var overlap = Math.Max(0m, Math.Min(aHi, bHi) - Math.Max(aLo, bLo));
        var union = Math.Max(aHi, bHi) - Math.Min(aLo, bLo);
        if (union <= 0m)
            return null;
        return overlap / union;
    }

    public static decimal? RangeOverlapRatio(decimal? aLo, decimal? aHi, decimal? bLo, decimal? bHi) =>
        ValueAreaOverlapRatio(aLo, aHi, bLo, bHi);

    public static long? DisplacementTicks(decimal? a, decimal? b, PriceGrid grid)
    {
        if (a is null || b is null) return null;
        if (!grid.TryToTickIndex(a.Value, out var ta) || !grid.TryToTickIndex(b.Value, out var tb))
            return null;
        return Math.Abs(ta - tb);
    }

    public static long? CentroidTick(IReadOnlyDictionary<long, int> counts)
    {
        if (counts is null || counts.Count == 0) return null;
        long sumW = 0;
        decimal sum = 0m;
        foreach (var kv in counts)
        {
            sum += kv.Key * (decimal)kv.Value;
            sumW += kv.Value;
        }

        if (sumW <= 0) return null;
        return (long)decimal.Round(sum / sumW, 0, MidpointRounding.AwayFromZero);
    }

    public static long? CentroidTick(IReadOnlyDictionary<long, decimal> volumes)
    {
        if (volumes is null || volumes.Count == 0) return null;
        decimal sumW = 0m;
        decimal sum = 0m;
        foreach (var kv in volumes)
        {
            sum += kv.Key * kv.Value;
            sumW += kv.Value;
        }

        if (sumW <= 0m) return null;
        return (long)decimal.Round(sum / sumW, 0, MidpointRounding.AwayFromZero);
    }

    public static decimal? OutsideShare(
        IReadOnlyDictionary<long, int> contribution,
        long compositeLowTick,
        long compositeHighTick)
    {
        if (contribution.Count == 0) return null;
        var total = contribution.Values.Sum();
        if (total <= 0) return null;
        var outside = contribution.Where(kv => kv.Key < compositeLowTick || kv.Key > compositeHighTick).Sum(kv => kv.Value);
        return (decimal)outside / total;
    }

    public static decimal? OutsideShare(
        IReadOnlyDictionary<long, decimal> contribution,
        long compositeLowTick,
        long compositeHighTick)
    {
        if (contribution.Count == 0) return null;
        var total = contribution.Values.Sum();
        if (total <= 0m) return null;
        var outside = contribution.Where(kv => kv.Key < compositeLowTick || kv.Key > compositeHighTick).Sum(kv => kv.Value);
        return outside / total;
    }

    public static CompositeMergeEvidence ForAdjacent(
        CompositeAuctionContribution left,
        CompositeAuctionContribution right,
        PriceGrid grid)
    {
        var limitations = new List<string>();
        var tpoOverlap = ValueAreaOverlapRatio(left.TpoVal, left.TpoVah, right.TpoVal, right.TpoVah);
        var volOverlap = ValueAreaOverlapRatio(left.VolumeVal, left.VolumeVah, right.VolumeVal, right.VolumeVah);
        if (tpoOverlap is null) limitations.Add("TPO_VA_OVERLAP_UNAVAILABLE");
        if (volOverlap is null) limitations.Add("VOLUME_VA_OVERLAP_UNAVAILABLE");

        var rangeOverlap = RangeOverlapRatio(left.ProfileLow, left.ProfileHigh, right.ProfileLow, right.ProfileHigh);
        var tpoDisp = DisplacementTicks(left.TpoPoc, right.TpoPoc, grid);
        var volDisp = DisplacementTicks(left.VolumePoc, right.VolumePoc, grid);

        var leftTpoC = CentroidTick(left.PriceLevelTpoCounts);
        var rightTpoC = CentroidTick(right.PriceLevelTpoCounts);
        long? tpoCentDisp = leftTpoC is long a && rightTpoC is long b ? Math.Abs(a - b) : null;

        var leftVolC = CentroidTick(left.PriceLevelVolumes);
        var rightVolC = CentroidTick(right.PriceLevelVolumes);
        long? volCentDisp = leftVolC is long c && rightVolC is long d ? Math.Abs(c - d) : null;

        var cap = left.PriceVolumeCapability == PriceVolumeCapability.Exact
                  && right.PriceVolumeCapability == PriceVolumeCapability.Exact
            ? PriceVolumeCapability.Exact
            : PriceVolumeCapability.Unavailable;

        return new CompositeMergeEvidence(
            left.AuctionId,
            right.AuctionId,
            tpoOverlap,
            volOverlap,
            rangeOverlap,
            tpoDisp,
            volDisp,
            tpoCentDisp,
            volCentDisp,
            null, null, null, null, null, null,
            cap,
            left.DataQuality == ProfileDataQuality.Complete && right.DataQuality == ProfileDataQuality.Complete
                ? ProfileDataQuality.Complete
                : ProfileDataQuality.Partial,
            limitations,
            "AdjacentContributionPair");
    }

    public static CompositeMergeEvidence ForCandidateVsComposite(
        CompositeAuctionContribution candidate,
        CompositeAggregateResult composite,
        PriceGrid grid)
    {
        var limitations = new List<string> { "CANDIDATE_VS_COMPOSITE" };
        var tpoOverlap = ValueAreaOverlapRatio(candidate.TpoVal, candidate.TpoVah, composite.TpoVal, composite.TpoVah);
        var volOverlap = ValueAreaOverlapRatio(candidate.VolumeVal, candidate.VolumeVah, composite.VolumeVal, composite.VolumeVah);
        var rangeOverlap = RangeOverlapRatio(candidate.ProfileLow, candidate.ProfileHigh, composite.ProfileLow, composite.ProfileHigh);
        var tpoDisp = DisplacementTicks(candidate.TpoPoc, composite.TpoPoc, grid);
        var volDisp = DisplacementTicks(candidate.VolumePoc, composite.VolumePoc, grid);

        long? expandUp = null, expandDown = null;
        if (candidate.ProfileHigh is decimal ch && composite.ProfileHigh is decimal oh
            && candidate.ProfileLow is decimal cl && composite.ProfileLow is decimal ol)
        {
            if (grid.TryToTickIndex(ch, out var cht) && grid.TryToTickIndex(oh, out var oht))
                expandUp = Math.Max(0, cht - oht);
            if (grid.TryToTickIndex(cl, out var clt) && grid.TryToTickIndex(ol, out var olt))
                expandDown = Math.Max(0, olt - clt);
        }

        decimal? outsideTpo = null, outsideVol = null;
        if (composite.TpoCounts.Count > 0)
        {
            var cLo = composite.TpoCounts.Keys.Min();
            var cHi = composite.TpoCounts.Keys.Max();
            outsideTpo = OutsideShare(candidate.PriceLevelTpoCounts, cLo, cHi);
            if (candidate.PriceVolumeCapability == PriceVolumeCapability.Exact)
                outsideVol = OutsideShare(candidate.PriceLevelVolumes, cLo, cHi);
        }

        decimal? tpoShare = composite.TotalTpoCount > 0
            ? (decimal)candidate.TotalTpoCount / (composite.TotalTpoCount + candidate.TotalTpoCount)
            : null;
        decimal? volShare = composite.TotalExecutedVolume > 0m && candidate.PriceVolumeCapability == PriceVolumeCapability.Exact
            ? candidate.TotalExecutedVolume / (composite.TotalExecutedVolume + candidate.TotalExecutedVolume)
            : null;

        return new CompositeMergeEvidence(
            "COMPOSITE",
            candidate.AuctionId,
            tpoOverlap,
            volOverlap,
            rangeOverlap,
            tpoDisp,
            volDisp,
            null, null,
            expandUp,
            expandDown,
            outsideTpo,
            outsideVol,
            tpoShare,
            volShare,
            candidate.PriceVolumeCapability,
            candidate.DataQuality,
            limitations,
            "CandidateVsComposite");
    }

    /// <summary>Shadow-only. Unset thresholds → NotCalibrated.</summary>
    public static CompositeEvidenceState EvaluateShadow(
        CompositePolicyConfig policy,
        IReadOnlyList<CompositeMergeEvidence> evidence)
    {
        if (!policy.EnableShadowEvidence)
            return CompositeEvidenceState.NotEvaluated;
        if (policy.ShadowMinValueOverlapRatio is null && policy.ShadowMaxPocDisplacementTicks is null)
            return CompositeEvidenceState.NotCalibrated;
        if (evidence.Count == 0)
            return CompositeEvidenceState.InsufficientData;

        var merge = false;
        var sep = false;
        foreach (var e in evidence)
        {
            if (policy.ShadowMinValueOverlapRatio is decimal minOv && e.TpoValueOverlapRatio is decimal ov)
            {
                if (ov >= minOv) merge = true;
                else sep = true;
            }

            if (policy.ShadowMaxPocDisplacementTicks is long maxDisp && e.TpoPocDisplacementTicks is long disp)
            {
                if (disp <= maxDisp) merge = true;
                else sep = true;
            }
        }

        if (merge && sep) return CompositeEvidenceState.Conflicted;
        if (merge) return CompositeEvidenceState.MergeEvidencePresent;
        if (sep) return CompositeEvidenceState.SeparationEvidencePresent;
        return CompositeEvidenceState.InsufficientData;
    }
}
