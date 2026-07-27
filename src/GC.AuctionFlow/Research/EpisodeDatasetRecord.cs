using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Reference;

namespace GC.AuctionFlow.Research;

/// <summary>
/// One episode, as raw features.
///
/// v1.2 §46.3 is explicit that logging signal outcomes is not enough: enough raw
/// features must be kept that a different rule version can be re-run later without
/// re-collecting the data. So this carries measurements, not conclusions — no episode
/// here is labelled good, failed, strong or weak.
///
/// Unavailable measurements are null. Writing zero for an unobserved delta would be a
/// fabricated observation, and a distribution built on fabricated zeros is worse than a
/// smaller honest one (v1.3 `G-ACC-003`).
/// </summary>
public sealed class EpisodeDatasetRecord
{
    public EpisodeDatasetRecord(
        string episodeId,
        string primaryAuctionId,
        string referenceId,
        ReferenceType referenceType,
        ReferenceMaturity referenceMaturity,
        ReferenceSourceHorizon sourceHorizon,
        EpisodeInteractionDirection interactionDirection,
        EpisodeState terminalState,
        EpisodeResolution resolution,
        DateTime startedAtUtc,
        DateTime lastUpdatedAtUtc,
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
        decimal? canonicalOutsideDelta,
        AggressorEvidenceAvailability aggressorEvidence,
        EpisodeDataQuality dataQuality,
        DatasetRowAdmissibility admissibility,
        string episodePolicyVersion,
        long stateVersion)
    {
        EpisodeId = episodeId ?? "";
        PrimaryAuctionId = primaryAuctionId ?? "";
        ReferenceId = referenceId ?? "";
        ReferenceType = referenceType;
        ReferenceMaturity = referenceMaturity;
        SourceHorizon = sourceHorizon;
        InteractionDirection = interactionDirection;
        TerminalState = terminalState;
        Resolution = resolution;
        StartedAtUtc = startedAtUtc;
        LastUpdatedAtUtc = lastUpdatedAtUtc;
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
        CanonicalOutsideDelta = canonicalOutsideDelta;
        AggressorEvidence = aggressorEvidence;
        DataQuality = dataQuality;
        Admissibility = admissibility;
        EpisodePolicyVersion = episodePolicyVersion ?? "";
        StateVersion = stateVersion;
    }

    // ---- identity and provenance ----

    public string EpisodeId { get; }
    public string PrimaryAuctionId { get; }
    public string ReferenceId { get; }

    /// <summary>Policy that produced the episode, so a re-run knows what it is reading.</summary>
    public string EpisodePolicyVersion { get; }

    public long StateVersion { get; }

    // ---- stratification keys ----
    //
    // Only ReferenceType is usable today. The other two axes G-CAL-002 requires do not
    // exist as classifiers, which is why CalibrationProtocol blocks at step two.

    public ReferenceType ReferenceType { get; }
    public ReferenceMaturity ReferenceMaturity { get; }
    public ReferenceSourceHorizon SourceHorizon { get; }
    public EpisodeInteractionDirection InteractionDirection { get; }

    // ---- outcome, unlabelled ----

    public EpisodeState TerminalState { get; }
    public EpisodeResolution Resolution { get; }
    public DateTime StartedAtUtc { get; }
    public DateTime LastUpdatedAtUtc { get; }
    public TimeSpan Duration => LastUpdatedAtUtc - StartedAtUtc;

    // ---- raw measurements ----

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
    public decimal? CanonicalOutsideDelta { get; }
    public AggressorEvidenceAvailability AggressorEvidence { get; }
    public EpisodeDataQuality DataQuality { get; }
    public DatasetRowAdmissibility Admissibility { get; }

    /// <summary>
    /// Excursion in the direction the episode was testing.
    ///
    /// This is the raw MAE/MFE pair v1.2 §29.5 requires for stratified evaluation. It is
    /// a re-projection of the two distance measurements onto the interaction direction —
    /// arithmetic, not a judgement about whether the excursion was adverse in any sense
    /// that matters to a trade. Null for Unknown and Bidirectional, because signing an
    /// excursion against an undetermined direction would invent the sign.
    /// </summary>
    public long? FavourableExcursionTicks => InteractionDirection switch
    {
        EpisodeInteractionDirection.Up => MaximumAboveDistanceTicks,
        EpisodeInteractionDirection.Down => MaximumBelowDistanceTicks,
        _ => null,
    };

    /// <summary>Excursion against the direction the episode was testing. See above.</summary>
    public long? AdverseExcursionTicks => InteractionDirection switch
    {
        EpisodeInteractionDirection.Up => MaximumBelowDistanceTicks,
        EpisodeInteractionDirection.Down => MaximumAboveDistanceTicks,
        _ => null,
    };

    /// <summary>
    /// Builds a row from a closed episode.
    ///
    /// Admissibility is recorded rather than filtered on: a row dropped at collection
    /// time cannot be reconsidered by a later rule version, which is the whole point of
    /// keeping raw features.
    /// </summary>
    public static EpisodeDatasetRecord FromEpisode(AuctionEpisodeSnapshot episode)
    {
        ArgumentNullException.ThrowIfNull(episode);

        var admissibility = episode.DataQuality switch
        {
            EpisodeDataQuality.Invalid => DatasetRowAdmissibility.InvalidData,
            _ when episode.State == EpisodeState.InvalidData => DatasetRowAdmissibility.InvalidData,
            _ when episode.AggressorEvidenceAvailability == AggressorEvidenceAvailability.Unavailable
                => DatasetRowAdmissibility.AggressorEvidenceMissing,
            _ => DatasetRowAdmissibility.Admissible,
        };

        return new EpisodeDatasetRecord(
            episode.EpisodeId,
            episode.PrimaryAuctionId,
            episode.ReferenceId,
            episode.ReferenceType,
            episode.ReferenceMaturity,
            episode.SourceHorizon,
            episode.InteractionDirection,
            episode.State,
            episode.Resolution,
            episode.StartedAtUtc,
            episode.LastUpdatedAtUtc,
            episode.AttemptCount,
            episode.InteractionCount,
            episode.CrossCount,
            episode.UpExcursionCount,
            episode.DownExcursionCount,
            episode.MaximumAboveDistanceTicks,
            episode.MaximumBelowDistanceTicks,
            episode.MaximumCanonicalOutsideDistanceTicks,
            episode.CanonicalOutsideDuration,
            episode.CanonicalOutsideExecutedVolume,
            episode.CanonicalOutsideTradeCount,
            episode.CanonicalOutsideDelta,
            episode.AggressorEvidenceAvailability,
            episode.DataQuality,
            admissibility,
            episode.PolicyVersion,
            episode.StateVersion);
    }
}
