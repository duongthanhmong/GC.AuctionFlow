using System.Linq;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Runtime;

/// <summary>
/// B0-S1 schedule dependency lock.
///
/// The indicator schedule under-declared the data dependencies it actually reads:
/// ExecutedOrderflow reads AuctionEpisodes, ClusterRawFeatures reads ExecutedOrderflow,
/// and AuctionEfficiencyEvidence reads ExecutedOrderflow + ClusterRawFeatures +
/// AuctionEpisodes + AcceptanceReentryEvidence — yet only the last declared a single
/// dependency. This locks the real dependency matrix on the actual indicator schedule
/// (read out of its source by <see cref="IndicatorSchedule"/>), not the generic engine
/// construction test.
///
/// PrimaryProfile is an EXTERNAL pre-schedule input (refreshed by ProcessProfileBar ->
/// Schedule().RunBar()); it must never appear in a ModuleDriveStep.DependsOn.
///
/// This slice declares and locks existing data dependencies. It does NOT activate the
/// KDK Effort/Result adjudication join: AcceptanceReentryResolution stays Acceptance-only.
///
/// Every assertion was verified by mutation (see B0-S1 mutation evidence).
/// </summary>
public sealed class IndicatorScheduleDependencyMatrixTests
{
    private static IReadOnlyList<string> DependsOf(string module) =>
        IndicatorSchedule.Declared().Single(s => s.Module == module).DependsOn;

    [Fact]
    public void A01_Executed_orderflow_depends_on_auction_episodes()
    {
        Assert.Equal(new[] { "AuctionEpisodes" }, DependsOf("ExecutedOrderflow").OrderBy(d => d).ToArray());
    }

    [Fact]
    public void A02_Cluster_raw_features_depends_on_executed_orderflow()
    {
        Assert.Equal(new[] { "ExecutedOrderflow" }, DependsOf("ClusterRawFeatures").OrderBy(d => d).ToArray());
    }

    [Fact]
    public void A03_Auction_efficiency_declares_every_scheduled_input()
    {
        var expected = new[]
        {
            "AcceptanceReentryEvidence",
            "AuctionEpisodes",
            "ClusterRawFeatures",
            "ExecutedOrderflow",
        };
        Assert.Equal(expected, DependsOf("AuctionEfficiencyEvidence").OrderBy(d => d).ToArray());
    }

    [Fact]
    public void A04_Primary_profile_remains_an_external_input()
    {
        foreach (var step in IndicatorSchedule.Declared())
            Assert.DoesNotContain("PrimaryProfile", step.DependsOn);
    }

    [Fact]
    public void A05_B0_does_not_activate_the_resolution_join()
    {
        var deps = DependsOf("AcceptanceReentryResolution");
        Assert.Equal(new[] { "AcceptanceReentryEvidence" }, deps.OrderBy(d => d).ToArray());
        Assert.DoesNotContain("EffortResult", deps);
    }

    [Fact]
    public void A06_The_corrected_real_indicator_chain_is_constructible()
    {
        var steps = IndicatorSchedule.Declared();
        var order = steps.Select((s, i) => (s.Module, i))
            .ToDictionary(x => x.Module, x => x.i, StringComparer.Ordinal);

        foreach (var step in steps)
        {
            foreach (var dependency in step.DependsOn)
            {
                Assert.True(order.ContainsKey(dependency),
                    step.Module + " depends on " + dependency + " which is not a declared step");
                Assert.True(order[dependency] < order[step.Module],
                    step.Module + " depends on " + dependency + " which is not ordered before it");
            }
        }
    }
}
