using GC.AuctionFlow.Directional;
using GC.AuctionFlow.Imbalance;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Imbalance;

/// <summary>
/// Phase 2H Imbalance (KDK Ch 25).
///
/// Phase 2B already measures every ratio this needs. What this phase adds is the
/// classification GATE and the location context KDK Ch 25 requires. It emits no
/// verdict: both the ratio rule and the minimum-volume rule are calibrated, and KDK
/// is explicit that a huge ratio on tiny volume is meaningless.
/// </summary>
public sealed class Phase2HImbalanceTests
{
    private static DateTime Utc(int sec = 0) =>
        new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc).AddSeconds(sec);

    private static ImbalanceHost EnabledHost() => new(new ImbalancePolicyConfig(enabled: true));

    private static ProfileLocationContextSnapshot Location(PriceValueLocation volume) =>
        new(PriceValueLocation.Unavailable, volume,
            PriceValueLocation.Unavailable, PriceValueLocation.Unavailable,
            PriceValueLocation.Unavailable);

    // ========== A: Policy + enum gates ==========

    [Fact]
    public void A01_PolicyVersion_is_imbalance_v1() =>
        Assert.Equal("IMBALANCE_POLICY_V1", ImbalancePolicyConfig.PolicyVersion);

    [Fact]
    public void A02_Module_default_is_disabled() =>
        Assert.False(new ImbalancePolicyConfig().Enabled);

    [Fact]
    public void A03_Qualification_verdicts_are_reserved()
    {
        Assert.Equal(2, (int)ImbalanceQualification.NotCalibrated);
        Assert.True((int)ImbalanceQualification.AskImbalance >= 100);
        Assert.True((int)ImbalanceQualification.BidImbalance >= 100);
        Assert.True((int)ImbalanceQualification.NotImbalanced >= 100);
    }

    [Fact]
    public void A04_Stacked_verdicts_are_reserved()
    {
        Assert.Equal(2, (int)StackedImbalanceState.NotCalibrated);
        Assert.True((int)StackedImbalanceState.AskStacked >= 100);
        Assert.True((int)StackedImbalanceState.BidStacked >= 100);
        Assert.True((int)StackedImbalanceState.NotStacked >= 100);
    }

    /// <summary>KDK Ch 25 names both comparison modes; neither is universally correct.</summary>
    [Fact]
    public void A05_Both_comparison_modes_exist()
    {
        var modes = Enum.GetValues<ImbalanceComparisonMode>();
        Assert.Equal(2, modes.Length);
        Assert.Contains(ImbalanceComparisonMode.SamePrice, modes);
        Assert.Contains(ImbalanceComparisonMode.Diagonal, modes);
    }

    [Fact]
    public void A06_Location_default_is_unavailable() =>
        Assert.Equal(ImbalanceLocationContext.Unavailable, default(ImbalanceLocationContext));

    [Fact]
    public void A07_Limitation_constants_are_stable()
    {
        Assert.Equal("IMBALANCE_RATIO_RULE_NOT_CALIBRATED", ImbalancePolicyConfig.LimitationRatioRuleNotCalibrated);
        Assert.Equal("IMBALANCE_MINIMUM_VOLUME_NOT_CALIBRATED", ImbalancePolicyConfig.LimitationMinimumVolumeNotCalibrated);
        Assert.Equal("IMBALANCE_IS_NOT_ACCEPTANCE_EVIDENCE", ImbalancePolicyConfig.LimitationNotAcceptanceEvidence);
        Assert.Equal("IMBALANCE_IS_NOT_PERMANENT_SUPPORT_RESISTANCE", ImbalancePolicyConfig.LimitationNotPermanentSupportResistance);
    }

    // ========== B: The gate ==========

    [Fact]
    public void B01_Awaiting_cluster_declares_the_rule_uncalibrated()
    {
        var set = EnabledHost().Rebuild(null, null, Utc());
        Assert.Equal(ImbalanceModuleState.AwaitingCluster, set.ModuleState);
        Assert.Contains(ImbalancePolicyConfig.LimitationRatioRuleNotCalibrated, set.Limitations);
        Assert.Contains(ImbalancePolicyConfig.LimitationMinimumVolumeNotCalibrated, set.Limitations);
    }

    /// <summary>
    /// KDK Ch 25: a ratio rule alone is not enough — a huge ratio on tiny volume can be
    /// meaningless. Both rules must be declared uncalibrated, not just the ratio.
    /// </summary>
    [Fact]
    public void B02_Minimum_volume_rule_is_gated_alongside_the_ratio_rule()
    {
        var set = EnabledHost().Rebuild(null, null, Utc());
        Assert.Contains(ImbalancePolicyConfig.LimitationRatioRuleNotCalibrated, set.Limitations);
        Assert.Contains(ImbalancePolicyConfig.LimitationMinimumVolumeNotCalibrated, set.Limitations);
    }

    [Fact]
    public void B03_Qualified_and_stacked_counts_are_always_zero()
    {
        var set = EnabledHost().Rebuild(null, null, Utc());
        Assert.Equal(0, set.QualifiedCount);
        Assert.Equal(0, set.StackedCount);
    }

    /// <summary>AP-018: imbalance must never substitute for acceptance.</summary>
    [Fact]
    public void B04_Imbalance_is_declared_not_acceptance_evidence()
    {
        var set = EnabledHost().Rebuild(null, null, Utc());
        Assert.Contains(ImbalancePolicyConfig.LimitationNotAcceptanceEvidence, set.Limitations);
    }

    /// <summary>
    /// AP-018 structurally: the snapshot must carry no acceptance surface at all, so a
    /// downstream consumer cannot read acceptance out of an imbalance.
    /// </summary>
    [Fact]
    public void B05_No_acceptance_surface_exists_on_the_snapshot()
    {
        var banned = new[] { "Acceptance", "Accepted", "Reentry", "Resolution", "Thesis" };
        foreach (var t in new[] { typeof(ImbalanceLevelSnapshot), typeof(ImbalanceSetSnapshot) })
        foreach (var p in t.GetProperties())
        foreach (var b in banned)
            Assert.False(p.Name.Contains(b, StringComparison.OrdinalIgnoreCase),
                t.Name + "." + p.Name + " leaks an acceptance surface (" + b + ")");
    }

    /// <summary>
    /// KDK Ch 25 mistake: treating a stack as permanent support or resistance.
    /// </summary>
    [Fact]
    public void B06_Permanence_is_explicitly_disclaimed() =>
        Assert.Contains(ImbalancePolicyConfig.LimitationNotPermanentSupportResistance,
            EnabledHost().Rebuild(null, null, Utc()).Limitations);

    // ========== C: Location context — the new measurement ==========

    [Theory]
    [InlineData(PriceValueLocation.InsideValue, ImbalanceLocationContext.MidValue)]
    [InlineData(PriceValueLocation.AtPoc, ImbalanceLocationContext.MidValue)]
    [InlineData(PriceValueLocation.AtValueHigh, ImbalanceLocationContext.ValueBoundary)]
    [InlineData(PriceValueLocation.AtValueLow, ImbalanceLocationContext.ValueBoundary)]
    [InlineData(PriceValueLocation.AboveValue, ImbalanceLocationContext.OutsideValue)]
    [InlineData(PriceValueLocation.BelowValue, ImbalanceLocationContext.OutsideValue)]
    [InlineData(PriceValueLocation.Unavailable, ImbalanceLocationContext.Unavailable)]
    public void C01_Location_maps_to_context(PriceValueLocation loc, ImbalanceLocationContext expected) =>
        Assert.Equal(expected, EnabledHost().Rebuild(null, Location(loc), Utc()).LocationContext);

    [Fact]
    public void C02_Null_location_is_unavailable_and_flagged()
    {
        var set = EnabledHost().Rebuild(null, null, Utc());
        Assert.Equal(ImbalanceLocationContext.Unavailable, set.LocationContext);
        Assert.Contains(ImbalancePolicyConfig.LimitationLocationUnavailable, set.Limitations);
    }

    /// <summary>
    /// KDK Ch 25 "Vị trí": mid-value imbalance may just be rotation. The context must
    /// therefore be distinguishable from boundary and outside.
    /// </summary>
    [Fact]
    public void C03_Mid_value_is_distinguishable_from_boundary_and_outside()
    {
        var mid = EnabledHost().Rebuild(null, Location(PriceValueLocation.InsideValue), Utc());
        var edge = EnabledHost().Rebuild(null, Location(PriceValueLocation.AtValueHigh), Utc());
        var outside = EnabledHost().Rebuild(null, Location(PriceValueLocation.AboveValue), Utc());

        Assert.Equal(3, new[] { mid.LocationContext, edge.LocationContext, outside.LocationContext }
            .Distinct().Count());
    }

    // ========== D: Ratio is never carried without its volume ==========

    /// <summary>
    /// KDK Ch 25: a very large ratio on very small volume can be meaningless, so the
    /// volume the ratio came from must always travel with it.
    /// </summary>
    [Fact]
    public void D01_Level_always_carries_the_volume_behind_the_ratio()
    {
        var props = typeof(ImbalanceLevelSnapshot).GetProperties().Select(p => p.Name).ToArray();
        Assert.Contains("ClassifiedVolume", props);
        Assert.Contains("UnknownAggressorVolume", props);
        Assert.Contains("AggressorCoverageRatio", props);
    }

    /// <summary>KDK Ch 25 mistake: ignoring unknown-aggressor volume and data quality.</summary>
    [Fact]
    public void D02_Unknown_aggressor_volume_is_disclosed_not_dropped() =>
        Assert.Contains(typeof(ImbalanceLevelSnapshot).GetProperties(),
            p => p.Name == "UnknownAggressorVolume");

    [Fact]
    public void D03_Both_comparison_directions_are_carried()
    {
        var props = typeof(ImbalanceLevelSnapshot).GetProperties().Select(p => p.Name).ToArray();
        Assert.Contains("SamePriceAskToBidRatio", props);
        Assert.Contains("SamePriceBidToAskRatio", props);
        Assert.Contains("DiagonalAskToBidBelowRatio", props);
        Assert.Contains("DiagonalBidToAskAboveRatio", props);
    }

    [Fact]
    public void D04_No_threshold_constant_exists_on_the_policy()
    {
        foreach (var f in typeof(ImbalancePolicyConfig).GetFields(
                     System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
        {
            Assert.NotEqual(typeof(decimal), f.FieldType);
            Assert.NotEqual(typeof(double), f.FieldType);
            Assert.DoesNotContain("Threshold", f.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("MinimumRatio", f.Name, StringComparison.OrdinalIgnoreCase);
        }
    }

    // ========== E: Host lifecycle ==========

    [Fact]
    public void E01_Disabled_host_publishes_disabled()
    {
        var host = new ImbalanceHost(new ImbalancePolicyConfig(enabled: false));
        var set = host.Rebuild(null, null, Utc());
        Assert.Equal(ImbalanceModuleState.Disabled, set.ModuleState);
        Assert.Contains("MODULE_DISABLED", set.Limitations);
    }

    [Fact]
    public void E02_Configure_null_throws() =>
        Assert.Throws<ArgumentNullException>(() => EnabledHost().Configure(null!));

    [Fact]
    public void E03_Reset_clears_current()
    {
        var host = EnabledHost();
        host.Rebuild(null, null, Utc());
        host.Reset();
        Assert.Null(host.Current);
    }

    [Fact]
    public void E04_Empty_set_has_zero_run_length() =>
        Assert.Equal(0, EnabledHost().Rebuild(null, null, Utc()).MaximumConsecutiveDominanceTicks);

    // ========== F: GPS card ==========

    [Fact]
    public void F01_ImbalanceLines_null_is_empty() =>
        Assert.Empty(AuctionGpsCardMapper.BuildImbalanceLines(null, false));

    [Fact]
    public void F02_ImbalanceLines_disabled_is_single_row()
    {
        var host = new ImbalanceHost(new ImbalancePolicyConfig(enabled: false));
        var rows = AuctionGpsCardMapper.BuildImbalanceLines(host.Rebuild(null, null, Utc()), false);
        Assert.Single(rows);
        Assert.Equal("IMBALANCE: DISABLED", rows[0]);
    }

    [Fact]
    public void F03_ImbalanceLines_declare_the_rule_uncalibrated()
    {
        var rows = AuctionGpsCardMapper.BuildImbalanceLines(
            EnabledHost().Rebuild(null, null, Utc()), false);
        Assert.Contains("IMBALANCE RULE: NOT CALIBRATED", rows);
    }

    [Fact]
    public void F04_ImbalanceLines_never_emit_a_verdict()
    {
        var text = string.Join(" | ", AuctionGpsCardMapper.BuildImbalanceLines(
            EnabledHost().Rebuild(null, Location(PriceValueLocation.AboveValue), Utc()), true));
        Assert.DoesNotContain("ASKIMBALANCE", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BIDIMBALANCE", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ASKSTACKED", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BIDSTACKED", text, StringComparison.OrdinalIgnoreCase);
    }
}
