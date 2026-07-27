using System.Drawing;
using ATAS.Indicators;
using GC.AuctionFlow.Core;
using GC.AuctionFlow.UI;
using OFT.Rendering.Context;
using OFT.Rendering.Tools;

namespace GC.AuctionFlow.Atas;

/// <summary>
/// ATAS OnRender adapter for Auction GPS Card. Reads immutable ViewModel only.
/// No file I/O, no waits, no JSON, no recorder mutation, no DataSeries writes.
/// </summary>
public sealed class AuctionGpsCardRenderer : IDisposable
{
    private readonly RenderFont _titleFont = new("Segoe UI", 11f, FontStyle.Bold);
    private readonly RenderFont _bodyFont = new("Consolas", 10f, FontStyle.Regular);
    private AuctionGpsCardViewModel? _viewModel;
    private bool _showDiagnostics;
    private bool _compact;
    private bool _disposed;
    private int _marginX = 12;
    private int _marginY = 12;

    public void Update(AuctionGpsCardViewModel? viewModel, bool showDiagnostics, bool compact = false)
    {
        Volatile.Write(ref _viewModel, viewModel);
        Volatile.Write(ref _showDiagnostics, showDiagnostics);
        Volatile.Write(ref _compact, compact);
    }

    public void SetMargins(int marginX, int marginY)
    {
        _marginX = Math.Max(0, marginX);
        _marginY = Math.Max(0, marginY);
    }

    public void Render(RenderContext context, DrawingLayouts layout)
    {
        if (_disposed || context is null)
            return;
        if (layout != DrawingLayouts.Final && layout != DrawingLayouts.LatestBar)
            return;

        var vm = Volatile.Read(ref _viewModel);
        if (vm is null)
            return;

        try
        {
            var showDiag = Volatile.Read(ref _showDiagnostics);
            var compact = Volatile.Read(ref _compact);
            var lines = vm.AllLines(showDiag, compact);
            if (lines.Count == 0)
                return;

            var lineHeight = 14;
            var padding = 8;
            var maxWidth = 0;
            foreach (var line in lines)
            {
                var size = context.MeasureString(line, line == vm.Title ? _titleFont : _bodyFont);
                if (size.Width > maxWidth)
                    maxWidth = size.Width;
            }

            var boxW = maxWidth + padding * 2;
            var boxH = lines.Count * lineHeight + padding * 2;
            var rect = new Rectangle(_marginX, _marginY, boxW, boxH);

            var (fill, border, text) = ColorsFor(vm.DataState);
            context.FillRectangle(fill, rect);
            context.DrawRectangle(new RenderPen(border, 1f), rect);

            var y = rect.Y + padding;
            for (var i = 0; i < lines.Count; i++)
            {
                var font = i == 0 ? _titleFont : _bodyFont;
                context.DrawString(lines[i], font, text, rect.X + padding, y);
                y += lineHeight;
            }
        }
        catch
        {
            // Contained — never escape into ATAS.
        }
    }

    private static (Color Fill, Color Border, Color Text) ColorsFor(DataState state)
    {
        // Theme-safe: high-contrast panels readable on dark and light charts.
        return state switch
        {
            DataState.Invalid => (Color.FromArgb(220, 60, 20, 20), Color.FromArgb(255, 220, 80, 80), Color.FromArgb(255, 255, 230, 230)),
            DataState.Ready => (Color.FromArgb(210, 20, 50, 30), Color.FromArgb(255, 80, 200, 120), Color.FromArgb(255, 230, 255, 235)),
            _ => (Color.FromArgb(210, 45, 40, 15), Color.FromArgb(255, 220, 180, 60), Color.FromArgb(255, 255, 245, 220))
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Volatile.Write(ref _viewModel, null);
    }
}
