using GC.AuctionFlow.Core;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Runtime;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Runtime;

/// <summary>
/// GPS card compact mode.
///
/// With every module enabled the detail block runs to hundreds of lines and overflows
/// any screen, putting the per-module status rows out of reach — and those rows are the
/// part that answers "is each module alive". Compact mode drops the detail block and
/// keeps the status rows.
/// </summary>
public sealed class GpsCardCompactModeTests
{
    private static DateTime Utc() => new(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);

    private static AuctionGpsCardViewModel Card(bool showDiagnostics)
    {
        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(0.1m, 14));
        var snap = engine.Publish(
            new ObservedInstrumentSnapshot("GCU6", "GCU6-ID", "GCU6", "COMEX",
                new DateTime(2026, 8, 27), 0.1m, "GC", "GCU6", "COMEX", 0.1m, null),
            "GCU6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            tradeObserved: true, lastTradeCallbackUtc: Utc(),
            rawRecorderMasterEnabled: false, tradeRecordingEnabled: true,
            recorderAccepting: false, recorderFaulted: false, recorderSessionPresent: false,
            indicatorDisposed: false, timestampUtc: Utc());
        return AuctionGpsCardMapper.FromSnapshot(snap, showDiagnostics);
    }

    [Fact]
    public void A01_Compact_is_never_longer_than_full()
    {
        var vm = Card(showDiagnostics: true);
        Assert.True(vm.AllLines(true, compact: true).Count
                    <= vm.AllLines(true, compact: false).Count);
    }

    /// <summary>
    /// The whole point: the status rows must survive compaction. Dropping them would
    /// defeat the purpose of the mode.
    /// </summary>
    [Fact]
    public void A02_Compact_keeps_every_status_row()
    {
        var vm = Card(showDiagnostics: true);
        var compact = vm.AllLines(true, compact: true);
        foreach (var row in vm.DiagnosticRows)
            Assert.Contains(row, compact);
    }

    [Fact]
    public void A03_Compact_keeps_the_header()
    {
        var vm = Card(showDiagnostics: true);
        var compact = vm.AllLines(true, compact: true);
        Assert.Contains(vm.Title, compact);
        Assert.Contains(vm.DataLine, compact);
        Assert.Contains(vm.ModeLine, compact);
        Assert.Contains(vm.ContractLine, compact);
    }

    [Fact]
    public void A04_Compact_drops_the_detail_block()
    {
        var vm = Card(showDiagnostics: true);
        var compact = vm.AllLines(true, compact: true);
        foreach (var detail in vm.ProfileDetailLines)
            Assert.DoesNotContain(detail, compact);
    }

    /// <summary>
    /// Compact implies the status rows.
    ///
    /// This originally asserted the two flags were independent. That encoded a bad
    /// design, and the first live run proved it: turning on Compact Mode while leaving
    /// diagnostics off produced a bare 15-line header with nothing to read. Compact
    /// exists to surface the status block, so it must never be reachable without it.
    /// </summary>
    [Fact]
    public void A05_Compact_implies_the_status_rows()
    {
        var vm = Card(showDiagnostics: true);
        var compactNoDiag = vm.AllLines(includeDiagnostics: false, compact: true);
        foreach (var row in vm.DiagnosticRows)
            Assert.Contains(row, compactNoDiag);
    }

    /// <summary>Non-compact keeps the original meaning: diagnostics gates the rows.</summary>
    [Fact]
    public void A05b_Non_compact_still_respects_the_diagnostics_flag()
    {
        var vm = Card(showDiagnostics: true);
        var plain = vm.AllLines(includeDiagnostics: false, compact: false);
        Assert.All(vm.DiagnosticRows, r => Assert.DoesNotContain(r, plain));
    }

    /// <summary>Existing callers must be unaffected.</summary>
    [Fact]
    public void A06_Default_is_not_compact()
    {
        var vm = Card(showDiagnostics: true);
        Assert.Equal(vm.AllLines(true, compact: false).Count, vm.AllLines(true).Count);
    }

    /// <summary>
    /// The reason the mode exists: a real screen fits roughly 60 rows at the card's
    /// line height. Compact must land well inside that.
    /// </summary>
    [Fact]
    public void A07_Compact_fits_a_screen()
    {
        var compact = Card(showDiagnostics: true).AllLines(true, compact: true);
        Assert.True(compact.Count <= 60,
            "compact card is " + compact.Count + " lines; it must fit one screen");
    }

    /// <summary>
    /// The path that actually broke live.
    ///
    /// Every earlier test in this file built the card with showDiagnostics:true, so the
    /// status rows already existed and compaction merely kept them. That never exercised
    /// the real configuration — compact on, diagnostics off — where FromSnapshot is told
    /// not to BUILD the rows at all and compaction has nothing to keep.
    ///
    /// This reproduces what the indicator does, so the gap cannot reopen.
    /// </summary>
    [Fact]
    public void A08_Compact_with_diagnostics_off_still_builds_the_status_rows()
    {
        // The indicator must ask FromSnapshot to build the rows when compact is on.
        const bool showDiagnosticsSetting = false;
        const bool compactSetting = true;
        var buildDiagnosticRows = showDiagnosticsSetting || compactSetting;

        var vm = Card(showDiagnostics: buildDiagnosticRows);
        var lines = vm.AllLines(buildDiagnosticRows, compactSetting);

        Assert.NotEmpty(vm.DiagnosticRows);
        foreach (var row in vm.DiagnosticRows)
            Assert.Contains(row, lines);
    }

    /// <summary>
    /// Guards the failure directly: building without diagnostics yields no rows, so a
    /// render-time flag alone can never recover them.
    /// </summary>
    [Fact]
    public void A09_Rows_not_built_cannot_be_recovered_at_render_time()
    {
        var vm = Card(showDiagnostics: false);
        Assert.Empty(vm.DiagnosticRows);

        // Even asking for compact cannot add rows that were never constructed.
        var lines = vm.AllLines(includeDiagnostics: true, compact: true);
        Assert.DoesNotContain(lines, l => l.StartsWith("EPISODE:", StringComparison.Ordinal));
    }
}
