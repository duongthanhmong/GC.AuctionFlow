using GC.AuctionFlow.Probe;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Probe;

/// <summary>
/// Separating live trades from replayed history arriving on the same callbacks.
///
/// On indicator add, ATAS delivers a backlog through the real-time trade callbacks. One
/// recorded session held 853 minutes of exchange time written in 3.8 minutes of wall clock
/// — about 224x real time — and the engine consumed all of it as if it were the present.
/// Episodes formed, orderflow accumulated and delta moved over fourteen hours of history
/// compressed into seconds.
///
/// It was invisible from inside the engine, and it fooled the first analysis of the
/// recording too: grouping by the time the recorder *wrote* each trade rendered the backlog
/// as a 47-point collapse that never happened. By exchange source time no minute exceeded
/// 8.1 points, and an external chart of the same instrument showed no such move.
///
/// No threshold is chosen. The boundary is when observation began, which `LIVE_ONLY`
/// already defines.
/// </summary>
public sealed class TradeObservationFreshnessTests
{
    private static readonly DateTime Start =
        new(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);

    private static long Ticks(DateTime utc) => utc.Ticks;

    // ========== A: the boundary ==========

    [Fact]
    public void A01_A_trade_after_start_is_live() =>
        Assert.Equal(
            TradeFreshness.Live,
            TradeObservationFreshness.Classify(Ticks(Start.AddSeconds(1)), Start));

    /// <summary>The case that was corrupting the engine: hours-old prints arriving now.</summary>
    [Fact]
    public void A02_A_trade_from_before_start_is_replay() =>
        Assert.Equal(
            TradeFreshness.PreStartReplay,
            TradeObservationFreshness.Classify(Ticks(Start.AddHours(-14)), Start));

    /// <summary>Exactly at the boundary did not precede it.</summary>
    [Fact]
    public void A03_A_trade_stamped_at_start_is_live() =>
        Assert.Equal(
            TradeFreshness.Live, TradeObservationFreshness.Classify(Ticks(Start), Start));

    [Fact]
    public void A04_One_tick_before_start_is_replay() =>
        Assert.Equal(
            TradeFreshness.PreStartReplay,
            TradeObservationFreshness.Classify(Ticks(Start) - 1, Start));

    // ========== B: nothing is guessed ==========

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void B01_No_source_time_is_unknown(long ticks) =>
        Assert.Equal(TradeFreshness.Unknown, TradeObservationFreshness.Classify(ticks, Start));

    [Fact]
    public void B02_Before_observation_starts_nothing_can_be_classified() =>
        Assert.Equal(
            TradeFreshness.Unknown,
            TradeObservationFreshness.Classify(Ticks(Start), observationStartUtc: default));

    [Fact]
    public void B03_An_impossible_source_time_is_unknown() =>
        Assert.Equal(
            TradeFreshness.Unknown,
            TradeObservationFreshness.Classify(DateTime.MaxValue.Ticks + 1L, Start));

    // ========== C: what may drive the live chain ==========

    /// <summary>
    /// Only replay is excluded. `Unknown` is admitted deliberately: a feed with no usable
    /// source time would otherwise be shut out of the entire engine, which is a larger
    /// failure than admitting a trade whose age cannot be established.
    /// </summary>
    [Fact]
    public void C01_Only_replay_is_excluded()
    {
        Assert.True(TradeObservationFreshness.MayDriveLiveAnalysis(TradeFreshness.Live));
        Assert.True(TradeObservationFreshness.MayDriveLiveAnalysis(TradeFreshness.Unknown));
        Assert.False(TradeObservationFreshness.MayDriveLiveAnalysis(TradeFreshness.PreStartReplay));
    }

    /// <summary>
    /// The realistic shape of the defect: a backlog spanning most of a day, delivered in
    /// seconds. Every one of these must be refused, and the live prints among them kept.
    /// </summary>
    [Fact]
    public void C02_A_full_day_backlog_is_refused_and_live_prints_survive()
    {
        var backlog = Enumerable.Range(1, 840)
            .Select(m => TradeObservationFreshness.Classify(Ticks(Start.AddMinutes(-m)), Start))
            .ToArray();

        Assert.All(backlog, f => Assert.Equal(TradeFreshness.PreStartReplay, f));
        Assert.DoesNotContain(backlog, TradeObservationFreshness.MayDriveLiveAnalysis);

        var live = TradeObservationFreshness.Classify(Ticks(Start.AddSeconds(30)), Start);
        Assert.True(TradeObservationFreshness.MayDriveLiveAnalysis(live));
    }

    // ========== D: the live chain actually gates on it ==========

    /// <summary>
    /// The gate has to be on both paths. Episodes and orderflow each consume trades
    /// directly, and gating only one would leave delta and CVD accumulating over replay
    /// while episodes ignored it — a disagreement worse than either failure alone.
    /// </summary>
    [Fact]
    public void D01_Both_live_paths_reject_pre_start_replay()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src")))
            dir = dir.Parent;
        Assert.NotNull(dir);

        var text = File.ReadAllText(Path.Combine(
            dir!.FullName, "src", "GC.AuctionFlow", "Atas", "GcAuctionFlowIndicator.cs"));

        foreach (var method in new[] { "TryProcessEpisodeTrade", "TryProcessOrderflowTrade" })
        {
            var start = text.IndexOf("private void " + method, StringComparison.Ordinal);
            Assert.True(start >= 0, method + " not found");

            var end = text.IndexOf("\n    private", start + 1, StringComparison.Ordinal);
            var body = end < 0 ? text[start..] : text[start..end];

            Assert.Contains("MayDriveLiveAnalysis", body, StringComparison.Ordinal);
        }
    }
}
