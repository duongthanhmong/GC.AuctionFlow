using GC.AuctionFlow.OptionFlow;

namespace GC.AuctionFlow.Tests.Unit.OptionFlow;

public sealed class OptionFlowProviderTests
{
    private static GexContext Ctx() => new(
        "GC", "GCQ6", 4079.75, "NEGATIVE_GAMMA", 4086.46, null, "LIVE",
        1_785_182_400, new List<GexLevel>(), null);

    [Fact]
    public void First_refresh_reads_then_throttles_within_interval()
    {
        int reads = 0;
        OptionFlowReadOutcome Read(string root, string product, DateTimeOffset now, TimeSpan maxAge)
        {
            reads++;
            return new OptionFlowReadOutcome(Ctx(), "LIVE");
        }

        var p = new OptionFlowProvider("root", TimeSpan.FromMinutes(30), TimeSpan.FromSeconds(60), Read);
        var t0 = DateTimeOffset.FromUnixTimeSeconds(1_785_182_400);

        Assert.True(p.Refresh("GC", t0));                 // reads
        Assert.False(p.Refresh("GC", t0.AddSeconds(30))); // throttled
        Assert.Equal(1, reads);
        Assert.NotNull(p.Current);
    }

    [Fact]
    public void Refresh_after_interval_reads_again()
    {
        int reads = 0;
        OptionFlowReadOutcome Read(string root, string product, DateTimeOffset now, TimeSpan maxAge)
        {
            reads++;
            return new OptionFlowReadOutcome(Ctx(), "LIVE");
        }

        var p = new OptionFlowProvider("root", TimeSpan.FromMinutes(30), TimeSpan.FromSeconds(60), Read);
        var t0 = DateTimeOffset.FromUnixTimeSeconds(1_785_182_400);

        Assert.True(p.Refresh("GC", t0));
        Assert.True(p.Refresh("GC", t0.AddSeconds(61)));  // interval elapsed
        Assert.Equal(2, reads);
    }

    [Fact]
    public void Null_outcome_clears_current_and_records_diagnostic()
    {
        OptionFlowReadOutcome Read(string root, string product, DateTimeOffset now, TimeSpan maxAge) =>
            new(null, "OptionFlow stale: 3600s old");

        var p = new OptionFlowProvider("root", TimeSpan.FromMinutes(30), TimeSpan.FromSeconds(1), Read);
        p.Refresh("GC", DateTimeOffset.FromUnixTimeSeconds(1_785_182_400));

        Assert.Null(p.Current);
        Assert.Contains("stale", p.Diagnostic);
    }
}
