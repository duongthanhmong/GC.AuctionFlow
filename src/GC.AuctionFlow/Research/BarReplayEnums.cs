namespace GC.AuctionFlow.Research;

/// <summary>
/// Where a research row was measured from.
///
/// A bar-derived row and a live-trade row are not the same measurement, and pooling them
/// would be the same class of error as pooling across a missing stratification axis: the
/// combined set would look larger and be less sound. So provenance is carried on every
/// row and the two are counted separately.
/// </summary>
public enum ObservationSource
{
    /// <summary>Built from normalized trade prints. Full episode semantics.</summary>
    LiveTrades = 0,

    /// <summary>
    /// Built from completed bars. Geometry and volume-at-price are real; ordering within
    /// the bar is not available at any price.
    /// </summary>
    HistoricalBars = 1
}

/// <summary>
/// Whether the order of events inside a bar is known.
///
/// It never is. A bar reports open, high, low and close but not whether price went
/// O-H-L-C or O-L-H-C, so a bar that spans a reference cannot say whether it crossed
/// once or six times, nor which side it approached from. Assuming a path would invent the
/// sequence, and every acceptance and re-entry conclusion in this engine rests on
/// sequence.
///
/// The enum has one member deliberately: a second one would imply the first is sometimes
/// avoidable.
/// </summary>
public enum IntraBarPathAvailability
{
    Unavailable = 0
}

/// <summary>
/// Where a bar closed relative to a reference zone.
///
/// Unlike the path, this is unambiguous — the close is a single number. It is geometry,
/// not acceptance: closing above a level says nothing about whether the market accepted
/// value there.
/// </summary>
public enum BarCloseLocation
{
    Above = 0,
    Inside = 1,
    Below = 2
}

/// <summary>
/// State of the one-shot replay over already-loaded bars.
/// </summary>
public enum BarReplayState
{
    Disabled = 0,

    /// <summary>Bars are buffered but no confirmed reference set has appeared yet.</summary>
    AwaitingReferences = 1,

    /// <summary>The loaded history has been walked once; new bars fold incrementally.</summary>
    Replayed = 2,

    /// <summary>More bars were loaded than the buffer holds; the earliest were not replayed.</summary>
    TruncatedByBuffer = 3
}
