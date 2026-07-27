using System.Text.RegularExpressions;
using Xunit;

// These tests inspect indicator source text rather than running the indicator, because
// GcAuctionFlowIndicator derives from an ATAS type that cannot load in the test host.
// That limitation is exactly why three integration defects reached a live session, so
// each assertion here was verified by mutation rather than trusted:
//
//   delete the TradeFacilitation step from the schedule
//     -> ChainTests A02, A03 and PublishPathCoverage A03 fail
//   delete the Plar step from the schedule
//     -> ChainTests A03 and PublishPathCoverage A02, A03 fail
//   give TradeFacilitation a publish guard as its publish drive (the original defect)
//     -> ChainTests A01, A02 fail
//   declare SignalMaturity before the theses it reads
//     -> ChainTests A05 fails
//   add one direct ProcessPlar() call back into the publisher
//     -> ChainTests A04 fails
//
// The chain itself now lives in ModuleDriveSchedule, which is a plain type with real
// behavioural tests in ModuleDriveScheduleTests. What is left here is the part that
// still cannot be instantiated: the indicator's own declaration of that chain.
//
// Re-run those mutations if these tests are ever refactored. A source-shape test that
// stops failing is indistinguishable from one that passes, and the earlier version of
// PublishPathCoverageTests.A03 was exactly that: it accepted any host as driven because
// the publish body always contains the substring "Ensure".

namespace GC.AuctionFlow.Tests.Unit.Runtime;

/// <summary>
/// The chain the indicator declares, read out of its source.
///
/// The chain used to live in two hand-maintained call lists, one per code path, and they
/// had already drifted apart. It is now declared once as ModuleDriveStep entries, so this
/// parses that single declaration rather than cross-checking two lists that agree by
/// discipline alone.
/// </summary>
internal static class IndicatorSchedule
{
    internal sealed record Step(
        string Module, string OnBar, string? OnPublish, IReadOnlyList<string> DependsOn);

    internal static string Source()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src")))
            dir = dir.Parent;
        Assert.NotNull(dir);

        var path = Path.Combine(dir!.FullName, "src", "GC.AuctionFlow", "Atas", "GcAuctionFlowIndicator.cs");
        var text = File.ReadAllText(path);

        // Turns a read racing the build into a clear message rather than a confusing
        // assertion failure downstream. See the notes on the unreproduced post-rebuild
        // flake in IMPLEMENTATION_STATUS.
        Assert.False(string.IsNullOrWhiteSpace(text), "indicator source read as empty: " + path);
        Assert.Contains("class GcAuctionFlowIndicator", text, StringComparison.Ordinal);
        return text;
    }

    internal static IReadOnlyList<Step> Declared()
    {
        var steps = new List<Step>();
        foreach (Match m in Regex.Matches(Source(), @"new ModuleDriveStep\(([^)]*)\)"))
        {
            var args = m.Groups[1].Value.Split(',').Select(a => a.Trim()).ToArray();
            Assert.True(args.Length >= 2, "malformed schedule step: " + m.Value);

            var publish = args.Length > 2 && args[2] != "null" ? args[2] : null;
            steps.Add(new Step(
                args[0].Trim('"'),
                args[1],
                publish,
                args.Skip(3).Select(a => a.Trim('"')).ToArray()));
        }

        Assert.NotEmpty(steps);
        return steps;
    }
}


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
    /// <summary>
    /// An Ensure*InitializedForPublish method is a cheaper publish-path drive for a
    /// module that would otherwise do real work on every trade callback. It is only ever
    /// legitimate as a step's publish delegate — used as a module's sole drive it
    /// becomes a freeze, which is exactly what happened to Trade Facilitation.
    /// </summary>
    [Fact]
    public void A01_Every_publish_guard_belongs_to_a_step_that_also_has_a_bar_drive()
    {
        var src = IndicatorSchedule.Source();
        var steps = IndicatorSchedule.Declared();

        var publishDrives = steps
            .Where(s => s.OnPublish is not null)
            .ToDictionary(s => s.OnPublish!, s => s, StringComparer.Ordinal);

        var guards = Regex.Matches(src, @"private void (Ensure(\w+?)InitializedForPublish)\(\)")
            .Select(m => m.Groups[1].Value)
            .ToArray();
        Assert.NotEmpty(guards);

        var orphaned = guards.Where(g => !publishDrives.ContainsKey(g)).ToArray();
        Assert.True(orphaned.Length == 0,
            "these publish guards exist but no schedule step uses them, so they are dead "
            + "code and their module falls back to the full bar drive on every trade "
            + "callback: " + string.Join(", ", orphaned));

        // Every guard-backed step still needs its own bar drive, or the guard is the
        // module's only path and the freeze is back.
        foreach (var guard in guards)
            Assert.StartsWith("Process", publishDrives[guard].OnBar, StringComparison.Ordinal);
    }

    /// <summary>
    /// Regression for the specific module that shipped broken. Its guard was removed
    /// rather than demoted, so it must be driven identically on both passes.
    /// </summary>
    [Fact]
    public void A02_Trade_facilitation_is_driven_the_same_way_on_both_passes()
    {
        var step = Assert.Single(
            IndicatorSchedule.Declared(), s => s.Module == "TradeFacilitation");

        Assert.Equal("ProcessTradeFacilitation", step.OnBar);
        Assert.Null(step.OnPublish);
        Assert.Contains("AuctionEfficiencyEvidence", step.DependsOn);
    }

    /// <summary>
    /// Every module has a step. A Process method outside the schedule is a module
    /// nothing drives, which renders as a permanently null or stale row.
    /// </summary>
    [Fact]
    public void A03_Every_process_method_is_some_steps_bar_drive()
    {
        var src = IndicatorSchedule.Source();
        var barDrives = IndicatorSchedule.Declared().Select(s => s.OnBar).ToArray();

        foreach (var method in Regex.Matches(src, @"private void (Process\w+)\(\)")
                     .Select(m => m.Groups[1].Value)
                     // ProfileBar takes a bar index and is driven separately.
                     .Where(n => n != "ProcessProfileBar"))
        {
            Assert.True(barDrives.Contains(method, StringComparer.Ordinal),
                method + " exists but no schedule step drives it");
        }

        Assert.Equal(barDrives.Length, barDrives.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// The schedule must be the only chain.
    ///
    /// A second call list is how the two paths drifted apart in the first place, and the
    /// drift was invisible: both lists looked complete on their own. Keeping the handlers
    /// free of direct module calls is what makes that class of bug unwritable rather
    /// than merely caught.
    /// </summary>
    [Fact]
    public void A04_Neither_handler_keeps_a_call_list_of_its_own()
    {
        var src = IndicatorSchedule.Source();

        foreach (var (handler, pattern) in new[]
                 {
                     ("OnCalculate", @"protected override void OnCalculate\([^)]*\)\s*\{(.*?)\n    \}"),
                     ("PublishRuntimeSnapshot",
                      @"private void PublishRuntimeSnapshot\(\)\s*\{(.*?)var snapshot = runtime\.Publish"),
                 })
        {
            var m = Regex.Match(src, pattern, RegexOptions.Singleline);
            Assert.True(m.Success, "could not locate " + handler);

            var stray = Regex.Matches(m.Groups[1].Value, @"\b(?:Process|Ensure)\w+\(\);")
                .Select(c => c.Value)
                // ProfileBar is not a schedule module; the runtime, probe and profile-host
                // starters are lifecycle rather than analysis.
                .Where(c => !c.StartsWith("ProcessProfileBar", StringComparison.Ordinal)
                            && !c.StartsWith("EnsureRuntimeStarted", StringComparison.Ordinal)
                            && !c.StartsWith("EnsureProbesStarted", StringComparison.Ordinal)
                            && !c.StartsWith("EnsureProfileHost", StringComparison.Ordinal))
                .ToArray();

            Assert.True(stray.Length == 0,
                handler + " drives modules directly instead of through the schedule, which "
                + "reintroduces a second call order that can drift: " + string.Join(", ", stray));
        }

        Assert.Contains("Schedule()?.RunBar();", src, StringComparison.Ordinal);
        Assert.Contains("Schedule()?.RunPublish();", src, StringComparison.Ordinal);
    }

    /// <summary>
    /// The declared dependencies must be ordered before their dependents, or the
    /// ordering guarantee is decorative.
    /// </summary>
    [Fact]
    public void A05_Declared_dependencies_are_ordered_before_their_dependents()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var step in IndicatorSchedule.Declared())
        {
            foreach (var dependency in step.DependsOn)
                Assert.True(seen.Contains(dependency),
                    step.Module + " reads " + dependency + " but is declared before it");
            seen.Add(step.Module);
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
        Assert.NotNull(dir);

        var path = Path.Combine(dir!.FullName, "src", "GC.AuctionFlow", "Atas", "GcAuctionFlowIndicator.cs");
        var text = File.ReadAllText(path);

        // A test in this file failed once right after a clean rebuild and has not been
        // reproduced in three attempts since. The leading hypothesis is a read racing the
        // build. This does not fix that, but it turns a partial read into a clear message
        // instead of a confusing assertion failure somewhere downstream.
        Assert.False(string.IsNullOrWhiteSpace(text), "indicator source read as empty: " + path);
        Assert.Contains("class GcAuctionFlowIndicator", text, StringComparison.Ordinal);
        return text;
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
            // Neither of these is a schedule module. ModuleSchedule is the chain itself
            // failing to validate, reported from the builder; DepthRecording is a
            // callback-path fault reported from TryRecordDepth. Both still belong in the
            // fault ledger — a recorder that silently stops writing depth is exactly the
            // kind of failure this ledger exists to surface — but neither has a Process
            // method for the name to match.
            if (call.Groups[1].Value is "ModuleSchedule" or "DepthRecording" or "RecorderStartup" or "MboRecording")
                continue;

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
        Assert.NotNull(dir);

        var path = Path.Combine(dir!.FullName, "src", "GC.AuctionFlow", "Atas", "GcAuctionFlowIndicator.cs");
        var text = File.ReadAllText(path);

        // A test in this file failed once right after a clean rebuild and has not been
        // reproduced in three attempts since. The leading hypothesis is a read racing the
        // build. This does not fix that, but it turns a partial read into a clear message
        // instead of a confusing assertion failure somewhere downstream.
        Assert.False(string.IsNullOrWhiteSpace(text), "indicator source read as empty: " + path);
        Assert.Contains("class GcAuctionFlowIndicator", text, StringComparison.Ordinal);
        return text;
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
        Assert.NotNull(dir);

        var path = Path.Combine(dir!.FullName, "src", "GC.AuctionFlow", "Atas", "GcAuctionFlowIndicator.cs");
        var text = File.ReadAllText(path);

        // A test in this file failed once right after a clean rebuild and has not been
        // reproduced in three attempts since. The leading hypothesis is a read racing the
        // build. This does not fix that, but it turns a partial read into a clear message
        // instead of a confusing assertion failure somewhere downstream.
        Assert.False(string.IsNullOrWhiteSpace(text), "indicator source read as empty: " + path);
        Assert.Contains("class GcAuctionFlowIndicator", text, StringComparison.Ordinal);
        return text;
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

    /// <summary>
    /// Every module the card reads is driven on the publish path.
    ///
    /// This is now structural: a step cannot exist without a publish delegate, and the
    /// publisher runs the whole schedule. What is left worth asserting is that these four
    /// specific modules — the ones that read NOT AVAILABLE for a whole live session — are
    /// in the schedule at all.
    /// </summary>
    [Fact]
    public void A02_Every_optional_module_is_in_the_schedule()
    {
        var modules = IndicatorSchedule.Declared().Select(s => s.Module).ToArray();

        foreach (var module in new[] { "Plar", "PriceMemory", "Imbalance", "ExecutionReadiness" })
            Assert.True(modules.Contains(module, StringComparer.Ordinal),
                module + " is published but has no schedule step, so it is null on every "
                + "publish that does not come from OnCalculate");
    }

    /// <summary>
    /// Generalises it: every host read into the Publish arguments must be assigned by a
    /// method the schedule actually drives.
    ///
    /// The first version of this test was vacuous. It accepted any host as "driven" if
    /// the publish body contained the substring "Ensure" anywhere, which it always does,
    /// so it passed for hosts that were never populated at all. A test that catches
    /// nothing is worse than no test: it manufactures the confidence that let three
    /// integration defects reach a live session.
    /// </summary>
    [Fact]
    public void A03_Every_published_host_is_assigned_by_something_the_schedule_drives()
    {
        var src = IndicatorSchedule.Source();
        var body = PublishBody(src);

        // method name -> its start, so assignments can be attributed to an owner
        var methods = System.Text.RegularExpressions.Regex
            .Matches(src, @"private void (\w+)\([^)]*\)\s*\{")
            .Select(m => (Name: m.Groups[1].Value, Start: m.Index))
            .OrderBy(m => m.Start)
            .ToArray();

        string OwnerOf(int position) =>
            methods.LastOrDefault(m => m.Start < position).Name ?? "";

        // everything the schedule drives, plus one level of indirection through the
        // publish guards, which delegate to their module's Process method
        var driven = IndicatorSchedule.Declared()
            .SelectMany(s => new[] { s.OnBar, s.OnPublish })
            .Where(n => n is not null)
            .Select(n => n!)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var name in driven.ToArray())
        {
            var guard = System.Text.RegularExpressions.Regex.Match(src,
                @"private void " + name + @"\(\)\s*\{(.*?)\n    \}",
                System.Text.RegularExpressions.RegexOptions.Singleline);
            if (!guard.Success) continue;
            foreach (System.Text.RegularExpressions.Match call in
                     System.Text.RegularExpressions.Regex.Matches(guard.Groups[1].Value, @"(\w+)\(\);"))
                driven.Add(call.Groups[1].Value);
        }

        var unread = new List<string>();
        foreach (System.Text.RegularExpressions.Match read in
                 System.Text.RegularExpressions.Regex.Matches(body, @"_(\w+Host)\?\.Current"))
        {
            var field = "_" + read.Groups[1].Value;

            // The primary profile is the one host that genuinely cannot be schedule-driven:
            // it is built from candles by bar index, so ProcessProfileBar owns it and a
            // publish between bars has nothing new to give it.
            if (field == "_profileHost")
                continue;

            var assigners = System.Text.RegularExpressions.Regex
                .Matches(src, System.Text.RegularExpressions.Regex.Escape(field) + @"\s*(\?\?=|=)\s*new ")
                .Select(a => OwnerOf(a.Index))
                .Where(n => n.Length > 0)
                .ToHashSet(StringComparer.Ordinal);

            if (assigners.Count == 0) continue;
            if (!assigners.Any(driven.Contains))
                unread.Add(field + " (assigned by " + string.Join("/", assigners) + ")");
        }

        Assert.True(unread.Count == 0,
            "these hosts are read into the Publish arguments but nothing the schedule "
            + "drives assigns them, so they are null on every publish that does not come "
            + "from OnCalculate: " + string.Join(", ", unread));
    }
}

/// <summary>
/// No publish guard may skip a rebuild merely because a snapshot already exists.
///
/// Three did. Trade Facilitation froze at AwaitingEfficiency while efficiency reported
/// four scopes, and Signal Maturity froze at AwaitingThesis while FAR and AAC each
/// reported three — the second one looked plausible enough that it went unnoticed
/// through several rounds of debugging.
///
/// The guards were also pointless: every host already gates its own rebuild on an input
/// fingerprint, so a redundant call costs nothing. Trade Facilitation's guard even
/// computed a fingerprint and then ignored it.
/// </summary>
public sealed class PublishGuardFreezeTests
{
    private static string IndicatorSource()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src")))
            dir = dir.Parent;
        Assert.NotNull(dir);

        var path = Path.Combine(dir!.FullName, "src", "GC.AuctionFlow", "Atas", "GcAuctionFlowIndicator.cs");
        var text = File.ReadAllText(path);

        // A test in this file failed once right after a clean rebuild and has not been
        // reproduced in three attempts since. The leading hypothesis is a read racing the
        // build. This does not fix that, but it turns a partial read into a clear message
        // instead of a confusing assertion failure somewhere downstream.
        Assert.False(string.IsNullOrWhiteSpace(text), "indicator source read as empty: " + path);
        Assert.Contains("class GcAuctionFlowIndicator", text, StringComparison.Ordinal);
        return text;
    }

    [Fact]
    public void A01_No_guard_short_circuits_on_snapshot_existence_alone()
    {
        var src = IndicatorSource();
        var frozen = new List<string>();

        foreach (Match g in Regex.Matches(src,
                     @"private void Ensure(\w+?)InitializedForPublish\(\)\s*\{(.*?)\n    \}",
                     RegexOptions.Singleline))
        {
            var body = g.Groups[2].Value;
            var skipsOnExistence = body.Contains("Current is not null", StringComparison.Ordinal);
            var comparesInput = body.Contains("Fingerprint", StringComparison.Ordinal)
                                && body.Contains("Equals(fp", StringComparison.Ordinal);

            if (skipsOnExistence && !comparesInput)
                frozen.Add(g.Groups[1].Value);
        }

        Assert.True(frozen.Count == 0,
            "these guards skip the rebuild once a snapshot exists, so the module "
            + "freezes at its first state: " + string.Join(", ", frozen));
    }
}
