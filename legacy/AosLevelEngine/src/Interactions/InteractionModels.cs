using Aos.LevelEngine.Levels;
using Aos.LevelEngine.Profiles;

namespace Aos.LevelEngine.Interactions;

public enum ApproachDirection { FROM_ABOVE, FROM_BELOW, UNKNOWN }

public enum ClampSide { None, Min, Max }

public enum InteractionState { ARMED, ACTIVE, RESET_PENDING, CLOSED }

public enum InteractionCloseReason
{
    ExitDistance,
    MaxInteractionTime,
    SessionEnd,
    MarketDataInvalid,
    ProcessRestart,
    ProcessShutdown
}

public enum HorizonType
{
    M5,
    M15,
    M30,
    NEXT_INTERACTION_SAME_LEVEL,
    SESSION_END
}

public enum OutcomeStatus
{
    COMPLETE,
    TRUNCATED,
    PENDING
}

public sealed class RotationRState
{
    public required int RotationRTicks { get; init; }
    public required DateTime FrozenAtExchange { get; init; }
    public required string RSourceBreakdown { get; init; }
    public required ClampSide ClampSide { get; init; }
    public required bool MacroEventInVolWindow { get; init; }
    public bool IsFrozen { get; init; } = true;
}

public static class RotationRCalculator
{
    public static RotationRState Freeze(
        InstrumentProfile profile,
        IReadOnlyList<decimal> m1TrueRangesTicks,
        DateTime freezeTimeExchange,
        bool macroEventInVolWindow)
    {
        if (m1TrueRangesTicks.Count == 0)
            throw new ArgumentException("Need M1 true ranges in volatility window.");

        var sorted = m1TrueRangesTicks.OrderBy(x => x).ToList();
        var mid = sorted.Count / 2;
        var median = sorted.Count % 2 == 1
            ? sorted[mid]
            : (sorted[mid - 1] + sorted[mid]) / 2m;

        // seed value, subject to sensitivity test (Rotation.TrueRangeMultiplier)
        var raw = (decimal)profile.Rotation.TrueRangeMultiplier * median;
        var rounded = (int)decimal.Round(raw, 0, MidpointRounding.AwayFromZero);
        var clampSide = ClampSide.None;
        var r = rounded;
        // seed value, subject to sensitivity test (Rotation.ClampMinTicks / ClampMaxTicks)
        if (r < profile.Rotation.ClampMinTicks) { r = profile.Rotation.ClampMinTicks; clampSide = ClampSide.Min; }
        else if (r > profile.Rotation.ClampMaxTicks) { r = profile.Rotation.ClampMaxTicks; clampSide = ClampSide.Max; }

        return new RotationRState
        {
            RotationRTicks = r,
            FrozenAtExchange = freezeTimeExchange,
            RSourceBreakdown =
                $"medianM1TR={median:F2}; mult={profile.Rotation.TrueRangeMultiplier}; raw={raw:F2}; rounded={rounded}",
            ClampSide = clampSide,
            MacroEventInVolWindow = macroEventInVolWindow,
            IsFrozen = true
        };
    }
}

/// <summary>Stream sequence range — never spans two ProcessInstanceIds.</summary>
public sealed class StreamRange
{
    public required string StreamType { get; init; }
    public required Guid ProcessInstanceId { get; init; }
    public long? LocalSequenceStart { get; init; }
    public long? LocalSequenceEnd { get; init; }
    public string? SpoolSegmentId { get; init; }
}

public sealed class LevelInteractionRecord
{
    public required Guid InteractionId { get; init; }
    public required Guid SessionId { get; init; }
    public required Guid ProcessInstanceId { get; init; }
    public required string DeclaredDataSourceMode { get; init; }
    public required string InstrumentProfileId { get; init; }
    public required string LevelEngineVersion { get; init; }
    public required string InterpretationVersion { get; init; }
    public required string ContractCode { get; init; }

    public required Guid LevelId { get; init; }
    public required string LevelType { get; init; }
    public required string StructuralGrade { get; init; }
    public string? EffectiveGrade { get; init; }
    public required string FreshnessState { get; init; }
    public required int ConfluenceCount { get; init; }
    public required decimal ZoneUpper { get; init; }
    public required decimal ZoneLower { get; init; }
    public required decimal ZoneMidPrice { get; init; }
    public required DateTime FrozenAtExchangeTime { get; init; }
    public Guid? ClusterId { get; init; }
    public required bool IsOverwideCluster { get; init; }
    public Guid? ConcurrentInteractionGroupId { get; init; }

    public required DateTime StartTimeExchange { get; init; }
    public required DateTime EndTimeExchange { get; init; }
    public required long MonotonicStartTicks { get; init; }
    public required long MonotonicEndTicks { get; init; }

    public required ApproachDirection ApproachDirection { get; init; }
    public string? StartReason { get; init; }
    public required double ApproachSpeedTicksPerMin { get; init; }
    public int? PenetrationDepthTicks { get; init; }
    public required int MaxExcursionInsideZone { get; init; }

    public required long VolumeInZone { get; init; }
    public required long VolumeAbove { get; init; }
    public required long VolumeBelow { get; init; }
    public required int TradeCount { get; init; }

    public StreamRange? NewTradeRange { get; init; }

    public required byte MarketDataValidity { get; init; }
    public required byte TimingDiagnosticValidity { get; init; }

    public required decimal ReferencePrice { get; init; }
    public required string CloseReason { get; init; }
}

public sealed class InteractionOutcomeRecord
{
    public required Guid InteractionId { get; init; }
    public required int OutcomeVersion { get; init; }
    public required HorizonType HorizonType { get; init; }
    public DateTime? HorizonTargetExchangeTime { get; init; }
    public required DateTime ObservedUntilExchangeTime { get; init; }
    public required decimal ReferencePrice { get; init; }
    public int? UpExcursionTicks { get; init; }
    public int? DownExcursionTicks { get; init; }
    public double? UpExcursionR { get; init; }
    public double? DownExcursionR { get; init; }
    public required OutcomeStatus OutcomeStatus { get; init; }
    public string? TruncationReason { get; init; }
    public required string ComputedByVersion { get; init; }
}

public static class ReferencePriceSelector
{
    public static decimal? Select(ApproachDirection direction, FrozenZone zone) =>
        direction switch
        {
            ApproachDirection.FROM_BELOW => zone.ZoneLower,
            ApproachDirection.FROM_ABOVE => zone.ZoneUpper,
            ApproachDirection.UNKNOWN => null, // session open inside — no invented edge
            _ => throw new ArgumentOutOfRangeException(nameof(direction))
        };

    /// <summary>When UNKNOWN, store mid for schema non-null; mark StartReason separately.</summary>
    public static decimal SelectOrMid(ApproachDirection direction, FrozenZone zone) =>
        Select(direction, zone) ?? zone.MidPrice;
}

/// <summary>Append-only outcome store — never updates rows.</summary>
public sealed class InteractionOutcomeStore
{
    private readonly List<InteractionOutcomeRecord> _rows = new();
    private readonly Dictionary<(Guid, HorizonType), int> _versions = new();

    public IReadOnlyList<InteractionOutcomeRecord> All => _rows;

    public InteractionOutcomeRecord Append(
        Guid interactionId,
        HorizonType horizon,
        DateTime? targetEx,
        DateTime observedUntilEx,
        decimal referencePrice,
        int? upTicks,
        int? downTicks,
        double? upR,
        double? downR,
        OutcomeStatus status,
        string? truncationReason,
        string computedByVersion)
    {
        var key = (interactionId, horizon);
        _versions.TryGetValue(key, out var prev);
        var ver = prev + 1;
        _versions[key] = ver;
        var row = new InteractionOutcomeRecord
        {
            InteractionId = interactionId,
            OutcomeVersion = ver,
            HorizonType = horizon,
            HorizonTargetExchangeTime = targetEx,
            ObservedUntilExchangeTime = observedUntilEx,
            ReferencePrice = referencePrice,
            UpExcursionTicks = upTicks,
            DownExcursionTicks = downTicks,
            UpExcursionR = upR,
            DownExcursionR = downR,
            OutcomeStatus = status,
            TruncationReason = truncationReason,
            ComputedByVersion = computedByVersion
        };
        _rows.Add(row);
        return row;
    }
}
