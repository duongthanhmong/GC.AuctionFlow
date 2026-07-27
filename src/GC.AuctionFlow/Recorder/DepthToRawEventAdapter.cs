using ATAS.Indicators;
using GC.AuctionFlow.Recorder.Payloads;

namespace GC.AuctionFlow.Recorder;

/// <summary>
/// Turns depth and best-bid/ask callbacks into recorder drafts.
///
/// v1.2 §46.5 lists DOM changes among the things a live recorder must capture because no
/// amount of downloaded history contains them. Only trades were being written, so that
/// half of the foundation was being discarded as it arrived.
///
/// This records **only what the platform already delivers**. It issues no subscription and
/// requests no snapshot: `MarketDepthChanged` and `OnBestBidAskChanged` fire on their own,
/// and the P0-06D chart side effect was observed during an active MBO subscription, not
/// during passive receipt. The DOM snapshot pull and MBO remain out of scope for exactly
/// that reason — they are actions, and actions are what carried the risk.
/// </summary>
public static class DepthToRawEventAdapter
{
    /// <summary>Side as the feed reported it, without inferring one when it reported neither.</summary>
    public static string DeriveSide(bool isBid, bool isAsk) =>
        isBid && isAsk ? "Both"
        : isBid ? "Bid"
        : isAsk ? "Ask"
        : "Unknown";

    public static RawEventDraft? TryDepthDraft(
        MarketDataArg? depth,
        CallbackCaptureContext context,
        ObservedInstrumentIdentity instrument,
        Guid sessionId,
        Guid processId,
        long streamLocalCaptureSequence,
        string declaredMode,
        string modeProvenance,
        string declaredProvider,
        string providerProvenance,
        int contractEpoch = 1)
    {
        if (depth is null)
            return null;

        var payload = new DepthPayload(
            depth.Price,
            depth.Volume,
            depth.DataType.ToString(),
            depth.IsBid,
            depth.IsAsk,
            DeriveSide(depth.IsBid, depth.IsAsk));

        return Build(
            RawEventPayloadKind.Depth, payload, depth.Time, context, instrument,
            sessionId, processId, streamLocalCaptureSequence,
            declaredMode, modeProvenance, declaredProvider, providerProvenance, contractEpoch);
    }

    /// <summary>
    /// Best bid/ask.
    ///
    /// ATAS raises this with one side per call, so the opposite side is not known at this
    /// moment and is written as zero volume at zero price rather than as a fabricated
    /// quote. A consumer reads the side from the payload's own price/volume pair.
    /// </summary>
    public static RawEventDraft? TryBestBidAskDraft(
        MarketDataArg? quote,
        CallbackCaptureContext context,
        ObservedInstrumentIdentity instrument,
        Guid sessionId,
        Guid processId,
        long streamLocalCaptureSequence,
        string declaredMode,
        string modeProvenance,
        string declaredProvider,
        string providerProvenance,
        int contractEpoch = 1)
    {
        if (quote is null)
            return null;

        var payload = new BestBidAskPayload(
            bidPrice: quote.IsBid ? quote.Price : 0m,
            bidVolume: quote.IsBid ? quote.Volume : 0m,
            askPrice: quote.IsAsk ? quote.Price : 0m,
            askVolume: quote.IsAsk ? quote.Volume : 0m,
            rawDataType: quote.DataType.ToString());

        return Build(
            RawEventPayloadKind.BestBidAsk, payload, quote.Time, context, instrument,
            sessionId, processId, streamLocalCaptureSequence,
            declaredMode, modeProvenance, declaredProvider, providerProvenance, contractEpoch);
    }

    private static RawEventDraft Build(
        RawEventPayloadKind kind,
        RawEventPayload payload,
        DateTime sourceTime,
        CallbackCaptureContext context,
        ObservedInstrumentIdentity instrument,
        Guid sessionId,
        Guid processId,
        long streamLocalCaptureSequence,
        string declaredMode,
        string modeProvenance,
        string declaredProvider,
        string providerProvenance,
        int contractEpoch) =>
        new(
            RawEventRecorderVersions.RawEventRecorderSchemaVersion,
            sessionId,
            processId,
            RecorderStreamKind.Dom,
            context.CallbackSource,
            streamLocalCaptureSequence,
            context.CallbackInvocationSequence,
            callbackItemOrdinal: 0,
            subscriptionOrCaptureEpoch: null,
            contractEpoch,
            instrument,
            declaredMode,
            modeProvenance,
            declaredProvider,
            providerProvenance,
            sourceTime.Ticks,
            sourceTime.Kind,
            context.CallbackReceiveUtc,
            context.CallbackReceiveStopwatchTimestamp,
            context.CallbackManagedThreadId,
            kind,
            payload,
            // Depth callbacks carry no native sequence, and claiming one would make a gap
            // in the recording indistinguishable from a gap in the feed. An unspecified
            // source-time kind is flagged for the same reason.
            RecorderIntegrityFlags.NativeSequenceAbsent
            | (sourceTime.Kind == DateTimeKind.Unspecified
                ? RecorderIntegrityFlags.SourceTimeKindUnspecified
                : RecorderIntegrityFlags.None),
            nativeSequenceAvailable: false);
}
