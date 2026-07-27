using System.IO;
using System.Text;
using System.Text.Json;
using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Logging;
using GC.AuctionFlow.Reference;

namespace GC.AuctionFlow.Research;

/// <summary>
/// Keeps the episode dataset across restarts.
///
/// Episodes close roughly once per trading day, at the auction anchor, so a session's whole
/// yield is a single batch of rows. Holding them in memory meant every restart discarded
/// the day's work — and `G-CAL-002` step 4 wants out-of-sample validation, which needs a
/// sample spanning many sessions. Without persistence, waiting longer accumulated nothing.
///
/// Append-only JSONL. A crash costs at most the rows not yet flushed, never the file, and a
/// partially written last line is skipped on load rather than failing the read — the same
/// reasoning the recorder applies to an unsealed segment.
///
/// **All I/O runs on one dedicated thread that this store owns.**
///
/// The first version of this class dispatched each flush with `Task.Run` and wrote through
/// `File.AppendAllText`, which opens, writes and closes the file every time. That looked
/// off-thread and was not: ATAS pumps its own data through the same ThreadPool, so a session
/// producing thousands of rows produced thousands of open/write/close operations competing
/// for the very threads the platform needed, and the abnormal chart bar came straight back.
/// It was the original stall moved rather than removed.
///
/// So: one long-running background thread, outside the ThreadPool entirely, holding one
/// FileStream open for the life of the store. Appends hand a row to a queue and return.
/// However many rows a session yields, the platform never waits on any of them.
/// </summary>
public sealed class EpisodeDatasetStore
{
    public const string DirectoryName = "research";
    public const string FileName = "episode-dataset.jsonl";

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    /// <summary>
    /// How long the writer thread waits for more rows before retiring. A session's rows
    /// arrive in bursts, so the thread exists during a burst and not between them.
    /// </summary>
    private const int IdleRetireMilliseconds = 5_000;

    private readonly object _gate = new();
    private readonly Queue<EpisodeDatasetRecord> _pending = new();
    private readonly string _path;

    /// <summary>
    /// Episode ids already on disk. Touched only by the writer thread, so it needs no lock
    /// — and must stay that way.
    /// </summary>
    private readonly HashSet<string> _onDisk = new(StringComparer.Ordinal);
    private bool _knownIdsLoaded;

    private Thread? _writer;
    private long _duplicatesRejected;
    private long _rowsAppended;
    private long _rowsWritten;
    private long _rowsLoaded;
    private long _writeFailures;
    private long _writeBatches;
    private int _wroteOnPoolThread;

    public EpisodeDatasetStore(string? userProfileOverride = null)
    {
        var profile = userProfileOverride
                      ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _path = Path.Combine(profile, SpoolRoot.DirectoryName, DirectoryName, FileName);
    }

    /// <summary>Where the dataset lives on disk.</summary>
    public string FilePath => _path;

    /// <summary>Rows handed to the store, whether or not they have reached disk yet.</summary>
    public long RowsAppended => Interlocked.Read(ref _rowsAppended);

    /// <summary>Rows recovered from a previous session.</summary>
    public long RowsLoaded => Interlocked.Read(ref _rowsLoaded);

    /// <summary>
    /// Write failures. Surfaced rather than swallowed: a store that silently stops
    /// persisting is indistinguishable from one with nothing to persist.
    /// </summary>
    public long WriteFailures => Interlocked.Read(ref _writeFailures);

    /// <summary>Rows that have reached disk.</summary>
    public long RowsWritten => Interlocked.Read(ref _rowsWritten);

    /// <summary>Rows refused because the episode was already in the file.</summary>
    public long DuplicatesRejected => Interlocked.Read(ref _duplicatesRejected);

    /// <summary>
    /// How many times the file has been opened to write.
    ///
    /// Measured because it is the quantity that broke the platform: the previous design
    /// opened the file once per append, so this tracked the row count. It must now track
    /// bursts instead, and a test asserts the difference.
    /// </summary>
    public long WriteBatches => Interlocked.Read(ref _writeBatches);

    /// <summary>
    /// True if any write ran on a ThreadPool thread.
    ///
    /// Reported rather than assumed. ATAS pumps market data through that pool, so a store
    /// that quietly moves back onto it is back to the defect that corrupted a chart, and
    /// nothing about the file on disk would show it.
    /// </summary>
    public bool WroteOnThreadPoolThread => Volatile.Read(ref _wroteOnPoolThread) != 0;

    /// <summary>
    /// Queues rows for the writer thread.
    ///
    /// Returns immediately. Touches neither the disk nor the ThreadPool on the calling
    /// thread — the scanner is driven from ATAS callbacks, and both are the platform's.
    /// </summary>
    public void Append(IEnumerable<EpisodeDatasetRecord>? rows)
    {
        if (rows is null)
            return;

        lock (_gate)
        {
            var queued = 0;
            foreach (var row in rows)
            {
                if (row is null) continue;
                _pending.Enqueue(row);
                Interlocked.Increment(ref _rowsAppended);
                queued++;
            }

            if (queued == 0)
                return;

            // Started under the lock so two callers cannot race one into existence twice,
            // and restarted freely: the previous thread retires when a burst ends.
            if (_writer is null)
            {
                _writer = new Thread(WriterLoop)
                {
                    IsBackground = true,
                    Name = "gcae-episode-dataset",
                };
                _writer.Start();
            }

            Monitor.Pulse(_gate);
        }
    }

    /// <summary>
    /// Drains the queue onto disk, one file open per burst rather than per row.
    ///
    /// Retires after an idle spell instead of spinning for the life of the process. A
    /// research store should cost nothing on a session that closes no episodes.
    /// </summary>
    private void WriterLoop()
    {
        while (true)
        {
            List<EpisodeDatasetRecord> batch;

            lock (_gate)
            {
                while (_pending.Count == 0)
                {
                    if (!Monitor.Wait(_gate, IdleRetireMilliseconds))
                    {
                        // Nothing arrived. Retire, and let the next append start a
                        // successor.
                        _writer = null;
                        return;
                    }
                }

                batch = new List<EpisodeDatasetRecord>(_pending.Count);
                while (_pending.Count > 0)
                    batch.Add(_pending.Dequeue());
            }

            WriteBatch(batch);
        }
    }

    /// <summary>
    /// Reads the dataset back on a background thread.
    ///
    /// A malformed or truncated line is skipped, not fatal: the last line of an
    /// interrupted session is expected to be partial, and refusing the whole file over it
    /// would discard every good row before it.
    /// </summary>
    public Task<IReadOnlyList<PersistedEpisodeRow>> LoadAsync() => Task.Run(() =>
    {
        var rows = new List<PersistedEpisodeRow>();

        try
        {
            if (!File.Exists(_path))
                return (IReadOnlyList<PersistedEpisodeRow>)rows;

            foreach (var line in File.ReadLines(_path))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var row = JsonSerializer.Deserialize<PersistedEpisodeRow>(line, Json);
                    if (row is not null && !string.IsNullOrEmpty(row.EpisodeId))
                        rows.Add(row);
                }
                catch (JsonException)
                {
                    // Truncated tail or a line from a newer schema. Skipped deliberately.
                }
            }

            Interlocked.Exchange(ref _rowsLoaded, rows.Count);
        }
        catch (IOException)
        {
            // The dataset is research material, not a runtime dependency. A read failure
            // must not stop the engine.
        }

        return (IReadOnlyList<PersistedEpisodeRow>)rows;
    });

    /// <summary>
    /// Reads the ids already in the file, once, on the writer thread.
    ///
    /// Deliberately not the caller's thread and deliberately not the ThreadPool: this is
    /// the read that has to happen before the first write, and both of those threads belong
    /// to the platform.
    /// </summary>
    private void EnsureKnownIdsLoaded()
    {
        if (_knownIdsLoaded)
            return;

        _knownIdsLoaded = true;

        try
        {
            if (!File.Exists(_path))
                return;

            foreach (var line in File.ReadLines(_path))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var row = JsonSerializer.Deserialize<PersistedEpisodeRow>(line, Json);
                    if (row is not null && !string.IsNullOrEmpty(row.EpisodeId))
                        _onDisk.Add(row.EpisodeId);
                }
                catch (JsonException)
                {
                    // A truncated tail is expected; it simply is not a known id.
                }
            }
        }
        catch (IOException)
        {
            // Unreadable means unknown, which costs a duplicate rather than a lost row.
        }
    }

    private void WriteBatch(List<EpisodeDatasetRecord> batch)
    {
        if (batch.Count == 0)
            return;

        if (Thread.CurrentThread.IsThreadPoolThread)
            Volatile.Write(ref _wroteOnPoolThread, 1);

        try
        {
            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            // One episode, one line — enforced here rather than by the caller.
            //
            // The scanner dedups within a session, but it learns what a *previous* session
            // wrote from an asynchronous load, so anything folded before that landed was
            // appended again. Measured on the live file: 5,126 lines holding 2,098 distinct
            // episodes. Every distribution built on it would have been weighted by how
            // often the operator restarted.
            //
            // Making the scanner wait for the load instead would tie whether a module
            // collects anything to disk latency. The file's uniqueness is the file's
            // invariant, so it is kept where the file is written.
            EnsureKnownIdsLoaded();

            var text = new StringBuilder();
            var written = 0;
            foreach (var row in batch)
            {
                if (!_onDisk.Add(row.EpisodeId))
                {
                    Interlocked.Increment(ref _duplicatesRejected);
                    continue;
                }

                text.AppendLine(JsonSerializer.Serialize(PersistedEpisodeRow.From(row), Json));
                written++;
            }

            if (written == 0)
                return;

            // Append, never rewrite. A rewrite that fails halfway loses everything already
            // earned; an append that fails halfway loses one line.
            //
            // Shared for reading so a concurrent load — or an operator looking at the file —
            // is not an error, and closed at the end of the burst so nothing external is
            // blocked between bursts.
            var bytes = new UTF8Encoding(false).GetBytes(text.ToString());
            using (var stream = new FileStream(
                       _path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite,
                       bufferSize: 4096, FileOptions.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                // Flushed to the OS, never to the platter. `flushToDisk: true` inside a
                // callback is the defect that started all of this; there is no reason to
                // pay a device sync for research rows.
                stream.Flush();
            }

            Interlocked.Add(ref _rowsWritten, written);
            Interlocked.Increment(ref _writeBatches);
        }
        catch (Exception)
        {
            Interlocked.Increment(ref _writeFailures);
        }
    }
}

/// <summary>
/// One dataset row as stored.
///
/// A flat, explicit shape rather than the live record: a file that outlives the process must
/// not depend on the internal type staying still, and a reader written months later should
/// not need this assembly to make sense of it.
/// </summary>
public sealed class PersistedEpisodeRow
{
    public string EpisodeId { get; set; } = "";
    public string PrimaryAuctionId { get; set; } = "";
    public string ReferenceId { get; set; } = "";
    public ReferenceType ReferenceType { get; set; }
    public ReferenceMaturity ReferenceMaturity { get; set; }
    public EpisodeInteractionDirection InteractionDirection { get; set; }
    public EpisodeState TerminalState { get; set; }
    public EpisodeResolution Resolution { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime LastUpdatedAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public int InteractionCount { get; set; }
    public int CrossCount { get; set; }
    public long MaximumAboveDistanceTicks { get; set; }
    public long MaximumBelowDistanceTicks { get; set; }
    public decimal CanonicalOutsideExecutedVolume { get; set; }
    public int CanonicalOutsideTradeCount { get; set; }
    public decimal? CanonicalOutsideDelta { get; set; }
    public AggressorEvidenceAvailability AggressorEvidence { get; set; }
    public EpisodeDataQuality DataQuality { get; set; }
    public DatasetRowAdmissibility Admissibility { get; set; }
    public string EpisodePolicyVersion { get; set; } = "";

    public static PersistedEpisodeRow From(EpisodeDatasetRecord r) => new()
    {
        EpisodeId = r.EpisodeId,
        PrimaryAuctionId = r.PrimaryAuctionId,
        ReferenceId = r.ReferenceId,
        ReferenceType = r.ReferenceType,
        ReferenceMaturity = r.ReferenceMaturity,
        InteractionDirection = r.InteractionDirection,
        TerminalState = r.TerminalState,
        Resolution = r.Resolution,
        StartedAtUtc = r.StartedAtUtc,
        LastUpdatedAtUtc = r.LastUpdatedAtUtc,
        AttemptCount = r.AttemptCount,
        InteractionCount = r.InteractionCount,
        CrossCount = r.CrossCount,
        MaximumAboveDistanceTicks = r.MaximumAboveDistanceTicks,
        MaximumBelowDistanceTicks = r.MaximumBelowDistanceTicks,
        CanonicalOutsideExecutedVolume = r.CanonicalOutsideExecutedVolume,
        CanonicalOutsideTradeCount = r.CanonicalOutsideTradeCount,
        CanonicalOutsideDelta = r.CanonicalOutsideDelta,
        AggressorEvidence = r.AggressorEvidence,
        DataQuality = r.DataQuality,
        Admissibility = r.Admissibility,
        EpisodePolicyVersion = r.EpisodePolicyVersion,
    };
}
