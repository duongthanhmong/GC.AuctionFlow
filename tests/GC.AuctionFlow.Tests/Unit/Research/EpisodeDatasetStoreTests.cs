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
        for (var i = 0; i < 100; i++)
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

        for (var i = 0; i < 100; i++)
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

    [Fact]
    public void C02_Append_of_nothing_is_harmless()
    {
        var store = Store();
        store.Append(null);
        store.Append(Array.Empty<EpisodeDatasetRecord>());
        Assert.Equal(0, store.RowsAppended);
    }
}
