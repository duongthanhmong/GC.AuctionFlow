using GC.AuctionFlow.Profile;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Profile;

/// <summary>
/// The first independent check on this engine's profile arithmetic.
///
/// The profile module has reported READY since Phase 1 and nothing outside this project has
/// ever verified it. `RequestFixedProfileAsync` returns ATAS's own profile for the same
/// period, built by a different codebase from the same feed.
///
/// It stays a check and never becomes a source: adopting the platform's number on a
/// disagreement would erase the evidence that there was one.
/// </summary>
public sealed class FixedProfileParityTests
{
    private const decimal Tick = 0.1m;

    private static FixedProfileParitySnapshot Compare(
        decimal? ourVpoc = 4094.1m, decimal? ourVah = 4101.0m, decimal? ourVal = 4086.1m,
        decimal? theirVpoc = 4094.1m, decimal? theirVah = 4101.0m, decimal? theirVal = 4086.1m,
        decimal tick = Tick) =>
        FixedProfileParity.Compare(
            ourVpoc, ourVah, ourVal, theirVpoc, theirVah, theirVal, tick, "LastDay");

    [Fact]
    public void A01_Identical_levels_agree()
    {
        var parity = Compare();
        Assert.Equal(FixedProfileParityState.Agreed, parity.State);
        Assert.All(parity.Levels, l => Assert.True(l.Agrees));
        Assert.Contains("AGREED", parity.Describe());
    }

    /// <summary>
    /// The whole point. If our profile arithmetic drifts from the platform's, the operator
    /// has to be told which level and by how much.
    /// </summary>
    [Fact]
    public void A02_A_disagreement_names_the_level_and_the_gap()
    {
        var parity = Compare(ourVpoc: 4094.4m);

        Assert.Equal(FixedProfileParityState.Disagreed, parity.State);

        var vpoc = parity.Levels.Single(l => l.Name == FixedProfileParity.LevelVpoc);
        Assert.Equal(3, vpoc.DifferenceTicks);

        var line = parity.Describe();
        Assert.Contains("DISAGREED", line);
        Assert.Contains("VPOC", line);
        Assert.Contains("+3t", line);
    }

    [Fact]
    public void A03_A_negative_gap_keeps_its_sign()
    {
        var parity = Compare(ourVal: 4085.9m);
        var val = parity.Levels.Single(l => l.Name == FixedProfileParity.LevelValueAreaLow);
        Assert.Equal(-2, val.DifferenceTicks);
        Assert.Contains("-2t", parity.Describe());
    }

    /// <summary>
    /// Agreement means equal. A one-tick gap is reported as one tick, because deciding that
    /// some gap is "close enough" is inventing a threshold, which the calibration rules
    /// forbid outright.
    /// </summary>
    [Fact]
    public void B01_One_tick_is_a_disagreement_not_a_rounding_allowance()
    {
        var parity = Compare(ourVah: 4101.1m);
        Assert.Equal(FixedProfileParityState.Disagreed, parity.State);
        Assert.Equal(1, parity.Levels.Single(l => l.Name == FixedProfileParity.LevelValueAreaHigh)
            .DifferenceTicks);
    }

    /// <summary>
    /// A missing level and a matching level are opposite findings. Counting an absent number
    /// as agreement would let a profile that produced nothing report a clean bill.
    /// </summary>
    [Fact]
    public void B02_A_missing_level_is_not_counted_as_agreement()
    {
        var parity = Compare(theirVpoc: null);
        var vpoc = parity.Levels.Single(l => l.Name == FixedProfileParity.LevelVpoc);
        Assert.Null(vpoc.DifferenceTicks);
        Assert.False(vpoc.Agrees);
        Assert.Equal(FixedProfileParityState.Agreed, parity.State); // the other two matched
    }

    [Fact]
    public void B03_Nothing_comparable_is_unavailable_not_agreed()
    {
        var parity = Compare(theirVpoc: null, theirVah: null, theirVal: null);
        Assert.Equal(FixedProfileParityState.Unavailable, parity.State);
        Assert.Equal("UNAVAILABLE", parity.Describe());
    }

    [Fact]
    public void B04_A_zero_level_is_absent_not_a_price()
    {
        var parity = Compare(theirVpoc: 0m, theirVah: 0m, theirVal: 0m);
        Assert.Equal(FixedProfileParityState.Unavailable, parity.State);
    }

    [Fact]
    public void B05_Without_a_tick_size_nothing_can_be_measured()
    {
        var parity = Compare(tick: 0m);
        Assert.Equal(FixedProfileParityState.Unavailable, parity.State);
    }

    [Fact]
    public void C01_Not_requested_and_pending_are_distinguishable()
    {
        Assert.Equal("NOT REQUESTED", FixedProfileParitySnapshot.NotRequested.Describe());
        Assert.Equal("PENDING", FixedProfileParitySnapshot.Pending("LastDay").Describe());
    }
}
