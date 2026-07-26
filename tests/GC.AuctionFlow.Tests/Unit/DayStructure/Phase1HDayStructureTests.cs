using GC.AuctionFlow.Core;
using GC.AuctionFlow.DayStructure;
using GC.AuctionFlow.Profile;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.DayStructure;

/// <summary>
/// Phase 1H Day Structure (v1.2 §18.4, KDK Ch 11). RESEARCH_ONLY.
///
/// KDK Ch 11: day type describes the auction after it has developed; it is never a
/// mould the market must fill. Every in-session label is a candidate, and predicting
/// the type early to justify a trade is named as a mistake.
/// </summary>
public sealed class Phase1HDayStructureTests
{
    private const decimal Tick = 0.1m;

    private static DateTime Utc(int sec = 0) =>
        new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc).AddSeconds(sec);

    private static DayStructureHost EnabledHost() =>
        new(new DayStructurePolicyConfig(enabled: true));

    private static CompletedTpoPeriodSnapshot Period(int index, long lowTick, long highTick) =>
        new(periodIndex: index,
            periodStartUtc: Utc(index * 10),
            periodEndUtc: Utc(index * 10 + 10),
            periodHigh: highTick * Tick,
            periodLow: lowTick * Tick,
            periodHighTick: highTick,
            periodLowTick: lowTick);

    private static PrimaryAuctionProfileSnapshot Auction(
        decimal? high, decimal? low, decimal? last = null, bool completed = false) =>
        new(profileState: AuctionProfileState.Ready,
            auctionId: "PI-1",
            auctionStartUtc: Utc(),
            auctionEndUtc: Utc(600),
            isCompleted: completed,
            tpoProfile: null,
            volumeProfile: null,
            profileHigh: high,
            profileLow: low,
            lastObservedPrice: last,
            lastUpdatedUtc: Utc(1),
            sourceBarRange: null,
            dataQuality: ProfileDataQuality.Complete,
            knownLimitations: Array.Empty<string>());

    /// <summary>IB from ticks 990..1010, session 980..1040.</summary>
    private static DayStructureSnapshot Standard(bool completed = false, decimal? last = null) =>
        EnabledHost().Rebuild(
            Auction(1040 * Tick, 980 * Tick, last, completed),
            new[] { Period(0, 995L, 1005L), Period(1, 990L, 1010L) },
            Tick, Utc());

    // ========== A: Policy + enum gates ==========

    [Fact]
    public void A01_PolicyVersion_is_day_structure_v1() =>
        Assert.Equal("DAY_STRUCTURE_POLICY_V1", DayStructurePolicyConfig.PolicyVersion);

    [Fact]
    public void A02_Module_default_is_disabled() =>
        Assert.False(new DayStructurePolicyConfig().Enabled);

    /// <summary>v1.2 §18.4 names every shape; all of them are reserved.</summary>
    [Fact]
    public void A03_Every_shape_label_is_reserved()
    {
        foreach (var v in Enum.GetValues<DayStructureState>())
        {
            var n = v.ToString();
            if (n is "Unknown" or "NotCalibrated" or "SessionComplete") continue;
            Assert.True((int)v >= 100, n + " must be reserved");
        }
    }

    [Fact]
    public void A04_All_v12_shape_names_are_present()
    {
        var names = Enum.GetNames<DayStructureState>();
        foreach (var expected in new[]
                 {
                     "NormalCandidate", "NormalVariationCandidate", "NeutralCandidate",
                     "NonTrendCandidate", "TrendUpCandidate", "TrendDownCandidate",
                     "DoubleDistributionUpCandidate", "DoubleDistributionDownCandidate",
                     "Transition", "PostSessionConfirmed"
                 })
            Assert.Contains(expected, names);
    }

    [Fact]
    public void A05_Research_only_and_no_entry_are_declared()
    {
        var lim = Standard().Limitations;
        Assert.Contains(DayStructurePolicyConfig.LimitationResearchOnly, lim);
        Assert.Contains(DayStructurePolicyConfig.LimitationNoEntrySignal, lim);
        Assert.Contains(DayStructurePolicyConfig.LimitationNoHardVetoFeed, lim);
        Assert.Contains(DayStructurePolicyConfig.LimitationLabelIsCandidate, lim);
    }

    [Fact]
    public void A06_Initial_balance_period_count_is_declared_conventional() =>
        Assert.Equal(2, DayStructurePolicyConfig.InitialBalancePeriodCount);

    // ========== B: No shape is ever named ==========

    [Fact]
    public void B01_State_is_never_a_shape_label()
    {
        foreach (var completed in new[] { false, true })
            Assert.True((int)Standard(completed).State < 100);
    }

    [Fact]
    public void B02_Developing_session_is_not_calibrated() =>
        Assert.Equal(DayStructureState.NotCalibrated, Standard().State);

    /// <summary>
    /// KDK Ch 11: only confirm after the session is complete. Even then the shape rules
    /// are uncalibrated, so completion reports SessionComplete, not a type.
    /// </summary>
    [Fact]
    public void B03_Completed_session_reports_completion_not_a_type()
    {
        var s = Standard(completed: true);
        Assert.Equal(DayStructureState.SessionComplete, s.State);
        Assert.NotEqual(DayStructureState.PostSessionConfirmed, s.State);
    }

    [Fact]
    public void B04_Ib_width_percentile_is_never_fabricated()
    {
        var s = Standard();
        Assert.Null(s.IbWidthPercentile);
        Assert.Contains(DayStructurePolicyConfig.LimitationIbWidthPercentileNotCalibrated, s.Limitations);
    }

    // ========== C: Initial balance ==========

    [Fact]
    public void C01_Ib_is_the_envelope_of_the_first_two_periods()
    {
        var s = Standard();
        Assert.Equal(1010L, s.IbHighTick);
        Assert.Equal(990L, s.IbLowTick);
        Assert.Equal(20L, s.IbWidthTicks);
        Assert.True(s.IbAvailable);
    }

    /// <summary>A partial initial balance is not an initial balance.</summary>
    [Fact]
    public void C02_One_period_is_not_an_initial_balance()
    {
        var s = EnabledHost().Rebuild(
            Auction(104m, 98m), new[] { Period(0, 995L, 1005L) }, Tick, Utc());
        Assert.Null(s.IbWidthTicks);
        Assert.False(s.IbAvailable);
        Assert.Contains(DayStructurePolicyConfig.LimitationIbUnavailable, s.Limitations);
        Assert.Equal(DayStructureModuleState.Partial, s.ModuleState);
    }

    [Fact]
    public void C03_No_periods_yields_no_initial_balance() =>
        Assert.Null(EnabledHost().Rebuild(Auction(104m, 98m), null, Tick, Utc()).IbWidthTicks);

    [Fact]
    public void C04_Periods_are_ordered_by_index_not_arrival()
    {
        var s = EnabledHost().Rebuild(
            Auction(104m, 98m),
            new[] { Period(1, 990L, 1010L), Period(0, 995L, 1005L), Period(2, 900L, 1100L) },
            Tick, Utc());
        // Period 2 must not contaminate the initial balance.
        Assert.Equal(1010L, s.IbHighTick);
        Assert.Equal(990L, s.IbLowTick);
    }

    // ========== D: Range extension ==========

    [Fact]
    public void D01_Extension_both_sides_is_detected()
    {
        var s = Standard();
        Assert.Equal(RangeExtensionDirection.BothSides, s.RangeExtension);
        Assert.Equal(30L, s.RangeExtensionUpTicks);
        Assert.Equal(10L, s.RangeExtensionDownTicks);
    }

    [Fact]
    public void D02_No_extension_is_detected()
    {
        var s = EnabledHost().Rebuild(
            Auction(1010 * Tick, 990 * Tick),
            new[] { Period(0, 995L, 1005L), Period(1, 990L, 1010L) }, Tick, Utc());
        Assert.Equal(RangeExtensionDirection.None, s.RangeExtension);
        Assert.Equal(0L, s.RangeExtensionUpTicks);
    }

    [Fact]
    public void D03_Up_only_extension_is_detected()
    {
        var s = EnabledHost().Rebuild(
            Auction(1050 * Tick, 990 * Tick),
            new[] { Period(0, 995L, 1005L), Period(1, 990L, 1010L) }, Tick, Utc());
        Assert.Equal(RangeExtensionDirection.UpOnly, s.RangeExtension);
    }

    [Fact]
    public void D04_Extension_is_unavailable_without_an_initial_balance() =>
        Assert.Equal(RangeExtensionDirection.Unavailable,
            EnabledHost().Rebuild(Auction(104m, 98m), null, Tick, Utc()).RangeExtension);

    [Fact]
    public void D05_Extension_is_never_negative()
    {
        var s = Standard();
        Assert.True(s.RangeExtensionUpTicks >= 0L);
        Assert.True(s.RangeExtensionDownTicks >= 0L);
    }

    // ========== E: Close location ==========

    [Theory]
    [InlineData(1035, SessionCloseLocation.UpperThird)]
    [InlineData(1010, SessionCloseLocation.MiddleThird)]
    [InlineData(985, SessionCloseLocation.LowerThird)]
    public void E01_Close_location_bands(int closeTick, SessionCloseLocation expected) =>
        Assert.Equal(expected, Standard(last: closeTick * Tick).CloseLocation);

    [Fact]
    public void E02_Close_location_unavailable_without_a_close() =>
        Assert.Equal(SessionCloseLocation.Unavailable, Standard().CloseLocation);

    // ========== F: IB extreme look-ahead split (v1.2 §18.4.1) ==========

    /// <summary>
    /// The critical correctness item. End-of-day labels must not exist intra-session:
    /// producing them would leak the session outcome into a live decision.
    /// </summary>
    [Fact]
    public void F01_Eod_labels_are_withheld_intra_session()
    {
        var s = Standard(completed: false);
        Assert.False(s.IbExtreme.SessionComplete);
        Assert.Null(s.IbExtreme.IbHighWasDayHigh);
        Assert.Null(s.IbExtreme.IbLowWasDayLow);
        Assert.False(s.IbExtreme.EodLabelsAvailable);
        Assert.Contains(DayStructurePolicyConfig.LimitationEodLabelsWithheldIntraSession, s.Limitations);
    }

    [Fact]
    public void F02_Live_feature_is_available_intra_session()
    {
        var s = Standard(completed: false);
        // Session high 1040 exceeded IB high 1010, so the live answer is a real "no".
        Assert.False(s.IbExtreme.CurrentDayExtremeStillEqualsIbHigh);
        Assert.False(s.IbExtreme.CurrentDayExtremeStillEqualsIbLow);
    }

    [Fact]
    public void F03_Live_feature_is_true_while_ib_extreme_holds()
    {
        var s = EnabledHost().Rebuild(
            Auction(1010 * Tick, 990 * Tick),
            new[] { Period(0, 995L, 1005L), Period(1, 990L, 1010L) }, Tick, Utc());
        Assert.True(s.IbExtreme.CurrentDayExtremeStillEqualsIbHigh);
        Assert.True(s.IbExtreme.CurrentDayExtremeStillEqualsIbLow);
    }

    [Fact]
    public void F04_Eod_labels_appear_only_after_completion()
    {
        var s = Standard(completed: true);
        Assert.True(s.IbExtreme.EodLabelsAvailable);
        Assert.NotNull(s.IbExtreme.IbHighWasDayHigh);
        Assert.NotNull(s.IbExtreme.IbLowWasDayLow);
        Assert.DoesNotContain(DayStructurePolicyConfig.LimitationEodLabelsWithheldIntraSession, s.Limitations);
    }

    [Fact]
    public void F05_No_ib_means_no_extreme_observation()
    {
        var s = EnabledHost().Rebuild(Auction(104m, 98m), null, Tick, Utc());
        Assert.Null(s.IbExtreme.CurrentDayExtremeStillEqualsIbHigh);
        Assert.Null(s.IbExtreme.IbHighWasDayHigh);
    }

    // ========== G: Revision tracking + lifecycle ==========

    [Fact]
    public void G01_Revisions_are_counted_within_one_session()
    {
        var host = EnabledHost();
        for (var i = 0; i < 4; i++)
            host.Rebuild(Auction(104m, 98m),
                new[] { Period(0, 995L, 1005L), Period(1, 990L, 1010L) }, Tick, Utc(i));
        Assert.Equal(3, host.Current!.RevisionCount);
    }

    [Fact]
    public void G02_New_auction_restarts_the_revision_count()
    {
        var host = EnabledHost();
        host.Rebuild(Auction(104m, 98m), null, Tick, Utc());
        host.Rebuild(Auction(104m, 98m), null, Tick, Utc(1));
        Assert.Equal(1, host.Current!.RevisionCount);

        var next = new PrimaryAuctionProfileSnapshot(
            AuctionProfileState.Ready, "PI-2", Utc(600), Utc(1200), false,
            null, null, 104m, 98m, null, Utc(601), null,
            ProfileDataQuality.Complete, Array.Empty<string>());
        host.Rebuild(next, null, Tick, Utc(601));
        Assert.Equal(0, host.Current!.RevisionCount);
    }

    [Fact]
    public void G03_Disabled_host_publishes_disabled()
    {
        var host = new DayStructureHost(new DayStructurePolicyConfig(enabled: false));
        var s = host.Rebuild(Auction(104m, 98m), null, Tick, Utc());
        Assert.Equal(DayStructureModuleState.Disabled, s.ModuleState);
        Assert.Contains("MODULE_DISABLED", s.Limitations);
    }

    [Fact]
    public void G04_Configure_null_throws() =>
        Assert.Throws<ArgumentNullException>(() => EnabledHost().Configure(null!));

    [Fact]
    public void G05_Reset_clears_current()
    {
        var host = EnabledHost();
        host.Rebuild(Auction(104m, 98m), null, Tick, Utc());
        host.Reset();
        Assert.Null(host.Current);
    }

    [Fact]
    public void G06_Null_auction_yields_awaiting_profile() =>
        Assert.Equal(DayStructureModuleState.AwaitingProfile,
            EnabledHost().Rebuild(null, null, Tick, Utc()).ModuleState);

    [Fact]
    public void G07_Non_positive_tick_yields_awaiting_profile() =>
        Assert.Equal(DayStructureModuleState.AwaitingProfile,
            EnabledHost().Rebuild(Auction(104m, 98m), null, 0m, Utc()).ModuleState);

    // ========== H: Scope guards ==========

    [Fact]
    public void H01_No_entry_or_veto_surface_exists()
    {
        var banned = new[] { "Entry", "Stop", "Target", "Size", "Veto", "Signal", "Score" };
        foreach (var p in typeof(DayStructureSnapshot).GetProperties())
        foreach (var b in banned)
            Assert.False(p.Name.Contains(b, StringComparison.OrdinalIgnoreCase),
                "DayStructureSnapshot." + p.Name + " leaks " + b);
    }

    [Fact]
    public void H02_No_threshold_constant_exists()
    {
        foreach (var f in typeof(DayStructurePolicyConfig).GetFields(
                     System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
        {
            Assert.NotEqual(typeof(decimal), f.FieldType);
            Assert.NotEqual(typeof(double), f.FieldType);
            Assert.DoesNotContain("Threshold", f.Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
