using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Reference;

namespace GC.AuctionFlow.Evidence;

public sealed class AcceptanceReentryEvidenceSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public AcceptanceReentryEvidenceSnapshot(
        string evidenceId,
        string policyVersion,
        string episodeId,
        string primaryAuctionId,
        string referenceId,
        ReferenceType referenceType,
        ReferenceInteractionRole referenceRole,
        long referencePriceTick,
        decimal referencePrice,
        ReferenceSidePosition? canonicalOutsideDirection,
        EpisodeState episodeState,
        EpisodeResolution episodeResolution,
        EvidenceMeasurementStatus measurementStatus,
        AcceptanceObservationState acceptanceObservationState,
        ReentryObservationState reentryObservationState,
        DateTime startedAtUtc,
        DateTime lastUpdatedAtUtc,
        DateTime? firstOutsideAtUtc,
        DateTime? firstGeometricReentryAtUtc,
        DateTime? lastOutsideAtUtc,
        DateTime? lastInsideAtUtc,
        TimeSpan totalObservedDuration,
        decimal totalObservedExecutedVolume,
        long totalObservedTradeCount,
        AcceptanceEvidenceVector acceptance,
        ReentryEvidenceVector reentry,
        long stateVersion,
        long eventRevision,
        EpisodeDataQuality dataQuality,
        IReadOnlyDictionary<string, EvidenceComponentAvailability> evidenceAvailability,
        IReadOnlyList<string> limitations,
        string? directionalProvenance)
    {
        EvidenceId = evidenceId ?? "";
        PolicyVersion = policyVersion ?? "";
        EpisodeId = episodeId ?? "";
        PrimaryAuctionId = primaryAuctionId ?? "";
        ReferenceId = referenceId ?? "";
        ReferenceType = referenceType;
        ReferenceRole = referenceRole;
        ReferencePriceTick = referencePriceTick;
        ReferencePrice = referencePrice;
        CanonicalOutsideDirection = canonicalOutsideDirection;
        EpisodeState = episodeState;
        EpisodeResolution = episodeResolution;
        MeasurementStatus = measurementStatus;
        AcceptanceObservationState = acceptanceObservationState;
        ReentryObservationState = reentryObservationState;
        StartedAtUtc = startedAtUtc;
        LastUpdatedAtUtc = lastUpdatedAtUtc;
        FirstOutsideAtUtc = firstOutsideAtUtc;
        FirstGeometricReentryAtUtc = firstGeometricReentryAtUtc;
        LastOutsideAtUtc = lastOutsideAtUtc;
        LastInsideAtUtc = lastInsideAtUtc;
        TotalObservedDuration = totalObservedDuration;
        TotalObservedExecutedVolume = totalObservedExecutedVolume;
        TotalObservedTradeCount = totalObservedTradeCount;
        Acceptance = acceptance;
        Reentry = reentry;
        StateVersion = stateVersion;
        EventRevision = eventRevision;
        DataQuality = dataQuality;
        EvidenceAvailability = evidenceAvailability ?? new Dictionary<string, EvidenceComponentAvailability>();
        Limitations = limitations ?? Array.Empty<string>();
        DirectionalProvenance = directionalProvenance;
    }

    public string EvidenceId { get; }
    public string PolicyVersion { get; }
    public string EpisodeId { get; }
    public string PrimaryAuctionId { get; }
    public string ReferenceId { get; }
    public ReferenceType ReferenceType { get; }
    public ReferenceInteractionRole ReferenceRole { get; }
    public long ReferencePriceTick { get; }
    public decimal ReferencePrice { get; }
    public ReferenceSidePosition? CanonicalOutsideDirection { get; }
    public EpisodeState EpisodeState { get; }
    public EpisodeResolution EpisodeResolution { get; }
    public EvidenceMeasurementStatus MeasurementStatus { get; }
    public AcceptanceObservationState AcceptanceObservationState { get; }
    public ReentryObservationState ReentryObservationState { get; }
    public DateTime StartedAtUtc { get; }
    public DateTime LastUpdatedAtUtc { get; }
    public DateTime? FirstOutsideAtUtc { get; }
    public DateTime? FirstGeometricReentryAtUtc { get; }
    public DateTime? LastOutsideAtUtc { get; }
    public DateTime? LastInsideAtUtc { get; }
    public TimeSpan TotalObservedDuration { get; }
    public decimal TotalObservedExecutedVolume { get; }
    public long TotalObservedTradeCount { get; }
    public AcceptanceEvidenceVector Acceptance { get; }
    public ReentryEvidenceVector Reentry { get; }
    public long StateVersion { get; }
    public long EventRevision { get; }
    public EpisodeDataQuality DataQuality { get; }
    public IReadOnlyDictionary<string, EvidenceComponentAvailability> EvidenceAvailability { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string? DirectionalProvenance { get; }
    public string Version => SnapshotVersion;
}

public sealed class AcceptanceReentryEvidenceSetSnapshot
{
    public const string SnapshotVersion = "1.0.0";
    public const int RecentlyClosedCapacity = 64;

    public AcceptanceReentryEvidenceSetSnapshot(
        EvidenceModuleState moduleState,
        string policyVersion,
        string primaryAuctionId,
        IReadOnlyList<AcceptanceReentryEvidenceSnapshot> activeEvidence,
        IReadOnlyList<AcceptanceReentryEvidenceSnapshot> recentlyClosedEvidence,
        AcceptanceReentryEvidenceSnapshot? latestUpdatedEvidence,
        IReadOnlyDictionary<AcceptanceObservationState, int> countsByAcceptanceObservation,
        IReadOnlyDictionary<ReentryObservationState, int> countsByReentryObservation,
        int availableComponentCount,
        int unavailableComponentCount,
        string inputFingerprint,
        long registryRevision,
        DateTime createdAtUtc,
        DateTime lastUpdatedAtUtc,
        IReadOnlyList<string> limitations)
    {
        ModuleState = moduleState;
        PolicyVersion = policyVersion ?? "";
        PrimaryAuctionId = primaryAuctionId ?? "";
        ActiveEvidence = activeEvidence ?? Array.Empty<AcceptanceReentryEvidenceSnapshot>();
        RecentlyClosedEvidence = recentlyClosedEvidence ?? Array.Empty<AcceptanceReentryEvidenceSnapshot>();
        LatestUpdatedEvidence = latestUpdatedEvidence;
        CountsByAcceptanceObservation = countsByAcceptanceObservation ?? new Dictionary<AcceptanceObservationState, int>();
        CountsByReentryObservation = countsByReentryObservation ?? new Dictionary<ReentryObservationState, int>();
        AvailableComponentCount = availableComponentCount;
        UnavailableComponentCount = unavailableComponentCount;
        InputFingerprint = inputFingerprint ?? "";
        RegistryRevision = registryRevision;
        CreatedAtUtc = createdAtUtc;
        LastUpdatedAtUtc = lastUpdatedAtUtc;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public EvidenceModuleState ModuleState { get; }
    public string PolicyVersion { get; }
    public string PrimaryAuctionId { get; }
    public IReadOnlyList<AcceptanceReentryEvidenceSnapshot> ActiveEvidence { get; }
    public IReadOnlyList<AcceptanceReentryEvidenceSnapshot> RecentlyClosedEvidence { get; }
    public AcceptanceReentryEvidenceSnapshot? LatestUpdatedEvidence { get; }
    public IReadOnlyDictionary<AcceptanceObservationState, int> CountsByAcceptanceObservation { get; }
    public IReadOnlyDictionary<ReentryObservationState, int> CountsByReentryObservation { get; }
    public int AvailableComponentCount { get; }
    public int UnavailableComponentCount { get; }
    public string InputFingerprint { get; }
    public long RegistryRevision { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime LastUpdatedAtUtc { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;
}
