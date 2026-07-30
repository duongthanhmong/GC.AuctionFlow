using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Efficiency;
using GC.AuctionFlow.EffortResult;
using GC.AuctionFlow.Orderflow;
using GC.AuctionFlow.Research;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Research;

/// <summary>
/// WP01A DEC-T07: the Effort/Result research collection bridge.
///
/// The Efficiency module computes the Effort and Result vectors and the Historical Scanner
/// collects a corpus, and until this work package nothing joined them. These tests cover the
/// join and the reuse of the store's proven writer discipline — not a new persistence engine,
/// which is why `E01`, `E02` and `D03` appear here again against the new product.
///
/// Nothing here calibrates anything. The corpus is research material: production
/// classification stays `NotCalibrated`, both feature flags stay `false`, and no numeric
/// threshold exists.
/// </summary>
public sealed class EffortResultResearchBridgeTests : IDisposable
{
    private readonly string _profile = Path.Combine(
        Path.GetTempPath(), "gcae-err-" + Guid.NewGuid().ToString("N"));

    private static readonly DateTime At = new(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);

    public void Dispose()
    {
        try { if (Directory.Exists(_profile)) Directory.Delete(_profile, recursive: true); }
        catch (IOException) { /* best effort */ }
    }

    private EpisodeDatasetStore Store() => new(_profile);

    private static EffortResultResearchCollector Collector(EpisodeDatasetStore store) =>
        new(new EffortResultResearchPolicyConfig(enabled: true), store);

    /// <summary>
    /// The write barrier. Not a poll.
    ///
    /// Appends and loads share one queue, one lock and one worker, and a drained burst writes
    /// before it answers loads. So a load queued after an append on <b>the same store</b>
    /// completes only once that append is on disk — there is nothing to wait for and no budget
    /// to exhaust. The proof is in `docs/review/kdk_v4/wp_l1_02/01_BARRIER_PROOF.md`.
    ///
    /// <paramref name="owner"/> must be the instance that took the append. A load on a second
    /// store pointing at the same file is a different queue answered by a different worker and
    /// orders after nothing; waiting on one is what made these tests depend on a 4-second
    /// budget and fail under full-suite load.
    /// </summary>
    private static Task<IReadOnlyList<EffortResultResearchRecord>> Written(
        EpisodeDatasetStore owner) => owner.LoadEffortResultResearchAsync();

    // ========== A: admission ==========

    /// <summary>Frozen closed-episode evidence is collected.</summary>
    [Fact]
    public void A01_Frozen_closed_episode_evidence_is_collected()
    {
        var collector = Collector(Store());
        collector.Rebuild(Set(Evidence("EFF-1", "EP-1")), At);

        Assert.Equal(1, collector.ObservationsCollected);
        Assert.Equal(0, collector.RejectedNotClosedEpisode);
        Assert.Equal(0, collector.RejectedNotFrozen);
    }

    /// <summary>
    /// A current-auction or active-episode measurement is still moving. Collecting it would
    /// record the publish schedule rather than the market.
    /// </summary>
    [Theory]
    [InlineData(EfficiencyScopeType.CurrentPrimaryAuction)]
    [InlineData(EfficiencyScopeType.ActiveEpisode)]
    public void A02_Non_closed_scopes_are_rejected(EfficiencyScopeType scope)
    {
        var collector = Collector(Store());
        collector.Rebuild(Set(Evidence("EFF-1", "EP-1", scope: scope)), At);

        Assert.Equal(0, collector.ObservationsCollected);
        Assert.Equal(1, collector.RejectedNotClosedEpisode);
    }

    /// <summary>An unfrozen snapshot can still be revised, so its identity would name two
    /// different measurements at two different times.</summary>
    [Fact]
    public void A03_Unfrozen_evidence_is_rejected()
    {
        var collector = Collector(Store());
        collector.Rebuild(Set(Evidence("EFF-1", "EP-1", frozen: false)), At);

        Assert.Equal(0, collector.ObservationsCollected);
        Assert.Equal(1, collector.RejectedNotFrozen);
    }

    /// <summary>
    /// The raw source is the efficiency evidence, never a classification snapshot — and the
    /// projection carries the vectors across verbatim.
    /// </summary>
    [Fact]
    public void A04_The_projection_reads_the_efficiency_evidence_directly()
    {
        var evidence = Evidence("EFF-1", "EP-1");
        var record = EffortResultResearchProjection.TryProject(evidence, At, out var rejection);

        Assert.Equal(EffortResultResearchRejection.None, rejection);
        Assert.Equal(evidence.Effort.TotalExecutedVolume, record!.Effort.TotalExecutedVolume);
        Assert.Equal(evidence.Effort.ClassifiedDelta, record.Effort.ClassifiedDelta);
        Assert.Equal(evidence.Result.MaximumFavorableProgressTicks,
            record.Result.MaximumFavorableProgressTicks);
        Assert.Equal(evidence.InputFingerprint, record.InputFingerprint);
    }

    /// <summary>
    /// The record holds no Efficiency or EffortResult type.
    ///
    /// `AuthorityGuardTests.A02` fences `ProcessHistoricalScanner` off from that authority, and
    /// this record travels through the store the scanner also uses. An earlier draft put the
    /// live snapshot on the record and A02 failed immediately — correctly. This keeps the
    /// boundary a build failure rather than a review opinion.
    /// </summary>
    [Fact]
    public void A05_The_record_carries_no_efficiency_or_effort_result_type()
    {
        var offenders = new[]
            {
                typeof(EffortResultResearchRecord), typeof(ResearchEffortVector),
                typeof(ResearchResultVector), typeof(ResearchRawRelationships),
            }
            .SelectMany(t => t.GetProperties())
            .Select(p => Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType)
            .Select(t => t.Namespace ?? "")
            .Where(ns => ns.StartsWith("GC.AuctionFlow.Efficiency", StringComparison.Ordinal)
                         || ns.StartsWith("GC.AuctionFlow.EffortResult", StringComparison.Ordinal))
            .Distinct()
            .ToArray();

        Assert.Empty(offenders);
    }

    // ========== B: nulls, provenance, timestamps ==========

    /// <summary>
    /// Absent measurements stay null. Writing zero for an unobserved delta would be a
    /// fabricated observation (v1.3 `G-ACC-003`).
    /// </summary>
    [Fact]
    public async Task B01_Null_measurements_round_trip_as_null()
    {
        var store = Store();
        Collector(store).Rebuild(Set(Evidence("EFF-1", "EP-1")), At);

        var row = Assert.Single(await Written(store));

        Assert.Null(row.Effort.AskVolume);
        Assert.Null(row.Effort.ClassifiedCvdChange);
        Assert.Null(row.Effort.AggressorCoverageRatio);
        Assert.Null(row.Result.NetPriceProgressTicks);
        Assert.Null(row.Result.ProgressRetentionRatio);
        Assert.Null(row.RawRelationships.NetProgressPerExecutedContract);
    }

    /// <summary>Every provenance field the supervisor listed must survive the round trip.</summary>
    [Fact]
    public async Task B02_Provenance_and_scope_round_trip()
    {
        var store = Store();
        Collector(store).Rebuild(Set(Evidence("EFF-1", "EP-1")), At);

        var row = Assert.Single(await Written(store));

        Assert.Equal(EffortResultResearchPolicyConfig.RecordSchemaVersion, row.Schema);
        Assert.Equal("EFF-1", row.SnapshotId);
        Assert.Equal("PI-2026-07-27", row.PrimaryAuctionId);
        Assert.Equal("EP-1", row.EpisodeId);
        Assert.Equal("REF-1", row.ReferenceId);
        Assert.Equal(nameof(EfficiencyScopeType.ClosedEpisode), row.SourceScopeType);
        Assert.Equal("GCQ6", row.InstrumentIdentity);
        Assert.Equal("GCQ6|tick=0.1", row.DataEpoch);
        Assert.Equal(0.1m, row.TickSize);
        Assert.Equal(AtasTimestampNormalizer.PolicyVersion, row.TimestampPolicy);
        Assert.Equal(AuctionEfficiencyEvidencePolicyConfig.PolicyVersion, row.SourcePolicyVersion);
        Assert.Equal(AuctionEfficiencyEvidenceSnapshot.SnapshotVersion, row.SourceSnapshotVersion);
        Assert.Equal(7L, row.StateVersion);
        Assert.Equal(11L, row.EventRevision);
        Assert.Equal("fp-1", row.InputFingerprint);
        Assert.Equal(nameof(OrderflowCoverageMode.LiveOnlyFromAuctionStart), row.CoverageMode);
        Assert.Equal(nameof(EfficiencyDataQuality.Complete), row.DataQuality);
        Assert.Equal(nameof(EfficiencyAvailability.Available), row.Availability);
        Assert.True(row.IsFrozen);
        Assert.Contains("SOURCE_LIMITATION", row.SourceLimitations);
    }

    /// <summary>
    /// The original measurement timestamps are preserved. `CollectedAtUtc` is a diagnostic
    /// about when the bridge ran and must never stand in for one of them.
    /// </summary>
    [Fact]
    public async Task B03_Original_measurement_timestamps_are_preserved()
    {
        var store = Store();
        Collector(store).Rebuild(Set(Evidence("EFF-1", "EP-1")), At);

        var row = Assert.Single(await Written(store));

        Assert.Equal(new DateTime(2026, 7, 27, 8, 20, 0, DateTimeKind.Utc), row.ObservationStartedAtUtc);
        Assert.Equal(new DateTime(2026, 7, 27, 8, 25, 0, DateTimeKind.Utc), row.FirstInputAtUtc);
        Assert.Equal(new DateTime(2026, 7, 27, 9, 5, 0, DateTimeKind.Utc), row.LastInputAtUtc);
        Assert.Equal(At, row.CollectedAtUtc);
        Assert.NotEqual(row.CollectedAtUtc, row.ObservationStartedAtUtc);
    }

    /// <summary>The product is labelled research-only and non-authoritative, on every row.</summary>
    [Fact]
    public async Task B04_Rows_are_marked_research_only_and_non_authoritative()
    {
        var store = Store();
        Collector(store).Rebuild(Set(Evidence("EFF-1", "EP-1")), At);

        var row = Assert.Single(await Written(store));

        Assert.True(row.ResearchOnly);
        Assert.False(row.Authoritative);
        Assert.Contains(EffortResultResearchPolicyConfig.LimitationResearchOnly, row.Limitations);
        Assert.Contains(EffortResultResearchPolicyConfig.LimitationNonAuthoritative, row.Limitations);
        Assert.Contains(EffortResultResearchPolicyConfig.LimitationNoNormalization, row.Limitations);
        Assert.Contains(EffortResultResearchPolicyConfig.LimitationNoOutcomeLabel, row.Limitations);
    }

    /// <summary>Raw measures are carried through unchanged — no ranking, scaling or scoring.</summary>
    [Fact]
    public async Task B05_Raw_measures_are_carried_without_normalization()
    {
        var store = Store();
        Collector(store).Rebuild(Set(Evidence("EFF-1", "EP-1")), At);

        var row = Assert.Single(await Written(store));

        Assert.Equal(1234.5m, row.Effort.TotalExecutedVolume);
        Assert.Equal(77L, row.Effort.TradeCount);
        Assert.Equal(-42m, row.Effort.ClassifiedDelta);
        Assert.Equal(19, row.Effort.PriceLevelCount);
        Assert.Equal(31L, row.Result.MaximumFavorableProgressTicks);
        Assert.Equal(-13L, row.Result.MaximumAdverseProgressTicks);
    }

    // ========== C: deterministic identity and restart deduplication ==========

    /// <summary>The identity is a pure function of the snapshot — no clock, no GUID.</summary>
    [Fact]
    public void C01_The_observation_id_is_deterministic()
    {
        var a = EffortResultResearchProjection.BuildObservationId(Evidence("EFF-1", "EP-1"));
        var b = EffortResultResearchProjection.BuildObservationId(Evidence("EFF-1", "EP-1"));

        Assert.Equal(a, b);
        Assert.Equal("ERRO|PI-2026-07-27|EP-1|EFF-1|7", a);
        Assert.NotEqual(a, EffortResultResearchProjection.BuildObservationId(Evidence("EFF-2", "EP-2")));
    }

    /// <summary>
    /// The closed list is a rolling window, so the same observation arrives on many
    /// publishes. Collecting it twice would weight every distribution by the publish
    /// schedule.
    /// </summary>
    [Fact]
    public void C02_Repeated_publishes_collect_the_observation_once()
    {
        var collector = Collector(Store());
        var set = Set(Evidence("EFF-1", "EP-1"));

        for (var i = 0; i < 25; i++)
            collector.Rebuild(set, At);

        Assert.Equal(1, collector.ObservationsCollected);
    }

    /// <summary>
    /// `D03` for the new product: a restart must not re-append what it recovered.
    ///
    /// Measured on the episode dataset before this discipline existed: 5,126 lines holding
    /// 2,098 distinct rows. The collector learns the previous session's contents from an
    /// asynchronous load, so the file has to be what refuses the duplicate.
    /// </summary>
    [Fact]
    public async Task C03A_The_store_refuses_an_observation_that_is_already_in_the_file()
    {
        var records = new[]
        {
            EffortResultResearchProjection.TryProject(Evidence("EFF-1", "EP-1"), At, out _)!,
            EffortResultResearchProjection.TryProject(Evidence("EFF-2", "EP-2"), At, out _)!,
        };

        var writer = Store();
        writer.AppendEffortResultResearch(records);
        Assert.Equal(2, (await Written(writer)).Count);
        Assert.Equal(2, writer.ResearchRowsWritten);
        Assert.Equal(0, writer.ResearchDuplicatesRejected);

        // Offered again to the same instance: refused from the keys it already holds.
        writer.AppendEffortResultResearch(records);
        Assert.Equal(2, (await Written(writer)).Count);
        Assert.Equal(2, writer.ResearchRowsWritten);
        Assert.Equal(2, writer.ResearchDuplicatesRejected);

        // A new process holds no keys, so here the refusal has to come from the file itself.
        var reopened = Store();
        reopened.AppendEffortResultResearch(records);
        Assert.Equal(2, (await Written(reopened)).Count);
        Assert.Equal(0, reopened.ResearchRowsWritten);
        Assert.Equal(2, reopened.ResearchDuplicatesRejected);
    }

    /// <summary>
    /// `C03B` — the restart invariant, asserted so it holds on **both** orderings.
    ///
    /// If recovery merges first the fold is suppressed and nothing is offered; if the fold runs
    /// first both rows are offered and the file refuses them. Neither branch may write a row
    /// and neither may leave a duplicate. `ResearchDuplicatesRejected` is deliberately not
    /// asserted here — it is 2 on one branch and 0 on the other, and asserting it is what
    /// turned this test into a race. `C03A` covers it directly.
    /// </summary>
    [Fact]
    public async Task C03B_A_restart_never_duplicates_a_recovered_observation()
    {
        var set = Set(Evidence("EFF-1", "EP-1"), Evidence("EFF-2", "EP-2"));

        var first = Store();
        for (var i = 0; i < 10; i++) Collector(first).Rebuild(set, At);
        Assert.Equal(2, (await Written(first)).Count);

        // A new process seeing the very same rolling window.
        var reopened = Store();
        var restarted = Collector(reopened);
        for (var i = 0; i < 10; i++) restarted.Rebuild(set, At);

        var rows = await Written(reopened);

        Assert.Equal(0, reopened.ResearchRowsWritten);
        Assert.Equal(2, rows.Count);
        Assert.Equal(2, rows.Select(r => r.ObservationId).Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// `C03C` — the ordering the old test tried to reach by luck, reached deterministically.
    ///
    /// The collector's recovery load is queued on `reopened` before the test's, and one worker
    /// answers them in order, so completing the test's load proves the collector's completed.
    /// Recovery is then guaranteed to merge before anything is folded, and nothing may be
    /// offered to the store at all.
    /// </summary>
    [Fact]
    public async Task C03C_When_recovery_lands_first_nothing_is_offered_to_the_store()
    {
        var set = Set(Evidence("EFF-1", "EP-1"), Evidence("EFF-2", "EP-2"));

        var first = Store();
        Collector(first).Rebuild(set, At);
        Assert.Equal(2, (await Written(first)).Count);

        var reopened = Store();
        var restarted = Collector(reopened);

        restarted.Rebuild(null, At);                        // dispatches the recovery load
        await reopened.LoadEffortResultResearchAsync();     // orders after it
        restarted.Rebuild(set, At);                         // merges it, then finds nothing new

        Assert.Equal(2, restarted.RecoveredObservations);
        Assert.Equal(0, reopened.ResearchRowsAppended);
        Assert.Equal(0, reopened.ResearchDuplicatesRejected);
        Assert.Equal(2, (await Written(reopened)).Count);
    }

    /// <summary>
    /// `C03D` — the other ordering, and the one that actually produced the duplicates: folding
    /// runs while recovery is still in flight.
    ///
    /// `C03B` proves the outcome is the same on either branch, but an outcome that holds both
    /// ways does not prove both ways ran. This forces the fold-first branch and asserts what only
    /// that branch can produce: observations really were offered to the store, and the **file** is
    /// what refused them.
    ///
    /// The ordering is constructed, not waited for — <see cref="RecoveryGate"/> holds the recovery
    /// load incomplete, and the test asserts it is still incomplete on both sides of the fold. No
    /// delay, no poll, no scheduler.
    /// </summary>
    [Fact]
    public async Task C03D_When_folding_runs_before_recovery_the_file_refuses_the_duplicates()
    {
        var set = Set(Evidence("EFF-1", "EP-1"), Evidence("EFF-2", "EP-2"));

        var first = Store();
        Collector(first).Rebuild(set, At);
        Assert.Equal(2, (await Written(first)).Count);

        var reopened = Store();
        var restarted = Collector(reopened);
        var recovery = RecoveryGate.Install<IReadOnlyList<EffortResultResearchRecord>>(restarted);

        Assert.False(recovery.Task.IsCompleted);
        restarted.Rebuild(set, At);
        Assert.False(recovery.Task.IsCompleted);
        Assert.Equal(0, restarted.RecoveredObservations);

        // Only the fold-first branch reaches the store at all.
        Assert.Equal(2, reopened.ResearchRowsAppended);

        var rows = await Written(reopened);

        Assert.Equal(2, reopened.ResearchDuplicatesRejected);
        Assert.Equal(0, reopened.ResearchRowsWritten);
        Assert.Equal(2, rows.Count);
        Assert.Equal(2, rows.Select(r => r.ObservationId).Distinct(StringComparer.Ordinal).Count());

        // Recovery landing afterwards must not count the same observations a second time.
        recovery.SetResult(rows);
        restarted.Rebuild(set, At);

        Assert.Equal(0, restarted.RecoveredObservations);
        Assert.Equal(2, restarted.ObservationsCollected);
        Assert.Equal(2, reopened.ResearchRowsAppended);
        Assert.Equal(0, reopened.ResearchRowsWritten);
        Assert.Equal(2, (await Written(reopened)).Count);
    }

    /// <summary>A restart recovers the sample and says how much of it was restored.</summary>
    [Fact]
    public async Task C04_A_restarted_collector_recovers_the_sample()
    {
        var set = Set(Evidence("EFF-1", "EP-1"), Evidence("EFF-2", "EP-2"));

        var first = Store();
        Collector(first).Rebuild(set, At);
        Assert.Equal(2, (await Written(first)).Count);

        var reopened = Store();
        var restarted = Collector(reopened);

        restarted.Rebuild(null, At);                        // dispatches the recovery load
        await reopened.LoadEffortResultResearchAsync();     // orders after it
        restarted.Rebuild(null, At);                        // merges it

        Assert.Equal(2, restarted.RecoveredObservations);
    }

    // ========== D: the store's proven discipline, on the new product ==========

    /// <summary>
    /// `E01`: a burst must not open the file once per row.
    ///
    /// The shape that broke the platform was `Task.Run` plus `File.AppendAllText` — an
    /// open/write/close per row on the ThreadPool ATAS pumps market data through. Row count
    /// and file-open count must be allowed to diverge, so this asserts that they do.
    /// </summary>
    [Fact]
    public async Task D01_A_burst_of_research_rows_costs_a_handful_of_file_opens()
    {
        const int rows = 2_000;
        var store = Store();

        for (var i = 0; i < rows; i++)
        {
            var record = EffortResultResearchProjection.TryProject(
                Evidence("EFF-" + i, "EP-" + i), At, out _);
            store.AppendEffortResultResearch(new[] { record! });
        }

        // The barrier, not a budget: this load is queued behind all 2,000 appends.
        Assert.Equal(rows, (await Written(store)).Count);
        Assert.Equal(rows, store.ResearchRowsWritten);
        Assert.Equal(0, store.WriteFailures);
        Assert.True(
            store.ResearchWriteBatches < rows / 4,
            $"{rows} rows opened the file {store.ResearchWriteBatches} times; a per-row open is "
            + "the defect this guards.");
    }

    /// <summary>`E02`: the writer must not be a ThreadPool work item.</summary>
    [Fact]
    public async Task D02_Research_writing_happens_off_the_thread_pool()
    {
        var store = Store();
        Collector(store).Rebuild(Set(Evidence("EFF-1", "EP-1")), At);
        await Written(store);

        Assert.False(store.WroteOnThreadPoolThread);
        Assert.Equal(1, store.ResearchWriteBatches);
    }

    /// <summary>Append returns before anything reaches disk.</summary>
    [Fact]
    public void D03_Append_returns_before_the_file_exists()
    {
        var store = Store();
        var record = EffortResultResearchProjection.TryProject(Evidence("EFF-1", "EP-1"), At, out _);
        store.AppendEffortResultResearch(new[] { record! });

        Assert.Equal(1, store.ResearchRowsAppended);
    }

    /// <summary>A truncated or malformed tail costs one line, never the file.</summary>
    [Fact]
    public async Task D04_A_damaged_tail_is_not_fatal()
    {
        var store = Store();
        Collector(store).Rebuild(Set(Evidence("EFF-1", "EP-1"), Evidence("EFF-2", "EP-2")), At);
        await Written(store);

        var text = File.ReadAllText(store.EffortResultResearchFilePath);
        File.WriteAllText(store.EffortResultResearchFilePath, text[..(text.Length - 30)]);

        Assert.Single(await Store().LoadEffortResultResearchAsync());

        File.AppendAllText(store.EffortResultResearchFilePath, "\nthis is not json\n");
        Assert.Single(await Store().LoadEffortResultResearchAsync());
    }

    [Fact]
    public async Task D05_A_missing_research_file_reads_as_empty() =>
        Assert.Empty(await Store().LoadEffortResultResearchAsync());

    // ========== E: the legacy product is untouched ==========

    /// <summary>
    /// The episode dataset keeps its own file, its own schema and its own counters.
    ///
    /// Two products in one stream would force every reader to discriminate before it could
    /// parse, and would change bytes that existing readers already depend on.
    /// </summary>
    [Fact]
    public async Task E01_The_episode_dataset_is_a_separate_file_and_is_unchanged()
    {
        var store = Store();

        Assert.EndsWith("episode-dataset.jsonl", store.FilePath, StringComparison.Ordinal);
        Assert.EndsWith("effort-result-research.jsonl", store.EffortResultResearchFilePath,
            StringComparison.Ordinal);
        Assert.NotEqual(store.FilePath, store.EffortResultResearchFilePath);

        store.Append(new[]
        {
            EpisodeDatasetRecord.FromEpisode(Phase5AHistoricalScannerTests.EpisodeForStore("EP-1")),
        });
        Collector(store).Rebuild(Set(Evidence("EFF-1", "EP-9")), At);

        // One queue carries both products, so either load orders after both appends. Each
        // product is complete in its own file, and neither counter borrows the other's.
        Assert.Single(await store.LoadAsync());
        Assert.Single(await store.LoadEffortResultResearchAsync());
        Assert.Equal(1, store.RowsWritten);
        Assert.Equal(1, store.ResearchRowsWritten);

        var episodeLine = File.ReadAllText(store.FilePath);
        Assert.Contains("\"EpisodeId\":\"EP-1\"", episodeLine, StringComparison.Ordinal);
        Assert.DoesNotContain("effort-result-research", episodeLine, StringComparison.Ordinal);
        Assert.DoesNotContain(EffortResultResearchPolicyConfig.RecordSchemaVersion, episodeLine,
            StringComparison.Ordinal);
    }

    // ========== G: REV1 blockers ==========

    /// <summary>
    /// **No read may run on the ThreadPool either.**
    ///
    /// The first WP01A build moved writes onto the owned thread and left `LoadAsync` and
    /// `LoadEffortResultResearchAsync` on `Task.Run`, which made the class comment true of
    /// half the class. A file read competes for the very threads ATAS pumps market data
    /// through; the direction the bytes travel is not what made the write path dangerous.
    /// </summary>
    [Fact]
    public async Task G01_Loads_do_not_run_on_the_thread_pool()
    {
        var store = Store();
        Collector(store).Rebuild(Set(Evidence("EFF-1", "EP-1")), At);
        await Written(store);

        // Both products, and the episode load too - the contract belongs to the class, not
        // to the new product.
        await store.LoadAsync();
        await store.LoadEffortResultResearchAsync();

        Assert.False(store.ReadOnThreadPoolThread);
        Assert.False(store.WroteOnThreadPoolThread);
    }

    /// <summary>A load with nothing queued still answers, and still answers off-pool.</summary>
    [Fact]
    public async Task G02_A_load_with_nothing_queued_still_completes_off_pool()
    {
        var store = Store();

        Assert.Empty(await store.LoadEffortResultResearchAsync());
        Assert.Empty(await store.LoadAsync());
        Assert.False(store.ReadOnThreadPoolThread);
    }

    /// <summary>
    /// Disabling the collector PAUSES it. It must not erase the corpus.
    ///
    /// The object surviving a toggle is no comfort if its state does not: a flag flipped
    /// twice mid-session would silently reset the sample any later calibration decision
    /// rests on.
    /// </summary>
    [Fact]
    public async Task G03_Disabling_the_collector_preserves_everything()
    {
        var store = Store();
        var collector = Collector(store);
        var set = Set(Evidence("EFF-1", "EP-1"), Evidence("EFF-2", "EP-2"));

        collector.Rebuild(set, At);
        await Written(store);

        var collected = collector.ObservationsCollected;
        var started = collector.StartedAtUtc;
        var observations = collector.Observations.Count;
        var fileBefore = File.ReadAllBytes(store.EffortResultResearchFilePath);

        collector.Configure(new EffortResultResearchPolicyConfig(enabled: false));

        Assert.Equal(collected, collector.ObservationsCollected);
        Assert.Equal(observations, collector.Observations.Count);
        Assert.Equal(started, collector.StartedAtUtc);

        // Disabled: a rebuild collects nothing and changes nothing.
        collector.Rebuild(set, At);
        Assert.Equal(collected, collector.ObservationsCollected);

        // Re-enabled: the same frozen snapshots are NOT collected again, because the dedup
        // ids survived the toggle.
        collector.Configure(new EffortResultResearchPolicyConfig(enabled: true));
        collector.Rebuild(set, At);

        Assert.Equal(collected, collector.ObservationsCollected);
        Assert.Equal(observations, collector.Observations.Count);
        Assert.Equal(started, collector.StartedAtUtc);
        Assert.Equal(fileBefore, File.ReadAllBytes(store.EffortResultResearchFilePath));
    }

    /// <summary>Recovered rows survive a toggle too - recovery is neither re-run nor forgotten.</summary>
    [Fact]
    public async Task G04_Disabling_does_not_forget_recovered_rows()
    {
        var set = Set(Evidence("EFF-1", "EP-1"), Evidence("EFF-2", "EP-2"));

        var first = Store();
        Collector(first).Rebuild(set, At);
        Assert.Equal(2, (await Written(first)).Count);

        var reopened = Store();
        var restarted = Collector(reopened);

        restarted.Rebuild(null, At);                        // dispatches the recovery load
        await reopened.LoadEffortResultResearchAsync();     // orders after it
        restarted.Rebuild(null, At);                        // merges it

        Assert.Equal(2, restarted.RecoveredObservations);

        restarted.Configure(new EffortResultResearchPolicyConfig(enabled: false));
        Assert.Equal(2, restarted.RecoveredObservations);

        restarted.Configure(new EffortResultResearchPolicyConfig(enabled: true));
        Assert.Equal(2, restarted.RecoveredObservations);
        Assert.Equal(2, restarted.ObservationsCollected);
    }

    /// <summary>
    /// The reader admits only the registered schema.
    ///
    /// "A reader can refuse a shape it does not know" was a comment on a field nothing
    /// checked. A future row must be counted and left alone, never reinterpreted as this
    /// shape - a field that changed meaning between versions would otherwise be mis-read in
    /// silence.
    /// </summary>
    [Fact]
    public async Task G05_Only_the_registered_schema_is_admitted()
    {
        var store = Store();
        Collector(store).Rebuild(Set(Evidence("EFF-1", "EP-1")), At);
        await Written(store);

        var path = store.EffortResultResearchFilePath;
        File.AppendAllText(path, FutureRow("ERRO|future") + "\n");
        File.AppendAllText(path, "not json at all\n");

        var reader = Store();
        var rows = await reader.LoadEffortResultResearchAsync();

        Assert.Single(rows);
        Assert.Equal(EffortResultResearchPolicyConfig.RecordSchemaVersion, rows[0].Schema);
        Assert.Equal(1, reader.ResearchUnknownSchemaRows);
        Assert.Equal(1, reader.ResearchMalformedRows);
    }

    /// <summary>
    /// A future-schema row must not suppress the current-schema row for the same observation.
    ///
    /// Storage deduplication keys on schema AND observation id; `ObservationId` itself stays
    /// stable so a research join across builds still works. Without the schema in the key, a
    /// newer build's file would silently stop this build collecting, and the gap would look
    /// like the market simply produced nothing.
    /// </summary>
    [Fact]
    public async Task G06_A_future_schema_row_does_not_block_the_current_schema_row()
    {
        var store = Store();
        var evidence = Evidence("EFF-1", "EP-1");
        var observationId = EffortResultResearchProjection.BuildObservationId(evidence);

        // A newer build wrote this observation first, under a schema this build cannot read.
        Directory.CreateDirectory(Path.GetDirectoryName(store.EffortResultResearchFilePath)!);
        File.WriteAllText(store.EffortResultResearchFilePath, FutureRow(observationId) + "\n");

        Collector(store).Rebuild(Set(evidence), At);
        var rows = await Written(store);

        // The current-schema row was still written, and it is the one the reader returns.
        var row = Assert.Single(rows);
        Assert.Equal(observationId, row.ObservationId);
        Assert.Equal(EffortResultResearchPolicyConfig.RecordSchemaVersion, row.Schema);
        Assert.Equal(1, store.ResearchRowsWritten);
        Assert.Equal(0, store.ResearchDuplicatesRejected);

        // The future row is still on disk, counted and untouched.
        Assert.Equal(2, File.ReadAllLines(store.EffortResultResearchFilePath).Length);
        Assert.Equal(1, store.ResearchUnknownSchemaRows);
    }

    /// <summary>One line as a build from the future would have written it.</summary>
    private static string FutureRow(string observationId) =>
        "{\"Schema\":\"gcae-effort-result-research-v99\",\"ObservationId\":\"" + observationId + "\"}";

    // ========== F: nothing became a production input ==========

    /// <summary>Disabled is the default, and a disabled collector collects nothing.</summary>
    [Fact]
    public void F01_The_collector_is_off_by_default()
    {
        Assert.False(new EffortResultResearchPolicyConfig().Enabled);

        var collector = new EffortResultResearchCollector(store: Store());
        collector.Rebuild(Set(Evidence("EFF-1", "EP-1")), At);

        Assert.Equal(0, collector.ObservationsCollected);
    }

    /// <summary>
    /// The bridge carries no threshold, no window, no percentile and no bucket. Its only
    /// number is a retention bound, and it is named as one.
    /// </summary>
    [Fact]
    public void F02_The_policy_contains_no_calibration_value()
    {
        var numeric = typeof(EffortResultResearchPolicyConfig)
            .GetFields(System.Reflection.BindingFlags.Public
                       | System.Reflection.BindingFlags.Static
                       | System.Reflection.BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && f.FieldType != typeof(string))
            .Select(f => f.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(new[] { "ObservationCapacity" }, numeric);
    }

    /// <summary>Collecting a corpus does not calibrate anything.</summary>
    [Fact]
    public void F03_Classification_and_the_unlock_protocol_are_unaffected()
    {
        var collector = Collector(Store());
        collector.Rebuild(Set(Evidence("EFF-1", "EP-1")), At);

        // Every real state is still reserved at 100+, and NotCalibrated is still the only
        // non-Unknown state below that. Collecting a corpus does not make one reachable.
        Assert.All(
            Enum.GetValues<EffortResultClassificationState>()
                .Where(v => v != EffortResultClassificationState.Unknown
                            && v != EffortResultClassificationState.NotCalibrated),
            v => Assert.True((int)v >= 100));

        Assert.False(CalibrationProtocol.Evaluate(hasCollectedRows: true).UnlockPermitted);
        Assert.Equal(0, EffortResultPolicyProbe.NumericThresholdConstantCount());
    }

    // ---------------- fixtures ----------------

    private static AuctionEfficiencyEvidenceSetSnapshot Set(
        params AuctionEfficiencyEvidenceSnapshot[] closed) =>
        new(
            moduleState: EfficiencyModuleState.Ready,
            policyVersion: AuctionEfficiencyEvidencePolicyConfig.PolicyVersion,
            currentAuctionEvidence: null,
            activeEpisodeEvidence: Array.Empty<AuctionEfficiencyEvidenceSnapshot>(),
            recentlyClosedEpisodeEvidence: closed,
            latestUpdatedEvidence: closed.Length > 0 ? closed[^1] : null,
            readyCount: closed.Length,
            partialCount: 0,
            invalidCount: 0,
            inputFingerprint: null,
            rejectedStaleCount: 0L,
            lastRejectionReason: null,
            createdAtUtc: At,
            lastUpdatedAtUtc: At,
            limitations: Array.Empty<string>());

    /// <summary>
    /// A closed, frozen efficiency observation.
    ///
    /// Most measures are deliberately null: the point of the null-preservation rule is that
    /// an unobserved value stays unobserved, so the fixture has to contain some.
    /// </summary>
    private static AuctionEfficiencyEvidenceSnapshot Evidence(
        string snapshotId,
        string episodeId,
        EfficiencyScopeType scope = EfficiencyScopeType.ClosedEpisode,
        bool frozen = true)
    {
        var effort = new AuctionEffortEvidenceVector(
            totalExecutedVolume: 1234.5m, tradeCount: 77L, priceLevelCount: 19,
            classifiedVolume: 900m, askVolume: null, bidVolume: null,
            unknownAggressorVolume: 334.5m, classifiedDelta: -42m,
            absoluteClassifiedDelta: null, classifiedCvdChange: null,
            aggressorCoverageRatio: null, observationDuration: TimeSpan.FromMinutes(45),
            minimumTradeInterval: null, maximumTradeInterval: null, meanTradeInterval: null,
            latestTradeInterval: null, tradesPerSecondRaw: null, contractsPerSecondRaw: null,
            maximumLevelExecutedVolume: null, maximumLevelTradeCount: null,
            maximumAbsoluteLevelDelta: null, revisitedLevelCount: 3, maximumVisitCount: 2,
            classifiedLevelCount: 12, unknownOnlyLevelCount: 7,
            samePriceRatioAvailabilityCount: 0, diagonalRatioAvailabilityCount: 0,
            rawAskDominantLevelCount: 4, rawBidDominantLevelCount: 6, rawEqualLevelCount: 2,
            rawUnknownDominantLevelCount: 7, maximumConsecutiveRawAskDominanceTicks: 2,
            maximumConsecutiveRawBidDominanceTicks: 3, clusterPopulationSize: 19,
            evidenceAvailability: EfficiencyAvailability.Partial,
            limitations: new[] { "EFFORT_LIMITATION" });

        var result = new AuctionResultEvidenceVector(
            firstPriceTick: 1000L, latestPriceTick: 1018L, highPriceTick: 1031L, lowPriceTick: 987L,
            netPriceProgressTicks: null, grossRangeTicks: 44L,
            maximumFavorableProgressTicks: 31L, maximumAdverseProgressTicks: -13L,
            progressRetainedTicks: null, progressRetentionRatio: null,
            timeToMaximumFavorableProgress: null, timeToLatestProgress: null,
            timeAtMaximumExcursion: null,
            episodeReferenceDistanceStartTicks: null, episodeReferenceDistanceLatestTicks: null,
            maximumDistanceFromReferenceTicks: null, currentDistanceFromReferenceTicks: null,
            geometricReentryObserved: null, timeMaintainedInside: null,
            outsideTimeRatio: null, outsideVolumeRatio: null, outsideTradeRatio: null,
            localPocTick: null, localPocDisplacementTicks: null,
            developingTpoPocStartTick: null, developingTpoPocLatestTick: null,
            tpoPocMigrationTicks: null,
            developingVolumePocStartTick: null, developingVolumePocLatestTick: null,
            volumePocMigrationTicks: null,
            developingTpoValueLowStartTick: null, developingTpoValueHighStartTick: null,
            developingTpoValueLowLatestTick: null, developingTpoValueHighLatestTick: null,
            developingVolumeValueLowStartTick: null, developingVolumeValueHighStartTick: null,
            developingVolumeValueLowLatestTick: null, developingVolumeValueHighLatestTick: null,
            tpoValueCentroidMigrationTicks: null, volumeValueCentroidMigrationTicks: null,
            priceLocationAtStart: null, priceLocationLatest: null,
            evidenceAvailability: EfficiencyAvailability.Partial,
            limitations: new[] { "RESULT_LIMITATION" });

        var raw = new AuctionEfficiencyRawRelationships(
            null, null, null, null, null, null, null, null, null, null, null, null);

        return new AuctionEfficiencyEvidenceSnapshot(
            snapshotId: snapshotId,
            policyVersion: AuctionEfficiencyEvidencePolicyConfig.PolicyVersion,
            scopeType: scope,
            primaryAuctionId: "PI-2026-07-27",
            episodeId: episodeId,
            referenceId: "REF-1",
            referenceRole: null,
            instrumentIdentity: "GCQ6",
            dataEpoch: "GCQ6|tick=0.1",
            tickSize: 0.1m,
            timestampPolicy: AtasTimestampNormalizer.PolicyVersion,
            measurementStatus: EfficiencyModuleState.Ready,
            classificationState: EfficiencyClassificationState.NotCalibrated,
            observationStartedAtUtc: new DateTime(2026, 7, 27, 8, 20, 0, DateTimeKind.Utc),
            firstInputAtUtc: new DateTime(2026, 7, 27, 8, 25, 0, DateTimeKind.Utc),
            lastInputAtUtc: new DateTime(2026, 7, 27, 9, 5, 0, DateTimeKind.Utc),
            coverageMode: OrderflowCoverageMode.LiveOnlyFromAuctionStart,
            resultDirection: EfficiencyResultDirection.Up,
            effort: effort,
            result: result,
            rawRelationships: raw,
            stateVersion: 7L,
            eventRevision: 11L,
            dataQuality: EfficiencyDataQuality.Complete,
            availability: EfficiencyAvailability.Available,
            limitations: new[] { "SOURCE_LIMITATION" },
            inputFingerprint: "fp-1",
            isFrozen: frozen);
    }
}

/// <summary>
/// Counts numeric threshold constants on the Effort/Result policy.
///
/// Kept beside the bridge tests because the bridge is the first thing that could ever have a
/// reason to add one, and the answer must stay zero.
/// </summary>
internal static class EffortResultPolicyProbe
{
    public static int NumericThresholdConstantCount() =>
        typeof(EffortResultClassifierPolicyConfig)
            .GetFields(System.Reflection.BindingFlags.Public
                       | System.Reflection.BindingFlags.Static
                       | System.Reflection.BindingFlags.FlattenHierarchy)
            .Count(f => f.IsLiteral && f.FieldType != typeof(string) && f.FieldType != typeof(bool));
}
