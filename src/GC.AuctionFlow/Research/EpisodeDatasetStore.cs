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
/// **All I/O runs off the caller's thread.** The scanner is driven from the module schedule,
/// which runs on ATAS threads, and this project has already shipped one defect where disk
/// work there stalled the platform's data pump and corrupted a chart. Loads are dispatched
/// and merged when they arrive; writes are queued and flushed in the background.
/// </summary>
public sealed class EpisodeDatasetStore
{
    public const string DirectoryName = "research";
    public const string FileName = "episode-dataset.jsonl";

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    private readonly object _gate = new();
    private readonly List<EpisodeDatasetRecord> _pending = new();
    private readonly string _path;

    private int _flushDispatched;
    private long _rowsAppended;
    private long _rowsLoaded;
    private long _writeFailures;

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

    /// <summary>
    /// Queues rows and schedules a background flush.
    ///
    /// Returns immediately. Never touches the disk on the calling thread.
    /// </summary>
    public void Append(IEnumerable<EpisodeDatasetRecord>? rows)
    {
        if (rows is null)
            return;

        lock (_gate)
        {
            foreach (var row in rows)
            {
                if (row is null) continue;
                _pending.Add(row);
                Interlocked.Increment(ref _rowsAppended);
            }

            if (_pending.Count == 0)
                return;
        }

        if (Interlocked.CompareExchange(ref _flushDispatched, 1, 0) == 0)
            _ = Task.Run(Flush);
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

    private void Flush()
    {
        try
        {
            List<EpisodeDatasetRecord> batch;
            lock (_gate)
            {
                if (_pending.Count == 0) return;
                batch = new List<EpisodeDatasetRecord>(_pending);
                _pending.Clear();
            }

            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var text = new StringBuilder();
            foreach (var row in batch)
                text.AppendLine(JsonSerializer.Serialize(PersistedEpisodeRow.From(row), Json));

            // Append, never rewrite. A rewrite that fails halfway loses everything already
            // earned; an append that fails halfway loses one line.
            File.AppendAllText(_path, text.ToString(), new UTF8Encoding(false));
        }
        catch (Exception)
        {
            Interlocked.Increment(ref _writeFailures);
        }
        finally
        {
            Volatile.Write(ref _flushDispatched, 0);
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
