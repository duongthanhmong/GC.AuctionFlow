using GC.AuctionFlow.Orderflow;

namespace GC.AuctionFlow.Probe;

/// <summary>
/// Resolves the aggressor side of a trade.
///
/// The engine read `MarketDataArg.IsAsk` / `IsBid` for the life of the project. Rithmic
/// never populates them — 24,214 recorded trades, zero with either flag set — so every
/// aggressor-dependent measurement reported Unavailable and that was mistaken for a
/// property of the feed. The side was there the whole time in `Direction`, carried
/// intact as far as `NewTradeObservation.Direction` and then discarded.
///
/// The Buy-to-ask mapping is measured rather than assumed, because it sets the sign of
/// every delta in the engine and an inverted sign is worse than no sign at all. Against
/// the recorded quotes, Buy traded at the offer 2,336 times and at the bid **zero** times.
///
/// `IsAsk` / `IsBid` are still honoured when present, so a feed that does populate them
/// keeps working unchanged.
/// </summary>
public static class TradeAggressorSide
{
    public const string BuyDirection = "Buy";
    public const string SellDirection = "Sell";

    public static AggressorSide Resolve(string? direction, bool isAsk, bool isBid)
    {
        // Explicit flags win where a feed provides them: they are the platform's own
        // statement about the touch, and Direction is a classification alongside it.
        if (isAsk && !isBid) return AggressorSide.Ask;
        if (isBid && !isAsk) return AggressorSide.Bid;

        if (string.Equals(direction, BuyDirection, StringComparison.OrdinalIgnoreCase))
            return AggressorSide.Ask;
        if (string.Equals(direction, SellDirection, StringComparison.OrdinalIgnoreCase))
            return AggressorSide.Bid;

        // Anything else — Between, an unmapped value, both flags set — stays Unknown.
        // Choosing a side here would fabricate the one measurement the engine refuses to
        // fabricate anywhere else.
        return AggressorSide.Unknown;
    }

    public static bool IsAskSide(AggressorSide side) => side == AggressorSide.Ask;

    public static bool IsBidSide(AggressorSide side) => side == AggressorSide.Bid;

    public static bool IsClassified(AggressorSide side) => side != AggressorSide.Unknown;
}
