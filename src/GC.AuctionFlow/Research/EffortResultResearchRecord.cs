namespace GC.AuctionFlow.Research;

/// <summary>
/// The Effort/Result research collection bridge (WP01A).
///
/// The Historical Scanner already collects episode geometry. It does not collect the
/// Effort and Result evidence vectors, because nothing connected the Efficiency module to
/// Research — the collection machinery existed and ran, and the features existed, and no
/// call joined them. This policy names that bridge.
///
/// There is no numeric constant here that gates anything. The only number is a retention
/// bound, named as such.
/// </summary>
public sealed class EffortResultResearchPolicyConfig
{
    public const string PolicyVersion = "EFFORT_RESULT_RESEARCH_POLICY_V1";

    /// <summary>Schema of the persisted row. Bumped when the stored shape changes.</summary>
    public const string RecordSchemaVersion = "gcae-effort-result-research-v1";

    public const string LimitationResearchOnly = "RESEARCH_ONLY_NOT_A_PRODUCTION_INPUT";
    public const string LimitationNonAuthoritative = "NON_AUTHORITATIVE_NOT_A_DECISION_INPUT";
    public const string LimitationNoNormalization = "RAW_MEASURES_NO_STATISTICAL_NORMALIZATION";
    public const string LimitationNoOutcomeLabel = "NO_FUTURE_OUTCOME_LABEL_COLLECTED";
    public const string LimitationClosedEpisodeOnly = "CLOSED_EPISODE_FROZEN_EVIDENCE_ONLY";
    public const string LimitationLiveOnly = "LIVE_ONLY_HISTORY";
    public const string LimitationDatasetStartsAtIndicatorStart = "DATASET_STARTS_AT_INDICATOR_START";

    /// <summary>
    /// Observations retained in memory before the oldest is dropped.
    ///
    /// A retention bound, not a sample size. Deciding how many observations a study needs
    /// is a research governance act — see <see cref="CalibrationProtocol"/>.
    /// </summary>
    public const int ObservationCapacity = 2048;

    public EffortResultResearchPolicyConfig(bool enabled = false)
    {
        Enabled = enabled;
    }

    public bool Enabled { get; }

    public string Version => PolicyVersion;

    /// <summary>
    /// Limitations this product always carries.
    ///
    /// None can be retired at runtime. The product is research material; turning it into a
    /// decision input is a governance act that happens outside this build.
    /// </summary>
    public static IReadOnlyList<string> StandingLimitations { get; } = new[]
    {
        LimitationResearchOnly,
        LimitationNonAuthoritative,
        LimitationNoNormalization,
        LimitationNoOutcomeLabel,
        LimitationClosedEpisodeOnly,
        LimitationLiveOnly,
        LimitationDatasetStartsAtIndicatorStart,
    };
}

/// <summary>Why an efficiency snapshot was not admitted as a research observation.</summary>
public enum EffortResultResearchRejection
{
    /// <summary>Admitted. Not a claim that any study may use it.</summary>
    None = 0,

    /// <summary>Scope was not a closed episode, so the measurement is still moving.</summary>
    NotClosedEpisode = 1,

    /// <summary>Evidence was not frozen, so a later publish could still change it.</summary>
    NotFrozen = 2,

    /// <summary>The snapshot carried no usable identity to deduplicate on.</summary>
    NoStableIdentity = 3,
}

/// <summary>
/// One closed-episode Effort/Result observation, as raw features.
///
/// v1.2 §46.3 requires enough raw features that a different rule version can be re-run
/// later without re-collecting the data, so this carries measurements and not conclusions.
/// Nothing here is labelled strong, weak, efficient or failed, and no value is normalized,
/// ranked or scored — normalization is Stage 2, and choosing it before the corpus exists is
/// what `G-CAL-001` forbids.
///
/// Unavailable measurements stay null. Writing zero for an unobserved delta would be a
/// fabricated observation, and a distribution built on fabricated zeros is worse than a
/// smaller honest one (v1.3 `G-ACC-003`).
///
/// **This is not an outcome label.** The Result vector is what was observable at the moment
/// the episode closed, not a follow-through measurement made afterwards. A future-outcome
/// product needs its own horizon and identity and is deliberately not collected here.
///
/// **This type deliberately holds no Efficiency or EffortResult type.**
///
/// Two reasons, and the second is the one that has teeth. First, a file that outlives the
/// process must not depend on an internal type staying still — a reader written months later
/// should not need this assembly, which is why every enum is stored as its name. Second,
/// `AuthorityGuardTests.A02` fences `ProcessHistoricalScanner` off from Efficiency and
/// EffortResult authority, and this record travels through the store that the scanner also
/// uses. Holding the live snapshot here made a forbidden type reachable from that closure
/// and A02 failed immediately — correctly. Projection happens in
/// <see cref="EffortResultResearchProjection"/>, which only the collector reaches.
/// </summary>
public sealed class EffortResultResearchRecord
{
    /// <summary>Schema tag, so a reader can refuse a shape it does not know.</summary>
    public string Schema { get; set; } = EffortResultResearchPolicyConfig.RecordSchemaVersion;

    /// <summary>
    /// Stable deterministic identity, derived only from the source snapshot.
    ///
    /// Format: <c>ERRO|{primaryAuctionId}|{episodeId}|{snapshotId}|{stateVersion}</c>.
    ///
    /// No clock and no GUID take part: the same frozen snapshot must produce the same id in
    /// a later process, or restart deduplication cannot work and the corpus is weighted by
    /// how often the operator restarted ATAS — the defect measured on the episode dataset,
    /// 5,126 lines holding 2,098 distinct rows.
    /// </summary>
    public string ObservationId { get; set; } = "";

    public string CollectorPolicyVersion { get; set; } = EffortResultResearchPolicyConfig.PolicyVersion;

    /// <summary>Recorded so a consumer cannot mistake research material for a conclusion.</summary>
    public bool ResearchOnly { get; set; } = true;
    public bool Authoritative { get; set; }
    public string[] Limitations { get; set; } = Array.Empty<string>();

    // ---- identity and scope ----
    public string SnapshotId { get; set; } = "";
    public string PrimaryAuctionId { get; set; } = "";
    public string? EpisodeId { get; set; }
    public string? ReferenceId { get; set; }
    public string? ReferenceRole { get; set; }
    /// <summary>
    /// The source measurement scope, as its name.
    ///
    /// Called <c>SourceScopeType</c> and not <c>ScopeType</c> on purpose. `COL-01`/`COL-02`:
    /// that name already belongs to <c>EfficiencyScopeType</c> on three published snapshots,
    /// and the GPS card renders it. `Wp01aT03ContractGuardTests.T07` fails the build if a
    /// second meaning ever claims the name — including from here.
    /// </summary>
    public string SourceScopeType { get; set; } = "";
    public string InstrumentIdentity { get; set; } = "";
    public string DataEpoch { get; set; } = "";
    public decimal TickSize { get; set; }
    public string TimestampPolicy { get; set; } = "";

    // ---- source provenance ----
    public string SourcePolicyVersion { get; set; } = "";
    public string SourceSnapshotVersion { get; set; } = "";
    public long StateVersion { get; set; }
    public long EventRevision { get; set; }
    public string InputFingerprint { get; set; } = "";
    public string MeasurementStatus { get; set; } = "";
    public string ClassificationState { get; set; } = "";
    public string CoverageMode { get; set; } = "";
    public string DataQuality { get; set; } = "";
    public string Availability { get; set; } = "";
    public bool IsFrozen { get; set; }
    public string[] SourceLimitations { get; set; } = Array.Empty<string>();

    // ---- original measurement timestamps, preserved ----
    public DateTime ObservationStartedAtUtc { get; set; }
    public DateTime? FirstInputAtUtc { get; set; }
    public DateTime? LastInputAtUtc { get; set; }

    /// <summary>Diagnostic only — when the bridge ran, never when the market did anything.</summary>
    public DateTime CollectedAtUtc { get; set; }

    // ---- raw vectors, unnormalized ----
    public string ResultDirection { get; set; } = "";
    public ResearchEffortVector Effort { get; set; } = new();
    public ResearchResultVector Result { get; set; } = new();
    public ResearchRawRelationships RawRelationships { get; set; } = new();
}

/// <summary>The Effort vector as collected. Every optional measure stays optional.</summary>
public sealed class ResearchEffortVector
{
    public decimal TotalExecutedVolume { get; set; }
    public long TradeCount { get; set; }
    public int PriceLevelCount { get; set; }
    public decimal ClassifiedVolume { get; set; }
    public decimal? AskVolume { get; set; }
    public decimal? BidVolume { get; set; }
    public decimal UnknownAggressorVolume { get; set; }
    public decimal ClassifiedDelta { get; set; }
    public decimal? AbsoluteClassifiedDelta { get; set; }
    public decimal? ClassifiedCvdChange { get; set; }
    public decimal? AggressorCoverageRatio { get; set; }
    public TimeSpan? ObservationDuration { get; set; }
    public TimeSpan? MinimumTradeInterval { get; set; }
    public TimeSpan? MaximumTradeInterval { get; set; }
    public TimeSpan? MeanTradeInterval { get; set; }
    public TimeSpan? LatestTradeInterval { get; set; }
    public decimal? TradesPerSecondRaw { get; set; }
    public decimal? ContractsPerSecondRaw { get; set; }
    public decimal? MaximumLevelExecutedVolume { get; set; }
    public long? MaximumLevelTradeCount { get; set; }
    public decimal? MaximumAbsoluteLevelDelta { get; set; }
    public int RevisitedLevelCount { get; set; }
    public int MaximumVisitCount { get; set; }
    public int ClassifiedLevelCount { get; set; }
    public int UnknownOnlyLevelCount { get; set; }
    public int SamePriceRatioAvailabilityCount { get; set; }
    public int DiagonalRatioAvailabilityCount { get; set; }
    public int RawAskDominantLevelCount { get; set; }
    public int RawBidDominantLevelCount { get; set; }
    public int RawEqualLevelCount { get; set; }
    public int RawUnknownDominantLevelCount { get; set; }
    public int MaximumConsecutiveRawAskDominanceTicks { get; set; }
    public int MaximumConsecutiveRawBidDominanceTicks { get; set; }
    public int ClusterPopulationSize { get; set; }
    public string EvidenceAvailability { get; set; } = "";
    public string[] Limitations { get; set; } = Array.Empty<string>();
}

/// <summary>The Result vector as collected. Observable at close — not a future outcome.</summary>
public sealed class ResearchResultVector
{
    public long? FirstPriceTick { get; set; }
    public long? LatestPriceTick { get; set; }
    public long? HighPriceTick { get; set; }
    public long? LowPriceTick { get; set; }
    public long? NetPriceProgressTicks { get; set; }
    public long? GrossRangeTicks { get; set; }
    public long? MaximumFavorableProgressTicks { get; set; }
    public long? MaximumAdverseProgressTicks { get; set; }
    public long? ProgressRetainedTicks { get; set; }
    public decimal? ProgressRetentionRatio { get; set; }
    public TimeSpan? TimeToMaximumFavorableProgress { get; set; }
    public TimeSpan? TimeToLatestProgress { get; set; }
    public TimeSpan? TimeAtMaximumExcursion { get; set; }
    public long? EpisodeReferenceDistanceStartTicks { get; set; }
    public long? EpisodeReferenceDistanceLatestTicks { get; set; }
    public long? MaximumDistanceFromReferenceTicks { get; set; }
    public long? CurrentDistanceFromReferenceTicks { get; set; }
    public bool? GeometricReentryObserved { get; set; }
    public TimeSpan? TimeMaintainedInside { get; set; }
    public decimal? OutsideTimeRatio { get; set; }
    public decimal? OutsideVolumeRatio { get; set; }
    public decimal? OutsideTradeRatio { get; set; }
    public long? LocalPocTick { get; set; }
    public long? LocalPocDisplacementTicks { get; set; }
    public long? DevelopingTpoPocStartTick { get; set; }
    public long? DevelopingTpoPocLatestTick { get; set; }
    public long? TpoPocMigrationTicks { get; set; }
    public long? DevelopingVolumePocStartTick { get; set; }
    public long? DevelopingVolumePocLatestTick { get; set; }
    public long? VolumePocMigrationTicks { get; set; }
    public long? DevelopingTpoValueLowStartTick { get; set; }
    public long? DevelopingTpoValueHighStartTick { get; set; }
    public long? DevelopingTpoValueLowLatestTick { get; set; }
    public long? DevelopingTpoValueHighLatestTick { get; set; }
    public long? DevelopingVolumeValueLowStartTick { get; set; }
    public long? DevelopingVolumeValueHighStartTick { get; set; }
    public long? DevelopingVolumeValueLowLatestTick { get; set; }
    public long? DevelopingVolumeValueHighLatestTick { get; set; }
    public long? TpoValueCentroidMigrationTicks { get; set; }
    public long? VolumeValueCentroidMigrationTicks { get; set; }
    public string? PriceLocationAtStart { get; set; }
    public string? PriceLocationLatest { get; set; }
    public string EvidenceAvailability { get; set; } = "";
    public string[] Limitations { get; set; } = Array.Empty<string>();
}

/// <summary>Descriptive raw relationships as collected — not an EfficiencyScore.</summary>
public sealed class ResearchRawRelationships
{
    public decimal? NetProgressPerExecutedContract { get; set; }
    public decimal? GrossRangePerExecutedContract { get; set; }
    public decimal? FavorableProgressPerExecutedContract { get; set; }
    public decimal? NetProgressPerTrade { get; set; }
    public decimal? FavorableProgressPerTrade { get; set; }
    public decimal? ExecutedContractsPerTickOfGrossRange { get; set; }
    public decimal? ClassifiedAbsoluteDeltaPerTickOfGrossRange { get; set; }
    public decimal? ClassifiedAbsoluteDeltaPerTickOfFavorableProgress { get; set; }
    public decimal? TimePerNetProgressTick { get; set; }
    public decimal? TimePerFavorableProgressTick { get; set; }
    public decimal? TradesPerNetProgressTick { get; set; }
    public decimal? VolumePerNetProgressTick { get; set; }
}
