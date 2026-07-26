using GC.AuctionFlow.Efficiency;

namespace GC.AuctionFlow.Facilitation;

/// <summary>
/// Immutable Phase 2F trade facilitation snapshot for a single efficiency evidence scope.
/// Classification always NotCalibrated — TradeFacilitationIndex thresholds not yet calibrated.
/// Raw index components stored for future calibration research.
/// </summary>
public sealed class TradeFacilitationSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public TradeFacilitationSnapshot(
        string snapshotId,
        string policyVersion,
        string efficiencySnapshotId,
        string primaryAuctionId,
        string? episodeId,
        string? referenceId,
        EfficiencyScopeType scopeType,
        TradeFacilitationClassificationState classification,
        TradeFacilitationDataQuality dataQuality,
        decimal? directionConsistentEffortVolume,
        decimal? directionConsistentEffortRatio,
        long? achievedFavorableProgressTicks,
        decimal? favorableProgressPerDirectionUnit,
        long stateVersion,
        long eventRevision,
        DateTime classifiedAtUtc,
        IReadOnlyList<string> limitations)
    {
        SnapshotId = snapshotId ?? "";
        PolicyVersion = policyVersion ?? TradeFacilitationPolicyConfig.PolicyVersion;
        EfficiencySnapshotId = efficiencySnapshotId ?? "";
        PrimaryAuctionId = primaryAuctionId ?? "";
        EpisodeId = episodeId;
        ReferenceId = referenceId;
        ScopeType = scopeType;
        Classification = classification;
        DataQuality = dataQuality;
        DirectionConsistentEffortVolume = directionConsistentEffortVolume;
        DirectionConsistentEffortRatio = directionConsistentEffortRatio;
        AchievedFavorableProgressTicks = achievedFavorableProgressTicks;
        FavorableProgressPerDirectionUnit = favorableProgressPerDirectionUnit;
        StateVersion = stateVersion;
        EventRevision = eventRevision;
        ClassifiedAtUtc = classifiedAtUtc;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public string SnapshotId { get; }
    public string PolicyVersion { get; }
    public string EfficiencySnapshotId { get; }
    public string PrimaryAuctionId { get; }
    public string? EpisodeId { get; }
    public string? ReferenceId { get; }
    public EfficiencyScopeType ScopeType { get; }
    public TradeFacilitationClassificationState Classification { get; }
    public TradeFacilitationDataQuality DataQuality { get; }

    /// <summary>Ask volume if direction Up, Bid volume if direction Down, null if direction unknown.</summary>
    public decimal? DirectionConsistentEffortVolume { get; }

    /// <summary>Direction-consistent volume / total classified volume. Research raw value.</summary>
    public decimal? DirectionConsistentEffortRatio { get; }

    /// <summary>MaximumFavorableProgressTicks from efficiency result vector.</summary>
    public long? AchievedFavorableProgressTicks { get; }

    /// <summary>Ticks per direction-consistent contract. Research raw value for future calibration.</summary>
    public decimal? FavorableProgressPerDirectionUnit { get; }

    public long StateVersion { get; }
    public long EventRevision { get; }
    public DateTime ClassifiedAtUtc { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;
}

/// <summary>Immutable Phase 2F set-level snapshot: current auction + active episodes + closed episodes.</summary>
public sealed class TradeFacilitationSetSnapshot
{
    public const string SnapshotVersion = "1.0.0";
    public const int RecentlyClosedCapacity = 64;

    public TradeFacilitationSetSnapshot(
        TradeFacilitationModuleState moduleState,
        string policyVersion,
        TradeFacilitationSnapshot? currentAuctionFacilitation,
        IReadOnlyList<TradeFacilitationSnapshot> activeEpisodeFacilitations,
        IReadOnlyList<TradeFacilitationSnapshot> recentlyClosedFacilitations,
        TradeFacilitationSnapshot? latestUpdated,
        int readyCount,
        int partialCount,
        DateTime createdAtUtc,
        DateTime lastUpdatedAtUtc,
        IReadOnlyList<string> limitations)
    {
        ModuleState = moduleState;
        PolicyVersion = policyVersion ?? TradeFacilitationPolicyConfig.PolicyVersion;
        CurrentAuctionFacilitation = currentAuctionFacilitation;
        ActiveEpisodeFacilitations = activeEpisodeFacilitations ?? Array.Empty<TradeFacilitationSnapshot>();
        RecentlyClosedFacilitations = recentlyClosedFacilitations ?? Array.Empty<TradeFacilitationSnapshot>();
        LatestUpdated = latestUpdated;
        ReadyCount = readyCount;
        PartialCount = partialCount;
        CreatedAtUtc = createdAtUtc;
        LastUpdatedAtUtc = lastUpdatedAtUtc;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public TradeFacilitationModuleState ModuleState { get; }
    public string PolicyVersion { get; }
    public TradeFacilitationSnapshot? CurrentAuctionFacilitation { get; }
    public IReadOnlyList<TradeFacilitationSnapshot> ActiveEpisodeFacilitations { get; }
    public IReadOnlyList<TradeFacilitationSnapshot> RecentlyClosedFacilitations { get; }
    public TradeFacilitationSnapshot? LatestUpdated { get; }
    public int ReadyCount { get; }
    public int PartialCount { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime LastUpdatedAtUtc { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;
}
