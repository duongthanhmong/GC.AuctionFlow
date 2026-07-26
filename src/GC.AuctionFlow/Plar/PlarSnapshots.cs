using GC.AuctionFlow.Reference;

namespace GC.AuctionFlow.Plar;

/// <summary>
/// One structural level on the projected path.
/// Distance is signed-free: it is always "ticks ahead", measured along the direction.
/// </summary>
public sealed class PathObstacleSnapshot
{
    public PathObstacleSnapshot(
        string referenceId,
        ReferenceType referenceType,
        ReferenceMaturity maturity,
        PathObstacleRole role,
        long distanceTicks,
        decimal zoneLow,
        decimal zoneHigh,
        int corridorIndex,
        BarrierPermeability permeability)
    {
        ReferenceId = referenceId ?? "";
        ReferenceType = referenceType;
        Maturity = maturity;
        Role = role;
        DistanceTicks = distanceTicks;
        ZoneLow = zoneLow;
        ZoneHigh = zoneHigh;
        CorridorIndex = corridorIndex;
        Permeability = permeability;
    }

    public string ReferenceId { get; }
    public ReferenceType ReferenceType { get; }
    public ReferenceMaturity Maturity { get; }
    public PathObstacleRole Role { get; }

    /// <summary>Ticks ahead along the path. Always >= 0.</summary>
    public long DistanceTicks { get; }

    public decimal ZoneLow { get; }
    public decimal ZoneHigh { get; }

    /// <summary>0-based position in the corridor: 0 = Barrier 1.</summary>
    public int CorridorIndex { get; }

    /// <summary>Always NotCalibrated — friction needs reaction history.</summary>
    public BarrierPermeability Permeability { get; }

    public bool IsTarget =>
        Role is PathObstacleRole.Target or PathObstacleRole.TargetAndBarrier;

    public bool IsBarrier =>
        Role is PathObstacleRole.Barrier or PathObstacleRole.TargetAndBarrier;
}

/// <summary>
/// The projected path in one direction from the current price.
/// Ordering and distances are pure geometry; nothing here estimates whether a barrier
/// will actually hold.
/// </summary>
public sealed class AuctionPathSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public AuctionPathSnapshot(
        string pathId,
        string policyVersion,
        PathDirection direction,
        long currentPriceTick,
        IReadOnlyList<PathObstacleSnapshot> obstacles,
        PathObstacleSnapshot? nearestBarrier,
        PathObstacleSnapshot? nearestTarget,
        PathObstacleSnapshot? finalTarget,
        TargetSpaceAvailability targetSpaceAvailability,
        long? remainingTargetSpaceTicks,
        PlarDataQuality dataQuality,
        DateTime observedAtUtc,
        IReadOnlyList<string> limitations)
    {
        PathId = pathId ?? "";
        PolicyVersion = policyVersion ?? PlarPolicyConfig.PolicyVersion;
        Direction = direction;
        CurrentPriceTick = currentPriceTick;
        Obstacles = obstacles ?? Array.Empty<PathObstacleSnapshot>();
        NearestBarrier = nearestBarrier;
        NearestTarget = nearestTarget;
        FinalTarget = finalTarget;
        TargetSpaceAvailability = targetSpaceAvailability;
        RemainingTargetSpaceTicks = remainingTargetSpaceTicks;
        DataQuality = dataQuality;
        ObservedAtUtc = observedAtUtc;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public string PathId { get; }
    public string PolicyVersion { get; }
    public PathDirection Direction { get; }
    public long CurrentPriceTick { get; }

    /// <summary>Ordered nearest-first along the direction.</summary>
    public IReadOnlyList<PathObstacleSnapshot> Obstacles { get; }

    public PathObstacleSnapshot? NearestBarrier { get; }
    public PathObstacleSnapshot? NearestTarget { get; }
    public PathObstacleSnapshot? FinalTarget { get; }

    /// <summary>
    /// Distinguishes "no room ahead" from "cannot measure" — the distinction
    /// G-LOC-002 depends on.
    /// </summary>
    public TargetSpaceAvailability TargetSpaceAvailability { get; }

    /// <summary>
    /// Ticks to the nearest target ahead. Null when availability is Unavailable;
    /// 0 when references exist but nothing lies ahead.
    /// </summary>
    public long? RemainingTargetSpaceTicks { get; }

    public PlarDataQuality DataQuality { get; }
    public DateTime ObservedAtUtc { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;

    /// <summary>v1.2 §33.2: Barrier 1..3 ahead of the final target.</summary>
    public IReadOnlyList<PathObstacleSnapshot> Corridor =>
        Obstacles.Where(o => o.IsBarrier)
                 .Take(PlarPolicyConfig.CorridorBarrierCapacity)
                 .ToArray();
}

/// <summary>
/// Immutable Phase 3E PLAR set: one path per direction from the current price.
/// </summary>
public sealed class PlarSetSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public PlarSetSnapshot(
        PlarModuleState moduleState,
        string policyVersion,
        AuctionPathSnapshot? upPath,
        AuctionPathSnapshot? downPath,
        int referenceCount,
        DateTime createdAtUtc,
        DateTime lastUpdatedAtUtc,
        IReadOnlyList<string> limitations)
    {
        ModuleState = moduleState;
        PolicyVersion = policyVersion ?? PlarPolicyConfig.PolicyVersion;
        UpPath = upPath;
        DownPath = downPath;
        ReferenceCount = referenceCount;
        CreatedAtUtc = createdAtUtc;
        LastUpdatedAtUtc = lastUpdatedAtUtc;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public PlarModuleState ModuleState { get; }
    public string PolicyVersion { get; }
    public AuctionPathSnapshot? UpPath { get; }
    public AuctionPathSnapshot? DownPath { get; }
    public int ReferenceCount { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime LastUpdatedAtUtc { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;

    /// <summary>Path for a thesis direction, or null when the direction is unknown.</summary>
    public AuctionPathSnapshot? PathFor(PathDirection direction) => direction switch
    {
        PathDirection.Up => UpPath,
        PathDirection.Down => DownPath,
        _ => null
    };
}
