using GC.AuctionFlow.OptionFlow;

namespace GC.AuctionFlow.Tests.Unit.OptionFlow;

public sealed class OptionFlowReaderTests : IDisposable
{
    private readonly string _root;
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(1_785_182_400);
    private static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(30);

    public OptionFlowReaderTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "gcae-of-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    private string WriteDoc(string product, string json)
    {
        var dir = Path.Combine(_root, product);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "levels.json");
        File.WriteAllText(path, json);
        return path;
    }

    private static string ValidJson(long epoch, string schema = "gcae-optionflow-v1") => """
    {
      "schema_version": "__SCHEMA__",
      "product": "GC",
      "underlying_symbol": "GCQ6",
      "spot": 4079.75,
      "regime": "NEGATIVE_GAMMA",
      "selected_flip": 4086.46,
      "primary_expiry": "2026-07-28T17:30:00+00:00",
      "published_at": "2026-07-28T04:00:00+00:00",
      "published_at_epoch": __EPOCH__,
      "data_health": "LIVE",
      "coverage": {"strike_count": 62, "oi_coverage_pct": 100.0, "gex_contract_coverage_pct": 100.0},
      "levels": [
        {"level_id":"0DTE_CALL_OI_WALL","level_type":"CALL_OI_WALL","scope":"0DTE","price":4200.0,"status":"VALID","production_eligible":true,"formula_version":"OI_WALLS_1.0.0","detail":{"call_oi":3188}},
        {"level_id":"0DTE_ATM_STRIKE","level_type":"ATM_STRIKE","scope":"0DTE","price":4080.0,"status":"VALID","production_eligible":true,"formula_version":"ATM_1.0.0","detail":{}},
        {"level_id":"0DTE_DEAD","level_type":"MAX_PAIN","scope":"0DTE","price":4100.0,"status":"STALE","production_eligible":true,"formula_version":"MAX_PAIN_1.0.0","detail":{}}
      ],
      "analytics": {
        "atm": {"strike": 4080.0, "call_price": 21.4, "put_price": 14.5, "straddle": 35.9, "straddle_move_to_expiry": 35.9, "atm_iv": 0.202, "dte": 0.8, "distance_from_spot": 0.25},
        "dealer_positioning": {"total_net_gex": -1447561.0, "total_net_dex": 0.0, "gross_gamma": 5.0, "posture": "SHORT_GAMMA"},
        "term_structure": {"0DTE": {"net_gex": -692448.0, "strikes": 62, "contracts": 79}, "1_7DTE": {"net_gex": -755113.0, "strikes": 62, "contracts": 80}}
      },
      "gex_profile": [
        {"strike": 4100.0, "net_gex": 203552629.0, "normalized": 1.0},
        {"strike": 4000.0, "net_gex": -150000000.0, "normalized": -0.737}
      ]
    }
    """.Replace("__SCHEMA__", schema).Replace("__EPOCH__", epoch.ToString());

    [Fact]
    public void Fresh_valid_file_yields_context_with_only_VALID_levels()
    {
        WriteDoc("GC", ValidJson(Now.ToUnixTimeSeconds() - 60));
        var outcome = OptionFlowReader.Read(_root, "GC", Now, MaxAge);

        Assert.True(outcome.HasContext);
        var ctx = outcome.Context!;
        Assert.Equal("GC", ctx.Product);
        Assert.Equal("NEGATIVE_GAMMA", ctx.Regime);
        Assert.Equal(4086.46, ctx.SelectedFlip);
        // 3 levels in file; the STALE one is dropped.
        Assert.Equal(2, ctx.Levels.Count);
        Assert.Contains(ctx.Levels, l => l.LevelType == "CALL_OI_WALL" && l.Price == 4200.0);
        Assert.DoesNotContain(ctx.Levels, l => l.LevelType == "MAX_PAIN");
        Assert.Equal(4080.0, ctx.Analytics!.Atm!.Strike);
        Assert.Equal("SHORT_GAMMA", ctx.Analytics!.DealerPositioning!.Posture);

        // GEX profile parsed for the histogram.
        Assert.Equal(2, ctx.Profile.Count);
        Assert.Contains(ctx.Profile, n => n.Strike == 4100.0 && n.Normalized == 1.0);
        Assert.Contains(ctx.Profile, n => n.Strike == 4000.0 && n.NetGex < 0);
    }

    [Fact]
    public void Missing_file_yields_null_context()
    {
        var outcome = OptionFlowReader.Read(_root, "GC", Now, MaxAge);
        Assert.False(outcome.HasContext);
        Assert.Contains("absent", outcome.Diagnostic);
    }

    [Fact]
    public void Schema_mismatch_yields_null_context()
    {
        WriteDoc("GC", ValidJson(Now.ToUnixTimeSeconds() - 60, schema: "gcae-optionflow-v2"));
        var outcome = OptionFlowReader.Read(_root, "GC", Now, MaxAge);
        Assert.False(outcome.HasContext);
        Assert.Contains("SchemaVersion", outcome.Diagnostic);
    }

    [Fact]
    public void Stale_file_yields_null_context()
    {
        WriteDoc("GC", ValidJson(Now.ToUnixTimeSeconds() - 3600));  // 1h old, max 30m
        var outcome = OptionFlowReader.Read(_root, "GC", Now, MaxAge);
        Assert.False(outcome.HasContext);
        Assert.Contains("stale", outcome.Diagnostic);
    }

    [Fact]
    public void Future_timestamp_yields_null_context()
    {
        WriteDoc("GC", ValidJson(Now.ToUnixTimeSeconds() + 3600));  // 1h in the future
        var outcome = OptionFlowReader.Read(_root, "GC", Now, MaxAge);
        Assert.False(outcome.HasContext);
        Assert.Contains("future", outcome.Diagnostic);
    }

    [Fact]
    public void Garbage_json_yields_null_context_not_throw()
    {
        WriteDoc("GC", "{ this is not valid json ");
        var outcome = OptionFlowReader.Read(_root, "GC", Now, MaxAge);
        Assert.False(outcome.HasContext);
    }

    [Fact]
    public void TryCreate_null_dto_fails()
    {
        var result = GexContext.TryCreate(null, out var ctx);
        Assert.False(result.IsValid);
        Assert.Null(ctx);
    }
}
