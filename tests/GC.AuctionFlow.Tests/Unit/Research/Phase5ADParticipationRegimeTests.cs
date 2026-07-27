using System.Reflection;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Research;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Research;

/// <summary>
/// Phase 5A-d participation regime — the last of the three stratification axes
/// `G-CAL-002` step 2 requires.
///
/// v1.2 §10.4 does specify this one, and the specification carries a constraint that
/// shapes the module: **volume percentile by clock bucket**. An overnight period and a
/// cash-session period are not comparable on volume, so a single global boundary set would
/// produce a distribution whose shape is mostly the session calendar. Registration is
/// therefore per clock bucket, and the axis is unusable until every observed bucket has
/// one.
///
/// Boundaries are registered rather than derived, for the reasons set out on the
/// volatility axis: derived bins re-choose sample criteria after seeing results
/// (`G-FAST-001`), restratify retroactively, and break step 4's out-of-sample validation.
/// The existing production limitation already said as much —
/// `THIN_PARTICIPATION_PERCENTILE_THRESHOLDS_NOT_CALIBRATED`.
/// </summary>
public sealed class Phase5ADParticipationRegimeTests
{
    private static CompletedTpoPeriodSnapshot Period(
        int index, long lowTick = 40_900, long highTick = 40_920) =>
        new(
            periodIndex: index,
            periodStartUtc: new DateTime(2026, 7, 27, 0, 0, 0, DateTimeKind.Utc).AddMinutes(30 * index),
            periodEndUtc: new DateTime(2026, 7, 27, 0, 30, 0, DateTimeKind.Utc).AddMinutes(30 * index),
            periodHigh: highTick * 0.1m,
            periodLow: lowTick * 0.1m,
            periodHighTick: highTick,
            periodLowTick: lowTick);

    private static VolumeProfileSnapshot Volume(params (long Tick, decimal Volume)[] levels) =>
        new(
            auctionId: "PI-2026-07-26",
            profileHigh: 4095m,
            profileLow: 4085m,
            volumePoc: 4090m,
            volumeVah: 4092m,
            volumeVal: 4088m,
            totalExecutedVolume: levels.Sum(l => l.Volume),
            priceLevelVolumes: levels.ToDictionary(l => l.Tick, l => l.Volume),
            priceVolumeCapability: PriceVolumeCapability.Unavailable,
            dataQuality: ProfileDataQuality.Complete,
            provenance: "test",
            knownLimitations: Array.Empty<string>());

    private static ParticipationRegimeHost Host() => new(enabled: true);

    private static ParticipationRegimeBoundaries Registered(int clockBucket) =>
        ParticipationRegimeBoundaries.Unregistered
            .Register(clockBucket, thinBelow: 100m, reducedBelow: 300m,
                dislocatedAtOrAbove: 1000m, decisionLogReference: "DL-1");

    // ========== A: the DLL never derives a boundary ==========

    [Fact]
    public void A01_Nothing_here_derives_a_boundary_from_the_sample()
    {
        var banned = new[]
        {
            "Percentile", "Quantile", "Quartile", "Median", "Mean", "Average",
            "StdDev", "Derive", "Suggest", "Recommend", "Infer",
        };

        foreach (var type in new[]
                 {
                     typeof(ParticipationRegimeHost), typeof(ParticipationRegimeBoundaries),
                     typeof(ParticipationObservation),
                 })
        foreach (var member in type.GetMembers(
                     BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        foreach (var word in banned)
            Assert.False(member.Name.Contains(word, StringComparison.OrdinalIgnoreCase),
                type.Name + "." + member.Name + " derives a boundary from the sample");
    }

    [Fact]
    public void A02_The_four_regimes_of_the_spec_are_declared()
    {
        foreach (var name in new[] { "Normal", "Reduced", "Thin", "Dislocated" })
            Assert.Contains(name, Enum.GetNames<ParticipationRegimeBucket>());
    }

    // ========== B: per-clock-bucket registration ==========

    [Fact]
    public void B01_Unregistered_boundaries_place_nothing()
    {
        var observation = new ParticipationObservation(
            0, 0, DateTime.UtcNow, 50m, null, 20, TimeSpan.FromMinutes(30));

        Assert.Equal(
            ParticipationRegimeBucket.Unavailable,
            ParticipationRegimeBoundaries.Unregistered.Classify(observation));
    }

    /// <summary>
    /// The §10.4 constraint, asserted. Falling back to another bucket's boundaries would
    /// judge an overnight period against cash-session volume.
    /// </summary>
    [Fact]
    public void B02_An_unregistered_clock_bucket_never_borrows_another_ones_boundaries()
    {
        var boundaries = Registered(clockBucket: 5);

        var registered = new ParticipationObservation(
            0, 5, DateTime.UtcNow, 50m, null, 20, TimeSpan.FromMinutes(30));
        var elsewhere = new ParticipationObservation(
            1, 9, DateTime.UtcNow, 50m, null, 20, TimeSpan.FromMinutes(30));

        Assert.Equal(ParticipationRegimeBucket.Thin, boundaries.Classify(registered));
        Assert.Equal(ParticipationRegimeBucket.Unavailable, boundaries.Classify(elsewhere));
    }

    [Fact]
    public void B03_A_registration_without_a_decision_log_reference_is_refused() =>
        Assert.False(ParticipationRegimeBoundaries.Unregistered
            .Register(0, 100m, 300m, 1000m, "").IsRegistered(0));

    /// <summary>Refused rather than reordered — see the volatility axis.</summary>
    [Fact]
    public void B04_A_mis_ordered_triple_is_refused()
    {
        var b = ParticipationRegimeBoundaries.Unregistered;
        Assert.False(b.Register(0, 300m, 100m, 1000m, "DL-1").IsRegistered(0));
        Assert.False(b.Register(0, 100m, 1000m, 300m, "DL-1").IsRegistered(0));
        Assert.False(b.Register(0, 0m, 300m, 1000m, "DL-1").IsRegistered(0));
    }

    /// <summary>
    /// §10.4 keeps Dislocated as its own regime rather than the top of Normal: an
    /// event-driven period is not a busy normal one.
    /// </summary>
    [Fact]
    public void B05_Every_regime_is_reachable()
    {
        var boundaries = Registered(clockBucket: 0);

        ParticipationRegimeBucket Place(decimal volume) => boundaries.Classify(
            new ParticipationObservation(0, 0, DateTime.UtcNow, volume, null, 20, TimeSpan.FromMinutes(30)));

        Assert.Equal(ParticipationRegimeBucket.Thin, Place(99m));
        Assert.Equal(ParticipationRegimeBucket.Reduced, Place(100m));
        Assert.Equal(ParticipationRegimeBucket.Normal, Place(300m));
        Assert.Equal(ParticipationRegimeBucket.Dislocated, Place(1000m));
    }

    // ========== C: raw collection ==========

    [Fact]
    public void C01_Volume_is_summed_only_within_the_periods_price_range()
    {
        var host = Host();
        host.Rebuild(
            new[] { Period(0, 40_900, 40_920) },
            Volume((40_890, 500m), (40_905, 200m), (40_915, 150m), (40_930, 900m)));

        Assert.Equal(350m, host.Observations[0].ExecutedVolume);
    }

    /// <summary>
    /// The volume profile carries no per-level trade count, so this feature of §10.4 is
    /// simply not observable here. Zero would make every period look like it had no trades.
    /// </summary>
    [Fact]
    public void C02_Trade_count_is_null_not_zero()
    {
        var host = Host();
        host.Rebuild(new[] { Period(0) }, Volume((40_905, 200m)));
        Assert.Null(host.Observations[0].TradeCount);
    }

    /// <summary>Dividing by a zero range would report infinite density for a period that stood still.</summary>
    [Fact]
    public void C03_Density_is_null_when_the_period_did_not_move()
    {
        var host = Host();
        host.Rebuild(
            new[] { Period(0, 40_900, 40_900), Period(1, 40_900, 40_920) },
            Volume((40_900, 200m), (40_910, 200m)));

        Assert.Null(host.Observations[0].ExecutedVolumeDensity);
        Assert.NotNull(host.Observations[1].ExecutedVolumeDensity);
    }

    [Fact]
    public void C04_A_period_is_never_folded_twice()
    {
        var host = Host();
        var periods = new[] { Period(0), Period(1) };
        var volume = Volume((40_905, 200m));

        host.Rebuild(periods, volume);
        host.Rebuild(periods, volume);
        host.Rebuild(new[] { periods[1], Period(2) }, volume);

        Assert.Equal(3, host.ObservationsSeen);
    }

    /// <summary>
    /// The actionable output. An axis that only says "not calibrated" leaves nobody knowing
    /// what to do; this names exactly which slots of the auction clock still need one.
    /// </summary>
    [Fact]
    public void C05_Unregistered_clock_buckets_are_named()
    {
        var host = Host();
        host.Rebuild(new[] { Period(0), Period(1), Period(2) }, Volume((40_905, 200m)));
        host.RegisterBoundaries(Registered(clockBucket: 1));

        Assert.Equal(new[] { 0, 2 }, host.UnregisteredClockBuckets);
    }

    /// <summary>
    /// Partial registration stratifies a silently self-selected subset, which is worse than
    /// no distribution because it looks like one.
    /// </summary>
    [Fact]
    public void C06_Partial_registration_cannot_stratify()
    {
        var host = Host();
        host.Rebuild(new[] { Period(0), Period(1) }, Volume((40_905, 200m)));
        host.RegisterBoundaries(Registered(clockBucket: 0));

        Assert.False(host.CanStratify);
        Assert.Empty(host.CountsByBucket());
    }

    [Fact]
    public void C07_Full_registration_can_stratify()
    {
        var host = Host();
        host.Rebuild(new[] { Period(0), Period(1) }, Volume((40_905, 200m)));
        host.RegisterBoundaries(
            Registered(clockBucket: 0).Register(1, 100m, 300m, 1000m, "DL-1"));

        Assert.True(host.CanStratify);
        Assert.NotEmpty(host.CountsByBucket());
    }

    [Fact]
    public void C08_Boundaries_without_observations_cannot_stratify()
    {
        var host = Host();
        host.RegisterBoundaries(Registered(clockBucket: 0));
        Assert.False(host.CanStratify);
    }

    [Fact]
    public void C09_Retention_does_not_rewrite_the_observed_count()
    {
        var host = Host();
        var volume = Volume((40_905, 200m));
        for (var i = 0; i < ParticipationRegimeHost.ObservationCapacity + 12; i++)
            host.Rebuild(new[] { Period(i) }, volume);

        Assert.Equal(ParticipationRegimeHost.ObservationCapacity + 12, host.ObservationsSeen);
        Assert.Equal(12, host.ObservationsDroppedToCapacity);
    }

    [Fact]
    public void C10_Disabled_host_collects_nothing()
    {
        var host = new ParticipationRegimeHost(enabled: false);
        host.Rebuild(new[] { Period(0) }, Volume((40_905, 200m)));
        Assert.Equal(0, host.ObservationsSeen);
    }

    // ========== D: the protocol, and the gate that stays shut ==========

    [Fact]
    public void D01_An_unregistered_axis_reports_implemented_but_not_calibrated()
    {
        var axis = CalibrationProtocol.Evaluate(hasCollectedRows: true)
            .StratificationAxes.Single(a => a.Axis == StratificationAxis.ParticipationRegime);

        Assert.Equal(StratificationAxisAvailability.ImplementedButNotCalibrated, axis.Availability);
        Assert.Contains("clock bucket", axis.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// With all three axes registered the protocol finally leaves step two — and stops at
    /// step three, which is where it should stop. Sample criteria are pre-registered by a
    /// research process, and choosing them here after seeing rows is what `G-FAST-001`
    /// forbids.
    /// </summary>
    [Fact]
    public void D02_All_three_axes_move_the_block_to_sample_criteria()
    {
        var protocol = CalibrationProtocol.Evaluate(
            hasCollectedRows: true,
            volatilityRegimeCanStratify: true,
            participationRegimeCanStratify: true);

        Assert.Empty(protocol.MissingAxes);
        Assert.Equal(CalibrationProtocolStep.PreRegisteredSampleCriteria, protocol.BlockedAt);
        Assert.Equal(2, protocol.StepsSatisfied);
    }

    /// <summary>
    /// Even with every axis available the gate stays shut. Steps four through six happen
    /// outside this build, so no configuration of it can permit an unlock.
    /// </summary>
    [Fact]
    public void D03_No_combination_of_inputs_permits_an_unlock()
    {
        foreach (var rows in new[] { false, true })
        foreach (var volatility in new[] { false, true })
        foreach (var participation in new[] { false, true })
            Assert.False(CalibrationProtocol
                .Evaluate(rows, volatility, participation).UnlockPermitted);
    }
}
