using GC.AuctionFlow.Profile;

namespace GC.AuctionFlow.Research;

/// <summary>
/// Volatility regime bucket, used only as a stratification key.
/// </summary>
public enum VolatilityRegimeBucket
{
    /// <summary>No boundaries have been registered, so nothing can be placed.</summary>
    Unavailable = 0,
    Low = 1,
    Medium = 2,
    High = 3
}

/// <summary>
/// One completed TPO period's realized range, in ticks.
///
/// Observable and complete: a finished period has a settled high and low. No threshold is
/// involved in producing it. This is the raw material `G-CAL-002` step 1 asks for, and it
/// is all the DLL contributes to this axis.
/// </summary>
public sealed class VolatilityObservation
{
    public VolatilityObservation(int periodIndex, DateTime periodEndUtc, long realizedRangeTicks)
    {
        PeriodIndex = periodIndex;
        PeriodEndUtc = periodEndUtc;
        RealizedRangeTicks = realizedRangeTicks;
    }

    public int PeriodIndex { get; }
    public DateTime PeriodEndUtc { get; }
    public long RealizedRangeTicks { get; }

    public static VolatilityObservation? TryFrom(CompletedTpoPeriodSnapshot period)
    {
        ArgumentNullException.ThrowIfNull(period);

        var range = period.PeriodHighTick - period.PeriodLowTick;
        return range < 0 ? null : new VolatilityObservation(
            period.PeriodIndex, period.PeriodEndUtc, range);
    }
}

/// <summary>
/// Volatility regime boundaries, registered from outside the build.
///
/// The DLL does not derive these, and the reason is structural rather than cautious.
/// Boundaries computed from the observed sample would move every time a period completed,
/// which breaks the unlock protocol in three places at once: `G-FAST-001` forbids
/// re-choosing sample criteria after seeing results; the same episode would change stratum
/// retroactively, so no distribution built on them could be reproduced or refuted; and
/// `G-CAL-002` step 4 asks for out-of-sample validation, which is impossible when the
/// out-of-sample data helped build the bins it is judged against.
///
/// Registered boundaries are frozen, so all three work. The DLL supplies the raw
/// observations, a research process decides, the DECISION_LOG records it, and this type
/// carries the decision back in.
/// </summary>
public sealed class VolatilityRegimeBoundaries
{
    private VolatilityRegimeBoundaries(
        long? lowerBoundaryTicks,
        long? upperBoundaryTicks,
        string decisionLogReference)
    {
        LowerBoundaryTicks = lowerBoundaryTicks;
        UpperBoundaryTicks = upperBoundaryTicks;
        DecisionLogReference = decisionLogReference;
    }

    /// <summary>Below this, a period is Low.</summary>
    public long? LowerBoundaryTicks { get; }

    /// <summary>At or above this, a period is High.</summary>
    public long? UpperBoundaryTicks { get; }

    /// <summary>
    /// Where the decision is recorded. Required, because `G-CAL-002` step 6 makes a
    /// DECISION_LOG entry part of the protocol — a boundary with no provenance is
    /// indistinguishable from one somebody typed in, which is what `G-CAL-001` bans.
    /// </summary>
    public string DecisionLogReference { get; }

    public bool CanStratify => LowerBoundaryTicks.HasValue && UpperBoundaryTicks.HasValue;

    /// <summary>Nothing registered. The axis exists but cannot key a distribution.</summary>
    public static VolatilityRegimeBoundaries Unregistered { get; } = new(null, null, "");

    /// <summary>
    /// Accepts a registered pair.
    ///
    /// Rejects rather than corrects: a mis-ordered or unattributed pair is a governance
    /// failure, and silently repairing it would hide the failure while still producing
    /// buckets somebody would trust.
    /// </summary>
    public static VolatilityRegimeBoundaries Register(
        long lowerBoundaryTicks,
        long upperBoundaryTicks,
        string decisionLogReference)
    {
        if (string.IsNullOrWhiteSpace(decisionLogReference))
            return Unregistered;

        if (lowerBoundaryTicks >= upperBoundaryTicks || lowerBoundaryTicks < 0)
            return Unregistered;

        return new VolatilityRegimeBoundaries(
            lowerBoundaryTicks, upperBoundaryTicks, decisionLogReference.Trim());
    }

    /// <summary>
    /// Places one observation in a bucket.
    ///
    /// <see cref="VolatilityRegimeBucket.Unavailable"/> when nothing is registered — never
    /// a guessed bucket, which would put every period in one stratum and make the axis look
    /// present while stratifying nothing.
    /// </summary>
    public VolatilityRegimeBucket Classify(long realizedRangeTicks)
    {
        if (!CanStratify)
            return VolatilityRegimeBucket.Unavailable;

        if (realizedRangeTicks < LowerBoundaryTicks!.Value)
            return VolatilityRegimeBucket.Low;

        return realizedRangeTicks >= UpperBoundaryTicks!.Value
            ? VolatilityRegimeBucket.High
            : VolatilityRegimeBucket.Medium;
    }
}
