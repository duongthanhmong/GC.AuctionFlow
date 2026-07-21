using System.Diagnostics;
using ATAS.Indicators;

namespace GC.AuctionFlow.Probe;

/// <summary>Thin ATAS → DepthObservation mapping. Copies primitives only.</summary>
public static class DepthAtasMapper
{
    public static (DepthObservation Obs, string SideDetail) Map(
        MarketDataArg depth,
        DepthCallbackSource source,
        long sequence,
        string instrumentIdentityKey)
    {
        var time = depth.Time;
        var price = depth.Price;
        var volume = depth.Volume;
        var rawDt = depth.DataType.ToString();
        var isBid = depth.IsBid;
        var isAsk = depth.IsAsk;
        var side = DepthSideClassifier.Classify(rawDt, isBid, isAsk, out var detail);
        var fp = DepthFingerprints.Core(time.Ticks, price, volume, rawDt, isBid, isAsk);

        var obs = new DepthObservation(
            callbackSource: source,
            localMonotonicSequence: sequence,
            sourceTimeTicks: time.Ticks,
            sourceDateTimeKind: time.Kind,
            receiveUtc: DateTime.UtcNow,
            receiveStopwatchTimestamp: Stopwatch.GetTimestamp(),
            callbackManagedThreadId: Environment.CurrentManagedThreadId,
            observedInstrumentIdentityKey: instrumentIdentityKey,
            price: price,
            volume: volume,
            rawDataType: rawDt,
            isBid: isBid,
            isAsk: isAsk,
            derivedSide: side,
            diagnosticFingerprint: fp);

        return (obs, detail);
    }
}
