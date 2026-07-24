using GC.AuctionFlow.Core;

namespace GC.AuctionFlow.Reference;

/// <summary>Immutable structural reference object. Not a drawing line.</summary>
public sealed class StructuralReferenceSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    /// <summary>
    /// Non-authoritative desired-payload marker for registry reconciliation drafts.
    /// Registry owns monotonic StateVersion assignment.
    /// </summary>
    public const int RegistryAssignedStateVersion = 0;

    public StructuralReferenceSnapshot(
        string referenceId,
        ReferenceType referenceType,
        decimal zoneLow,
        decimal zoneHigh,
        long zoneLowTick,
        long zoneHighTick,
        string sourceId,
        ReferenceSourceKind sourceKind,
        ReferenceSourceHorizon sourceHorizon,
        DateTime sourceCreatedAtUtc,
        ReferenceMaturity maturity,
        ReferenceStatus status,
        ReferenceEvidenceTier evidenceTier,
        decimal tickSize,
        string dataEpoch,
        string timestampPolicyVersion,
        int stateVersion,
        DateTime createdAtUtc,
        DateTime lastUpdatedAtUtc,
        IReadOnlyDictionary<string, string>? features = null,
        EvidenceProvenance provenance = EvidenceProvenance.ObservedApi)
    {
        if (string.IsNullOrWhiteSpace(referenceId))
            throw new ArgumentException("ReferenceId required.", nameof(referenceId));
        if (tickSize <= 0m)
            throw new ArgumentOutOfRangeException(nameof(tickSize));
        if (zoneLowTick > zoneHighTick)
            throw new ArgumentException("ZoneLowTick must be <= ZoneHighTick.");
        // 0 = non-authoritative desired payload (registry assigns). Published entries use >= 1.
        if (stateVersion < 0)
            throw new ArgumentOutOfRangeException(nameof(stateVersion));

        ReferenceId = referenceId;
        ReferenceType = referenceType;
        ZoneLow = zoneLow;
        ZoneHigh = zoneHigh;
        ZoneLowTick = zoneLowTick;
        ZoneHighTick = zoneHighTick;
        SourceId = sourceId ?? throw new ArgumentNullException(nameof(sourceId));
        SourceKind = sourceKind;
        SourceHorizon = sourceHorizon;
        SourceCreatedAtUtc = sourceCreatedAtUtc;
        Maturity = maturity;
        Status = status;
        EvidenceTier = evidenceTier;
        TickSize = tickSize;
        DataEpoch = dataEpoch ?? throw new ArgumentNullException(nameof(dataEpoch));
        TimestampPolicyVersion = timestampPolicyVersion ?? throw new ArgumentNullException(nameof(timestampPolicyVersion));
        StateVersion = stateVersion;
        CreatedAtUtc = createdAtUtc;
        LastUpdatedAtUtc = lastUpdatedAtUtc;
        Features = features ?? new Dictionary<string, string>();
        Provenance = provenance;
    }

    public string ReferenceId { get; }
    public ReferenceType ReferenceType { get; }
    public decimal ZoneLow { get; }
    public decimal ZoneHigh { get; }
    public long ZoneLowTick { get; }
    public long ZoneHighTick { get; }
    public string SourceId { get; }
    public ReferenceSourceKind SourceKind { get; }
    public ReferenceSourceHorizon SourceHorizon { get; }
    public DateTime SourceCreatedAtUtc { get; }
    public ReferenceMaturity Maturity { get; }
    public ReferenceStatus Status { get; }
    public ReferenceEvidenceTier EvidenceTier { get; }
    public decimal TickSize { get; }
    public string DataEpoch { get; }
    public string TimestampPolicyVersion { get; }
    public int StateVersion { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime LastUpdatedAtUtc { get; }
    public IReadOnlyDictionary<string, string> Features { get; }
    public EvidenceProvenance Provenance { get; }
    public string Version => SnapshotVersion;

    /// <summary>Phase 1C: TestCount not evaluated (always 0).</summary>
    public int TestCount => 0;

    public StructuralReferenceSnapshot WithLifecycle(
        ReferenceStatus status,
        int stateVersion,
        DateTime lastUpdatedAtUtc,
        decimal? zoneLow = null,
        decimal? zoneHigh = null,
        long? zoneLowTick = null,
        long? zoneHighTick = null) =>
        new(
            ReferenceId,
            ReferenceType,
            zoneLow ?? ZoneLow,
            zoneHigh ?? ZoneHigh,
            zoneLowTick ?? ZoneLowTick,
            zoneHighTick ?? ZoneHighTick,
            SourceId,
            SourceKind,
            SourceHorizon,
            SourceCreatedAtUtc,
            Maturity,
            status,
            EvidenceTier,
            TickSize,
            DataEpoch,
            TimestampPolicyVersion,
            stateVersion,
            CreatedAtUtc,
            lastUpdatedAtUtc,
            Features,
            Provenance);

    public bool SameNormalizedZone(StructuralReferenceSnapshot other) =>
        ZoneLowTick == other.ZoneLowTick && ZoneHighTick == other.ZoneHighTick;

    public bool SameImmutablePayload(StructuralReferenceSnapshot other) =>
        string.Equals(ReferenceId, other.ReferenceId, StringComparison.Ordinal)
        && ReferenceType == other.ReferenceType
        && Maturity == other.Maturity
        && SameNormalizedZone(other)
        && string.Equals(SourceId, other.SourceId, StringComparison.Ordinal)
        && SourceKind == other.SourceKind
        && TickSize == other.TickSize
        && string.Equals(DataEpoch, other.DataEpoch, StringComparison.Ordinal)
        && string.Equals(TimestampPolicyVersion, other.TimestampPolicyVersion, StringComparison.Ordinal);
}

/// <summary>Exact-tick confluence group. No strength score.</summary>
public sealed class ReferenceConfluenceGroup
{
    public ReferenceConfluenceGroup(
        decimal zoneLow,
        decimal zoneHigh,
        long zoneLowTick,
        long zoneHighTick,
        IReadOnlyList<string> constituentReferenceIds,
        IReadOnlyList<ReferenceType> constituentTypes,
        int confirmedCount,
        int developingCount,
        IReadOnlyList<ReferenceSourceHorizon> sourceHorizons)
    {
        ZoneLow = zoneLow;
        ZoneHigh = zoneHigh;
        ZoneLowTick = zoneLowTick;
        ZoneHighTick = zoneHighTick;
        ConstituentReferenceIds = constituentReferenceIds ?? Array.Empty<string>();
        ConstituentTypes = constituentTypes ?? Array.Empty<ReferenceType>();
        ConfirmedCount = confirmedCount;
        DevelopingCount = developingCount;
        SourceHorizons = sourceHorizons ?? Array.Empty<ReferenceSourceHorizon>();
    }

    public decimal ZoneLow { get; }
    public decimal ZoneHigh { get; }
    public long ZoneLowTick { get; }
    public long ZoneHighTick { get; }
    public IReadOnlyList<string> ConstituentReferenceIds { get; }
    public IReadOnlyList<ReferenceType> ConstituentTypes { get; }
    public int ConstituentCount => ConstituentReferenceIds.Count;
    public int ConfirmedCount { get; }
    public int DevelopingCount { get; }
    public bool IsMixedMaturity => ConfirmedCount > 0 && DevelopingCount > 0;
    public IReadOnlyList<ReferenceSourceHorizon> SourceHorizons { get; }
}

/// <summary>Descriptive nearest-reference view. No support/resistance or directional bias.</summary>
public sealed class NearestReferenceView
{
    public NearestReferenceView(
        decimal? currentPrice,
        long? currentPriceTick,
        IReadOnlyList<StructuralReferenceSnapshot> nearestBelow,
        IReadOnlyList<StructuralReferenceSnapshot> nearestAbove,
        IReadOnlyList<StructuralReferenceSnapshot> containingExact,
        long? distanceBelowTicks,
        long? distanceAboveTicks)
    {
        CurrentPrice = currentPrice;
        CurrentPriceTick = currentPriceTick;
        NearestBelow = nearestBelow ?? Array.Empty<StructuralReferenceSnapshot>();
        NearestAbove = nearestAbove ?? Array.Empty<StructuralReferenceSnapshot>();
        ContainingExact = containingExact ?? Array.Empty<StructuralReferenceSnapshot>();
        DistanceBelowTicks = distanceBelowTicks;
        DistanceAboveTicks = distanceAboveTicks;
    }

    public decimal? CurrentPrice { get; }
    public long? CurrentPriceTick { get; }
    public IReadOnlyList<StructuralReferenceSnapshot> NearestBelow { get; }
    public IReadOnlyList<StructuralReferenceSnapshot> NearestAbove { get; }
    public IReadOnlyList<StructuralReferenceSnapshot> ContainingExact { get; }
    public long? DistanceBelowTicks { get; }
    public long? DistanceAboveTicks { get; }
}

/// <summary>Module-level Structural Reference set published to runtime.</summary>
public sealed class StructuralReferenceSetSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public StructuralReferenceSetSnapshot(
        StructuralReferenceModuleState moduleState,
        string policyVersion,
        IReadOnlyList<StructuralReferenceSnapshot> confirmedReferences,
        IReadOnlyList<StructuralReferenceSnapshot> developingReferences,
        IReadOnlyList<StructuralReferenceSnapshot> retiredOrExpired,
        IReadOnlyList<ReferenceConfluenceGroup> confluenceGroups,
        NearestReferenceView? nearest,
        string inputFingerprint,
        long registryRevision,
        IReadOnlyList<string> knownLimitations,
        IReadOnlyList<string> unavailableVolumeReasons,
        DateTime publishedAtUtc)
    {
        ModuleState = moduleState;
        PolicyVersion = policyVersion ?? ReferencePolicyConfig.PolicyVersion;
        ConfirmedReferences = confirmedReferences ?? Array.Empty<StructuralReferenceSnapshot>();
        DevelopingReferences = developingReferences ?? Array.Empty<StructuralReferenceSnapshot>();
        RetiredOrExpired = retiredOrExpired ?? Array.Empty<StructuralReferenceSnapshot>();
        ConfluenceGroups = confluenceGroups ?? Array.Empty<ReferenceConfluenceGroup>();
        Nearest = nearest;
        InputFingerprint = inputFingerprint ?? "";
        RegistryRevision = registryRevision;
        KnownLimitations = knownLimitations ?? Array.Empty<string>();
        UnavailableVolumeReasons = unavailableVolumeReasons ?? Array.Empty<string>();
        PublishedAtUtc = publishedAtUtc;
    }

    public StructuralReferenceModuleState ModuleState { get; }
    public string PolicyVersion { get; }
    public IReadOnlyList<StructuralReferenceSnapshot> ConfirmedReferences { get; }
    public IReadOnlyList<StructuralReferenceSnapshot> DevelopingReferences { get; }
    public IReadOnlyList<StructuralReferenceSnapshot> RetiredOrExpired { get; }
    public IReadOnlyList<ReferenceConfluenceGroup> ConfluenceGroups { get; }
    public NearestReferenceView? Nearest { get; }
    public string InputFingerprint { get; }
    public long RegistryRevision { get; }
    public IReadOnlyList<string> KnownLimitations { get; }
    public IReadOnlyList<string> UnavailableVolumeReasons { get; }
    public DateTime PublishedAtUtc { get; }
    public string Version => SnapshotVersion;

    public IReadOnlyList<StructuralReferenceSnapshot> ActiveReferences
    {
        get
        {
            var list = new List<StructuralReferenceSnapshot>(ConfirmedReferences.Count + DevelopingReferences.Count);
            list.AddRange(ConfirmedReferences);
            list.AddRange(DevelopingReferences);
            return list;
        }
    }
}
