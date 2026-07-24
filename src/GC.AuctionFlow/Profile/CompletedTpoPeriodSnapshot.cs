namespace GC.AuctionFlow.Profile;

/// <summary>
/// Immutable completed TPO period range for OTF / Directional Context.
/// Production feed — not diagnostics. Developing periods are excluded by construction.
/// </summary>
public sealed class CompletedTpoPeriodSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public CompletedTpoPeriodSnapshot(
        int periodIndex,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        decimal periodHigh,
        decimal periodLow,
        long periodHighTick,
        long periodLowTick)
    {
        PeriodIndex = periodIndex;
        PeriodStartUtc = periodStartUtc;
        PeriodEndUtc = periodEndUtc;
        PeriodHigh = periodHigh;
        PeriodLow = periodLow;
        PeriodHighTick = periodHighTick;
        PeriodLowTick = periodLowTick;
    }

    public int PeriodIndex { get; }
    public DateTime PeriodStartUtc { get; }
    public DateTime PeriodEndUtc { get; }
    public decimal PeriodHigh { get; }
    public decimal PeriodLow { get; }
    public long PeriodHighTick { get; }
    public long PeriodLowTick { get; }
    public string PeriodId => "TPO|" + PeriodIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
    public string Version => SnapshotVersion;
}
