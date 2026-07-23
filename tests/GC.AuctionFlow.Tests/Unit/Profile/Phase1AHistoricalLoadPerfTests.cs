using GC.AuctionFlow.Profile;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Profile;

public sealed class Phase1AHistoricalLoadPerfTests
{
    private static readonly PrimaryAuctionClockConfig Cfg = new();

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
    public void Deferred_historical_ingest_then_final_rebuild_equals_eager_rebuild()
    {
        var start = EtToUtc(2026, 7, 22, 8, 20, 0);
        var bars = new List<ProfileBarObservation>();
        for (var i = 0; i < 48; i++)
        {
            var s = start.AddMinutes(5 * i);
            bars.Add(Bar(i, s, s.AddMinutes(5), 100m, 100.2m + (i % 5) * 0.1m, 99.8m, 100.0m));
        }

        var eval = start.AddMinutes(5 * 47);
        var last = bars[^1].BarIndex;

        var eager = new PrimaryProfileHost(0.1m, Cfg);
        foreach (var b in bars)
            eager.UpsertBar(b, last, eval, rebuildNow: true);

        var deferred = new PrimaryProfileHost(0.1m, Cfg);
        for (var i = 0; i < bars.Count - 1; i++)
            deferred.UpsertBar(bars[i], bars[i].BarIndex, eval, rebuildNow: false);
        deferred.UpsertBar(bars[^1], last, eval, rebuildNow: true);

        var a = eager.Current!.CurrentAuction!.TpoProfile!;
        var bSnap = deferred.Current!.CurrentAuction!.TpoProfile!;
        Assert.Equal(a.TpoPoc, bSnap.TpoPoc);
        Assert.Equal(a.TpoVal, bSnap.TpoVal);
        Assert.Equal(a.TpoVah, bSnap.TpoVah);
        Assert.Equal(
            a.PriceLevelTpoCounts.OrderBy(kv => kv.Key).Select(kv => (kv.Key, kv.Value)),
            bSnap.PriceLevelTpoCounts.OrderBy(kv => kv.Key).Select(kv => (kv.Key, kv.Value)));
    }

    [Fact]
    public void Bar_replace_during_live_forces_rebuild_path_via_ContainsBar()
    {
        var host = new PrimaryProfileHost(0.1m, Cfg);
        var start = EtToUtc(2026, 7, 22, 8, 20, 0);
        Assert.False(host.ContainsBar(0));
        host.UpsertBar(Bar(0, start, start.AddMinutes(5), 100m, 100.5m, 100.0m, 100.0m, 1), 0, start.AddMinutes(5), rebuildNow: true);
        Assert.True(host.ContainsBar(0));
        Assert.Equal(100.5m, host.Current!.CurrentAuction!.TpoProfile!.ProfileHigh);

        // Simulate ATAS re-calling historical bar after it exists → rebuildNow true in indicator.
        host.UpsertBar(Bar(0, start, start.AddMinutes(5), 100m, 100.1m, 100.0m, 100.0m, 2), 0, start.AddMinutes(5), rebuildNow: true);
        Assert.Equal(100.1m, host.Current!.CurrentAuction!.TpoProfile!.ProfileHigh);
    }

    [Fact]
    public void Indicator_defers_historical_rebuild_and_publish()
    {
        var root = FindRepoRoot();
        var src = File.ReadAllText(Path.Combine(root, "src", "GC.AuctionFlow", "Atas", "GcAuctionFlowIndicator.cs"));
        Assert.Contains("rebuildNow: rebuildNow", src, StringComparison.Ordinal);
        Assert.Contains("if (bar >= CurrentBar)", src, StringComparison.Ordinal);
        Assert.Contains("PublishRuntimeSnapshot()", src, StringComparison.Ordinal);
        Assert.Contains("HISTORICAL_INGEST_DEFERRED_REBUILD", File.ReadAllText(
            Path.Combine(root, "src", "GC.AuctionFlow", "Profile", "PrimaryProfileHost.cs")), StringComparison.Ordinal);
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
