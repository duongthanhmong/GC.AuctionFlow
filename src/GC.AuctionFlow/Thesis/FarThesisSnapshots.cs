namespace GC.AuctionFlow.Thesis;

/// <summary>
/// Immutable Phase 3 FAR thesis snapshot for one evidence scope.
/// Observable state capped at ReentryDeveloping — Armed and beyond NOT CALIBRATED.
/// </summary>
public sealed class FarThesisSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public FarThesisSnapshot(
        string thesisId,
        string policyVersion,
        string evidenceId,
        string episodeId,
        string primaryAuctionId,
        string referenceId,
        ThesisDirection direction,
        FarState farState,
        bool notCalibrated,
        int attemptCount,
        ThesisDataQuality dataQuality,
        long stateVersion,
        long eventRevision,
        DateTime observedAtUtc,
        IReadOnlyList<string> limitations)
    {
        ThesisId = thesisId ?? "";
        PolicyVersion = policyVersion ?? FarThesisPolicyConfig.PolicyVersion;
        EvidenceId = evidenceId ?? "";
        EpisodeId = episodeId ?? "";
        PrimaryAuctionId = primaryAuctionId ?? "";
        ReferenceId = referenceId ?? "";
        Direction = direction;
        FarState = farState;
        NotCalibrated = notCalibrated;
        AttemptCount = attemptCount;
        DataQuality = dataQuality;
        StateVersion = stateVersion;
        EventRevision = eventRevision;
        ObservedAtUtc = observedAtUtc;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public string ThesisId { get; }
    public string PolicyVersion { get; }
    public string EvidenceId { get; }
    public string EpisodeId { get; }
    public string PrimaryAuctionId { get; }
    public string ReferenceId { get; }
    public ThesisDirection Direction { get; }
    public FarState FarState { get; }
    public bool NotCalibrated { get; }
    public int AttemptCount { get; }
    public ThesisDataQuality DataQuality { get; }
    public long StateVersion { get; }
    public long EventRevision { get; }
    public DateTime ObservedAtUtc { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;
}

/// <summary>
/// Immutable Phase 3 FAR thesis set snapshot — active + closed scopes.
/// ArmableCount and ExecutableCount always 0 — NOT CALIBRATED.
/// </summary>
public sealed class FarThesisSetSnapshot
{
    public const string SnapshotVersion = "1.0.0";
    public const int RecentlyClosedCapacity = 64;

    public FarThesisSetSnapshot(
        ThesisModuleState moduleState,
        string policyVersion,
        IReadOnlyList<FarThesisSnapshot> activeTheses,
        IReadOnlyList<FarThesisSnapshot> recentlyClosedTheses,
        FarThesisSnapshot? latestUpdated,
        int armableCount,
        int executableCount,
        DateTime createdAtUtc,
        DateTime lastUpdatedAtUtc,
        IReadOnlyList<string> limitations)
    {
        ModuleState = moduleState;
        PolicyVersion = policyVersion ?? FarThesisPolicyConfig.PolicyVersion;
        ActiveTheses = activeTheses ?? Array.Empty<FarThesisSnapshot>();
        RecentlyClosedTheses = recentlyClosedTheses ?? Array.Empty<FarThesisSnapshot>();
        LatestUpdated = latestUpdated;
        ArmableCount = armableCount;
        ExecutableCount = executableCount;
        CreatedAtUtc = createdAtUtc;
        LastUpdatedAtUtc = lastUpdatedAtUtc;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public ThesisModuleState ModuleState { get; }
    public string PolicyVersion { get; }
    public IReadOnlyList<FarThesisSnapshot> ActiveTheses { get; }
    public IReadOnlyList<FarThesisSnapshot> RecentlyClosedTheses { get; }
    public FarThesisSnapshot? LatestUpdated { get; }
    public int ArmableCount { get; }
    public int ExecutableCount { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime LastUpdatedAtUtc { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;
}
