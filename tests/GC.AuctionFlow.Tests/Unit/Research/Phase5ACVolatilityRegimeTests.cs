using System.Reflection;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Research;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Research;

/// <summary>
/// Phase 5A-c volatility regime — the second of the three stratification axes
/// `G-CAL-002` step 2 requires.
///
/// Neither v1.2 nor v1.3 defines this axis. Both name it as mandatory and neither says
/// what the buckets are or where their boundaries come from, so the choice was made here
/// and it is the whole point of the module: the DLL collects the raw realized range and
/// applies boundaries decided elsewhere. It never derives them.
///
/// Deriving them from the observed sample was considered and rejected. Boundaries computed
/// that way move every time a period completes, which re-chooses the sample criteria after
/// seeing results (`G-FAST-001`), restratifies past episodes retroactively so no
/// distribution can be reproduced or refuted, and makes step 4's out-of-sample validation
/// impossible because the out-of-sample data would have helped build the bins judging it.
/// </summary>
public sealed class Phase5ACVolatilityRegimeTests
{
    private static CompletedTpoPeriodSnapshot Period(int index, long lowTick, long highTick) =>
        new(
            periodIndex: index,
            periodStartUtc: new DateTime(2026, 7, 27, 0, 0, 0, DateTimeKind.Utc).AddMinutes(30 * index),
            periodEndUtc: new DateTime(2026, 7, 27, 0, 30, 0, DateTimeKind.Utc).AddMinutes(30 * index),
            periodHigh: highTick * 0.1m,
            periodLow: lowTick * 0.1m,
            periodHighTick: highTick,
            periodLowTick: lowTick);

    private static VolatilityRegimeHost Host() => new(enabled: true);

    // ========== A: the DLL never derives a boundary ==========

    /// <summary>
    /// The rule this module exists to respect, asserted structurally.
    ///
    /// Any member that sorts, ranks or averages the sample would be the DLL choosing the
    /// threshold under another name.
    /// </summary>
    [Fact]
    public void A01_Nothing_here_derives_a_boundary_from_the_sample()
    {
        var banned = new[]
        {
            "Percentile", "Quantile", "Quartile", "Tercile", "Median", "Mean", "Average",
            "StdDev", "Derive", "Suggest", "Recommend", "Compute", "Infer",
        };

        foreach (var type in new[]
                 {
                     typeof(VolatilityRegimeHost), typeof(VolatilityRegimeBoundaries),
                     typeof(VolatilityObservation),
                 })
        foreach (var member in type.GetMembers(
                     BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        foreach (var word in banned)
            Assert.False(member.Name.Contains(word, StringComparison.OrdinalIgnoreCase),
                type.Name + "." + member.Name + " derives a boundary from the sample; "
                + "that re-chooses sample criteria after seeing results (G-FAST-001)");
    }

    [Fact]
    public void A02_No_numeric_boundary_constant_exists()
    {
        foreach (var type in new[] { typeof(VolatilityRegimeHost), typeof(VolatilityRegimeBoundaries) })
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.FieldType == typeof(string) || !field.FieldType.IsValueType) continue;

            Assert.True(field.Name == "ObservationCapacity",
                type.Name + "." + field.Name + " is an unexplained numeric constant on an "
                + "axis whose boundaries must come from outside the build");
        }
    }

    // ========== B: registration is the only way in ==========

    [Fact]
    public void B01_Unregistered_boundaries_cannot_stratify()
    {
        Assert.False(VolatilityRegimeBoundaries.Unregistered.CanStratify);
        Assert.Equal(
            VolatilityRegimeBucket.Unavailable,
            VolatilityRegimeBoundaries.Unregistered.Classify(500));
    }

    /// <summary>
    /// A boundary with no provenance is indistinguishable from one somebody typed in,
    /// which is exactly what `G-CAL-001` bans — so it is refused, not accepted quietly.
    /// </summary>
    [Fact]
    public void B02_A_boundary_without_a_decision_log_reference_is_refused()
    {
        Assert.False(VolatilityRegimeBoundaries.Register(10, 30, "").CanStratify);
        Assert.False(VolatilityRegimeBoundaries.Register(10, 30, "   ").CanStratify);
        Assert.True(VolatilityRegimeBoundaries.Register(10, 30, "DL-2026-07-27-001").CanStratify);
    }

    /// <summary>
    /// Refused rather than repaired. Silently swapping a mis-ordered pair would hide a
    /// governance failure while still producing buckets somebody would trust.
    /// </summary>
    [Fact]
    public void B03_A_mis_ordered_or_negative_pair_is_refused()
    {
        Assert.False(VolatilityRegimeBoundaries.Register(30, 10, "DL-1").CanStratify);
        Assert.False(VolatilityRegimeBoundaries.Register(20, 20, "DL-1").CanStratify);
        Assert.False(VolatilityRegimeBoundaries.Register(-5, 30, "DL-1").CanStratify);
    }

    [Fact]
    public void B04_Registered_boundaries_place_observations()
    {
        var boundaries = VolatilityRegimeBoundaries.Register(10, 30, "DL-1");

        Assert.Equal(VolatilityRegimeBucket.Low, boundaries.Classify(9));
        Assert.Equal(VolatilityRegimeBucket.Medium, boundaries.Classify(10));
        Assert.Equal(VolatilityRegimeBucket.Medium, boundaries.Classify(29));
        Assert.Equal(VolatilityRegimeBucket.High, boundaries.Classify(30));
    }

    [Fact]
    public void B05_The_decision_log_reference_travels_with_the_boundaries() =>
        Assert.Equal("DL-2026-07-27-001",
            VolatilityRegimeBoundaries.Register(10, 30, " DL-2026-07-27-001 ").DecisionLogReference);

    // ========== C: raw collection ==========

    [Fact]
    public void C01_Completed_periods_become_realized_ranges()
    {
        var host = Host();
        host.Rebuild(new[] { Period(0, 40_900, 40_940), Period(1, 40_910, 40_925) });

        Assert.Equal(2, host.ObservationsSeen);
        Assert.Equal(40, host.Observations[0].RealizedRangeTicks);
        Assert.Equal(15, host.Observations[1].RealizedRangeTicks);
        Assert.Equal(15, host.MinimumRangeTicks);
        Assert.Equal(40, host.MaximumRangeTicks);
    }

    /// <summary>Re-folding a period would double-count it into every distribution.</summary>
    [Fact]
    public void C02_A_period_is_never_folded_twice()
    {
        var host = Host();
        var periods = new[] { Period(0, 40_900, 40_940), Period(1, 40_910, 40_925) };

        host.Rebuild(periods);
        host.Rebuild(periods);
        host.Rebuild(new[] { periods[1], Period(2, 40_900, 40_960) });

        Assert.Equal(3, host.ObservationsSeen);
    }

    /// <summary>
    /// Collecting is not stratifying. Any amount of raw data leaves the axis unusable until
    /// somebody registers boundaries — which is the honest position, and the one that keeps
    /// the blocking step actionable.
    /// </summary>
    [Fact]
    public void C03_Any_number_of_observations_alone_cannot_stratify()
    {
        var host = Host();
        for (var i = 0; i < 1000; i++)
            host.Rebuild(new[] { Period(i, 40_900, 40_900 + i) });

        Assert.Equal(1000, host.ObservationsSeen);
        Assert.False(host.CanStratify);
        Assert.Empty(host.CountsByBucket());
    }

    [Fact]
    public void C04_Registered_boundaries_plus_observations_can_stratify()
    {
        var host = Host();
        host.Rebuild(new[]
        {
            Period(0, 40_900, 40_905),   // 5  -> Low
            Period(1, 40_900, 40_920),   // 20 -> Medium
            Period(2, 40_900, 40_940),   // 40 -> High
        });
        host.RegisterBoundaries(VolatilityRegimeBoundaries.Register(10, 30, "DL-1"));

        Assert.True(host.CanStratify);
        var counts = host.CountsByBucket();
        Assert.Equal(1, counts[VolatilityRegimeBucket.Low]);
        Assert.Equal(1, counts[VolatilityRegimeBucket.Medium]);
        Assert.Equal(1, counts[VolatilityRegimeBucket.High]);
    }

    /// <summary>
    /// Boundaries with no observations stratify nothing, so the axis is not usable and must
    /// not claim to be.
    /// </summary>
    [Fact]
    public void C05_Boundaries_without_observations_cannot_stratify()
    {
        var host = Host();
        host.RegisterBoundaries(VolatilityRegimeBoundaries.Register(10, 30, "DL-1"));
        Assert.False(host.CanStratify);
    }

    [Fact]
    public void C06_Disabled_host_collects_nothing()
    {
        var host = new VolatilityRegimeHost(enabled: false);
        host.Rebuild(new[] { Period(0, 40_900, 40_940) });
        Assert.Equal(0, host.ObservationsSeen);
    }

    [Fact]
    public void C07_Retention_does_not_rewrite_the_observed_count()
    {
        var host = Host();
        for (var i = 0; i < VolatilityRegimeHost.ObservationCapacity + 25; i++)
            host.Rebuild(new[] { Period(i, 40_900, 40_920) });

        Assert.Equal(VolatilityRegimeHost.ObservationCapacity + 25, host.ObservationsSeen);
        Assert.Equal(VolatilityRegimeHost.ObservationCapacity, host.Observations.Count);
        Assert.Equal(25, host.ObservationsDroppedToCapacity);
    }

    // ========== D: the protocol reports the axis accurately ==========

    /// <summary>
    /// The axis moves from NotImplemented to ImplementedButNotCalibrated. The protocol is
    /// still blocked at step 2 — but now on a decision somebody can make, rather than on a
    /// classifier that does not exist.
    /// </summary>
    [Fact]
    public void D01_An_unregistered_axis_reports_implemented_but_not_calibrated()
    {
        var axis = CalibrationProtocol.Evaluate(hasCollectedRows: true)
            .StratificationAxes.Single(a => a.Axis == StratificationAxis.VolatilityRegime);

        Assert.Equal(StratificationAxisAvailability.ImplementedButNotCalibrated, axis.Availability);
        Assert.Contains("no boundaries are registered", axis.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void D02_A_registered_axis_becomes_available()
    {
        var protocol = CalibrationProtocol.Evaluate(
            hasCollectedRows: true, volatilityRegimeCanStratify: true);

        var axis = protocol.StratificationAxes
            .Single(a => a.Axis == StratificationAxis.VolatilityRegime);

        Assert.Equal(StratificationAxisAvailability.Available, axis.Availability);

        // Participation is still gated, so the protocol has not moved. Registering one axis
        // must not read as progress past step two.
        Assert.Equal(CalibrationProtocolStep.StratifiedDistribution, protocol.BlockedAt);
        Assert.Equal(
            new[] { StratificationAxis.ParticipationRegime }, protocol.MissingAxes);
    }

    /// <summary>Registering boundaries never unlocks anything on its own.</summary>
    [Fact]
    public void D03_A_registered_axis_does_not_permit_an_unlock() =>
        Assert.False(CalibrationProtocol
            .Evaluate(hasCollectedRows: true, volatilityRegimeCanStratify: true).UnlockPermitted);
}
