using GC.AuctionFlow.OptionFlow;

namespace GC.AuctionFlow.Tests.Unit.OptionFlow;

public sealed class OptionFlowOverlayViewModelTests
{
    private static GexContext SampleContext() => new(
        Product: "GC",
        UnderlyingSymbol: "GCQ6",
        Spot: 4079.75,
        Regime: "NEGATIVE_GAMMA",
        SelectedFlip: 4086.46,
        PrimaryExpiry: null,
        DataHealth: "LIVE",
        PublishedAtEpoch: 1_785_182_400,
        Levels: new List<GexLevel>
        {
            new("CALL_OI_WALL", "0DTE", 4200.0),
            new("PUT_OI_WALL", "0DTE", 4000.0),
            new("TRUE_ZERO_GAMMA", "0DTE", 4086.46),
            new("ATM_STRIKE", "0DTE", 4080.0),
        },
        Analytics: new AnalyticsDto
        {
            Atm = new AtmDto { Strike = 4080.0, Straddle = 35.9, AtmIv = 0.202 },
            ExpectedMove = new ExpectedMoveDto { OneDayMove = 43.6, AtmIv = 0.204 },
            IvSkew = new IvSkewDto { Skew = 0.055 },
            DealerPositioning = new DealerPositioningDto { Posture = "SHORT_GAMMA" },
        },
        Profile: Array.Empty<GexProfileNode>());

    [Fact]
    public void Null_context_yields_empty_view()
    {
        var vm = OptionFlowOverlayViewModel.Build(null);
        Assert.False(vm.HasData);
        Assert.Empty(vm.Lines);
        Assert.Empty(vm.PanelLines);
    }

    [Fact]
    public void Lines_map_types_to_kinds()
    {
        var vm = OptionFlowOverlayViewModel.Build(SampleContext());
        Assert.Equal(4, vm.Lines.Count);
        Assert.Contains(vm.Lines, l => l.Kind == OptionFlowLineKind.CallWall && l.Price == 4200m);
        Assert.Contains(vm.Lines, l => l.Kind == OptionFlowLineKind.PutWall && l.Price == 4000m);
        Assert.Contains(vm.Lines, l => l.Kind == OptionFlowLineKind.Flip && l.Price == 4086.46m);
        Assert.Contains(vm.Lines, l => l.Kind == OptionFlowLineKind.Atm && l.Price == 4080m);
    }

    [Fact]
    public void Panel_includes_regime_flip_atm_em_skew_posture()
    {
        var vm = OptionFlowOverlayViewModel.Build(SampleContext());
        var text = string.Join(" | ", vm.PanelLines);
        Assert.Contains("OPTIONFLOW GC", text);
        Assert.Contains("LIVE", text);
        Assert.Contains("Regime NEGATIVE GAMMA", text);
        Assert.Contains("Flip 4086", text);
        Assert.Contains("ATM 4080", text);
        Assert.Contains("EM 1d", text);
        Assert.Contains("Skew", text);
        Assert.Contains("Dealer SHORT GAMMA", text);
    }

    [Fact]
    public void Labels_are_compact_and_carry_price()
    {
        var vm = OptionFlowOverlayViewModel.Build(SampleContext());
        var call = vm.Lines.First(l => l.Kind == OptionFlowLineKind.CallWall);
        Assert.Contains("CallOI", call.Label);
        Assert.Contains("4200", call.Label);
    }

    [Fact]
    public void Gex_level_near_amt_level_is_marked_confluent()
    {
        var amt = new List<AmtLevelRef>
        {
            new(4201.0m, "REF CONFIRMED | CMP RANGE HIGH | 4201.0"),   // 1.0 from the 4200 wall
            new(3500.0m, "REF | FAR | 3500"),                          // far away
        };
        var vm = OptionFlowOverlayViewModel.Build(SampleContext(), amt, tolerance: 2.0m);

        var wall = vm.Lines.First(l => l.Price == 4200m);
        Assert.True(wall.Confluent);
        Assert.Equal("CMP RANGE HIGH", wall.ConfluenceWith);
        Assert.StartsWith("◆", wall.Label);

        var atm = vm.Lines.First(l => l.Price == 4080m);
        Assert.False(atm.Confluent);   // nearest AMT (4201) is far from 4080

        Assert.Contains(vm.PanelLines, p => p.StartsWith("Confluence x"));
    }

    [Fact]
    public void No_confluence_when_tolerance_zero_or_no_amt()
    {
        var vm = OptionFlowOverlayViewModel.Build(SampleContext(), null, tolerance: 0m);
        Assert.All(vm.Lines, l => Assert.False(l.Confluent));
        Assert.DoesNotContain(vm.PanelLines, p => p.StartsWith("Confluence"));
    }

    [Fact]
    public void Same_price_levels_merge_into_one_line_with_dominant_kind()
    {
        var ctx = new GexContext(
            "GC", "GCQ6", 4080.0, "NEGATIVE_GAMMA", null, null, "LIVE", 1_785_182_400,
            new List<GexLevel>
            {
                new("CALL_GEX_WALL", "0DTE", 4100.0),
                new("GAMMA_ACTIVITY_PEAK", "0DTE", 4100.0),
            },
            Analytics: null,
            Profile: Array.Empty<GexProfileNode>());

        var vm = OptionFlowOverlayViewModel.Build(ctx);
        Assert.Single(vm.Lines);                                   // merged, not stacked
        var line = vm.Lines[0];
        Assert.Equal(OptionFlowLineKind.CallWall, line.Kind);      // wall (80) beats peak (40)
        Assert.Contains("CallGEX", line.Label);
        Assert.Contains("GammaPeak", line.Label);
    }
}
