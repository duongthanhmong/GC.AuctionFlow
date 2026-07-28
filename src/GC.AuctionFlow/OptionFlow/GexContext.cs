using GC.AuctionFlow.Data;

namespace GC.AuctionFlow.OptionFlow;

/// <summary>One drawable option-derived level (VALID only).</summary>
public sealed record GexLevel(string LevelType, string Scope, double Price);

/// <summary>One strike in the GEX histogram. Normalized ∈ [-1,1] vs max |net_gex|.</summary>
public sealed record GexProfileNode(double Strike, double NetGex, double Normalized);

/// <summary>
/// Validated, read-only option-flow context sourced from the sidecar. OPTIONAL by
/// contract (D-V13-002a / v1.3 §50): callers must treat a <c>null</c> GexContext as
/// "no GEX", and every downstream decision MUST be identical to the no-GEX path.
/// This type never drives Episode/Thesis/GPS/alerts.
/// </summary>
public sealed record GexContext(
    string Product,
    string? UnderlyingSymbol,
    double? Spot,
    string? Regime,
    double? SelectedFlip,
    string? PrimaryExpiry,
    string? DataHealth,
    long PublishedAtEpoch,
    IReadOnlyList<GexLevel> Levels,
    AnalyticsDto? Analytics,
    IReadOnlyList<GexProfileNode> Profile)
{
    public const string SchemaVersion = "gcae-optionflow-v1";

    /// <summary>
    /// Validate a parsed DTO into a GexContext. Returns Fail (and a null context)
    /// on any contract violation so the caller falls back to the no-GEX path.
    /// Only VALID, production-eligible, priced levels are carried through — a level
    /// that was never VALID is never surfaced.
    /// </summary>
    public static SchemaValidationResult TryCreate(OptionFlowDocDto? dto, out GexContext? context)
    {
        context = null;
        if (dto is null)
            return SchemaValidationResult.Fail("Document is null.");

        var errors = new List<string>();
        if (dto.SchemaVersion != SchemaVersion)
            errors.Add($"SchemaVersion '{dto.SchemaVersion}' != '{SchemaVersion}'.");
        if (string.IsNullOrWhiteSpace(dto.Product))
            errors.Add("Product is required.");
        if (dto.PublishedAtEpoch is null or <= 0)
            errors.Add("PublishedAtEpoch is required.");

        if (errors.Count > 0)
            return SchemaValidationResult.Fail(errors);

        var levels = new List<GexLevel>();
        foreach (var l in dto.Levels ?? new List<LevelDto>())
        {
            if (l.Price is null) continue;
            if (!string.Equals(l.Status, "VALID", StringComparison.Ordinal)) continue;
            if (l.ProductionEligible != true) continue;
            if (string.IsNullOrWhiteSpace(l.LevelType) || string.IsNullOrWhiteSpace(l.Scope)) continue;
            levels.Add(new GexLevel(l.LevelType!, l.Scope!, l.Price.Value));
        }

        var profile = new List<GexProfileNode>();
        foreach (var n in dto.GexProfile ?? new List<GexProfileNodeDto>())
        {
            if (n.Strike is null || n.NetGex is null) continue;
            profile.Add(new GexProfileNode(n.Strike.Value, n.NetGex.Value, n.Normalized ?? 0.0));
        }

        context = new GexContext(
            Product: dto.Product!,
            UnderlyingSymbol: dto.UnderlyingSymbol,
            Spot: dto.Spot,
            Regime: dto.Regime,
            SelectedFlip: dto.SelectedFlip,
            PrimaryExpiry: dto.PrimaryExpiry,
            DataHealth: dto.DataHealth,
            PublishedAtEpoch: dto.PublishedAtEpoch!.Value,
            Levels: levels,
            Analytics: dto.Analytics,
            Profile: profile);
        return SchemaValidationResult.Ok();
    }
}
