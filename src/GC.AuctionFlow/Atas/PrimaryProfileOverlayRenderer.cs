using System.Drawing;
using ATAS.Indicators;
using GC.AuctionFlow.UI;
using OFT.Rendering.Context;
using OFT.Rendering.Tools;

namespace GC.AuctionFlow.Atas;

/// <summary>
/// Minimal primary profile overlay. Reads immutable OverlayViewModel only.
/// Observed ATAS API: IChartContainer.GetYByPrice(decimal, bool), RenderContext.DrawLine / DrawString.
/// No DataSeries writes, no profile calculation, no file I/O.
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

            foreach (var level in vm.Levels)
            {
                var y = container.GetYByPrice(level.Price, false);
                var color = ColorFor(level.Kind);
                context.DrawLine(new RenderPen(color, 1f), x0, y, x1, y);
                context.DrawString(level.Label + " " + level.Price.ToString("0.0"), _labelFont, color, x0 + 4, y - 12);
            }
        }
        catch
        {
            // Contained.
        }
    }

    private static Color ColorFor(ProfileOverlayKind kind) => kind switch
    {
        ProfileOverlayKind.TpoPoc => Color.FromArgb(255, 255, 200, 60),
        ProfileOverlayKind.Vpoc => Color.FromArgb(255, 80, 180, 255),
        ProfileOverlayKind.TpoVah or ProfileOverlayKind.TpoVal => Color.FromArgb(220, 180, 160, 80),
        _ => Color.FromArgb(220, 120, 160, 200)
    };

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Volatile.Write(ref _viewModel, null);
    }
}
