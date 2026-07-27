namespace GC.AuctionFlow.Profile;

/// <summary>What a comparison against the platform's own profile came to.</summary>
public enum FixedProfileParityState
{
    /// <summary>No comparison has been attempted.</summary>
    NotRequested = 0,

    /// <summary>Requested; the platform has not answered yet.</summary>
    Pending = 1,

    /// <summary>The platform answered but the profile carried no usable levels.</summary>
    Unavailable = 2,

    /// <summary>Every level matched exactly.</summary>
    Agreed = 3,

    /// <summary>At least one level differed. The differences are reported, not resolved.</summary>
    Disagreed = 4,
}

/// <summary>One level, ours beside theirs.</summary>
public sealed record FixedProfileParityLevel(
    string Name,
    decimal? Ours,
    decimal? Theirs,
    long? DifferenceTicks)
{
    /// <summary>True only on an exact match. Nothing here invents a tolerance.</summary>
    public bool Agrees => DifferenceTicks == 0;

    public string Describe() =>
        Ours is null || Theirs is null
            ? $"{Name} n/a"
            : DifferenceTicks == 0
                ? $"{Name} ok"
                : $"{Name} {Ours}/{Theirs} ({DifferenceTicks:+#;-#;0}t)";
}

/// <summary>
/// Compares this engine's profile levels against the platform's own.
///
/// The profile module has been READY since Phase 1 and nothing outside this project has
/// ever checked its arithmetic. `RequestFixedProfileAsync` hands back ATAS's own profile
/// for the same period, computed by a different codebase from the same feed — the only
/// independent check available without leaving the platform.
///
/// It is a check, never a source. Our values stay ours: a disagreement is a finding to
/// investigate, and silently adopting the platform's number would destroy the evidence that
/// there was ever a difference. `G-REF-001` puts reference derivation in this engine, and
/// that does not change because a second opinion exists.
///
/// Agreement means exactly equal. No tolerance is invented here — a one-tick difference is
/// reported as one tick and the operator decides what it means. Inventing a threshold is
/// precisely what the calibration rules forbid, and "close enough" is a threshold.
/// </summary>
public static class FixedProfileParity
{
    public const string LevelVpoc = "VPOC";
    public const string LevelValueAreaHigh = "VAH";
    public const string LevelValueAreaLow = "VAL";

    /// <summary>
    /// Builds the comparison.
    ///
    /// A level missing on either side is reported as missing rather than counted as
    /// agreement — an absent number and a matching number are opposite findings.
    /// </summary>
    public static FixedProfileParitySnapshot Compare(
        decimal? ourVpoc,
        decimal? ourValueAreaHigh,
        decimal? ourValueAreaLow,
        decimal? theirVpoc,
        decimal? theirValueAreaHigh,
        decimal? theirValueAreaLow,
        decimal tickSize,
        string period)
    {
        if (tickSize <= 0m)
            return new FixedProfileParitySnapshot(
                FixedProfileParityState.Unavailable, period, Array.Empty<FixedProfileParityLevel>());

        var levels = new[]
        {
            Level(LevelVpoc, ourVpoc, theirVpoc, tickSize),
            Level(LevelValueAreaHigh, ourValueAreaHigh, theirValueAreaHigh, tickSize),
            Level(LevelValueAreaLow, ourValueAreaLow, theirValueAreaLow, tickSize),
        };

        // Nothing comparable at all is a different answer from "we compared and agreed".
        if (levels.All(l => l.DifferenceTicks is null))
            return new FixedProfileParitySnapshot(
                FixedProfileParityState.Unavailable, period, levels);

        var state = levels.Any(l => l.DifferenceTicks is not null && l.DifferenceTicks != 0)
            ? FixedProfileParityState.Disagreed
            : FixedProfileParityState.Agreed;

        return new FixedProfileParitySnapshot(state, period, levels);
    }

    private static FixedProfileParityLevel Level(
        string name, decimal? ours, decimal? theirs, decimal tickSize)
    {
        if (ours is null || theirs is null || ours == 0m || theirs == 0m)
            return new FixedProfileParityLevel(name, ours, theirs, null);

        var ticks = (long)Math.Round((ours.Value - theirs.Value) / tickSize,
            MidpointRounding.AwayFromZero);

        return new FixedProfileParityLevel(name, ours, theirs, ticks);
    }
}

/// <summary>The comparison, as the card and the diagnostics read it.</summary>
public sealed class FixedProfileParitySnapshot
{
    public FixedProfileParitySnapshot(
        FixedProfileParityState state,
        string period,
        IReadOnlyList<FixedProfileParityLevel> levels)
    {
        State = state;
        Period = period ?? "";
        Levels = levels ?? Array.Empty<FixedProfileParityLevel>();
    }

    public FixedProfileParityState State { get; }

    /// <summary>Which fixed-profile period was compared.</summary>
    public string Period { get; }

    public IReadOnlyList<FixedProfileParityLevel> Levels { get; }

    /// <summary>
    /// One line for the card.
    ///
    /// A disagreement names the levels and the size of the gap. "DISAGREED" on its own
    /// would send the operator hunting for a number this already knows.
    /// </summary>
    public string Describe() => State switch
    {
        FixedProfileParityState.NotRequested => "NOT REQUESTED",
        FixedProfileParityState.Pending => "PENDING",
        FixedProfileParityState.Unavailable => "UNAVAILABLE",
        FixedProfileParityState.Agreed => $"AGREED ({Period})",
        _ => $"DISAGREED ({Period}) " + string.Join(" ",
            Levels.Where(l => l.DifferenceTicks is not null && l.DifferenceTicks != 0)
                  .Select(l => l.Describe())),
    };

    public static FixedProfileParitySnapshot NotRequested { get; } =
        new(FixedProfileParityState.NotRequested, "", Array.Empty<FixedProfileParityLevel>());

    public static FixedProfileParitySnapshot Pending(string period) =>
        new(FixedProfileParityState.Pending, period, Array.Empty<FixedProfileParityLevel>());
}
