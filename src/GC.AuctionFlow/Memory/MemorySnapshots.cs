using GC.AuctionFlow.Episode;

namespace GC.AuctionFlow.Memory;

/// <summary>
/// One recorded test of a reference. Mechanical facts only.
/// </summary>
public sealed class ReferenceTestRecord
{
    public ReferenceTestRecord(
        string episodeId,
        DateTime startedAtUtc,
        DateTime lastUpdatedAtUtc,
        EpisodeState finalState,
        EpisodeResolution resolution,
        int attemptCount,
        ReferenceTestOutcome outcome)
    {
        EpisodeId = episodeId ?? "";
        StartedAtUtc = startedAtUtc;
        LastUpdatedAtUtc = lastUpdatedAtUtc;
        FinalState = finalState;
        Resolution = resolution;
        AttemptCount = attemptCount;
        Outcome = outcome;
    }

    public string EpisodeId { get; }
    public DateTime StartedAtUtc { get; }
    public DateTime LastUpdatedAtUtc { get; }
    public EpisodeState FinalState { get; }
    public EpisodeResolution Resolution { get; }

    /// <summary>Outside attempts within this single test.</summary>
    public int AttemptCount { get; }

    public ReferenceTestOutcome Outcome { get; }

    public bool IsClosed => Outcome != ReferenceTestOutcome.InProgress;
}

/// <summary>
/// Accumulated memory for one reference (v1.3 §13).
///
/// v1.3 G-REF-001: counts are recorded, significance is not inferred. A reference
/// tested fifty times reports fifty tests and <see cref="ReferenceStrengthState.NotCalibrated"/>,
/// exactly as one tested once does.
/// </summary>
public sealed class ReferenceMemorySnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public ReferenceMemorySnapshot(
        string referenceId,
        DateTime firstTestAtUtc,
        DateTime lastTestAtUtc,
        int testCount,
        IReadOnlyList<ReferenceTestRecord> records,
        ReferenceStrengthState strengthState,
        LiquidityReplenishmentObservability replenishment,
        IReadOnlyList<string> limitations)
    {
        ReferenceId = referenceId ?? "";
        FirstTestAtUtc = firstTestAtUtc;
        LastTestAtUtc = lastTestAtUtc;
        TestCount = testCount;
        Records = records ?? Array.Empty<ReferenceTestRecord>();
        StrengthState = strengthState;
        Replenishment = replenishment;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public string ReferenceId { get; }

    /// <summary>When this reference was first interacted with, since indicator start.</summary>
    public DateTime FirstTestAtUtc { get; }

    public DateTime LastTestAtUtc { get; }

    /// <summary>Total tests observed, including the first.</summary>
    public int TestCount { get; }

    /// <summary>Tests after the first. Never negative.</summary>
    public int RetestCount => TestCount > 0 ? TestCount - 1 : 0;

    /// <summary>Ordered oldest-first, capped at the policy record capacity.</summary>
    public IReadOnlyList<ReferenceTestRecord> Records { get; }

    /// <summary>Always NotCalibrated — G-REF-001 forbids inferring decay or reinforcement.</summary>
    public ReferenceStrengthState StrengthState { get; }

    /// <summary>Always Unavailable while MBO is BLOCKED.</summary>
    public LiquidityReplenishmentObservability Replenishment { get; }

    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;

    public bool HasBeenRetested => RetestCount > 0;

    public int ClosedTestCount => Records.Count(r => r.IsClosed);
}

/// <summary>
/// Immutable Phase 1I price memory set.
/// LIVE_ONLY: the ledger begins at indicator start and is not reconstructed.
/// </summary>
public sealed class PriceMemorySetSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public PriceMemorySetSnapshot(
        MemoryModuleState moduleState,
        string policyVersion,
        IReadOnlyList<ReferenceMemorySnapshot> references,
        ReferenceMemorySnapshot? mostRecentlyTested,
        int trackedReferenceCount,
        int retestedReferenceCount,
        long totalTestsObserved,
        DateTime memoryStartedAtUtc,
        DateTime lastUpdatedAtUtc,
        IReadOnlyList<string> limitations)
    {
        ModuleState = moduleState;
        PolicyVersion = policyVersion ?? PriceMemoryPolicyConfig.PolicyVersion;
        References = references ?? Array.Empty<ReferenceMemorySnapshot>();
        MostRecentlyTested = mostRecentlyTested;
        TrackedReferenceCount = trackedReferenceCount;
        RetestedReferenceCount = retestedReferenceCount;
        TotalTestsObserved = totalTestsObserved;
        MemoryStartedAtUtc = memoryStartedAtUtc;
        LastUpdatedAtUtc = lastUpdatedAtUtc;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public MemoryModuleState ModuleState { get; }
    public string PolicyVersion { get; }
    public IReadOnlyList<ReferenceMemorySnapshot> References { get; }
    public ReferenceMemorySnapshot? MostRecentlyTested { get; }
    public int TrackedReferenceCount { get; }
    public int RetestedReferenceCount { get; }
    public long TotalTestsObserved { get; }

    /// <summary>
    /// When the ledger began. Everything before this is unknown, not absent —
    /// LIVE_ONLY means a reference may have been tested many times before start.
    /// </summary>
    public DateTime MemoryStartedAtUtc { get; }

    public DateTime LastUpdatedAtUtc { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;

    public ReferenceMemorySnapshot? ForReference(string referenceId) =>
        References.FirstOrDefault(r =>
            string.Equals(r.ReferenceId, referenceId, StringComparison.Ordinal));
}
