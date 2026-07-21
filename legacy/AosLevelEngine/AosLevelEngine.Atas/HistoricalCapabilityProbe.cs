using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using ATAS.Indicators;
using Aos.LevelEngine.History;
using Aos.LevelEngine.Identity;
using Aos.LevelEngine.Profiles;
using Aos.LevelEngine.Session;

namespace Aos.LevelEngine.Atas;

public sealed class HistoricalProbeHost
{
    public required int CurrentBar { get; init; }
    public int? TotalBars { get; init; }
    public required Func<int, IndicatorCandle?> GetCandle { get; init; }
    public string? InstrumentInfoInstrument { get; init; }
    public string? IndicatorInstrument { get; init; }
    public string? Exchange { get; init; }
    public decimal? TickSize { get; init; }
    public string? ChartTimeFrame { get; set; }
    public string? ChartType { get; set; }
    public List<ContractCodeCandidate> ContractCandidates { get; init; } = new();
    public FixedProfileRuntimeSection? FixedProfileRuntime { get; set; }
    public DeclaredDataSourceMode DeclaredMode { get; init; } = DeclaredDataSourceMode.LIVE;
    public string? SecurityCode { get; init; }
    public string? SecurityId { get; init; }
    public DateTime? SecurityExpiration { get; init; }
}

/// <summary>
/// Historical capability probe — Prompt 5A session identity + provenance (no level creation).
/// </summary>
public static class HistoricalCapabilityProbe
{
    public const string ProbeVersion = "0.4.0";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public static HistoricalCapabilityReport Run(HistoricalProbeHost host, InstrumentProfile profile)
    {
        var report = new HistoricalCapabilityReport
        {
            GeneratedUtc = DateTime.UtcNow,
            ProbeVersion = ProbeVersion,
            SchemaVersion = "1.3.0",
            InstrumentInfoInstrument = host.InstrumentInfoInstrument,
            IndicatorInstrumentProperty = host.IndicatorInstrument,
            Exchange = host.Exchange,
            TickSize = host.TickSize,
            ChartTimeFrame = host.ChartTimeFrame,
            ChartType = host.ChartType
        };

        FillContractCandidates(host, report);
        ResolveContractIdentity(host, report);

        var resolver = TradingDateResolver.FromProfile(profile);
        report.SessionResolution.TradingDateResolverVersion = TradingDateResolver.TradingDateResolverVersion;
        report.SessionResolution.SessionRolloverHourEt = resolver.TradingSessionRolloverLocalTime.Hour;

        var bars = ScanBars(host, report);
        var barTimes = bars.Select(b => b.TimeUtc).ToList();
        var coverage = new SessionCoverageAnalyzer(resolver).Analyze(barTimes);

        DateOnly? resolved = null;
        DateOnly? prior = null;
        if (barTimes.Count > 0)
        {
            resolved = resolver.ResolveTradingDate(barTimes.Max());
            prior = resolver.PriorTradingDate(resolved.Value, coverage.CompletedRthSessions);
            report.SessionResolution.ResolvedTradingDate = resolver.Format(resolved);
            report.SessionResolution.PriorTradingDate = resolver.Format(prior);
            report.SessionResolution.ExpectedLastDayTradingDate = resolver.Format(prior);
            report.SessionResolution.EarliestUsableTradingDate = resolver.Format(coverage.EarliestUsableTradingDate);
            report.SessionResolution.Notes =
                $"COMPLETE RTH={coverage.CompletedRthSessions.Count}; map={coverage.ByTradingDate.Count}. " +
                "PARTIAL/UNKNOWN excluded from Weekly/Composite/nPOC.";
        }

        AssessFromCoverage(profile, resolver, coverage, bars, resolved, prior, report);
        FillVolumeProfileApi(report);
        FillSecondarySeries(report);
        FillOutOfChartSection(report);

        var periodAlignments = new List<PeriodIdentityAlignment>();
        if (host.FixedProfileRuntime is not null)
        {
            report.FixedProfileRuntime = host.FixedProfileRuntime;
            ApplyFixedProfileGuard(resolver, prior, report, periodAlignments);
        }
        else
        {
            report.SessionResolution.SessionAlignment = nameof(AlignmentStatus.NOT_APPLICABLE);
        }

        if (resolved is not null)
        {
            ContractIdentity? contract = TryReadContract(report);
            var manifest = new HistoryCapabilityEvaluator(profile, resolver).Build(
                coverage, barTimes, contract, resolved.Value, prior, host.ChartTimeFrame, periodAlignments);
            report.HistoryCapabilityManifest = manifest;
            report.SessionResolution.HistoryCapabilityStatus = manifest.HistoryCapabilityStatus.ToString();
        }

        report.Notes.Add(
            $"Prompt 5A only. TradingDateResolverVersion={TradingDateResolver.TradingDateResolverVersion}.");
        return report;
    }

    public static string WriteReport(HistoricalCapabilityReport report, string? directory = null, bool alsoTimestamped = true)
    {
        directory ??= Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aos");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "HistoricalCapability.json");
        var json = JsonSerializer.Serialize(report, JsonOpts);
        File.WriteAllText(path, json);
        if (alsoTimestamped)
        {
            var stamp = report.GeneratedUtc.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            File.WriteAllText(Path.Combine(directory, $"HistoricalCapability_{stamp}.json"), json);
        }
        return path;
    }

    private static ContractIdentity? TryReadContract(HistoricalCapabilityReport report)
    {
        if (string.IsNullOrEmpty(report.ContractCode)) return null;
        if (!Enum.TryParse<ContractIdentitySource>(report.ContractIdentitySource, out var src)) return null;
        if (!Enum.TryParse<ContractIdentityConfidence>(report.ContractIdentityConfidence, out var conf)) return null;
        return new ContractIdentity
        {
            ContractCode = report.ContractCode,
            SecurityId = report.SecurityId,
            ExpirationDate = report.ExpirationDate,
            Source = src,
            Confidence = conf
        };
    }

    private static void ResolveContractIdentity(HistoricalProbeHost host, HistoricalCapabilityReport report)
    {
        try
        {
            var id = ContractIdentityResolver.Resolve(host.DeclaredMode, new ContractIdentityInputs
            {
                SecurityCode = host.SecurityCode,
                SecurityId = host.SecurityId,
                SecurityExpiration = host.SecurityExpiration,
                InstrumentInfoInstrument = host.InstrumentInfoInstrument,
                ChartSymbolFallback = null // root Instrument never used as fallback here
            });
            report.ContractCode = id.ContractCode;
            report.SecurityId = id.SecurityId;
            report.ExpirationDate = id.ExpirationDate;
            report.ContractIdentitySource = id.Source.ToString();
            report.ContractIdentityConfidence = id.Confidence.ToString();
            report.ContractCodeCandidates.PreferredFullCode = id.ContractCode;
        }
        catch (ContractIdentityResolveException ex)
        {
            report.ContractIdentitySource = nameof(ContractIdentitySource.UNAVAILABLE);
            report.ContractIdentityConfidence = nameof(ContractIdentityConfidence.NONE);
            report.ContractCode = host.SecurityCode;
            report.DataQualityEvents.Add(new DataQualityEvent
            {
                Utc = DateTime.UtcNow,
                Code = "CONTRACT_IDENTITY_UNAVAILABLE",
                Message = ex.Message,
                Detail = $"mode={host.DeclaredMode}"
            });
            report.Notes.Add("Contract identity unavailable (FAIL-CLOSED for engine); probe continues diagnostics.");
        }
    }

    private static void FillContractCandidates(HistoricalProbeHost host, HistoricalCapabilityReport report)
    {
        report.ContractCodeCandidates.Candidates = host.ContractCandidates.ToList();
        report.ContractCodeCandidates.Notes =
            "Runtime-verified: Security.Code/SecurityId/Expiration; InstrumentInfo.Instrument=root; " +
            "ChartInfo.ContractCode=NOT_FOUND; InstrumentInfo.ExpirationDate=NOT_FOUND.";
    }

    private static List<(int Index, DateTime TimeUtc, decimal High, decimal Low, decimal Volume)> ScanBars(
        HistoricalProbeHost host,
        HistoricalCapabilityReport report)
    {
        report.Bars.CurrentBar = host.CurrentBar;
        report.Bars.TotalBars = host.TotalBars;

        var last = host.CurrentBar;
        if (host.TotalBars is int tb && tb - 1 > last)
            last = tb - 1;

        var barTimes = new List<(int Index, DateTime TimeUtc, decimal High, decimal Low, decimal Volume)>();
        if (last < 0)
        {
            report.Notes.Add("No bars on chart.");
            return barTimes;
        }

        DateTime? oldestUtc = null, newestUtc = null;
        string? oldestKind = null, newestKind = null;
        var sampled = 0;
        var kindAnomalyLogged = false;

        for (var i = 0; i <= last; i++)
        {
            IndicatorCandle? c;
            try { c = host.GetCandle(i); }
            catch { continue; }
            if (c is null) continue;

            var raw = c.Time;
            if (raw.Kind is not DateTimeKind.Unspecified and not DateTimeKind.Utc && !kindAnomalyLogged)
            {
                report.DataQualityEvents.Add(new DataQualityEvent
                {
                    Utc = DateTime.UtcNow,
                    Code = "TIMESTAMP_KIND_UNEXPECTED",
                    Message = $"IndicatorCandle.Time.Kind={raw.Kind}; treating as UTC via SpecifyKind.",
                    Detail = raw.ToString("o", CultureInfo.InvariantCulture)
                });
                kindAnomalyLogged = true;
            }

            var tUtc = DateTime.SpecifyKind(raw, DateTimeKind.Utc);
            sampled++;
            barTimes.Add((i, tUtc, c.High, c.Low, c.Volume));
            if (oldestUtc is null || tUtc < oldestUtc) { oldestUtc = tUtc; oldestKind = raw.Kind.ToString(); }
            if (newestUtc is null || tUtc > newestUtc) { newestUtc = tUtc; newestKind = raw.Kind.ToString(); }
        }

        report.Bars.BarsSampled = sampled;
        report.Bars.OldestBarExchangeTimeUtc = oldestUtc;
        report.Bars.NewestBarExchangeTimeUtc = newestUtc;
        report.Bars.OldestBarExchangeTime = oldestUtc;
        report.Bars.NewestBarExchangeTime = newestUtc;
        report.Bars.OldestBarTimeKind = oldestKind;
        report.Bars.NewestBarTimeKind = newestKind;
        if (oldestUtc is not null && newestUtc is not null)
            report.Bars.HistorySpanDays = (newestUtc.Value - oldestUtc.Value).TotalDays;

        report.TimestampExchangeKind = newestUtc is not null
            ? new TimestampKindSection
            {
                SampleSource = "IndicatorCandle.Time (GetCandle)",
                DateTimeKind = newestKind,
                SampleValueIso = newestUtc.Value.ToString("o", CultureInfo.InvariantCulture),
                Treatment = "DateTime.SpecifyKind(..., Utc)",
                Notes = "Runtime: Kind=Unspecified, values match UTC."
            }
            : new TimestampKindSection
            {
                SampleSource = "IndicatorCandle.Time",
                DateTimeKind = "UNKNOWN",
                Treatment = "DateTime.SpecifyKind(..., Utc)",
                Notes = "No candle sampled."
            };

        return barTimes;
    }

    private static void AssessFromCoverage(
        InstrumentProfile profile,
        TradingDateResolver resolver,
        SessionCoverageAnalyzer.Result coverage,
        List<(int Index, DateTime TimeUtc, decimal High, decimal Low, decimal Volume)> bars,
        DateOnly? resolved,
        DateOnly? prior,
        HistoricalCapabilityReport report)
    {
        if (resolved is null)
        {
            foreach (var key in new[]
                     { "PDH_PDL", "ONH_ONL", "WeeklyHL", "PriorVAH_VAL_POC", "RotationR_M1_0600_0915", "Composite" })
            {
                report.PriorSessionData[key] = new LevelDataAssessment
                {
                    Status = "INSUFFICIENT",
                    Reason = "No chart bars."
                };
            }
            return;
        }

        var primary = profile.Sessions.Definitions.First(d => d.Name == "PrimarySession");
        var overnight = profile.Sessions.Definitions.First(d => d.Name == "Overnight");
        var rthStart = InstrumentProfileLoader.ParseTime(primary.Start);
        var rthEnd = InstrumentProfileLoader.ParseTime(primary.End);
        var onStart = InstrumentProfileLoader.ParseTime(overnight.Start);
        var onEnd = InstrumentProfileLoader.ParseTime(overnight.End);

        var priorLabel = resolver.Format(prior);
        var resolvedLabel = resolver.Format(resolved);

        var pdhBars = prior is { } pd ? CountBarsInRth(bars, resolver, pd, rthStart, rthEnd) : 0;
        report.PriorSessionData["PDH_PDL"] = new LevelDataAssessment
        {
            Status = pdhBars > 0 ? "AVAILABLE" : "INSUFFICIENT",
            Reason = pdhBars > 0
                ? $"RTH bars={pdhBars} on prior COMPLETE TradingDate {priorLabel}."
                : $"No RTH bars for prior TradingDate {priorLabel}.",
            BarsInWindow = pdhBars,
            SourceTradingDate = priorLabel
        };

        var onOpen = resolved.Value.AddDays(-1);
        var onhBars = CountBarsOvernight(bars, resolver, onOpen, resolved.Value, onStart, onEnd);
        report.PriorSessionData["ONH_ONL"] = new LevelDataAssessment
        {
            Status = onhBars > 0 ? "AVAILABLE" : "INSUFFICIENT",
            Reason = $"Overnight bars={onhBars} for TradingDate {resolvedLabel}.",
            BarsInWindow = onhBars,
            SourceTradingDate = resolvedLabel
        };

        var completed = coverage.CompletedRthSessions.Count;
        var weeklyNeed = profile.HistoryCapabilities.WeeklyHl.MinCompletedRthSessions ?? 5;
        report.PriorSessionData["WeeklyHL"] = new LevelDataAssessment
        {
            Status = completed >= weeklyNeed ? "AVAILABLE" : "INSUFFICIENT",
            Reason = $"COMPLETE RTH={completed}; need ≥{weeklyNeed} (PARTIAL/UNKNOWN excluded).",
            BarsInWindow = completed,
            SourceTradingDate = resolvedLabel
        };

        report.PriorSessionData["PriorVAH_VAL_POC"] = new LevelDataAssessment
        {
            Status = prior is not null ? "UNKNOWN" : "INSUFFICIENT",
            Reason = $"Await FixedProfile LastDay alignment; expected TradingDate={priorLabel}.",
            SourceTradingDate = priorLabel
        };

        var rot = profile.Rotation;
        var rotStart = InstrumentProfileLoader.ParseTime(rot.VolatilityWindowStart);
        var rotEnd = InstrumentProfileLoader.ParseTime(rot.VolatilityWindowEnd);
        var rotBars = CountBarsInClockWindow(bars, resolver, resolved.Value, rotStart, rotEnd);
        var tf = report.ChartTimeFrame ?? "";
        var looksM1 = tf.Contains("M1", StringComparison.OrdinalIgnoreCase);
        var asOf = bars.Count > 0 ? bars.Max(b => b.TimeUtc) : DateTime.UtcNow;
        // Probe does not invent TR — empty TR after freeze ⇒ INSUFFICIENT (no default R).
        // Full AVAILABLE requires OHLC-derived TR list (Interaction path); here we still classify time gate.
        var trPlaceholder = Array.Empty<decimal>();
        var rotAvail = RotationRAvailability.Evaluate(
            profile, resolver, resolved.Value, asOf, trPlaceholder, looksM1 ? "M1" : tf);
        // If bars exist in window and past freeze, still INSUFFICIENT without TR (cannot freeze).
        // If before freeze → NOT_YET_AVAILABLE even with bars.
        report.PriorSessionData["RotationR_M1_0600_0915"] = new LevelDataAssessment
        {
            Status = rotAvail.Status switch
            {
                RotationRAvailabilityStatus.AVAILABLE => "AVAILABLE",
                RotationRAvailabilityStatus.NOT_YET_AVAILABLE => "NOT_YET_AVAILABLE",
                _ => "INSUFFICIENT"
            },
            Reason = $"{rotAvail.Reason} windowBars={rotBars}; FailClosedInteraction={!rotAvail.AllowsInteractionEngine}.",
            BarsInWindow = rotBars,
            SourceTradingDate = resolvedLabel
        };

        var n = profile.LevelEngine.CompositeLookbackSessions;
        report.PriorSessionData["Composite"] = new LevelDataAssessment
        {
            Status = completed >= n ? "AVAILABLE" : "INSUFFICIENT",
            Reason = $"COMPLETE RTH={completed}; CompositeLookbackSessions={n}.",
            BarsInWindow = completed,
            SourceTradingDate = resolvedLabel
        };
    }

    private static void ApplyFixedProfileGuard(
        TradingDateResolver resolver,
        DateOnly? prior,
        HistoricalCapabilityReport report,
        List<PeriodIdentityAlignment> alignments)
    {
        var fp = report.FixedProfileRuntime;
        if (!fp.Attempted || fp.Outcome != "OK")
        {
            report.SessionResolution.SessionAlignment = nameof(AlignmentStatus.UNVERIFIABLE);
            return;
        }

        DateTime? scaledUtc = fp.Scaled?.TimeUtc;
        if (scaledUtc is null && !string.IsNullOrWhiteSpace(fp.Scaled?.TimeIso)
            && DateTime.TryParse(fp.Scaled.TimeIso, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var parsed))
            scaledUtc = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);

        var g = PeriodIdentityGuard.Evaluate("FixedProfile.LastDay", prior, scaledUtc, resolver);
        alignments.Add(g.Alignment);
        report.SessionResolution.SessionAlignment = g.Alignment.AlignmentStatus.ToString();
        report.SessionResolution.FixedProfileTradingDate = resolver.Format(g.Alignment.ObservedTradingDate);
        report.SessionResolution.ExpectedLastDayTradingDate = resolver.Format(g.Alignment.ExpectedTradingDate);

        if (g.EmitDataQualityEvent)
        {
            report.DataQualityEvents.Add(new DataQualityEvent
            {
                Utc = DateTime.UtcNow,
                Code = PeriodIdentityGuard.SessionDefinitionMismatch,
                Message =
                    "FixedProfile LastDay TradingDate != prior COMPLETE TradingDate. " +
                    "Do not create levels from this source.",
                Detail = g.Alignment.Detail
            });
        }

        if (!g.AllowLevelCreation)
        {
            report.PriorSessionData["PriorVAH_VAL_POC"] = new LevelDataAssessment
            {
                Status = g.Alignment.AlignmentStatus == AlignmentStatus.MISALIGNED ? "MISALIGNED" : "UNKNOWN",
                Reason = $"Alignment={g.Alignment.AlignmentStatus}; level creation forbidden for this source.",
                SourceTradingDate = resolver.Format(g.Alignment.ObservedTradingDate)
            };
            return;
        }

        if (fp.VahValPocComputable)
        {
            report.PriorSessionData["PriorVAH_VAL_POC"] = new LevelDataAssessment
            {
                Status = "AVAILABLE",
                Reason =
                    $"FixedProfile OK ALIGNED VAH={fp.ComputedVah} VAL={fp.ComputedVal} POC={fp.ComputedPoc} " +
                    $"levels={fp.Scaled?.PriceLevelCount} latencyMs={fp.LatencyMs}.",
                SourceTradingDate = resolver.Format(g.Alignment.ObservedTradingDate)
            };
        }
    }

    private static void FillVolumeProfileApi(HistoricalCapabilityReport report)
    {
        report.VolumeProfileApi = new VolumeProfileApiSection
        {
            GetFixedProfile = "PRESENT_IN_DLL",
            RequestFixedProfileAsync = "PRESENT_IN_DLL",
            OnFixedProfilesResponse = "PRESENT_IN_DLL",
            FixedProfilePeriods = "PRESENT_IN_DLL",
            RuntimeAttempt = "See fixedProfileRuntime + periodAlignments.",
            Notes = "Period-identity guard applied to FixedProfile.LastDay (Prompt 5A)."
        };
    }

    private static void FillSecondarySeries(HistoricalCapabilityReport report)
    {
        report.SecondarySeries = new SecondarySeriesSection
        {
            Verdict = "NO_SECONDARY_SERIES_API",
            CandidatesFoundInDll =
            [
                "BaseIndicator.DataSeries / SourceDataSeries",
                "IChart.TimeFrame"
            ],
            Consequence = "Chart MUST be M1 for RotationR."
        };
    }

    private static void FillOutOfChartSection(HistoricalCapabilityReport report)
    {
        report.OutOfChartHistory = new OutOfChartHistorySection
        {
            Verdict = "NO_OUT_OF_CHART_HISTORY_API",
            CandidatesFoundInDll = ["RequestFixedProfileAsync / FixedProfilePeriods"],
            Consequence = "OHLCV limited to chart-loaded bars; coverage states classify edges."
        };
    }

    private static int CountBarsInRth(
        List<(int Index, DateTime TimeUtc, decimal High, decimal Low, decimal Volume)> bars,
        TradingDateResolver resolver, DateOnly tradingDate, TimeOnly start, TimeOnly end)
    {
        var n = 0;
        foreach (var b in bars)
        {
            if (resolver.ResolveTradingDate(b.TimeUtc) != tradingDate) continue;
            var local = TimeZoneInfo.ConvertTimeFromUtc(b.TimeUtc, resolver.TradingTimeZone);
            var tod = TimeOnly.FromDateTime(local);
            if (tod >= start && tod < end) n++;
        }
        return n;
    }

    private static int CountBarsOvernight(
        List<(int Index, DateTime TimeUtc, decimal High, decimal Low, decimal Volume)> bars,
        TradingDateResolver resolver, DateOnly openDate, DateOnly endDate, TimeOnly start, TimeOnly end)
    {
        var n = 0;
        foreach (var b in bars)
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(b.TimeUtc, resolver.TradingTimeZone);
            var d = DateOnly.FromDateTime(local);
            var tod = TimeOnly.FromDateTime(local);
            if (d == openDate && tod >= start) n++;
            else if (d == endDate && tod < end) n++;
        }
        return n;
    }

    private static int CountBarsInClockWindow(
        List<(int Index, DateTime TimeUtc, decimal High, decimal Low, decimal Volume)> bars,
        TradingDateResolver resolver, DateOnly date, TimeOnly start, TimeOnly end)
    {
        var n = 0;
        foreach (var b in bars)
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(b.TimeUtc, resolver.TradingTimeZone);
            if (DateOnly.FromDateTime(local) != date) continue;
            var tod = TimeOnly.FromDateTime(local);
            if (tod >= start && tod < end) n++;
        }
        return n;
    }
}
