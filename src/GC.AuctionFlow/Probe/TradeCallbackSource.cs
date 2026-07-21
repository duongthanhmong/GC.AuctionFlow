namespace GC.AuctionFlow.Probe;

/// <summary>ATAS trade callback origin. Stable string enum for artifacts.</summary>
public enum TradeCallbackSource
{
    OnNewTrade = 0,
    OnNewTradesBatch = 1,
    OnCumulativeTrade = 2,
    OnUpdateCumulativeTrade = 3
}
