using System.Diagnostics;
using ATAS.DataFeedsCore;

namespace GC.AuctionFlow.Probe;

/// <summary>
/// Maps ATAS MarketByOrder → immutable primitives. Copies during enumeration; retains no ATAS refs.
/// </summary>
public static class MboAtasMapper
{
    public static MboObservation Map(
        MarketByOrder mbo,
        long subscriptionEpoch,
        long localSequence,
        DateTime receiveUtc,
        long receiveStopwatchTimestamp,
        int callbackManagedThreadId,
        string instrumentIdentityKey)
    {
        ArgumentNullException.ThrowIfNull(mbo);

        string? securityCode = null;
        string? securityId = null;
        string? exchange = null;
        try
        {
            var sec = mbo.Security;
            if (sec is not null)
            {
                securityCode = sec.Code;
                securityId = sec.SecurityId;
                exchange = sec.Exchange;
            }
        }
        catch
        {
            // identity optional
        }

        var typeNumeric = (int)mbo.Type;
        var typeName = mbo.Type.ToString();
        var knownType = MboKnownRawUpdateTypes.TryGetKnownName(typeNumeric, out _);
        var resolvedType = MboKnownRawUpdateTypes.Resolve(typeNumeric, typeName);

        var sideNumeric = (int)mbo.Side;
        var sideName = MboKnownRawSideTypes.TryGetKnownName(sideNumeric, out var sn)
            ? sn
            : mbo.Side.ToString();
        var derived = MboSideClassifier.Classify(sideNumeric, out _);

        var time = mbo.Time;
        var fingerprint = MboFingerprints.Core(
            time.Ticks, mbo.ExchangeOrderId, typeNumeric, sideNumeric, mbo.Price, mbo.Volume, mbo.Priority);

        return new MboObservation(
            callbackSource: MboCallbackSource.OnMarketByOrdersChangedBatch,
            subscriptionEpoch: subscriptionEpoch,
            localMonotonicSequence: localSequence,
            receiveUtc: receiveUtc,
            receiveStopwatchTimestamp: receiveStopwatchTimestamp,
            callbackManagedThreadId: callbackManagedThreadId,
            sourceTimeTicks: time.Ticks,
            sourceDateTimeKind: time.Kind,
            instrumentIdentityKey: instrumentIdentityKey,
            securityCode: securityCode,
            securityId: securityId,
            exchange: exchange,
            rawTypeName: resolvedType.RawName,
            rawTypeNumeric: resolvedType.RawNumericValue,
            rawTypeIsKnownEnumMember: knownType,
            rawSideName: sideName,
            rawSideNumeric: sideNumeric,
            derivedSide: derived,
            exchangeOrderId: mbo.ExchangeOrderId,
            price: mbo.Price,
            volume: mbo.Volume,
            priority: mbo.Priority,
            diagnosticFingerprint: fingerprint);
    }

    public static (DateTime ReceiveUtc, long StopwatchTs, int ThreadId) CaptureReceiveContext() =>
        (DateTime.UtcNow, Stopwatch.GetTimestamp(), Environment.CurrentManagedThreadId);
}
