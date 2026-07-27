using GC.AuctionFlow.Orderflow;
using GC.AuctionFlow.Probe;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Probe;

/// <summary>
/// Resolving the aggressor side of a trade.
///
/// The engine read `IsAsk` / `IsBid` for the life of the project. Rithmic never populates
/// them — 24,214 recorded trades, zero with either set — so every aggressor-dependent
/// measurement reported Unavailable, and that was mistaken for a property of the feed. The
/// side was in `Direction` the whole time, carried as far as `NewTradeObservation` and then
/// discarded one line before use.
///
/// The Buy-to-ask mapping was measured against recorded quotes rather than assumed: Buy
/// traded at the offer 2,336 times and at the bid zero times. That measurement matters
/// more than any other assertion here, because an inverted sign flips every delta in the
/// engine and would be worse than having no side at all.
/// </summary>
public sealed class TradeAggressorSideTests
{
    // ========== A: the mapping, and its sign ==========

    [Fact]
    public void A01_Buy_is_the_ask_side() =>
        Assert.Equal(
            AggressorSide.Ask,
            TradeAggressorSide.Resolve("Buy", isAsk: false, isBid: false));

    [Fact]
    public void A02_Sell_is_the_bid_side() =>
        Assert.Equal(
            AggressorSide.Bid,
            TradeAggressorSide.Resolve("Sell", isAsk: false, isBid: false));

    /// <summary>
    /// The inversion guard, stated as its own test because it is the failure that would do
    /// the most damage and the least visibly: every delta, every effort-versus-result
    /// vector and every imbalance would keep producing plausible numbers with the sign
    /// reversed.
    /// </summary>
    [Fact]
    public void A03_The_mapping_is_not_inverted()
    {
        Assert.NotEqual(
            AggressorSide.Bid, TradeAggressorSide.Resolve("Buy", false, false));
        Assert.NotEqual(
            AggressorSide.Ask, TradeAggressorSide.Resolve("Sell", false, false));
    }

    [Fact]
    public void A04_Direction_matching_ignores_case() =>
        Assert.Equal(
            AggressorSide.Ask, TradeAggressorSide.Resolve("buy", false, false));

    // ========== B: nothing is invented ==========

    /// <summary>
    /// Between, an unmapped value, or nothing at all stays Unknown. Choosing a side here
    /// would fabricate the one measurement the engine refuses to fabricate anywhere else,
    /// and it would be indistinguishable downstream from an observed one.
    /// </summary>
    [Theory]
    [InlineData("Between")]
    [InlineData("Unknown")]
    [InlineData("")]
    [InlineData(null)]
    public void B01_An_unmapped_direction_stays_unknown(string? direction) =>
        Assert.Equal(
            AggressorSide.Unknown, TradeAggressorSide.Resolve(direction, false, false));

    /// <summary>Both flags set describes no single aggressor, so it resolves to neither.</summary>
    [Fact]
    public void B02_Both_flags_set_is_unknown() =>
        Assert.Equal(
            AggressorSide.Unknown, TradeAggressorSide.Resolve(null, isAsk: true, isBid: true));

    [Fact]
    public void B03_Unknown_is_not_classified()
    {
        Assert.False(TradeAggressorSide.IsClassified(AggressorSide.Unknown));
        Assert.False(TradeAggressorSide.IsAskSide(AggressorSide.Unknown));
        Assert.False(TradeAggressorSide.IsBidSide(AggressorSide.Unknown));
    }

    // ========== C: a feed that does populate the flags keeps working ==========

    [Fact]
    public void C01_Explicit_flags_win_over_direction()
    {
        Assert.Equal(AggressorSide.Ask, TradeAggressorSide.Resolve("Sell", isAsk: true, isBid: false));
        Assert.Equal(AggressorSide.Bid, TradeAggressorSide.Resolve("Buy", isAsk: false, isBid: true));
    }

    [Fact]
    public void C02_Flags_alone_still_resolve()
    {
        Assert.Equal(AggressorSide.Ask, TradeAggressorSide.Resolve(null, isAsk: true, isBid: false));
        Assert.Equal(AggressorSide.Bid, TradeAggressorSide.Resolve(null, isAsk: false, isBid: true));
    }

    // ========== E: no path may resolve the side by itself ==========

    /// <summary>
    /// Two paths had the same defect independently — the episode event and the executed
    /// orderflow event each read IsAsk/IsBid alone, and fixing one left six modules still
    /// unclassified. A third copy would fail the same way and just as quietly, so the
    /// resolution has to stay in one place.
    /// </summary>
    [Fact]
    public void E01_No_source_file_resolves_the_side_on_its_own()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src")))
            dir = dir.Parent;
        Assert.NotNull(dir);

        var root = Path.Combine(dir!.FullName, "src", "GC.AuctionFlow");
        var offenders = new List<string>();

        foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            // The resolver itself is where this logic belongs.
            if (file.EndsWith("TradeAggressorSide.cs", StringComparison.Ordinal)) continue;

            foreach (var line in File.ReadAllLines(file))
            {
                var code = line.TrimStart();
                if (code.StartsWith("//", StringComparison.Ordinal)
                    || code.StartsWith("///", StringComparison.Ordinal))
                    continue;

                // The exact shape of the bug: deciding classification from the flags alone.
                if (code.Contains("IsAsk || obs.IsBid", StringComparison.Ordinal)
                    || code.Contains("IsAsk || trade.IsBid", StringComparison.Ordinal))
                    offenders.Add(Path.GetFileName(file) + ": " + code);
            }
        }

        Assert.True(offenders.Count == 0,
            "these resolve the aggressor side without TradeAggressorSide, so a feed that "
            + "reports side only in Direction goes unclassified: "
            + string.Join(" | ", offenders));
    }

    // ========== D: the episode event actually uses it ==========

    /// <summary>
    /// The regression that mattered: a trade carrying only a Direction must arrive at the
    /// episode registry classified. Before this, `classified` was `IsAsk || IsBid` and the
    /// whole aggressor chain died here.
    /// </summary>
    [Fact]
    public void D01_A_direction_only_trade_reaches_the_episode_classified()
    {
        var observation = new NewTradeObservation(
            callbackSource: TradeCallbackSource.OnNewTrade,
            localMonotonicSequence: 1,
            sourceTimeTicks: 638_000_000_000_000_000L,
            sourceDateTimeKind: DateTimeKind.Unspecified,
            receiveUtc: new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc),
            receiveStopwatchTimestamp: 1,
            observedInstrumentIdentityKey: "GCQ6",
            coreDiagnosticFingerprint: "core",
            extendedDiagnosticFingerprint: "ext",
            price: 4089.3m,
            volume: 1m,
            originPrice: 4089.3m,
            direction: "Sell",
            dataType: "Trade",
            isAsk: false,
            isBid: false,
            exchangeOrderId: 1,
            aggressorExchangeOrderId: 2,
            openInterest: 0m);

        var evt = GC.AuctionFlow.Episode.EpisodeTradeEvent.TryFromNewTrade(
            observation, tickSize: 0.1m, dataEpoch: "E1", timestampPolicyVersion: "TS_V1",
            toTick: p => (long)(p / 0.1m));

        Assert.NotNull(evt);
        Assert.True(evt!.AggressorClassified);
        Assert.True(evt.IsBid);
        Assert.False(evt.IsAsk);
    }
}
