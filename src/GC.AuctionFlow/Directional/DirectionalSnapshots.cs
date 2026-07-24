namespace GC.AuctionFlow.Directional;

public sealed class ProfileLocationContextSnapshot
{
    public ProfileLocationContextSnapshot(
        PriceValueLocation currentPrimaryTpo,
        PriceValueLocation currentPrimaryVolume,
        PriceValueLocation previousPrimaryTpo,
        PriceValueLocation confirmedCompositeTpo,
        PriceValueLocation confirmedCompositeVolume)
    {
        CurrentPrimaryTpo = currentPrimaryTpo;
        CurrentPrimaryVolume = currentPrimaryVolume;
        PreviousPrimaryTpo = previousPrimaryTpo;
        ConfirmedCompositeTpo = confirmedCompositeTpo;
        ConfirmedCompositeVolume = confirmedCompositeVolume;
    }

    public PriceValueLocation CurrentPrimaryTpo { get; }
    public PriceValueLocation CurrentPrimaryVolume { get; }
    public PriceValueLocation PreviousPrimaryTpo { get; }
    public PriceValueLocation ConfirmedCompositeTpo { get; }
    public PriceValueLocation ConfirmedCompositeVolume { get; }
}

public sealed class OneTimeFramingSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public OneTimeFramingSnapshot(
        OneTimeFramingState state,
        string? sourcePrimaryAuctionId,
        string? tpoAnchor,
        int periodLengthMinutes,
        int completedPeriodCount,
        IReadOnlyList<string> lastTwoCompletedPeriodIds,
        int upStreak,
        int downStreak,
        long stateVersion,
        DateTime? lastCompletedPeriodTimestampUtc,
        IReadOnlyList<string> knownLimitations,
        long categoricalVersion)
    {
        State = state;
        SourcePrimaryAuctionId = sourcePrimaryAuctionId;
        TpoAnchor = tpoAnchor;
        PeriodLengthMinutes = periodLengthMinutes;
        CompletedPeriodCount = completedPeriodCount;
        LastTwoCompletedPeriodIds = lastTwoCompletedPeriodIds ?? Array.Empty<string>();
        UpStreak = upStreak;
        DownStreak = downStreak;
        StateVersion = stateVersion;
        LastCompletedPeriodTimestampUtc = lastCompletedPeriodTimestampUtc;
        KnownLimitations = knownLimitations ?? Array.Empty<string>();
        CategoricalVersion = categoricalVersion;
    }

    public OneTimeFramingState State { get; }
    public string? SourcePrimaryAuctionId { get; }
    public string? TpoAnchor { get; }
    public int PeriodLengthMinutes { get; }
    public int CompletedPeriodCount { get; }
    public IReadOnlyList<string> LastTwoCompletedPeriodIds { get; }
    public int UpStreak { get; }
    public int DownStreak { get; }
    public long StateVersion { get; }
    public DateTime? LastCompletedPeriodTimestampUtc { get; }
    public IReadOnlyList<string> KnownLimitations { get; }
    public long CategoricalVersion { get; }
    public string Version => SnapshotVersion;
}

public sealed class DirectionalHorizonContextSnapshot
{
    public DirectionalHorizonContextSnapshot(
        DirectionalHorizon horizon,
        DirectionalAuctionState state,
        DirectionalMaturity maturity,
        IReadOnlyList<string> sourceAuctionIds,
        ValueRelationship tpoValueMigration,
        ValueRelationship volumeValueMigration,
        MigrationDirection tpoPocMigration,
        MigrationDirection volumePocMigration,
        MigrationDirection tpoValueMidpointMigration,
        MigrationDirection volumeValueMidpointMigration,
        ProfileLocationContextSnapshot? priceLocation,
        OneTimeFramingState? oneTimeFramingState,
        IReadOnlyList<string> evidenceComponents,
        IReadOnlyList<string> conflicts,
        IReadOnlyList<string> knownLimitations,
        long stateVersion,
        PairwiseAuctionComparisonEvidence? primaryPairwise = null,
        int completedTransitionCount = 0)
    {
        Horizon = horizon;
        State = state;
        Maturity = maturity;
        SourceAuctionIds = sourceAuctionIds ?? Array.Empty<string>();
        TpoValueMigration = tpoValueMigration;
        VolumeValueMigration = volumeValueMigration;
        TpoPocMigration = tpoPocMigration;
        VolumePocMigration = volumePocMigration;
        TpoValueMidpointMigration = tpoValueMidpointMigration;
        VolumeValueMidpointMigration = volumeValueMidpointMigration;
        PriceLocation = priceLocation;
        OneTimeFramingState = oneTimeFramingState;
        EvidenceComponents = evidenceComponents ?? Array.Empty<string>();
        Conflicts = conflicts ?? Array.Empty<string>();
        KnownLimitations = knownLimitations ?? Array.Empty<string>();
        StateVersion = stateVersion;
        PrimaryPairwise = primaryPairwise;
        CompletedTransitionCount = completedTransitionCount;
    }

    public DirectionalHorizon Horizon { get; }
    public DirectionalAuctionState State { get; }
    public DirectionalMaturity Maturity { get; }
    public IReadOnlyList<string> SourceAuctionIds { get; }
    public ValueRelationship TpoValueMigration { get; }
    public ValueRelationship VolumeValueMigration { get; }
    public MigrationDirection TpoPocMigration { get; }
    public MigrationDirection VolumePocMigration { get; }
    public MigrationDirection TpoValueMidpointMigration { get; }
    public MigrationDirection VolumeValueMidpointMigration { get; }
    public ProfileLocationContextSnapshot? PriceLocation { get; }
    public OneTimeFramingState? OneTimeFramingState { get; }
    public IReadOnlyList<string> EvidenceComponents { get; }
    public IReadOnlyList<string> Conflicts { get; }
    public IReadOnlyList<string> KnownLimitations { get; }
    public long StateVersion { get; }
    public PairwiseAuctionComparisonEvidence? PrimaryPairwise { get; }
    public int CompletedTransitionCount { get; }
}

public sealed class ExecutionContextSnapshot
{
    public ExecutionContextSnapshot(
        ExecutionContextAvailability availability,
        DirectionalAuctionState state,
        IReadOnlyList<string> knownLimitations)
    {
        Availability = availability;
        State = state;
        KnownLimitations = knownLimitations ?? Array.Empty<string>();
    }

    public ExecutionContextAvailability Availability { get; }
    public DirectionalAuctionState State { get; }
    public IReadOnlyList<string> KnownLimitations { get; }
}

/// <summary>Immutable Directional Context set. Schema consumer: GcaeRuntimeSnapshot 0.5.0+.</summary>
public sealed class DirectionalContextSetSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public DirectionalContextSetSnapshot(
        DirectionalModuleState status,
        string policyVersion,
        string instrumentIdentity,
        string dataEpoch,
        decimal tickSize,
        string timestampPolicy,
        DirectionalHorizonContextSnapshot structuralContext,
        DirectionalHorizonContextSnapshot tacticalContext,
        OneTimeFramingSnapshot oneTimeFraming,
        ExecutionContextSnapshot executionContext,
        ProfileLocationContextSnapshot priceLocation,
        string inputFingerprint,
        long snapshotVersionToken,
        DateTime createdAtUtc,
        DateTime lastUpdatedAtUtc,
        IReadOnlyList<string> limitations)
    {
        Status = status;
        PolicyVersion = policyVersion ?? "";
        InstrumentIdentity = instrumentIdentity ?? "";
        DataEpoch = dataEpoch ?? "";
        TickSize = tickSize;
        TimestampPolicy = timestampPolicy ?? "";
        StructuralContext = structuralContext;
        TacticalContext = tacticalContext;
        OneTimeFraming = oneTimeFraming;
        ExecutionContext = executionContext;
        PriceLocation = priceLocation;
        InputFingerprint = inputFingerprint ?? "";
        SnapshotVersionToken = snapshotVersionToken;
        CreatedAtUtc = createdAtUtc;
        LastUpdatedAtUtc = lastUpdatedAtUtc;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public DirectionalModuleState Status { get; }
    public string PolicyVersion { get; }
    public string InstrumentIdentity { get; }
    public string DataEpoch { get; }
    public decimal TickSize { get; }
    public string TimestampPolicy { get; }
    public DirectionalHorizonContextSnapshot StructuralContext { get; }
    public DirectionalHorizonContextSnapshot TacticalContext { get; }
    public OneTimeFramingSnapshot OneTimeFraming { get; }
    public ExecutionContextSnapshot ExecutionContext { get; }
    public ProfileLocationContextSnapshot PriceLocation { get; }
    public string InputFingerprint { get; }
    public long SnapshotVersionToken { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime LastUpdatedAtUtc { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;
}
