namespace GC.AuctionFlow.Probe;

/// <summary>
/// Immutable copied new-trade observation. Never retains ATAS MarketDataArg references.
/// Source Time is not converted to UTC.
/// </summary>
public sealed class NewTradeObservation
{
    public NewTradeObservation(
        TradeCallbackSource callbackSource,
        long localMonotonicSequence,
        long sourceTimeTicks,
        DateTimeKind sourceDateTimeKind,
        DateTime receiveUtc,
        long receiveStopwatchTimestamp,
        string observedInstrumentIdentityKey,
        string coreDiagnosticFingerprint,
        string? extendedDiagnosticFingerprint,
        decimal price,
        decimal volume,
        decimal originPrice,
        string direction,
        string dataType,
        bool isAsk,
        bool isBid,
        long? exchangeOrderId,
        long? aggressorExchangeOrderId,
        decimal? openInterest)
    {
        CallbackSource = callbackSource;
        LocalMonotonicSequence = localMonotonicSequence;
        SourceTimeTicks = sourceTimeTicks;
        SourceDateTimeKind = sourceDateTimeKind;
        ReceiveUtc = receiveUtc;
        ReceiveStopwatchTimestamp = receiveStopwatchTimestamp;
        ObservedInstrumentIdentityKey = observedInstrumentIdentityKey;
        CoreDiagnosticFingerprint = coreDiagnosticFingerprint;
        ExtendedDiagnosticFingerprint = extendedDiagnosticFingerprint;
        Price = price;
        Volume = volume;
        OriginPrice = originPrice;
        Direction = direction;
        DataType = dataType;
        IsAsk = isAsk;
        IsBid = isBid;
        ExchangeOrderId = exchangeOrderId;
        AggressorExchangeOrderId = aggressorExchangeOrderId;
        OpenInterest = openInterest;
    }

    public TradeCallbackSource CallbackSource { get; }
    public long LocalMonotonicSequence { get; }
    public long SourceTimeTicks { get; }
    public DateTimeKind SourceDateTimeKind { get; }
    public DateTime ReceiveUtc { get; }
    public long ReceiveStopwatchTimestamp { get; }
    public string ObservedInstrumentIdentityKey { get; }
    public string CoreDiagnosticFingerprint { get; }
    public string? ExtendedDiagnosticFingerprint { get; }
    public decimal Price { get; }
    public decimal Volume { get; }
    public decimal OriginPrice { get; }
    public string Direction { get; }
    public string DataType { get; }
    public bool IsAsk { get; }
    public bool IsBid { get; }
    public long? ExchangeOrderId { get; }
    public long? AggressorExchangeOrderId { get; }
    public decimal? OpenInterest { get; }
}
