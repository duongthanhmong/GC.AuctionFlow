using GC.AuctionFlow.Research;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Research;

/// <summary>
/// The episode dataset must survive a restart.
///
/// Episodes close roughly once per trading day, so a session's entire yield is one batch of
/// rows. Held only in memory, every restart discarded the day's work — and `G-CAL-002` step
/// 4 wants out-of-sample validation, which needs a sample spanning sessions. Without this,
/// running longer accumulated nothing, which made the "just wait for more data" answer to
/// the calibration block untrue.
/// </summary>
public sealed class EpisodeDatasetStoreTests : IDisposable
{
    private readonly string _profile = Path.Combine(
        Path.GetTempPath(), "gcae-store-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try { if (Directory.Exists(_profile)) Directory.Delete(_profile, recursive: true); }
        catch (IOException) { /* best effort */ }
    }

    private EpisodeDatasetStore Store() => new(_profile);

    private static GC.AuctionFlow.Episode.AuctionEpisodeSnapshot Episode(string id) =>
        Phase5AHistoricalScannerTests.EpisodeForStore(id);

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
    private static Task<IReadOnlyList<PersistedEpisodeRow>> Written(EpisodeDatasetStore owner) =>
        owner.LoadAsync();

    // ========== A: rows survive ==========

    [Fact]
    public async Task A01_Appended_rows_come_back_after_a_restart()
    {
        var writer = Store();
        writer.Append(new[]
        {
            EpisodeDatasetRecord.FromEpisode(Episode("EP-1")),
            EpisodeDatasetRecord.FromEpisode(Episode("EP-2")),
        });

        var rows = await Written(writer);
        Assert.Equal(2, rows.Count);

        // A different instance, as a new process would see it.
        var reader = Store();
        var reloaded = await reader.LoadAsync();
        Assert.Equal(
            new[] { "EP-1", "EP-2" },
            reloaded.Select(r => r.EpisodeId).OrderBy(x => x, StringComparer.Ordinal));
    }

    /// <summary>
    /// Append, never rewrite. A rewrite that fails halfway loses everything already earned;
    /// an append that fails halfway loses one line.
    /// </summary>
    [Fact]
    public async Task A02_A_second_session_adds_to_the_first_rather_than_replacing_it()
    {
        var first = Store();
        first.Append(new[] { EpisodeDatasetRecord.FromEpisode(Episode("EP-1")) });
        await Written(first);

        var second = Store();
        second.Append(new[] { EpisodeDatasetRecord.FromEpisode(Episode("EP-2")) });

        Assert.Equal(2, (await Written(second)).Count);
    }

    // ========== B: damage costs one line, not the file ==========

    /// <summary>
    /// The last line of an interrupted session is expected to be partial. Refusing the whole
    /// file over it would discard every good row before it.
    /// </summary>
    [Fact]
    public async Task B01_A_truncated_last_line_does_not_lose_the_rows_before_it()
    {
        var store = Store();
        store.Append(new[]
        {
            EpisodeDatasetRecord.FromEpisode(Episode("EP-1")),
            EpisodeDatasetRecord.FromEpisode(Episode("EP-2")),
        });
        await Written(store);

        var text = File.ReadAllText(store.FilePath);
        File.WriteAllText(store.FilePath, text[..(text.Length - 20)] );

        var rows = await Store().LoadAsync();
        Assert.Single(rows);
        Assert.Equal("EP-1", rows[0].EpisodeId);
    }

    [Fact]
    public async Task B02_A_missing_file_reads_as_empty_rather_than_throwing() =>
        Assert.Empty(await Store().LoadAsync());

    [Fact]
    public async Task B03_Garbage_lines_are_skipped_not_fatal()
    {
        var store = Store();
        store.Append(new[] { EpisodeDatasetRecord.FromEpisode(Episode("EP-1")) });
        await Written(store);

        File.AppendAllText(store.FilePath, "this is not json\n");
        var rows = await Store().LoadAsync();

        Assert.Single(rows);
    }

    // ========== C: no disk work on the caller's thread ==========

    /// <summary>
    /// The scanner runs on ATAS threads, and this project has already shipped one defect
    /// where disk work there stalled the platform's data pump and corrupted a chart. Append
    /// must return before anything reaches disk.
    /// </summary>
    [Fact]
    public void C01_Append_returns_before_the_file_exists()
    {
        var store = Store();
        store.Append(new[] { EpisodeDatasetRecord.FromEpisode(Episode("EP-1")) });

        // Recorded immediately; written later.
        Assert.Equal(1, store.RowsAppended);
    }

    // ========== D: the scanner actually uses the store ==========

    /// <summary>
    /// The wiring, not the component.
    ///
    /// Removing the scanner's call to the store failed nothing until this existed — the
    /// store was correct and unused, which is the same trap that let a hard-coded MBO row
    /// ship beside a correct helper. Persistence is only real if the thing that produces
    /// rows hands them over.
    /// </summary>
    [Fact]
    public async Task D01_The_scanner_persists_the_rows_it_folds()
    {
        var store = Store();
        var scanner = new HistoricalScannerHost(
            new HistoricalScannerPolicyConfig(enabled: true), store);

        scanner.Rebuild(
            Phase5AHistoricalScannerTests.ClosedForStore(Episode("EP-1"), Episode("EP-2")),
            new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc));

        var rows = await Written(store);
        Assert.Equal(2, rows.Count);
    }

    /// <summary>
    /// A restart must recover the sample and say how much of it was restored.
    ///
    /// The recovery load is dispatched by the first rebuild and merged by whichever rebuild
    /// finds it complete, so the ordering is controlled rather than waited for: the host's
    /// load is queued on `reopened` before the test's, and one worker answers them in order,
    /// so the test's load completing proves the host's already did.
    /// </summary>
    [Fact]
    public async Task D02_A_restarted_scanner_recovers_the_sample()
    {
        var at = new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);
        var closed = Phase5AHistoricalScannerTests.ClosedForStore(Episode("EP-1"), Episode("EP-2"));

        var writerStore = Store();
        new HistoricalScannerHost(new HistoricalScannerPolicyConfig(enabled: true), writerStore)
            .Rebuild(closed, at);
        Assert.Equal(2, (await Written(writerStore)).Count);

        var reopened = Store();
        var restarted = new HistoricalScannerHost(
            new HistoricalScannerPolicyConfig(enabled: true), reopened);

        restarted.Rebuild(null, at);      // dispatches the recovery load
        await reopened.LoadAsync();       // orders after it
        restarted.Rebuild(null, at);      // merges it

        Assert.Equal(2, restarted.RecoveredRows);
        Assert.Equal(2, restarted.Current!.RowsCollected);
    }

    /// <summary>
    /// `D03A` — what `DuplicatesRejected` actually counts: a row offered to the store that is
    /// already in the file.
    ///
    /// Offered directly, with no host and no recovery, because that is the only way to state
    /// the store's contract without depending on which of two orderings a collaborator took.
    /// The previous single `D03` asserted this counter after a race that decides whether
    /// anything is offered at all, and no budget can rescue that.
    /// </summary>
    [Fact]
    public async Task D03A_The_store_refuses_a_row_that_is_already_in_the_file()
    {
        var rows = new[]
        {
            EpisodeDatasetRecord.FromEpisode(Episode("EP-1")),
            EpisodeDatasetRecord.FromEpisode(Episode("EP-2")),
        };

        var writer = Store();
        writer.Append(rows);
        Assert.Equal(2, (await Written(writer)).Count);
        Assert.Equal(2, writer.RowsWritten);
        Assert.Equal(0, writer.DuplicatesRejected);

        // Offered again to the same instance: refused from the keys it already holds.
        writer.Append(rows);
        Assert.Equal(2, (await Written(writer)).Count);
        Assert.Equal(2, writer.RowsWritten);
        Assert.Equal(2, writer.DuplicatesRejected);

        // A new process holds no keys, so here the refusal has to come from the file itself.
        var reopened = Store();
        reopened.Append(rows);
        Assert.Equal(2, (await Written(reopened)).Count);
        Assert.Equal(0, reopened.RowsWritten);
        Assert.Equal(2, reopened.DuplicatesRejected);
    }

    /// <summary>
    /// `D03B` — the restart invariant, and the reason this file exists in its current form.
    ///
    /// Measured on the live dataset before this discipline: 5,126 lines holding 2,098 distinct
    /// episodes. The sample looked twice the size it was, and every distribution built on it
    /// would have been weighted by how often the operator restarted.
    ///
    /// Asserted so it holds on **both** orderings. If recovery merges first the fold is
    /// suppressed and nothing is offered; if the fold runs first both rows are offered and the
    /// file refuses them. Neither branch may write a row and neither may leave a duplicate.
    /// `DuplicatesRejected` is deliberately not asserted here — it is 2 on one branch and 0 on
    /// the other, and asserting it is what turned this test into a race. `D03A` covers it.
    /// </summary>
    [Fact]
    public async Task D03B_A_restart_never_duplicates_a_recovered_episode()
    {
        var closed = Phase5AHistoricalScannerTests.ClosedForStore(Episode("EP-1"), Episode("EP-2"));
        var at = new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);

        var writerStore = Store();
        var first = new HistoricalScannerHost(
            new HistoricalScannerPolicyConfig(enabled: true), writerStore);
        for (var i = 0; i < 20; i++) first.Rebuild(closed, at);
        Assert.Equal(2, (await Written(writerStore)).Count);

        // A new process seeing the very same rolling window of closed episodes.
        var reopened = Store();
        var restarted = new HistoricalScannerHost(
            new HistoricalScannerPolicyConfig(enabled: true), reopened);
        for (var i = 0; i < 20; i++) restarted.Rebuild(closed, at);

        var rows = await Written(reopened);

        Assert.Equal(0, reopened.RowsWritten);
        Assert.Equal(2, rows.Count);
        Assert.Equal(
            new[] { "EP-1", "EP-2" },
            rows.Select(r => r.EpisodeId).OrderBy(x => x, StringComparer.Ordinal));
    }

    /// <summary>
    /// `D03C` — the ordering the old test tried to reach by luck, reached deterministically.
    ///
    /// Recovery is forced to land before anything is folded by ordering after the host's own
    /// load, and then nothing may be offered to the store at all: the recovered ids seed the
    /// dedup set, so the fold finds every episode already known.
    /// </summary>
    [Fact]
    public async Task D03C_When_recovery_lands_first_nothing_is_offered_to_the_store()
    {
        var closed = Phase5AHistoricalScannerTests.ClosedForStore(Episode("EP-1"), Episode("EP-2"));
        var at = new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);

        var writerStore = Store();
        new HistoricalScannerHost(new HistoricalScannerPolicyConfig(enabled: true), writerStore)
            .Rebuild(closed, at);
        Assert.Equal(2, (await Written(writerStore)).Count);

        var reopened = Store();
        var restarted = new HistoricalScannerHost(
            new HistoricalScannerPolicyConfig(enabled: true), reopened);

        restarted.Rebuild(null, at);      // dispatches the recovery load, folds nothing
        await reopened.LoadAsync();       // orders after it
        restarted.Rebuild(closed, at);    // merges recovery, then finds nothing new to fold

        Assert.Equal(2, restarted.RecoveredRows);
        Assert.Equal(0, reopened.RowsAppended);
        Assert.Equal(0, reopened.DuplicatesRejected);
        Assert.Equal(2, (await Written(reopened)).Count);
    }

    /// <summary>
    /// The regression that shipped, and the reason this file exists in its current form.
    ///
    /// The first version dispatched a `Task.Run` flush per append and wrote through
    /// `File.AppendAllText`, which opens and closes the file every call. A live session
    /// produced 2,098 rows, so it produced thousands of open/write/close operations on the
    /// ThreadPool — the same pool ATAS pumps market data through. The abnormal chart bar
    /// came straight back, and turning the scanner off was what made it stop.
    ///
    /// Off-thread was never the requirement. Not competing for the platform's threads, and
    /// not opening the file per row, are the requirements. Row count and file-open count
    /// must be allowed to diverge, so this asserts they do.
    /// </summary>
    [Fact]
    public async Task E01_A_burst_of_rows_costs_a_handful_of_file_opens_not_one_each()
    {
        const int rows = 2_000;
        var store = Store();

        for (var i = 0; i < rows; i++)
            store.Append(new[] { EpisodeDatasetRecord.FromEpisode(Episode("EP-" + i)) });

        // The barrier, not a budget: this load is queued behind all 2,000 appends.
        Assert.Equal(rows, (await store.LoadAsync()).Count);
        Assert.Equal(rows, store.RowsWritten);
        Assert.Equal(0, store.WriteFailures);

        // The bound is loose on purpose — the exact coalescing depends on scheduling. What
        // it forbids is the shape that broke the platform: one open per row.
        Assert.True(
            store.WriteBatches < rows / 4,
            $"{rows} rows opened the file {store.WriteBatches} times; a per-row open is the "
            + "defect this guards.");

        Assert.Equal(rows, (await store.LoadAsync()).Count);
    }

    /// <summary>
    /// The writer must not be a ThreadPool work item. ATAS's data pump uses that pool, and
    /// starving it is indistinguishable from blocking it.
    /// </summary>
    [Fact]
    public async Task E02_Writing_happens_off_the_thread_pool()
    {
        var store = Store();
        store.Append(new[] { EpisodeDatasetRecord.FromEpisode(Episode("EP-1")) });
        await Written(store);

        // The store reports which kind of thread did the writing. `Task.Run` would make
        // this true, and `Task.Run` is what broke the chart.
        Assert.False(store.WroteOnThreadPoolThread);
        Assert.Equal(1, store.WriteBatches);
    }

    /// <summary>
    /// `D03D` — the other ordering, and the one that actually produced the duplicates: folding
    /// runs while recovery is still in flight.
    ///
    /// `D03B` proves the outcome is the same on either branch, but an outcome that holds both
    /// ways does not prove both ways ran. This forces the fold-first branch and asserts what only
    /// that branch can produce: rows really were offered to the store, and the **file** is what
    /// refused them.
    ///
    /// The ordering is constructed, not waited for — <see cref="RecoveryGate"/> holds the
    /// recovery load incomplete, and the test asserts it is still incomplete on both sides of the
    /// fold. No delay, no poll, no scheduler.
    /// </summary>
    [Fact]
    public async Task D03D_When_folding_runs_before_recovery_the_file_refuses_the_duplicates()
    {
        var closed = Phase5AHistoricalScannerTests.ClosedForStore(Episode("EP-1"), Episode("EP-2"));
        var at = new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);

        var writerStore = Store();
        new HistoricalScannerHost(new HistoricalScannerPolicyConfig(enabled: true), writerStore)
            .Rebuild(closed, at);
        Assert.Equal(2, (await Written(writerStore)).Count);

        var reopened = Store();
        var restarted = new HistoricalScannerHost(
            new HistoricalScannerPolicyConfig(enabled: true), reopened);
        var recovery = RecoveryGate.Install<IReadOnlyList<PersistedEpisodeRow>>(restarted);

        Assert.False(recovery.Task.IsCompleted);
        restarted.Rebuild(closed, at);
        Assert.False(recovery.Task.IsCompleted);
        Assert.Equal(0, restarted.RecoveredRows);

        // Only the fold-first branch reaches the store at all.
        Assert.Equal(2, reopened.RowsAppended);

        var rows = await Written(reopened);

        Assert.Equal(2, reopened.DuplicatesRejected);
        Assert.Equal(0, reopened.RowsWritten);
        Assert.Equal(2, rows.Count);
        Assert.Equal(
            new[] { "EP-1", "EP-2" },
            rows.Select(r => r.EpisodeId).OrderBy(x => x, StringComparer.Ordinal));

        // Recovery landing afterwards must not count the same episodes a second time.
        recovery.SetResult(rows);
        restarted.Rebuild(closed, at);

        Assert.Equal(0, restarted.RecoveredRows);
        Assert.Equal(2, restarted.Current!.RowsCollected);
        Assert.Equal(2, reopened.RowsAppended);
        Assert.Equal(0, reopened.RowsWritten);
        Assert.Equal(2, (await Written(reopened)).Count);
    }

    // ========== H: the load is the write barrier ==========

    /// <summary>
    /// The property every wait in this file now rests on, asserted directly.
    ///
    /// Appends and loads share one queue answered by one worker, and a drained burst writes
    /// before it answers loads, so a load queued after an append completes only once that
    /// append is on disk. There is no delay, no poll and no budget here on purpose: if the
    /// ordering did not hold this would fail every time rather than occasionally.
    /// </summary>
    [Fact]
    public async Task H01_A_load_completes_only_after_every_append_queued_before_it()
    {
        var store = Store();
        for (var i = 0; i < 200; i++)
            store.Append(new[] { EpisodeDatasetRecord.FromEpisode(Episode("EP-" + i)) });

        var rows = await store.LoadAsync();

        Assert.Equal(200, rows.Count);
        Assert.Equal(200, store.RowsWritten);
        Assert.Equal(0, store.WriteFailures);
    }

    /// <summary>The barrier holds burst by burst, not only once at the end.</summary>
    [Fact]
    public async Task H02_The_barrier_holds_across_separate_bursts()
    {
        var store = Store();
        for (var i = 0; i < 20; i++)
        {
            store.Append(new[] { EpisodeDatasetRecord.FromEpisode(Episode("EP-" + i)) });
            Assert.Equal(i + 1, (await store.LoadAsync()).Count);
        }

        Assert.Equal(20, store.RowsWritten);
    }

    /// <summary>
    /// The barrier must survive the writer retiring.
    ///
    /// The worker exists during a burst and not between them, so a later append starts a
    /// successor. Two things could go wrong: the successor never starts, or the retiring
    /// thread walks away from work that arrived while its idle timeout was expiring. The
    /// second is why `WorkerLoop` re-tests the queue instead of retiring on the timeout alone
    /// — a stranded load's `TaskCompletionSource` would never complete.
    ///
    /// Deliberately slow: `IdleRetireMilliseconds` is 5 s and this waits past it.
    /// </summary>
    [Fact]
    public async Task H03_The_barrier_still_holds_after_the_writer_has_retired()
    {
        var store = Store();
        store.Append(new[] { EpisodeDatasetRecord.FromEpisode(Episode("EP-1")) });
        Assert.Single(await store.LoadAsync());

        await Task.Delay(TimeSpan.FromSeconds(6));

        store.Append(new[] { EpisodeDatasetRecord.FromEpisode(Episode("EP-2")) });
        var rows = await store.LoadAsync();

        Assert.Equal(2, rows.Count);
        Assert.Equal(2, store.RowsWritten);
    }

    /// <summary>
    /// Both products share the one queue, so a load of either orders after appends of both.
    /// </summary>
    [Fact]
    public async Task H04_A_load_of_one_product_orders_after_appends_of_the_other()
    {
        var store = Store();
        for (var i = 0; i < 50; i++)
            store.Append(new[] { EpisodeDatasetRecord.FromEpisode(Episode("EP-" + i)) });

        // A research load queued behind fifty episode appends.
        Assert.Empty(await store.LoadEffortResultResearchAsync());
        Assert.Equal(50, store.RowsWritten);
    }

    [Fact]
    public void C02_Append_of_nothing_is_harmless()
    {
        var store = Store();
        store.Append(null);
        store.Append(Array.Empty<EpisodeDatasetRecord>());
        Assert.Equal(0, store.RowsAppended);
    }

    // ========== F: disabling pauses, it does not erase ==========

    /// <summary>
    /// Turning the Research feature off must not reset the scanner's corpus.
    ///
    /// `Configure(enabled: false)` used to call `Reset()`, clearing the rows, the dedup set
    /// and every count. The host object surviving the toggle was no comfort at all: a flag
    /// flipped twice mid-session silently reset the sample any later calibration decision
    /// would rest on, and `G-CAL-002` step 4 wants a sample spanning sessions.
    ///
    /// The published snapshot still goes to Disabled - that is the module's public statement
    /// about what it is doing now, and it is unchanged.
    /// </summary>
    [Fact]
    public async Task F01_Disabling_the_scanner_preserves_its_corpus()
    {
        var store = Store();
        var scanner = new HistoricalScannerHost(
            new HistoricalScannerPolicyConfig(enabled: true), store);
        var closed = Phase5AHistoricalScannerTests.ClosedForStore(Episode("EP-1"), Episode("EP-2"));
        var at = new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);

        scanner.Rebuild(closed, at);
        await Written(store);

        var collected = scanner.Current!.RowsCollected;
        var rows = scanner.Rows.Count;
        Assert.Equal(2, collected);

        scanner.Configure(new HistoricalScannerPolicyConfig(enabled: false));

        // Public output says Disabled; the corpus behind it is intact.
        Assert.Equal(HistoricalScannerState.Disabled, scanner.Current!.State);
        Assert.Equal(rows, scanner.Rows.Count);

        // Re-enabled, the same rolling window is NOT folded again: the dedup set survived.
        scanner.Configure(new HistoricalScannerPolicyConfig(enabled: true));
        scanner.Rebuild(closed, at);

        Assert.Equal(collected, scanner.Current!.RowsCollected);
        Assert.Equal(rows, scanner.Rows.Count);
        Assert.Equal(2, (await Store().LoadAsync()).Count);
    }

    /// <summary>A disabled rebuild collects nothing and leaves the file alone.</summary>
    [Fact]
    public async Task F02_A_disabled_scanner_collects_nothing_and_writes_nothing()
    {
        var store = Store();
        var scanner = new HistoricalScannerHost(
            new HistoricalScannerPolicyConfig(enabled: true), store);
        var at = new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);

        scanner.Rebuild(Phase5AHistoricalScannerTests.ClosedForStore(Episode("EP-1")), at);
        await Written(store);
        var before = File.ReadAllBytes(store.FilePath);

        scanner.Configure(new HistoricalScannerPolicyConfig(enabled: false));
        scanner.Rebuild(Phase5AHistoricalScannerTests.ClosedForStore(Episode("EP-2")), at);

        Assert.Equal(before, File.ReadAllBytes(store.FilePath));
        Assert.Equal(1, store.RowsWritten);
    }
}
