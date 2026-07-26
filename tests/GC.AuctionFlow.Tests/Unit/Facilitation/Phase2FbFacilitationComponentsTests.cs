using GC.AuctionFlow.Efficiency;
using GC.AuctionFlow.Orderflow;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Facilitation;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Facilitation;

/// <summary>
/// Phase 2F-b — the four facilitation components (v1.3 §5.2, KDK Ch 31).
///
/// Facilitation is a convergent conclusion from Activity, Progress, Structure and
/// Maintenance. Phase 2F shipped only the first two, so G-TF-002 could not be
/// satisfied even in principle. This phase adds Structure (POC / value migration
/// aligned with the attempt) and Maintenance (did pullbacks hold).
///
/// Alignment is a sign comparison and therefore observable. Whether a migration is
/// LARGE ENOUGH to matter stays calibrated (G-TF-004, stratified by regime).
/// </summary>
public sealed class Phase2FbFacilitationComponentsTests
{
    private static DateTime Utc(int sec = 0) =>
        new(2026, 7, 27, 12, 0, sec, DateTimeKind.Utc);

    // ---------- helpers ----------

    private static AuctionEfficiencyEvidenceSnapshot MakeEfficiency(
        EfficiencyResultDirection direction = EfficiencyResultDirection.Up,
        decimal? askVolume = 400m,
        decimal? bidVolume = 100m,
        decimal classifiedVolume = 500m,
        long? favorableTicks = 40L,
        long? volumePocMigration = null,
        long? tpoPocMigration = null,
        long? volumeValueMigration = null,
        long? tpoValueMigration = null,
        long? progressRetained = null,
        decimal? progressRetentionRatio = null,
        TimeSpan? timeAtMaxExcursion = null,
        string snapshotId = "EFF-1")
    {
        var effort = new AuctionEffortEvidenceVector(
            totalExecutedVolume: 500m, tradeCount: 100L, priceLevelCount: 10,
            classifiedVolume: classifiedVolume, askVolume: askVolume, bidVolume: bidVolume,
            unknownAggressorVolume: 0m, classifiedDelta: 300m, absoluteClassifiedDelta: 300m,
            classifiedCvdChange: null, aggressorCoverageRatio: null,
            observationDuration: null, minimumTradeInterval: null, maximumTradeInterval: null,
            meanTradeInterval: null, latestTradeInterval: null,
            tradesPerSecondRaw: null, contractsPerSecondRaw: null,
            maximumLevelExecutedVolume: null, maximumLevelTradeCount: null,
            maximumAbsoluteLevelDelta: null, revisitedLevelCount: 0, maximumVisitCount: 1,
            classifiedLevelCount: 10, unknownOnlyLevelCount: 0,
            samePriceRatioAvailabilityCount: 0, diagonalRatioAvailabilityCount: 0,
            rawAskDominantLevelCount: 0, rawBidDominantLevelCount: 0,
            rawEqualLevelCount: 0, rawUnknownDominantLevelCount: 0,
            maximumConsecutiveRawAskDominanceTicks: 0,
            maximumConsecutiveRawBidDominanceTicks: 0,
            clusterPopulationSize: 10,
            evidenceAvailability: EfficiencyAvailability.Available,
            limitations: Array.Empty<string>());

        var result = new AuctionResultEvidenceVector(
            firstPriceTick: null, latestPriceTick: null, highPriceTick: null, lowPriceTick: null,
            netPriceProgressTicks: null, grossRangeTicks: null,
            maximumFavorableProgressTicks: favorableTicks,
            maximumAdverseProgressTicks: null,
            progressRetainedTicks: progressRetained,
            progressRetentionRatio: progressRetentionRatio,
            timeToMaximumFavorableProgress: null, timeToLatestProgress: null,
            timeAtMaximumExcursion: timeAtMaxExcursion,
            episodeReferenceDistanceStartTicks: null, episodeReferenceDistanceLatestTicks: null,
            maximumDistanceFromReferenceTicks: null, currentDistanceFromReferenceTicks: null,
            geometricReentryObserved: null, timeMaintainedInside: null,
            outsideTimeRatio: null, outsideVolumeRatio: null, outsideTradeRatio: null,
            localPocTick: null, localPocDisplacementTicks: null,
            developingTpoPocStartTick: null, developingTpoPocLatestTick: null,
            tpoPocMigrationTicks: tpoPocMigration,
            developingVolumePocStartTick: null, developingVolumePocLatestTick: null,
            volumePocMigrationTicks: volumePocMigration,
            developingTpoValueLowStartTick: null, developingTpoValueHighStartTick: null,
            developingTpoValueLowLatestTick: null, developingTpoValueHighLatestTick: null,
            developingVolumeValueLowStartTick: null, developingVolumeValueHighStartTick: null,
            developingVolumeValueLowLatestTick: null, developingVolumeValueHighLatestTick: null,
            tpoValueCentroidMigrationTicks: tpoValueMigration,
            volumeValueCentroidMigrationTicks: volumeValueMigration,
            priceLocationAtStart: null, priceLocationLatest: null,
            evidenceAvailability: EfficiencyAvailability.Available,
            limitations: Array.Empty<string>());

        var raw = new AuctionEfficiencyRawRelationships(
            null, null, null, null, null, null, null, null, null, null, null, null);

        return new AuctionEfficiencyEvidenceSnapshot(
            snapshotId: snapshotId,
            policyVersion: AuctionEfficiencyEvidencePolicyConfig.PolicyVersion,
            scopeType: EfficiencyScopeType.CurrentPrimaryAuction,
            primaryAuctionId: "PI-1",
            episodeId: null,
            referenceId: null,
            referenceRole: null,
            instrumentIdentity: "GCQ6",
            dataEpoch: "GCQ6|tick=0.1",
            tickSize: 0.1m,
            timestampPolicy: AtasTimestampNormalizer.PolicyVersion,
            measurementStatus: EfficiencyModuleState.Ready,
            classificationState: EfficiencyClassificationState.NotCalibrated,
            observationStartedAtUtc: Utc(),
            firstInputAtUtc: Utc(),
            lastInputAtUtc: Utc(30),
            coverageMode: OrderflowCoverageMode.LiveOnlyFromAuctionStart,
            resultDirection: direction,
            effort: effort,
            result: result,
            rawRelationships: raw,
            stateVersion: 1L,
            eventRevision: 1L,
            dataQuality: EfficiencyDataQuality.Complete,
            availability: EfficiencyAvailability.Available,
            limitations: Array.Empty<string>(),
            inputFingerprint: "fp1",
            isFrozen: false);
    }

    private static AuctionEfficiencyEvidenceSetSnapshot MakeSet(
        AuctionEfficiencyEvidenceSnapshot current) =>
        new(
            moduleState: EfficiencyModuleState.Ready,
            policyVersion: AuctionEfficiencyEvidencePolicyConfig.PolicyVersion,
            currentAuctionEvidence: current,
            activeEpisodeEvidence: Array.Empty<AuctionEfficiencyEvidenceSnapshot>(),
            recentlyClosedEpisodeEvidence: Array.Empty<AuctionEfficiencyEvidenceSnapshot>(),
            latestUpdatedEvidence: current,
            readyCount: 1,
            partialCount: 0,
            invalidCount: 0,
            inputFingerprint: null,
            rejectedStaleCount: 0L,
            lastRejectionReason: null,
            createdAtUtc: Utc(),
            lastUpdatedAtUtc: Utc(30),
            limitations: Array.Empty<string>());

    private static TradeFacilitationSnapshot Facilitate(AuctionEfficiencyEvidenceSnapshot eff)
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        return host.Rebuild(MakeSet(eff), Utc()).CurrentAuctionFacilitation!;
    }

    // ========== A: Policy + enum shape ==========

    [Fact]
    public void A01_Four_components_are_required() =>
        Assert.Equal(4, TradeFacilitationPolicyConfig.RequiredComponents);

    [Fact]
    public void A02_All_four_kdk_components_are_named()
    {
        var c = Enum.GetValues<FacilitationComponent>();
        Assert.Equal(4, c.Length);
        Assert.Contains(FacilitationComponent.Activity, c);
        Assert.Contains(FacilitationComponent.Progress, c);
        Assert.Contains(FacilitationComponent.Structure, c);
        Assert.Contains(FacilitationComponent.Maintenance, c);
    }

    [Fact]
    public void A03_Alignment_default_is_unavailable() =>
        Assert.Equal(FacilitationComponentAlignment.Unavailable, default(FacilitationComponentAlignment));

    [Fact]
    public void A04_New_limitation_constants_are_stable()
    {
        Assert.Equal("TRADE_FACILITATION_COMPONENTS_INCOMPLETE", TradeFacilitationPolicyConfig.LimitationComponentsIncomplete);
        Assert.Equal("TRADE_FACILITATION_STRUCTURE_UNAVAILABLE", TradeFacilitationPolicyConfig.LimitationStructureUnavailable);
        Assert.Equal("TRADE_FACILITATION_MAINTENANCE_UNAVAILABLE", TradeFacilitationPolicyConfig.LimitationMaintenanceUnavailable);
    }

    // ========== B: Structure alignment ==========

    [Fact]
    public void B01_Up_with_upward_poc_migration_is_aligned()
    {
        var f = Facilitate(MakeEfficiency(EfficiencyResultDirection.Up, volumePocMigration: 12L));
        Assert.Equal(FacilitationComponentAlignment.Aligned, f.StructureAlignment);
        Assert.Equal(12L, f.StructurePocMigrationTicks);
    }

    [Fact]
    public void B02_Up_with_downward_poc_migration_is_opposed()
    {
        var f = Facilitate(MakeEfficiency(EfficiencyResultDirection.Up, volumePocMigration: -8L));
        Assert.Equal(FacilitationComponentAlignment.Opposed, f.StructureAlignment);
    }

    [Fact]
    public void B03_Down_with_downward_poc_migration_is_aligned()
    {
        var f = Facilitate(MakeEfficiency(
            EfficiencyResultDirection.Down, askVolume: 100m, bidVolume: 400m, volumePocMigration: -15L));
        Assert.Equal(FacilitationComponentAlignment.Aligned, f.StructureAlignment);
    }

    [Fact]
    public void B04_Down_with_upward_poc_migration_is_opposed()
    {
        var f = Facilitate(MakeEfficiency(
            EfficiencyResultDirection.Down, askVolume: 100m, bidVolume: 400m, volumePocMigration: 15L));
        Assert.Equal(FacilitationComponentAlignment.Opposed, f.StructureAlignment);
    }

    [Fact]
    public void B05_Zero_migration_is_flat()
    {
        var f = Facilitate(MakeEfficiency(EfficiencyResultDirection.Up, volumePocMigration: 0L));
        Assert.Equal(FacilitationComponentAlignment.Flat, f.StructureAlignment);
    }

    [Fact]
    public void B06_Unknown_direction_yields_unknown_alignment()
    {
        var f = Facilitate(MakeEfficiency(EfficiencyResultDirection.Unknown, volumePocMigration: 12L));
        Assert.Equal(FacilitationComponentAlignment.Unknown, f.StructureAlignment);
    }

    [Fact]
    public void B07_Missing_migration_yields_unavailable_not_flat()
    {
        // Absent data must not be reported as "no migration" (G-ACC-003 in spirit).
        var f = Facilitate(MakeEfficiency(EfficiencyResultDirection.Up));
        Assert.Equal(FacilitationComponentAlignment.Unavailable, f.StructureAlignment);
        Assert.Null(f.StructurePocMigrationTicks);
    }

    /// <summary>Volume POC reflects executed activity, so it wins over TPO POC.</summary>
    [Fact]
    public void B08_Volume_poc_is_preferred_over_tpo_poc()
    {
        var f = Facilitate(MakeEfficiency(EfficiencyResultDirection.Up,
            volumePocMigration: 20L, tpoPocMigration: -20L));
        Assert.Equal(20L, f.StructurePocMigrationTicks);
        Assert.Equal(FacilitationComponentAlignment.Aligned, f.StructureAlignment);
    }

    [Fact]
    public void B09_Tpo_poc_is_used_when_volume_poc_is_absent()
    {
        var f = Facilitate(MakeEfficiency(EfficiencyResultDirection.Up, tpoPocMigration: 7L));
        Assert.Equal(7L, f.StructurePocMigrationTicks);
    }

    [Fact]
    public void B10_Value_migration_is_carried_separately()
    {
        var f = Facilitate(MakeEfficiency(EfficiencyResultDirection.Up,
            volumePocMigration: 5L, volumeValueMigration: 9L));
        Assert.Equal(5L, f.StructurePocMigrationTicks);
        Assert.Equal(9L, f.StructureValueMigrationTicks);
    }

    /// <summary>
    /// G-TF-004: magnitude significance is regime-dependent and NOT CALIBRATED.
    /// A one-tick migration and a fifty-tick migration must classify identically here.
    /// </summary>
    [Fact]
    public void B11_Alignment_is_magnitude_free()
    {
        var small = Facilitate(MakeEfficiency(EfficiencyResultDirection.Up, volumePocMigration: 1L));
        var large = Facilitate(MakeEfficiency(EfficiencyResultDirection.Up, volumePocMigration: 500L));
        Assert.Equal(small.StructureAlignment, large.StructureAlignment);
    }

    // ========== C: Maintenance ==========

    [Fact]
    public void C01_Retention_measurements_are_carried()
    {
        var f = Facilitate(MakeEfficiency(
            progressRetained: 30L,
            progressRetentionRatio: 0.75m,
            timeAtMaxExcursion: TimeSpan.FromSeconds(12)));
        Assert.Equal(30L, f.MaintenanceProgressRetainedTicks);
        Assert.Equal(0.75m, f.MaintenanceProgressRetentionRatio);
        Assert.Equal(TimeSpan.FromSeconds(12), f.MaintenanceTimeAtMaximumExcursion);
    }

    [Fact]
    public void C02_Missing_retention_stays_null()
    {
        var f = Facilitate(MakeEfficiency());
        Assert.Null(f.MaintenanceProgressRetentionRatio);
        Assert.Null(f.MaintenanceProgressRetainedTicks);
    }

    /// <summary>Retention is a raw ratio: no threshold is applied to it here.</summary>
    [Fact]
    public void C03_Low_retention_is_reported_not_judged()
    {
        var f = Facilitate(MakeEfficiency(progressRetentionRatio: 0.05m));
        Assert.Equal(0.05m, f.MaintenanceProgressRetentionRatio);
        Assert.Equal(TradeFacilitationClassificationState.NotCalibrated, f.Classification);
    }

    // ========== D: Component completeness (G-TF-002) ==========

    [Fact]
    public void D01_All_four_components_present_is_complete()
    {
        var f = Facilitate(MakeEfficiency(
            EfficiencyResultDirection.Up,
            favorableTicks: 40L,
            volumePocMigration: 10L,
            progressRetentionRatio: 0.8m));
        Assert.Equal(4, f.AvailableComponentCount);
        Assert.True(f.ComponentsComplete);
    }

    [Fact]
    public void D02_Phase2F_shape_is_only_two_components()
    {
        // Activity + Progress only — the shape Phase 2F shipped.
        var f = Facilitate(MakeEfficiency(EfficiencyResultDirection.Up, favorableTicks: 40L));
        Assert.Equal(2, f.AvailableComponentCount);
        Assert.False(f.ComponentsComplete);
        Assert.Contains(TradeFacilitationPolicyConfig.LimitationComponentsIncomplete, f.Limitations);
    }

    [Fact]
    public void D03_Missing_structure_is_flagged()
    {
        var f = Facilitate(MakeEfficiency(progressRetentionRatio: 0.8m));
        Assert.Contains(TradeFacilitationPolicyConfig.LimitationStructureUnavailable, f.Limitations);
    }

    [Fact]
    public void D04_Missing_maintenance_is_flagged()
    {
        var f = Facilitate(MakeEfficiency(EfficiencyResultDirection.Up, volumePocMigration: 10L));
        Assert.Contains(TradeFacilitationPolicyConfig.LimitationMaintenanceUnavailable, f.Limitations);
    }

    [Fact]
    public void D05_Complete_components_drop_the_incomplete_flag()
    {
        var f = Facilitate(MakeEfficiency(
            EfficiencyResultDirection.Up, favorableTicks: 40L,
            volumePocMigration: 10L, progressRetentionRatio: 0.8m));
        Assert.DoesNotContain(TradeFacilitationPolicyConfig.LimitationComponentsIncomplete, f.Limitations);
        Assert.DoesNotContain(TradeFacilitationPolicyConfig.LimitationStructureUnavailable, f.Limitations);
        Assert.DoesNotContain(TradeFacilitationPolicyConfig.LimitationMaintenanceUnavailable, f.Limitations);
    }

    /// <summary>
    /// G-TF-002: completeness is a PRECONDITION for a verdict, never the verdict itself.
    /// Even with all four components the classification stays gated.
    /// </summary>
    [Fact]
    public void D06_Completeness_never_unlocks_a_verdict()
    {
        var f = Facilitate(MakeEfficiency(
            EfficiencyResultDirection.Up, favorableTicks: 40L,
            volumePocMigration: 10L, progressRetentionRatio: 0.95m));
        Assert.True(f.ComponentsComplete);
        Assert.Equal(TradeFacilitationClassificationState.NotCalibrated, f.Classification);
        Assert.Contains(TradeFacilitationPolicyConfig.LimitationNotCalibrated, f.Limitations);
        Assert.Contains(TradeFacilitationPolicyConfig.LimitationHealthyNotCalibrated, f.Limitations);
        Assert.Contains(TradeFacilitationPolicyConfig.LimitationFailingNotCalibrated, f.Limitations);
    }

    // ========== E: GPS card ==========

    [Fact]
    public void E01_Gps_shows_component_count()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var set = host.Rebuild(MakeSet(MakeEfficiency(
            EfficiencyResultDirection.Up, favorableTicks: 40L,
            volumePocMigration: 10L, progressRetentionRatio: 0.8m)), Utc());
        var rows = AuctionGpsCardMapper.BuildTradeFacilitationLines(set, false);
        Assert.Contains("FACILITATION COMPONENTS: 4/4", rows);
        Assert.Contains("STRUCTURE ALIGNMENT: ALIGNED", rows);
    }

    [Fact]
    public void E02_Gps_marks_incomplete_components()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var set = host.Rebuild(MakeSet(MakeEfficiency(EfficiencyResultDirection.Up)), Utc());
        var rows = AuctionGpsCardMapper.BuildTradeFacilitationLines(set, false);
        Assert.Contains(rows, r => r.StartsWith("FACILITATION COMPONENTS:", StringComparison.Ordinal)
                                   && r.EndsWith("INCOMPLETE", StringComparison.Ordinal));
    }

    [Fact]
    public void E03_Gps_reports_unavailable_maintenance_not_zero()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var set = host.Rebuild(MakeSet(MakeEfficiency(EfficiencyResultDirection.Up)), Utc());
        var rows = AuctionGpsCardMapper.BuildTradeFacilitationLines(set, false);
        Assert.Contains("MAINTENANCE RETENTION: unavailable", rows);
    }

    [Fact]
    public void E04_Gps_never_emits_a_facilitation_verdict()
    {
        var host = new TradeFacilitationHost(new TradeFacilitationPolicyConfig(enabled: true));
        var set = host.Rebuild(MakeSet(MakeEfficiency(
            EfficiencyResultDirection.Up, favorableTicks: 40L,
            volumePocMigration: 10L, progressRetentionRatio: 0.95m)), Utc());
        var text = string.Join(" | ", AuctionGpsCardMapper.BuildTradeFacilitationLines(set, true));
        Assert.DoesNotContain("TRADE FACILITATION HEALTHY", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TRADE FACILITATION FAILING", text, StringComparison.OrdinalIgnoreCase);
    }
}
