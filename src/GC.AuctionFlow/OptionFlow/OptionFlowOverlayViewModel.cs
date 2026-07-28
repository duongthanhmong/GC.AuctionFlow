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

/// <summary>An existing AMT / profile / reference level to test GEX confluence against.</summary>
public sealed record AmtLevelRef(decimal Price, string Label)
{
    /// <summary>Compact descriptor pulled from a "REF ... | NAME | price" style label.</summary>
    public string ShortName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Label))
                return "";
            var parts = Label.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return parts.Length >= 2 ? parts[^2] : Label.Trim();
        }
    }
}

/// <summary>
/// One horizontal line to draw. <see cref="Priority"/> orders label-collision
/// resolution (higher wins its label; the line is always drawn). When
/// <see cref="Confluent"/>, this GEX level sits within tolerance of an existing
/// AMT level (<see cref="ConfluenceWith"/>) — a display-only annotation.
/// </summary>
public sealed record OptionFlowLine(
    decimal Price, string Label, OptionFlowLineKind Kind, int Priority,
    bool Confluent = false, string? ConfluenceWith = null);

/// <summary>
/// Pure mapping from a <see cref="GexContext"/> to drawable overlay lines and a
/// diagnostics panel. Levels that share a strike are merged into one line with a
/// combined compact label, so the chart is not cluttered with stacked labels. A
/// null context yields the empty view — the DLL never fabricates a level.
/// </summary>
public sealed record OptionFlowOverlayViewModel(
    bool HasData,
    IReadOnlyList<OptionFlowLine> Lines,
    IReadOnlyList<string> PanelLines,
    IReadOnlyList<GexProfileNode> Profile)
{
    public static readonly OptionFlowOverlayViewModel Empty =
        new(false, Array.Empty<OptionFlowLine>(), Array.Empty<string>(), Array.Empty<GexProfileNode>());

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

    public static OptionFlowOverlayViewModel Build(GexContext? ctx) =>
        Build(ctx, null, 0m);

    /// <param name="amtLevels">Existing AMT/profile/reference levels for confluence.</param>
    /// <param name="tolerance">Max price distance (same units as strikes) to count as confluent.</param>
    public static OptionFlowOverlayViewModel Build(
        GexContext? ctx, IReadOnlyList<AmtLevelRef>? amtLevels, decimal tolerance)
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

        var confluences = new List<string>();
        var lines = new List<OptionFlowLine>(groups.Count);
        foreach (var (price, members) in groups)
        {
            var kind = members.Select(m => KindOf(m.LevelType))
                              .OrderByDescending(PriorityOf).First();
            var tags = string.Join("+", members.Select(m => Tag(m.LevelType)).Distinct());

            var (confluent, with) = NearestAmt(price, amtLevels, tolerance);
            var label = confluent ? $"◆ {tags} {price:0.##}" : $"{tags} {price:0.##}";
            lines.Add(new OptionFlowLine(price, label, kind, PriorityOf(kind), confluent, with));
            if (confluent && with is not null)
                confluences.Add($"{price:0.##} {tags} = {with}");
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

        if (confluences.Count > 0)
        {
            panel.Add($"Confluence x{confluences.Count}:");
            foreach (var c in confluences)
                panel.Add($"  {c}");
        }

        return new OptionFlowOverlayViewModel(
            lines.Count > 0 || panel.Count > 1, lines, panel, ctx.Profile);
    }

    /// <summary>Nearest AMT level within tolerance, or (false, null).</summary>
    private static (bool confluent, string? with) NearestAmt(
        decimal price, IReadOnlyList<AmtLevelRef>? amtLevels, decimal tolerance)
    {
        if (amtLevels is null || amtLevels.Count == 0 || tolerance <= 0m)
            return (false, null);

        AmtLevelRef? best = null;
        var bestDist = decimal.MaxValue;
        foreach (var a in amtLevels)
        {
            var d = Math.Abs(a.Price - price);
            if (d <= tolerance && d < bestDist)
            {
                best = a;
                bestDist = d;
            }
        }
        return best is null ? (false, null) : (true, best.ShortName);
    }
}
