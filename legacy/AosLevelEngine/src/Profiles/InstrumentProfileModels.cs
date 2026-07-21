namespace Aos.LevelEngine.Profiles;

public enum ProfileMode { FULL, LOGGER_ONLY }

public enum SessionDefinitionType
{
    EXCHANGE_SESSION,
    ANALYTICAL_CONVENTION,
    INITIAL_BALANCE,
    OPENING_RANGE
}

public enum OutcomeReferencePrice { APPROACH_SIDE_ZONE_EDGE }

public enum ValueAreaAlgorithm
{
    EXPAND_FROM_POC_V1
}

public enum PocTieBreak
{
    LOWEST_PRICE,
    HIGHEST_PRICE
}

public enum CompositeBoundaryAlgorithm
{
    SESSION_HIGH_LOW_EXTREMES_V1
}

public enum ExhaustedPolicy
{
    /// <summary>EffectiveGrade becomes inactive (not traded as A/B/C).</summary>
    INACTIVE,
    /// <summary>EffectiveGrade capped at C.</summary>
    CAP_C
}

/// <summary>v1 predeclared levels only — VWAP/developing/intraday OUT OF SCOPE.</summary>
public enum LevelTypeKind
{
    Pdh, Pdl, Onh, Onl,
    Vah, Val, Poc, Npoc,
    Ibh, Ibl,
    CompositeBoundary,
    WeeklyHigh, WeeklyLow,
    /// <summary>Only if profile SinglePrintEdgesEnabled and TPO source verified.</summary>
    SinglePrintEdge
}

public enum IndependentSourceFamily
{
    SESSION_EXTREME,
    VOLUME_PROFILE,
    TPO_STRUCTURE,
    MULTISESSION_COMPOSITE,
    WEEKLY_STRUCTURE
}

public enum StructuralGrade { A, B, C }

public enum FreshnessState
{
    FRESH,
    VALID,
    DEGRADED,
    EXHAUSTED
}

public enum LevelBatch
{
    PRESESSION_SET,
    OPEN_SET,
    IB_SET
}

public enum LevelOmissionReason
{
    None,
    MissingHistoricalData,
    INSUFFICIENT_HISTORY,
    SinglePrintNotVerified,
    NpocAlreadyTested,
    OutsideEngineScope,
    SessionMisaligned,
    PartialSessionCoverage
}

/// <summary>Frozen product knobs — engines never hard-code ticks/seconds.</summary>
public sealed class InstrumentProfile
{
    public required string SchemaVersion { get; init; }
    public required string ProfileId { get; init; }
    public required ProfileMode Mode { get; init; }
    public required string AnalysisSymbol { get; init; }
    public string? ExecutionSymbol { get; init; }
    public required decimal TickSize { get; init; }
    public required string Timezone { get; init; }
    public required SessionsSection Sessions { get; init; }
    public required TradingSessionIdentitySection TradingSessionIdentity { get; init; }
    public required HistoryCapabilitiesSection HistoryCapabilities { get; init; }
    public required RotationSection Rotation { get; init; }
    public required InteractionSection Interaction { get; init; }
    public required ZonesSection Zones { get; init; }
    public OutcomeSection? Outcome { get; init; }
    public required LevelEngineSection LevelEngine { get; init; }

    public int ComputeResetTicks(int rotationR)
    {
        if (!Interaction.ResetTicksUsesMaxWithRotationR)
            return Interaction.ResetTicksFloor;
        return Math.Max(Interaction.ResetTicksFloor, rotationR);
    }

    public int WidthTicksFor(LevelTypeKind kind) => kind switch
    {
        LevelTypeKind.Pdh or LevelTypeKind.Pdl or LevelTypeKind.Onh or LevelTypeKind.Onl
            or LevelTypeKind.Ibh or LevelTypeKind.Ibl or LevelTypeKind.SinglePrintEdge
            or LevelTypeKind.WeeklyHigh or LevelTypeKind.WeeklyLow
            => Zones.ExtremeTicks,
        LevelTypeKind.Vah or LevelTypeKind.Val
            => Zones.VahValTicks ?? Zones.ValueAreaOrPocTicks
               ?? throw new InvalidOperationException("Zones.VahValTicks required"),
        LevelTypeKind.Poc or LevelTypeKind.Npoc
            => Zones.PocTicks ?? Zones.ValueAreaOrPocTicks
               ?? throw new InvalidOperationException("Zones.PocTicks required"),
        LevelTypeKind.CompositeBoundary
            => Zones.CompositeTicks ?? throw new InvalidOperationException("Zones.CompositeTicks required"),
        _ => throw new InvalidOperationException($"OUT OF SCOPE level type: {kind}")
    };

    public static IndependentSourceFamily FamilyFor(LevelTypeKind kind) => kind switch
    {
        LevelTypeKind.Pdh or LevelTypeKind.Pdl or LevelTypeKind.Onh or LevelTypeKind.Onl
            or LevelTypeKind.Ibh or LevelTypeKind.Ibl
            => IndependentSourceFamily.SESSION_EXTREME,
        LevelTypeKind.Vah or LevelTypeKind.Val or LevelTypeKind.Poc or LevelTypeKind.Npoc
            => IndependentSourceFamily.VOLUME_PROFILE,
        LevelTypeKind.SinglePrintEdge
            => IndependentSourceFamily.TPO_STRUCTURE,
        LevelTypeKind.CompositeBoundary
            => IndependentSourceFamily.MULTISESSION_COMPOSITE,
        LevelTypeKind.WeeklyHigh or LevelTypeKind.WeeklyLow
            => IndependentSourceFamily.WEEKLY_STRUCTURE,
        _ => throw new InvalidOperationException($"OUT OF SCOPE: {kind}")
    };

    public static StructuralGrade StructuralFor(LevelTypeKind kind) => kind switch
    {
        LevelTypeKind.Pdh or LevelTypeKind.Pdl or LevelTypeKind.Onh or LevelTypeKind.Onl
            or LevelTypeKind.Vah or LevelTypeKind.Val or LevelTypeKind.Ibh or LevelTypeKind.Ibl
            or LevelTypeKind.WeeklyHigh or LevelTypeKind.WeeklyLow
            or LevelTypeKind.CompositeBoundary
            => StructuralGrade.A,
        LevelTypeKind.Npoc or LevelTypeKind.SinglePrintEdge
            => StructuralGrade.B,
        _ => StructuralGrade.C
    };

    /// <summary>Normalize price onto TickSize grid using decimal arithmetic.</summary>
    public decimal NormalizeToTick(decimal price)
    {
        var ticks = decimal.Round(price / TickSize, 0, MidpointRounding.AwayFromZero);
        return ticks * TickSize;
    }
}

public sealed class LevelEngineSection
{
    public required double ValueAreaVolumePercent { get; init; }
    public required ValueAreaAlgorithm ValueAreaAlgorithm { get; init; }
    public required PocTieBreak PocTieBreak { get; init; }
    public required int CompositeLookbackSessions { get; init; }
    public required CompositeBoundaryAlgorithm CompositeBoundaryAlgorithm { get; init; }
    public required string CompositeBoundaryAlgorithmVersion { get; init; }
    public required ExhaustedPolicy ExhaustedPolicy { get; init; }
    public required bool SinglePrintEdgesEnabled { get; init; }
    public required string SinglePrintStatus { get; init; }
    public required bool AllowMidSessionLevelCreation { get; init; }
    /// <summary>seed value, subject to sensitivity test — nPOC lookback sessions.</summary>
    public required int NPocLookbackSessions { get; init; }
}

/// <summary>Prompt 5A — session identity parameters. All required; FAIL STARTUP if missing.</summary>
public sealed class TradingSessionIdentitySection
{
    public required string TradingSessionRolloverLocalTime { get; init; }
    public required string TradingTimeZone { get; init; }
    public required string RthStartLocalTime { get; init; }
    public required string RthEndLocalTime { get; init; }
    /// <summary>seed value, subject to sensitivity test — max missing RTH minutes before PARTIAL.</summary>
    public required int PartialCoverageMaxMissingMinutes { get; init; }
}

public sealed class HistoryCapabilitiesSection
{
    public required HistoryCapabilityRequirement PdhPdl { get; init; }
    public required HistoryCapabilityRequirement OnhOnl { get; init; }
    public required HistoryCapabilityRequirement WeeklyHl { get; init; }
    public required HistoryCapabilityRequirement Composite { get; init; }
    public required HistoryCapabilityRequirement Npoc { get; init; }
    public required HistoryCapabilityRequirement RotationR { get; init; }
}

public sealed class HistoryCapabilityRequirement
{
    public int? MinCompletedRthSessions { get; init; }
    public int? MinCompletedOvernightSessions { get; init; }
    public bool RequireChartTimeframeM1 { get; init; }
    public bool Required { get; init; }
}

public sealed class SessionsSection
{
    public required IReadOnlyList<SessionDefinition> Definitions { get; init; }
}

public sealed class SessionDefinition
{
    public required string Name { get; init; }
    public required string Start { get; init; }
    public required string End { get; init; }
    public required SessionDefinitionType SessionDefinitionType { get; init; }
    public bool WrapsMidnight { get; init; }
}

public sealed class RotationSection
{
    public required string VolatilityWindowStart { get; init; }
    public required string VolatilityWindowEnd { get; init; }
    public required double TrueRangeMultiplier { get; init; }
    public required int ClampMinTicks { get; init; }
    public required int ClampMaxTicks { get; init; }
    public required string FreezeTime { get; init; }
    public required string TrueRangeTimeframe { get; init; }
}

public sealed class InteractionSection
{
    public required int ResetTicksFloor { get; init; }
    public required bool ResetTicksUsesMaxWithRotationR { get; init; }
    public required double ResetTimeMinutes { get; init; }
    public double? MaxInteractionTimeMinutes { get; init; }
    public bool? ExitDistanceEqualsRotationR { get; init; }
}

public sealed class ZonesSection
{
    public required bool WidthsAreTotalTicks { get; init; }
    public required int ExtremeTicks { get; init; }
    public int? VahValTicks { get; init; }
    public int? PocTicks { get; init; }
    public int? ValueAreaOrPocTicks { get; init; }
    public int? CompositeTicks { get; init; }
    public required int ClusterMaxTicks { get; init; }
    public required int ClusterGapTicks { get; init; }
}

public sealed class OutcomeSection
{
    public required OutcomeReferencePrice ReferencePrice { get; init; }
    public required bool RecordExcursionTicks { get; init; }
    public required bool RecordExcursionR { get; init; }
    public required IReadOnlyList<string> Horizons { get; init; }
    public required bool SupportTruncationReason { get; init; }
}
