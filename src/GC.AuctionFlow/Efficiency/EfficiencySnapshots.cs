using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Orderflow;
using GC.AuctionFlow.Reference;

namespace GC.AuctionFlow.Efficiency;

public sealed class AuctionEfficiencyEvidenceSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public AuctionEfficiencyEvidenceSnapshot(
        string snapshotId,
        string policyVersion,
        EfficiencyScopeType scopeType,
        string primaryAuctionId,
        string? episodeId,
        string? referenceId,
        ReferenceInteractionRole? referenceRole,
        string instrumentIdentity,
        string dataEpoch,
        decimal tickSize,
        string timestampPolicy,
        EfficiencyModuleState measurementStatus,
        EfficiencyClassificationState classificationState,
        DateTime observationStartedAtUtc,
        DateTime? firstInputAtUtc,
        DateTime? lastInputAtUtc,
        OrderflowCoverageMode coverageMode,
        EfficiencyResultDirection resultDirection,
        AuctionEffortEvidenceVector effort,
        AuctionResultEvidenceVector result,
        AuctionEfficiencyRawRelationships rawRelationships,
        long stateVersion,
        long eventRevision,
        EfficiencyDataQuality dataQuality,
        EfficiencyAvailability availability,
        IReadOnlyList<string> limitations,
        string inputFingerprint,
        bool isFrozen)
    {
        SnapshotId = snapshotId ?? "";
        PolicyVersion = policyVersion ?? AuctionEfficiencyEvidencePolicyConfig.PolicyVersion;
        ScopeType = scopeType;
        PrimaryAuctionId = primaryAuctionId ?? "";
        EpisodeId = episodeId;
        ReferenceId = referenceId;
        ReferenceRole = referenceRole;
        InstrumentIdentity = instrumentIdentity ?? "";
        DataEpoch = dataEpoch ?? "";
        TickSize = tickSize;
        TimestampPolicy = timestampPolicy ?? "";
        MeasurementStatus = measurementStatus;
        ClassificationState = classificationState;
        ObservationStartedAtUtc = observationStartedAtUtc;
        FirstInputAtUtc = firstInputAtUtc;
        LastInputAtUtc = lastInputAtUtc;
        CoverageMode = coverageMode;
        ResultDirection = resultDirection;
        Effort = effort ?? throw new ArgumentNullException(nameof(effort));
        Result = result ?? throw new ArgumentNullException(nameof(result));
        RawRelationships = rawRelationships ?? throw new ArgumentNullException(nameof(rawRelationships));
        StateVersion = stateVersion;
        EventRevision = eventRevision;
        DataQuality = dataQuality;
        Availability = availability;
        Limitations = limitations ?? Array.Empty<string>();
        InputFingerprint = inputFingerprint ?? "";
        IsFrozen = isFrozen;
    }

    public string SnapshotId { get; }
    public string PolicyVersion { get; }
    public EfficiencyScopeType ScopeType { get; }
    public string PrimaryAuctionId { get; }
    public string? EpisodeId { get; }
    public string? ReferenceId { get; }
    public ReferenceInteractionRole? ReferenceRole { get; }
    public string InstrumentIdentity { get; }
    public string DataEpoch { get; }
    public decimal TickSize { get; }
    public string TimestampPolicy { get; }
    public EfficiencyModuleState MeasurementStatus { get; }
    public EfficiencyClassificationState ClassificationState { get; }
    public DateTime ObservationStartedAtUtc { get; }
    public DateTime? FirstInputAtUtc { get; }
    public DateTime? LastInputAtUtc { get; }
    public OrderflowCoverageMode CoverageMode { get; }
    public EfficiencyResultDirection ResultDirection { get; }
    public AuctionEffortEvidenceVector Effort { get; }
    public AuctionResultEvidenceVector Result { get; }
    public AuctionEfficiencyRawRelationships RawRelationships { get; }
    public long StateVersion { get; }
    public long EventRevision { get; }
    public EfficiencyDataQuality DataQuality { get; }
    public EfficiencyAvailability Availability { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string InputFingerprint { get; }
    public bool IsFrozen { get; }
    public string Version => SnapshotVersion;
}

public sealed class AuctionEfficiencyEvidenceSetSnapshot
{
    public const string SnapshotVersion = "1.0.0";
    public const int RecentlyClosedEpisodeCapacity = 64;

    public AuctionEfficiencyEvidenceSetSnapshot(
        EfficiencyModuleState moduleState,
        string policyVersion,
        AuctionEfficiencyEvidenceSnapshot? currentAuctionEvidence,
        IReadOnlyList<AuctionEfficiencyEvidenceSnapshot> activeEpisodeEvidence,
        IReadOnlyList<AuctionEfficiencyEvidenceSnapshot> recentlyClosedEpisodeEvidence,
        AuctionEfficiencyEvidenceSnapshot? latestUpdatedEvidence,
        int readyCount,
        int partialCount,
        int invalidCount,
        EfficiencyInputFingerprint? inputFingerprint,
        long rejectedStaleCount,
        string? lastRejectionReason,
        DateTime createdAtUtc,
        DateTime lastUpdatedAtUtc,
        IReadOnlyList<string> limitations)
    {
        ModuleState = moduleState;
        PolicyVersion = policyVersion ?? AuctionEfficiencyEvidencePolicyConfig.PolicyVersion;
        CurrentAuctionEvidence = currentAuctionEvidence;
        ActiveEpisodeEvidence = activeEpisodeEvidence ?? Array.Empty<AuctionEfficiencyEvidenceSnapshot>();
        RecentlyClosedEpisodeEvidence = recentlyClosedEpisodeEvidence ?? Array.Empty<AuctionEfficiencyEvidenceSnapshot>();
        LatestUpdatedEvidence = latestUpdatedEvidence;
        ReadyCount = readyCount;
        PartialCount = partialCount;
        InvalidCount = invalidCount;
        InputFingerprint = inputFingerprint;
        RejectedStaleCount = rejectedStaleCount;
        LastRejectionReason = lastRejectionReason;
        CreatedAtUtc = createdAtUtc;
        LastUpdatedAtUtc = lastUpdatedAtUtc;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public EfficiencyModuleState ModuleState { get; }
    public string PolicyVersion { get; }
    public AuctionEfficiencyEvidenceSnapshot? CurrentAuctionEvidence { get; }
    public IReadOnlyList<AuctionEfficiencyEvidenceSnapshot> ActiveEpisodeEvidence { get; }
    public IReadOnlyList<AuctionEfficiencyEvidenceSnapshot> RecentlyClosedEpisodeEvidence { get; }
    public AuctionEfficiencyEvidenceSnapshot? LatestUpdatedEvidence { get; }
    public int ReadyCount { get; }
    public int PartialCount { get; }
    public int InvalidCount { get; }
    public EfficiencyInputFingerprint? InputFingerprint { get; }
    public long RejectedStaleCount { get; }
    public string? LastRejectionReason { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime LastUpdatedAtUtc { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;
}
