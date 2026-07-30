using System.IO;
using System.Text;
using System.Text.Json;
using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Logging;
using GC.AuctionFlow.Reference;

namespace GC.AuctionFlow.Research;

/// <summary>
/// Keeps the research datasets across restarts.
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
/// **All I/O runs on one dedicated thread that this store owns. Reads included.**
///
/// The first version of this class dispatched each flush with `Task.Run` and wrote through
/// `File.AppendAllText`, which opens, writes and closes the file every time. That looked
/// off-thread and was not: ATAS pumps its own data through the same ThreadPool, so a session
/// producing thousands of rows produced thousands of open/write/close operations competing
/// for the very threads the platform needed, and the abnormal chart bar came straight back.
/// It was the original stall moved rather than removed.
///
/// The writes were then moved onto a dedicated thread and the reads were left on `Task.Run`,
/// which made the class comment true of half the class. A file read is the same competition
/// for the same pool; being on the write path is not what made the write path dangerous.
/// So the queue now carries **commands**, not rows: appends and loads alike are handed to the
/// one worker, and a load answers through a `TaskCompletionSource` whose continuations are
/// forced asynchronous so a caller's `await` can never run inline on the I/O thread.
///
/// **Two products, one writer.**
///
/// WP01A added a second research product — Effort/Result observations. It gets its own file,
/// because two schemas in one stream would force every reader to discriminate before it could
/// parse, and because the episode file's bytes must not change. It does **not** get its own
/// writer, its own thread, or its own reader.
/// </summary>
public sealed class EpisodeDatasetStore
{
    public const string DirectoryName = "research";
    public const string FileName = "episode-dataset.jsonl";

    /// <summary>The Effort/Result research product. A separate file, never a separate writer.</summary>
    public const string EffortResultResearchFileName = "effort-result-research.jsonl";

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    /// <summary>What a queued work item asks the worker to do.</summary>
    private enum WorkKind
    {
        AppendEpisode = 0,
        AppendResearch = 1,
        LoadEpisodes = 2,
        LoadResearch = 3,
    }

    /// <summary>
    /// One unit of work for the owned thread.
    ///
    /// Serialization and deserialization both happen on the worker, never here. The caller —
    /// an ATAS-driven module step — pays for an enqueue and nothing else, whichever kind of
    /// work it is asking for.
    /// </summary>
    private readonly struct WorkItem
    {
        private WorkItem(WorkKind kind, EpisodeDatasetRecord? episode,
            EffortResultResearchRecord? research, object? completion)
        {
            Kind = kind;
            Episode = episode;
            Research = research;
            Completion = completion;
        }

        public WorkKind Kind { get; }
        public EpisodeDatasetRecord? Episode { get; }
        public EffortResultResearchRecord? Research { get; }

        /// <summary>The <c>TaskCompletionSource</c> a load answers through. Null for appends.</summary>
        public object? Completion { get; }

        public static WorkItem Append(EpisodeDatasetRecord row) =>
            new(WorkKind.AppendEpisode, row, null, null);

        public static WorkItem Append(EffortResultResearchRecord row) =>
            new(WorkKind.AppendResearch, null, row, null);

        public static WorkItem LoadEpisodes(
            TaskCompletionSource<IReadOnlyList<PersistedEpisodeRow>> tcs) =>
            new(WorkKind.LoadEpisodes, null, null, tcs);

        public static WorkItem LoadResearch(
            TaskCompletionSource<IReadOnlyList<EffortResultResearchRecord>> tcs) =>
            new(WorkKind.LoadResearch, null, null, tcs);
    }

    /// <summary>
    /// How long the writer thread waits for more work before retiring. A session's rows
    /// arrive in bursts, so the thread exists during a burst and not between them.
    /// </summary>
    private const int IdleRetireMilliseconds = 5_000;

    private readonly object _gate = new();
    private readonly Queue<WorkItem> _pending = new();
    private readonly string _path;
    private readonly string _researchPath;

    /// <summary>
    /// Episode ids already on disk. Touched only by the worker thread, so it needs no lock
    /// — and must stay that way.
    /// </summary>
    private readonly HashSet<string> _onDisk = new(StringComparer.Ordinal);
    private bool _knownIdsLoaded;

    /// <summary>
    /// Research storage keys already on disk. Worker thread only, as above.
    ///
    /// The key is <c>schema + observation id</c>, not the observation id alone. A row written
    /// under a future schema must not stop the current schema's row for the same observation
    /// from being written — otherwise a newer build's file would silently suppress this
    /// build's collection, and the gap would look like the market simply produced nothing.
    /// </summary>
    private readonly HashSet<string> _researchOnDisk = new(StringComparer.Ordinal);
    private bool _researchKnownIdsLoaded;

    private Thread? _writer;
    private long _duplicatesRejected;
    private long _rowsAppended;
    private long _rowsWritten;
    private long _rowsLoaded;
    private long _writeFailures;
    private long _writeBatches;
    private int _wroteOnPoolThread;
    private int _readOnPoolThread;

    private long _researchRowsAppended;
    private long _researchRowsWritten;
    private long _researchRowsLoaded;
    private long _researchDuplicatesRejected;
    private long _researchWriteBatches;
    private long _researchUnknownSchemaRows;
    private long _researchMalformedRows;

    public EpisodeDatasetStore(string? userProfileOverride = null)
    {
        var profile = userProfileOverride
                      ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var dir = Path.Combine(profile, SpoolRoot.DirectoryName, DirectoryName);
        _path = Path.Combine(dir, FileName);
        _researchPath = Path.Combine(dir, EffortResultResearchFileName);
    }

    /// <summary>Where the dataset lives on disk.</summary>
    public string FilePath => _path;

    /// <summary>Where the Effort/Result research product lives on disk.</summary>
    public string EffortResultResearchFilePath => _researchPath;

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
    /// True if any <b>read</b> ran on a ThreadPool thread.
    ///
    /// Added because the write path being off-pool was mistaken for the whole contract. A
    /// file read competes for the same threads; `Task.Run` is `Task.Run` whichever direction
    /// the bytes travel.
    /// </summary>
    public bool ReadOnThreadPoolThread => Volatile.Read(ref _readOnPoolThread) != 0;

    /// <summary>Effort/Result research rows handed to the store, written or not.</summary>
    public long ResearchRowsAppended => Interlocked.Read(ref _researchRowsAppended);

    /// <summary>Effort/Result research rows that have reached disk.</summary>
    public long ResearchRowsWritten => Interlocked.Read(ref _researchRowsWritten);

    /// <summary>Effort/Result research rows recovered from a previous session.</summary>
    public long ResearchRowsLoaded => Interlocked.Read(ref _researchRowsLoaded);

    /// <summary>Research rows refused because that schema+observation was already in the file.</summary>
    public long ResearchDuplicatesRejected => Interlocked.Read(ref _researchDuplicatesRejected);

    /// <summary>
    /// How many times the research file has been opened to write.
    ///
    /// Counted separately from <see cref="WriteBatches"/> so the episode guard keeps
    /// measuring the episode file, and so the same per-row-open defect is visible on the new
    /// product without borrowing the old product's evidence.
    /// </summary>
    public long ResearchWriteBatches => Interlocked.Read(ref _researchWriteBatches);

    /// <summary>
    /// Research rows on disk whose <c>Schema</c> this build does not recognise.
    ///
    /// Surfaced rather than swallowed: a file written by a newer build would otherwise read
    /// as a smaller sample with no explanation, and a smaller sample that looks complete is
    /// the failure `G-CAL-001` exists to prevent.
    /// </summary>
    public long ResearchUnknownSchemaRows => Interlocked.Read(ref _researchUnknownSchemaRows);

    /// <summary>Research lines that could not be parsed at all — a truncated or damaged tail.</summary>
    public long ResearchMalformedRows => Interlocked.Read(ref _researchMalformedRows);

    /// <summary>
    /// Queues rows for the worker thread.
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
                _pending.Enqueue(WorkItem.Append(row));
                Interlocked.Increment(ref _rowsAppended);
                queued++;
            }

            if (queued == 0)
                return;

            EnsureWorkerStarted();
        }
    }

    /// <summary>
    /// Queues Effort/Result research observations for the worker thread.
    ///
    /// Same contract as <see cref="Append"/>, and deliberately the same thread: this is
    /// called from a module step that ATAS drives, so it must enqueue and return.
    /// </summary>
    public void AppendEffortResultResearch(IEnumerable<EffortResultResearchRecord>? rows)
    {
        if (rows is null)
            return;

        lock (_gate)
        {
            var queued = 0;
            foreach (var row in rows)
            {
                if (row is null) continue;
                _pending.Enqueue(WorkItem.Append(row));
                Interlocked.Increment(ref _researchRowsAppended);
                queued++;
            }

            if (queued == 0)
                return;

            EnsureWorkerStarted();
        }
    }

    /// <summary>
    /// Reads the episode dataset back, on the store's own thread.
    ///
    /// A malformed or truncated line is skipped, not fatal: the last line of an interrupted
    /// session is expected to be partial, and refusing the whole file over it would discard
    /// every good row before it.
    ///
    /// The returned task completes on the worker, but its continuations are forced
    /// asynchronous, so an awaiting caller never resumes on the I/O thread and cannot stall
    /// the next write behind its own work.
    /// </summary>
    public Task<IReadOnlyList<PersistedEpisodeRow>> LoadAsync()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<PersistedEpisodeRow>>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        lock (_gate)
        {
            _pending.Enqueue(WorkItem.LoadEpisodes(tcs));
            EnsureWorkerStarted();
        }

        return tcs.Task;
    }

    /// <summary>
    /// Reads the Effort/Result research product back, on the store's own thread.
    ///
    /// Only rows carrying the current registered schema are returned. A row from a newer
    /// build is counted in <see cref="ResearchUnknownSchemaRows"/> and left alone — never
    /// reinterpreted as the current shape, because a field that moved meaning between
    /// versions would be silently mis-read rather than loudly refused.
    /// </summary>
    public Task<IReadOnlyList<EffortResultResearchRecord>> LoadEffortResultResearchAsync()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<EffortResultResearchRecord>>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        lock (_gate)
        {
            _pending.Enqueue(WorkItem.LoadResearch(tcs));
            EnsureWorkerStarted();
        }

        return tcs.Task;
    }

    /// <summary>
    /// The storage deduplication key for a research row.
    ///
    /// Schema first, then the observation id. <see cref="EffortResultResearchRecord.ObservationId"/>
    /// stays exactly what it was so a research join across builds still works; this key exists
    /// only so one schema's row cannot block another's.
    /// </summary>
    internal static string ResearchStorageKey(string? schema, string? observationId) =>
        (schema ?? "") + "\u0001" + (observationId ?? "");

    /// <summary>
    /// Starts the worker if none is currently draining. Callers hold <c>_gate</c>.
    ///
    /// Started under the lock so two callers cannot race one into existence twice, and
    /// restarted freely: the previous thread retires when a burst ends.
    /// </summary>
    private void EnsureWorkerStarted()
    {
        if (_writer is null)
        {
            _writer = new Thread(WorkerLoop)
            {
                IsBackground = true,
                Name = "gcae-research-dataset",
            };
            _writer.Start();
        }

        Monitor.Pulse(_gate);
    }

    /// <summary>
    /// Drains the queue, one file open per product per burst rather than per row.
    ///
    /// Retires after an idle spell instead of spinning for the life of the process. A
    /// research store should cost nothing on a session that closes no episodes.
    /// </summary>
    private void WorkerLoop()
    {
        while (true)
        {
            List<WorkItem> batch;

            lock (_gate)
            {
                while (_pending.Count == 0)
                {
                    if (!Monitor.Wait(_gate, IdleRetireMilliseconds))
                    {
                        // Nothing arrived. Retire, and let the next command start a
                        // successor.
                        _writer = null;
                        return;
                    }
                }

                batch = new List<WorkItem>(_pending.Count);
                while (_pending.Count > 0)
                    batch.Add(_pending.Dequeue());
            }

            Drain(batch);
        }
    }

    /// <summary>
    /// Processes one drained burst: writes first, then answers loads.
    ///
    /// Writes go first so a load queued behind an append sees that append, which is what a
    /// caller polling for its own rows expects. Splitting by product here rather than keeping
    /// separate queues is what keeps a burst intact — rows that arrived together are written
    /// together, whichever product they belong to.
    /// </summary>
    private void Drain(List<WorkItem> batch)
    {
        List<EpisodeDatasetRecord>? episodes = null;
        List<EffortResultResearchRecord>? research = null;
        List<WorkItem>? loads = null;

        foreach (var item in batch)
        {
            switch (item.Kind)
            {
                case WorkKind.AppendEpisode when item.Episode is not null:
                    (episodes ??= new List<EpisodeDatasetRecord>()).Add(item.Episode);
                    break;
                case WorkKind.AppendResearch when item.Research is not null:
                    (research ??= new List<EffortResultResearchRecord>()).Add(item.Research);
                    break;
                case WorkKind.LoadEpisodes:
                case WorkKind.LoadResearch:
                    (loads ??= new List<WorkItem>()).Add(item);
                    break;
            }
        }

        if (episodes is not null)
            WriteBatch(episodes);
        if (research is not null)
            WriteResearchBatch(research);

        if (loads is null)
            return;

        foreach (var load in loads)
        {
            if (load.Kind == WorkKind.LoadEpisodes)
            {
                var tcs = (TaskCompletionSource<IReadOnlyList<PersistedEpisodeRow>>)load.Completion!;
                tcs.TrySetResult(ReadEpisodes());
            }
            else
            {
                var tcs = (TaskCompletionSource<IReadOnlyList<EffortResultResearchRecord>>)
                    load.Completion!;
                tcs.TrySetResult(ReadResearch());
            }
        }
    }

    private void MarkReadThread()
    {
        if (Thread.CurrentThread.IsThreadPoolThread)
            Volatile.Write(ref _readOnPoolThread, 1);
    }

    private IReadOnlyList<PersistedEpisodeRow> ReadEpisodes()
    {
        MarkReadThread();
        var rows = new List<PersistedEpisodeRow>();

        try
        {
            if (!File.Exists(_path))
                return rows;

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

        return rows;
    }

    private IReadOnlyList<EffortResultResearchRecord> ReadResearch()
    {
        MarkReadThread();
        var rows = new List<EffortResultResearchRecord>();
        long unknown = 0;
        long malformed = 0;

        try
        {
            if (!File.Exists(_researchPath))
                return rows;

            foreach (var line in File.ReadLines(_researchPath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var row = JsonSerializer.Deserialize<EffortResultResearchRecord>(line, Json);
                    if (row is null || string.IsNullOrEmpty(row.ObservationId))
                    {
                        malformed++;
                        continue;
                    }

                    if (!string.Equals(
                            row.Schema,
                            EffortResultResearchPolicyConfig.RecordSchemaVersion,
                            StringComparison.Ordinal))
                    {
                        // Counted and left alone. Reinterpreting an unknown shape as this
                        // one is how a field that changed meaning becomes a silent error.
                        unknown++;
                        continue;
                    }

                    rows.Add(row);
                }
                catch (JsonException)
                {
                    malformed++;
                }
            }

            Interlocked.Exchange(ref _researchRowsLoaded, rows.Count);
            Interlocked.Exchange(ref _researchUnknownSchemaRows, unknown);
            Interlocked.Exchange(ref _researchMalformedRows, malformed);
        }
        catch (IOException)
        {
            // Research material, not a runtime dependency.
        }

        return rows;
    }

    /// <summary>Reads the ids already in the file, once, on the worker thread.</summary>
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

    /// <summary>
    /// Reads the research storage keys already in the file, once, on the worker thread.
    ///
    /// Rows of every schema are keyed, including ones this build cannot interpret, so a
    /// future row is neither rewritten nor allowed to shadow the current schema's row.
    /// </summary>
    private void EnsureResearchKnownIdsLoaded()
    {
        if (_researchKnownIdsLoaded)
            return;

        _researchKnownIdsLoaded = true;

        try
        {
            if (!File.Exists(_researchPath))
                return;

            foreach (var line in File.ReadLines(_researchPath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var row = JsonSerializer.Deserialize<EffortResultResearchRecord>(line, Json);
                    if (row is not null && !string.IsNullOrEmpty(row.ObservationId))
                        _researchOnDisk.Add(ResearchStorageKey(row.Schema, row.ObservationId));
                }
                catch (JsonException)
                {
                    // A truncated tail is expected; it simply is not a known key.
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

            AppendLines(_path, text.ToString());

            Interlocked.Add(ref _rowsWritten, written);
            Interlocked.Increment(ref _writeBatches);
        }
        catch (Exception)
        {
            Interlocked.Increment(ref _writeFailures);
        }
    }

    /// <summary>
    /// Drains a burst of research observations to their own file.
    ///
    /// Deduplication lives here for the same reason it lives in <see cref="WriteBatch"/>:
    /// the collector learns what a previous session wrote from an asynchronous load, so
    /// anything collected before that landed would be appended again. The file's uniqueness
    /// is the file's invariant.
    /// </summary>
    private void WriteResearchBatch(List<EffortResultResearchRecord> batch)
    {
        if (batch.Count == 0)
            return;

        if (Thread.CurrentThread.IsThreadPoolThread)
            Volatile.Write(ref _wroteOnPoolThread, 1);

        try
        {
            var dir = Path.GetDirectoryName(_researchPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            EnsureResearchKnownIdsLoaded();

            var text = new StringBuilder();
            var written = 0;
            foreach (var row in batch)
            {
                if (!_researchOnDisk.Add(ResearchStorageKey(row.Schema, row.ObservationId)))
                {
                    Interlocked.Increment(ref _researchDuplicatesRejected);
                    continue;
                }

                text.AppendLine(JsonSerializer.Serialize(row, Json));
                written++;
            }

            if (written == 0)
                return;

            AppendLines(_researchPath, text.ToString());

            Interlocked.Add(ref _researchRowsWritten, written);
            Interlocked.Increment(ref _researchWriteBatches);
        }
        catch (Exception)
        {
            Interlocked.Increment(ref _writeFailures);
        }
    }

    /// <summary>
    /// The one place this class opens a file to write.
    ///
    /// Append, never rewrite. A rewrite that fails halfway loses everything already earned;
    /// an append that fails halfway loses one line.
    ///
    /// Shared for reading so a concurrent load — or an operator looking at the file — is not
    /// an error, and closed at the end of the burst so nothing external is blocked between
    /// bursts. Both products go through here: a second copy of this method would be a second
    /// chance to reintroduce the open-per-row defect.
    /// </summary>
    private static void AppendLines(string path, string text)
    {
        var bytes = new UTF8Encoding(false).GetBytes(text);
        using var stream = new FileStream(
            path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite,
            bufferSize: 4096, FileOptions.None);

        stream.Write(bytes, 0, bytes.Length);
        // Flushed to the OS, never to the platter. `flushToDisk: true` inside a callback is
        // the defect that started all of this; there is no reason to pay a device sync for
        // research rows.
        stream.Flush();
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
