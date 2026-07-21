using Aos.LevelEngine.History;
using Aos.LevelEngine.Identity;
using Aos.LevelEngine.Profiles;
using Aos.LevelEngine.Session;
using Xunit;

namespace Aos.LevelEngine.Tests;

public class Prompt5ATests
{
    private static readonly TimeZoneInfo Et = ExchangeClock.Resolve("America/New_York");

    private static TradingDateResolver Resolver(int partialMissing = 30) =>
        new(Et, new TimeOnly(18, 0), new TimeOnly(9, 30), new TimeOnly(16, 0), partialMissing);

    private static List<DateTime> CompleteRthBars(int y, int m, int d)
    {
        var list = new List<DateTime>();
        for (var t = new TimeOnly(9, 30); t < new TimeOnly(16, 0); t = t.AddMinutes(1))
            list.Add(EtUtc.Of(y, m, d, t.Hour, t.Minute));
        return list;
    }

    private static List<DateTime> PartialRthBars(int y, int m, int d, int count)
    {
        var list = new List<DateTime>();
        var t = new TimeOnly(9, 30);
        for (var i = 0; i < count; i++)
        {
            list.Add(EtUtc.Of(y, m, d, t.Hour, t.Minute));
            t = t.AddMinutes(1);
        }
        return list;
    }

    // —— T1–T5 TradingDate ——

    [Fact]
    public void T1_1759_ET_TradingDate_is_today()
    {
        var r = Resolver();
        Assert.Equal(new DateOnly(2026, 7, 20), r.ResolveTradingDate(EtUtc.Of(2026, 7, 20, 17, 59)));
    }

    [Fact]
    public void T2_1800_ET_TradingDate_is_tomorrow()
    {
        var r = Resolver();
        Assert.Equal(new DateOnly(2026, 7, 21), r.ResolveTradingDate(EtUtc.Of(2026, 7, 20, 18, 0)));
    }

    [Fact]
    public void T3_Monday_2252_ET_TradingDate_is_Tuesday()
    {
        var r = Resolver();
        var t = DateTime.SpecifyKind(new DateTime(2026, 7, 21, 2, 52, 0), DateTimeKind.Utc);
        Assert.Equal(new DateOnly(2026, 7, 21), r.ResolveTradingDate(t));
    }

    [Fact]
    public void T4_Prior_of_Monday_is_Friday_via_completed_sessions()
    {
        var r = Resolver();
        var monday = new DateOnly(2026, 7, 20);
        var friday = new DateOnly(2026, 7, 17);
        Assert.Equal(friday, r.PriorTradingDate(monday, new[] { friday, monday }));
    }

    [Fact]
    public void T5_DST_winter_and_summer_same_local_hour_different_utc_offset()
    {
        var r = Resolver();
        var summer = EtUtc.Of(2026, 7, 20, 17, 59); // EDT UTC-4
        var winter = EtUtc.Of(2026, 1, 15, 17, 59); // EST UTC-5
        Assert.Equal(TimeSpan.FromHours(-4), Et.GetUtcOffset(summer));
        Assert.Equal(TimeSpan.FromHours(-5), Et.GetUtcOffset(winter));
        Assert.NotEqual(summer.Hour, winter.Hour); // UTC hours differ
        Assert.Equal(new DateOnly(2026, 7, 20), r.ResolveTradingDate(summer));
        Assert.Equal(new DateOnly(2026, 1, 15), r.ResolveTradingDate(winter));
    }

    // —— T6–T8 Coverage ——

    [Fact]
    public void T6_Gap_between_two_COMPLETE_is_NON_TRADING_DAY()
    {
        var r = Resolver();
        var bars = new List<DateTime>();
        bars.AddRange(CompleteRthBars(2026, 7, 17)); // Fri
        bars.AddRange(CompleteRthBars(2026, 7, 20)); // Mon
        var cov = new SessionCoverageAnalyzer(r).Analyze(bars);
        Assert.Equal(SessionCoverageState.NON_TRADING_DAY, cov.ByTradingDate[new DateOnly(2026, 7, 18)]);
        Assert.Equal(SessionCoverageState.NON_TRADING_DAY, cov.ByTradingDate[new DateOnly(2026, 7, 19)]);
    }

    [Fact]
    public void T7_Before_EarliestUsable_is_UNKNOWN_not_NON_TRADING()
    {
        var r = Resolver();
        var bars = new List<DateTime>
        {
            // Pre-edge bar: Thu 17:00 ET → TradingDate Thu, no RTH → UNKNOWN before earliest COMPLETE
            EtUtc.Of(2026, 7, 16, 17, 0)
        };
        bars.AddRange(CompleteRthBars(2026, 7, 17)); // Fri = earliest usable
        bars.AddRange(CompleteRthBars(2026, 7, 20));
        var cov = new SessionCoverageAnalyzer(r).Analyze(bars);
        Assert.Equal(new DateOnly(2026, 7, 17), cov.EarliestUsableTradingDate);
        Assert.Equal(SessionCoverageState.UNKNOWN_COVERAGE, cov.ByTradingDate[new DateOnly(2026, 7, 16)]);
        Assert.NotEqual(SessionCoverageState.NON_TRADING_DAY, cov.ByTradingDate[new DateOnly(2026, 7, 16)]);
    }

    [Fact]
    public void T8_Partial_RTH_excluded_from_completed_Weekly_Composite()
    {
        var r = Resolver();
        var bars = new List<DateTime>();
        bars.AddRange(CompleteRthBars(2026, 7, 17));
        bars.AddRange(PartialRthBars(2026, 7, 20, 40)); // abnormal short → PARTIAL
        var cov = new SessionCoverageAnalyzer(r).Analyze(bars);
        Assert.Equal(SessionCoverageState.PARTIAL_COVERAGE, cov.ByTradingDate[new DateOnly(2026, 7, 20)]);
        Assert.DoesNotContain(new DateOnly(2026, 7, 20), cov.CompletedRthSessions);
        Assert.False(SessionCoverageAnalyzer.IsUsableForMultiSession(SessionCoverageState.PARTIAL_COVERAGE));
    }

    // —— T9 Alignment ——

    [Fact]
    public void T9_FixedProfile_mismatch_MISALIGNED_no_level_creation()
    {
        var r = Resolver();
        var expectedPrior = new DateOnly(2026, 7, 17);
        var scaledUtc = DateTime.SpecifyKind(new DateTime(2026, 7, 19, 22, 0, 0), DateTimeKind.Utc);
        var g = PeriodIdentityGuard.Evaluate("FixedProfile.LastDay", expectedPrior, scaledUtc, r);
        Assert.Equal(AlignmentStatus.MISALIGNED, g.Alignment.AlignmentStatus);
        Assert.True(g.EmitDataQualityEvent);
        Assert.False(g.AllowLevelCreation);
        Assert.Equal(PeriodIdentityGuard.SessionDefinitionMismatch, "SESSION_DEFINITION_MISMATCH");
    }

    // —— T10–T12 Contract ——

    [Fact]
    public void T10_LIVE_Security_null_FAIL_CLOSED()
    {
        var ex = Assert.Throws<ContractIdentityResolveException>(() =>
            ContractIdentityResolver.Resolve(DeclaredDataSourceMode.LIVE, new ContractIdentityInputs
            {
                SecurityCode = null,
                InstrumentInfoInstrument = "ES"
            }));
        Assert.Contains("FAIL-CLOSED", ex.Message);
    }

    [Fact]
    public void T11_CHART_SYMBOL_FALLBACK_root_ES_UNAVAILABLE()
    {
        Assert.False(ContractIdentityResolver.LooksLikeSpecificContract("ES"));
        var ex = Assert.Throws<ContractIdentityResolveException>(() =>
            ContractIdentityResolver.Resolve(DeclaredDataSourceMode.LIVE, new ContractIdentityInputs
            {
                SecurityCode = null,
                ChartSymbolFallback = "ES",
                InstrumentInfoInstrument = "ES"
            }));
        Assert.Contains("FAIL-CLOSED", ex.Message);
    }

    [Fact]
    public void T12_Contract_mutation_closes_session()
    {
        var guard = new SessionContractGuard();
        Assert.True(guard.Observe("ESU6").AllowContinue);
        var mut = guard.Observe("ESZ6");
        Assert.True(mut.EmitMutationEvent);
        Assert.True(mut.CloseSession);
        Assert.False(mut.AllowContinue);
        Assert.True(guard.IsSessionClosed);
        Assert.Equal(SessionContractGuard.MutationEventCode, "SESSION_CONTRACT_MUTATION");
    }

    // —— T13 Profile FAIL STARTUP ——

    [Fact]
    public void T13_Missing_TradingSessionRolloverLocalTime_FAIL_STARTUP()
    {
        var json = MinimalProfileJson(rollover: null);
        var ex = Assert.Throws<InstrumentProfileLoadException>(() =>
            InstrumentProfileLoader.LoadFromJson(json, "t13"));
        Assert.Contains("FAIL STARTUP", ex.Message);
        Assert.Contains("TradingSessionRolloverLocalTime", ex.Message);
    }

    // —— T14 Optional Composite insufficient ——

    [Fact]
    public void T14_Composite_insufficient_other_levels_still_ok()
    {
        var profile = InstrumentProfileLoader.LoadEmbeddedEs();
        var resolver = TradingDateResolver.FromProfile(profile);
        var bars = new List<DateTime>();
        // 3 COMPLETE RTH sessions only
        bars.AddRange(CompleteRthBars(2026, 7, 15));
        bars.AddRange(CompleteRthBars(2026, 7, 16));
        bars.AddRange(CompleteRthBars(2026, 7, 17));
        var cov = new SessionCoverageAnalyzer(resolver).Analyze(bars);
        Assert.Equal(3, cov.CompletedRthSessions.Count);

        var contract = new ContractIdentity
        {
            ContractCode = "ESU6",
            Source = ContractIdentitySource.SECURITY_CODE,
            Confidence = ContractIdentityConfidence.HIGH
        };
        var resolved = new DateOnly(2026, 7, 17);
        var prior = resolver.PriorTradingDate(resolved, cov.CompletedRthSessions);
        var manifest = new HistoryCapabilityEvaluator(profile, resolver).Build(
            cov, bars, contract, resolved, prior, chartTimeFrame: "M1");

        Assert.Equal(HistoryCapabilityStatus.INSUFFICIENT, manifest.Capabilities["Composite"].Status);
        Assert.Equal(LevelOmissionReasonCode.INSUFFICIENT_HISTORY, manifest.Capabilities["Composite"].OmissionReason);
        Assert.False(manifest.Capabilities["Composite"].FailClosedEngine); // optional
        Assert.Equal(HistoryCapabilityStatus.SUFFICIENT, manifest.Capabilities["PDH_PDL"].Status);
        Assert.DoesNotContain(manifest.Capabilities.Values, c => c.FailClosedEngine);
    }

    private static string MinimalProfileJson(string? rollover)
    {
        var rolloverLine = rollover is null
            ? ""
            : $"\"TradingSessionRolloverLocalTime\": \"{rollover}\",";
        return $$"""
        {
          "SchemaVersion":"0.1.0","ProfileId":"t","Mode":"FULL","AnalysisSymbol":"ES",
          "TickSize":0.25,"Timezone":"America/New_York",
          "TradingSessionIdentity": {
            {{rolloverLine}}
            "TradingTimeZone":"America/New_York",
            "RthStartLocalTime":"09:30","RthEndLocalTime":"16:00",
            "PartialCoverageMaxMissingMinutes":30
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
          "Interaction":{"ResetTicksFloor":8,"ResetTicksUsesMaxWithRotationR":true,"ResetTimeMinutes":5},
          "Zones":{"WidthsAreTotalTicks":true,"ExtremeTicks":2,"ClusterMaxTicks":8,"ClusterGapTicks":2},
          "LevelEngine":{
            "ValueAreaVolumePercent":0.70,"ValueAreaAlgorithm":"EXPAND_FROM_POC_V1","PocTieBreak":"LOWEST_PRICE",
            "CompositeLookbackSessions":5,"NPocLookbackSessions":5,
            "CompositeBoundaryAlgorithm":"SESSION_HIGH_LOW_EXTREMES_V1","CompositeBoundaryAlgorithmVersion":"1.0.0",
            "ExhaustedPolicy":"INACTIVE","SinglePrintEdgesEnabled":false,"SinglePrintStatus":"x",
            "AllowMidSessionLevelCreation":false
          }
        }
        """;
    }
}
