using GC.AuctionFlow.Core;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Runtime;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Profile;

/// <summary>Focused Classic TPO parity / period-ownership / developing-policy tests. Does not change production POC policy.</summary>
public sealed class Phase1ATpoParityDiagnosticsTests
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
    public void Six_M5_bars_in_one_30m_period_count_each_price_once()
    {
        var clock = new PrimaryAuctionClock(Cfg);
        var start = EtToUtc(2026, 7, 22, 8, 20, 0);
        var bars = new List<ProfileBarObservation>();
        for (var i = 0; i < 6; i++)
        {
            var s = start.AddMinutes(5 * i);
            bars.Add(Bar(i, s, s.AddMinutes(5), 100m, 100.3m, 100.0m, 100.1m));
        }

        var point = clock.Resolve(start);
        var snap = ClassicTpoEngine.Build(
            point.AuctionId, point.AuctionStartUtc, point.AuctionEndUtc,
            bars, clock, Grid, 0.70m, null, start.AddMinutes(29));

        Assert.Equal(1, snap.ParityDiagnostic!.ObservedPeriodCount);
        Assert.Equal(1, snap.PriceLevelTpoCounts[Grid.ToTickIndex(100.0m)]);
        Assert.Equal(1, snap.PriceLevelTpoCounts[Grid.ToTickIndex(100.1m)]);
        Assert.Equal(1, snap.PriceLevelTpoCounts[Grid.ToTickIndex(100.2m)]);
        Assert.Equal(1, snap.PriceLevelTpoCounts[Grid.ToTickIndex(100.3m)]);
        Assert.Equal(4, snap.TotalTpoCount);
    }

    [Fact]
    public void Bar_at_0820_ET_starts_period_0_and_0815_belongs_to_previous_auction()
    {
        var clock = new PrimaryAuctionClock(Cfg);
        var at0820 = clock.Resolve(EtToUtc(2026, 7, 22, 8, 20, 0));
        Assert.Equal("PI-2026-07-22", at0820.AuctionId);
        Assert.Equal(0, at0820.TpoPeriodIndex);

        var at0815 = clock.Resolve(EtToUtc(2026, 7, 22, 8, 15, 0));
        Assert.Equal("PI-2026-07-21", at0815.AuctionId);

        var instantBefore = clock.Resolve(EtToUtc(2026, 7, 22, 8, 19, 59));
        Assert.Equal("PI-2026-07-21", instantBefore.AuctionId);
    }

    [Fact]
    public void Bar_at_0850_ET_starts_period_1_and_exact_boundary_not_double_counted()
    {
        var clock = new PrimaryAuctionClock(Cfg);
        var p0 = clock.Resolve(EtToUtc(2026, 7, 22, 8, 20, 0));
        var p1 = clock.Resolve(EtToUtc(2026, 7, 22, 8, 50, 0));
        Assert.Equal(0, p0.TpoPeriodIndex);
        Assert.Equal(1, p1.TpoPeriodIndex);
        Assert.Equal(p0.TpoPeriodEndUtc, p1.TpoPeriodStartUtc);

        var bars = new List<ProfileBarObservation>
        {
            Bar(0, EtToUtc(2026, 7, 22, 8, 20, 0), EtToUtc(2026, 7, 22, 8, 25, 0), 100m, 100.0m, 100.0m, 100.0m),
            // Bar start equals period-0 end / period-1 start — owns period 1 only (StartUtc ownership).
            Bar(1, EtToUtc(2026, 7, 22, 8, 50, 0), EtToUtc(2026, 7, 22, 8, 55, 0), 100m, 100.0m, 100.0m, 100.0m)
        };

        var snap = ClassicTpoEngine.Build(
            p0.AuctionId, p0.AuctionStartUtc, p0.AuctionEndUtc,
            bars, clock, Grid, 0.70m, null, EtToUtc(2026, 7, 22, 9, 0, 0));

        Assert.Equal(2, snap.PriceLevelTpoCounts[Grid.ToTickIndex(100.0m)]);
        Assert.Equal(2, snap.ParityDiagnostic!.ObservedPeriodCount);
        Assert.DoesNotContain(snap.ParityDiagnostic.Periods, p => p.PeriodIndex == 0 && p.LastBarIndex == 1);
    }

    [Fact]
    public void Developing_period_included_in_production_poc_with_completed_only_diagnostic()
    {
        Assert.True(TpoProfileSnapshot.DevelopingPeriodIncludedInPocAndVa);

        var clock = new PrimaryAuctionClock(Cfg);
        var start = EtToUtc(2026, 7, 22, 8, 20, 0);
        // Period 0 completed: 100.0 + 100.5. Developing period 1: 100.5 only → production POC 100.5.
        var bars = new List<ProfileBarObservation>
        {
            Bar(0, start, start.AddMinutes(5), 100m, 100.5m, 100.0m, 100.0m),
            Bar(1, start.AddMinutes(30), start.AddMinutes(35), 100.5m, 100.5m, 100.5m, 100.5m)
        };
        var point = clock.Resolve(start);
        var snap = ClassicTpoEngine.Build(
            point.AuctionId, point.AuctionStartUtc, point.AuctionEndUtc,
            bars, clock, Grid, 0.70m, null, start.AddMinutes(40));

        Assert.Equal(1, snap.DevelopingPeriodIndex);
        Assert.True(snap.ParityDiagnostic!.DevelopingPeriodIncludedInPocAndVa);
        Assert.Equal(100.5m, snap.TpoPoc);
        // Completed-only: flat max across 100.0–100.5 → midpoint/lower-tick → 100.2
        Assert.Equal(100.2m, snap.ParityDiagnostic.CompletedOnlyTpoPoc);
        Assert.Contains(snap.KnownLimitations, l => l.Contains("DEVELOPING_PERIOD_INCLUDED_IN_POC_AND_VA=true", StringComparison.Ordinal));
    }

    [Fact]
    public void Empty_two_period_gap_does_not_contribute_tpos()
    {
        var clock = new PrimaryAuctionClock(Cfg);
        var start = EtToUtc(2026, 7, 22, 8, 20, 0);
        var bars = new List<ProfileBarObservation>
        {
            Bar(0, start, start.AddMinutes(5), 100m, 100.0m, 100.0m, 100.0m),
            // Skip periods 1 and 2; period 3 starts at 08:20+90m = 09:50
            Bar(1, start.AddMinutes(90), start.AddMinutes(95), 100m, 100.0m, 100.0m, 100.0m)
        };
        var point = clock.Resolve(start);
        var snap = ClassicTpoEngine.Build(
            point.AuctionId, point.AuctionStartUtc, point.AuctionEndUtc,
            bars, clock, Grid, 0.70m, null, start.AddMinutes(120));

        Assert.Equal(2, snap.ParityDiagnostic!.ObservedPeriodCount);
        Assert.Equal(2, snap.ParityDiagnostic.EmptyPeriodSlots);
        Assert.Equal(2, snap.PriceLevelTpoCounts[Grid.ToTickIndex(100.0m)]);
        Assert.Equal(2, snap.TotalTpoCount);
    }

    [Fact]
    public void Revised_bar_range_replacement_removes_old_ticks()
    {
        var host = new PrimaryProfileHost(0.1m, Cfg);
        var start = EtToUtc(2026, 7, 22, 8, 20, 0);
        var eval = start.AddMinutes(10);
        host.UpsertBar(Bar(0, start, start.AddMinutes(5), 100m, 100.5m, 100.0m, 100.0m, ver: 1), 0, eval);
        var afterWide = host.Current!.CurrentAuction!.TpoProfile!;
        Assert.True(afterWide.PriceLevelTpoCounts.ContainsKey(Grid.ToTickIndex(100.5m)));

        host.UpsertBar(Bar(0, start, start.AddMinutes(5), 100m, 100.1m, 100.0m, 100.0m, ver: 2), 0, eval);
        var afterNarrow = host.Current!.CurrentAuction!.TpoProfile!;
        Assert.False(afterNarrow.PriceLevelTpoCounts.ContainsKey(Grid.ToTickIndex(100.5m)));
        Assert.True(afterNarrow.PriceLevelTpoCounts.ContainsKey(Grid.ToTickIndex(100.0m)));
        Assert.True(afterNarrow.PriceLevelTpoCounts.ContainsKey(Grid.ToTickIndex(100.1m)));
    }

    [Fact]
    public void Inclusive_low_high_endpoints_and_no_float_drift()
    {
        var clock = new PrimaryAuctionClock(Cfg);
        var start = EtToUtc(2026, 7, 22, 8, 20, 0);
        var bars = new[] { Bar(0, start, start.AddMinutes(5), 4129.0m, 4130.2m, 4129.0m, 4129.5m) };
        var point = clock.Resolve(start);
        var snap = ClassicTpoEngine.Build(
            point.AuctionId, point.AuctionStartUtc, point.AuctionEndUtc,
            bars, clock, Grid, 0.70m, null, start.AddMinutes(5));

        // 4129.0 .. 4130.2 inclusive at 0.1 = 13 levels
        Assert.Equal(13, snap.TotalTpoCount);
        Assert.True(snap.PriceLevelTpoCounts.ContainsKey(Grid.ToTickIndex(4129.0m)));
        Assert.True(snap.PriceLevelTpoCounts.ContainsKey(Grid.ToTickIndex(4130.2m)));
        Assert.False(snap.PriceLevelTpoCounts.ContainsKey(Grid.ToTickIndex(4128.9m)));
        Assert.False(snap.PriceLevelTpoCounts.ContainsKey(Grid.ToTickIndex(4130.3m)));
    }

    [Fact]
    public void Live_equivalent_Unspecified_UTC_timestamps_group_correctly()
    {
        // LiveObserved: Unspecified wall clock is UTC (04:50Z → 00:50 ET → period 33 on PI-2026-07-22).
        var raw = new DateTime(2026, 7, 23, 4, 50, 0, DateTimeKind.Unspecified);
        var utc = AtasTimestampNormalizer.NormalizeCandleTime(raw);
        Assert.Equal(DateTimeKind.Utc, utc.UtcDateTime.Kind);
        Assert.Equal(new DateTime(2026, 7, 23, 4, 50, 0, DateTimeKind.Utc), utc.UtcDateTime);

        var clock = new PrimaryAuctionClock(Cfg);
        var point = clock.Resolve(utc);
        Assert.Equal("PI-2026-07-22", point.AuctionId);
        Assert.Equal(33, point.TpoPeriodIndex);
    }

    [Fact]
    public void Deterministic_tpo_poc_from_sorted_counts_and_diagnostic_snapshot()
    {
        var clock = new PrimaryAuctionClock(Cfg);
        var start = EtToUtc(2026, 7, 22, 8, 20, 0);
        var bars = new List<ProfileBarObservation>
        {
            Bar(0, start, start.AddMinutes(5), 100m, 100.2m, 100.0m, 100.1m),
            Bar(1, start.AddMinutes(30), start.AddMinutes(35), 100m, 100.1m, 100.0m, 100.0m),
            Bar(2, start.AddMinutes(60), start.AddMinutes(65), 100m, 100.0m, 100.0m, 100.0m)
        };
        var point = clock.Resolve(start);
        var snap = ClassicTpoEngine.Build(
            point.AuctionId, point.AuctionStartUtc, point.AuctionEndUtc,
            bars, clock, Grid, 0.70m, null, start.AddMinutes(90));

        var d = snap.ParityDiagnostic!;
        Assert.Equal("TPO_PARITY_DIAG_V2", d.Version);
        Assert.Equal(AtasTimestampNormalizer.PolicyVersion, d.TimestampPolicyVersion);
        Assert.True(d.SortedCounts.Select(x => x.Tick).SequenceEqual(d.SortedCounts.Select(x => x.Tick).OrderBy(t => t)));
        Assert.Equal(snap.TpoPoc, d.SelectedTpoPoc);
        Assert.Equal(d.MaxTpoCount, d.SortedCounts.Max(x => x.Count));
        Assert.All(d.MaxTiedPrices, p => Assert.Equal(d.MaxTpoCount, snap.PriceLevelTpoCounts[Grid.ToTickIndex(p)]));
    }

    [Fact]
    public void Full_rebuild_equals_incremental_upsert_sorted_distribution()
    {
        var start = EtToUtc(2026, 7, 22, 8, 20, 0);
        var bars = new List<ProfileBarObservation>();
        for (var i = 0; i < 8; i++)
        {
            var s = start.AddMinutes(5 * i);
            bars.Add(Bar(i, s, s.AddMinutes(5), 100m, 100.2m + (i % 3) * 0.1m, 100.0m, 100.1m));
        }

        var eval = start.AddMinutes(50);
        var incremental = new PrimaryProfileHost(0.1m, Cfg);
        PrimaryProfileSetSnapshot? last = null;
        foreach (var b in bars)
            last = incremental.UpsertBar(b, b.BarIndex, eval);

        var full = new PrimaryProfileHost(0.1m, Cfg);
        foreach (var b in bars)
            full.UpsertBar(b, bars[^1].BarIndex, eval);
        var rebuilt = full.Rebuild(bars[^1].BarIndex, eval);

        var a = last!.CurrentAuction!.TpoProfile!;
        var bSnap = rebuilt.CurrentAuction!.TpoProfile!;
        Assert.Equal(a.TpoPoc, bSnap.TpoPoc);
        Assert.Equal(
            a.PriceLevelTpoCounts.OrderBy(kv => kv.Key).Select(kv => (kv.Key, kv.Value)),
            bSnap.PriceLevelTpoCounts.OrderBy(kv => kv.Key).Select(kv => (kv.Key, kv.Value)));
    }

    [Fact]
    public void Auction_end_exclusive_bar_at_next_0820_not_in_prior_auction()
    {
        var clock = new PrimaryAuctionClock(Cfg);
        var prior = clock.Resolve(EtToUtc(2026, 7, 22, 8, 20, 0));
        var nextAnchor = EtToUtc(2026, 7, 23, 8, 20, 0);
        Assert.Equal(prior.AuctionEndUtc, nextAnchor.UtcDateTime);

        var bars = new[]
        {
            Bar(0, EtToUtc(2026, 7, 22, 8, 20, 0), EtToUtc(2026, 7, 22, 8, 25, 0), 100m, 100.0m, 100.0m, 100.0m),
            Bar(1, nextAnchor, nextAnchor.AddMinutes(5), 200m, 200.0m, 200.0m, 200.0m)
        };

        var snap = ClassicTpoEngine.Build(
            prior.AuctionId, prior.AuctionStartUtc, prior.AuctionEndUtc,
            bars, clock, Grid, 0.70m, null, nextAnchor.AddMinutes(10));

        Assert.True(snap.PriceLevelTpoCounts.ContainsKey(Grid.ToTickIndex(100.0m)));
        Assert.False(snap.PriceLevelTpoCounts.ContainsKey(Grid.ToTickIndex(200.0m)));
    }

    [Fact]
    public void Gps_tpo_parity_rows_hidden_by_default_and_shown_when_enabled()
    {
        var host = new PrimaryProfileHost(0.1m, Cfg);
        var start = EtToUtc(2026, 7, 22, 8, 25, 0);
        host.UpsertBar(Bar(0, start, start.AddMinutes(5), 100m, 100.1m, 100.0m, 100.0m), 0, start.AddMinutes(10));
        var profiles = host.Current!;

        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(0.1m, 14));
        var observed = new ObservedInstrumentSnapshot(
            "GCQ6", "id", "GCQ6", "COMEX", new DateTime(2026, 8, 27), 0.1m, "GC", "GCQ6", "COMEX", 0.1m, null);

        var off = engine.Publish(
            observed, "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, start.UtcDateTime, false, true, false, false, false, false, profiles, start.UtcDateTime,
            enableTpoParityDiagnostics: false);
        var vmOff = AuctionGpsCardMapper.FromSnapshot(off, false);
        Assert.DoesNotContain(vmOff.ProfileDetailLines, l => l.StartsWith("TPO MAX COUNT:", StringComparison.Ordinal));

        var on = engine.Publish(
            observed, "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, start.UtcDateTime, false, true, false, false, false, false, profiles, start.UtcDateTime,
            enableTpoParityDiagnostics: true);
        var vmOn = AuctionGpsCardMapper.FromSnapshot(on, false);
        Assert.Contains(vmOn.ProfileDetailLines, l => l.StartsWith("TPO MAX COUNT:", StringComparison.Ordinal));
        Assert.Contains(vmOn.ProfileDetailLines, l => l.StartsWith("TPO DEVELOPING INCLUDED:", StringComparison.Ordinal));
        Assert.Contains(vmOn.ProfileDetailLines, l => l.StartsWith("TPO DIAGNOSTIC VERSION:", StringComparison.Ordinal));
    }

    [Fact]
    public void Indicator_defaults_tpo_parity_diagnostics_off()
    {
        var root = FindRepoRoot();
        var src = File.ReadAllText(Path.Combine(root, "src", "GC.AuctionFlow", "Atas", "GcAuctionFlowIndicator.cs"));
        Assert.Contains("EnableTpoParityDiagnostics = false", src, StringComparison.Ordinal);
        Assert.Contains("EnableMboLifecycleProbe = false", src, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "GC.AuctionFlow.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
