using GC.AuctionFlow.Thesis;

namespace GC.AuctionFlow.Maturity;

/// <summary>
/// Immutable Phase 3B signal maturity snapshot for one thesis scope.
/// MaturityLevel is always NotCalibrated — Fast/Standard/Confirmed are reserved.
/// </summary>
public sealed class SignalMaturitySnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public SignalMaturitySnapshot(
        string snapshotId,
        string policyVersion,
        string thesisId,
        string thesisFamily,
        string evidenceId,
        string episodeId,
        string referenceId,
        ThesisDirection direction,
        AnalysisLifecycleState lifecycleState,
        SignalMaturityLevel maturityLevel,
        ExpectedBehaviorContractKind expectedBehavior,
        DateTime? expectedBehaviorDeadlineUtc,
        RetestObservationState retestObservation,
        bool microConfirmationObserved,
        bool notCalibrated,
        IReadOnlyList<MaturityBlockingReason> blockingReasons,
        MaturityDataQuality dataQuality,
        long stateVersion,
        long eventRevision,
        DateTime observedAtUtc,
        IReadOnlyList<string> limitations)
    {
        SnapshotId = snapshotId ?? "";
        PolicyVersion = policyVersion ?? SignalMaturityPolicyConfig.PolicyVersion;
        ThesisId = thesisId ?? "";
        ThesisFamily = thesisFamily ?? "";
        EvidenceId = evidenceId ?? "";
        EpisodeId = episodeId ?? "";
        ReferenceId = referenceId ?? "";
        Direction = direction;
        LifecycleState = lifecycleState;
        MaturityLevel = maturityLevel;
        ExpectedBehavior = expectedBehavior;
        ExpectedBehaviorDeadlineUtc = expectedBehaviorDeadlineUtc;
        RetestObservation = retestObservation;
        MicroConfirmationObserved = microConfirmationObserved;
        NotCalibrated = notCalibrated;
        BlockingReasons = blockingReasons ?? Array.Empty<MaturityBlockingReason>();
        DataQuality = dataQuality;
        StateVersion = stateVersion;
        EventRevision = eventRevision;
        ObservedAtUtc = observedAtUtc;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public string SnapshotId { get; }
    public string PolicyVersion { get; }
    public string ThesisId { get; }
    /// <summary>"FAR" or "AAC".</summary>
    public string ThesisFamily { get; }
    public string EvidenceId { get; }
    public string EpisodeId { get; }
    public string ReferenceId { get; }
    public ThesisDirection Direction { get; }
    public AnalysisLifecycleState LifecycleState { get; }
    public SignalMaturityLevel MaturityLevel { get; }
    public ExpectedBehaviorContractKind ExpectedBehavior { get; }
    /// <summary>Always null in Phase 3B — deadline duration NOT CALIBRATED.</summary>
    public DateTime? ExpectedBehaviorDeadlineUtc { get; }
    public RetestObservationState RetestObservation { get; }
    /// <summary>Always false in Phase 3B — micro-confirmation criteria NOT CALIBRATED.</summary>
    public bool MicroConfirmationObserved { get; }
    public bool NotCalibrated { get; }
    public IReadOnlyList<MaturityBlockingReason> BlockingReasons { get; }
    public MaturityDataQuality DataQuality { get; }
    public long StateVersion { get; }
    public long EventRevision { get; }
    public DateTime ObservedAtUtc { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;
}

/// <summary>
/// Immutable Phase 3B signal maturity set snapshot.
/// FastCount / StandardCount / ConfirmedCount always 0 — NOT CALIBRATED.
/// </summary>
public sealed class SignalMaturitySetSnapshot
{
    public const string SnapshotVersion = "1.0.0";
    public const int RecentlyClosedCapacity = 64;

    public SignalMaturitySetSnapshot(
        MaturityModuleState moduleState,
        string policyVersion,
        IReadOnlyList<SignalMaturitySnapshot> activeCandidates,
        IReadOnlyList<SignalMaturitySnapshot> recentlyClosed,
        SignalMaturitySnapshot? latestUpdated,
        int candidateCount,
        int fastCount,
        int standardCount,
        int confirmedCount,
        bool fastShadowOnly,
        DateTime createdAtUtc,
        DateTime lastUpdatedAtUtc,
        IReadOnlyList<string> limitations)
    {
        ModuleState = moduleState;
        PolicyVersion = policyVersion ?? SignalMaturityPolicyConfig.PolicyVersion;
        ActiveCandidates = activeCandidates ?? Array.Empty<SignalMaturitySnapshot>();
        RecentlyClosed = recentlyClosed ?? Array.Empty<SignalMaturitySnapshot>();
        LatestUpdated = latestUpdated;
        CandidateCount = candidateCount;
        FastCount = fastCount;
        StandardCount = standardCount;
        ConfirmedCount = confirmedCount;
        FastShadowOnly = fastShadowOnly;
        CreatedAtUtc = createdAtUtc;
        LastUpdatedAtUtc = lastUpdatedAtUtc;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public MaturityModuleState ModuleState { get; }
    public string PolicyVersion { get; }
    public IReadOnlyList<SignalMaturitySnapshot> ActiveCandidates { get; }
    public IReadOnlyList<SignalMaturitySnapshot> RecentlyClosed { get; }
    public SignalMaturitySnapshot? LatestUpdated { get; }
    public int CandidateCount { get; }
    /// <summary>Always 0 — NOT CALIBRATED.</summary>
    public int FastCount { get; }
    /// <summary>Always 0 — NOT CALIBRATED.</summary>
    public int StandardCount { get; }
    /// <summary>Always 0 — NOT CALIBRATED.</summary>
    public int ConfirmedCount { get; }
    /// <summary>Always true — v1.2 §29.5 FAST deployment guardrail.</summary>
    public bool FastShadowOnly { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime LastUpdatedAtUtc { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;
}
