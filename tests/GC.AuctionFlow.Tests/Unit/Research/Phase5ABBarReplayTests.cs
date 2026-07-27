using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Reference;
using GC.AuctionFlow.Research;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Research;

/// <summary>
/// Phase 5A-b bar replay (v1.2 §46.2).
///
/// §46 says the Historical Scanner exists so research does not wait months for live data.
/// The live episode path cannot deliver that — an episode closes only when the primary
/// auction rolls or its reference retires, roughly one batch of rows per trading day — so
/// this walks the bars already loaded on the chart instead.
///
/// The constraint that shapes everything here: a bar has no intra-bar ordering. It reports
/// open, high, low and close but not whether price went O-H-L-C or O-L-H-C, so it cannot
/// say whether price crossed a level once or six times, nor which side it came from. Every
/// acceptance and re-entry conclusion in this engine rests on that sequence. So bar rows
/// are a separate, weaker dataset that is never pooled with live episode rows.
/// </summary>
public sealed class Phase5ABBarReplayTests
{
    private const decimal Tick = 0.1m;

    private static DateTimeOffset At(int minutes) =>
        new DateTimeOffset(2026, 7, 27, 12, 0, 0, TimeSpan.Zero).AddMinutes(minutes);

    private static ProfileBarObservation Bar(
        int index,
        decimal open, decimal high, decimal low, decimal close,
        decimal volume = 100m,
        IReadOnlyList<PriceVolumeObservation>? levels = null,
        bool completed = true,
        bool historical = true) =>
        new(
            barIndex: index,
            startUtc: At(index),
            endUtc: At(index + 1),
            open: open,
            high: high,
            low: low,
            close: close,
            totalVolume: volume,
            priceVolumes: levels ?? Array.Empty<PriceVolumeObservation>(),
            priceVolumeCapability: PriceVolumeCapability.Unavailable,
            isHistorical: historical,
            isCompleted: completed,
            sourceVersion: 1,
            timestampPolicyVersion: "TS_V1");

    private static StructuralReferenceSnapshot Reference(
        decimal low, decimal high,
        ReferenceMaturity maturity = ReferenceMaturity.Confirmed,
        ReferenceType type = ReferenceType.PreviousPrimaryTpoPoc,
        string id = "REF-A") =>
        new(
            id, type, low, high,
            (long)(low / Tick), (long)(high / Tick),
            "PI-2026-07-26",
            ReferenceSourceKind.PreviousPrimaryAuction,
            ReferenceSourceHorizon.PreviousPrimaryAuction,
            At(0).UtcDateTime,
            maturity, ReferenceStatus.Active, ReferenceEvidenceTier.ProfileDerived,
            Tick, "E1", "TS_V1", 1, At(0).UtcDateTime, At(0).UtcDateTime);

    private static StructuralReferenceSetSnapshot ReferenceSet(
        params StructuralReferenceSnapshot[] refs) =>
        new(
            StructuralReferenceModuleState.Ready,
            ReferencePolicyConfig.PolicyVersion,
            refs.Where(r => r.Maturity == ReferenceMaturity.Confirmed).ToArray(),
            refs.Where(r => r.Maturity == ReferenceMaturity.Developing).ToArray(),
            Array.Empty<StructuralReferenceSnapshot>(),
            Array.Empty<ReferenceConfluenceGroup>(),
            null, "fp", 1,
            Array.Empty<string>(), Array.Empty<string>(),
            At(0).UtcDateTime);

    // ========== A: what a bar can honestly say ==========

    [Fact]
    public void A01_A_bar_that_never_reaches_the_zone_produces_no_row() =>
        Assert.Null(BarDerivedInteractionRecord.TryMeasure(
            Bar(1, 4100m, 4105m, 4098m, 4102m), Reference(4090m, 4092m), Tick));

    [Fact]
    public void A02_Excursions_are_measured_from_the_zone_edges()
    {
        var row = BarDerivedInteractionRecord.TryMeasure(
            Bar(1, 4091m, 4094m, 4088m, 4091m), Reference(4090m, 4092m), Tick);

        Assert.NotNull(row);
        Assert.Equal(20, row!.MaximumExcursionAboveTicks);   // 4094.0 - 4092.0
        Assert.Equal(20, row.MaximumExcursionBelowTicks);    // 4090.0 - 4088.0
    }

    /// <summary>
    /// Null, not zero. Zero ticks above would claim the bar reached the top edge exactly;
    /// null says it never went above at all. Different facts (v1.3 `G-ACC-003`).
    /// </summary>
    [Fact]
    public void A03_An_excursion_that_never_happened_is_null_not_zero()
    {
        var row = BarDerivedInteractionRecord.TryMeasure(
            Bar(1, 4091m, 4091.5m, 4088m, 4090.5m), Reference(4090m, 4092m), Tick);

        Assert.NotNull(row);
        Assert.Null(row!.MaximumExcursionAboveTicks);
        Assert.Equal(20, row.MaximumExcursionBelowTicks);
    }

    [Fact]
    public void A04_Close_location_is_geometry_not_acceptance()
    {
        var reference = Reference(4090m, 4092m);

        Assert.Equal(BarCloseLocation.Above, BarDerivedInteractionRecord
            .TryMeasure(Bar(1, 4091m, 4095m, 4089m, 4093m), reference, Tick)!.CloseLocation);
        Assert.Equal(BarCloseLocation.Below, BarDerivedInteractionRecord
            .TryMeasure(Bar(2, 4091m, 4095m, 4085m, 4087m), reference, Tick)!.CloseLocation);
        Assert.Equal(BarCloseLocation.Inside, BarDerivedInteractionRecord
            .TryMeasure(Bar(3, 4091m, 4095m, 4085m, 4091m), reference, Tick)!.CloseLocation);
    }

    /// <summary>
    /// The core limitation, asserted rather than left to a comment. If a future edit adds
    /// a cross count or an approach direction to this record, it will have invented the
    /// intra-bar path, and this fails.
    /// </summary>
    [Fact]
    public void A05_Intra_bar_ordering_is_never_available()
    {
        var row = BarDerivedInteractionRecord.TryMeasure(
            Bar(1, 4091m, 4095m, 4085m, 4091m), Reference(4090m, 4092m), Tick);

        Assert.Equal(IntraBarPathAvailability.Unavailable, row!.IntraBarPath);
        Assert.Single(Enum.GetValues<IntraBarPathAvailability>());

        var banned = new[] { "CrossCount", "Direction", "Sequence", "ApproachedFrom", "FirstTouch", "Order" };
        foreach (var property in typeof(BarDerivedInteractionRecord).GetProperties())
        foreach (var word in banned)
            Assert.False(property.Name.Contains(word, StringComparison.OrdinalIgnoreCase),
                "BarDerivedInteractionRecord." + property.Name + " implies an intra-bar "
                + "ordering that a bar does not contain");
    }

    [Fact]
    public void A06_Every_row_declares_it_came_from_bars()
    {
        var row = BarDerivedInteractionRecord.TryMeasure(
            Bar(1, 4091m, 4095m, 4085m, 4091m), Reference(4090m, 4092m), Tick);
        Assert.Equal(ObservationSource.HistoricalBars, row!.Source);
    }

    // ========== B: volume at price, when the feed gives it ==========

    [Fact]
    public void B01_Volume_at_zone_sums_only_levels_inside_the_zone()
    {
        var levels = new[]
        {
            new PriceVolumeObservation(4089m, 50m, null, null, null, "bar"),
            new PriceVolumeObservation(4090m, 30m, null, null, null, "bar"),
            new PriceVolumeObservation(4091m, 20m, null, null, null, "bar"),
            new PriceVolumeObservation(4095m, 70m, null, null, null, "bar"),
        };

        var row = BarDerivedInteractionRecord.TryMeasure(
            Bar(1, 4089m, 4095m, 4089m, 4091m, levels: levels), Reference(4090m, 4092m), Tick);

        Assert.Equal(50m, row!.VolumeAtZone);
    }

    /// <summary>Zero would claim the zone traded nothing; null says the feed did not say.</summary>
    [Fact]
    public void B02_No_volume_at_price_yields_null_not_zero()
    {
        var row = BarDerivedInteractionRecord.TryMeasure(
            Bar(1, 4091m, 4095m, 4085m, 4091m), Reference(4090m, 4092m), Tick);

        Assert.Null(row!.VolumeAtZone);
        Assert.Null(row.DeltaAtZone);
    }

    [Fact]
    public void B03_Delta_requires_both_sides()
    {
        var oneSided = new[] { new PriceVolumeObservation(4091m, 20m, 8m, null, null, "bar") };
        var bothSides = new[] { new PriceVolumeObservation(4091m, 20m, 8m, 12m, null, "bar") };

        Assert.Null(BarDerivedInteractionRecord.TryMeasure(
            Bar(1, 4091m, 4092m, 4090m, 4091m, levels: oneSided), Reference(4090m, 4092m), Tick)!.DeltaAtZone);

        Assert.Equal(4m, BarDerivedInteractionRecord.TryMeasure(
            Bar(2, 4091m, 4092m, 4090m, 4091m, levels: bothSides), Reference(4090m, 4092m), Tick)!.DeltaAtZone);
    }

    // ========== C: the replay walks history once ==========

    [Fact]
    public void C01_Buffered_bars_are_measured_once_references_arrive()
    {
        var host = new HistoricalBarReplayHost(enabled: true);
        for (var i = 0; i < 5; i++)
            host.ObserveBar(Bar(i, 4091m, 4093m, 4089m, 4091m));

        Assert.Equal(BarReplayState.AwaitingReferences, host.State);

        host.Replay(ReferenceSet(Reference(4090m, 4092m)), Tick);

        Assert.Equal(BarReplayState.Replayed, host.State);
        Assert.Equal(5, host.BarsWalked);
        Assert.Equal(5, host.RowsMeasured);
    }

    /// <summary>Re-measuring a bar would duplicate it into every distribution built on it.</summary>
    [Fact]
    public void C02_A_bar_is_never_measured_twice()
    {
        var host = new HistoricalBarReplayHost(enabled: true);
        var references = ReferenceSet(Reference(4090m, 4092m));

        for (var i = 0; i < 3; i++)
            host.ObserveBar(Bar(i, 4091m, 4093m, 4089m, 4091m));

        host.Replay(references, Tick);
        host.Replay(references, Tick);
        host.Replay(references, Tick);

        Assert.Equal(3, host.RowsMeasured);
    }

    /// <summary>
    /// Developing references migrate as the auction builds. Measuring an old bar against a
    /// level that did not yet hold that price would be reconstruction, not observation.
    /// </summary>
    [Fact]
    public void C03_Only_confirmed_references_are_replayed()
    {
        var host = new HistoricalBarReplayHost(enabled: true);
        host.ObserveBar(Bar(1, 4091m, 4093m, 4089m, 4091m));

        host.Replay(
            ReferenceSet(
                Reference(4090m, 4092m, ReferenceMaturity.Confirmed, id: "REF-CONFIRMED"),
                Reference(4090m, 4092m, ReferenceMaturity.Developing, id: "REF-DEVELOPING")),
            Tick);

        Assert.Equal(1, host.ReferencesReplayed);
        Assert.Equal("REF-CONFIRMED", Assert.Single(host.Rows).ReferenceId);
    }

    /// <summary>A forming bar's high, low and close all still move.</summary>
    [Fact]
    public void C04_Incomplete_bars_are_not_buffered()
    {
        var host = new HistoricalBarReplayHost(enabled: true);
        host.ObserveBar(Bar(1, 4091m, 4093m, 4089m, 4091m, completed: false));
        host.Replay(ReferenceSet(Reference(4090m, 4092m)), Tick);

        Assert.Equal(0, host.RowsMeasured);
    }

    [Fact]
    public void C05_No_references_means_no_replay()
    {
        var host = new HistoricalBarReplayHost(enabled: true);
        host.ObserveBar(Bar(1, 4091m, 4093m, 4089m, 4091m));

        host.Replay(null, Tick);
        host.Replay(ReferenceSet(), Tick);

        Assert.Equal(BarReplayState.AwaitingReferences, host.State);
        Assert.Equal(0, host.RowsMeasured);
    }

    [Fact]
    public void C06_Disabled_host_buffers_nothing()
    {
        var host = new HistoricalBarReplayHost(enabled: false);
        host.ObserveBar(Bar(1, 4091m, 4093m, 4089m, 4091m));
        host.Replay(ReferenceSet(Reference(4090m, 4092m)), Tick);

        Assert.Equal(BarReplayState.Disabled, host.State);
        Assert.Equal(0, host.RowsMeasured);
    }

    // ========== D: the two datasets stay apart ==========

    /// <summary>
    /// The card shows a pair, not a total.
    ///
    /// Summing them would produce a number that is larger and less sound — a bar row
    /// cannot answer an acceptance question — and the operator would have no way to see
    /// which of the two actually grew.
    /// </summary>
    [Fact]
    public void D01_Card_reports_episode_and_bar_rows_separately()
    {
        var replay = new HistoricalBarReplayHost(enabled: true);
        for (var i = 0; i < 4; i++)
            replay.ObserveBar(Bar(i, 4091m, 4093m, 4089m, 4091m));
        replay.Replay(ReferenceSet(Reference(4090m, 4092m)), Tick);

        var scanner = new HistoricalScannerHost(new HistoricalScannerPolicyConfig(enabled: true));
        scanner.Rebuild(null, At(0).UtcDateTime, replay);

        var line = AuctionGpsCardMapper.ScannerLine(scanner.Current);
        Assert.Contains("0 EP / 4 BAR", line, StringComparison.Ordinal);
    }

    /// <summary>
    /// Bar rows must not advance the calibration protocol.
    ///
    /// The protocol reads the episode dataset. Letting bar rows satisfy step one would
    /// mean a distribution built partly from measurements with no intra-bar ordering, and
    /// the missing axes would still be missing regardless.
    /// </summary>
    [Fact]
    public void D02_Bar_rows_do_not_advance_the_calibration_protocol()
    {
        var replay = new HistoricalBarReplayHost(enabled: true);
        for (var i = 0; i < 500; i++)
            replay.ObserveBar(Bar(i, 4091m, 4093m, 4089m, 4091m));
        replay.Replay(ReferenceSet(Reference(4090m, 4092m)), Tick);

        var scanner = new HistoricalScannerHost(new HistoricalScannerPolicyConfig(enabled: true));
        scanner.Rebuild(null, At(0).UtcDateTime, replay);

        Assert.Equal(500, scanner.Current!.BarDerivedRows);
        Assert.Equal(0, scanner.Current.RowsCollected);
        Assert.Equal(CalibrationProtocolStep.CollectRawFeatures, scanner.Current.Protocol.BlockedAt);
        Assert.False(scanner.Current.Protocol.UnlockPermitted);
    }

    /// <summary>
    /// The locked-phase invariant that shaped this whole design.
    ///
    /// EpisodeTradeEvent is documented as built from trade prints and never from candles,
    /// but nothing enforced it. Synthesising episode events from bar geometry would have
    /// been the obvious way to build this phase and would have silently corrupted every
    /// acceptance conclusion downstream, because the sequence would have been invented.
    /// </summary>
    [Fact]
    public void D03_Nothing_in_research_constructs_an_episode_trade_event()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !Directory.Exists(Path.Combine(root.FullName, "src")))
            root = root.Parent;
        Assert.NotNull(root);

        var research = Path.Combine(root!.FullName, "src", "GC.AuctionFlow", "Research");
        var files = Directory.GetFiles(research, "*.cs");
        Assert.NotEmpty(files);

        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("new EpisodeTradeEvent(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("EpisodeTradeEvent.TryFrom", text, StringComparison.Ordinal);
        }
    }
}
