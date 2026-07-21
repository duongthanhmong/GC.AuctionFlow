using Aos.LevelEngine.Identity;
using Aos.LevelEngine.Profiles;
using Aos.LevelEngine.Session;

namespace Aos.LevelEngine.History;

public enum HistoryCapabilityStatus
{
    SUFFICIENT,
    PARTIAL,
    INSUFFICIENT,
    UNKNOWN
}

public enum LevelOmissionReasonCode
{
    None,
    INSUFFICIENT_HISTORY,
    SESSION_MISALIGNED,
    CONTRACT_UNAVAILABLE,
    PARTIAL_SESSION_COVERAGE,
    UNKNOWN_COVERAGE,
    ROTATION_R_NOT_YET_AVAILABLE
}

public sealed class HistoryCapabilityManifest
{
    public required string TradingDateResolverVersion { get; init; }
    public int? BarsLoadedAtStart { get; init; }
    public DateTime? HistoryStartExchangeTime { get; init; }
    public DateTime? HistoryEndExchangeTime { get; init; }
    public double? HistorySpanDays { get; init; }
    public int CompletedRthSessionCount { get; init; }
    public int CompletedOvernightSessionCount { get; init; }
    public string? ContractCode { get; set; }
    public string? SecurityId { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public ContractIdentitySource? ContractIdentitySource { get; set; }
    public ContractIdentityConfidence? ContractIdentityConfidence { get; set; }
    public string? EarliestUsableTradingDate { get; init; }
    public string? ResolvedTradingDate { get; init; }
    public string? PriorTradingDate { get; init; }
    public int SessionRolloverHourEt { get; init; }
    public required HistoryCapabilityStatus HistoryCapabilityStatus { get; init; }
    public Dictionary<string, CapabilityAssessment> Capabilities { get; init; } = new();
    public Dictionary<string, string> SessionCoverageByDate { get; init; } = new();
    public List<PeriodIdentityAlignment> PeriodAlignments { get; init; } = new();
    /// <summary>NOT_YET_AVAILABLE | INSUFFICIENT | AVAILABLE — Interaction fail-closed until AVAILABLE.</summary>
    public string? RotationRAvailabilityStatus { get; set; }
    public int? RotationRTicksFrozen { get; set; }
}

public sealed class CapabilityAssessment
{
    public required string CapabilityId { get; init; }
    public required HistoryCapabilityStatus Status { get; init; }
    public LevelOmissionReasonCode OmissionReason { get; init; }
    public bool Required { get; init; }
    public string? SourceTradingDate { get; init; }
    public string? Reason { get; init; }
    public bool FailClosedEngine { get; init; }
    /// <summary>RotationR only: NOT_YET_AVAILABLE | INSUFFICIENT | AVAILABLE.</summary>
    public string? RotationRAvailabilityStatus { get; init; }
    /// <summary>True when Interaction Engine must not start (R not AVAILABLE).</summary>
    public bool FailClosedInteraction { get; init; }
    public int? RotationRTicksFrozen { get; init; }
}

public sealed class HistoryCapabilityEvaluator
{
    private readonly InstrumentProfile _profile;
    private readonly TradingDateResolver _resolver;

    public HistoryCapabilityEvaluator(InstrumentProfile profile, TradingDateResolver resolver)
    {
        _profile = profile;
        _resolver = resolver;
    }

    public HistoryCapabilityManifest Build(
        SessionCoverageAnalyzer.Result coverage,
        IReadOnlyList<DateTime> barTimesUtc,
        ContractIdentity? contract,
        DateOnly resolvedTradingDate,
        DateOnly? priorTradingDate,
        string? chartTimeFrame,
        IReadOnlyList<PeriodIdentityAlignment>? periodAlignments = null,
        IReadOnlyList<decimal>? m1TrueRangesInVolatilityWindow = null,
        DateTime? asOfExchangeUtc = null)
    {
        var caps = _profile.HistoryCapabilities
            ?? throw new InstrumentProfileLoadException("FAIL STARTUP: HistoryCapabilities required.");

        DateTime? start = barTimesUtc.Count > 0 ? barTimesUtc.Min() : null;
        DateTime? end = barTimesUtc.Count > 0 ? barTimesUtc.Max() : null;
        double? span = start is not null && end is not null
            ? (end.Value - start.Value).TotalDays
            : null;

        var completed = coverage.CompletedRthSessions;
        var assessments = new Dictionary<string, CapabilityAssessment>();

        assessments["PDH_PDL"] = AssessRth("PDH_PDL", caps.PdhPdl, completed.Count, priorTradingDate);
        assessments["ONH_ONL"] = AssessOvernight(caps.OnhOnl, coverage.CompletedOvernightSessionCount, resolvedTradingDate);
        assessments["WeeklyHL"] = AssessRth("WeeklyHL", caps.WeeklyHl, completed.Count, resolvedTradingDate);
        assessments["Composite"] = AssessRth(
            "Composite",
            OverrideMin(caps.Composite, _profile.LevelEngine.CompositeLookbackSessions),
            completed.Count,
            resolvedTradingDate);
        assessments["nPOC"] = AssessRth(
            "nPOC",
            OverrideMin(caps.Npoc, _profile.LevelEngine.NPocLookbackSessions),
            completed.Count,
            resolvedTradingDate);

        var asOf = asOfExchangeUtc ?? end ?? DateTime.UtcNow;
        var rotAssess = AssessRotation(
            barTimesUtc, resolvedTradingDate, chartTimeFrame, asOf, m1TrueRangesInVolatilityWindow);
        assessments["RotationR"] = rotAssess;

        // Optional levels omit; required → FailClosedEngine
        var anyRequiredFail = assessments.Values.Any(a => a.FailClosedEngine);
        var allOk = assessments.Values.All(a =>
            a.CapabilityId == "RotationR"
                ? a.RotationRAvailabilityStatus == nameof(RotationRAvailabilityStatus.AVAILABLE)
                : a.Status == HistoryCapabilityStatus.SUFFICIENT);
        var anyPartial = assessments.Values.Any(a => a.Status == HistoryCapabilityStatus.PARTIAL);

        var overall = anyRequiredFail ? HistoryCapabilityStatus.INSUFFICIENT
            : allOk ? HistoryCapabilityStatus.SUFFICIENT
            : anyPartial ? HistoryCapabilityStatus.PARTIAL
            : HistoryCapabilityStatus.INSUFFICIENT;

        return new HistoryCapabilityManifest
        {
            TradingDateResolverVersion = TradingDateResolver.TradingDateResolverVersion,
            BarsLoadedAtStart = barTimesUtc.Count,
            HistoryStartExchangeTime = start,
            HistoryEndExchangeTime = end,
            HistorySpanDays = span,
            CompletedRthSessionCount = completed.Count,
            CompletedOvernightSessionCount = coverage.CompletedOvernightSessionCount,
            ContractCode = contract?.ContractCode,
            SecurityId = contract?.SecurityId,
            ExpirationDate = contract?.ExpirationDate,
            ContractIdentitySource = contract?.Source,
            ContractIdentityConfidence = contract?.Confidence,
            EarliestUsableTradingDate = _resolver.Format(coverage.EarliestUsableTradingDate),
            ResolvedTradingDate = _resolver.Format(resolvedTradingDate),
            PriorTradingDate = _resolver.Format(priorTradingDate),
            SessionRolloverHourEt = _resolver.TradingSessionRolloverLocalTime.Hour,
            HistoryCapabilityStatus = overall,
            Capabilities = assessments,
            SessionCoverageByDate = coverage.ByTradingDate.ToDictionary(
                kv => _resolver.Format(kv.Key),
                kv => kv.Value.ToString()),
            PeriodAlignments = periodAlignments?.ToList() ?? new List<PeriodIdentityAlignment>(),
            RotationRAvailabilityStatus = rotAssess.RotationRAvailabilityStatus,
            RotationRTicksFrozen = rotAssess.RotationRTicksFrozen
        };
    }

    private static HistoryCapabilityRequirement OverrideMin(HistoryCapabilityRequirement req, int lookback) =>
        new()
        {
            MinCompletedRthSessions = lookback,
            MinCompletedOvernightSessions = req.MinCompletedOvernightSessions,
            RequireChartTimeframeM1 = req.RequireChartTimeframeM1,
            Required = req.Required
        };

    private CapabilityAssessment AssessRth(
        string id, HistoryCapabilityRequirement req, int completedCount, DateOnly? sourceTd)
    {
        var need = req.MinCompletedRthSessions ?? 0;
        if (completedCount >= need)
        {
            return new CapabilityAssessment
            {
                CapabilityId = id,
                Status = HistoryCapabilityStatus.SUFFICIENT,
                Required = req.Required,
                SourceTradingDate = _resolver.Format(sourceTd),
                Reason = $"CompletedRthSessions={completedCount} >= {need}."
            };
        }

        return new CapabilityAssessment
        {
            CapabilityId = id,
            Status = HistoryCapabilityStatus.INSUFFICIENT,
            OmissionReason = LevelOmissionReasonCode.INSUFFICIENT_HISTORY,
            Required = req.Required,
            FailClosedEngine = req.Required,
            SourceTradingDate = _resolver.Format(sourceTd),
            Reason = $"INSUFFICIENT_HISTORY: CompletedRthSessions={completedCount} < {need}."
        };
    }

    private CapabilityAssessment AssessOvernight(
        HistoryCapabilityRequirement req, int overnightCount, DateOnly sourceTd)
    {
        var need = req.MinCompletedOvernightSessions ?? 0;
        if (overnightCount >= need)
        {
            return new CapabilityAssessment
            {
                CapabilityId = "ONH_ONL",
                Status = HistoryCapabilityStatus.SUFFICIENT,
                Required = req.Required,
                SourceTradingDate = _resolver.Format(sourceTd),
                Reason = $"CompletedOvernightSessions={overnightCount} >= {need}."
            };
        }

        return new CapabilityAssessment
        {
            CapabilityId = "ONH_ONL",
            Status = HistoryCapabilityStatus.INSUFFICIENT,
            OmissionReason = LevelOmissionReasonCode.INSUFFICIENT_HISTORY,
            Required = req.Required,
            FailClosedEngine = req.Required,
            SourceTradingDate = _resolver.Format(sourceTd),
            Reason = $"INSUFFICIENT_HISTORY: CompletedOvernightSessions={overnightCount} < {need}."
        };
    }

    private CapabilityAssessment AssessRotation(
        IReadOnlyList<DateTime> bars,
        DateOnly resolved,
        string? chartTf,
        DateTime asOfExchangeUtc,
        IReadOnlyList<decimal>? m1TrueRanges)
    {
        var rot = _profile.Rotation;
        var start = InstrumentProfileLoader.ParseTime(rot.VolatilityWindowStart);
        var end = InstrumentProfileLoader.ParseTime(rot.VolatilityWindowEnd);
        var barCount = 0;
        foreach (var raw in bars)
        {
            var utc = DateTime.SpecifyKind(raw, DateTimeKind.Utc);
            if (_resolver.ResolveTradingDate(utc) != resolved) continue;
            var local = TimeZoneInfo.ConvertTimeFromUtc(utc, _resolver.TradingTimeZone);
            if (DateOnly.FromDateTime(local) != resolved) continue;
            var tod = TimeOnly.FromDateTime(local);
            if (tod >= start && tod < end) barCount++;
        }

        // True ranges: use supplied list; never invent defaults. Empty after freeze → INSUFFICIENT.
        IReadOnlyList<decimal> trs = m1TrueRanges ?? Array.Empty<decimal>();
        // If TRs not supplied but bars exist, still cannot freeze without TR values.
        if (trs.Count == 0 && barCount > 0 && m1TrueRanges is null)
        {
            // Capability scan without OHLC: treat bar presence as candidate; freeze deferred.
            // After freeze time without TR → INSUFFICIENT (no default R).
        }

        var result = RotationRAvailability.Evaluate(
            _profile, _resolver, resolved, asOfExchangeUtc, trs, chartTf);

        // If we had bars but empty TR and past freeze, Evaluate already returns INSUFFICIENT.
        // If before freeze, NOT_YET_AVAILABLE regardless of bars.

        return result.Status switch
        {
            RotationRAvailabilityStatus.NOT_YET_AVAILABLE => new CapabilityAssessment
            {
                CapabilityId = "RotationR",
                Status = HistoryCapabilityStatus.UNKNOWN,
                OmissionReason = LevelOmissionReasonCode.ROTATION_R_NOT_YET_AVAILABLE,
                Required = _profile.HistoryCapabilities.RotationR.Required,
                FailClosedEngine = false,
                FailClosedInteraction = true,
                RotationRAvailabilityStatus = nameof(RotationRAvailabilityStatus.NOT_YET_AVAILABLE),
                SourceTradingDate = _resolver.Format(resolved),
                Reason = result.Reason
            },
            RotationRAvailabilityStatus.AVAILABLE => new CapabilityAssessment
            {
                CapabilityId = "RotationR",
                Status = HistoryCapabilityStatus.SUFFICIENT,
                Required = _profile.HistoryCapabilities.RotationR.Required,
                FailClosedInteraction = false,
                RotationRAvailabilityStatus = nameof(RotationRAvailabilityStatus.AVAILABLE),
                RotationRTicksFrozen = result.RotationRTicks,
                SourceTradingDate = _resolver.Format(resolved),
                Reason = result.Reason
            },
            _ => new CapabilityAssessment
            {
                CapabilityId = "RotationR",
                Status = HistoryCapabilityStatus.INSUFFICIENT,
                OmissionReason = LevelOmissionReasonCode.INSUFFICIENT_HISTORY,
                Required = _profile.HistoryCapabilities.RotationR.Required,
                FailClosedEngine = _profile.HistoryCapabilities.RotationR.Required,
                FailClosedInteraction = true,
                RotationRAvailabilityStatus = nameof(RotationRAvailabilityStatus.INSUFFICIENT),
                SourceTradingDate = _resolver.Format(resolved),
                Reason = result.Reason + $" windowBars={barCount}."
            }
        };
    }
}
