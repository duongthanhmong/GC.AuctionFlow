using System.Drawing;
using ATAS.Indicators;
using GC.AuctionFlow.OptionFlow;
using OFT.Rendering.Context;
using OFT.Rendering.Tools;

namespace GC.AuctionFlow.Atas;

/// <summary>
/// Draws the OptionFlow / GEX overlay: horizontal lines at walls / flip / ATM /
/// max-pain, plus a compact top-right panel (regime / EM / skew / dealer posture).
/// Reads an immutable <see cref="OptionFlowOverlayViewModel"/> only — no file IO,
/// no DataSeries writes, no GEX computation. Diagnostics-gated by the indicator;
/// an empty view (null GexContext upstream) draws nothing.
/// </summary>
public sealed class OptionFlowOverlayRenderer : IDisposable
{
    private readonly RenderFont _lineFont = new("Segoe UI", 9f, FontStyle.Regular);
    private readonly RenderFont _panelFont = new("Segoe UI", 9f, FontStyle.Bold);
    private OptionFlowOverlayViewModel? _viewModel;
    private bool _disposed;

    public void Update(OptionFlowOverlayViewModel? viewModel)
    {
        Volatile.Write(ref _viewModel, viewModel);
    }

    public void Render(RenderContext context, DrawingLayouts layout, IChart? chart, int panelMarginX, int panelMarginY)
    {
        if (_disposed || context is null || chart is null)
            return;
        if (layout != DrawingLayouts.Final && layout != DrawingLayouts.LatestBar)
            return;

        var vm = Volatile.Read(ref _viewModel);
        if (vm is null || !vm.HasData)
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

            foreach (var line in vm.Lines)
            {
                var y = container.GetYByPrice(line.Price, false);
                var color = ColorFor(line.Kind);
                context.DrawLine(new RenderPen(color, 1f), x0, y, x1, y);
                context.DrawString(line.Label, _lineFont, color, x0 + 4, y - 12);
            }

            DrawPanel(context, vm, x0 + panelMarginX, panelMarginY);
        }
        catch
        {
            // Contained: render must never throw into ATAS.
        }
    }

    private void DrawPanel(RenderContext context, OptionFlowOverlayViewModel vm, int x, int y)
    {
        const int lineHeight = 14;
        var yy = y;
        foreach (var line in vm.PanelLines)
        {
            context.DrawString(line, _panelFont, Color.FromArgb(235, 220, 220, 235), x, yy);
            yy += lineHeight;
        }
    }

    private static Color ColorFor(OptionFlowLineKind kind) => kind switch
    {
        OptionFlowLineKind.Flip => Color.FromArgb(255, 255, 230, 90),     // zero-gamma: bright yellow
        OptionFlowLineKind.CallWall => Color.FromArgb(230, 235, 90, 90),  // resistance: red
        OptionFlowLineKind.PutWall => Color.FromArgb(230, 90, 200, 120),  // support: green
        OptionFlowLineKind.Atm => Color.FromArgb(230, 120, 200, 235),     // ATM: cyan
        OptionFlowLineKind.MaxPain => Color.FromArgb(220, 200, 120, 220), // max pain: magenta
        OptionFlowLineKind.Peak => Color.FromArgb(210, 235, 170, 80),     // gex peak: orange
        _ => Color.FromArgb(200, 170, 170, 190)
    };

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Volatile.Write(ref _viewModel, null);
    }
}
