using System.Text.RegularExpressions;
using Xunit;

// Source-shape tests, for the same reason the rest of this folder uses them:
// GcAuctionFlowIndicator derives from an ATAS type that cannot load in the test host. That
// limitation is why integration defects reached live sessions before, so every assertion
// here was verified by mutation rather than trusted:
//
//   delete the EffortResultResearchCollector step from the schedule
//     -> A01, A02 and A06 fail
//   delete the `_effortResultResearchCollector.Rebuild(` call from the handler
//     -> A05 fails
//   gate the handler on EnableEffortResultClassifier
//     -> A04 fails
//   change the handler's gate to always-on
//     -> A03 fails
//   remove the HistoricalScanner step or change its dependencies
//     -> A07 fails
//
// Re-run those mutations if these tests are ever refactored. A source-shape test that stops
// failing is indistinguishable from one that passes.

namespace GC.AuctionFlow.Tests.Unit.Runtime;

/// <summary>
/// WP01A: the Effort/Result research collector must be wired, gated and ordered.
///
/// The bridge is the whole point of the work package, and the trap it walks into is
/// documented in this repository: a store was once "correct and unused", and removing the
/// caller failed no test at all. So this asserts the wiring, not the component.
/// </summary>
public sealed class EffortResultResearchWiringTests
{
    private const string Step = "EffortResultResearchCollector";

    private static IndicatorSchedule.Step Declared(string module) =>
        IndicatorSchedule.Declared().Single(s => s.Module == module);

    /// <summary>The collector is a declared step, not a side call from somewhere else.</summary>
    [Fact]
    public void A01_The_collector_is_a_declared_schedule_step()
    {
        var step = Declared(Step);

        Assert.Equal("ProcessEffortResultResearchCollector", step.OnBar);
        // No cheaper publish variant: collection is a set lookup per closed episode, and a
        // publish-only guard is how Trade Facilitation froze for a whole session.
        Assert.Null(step.OnPublish);
    }

    /// <summary>It reads the efficiency evidence, and says so.</summary>
    [Fact]
    public void A02_The_collector_declares_its_dependency_on_auction_efficiency_evidence()
    {
        Assert.Equal(new[] { "AuctionEfficiencyEvidence" }, Declared(Step).DependsOn.ToArray());
    }

    /// <summary>
    /// Collection runs only when the Research feature is enabled, and the flag is off by
    /// default.
    /// </summary>
    [Fact]
    public void A03_Collection_is_gated_on_the_research_feature_flag()
    {
        var body = HandlerBody();

        Assert.Contains("!EnableHistoricalScanner", body, StringComparison.Ordinal);

        var indicator = IndicatorSchedule.Source();
        Assert.Contains("EnableHistoricalScanner = false", indicator, StringComparison.Ordinal);
        Assert.Contains("EnableEffortResultClassifier = false", indicator, StringComparison.Ordinal);
    }

    /// <summary>
    /// It must **not** require the classifier.
    ///
    /// The corpus is what would eventually let the classifier be calibrated, so requiring the
    /// classifier to be enabled in order to collect the corpus would be circular — and would
    /// mean the module could never leave `NotCalibrated`.
    /// </summary>
    [Fact]
    public void A04_Collection_does_not_require_the_effort_result_classifier()
    {
        Assert.DoesNotContain("EnableEffortResultClassifier", HandlerBody(), StringComparison.Ordinal);
    }

    /// <summary>
    /// The handler actually drives the collector with the efficiency snapshot.
    ///
    /// This is the mutation that matters: a correct collector that nobody calls collects
    /// nothing, silently, for an entire session.
    /// </summary>
    [Fact]
    public void A05_The_handler_drives_the_collector_with_the_efficiency_snapshot()
    {
        var body = HandlerBody();

        Assert.Contains("_efficiencyHost?.Current", body, StringComparison.Ordinal);
        Assert.Contains("_effortResultResearchCollector.Rebuild(efficiency)", body,
            StringComparison.Ordinal);
        Assert.Contains("RecordFault(\"EffortResultResearchCollector\"", body, StringComparison.Ordinal);
    }

    /// <summary>The collector is ordered after the evidence it reads.</summary>
    [Fact]
    public void A06_The_collector_is_ordered_after_auction_efficiency_evidence()
    {
        var steps = IndicatorSchedule.Declared();
        var order = steps.Select((s, i) => (s.Module, i))
            .ToDictionary(x => x.Module, x => x.i, StringComparer.Ordinal);

        Assert.True(order["AuctionEfficiencyEvidence"] < order[Step]);
    }

    /// <summary>
    /// The existing Historical Scanner step is untouched — not reordered, not re-scoped, not
    /// given new dependencies.
    /// </summary>
    [Fact]
    public void A07_The_historical_scanner_step_is_unchanged()
    {
        var scanner = Declared("HistoricalScanner");

        Assert.Equal("ProcessHistoricalScanner", scanner.OnBar);
        Assert.Null(scanner.OnPublish);
        Assert.Equal(
            new[] { "AuctionEpisodes", "StructuralReferences" },
            scanner.DependsOn.OrderBy(d => d, StringComparer.Ordinal).ToArray());

        var steps = IndicatorSchedule.Declared();
        var order = steps.Select((s, i) => (s.Module, i))
            .ToDictionary(x => x.Module, x => x.i, StringComparer.Ordinal);
        Assert.True(order["AuctionEpisodes"] < order["HistoricalScanner"]);
        Assert.True(order["StructuralReferences"] < order["HistoricalScanner"]);
    }

    /// <summary>
    /// Nothing in the collector's path reaches a decision sink, a card or a thesis.
    ///
    /// A02 covers the namespace globally; this covers the one handler this work package
    /// added, where a convenience call would be easiest to add and hardest to notice.
    /// </summary>
    [Fact]
    public void A08_The_handler_touches_no_decision_sink_or_card()
    {
        var body = HandlerBody();

        foreach (var forbidden in new[] { "Thesis", "GpsCard", "_maturityHost", "_facilitationHost" })
            Assert.DoesNotContain(forbidden, body, StringComparison.Ordinal);
    }

    /// <summary>
    /// Both research hosts must receive the **same** store instance.
    ///
    /// The first WP01A build let each host default-construct its own store, so the running
    /// indicator had two queues and two writer threads against the same two files, while the
    /// report claimed one. The class contract was true; the process contract was not.
    /// </summary>
    [Fact]
    public void A09_Both_research_hosts_receive_the_indicator_owned_store()
    {
        var src = IndicatorSchedule.Source();

        Assert.Contains("_researchDatasetStore ??= new EpisodeDatasetStore()", src,
            StringComparison.Ordinal);
        Assert.Contains("new HistoricalScannerHost(policy, ResearchDatasetStore)", src,
            StringComparison.Ordinal);
        Assert.Contains("new EffortResultResearchCollector(policy, ResearchDatasetStore)", src,
            StringComparison.Ordinal);

        // Exactly one construction of a store in the whole indicator, and it is the owned
        // one. A second `new EpisodeDatasetStore()` anywhere here is the defect returning.
        var constructions = Regex.Matches(src, @"new EpisodeDatasetStore\(").Count;
        Assert.Equal(1, constructions);
    }

    private static string HandlerBody()
    {
        var src = IndicatorSchedule.Source();
        var start = src.IndexOf("private void ProcessEffortResultResearchCollector()",
            StringComparison.Ordinal);
        Assert.True(start >= 0, "ProcessEffortResultResearchCollector is not declared");

        // Up to the next method declaration at the same indentation.
        var rest = src[start..];
        var next = Regex.Match(rest[1..], @"\n    private (void|bool|string|Guid|[A-Z])");
        return next.Success ? rest[..next.Index] : rest;
    }
}
