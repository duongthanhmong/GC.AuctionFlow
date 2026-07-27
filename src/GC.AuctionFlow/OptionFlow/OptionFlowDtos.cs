using System.Text.Json;

namespace GC.AuctionFlow.OptionFlow;

/// <summary>
/// Round-trip DTOs for the <c>gcae-optionflow-v1</c> contract (see
/// research/optionflow/SCHEMA.md). Every field is nullable so a partial/older
/// file still deserializes; validation into <see cref="GexContext"/> happens in
/// <see cref="GexContext.TryCreate"/>. Property names bind via SnakeCaseLower.
/// </summary>
public sealed class OptionFlowDocDto
{
    public string? SchemaVersion { get; set; }
    public string? Product { get; set; }
    public string? UnderlyingSymbol { get; set; }
    public double? Spot { get; set; }
    public string? Regime { get; set; }
    public double? SelectedFlip { get; set; }
    public string? PrimaryExpiry { get; set; }
    public string? PublishedAt { get; set; }
    public long? PublishedAtEpoch { get; set; }
    public string? DataHealth { get; set; }
    public CoverageDto? Coverage { get; set; }
    public List<LevelDto>? Levels { get; set; }
    public AnalyticsDto? Analytics { get; set; }
}

public sealed class CoverageDto
{
    public int? StrikeCount { get; set; }
    public double? OiCoveragePct { get; set; }
    public double? GexContractCoveragePct { get; set; }
}

public sealed class LevelDto
{
    public string? LevelId { get; set; }
    public string? LevelType { get; set; }
    public string? Scope { get; set; }
    public double? Price { get; set; }
    public string? Status { get; set; }
    public bool? ProductionEligible { get; set; }
    public string? FormulaVersion { get; set; }
    public JsonElement? Detail { get; set; }
}

public sealed class AnalyticsDto
{
    public AtmDto? Atm { get; set; }
    public WallDto? VannaWall { get; set; }
    public WallDto? CharmWall { get; set; }
    public ExpectedMoveDto? ExpectedMove { get; set; }
    public IvSkewDto? IvSkew { get; set; }
    public Dictionary<string, TermBucketDto?>? TermStructure { get; set; }
    public GammaCurveDto? GammaProfileCurve { get; set; }
    public DealerPositioningDto? DealerPositioning { get; set; }
}

public sealed class AtmDto
{
    public double? Strike { get; set; }
    public double? CallPrice { get; set; }
    public double? PutPrice { get; set; }
    public double? Straddle { get; set; }
    public double? StraddleMoveToExpiry { get; set; }
    public double? AtmIv { get; set; }
    public double? Dte { get; set; }
    public double? DistanceFromSpot { get; set; }
}

public sealed class WallDto
{
    public double? Strike { get; set; }
    public double? Exposure { get; set; }
}

public sealed class ExpectedMoveDto
{
    public double? AtmIv { get; set; }
    public double? OneDayMove { get; set; }
    public double? ToExpiryMove { get; set; }
    public double? ExpiryDte { get; set; }
}

public sealed class IvSkewDto
{
    public double? PutIv { get; set; }
    public double? CallIv { get; set; }
    public double? Skew { get; set; }
    public double? PutStrike { get; set; }
    public double? CallStrike { get; set; }
}

public sealed class TermBucketDto
{
    public double? NetGex { get; set; }
    public int? Strikes { get; set; }
    public int? Contracts { get; set; }
}

public sealed class GammaCurveDto
{
    public List<CurvePointDto>? Curve { get; set; }
    public double? SlopeAtSpot { get; set; }
    public double? NetGammaAtSpot { get; set; }
}

public sealed class CurvePointDto
{
    public double? Price { get; set; }
    public double? NetGamma { get; set; }
}

public sealed class DealerPositioningDto
{
    public double? TotalNetGex { get; set; }
    public double? TotalNetDex { get; set; }
    public double? GrossGamma { get; set; }
    public string? Posture { get; set; }
}
