using System.Text.Json.Serialization;

namespace Aos.LevelEngine.Atas;

public sealed class HistoricalCapabilityReport
{
    public string SchemaVersion { get; set; } = "1.3.0";
    public string ProbeVersion { get; set; } = "0.4.0";
    public DateTime GeneratedUtc { get; set; }
    public string? ContractCode { get; set; }
    public string? SecurityId { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? ContractIdentitySource { get; set; }
    public string? ContractIdentityConfidence { get; set; }
    public string? InstrumentInfoInstrument { get; set; }
    public string? IndicatorInstrumentProperty { get; set; }
    public string? Exchange { get; set; }
    public decimal? TickSize { get; set; }
    public string? ChartTimeFrame { get; set; }
    public string? ChartType { get; set; }
    public ContractCodeCandidatesSection ContractCodeCandidates { get; set; } = new();
    public BarsAccessSection Bars { get; set; } = new();
    public SessionResolutionSection SessionResolution { get; set; } = new();
    public History.HistoryCapabilityManifest? HistoryCapabilityManifest { get; set; }
    public Dictionary<string, LevelDataAssessment> PriorSessionData { get; set; } = new();
    public VolumeProfileApiSection VolumeProfileApi { get; set; } = new();
    public FixedProfileRuntimeSection FixedProfileRuntime { get; set; } = new();
    public SecondarySeriesSection SecondarySeries { get; set; } = new();
    public OutOfChartHistorySection OutOfChartHistory { get; set; } = new();
    public TimestampKindSection TimestampExchangeKind { get; set; } = new();
    public List<DataQualityEvent> DataQualityEvents { get; set; } = new();
    public List<string> Notes { get; set; } = new();
}

public sealed class SessionResolutionSection
{
    public string? TradingDateResolverVersion { get; set; }
    public string? ResolvedTradingDate { get; set; }
    public string? PriorTradingDate { get; set; }
    public string? EarliestUsableTradingDate { get; set; }
    public int SessionRolloverHourEt { get; set; } = 18;
    public string? SessionAlignment { get; set; }
    public string? FixedProfileTradingDate { get; set; }
    public string? ExpectedLastDayTradingDate { get; set; }
    public string? HistoryCapabilityStatus { get; set; }
    public string? Notes { get; set; }
}

public sealed class ContractCodeCandidatesSection
{
    public string? PreferredFullCode { get; set; }
    public List<ContractCodeCandidate> Candidates { get; set; } = new();
    public string? Notes { get; set; }
}

public sealed class ContractCodeCandidate
{
    public string Source { get; set; } = "";
    public string Property { get; set; } = "";
    public string? Value { get; set; }
    public string? Evidence { get; set; }
}

public sealed class BarsAccessSection
{
    public int? CurrentBar { get; set; }
    public int? TotalBars { get; set; }
    public int BarsSampled { get; set; }
    public DateTime? OldestBarExchangeTimeUtc { get; set; }
    public DateTime? NewestBarExchangeTimeUtc { get; set; }
    /// <summary>Legacy alias — same as OldestBarExchangeTimeUtc after SpecifyKind(Utc).</summary>
    public DateTime? OldestBarExchangeTime { get; set; }
    public DateTime? NewestBarExchangeTime { get; set; }
    public double? HistorySpanDays { get; set; }
    public string? OldestBarTimeKind { get; set; }
    public string? NewestBarTimeKind { get; set; }
}

public sealed class LevelDataAssessment
{
    public string Status { get; set; } = "UNKNOWN"; // AVAILABLE | INSUFFICIENT | UNKNOWN | MISALIGNED
    public string? Reason { get; set; }
    public int? BarsInWindow { get; set; }
    public DateTime? WindowStartEt { get; set; }
    public DateTime? WindowEndEt { get; set; }
    /// <summary>TradingDate this source is attributed to (ISO yyyy-MM-dd).</summary>
    public string? SourceTradingDate { get; set; }
}

public sealed class VolumeProfileApiSection
{
    public string GetFixedProfile { get; set; } = "PRESENT_IN_DLL";
    public string RequestFixedProfileAsync { get; set; } = "PRESENT_IN_DLL";
    public string OnFixedProfilesResponse { get; set; } = "PRESENT_IN_DLL";
    public string FixedProfilePeriods { get; set; } = "PRESENT_IN_DLL";
    public string? RuntimeAttempt { get; set; }
    public string? Notes { get; set; }
}

public sealed class FixedProfileRuntimeSection
{
    public bool Attempted { get; set; }
    public string Period { get; set; } = "LastDay";
    public bool? ResponseReturned { get; set; }
    public bool? ResponseNull { get; set; }
    public long? LatencyMs { get; set; }
    public string? Outcome { get; set; } // OK | NULL_RESPONSE | TIMEOUT | EXCEPTION | SKIPPED
    public string? FailureReason { get; set; }
    public FixedProfileCandleSample? Scaled { get; set; }
    public FixedProfileCandleSample? Original { get; set; }
    public decimal? ComputedVah { get; set; }
    public decimal? ComputedVal { get; set; }
    public decimal? ComputedPoc { get; set; }
    public bool VahValPocComputable { get; set; }
}

public sealed class FixedProfileCandleSample
{
    public string? TimeIso { get; set; }
    public string? TimeKind { get; set; }
    /// <summary>SpecifyKind(Utc) of candle.Time for TradingDate math.</summary>
    public DateTime? TimeUtc { get; set; }
    public decimal? High { get; set; }
    public decimal? Low { get; set; }
    public decimal? Volume { get; set; }
    public int? PriceLevelCount { get; set; }
    public decimal? ValueAreaHigh { get; set; }
    public decimal? ValueAreaLow { get; set; }
    public decimal? MaxVolumePrice { get; set; }
}

public sealed class SecondarySeriesSection
{
    public string Verdict { get; set; } = "NO_SECONDARY_SERIES_API";
    public List<string> CandidatesFoundInDll { get; set; } = new();
    public List<string> DocsNotes { get; set; } = new();
    public string? Consequence { get; set; }
}

public sealed class OutOfChartHistorySection
{
    public string Verdict { get; set; } = "NO_OUT_OF_CHART_HISTORY_API";
    public List<string> CandidatesFoundInDll { get; set; } = new();
    public string? Consequence { get; set; }
}

public sealed class TimestampKindSection
{
    public string? SampleSource { get; set; }
    public string? DateTimeKind { get; set; }
    public string? SampleValueIso { get; set; }
    public string? Treatment { get; set; }
    public string? Notes { get; set; }
}

public sealed class DataQualityEvent
{
    public DateTime Utc { get; set; }
    public string Code { get; set; } = "";
    public string Message { get; set; } = "";
    public string? Detail { get; set; }
}
