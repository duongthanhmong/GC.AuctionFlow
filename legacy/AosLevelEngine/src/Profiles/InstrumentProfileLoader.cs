using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aos.LevelEngine.Profiles;

public sealed class InstrumentProfileLoadException : Exception
{
    public InstrumentProfileLoadException(string message) : base(message) { }
    public InstrumentProfileLoadException(string message, Exception inner) : base(message, inner) { }
}

public static class InstrumentProfileLoader
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        PropertyNameCaseInsensitive = false,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters = { new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false) }
    };

    public static InstrumentProfile LoadFromFile(string path)
    {
        if (!File.Exists(path))
            throw new InstrumentProfileLoadException($"Profile file not found: {path}");
        return LoadFromJson(File.ReadAllText(path), path);
    }

    public static InstrumentProfile LoadEmbeddedEs()
    {
        var asm = typeof(InstrumentProfileLoader).Assembly;
        using var s = asm.GetManifestResourceStream("Aos.LevelEngine.Config.InstrumentProfile-ES-v0.1.json")
            ?? throw new InstrumentProfileLoadException("Embedded InstrumentProfile-ES-v0.1.json missing.");
        using var r = new StreamReader(s);
        return LoadFromJson(r.ReadToEnd(), "embedded:ES-v0.1");
    }

    public static InstrumentProfile LoadFromJson(string json, string? label = null)
    {
        label ??= "(memory)";
        var dto = JsonSerializer.Deserialize<Dto>(json, Opts)
            ?? throw new InstrumentProfileLoadException($"Null profile: {label}");
        Validate(dto, label);
        return Map(dto);
    }

    private static void Validate(Dto d, string label)
    {
        var e = new List<string>();
        if (string.IsNullOrWhiteSpace(d.ProfileId)) e.Add("ProfileId");
        if (d.TickSize is null or <= 0) e.Add("TickSize");
        if (d.Zones is null) e.Add("Zones");
        if (d.Interaction is null) e.Add("Interaction");
        if (d.Rotation is null) e.Add("Rotation");
        if (d.Sessions?.Definitions is null || d.Sessions.Definitions.Count == 0) e.Add("Sessions");

        // Prompt 5A — TradingSessionIdentity FAIL STARTUP
        if (d.TradingSessionIdentity is null)
            e.Add("TradingSessionIdentity");
        else
        {
            var tsi = d.TradingSessionIdentity;
            if (string.IsNullOrWhiteSpace(tsi.TradingSessionRolloverLocalTime))
                e.Add("TradingSessionIdentity.TradingSessionRolloverLocalTime");
            if (string.IsNullOrWhiteSpace(tsi.TradingTimeZone))
                e.Add("TradingSessionIdentity.TradingTimeZone");
            if (string.IsNullOrWhiteSpace(tsi.RthStartLocalTime))
                e.Add("TradingSessionIdentity.RthStartLocalTime");
            if (string.IsNullOrWhiteSpace(tsi.RthEndLocalTime))
                e.Add("TradingSessionIdentity.RthEndLocalTime");
            if (tsi.PartialCoverageMaxMissingMinutes is null)
                e.Add("TradingSessionIdentity.PartialCoverageMaxMissingMinutes");
        }

        if (d.HistoryCapabilities is null)
            e.Add("HistoryCapabilities");
        else
        {
            if (d.HistoryCapabilities.PDH_PDL is null) e.Add("HistoryCapabilities.PDH_PDL");
            if (d.HistoryCapabilities.ONH_ONL is null) e.Add("HistoryCapabilities.ONH_ONL");
            if (d.HistoryCapabilities.WeeklyHL is null) e.Add("HistoryCapabilities.WeeklyHL");
            if (d.HistoryCapabilities.Composite is null) e.Add("HistoryCapabilities.Composite");
            if (d.HistoryCapabilities.nPOC is null) e.Add("HistoryCapabilities.nPOC");
            if (d.HistoryCapabilities.RotationR is null) e.Add("HistoryCapabilities.RotationR");
        }

        // A0 — FAIL STARTUP, no silent defaults
        if (d.LevelEngine is null)
            e.Add("LevelEngine");
        else
        {
            var le = d.LevelEngine;
            if (le.ValueAreaVolumePercent is null) e.Add("LevelEngine.ValueAreaVolumePercent");
            if (string.IsNullOrWhiteSpace(le.ValueAreaAlgorithm)) e.Add("LevelEngine.ValueAreaAlgorithm");
            if (string.IsNullOrWhiteSpace(le.PocTieBreak)) e.Add("LevelEngine.PocTieBreak");
            if (le.CompositeLookbackSessions is null) e.Add("LevelEngine.CompositeLookbackSessions");
            if (le.NPocLookbackSessions is null) e.Add("LevelEngine.NPocLookbackSessions");
            if (string.IsNullOrWhiteSpace(le.CompositeBoundaryAlgorithm)) e.Add("LevelEngine.CompositeBoundaryAlgorithm");
            if (string.IsNullOrWhiteSpace(le.CompositeBoundaryAlgorithmVersion)) e.Add("LevelEngine.CompositeBoundaryAlgorithmVersion");
            if (string.IsNullOrWhiteSpace(le.ExhaustedPolicy)) e.Add("LevelEngine.ExhaustedPolicy");
            if (le.SinglePrintEdgesEnabled is null) e.Add("LevelEngine.SinglePrintEdgesEnabled");
            if (string.IsNullOrWhiteSpace(le.SinglePrintStatus)) e.Add("LevelEngine.SinglePrintStatus");
            if (le.AllowMidSessionLevelCreation is null) e.Add("LevelEngine.AllowMidSessionLevelCreation");
        }

        if (e.Count > 0)
            throw new InstrumentProfileLoadException(
                $"FAIL STARTUP ({label}) missing required fields: {string.Join(", ", e)}");
    }

    private static InstrumentProfile Map(Dto d)
    {
        var le = d.LevelEngine!;
        return new InstrumentProfile
        {
            SchemaVersion = d.SchemaVersion!,
            ProfileId = d.ProfileId!,
            Mode = Enum.Parse<ProfileMode>(d.Mode!, ignoreCase: false),
            AnalysisSymbol = d.AnalysisSymbol!,
            ExecutionSymbol = d.ExecutionSymbol,
            TickSize = d.TickSize!.Value,
            Timezone = d.Timezone!,
            Sessions = new SessionsSection
            {
                Definitions = d.Sessions!.Definitions!.Select(s => new SessionDefinition
                {
                    Name = s.Name!,
                    Start = s.Start!,
                    End = s.End!,
                    SessionDefinitionType = Enum.Parse<SessionDefinitionType>(s.SessionDefinitionType!, false),
                    WrapsMidnight = s.WrapsMidnight ?? false
                }).ToList()
            },
            TradingSessionIdentity = new TradingSessionIdentitySection
            {
                TradingSessionRolloverLocalTime = d.TradingSessionIdentity!.TradingSessionRolloverLocalTime!,
                TradingTimeZone = d.TradingSessionIdentity.TradingTimeZone!,
                RthStartLocalTime = d.TradingSessionIdentity.RthStartLocalTime!,
                RthEndLocalTime = d.TradingSessionIdentity.RthEndLocalTime!,
                PartialCoverageMaxMissingMinutes = d.TradingSessionIdentity.PartialCoverageMaxMissingMinutes!.Value
            },
            HistoryCapabilities = MapHistoryCapabilities(d.HistoryCapabilities!),
            Rotation = new RotationSection
            {
                VolatilityWindowStart = d.Rotation!.VolatilityWindowStart!,
                VolatilityWindowEnd = d.Rotation.VolatilityWindowEnd!,
                TrueRangeMultiplier = d.Rotation.TrueRangeMultiplier!.Value,
                ClampMinTicks = d.Rotation.ClampMinTicks!.Value,
                ClampMaxTicks = d.Rotation.ClampMaxTicks!.Value,
                FreezeTime = d.Rotation.FreezeTime!,
                TrueRangeTimeframe = d.Rotation.TrueRangeTimeframe!
            },
            Interaction = new InteractionSection
            {
                ResetTicksFloor = d.Interaction!.ResetTicksFloor!.Value,
                ResetTicksUsesMaxWithRotationR = d.Interaction.ResetTicksUsesMaxWithRotationR!.Value,
                ResetTimeMinutes = d.Interaction.ResetTimeMinutes!.Value,
                MaxInteractionTimeMinutes = d.Interaction.MaxInteractionTimeMinutes,
                ExitDistanceEqualsRotationR = d.Interaction.ExitDistanceEqualsRotationR
            },
            Zones = new ZonesSection
            {
                WidthsAreTotalTicks = d.Zones!.WidthsAreTotalTicks!.Value,
                ExtremeTicks = d.Zones.ExtremeTicks!.Value,
                VahValTicks = d.Zones.VahValTicks,
                PocTicks = d.Zones.PocTicks,
                ValueAreaOrPocTicks = d.Zones.ValueAreaOrPocTicks,
                CompositeTicks = d.Zones.CompositeTicks,
                ClusterMaxTicks = d.Zones.ClusterMaxTicks!.Value,
                ClusterGapTicks = d.Zones.ClusterGapTicks!.Value
            },
            Outcome = d.Outcome is null ? null : new OutcomeSection
            {
                ReferencePrice = Enum.Parse<OutcomeReferencePrice>(d.Outcome.ReferencePrice!, false),
                RecordExcursionTicks = d.Outcome.RecordExcursionTicks!.Value,
                RecordExcursionR = d.Outcome.RecordExcursionR!.Value,
                Horizons = d.Outcome.Horizons!.ToList(),
                SupportTruncationReason = d.Outcome.SupportTruncationReason!.Value
            },
            LevelEngine = new LevelEngineSection
            {
                ValueAreaVolumePercent = le.ValueAreaVolumePercent!.Value,
                ValueAreaAlgorithm = Enum.Parse<ValueAreaAlgorithm>(le.ValueAreaAlgorithm!, false),
                PocTieBreak = Enum.Parse<PocTieBreak>(le.PocTieBreak!, false),
                CompositeLookbackSessions = le.CompositeLookbackSessions!.Value,
                NPocLookbackSessions = le.NPocLookbackSessions!.Value,
                CompositeBoundaryAlgorithm = Enum.Parse<CompositeBoundaryAlgorithm>(le.CompositeBoundaryAlgorithm!, false),
                CompositeBoundaryAlgorithmVersion = le.CompositeBoundaryAlgorithmVersion!,
                ExhaustedPolicy = Enum.Parse<ExhaustedPolicy>(le.ExhaustedPolicy!, false),
                SinglePrintEdgesEnabled = le.SinglePrintEdgesEnabled!.Value,
                SinglePrintStatus = le.SinglePrintStatus!,
                AllowMidSessionLevelCreation = le.AllowMidSessionLevelCreation!.Value
            }
        };
    }

    private static HistoryCapabilitiesSection MapHistoryCapabilities(HistoryCapabilitiesDto h) =>
        new()
        {
            PdhPdl = MapReq(h.PDH_PDL!),
            OnhOnl = MapReq(h.ONH_ONL!),
            WeeklyHl = MapReq(h.WeeklyHL!),
            Composite = MapReq(h.Composite!),
            Npoc = MapReq(h.nPOC!),
            RotationR = MapReq(h.RotationR!)
        };

    private static HistoryCapabilityRequirement MapReq(HistoryCapabilityReqDto r) =>
        new()
        {
            MinCompletedRthSessions = r.MinCompletedRthSessions,
            MinCompletedOvernightSessions = r.MinCompletedOvernightSessions,
            RequireChartTimeframeM1 = r.RequireChartTimeframeM1 ?? false,
            Required = r.Required ?? false
        };

    private sealed class Dto
    {
        public string? SchemaVersion { get; set; }
        public string? ProfileId { get; set; }
        public string? Mode { get; set; }
        public string? AnalysisSymbol { get; set; }
        public string? ExecutionSymbol { get; set; }
        public decimal? TickSize { get; set; }
        public string? Timezone { get; set; }
        public SessionsDto? Sessions { get; set; }
        public TradingSessionIdentityDto? TradingSessionIdentity { get; set; }
        public HistoryCapabilitiesDto? HistoryCapabilities { get; set; }
        public RotationDto? Rotation { get; set; }
        public InteractionDto? Interaction { get; set; }
        public ZonesDto? Zones { get; set; }
        public OutcomeDto? Outcome { get; set; }
        public LevelEngineDto? LevelEngine { get; set; }
    }

    private sealed class TradingSessionIdentityDto
    {
        public string? TradingSessionRolloverLocalTime { get; set; }
        public string? TradingTimeZone { get; set; }
        public string? RthStartLocalTime { get; set; }
        public string? RthEndLocalTime { get; set; }
        public int? PartialCoverageMaxMissingMinutes { get; set; }
    }

    private sealed class HistoryCapabilitiesDto
    {
        public HistoryCapabilityReqDto? PDH_PDL { get; set; }
        public HistoryCapabilityReqDto? ONH_ONL { get; set; }
        public HistoryCapabilityReqDto? WeeklyHL { get; set; }
        public HistoryCapabilityReqDto? Composite { get; set; }
        public HistoryCapabilityReqDto? nPOC { get; set; }
        public HistoryCapabilityReqDto? RotationR { get; set; }
    }

    private sealed class HistoryCapabilityReqDto
    {
        public int? MinCompletedRthSessions { get; set; }
        public int? MinCompletedOvernightSessions { get; set; }
        public bool? RequireChartTimeframeM1 { get; set; }
        public bool? Required { get; set; }
    }

    private sealed class LevelEngineDto
    {
        public double? ValueAreaVolumePercent { get; set; }
        public string? ValueAreaAlgorithm { get; set; }
        public string? PocTieBreak { get; set; }
        public int? CompositeLookbackSessions { get; set; }
        public int? NPocLookbackSessions { get; set; }
        public string? CompositeBoundaryAlgorithm { get; set; }
        public string? CompositeBoundaryAlgorithmVersion { get; set; }
        public string? ExhaustedPolicy { get; set; }
        public bool? SinglePrintEdgesEnabled { get; set; }
        public string? SinglePrintStatus { get; set; }
        public bool? AllowMidSessionLevelCreation { get; set; }
    }

    private sealed class SessionsDto { public List<SessionDto>? Definitions { get; set; } }
    private sealed class SessionDto
    {
        public string? Name { get; set; }
        public string? Start { get; set; }
        public string? End { get; set; }
        public string? SessionDefinitionType { get; set; }
        public bool? WrapsMidnight { get; set; }
    }
    private sealed class RotationDto
    {
        public string? VolatilityWindowStart { get; set; }
        public string? VolatilityWindowEnd { get; set; }
        public double? TrueRangeMultiplier { get; set; }
        public int? ClampMinTicks { get; set; }
        public int? ClampMaxTicks { get; set; }
        public string? FreezeTime { get; set; }
        public string? TrueRangeTimeframe { get; set; }
    }
    private sealed class InteractionDto
    {
        public int? ResetTicksFloor { get; set; }
        public bool? ResetTicksUsesMaxWithRotationR { get; set; }
        public double? ResetTimeMinutes { get; set; }
        public double? MaxInteractionTimeMinutes { get; set; }
        public bool? ExitDistanceEqualsRotationR { get; set; }
    }
    private sealed class ZonesDto
    {
        public bool? WidthsAreTotalTicks { get; set; }
        public int? ExtremeTicks { get; set; }
        public int? VahValTicks { get; set; }
        public int? PocTicks { get; set; }
        public int? ValueAreaOrPocTicks { get; set; }
        public int? CompositeTicks { get; set; }
        public int? ClusterMaxTicks { get; set; }
        public int? ClusterGapTicks { get; set; }
    }
    private sealed class OutcomeDto
    {
        public string? ReferencePrice { get; set; }
        public bool? RecordExcursionTicks { get; set; }
        public bool? RecordExcursionR { get; set; }
        public List<string>? Horizons { get; set; }
        public bool? SupportTruncationReason { get; set; }
    }

    public static TimeOnly ParseTime(string hhmm) =>
        TimeOnly.ParseExact(hhmm, "HH:mm", CultureInfo.InvariantCulture);
}
