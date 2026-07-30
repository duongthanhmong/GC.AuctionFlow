using System.Globalization;
using GC.AuctionFlow.Efficiency;

namespace GC.AuctionFlow.Research;

/// <summary>
/// Projects a frozen closed-episode efficiency snapshot into a research observation.
///
/// This is the only type in <c>Research</c> that touches the <c>Efficiency</c> namespace, and
/// that is deliberate. <see cref="AuthorityGuardTests"/> A02 fences
/// <c>ProcessHistoricalScanner</c> off from Efficiency and EffortResult authority, and the
/// research observation travels through the same store the scanner uses. An earlier draft put
/// the live snapshot on the record itself; A02 failed immediately, correctly, because the
/// forbidden type became reachable from the scanner's closure through a shared field.
///
/// So the boundary is here: everything downstream of this class holds primitives and strings,
/// and only <see cref="EffortResultResearchCollector"/> — which no decision orchestrator
/// reaches — calls it.
///
/// Enums are projected to their names rather than their numbers. A file that outlives the
/// process should not silently change meaning because a member was inserted in the middle of
/// an enum, and a reader written months later should not need this assembly to interpret it.
/// </summary>
public static class EffortResultResearchProjection
{
    /// <summary>
    /// Builds the deterministic identity for a snapshot, without admitting it.
    ///
    /// Exposed so the collector can deduplicate before projecting, and so a test can assert
    /// the identity is a pure function of the snapshot.
    /// </summary>
    public static string BuildObservationId(AuctionEfficiencyEvidenceSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return "ERRO|" + Part(snapshot.PrimaryAuctionId)
               + "|" + Part(snapshot.EpisodeId)
               + "|" + Part(snapshot.SnapshotId)
               + "|" + snapshot.StateVersion.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Admits a snapshot as a research observation, or says why it was refused.
    ///
    /// Two admission rules, both structural:
    ///
    /// <list type="bullet">
    /// <item><description>
    /// <b>Closed episodes only.</b> A current-auction or active-episode measurement is still
    /// moving. Collecting it would record the publish schedule rather than the market — the
    /// same reason the Historical Scanner folds only closed episodes.
    /// </description></item>
    /// <item><description>
    /// <b>Frozen evidence only.</b> An unfrozen snapshot can still be revised, so its
    /// identity would name two different measurements at two different times.
    /// </description></item>
    /// </list>
    /// </summary>
    public static EffortResultResearchRecord? TryProject(
        AuctionEfficiencyEvidenceSnapshot? snapshot,
        DateTime collectedAtUtc,
        out EffortResultResearchRejection rejection)
    {
        if (snapshot is null)
        {
            rejection = EffortResultResearchRejection.NoStableIdentity;
            return null;
        }

        if (snapshot.ScopeType != EfficiencyScopeType.ClosedEpisode)
        {
            rejection = EffortResultResearchRejection.NotClosedEpisode;
            return null;
        }

        if (!snapshot.IsFrozen)
        {
            rejection = EffortResultResearchRejection.NotFrozen;
            return null;
        }

        // An identity built only from empty parts cannot deduplicate anything across a
        // restart, and a row that cannot be deduplicated silently inflates the sample.
        if (string.IsNullOrWhiteSpace(snapshot.SnapshotId)
            && string.IsNullOrWhiteSpace(snapshot.EpisodeId))
        {
            rejection = EffortResultResearchRejection.NoStableIdentity;
            return null;
        }

        rejection = EffortResultResearchRejection.None;

        return new EffortResultResearchRecord
        {
            ObservationId = BuildObservationId(snapshot),
            Limitations = EffortResultResearchPolicyConfig.StandingLimitations.ToArray(),
            SnapshotId = snapshot.SnapshotId,
            PrimaryAuctionId = snapshot.PrimaryAuctionId,
            EpisodeId = snapshot.EpisodeId,
            ReferenceId = snapshot.ReferenceId,
            ReferenceRole = snapshot.ReferenceRole?.ToString(),
            SourceScopeType = snapshot.ScopeType.ToString(),
            InstrumentIdentity = snapshot.InstrumentIdentity,
            DataEpoch = snapshot.DataEpoch,
            TickSize = snapshot.TickSize,
            TimestampPolicy = snapshot.TimestampPolicy,
            SourcePolicyVersion = snapshot.PolicyVersion,
            SourceSnapshotVersion = snapshot.Version,
            StateVersion = snapshot.StateVersion,
            EventRevision = snapshot.EventRevision,
            InputFingerprint = snapshot.InputFingerprint,
            MeasurementStatus = snapshot.MeasurementStatus.ToString(),
            ClassificationState = snapshot.ClassificationState.ToString(),
            CoverageMode = snapshot.CoverageMode.ToString(),
            DataQuality = snapshot.DataQuality.ToString(),
            Availability = snapshot.Availability.ToString(),
            IsFrozen = snapshot.IsFrozen,
            SourceLimitations = snapshot.Limitations.ToArray(),
            ObservationStartedAtUtc = snapshot.ObservationStartedAtUtc,
            FirstInputAtUtc = snapshot.FirstInputAtUtc,
            LastInputAtUtc = snapshot.LastInputAtUtc,
            CollectedAtUtc = collectedAtUtc,
            ResultDirection = snapshot.ResultDirection.ToString(),
            Effort = Project(snapshot.Effort),
            Result = Project(snapshot.Result),
            RawRelationships = Project(snapshot.RawRelationships),
        };
    }

    private static string Part(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : value.Replace("|", "_", StringComparison.Ordinal);

    private static ResearchEffortVector Project(AuctionEffortEvidenceVector v) => new()
    {
        TotalExecutedVolume = v.TotalExecutedVolume,
        TradeCount = v.TradeCount,
        PriceLevelCount = v.PriceLevelCount,
        ClassifiedVolume = v.ClassifiedVolume,
        AskVolume = v.AskVolume,
        BidVolume = v.BidVolume,
        UnknownAggressorVolume = v.UnknownAggressorVolume,
        ClassifiedDelta = v.ClassifiedDelta,
        AbsoluteClassifiedDelta = v.AbsoluteClassifiedDelta,
        ClassifiedCvdChange = v.ClassifiedCvdChange,
        AggressorCoverageRatio = v.AggressorCoverageRatio,
        ObservationDuration = v.ObservationDuration,
        MinimumTradeInterval = v.MinimumTradeInterval,
        MaximumTradeInterval = v.MaximumTradeInterval,
        MeanTradeInterval = v.MeanTradeInterval,
        LatestTradeInterval = v.LatestTradeInterval,
        TradesPerSecondRaw = v.TradesPerSecondRaw,
        ContractsPerSecondRaw = v.ContractsPerSecondRaw,
        MaximumLevelExecutedVolume = v.MaximumLevelExecutedVolume,
        MaximumLevelTradeCount = v.MaximumLevelTradeCount,
        MaximumAbsoluteLevelDelta = v.MaximumAbsoluteLevelDelta,
        RevisitedLevelCount = v.RevisitedLevelCount,
        MaximumVisitCount = v.MaximumVisitCount,
        ClassifiedLevelCount = v.ClassifiedLevelCount,
        UnknownOnlyLevelCount = v.UnknownOnlyLevelCount,
        SamePriceRatioAvailabilityCount = v.SamePriceRatioAvailabilityCount,
        DiagonalRatioAvailabilityCount = v.DiagonalRatioAvailabilityCount,
        RawAskDominantLevelCount = v.RawAskDominantLevelCount,
        RawBidDominantLevelCount = v.RawBidDominantLevelCount,
        RawEqualLevelCount = v.RawEqualLevelCount,
        RawUnknownDominantLevelCount = v.RawUnknownDominantLevelCount,
        MaximumConsecutiveRawAskDominanceTicks = v.MaximumConsecutiveRawAskDominanceTicks,
        MaximumConsecutiveRawBidDominanceTicks = v.MaximumConsecutiveRawBidDominanceTicks,
        ClusterPopulationSize = v.ClusterPopulationSize,
        EvidenceAvailability = v.EvidenceAvailability.ToString(),
        Limitations = v.Limitations.ToArray(),
    };

    private static ResearchResultVector Project(AuctionResultEvidenceVector v) => new()
    {
        FirstPriceTick = v.FirstPriceTick,
        LatestPriceTick = v.LatestPriceTick,
        HighPriceTick = v.HighPriceTick,
        LowPriceTick = v.LowPriceTick,
        NetPriceProgressTicks = v.NetPriceProgressTicks,
        GrossRangeTicks = v.GrossRangeTicks,
        MaximumFavorableProgressTicks = v.MaximumFavorableProgressTicks,
        MaximumAdverseProgressTicks = v.MaximumAdverseProgressTicks,
        ProgressRetainedTicks = v.ProgressRetainedTicks,
        ProgressRetentionRatio = v.ProgressRetentionRatio,
        TimeToMaximumFavorableProgress = v.TimeToMaximumFavorableProgress,
        TimeToLatestProgress = v.TimeToLatestProgress,
        TimeAtMaximumExcursion = v.TimeAtMaximumExcursion,
        EpisodeReferenceDistanceStartTicks = v.EpisodeReferenceDistanceStartTicks,
        EpisodeReferenceDistanceLatestTicks = v.EpisodeReferenceDistanceLatestTicks,
        MaximumDistanceFromReferenceTicks = v.MaximumDistanceFromReferenceTicks,
        CurrentDistanceFromReferenceTicks = v.CurrentDistanceFromReferenceTicks,
        GeometricReentryObserved = v.GeometricReentryObserved,
        TimeMaintainedInside = v.TimeMaintainedInside,
        OutsideTimeRatio = v.OutsideTimeRatio,
        OutsideVolumeRatio = v.OutsideVolumeRatio,
        OutsideTradeRatio = v.OutsideTradeRatio,
        LocalPocTick = v.LocalPocTick,
        LocalPocDisplacementTicks = v.LocalPocDisplacementTicks,
        DevelopingTpoPocStartTick = v.DevelopingTpoPocStartTick,
        DevelopingTpoPocLatestTick = v.DevelopingTpoPocLatestTick,
        TpoPocMigrationTicks = v.TpoPocMigrationTicks,
        DevelopingVolumePocStartTick = v.DevelopingVolumePocStartTick,
        DevelopingVolumePocLatestTick = v.DevelopingVolumePocLatestTick,
        VolumePocMigrationTicks = v.VolumePocMigrationTicks,
        DevelopingTpoValueLowStartTick = v.DevelopingTpoValueLowStartTick,
        DevelopingTpoValueHighStartTick = v.DevelopingTpoValueHighStartTick,
        DevelopingTpoValueLowLatestTick = v.DevelopingTpoValueLowLatestTick,
        DevelopingTpoValueHighLatestTick = v.DevelopingTpoValueHighLatestTick,
        DevelopingVolumeValueLowStartTick = v.DevelopingVolumeValueLowStartTick,
        DevelopingVolumeValueHighStartTick = v.DevelopingVolumeValueHighStartTick,
        DevelopingVolumeValueLowLatestTick = v.DevelopingVolumeValueLowLatestTick,
        DevelopingVolumeValueHighLatestTick = v.DevelopingVolumeValueHighLatestTick,
        TpoValueCentroidMigrationTicks = v.TpoValueCentroidMigrationTicks,
        VolumeValueCentroidMigrationTicks = v.VolumeValueCentroidMigrationTicks,
        PriceLocationAtStart = v.PriceLocationAtStart,
        PriceLocationLatest = v.PriceLocationLatest,
        EvidenceAvailability = v.EvidenceAvailability.ToString(),
        Limitations = v.Limitations.ToArray(),
    };

    private static ResearchRawRelationships Project(AuctionEfficiencyRawRelationships v) => new()
    {
        NetProgressPerExecutedContract = v.NetProgressPerExecutedContract,
        GrossRangePerExecutedContract = v.GrossRangePerExecutedContract,
        FavorableProgressPerExecutedContract = v.FavorableProgressPerExecutedContract,
        NetProgressPerTrade = v.NetProgressPerTrade,
        FavorableProgressPerTrade = v.FavorableProgressPerTrade,
        ExecutedContractsPerTickOfGrossRange = v.ExecutedContractsPerTickOfGrossRange,
        ClassifiedAbsoluteDeltaPerTickOfGrossRange = v.ClassifiedAbsoluteDeltaPerTickOfGrossRange,
        ClassifiedAbsoluteDeltaPerTickOfFavorableProgress =
            v.ClassifiedAbsoluteDeltaPerTickOfFavorableProgress,
        TimePerNetProgressTick = v.TimePerNetProgressTick,
        TimePerFavorableProgressTick = v.TimePerFavorableProgressTick,
        TradesPerNetProgressTick = v.TradesPerNetProgressTick,
        VolumePerNetProgressTick = v.VolumePerNetProgressTick,
    };
}
