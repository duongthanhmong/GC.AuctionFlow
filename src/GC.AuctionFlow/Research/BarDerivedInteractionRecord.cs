using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Reference;

namespace GC.AuctionFlow.Research;

/// <summary>
/// What one completed bar unambiguously shows about one reference.
///
/// This is not an episode and must never be mistaken for one. An episode is a sequence —
/// approach, cross, excursion, re-entry — and a bar has no sequence at any price. What a
/// bar does give, honestly, is geometry and volume at price, and that is enough for the
/// revisit and distribution studies of v1.2 §46.7 without waiting months for live
/// episodes to accumulate.
///
/// Every field here is either a measurement the bar actually contains or an explicit
/// null. Nothing is inferred from the bar's shape.
/// </summary>
public sealed class BarDerivedInteractionRecord
{
    public BarDerivedInteractionRecord(
        int barIndex,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        string referenceId,
        ReferenceType referenceType,
        ReferenceMaturity referenceMaturity,
        decimal zoneLow,
        decimal zoneHigh,
        bool touchedZone,
        BarCloseLocation closeLocation,
        long? maximumExcursionAboveTicks,
        long? maximumExcursionBelowTicks,
        decimal? volumeAtZone,
        decimal? deltaAtZone,
        decimal barVolume,
        bool isHistorical)
    {
        BarIndex = barIndex;
        StartUtc = startUtc;
        EndUtc = endUtc;
        ReferenceId = referenceId ?? "";
        ReferenceType = referenceType;
        ReferenceMaturity = referenceMaturity;
        ZoneLow = zoneLow;
        ZoneHigh = zoneHigh;
        TouchedZone = touchedZone;
        CloseLocation = closeLocation;
        MaximumExcursionAboveTicks = maximumExcursionAboveTicks;
        MaximumExcursionBelowTicks = maximumExcursionBelowTicks;
        VolumeAtZone = volumeAtZone;
        DeltaAtZone = deltaAtZone;
        BarVolume = barVolume;
        IsHistorical = isHistorical;
    }

    public int BarIndex { get; }
    public DateTimeOffset StartUtc { get; }
    public DateTimeOffset EndUtc { get; }

    public string ReferenceId { get; }
    public ReferenceType ReferenceType { get; }
    public ReferenceMaturity ReferenceMaturity { get; }
    public decimal ZoneLow { get; }
    public decimal ZoneHigh { get; }

    /// <summary>The bar's range overlapped the zone. Geometry, not interaction intent.</summary>
    public bool TouchedZone { get; }

    public BarCloseLocation CloseLocation { get; }

    /// <summary>Ticks the high reached above the zone, or null when it never went above.</summary>
    public long? MaximumExcursionAboveTicks { get; }

    /// <summary>Ticks the low reached below the zone, or null when it never went below.</summary>
    public long? MaximumExcursionBelowTicks { get; }

    /// <summary>
    /// Executed volume at prices inside the zone. Null when the feed gave no volume-at-price
    /// for this bar — zero would claim the zone traded nothing, which is a different fact.
    /// </summary>
    public decimal? VolumeAtZone { get; }

    /// <summary>Ask minus bid volume inside the zone. Null unless both sides were reported.</summary>
    public decimal? DeltaAtZone { get; }

    public decimal BarVolume { get; }

    public bool IsHistorical { get; }

    /// <summary>Always <see cref="ObservationSource.HistoricalBars"/>.</summary>
    public ObservationSource Source => ObservationSource.HistoricalBars;

    /// <summary>Always unavailable. See <see cref="IntraBarPathAvailability"/>.</summary>
    public IntraBarPathAvailability IntraBarPath => IntraBarPathAvailability.Unavailable;

    /// <summary>
    /// Measures one bar against one reference.
    ///
    /// Returns null when the bar never reached the zone. A row per non-interaction would
    /// swamp the dataset with rows saying nothing happened, and "did not reach" is already
    /// recoverable from the bar series itself.
    /// </summary>
    public static BarDerivedInteractionRecord? TryMeasure(
        ProfileBarObservation bar,
        StructuralReferenceSnapshot reference,
        decimal tickSize)
    {
        ArgumentNullException.ThrowIfNull(bar);
        ArgumentNullException.ThrowIfNull(reference);

        if (tickSize <= 0m)
            return null;

        var low = Math.Min(reference.ZoneLow, reference.ZoneHigh);
        var high = Math.Max(reference.ZoneLow, reference.ZoneHigh);

        var touched = bar.Low <= high && bar.High >= low;
        if (!touched)
            return null;

        long? above = bar.High > high
            ? (long)Math.Round((bar.High - high) / tickSize, MidpointRounding.AwayFromZero)
            : null;
        long? below = bar.Low < low
            ? (long)Math.Round((low - bar.Low) / tickSize, MidpointRounding.AwayFromZero)
            : null;

        var closeLocation = bar.Close > high
            ? BarCloseLocation.Above
            : bar.Close < low
                ? BarCloseLocation.Below
                : BarCloseLocation.Inside;

        decimal? volumeAtZone = null;
        decimal? deltaAtZone = null;

        if (bar.PriceVolumes.Count > 0)
        {
            decimal volume = 0m;
            decimal bid = 0m;
            decimal ask = 0m;
            var anySided = false;

            foreach (var level in bar.PriceVolumes)
            {
                if (level.Price < low || level.Price > high)
                    continue;

                volume += level.ExecutedVolume;

                if (level.BidVolume.HasValue && level.AskVolume.HasValue)
                {
                    bid += level.BidVolume.Value;
                    ask += level.AskVolume.Value;
                    anySided = true;
                }
            }

            volumeAtZone = volume;

            // Only when every contributing level reported both sides. A partial delta is
            // a different measurement wearing the same name.
            if (anySided)
                deltaAtZone = ask - bid;
        }

        return new BarDerivedInteractionRecord(
            bar.BarIndex,
            bar.StartUtc,
            bar.EndUtc,
            reference.ReferenceId,
            reference.ReferenceType,
            reference.Maturity,
            low,
            high,
            touchedZone: true,
            closeLocation,
            above,
            below,
            volumeAtZone,
            deltaAtZone,
            bar.TotalVolume,
            bar.IsHistorical);
    }
}
