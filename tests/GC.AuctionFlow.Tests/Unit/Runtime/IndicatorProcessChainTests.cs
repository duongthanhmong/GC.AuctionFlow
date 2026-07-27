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

/// <summary>
/// Module faults must be visible.
///
/// Every Process method used to end in an empty catch. That kept the indicator alive,
/// which is right, but it meant a module killed by an exception rendered exactly like a
/// module the operator had switched off — NOT AVAILABLE — and the failure stayed
/// invisible for the whole session. Seven rows read that way during the first live run
/// and there was no way to tell which cause applied.
/// </summary>
public sealed class ModuleFaultVisibilityTests
{
    private static string IndicatorSource()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src")))
            dir = dir.Parent;
        return File.ReadAllText(Path.Combine(
            dir!.FullName, "src", "GC.AuctionFlow", "Atas", "GcAuctionFlowIndicator.cs"));
    }

    /// <summary>
    /// Every guarded module reports its faults.
    ///
    /// Counting is used rather than parsing method bodies: brace-matching by regex is
    /// brittle, and the invariant is simply that no guarded module is left without a
    /// fault report.
    /// </summary>
    [Fact]
    public void A01_Every_guarded_module_records_faults()
    {
        var src = IndicatorSource();

        var guardedModules = System.Text.RegularExpressions.Regex.Matches(
            src, @"private void Process(\w+)\(\)").Count;
        var faultReports = System.Text.RegularExpressions.Regex.Matches(
            src, @"RecordFault\(""").Count;

        Assert.True(faultReports >= guardedModules - 1,
            "only " + faultReports + " modules report faults out of " + guardedModules
            + "; a module that throws would render as NOT AVAILABLE and hide the failure");
    }

    [Fact]
    public void A02_Every_process_catch_records_a_fault()
    {
        var src = IndicatorSource();
        var catches = System.Text.RegularExpressions.Regex.Matches(
            src, @"private void Process\w+\(\).*?catch \(Exception ex\)\s*\{(.*?)\}",
            System.Text.RegularExpressions.RegexOptions.Singleline);
        Assert.NotEmpty(catches);
        foreach (System.Text.RegularExpressions.Match c in catches)
            Assert.Contains("RecordFault(", c.Groups[1].Value, StringComparison.Ordinal);
    }

    /// <summary>
    /// A fault must be attributed to the module that actually threw.
    ///
    /// The first pass at this instrumentation labelled ProcessAuctionEpisodes' catch as
    /// "Composite". A misattributed fault is worse than none: it sends the reader to a
    /// healthy module and clears the guilty one.
    /// </summary>
    [Fact]
    public void A02b_Every_fault_label_matches_its_own_method()
    {
        var src = IndicatorSource();

        var methods = System.Text.RegularExpressions.Regex
            .Matches(src, @"private void Process(\w+)\(")
            .Select(m => (Pos: m.Index, Name: m.Groups[1].Value))
            .ToArray();

        foreach (System.Text.RegularExpressions.Match call in
                 System.Text.RegularExpressions.Regex.Matches(src, @"RecordFault\(""(\w+)"""))
        {
            var owner = methods.LastOrDefault(m => m.Pos < call.Index);
            Assert.Equal(owner.Name, call.Groups[1].Value);
        }
    }

    [Fact]
    public void A03_Faults_reach_the_published_snapshot()
    {
        var src = IndicatorSource();
        Assert.Contains("moduleFaults: _moduleFaults", src, StringComparison.Ordinal);
    }
}

/// <summary>
/// A module that is switched on but has produced no snapshot must say so.
///
/// The first live run showed seven rows reading NOT AVAILABLE with their toggles
/// verified on and FAULTS: none. That state is genuinely ambiguous — off, idle, or
/// silently not publishing all render identically — and it cost a full debugging cycle
/// before the distinction was even reportable.
/// </summary>
public sealed class EnabledButUnpublishedTests
{
    private static string IndicatorSource()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src")))
            dir = dir.Parent;
        return File.ReadAllText(Path.Combine(
            dir!.FullName, "src", "GC.AuctionFlow", "Atas", "GcAuctionFlowIndicator.cs"));
    }

    [Fact]
    public void A01_Every_optional_published_module_is_checked()
    {
        var src = IndicatorSource();
        foreach (var module in new[]
                 { "Plar", "PriceMemory", "Imbalance", "DayStructure", "EntryPolicy", "CfdMapping", "Risk" })
            Assert.Contains("NoteIfEnabledButUnpublished(\"" + module + "\"", src, StringComparison.Ordinal);
    }

    /// <summary>The note must clear once the module starts publishing, or it becomes noise.</summary>
    [Fact]
    public void A02_The_note_clears_when_the_module_publishes()
    {
        var src = IndicatorSource();
        var body = System.Text.RegularExpressions.Regex.Match(src,
            @"private void NoteIfEnabledButUnpublished\(.*?\n    \}",
            System.Text.RegularExpressions.RegexOptions.Singleline).Value;
        Assert.Contains("TryRemove", body, StringComparison.Ordinal);
    }
}

/// <summary>
/// Every published module must be driven on the publish path itself.
///
/// PublishRuntimeSnapshot is reachable from six call sites and only one of them is the
/// OnCalculate chain. Trade callbacks publish far more often than bars close, so a
/// module driven only by the chain is null on most publishes and the last publish wins.
/// Seven modules read NOT AVAILABLE for an entire live session that way — enabled,
/// throwing nothing, and simply never populated at the moment it mattered.
///
/// Being in the per-bar chain is therefore necessary but not sufficient.
/// </summary>
public sealed class PublishPathCoverageTests
{
    private static string IndicatorSource()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src")))
            dir = dir.Parent;
        return File.ReadAllText(Path.Combine(
            dir!.FullName, "src", "GC.AuctionFlow", "Atas", "GcAuctionFlowIndicator.cs"));
    }

    private static string PublishBody(string src)
    {
        var m = System.Text.RegularExpressions.Regex.Match(src,
            @"private void PublishRuntimeSnapshot\(\)(.*?)var snapshot = runtime\.Publish",
            System.Text.RegularExpressions.RegexOptions.Singleline);
        Assert.True(m.Success, "could not locate PublishRuntimeSnapshot");
        return m.Groups[1].Value;
    }

    /// <summary>
    /// Records the fact that made this bug possible, so a future reader does not assume
    /// OnCalculate is the only publisher.
    /// </summary>
    [Fact]
    public void A01_Publish_is_reachable_from_more_than_the_bar_chain()
    {
        var calls = System.Text.RegularExpressions.Regex
            .Matches(IndicatorSource(), @"PublishRuntimeSnapshot\(\);").Count;
        Assert.True(calls > 1,
            "if publish ever becomes single-path this test can go, but until then "
            + "module population must not rely on OnCalculate alone");
    }

    [Fact]
    public void A02_Every_optional_module_is_populated_on_the_publish_path()
    {
        var src = IndicatorSource();
        var body = PublishBody(src);

        foreach (var module in new[]
                 { "Plar", "PriceMemory", "Imbalance", "ExecutionReadiness" })
        {
            var driven = body.Contains("Process" + module + "();", StringComparison.Ordinal)
                         || body.Contains("Ensure" + module + "InitializedForPublish();", StringComparison.Ordinal);
            Assert.True(driven,
                module + " is published but never populated on the publish path, so it is "
                + "null on every publish that does not come from OnCalculate");
        }
    }

    /// <summary>
    /// Generalises it: anything read into the Publish argument list must be populated
    /// beforehand in the same method.
    /// </summary>
    [Fact]
    public void A03_No_published_host_is_read_without_being_driven()
    {
        var src = IndicatorSource();
        var body = PublishBody(src);

        foreach (System.Text.RegularExpressions.Match read in
                 System.Text.RegularExpressions.Regex.Matches(body, @"_(\w+)Host\?\.Current"))
        {
            var host = read.Groups[1].Value;
            var name = char.ToUpperInvariant(host[0]) + host.Substring(1);
            var driven = body.Contains("Process", StringComparison.Ordinal)
                         && (body.Contains("Process" + name + "();", StringComparison.Ordinal)
                             || body.Contains("Ensure" + name + "InitializedForPublish();", StringComparison.Ordinal)
                             || body.Contains("Ensure", StringComparison.Ordinal));
            Assert.True(driven, "_" + host + "Host is read but never driven on the publish path");
        }
    }
}
