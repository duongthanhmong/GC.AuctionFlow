namespace GC.AuctionFlow.Directional;

/// <summary>Deterministic pairwise auction comparison evidence. Exact-tick; no inferred volume.</summary>
public sealed class PairwiseAuctionComparisonEvidence
{
    public const string EvidenceVersion = "1.0.0";

    public PairwiseAuctionComparisonEvidence(
        string previousAuctionId,
        string currentAuctionId,
        decimal? previousTpoPoc,
        decimal? currentTpoPoc,
        decimal? previousTpoVah,
        decimal? previousTpoVal,
        decimal? currentTpoVah,
        decimal? currentTpoVal,
        decimal? previousVpoc,
        decimal? currentVpoc,
        decimal? previousVolumeVah,
        decimal? previousVolumeVal,
        decimal? currentVolumeVah,
        decimal? currentVolumeVal,
        MigrationDirection tpoValueMidpointMigration,
        MigrationDirection volumeValueMidpointMigration,
        ValueRelationship tpoValueRelationship,
        ValueRelationship volumeValueRelationship,
        MigrationDirection tpoPocMigration,
        MigrationDirection volumePocMigration,
        MigrationDirection rangeRelationship,
        bool exactVolumeAvailable,
        bool compatible,
        IReadOnlyList<string> conflicts,
        IReadOnlyList<string> limitations,
        DirectionalAuctionState classifiedState)
    {
        PreviousAuctionId = previousAuctionId ?? "";
        CurrentAuctionId = currentAuctionId ?? "";
        PreviousTpoPoc = previousTpoPoc;
        CurrentTpoPoc = currentTpoPoc;
        PreviousTpoVah = previousTpoVah;
        PreviousTpoVal = previousTpoVal;
        CurrentTpoVah = currentTpoVah;
        CurrentTpoVal = currentTpoVal;
        PreviousVpoc = previousVpoc;
        CurrentVpoc = currentVpoc;
        PreviousVolumeVah = previousVolumeVah;
        PreviousVolumeVal = previousVolumeVal;
        CurrentVolumeVah = currentVolumeVah;
        CurrentVolumeVal = currentVolumeVal;
        TpoValueMidpointMigration = tpoValueMidpointMigration;
        VolumeValueMidpointMigration = volumeValueMidpointMigration;
        TpoValueRelationship = tpoValueRelationship;
        VolumeValueRelationship = volumeValueRelationship;
        TpoPocMigration = tpoPocMigration;
        VolumePocMigration = volumePocMigration;
        RangeRelationship = rangeRelationship;
        ExactVolumeAvailable = exactVolumeAvailable;
        Compatible = compatible;
        Conflicts = conflicts ?? Array.Empty<string>();
        Limitations = limitations ?? Array.Empty<string>();
        ClassifiedState = classifiedState;
    }

    public string PreviousAuctionId { get; }
    public string CurrentAuctionId { get; }
    public decimal? PreviousTpoPoc { get; }
    public decimal? CurrentTpoPoc { get; }
    public decimal? PreviousTpoVah { get; }
    public decimal? PreviousTpoVal { get; }
    public decimal? CurrentTpoVah { get; }
    public decimal? CurrentTpoVal { get; }
    public decimal? PreviousVpoc { get; }
    public decimal? CurrentVpoc { get; }
    public decimal? PreviousVolumeVah { get; }
    public decimal? PreviousVolumeVal { get; }
    public decimal? CurrentVolumeVah { get; }
    public decimal? CurrentVolumeVal { get; }
    public MigrationDirection TpoValueMidpointMigration { get; }
    public MigrationDirection VolumeValueMidpointMigration { get; }
    public ValueRelationship TpoValueRelationship { get; }
    public ValueRelationship VolumeValueRelationship { get; }
    public MigrationDirection TpoPocMigration { get; }
    public MigrationDirection VolumePocMigration { get; }
    public MigrationDirection RangeRelationship { get; }
    public bool ExactVolumeAvailable { get; }
    public bool Compatible { get; }
    public IReadOnlyList<string> Conflicts { get; }
    public IReadOnlyList<string> Limitations { get; }
    public DirectionalAuctionState ClassifiedState { get; }
    public string Version => EvidenceVersion;
}
