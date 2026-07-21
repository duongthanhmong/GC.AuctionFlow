using Aos.LevelEngine.Interactions;
using Aos.LevelEngine.Levels;
using Aos.LevelEngine.Profiles;
using Aos.LevelEngine.Session;
using Xunit;

namespace Aos.LevelEngine.Tests;

internal static class EtUtc
{
    /// <summary>Build TimestampExchange as UTC for a given America/New_York wall clock.</summary>
    public static DateTime Of(int y, int m, int d, int h, int min, int s = 0)
    {
        var tz = ExchangeClock.Resolve("America/New_York");
        var local = new DateTime(y, m, d, h, min, s, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, tz);
    }
}

public class ZoneFreezeTests
{
    [Fact]
    public void Mutating_zone_geometry_after_create_throws()
    {
        var profile = InstrumentProfileLoader.LoadEmbeddedEs();
        var z = ZoneFactory.Create(profile, LevelTypeKind.Pdh, LevelBatch.PRESESSION_SET, 5500m,
            new DateTime(2026, 7, 20, 9, 0, 0), DateTime.UtcNow, "PriorRth");
        Assert.Throws<InvalidOperationException>(() => z.TryMutateGeometry(5499m, 5501m));
    }
}

public class ClusterTests
{
    [Fact]
    public void Overwide_cluster_sets_flag_but_structural_grade_immutable()
    {
        var profile = InstrumentProfileLoader.LoadEmbeddedEs();
        var t = profile.TickSize;
        var zones = new List<FrozenZone>();
        for (var i = 0; i < 6; i++)
        {
            zones.Add(ZoneFactory.Create(profile, LevelTypeKind.Pdh, LevelBatch.PRESESSION_SET,
                5500m + i * 2 * t, new DateTime(2026, 7, 20, 9, 0, 0), DateTime.UtcNow, "PriorRth"));
        }
        var clusters = ClusterBuilder.AssignClusters(zones, profile);
        Assert.True(clusters[0].IsOverwide);
        Assert.All(zones, z => Assert.Equal(StructuralGrade.A, z.StructuralGrade));
        Assert.All(zones, z => Assert.True(z.IsOverwideCluster));
    }
}

public class EffectiveGradeTests
{
    [Theory]
    [InlineData(0, FreshnessState.FRESH, StructuralGrade.A, false)]
    [InlineData(1, FreshnessState.VALID, StructuralGrade.A, false)]
    [InlineData(2, FreshnessState.DEGRADED, StructuralGrade.B, false)]
    [InlineData(3, FreshnessState.EXHAUSTED, null, true)]
    public void EffectiveGrade_is_pure_function_of_structural_and_count(
        int completed, FreshnessState freshness, StructuralGrade? effective, bool inactive)
    {
        var (f, e, i) = FrozenZone.ComputeEffective(StructuralGrade.A, completed, ExhaustedPolicy.INACTIVE);
        Assert.Equal(freshness, f);
        Assert.Equal(effective, e);
        Assert.Equal(inactive, i);
    }

    [Fact]
    public void Vah_and_Poc_same_profile_are_one_VOLUME_PROFILE_family()
    {
        Assert.Equal(IndependentSourceFamily.VOLUME_PROFILE, InstrumentProfile.FamilyFor(LevelTypeKind.Vah));
        Assert.Equal(IndependentSourceFamily.VOLUME_PROFILE, InstrumentProfile.FamilyFor(LevelTypeKind.Poc));
        Assert.Equal(IndependentSourceFamily.VOLUME_PROFILE, InstrumentProfile.FamilyFor(LevelTypeKind.Val));
    }
}

public class FreezeScheduleTests
{
    [Fact]
    public void OPEN_SET_rejects_when_caller_cannot_attest_data_before_930()
    {
        var profile = InstrumentProfileLoader.LoadEmbeddedEs();
        var eng = new LevelSetEngine(profile);
        eng.FreezePreSessionSet(new PriorSessionInputs
        {
            ContractCode = "ESU6",
            SessionDateEt = new DateOnly(2026, 7, 20),
            PriorRthHigh = 5500m,
            PriorRthLow = 5400m
        }, EtUtc.Of(2026, 7, 20, 9, 29, 59), DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            eng.FreezeOpenSet(5520m, 5480m, EtUtc.Of(2026, 7, 20, 9, 30, 0), DateTime.UtcNow,
                callerAttestsDataStrictlyBefore930: false));
    }

    [Fact]
    public void OPEN_SET_accepts_attested_pre_930_data_at_930_event()
    {
        var profile = InstrumentProfileLoader.LoadEmbeddedEs();
        var eng = new LevelSetEngine(profile);
        eng.FreezePreSessionSet(new PriorSessionInputs
        {
            ContractCode = "ESU6",
            SessionDateEt = new DateOnly(2026, 7, 20),
            PriorRthHigh = 5500m
        }, EtUtc.Of(2026, 7, 20, 9, 29, 0), DateTime.UtcNow);

        var snap = eng.FreezeOpenSet(5520m, 5480m, EtUtc.Of(2026, 7, 20, 9, 30, 0), DateTime.UtcNow,
            callerAttestsDataStrictlyBefore930: true);
        Assert.Equal(LevelBatch.OPEN_SET, snap.Batch);
        Assert.Contains(snap.Levels, l => l.LevelType == LevelTypeKind.Onh);
    }

    [Fact]
    public void PRESESSION_SET_rejects_at_or_after_930()
    {
        var profile = InstrumentProfileLoader.LoadEmbeddedEs();
        var eng = new LevelSetEngine(profile);
        Assert.Throws<InvalidOperationException>(() =>
            eng.FreezePreSessionSet(new PriorSessionInputs
            {
                ContractCode = "ESU6",
                SessionDateEt = new DateOnly(2026, 7, 20),
                PriorRthHigh = 5500m
            }, EtUtc.Of(2026, 7, 20, 9, 30, 0), DateTime.UtcNow));
    }
}

public class ReferencePriceTests
{
    [Fact]
    public void ReferencePrice_approach_side_edge_UNKNOWN_is_null()
    {
        var profile = InstrumentProfileLoader.LoadEmbeddedEs();
        var z = ZoneFactory.Create(profile, LevelTypeKind.Val, LevelBatch.PRESESSION_SET, 5400m,
            new DateTime(2026, 7, 20, 9, 0, 0), DateTime.UtcNow, "Prior");
        Assert.Equal(z.ZoneLower, ReferencePriceSelector.Select(ApproachDirection.FROM_BELOW, z));
        Assert.Equal(z.ZoneUpper, ReferencePriceSelector.Select(ApproachDirection.FROM_ABOVE, z));
        Assert.Null(ReferencePriceSelector.Select(ApproachDirection.UNKNOWN, z));
    }
}

public class InteractionResetTests
{
    private static (InstrumentProfile p, InteractionLogger log, FrozenZone z) Setup()
    {
        var p = InstrumentProfileLoader.LoadEmbeddedEs();
        var rot = RotationRCalculator.Freeze(p, Enumerable.Repeat(3m, 11).ToList(),
            new DateTime(2026, 7, 20, 9, 15, 0), false);
        var log = new InteractionLogger(p, rot, "ESU6", Guid.NewGuid(), Guid.NewGuid(), "LIVE",
            LevelSetEngine.EngineVersion);
        var z = ZoneFactory.Create(p, LevelTypeKind.Pdh, LevelBatch.PRESESSION_SET, 5500m,
            new DateTime(2026, 7, 20, 9, 0, 0), DateTime.UtcNow, "Prior");
        return (p, log, z);
    }

    [Fact]
    public void Reset_requires_simultaneous_ticks_and_time_violation_clears_timer()
    {
        var (p, log, z) = Setup();
        var freq = System.Diagnostics.Stopwatch.Frequency;
        var needTicks = log.ResetTicks;
        var far = z.ZoneLower - needTicks * p.TickSize;
        var near = z.ZoneLower - 1 * p.TickSize;

        // Far for 6 minutes worth of mono
        var sixMin = (long)(freq * 60 * 6);
        var (ok1, _) = log.ProbeReset(z, new[]
        {
            (far, 0L),
            (far, sixMin)
        }, freq);
        Assert.True(ok1);

        // Far then violate (come near) → timer cleared → not satisfied even after more time
        var (ok2, elapsed) = log.ProbeReset(z, new[]
        {
            (far, 0L),
            (near, sixMin / 2), // violate → reset timer
            (far, sixMin / 2 + 1),
            (far, sixMin / 2 + freq) // only ~1s after restart — not enough
        }, freq);
        Assert.False(ok2);
        Assert.True(elapsed < sixMin);
    }

    [Fact]
    public void Session_open_inside_zone_ApproachDirection_UNKNOWN()
    {
        var (p, log, z) = Setup();
        log.ArmLevels(new[] { z });
        var t0 = new DateTime(2026, 7, 20, 9, 30, 0);
        log.OnTrade(new[] { z }, z.MidPrice, 1, t0, System.Diagnostics.Stopwatch.GetTimestamp(),
            1, 1, 1, sessionJustOpened: true);
        // Force close via exit
        var exit = z.ZoneUpper + (log.ExitDistanceTicks + 1) * p.TickSize;
        var closed = log.OnTrade(new[] { z }, exit, 1, t0.AddMinutes(1),
            System.Diagnostics.Stopwatch.GetTimestamp(), 2, 1, 1);
        Assert.NotEmpty(closed);
        Assert.Equal(ApproachDirection.UNKNOWN, closed[0].ApproachDirection);
        Assert.Equal("SESSION_OPEN_INSIDE_ZONE", closed[0].StartReason);
    }

    [Fact]
    public void Overlapping_zones_share_ConcurrentInteractionGroupId()
    {
        var p = InstrumentProfileLoader.LoadEmbeddedEs();
        var rot = RotationRCalculator.Freeze(p, Enumerable.Repeat(3m, 11).ToList(),
            new DateTime(2026, 7, 20, 9, 15, 0), false);
        var log = new InteractionLogger(p, rot, "ESU6", Guid.NewGuid(), Guid.NewGuid(), "LIVE",
            LevelSetEngine.EngineVersion);
        // Two identical mids → overlapping zones
        var a = ZoneFactory.Create(p, LevelTypeKind.Pdh, LevelBatch.PRESESSION_SET, 5500m,
            new DateTime(2026, 7, 20, 9, 0, 0), DateTime.UtcNow, "Prior");
        var b = ZoneFactory.Create(p, LevelTypeKind.WeeklyHigh, LevelBatch.PRESESSION_SET, 5500m,
            new DateTime(2026, 7, 20, 9, 0, 0), DateTime.UtcNow, "Weekly");
        ClusterBuilder.AssignClusters(new[] { a, b }, p);
        log.ArmLevels(new[] { a, b });

        // Approach from below then enter
        var below = a.ZoneLower - (log.ResetTicks + 2) * p.TickSize;
        var t0 = new DateTime(2026, 7, 20, 10, 0, 0);
        var freq = System.Diagnostics.Stopwatch.Frequency;
        var mono0 = 0L;
        log.OnTrade(new[] { a, b }, below, 1, t0, mono0, 1, 1, 1);
        // Stay outside long enough — use RESET path: first close isn't needed; start ARMED
        // Direct enter after recording last outside
        log.OnTrade(new[] { a, b }, a.MidPrice, 1, t0.AddMinutes(1), mono0 + freq, 2, 1, 1);

        var exit = a.ZoneUpper + (log.ExitDistanceTicks + 1) * p.TickSize;
        var closed = log.OnTrade(new[] { a, b }, exit, 1, t0.AddMinutes(2), mono0 + 2 * freq, 3, 1, 1);
        Assert.True(closed.Count >= 2);
        Assert.NotNull(closed[0].ConcurrentInteractionGroupId);
        Assert.Equal(closed[0].ConcurrentInteractionGroupId, closed[1].ConcurrentInteractionGroupId);
        Assert.NotEqual(closed[0].LevelId, closed[1].LevelId);
    }
}

public class OutcomeAppendTests
{
    [Fact]
    public void Outcome_append_new_version_never_updates()
    {
        var store = new InteractionOutcomeStore();
        var id = Guid.NewGuid();
        var r1 = store.Append(id, HorizonType.M5, DateTime.UtcNow, DateTime.UtcNow, 100m,
            null, null, null, null, OutcomeStatus.PENDING, null, "0.2.0");
        var r2 = store.Append(id, HorizonType.M5, DateTime.UtcNow, DateTime.UtcNow, 100m,
            3, 1, 0.3, 0.1, OutcomeStatus.COMPLETE, null, "0.2.0");
        Assert.Equal(1, r1.OutcomeVersion);
        Assert.Equal(2, r2.OutcomeVersion);
        Assert.Equal(2, store.All.Count);
        Assert.Equal(OutcomeStatus.PENDING, store.All[0].OutcomeStatus);
        Assert.Equal(OutcomeStatus.COMPLETE, store.All[1].OutcomeStatus);
    }
}

public class ProfileA0Tests
{
    [Fact]
    public void Missing_ValueAreaAlgorithm_fails_startup()
    {
        var json = """
        {
          "SchemaVersion":"0.1.0","ProfileId":"x","Mode":"FULL","AnalysisSymbol":"ES",
          "TickSize":0.25,"Timezone":"America/New_York",
          "TradingSessionIdentity":{
            "TradingSessionRolloverLocalTime":"18:00","TradingTimeZone":"America/New_York",
            "RthStartLocalTime":"09:30","RthEndLocalTime":"16:00","PartialCoverageMaxMissingMinutes":30
          },
          "HistoryCapabilities":{
            "PDH_PDL":{"MinCompletedRthSessions":1,"Required":false},
            "ONH_ONL":{"MinCompletedOvernightSessions":1,"Required":false},
            "WeeklyHL":{"MinCompletedRthSessions":5,"Required":false},
            "Composite":{"MinCompletedRthSessions":5,"Required":false},
            "nPOC":{"MinCompletedRthSessions":5,"Required":false},
            "RotationR":{"RequireChartTimeframeM1":true,"Required":false}
          },
          "Sessions":{"Definitions":[{"Name":"PrimarySession","Start":"09:30","End":"16:00","SessionDefinitionType":"EXCHANGE_SESSION"}]},
          "Rotation":{"VolatilityWindowStart":"06:00","VolatilityWindowEnd":"09:15","TrueRangeMultiplier":3.0,
            "ClampMinTicks":6,"ClampMaxTicks":16,"FreezeTime":"09:15","TrueRangeTimeframe":"M1"},
          "Interaction":{"ResetTicksFloor":8,"ResetTicksUsesMaxWithRotationR":true,"ResetTimeMinutes":5,"MaxInteractionTimeMinutes":30},
          "Zones":{"WidthsAreTotalTicks":true,"ExtremeTicks":2,"ClusterMaxTicks":8,"ClusterGapTicks":2},
          "LevelEngine":{
            "ValueAreaVolumePercent":0.70,
            "PocTieBreak":"LOWEST_PRICE",
            "CompositeLookbackSessions":5,
            "NPocLookbackSessions":5,
            "CompositeBoundaryAlgorithm":"SESSION_HIGH_LOW_EXTREMES_V1",
            "CompositeBoundaryAlgorithmVersion":"1.0.0",
            "ExhaustedPolicy":"INACTIVE",
            "SinglePrintEdgesEnabled":false,
            "SinglePrintStatus":"CAN_XAC_MINH",
            "AllowMidSessionLevelCreation":false
          }
        }
        """;
        var ex = Assert.Throws<InstrumentProfileLoadException>(() =>
            InstrumentProfileLoader.LoadFromJson(json, "test"));
        Assert.Contains("ValueAreaAlgorithm", ex.Message);
        Assert.Contains("FAIL STARTUP", ex.Message);
    }

    [Fact]
    public void OutOfScope_VWAP_rejected()
    {
        var profile = InstrumentProfileLoader.LoadEmbeddedEs();
        var eng = new LevelSetEngine(profile);
        Assert.Throws<InvalidOperationException>(() => eng.RejectOutOfScopeLevel("VWAP"));
    }
}

public class FreshnessCountTests
{
    [Fact]
    public void Completed_interactions_drive_freshness_not_near_miss()
    {
        var profile = InstrumentProfileLoader.LoadEmbeddedEs();
        var eng = new LevelSetEngine(profile);
        eng.FreezePreSessionSet(new PriorSessionInputs
        {
            ContractCode = "ESU6",
            SessionDateEt = new DateOnly(2026, 7, 20),
            PriorRthHigh = 5500m
        }, EtUtc.Of(2026, 7, 20, 9, 0, 0), DateTime.UtcNow);
        var pdh = eng.Levels.First(l => l.LevelType == LevelTypeKind.Pdh);
        Assert.Equal(FreshnessState.FRESH, pdh.FreshnessState);
        pdh.RecordCompletedInteraction();
        Assert.Equal(FreshnessState.VALID, pdh.FreshnessState);
        pdh.RecordCompletedInteraction();
        Assert.Equal(FreshnessState.DEGRADED, pdh.FreshnessState);
        Assert.Equal(StructuralGrade.B, pdh.EffectiveGrade);
    }
}
