using ATAS.Indicators;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Recorder.FanOut;
using GC.AuctionFlow.Recorder.Payloads;

namespace GC.AuctionFlow.Recorder;

/// <summary>
/// Maps ATAS trade payloads to primitive recorder contracts inside the callback boundary.
/// Never retains ATAS objects. Never reads cumulative constituent tick collections.
/// </summary>
public sealed class TradeToRawEventAdapter
{
    private readonly TradeStreamAtasMapper _cumIdSource;
    private long _streamLocal;

    public TradeToRawEventAdapter(TradeStreamAtasMapper cumIdSource)
    {
        _cumIdSource = cumIdSource ?? throw new ArgumentNullException(nameof(cumIdSource));
    }

    public long NextStreamLocal() => Interlocked.Increment(ref _streamLocal);

    public PrimitiveFanOutItem? TryMapNewTrade(
        MarketDataArg trade,
        CallbackCaptureContext context,
        int callbackItemOrdinal,
        ObservedInstrumentIdentity instrument,
        string declaredMode,
        string modeProvenance,
        string declaredProvider,
        string providerProvenance,
        Guid sessionId,
        Guid processId)
    {
        if (trade is null) return null;

        var time = trade.Time;
        var price = trade.Price;
        var volume = trade.Volume;
        var origin = trade.OriginPrice;
        var directionRaw = Convert.ToInt64(trade.Direction);
        var directionName = trade.Direction.ToString() ?? string.Empty;
        var dataTypeRaw = Convert.ToInt64(trade.DataType);
        var dataTypeName = trade.DataType.ToString() ?? string.Empty;
        var isAsk = trade.IsAsk;
        var isBid = trade.IsBid;
        var exchOid = trade.ExchangeOrderId;
        var aggrOid = trade.AggressorExchangeOrderId;
        var oi = trade.OpenInterest;

        var payload = new NewTradePayload(
            price, volume, origin,
            directionRaw, directionName,
            dataTypeRaw, dataTypeName,
            isAsk, isBid,
            exchOid, aggrOid, oi);

        var streamLocal = NextStreamLocal();
        return new PrimitiveFanOutItem(
            context,
            callbackItemOrdinal,
            streamLocal,
            instrument,
            time.Ticks,
            time.Kind,
            RawEventPayloadKind.NewTrade,
            payload,
            RecorderIntegrityFlags.NativeSequenceAbsent
            | (time.Kind == DateTimeKind.Unspecified ? RecorderIntegrityFlags.SourceTimeKindUnspecified : RecorderIntegrityFlags.None),
            nativeSequenceAvailable: false);
    }

    public PrimitiveFanOutItem? TryMapCumulative(
        CumulativeTrade trade,
        CallbackCaptureContext context,
        int callbackItemOrdinal,
        bool assignInstanceId,
        bool isUpdate,
        ObservedInstrumentIdentity instrument,
        string declaredMode,
        string modeProvenance,
        string declaredProvider,
        string providerProvenance,
        Guid sessionId,
        Guid processId)
    {
        if (trade is null) return null;

        // Lock: do not read, Count, enumerate, or retain cumulative constituent tick collections.
        var time = trade.Time;
        var volume = trade.Volume;
        var first = trade.FirstPrice;
        var last = trade.Lastprice;
        var directionRaw = Convert.ToInt64(trade.Direction);
        var directionName = trade.Direction.ToString() ?? string.Empty;

        long? processLocalId;
        if (assignInstanceId)
            processLocalId = _cumIdSource.GetOrAssignProcessLocalCumulativeId(trade);
        else if (_cumIdSource.TryGetProcessLocalCumulativeId(trade, out var existing))
            processLocalId = existing;
        else
            processLocalId = _cumIdSource.GetOrAssignProcessLocalCumulativeId(trade);

        RawEventPayload payload = isUpdate
            ? new CumulativeTradeUpdatePayload(
                volume, first, last, directionRaw, directionName,
                reportedTickCountAvailable: false,
                reportedTickCount: null,
                processLocalId,
                processLocalInstanceIdObserved: true)
            : new CumulativeTradeNewPayload(
                volume, first, last, directionRaw, directionName,
                reportedTickCountAvailable: false,
                reportedTickCount: null,
                processLocalId,
                processLocalInstanceIdObserved: true);

        var kind = isUpdate ? RawEventPayloadKind.CumulativeTradeUpdate : RawEventPayloadKind.CumulativeTradeNew;
        var streamLocal = NextStreamLocal();
        return new PrimitiveFanOutItem(
            context,
            callbackItemOrdinal,
            streamLocal,
            instrument,
            time.Ticks,
            time.Kind,
            kind,
            payload,
            RecorderIntegrityFlags.NativeSequenceAbsent
            | (time.Kind == DateTimeKind.Unspecified ? RecorderIntegrityFlags.SourceTimeKindUnspecified : RecorderIntegrityFlags.None),
            nativeSequenceAvailable: false);
    }

    public static RawEventDraft ToDraft(
        PrimitiveFanOutItem item,
        Guid sessionId,
        Guid processId,
        string declaredMode,
        string modeProvenance,
        string declaredProvider,
        string providerProvenance,
        int contractEpoch = 1) =>
        new(
            RawEventRecorderVersions.RawEventRecorderSchemaVersion,
            sessionId,
            processId,
            item.PayloadDiscriminator is RawEventPayloadKind.NewTrade
                or RawEventPayloadKind.CumulativeTradeNew
                or RawEventPayloadKind.CumulativeTradeUpdate
                ? RecorderStreamKind.Trade
                : RecorderStreamKind.Lifecycle,
            item.CallbackContext.CallbackSource,
            item.StreamLocalCaptureSequence,
            item.CallbackContext.CallbackInvocationSequence,
            item.CallbackItemOrdinal,
            subscriptionOrCaptureEpoch: null,
            contractEpoch,
            item.Instrument,
            declaredMode,
            modeProvenance,
            declaredProvider,
            providerProvenance,
            item.SourceTimeTicks,
            item.SourceDateTimeKind,
            item.CallbackContext.CallbackReceiveUtc,
            item.CallbackContext.CallbackReceiveStopwatchTimestamp,
            item.CallbackContext.CallbackManagedThreadId,
            item.PayloadDiscriminator,
            item.Payload,
            item.IntegrityFlags,
            item.NativeSequenceAvailable);

    public static RawEventDraft ToInvocationResultDraft(
        CallbackInvocationResultPayload payload,
        CallbackCaptureContext context,
        ObservedInstrumentIdentity instrument,
        Guid sessionId,
        Guid processId,
        string declaredMode,
        string modeProvenance,
        string declaredProvider,
        string providerProvenance) =>
        new(
            RawEventRecorderVersions.RawEventRecorderSchemaVersion,
            sessionId,
            processId,
            RecorderStreamKind.Integrity,
            context.CallbackSource,
            streamLocalCaptureSequence: 0,
            context.CallbackInvocationSequence,
            callbackItemOrdinal: 0,
            subscriptionOrCaptureEpoch: null,
            contractEpoch: 1,
            instrument,
            declaredMode,
            modeProvenance,
            declaredProvider,
            providerProvenance,
            sourceTimeTicks: context.CallbackReceiveUtc.Ticks,
            sourceDateTimeKind: DateTimeKind.Utc,
            context.CallbackReceiveUtc,
            context.CallbackReceiveStopwatchTimestamp,
            context.CallbackManagedThreadId,
            RawEventPayloadKind.CallbackInvocationResult,
            payload,
            RecorderIntegrityFlags.None,
            nativeSequenceAvailable: false);
}
