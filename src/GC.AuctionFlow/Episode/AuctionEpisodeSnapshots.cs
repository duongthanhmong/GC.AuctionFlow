using GC.AuctionFlow.Reference;

namespace GC.AuctionFlow.Episode;

public sealed class AuctionEpisodeSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public AuctionEpisodeSnapshot(
        string episodeId,
        string policyVersion,
        string primaryAuctionId,
        string referenceId,
        ReferenceType referenceType,
        ReferenceInteractionRole referenceRole,
        long referencePriceTick,
        decimal referencePrice,
        ReferenceMaturity referenceMaturity,
        ReferenceSourceHorizon sourceHorizon,
        string? directionalContextProvenance,
        EpisodeInteractionDirection interactionDirection,
        EpisodeState state,
        EpisodeResolution resolution,
        DateTime startedAtUtc,
        DateTime lastUpdatedAtUtc,
        string firstInteractionEventId,
        string lastProcessedEventId,
        int attemptCount,
        int interactionCount,
        int crossCount,
        int upExcursionCount,
        int downExcursionCount,
        long maximumAboveDistanceTicks,
        long maximumBelowDistanceTicks,
        long? maximumCanonicalOutsideDistanceTicks,
        TimeSpan canonicalOutsideDuration,
        decimal canonicalOutsideExecutedVolume,
        int canonicalOutsideTradeCount,
        decimal? canonicalOutsideBidVolume,
        decimal? canonicalOutsideAskVolume,
        decimal? canonicalOutsideDelta,
        AggressorEvidenceAvailability aggressorEvidenceAvailability,
        decimal? localPoc,
        long stateVersion,
        long eventRevision,
        EpisodeDataQuality dataQuality,
        IReadOnlyList<string> limitations)
    {
        EpisodeId = episodeId ?? "";
        PolicyVersion = policyVersion ?? "";
        PrimaryAuctionId = primaryAuctionId ?? "";
        ReferenceId = referenceId ?? "";
        ReferenceType = referenceType;
        ReferenceRole = referenceRole;
        ReferencePriceTick = referencePriceTick;
        ReferencePrice = referencePrice;
        ReferenceMaturity = referenceMaturity;
        SourceHorizon = sourceHorizon;
        DirectionalContextProvenance = directionalContextProvenance;
        InteractionDirection = interactionDirection;
        State = state;
        Resolution = resolution;
        StartedAtUtc = startedAtUtc;
        LastUpdatedAtUtc = lastUpdatedAtUtc;
        FirstInteractionEventId = firstInteractionEventId ?? "";
        LastProcessedEventId = lastProcessedEventId ?? "";
        AttemptCount = attemptCount;
        InteractionCount = interactionCount;
        CrossCount = crossCount;
        UpExcursionCount = upExcursionCount;
        DownExcursionCount = downExcursionCount;
        MaximumAboveDistanceTicks = maximumAboveDistanceTicks;
        MaximumBelowDistanceTicks = maximumBelowDistanceTicks;
        MaximumCanonicalOutsideDistanceTicks = maximumCanonicalOutsideDistanceTicks;
        CanonicalOutsideDuration = canonicalOutsideDuration;
        CanonicalOutsideExecutedVolume = canonicalOutsideExecutedVolume;
        CanonicalOutsideTradeCount = canonicalOutsideTradeCount;
        CanonicalOutsideBidVolume = canonicalOutsideBidVolume;
        CanonicalOutsideAskVolume = canonicalOutsideAskVolume;
        CanonicalOutsideDelta = canonicalOutsideDelta;
        AggressorEvidenceAvailability = aggressorEvidenceAvailability;
        LocalPoc = localPoc;
        StateVersion = stateVersion;
        EventRevision = eventRevision;
        DataQuality = dataQuality;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public string EpisodeId { get; }
    public string PolicyVersion { get; }
    public string PrimaryAuctionId { get; }
    public string ReferenceId { get; }
    public ReferenceType ReferenceType { get; }
    public ReferenceInteractionRole ReferenceRole { get; }
    public long ReferencePriceTick { get; }
    public decimal ReferencePrice { get; }
    public ReferenceMaturity ReferenceMaturity { get; }
    public ReferenceSourceHorizon SourceHorizon { get; }
    public string? DirectionalContextProvenance { get; }
    public EpisodeInteractionDirection InteractionDirection { get; }
    public EpisodeState State { get; }
    public EpisodeResolution Resolution { get; }
    public DateTime StartedAtUtc { get; }
    public DateTime LastUpdatedAtUtc { get; }
    public string FirstInteractionEventId { get; }
    public string LastProcessedEventId { get; }
    public int AttemptCount { get; }
    public int InteractionCount { get; }
    public int CrossCount { get; }
    public int UpExcursionCount { get; }
    public int DownExcursionCount { get; }
    public long MaximumAboveDistanceTicks { get; }
    public long MaximumBelowDistanceTicks { get; }
    public long? MaximumCanonicalOutsideDistanceTicks { get; }
    public TimeSpan CanonicalOutsideDuration { get; }
    public decimal CanonicalOutsideExecutedVolume { get; }
    public int CanonicalOutsideTradeCount { get; }
    public decimal? CanonicalOutsideBidVolume { get; }
    public decimal? CanonicalOutsideAskVolume { get; }
    public decimal? CanonicalOutsideDelta { get; }
    public AggressorEvidenceAvailability AggressorEvidenceAvailability { get; }
    public decimal? LocalPoc { get; }
    public long StateVersion { get; }
    public long EventRevision { get; }
    public EpisodeDataQuality DataQuality { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;
}

public sealed class AuctionEpisodeSetSnapshot
{
    public const string SnapshotVersion = "1.0.0";
    public const int RecentlyClosedCapacity = 64;

    public AuctionEpisodeSetSnapshot(
        EpisodeModuleState moduleState,
        string policyVersion,
        EpisodeHistoryMode historyMode,
        string primaryAuctionId,
        int eligibleReferenceCount,
        int interactedReferenceCount,
        IReadOnlyList<AuctionEpisodeSnapshot> activeEpisodes,
        IReadOnlyList<AuctionEpisodeSnapshot> recentlyClosedEpisodes,
        AuctionEpisodeSnapshot? latestUpdatedEpisode,
        IReadOnlyDictionary<EpisodeState, int> episodeCountsByState,
        string inputFingerprint,
        long registryRevision,
        DateTime createdAtUtc,
        DateTime lastUpdatedAtUtc,
        IReadOnlyList<string> limitations,
        long tradeEventsSeen = 0,
        long tradeEventsAccepted = 0,
        long tradeEventsDuplicate = 0,
        long tradeEventsRejected = 0,
        string? lastTradeRejectReason = null,
        string? lastAcceptedTradeEventId = null,
        long? lastAcceptedTradeSequence = null)
    {
        ModuleState = moduleState;
        PolicyVersion = policyVersion ?? "";
        HistoryMode = historyMode;
        PrimaryAuctionId = primaryAuctionId ?? "";
        EligibleReferenceCount = eligibleReferenceCount;
        InteractedReferenceCount = interactedReferenceCount;
        ActiveEpisodes = activeEpisodes ?? Array.Empty<AuctionEpisodeSnapshot>();
        RecentlyClosedEpisodes = recentlyClosedEpisodes ?? Array.Empty<AuctionEpisodeSnapshot>();
        LatestUpdatedEpisode = latestUpdatedEpisode;
        EpisodeCountsByState = episodeCountsByState ?? new Dictionary<EpisodeState, int>();
        InputFingerprint = inputFingerprint ?? "";
        RegistryRevision = registryRevision;
        CreatedAtUtc = createdAtUtc;
        LastUpdatedAtUtc = lastUpdatedAtUtc;
        Limitations = limitations ?? Array.Empty<string>();
        TradeEventsSeen = tradeEventsSeen;
        TradeEventsAccepted = tradeEventsAccepted;
        TradeEventsDuplicate = tradeEventsDuplicate;
        TradeEventsRejected = tradeEventsRejected;
        LastTradeRejectReason = lastTradeRejectReason;
        LastAcceptedTradeEventId = lastAcceptedTradeEventId;
        LastAcceptedTradeSequence = lastAcceptedTradeSequence;
    }

    public EpisodeModuleState ModuleState { get; }
    public string PolicyVersion { get; }
    public EpisodeHistoryMode HistoryMode { get; }
    public string PrimaryAuctionId { get; }
    public int EligibleReferenceCount { get; }
    public int InteractedReferenceCount { get; }
    public IReadOnlyList<AuctionEpisodeSnapshot> ActiveEpisodes { get; }
    public IReadOnlyList<AuctionEpisodeSnapshot> RecentlyClosedEpisodes { get; }
    public AuctionEpisodeSnapshot? LatestUpdatedEpisode { get; }
    public IReadOnlyDictionary<EpisodeState, int> EpisodeCountsByState { get; }
    public string InputFingerprint { get; }
    public long RegistryRevision { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime LastUpdatedAtUtc { get; }
    public IReadOnlyList<string> Limitations { get; }
    public long TradeEventsSeen { get; }
    public long TradeEventsAccepted { get; }
    public long TradeEventsDuplicate { get; }
    public long TradeEventsRejected { get; }
    public string? LastTradeRejectReason { get; }
    public string? LastAcceptedTradeEventId { get; }
    public long? LastAcceptedTradeSequence { get; }
    public string Version => SnapshotVersion;
}
