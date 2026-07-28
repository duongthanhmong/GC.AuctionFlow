namespace GC.AuctionFlow.OptionFlow;

/// <summary>Semantic kind of an option-flow line, so the renderer can colour it.</summary>
public enum OptionFlowLineKind
{
    Flip,
    CallWall,
    PutWall,
    Atm,
    MaxPain,
    Peak,
    Other
}

/// <summary>One horizontal line to draw: a price, a label, and its kind.</summary>
public sealed record OptionFlowLine(decimal Price, string Label, OptionFlowLineKind Kind);

/// <summary>
/// Pure mapping from a <see cref="GexContext"/> to drawable overlay lines and a
/// diagnostics panel. No ATAS dependency, so it is unit-testable. A null context
/// yields the empty view (nothing to draw) — the DLL never fabricates a level.
/// </summary>
public sealed record OptionFlowOverlayViewModel(
    bool HasData,
    IReadOnlyList<OptionFlowLine> Lines,
    IReadOnlyList<string> PanelLines)
{
    public static readonly OptionFlowOverlayViewModel Empty =
        new(false, Array.Empty<OptionFlowLine>(), Array.Empty<string>());

    private static OptionFlowLineKind KindOf(string levelType) => levelType switch
    {
        "TRUE_ZERO_GAMMA" => OptionFlowLineKind.Flip,
        "CALL_GEX_WALL" or "CALL_OI_WALL" or "CALL_VOLUME_WALL" => OptionFlowLineKind.CallWall,
        "PUT_GEX_WALL" or "PUT_OI_WALL" or "PUT_VOLUME_WALL" => OptionFlowLineKind.PutWall,
        "ATM_STRIKE" => OptionFlowLineKind.Atm,
        "MAX_PAIN" => OptionFlowLineKind.MaxPain,
        "POSITIVE_NET_GEX_PEAK" or "NEGATIVE_NET_GEX_PEAK" or "GAMMA_ACTIVITY_PEAK" => OptionFlowLineKind.Peak,
        _ => OptionFlowLineKind.Other
    };

    private static string Humanize(string levelType) =>
        levelType.Replace('_', ' ');

    public static OptionFlowOverlayViewModel Build(GexContext? ctx)
    {
        if (ctx is null)
            return Empty;

        var lines = new List<OptionFlowLine>(ctx.Levels.Count);
        foreach (var l in ctx.Levels)
            lines.Add(new OptionFlowLine(
                (decimal)l.Price,
                $"OPT {Humanize(l.LevelType)} | {l.Price:0.##}",
                KindOf(l.LevelType)));

        var panel = new List<string>
        {
            $"OPTIONFLOW {ctx.Product}  [{ctx.DataHealth ?? "?"}]",
        };
        if (ctx.Spot is { } spot) panel.Add($"Spot {spot:0.##}");
        if (ctx.Regime is { } regime) panel.Add($"Regime {Humanize(regime)}");
        if (ctx.SelectedFlip is { } flip) panel.Add($"Flip {flip:0.##}");

        var a = ctx.Analytics;
        if (a?.Atm is { } atm && atm.Strike is { } atmK)
        {
            var straddle = atm.Straddle is { } s ? $" straddle {s:0.##}" : "";
            panel.Add($"ATM {atmK:0.##}{straddle}");
        }
        if (a?.ExpectedMove is { } em && em.OneDayMove is { } oneDay)
            panel.Add($"EM 1d +/-{oneDay:0.#}");
        if (a?.IvSkew is { } skew && skew.Skew is { } sk)
            panel.Add($"Skew {sk:+0.0%;-0.0%;0.0%}");
        if (a?.DealerPositioning is { } dp && dp.Posture is { } posture)
            panel.Add($"Dealer {Humanize(posture)}");

        return new OptionFlowOverlayViewModel(lines.Count > 0 || panel.Count > 1, lines, panel);
    }
}
