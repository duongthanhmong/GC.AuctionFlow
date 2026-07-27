namespace GC.AuctionFlow.Probe;

/// <summary>
/// Whether a trade arriving on a live callback actually describes the present.
/// </summary>
public enum TradeFreshness
{
    /// <summary>No usable source time, so freshness cannot be established.</summary>
    Unknown = 0,

    /// <summary>The trade happened at or after observation started.</summary>
    Live = 1,

    /// <summary>
    /// The trade happened before observation started and was delivered afterwards.
    /// Pre-start activity, which `LIVE_ONLY` forbids reconstructing.
    /// </summary>
    PreStartReplay = 2
}

/// <summary>
/// Separates live trades from replayed history arriving on the same callbacks.
///
/// On indicator add, ATAS delivers a backlog through the real-time trade callbacks. One
/// recorded session held **853 minutes** of exchange time written in **3.8 minutes** of
/// wall clock — roughly 224x real time — and the engine consumed all of it as if it were
/// happening now. Episodes formed, orderflow accumulated and delta moved on fourteen hours
/// of history compressed into seconds.
///
/// That was invisible from the inside. It was also what fooled the first analysis of the
/// spool: grouping by the time the recorder *wrote* each trade collapsed the backlog into a
/// few seconds and rendered it as a 47-point collapse that never happened. Grouping by
/// exchange source time showed no minute exceeding 8.1 points, and an external chart of the
/// same instrument showed no such move at all.
///
/// **No threshold is chosen here.** The boundary is the moment observation began, which
/// `LIVE_ONLY` already defines: activity before indicator start is not to be reconstructed.
/// Source time is read as UTC under the established `ATAS_CANDLE_TIME_UTC_V1` policy —
/// ATAS wall-clock values carry UTC even when `DateTimeKind` is `Unspecified`.
/// </summary>
public static class TradeObservationFreshness
{
    public static TradeFreshness Classify(long sourceTimeTicks, DateTime observationStartUtc)
    {
        if (sourceTimeTicks <= 0 || observationStartUtc == default)
            return TradeFreshness.Unknown;

        if (sourceTimeTicks > DateTime.MaxValue.Ticks)
            return TradeFreshness.Unknown;

        var sourceUtc = new DateTime(sourceTimeTicks, DateTimeKind.Utc);

        // Equal counts as live: a trade stamped exactly at start did not precede it.
        return sourceUtc < observationStartUtc
            ? TradeFreshness.PreStartReplay
            : TradeFreshness.Live;
    }

    /// <summary>
    /// Whether a trade may drive the live analysis chain.
    ///
    /// `Unknown` is admitted deliberately. A feed that gives no usable source time would
    /// otherwise be silently excluded from the entire engine, which is a larger failure
    /// than admitting a trade whose age cannot be established — and the existing data
    /// quality flags already carry that uncertainty forward.
    /// </summary>
    public static bool MayDriveLiveAnalysis(TradeFreshness freshness) =>
        freshness != TradeFreshness.PreStartReplay;
}
