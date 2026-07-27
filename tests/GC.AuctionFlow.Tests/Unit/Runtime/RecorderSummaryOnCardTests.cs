using GC.AuctionFlow.Core;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Runtime;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Runtime;

/// <summary>
/// The recorder summary the runtime builds must reach the card.
///
/// It did not. The card re-derived the line from `RecorderState` alone, so everything the
/// runtime attached to `RecorderDiagnosticSummary` — the depth frame count among it — was
/// discarded at the last step, and the card kept showing a bare RECORDING while the answer
/// sat one layer below. That cost a live debugging round trip: three different states
/// (depth off, depth on and idle, depth on and writing) all rendered identically.
/// </summary>
public sealed class RecorderSummaryOnCardTests
{
    private static GcaeRuntimeSnapshot Publish(
        bool depthEnabled, long depthFrames, bool recorderOn = true) =>
        new GcaeRuntimeEngine().Publish(
            null, "GCQ6", DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            tradeObserved: true,
            lastTradeCallbackUtc: null,
            rawRecorderMasterEnabled: recorderOn,
            tradeRecordingEnabled: recorderOn,
            recorderAccepting: recorderOn,
            recorderFaulted: false,
            recorderSessionPresent: recorderOn,
            indicatorDisposed: false,
            depthFramesRecorded: depthFrames,
            depthRecordingEnabled: depthEnabled);

    private static string RecorderLine(GcaeRuntimeSnapshot snapshot) =>
        AuctionGpsCardMapper.FromSnapshot(snapshot, true)
            .AllLines(includeDiagnostics: true)
            .Single(l => l.StartsWith("RECORDER:", StringComparison.Ordinal));

    /// <summary>
    /// Three states, and the operator needs a different action for each: flip a switch,
    /// report a bug, or nothing.
    /// </summary>
    [Fact]
    public void A01_The_three_depth_states_render_differently()
    {
        var off = RecorderLine(Publish(depthEnabled: false, depthFrames: 0));
        var idle = RecorderLine(Publish(depthEnabled: true, depthFrames: 0));
        var writing = RecorderLine(Publish(depthEnabled: true, depthFrames: 4321));

        Assert.Equal(3, new[] { off, idle, writing }.Distinct(StringComparer.Ordinal).Count());
        Assert.DoesNotContain("DEPTH", off, StringComparison.Ordinal);
        Assert.Contains("DEPTH ON, 0 FRAMES", idle, StringComparison.Ordinal);
        Assert.Contains("+4321 DEPTH", writing, StringComparison.Ordinal);
    }

    /// <summary>The regression itself: the card must not rebuild this line from the enum.</summary>
    [Fact]
    public void A02_The_card_uses_the_runtime_summary_not_the_raw_state()
    {
        var snapshot = Publish(depthEnabled: true, depthFrames: 99);

        Assert.Contains("DEPTH", snapshot.RecorderDiagnosticSummary, StringComparison.Ordinal);
        Assert.Contains(
            snapshot.RecorderDiagnosticSummary, RecorderLine(snapshot), StringComparison.Ordinal);
    }

    /// <summary>
    /// Every EnsureStarted call must declare the depth stream.
    ///
    /// There are two call sites and only one was patched at first. The unpatched one runs
    /// earlier and is the one that actually creates the pending start, so the manifest
    /// declared ["Trade"] while Dom frames were being written into the same session — a
    /// recording that misdescribes itself, in the one file whose job is to describe it.
    ///
    /// A third call site would reintroduce that silently, so this counts them.
    /// </summary>
    [Fact]
    public void A04_Every_recorder_start_declares_the_depth_stream()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src")))
            dir = dir.Parent;
        Assert.NotNull(dir);

        var text = File.ReadAllText(Path.Combine(
            dir!.FullName, "src", "GC.AuctionFlow", "Atas", "GcAuctionFlowIndicator.cs"));

        var starts = System.Text.RegularExpressions.Regex
            .Matches(text, @"\.EnsureStarted\(").Count;
        var declared = System.Text.RegularExpressions.Regex
            .Matches(text, @"enableDepthRecording: EnableDepthAndQuoteRecording").Count;

        Assert.True(starts > 0, "no EnsureStarted call found — has it been renamed?");
        Assert.True(declared == starts,
            starts + " EnsureStarted call(s) but only " + declared + " declare the depth "
            + "stream; the manifest is built from whichever one runs first");
    }

    /// <summary>
    /// Every depth callback must both count and record.
    ///
    /// `MarketDepthsChanged` — the batch callback, which is where book updates actually
    /// arrive — did neither. The spool therefore held zero `Depth` frames while the card
    /// reported DOM as present, because the count came entirely from
    /// `OnBestBidAskChanged`. Top-of-book looked like a book.
    ///
    /// A callback that observes depth and does not record it loses data that v1.2 §46.5
    /// says cannot be recovered from history, and it does so silently.
    /// </summary>
    [Fact]
    public void A05_Every_depth_callback_counts_and_records()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src")))
            dir = dir.Parent;
        Assert.NotNull(dir);

        var text = File.ReadAllText(Path.Combine(
            dir!.FullName, "src", "GC.AuctionFlow", "Atas", "GcAuctionFlowIndicator.cs"));

        foreach (var callback in new[]
                 {
                     "MarketDepthChanged(MarketDataArg",
                     "MarketDepthsChanged(IEnumerable",
                     "OnBestBidAskChanged(MarketDataArg",
                 })
        {
            var start = text.IndexOf("protected override void " + callback, StringComparison.Ordinal);
            Assert.True(start >= 0, callback + " not found — has it been renamed?");

            var end = text.IndexOf("\n    protected override", start + 1, StringComparison.Ordinal);
            var body = end < 0 ? text[start..] : text[start..end];

            Assert.Contains("NoteDepthCallback()", body, StringComparison.Ordinal);
            Assert.Contains("TryRecordDepth(", body, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Recorder startup must not run on the ATAS thread.
    ///
    /// TryCompleteStartupFromLifecycle creates directories and writes the manifest with
    /// Flush(flushToDisk: true) — two forced fsyncs — and it was being called inside
    /// OnCalculate. A blocked OnCalculate stalls the platform's data pump; when the pump
    /// resumed it delivered everything queued, and the chart aggregated 42,511 backlogged
    /// trades into a single bar 47.7 points tall, on this chart and on others sharing the
    /// workspace.
    ///
    /// Established by bisection, not inference: disabling the recorder removed the artifact
    /// and dropped the backlog from 42,511 to 68, while disabling either renderer changed
    /// nothing.
    /// </summary>
    [Fact]
    public void A06_Recorder_startup_does_not_run_on_the_atas_thread()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src")))
            dir = dir.Parent;
        Assert.NotNull(dir);

        var text = File.ReadAllText(Path.Combine(
            dir!.FullName, "src", "GC.AuctionFlow", "Atas", "GcAuctionFlowIndicator.cs"));

        var start = text.IndexOf("private void TryCompleteRecorderStartup", StringComparison.Ordinal);
        Assert.True(start >= 0, "TryCompleteRecorderStartup not found");

        var end = text.IndexOf("\n    /// <summary>", start + 1, StringComparison.Ordinal);
        var body = end < 0 ? text[start..] : text[start..end];

        // The call, not the mention: the explanatory comment above it names the method.
        var call = body.IndexOf("host.TryCompleteStartupFromLifecycle(", StringComparison.Ordinal);
        Assert.True(call >= 0, "startup is no longer invoked at all");

        Assert.Contains("Task.Run", body, StringComparison.Ordinal);
        Assert.True(
            body.IndexOf("Task.Run", StringComparison.Ordinal) < call,
            "TryCompleteStartupFromLifecycle must be dispatched off the ATAS thread; "
            + "it creates directories and fsyncs the manifest");
    }

    [Fact]
    public void A03_A_recorder_that_is_off_still_reads_clearly() =>
        Assert.DoesNotContain(
            "DEPTH",
            RecorderLine(Publish(depthEnabled: false, depthFrames: 0, recorderOn: false)),
            StringComparison.Ordinal);
}
