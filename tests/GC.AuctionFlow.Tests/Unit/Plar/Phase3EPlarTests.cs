using GC.AuctionFlow.Core;
using GC.AuctionFlow.Directional;
using GC.AuctionFlow.Maturity;
using GC.AuctionFlow.Plar;
using GC.AuctionFlow.Reference;
using GC.AuctionFlow.Thesis;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Plar;

/// <summary>
/// Phase 3E Path of Least Auction Resistance (v1.2 §33, v1.3 §7.4 G-FAR-006).
///
/// The path is geometry: what lies ahead, how far, and what role it plays. Nothing
/// estimates whether a barrier holds — that needs reaction history and adjacent-build
/// research, neither of which is calibrated.
///
/// This phase also supplies RemainingTargetSpace, which closes G-LOC-002 left open by
/// Phase 3D.
/// </summary>
public sealed class Phase3EPlarTests
{
    private static DateTime Utc(int sec = 0) =>
        new(2026, 7, 27, 12, 0, sec, DateTimeKind.Utc);

    // ---------- helpers ----------

    private static StructuralReferenceSnapshot Ref(
        ReferenceType type,
        long zoneLowTick,
        long zoneHighTick,
        string id = "",
        ReferenceStatus status = ReferenceStatus.Active,
        ReferenceMaturity maturity = ReferenceMaturity.Confirmed) =>
        new(
            referenceId: string.IsNullOrEmpty(id) ? type + ":" + zoneLowTick : id,
            referenceType: type,
            zoneLow: zoneLowTick * 0.1m,
            zoneHigh: zoneHighTick * 0.1m,
            zoneLowTick: zoneLowTick,
            zoneHighTick: zoneHighTick,
            sourceId: "SRC-1",
            sourceKind: ReferenceSourceKind.PreviousPrimaryAuction,
            sourceHorizon: ReferenceSourceHorizon.PreviousPrimaryAuction,
            sourceCreatedAtUtc: Utc(),
            maturity: maturity,
            status: status,
            evidenceTier: ReferenceEvidenceTier.ProfileDerived,
            tickSize: 0.1m,
            dataEpoch: "GCQ6|tick=0.1",
            timestampPolicyVersion: "TS_V1",
            stateVersion: 1,
            createdAtUtc: Utc(),
            lastUpdatedAtUtc: Utc(1),
            features: new Dictionary<string, string>(),
            provenance: EvidenceProvenance.ObservedApi);

    private static PlarHost EnabledHost() => new(new PlarPolicyConfig(enabled: true));

    private static AuctionPathSnapshot Path(
        PathDirection dir, long price, params StructuralReferenceSnapshot[] refs) =>
        EnabledHost().Rebuild(refs, price, Utc()).PathFor(dir)!;

    // ========== A: Policy + enums ==========

    [Fact]
    public void A01_PolicyVersion_is_plar_v1() =>
        Assert.Equal("PLAR_POLICY_V1", PlarPolicyConfig.PolicyVersion);

    [Fact]
    public void A02_Module_default_is_disabled() =>
        Assert.False(new PlarPolicyConfig().Enabled);

    [Fact]
    public void A03_Corridor_depth_matches_spec() =>
        Assert.Equal(3, PlarPolicyConfig.CorridorBarrierCapacity);

    [Fact]
    public void A04_Permeability_verdicts_are_reserved()
    {
        Assert.Equal(1, (int)BarrierPermeability.NotCalibrated);
        Assert.True((int)BarrierPermeability.LowFriction >= 100);
        Assert.True((int)BarrierPermeability.ModerateFriction >= 100);
        Assert.True((int)BarrierPermeability.HighFriction >= 100);
    }

    [Fact]
    public void A05_Limitation_constants_are_stable()
    {
        Assert.Equal("BARRIER_PERMEABILITY_NOT_CALIBRATED", PlarPolicyConfig.LimitationPermeabilityNotCalibrated);
        Assert.Equal("ADJACENT_BUILD_RATIO_RESEARCH_ONLY", PlarPolicyConfig.LimitationAdjacentBuildResearchOnly);
        Assert.Equal("PLAR_NO_TAKE_PROFIT_LADDER", PlarPolicyConfig.LimitationNoTakeProfitLadder);
    }

    // ========== B: POC is a barrier, not just a target (G-FAR-006) ==========

    [Theory]
    [InlineData(ReferenceType.PreviousPrimaryTpoPoc)]
    [InlineData(ReferenceType.PreviousPrimaryVpoc)]
    [InlineData(ReferenceType.CurrentPrimaryTpoPoc)]
    [InlineData(ReferenceType.CurrentPrimaryVpoc)]
    [InlineData(ReferenceType.CompositeTpoPoc)]
    [InlineData(ReferenceType.CompositeVpoc)]
    public void B01_Every_poc_is_target_and_barrier(ReferenceType poc)
    {
        var o = Path(PathDirection.Up, 1000L, Ref(poc, 1050L, 1051L)).Obstacles[0];
        Assert.Equal(PathObstacleRole.TargetAndBarrier, o.Role);
        Assert.True(o.IsBarrier, "G-FAR-006: POC must be modelled as a barrier");
        Assert.True(o.IsTarget, "KDK Ch 64 lists POC as a valid FAR target");
    }

    [Theory]
    [InlineData(ReferenceType.PreviousPrimaryTpoVah)]
    [InlineData(ReferenceType.CompositeRangeHigh)]
    [InlineData(ReferenceType.PreviousPrimaryAuctionHigh)]
    public void B02_Value_edges_and_extremes_are_targets(ReferenceType t)
    {
        var o = Path(PathDirection.Up, 1000L, Ref(t, 1050L, 1051L)).Obstacles[0];
        Assert.Equal(PathObstacleRole.Target, o.Role);
        Assert.False(o.IsBarrier);
    }

    /// <summary>
    /// KDK Ch 19 Rule 4: a POC between price and the opposite edge can end the rotation.
    /// The corridor must therefore contain it.
    /// </summary>
    [Fact]
    public void B03_Poc_between_price_and_edge_enters_the_corridor()
    {
        var path = Path(PathDirection.Up, 1000L,
            Ref(ReferenceType.PreviousPrimaryVpoc, 1020L, 1021L),
            Ref(ReferenceType.PreviousPrimaryTpoVah, 1060L, 1061L));

        Assert.Single(path.Corridor);
        Assert.Equal(ReferenceType.PreviousPrimaryVpoc, path.Corridor[0].ReferenceType);
        Assert.Equal(ReferenceType.PreviousPrimaryVpoc, path.NearestBarrier!.ReferenceType);
    }

    // ========== C: Direction and geometry ==========

    [Fact]
    public void C01_Up_path_only_contains_levels_above()
    {
        var path = Path(PathDirection.Up, 1000L,
            Ref(ReferenceType.PreviousPrimaryTpoVah, 1050L, 1051L),
            Ref(ReferenceType.PreviousPrimaryTpoVal, 940L, 941L));

        Assert.Single(path.Obstacles);
        Assert.Equal(ReferenceType.PreviousPrimaryTpoVah, path.Obstacles[0].ReferenceType);
    }

    [Fact]
    public void C02_Down_path_only_contains_levels_below()
    {
        var path = Path(PathDirection.Down, 1000L,
            Ref(ReferenceType.PreviousPrimaryTpoVah, 1050L, 1051L),
            Ref(ReferenceType.PreviousPrimaryTpoVal, 940L, 941L));

        Assert.Single(path.Obstacles);
        Assert.Equal(ReferenceType.PreviousPrimaryTpoVal, path.Obstacles[0].ReferenceType);
    }

    [Fact]
    public void C03_Distance_is_measured_to_the_near_edge()
    {
        var up = Path(PathDirection.Up, 1000L, Ref(ReferenceType.PreviousPrimaryTpoVah, 1050L, 1060L));
        Assert.Equal(50L, up.Obstacles[0].DistanceTicks);

        var down = Path(PathDirection.Down, 1000L, Ref(ReferenceType.PreviousPrimaryTpoVal, 900L, 940L));
        Assert.Equal(60L, down.Obstacles[0].DistanceTicks);
    }

    [Fact]
    public void C04_Distance_is_never_negative()
    {
        var path = Path(PathDirection.Up, 1000L,
            Ref(ReferenceType.PreviousPrimaryTpoVah, 1010L, 1011L),
            Ref(ReferenceType.CompositeRangeHigh, 1200L, 1201L));
        Assert.All(path.Obstacles, o => Assert.True(o.DistanceTicks >= 0));
    }

    [Fact]
    public void C05_Obstacles_are_ordered_nearest_first()
    {
        var path = Path(PathDirection.Up, 1000L,
            Ref(ReferenceType.CompositeRangeHigh, 1200L, 1201L),
            Ref(ReferenceType.PreviousPrimaryTpoVah, 1050L, 1051L),
            Ref(ReferenceType.CurrentPrimaryVpoc, 1010L, 1011L));

        Assert.Equal(new[] { 10L, 50L, 200L }, path.Obstacles.Select(o => o.DistanceTicks).ToArray());
        Assert.Equal(new[] { 0, 1, 2 }, path.Obstacles.Select(o => o.CorridorIndex).ToArray());
    }

    /// <summary>Price inside a zone is interacting with it, not travelling toward it.</summary>
    [Fact]
    public void C06_Level_containing_price_is_not_ahead()
    {
        var path = Path(PathDirection.Up, 1000L, Ref(ReferenceType.CurrentPrimaryVpoc, 995L, 1005L));
        Assert.Empty(path.Obstacles);
    }

    [Fact]
    public void C07_Retired_references_are_excluded()
    {
        var path = Path(PathDirection.Up, 1000L,
            Ref(ReferenceType.PreviousPrimaryTpoVah, 1050L, 1051L, status: ReferenceStatus.Retired));
        Assert.Empty(path.Obstacles);
    }

    [Fact]
    public void C08_Final_target_is_the_furthest_target()
    {
        var path = Path(PathDirection.Up, 1000L,
            Ref(ReferenceType.PreviousPrimaryTpoVah, 1050L, 1051L),
            Ref(ReferenceType.CompositeRangeHigh, 1200L, 1201L));
        Assert.Equal(ReferenceType.CompositeRangeHigh, path.FinalTarget!.ReferenceType);
        Assert.Equal(ReferenceType.PreviousPrimaryTpoVah, path.NearestTarget!.ReferenceType);
    }

    // ========== D: Target space — the G-LOC-002 input ==========

    [Fact]
    public void D01_Target_ahead_reports_available_distance()
    {
        var path = Path(PathDirection.Up, 1000L, Ref(ReferenceType.PreviousPrimaryTpoVah, 1040L, 1041L));
        Assert.Equal(TargetSpaceAvailability.Available, path.TargetSpaceAvailability);
        Assert.Equal(40L, path.RemainingTargetSpaceTicks);
    }

    /// <summary>
    /// References exist but none ahead: that is a MEASURED "no room", distinct from
    /// being unable to measure. G-LOC-002 turns on exactly this distinction.
    /// </summary>
    [Fact]
    public void D02_References_but_none_ahead_is_measured_no_room()
    {
        var path = Path(PathDirection.Up, 1000L, Ref(ReferenceType.PreviousPrimaryTpoVal, 900L, 901L));
        Assert.Equal(TargetSpaceAvailability.NoTargetAhead, path.TargetSpaceAvailability);
        Assert.Equal(0L, path.RemainingTargetSpaceTicks);
    }

    [Fact]
    public void D03_No_references_is_unmeasurable_not_zero()
    {
        var set = EnabledHost().Rebuild(Array.Empty<StructuralReferenceSnapshot>(), 1000L, Utc());
        var path = set.UpPath!;
        Assert.Equal(TargetSpaceAvailability.Unavailable, path.TargetSpaceAvailability);
        Assert.Null(path.RemainingTargetSpaceTicks);
    }

    [Fact]
    public void D04_No_price_is_unmeasurable()
    {
        var set = EnabledHost().Rebuild(
            new[] { Ref(ReferenceType.PreviousPrimaryTpoVah, 1050L, 1051L) }, null, Utc());
        Assert.Equal(TargetSpaceAvailability.Unavailable, set.UpPath!.TargetSpaceAvailability);
        Assert.Equal(PlarModuleState.AwaitingReferences, set.ModuleState);
    }

    /// <summary>A barrier alone is not a destination.</summary>
    [Fact]
    public void D05_Barrier_only_path_still_has_target_space_via_poc()
    {
        // POC is TargetAndBarrier, so it does count as a destination.
        var path = Path(PathDirection.Up, 1000L, Ref(ReferenceType.CurrentPrimaryVpoc, 1030L, 1031L));
        Assert.Equal(TargetSpaceAvailability.Available, path.TargetSpaceAvailability);
        Assert.Equal(30L, path.RemainingTargetSpaceTicks);
    }

    // ========== E: NOT CALIBRATED invariants ==========

    [Fact]
    public void E01_Permeability_is_always_not_calibrated()
    {
        var path = Path(PathDirection.Up, 1000L,
            Ref(ReferenceType.CurrentPrimaryVpoc, 1010L, 1011L),
            Ref(ReferenceType.PreviousPrimaryTpoVah, 1050L, 1051L));
        Assert.All(path.Obstacles,
            o => Assert.Equal(BarrierPermeability.NotCalibrated, o.Permeability));
    }

    [Fact]
    public void E02_Path_declares_its_uncalibrated_dependencies()
    {
        var path = Path(PathDirection.Up, 1000L, Ref(ReferenceType.PreviousPrimaryTpoVah, 1050L, 1051L));
        Assert.Contains(PlarPolicyConfig.LimitationPermeabilityNotCalibrated, path.Limitations);
        Assert.Contains(PlarPolicyConfig.LimitationReactionHistoryUnavailable, path.Limitations);
        Assert.Contains(PlarPolicyConfig.LimitationAdjacentBuildResearchOnly, path.Limitations);
    }

    [Fact]
    public void E03_Set_bars_entry_and_take_profit()
    {
        var set = EnabledHost().Rebuild(
            new[] { Ref(ReferenceType.PreviousPrimaryTpoVah, 1050L, 1051L) }, 1000L, Utc());
        Assert.Contains(PlarPolicyConfig.LimitationNoEntryPlan, set.Limitations);
        Assert.Contains(PlarPolicyConfig.LimitationNoTakeProfitLadder, set.Limitations);
        Assert.Contains(PlarPolicyConfig.LimitationNoProgressVerdict, set.Limitations);
    }

    [Fact]
    public void E04_No_price_or_size_surface_on_obstacles()
    {
        var banned = new[] { "Entry", "Stop", "Size", "Quantity", "Score", "Probability" };
        foreach (var p in typeof(PathObstacleSnapshot).GetProperties())
        foreach (var b in banned)
            Assert.False(p.Name.Contains(b, StringComparison.OrdinalIgnoreCase),
                "PathObstacleSnapshot." + p.Name + " leaks " + b);
    }

    // ========== F: Host lifecycle ==========

    [Fact]
    public void F01_Disabled_host_publishes_disabled()
    {
        var host = new PlarHost(new PlarPolicyConfig(enabled: false));
        var set = host.Rebuild(new[] { Ref(ReferenceType.PreviousPrimaryTpoVah, 1050L, 1051L) }, 1000L, Utc());
        Assert.Equal(PlarModuleState.Disabled, set.ModuleState);
        Assert.Contains("MODULE_DISABLED", set.Limitations);
    }

    [Fact]
    public void F02_Configure_null_throws() =>
        Assert.Throws<ArgumentNullException>(() => EnabledHost().Configure(null!));

    [Fact]
    public void F03_Reset_clears_current()
    {
        var host = EnabledHost();
        host.Rebuild(new[] { Ref(ReferenceType.PreviousPrimaryTpoVah, 1050L, 1051L) }, 1000L, Utc());
        host.Reset();
        Assert.Null(host.Current);
    }

    [Fact]
    public void F04_Same_price_and_references_return_cached()
    {
        var host = EnabledHost();
        var refs = new[] { Ref(ReferenceType.PreviousPrimaryTpoVah, 1050L, 1051L) };
        Assert.Same(host.Rebuild(refs, 1000L, Utc()), host.Rebuild(refs, 1000L, Utc(5)));
    }

    [Fact]
    public void F05_Price_move_rebuilds_the_path()
    {
        var host = EnabledHost();
        var refs = new[] { Ref(ReferenceType.PreviousPrimaryTpoVah, 1050L, 1051L) };
        var a = host.Rebuild(refs, 1000L, Utc());
        var b = host.Rebuild(refs, 1020L, Utc(1));
        Assert.NotSame(a, b);
        Assert.Equal(50L, a.UpPath!.RemainingTargetSpaceTicks);
        Assert.Equal(30L, b.UpPath!.RemainingTargetSpaceTicks);
    }

    [Fact]
    public void F06_Unknown_direction_has_no_path() =>
        Assert.Null(EnabledHost()
            .Rebuild(new[] { Ref(ReferenceType.PreviousPrimaryTpoVah, 1050L, 1051L) }, 1000L, Utc())
            .PathFor(PathDirection.Unknown));

    // ========== G: G-LOC-002 closure in the maturity gate ==========

    private static FarThesisSnapshot Far(FarState state = FarState.ReentryDeveloping) =>
        new("FAR-1", FarThesisPolicyConfig.PolicyVersion, "EV", "EP", "PI", "REF",
            ThesisDirection.Long, state, false, 1,
            ThesisDataQuality.Complete, 1L, 1L, Utc(), Array.Empty<string>());

    private static FarThesisSetSnapshot FarSet() =>
        new(ThesisModuleState.Ready, FarThesisPolicyConfig.PolicyVersion,
            new[] { Far() }, Array.Empty<FarThesisSnapshot>(), null,
            0, 0, Utc(), Utc(1), Array.Empty<string>());

    private static ProfileLocationContextSnapshot GoodLocation() =>
        new(PriceValueLocation.AboveValue, PriceValueLocation.AboveValue,
            PriceValueLocation.Unavailable, PriceValueLocation.Unavailable,
            PriceValueLocation.Unavailable);

    private static SignalMaturitySnapshot Mature(AuctionPathSnapshot? path) =>
        new SignalMaturityHost(new SignalMaturityPolicyConfig(enabled: true))
            .Rebuild(FarSet(), null, GoodLocation(), path, Utc())
            .ActiveCandidates[0];

    [Fact]
    public void G01_No_room_ahead_vetoes_the_candidate()
    {
        // Good location, but nowhere to go.
        var path = Path(PathDirection.Up, 1000L, Ref(ReferenceType.PreviousPrimaryTpoVal, 900L, 901L));
        var sm = Mature(path);

        Assert.Equal(LocationGateOutcome.BlockedNoTargetSpace, sm.LocationGate);
        Assert.NotEqual(AnalysisLifecycleState.Candidate, sm.LifecycleState);
        Assert.Contains(MaturityBlockingReason.NoRemainingTargetSpace, sm.BlockingReasons);
    }

    [Fact]
    public void G02_Room_ahead_allows_the_candidate()
    {
        var path = Path(PathDirection.Up, 1000L, Ref(ReferenceType.PreviousPrimaryTpoVah, 1040L, 1041L));
        var sm = Mature(path);

        Assert.Equal(LocationGateOutcome.AllowedOutside, sm.LocationGate);
        Assert.Equal(AnalysisLifecycleState.Candidate, sm.LifecycleState);
    }

    /// <summary>
    /// The veto outranks position quality: a perfect location with nowhere to go is
    /// still not tradeable.
    /// </summary>
    [Fact]
    public void G03_Veto_outranks_location_quality()
    {
        var path = Path(PathDirection.Up, 1000L, Ref(ReferenceType.PreviousPrimaryTpoVal, 900L, 901L));
        var host = new SignalMaturityHost(new SignalMaturityPolicyConfig(enabled: true));
        var inside = new ProfileLocationContextSnapshot(
            PriceValueLocation.InsideValue, PriceValueLocation.InsideValue,
            PriceValueLocation.Unavailable, PriceValueLocation.Unavailable,
            PriceValueLocation.Unavailable);

        var sm = host.Rebuild(FarSet(), null, inside, path, Utc()).ActiveCandidates[0];
        Assert.Equal(LocationGateOutcome.BlockedNoTargetSpace, sm.LocationGate);
    }

    /// <summary>
    /// Without a PLAR path the veto is UNMEASURABLE and must be reported as such,
    /// never silently treated as passed.
    /// </summary>
    [Fact]
    public void G04_Absent_path_reports_unmeasurable_not_passed()
    {
        var sm = Mature(null);
        Assert.Contains(SignalMaturityPolicyConfig.LimitationTargetSpaceNotAvailable, sm.Limitations);
        Assert.Contains(MaturityBlockingReason.TargetSpaceUnavailable, sm.BlockingReasons);
        Assert.NotEqual(LocationGateOutcome.BlockedNoTargetSpace, sm.LocationGate);
    }

    [Fact]
    public void G05_Measured_room_drops_the_unmeasurable_claim()
    {
        var path = Path(PathDirection.Up, 1000L, Ref(ReferenceType.PreviousPrimaryTpoVah, 1040L, 1041L));
        var sm = Mature(path);
        Assert.DoesNotContain(SignalMaturityPolicyConfig.LimitationTargetSpaceNotAvailable, sm.Limitations);
        Assert.DoesNotContain(MaturityBlockingReason.TargetSpaceUnavailable, sm.BlockingReasons);
    }

    [Fact]
    public void G06_Gate_still_never_unlocks_a_maturity_level()
    {
        var path = Path(PathDirection.Up, 1000L, Ref(ReferenceType.PreviousPrimaryTpoVah, 1040L, 1041L));
        Assert.Equal(SignalMaturityLevel.NotCalibrated, Mature(path).MaturityLevel);
    }

    // ========== H: GPS publication (Phase 3E-b) ==========

    private static PlarSetSnapshot Set(long price, params StructuralReferenceSnapshot[] refs) =>
        EnabledHost().Rebuild(refs, price, Utc());

    [Fact]
    public void H01_PlarLines_null_is_empty() =>
        Assert.Empty(AuctionGpsCardMapper.BuildPlarLines(null, false));

    [Fact]
    public void H02_PlarLines_disabled_is_single_row()
    {
        var host = new PlarHost(new PlarPolicyConfig(enabled: false));
        var rows = AuctionGpsCardMapper.BuildPlarLines(host.Rebuild(null, null, Utc()), false);
        Assert.Single(rows);
        Assert.Equal("PATH: DISABLED", rows[0]);
    }

    [Fact]
    public void H03_PlarLines_declare_permeability_not_calibrated()
    {
        var rows = AuctionGpsCardMapper.BuildPlarLines(
            Set(1000L, Ref(ReferenceType.PreviousPrimaryTpoVah, 1040L, 1041L)), false);
        Assert.Contains("BARRIER PERMEABILITY: NOT CALIBRATED", rows);
    }

    [Fact]
    public void H04_PlarLines_show_distance_when_room_exists()
    {
        var rows = AuctionGpsCardMapper.BuildPlarLines(
            Set(1000L, Ref(ReferenceType.PreviousPrimaryTpoVah, 1040L, 1041L)), false);
        Assert.Contains("UP TARGET SPACE: 40 ticks", rows);
    }

    /// <summary>
    /// A measured veto and an unmeasurable space must never render alike — that is the
    /// whole G-LOC-002 distinction, and the operator has to see which one it is.
    /// </summary>
    [Fact]
    public void H05_Veto_and_unmeasurable_render_differently()
    {
        var vetoRows = AuctionGpsCardMapper.BuildPlarLines(
            Set(1000L, Ref(ReferenceType.PreviousPrimaryTpoVal, 900L, 901L)), false);
        Assert.Contains("UP TARGET SPACE: NONE AHEAD (VETO)", vetoRows);

        var unmeasurableRows = AuctionGpsCardMapper.BuildPlarLines(
            Set(1000L), false);
        Assert.Contains("TARGET SPACE: NOT MEASURABLE", unmeasurableRows);
        Assert.DoesNotContain(unmeasurableRows, r => r.Contains("VETO", StringComparison.Ordinal));
    }

    [Fact]
    public void H06_Diagnostics_list_the_corridor_with_roles()
    {
        var rows = AuctionGpsCardMapper.BuildPlarLines(
            Set(1000L,
                Ref(ReferenceType.PreviousPrimaryVpoc, 1020L, 1021L),
                Ref(ReferenceType.PreviousPrimaryTpoVah, 1060L, 1061L)),
            showDiagnostics: true);

        Assert.Contains(rows, r => r.StartsWith("UP CORRIDOR: 1/3", StringComparison.Ordinal));
        Assert.Contains(rows, r => r.StartsWith("UP BARRIER 1:", StringComparison.Ordinal)
                                   && r.Contains("TARGETANDBARRIER", StringComparison.Ordinal));
    }

    [Fact]
    public void H07_PlarLines_never_emit_a_permeability_verdict()
    {
        var text = string.Join(" | ", AuctionGpsCardMapper.BuildPlarLines(
            Set(1000L,
                Ref(ReferenceType.PreviousPrimaryVpoc, 1020L, 1021L),
                Ref(ReferenceType.PreviousPrimaryTpoVah, 1060L, 1061L)),
            showDiagnostics: true));
        Assert.DoesNotContain("LOWFRICTION", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HIGHFRICTION", text, StringComparison.OrdinalIgnoreCase);
    }
}
