using GC.AuctionFlow.Entry;
using GC.AuctionFlow.Maturity;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Entry;

/// <summary>
/// Phase 4B Entry Policy (v1.2 §30).
///
/// v1.2 §30.6 lists ten inputs the order-type selector needs. None is available, so the
/// only emittable plan is ObserveOnly. That is the correct answer, not a gap to route
/// around: §30.7 forbids assuming any fixed order-type distribution, and §30.3 forbids
/// a blind limit merely because price touched a reference.
/// </summary>
public sealed class Phase4BEntryPolicyTests
{
    private static DateTime Utc(int sec = 0) =>
        new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc).AddSeconds(sec);

    private static EntryPolicyHost EnabledHost() => new(new EntryPolicyConfig(enabled: true));

    private static SignalMaturitySetSnapshot Maturity() =>
        new(MaturityModuleState.Ready,
            SignalMaturityPolicyConfig.PolicyVersion,
            Array.Empty<SignalMaturitySnapshot>(),
            Array.Empty<SignalMaturitySnapshot>(),
            null, 0, 0, 0, 0, true, Utc(), Utc(1), Array.Empty<string>());

    // ========== A: Policy + enum gates ==========

    [Fact]
    public void A01_PolicyVersion_is_entry_policy_v1() =>
        Assert.Equal("ENTRY_POLICY_V1", EntryPolicyConfig.PolicyVersion);

    [Fact]
    public void A02_Module_default_is_disabled() =>
        Assert.False(new EntryPolicyConfig().Enabled);

    /// <summary>v1.2 §30.1 lists seven shapes; only ObserveOnly is emittable.</summary>
    [Fact]
    public void A03_Every_active_plan_is_reserved()
    {
        Assert.Equal(0, (int)EntryPlanKind.ObserveOnly);
        foreach (var v in Enum.GetValues<EntryPlanKind>())
            if (v != EntryPlanKind.ObserveOnly)
                Assert.True((int)v >= 100, v + " must be reserved");
    }

    [Fact]
    public void A04_All_v12_plan_shapes_are_named()
    {
        var names = Enum.GetNames<EntryPlanKind>();
        foreach (var expected in new[]
                 {
                     "PassiveLimit", "MarketableLimit", "StopMarket",
                     "StopLimit", "Market", "HybridStaged", "ObserveOnly"
                 })
            Assert.Contains(expected, names);
    }

    /// <summary>All ten §30.6 inputs must be named individually.</summary>
    [Fact]
    public void A05_Ten_selector_inputs_are_named()
    {
        Assert.Equal(10, Enum.GetValues<EntrySelectorInput>().Length);
        Assert.Equal(10, EntryPolicyConfig.RequiredSelectorInputs);
    }

    // ========== B: The selector is blind ==========

    [Fact]
    public void B01_No_selector_input_is_usable()
    {
        var s = EnabledHost().Rebuild(Maturity(), nowUtc: Utc());
        Assert.Equal(0, s.UsableInputCount);
        Assert.False(s.SelectorReady);
    }

    [Fact]
    public void B02_All_ten_inputs_are_reported()
    {
        var s = EnabledHost().Rebuild(Maturity(), nowUtc: Utc());
        Assert.Equal(10, s.SelectorInputs.Count);
        foreach (var i in Enum.GetValues<EntrySelectorInput>())
            Assert.Contains(s.SelectorInputs, x => x.Input == i);
    }

    /// <summary>
    /// A single "not ready" flag would hide which dependency is blocking. There are
    /// five distinct reasons here, and each input must state its own.
    /// </summary>
    [Fact]
    public void B03_Each_input_states_its_own_reason()
    {
        var s = EnabledHost().Rebuild(Maturity(), nowUtc: Utc());
        Assert.All(s.SelectorInputs, i => Assert.False(string.IsNullOrWhiteSpace(i.Reason)));
        Assert.True(s.SelectorInputs.Select(i => i.Availability).Distinct().Count() >= 4,
            "distinct blocking reasons collapsed into one");
    }

    [Fact]
    public void B04_Depth_and_dom_are_blocked_by_mbo()
    {
        var s = EnabledHost().Rebuild(Maturity(), mboActive: false, nowUtc: Utc());
        foreach (var input in new[] { EntrySelectorInput.Depth, EntrySelectorInput.DomPersistence })
            Assert.Equal(SelectorInputAvailability.MboBlocked,
                s.SelectorInputs.Single(i => i.Input == input).Availability);
    }

    /// <summary>
    /// If MBO ever becomes active the reason must change honestly — depth aggregation
    /// still is not built, and saying "MBO blocked" then would be wrong.
    /// </summary>
    [Fact]
    public void B05_Mbo_active_changes_the_reason_not_the_availability()
    {
        var s = EnabledHost().Rebuild(Maturity(), mboActive: true, nowUtc: Utc());
        var depth = s.SelectorInputs.Single(i => i.Input == EntrySelectorInput.Depth);
        Assert.Equal(SelectorInputAvailability.NotBuilt, depth.Availability);
        Assert.False(depth.IsUsable);
    }

    [Fact]
    public void B06_Slippage_and_missed_trade_need_calibration()
    {
        var s = EnabledHost().Rebuild(Maturity(), nowUtc: Utc());
        foreach (var input in new[] { EntrySelectorInput.ExpectedSlippage, EntrySelectorInput.MissedTradeCost })
            Assert.Equal(SelectorInputAvailability.RequiresCalibration,
                s.SelectorInputs.Single(i => i.Input == input).Availability);
    }

    [Fact]
    public void B07_Maturity_is_present_but_not_calibrated()
    {
        var s = EnabledHost().Rebuild(Maturity(), nowUtc: Utc());
        Assert.Equal(SelectorInputAvailability.PresentButNotCalibrated,
            s.SelectorInputs.Single(i => i.Input == EntrySelectorInput.SignalMaturity).Availability);
    }

    [Fact]
    public void B08_Broker_constraints_are_an_operator_input()
    {
        var s = EnabledHost().Rebuild(Maturity(), nowUtc: Utc());
        Assert.Equal(SelectorInputAvailability.OperatorInputMissing,
            s.SelectorInputs.Single(i => i.Input == EntrySelectorInput.CfdBrokerConstraints).Availability);
    }

    [Fact]
    public void B09_Missing_inputs_lists_all_ten()
    {
        var s = EnabledHost().Rebuild(Maturity(), nowUtc: Utc());
        Assert.Equal(10, s.MissingInputs.Count);
    }

    // ========== C: Only ObserveOnly is ever selected ==========

    [Fact]
    public void C01_Selected_plan_is_always_observe_only()
    {
        foreach (var mbo in new[] { false, true })
        foreach (var mat in new[] { null, Maturity() })
            Assert.Equal(EntryPlanKind.ObserveOnly,
                EnabledHost().Rebuild(mat, mbo, Utc()).SelectedPlan);
    }

    [Fact]
    public void C02_No_active_plan_is_ever_emitted()
    {
        var s = EnabledHost().Rebuild(Maturity(), nowUtc: Utc());
        Assert.True((int)s.SelectedPlan < 100);
    }

    // ========== D: Prohibitions ==========

    /// <summary>v1.2 §2.8: the baseline places no orders.</summary>
    [Fact]
    public void D01_Order_placement_is_disclaimed()
    {
        var s = EnabledHost().Rebuild(Maturity(), nowUtc: Utc());
        Assert.Contains(EntryPolicyConfig.LimitationNoOrderPlacement, s.Limitations);
    }

    /// <summary>v1.2 §30.7: no fixed order-type distribution may be assumed.</summary>
    [Fact]
    public void D02_Fixed_order_type_ratio_is_disclaimed()
    {
        var s = EnabledHost().Rebuild(Maturity(), nowUtc: Utc());
        Assert.Contains(EntryPolicyConfig.LimitationNoFixedOrderTypeRatio, s.Limitations);
    }

    /// <summary>v1.2 §30.3: a reference touch alone never justifies a limit.</summary>
    [Fact]
    public void D03_Blind_limit_on_reference_touch_is_disclaimed()
    {
        var s = EnabledHost().Rebuild(Maturity(), nowUtc: Utc());
        Assert.Contains(EntryPolicyConfig.LimitationNoBlindLimitAtReference, s.Limitations);
    }

    /// <summary>
    /// No price, size or side may exist anywhere on the surface — this module describes
    /// HOW one would enter, never WHERE or HOW MUCH.
    /// </summary>
    [Fact]
    public void D04_No_price_size_or_side_surface_exists()
    {
        var banned = new[] { "Price", "Size", "Quantity", "Side", "Stop", "Target", "Limit" };

        // "Limitations" is the standard disclosure list every module carries; it is the
        // opposite of an order surface, so it is exempt by name rather than by loosening
        // the rule for everything else.
        var allowed = new[] { "Limitations" };

        foreach (var t in new[] { typeof(EntryPolicySnapshot), typeof(SelectorInputStatus) })
        foreach (var p in t.GetProperties())
        {
            if (allowed.Contains(p.Name, StringComparer.Ordinal)) continue;
            foreach (var b in banned)
                Assert.False(p.Name.Contains(b, StringComparison.OrdinalIgnoreCase),
                    t.Name + "." + p.Name + " leaks " + b);
        }
    }

    [Fact]
    public void D05_No_ratio_or_threshold_constant_exists()
    {
        // A threshold is a VALUE, not a name. Limitation strings may legitimately mention
        // "Ratio" precisely because they forbid one — LimitationNoFixedOrderTypeRatio is
        // the constant enforcing §30.7, and banning it by name would invert the rule.
        foreach (var f in typeof(EntryPolicyConfig).GetFields(
                     System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
        {
            if (f.FieldType == typeof(string)) continue;
            Assert.NotEqual(typeof(decimal), f.FieldType);
            Assert.NotEqual(typeof(double), f.FieldType);
            Assert.NotEqual(typeof(float), f.FieldType);
        }

        // The one numeric constant must be the input count, not a tuning knob.
        Assert.Equal(10, EntryPolicyConfig.RequiredSelectorInputs);
    }

    // ========== E: Lifecycle ==========

    [Fact]
    public void E01_Disabled_host_publishes_disabled()
    {
        var host = new EntryPolicyHost(new EntryPolicyConfig(enabled: false));
        var s = host.Rebuild(Maturity(), nowUtc: Utc());
        Assert.Equal(EntryPolicyModuleState.Disabled, s.ModuleState);
        Assert.Equal(EntryPlanKind.ObserveOnly, s.SelectedPlan);
        Assert.Contains("MODULE_DISABLED", s.Limitations);
    }

    [Fact]
    public void E02_Configure_null_throws() =>
        Assert.Throws<ArgumentNullException>(() => EnabledHost().Configure(null!));

    [Fact]
    public void E03_Reset_clears_current()
    {
        var host = EnabledHost();
        host.Rebuild(Maturity(), nowUtc: Utc());
        host.Reset();
        Assert.Null(host.Current);
    }

    [Fact]
    public void E04_Null_maturity_yields_awaiting() =>
        Assert.Equal(EntryPolicyModuleState.AwaitingMaturity,
            EnabledHost().Rebuild(null, nowUtc: Utc()).ModuleState);
}
