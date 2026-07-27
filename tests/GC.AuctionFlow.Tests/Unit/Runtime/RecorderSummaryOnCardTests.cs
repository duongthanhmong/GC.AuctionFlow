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

    [Fact]
    public void A03_A_recorder_that_is_off_still_reads_clearly() =>
        Assert.DoesNotContain(
            "DEPTH",
            RecorderLine(Publish(depthEnabled: false, depthFrames: 0, recorderOn: false)),
            StringComparison.Ordinal);
}
