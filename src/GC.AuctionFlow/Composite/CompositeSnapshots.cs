using GC.AuctionFlow.Profile;

namespace GC.AuctionFlow.Composite;

public sealed class ConfirmedCompositeProfileSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public ConfirmedCompositeProfileSnapshot(
        string compositeId,
        CompositePolicyMode policyMode,
        string policyVersion,
        string contractIdentity,
        string contractEpoch,
        decimal tickSize,
        string? anchorAuctionId,
        string? firstAuctionId,
        string? lastAuctionId,
        IReadOnlyList<string> includedAuctionIds,
        IReadOnlyList<string> excludedAuctionIds,
        int completedContributionCount,
        DateTime? compositeStartUtc,
        DateTime? compositeEndUtc,
        CompositeStatus compositeStatus,
        CompositeAggregateResult? aggregate,
        CompositeCapabilityState capability,
        CompositeEvidenceState evidenceState,
        IReadOnlyList<string> knownLimitations,
        string provenance)
    {
        CompositeId = compositeId;
        PolicyMode = policyMode;
        PolicyVersion = policyVersion;
        ContractIdentity = contractIdentity;
        ContractEpoch = contractEpoch;
        TickSize = tickSize;
        AnchorAuctionId = anchorAuctionId;
        FirstAuctionId = firstAuctionId;
        LastAuctionId = lastAuctionId;
        IncludedAuctionIds = includedAuctionIds ?? Array.Empty<string>();
        ExcludedAuctionIds = excludedAuctionIds ?? Array.Empty<string>();
        CompletedContributionCount = completedContributionCount;
        CompositeStartUtc = compositeStartUtc;
        CompositeEndUtc = compositeEndUtc;
        CompositeStatus = compositeStatus;
        Aggregate = aggregate;
        Capability = capability;
        EvidenceState = evidenceState;
        KnownLimitations = knownLimitations ?? Array.Empty<string>();
        Provenance = provenance;
    }

    public string CompositeId { get; }
    public CompositePolicyMode PolicyMode { get; }
    public string PolicyVersion { get; }
    public string ContractIdentity { get; }
    public string ContractEpoch { get; }
    public decimal TickSize { get; }
    public string? AnchorAuctionId { get; }
    public string? FirstAuctionId { get; }
    public string? LastAuctionId { get; }
    public IReadOnlyList<string> IncludedAuctionIds { get; }
    public IReadOnlyList<string> ExcludedAuctionIds { get; }
    public int CompletedContributionCount { get; }
    public DateTime? CompositeStartUtc { get; }
    public DateTime? CompositeEndUtc { get; }
    public CompositeStatus CompositeStatus { get; }
    public CompositeAggregateResult? Aggregate { get; }
    public CompositeCapabilityState Capability { get; }
    public CompositeEvidenceState EvidenceState { get; }
    public IReadOnlyList<string> KnownLimitations { get; }
    public string Provenance { get; }
    public string Version => SnapshotVersion;

    public decimal? ProfileHigh => Aggregate?.ProfileHigh;
    public decimal? ProfileLow => Aggregate?.ProfileLow;
    public decimal? TpoPoc => Aggregate?.TpoPoc;
    public decimal? VolumePoc => Aggregate?.VolumePoc;
    public decimal? TpoVah => Aggregate?.TpoVah;
    public decimal? TpoVal => Aggregate?.TpoVal;
    public decimal? VolumeVah => Aggregate?.VolumeVah;
    public decimal? VolumeVal => Aggregate?.VolumeVal;
    public int TotalTpoCount => Aggregate?.TotalTpoCount ?? 0;
    public decimal TotalExecutedVolume => Aggregate?.TotalExecutedVolume ?? 0m;
    public ProfileDataQuality DataQuality => Aggregate?.DataQuality ?? ProfileDataQuality.Unknown;
}

public sealed class DevelopingCompositePreviewSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public DevelopingCompositePreviewSnapshot(
        string baseCompositeId,
        string currentAuctionId,
        CompositeAggregateResult aggregate,
        decimal currentAuctionTpoShare,
        decimal currentAuctionVolumeShare,
        IReadOnlyList<string> knownLimitations)
    {
        BaseCompositeId = baseCompositeId;
        CurrentAuctionId = currentAuctionId;
        Aggregate = aggregate;
        CurrentAuctionTpoShare = currentAuctionTpoShare;
        CurrentAuctionVolumeShare = currentAuctionVolumeShare;
        KnownLimitations = knownLimitations ?? Array.Empty<string>();
    }

    public string BaseCompositeId { get; }
    public string CurrentAuctionId { get; }
    public CompositeAggregateResult Aggregate { get; }
    public decimal CurrentAuctionTpoShare { get; }
    public decimal CurrentAuctionVolumeShare { get; }
    public IReadOnlyList<string> KnownLimitations { get; }
    public string Version => SnapshotVersion;
    public bool IsPreview => true;
}

public sealed class CompositeSetSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public CompositeSetSnapshot(
        ConfirmedCompositeProfileSnapshot confirmed,
        DevelopingCompositePreviewSnapshot? preview,
        IReadOnlyList<CompositeMergeEvidence>? mergeEvidence,
        IReadOnlyList<string> transitionEvents)
    {
        Confirmed = confirmed;
        Preview = preview;
        MergeEvidence = mergeEvidence ?? Array.Empty<CompositeMergeEvidence>();
        TransitionEvents = transitionEvents ?? Array.Empty<string>();
    }

    public ConfirmedCompositeProfileSnapshot Confirmed { get; }
    public DevelopingCompositePreviewSnapshot? Preview { get; }
    public IReadOnlyList<CompositeMergeEvidence> MergeEvidence { get; }
    public IReadOnlyList<string> TransitionEvents { get; }
    public string Version => SnapshotVersion;
}
