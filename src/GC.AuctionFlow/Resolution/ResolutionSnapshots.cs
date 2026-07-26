using GC.AuctionFlow.Evidence;

namespace GC.AuctionFlow.Resolution;

/// <summary>
/// Old-value reclaim observation (v1.3 §6.2 G-ACC-005).
/// Carries the raw research measurements for the reclaim test plus a time bound;
/// the held-vs-failed verdict itself stays calibration-gated.
///
/// Supersedes <c>AcceptanceEvidenceVector.OldValueReclaimFailure</c>, a nullable bool
/// that no producer ever set. That field is left in place because Phase 1F is LOCKED.
/// </summary>
public sealed class OldValueReclaimObservation
{
    public OldValueReclaimObservation(
        OldValueReclaimState state,
        int attemptCount,
        TimeSpan? timeMaintainedInside,
        long? maximumDistanceReturnedInsideTicks,
        long? currentDistanceInsideTicks,
        bool? localValueRebuildInside,
        decimal? insideExecutedVolumeAfterReentry,
        TimeSpan? elapsedSinceLastInsideEvent,
        IReadOnlyList<string> limitations)
    {
        State = state;
        AttemptCount = attemptCount;
        TimeMaintainedInside = timeMaintainedInside;
        MaximumDistanceReturnedInsideTicks = maximumDistanceReturnedInsideTicks;
        CurrentDistanceInsideTicks = currentDistanceInsideTicks;
        LocalValueRebuildInside = localValueRebuildInside;
        InsideExecutedVolumeAfterReentry = insideExecutedVolumeAfterReentry;
        ElapsedSinceLastInsideEvent = elapsedSinceLastInsideEvent;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public OldValueReclaimState State { get; }

    /// <summary>Observable count of outside attempts against the reference.</summary>
    public int AttemptCount { get; }

    // --- raw research measurements; null means unavailable, never 0 (G-ACC-003) ---

    public TimeSpan? TimeMaintainedInside { get; }
    public long? MaximumDistanceReturnedInsideTicks { get; }
    public long? CurrentDistanceInsideTicks { get; }
    public bool? LocalValueRebuildInside { get; }
    public decimal? InsideExecutedVolumeAfterReentry { get; }

    /// <summary>
    /// Time bound required by G-ACC-005: how long since price was last inside.
    /// The window LENGTH that would decide held-vs-failed is NOT CALIBRATED.
    /// </summary>
    public TimeSpan? ElapsedSinceLastInsideEvent { get; }

    public IReadOnlyList<string> Limitations { get; }

    /// <summary>True only for the observable states, never for a calibrated verdict.</summary>
    public bool IsObservableOnly =>
        State == OldValueReclaimState.Unknown
        || State == OldValueReclaimState.NotAttempted
        || State == OldValueReclaimState.AttemptedOutcomeNotCalibrated;
}

/// <summary>
/// Immutable resolution snapshot for one EvidenceId.
/// AcceptanceResolution/ReentryResolution are always NotCalibrated for Established/Failed/Stable tiers.
/// Conclusion is always NotCalibrated (FAR/AAC require calibration).
/// </summary>
public sealed class AuctionResolutionSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public AuctionResolutionSnapshot(
        string resolutionId,
        string policyVersion,
        string evidenceId,
        string episodeId,
        string primaryAuctionId,
        string referenceId,
        AcceptanceResolutionState acceptanceResolution,
        ReentryResolutionState reentryResolution,
        AuctionResolutionConclusion conclusion,
        OldValueReclaimObservation oldValueReclaim,
        AcceptanceObservationState inputAcceptanceObservation,
        ReentryObservationState inputReentryObservation,
        long stateVersion,
        long eventRevision,
        ResolutionDataQuality dataQuality,
        DateTime lastUpdatedAtUtc,
        IReadOnlyList<string> limitations)
    {
        ResolutionId = resolutionId ?? "";
        PolicyVersion = policyVersion ?? AuctionResolutionPolicyConfig.PolicyVersion;
        EvidenceId = evidenceId ?? "";
        EpisodeId = episodeId ?? "";
        PrimaryAuctionId = primaryAuctionId ?? "";
        ReferenceId = referenceId ?? "";
        AcceptanceResolution = acceptanceResolution;
        ReentryResolution = reentryResolution;
        Conclusion = conclusion;
        OldValueReclaim = oldValueReclaim;
        InputAcceptanceObservation = inputAcceptanceObservation;
        InputReentryObservation = inputReentryObservation;
        StateVersion = stateVersion;
        EventRevision = eventRevision;
        DataQuality = dataQuality;
        LastUpdatedAtUtc = lastUpdatedAtUtc;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public string ResolutionId { get; }
    public string PolicyVersion { get; }
    public string EvidenceId { get; }
    public string EpisodeId { get; }
    public string PrimaryAuctionId { get; }
    public string ReferenceId { get; }

    /// <summary>Acceptance conclusion — never Established/Failed in Phase 2D (NOT_CALIBRATED).</summary>
    public AcceptanceResolutionState AcceptanceResolution { get; }

    /// <summary>Re-entry conclusion — never StableReacceptance/ReentryFailed in Phase 2D (NOT_CALIBRATED).</summary>
    public ReentryResolutionState ReentryResolution { get; }

    /// <summary>Overall auction conclusion — always NotCalibrated in Phase 2D. FAR/AAC NOT AUTHORIZED.</summary>
    public AuctionResolutionConclusion Conclusion { get; }

    /// <summary>Old-value reclaim test — the FAR-vs-AAC decision axis (v1.3 §6.2).</summary>
    public OldValueReclaimObservation OldValueReclaim { get; }

    /// <summary>Input evidence acceptance observation state (from Phase 1F).</summary>
    public AcceptanceObservationState InputAcceptanceObservation { get; }

    /// <summary>Input evidence reentry observation state (from Phase 1F).</summary>
    public ReentryObservationState InputReentryObservation { get; }

    public long StateVersion { get; }
    public long EventRevision { get; }
    public ResolutionDataQuality DataQuality { get; }
    public DateTime LastUpdatedAtUtc { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;
}

/// <summary>Immutable set-level resolution snapshot for GPS card and runtime publication.</summary>
public sealed class AuctionResolutionSetSnapshot
{
    public const string SnapshotVersion = "1.0.0";
    public const int RecentlyClosedCapacity = 64;

    public AuctionResolutionSetSnapshot(
        ResolutionModuleState moduleState,
        string policyVersion,
        IReadOnlyList<AuctionResolutionSnapshot> activeResolutions,
        IReadOnlyList<AuctionResolutionSnapshot> recentlyClosedResolutions,
        AuctionResolutionSnapshot? latestUpdated,
        int readyCount,
        int partialCount,
        DateTime createdAtUtc,
        DateTime lastUpdatedAtUtc,
        IReadOnlyList<string> limitations)
    {
        ModuleState = moduleState;
        PolicyVersion = policyVersion ?? AuctionResolutionPolicyConfig.PolicyVersion;
        ActiveResolutions = activeResolutions ?? Array.Empty<AuctionResolutionSnapshot>();
        RecentlyClosedResolutions = recentlyClosedResolutions ?? Array.Empty<AuctionResolutionSnapshot>();
        LatestUpdated = latestUpdated;
        ReadyCount = readyCount;
        PartialCount = partialCount;
        CreatedAtUtc = createdAtUtc;
        LastUpdatedAtUtc = lastUpdatedAtUtc;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public ResolutionModuleState ModuleState { get; }
    public string PolicyVersion { get; }
    public IReadOnlyList<AuctionResolutionSnapshot> ActiveResolutions { get; }
    public IReadOnlyList<AuctionResolutionSnapshot> RecentlyClosedResolutions { get; }
    public AuctionResolutionSnapshot? LatestUpdated { get; }
    public int ReadyCount { get; }
    public int PartialCount { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime LastUpdatedAtUtc { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;
}
