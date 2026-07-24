using System.Drawing;
using ATAS.Indicators;
using GC.AuctionFlow.UI;
using OFT.Rendering.Context;
using OFT.Rendering.Tools;

namespace GC.AuctionFlow.Atas;

/// <summary>
/// Minimal primary / composite / structural-reference overlay.
/// Reads immutable OverlayViewModel only. Structural Reference labels use
/// REFERENCE_OVERLAY_POLICY_V1 collision suppression (viewport Y).
/// No DataSeries writes, no profile/reference calculation, no file I/O.
/// </summary>
public sealed class PrimaryProfileOverlayRenderer : IDisposable
{
    private readonly RenderFont _labelFont = new("Segoe UI", 9f, FontStyle.Regular);
    private PrimaryProfileOverlayViewModel? _viewModel;
    private bool _disposed;

    public void Update(PrimaryProfileOverlayViewModel? viewModel)
    {
        Volatile.Write(ref _viewModel, viewModel);
    }

    public void Render(RenderContext context, DrawingLayouts layout, IChart? chart)
    {
        if (_disposed || context is null || chart is null)
            return;
        if (layout != DrawingLayouts.Final && layout != DrawingLayouts.LatestBar)
            return;

        var vm = Volatile.Read(ref _viewModel);
        if (vm is null || vm.Levels.Count == 0)
            return;

        try
        {
            var container = chart.PriceChartContainer;
            if (container is null)
                return;

            var first = container.FirstVisibleBarNumber;
            var last = container.LastVisibleBarNumber;
            var x0 = container.GetXByBar(first, false);
            var x1 = container.GetXByBar(last, false);
            if (x1 <= x0) x1 = x0 + 800;

            // Re-apply Structural Reference label collision with real chart Y (viewport/scale).
            var levels = ResolveLevelsForRender(vm.Levels, price => container.GetYByPrice(price, false));

            foreach (var level in levels)
            {
                var y = container.GetYByPrice(level.Price, false);
                var color = ColorFor(level.Kind);
                if (level.ShowLine)
                    context.DrawLine(new RenderPen(color, 1f), x0, y, x1, y);
                if (level.ShowLabel)
                    context.DrawString(level.Label, _labelFont, color, x0 + 4, y - 12);
            }
        }
        catch
        {
            // Contained.
        }
    }

    /// <summary>
    /// For Structural Reference levels, re-run collision with chart Y.
    /// Non-REF levels pass through unchanged. Does not mutate engine state.
    /// </summary>
    public static IReadOnlyList<ProfileOverlayLevel> ResolveLevelsForRender(
        IReadOnlyList<ProfileOverlayLevel> levels,
        Func<decimal, int> priceToY)
    {
        if (levels is null || levels.Count == 0)
            return Array.Empty<ProfileOverlayLevel>();

        var refLevels = levels.Where(l => l.Kind == ProfileOverlayKind.StructuralReference).ToArray();
        if (refLevels.Length == 0)
            return levels;

        // Priority order (highest first) for collision — OverlayPriority descending.
        var ordered = refLevels
            .OrderByDescending(l => l.OverlayPriority)
            .ThenBy(l => l.Price)
            .ThenBy(l => l.ConstituentReferenceIds.Count == 0 ? "" : l.ConstituentReferenceIds[0], StringComparer.Ordinal)
            .ToArray();

        var acceptedYs = new List<int>(ordered.Length);
        var resolved = new List<ProfileOverlayLevel>(ordered.Length);
        var minSep = ReferenceOverlayDisplayPolicy.DefaultMinLabelSeparationPx;

        foreach (var level in ordered)
        {
            var y = priceToY(level.Price);
            var collide = false;
            for (var i = 0; i < acceptedYs.Count; i++)
            {
                if (Math.Abs(acceptedYs[i] - y) < minSep)
                {
                    collide = true;
                    break;
                }
            }

            if (collide)
            {
                resolved.Add(new ProfileOverlayLevel(
                    level.Label, level.Price, level.Kind, level.ConstituentReferenceIds,
                    showLabel: false, showLine: level.ShowLine, overlayPriority: level.OverlayPriority,
                    maturityToken: level.MaturityToken, overlayPolicyVersion: level.OverlayPolicyVersion));
            }
            else
            {
                acceptedYs.Add(y);
                resolved.Add(new ProfileOverlayLevel(
                    level.Label, level.Price, level.Kind, level.ConstituentReferenceIds,
                    showLabel: true, showLine: level.ShowLine, overlayPriority: level.OverlayPriority,
                    maturityToken: level.MaturityToken, overlayPolicyVersion: level.OverlayPolicyVersion));
            }
        }

        // Keep non-REF levels (none when REF overlay owns the VM) + REF resolved by price.
        var nonRef = levels.Where(l => l.Kind != ProfileOverlayKind.StructuralReference);
        return nonRef
            .Concat(resolved.OrderBy(l => l.Price).ThenBy(l => l.Label, StringComparer.Ordinal))
            .ToArray();
    }

    private static Color ColorFor(ProfileOverlayKind kind) => kind switch
    {
        ProfileOverlayKind.TpoPoc => Color.FromArgb(255, 255, 200, 60),
        ProfileOverlayKind.Vpoc => Color.FromArgb(255, 80, 180, 255),
        ProfileOverlayKind.TpoVah or ProfileOverlayKind.TpoVal => Color.FromArgb(220, 180, 160, 80),
        ProfileOverlayKind.CompositeTpoPoc => Color.FromArgb(255, 255, 140, 40),
        ProfileOverlayKind.CompositeVpoc => Color.FromArgb(255, 40, 140, 220),
        ProfileOverlayKind.CompositeTpoVah or ProfileOverlayKind.CompositeTpoVal => Color.FromArgb(200, 200, 120, 60),
        ProfileOverlayKind.CompositeVolumeVah or ProfileOverlayKind.CompositeVolumeVal => Color.FromArgb(200, 100, 140, 180),
        ProfileOverlayKind.StructuralReference => Color.FromArgb(230, 200, 200, 200),
        _ => Color.FromArgb(220, 120, 160, 200)
    };

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Volatile.Write(ref _viewModel, null);
    }
}
