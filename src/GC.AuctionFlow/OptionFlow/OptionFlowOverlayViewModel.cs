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

/// <summary>
/// One horizontal line to draw. <see cref="Priority"/> orders label-collision
/// resolution (higher wins its label; the line is always drawn).
/// </summary>
public sealed record OptionFlowLine(decimal Price, string Label, OptionFlowLineKind Kind, int Priority);

/// <summary>
/// Pure mapping from a <see cref="GexContext"/> to drawable overlay lines and a
/// diagnostics panel. Levels that share a strike are merged into one line with a
/// combined compact label, so the chart is not cluttered with stacked labels. A
/// null context yields the empty view — the DLL never fabricates a level.
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

    // Higher priority keeps its label when lines collide, and picks the group colour.
    private static int PriorityOf(OptionFlowLineKind kind) => kind switch
    {
        OptionFlowLineKind.Flip => 100,
        OptionFlowLineKind.CallWall => 80,
        OptionFlowLineKind.PutWall => 80,
        OptionFlowLineKind.MaxPain => 70,
        OptionFlowLineKind.Atm => 60,
        OptionFlowLineKind.Peak => 40,
        _ => 10
    };

    // Compact per-type tag for the merged label.
    private static string Tag(string levelType) => levelType switch
    {
        "TRUE_ZERO_GAMMA" => "Flip",
        "CALL_GEX_WALL" => "CallGEX",
        "CALL_OI_WALL" => "CallOI",
        "CALL_VOLUME_WALL" => "CallVol",
        "PUT_GEX_WALL" => "PutGEX",
        "PUT_OI_WALL" => "PutOI",
        "PUT_VOLUME_WALL" => "PutVol",
        "ATM_STRIKE" => "ATM",
        "MAX_PAIN" => "MaxPain",
        "POSITIVE_NET_GEX_PEAK" => "GEX+Peak",
        "NEGATIVE_NET_GEX_PEAK" => "GEX-Peak",
        "GAMMA_ACTIVITY_PEAK" => "GammaPeak",
        _ => levelType
    };

    public static OptionFlowOverlayViewModel Build(GexContext? ctx)
    {
        if (ctx is null)
            return Empty;

        // Merge levels that land on the same strike into one line.
        var groups = new Dictionary<decimal, List<GexLevel>>();
        foreach (var l in ctx.Levels)
        {
            var key = (decimal)l.Price;
            if (!groups.TryGetValue(key, out var list))
                groups[key] = list = new List<GexLevel>();
            list.Add(l);
        }

        var lines = new List<OptionFlowLine>(groups.Count);
        foreach (var (price, members) in groups)
        {
            var kind = members.Select(m => KindOf(m.LevelType))
                              .OrderByDescending(PriorityOf).First();
            var tags = string.Join("+", members.Select(m => Tag(m.LevelType)).Distinct());
            lines.Add(new OptionFlowLine(price, $"{tags} {price:0.##}", kind, PriorityOf(kind)));
        }
        lines.Sort((a, b) => a.Price.CompareTo(b.Price));

        var panel = new List<string> { $"OPTIONFLOW {ctx.Product}  [{ctx.DataHealth ?? "?"}]" };
        if (ctx.Spot is { } spot) panel.Add($"Spot {spot:0.##}");
        if (ctx.Regime is { } regime) panel.Add($"Regime {regime.Replace('_', ' ')}");
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
            panel.Add($"Dealer {posture.Replace('_', ' ')}");

        return new OptionFlowOverlayViewModel(lines.Count > 0 || panel.Count > 1, lines, panel);
    }
}
