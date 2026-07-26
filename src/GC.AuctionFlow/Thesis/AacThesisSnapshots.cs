namespace GC.AuctionFlow.Thesis;

/// <summary>
/// Immutable Phase 3 AAC thesis snapshot for one evidence scope.
/// Observable state capped at OutsideAttempt — AcceptanceDeveloping and beyond NOT CALIBRATED.
/// </summary>
public sealed class AacThesisSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public AacThesisSnapshot(
        string thesisId,
        string policyVersion,
        string evidenceId,
        string episodeId,
        string primaryAuctionId,
        string referenceId,
        ThesisDirection direction,
        AacState aacState,
        bool notCalibrated,
        int attemptCount,
        ThesisDataQuality dataQuality,
        long stateVersion,
        long eventRevision,
        DateTime observedAtUtc,
        IReadOnlyList<string> limitations)
    {
        ThesisId = thesisId ?? "";
        PolicyVersion = policyVersion ?? AacThesisPolicyConfig.PolicyVersion;
        EvidenceId = evidenceId ?? "";
        EpisodeId = episodeId ?? "";
        PrimaryAuctionId = primaryAuctionId ?? "";
        ReferenceId = referenceId ?? "";
        Direction = direction;
        AacState = aacState;
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
    public AacState AacState { get; }
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
/// Immutable Phase 3 AAC thesis set snapshot — active + closed scopes.
/// ArmableCount and ExecutableCount always 0 — NOT CALIBRATED.
/// </summary>
public sealed class AacThesisSetSnapshot
{
    public const string SnapshotVersion = "1.0.0";
    public const int RecentlyClosedCapacity = 64;

    public AacThesisSetSnapshot(
        ThesisModuleState moduleState,
        string policyVersion,
        IReadOnlyList<AacThesisSnapshot> activeTheses,
        IReadOnlyList<AacThesisSnapshot> recentlyClosedTheses,
        AacThesisSnapshot? latestUpdated,
        int armableCount,
        int executableCount,
        DateTime createdAtUtc,
        DateTime lastUpdatedAtUtc,
        IReadOnlyList<string> limitations)
    {
        ModuleState = moduleState;
        PolicyVersion = policyVersion ?? AacThesisPolicyConfig.PolicyVersion;
        ActiveTheses = activeTheses ?? Array.Empty<AacThesisSnapshot>();
        RecentlyClosedTheses = recentlyClosedTheses ?? Array.Empty<AacThesisSnapshot>();
        LatestUpdated = latestUpdated;
        ArmableCount = armableCount;
        ExecutableCount = executableCount;
        CreatedAtUtc = createdAtUtc;
        LastUpdatedAtUtc = lastUpdatedAtUtc;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public ThesisModuleState ModuleState { get; }
    public string PolicyVersion { get; }
    public IReadOnlyList<AacThesisSnapshot> ActiveTheses { get; }
    public IReadOnlyList<AacThesisSnapshot> RecentlyClosedTheses { get; }
    public AacThesisSnapshot? LatestUpdated { get; }
    public int ArmableCount { get; }
    public int ExecutableCount { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime LastUpdatedAtUtc { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;
}
