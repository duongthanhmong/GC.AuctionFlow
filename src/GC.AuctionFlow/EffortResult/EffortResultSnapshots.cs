using GC.AuctionFlow.Efficiency;

namespace GC.AuctionFlow.EffortResult;

/// <summary>
/// Immutable Phase 2E classification snapshot for a single efficiency evidence scope.
/// Classification always NotCalibrated — thresholds not yet calibrated.
/// </summary>
public sealed class EffortResultClassificationSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public EffortResultClassificationSnapshot(
        string classificationId,
        string policyVersion,
        string efficiencySnapshotId,
        string primaryAuctionId,
        string? episodeId,
        string? referenceId,
        EfficiencyScopeType scopeType,
        EffortResultClassificationState classification,
        EffortResultDataQuality dataQuality,
        long stateVersion,
        long eventRevision,
        DateTime classifiedAtUtc,
        IReadOnlyList<string> limitations)
    {
        ClassificationId = classificationId ?? "";
        PolicyVersion = policyVersion ?? EffortResultClassifierPolicyConfig.PolicyVersion;
        EfficiencySnapshotId = efficiencySnapshotId ?? "";
        PrimaryAuctionId = primaryAuctionId ?? "";
        EpisodeId = episodeId;
        ReferenceId = referenceId;
        ScopeType = scopeType;
        Classification = classification;
        DataQuality = dataQuality;
        StateVersion = stateVersion;
        EventRevision = eventRevision;
        ClassifiedAtUtc = classifiedAtUtc;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public string ClassificationId { get; }
    public string PolicyVersion { get; }
    public string EfficiencySnapshotId { get; }
    public string PrimaryAuctionId { get; }
    public string? EpisodeId { get; }
    public string? ReferenceId { get; }
    public EfficiencyScopeType ScopeType { get; }
    public EffortResultClassificationState Classification { get; }
    public EffortResultDataQuality DataQuality { get; }
    public long StateVersion { get; }
    public long EventRevision { get; }
    public DateTime ClassifiedAtUtc { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;
}

/// <summary>
/// Immutable Phase 2E set-level snapshot: current auction + active episodes + closed episodes.
/// </summary>
public sealed class EffortResultClassificationSetSnapshot
{
    public const string SnapshotVersion = "1.0.0";
    public const int RecentlyClosedCapacity = 64;

    public EffortResultClassificationSetSnapshot(
        EffortResultModuleState moduleState,
        string policyVersion,
        EffortResultClassificationSnapshot? currentAuctionClassification,
        IReadOnlyList<EffortResultClassificationSnapshot> activeEpisodeClassifications,
        IReadOnlyList<EffortResultClassificationSnapshot> recentlyClosedClassifications,
        EffortResultClassificationSnapshot? latestUpdated,
        int readyCount,
        int partialCount,
        DateTime createdAtUtc,
        DateTime lastUpdatedAtUtc,
        IReadOnlyList<string> limitations)
    {
        ModuleState = moduleState;
        PolicyVersion = policyVersion ?? EffortResultClassifierPolicyConfig.PolicyVersion;
        CurrentAuctionClassification = currentAuctionClassification;
        ActiveEpisodeClassifications = activeEpisodeClassifications ?? Array.Empty<EffortResultClassificationSnapshot>();
        RecentlyClosedClassifications = recentlyClosedClassifications ?? Array.Empty<EffortResultClassificationSnapshot>();
        LatestUpdated = latestUpdated;
        ReadyCount = readyCount;
        PartialCount = partialCount;
        CreatedAtUtc = createdAtUtc;
        LastUpdatedAtUtc = lastUpdatedAtUtc;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public EffortResultModuleState ModuleState { get; }
    public string PolicyVersion { get; }
    public EffortResultClassificationSnapshot? CurrentAuctionClassification { get; }
    public IReadOnlyList<EffortResultClassificationSnapshot> ActiveEpisodeClassifications { get; }
    public IReadOnlyList<EffortResultClassificationSnapshot> RecentlyClosedClassifications { get; }
    public EffortResultClassificationSnapshot? LatestUpdated { get; }
    public int ReadyCount { get; }
    public int PartialCount { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime LastUpdatedAtUtc { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;
}
