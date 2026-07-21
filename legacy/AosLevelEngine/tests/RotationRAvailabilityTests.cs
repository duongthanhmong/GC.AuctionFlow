using Aos.LevelEngine.Profiles;
using Aos.LevelEngine.Session;
using Xunit;

namespace Aos.LevelEngine.Tests;

public class RotationRAvailabilityTests
{
    private static InstrumentProfile Profile() => InstrumentProfileLoader.LoadEmbeddedEs();

    private static TradingDateResolver Resolver() => TradingDateResolver.FromProfile(Profile());

    [Fact]
    public void Before_0915_ET_is_NOT_YET_AVAILABLE_fail_closed_interaction()
    {
        var profile = Profile();
        var resolver = Resolver();
        var td = new DateOnly(2026, 7, 20);
        var asOf = EtUtc.Of(2026, 7, 20, 9, 14);
        var trs = Enumerable.Repeat(3m, 50).ToList();

        var r = RotationRAvailability.Evaluate(profile, resolver, td, asOf, trs, "M1");
        Assert.Equal(RotationRAvailabilityStatus.NOT_YET_AVAILABLE, r.Status);
        Assert.False(r.AllowsInteractionEngine);
        Assert.Null(r.RotationRTicks);
        Assert.Throws<InvalidOperationException>(() =>
            RotationRAvailability.EnsureAvailableForInteraction(r));
    }

    [Fact]
    public void After_0915_without_M1_TR_is_INSUFFICIENT_no_default_R()
    {
        var profile = Profile();
        var resolver = Resolver();
        var td = new DateOnly(2026, 7, 20);
        var asOf = EtUtc.Of(2026, 7, 20, 9, 15);

        var r = RotationRAvailability.Evaluate(
            profile, resolver, td, asOf, Array.Empty<decimal>(), "M1");
        Assert.Equal(RotationRAvailabilityStatus.INSUFFICIENT, r.Status);
        Assert.False(r.AllowsInteractionEngine);
        Assert.Null(r.RotationRTicks);
        Assert.Contains("no default", r.Reason!, StringComparison.OrdinalIgnoreCase);
        Assert.Throws<InvalidOperationException>(() =>
            RotationRAvailability.EnsureAvailableForInteraction(r));
    }

    [Fact]
    public void After_0915_with_M1_TR_is_AVAILABLE_frozen()
    {
        var profile = Profile();
        var resolver = Resolver();
        var td = new DateOnly(2026, 7, 20);
        var asOf = EtUtc.Of(2026, 7, 20, 9, 15);
        var trs = Enumerable.Repeat(3m, 11).ToList();

        var r = RotationRAvailability.Evaluate(profile, resolver, td, asOf, trs, "M1");
        Assert.Equal(RotationRAvailabilityStatus.AVAILABLE, r.Status);
        Assert.True(r.AllowsInteractionEngine);
        Assert.NotNull(r.FrozenState);
        Assert.True(r.FrozenState!.IsFrozen);
        Assert.Equal(r.FrozenState.RotationRTicks, r.RotationRTicks);
        RotationRAvailability.EnsureAvailableForInteraction(r);
    }
}
