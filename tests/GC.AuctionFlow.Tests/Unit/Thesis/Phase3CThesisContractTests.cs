using GC.AuctionFlow.Core;
using GC.AuctionFlow.Maturity;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Runtime;
using GC.AuctionFlow.Thesis;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Thesis;

/// <summary>
/// Phase 3C Thesis Contract + multi-dimensional invalidation.
/// v1.2 §32 (contract, four invalidation types) + §11.1-11.3 (five horizons,
/// source-of-move) + v1.3 §11 (seven mandatory fields, five-timeframe
/// commitment, Evidence as the fifth invalidation dimension, consistency law).
///
/// Contract state is always NotCalibrated. No entry, stop, target or size.
/// </summary>
public sealed class Phase3CThesisContractTests
{
    private static DateTime Utc(int sec = 0) =>
        new(2026, 7, 27, 12, 0, sec, DateTimeKind.Utc);

    // ---------- helpers ----------

    private static FarThesisSnapshot Far(
        FarState state, string id = "FAR-1",
        ThesisDataQuality q = ThesisDataQuality.Complete,
        long eventRevision = 1L) =>
        new FarThesisSnapshot(
            id, FarThesisPolicyConfig.PolicyVersion,
            evidenceId: "EV-1", episodeId: "EP-1",
            primaryAuctionId: "PI-1", referenceId: "REF-1",
            direction: ThesisDirection.Long, farState: state, notCalibrated: false,
            attemptCount: 1, dataQuality: q,
            stateVersion: 1L, eventRevision: eventRevision,
            observedAtUtc: Utc(), limitations: Array.Empty<string>());

    private static AacThesisSnapshot Aac(
        AacState state, string id = "AAC-1",
        long eventRevision = 1L) =>
        new AacThesisSnapshot(
            id, AacThesisPolicyConfig.PolicyVersion,
            evidenceId: "EV-2", episodeId: "EP-2",
            primaryAuctionId: "PI-1", referenceId: "REF-2",
            direction: ThesisDirection.Short, aacState: state, notCalibrated: false,
            attemptCount: 1, dataQuality: ThesisDataQuality.Complete,
            stateVersion: 1L, eventRevision: eventRevision,
            observedAtUtc: Utc(), limitations: Array.Empty<string>());

    private static SignalMaturitySetSnapshot Maturity(
        FarThesisSnapshot[]? far = null,
        AacThesisSnapshot[]? aac = null,
        ThesisModuleState thesisState = ThesisModuleState.Ready)
    {
        var host = new SignalMaturityHost(new SignalMaturityPolicyConfig(enabled: true));
        var farSet = far is null ? null : new FarThesisSetSnapshot(
            thesisState, FarThesisPolicyConfig.PolicyVersion,
            far, Array.Empty<FarThesisSnapshot>(), far.Length > 0 ? far[^1] : null,
            0, 0, Utc(), Utc(1), Array.Empty<string>());
        var aacSet = aac is null ? null : new AacThesisSetSnapshot(
            thesisState, AacThesisPolicyConfig.PolicyVersion,
            aac, Array.Empty<AacThesisSnapshot>(), aac.Length > 0 ? aac[^1] : null,
            0, 0, Utc(), Utc(1), Array.Empty<string>());
        return host.Rebuild(farSet, aacSet, Utc());
    }

    private static ThesisContractHost EnabledHost() =>
        new ThesisContractHost(new ThesisContractPolicyConfig(enabled: true));

    private static ThesisContractSnapshot OneContract() =>
        EnabledHost().Rebuild(Maturity(far: new[] { Far(FarState.ReentryDeveloping) }), Utc())
            .ActiveContracts[0];

    // ========== A: Policy ==========

    [Fact]
    public void A01_PolicyVersion_is_thesis_contract_v1() =>
        Assert.Equal("THESIS_CONTRACT_POLICY_V1", ThesisContractPolicyConfig.PolicyVersion);

    [Fact]
    public void A02_Module_default_is_disabled() =>
        Assert.False(new ThesisContractPolicyConfig().Enabled);

    [Fact]
    public void A03_Five_invalidation_dimensions_and_five_horizons_are_required()
    {
        Assert.Equal(5, ThesisContractPolicyConfig.RequiredInvalidationDimensions);
        Assert.Equal(5, ThesisContractPolicyConfig.RequiredHorizonRoles);
    }

    [Fact]
    public void A04_Protective_stop_is_not_authorized_in_phase_3c() =>
        Assert.False(ThesisContractPolicyConfig.ProtectiveStopAuthorized);

    [Fact]
    public void A05_Limitation_constants_are_stable()
    {
        Assert.Equal("THESIS_CONTRACT_THRESHOLDS_NOT_CALIBRATED", ThesisContractPolicyConfig.LimitationNotCalibrated);
        Assert.Equal("EVIDENCE_INVALIDATION_NOT_CALIBRATED", ThesisContractPolicyConfig.LimitationEvidenceInvalidationNotCalibrated);
        Assert.Equal("THESIS_EXPIRY_DURATION_NOT_CALIBRATED", ThesisContractPolicyConfig.LimitationExpiryNotCalibrated);
        Assert.Equal("THESIS_CONTRACT_NO_PROTECTIVE_STOP", ThesisContractPolicyConfig.LimitationNoProtectiveStop);
        Assert.Equal("LIVE_ONLY_HISTORY", ThesisContractPolicyConfig.LimitationLiveOnly);
    }

    // ========== B: Enum gates ==========

    [Fact]
    public void B01_ContractState_calibrated_values_are_reserved()
    {
        Assert.Equal(2, (int)ThesisContractState.NotCalibrated);
        Assert.True((int)ThesisContractState.Complete >= 100);
        Assert.True((int)ThesisContractState.Executable >= 100);
    }

    [Fact]
    public void B02_InvalidationDimensionState_triggered_is_reserved()
    {
        Assert.Equal(1, (int)InvalidationDimensionState.NotCalibrated);
        Assert.True((int)InvalidationDimensionState.Triggered >= 100);
        Assert.True((int)InvalidationDimensionState.Cleared >= 100);
    }

    /// <summary>v1.3 §11.3: five dimensions, not v1.2's four.</summary>
    [Fact]
    public void B03_Invalidation_has_five_dimensions_including_evidence()
    {
        var dims = Enum.GetValues<InvalidationDimension>();
        Assert.Equal(5, dims.Length);
        Assert.Contains(InvalidationDimension.Price, dims);
        Assert.Contains(InvalidationDimension.Auction, dims);
        Assert.Contains(InvalidationDimension.Time, dims);
        Assert.Contains(InvalidationDimension.Context, dims);
        Assert.Contains(InvalidationDimension.Evidence, dims);
    }

    /// <summary>v1.2 §11.2: exactly five thesis horizon roles.</summary>
    [Fact]
    public void B04_Five_thesis_horizon_roles_exist()
    {
        var roles = Enum.GetValues<ThesisHorizonRole>();
        Assert.Equal(5, roles.Length);
        Assert.Contains(ThesisHorizonRole.Context, roles);
        Assert.Contains(ThesisHorizonRole.Thesis, roles);
        Assert.Contains(ThesisHorizonRole.Trigger, roles);
        Assert.Contains(ThesisHorizonRole.Management, roles);
        Assert.Contains(ThesisHorizonRole.Target, roles);
    }

    [Fact]
    public void B05_HorizonSource_default_is_unavailable() =>
        Assert.Equal(ThesisHorizonSource.Unavailable, default(ThesisHorizonSource));

    [Fact]
    public void B06_SourceOfMove_default_is_unknown() =>
        Assert.Equal(SourceOfMove.Unknown, default(SourceOfMove));

    // ========== C: Disabled / reset / awaiting ==========

    [Fact]
    public void C01_Disabled_host_publishes_disabled_snapshot()
    {
        var host = new ThesisContractHost(new ThesisContractPolicyConfig(enabled: false));
        var set = host.Rebuild(Maturity(far: new[] { Far(FarState.ReentryDeveloping) }), Utc());
        Assert.Equal(ThesisContractModuleState.Disabled, set.ModuleState);
        Assert.Empty(set.ActiveContracts);
        Assert.Contains("MODULE_DISABLED", set.Limitations);
    }

    [Fact]
    public void C02_Configure_null_throws() =>
        Assert.Throws<ArgumentNullException>(() => EnabledHost().Configure(null!));

    [Fact]
    public void C03_Reset_clears_current()
    {
        var host = EnabledHost();
        host.Rebuild(Maturity(far: new[] { Far(FarState.ReentryDeveloping) }), Utc());
        host.Reset();
        Assert.Null(host.Current);
    }

    [Fact]
    public void C04_Null_maturity_yields_awaiting()
    {
        var set = EnabledHost().Rebuild(null, Utc());
        Assert.Equal(ThesisContractModuleState.AwaitingMaturity, set.ModuleState);
    }

    [Fact]
    public void C05_Invalid_maturity_yields_invalid()
    {
        var host = new SignalMaturityHost(new SignalMaturityPolicyConfig(enabled: true));
        var badFar = new FarThesisSetSnapshot(
            ThesisModuleState.Invalid, FarThesisPolicyConfig.PolicyVersion,
            Array.Empty<FarThesisSnapshot>(), Array.Empty<FarThesisSnapshot>(),
            null, 0, 0, Utc(), Utc(1), Array.Empty<string>());
        var maturity = host.Rebuild(badFar, null, Utc());

        var set = EnabledHost().Rebuild(maturity, Utc());
        Assert.Equal(ThesisContractModuleState.Invalid, set.ModuleState);
        Assert.Contains("MATURITY_INPUT_INVALID", set.Limitations);
    }

    [Fact]
    public void C06_Empty_maturity_yields_awaiting()
    {
        var set = EnabledHost().Rebuild(Maturity(far: Array.Empty<FarThesisSnapshot>()), Utc());
        Assert.Equal(ThesisContractModuleState.AwaitingMaturity, set.ModuleState);
    }

    // ========== D: Five horizons (v1.2 §11.2) ==========

    [Fact]
    public void D01_All_five_horizon_roles_are_declared()
    {
        var c = OneContract();
        Assert.Equal(5, c.Horizons.Count);
        Assert.True(c.AllHorizonRolesDeclared);
        foreach (var role in Enum.GetValues<ThesisHorizonRole>())
            Assert.Contains(c.Horizons, h => h.Role == role);
    }

    [Fact]
    public void D02_No_horizon_role_is_silently_omitted()
    {
        var c = OneContract();
        Assert.Equal(c.Horizons.Select(h => h.Role).Distinct().Count(), c.Horizons.Count);
    }

    /// <summary>
    /// The multi-horizon map is not authorized, so every source must report
    /// Unavailable rather than being guessed.
    /// </summary>
    [Fact]
    public void D03_Horizon_sources_are_unavailable_not_fabricated()
    {
        var c = OneContract();
        Assert.All(c.Horizons, h => Assert.Equal(ThesisHorizonSource.Unavailable, h.Source));
        Assert.All(c.Horizons, h => Assert.False(h.IsResolved));
        Assert.Contains(ThesisContractPolicyConfig.LimitationHorizonMapUnavailable, c.Limitations);
    }

    // ========== E: Five invalidation dimensions ==========

    [Fact]
    public void E01_All_five_invalidation_dimensions_are_declared()
    {
        var c = OneContract();
        Assert.Equal(5, c.Invalidations.Count);
        Assert.True(c.AllInvalidationDimensionsDeclared);
        foreach (var d in Enum.GetValues<InvalidationDimension>())
            Assert.Contains(c.Invalidations, i => i.Dimension == d);
    }

    [Fact]
    public void E02_Every_dimension_is_not_calibrated()
    {
        var c = OneContract();
        Assert.All(c.Invalidations,
            i => Assert.Equal(InvalidationDimensionState.NotCalibrated, i.State));
    }

    [Fact]
    public void E03_Every_dimension_carries_its_own_limitation()
    {
        var c = OneContract();
        Assert.All(c.Invalidations, i => Assert.False(string.IsNullOrWhiteSpace(i.Limitation)));
        Assert.Equal(5, c.Invalidations.Select(i => i.Limitation).Distinct().Count());
    }

    /// <summary>
    /// v1.3 G-INV-001: Evidence invalidation is its own dimension, not folded
    /// into Auction invalidation — it fires earlier and on different inputs.
    /// </summary>
    [Fact]
    public void E04_Evidence_invalidation_is_independent_of_auction_invalidation()
    {
        var c = OneContract();
        var evidence = c.Invalidations.Single(i => i.Dimension == InvalidationDimension.Evidence);
        var auction = c.Invalidations.Single(i => i.Dimension == InvalidationDimension.Auction);
        Assert.NotEqual(evidence.Limitation, auction.Limitation);
        Assert.Equal(ThesisContractPolicyConfig.LimitationEvidenceInvalidationNotCalibrated, evidence.Limitation);
    }

    /// <summary>
    /// v1.2 §32.3 / v1.3 G-INV-002: analytical invalidation is not a stop.
    /// No dimension may carry a price.
    /// </summary>
    [Fact]
    public void E05_Invalidation_declaration_carries_no_price()
    {
        var props = typeof(InvalidationDimensionDeclaration).GetProperties().Select(p => p.Name).ToArray();
        Assert.DoesNotContain(props, n => n.Contains("Price", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(props, n => n.Contains("Stop", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(props, n => n.Contains("Tick", StringComparison.OrdinalIgnoreCase));
    }

    // ========== F: Expiry and expected behaviour ==========

    /// <summary>v1.2 §32.1 ExpiresAt — duration is NOT CALIBRATED, never fabricated.</summary>
    [Fact]
    public void F01_Expiry_is_never_fabricated()
    {
        var c = OneContract();
        Assert.Null(c.ExpiresAtUtc);
        Assert.Contains(ThesisContractPolicyConfig.LimitationExpiryNotCalibrated, c.Limitations);
    }

    [Fact]
    public void F02_Expected_behavior_is_carried_from_maturity()
    {
        var c = OneContract();
        Assert.Equal(ExpectedBehaviorContractKind.FarReentry, c.ExpectedBehavior);
    }

    [Fact]
    public void F03_Aac_pullback_carries_aac_retest_expectation()
    {
        var set = EnabledHost().Rebuild(Maturity(aac: new[] { Aac(AacState.Pullback) }), Utc());
        Assert.Equal(ExpectedBehaviorContractKind.AacRetest, set.ActiveContracts[0].ExpectedBehavior);
    }

    /// <summary>v1.2 §11.3: source-of-move needs the horizon map, so it stays Unknown.</summary>
    [Fact]
    public void F04_SourceOfMove_is_unknown_not_guessed() =>
        Assert.Equal(SourceOfMove.Unknown, OneContract().SourceOfMove);

    // ========== G: Missing evidence (v1.3 G-THE-001) ==========

    [Fact]
    public void G01_Missing_evidence_is_never_empty()
    {
        var c = OneContract();
        Assert.NotEmpty(c.MissingEvidence);
    }

    [Fact]
    public void G02_Missing_evidence_is_structured_not_free_text()
    {
        var prop = typeof(ThesisContractSnapshot).GetProperty("MissingEvidence");
        Assert.NotNull(prop);
        Assert.Equal(typeof(IReadOnlyList<MissingEvidenceKind>), prop!.PropertyType);
    }

    [Fact]
    public void G03_Missing_evidence_names_the_uncalibrated_dependencies()
    {
        var c = OneContract();
        Assert.Contains(MissingEvidenceKind.AcceptanceResolutionNotCalibrated, c.MissingEvidence);
        Assert.Contains(MissingEvidenceKind.ReentryResolutionNotCalibrated, c.MissingEvidence);
        Assert.Contains(MissingEvidenceKind.OldValueReclaimNotObserved, c.MissingEvidence);
        Assert.Contains(MissingEvidenceKind.TradeFacilitationNotCalibrated, c.MissingEvidence);
        Assert.Contains(MissingEvidenceKind.HorizonMapUnavailable, c.MissingEvidence);
        Assert.Contains(MissingEvidenceKind.MboBlocked, c.MissingEvidence);
    }

    // ========== H: Consistency gate (v1.3 §11.4 / G-THE-006) ==========

    [Fact]
    public void H01_Consistency_gate_has_four_separate_flags()
    {
        var props = typeof(ThesisConsistencyGate)
            .GetProperties().Where(p => p.PropertyType == typeof(bool)).ToArray();
        // Four conditions + the AllSatisfied aggregate.
        Assert.Equal(5, props.Length);
        var names = props.Select(p => p.Name).ToArray();
        Assert.Contains("RelatesToThesis", names);
        Assert.Contains("ProducedPriceResult", names);
        Assert.Contains("WasMaintained", names);
        Assert.Contains("WithinDeclaredHorizon", names);
    }

    [Fact]
    public void H02_Gate_is_not_satisfied_without_post_entry_information()
    {
        var g = OneContract().ConsistencyGate;
        Assert.False(g.RelatesToThesis);
        Assert.False(g.ProducedPriceResult);
        Assert.False(g.WasMaintained);
        Assert.False(g.WithinDeclaredHorizon);
        Assert.False(g.AllSatisfied);
    }

    [Fact]
    public void H03_Gate_requires_all_four_conditions()
    {
        Assert.False(new ThesisConsistencyGate(true, true, true, false).AllSatisfied);
        Assert.False(new ThesisConsistencyGate(true, true, false, true).AllSatisfied);
        Assert.False(new ThesisConsistencyGate(true, false, true, true).AllSatisfied);
        Assert.False(new ThesisConsistencyGate(false, true, true, true).AllSatisfied);
        Assert.True(new ThesisConsistencyGate(true, true, true, true).AllSatisfied);
    }

    // ========== I: NOT_CALIBRATED invariants ==========

    [Fact]
    public void I01_Contract_state_is_always_not_calibrated()
    {
        foreach (var far in new[] { FarState.EpisodeActive, FarState.ReentryDeveloping, FarState.ReacceptedInside })
        {
            var set = EnabledHost().Rebuild(Maturity(far: new[] { Far(far) }), Utc());
            Assert.Equal(ThesisContractState.NotCalibrated, set.ActiveContracts[0].ContractState);
            Assert.True(set.ActiveContracts[0].NotCalibrated);
        }
    }

    [Fact]
    public void I02_Complete_and_executable_counts_are_always_zero()
    {
        var set = EnabledHost().Rebuild(
            Maturity(far: new[] { Far(FarState.ReacceptedInside) },
                     aac: new[] { Aac(AacState.AcceptedOutside) }), Utc());
        Assert.Equal(0, set.CompleteCount);
        Assert.Equal(0, set.ExecutableCount);
        Assert.Equal(2, set.DeclaredCount);
    }

    [Fact]
    public void I03_Protective_stop_flag_is_always_false()
    {
        var set = EnabledHost().Rebuild(Maturity(far: new[] { Far(FarState.ReentryDeveloping) }), Utc());
        Assert.False(set.ProtectiveStopAuthorized);
        Assert.Contains(ThesisContractPolicyConfig.LimitationNoProtectiveStop, set.Limitations);
    }

    [Fact]
    public void I04_Set_limitations_bar_target_path_and_risk_sizing()
    {
        var set = EnabledHost().Rebuild(Maturity(far: new[] { Far(FarState.ReentryDeveloping) }), Utc());
        Assert.Contains(ThesisContractPolicyConfig.LimitationNoTargetPath, set.Limitations);
        Assert.Contains(ThesisContractPolicyConfig.LimitationNoRiskSizing, set.Limitations);
        Assert.Contains(ThesisContractPolicyConfig.LimitationExecutableNotAuthorized, set.Limitations);
    }

    // ========== J: Identity, family, fingerprint ==========

    [Fact]
    public void J01_ContractId_is_derived_from_maturity_id()
    {
        var c = OneContract();
        Assert.StartsWith("TC:", c.ContractId, StringComparison.Ordinal);
        Assert.Equal("TC:" + c.MaturityId, c.ContractId);
    }

    [Fact]
    public void J02_Family_is_mapped_from_maturity_family()
    {
        var far = EnabledHost().Rebuild(Maturity(far: new[] { Far(FarState.ReentryDeveloping) }), Utc());
        Assert.Equal(ThesisFamily.Far, far.ActiveContracts[0].Family);

        var aac = EnabledHost().Rebuild(Maturity(aac: new[] { Aac(AacState.AcceptanceDeveloping) }), Utc());
        Assert.Equal(ThesisFamily.Aac, aac.ActiveContracts[0].Family);
    }

    [Fact]
    public void J03_Direction_is_carried()
    {
        var far = EnabledHost().Rebuild(Maturity(far: new[] { Far(FarState.ReentryDeveloping) }), Utc());
        Assert.Equal(ThesisDirection.Long, far.ActiveContracts[0].Direction);
    }

    [Fact]
    public void J04_Unchanged_input_returns_cached_instance()
    {
        var host = EnabledHost();
        var m = Maturity(far: new[] { Far(FarState.ReentryDeveloping) });
        var a = host.Rebuild(m, Utc());
        var b = host.Rebuild(m, Utc(5));
        Assert.Same(a, b);
    }

    [Fact]
    public void J05_Partial_maturity_yields_partial_contract_set()
    {
        var set = EnabledHost().Rebuild(
            Maturity(far: new[] { Far(FarState.ReentryDeveloping, q: ThesisDataQuality.Partial) }), Utc());
        Assert.Equal(ThesisContractModuleState.Partial, set.ModuleState);
    }

    [Fact]
    public void J06_LatestUpdated_tracks_highest_event_revision()
    {
        var set = EnabledHost().Rebuild(
            Maturity(far: new[]
            {
                Far(FarState.EpisodeActive, id: "FAR-1", eventRevision: 1L),
                Far(FarState.ReentryDeveloping, id: "FAR-2", eventRevision: 9L)
            }), Utc());
        Assert.Equal("TC:SM:FAR-2", set.LatestUpdated!.ContractId);
    }

    // ========== K: Runtime wiring ==========

    private static GcaeRuntimeSnapshot PublishWith(ThesisContractSetSnapshot? contract, bool diag = false)
    {
        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(0.1m, 14));
        return engine.Publish(
            new ObservedInstrumentSnapshot("GCU6", "GCU6-ID", "GCU6", "COMEX",
                new DateTime(2026, 8, 27), 0.1m, "GC", "GCU6", "COMEX", 0.1m, null),
            "GCU7",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            tradeObserved: false, lastTradeCallbackUtc: null,
            rawRecorderMasterEnabled: false, tradeRecordingEnabled: true,
            recorderAccepting: false, recorderFaulted: false, recorderSessionPresent: false,
            indicatorDisposed: false, timestampUtc: Utc(),
            thesisContract: contract,
            showThesisContractDiagnostics: diag);
    }

    [Fact]
    public void K01_SnapshotVersion_is_0_17_0() =>
        Assert.Equal("0.17.0", GcaeRuntimeSnapshot.SnapshotVersion);

    [Fact]
    public void K02_Contract_defaults_to_null_on_snapshot() =>
        Assert.Null(PublishWith(null).ThesisContract);

    [Fact]
    public void K03_Contract_limitations_merge_into_known_limitations()
    {
        var set = EnabledHost().Rebuild(Maturity(far: new[] { Far(FarState.ReentryDeveloping) }), Utc());
        var snap = PublishWith(set);
        Assert.Contains(ThesisContractPolicyConfig.LimitationNotCalibrated, snap.KnownLimitations);
        Assert.Contains(ThesisContractPolicyConfig.LimitationNoProtectiveStop, snap.KnownLimitations);
    }

    // ========== L: GPS card ==========

    [Fact]
    public void L01_GpsCard_has_14_diagnostic_rows()
    {
        var vm = AuctionGpsCardMapper.FromSnapshot(PublishWith(null), showDiagnostics: true);
        Assert.Equal(14, vm.DiagnosticRows.Count);
        Assert.Contains("CONTRACT: NOT AVAILABLE", vm.DiagnosticRows);
    }

    [Fact]
    public void L02_ContractLines_null_set_is_empty() =>
        Assert.Empty(AuctionGpsCardMapper.BuildThesisContractLines(null, false));

    [Fact]
    public void L03_ContractLines_disabled_is_single_row()
    {
        var host = new ThesisContractHost(new ThesisContractPolicyConfig(enabled: false));
        var rows = AuctionGpsCardMapper.BuildThesisContractLines(host.Rebuild(null, Utc()), false);
        Assert.Single(rows);
        Assert.Equal("CONTRACT: DISABLED", rows[0]);
    }

    [Fact]
    public void L04_ContractLines_ready_declares_gates()
    {
        var set = EnabledHost().Rebuild(Maturity(far: new[] { Far(FarState.ReentryDeveloping) }), Utc());
        var rows = AuctionGpsCardMapper.BuildThesisContractLines(set, false);
        Assert.Contains("CONTRACT STATE: NOT CALIBRATED", rows);
        Assert.Contains("PROTECTIVE STOP: NOT AUTHORIZED", rows);
        Assert.Contains("THESIS EXPIRY: NOT CALIBRATED", rows);
        Assert.Contains(rows, r => r.StartsWith("INVALIDATION DIMS: 5/5", StringComparison.Ordinal));
        Assert.Contains(rows, r => r.StartsWith("HORIZONS DECLARED: 5/5", StringComparison.Ordinal));
    }

    [Fact]
    public void L05_ContractLines_diagnostics_list_every_dimension()
    {
        var set = EnabledHost().Rebuild(Maturity(far: new[] { Far(FarState.ReentryDeveloping) }), Utc());
        var rows = AuctionGpsCardMapper.BuildThesisContractLines(set, true);
        foreach (var d in Enum.GetValues<InvalidationDimension>())
            Assert.Contains(rows, r =>
                r.StartsWith("INVALIDATION " + d.ToString().ToUpperInvariant() + ":", StringComparison.Ordinal));
        Assert.Contains("CONSISTENCY GATE: NOT SATISFIED", rows);
    }

    [Fact]
    public void L06_ContractLines_never_emit_executable_wording()
    {
        var set = EnabledHost().Rebuild(
            Maturity(far: new[] { Far(FarState.ReacceptedInside) }), Utc());
        var text = string.Join(" | ", AuctionGpsCardMapper.BuildThesisContractLines(set, true));
        Assert.DoesNotContain("CONTRACT STATE: EXECUTABLE", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CONTRACT STATE: COMPLETE", text, StringComparison.OrdinalIgnoreCase);
    }

    // ========== M: Scope guards ==========

    /// <summary>
    /// v1.2 §32.4/§33/§34/§35 are NOT authorized in Phase 3C. The contract must
    /// carry no executable pricing surface whatsoever.
    /// </summary>
    [Fact]
    public void M01_Contract_carries_no_entry_stop_target_or_size()
    {
        var banned = new[] { "Entry", "Stop", "Target", "Size", "Quantity", "Reward", "Rr" };

        // ProtectiveStopAuthorized is the NEGATIVE declaration required by
        // v1.2 §32.3 — it exists to report that stops are not authorized, and
        // is asserted false by I03. It is not an execution surface.
        var allowed = new[] { "ProtectiveStopAuthorized" };

        foreach (var t in new[] { typeof(ThesisContractSnapshot), typeof(ThesisContractSetSnapshot) })
        foreach (var p in t.GetProperties())
        {
            if (allowed.Contains(p.Name, StringComparer.Ordinal)) continue;
            foreach (var b in banned)
                Assert.False(p.Name.Contains(b, StringComparison.OrdinalIgnoreCase),
                    t.Name + "." + p.Name + " leaks an unauthorized execution surface (" + b + ")");
        }

        // The one allowed name must genuinely be a negative flag.
        Assert.Equal(typeof(bool),
            typeof(ThesisContractSetSnapshot).GetProperty("ProtectiveStopAuthorized")!.PropertyType);
    }

    [Fact]
    public void M02_Contract_carries_no_score_or_probability()
    {
        foreach (var p in typeof(ThesisContractSnapshot).GetProperties())
        {
            Assert.DoesNotContain("Score", p.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Probability", p.Name, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void M03_Contract_carries_no_gex_surface()
    {
        foreach (var p in typeof(ThesisContractSnapshot).GetProperties())
            Assert.DoesNotContain("Gex", p.Name, StringComparison.OrdinalIgnoreCase);
    }
}
