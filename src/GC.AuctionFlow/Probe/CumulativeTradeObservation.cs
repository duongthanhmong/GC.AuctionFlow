namespace GC.AuctionFlow.Probe;

/// <summary>
/// Immutable copied cumulative-trade observation. Never retains CumulativeTrade or Ticks list references.
/// Source Time is not converted to UTC. Process-local instance ID is never an exchange ID.
/// </summary>
public sealed class CumulativeTradeObservation
{
    public CumulativeTradeObservation(
        TradeCallbackSource callbackSource,
        long localMonotonicSequence,
        long sourceTimeTicks,
        DateTimeKind sourceDateTimeKind,
        DateTime receiveUtc,
        long receiveStopwatchTimestamp,
        string observedInstrumentIdentityKey,
        string valueFingerprint,
        decimal volume,
        decimal firstPrice,
        decimal lastPrice,
        string direction,
        int copiedTickCount,
        long? processLocalInstanceId,
        bool processLocalInstanceIdObserved,
        IReadOnlyList<ConstituentPrintSummary>? constituentPrintSummaries)
    {
        CallbackSource = callbackSource;
        LocalMonotonicSequence = localMonotonicSequence;
        SourceTimeTicks = sourceTimeTicks;
        SourceDateTimeKind = sourceDateTimeKind;
        ReceiveUtc = receiveUtc;
        ReceiveStopwatchTimestamp = receiveStopwatchTimestamp;
        ObservedInstrumentIdentityKey = observedInstrumentIdentityKey;
        ValueFingerprint = valueFingerprint;
        Volume = volume;
        FirstPrice = firstPrice;
        LastPrice = lastPrice;
        Direction = direction;
        CopiedTickCount = copiedTickCount;
        ProcessLocalInstanceId = processLocalInstanceId;
        ProcessLocalInstanceIdObserved = processLocalInstanceIdObserved;
        ConstituentPrintSummaries = constituentPrintSummaries ?? Array.Empty<ConstituentPrintSummary>();
    }

    public TradeCallbackSource CallbackSource { get; }
    public long LocalMonotonicSequence { get; }
    public long SourceTimeTicks { get; }
    public DateTimeKind SourceDateTimeKind { get; }
    public DateTime ReceiveUtc { get; }
    public long ReceiveStopwatchTimestamp { get; }
    public string ObservedInstrumentIdentityKey { get; }
    public string ValueFingerprint { get; }
    public decimal Volume { get; }
    public decimal FirstPrice { get; }
    public decimal LastPrice { get; }
    public string Direction { get; }
    public int CopiedTickCount { get; }

    /// <summary>Valid only within one indicator instance. Never serialize as exchange ID.</summary>
    public long? ProcessLocalInstanceId { get; }

    public bool ProcessLocalInstanceIdObserved { get; }
    public IReadOnlyList<ConstituentPrintSummary> ConstituentPrintSummaries { get; }
}

/// <summary>Bounded copied print summary from CumulativeTrade.Ticks (no ATAS refs).</summary>
public readonly struct ConstituentPrintSummary
{
    public ConstituentPrintSummary(
        long sourceTimeTicks,
        decimal price,
        decimal volume,
        string direction,
        string dataType,
        string coreFingerprint)
    {
        SourceTimeTicks = sourceTimeTicks;
        Price = price;
        Volume = volume;
        Direction = direction;
        DataType = dataType;
        CoreFingerprint = coreFingerprint;
    }

    public long SourceTimeTicks { get; }
    public decimal Price { get; }
    public decimal Volume { get; }
    public string Direction { get; }
    public string DataType { get; }
    public string CoreFingerprint { get; }
}
