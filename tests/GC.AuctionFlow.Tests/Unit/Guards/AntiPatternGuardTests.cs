using System.Reflection;
using GC.AuctionFlow.Cluster;
using GC.AuctionFlow.Core;
using GC.AuctionFlow.Directional;
using GC.AuctionFlow.EffortResult;
using GC.AuctionFlow.Efficiency;
using GC.AuctionFlow.Evidence;
using GC.AuctionFlow.Facilitation;
using GC.AuctionFlow.Maturity;
using GC.AuctionFlow.Orderflow;
using GC.AuctionFlow.Participation;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Resolution;
using GC.AuctionFlow.Thesis;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Guards;

/// <summary>
/// v1.3 §12 Anti-Pattern Guard Registry (AP-001 … AP-028).
/// Each guard is derived from a "Sai lầm thường gặp" section of KIM ĐẤU KINH
/// and asserts that the engine STRUCTURALLY cannot fall into that mistake.
///
/// Per v1.3 G-AP-002 these check behaviour and type surface, not the presence
/// of strings in source files.
/// </summary>
public sealed class AntiPatternGuardTests
{
    private static readonly Assembly Engine = typeof(TradeFacilitationHost).Assembly;

    /// <summary>
    /// The ATAS-facing indicator type cannot be loaded in the test host
    /// (ATAS.Indicators is not deployed there). Enumerate everything that
    /// does load — the engine domain types — and skip the rest.
    /// </summary>
    private static IEnumerable<Type> AllTypes
    {
        get
        {
            try { return Engine.GetTypes(); }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t is not null).Select(t => t!);
            }
        }
    }

    private static IEnumerable<Type> PublicEnums =>
        AllTypes.Where(t => t.IsEnum && t.IsPublic);

    private static IEnumerable<PropertyInfo> AllPublicProperties =>
        AllTypes.Where(t => t.IsPublic && !t.IsEnum)
                .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));

    /// <summary>
    /// Public static methods whose signatures fully resolve. Members of the
    /// ATAS-facing surface reference types that are absent in the test host, so
    /// reading their signature throws; those are skipped, not failed.
    /// </summary>
    private static IEnumerable<MethodInfo> SafeStaticMethods()
    {
        foreach (var t in AllTypes)
        {
            MethodInfo[] methods;
            try { methods = t.GetMethods(BindingFlags.Public | BindingFlags.Static); }
            catch { continue; }

            foreach (var m in methods)
            {
                bool resolvable;
                try
                {
                    _ = m.ReturnType;
                    _ = m.GetParameters();
                    resolvable = true;
                }
                catch { resolvable = false; }

                if (resolvable) yield return m;
            }
        }
    }

    /// <summary>
    /// Identifier-aware containment: matches <paramref name="token"/> only at a
    /// PascalCase word boundary. Raw substring matching produces false positives
    /// ("PeriodIndexCompressor" contains "dex", "ContainingExact" contains "gEx").
    /// </summary>
    private static bool ContainsToken(string identifier, string token)
    {
        for (var i = 0; i <= identifier.Length - token.Length; i++)
        {
            if (string.Compare(identifier, i, token, 0, token.Length,
                    StringComparison.OrdinalIgnoreCase) != 0)
                continue;

            // Must start a word: position 0, or preceded by a non-lowercase char.
            if (i > 0 && char.IsLower(identifier[i - 1]))
                continue;

            // Must end a word: end of string, or followed by an upper/non-letter.
            var after = i + token.Length;
            if (after < identifier.Length && char.IsLower(identifier[after]))
                continue;

            return true;
        }
        return false;
    }

    /// <summary>Any enum member at 100+ is by project convention a calibration-gated state.</summary>
    private static IEnumerable<(Type Enum, string Member)> CalibratedMembers =>
        PublicEnums.SelectMany(e => Enum.GetValues(e).Cast<object>()
            .Where(v => Convert.ToInt64(v) >= 100L)
            .Select(v => (e, v.ToString() ?? "")));

    // ================================================================
    // AP-001 / AP-002 / AP-022 — single candle or wick is never evidence
    // ================================================================

    /// <summary>
    /// AP-001 + AP-002 + AP-022 (KDK Ch 17, Ch 56):
    /// geometric re-entry must never be promoted to stable reacceptance,
    /// and stable reacceptance must remain calibration-gated.
    /// </summary>
    [Fact]
    public void AP_001_002_022_Geometric_reentry_is_never_stable_reacceptance()
    {
        Assert.True((int)ReentryObservationState.GeometricReentry < 100,
            "GeometricReentry is an observation and must stay observable.");
        Assert.True((int)ReentryResolutionState.StableReacceptance >= 100,
            "StableReacceptance must remain calibration-gated.");
        Assert.True((int)ReentryResolutionState.ReentryFailed >= 100);

        // No API may convert one into the other.
        Assert.DoesNotContain(SafeStaticMethods(),
            m => m.ReturnType == typeof(ReentryResolutionState)
                 && m.GetParameters().Length == 1
                 && m.GetParameters()[0].ParameterType == typeof(ReentryObservationState));
    }

    /// <summary>
    /// AP-001 (KDK Ch 17): a single close outside must not establish acceptance.
    /// Established/Failed acceptance stay calibration-gated.
    /// </summary>
    [Fact]
    public void AP_001_Acceptance_established_is_calibration_gated()
    {
        Assert.True((int)AcceptanceResolutionState.Established >= 100);
        Assert.True((int)AcceptanceResolutionState.Failed >= 100);
        Assert.Equal(4, (int)AcceptanceResolutionState.NotCalibrated);
    }

    // ================================================================
    // AP-003 / AP-011 — no hardcoded thresholds, no single global ratio
    // ================================================================

    /// <summary>
    /// AP-003 + AP-011 + AP-024 (KDK Ch 17, Ch 30, Ch 60):
    /// no policy config may expose a numeric threshold constant.
    /// Thresholds must come from the Calibration Ledger, never from source.
    /// </summary>
    [Fact]
    public void AP_003_011_024_No_policy_exposes_numeric_thresholds()
    {
        var policyTypes = AllTypes.Where(t =>
            t.IsPublic && t.Name.EndsWith("PolicyConfig", StringComparison.Ordinal));
        Assert.NotEmpty(policyTypes);

        var offenders = new List<string>();
        foreach (var t in policyTypes)
        {
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (f.FieldType == typeof(decimal) || f.FieldType == typeof(double)
                    || f.FieldType == typeof(float))
                    offenders.Add(t.Name + "." + f.Name);

                // Integer constants are allowed only for capacities/versions, never thresholds.
                if ((f.FieldType == typeof(int) || f.FieldType == typeof(long))
                    && (f.Name.Contains("Threshold", StringComparison.OrdinalIgnoreCase)
                        || f.Name.Contains("MinimumRatio", StringComparison.OrdinalIgnoreCase)
                        || f.Name.Contains("Cutoff", StringComparison.OrdinalIgnoreCase)))
                    offenders.Add(t.Name + "." + f.Name);
            }
        }
        Assert.Empty(offenders);
    }

    // ================================================================
    // AP-004 / AP-005 / AP-006 — unresolved is a valid outcome
    // ================================================================

    /// <summary>
    /// AP-004 + AP-005 + AP-006 (KDK Ch 18):
    /// FAR/AAC conclusions are calibration-gated, and an explicit
    /// "not concluded" value always exists so Unresolved is never skipped.
    /// </summary>
    [Fact]
    public void AP_004_005_006_Unresolved_is_representable_and_conclusions_are_gated()
    {
        Assert.True((int)AuctionResolutionConclusion.FarCandidate >= 100);
        Assert.True((int)AuctionResolutionConclusion.AacCandidate >= 100);
        Assert.Equal(0, (int)AuctionResolutionConclusion.Unknown);
        Assert.Equal(1, (int)AuctionResolutionConclusion.NotCalibrated);
    }

    // ================================================================
    // AP-007 / AP-008 / AP-012 — absorption is a candidate, not a verdict
    // ================================================================

    /// <summary>
    /// AP-007 + AP-008 + AP-012 (KDK Ch 28, Ch 30):
    /// absorption must be named as a candidate ("Potential…") and stay gated,
    /// and no enum may assert a confirmed reversal.
    /// </summary>
    [Fact]
    public void AP_007_008_012_Absorption_is_potential_only_and_no_confirmed_reversal()
    {
        Assert.StartsWith("Potential",
            nameof(EffortResultClassificationState.PotentialPassiveAbsorption), StringComparison.Ordinal);
        Assert.True((int)EffortResultClassificationState.PotentialPassiveAbsorption >= 100);

        var banned = new[] { "ConfirmedReversal", "Reversal", "WhaleBuying", "WhaleSelling" };
        foreach (var (e, member) in PublicEnums.SelectMany(e =>
                     Enum.GetNames(e).Select(n => (e, n))))
            Assert.DoesNotContain(banned, b =>
                string.Equals(member, b, StringComparison.OrdinalIgnoreCase));
    }

    // ================================================================
    // AP-009 / AP-010 — exhaustion is regime-relative and gated
    // ================================================================

    /// <summary>
    /// AP-009 + AP-010 (KDK Ch 29): exhaustion is a candidate, calibration-gated,
    /// and a participation-regime label exists so it can be stratified.
    /// </summary>
    [Fact]
    public void AP_009_010_Exhaustion_is_gated_and_regime_is_available()
    {
        Assert.StartsWith("Potential",
            nameof(EffortResultClassificationState.PotentialExhaustion), StringComparison.Ordinal);
        Assert.True((int)EffortResultClassificationState.PotentialExhaustion >= 100);
        // Regime stratification input must exist (v1.3 G-DISC-008).
        Assert.True(Enum.GetValues<ThinParticipationLabel>().Length > 1);
    }

    // ================================================================
    // AP-013 / AP-014 — facilitation is not Delta and needs a horizon
    // ================================================================

    /// <summary>
    /// AP-013 (KDK Ch 31): facilitation must never be a signed-Delta shortcut.
    /// The snapshot stores direction-consistent effort and achieved progress
    /// separately, and the verdict stays gated.
    /// </summary>
    [Fact]
    public void AP_013_Facilitation_is_not_a_delta_field()
    {
        var props = typeof(TradeFacilitationSnapshot)
            .GetProperties().Select(p => p.Name).ToArray();
        Assert.DoesNotContain(props, n => n.Equals("Delta", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(props, n => n.Equals("Cvd", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(props, n => n.Contains("DirectionConsistent", StringComparison.Ordinal));
        Assert.True((int)TradeFacilitationClassificationState.Healthy >= 100);
        Assert.True((int)TradeFacilitationClassificationState.Failing >= 100);
    }

    /// <summary>
    /// AP-014 (KDK Ch 31): a facilitation reading must carry its measurement scope,
    /// otherwise the label has no timeframe and is meaningless.
    /// </summary>
    [Fact]
    public void AP_014_Facilitation_carries_measurement_scope()
    {
        var props = typeof(TradeFacilitationSnapshot).GetProperties().Select(p => p.Name).ToArray();
        Assert.Contains(props, n => n.Equals("ScopeType", StringComparison.Ordinal));
        Assert.Contains(props, n => n.Equals("EpisodeId", StringComparison.Ordinal));
    }

    // ================================================================
    // AP-015 / AP-016 — sweep and trapped-trader are not emitted
    // ================================================================

    /// <summary>
    /// AP-015 + AP-016 (KDK Ch 32, v1.3 G-DISC-004/005):
    /// no Tier-4 inference label may exist anywhere in the engine's public
    /// enum surface while MBO is BLOCKED.
    /// </summary>
    [Fact]
    public void AP_015_016_No_sweep_stoprun_or_trapped_labels_in_enums()
    {
        var banned = new[] { "Sweep", "StopRun", "StopHunt", "Trapped", "LiquidityGrab", "SmartMoney" };

        // CapabilityKind is the capability MATRIX, not an analytical verdict: it must be
        // able to name a feed capability in order to report it BLOCKED. Naming a capability
        // is not emitting a Tier-4 label, so it is exempt by design.
        var offenders = PublicEnums
            .Where(e => e != typeof(CapabilityKind))
            .SelectMany(e => Enum.GetNames(e).Select(n => e.Name + "." + n))
            .Where(full => banned.Any(b => ContainsToken(full, b)))
            .ToArray();
        Assert.Empty(offenders);

        // The capability declaration must stay gated: the DEFAULT subscription state
        // is "not attempted", so the engine can never assume MBO is live.
        Assert.Equal(MboSubscriptionState.NotAttempted, default(MboSubscriptionState));
    }

    // ================================================================
    // AP-017 / AP-018 — direction before Delta; imbalance is not acceptance
    // ================================================================

    /// <summary>
    /// AP-017 (KDK Ch 57): efficiency evidence must carry the attempted auction
    /// direction, so Delta is never read without knowing which auction is tested.
    /// </summary>
    [Fact]
    public void AP_017_Result_direction_exists_and_admits_unknown()
    {
        Assert.Contains(typeof(AuctionEfficiencyEvidenceSnapshot).GetProperties(),
            p => p.PropertyType == typeof(EfficiencyResultDirection));
        Assert.Equal(0, (int)EfficiencyResultDirection.Unknown);
    }

    /// <summary>
    /// AP-018 (KDK Ch 57): cluster raw features must stay unclassified —
    /// imbalance classification may not substitute for acceptance.
    /// </summary>
    [Fact]
    public void AP_018_Cluster_classification_is_gated()
    {
        Assert.Contains(Enum.GetNames<ClusterClassificationState>(),
            n => n.Contains("NotCalibrated", StringComparison.Ordinal));
    }

    // ================================================================
    // AP-019 / AP-021 — never fabricate a side, never zero-fill
    // ================================================================

    /// <summary>
    /// AP-019 (KDK Ch 57): an explicit Unknown aggressor side must exist so the
    /// engine can report "not classifiable" instead of inventing Bid/Ask.
    /// </summary>
    [Fact]
    public void AP_019_Unknown_aggressor_side_is_representable()
    {
        Assert.Contains(Enum.GetNames<AggressorSide>(),
            n => n.Equals("Unknown", StringComparison.Ordinal));
        Assert.Contains(Enum.GetNames<AggressorClassificationStatus>(),
            n => n.Contains("Unknown", StringComparison.OrdinalIgnoreCase)
                 || n.Contains("Unavailable", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// AP-021 (KDK Ch 56, v1.3 G-ACC-003): unavailable measurements must be
    /// nullable so they are never zero-filled. Ratio/volume/tick measurements on
    /// evidence and facilitation snapshots are nullable by contract.
    /// </summary>
    [Fact]
    public void AP_021_Unavailable_measurements_are_nullable_not_zero()
    {
        var mustBeNullable = new[]
        {
            typeof(TradeFacilitationSnapshot).GetProperty("DirectionConsistentEffortVolume"),
            typeof(TradeFacilitationSnapshot).GetProperty("DirectionConsistentEffortRatio"),
            typeof(TradeFacilitationSnapshot).GetProperty("AchievedFavorableProgressTicks"),
            typeof(TradeFacilitationSnapshot).GetProperty("FavorableProgressPerDirectionUnit")
        };
        foreach (var p in mustBeNullable)
        {
            Assert.NotNull(p);
            Assert.True(Nullable.GetUnderlyingType(p!.PropertyType) is not null,
                p.Name + " must be nullable so unavailable data is not reported as 0.");
        }
    }

    // ================================================================
    // AP-020 — no single ratio decides a resolution
    // ================================================================

    /// <summary>
    /// AP-020 (KDK Ch 56, v1.3 G-DISC-009): acceptance resolution must be backed by
    /// multiple independent evidence groups, so the evidence snapshot exposes
    /// several distinct outside-ratio measurements rather than one.
    /// </summary>
    [Fact]
    public void AP_020_Multiple_independent_outside_ratios_exist()
    {
        // The snapshot exposes the acceptance vector, which carries the ratios.
        Assert.Contains(typeof(AcceptanceReentryEvidenceSnapshot).GetProperties(),
            p => p.PropertyType == typeof(AcceptanceEvidenceVector));

        var ratios = typeof(AcceptanceEvidenceVector).GetProperties()
            .Where(p => p.Name.Contains("Outside", StringComparison.Ordinal)
                        && p.Name.Contains("Ratio", StringComparison.Ordinal))
            .ToArray();
        Assert.True(ratios.Length >= 3,
            "Expected >= 3 independent outside ratios (time / volume / trade), found " + ratios.Length);

        // AP-021 cross-check: each must be nullable so "unavailable" is not 0.
        Assert.All(ratios, p => Assert.NotNull(Nullable.GetUnderlyingType(p.PropertyType)));
    }

    // ================================================================
    // AP-023 / AP-025 — invalidation is independent of stop
    // ================================================================

    /// <summary>
    /// AP-023 + AP-025 (KDK Ch 59, Ch 60, v1.3 G-INV-003): no thesis or maturity
    /// snapshot may expose a stop-loss field, so invalidation can never be
    /// conflated with, or deferred to, a stop being hit.
    /// </summary>
    [Fact]
    public void AP_023_025_No_stop_loss_surface_in_thesis_or_maturity()
    {
        var types = new[]
        {
            typeof(FarThesisSnapshot), typeof(AacThesisSnapshot),
            typeof(SignalMaturitySnapshot), typeof(SignalMaturitySetSnapshot)
        };
        foreach (var t in types)
        foreach (var p in t.GetProperties())
        {
            Assert.DoesNotContain("Stop", p.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("StopLoss", p.Name, StringComparison.OrdinalIgnoreCase);
        }
        // Terminal invalidation exists as its own state, independent of price.
        Assert.True((int)FarState.Invalidated >= 200);
        Assert.True((int)AacState.Invalidated >= 200);
    }

    // ================================================================
    // AP-026 / AP-027 — POC is not fair value; boundary is not a signal
    // ================================================================

    /// <summary>
    /// AP-026 (KDK Ch 8, v1.2 §13.3): no public surface may call POC "fair value".
    /// </summary>
    [Fact]
    public void AP_026_Poc_is_never_named_fair_value()
    {
        var offenders = AllPublicProperties.Select(p => p.Name)
            .Concat(PublicEnums.SelectMany(Enum.GetNames))
            .Where(n => n.Contains("FairValue", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.Empty(offenders);
    }

    /// <summary>
    /// AP-027 (KDK Ch 19 Rule 2): touching a value-area boundary is a location,
    /// never a direction. PriceValueLocation must stay purely positional.
    /// </summary>
    [Fact]
    public void AP_027_Value_location_carries_no_trade_direction()
    {
        var banned = new[] { "Buy", "Sell", "Long", "Short", "Bullish", "Bearish" };
        foreach (var n in Enum.GetNames<PriceValueLocation>())
            Assert.DoesNotContain(banned, b => n.Contains(b, StringComparison.OrdinalIgnoreCase));
    }

    // ================================================================
    // AP-028 — FAST cannot escalate risk
    // ================================================================

    /// <summary>
    /// AP-028 (KDK Ch 63, v1.2 §29.5, v1.3 G-FAST-005): FAST is shadow-only and
    /// the maturity module owns no sizing surface, so a run of good FAST results
    /// cannot mechanically increase position size.
    /// </summary>
    [Fact]
    public void AP_028_Fast_is_shadow_only_and_carries_no_sizing()
    {
        Assert.True(SignalMaturityPolicyConfig.FastShadowOnlyDefault);
        var banned = new[] { "Size", "Risk", "Quantity", "Lots", "Contracts" };
        foreach (var p in typeof(SignalMaturitySnapshot).GetProperties())
            Assert.DoesNotContain(banned, b => p.Name.Contains(b, StringComparison.OrdinalIgnoreCase));
        Assert.True((int)SignalMaturityLevel.Fast >= 100);
    }

    // ================================================================
    // Cross-cutting registry guards
    // ================================================================

    /// <summary>
    /// v1.3 §14: every calibration-gated VERDICT enum must offer a NotCalibrated
    /// member, otherwise the engine has no truthful way to report "gated" and
    /// would be forced to emit Unknown or a real verdict.
    ///
    /// State-machine enums (FarState, AacState, EpisodeState, …) are exempt:
    /// they carry the gate as a separate NotCalibrated bool on their snapshot,
    /// which <see cref="Registry_State_machine_snapshots_carry_a_NotCalibrated_flag"/>
    /// verifies instead.
    /// </summary>
    [Fact]
    public void Registry_Every_gated_verdict_enum_offers_a_NotCalibrated_member()
    {
        var verdictSuffixes = new[] { "ClassificationState", "ResolutionState", "Conclusion", "Level" };

        var offenders = CalibratedMembers.Select(x => x.Enum).Distinct()
            .Where(e => verdictSuffixes.Any(sfx => e.Name.EndsWith(sfx, StringComparison.Ordinal)))
            .Where(e => !Enum.GetNames(e).Any(n =>
                n.Contains("NotCalibrated", StringComparison.Ordinal)))
            .Select(e => e.FullName ?? e.Name)
            .ToArray();
        Assert.Empty(offenders);
    }

    /// <summary>
    /// Counterpart to the rule above: every thesis/maturity state-machine snapshot
    /// must expose a NotCalibrated flag, so a gated state is always reported as gated
    /// even though the enum itself has no NotCalibrated member.
    /// </summary>
    [Fact]
    public void Registry_State_machine_snapshots_carry_a_NotCalibrated_flag()
    {
        var types = new[]
        {
            typeof(FarThesisSnapshot), typeof(AacThesisSnapshot), typeof(SignalMaturitySnapshot)
        };
        foreach (var t in types)
        {
            var flag = t.GetProperty("NotCalibrated");
            Assert.NotNull(flag);
            Assert.Equal(typeof(bool), flag!.PropertyType);
        }
    }

    /// <summary>
    /// v1.3 §0.3 / D-V13-002a (operator 2026-07-28): GEX/OptionFlow surface is
    /// AUTHORIZED, but ONLY inside <c>GC.AuctionFlow.OptionFlow</c>. It must not
    /// leak into any other module (thesis / episode / GPS / directional / ...),
    /// which would breach the "GEX is never a necessary condition" invariant (§50).
    /// So the ban still holds everywhere except that one namespace.
    /// </summary>
    [Fact]
    public void Registry_No_gex_surface_outside_optionflow()
    {
        const string OptionFlowNs = "GC.AuctionFlow.OptionFlow";
        var banned = new[] { "Gex", "Gamma", "CallResistance", "PutSupport", "Dex", "Options" };

        // ATAS-facing types throw on reflection in the test host, so access defensively
        // (mirrors SafeStaticMethods). Skip the authorized OptionFlow namespace entirely.
        var names = new List<string>();
        foreach (var t in AllTypes)
        {
            string? ns;
            try { ns = t.Namespace; } catch { continue; }
            if (ns == OptionFlowNs) continue;

            try { names.Add(t.Name); } catch { }
            if (t.IsEnum && t.IsPublic)
            {
                try { names.AddRange(Enum.GetNames(t)); } catch { }
                continue;
            }
            if (t.IsPublic)
            {
                try
                {
                    foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                        names.Add(p.Name);
                }
                catch { /* ATAS-facing surface: not our GEX types */ }
            }
        }

        var offenders = names
            .Where(n => banned.Any(b => ContainsToken(n, b)))
            .Distinct()
            .ToArray();
        Assert.Empty(offenders);
    }

    /// <summary>
    /// v1.2 §2.5 / §2.8: score must not masquerade as probability, and the
    /// baseline places no orders. Neither surface may exist.
    /// </summary>
    [Fact]
    public void Registry_No_probability_or_order_placement_surface()
    {
        var banned = new[] { "Probability", "WinRate", "PlaceOrder", "SubmitOrder", "SendOrder" };
        var offenders = AllTypes.Select(t => t.Name)
            .Concat(AllPublicProperties.Select(p => p.Name))
            .Where(n => banned.Any(b => n.Contains(b, StringComparison.OrdinalIgnoreCase)))
            .Distinct()
            .ToArray();
        Assert.Empty(offenders);
    }
}
