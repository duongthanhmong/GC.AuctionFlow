namespace GC.AuctionFlow.Probe;

/// <summary>
/// What the platform's market clock turned out to be, relative to UTC.
/// </summary>
public enum MarketClockAlignment
{
    /// <summary>No usable sample yet.</summary>
    NotSampled = 0,

    /// <summary>Every sample sat on UTC within the sampling jitter.</summary>
    Utc = 1,

    /// <summary>Every sample sat at the same non-zero offset from UTC.</summary>
    FixedOffset = 2,

    /// <summary>Samples disagreed by more than the sampling method can explain.</summary>
    Unstable = 3,
}

/// <summary>
/// Measures the platform's market clock against UTC.
///
/// Every recorded frame carries `SourceTimeKindUnspecified`, and the project has been
/// reading those timestamps as UTC on the strength of `ATAS_CANDLE_TIME_UTC_V1` — a note
/// written from inspection, never from measurement. That assumption has already cost a day:
/// grouping recorded trades by arrival time instead of source time produced a 47-point
/// candle that TradingView showed had never happened.
///
/// `Indicator.MarketTime` makes the question answerable. Sampling it beside `DateTime.UtcNow`
/// at the same instant says what the platform's clock actually is, so the assumption can be
/// replaced by an observation.
///
/// This deliberately does **not** rewrite any timestamp. It reports. A measurement that
/// silently corrects data is indistinguishable from a bug when it is wrong, and nothing here
/// has earned that trust yet.
/// </summary>
public sealed class MarketClockProbe
{
    /// <summary>
    /// How far apart two clocks may read and still count as the same instant.
    ///
    /// `MarketTime` and `UtcNow` cannot be read atomically — there is always a little code
    /// between them, and either may be quantised to a coarser tick than the other. This
    /// bounds that sampling artefact and nothing else. It is a property of how the
    /// measurement is taken, not a threshold about the market, and it gates no state.
    /// </summary>
    public const int SamplingJitterMilliseconds = 2_000;

    /// <summary>
    /// The granularity real exchange offsets come in. Used only to report an offset in the
    /// units operators think in; a value that does not land on this grid is still reported,
    /// as itself.
    /// </summary>
    public const int OffsetGridMinutes = 15;

    private readonly object _gate = new();

    private long _samples;
    private long _minOffsetTicks;
    private long _maxOffsetTicks;
    private long _lastOffsetTicks;

    /// <summary>Samples taken.</summary>
    public long Samples { get { lock (_gate) return _samples; } }

    /// <summary>The most recent measured offset, market clock minus UTC.</summary>
    public TimeSpan LastOffset { get { lock (_gate) return TimeSpan.FromTicks(_lastOffsetTicks); } }

    /// <summary>
    /// The spread between the smallest and largest offset seen.
    ///
    /// Reported because a clock that drifts is a different finding from one that sits at an
    /// offset, and averaging the two together would hide it.
    /// </summary>
    public TimeSpan OffsetSpread
    {
        get
        {
            lock (_gate)
                return _samples == 0
                    ? TimeSpan.Zero
                    : TimeSpan.FromTicks(_maxOffsetTicks - _minOffsetTicks);
        }
    }

    /// <summary>
    /// Takes one sample.
    ///
    /// Both readings must come from the caller, taken as close together as it can manage —
    /// this type cannot read the platform clock itself and should not pretend to.
    /// </summary>
    public void Observe(DateTime marketTime, DateTime utcNow)
    {
        if (marketTime == default || utcNow == default)
            return;

        // `MarketTime` arrives with whatever Kind the platform chose, and this measurement
        // exists precisely because that Kind is not trusted. Comparing the wall-clock
        // readings is the whole point, so both are stripped to their face value rather than
        // converted — a conversion here would assume the answer.
        var offset = DateTime.SpecifyKind(marketTime, DateTimeKind.Unspecified)
                     - DateTime.SpecifyKind(utcNow, DateTimeKind.Unspecified);

        lock (_gate)
        {
            if (_samples == 0)
            {
                _minOffsetTicks = offset.Ticks;
                _maxOffsetTicks = offset.Ticks;
            }
            else
            {
                if (offset.Ticks < _minOffsetTicks) _minOffsetTicks = offset.Ticks;
                if (offset.Ticks > _maxOffsetTicks) _maxOffsetTicks = offset.Ticks;
            }

            _lastOffsetTicks = offset.Ticks;
            _samples++;
        }
    }

    /// <summary>What the samples say.</summary>
    public MarketClockAlignment Alignment
    {
        get
        {
            long samples, min, max;
            lock (_gate)
            {
                samples = _samples;
                min = _minOffsetTicks;
                max = _maxOffsetTicks;
            }

            if (samples == 0)
                return MarketClockAlignment.NotSampled;

            var jitter = TimeSpan.FromMilliseconds(SamplingJitterMilliseconds).Ticks;

            // A clock that wanders by more than the sampling method can explain is neither
            // UTC nor a fixed offset, and calling it either would be a guess.
            if (max - min > jitter)
                return MarketClockAlignment.Unstable;

            return Math.Abs(min) <= jitter && Math.Abs(max) <= jitter
                ? MarketClockAlignment.Utc
                : MarketClockAlignment.FixedOffset;
        }
    }

    /// <summary>
    /// The offset rounded to the grid real exchanges use, for reporting.
    ///
    /// Null when there is nothing to report or when the clock is unstable — a rounded number
    /// beside an unstable measurement reads as more settled than it is.
    /// </summary>
    public TimeSpan? ReportedOffset
    {
        get
        {
            var alignment = Alignment;
            if (alignment is MarketClockAlignment.NotSampled or MarketClockAlignment.Unstable)
                return null;

            if (alignment == MarketClockAlignment.Utc)
                return TimeSpan.Zero;

            var grid = TimeSpan.FromMinutes(OffsetGridMinutes).Ticks;
            var offset = LastOffset.Ticks;
            var rounded = (long)Math.Round((double)offset / grid) * grid;

            // Only snapped when the raw value genuinely sits on the grid. An offset that
            // does not is a finding, and rounding it away would erase it.
            return Math.Abs(offset - rounded)
                   <= TimeSpan.FromMilliseconds(SamplingJitterMilliseconds).Ticks
                ? TimeSpan.FromTicks(rounded)
                : LastOffset;
        }
    }

    /// <summary>
    /// One line for the card.
    ///
    /// The measurement is useless if it never reaches the operator: this project has already
    /// shipped five defects where a correct value was computed and then dropped at the
    /// display layer.
    /// </summary>
    public string Describe() => Alignment switch
    {
        MarketClockAlignment.NotSampled => "NOT SAMPLED",
        MarketClockAlignment.Utc => $"UTC (n={Samples})",
        MarketClockAlignment.FixedOffset =>
            $"UTC{Format(ReportedOffset ?? LastOffset)} (n={Samples})",
        _ => $"UNSTABLE (spread {OffsetSpread.TotalSeconds:0.###}s, n={Samples})",
    };

    private static string Format(TimeSpan offset)
    {
        var sign = offset.Ticks < 0 ? "-" : "+";
        var abs = offset.Duration();
        return abs.Minutes == 0
            ? $"{sign}{abs.Hours}"
            : $"{sign}{abs.Hours}:{abs.Minutes:00}";
    }
}
