using GC.AuctionFlow.Episode;

namespace GC.AuctionFlow.Memory;

/// <summary>
/// Phase 1I Price Memory host (v1.3 §13, KDK Ch 27).
///
/// Accumulates, per reference, how many times it has been tested and how each test
/// ended. Every episode against a reference counts as one test; the first is the
/// first test, the rest are retests.
///
/// It never concludes that a reference has weakened or strengthened. KDK Ch 27 is
/// explicit that passive liquidity can be replenished between tests, so a raw count
/// carries no directional meaning (v1.3 G-REF-001).
///
/// Unlike the other hosts this one is stateful across rebuilds by design: memory that
/// forgets between snapshots is not memory. It is still LIVE_ONLY — the ledger starts
/// at indicator start and is never reconstructed.
/// </summary>
public sealed class PriceMemoryHost
{
    private sealed class ReferenceLedger
    {
        public DateTime FirstTestAtUtc;
        public DateTime LastTestAtUtc;

        /// <summary>
        /// Every test ever folded, independent of how many records are retained.
        /// Truncating the record list must not rewrite the count: a reference tested
        /// 74 times that reports 64 is a quiet lie.
        /// </summary>
        public int TotalTests;
        public readonly List<ReferenceTestRecord> Records = new();
        public readonly Dictionary<string, int> RecordIndexByEpisodeId = new(StringComparer.Ordinal);
    }

    private PriceMemoryPolicyConfig _policy;
    private PriceMemorySetSnapshot? _published;
    private DateTime _memoryStartedAtUtc;
    private long _totalTests;
    private readonly Dictionary<string, ReferenceLedger> _ledgers = new(StringComparer.Ordinal);

    public PriceMemoryHost(PriceMemoryPolicyConfig? policy = null)
    {
        _policy = policy ?? new PriceMemoryPolicyConfig(enabled: false);
    }

    public PriceMemorySetSnapshot? Current => _published;
    public PriceMemoryPolicyConfig Policy => _policy;

    public void Configure(PriceMemoryPolicyConfig policy)
    {
        if (policy is null) throw new ArgumentNullException(nameof(policy));
        _policy = policy;
        if (!_policy.Enabled)
        {
            ResetInternal();
            _published = DisabledSnapshot(DateTime.UtcNow);
        }
    }

    /// <summary>Clears the ledger. The memory window restarts from the next rebuild.</summary>
    public void Reset()
    {
        ResetInternal();
        _published = null;
    }

    /// <summary>
    /// Fold the current episode set into the ledger.
    /// Not fingerprint-gated: an unchanged episode set still needs its open tests
    /// reflected, and folding is idempotent per EpisodeId.
    /// </summary>
    public PriceMemorySetSnapshot Rebuild(
        AuctionEpisodeSetSnapshot? episodes,
        DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;

        if (!_policy.Enabled)
        {
            ResetInternal();
            _published = DisabledSnapshot(now);
            return _published;
        }

        if (_memoryStartedAtUtc == default)
            _memoryStartedAtUtc = now;

        if (episodes is null || episodes.ModuleState == EpisodeModuleState.Disabled)
        {
            _published = BuildSnapshot(MemoryModuleState.AwaitingEpisodes, now);
            return _published;
        }

        if (episodes.ModuleState == EpisodeModuleState.Invalid)
        {
            _published = BuildSnapshot(MemoryModuleState.Invalid, now,
                new[] { "EPISODE_INPUT_INVALID" });
            return _published;
        }

        foreach (var e in episodes.ActiveEpisodes)
            Fold(e);
        foreach (var e in episodes.RecentlyClosedEpisodes)
            Fold(e);

        var state = _ledgers.Count == 0
            ? MemoryModuleState.AwaitingEpisodes
            : episodes.ModuleState == EpisodeModuleState.Partial
                ? MemoryModuleState.Partial
                : MemoryModuleState.Ready;

        _published = BuildSnapshot(state, now);
        return _published;
    }

    // --- folding ---

    private void Fold(AuctionEpisodeSnapshot e)
    {
        if (string.IsNullOrEmpty(e.ReferenceId) || string.IsNullOrEmpty(e.EpisodeId))
            return;

        if (!_ledgers.TryGetValue(e.ReferenceId, out var ledger))
        {
            EvictOldestReferenceIfNeeded();
            ledger = new ReferenceLedger
            {
                FirstTestAtUtc = e.StartedAtUtc,
                LastTestAtUtc = e.LastUpdatedAtUtc
            };
            _ledgers[e.ReferenceId] = ledger;
        }

        var record = new ReferenceTestRecord(
            e.EpisodeId, e.StartedAtUtc, e.LastUpdatedAtUtc,
            e.State, e.Resolution, e.AttemptCount,
            MapOutcome(e.State, e.Resolution));

        // The same episode is folded repeatedly as it evolves; update in place so a
        // single test never inflates the count.
        if (ledger.RecordIndexByEpisodeId.TryGetValue(e.EpisodeId, out var idx))
        {
            ledger.Records[idx] = record;
        }
        else
        {
            ledger.Records.Add(record);
            ledger.RecordIndexByEpisodeId[e.EpisodeId] = ledger.Records.Count - 1;
            ledger.TotalTests++;
            _totalTests++;
            EvictOldestRecordIfNeeded(ledger);
        }

        if (e.StartedAtUtc < ledger.FirstTestAtUtc) ledger.FirstTestAtUtc = e.StartedAtUtc;
        if (e.LastUpdatedAtUtc > ledger.LastTestAtUtc) ledger.LastTestAtUtc = e.LastUpdatedAtUtc;
    }

    /// <summary>
    /// Held-vs-broken needs calibrated acceptance and re-entry resolution, so a closed
    /// test reports NotCalibrated rather than a verdict. Only mechanical terminations
    /// are stated plainly.
    /// </summary>
    private static ReferenceTestOutcome MapOutcome(EpisodeState state, EpisodeResolution resolution)
    {
        if (resolution == EpisodeResolution.Expired) return ReferenceTestOutcome.Expired;
        if (resolution == EpisodeResolution.InvalidData) return ReferenceTestOutcome.InvalidData;

        return state switch
        {
            EpisodeState.EpisodeExpired => ReferenceTestOutcome.Expired,
            EpisodeState.InvalidData => ReferenceTestOutcome.InvalidData,
            EpisodeState.Interacting => ReferenceTestOutcome.InProgress,
            EpisodeState.OutsideAttempt => ReferenceTestOutcome.InProgress,
            EpisodeState.Developing => ReferenceTestOutcome.InProgress,
            EpisodeState.ReentryDeveloping => ReferenceTestOutcome.InProgress,
            // Any reserved episode state implies a calibrated conclusion upstream.
            _ => ReferenceTestOutcome.NotCalibrated
        };
    }

    private void EvictOldestRecordIfNeeded(ReferenceLedger ledger)
    {
        if (ledger.Records.Count <= PriceMemoryPolicyConfig.TestRecordCapacity) return;

        var removed = ledger.Records[0];
        ledger.Records.RemoveAt(0);
        ledger.RecordIndexByEpisodeId.Remove(removed.EpisodeId);
        for (var i = 0; i < ledger.Records.Count; i++)
            ledger.RecordIndexByEpisodeId[ledger.Records[i].EpisodeId] = i;
    }

    private void EvictOldestReferenceIfNeeded()
    {
        if (_ledgers.Count < PriceMemoryPolicyConfig.ReferenceCapacity) return;

        var oldest = _ledgers.OrderBy(kv => kv.Value.LastTestAtUtc).FirstOrDefault();
        if (oldest.Key is not null) _ledgers.Remove(oldest.Key);
    }

    // --- publication ---

    private PriceMemorySetSnapshot BuildSnapshot(
        MemoryModuleState state, DateTime now, string[]? extra = null)
    {
        var refs = _ledgers
            .Select(kv => new ReferenceMemorySnapshot(
                kv.Key,
                kv.Value.FirstTestAtUtc,
                kv.Value.LastTestAtUtc,
                kv.Value.TotalTests,
                kv.Value.Records.ToArray(),
                // G-REF-001: the count is recorded, the meaning is not inferred.
                ReferenceStrengthState.NotCalibrated,
                LiquidityReplenishmentObservability.Unavailable,
                BuildReferenceLimitations()))
            .OrderByDescending(r => r.LastTestAtUtc)
            .ToArray();

        var lim = new List<string>
        {
            PriceMemoryPolicyConfig.LimitationStrengthNotCalibrated,
            PriceMemoryPolicyConfig.LimitationOutcomeNotCalibrated,
            PriceMemoryPolicyConfig.LimitationNoDirectionalDecayRule,
            PriceMemoryPolicyConfig.LimitationReplenishmentUnavailable,
            PriceMemoryPolicyConfig.LimitationLiveOnly,
            PriceMemoryPolicyConfig.LimitationMemoryStartsAtIndicatorStart
        };
        if (extra is not null) lim.AddRange(extra);

        return new PriceMemorySetSnapshot(
            state,
            PriceMemoryPolicyConfig.PolicyVersion,
            refs,
            refs.FirstOrDefault(),
            refs.Length,
            refs.Count(r => r.HasBeenRetested),
            _totalTests,
            _memoryStartedAtUtc,
            now,
            lim);
    }

    private static IReadOnlyList<string> BuildReferenceLimitations() => new[]
    {
        PriceMemoryPolicyConfig.LimitationStrengthNotCalibrated,
        PriceMemoryPolicyConfig.LimitationNoDirectionalDecayRule,
        PriceMemoryPolicyConfig.LimitationReplenishmentUnavailable
    };

    private void ResetInternal()
    {
        _ledgers.Clear();
        _totalTests = 0L;
        _memoryStartedAtUtc = default;
    }

    private PriceMemorySetSnapshot DisabledSnapshot(DateTime now) =>
        new(MemoryModuleState.Disabled,
            PriceMemoryPolicyConfig.PolicyVersion,
            Array.Empty<ReferenceMemorySnapshot>(),
            null, 0, 0, 0L, now, now,
            new[] { "MODULE_DISABLED" });
}
