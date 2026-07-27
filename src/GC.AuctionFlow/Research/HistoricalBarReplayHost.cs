using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Reference;

namespace GC.AuctionFlow.Research;

/// <summary>
/// Walks the bars already loaded on the chart against the confirmed reference set.
///
/// v1.2 §46.2 says the Historical Scanner exists so research does not have to wait months
/// for live data, and the live episode path cannot deliver that: an episode closes only
/// when the primary auction rolls or its reference retires, which is roughly one batch of
/// rows per trading day. This walks the history that is already in memory instead.
///
/// It does **not** feed the episode registry. `EpisodeTradeEvent` is documented as built
/// from trade prints and never from candles, and that is a locked-phase invariant. So this
/// produces a separate, explicitly weaker dataset that is counted separately and never
/// pooled with live rows.
///
/// Only Confirmed references are replayed. Those are previous-auction levels, fixed for
/// the whole session, so testing this session's bars against them is exact. Developing
/// references migrate as the auction builds, and measuring an old bar against a level that
/// did not yet hold that price would be a reconstruction, not an observation.
/// </summary>
public sealed class HistoricalBarReplayHost
{
    /// <summary>
    /// Bars buffered while waiting for a confirmed reference set.
    ///
    /// A retention bound. When a chart holds more than this, the earliest bars are not
    /// replayed and the state says so rather than reporting a smaller history as complete.
    /// </summary>
    public const int BarBufferCapacity = 20_000;

    /// <summary>Interaction rows retained. Bounded for the same reason as the episode dataset.</summary>
    public const int RowCapacity = 50_000;

    private readonly List<ProfileBarObservation> _buffered = new();
    private readonly List<BarDerivedInteractionRecord> _rows = new();
    private readonly HashSet<int> _replayedBars = new();

    private bool _enabled;
    private bool _replayed;
    private bool _bufferOverflowed;
    private int _rowsMeasured;
    private int _rowsDropped;
    private int _barsWalked;
    private int _referencesReplayed;

    public HistoricalBarReplayHost(bool enabled = false)
    {
        _enabled = enabled;
    }

    public IReadOnlyList<BarDerivedInteractionRecord> Rows => _rows;

    /// <summary>Every row ever measured, independent of how many are retained.</summary>
    public int RowsMeasured => _rowsMeasured;

    public int RowsDroppedToCapacity => _rowsDropped;

    public int BarsWalked => _barsWalked;

    public int ReferencesReplayed => _referencesReplayed;

    public BarReplayState State =>
        !_enabled ? BarReplayState.Disabled
        : _bufferOverflowed && _replayed ? BarReplayState.TruncatedByBuffer
        : _replayed ? BarReplayState.Replayed
        : BarReplayState.AwaitingReferences;

    public void Configure(bool enabled)
    {
        if (_enabled == enabled)
            return;

        _enabled = enabled;
        if (!enabled)
            Reset();
    }

    /// <summary>
    /// Buffers one bar as the profile ingests it.
    ///
    /// Called from the existing per-bar pass, so the whole loaded history arrives in order
    /// without a second walk over the chart. Only completed bars are kept: a forming bar's
    /// high, low and close all still move.
    /// </summary>
    public void ObserveBar(ProfileBarObservation? bar)
    {
        if (!_enabled || bar is null || !bar.IsCompleted)
            return;

        if (_buffered.Count >= BarBufferCapacity)
        {
            // Drop the oldest and record that the history is no longer whole. Silently
            // keeping the newest would report a partial walk as a complete one.
            _buffered.RemoveAt(0);
            _bufferOverflowed = true;
        }

        _buffered.Add(bar);
    }

    /// <summary>
    /// Measures buffered bars against the confirmed references.
    ///
    /// The first call with a usable reference set walks everything buffered; later calls
    /// fold only bars not already measured. A bar is measured once — re-measuring it
    /// against the same references would duplicate it into every distribution.
    /// </summary>
    public void Replay(StructuralReferenceSetSnapshot? references, decimal tickSize)
    {
        if (!_enabled || _buffered.Count == 0 || tickSize <= 0m)
            return;

        var confirmed = references?.ConfirmedReferences;

        if (confirmed is null || confirmed.Count == 0)
            return;

        _referencesReplayed = confirmed.Count;

        foreach (var bar in _buffered)
        {
            if (!_replayedBars.Add(bar.BarIndex))
                continue;

            _barsWalked++;

            foreach (var reference in confirmed)
            {
                var row = BarDerivedInteractionRecord.TryMeasure(bar, reference, tickSize);
                if (row is null)
                    continue;

                _rows.Add(row);
                _rowsMeasured++;

                while (_rows.Count > RowCapacity)
                {
                    _rows.RemoveAt(0);
                    _rowsDropped++;
                }
            }
        }

        _replayed = true;
    }

    private void Reset()
    {
        _buffered.Clear();
        _rows.Clear();
        _replayedBars.Clear();
        _replayed = false;
        _bufferOverflowed = false;
        _rowsMeasured = 0;
        _rowsDropped = 0;
        _barsWalked = 0;
        _referencesReplayed = 0;
    }
}
