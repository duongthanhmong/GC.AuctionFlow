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

    private static async Task<IReadOnlyList<PersistedEpisodeRow>> Settle(EpisodeDatasetStore store)
    {
        // The flush is dispatched, not synchronous — that is the point of the design, so the
        // test waits for the effect rather than assuming it already happened.
        for (var i = 0; i < 500; i++)
        {
            var rows = await store.LoadAsync();
            if (rows.Count > 0) return rows;
            await Task.Delay(20);
        }

        return await store.LoadAsync();
    }

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

        var rows = await Settle(writer);
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
        await Settle(first);

        var second = Store();
        second.Append(new[] { EpisodeDatasetRecord.FromEpisode(Episode("EP-2")) });

        // Generous, because the write is genuinely asynchronous and the whole suite
        // competes for the same disk. A short budget here measures the machine, not the
        // store.
        for (var i = 0; i < 500; i++)
        {
            if ((await second.LoadAsync()).Count == 2) break;
            await Task.Delay(20);
        }

        Assert.Equal(2, (await second.LoadAsync()).Count);
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
        await Settle(store);

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
        await Settle(store);

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

        var rows = await Settle(store);
        Assert.Equal(2, rows.Count);
    }

    /// <summary>A restart must recover the sample and say how much of it was restored.</summary>
    [Fact]
    public async Task D02_A_restarted_scanner_recovers_the_sample()
    {
        var first = new HistoricalScannerHost(
            new HistoricalScannerPolicyConfig(enabled: true), Store());
        first.Rebuild(
            Phase5AHistoricalScannerTests.ClosedForStore(Episode("EP-1"), Episode("EP-2")),
            new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc));

        await Settle(Store());

        var restarted = new HistoricalScannerHost(
            new HistoricalScannerPolicyConfig(enabled: true), Store());

        // The load is dispatched, so it lands on a later rebuild rather than the first.
        for (var i = 0; i < 100 && restarted.RecoveredRows == 0; i++)
        {
            restarted.Rebuild(null, new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc));
            await Task.Delay(20);
        }

        Assert.Equal(2, restarted.RecoveredRows);
        Assert.Equal(2, restarted.Current!.RowsCollected);
    }

    /// <summary>
    /// A restart must not re-append what it just recovered.
    ///
    /// The dedup set is seeded by the load, and the load is asynchronous, so a rebuild that
    /// folds before it lands writes episodes that are already in the file. Measured on the
    /// live dataset: 5,126 lines holding 2,098 distinct episodes — the sample looked twice
    /// the size it was, and every distribution built on it would have been weighted by how
    /// often the operator restarted.
    /// </summary>
    [Fact]
    public async Task D03_A_restart_does_not_duplicate_the_episodes_it_recovered()
    {
        var closed = Phase5AHistoricalScannerTests.ClosedForStore(Episode("EP-1"), Episode("EP-2"));
        var at = new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);

        var first = new HistoricalScannerHost(
            new HistoricalScannerPolicyConfig(enabled: true), Store());
        for (var i = 0; i < 20; i++) first.Rebuild(closed, at);
        await Settle(Store());

        Assert.Equal(2, (await Store().LoadAsync()).Count);

        // A new process, seeing the very same rolling window of closed episodes. It folds
        // them before its own recovery lands — which is exactly the sequence that produced
        // the duplicates — so the store has to be the thing that refuses them.
        var reopened = Store();
        var restarted = new HistoricalScannerHost(
            new HistoricalScannerPolicyConfig(enabled: true), reopened);

        for (var i = 0; i < 200 && reopened.DuplicatesRejected < 2; i++)
        {
            restarted.Rebuild(closed, at);
            await Task.Delay(20);
        }

        Assert.Equal(2, reopened.DuplicatesRejected);
        Assert.Equal(0, reopened.RowsWritten);

        // The file is what matters: still one line per episode.
        Assert.Equal(2, (await Store().LoadAsync()).Count);
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

        for (var i = 0; i < 400 && store.RowsWritten < rows; i++)
            await Task.Delay(20);

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
        await Settle(store);

        // The store reports which kind of thread did the writing. `Task.Run` would make
        // this true, and `Task.Run` is what broke the chart.
        Assert.False(store.WroteOnThreadPoolThread);
        Assert.Equal(1, store.WriteBatches);
    }

    [Fact]
    public void C02_Append_of_nothing_is_harmless()
    {
        var store = Store();
        store.Append(null);
        store.Append(Array.Empty<EpisodeDatasetRecord>());
        Assert.Equal(0, store.RowsAppended);
    }
}
