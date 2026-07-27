using System.Text.RegularExpressions;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Runtime;

/// <summary>
/// Every module needs a per-bar update path.
///
/// Trade Facilitation shipped without one: it was reachable only through its
/// Ensure*InitializedForPublish guard, and that guard early-returns once a snapshot
/// exists. So it published AwaitingEfficiency on the first bar and froze there for the
/// rest of the session, while the card kept showing that stale state as if it were live.
///
/// This asserts the shape that prevents it, rather than re-testing one module.
/// </summary>
public sealed class IndicatorProcessChainTests
{
    private static string IndicatorSource()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return File.ReadAllText(Path.Combine(
            dir!.FullName, "src", "GC.AuctionFlow", "Atas", "GcAuctionFlowIndicator.cs"));
    }

    /// <summary>
    /// An Ensure*InitializedForPublish guard is a first-publish safety net. Any module
    /// that has one must ALSO be driven per bar, or the guard becomes a freeze.
    ///
    /// The guard names its own process method, so that is what gets checked rather than
    /// a name-similarity guess — several modules legitimately differ (EnsureClusterRaw
    /// drives ProcessClusterRawFeatures).
    /// </summary>
    [Fact]
    public void A01_Every_ensured_module_is_also_processed_per_bar()
    {
        var src = IndicatorSource();

        var chain = Regex.Match(src, @"if \(bar >= CurrentBar\)\s*\{(.*?)PublishRuntimeSnapshot\(\);",
            RegexOptions.Singleline);
        Assert.True(chain.Success, "could not locate the per-bar process chain");
        var body = chain.Groups[1].Value;

        var guards = Regex.Matches(src,
            @"private void Ensure(\w+?)InitializedForPublish\(\)\s*\{(.*?)
    \}",
            RegexOptions.Singleline);
        Assert.NotEmpty(guards);

        var missing = new List<string>();
        foreach (Match g in guards)
        {
            var name = g.Groups[1].Value;
            var call = Regex.Match(g.Groups[2].Value, @"(Process\w+\(\));");
            Assert.True(call.Success, "Ensure" + name + " does not call any Process method");

            if (!body.Contains(call.Groups[1].Value + ";", StringComparison.Ordinal))
                missing.Add(name + " -> " + call.Groups[1].Value);
        }

        Assert.True(missing.Count == 0,
            "these modules have an Ensure guard but no per-bar Process call, so they freeze "
            + "after the first snapshot: " + string.Join(", ", missing));
    }

    /// <summary>
    /// Regression for the specific module that shipped broken.
    /// </summary>
    [Fact]
    public void A02_Trade_facilitation_is_processed_per_bar()
    {
        var chain = Regex.Match(IndicatorSource(),
            @"if \(bar >= CurrentBar\)\s*\{(.*?)PublishRuntimeSnapshot\(\);",
            RegexOptions.Singleline);
        Assert.True(chain.Success);
        Assert.Contains("ProcessTradeFacilitation();", chain.Groups[1].Value, StringComparison.Ordinal);
    }

    /// <summary>
    /// A module that is published must be processed. Publishing a host that nothing
    /// drives yields a permanently null or stale row on the card.
    /// </summary>
    [Fact]
    public void A03_Every_published_host_is_processed_per_bar()
    {
        var src = IndicatorSource();
        var chain = Regex.Match(src, @"if \(bar >= CurrentBar\)\s*\{(.*?)PublishRuntimeSnapshot\(\);",
            RegexOptions.Singleline);
        Assert.True(chain.Success);
        var body = chain.Groups[1].Value;

        foreach (var process in Regex.Matches(src, @"private void Process(\w+)\(\)")
                     .Select(m => m.Groups[1].Value)
                     // ProfileBar takes a bar index and is called separately.
                     .Where(n => n != "ProfileBar"))
        {
            Assert.True(body.Contains("Process" + process + "();", StringComparison.Ordinal),
                "Process" + process + " exists but is never called per bar");
        }
    }
}
