using Aos.LevelEngine.Profiles;

namespace Aos.LevelEngine.Levels;

/// <summary>Immutable zone geometry. Mutation of bounds throws.</summary>
public sealed class FrozenZone
{
    public Guid LevelId { get; }
    public LevelTypeKind LevelType { get; }
    public LevelBatch Batch { get; }
    public IndependentSourceFamily SourceFamily { get; }
    public string? SourceSession { get; }
    public decimal SourcePrice { get; }
    public decimal MidPrice { get; }
    public decimal ZoneLower { get; }
    public decimal ZoneUpper { get; }
    public int WidthTicks { get; }
    public DateTime FrozenAtExchangeTime { get; }
    public DateTime FrozenAtUtc { get; }
    public StructuralGrade StructuralGrade { get; }
    public int CompletedInteractionCount { get; private set; }
    public FreshnessState FreshnessState { get; private set; }
    public StructuralGrade? EffectiveGrade { get; private set; }
    public bool IsInactive { get; private set; }
    public int ConfluenceCount { get; private set; }
    public Guid? ClusterId { get; private set; }
    public bool IsOverwideCluster { get; private set; }
    public LevelOmissionReason OmissionReason { get; }

    private bool _clusterAssigned;
    private readonly ExhaustedPolicy _exhaustedPolicy;

    public FrozenZone(
        Guid levelId,
        LevelTypeKind levelType,
        LevelBatch batch,
        IndependentSourceFamily sourceFamily,
        string? sourceSession,
        decimal sourcePrice,
        decimal midPrice,
        decimal zoneLower,
        decimal zoneUpper,
        int widthTicks,
        DateTime frozenAtExchangeTime,
        DateTime frozenAtUtc,
        StructuralGrade structuralGrade,
        ExhaustedPolicy exhaustedPolicy,
        LevelOmissionReason omission = LevelOmissionReason.None)
    {
        if (zoneUpper < zoneLower)
            throw new ArgumentException("ZoneUpper must be >= ZoneLower");
        LevelId = levelId;
        LevelType = levelType;
        Batch = batch;
        SourceFamily = sourceFamily;
        SourceSession = sourceSession;
        SourcePrice = sourcePrice;
        MidPrice = midPrice;
        ZoneLower = zoneLower;
        ZoneUpper = zoneUpper;
        WidthTicks = widthTicks;
        FrozenAtExchangeTime = frozenAtExchangeTime;
        FrozenAtUtc = frozenAtUtc;
        StructuralGrade = structuralGrade;
        _exhaustedPolicy = exhaustedPolicy;
        OmissionReason = omission;
        CompletedInteractionCount = 0;
        ApplyEffectiveFromCount();
    }

    public bool ContainsTradePrice(decimal price) => price >= ZoneLower && price <= ZoneUpper;

    public void RecordCompletedInteraction()
    {
        CompletedInteractionCount++;
        ApplyEffectiveFromCount();
    }

    /// <summary>
    /// Pure function of (StructuralGrade, completed prior interactions, ExhaustedPolicy).
    /// Level Engine does not know win/lose — only completed interaction count.
    /// </summary>
    public static (FreshnessState freshness, StructuralGrade? effective, bool inactive)
        ComputeEffective(StructuralGrade structural, int completedCount, ExhaustedPolicy policy)
    {
        return completedCount switch
        {
            0 => (FreshnessState.FRESH, structural, false),
            1 => (FreshnessState.VALID, structural, false),
            2 => (FreshnessState.DEGRADED, CapGradeAtMost(structural, StructuralGrade.B), false),
            _ => policy == ExhaustedPolicy.INACTIVE
                ? (FreshnessState.EXHAUSTED, null, true)
                : (FreshnessState.EXHAUSTED, CapGradeAtMost(structural, StructuralGrade.C), false)
        };
    }

    /// <summary>A best … C worst. Cap at most B means A→B, B→B, C→C.</summary>
    public static StructuralGrade CapGradeAtMost(StructuralGrade g, StructuralGrade atMost) =>
        (StructuralGrade)Math.Max((int)g, (int)atMost);

    private void ApplyEffectiveFromCount()
    {
        var (f, e, inactive) = ComputeEffective(StructuralGrade, CompletedInteractionCount, _exhaustedPolicy);
        FreshnessState = f;
        EffectiveGrade = e;
        IsInactive = inactive;
    }

    public void SetConfluenceCount(int count) => ConfluenceCount = count;

    internal void ClearClusterAssignment()
    {
        ClusterId = null;
        IsOverwideCluster = false;
        _clusterAssigned = false;
    }

    internal void AssignCluster(Guid clusterId, bool overwide)
    {
        if (_clusterAssigned)
            throw new InvalidOperationException("Cluster already assigned.");
        ClusterId = clusterId;
        IsOverwideCluster = overwide;
        _clusterAssigned = true;
    }

    public void TryMutateGeometry(decimal newLower, decimal newUpper) =>
        throw new InvalidOperationException(
            "Zone geometry is frozen at creation. Cannot widen, shift, or edit after FrozenAtExchangeTime.");
}

public sealed class LevelCluster
{
    public required Guid ClusterId { get; init; }
    public required IReadOnlyList<Guid> MemberLevelIds { get; init; }
    public required decimal ClusterLower { get; init; }
    public required decimal ClusterUpper { get; init; }
    public required int WidthTicks { get; init; }
    public required bool IsOverwide { get; init; }
}

public sealed class LevelOmission
{
    public required LevelTypeKind LevelType { get; init; }
    public required LevelOmissionReason Reason { get; init; }
    public string? Detail { get; init; }
}

public sealed class LevelManifestEntry
{
    public required Guid LevelId { get; init; }
    public required string LevelType { get; init; }
    public string? SourceSession { get; init; }
    public required decimal SourcePrice { get; init; }
    public required decimal ZoneLower { get; init; }
    public required decimal ZoneUpper { get; init; }
    public required DateTime FrozenAtExchangeTime { get; init; }
    public required string StructuralGrade { get; init; }
    public required string FreshnessState { get; init; }
    public string? OmissionReason { get; init; }
}
