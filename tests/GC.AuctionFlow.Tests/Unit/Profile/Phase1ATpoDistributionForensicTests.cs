using GC.AuctionFlow.Core;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Runtime;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Profile;

public sealed class Phase1ATpoDistributionForensicTests
{
    private static readonly PrimaryAuctionClockConfig Cfg = new();
    private static readonly PriceGrid Grid = new(0.1m);

    private static DateTimeOffset EtToUtc(int y, int m, int d, int hh, int mm, int ss = 0)
    {
        var local = new DateTime(y, m, d, hh, mm, ss, DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, AuctionTimezoneResolver.Resolve()));
    }

    private static ProfileBarObservation Bar(
        int idx, DateTimeOffset start, DateTimeOffset end, decimal o, decimal h, decimal l, decimal c, long ver = 1) =>
        new(idx, start, end, o, h, l, c, 1m,
            Array.Empty<PriceVolumeObservation>(), PriceVolumeCapability.Unavailable, true, true, ver);

    [Fact]
    public void Operator_reference_price_diagnostics_count_rank_and_periods()
    {
        var clock = new PrimaryAuctionClock(Cfg);
        var start = EtToUtc(2026, 7, 22, 8, 20, 0);
        var bars = new List<ProfileBarObservation>
        {
            Bar(0, start, start.AddMinutes(5), 100m, 100.5m, 100.0m, 100.0m),
            Bar(1, start.AddMinutes(30), start.AddMinutes(35), 100m, 100.2m, 100.0m, 100.0m),
            Bar(2, start.AddMinutes(60), start.AddMinutes(65), 100.5m, 100.5m, 100.5m, 100.5m)
        };
        var point = clock.Resolve(start);
        var snap = ClassicTpoEngine.Build(
            point.AuctionId, point.AuctionStartUtc, point.AuctionEndUtc,
            bars, clock, Grid, 0.70m, null, start.AddMinutes(90),
            parityReferencePrice: 100.5m);

        Assert.NotNull(snap.ParityDiagnostic!.SelectedPocTarget);
        Assert.NotNull(snap.ParityDiagnostic.ReferenceTarget);
        Assert.Equal(100.5m, snap.ParityDiagnostic.OperatorReferencePrice);
        Assert.Equal(2, snap.ParityDiagnostic.ReferenceTarget!.TpoCount);
        Assert.True(snap.ParityDiagnostic.ReferenceTarget.RankAmongLevels >= 1);
        Assert.Contains(0, snap.ParityDiagnostic.ReferenceTarget.ContributingPeriodIndices);
        Assert.Contains(2, snap.ParityDiagnostic.ReferenceTarget.ContributingPeriodIndices);
        Assert.Equal(
            PeriodIndexCompressor.Compress(snap.ParityDiagnostic.ReferenceTarget.ContributingPeriodIndices.ToArray()),
            snap.ParityDiagnostic.ReferenceTarget.CompressedPeriods);
    }

    [Fact]
    public void Non_poc_level_rank_and_below_max()
    {
        var clock = new PrimaryAuctionClock(Cfg);
        var start = EtToUtc(2026, 7, 22, 8, 20, 0);
        // Build a clear max at 100.0 (3 periods) and lower at 100.5 (1 period).
        var bars = new List<ProfileBarObservation>
        {
            Bar(0, start, start.AddMinutes(5), 100m, 100.0m, 100.0m, 100.0m),
            Bar(1, start.AddMinutes(30), start.AddMinutes(35), 100m, 100.0m, 100.0m, 100.0m),
            Bar(2, start.AddMinutes(60), start.AddMinutes(65), 100m, 100.5m, 100.0m, 100.0m)
        };
        var point = clock.Resolve(start);
        var snap = ClassicTpoEngine.Build(
            point.AuctionId, point.AuctionStartUtc, point.AuctionEndUtc,
            bars, clock, Grid, 0.70m, null, start.AddMinutes(90),
            parityReferencePrice: 100.5m);

        Assert.Equal(100.0m, snap.TpoPoc);
        Assert.Equal(3, snap.ParityDiagnostic!.SelectedPocTarget!.TpoCount);
        Assert.Equal(1, snap.ParityDiagnostic.ReferenceTarget!.TpoCount);
        Assert.Equal(2, snap.ParityDiagnostic.ReferenceTarget.BelowMaxBy);
        Assert.True(snap.ParityDiagnostic.ReferenceTarget.RankAmongLevels > 1);
    }

    [Fact]
    public void Period_membership_compressor_deterministic()
    {
        Assert.Equal("—", PeriodIndexCompressor.Compress(Array.Empty<int>()));
        Assert.Equal("5-8,11,14-18", PeriodIndexCompressor.Compress(new[] { 5, 6, 7, 8, 11, 14, 15, 16, 17, 18 }));
        Assert.Equal("0,2,4", PeriodIndexCompressor.Compress(new[] { 0, 2, 4 }));
    }

    [Fact]
    public void Completed_vs_developing_contributions_separated_on_target()
    {
        var clock = new PrimaryAuctionClock(Cfg);
        var start = EtToUtc(2026, 7, 22, 8, 20, 0);
        var bars = new List<ProfileBarObservation>
        {
            Bar(0, start, start.AddMinutes(5), 100m, 100.5m, 100.0m, 100.0m),
            Bar(1, start.AddMinutes(30), start.AddMinutes(35), 100.5m, 100.5m, 100.5m, 100.5m)
        };
        var point = clock.Resolve(start);
        var snap = ClassicTpoEngine.Build(
            point.AuctionId, point.AuctionStartUtc, point.AuctionEndUtc,
            bars, clock, Grid, 0.70m, null, start.AddMinutes(40),
            parityReferencePrice: 100.5m);

        var refT = snap.ParityDiagnostic!.ReferenceTarget!;
        Assert.True(refT.DevelopingPeriodContributed);
        Assert.Equal(1, refT.CompletedPeriodContributionCount);
        Assert.Equal(2, refT.TpoCount);
    }

    [Fact]
    public void Complete_30m_period_expects_six_M5_starts_when_continuous()
    {
        var clock = new PrimaryAuctionClock(Cfg);
        var start = EtToUtc(2026, 7, 22, 8, 20, 0);
        var bars = new List<ProfileBarObservation>();
        for (var i = 0; i < 6; i++)
        {
            var s = start.AddMinutes(5 * i);
            bars.Add(Bar(i, s, s.AddMinutes(5), 100m, 100.1m, 100.0m, 100.0m));
        }

        var point = clock.Resolve(start);
        var snap = ClassicTpoEngine.Build(
            point.AuctionId, point.AuctionStartUtc, point.AuctionEndUtc,
            bars, clock, Grid, 0.70m, null, start.AddMinutes(30));

        var p0 = Assert.Single(snap.ParityDiagnostic!.Periods);
        Assert.Equal(0, p0.PeriodIndex);
        Assert.Equal(6, p0.ContributingBarCount);
        Assert.Equal(0, p0.MissingExpectedM5Slots);
        Assert.Equal(6, TpoM5SlotForensics.CountExpectedSlots(
            p0.PeriodStartUtc, p0.PeriodEndUtc, start.AddMinutes(30).UtcDateTime, isCompleted: true));
    }

    [Fact]
    public void Missing_M5_slot_detected_when_gap_present()
    {
        var clock = new PrimaryAuctionClock(Cfg);
        var start = EtToUtc(2026, 7, 22, 8, 20, 0);
        var bars = new List<ProfileBarObservation>
        {
            Bar(0, start, start.AddMinutes(5), 100m, 100.0m, 100.0m, 100.0m),
            // skip 08:25
            Bar(1, start.AddMinutes(10), start.AddMinutes(15), 100m, 100.0m, 100.0m, 100.0m),
            Bar(2, start.AddMinutes(15), start.AddMinutes(20), 100m, 100.0m, 100.0m, 100.0m),
            Bar(3, start.AddMinutes(20), start.AddMinutes(25), 100m, 100.0m, 100.0m, 100.0m),
            Bar(4, start.AddMinutes(25), start.AddMinutes(30), 100m, 100.0m, 100.0m, 100.0m)
        };
        var point = clock.Resolve(start);
        var snap = ClassicTpoEngine.Build(
            point.AuctionId, point.AuctionStartUtc, point.AuctionEndUtc,
            bars, clock, Grid, 0.70m, null, start.AddMinutes(30));

        Assert.Equal(1, snap.ParityDiagnostic!.Periods[0].MissingExpectedM5Slots);
    }

    [Fact]
    public void Duplicate_bar_index_and_revised_replacement_tracked_on_host()
    {
        var host = new PrimaryProfileHost(0.1m, Cfg);
        var start = EtToUtc(2026, 7, 22, 8, 20, 0);
        var eval = start.AddMinutes(10);
        host.UpsertBar(Bar(0, start, start.AddMinutes(5), 100m, 100.5m, 100.0m, 100.0m, ver: 1), 0, eval);
        host.UpsertBar(Bar(0, start, start.AddMinutes(5), 100m, 100.1m, 100.0m, 100.0m, ver: 2), 0, eval);

        var d = host.Current!.CurrentAuction!.TpoProfile!.ParityDiagnostic!;
        Assert.NotNull(d.LedgerAudit);
        Assert.True(d.LedgerAudit!.DuplicateBarIndexObservations >= 1);
        Assert.Contains(0, d.LedgerAudit.RevisedBarIndices);
        Assert.False(host.Current.CurrentAuction.TpoProfile.PriceLevelTpoCounts.ContainsKey(Grid.ToTickIndex(100.5m)));
    }

    [Fact]
    public void Rejected_lookahead_bar_accounted()
    {
        var host = new PrimaryProfileHost(0.1m, Cfg);
        var start = EtToUtc(2026, 7, 22, 8, 20, 0);
        host.UpsertBar(Bar(0, start, start.AddMinutes(5), 100m, 100.0m, 100.0m, 100.0m), evaluationBarIndex: 0, start.AddMinutes(5));
        host.UpsertBar(Bar(5, start.AddMinutes(25), start.AddMinutes(30), 100m, 100.0m, 100.0m, 100.0m), evaluationBarIndex: 0, start.AddMinutes(5));

        Assert.Contains(host.RejectedBars, r => r.Contains("LOOKAHEAD_REJECTED", StringComparison.Ordinal));
        Assert.Single(host.Current!.CurrentAuction!.TpoProfile!.ParityDiagnostic!.LedgerAudit!.RejectedBars);
    }

    [Fact]
    public void Current_forming_bar_membership_present_in_ledger_audit()
    {
        var host = new PrimaryProfileHost(0.1m, Cfg);
        var start = EtToUtc(2026, 7, 22, 8, 20, 0);
        host.UpsertBar(Bar(3, start.AddMinutes(15), start.AddMinutes(20), 100m, 100.0m, 100.0m, 100.0m), 3, start.AddMinutes(18));
        var audit = host.Current!.CurrentAuction!.TpoProfile!.ParityDiagnostic!.LedgerAudit!;
        Assert.Equal(3, audit.CurrentFormingBarIndex);
        Assert.True(audit.CurrentFormingBarPresent);
    }

    [Fact]
    public void Reload_remove_add_deterministic_bar_membership()
    {
        var start = EtToUtc(2026, 7, 22, 8, 20, 0);
        var bars = Enumerable.Range(0, 8)
            .Select(i => Bar(i, start.AddMinutes(5 * i), start.AddMinutes(5 * i + 5), 100m, 100.2m, 100.0m, 100.0m))
            .ToList();
        var eval = start.AddMinutes(40);

        var a = new PrimaryProfileHost(0.1m, Cfg)
        {
            EnableTpoParityReferencePrice = true,
            TpoParityReferencePrice = 4130.2m
        };
        foreach (var b in bars)
            a.UpsertBar(b, bars[^1].BarIndex, eval);

        var bHost = new PrimaryProfileHost(0.1m, Cfg)
        {
            EnableTpoParityReferencePrice = true,
            TpoParityReferencePrice = 4130.2m
        };
        // Simulate remove/add: reverse then forward order
        foreach (var b in bars.AsEnumerable().Reverse())
            bHost.UpsertBar(b, bars[^1].BarIndex, eval);

        var da = a.Current!.CurrentAuction!.TpoProfile!;
        var db = bHost.Current!.CurrentAuction!.TpoProfile!;
        Assert.Equal(da.TpoPoc, db.TpoPoc);
        Assert.Equal(
            da.PriceLevelTpoCounts.OrderBy(kv => kv.Key).Select(kv => (kv.Key, kv.Value)),
            db.PriceLevelTpoCounts.OrderBy(kv => kv.Key).Select(kv => (kv.Key, kv.Value)));
        Assert.Equal(da.ParityDiagnostic!.LedgerAudit!.LedgerBarCount, db.ParityDiagnostic!.LedgerAudit!.LedgerBarCount);
    }

    [Fact]
    public void Independent_oracle_equals_engine_distribution()
    {
        var clock = new PrimaryAuctionClock(Cfg);
        var start = EtToUtc(2026, 7, 22, 8, 20, 0);
        var bars = new List<ProfileBarObservation>();
        for (var i = 0; i < 12; i++)
        {
            var s = start.AddMinutes(5 * i);
            bars.Add(Bar(i, s, s.AddMinutes(5), 100m, 100.3m + (i % 2) * 0.1m, 99.9m, 100.0m));
        }

        var point = clock.Resolve(start);
        var snap = ClassicTpoEngine.Build(
            point.AuctionId, point.AuctionStartUtc, point.AuctionEndUtc,
            bars, clock, Grid, 0.70m, null, start.AddMinutes(60));
        var oracle = ClassicTpoOracle.BuildSortedCounts(
            point.AuctionId, point.AuctionStartUtc, point.AuctionEndUtc, bars, clock, Grid);

        Assert.True(ClassicTpoOracle.DistributionsEqual(snap.PriceLevelTpoCounts, oracle));
        Assert.Equal(
            snap.ParityDiagnostic!.MaxTiedPrices.OrderBy(p => p),
            ClassicTpoOracle.MaxCountPrices(oracle, Grid).OrderBy(p => p));
    }

    [Fact]
    public void Oracle_detects_intentionally_corrupted_engine_distribution()
    {
        var clock = new PrimaryAuctionClock(Cfg);
        var start = EtToUtc(2026, 7, 22, 8, 20, 0);
        var bars = new[] { Bar(0, start, start.AddMinutes(5), 100m, 100.2m, 100.0m, 100.0m) };
        var point = clock.Resolve(start);
        var snap = ClassicTpoEngine.Build(
            point.AuctionId, point.AuctionStartUtc, point.AuctionEndUtc,
            bars, clock, Grid, 0.70m, null, start.AddMinutes(5));
        var oracle = ClassicTpoOracle.BuildSortedCounts(
            point.AuctionId, point.AuctionStartUtc, point.AuctionEndUtc, bars, clock, Grid);

        var corrupted = snap.PriceLevelTpoCounts.ToDictionary(kv => kv.Key, kv => kv.Value);
        var key = corrupted.Keys.First();
        corrupted[key] = corrupted[key] + 1;

        Assert.False(ClassicTpoOracle.DistributionsEqual(corrupted, oracle));
    }

    [Fact]
    public void Exact_current_auction_boundary_membership()
    {
        var clock = new PrimaryAuctionClock(Cfg);
        var prior = clock.Resolve(EtToUtc(2026, 7, 22, 8, 20, 0));
        var next = EtToUtc(2026, 7, 23, 8, 20, 0);
        var bars = new[]
        {
            Bar(0, EtToUtc(2026, 7, 22, 8, 20, 0), EtToUtc(2026, 7, 22, 8, 25, 0), 100m, 100.0m, 100.0m, 100.0m),
            Bar(1, EtToUtc(2026, 7, 23, 8, 19, 0), EtToUtc(2026, 7, 23, 8, 20, 0), 150m, 150.0m, 150.0m, 150.0m),
            Bar(2, next, next.AddMinutes(5), 200m, 200.0m, 200.0m, 200.0m)
        };

        var snap = ClassicTpoEngine.Build(
            prior.AuctionId, prior.AuctionStartUtc, prior.AuctionEndUtc,
            bars, clock, Grid, 0.70m, null, next.AddMinutes(10));

        Assert.True(snap.PriceLevelTpoCounts.ContainsKey(Grid.ToTickIndex(100.0m)));
        Assert.True(snap.PriceLevelTpoCounts.ContainsKey(Grid.ToTickIndex(150.0m)));
        Assert.False(snap.PriceLevelTpoCounts.ContainsKey(Grid.ToTickIndex(200.0m)));
    }

    [Fact]
    public void Gps_rows_include_reference_and_ledger_when_diagnostics_enabled()
    {
        var host = new PrimaryProfileHost(0.1m, Cfg)
        {
            EnableTpoParityReferencePrice = true,
            TpoParityReferencePrice = 4130.2m
        };
        var start = EtToUtc(2026, 7, 22, 8, 25, 0);
        host.UpsertBar(Bar(0, start, start.AddMinutes(5), 4124.8m, 4130.2m, 4124.8m, 4125.0m), 0, start.AddMinutes(10));
        var profiles = host.Current!;

        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(0.1m, 14));
        var observed = new ObservedInstrumentSnapshot(
            "GCQ6", "id", "GCQ6", "COMEX", new DateTime(2026, 8, 27), 0.1m, "GC", "GCQ6", "COMEX", 0.1m, null);
        var snap = engine.Publish(
            observed, "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, start.UtcDateTime, false, true, false, false, false, false, profiles, start.UtcDateTime,
            enableTpoParityDiagnostics: true);
        var vm = AuctionGpsCardMapper.FromSnapshot(snap, false);
        Assert.Contains(vm.ProfileDetailLines, l => l.StartsWith("SELECTED POC COUNT:", StringComparison.Ordinal));
        Assert.Contains(vm.ProfileDetailLines, l => l.StartsWith("REFERENCE PRICE:", StringComparison.Ordinal));
        Assert.Contains(vm.ProfileDetailLines, l => l.StartsWith("REFERENCE COUNT:", StringComparison.Ordinal));
        Assert.Contains(vm.ProfileDetailLines, l => l.StartsWith("REFERENCE RANK:", StringComparison.Ordinal));
        Assert.Contains(vm.ProfileDetailLines, l => l.StartsWith("REFERENCE BELOW MAX BY:", StringComparison.Ordinal));
        Assert.Contains(vm.ProfileDetailLines, l => l.StartsWith("REFERENCE PERIODS:", StringComparison.Ordinal));
        Assert.Contains(vm.ProfileDetailLines, l => l.StartsWith("LEDGER BARS:", StringComparison.Ordinal));
        Assert.Contains(vm.ProfileDetailLines, l => l.StartsWith("ATAS BENCHMARK SETTINGS: UNCONFIRMED", StringComparison.Ordinal));
        Assert.Contains(vm.ProfileDetailLines, l => l.Contains("TPO_PARITY_DIAG_V2", StringComparison.Ordinal));
    }

    [Fact]
    public void Atas_benchmark_checklist_documents_required_settings()
    {
        Assert.Contains(TpoAtasBenchmarkChecklist.RequiredSettings, s => s.Contains("08:20", StringComparison.Ordinal));
        Assert.Contains(TpoAtasBenchmarkChecklist.RequiredSettings, s => s.Contains("30 minutes", StringComparison.Ordinal));
        Assert.Contains(TpoAtasBenchmarkChecklist.RequiredSettings, s => s.Contains("Prices per row: 1", StringComparison.Ordinal));
        Assert.Equal(TpoAtasBenchmarkChecklist.StatusUnconfirmed, TpoAtasBenchmarkChecklist.StatusUnconfirmed);
    }
}
