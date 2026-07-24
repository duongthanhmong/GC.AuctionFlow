using GC.AuctionFlow.Core;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Runtime;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Profile;

public sealed class Phase1APrimaryProfileTests
{
    private static readonly PrimaryAuctionClockConfig Cfg = new();
    private static readonly PriceGrid Grid = new(0.1m);

    private static DateTimeOffset EtToUtc(int y, int m, int d, int hh, int mm, int ss = 0)
    {
        var local = new DateTime(y, m, d, hh, mm, ss, DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, AuctionTimezoneResolver.Resolve()));
    }

    [Fact]
    public void Clock_before_at_after_0820_and_nextday_rollover()
    {
        var clock = new PrimaryAuctionClock(Cfg);
        var before = clock.Resolve(EtToUtc(2026, 7, 22, 8, 19, 59));
        Assert.Equal("PI-2026-07-21", before.AuctionId);

        var at = clock.Resolve(EtToUtc(2026, 7, 22, 8, 20, 0));
        Assert.Equal("PI-2026-07-22", at.AuctionId);
        Assert.Equal(0, at.TpoPeriodIndex);

        var after = clock.Resolve(EtToUtc(2026, 7, 22, 8, 20, 1));
        Assert.Equal("PI-2026-07-22", after.AuctionId);

        var next = clock.Resolve(EtToUtc(2026, 7, 23, 8, 20, 0));
        Assert.Equal("PI-2026-07-23", next.AuctionId);
    }

    [Fact]
    public void Clock_DST_spring_forward_and_fall_back()
    {
        var clock = new PrimaryAuctionClock(Cfg);
        // US DST 2026: spring forward 2026-03-08; fall back 2026-11-01
        var spring = clock.Resolve(EtToUtc(2026, 3, 8, 9, 0, 0));
        Assert.Equal("PI-2026-03-08", spring.AuctionId);
        Assert.True(spring.AuctionEndUtc > spring.AuctionStartUtc);

        var fall = clock.Resolve(EtToUtc(2026, 11, 1, 9, 0, 0));
        Assert.Equal("PI-2026-11-01", fall.AuctionId);
        Assert.True((fall.AuctionEndUtc - fall.AuctionStartUtc).TotalHours is >= 23 and <= 25);
    }

    [Fact]
    public void Clock_machine_timezone_independent()
    {
        var clock = new PrimaryAuctionClock(Cfg);
        var utc = new DateTimeOffset(2026, 7, 22, 12, 20, 0, TimeSpan.Zero); // 08:20 EDT
        var a = clock.Resolve(utc);
        var b = clock.Resolve(utc);
        Assert.Equal(a.AuctionId, b.AuctionId);
        Assert.Equal(a.AuctionStartUtc, b.AuctionStartUtc);
    }

    [Fact]
    public void PriceGrid_alignment_and_invalid_tick()
    {
        Assert.Equal(23451L, Grid.ToTickIndex(2345.1m));
        Assert.Equal(2345.1m, Grid.ToPrice(23451L));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PriceGrid(0m));
        Assert.Throws<ArgumentException>(() => Grid.ToTickIndex(2345.15m));
    }

    [Fact]
    public void Tpo_one_period_once_two_periods_twice_and_range_fill()
    {
        var clock = new PrimaryAuctionClock(Cfg);
        var start = EtToUtc(2026, 7, 22, 8, 20, 0);
        var bars = new List<ProfileBarObservation>
        {
            Bar(0, start, start.AddMinutes(1), 100m, 100.2m, 100.0m, 100.1m),
            Bar(1, start.AddMinutes(5), start.AddMinutes(6), 100m, 100.2m, 100.0m, 100.1m), // same period, same range
            Bar(2, start.AddMinutes(30), start.AddMinutes(31), 100m, 100.2m, 100.0m, 100.1m) // next period
        };
        var point = clock.Resolve(start);
        var snap = ClassicTpoEngine.Build(
            point.AuctionId, point.AuctionStartUtc, point.AuctionEndUtc,
            bars, clock, Grid, 0.70m, null, start.AddMinutes(40));

        Assert.Equal(2, snap.PriceLevelTpoCounts[Grid.ToTickIndex(100.0m)]);
        Assert.Equal(2, snap.PriceLevelTpoCounts[Grid.ToTickIndex(100.1m)]);
        Assert.Equal(2, snap.PriceLevelTpoCounts[Grid.ToTickIndex(100.2m)]);
        Assert.True(snap.DevelopingPeriodIndex is not null || snap.CompletedPeriodCount >= 1);
        Assert.Equal(100.2m, snap.ProfileHigh);
        Assert.Equal(100.0m, snap.ProfileLow);
    }

    [Fact]
    public void Tpo_poc_tie_uses_midpoint_then_lower_tick()
    {
        var counts = new Dictionary<long, decimal>
        {
            [Grid.ToTickIndex(100.0m)] = 5m,
            [Grid.ToTickIndex(100.5m)] = 5m,
            [Grid.ToTickIndex(101.0m)] = 5m
        };
        var poc = PocSelector.SelectPocTick(counts, previousPocTick: null);
        Assert.Equal(Grid.ToTickIndex(100.5m), poc);
    }

    [Fact]
    public void Volume_exact_accumulation_no_smear_and_unavailable_partial()
    {
        var start = EtToUtc(2026, 7, 22, 8, 20, 0);
        var exact = new ProfileBarObservation(
            0, start, start.AddMinutes(1), 100m, 100.2m, 100.0m, 100.1m, 999m,
            new[]
            {
                new PriceVolumeObservation(100.0m, 10m, null, null, 1, "t"),
                new PriceVolumeObservation(100.1m, 30m, null, null, 1, "t")
            },
            PriceVolumeCapability.Exact, true, true, 1);

        var clock = new PrimaryAuctionClock(Cfg);
        var point = clock.Resolve(start);
        var vol = VolumeProfileEngine.Build(
            point.AuctionId, point.AuctionStartUtc, point.AuctionEndUtc,
            new[] { exact }, Grid, 0.70m, null);
        Assert.Equal(40m, vol.TotalExecutedVolume);
        Assert.Equal(100.1m, vol.VolumePoc);
        Assert.DoesNotContain(vol.KnownLimitations, x => x.Contains("SMEAR", StringComparison.Ordinal) && x.StartsWith("APPLIED", StringComparison.Ordinal));

        var noPv = new ProfileBarObservation(
            0, start, start.AddMinutes(1), 100m, 100.2m, 100.0m, 100.1m, 999m,
            Array.Empty<PriceVolumeObservation>(), PriceVolumeCapability.Unavailable, true, true, 1);
        var unavailable = VolumeProfileEngine.Build(
            point.AuctionId, point.AuctionStartUtc, point.AuctionEndUtc,
            new[] { noPv }, Grid, 0.70m, null);
        Assert.Equal(PriceVolumeCapability.Unavailable, unavailable.PriceVolumeCapability);
        Assert.Null(unavailable.VolumePoc);
        Assert.Contains("PRICE_VOLUME_DATA_UNAVAILABLE", unavailable.KnownLimitations);
    }

    [Fact]
    public void Volume_duplicate_bar_replace_does_not_double_count()
    {
        var host = new PrimaryProfileHost(0.1m, Cfg);
        var start = EtToUtc(2026, 7, 22, 8, 25, 0);
        var v1 = new ProfileBarObservation(
            5, start, start.AddMinutes(1), 100m, 100.1m, 100.0m, 100.0m, 50m,
            new[] { new PriceVolumeObservation(100.0m, 10m, null, null, 1, "t") },
            PriceVolumeCapability.Exact, true, true, 1);
        var v2 = new ProfileBarObservation(
            5, start, start.AddMinutes(1), 100m, 100.1m, 100.0m, 100.0m, 50m,
            new[] { new PriceVolumeObservation(100.0m, 10m, null, null, 1, "t") },
            PriceVolumeCapability.Exact, true, true, 2);
        host.UpsertBar(v1, 5, start.AddMinutes(2));
        var set = host.UpsertBar(v2, 5, start.AddMinutes(2));
        Assert.Equal(10m, set.CurrentAuction!.VolumeProfile!.TotalExecutedVolume);
    }

    [Fact]
    public void ValueArea_symmetric_skew_tie_fraction_invalid()
    {
        var map = new Dictionary<long, decimal>
        {
            [10] = 1, [11] = 2, [12] = 10, [13] = 2, [14] = 1
        };
        var va = ValueAreaCalculator.Calculate(map, 12, 0.70m, new PriceGrid(0.1m));
        Assert.True(va.ValTick <= 12 && va.VahTick >= 12);
        Assert.Throws<ArgumentOutOfRangeException>(() => ValueAreaCalculator.Calculate(map, 12, 0m, Grid));
        Assert.Throws<ArgumentOutOfRangeException>(() => ValueAreaCalculator.Calculate(map, 12, 1.1m, Grid));
    }

    [Fact]
    public void Host_rollover_current_to_previous_and_idempotent_rebuild()
    {
        var host = new PrimaryProfileHost(0.1m, Cfg);
        var d1 = EtToUtc(2026, 7, 21, 10, 0, 0);
        var d2 = EtToUtc(2026, 7, 22, 10, 0, 0);
        host.UpsertBar(Bar(0, d1, d1.AddMinutes(1), 100m, 100.1m, 100.0m, 100.0m), 1, d1.AddHours(1));
        var mid = host.UpsertBar(Bar(1, d2, d2.AddMinutes(1), 101m, 101.1m, 101.0m, 101.0m), 1, d2.AddHours(1));
        Assert.NotNull(mid.CurrentAuction);
        Assert.NotNull(mid.PreviousAuction);
        Assert.NotEqual(mid.CurrentAuction!.AuctionId, mid.PreviousAuction!.AuctionId);

        var again = host.Rebuild(1, d2.AddHours(1));
        Assert.Equal(mid.CurrentAuction.AuctionId, again.CurrentAuction!.AuctionId);
        Assert.Equal(mid.CurrentAuction.TpoProfile!.TpoPoc, again.CurrentAuction.TpoProfile!.TpoPoc);
    }

    [Fact]
    public void Host_lookahead_rejected_and_settings_reset()
    {
        var host = new PrimaryProfileHost(0.1m, Cfg);
        var t = EtToUtc(2026, 7, 22, 10, 0, 0);
        host.UpsertBar(Bar(5, t, t.AddMinutes(1), 100m, 100.1m, 100.0m, 100.0m), evaluationBarIndex: 3, t);
        Assert.Contains(host.RevisionEvents, e => e.StartsWith("LOOKAHEAD_REJECTED", StringComparison.Ordinal));
        host.Reset();
        Assert.Null(host.Current?.CurrentAuction);
    }

    [Fact]
    public void Runtime_ProfileReady_removes_PROFILE_NOT_READY_but_BidAsk_Roll_keep_Degraded()
    {
        var host = new PrimaryProfileHost(0.1m, Cfg);
        var start = EtToUtc(2026, 7, 22, 8, 25, 0);
        var bar = new ProfileBarObservation(
            0, start, start.AddMinutes(1), 100m, 100.2m, 100.0m, 100.1m, 40m,
            new[]
            {
                new PriceVolumeObservation(100.0m, 10m, null, null, 1, "t"),
                new PriceVolumeObservation(100.1m, 30m, null, null, 1, "t")
            },
            PriceVolumeCapability.Exact, true, true, 1);
        var profiles = host.UpsertBar(bar, 0, start.AddMinutes(5));
        Assert.Equal(AuctionProfileState.Ready, profiles.CurrentAuction!.ProfileState);

        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(0.1m, 14));
        var observed = new ObservedInstrumentSnapshot(
            "GCQ6", "id", "GCQ6", "COMEX", new DateTime(2026, 8, 27), 0.1m, "GC", "GCQ6", "COMEX", 0.1m, null);
        var snap = engine.Publish(
            observed, "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            tradeObserved: true, lastTradeCallbackUtc: start.UtcDateTime,
            rawRecorderMasterEnabled: false, tradeRecordingEnabled: true,
            recorderAccepting: false, recorderFaulted: false, recorderSessionPresent: false,
            indicatorDisposed: false, profiles: profiles, timestampUtc: start.UtcDateTime);

        Assert.DoesNotContain(DataGateReasonCodes.ProfileNotReady, snap.DataGate.AllReasonCodes);
        Assert.Equal(DataState.Degraded, snap.DataGate.DataState); // BidAsk + Roll still degrade
        Assert.Contains(DataGateReasonCodes.BidAskUnknownOrPartial, snap.DataGate.AllReasonCodes);
        Assert.Contains(DataGateReasonCodes.RollStateUnknown, snap.DataGate.AllReasonCodes);

        var vm = AuctionGpsCardMapper.FromSnapshot(snap, false);
        Assert.Equal("PROFILE: READY", vm.ProfileLine);
        Assert.Contains(vm.ProfileDetailLines, l => l.StartsWith("TPO POC:", StringComparison.Ordinal));
        Assert.Contains(vm.ProfileDetailLines, l => l.StartsWith("VPOC:", StringComparison.Ordinal));
        Assert.DoesNotContain(vm.AllLines(true), l => l.Contains("FAR", StringComparison.Ordinal));
        Assert.DoesNotContain(vm.AllLines(true), l => l.Contains("ENTRY", StringComparison.Ordinal));
    }

    [Fact]
    public void Gps_Partial_text_and_overlay_viewmodel_only()
    {
        var host = new PrimaryProfileHost(0.1m, Cfg);
        var start = EtToUtc(2026, 7, 22, 8, 25, 0);
        var bar = Bar(0, start, start.AddMinutes(1), 100m, 100.2m, 100.0m, 100.1m);
        var profiles = host.UpsertBar(bar, 0, start.AddMinutes(5));
        Assert.Equal(AuctionProfileState.Partial, profiles.CurrentAuction!.ProfileState);

        var engine = new GcaeRuntimeEngine(new RuntimeGateConfig(0.1m, 14));
        var observed = new ObservedInstrumentSnapshot(
            "GCQ6", "id", "GCQ6", "COMEX", new DateTime(2026, 8, 27), 0.1m, "GC", "GCQ6", "COMEX", 0.1m, null);
        var snap = engine.Publish(
            observed, "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, start.UtcDateTime, false, true, false, false, false, false, profiles, start.UtcDateTime);

        var vm = AuctionGpsCardMapper.FromSnapshot(snap, false);
        Assert.Equal("PROFILE: PARTIAL", vm.ProfileLine);
        Assert.Contains(vm.ProfileDetailLines, l => l.Contains("VPOC: UNAVAILABLE", StringComparison.Ordinal));

        var overlay = PrimaryProfileOverlayViewModel.FromProfiles(profiles, showPrevious: true);
        Assert.Contains(overlay.Levels, l => l.Label.Contains("TPO POC", StringComparison.Ordinal));
        Assert.DoesNotContain(overlay.Levels, l => l.Label.Contains("VPOC", StringComparison.Ordinal));
    }

    [Fact]
    public void Indicator_source_has_no_dataseries_writes_and_mbo_still_default_off()
    {
        var root = FindRepoRoot();
        var src = File.ReadAllText(Path.Combine(root, "src", "GC.AuctionFlow", "Atas", "GcAuctionFlowIndicator.cs"));
        Assert.Contains("EnableCompositeProfile = false", src, StringComparison.Ordinal);
        Assert.Contains("EnableMboLifecycleProbe = false", src, StringComparison.Ordinal);
        Assert.DoesNotMatch(new System.Text.RegularExpressions.Regex(@"^\s*this\s*\[\s*bar\s*\]\s*=", System.Text.RegularExpressions.RegexOptions.Multiline), src);
        Assert.DoesNotContain("AuctionEpisode", src, StringComparison.Ordinal);
        Assert.DoesNotContain("StructuralReferenceEngine", src, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductionThesis", src, StringComparison.Ordinal);
    }

    private static ProfileBarObservation Bar(
        int idx, DateTimeOffset start, DateTimeOffset end, decimal o, decimal h, decimal l, decimal c) =>
        new(idx, start, end, o, h, l, c, 1m,
            Array.Empty<PriceVolumeObservation>(), PriceVolumeCapability.Unavailable, true, true, 1);

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
