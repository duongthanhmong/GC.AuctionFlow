using System.Globalization;
using GC.AuctionFlow.Reference;

namespace GC.AuctionFlow.UI;

/// <summary>
/// Rendering-only Structural Reference overlay policy.
/// Does not alter REFERENCE_POLICY_V1, registry, identity, or confluence semantics.
/// </summary>
public static class ReferenceOverlayDisplayPolicy
{
    public const string PolicyVersion = "REFERENCE_OVERLAY_POLICY_V1";

    /// <summary>Seed value, subject to sensitivity test — matches Segoe UI 9pt label height + padding.</summary>
    public const int DefaultMinLabelSeparationPx = 14;

    /// <summary>
    /// Build one display row per unique exact confluence zone.
    /// Constituent IDs are retained; no individual constituent rows are emitted.
    /// </summary>
    public static IReadOnlyList<ReferenceOverlayDisplayRow> BuildUniqueZoneRows(
        StructuralReferenceSetSnapshot set,
        long? currentPriceTick = null)
    {
        if (set is null)
            return Array.Empty<ReferenceOverlayDisplayRow>();
        if (set.ModuleState is not (StructuralReferenceModuleState.Ready or StructuralReferenceModuleState.Partial))
            return Array.Empty<ReferenceOverlayDisplayRow>();

        var rows = new List<ReferenceOverlayDisplayRow>(set.ConfluenceGroups.Count);
        foreach (var g in set.ConfluenceGroups)
        {
            var maturity = ResolveMaturityToken(g);
            var types = string.Join(" + ", g.ConstituentTypes.Select(ShortType).Distinct().Take(4));
            var label = g.ConstituentCount > 1
                ? $"REF {maturity} ×{g.ConstituentCount} | {types} | {Fmt(g.ZoneLow)}"
                : $"REF {maturity} | {types} | {Fmt(g.ZoneLow)}";

            rows.Add(new ReferenceOverlayDisplayRow(
                price: g.ZoneLow,
                zoneLowTick: g.ZoneLowTick,
                zoneHighTick: g.ZoneHighTick,
                label: label,
                maturityToken: maturity,
                constituentCount: g.ConstituentCount,
                constituentReferenceIds: g.ConstituentReferenceIds,
                constituentTypes: g.ConstituentTypes,
                priority: ComputePriority(g),
                showLabel: true,
                showLine: true));
        }

        return rows
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => DistanceFromCurrentTicks(r, currentPriceTick))
            .ThenBy(r => r.ZoneLowTick)
            .ThenBy(r => r.ConstituentReferenceIds.Count == 0 ? "" : r.ConstituentReferenceIds[0], StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Suppress lower-priority labels that collide vertically. Lines remain.
    /// Same inputs + Y map → same selected labels. No engine mutation.
    /// </summary>
    public static IReadOnlyList<ReferenceOverlayDisplayRow> ApplyLabelCollision(
        IReadOnlyList<ReferenceOverlayDisplayRow> orderedByPriority,
        Func<decimal, int> priceToY,
        int minSeparationPx = DefaultMinLabelSeparationPx)
    {
        if (orderedByPriority is null || orderedByPriority.Count == 0)
            return Array.Empty<ReferenceOverlayDisplayRow>();
        if (priceToY is null) throw new ArgumentNullException(nameof(priceToY));
        if (minSeparationPx < 1) minSeparationPx = 1;

        var acceptedYs = new List<int>(orderedByPriority.Count);
        var result = new List<ReferenceOverlayDisplayRow>(orderedByPriority.Count);

        // Input is expected priority-ordered (highest first).
        foreach (var row in orderedByPriority)
        {
            var y = priceToY(row.Price);
            var collide = false;
            for (var i = 0; i < acceptedYs.Count; i++)
            {
                if (Math.Abs(acceptedYs[i] - y) < minSeparationPx)
                {
                    collide = true;
                    break;
                }
            }

            if (collide)
            {
                result.Add(row.WithShowLabel(false));
            }
            else
            {
                acceptedYs.Add(y);
                result.Add(row.WithShowLabel(true));
            }
        }

        return result
            .OrderBy(r => r.ZoneLowTick)
            .ThenBy(r => r.ConstituentReferenceIds.Count == 0 ? "" : r.ConstituentReferenceIds[0], StringComparer.Ordinal)
            .ToArray();
    }

    public static string ResolveMaturityToken(ReferenceConfluenceGroup g)
    {
        if (g.IsMixedMaturity) return "MIXED";
        if (g.ConfirmedCount > 0) return "CONFIRMED";
        return "DEVELOPING";
    }

    public static int ComputePriority(ReferenceConfluenceGroup g)
    {
        // Higher = more important. Discrete buckets only — not a confidence/probability score.
        var p = 0;
        if (g.ConstituentCount > 1) p += 1000;
        if (g.ConstituentTypes.Any(IsComposite)) p += 400;
        if (g.ConstituentTypes.Any(IsPreviousPrimary)) p += 200;
        if (g.ConfirmedCount > 0) p += 100;
        if (g.DevelopingCount > 0) p += 10;
        p += Math.Min(g.ConstituentCount, 50);
        return p;
    }

    public static long DistanceFromCurrentTicks(ReferenceOverlayDisplayRow row, long? currentPriceTick)
    {
        if (currentPriceTick is null)
            return Math.Abs(row.ZoneLowTick);
        if (row.ZoneLowTick <= currentPriceTick.Value && currentPriceTick.Value <= row.ZoneHighTick)
            return 0;
        if (currentPriceTick.Value < row.ZoneLowTick)
            return row.ZoneLowTick - currentPriceTick.Value;
        return currentPriceTick.Value - row.ZoneHighTick;
    }

    private static bool IsComposite(ReferenceType t) =>
        t is ReferenceType.CompositeTpoPoc or ReferenceType.CompositeTpoVah or ReferenceType.CompositeTpoVal
            or ReferenceType.CompositeVpoc or ReferenceType.CompositeVolumeVah or ReferenceType.CompositeVolumeVal
            or ReferenceType.CompositeRangeHigh or ReferenceType.CompositeRangeLow;

    private static bool IsPreviousPrimary(ReferenceType t) =>
        t.ToString().StartsWith("PreviousPrimary", StringComparison.Ordinal);

    private static string ShortType(ReferenceType t) => t switch
    {
        ReferenceType.PreviousPrimaryAuctionHigh => "PREV HIGH",
        ReferenceType.PreviousPrimaryAuctionLow => "PREV LOW",
        ReferenceType.PreviousPrimaryTpoPoc => "PREV TPO POC",
        ReferenceType.PreviousPrimaryTpoVah => "PREV TPO VAH",
        ReferenceType.PreviousPrimaryTpoVal => "PREV TPO VAL",
        ReferenceType.PreviousPrimaryVpoc => "PREV VPOC",
        ReferenceType.PreviousPrimaryVolumeVah => "PREV VOL VAH",
        ReferenceType.PreviousPrimaryVolumeVal => "PREV VOL VAL",
        ReferenceType.CurrentPrimaryAuctionHigh => "CUR HIGH",
        ReferenceType.CurrentPrimaryAuctionLow => "CUR LOW",
        ReferenceType.CurrentPrimaryTpoPoc => "CUR TPO POC",
        ReferenceType.CurrentPrimaryTpoVah => "CUR TPO VAH",
        ReferenceType.CurrentPrimaryTpoVal => "CUR TPO VAL",
        ReferenceType.CurrentPrimaryVpoc => "CUR VPOC",
        ReferenceType.CurrentPrimaryVolumeVah => "CUR VOL VAH",
        ReferenceType.CurrentPrimaryVolumeVal => "CUR VOL VAL",
        ReferenceType.CompositeTpoPoc => "CMP TPO POC",
        ReferenceType.CompositeTpoVah => "CMP TPO VAH",
        ReferenceType.CompositeTpoVal => "CMP TPO VAL",
        ReferenceType.CompositeVpoc => "CMP VPOC",
        ReferenceType.CompositeVolumeVah => "CMP VOL VAH",
        ReferenceType.CompositeVolumeVal => "CMP VOL VAL",
        ReferenceType.CompositeRangeHigh => "CMP RANGE HIGH",
        ReferenceType.CompositeRangeLow => "CMP RANGE LOW",
        _ => t.ToString()
    };

    private static string Fmt(decimal v) => v.ToString("0.0", CultureInfo.InvariantCulture);
}

/// <summary>Immutable Structural Reference overlay display row. Rendering policy only.</summary>
public sealed class ReferenceOverlayDisplayRow
{
    public ReferenceOverlayDisplayRow(
        decimal price,
        long zoneLowTick,
        long zoneHighTick,
        string label,
        string maturityToken,
        int constituentCount,
        IReadOnlyList<string> constituentReferenceIds,
        IReadOnlyList<ReferenceType> constituentTypes,
        int priority,
        bool showLabel,
        bool showLine)
    {
        Price = price;
        ZoneLowTick = zoneLowTick;
        ZoneHighTick = zoneHighTick;
        Label = label ?? "";
        MaturityToken = maturityToken ?? "";
        ConstituentCount = constituentCount;
        ConstituentReferenceIds = constituentReferenceIds ?? Array.Empty<string>();
        ConstituentTypes = constituentTypes ?? Array.Empty<ReferenceType>();
        Priority = priority;
        ShowLabel = showLabel;
        ShowLine = showLine;
    }

    public decimal Price { get; }
    public long ZoneLowTick { get; }
    public long ZoneHighTick { get; }
    public string Label { get; }
    public string MaturityToken { get; }
    public int ConstituentCount { get; }
    public IReadOnlyList<string> ConstituentReferenceIds { get; }
    public IReadOnlyList<ReferenceType> ConstituentTypes { get; }
    public int Priority { get; }
    public bool ShowLabel { get; }
    public bool ShowLine { get; }
    public string OverlayPolicyVersion => ReferenceOverlayDisplayPolicy.PolicyVersion;

    public ReferenceOverlayDisplayRow WithShowLabel(bool showLabel) =>
        new(Price, ZoneLowTick, ZoneHighTick, Label, MaturityToken, ConstituentCount,
            ConstituentReferenceIds, ConstituentTypes, Priority, showLabel, ShowLine);
}
