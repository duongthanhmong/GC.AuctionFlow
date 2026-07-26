using GC.AuctionFlow.Entry;
using GC.AuctionFlow.Execution;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Execution;

/// <summary>
/// Phase 4C CFD Mapping (v1.2 §34) and Phase 4A Risk (v1.2 §35-36).
///
/// v1.2 §2.7 splits the markets: GC is analysed, a CFD is executed. The bridge is basis,
/// and basis needs a CFD price that never reaches the indicator. So the map is INVALID
/// and the sizing chain of §35 breaks before it can produce a number.
/// </summary>
public sealed class Phase4ACExecutionTests
{
    private static DateTime Utc(int sec = 0) =>
        new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc).AddSeconds(sec);

    private static CfdMappingHost CfdHost() => new(new CfdMappingPolicyConfig(enabled: true));
    private static RiskHost RiskHost() => new(new RiskPolicyConfig(enabled: true));

    private static EntryPolicySnapshot Entry() =>
        new EntryPolicyHost(new EntryPolicyConfig(enabled: true)).Rebuild(null, nowUtc: Utc());

    // ========== A: CFD mapping is INVALID without a price feed ==========

    [Fact]
    public void A01_PolicyVersion_is_cfd_mapping_v1() =>
        Assert.Equal("CFD_MAPPING_POLICY_V1", CfdMappingPolicyConfig.PolicyVersion);

    [Fact]
    public void A02_No_cfd_price_yields_invalid_map()
    {
        var s = CfdHost().Rebuild(gcPrice: 2400m, cfdPrice: null, nowUtc: Utc());
        Assert.Equal(CfdMappingState.Invalid, s.State);
        Assert.Equal(BasisAvailability.NoCfdPriceFeed, s.BasisAvailability);
    }

    /// <summary>v1.2 §34.5 prescribes this exact message.</summary>
    [Fact]
    public void A03_Operator_message_separates_analysis_from_execution()
    {
        var s = CfdHost().Rebuild(gcPrice: 2400m, nowUtc: Utc());
        Assert.Equal("GC ANALYSIS VALID / CFD EXECUTION MAP INVALID", s.OperatorMessage);
    }

    [Fact]
    public void A04_Basis_is_null_not_zero()
    {
        // Zero basis would mean the two markets trade identically — a claim, not a gap.
        var s = CfdHost().Rebuild(gcPrice: 2400m, nowUtc: Utc());
        Assert.Null(s.CurrentBasis);
        Assert.Null(s.RollingMedianBasis);
        Assert.Null(s.BasisVolatility);
        Assert.Null(s.CfdSpread);
    }

    /// <summary>v1.2 §34.2: only Executable RR may drive execution feasibility.</summary>
    [Fact]
    public void A05_Executable_rr_is_unavailable()
    {
        var s = CfdHost().Rebuild(gcPrice: 2400m, nowUtc: Utc());
        Assert.Null(s.ExecutableRewardToRisk);
        Assert.False(s.ExecutionFeasibilityDeterminable);
        Assert.Contains(CfdMappingPolicyConfig.LimitationExecutableRrUnavailable, s.Limitations);
        Assert.Contains(CfdMappingPolicyConfig.LimitationTheoreticalRrNotForExecution, s.Limitations);
    }

    /// <summary>
    /// One observation gives a basis but not its median, volatility or staleness.
    /// One sample is not a distribution, so the map degrades rather than validates.
    /// </summary>
    [Fact]
    public void A06_Single_observation_degrades_it_does_not_validate()
    {
        var s = CfdHost().Rebuild(gcPrice: 2400m, cfdPrice: 2401.5m, nowUtc: Utc());
        Assert.Equal(CfdMappingState.Degraded, s.State);
        Assert.Equal(1.5m, s.CurrentBasis);
        Assert.Null(s.RollingMedianBasis);
        Assert.Null(s.ExecutableRewardToRisk);
        Assert.False(s.ExecutionFeasibilityDeterminable);
    }

    [Fact]
    public void A07_Gc_price_alone_is_not_a_basis() =>
        Assert.Null(CfdHost().Rebuild(gcPrice: 2400m, cfdPrice: null, nowUtc: Utc()).CurrentBasis);

    [Fact]
    public void A08_Cfd_price_alone_is_not_a_basis() =>
        Assert.Null(CfdHost().Rebuild(gcPrice: null, cfdPrice: 2401m, nowUtc: Utc()).CurrentBasis);

    [Fact]
    public void A09_No_cfd_price_is_ever_emitted()
    {
        var banned = new[] { "CfdEntry", "CfdStop", "CfdTarget", "EstimatedCfd" };
        foreach (var p in typeof(CfdMappingSnapshot).GetProperties())
        foreach (var b in banned)
            Assert.False(p.Name.Contains(b, StringComparison.OrdinalIgnoreCase),
                "CfdMappingSnapshot." + p.Name + " emits a CFD price without a basis");
    }

    [Fact]
    public void A10_Disabled_host_is_still_invalid_not_valid()
    {
        // Failing open would be the dangerous default here.
        var s = new CfdMappingHost(new CfdMappingPolicyConfig(enabled: false))
            .Rebuild(2400m, 2401m, Utc());
        Assert.Equal(CfdMappingState.Invalid, s.State);
    }

    [Fact]
    public void A11_Configure_null_throws() =>
        Assert.Throws<ArgumentNullException>(() => CfdHost().Configure(null!));

    // ========== B: The sizing chain breaks at invalidation ==========

    [Fact]
    public void B01_PolicyVersion_is_risk_v1() =>
        Assert.Equal("RISK_POLICY_V1", RiskPolicyConfig.PolicyVersion);

    /// <summary>
    /// v1.2 §35 order: Thesis -> Structural Invalidation -> Stop Distance -> ...
    /// It breaks at step two, and the module must say so rather than skip ahead.
    /// </summary>
    [Fact]
    public void B02_Chain_breaks_at_structural_invalidation()
    {
        var s = RiskHost().Rebuild(Entry(), CfdHost().Rebuild(2400m, nowUtc: Utc()), Utc());
        Assert.False(s.InvalidationDistanceAvailable);
        Assert.False(s.SizingChainIntact);
        Assert.Contains(RiskPolicyConfig.LimitationSizingChainBroken, s.Limitations);
    }

    [Fact]
    public void B03_All_eleven_operator_inputs_are_missing()
    {
        var s = RiskHost().Rebuild(Entry(), CfdHost().Rebuild(2400m, nowUtc: Utc()), Utc());
        Assert.Equal(11, RiskPolicyConfig.RequiredOperatorInputs);
        Assert.Equal(11, s.MissingOperatorInputs.Count);
    }

    [Fact]
    public void B04_Risk_state_and_affordability_are_not_calibrated()
    {
        var s = RiskHost().Rebuild(Entry(), CfdHost().Rebuild(2400m, nowUtc: Utc()), Utc());
        Assert.Equal(RiskState.NotCalibrated, s.RiskState);
        Assert.Equal(TradeAffordability.NotCalibrated, s.Affordability);
    }

    [Fact]
    public void B05_Drawdown_states_are_reserved()
    {
        Assert.True((int)RiskState.Normal >= 100);
        Assert.True((int)RiskState.Reduced >= 100);
        Assert.True((int)RiskState.Recovery >= 100);
        Assert.True((int)RiskState.Locked >= 100);
    }

    [Fact]
    public void B06_Affordability_verdicts_are_reserved()
    {
        Assert.True((int)TradeAffordability.Affordable >= 100);
        Assert.True((int)TradeAffordability.RequiresSizeReduction >= 100);
        Assert.True((int)TradeAffordability.NotAffordable >= 100);
    }

    /// <summary>
    /// v1.2 §35.2: reduce size or skip the trade. Never pull the stop into noise to fit
    /// the account. The prohibition must be visible in the output, not just in the spec.
    /// </summary>
    [Fact]
    public void B07_Never_tighten_stop_to_fit_account_is_disclaimed()
    {
        var s = RiskHost().Rebuild(Entry(), CfdHost().Rebuild(2400m, nowUtc: Utc()), Utc());
        Assert.Contains(RiskPolicyConfig.LimitationNeverTightenStopToFitAccount, s.Limitations);
        Assert.Contains(RiskPolicyConfig.LimitationSizingOrderIsMandatory, s.Limitations);
    }

    /// <summary>No size, ever. Not zero, not a default — the field does not exist.</summary>
    [Fact]
    public void B08_No_size_or_lot_surface_exists()
    {
        var banned = new[] { "Size", "Lot", "Quantity", "RiskPerLot", "AllowedSize" };
        foreach (var p in typeof(RiskSnapshot).GetProperties())
        foreach (var b in banned)
            Assert.False(p.Name.Contains(b, StringComparison.OrdinalIgnoreCase),
                "RiskSnapshot." + p.Name + " leaks " + b);
        Assert.Contains(RiskPolicyConfig.LimitationNoSize,
            RiskHost().Rebuild(Entry(), null, Utc()).Limitations);
    }

    [Fact]
    public void B09_Entry_plan_and_cfd_state_are_carried_for_audit()
    {
        var s = RiskHost().Rebuild(Entry(), CfdHost().Rebuild(2400m, nowUtc: Utc()), Utc());
        Assert.Equal(EntryPlanKind.ObserveOnly, s.EntryPlan);
        Assert.Equal(CfdMappingState.Invalid, s.CfdMapping);
    }

    [Fact]
    public void B10_Missing_inputs_yield_awaiting() =>
        Assert.Equal(RiskModuleState.AwaitingInputs,
            RiskHost().Rebuild(null, null, Utc()).ModuleState);

    [Fact]
    public void B11_Configure_null_throws() =>
        Assert.Throws<ArgumentNullException>(() => RiskHost().Configure(null!));

    [Fact]
    public void B12_No_threshold_constant_exists()
    {
        foreach (var t in new[] { typeof(RiskPolicyConfig), typeof(CfdMappingPolicyConfig) })
        foreach (var f in t.GetFields(
                     System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
        {
            if (f.FieldType == typeof(string)) continue;
            Assert.NotEqual(typeof(decimal), f.FieldType);
            Assert.NotEqual(typeof(double), f.FieldType);
        }
    }

    // ========== C: GPS card makes the block visible ==========

    [Fact]
    public void C01_ExecutionLines_empty_when_nothing_runs() =>
        Assert.Empty(AuctionGpsCardMapper.BuildExecutionLines(null, null, null));

    [Fact]
    public void C02_ExecutionLines_show_the_operator_message()
    {
        var rows = AuctionGpsCardMapper.BuildExecutionLines(
            Entry(), CfdHost().Rebuild(2400m, nowUtc: Utc()), null);
        Assert.Contains("GC ANALYSIS VALID / CFD EXECUTION MAP INVALID", rows);
        Assert.Contains("BASIS: unavailable", rows);
    }

    [Fact]
    public void C03_ExecutionLines_name_where_sizing_is_blocked()
    {
        var rows = AuctionGpsCardMapper.BuildExecutionLines(
            Entry(), CfdHost().Rebuild(2400m, nowUtc: Utc()),
            RiskHost().Rebuild(Entry(), CfdHost().Rebuild(2400m, nowUtc: Utc()), Utc()));
        Assert.Contains("SIZING CHAIN: BROKEN", rows);
        Assert.Contains("SIZING BLOCKED AT: STRUCTURAL INVALIDATION", rows);
        Assert.Contains("POSITION SIZE: NOT AVAILABLE", rows);
    }

    [Fact]
    public void C04_ExecutionLines_never_show_a_size_or_price()
    {
        var text = string.Join(" | ", AuctionGpsCardMapper.BuildExecutionLines(
            Entry(), CfdHost().Rebuild(2400m, 2401m, Utc()),
            RiskHost().Rebuild(Entry(), null, Utc())));
        Assert.DoesNotContain("LOTS", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CFD ENTRY", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CFD STOP", text, StringComparison.OrdinalIgnoreCase);
    }
}
