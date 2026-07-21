namespace GC.AuctionFlow.Probe;

/// <summary>Explicit drop / acceptance counters. Thread-safe increments.</summary>
public sealed class TradeStreamProbeCounters
{
    private long _acceptedToQueue;
    private long _rejectedByModeGate;
    private long _rejectedByInstrumentGate;
    private long _rejectedAfterDispose;
    private long _queueFullDrops;
    private long _normalizationFailures;
    private long _processedByWorker;

    private long _cbOnNewTrade;
    private long _cbOnNewTradesBatch;
    private long _cbOnCumulativeTrade;
    private long _cbOnUpdateCumulativeTrade;

    private long _normOnNewTrade;
    private long _normOnNewTradesBatch;
    private long _normOnCumulativeTrade;
    private long _normOnUpdateCumulativeTrade;

    private long _cumulativeNewObservationCount;
    private long _cumulativeUpdateCount;

    public void IncCallback(TradeCallbackSource source)
    {
        switch (source)
        {
            case TradeCallbackSource.OnNewTrade:
                Interlocked.Increment(ref _cbOnNewTrade);
                break;
            case TradeCallbackSource.OnNewTradesBatch:
                Interlocked.Increment(ref _cbOnNewTradesBatch);
                break;
            case TradeCallbackSource.OnCumulativeTrade:
                Interlocked.Increment(ref _cbOnCumulativeTrade);
                break;
            case TradeCallbackSource.OnUpdateCumulativeTrade:
                Interlocked.Increment(ref _cbOnUpdateCumulativeTrade);
                break;
        }
    }

    public void IncNormalized(TradeCallbackSource source)
    {
        switch (source)
        {
            case TradeCallbackSource.OnNewTrade:
                Interlocked.Increment(ref _normOnNewTrade);
                break;
            case TradeCallbackSource.OnNewTradesBatch:
                Interlocked.Increment(ref _normOnNewTradesBatch);
                break;
            case TradeCallbackSource.OnCumulativeTrade:
                Interlocked.Increment(ref _normOnCumulativeTrade);
                break;
            case TradeCallbackSource.OnUpdateCumulativeTrade:
                Interlocked.Increment(ref _normOnUpdateCumulativeTrade);
                break;
        }
    }

    public void IncAcceptedToQueue() => Interlocked.Increment(ref _acceptedToQueue);
    public void IncRejectedByModeGate() => Interlocked.Increment(ref _rejectedByModeGate);
    public void IncRejectedByInstrumentGate() => Interlocked.Increment(ref _rejectedByInstrumentGate);
    public void IncRejectedAfterDispose() => Interlocked.Increment(ref _rejectedAfterDispose);
    public void IncQueueFullDrops() => Interlocked.Increment(ref _queueFullDrops);
    public void IncNormalizationFailures() => Interlocked.Increment(ref _normalizationFailures);
    public void IncProcessedByWorker() => Interlocked.Increment(ref _processedByWorker);

    /// <summary>
    /// Count of normalized observations from OnCumulativeTrade only.
    /// Not a unique/total exchange execution count. Updates must never call this.
    /// </summary>
    public void IncCumulativeNewObservationCount() =>
        Interlocked.Increment(ref _cumulativeNewObservationCount);

    public void IncCumulativeUpdateCount() => Interlocked.Increment(ref _cumulativeUpdateCount);

    public TradeStreamProbeCounterSnapshot Snapshot() => new(
        CallbackInvocationsOnNewTrade: Volatile.Read(ref _cbOnNewTrade),
        CallbackInvocationsOnNewTradesBatch: Volatile.Read(ref _cbOnNewTradesBatch),
        CallbackInvocationsOnCumulativeTrade: Volatile.Read(ref _cbOnCumulativeTrade),
        CallbackInvocationsOnUpdateCumulativeTrade: Volatile.Read(ref _cbOnUpdateCumulativeTrade),
        ObservationsNormalizedOnNewTrade: Volatile.Read(ref _normOnNewTrade),
        ObservationsNormalizedOnNewTradesBatch: Volatile.Read(ref _normOnNewTradesBatch),
        ObservationsNormalizedOnCumulativeTrade: Volatile.Read(ref _normOnCumulativeTrade),
        ObservationsNormalizedOnUpdateCumulativeTrade: Volatile.Read(ref _normOnUpdateCumulativeTrade),
        AcceptedToQueue: Volatile.Read(ref _acceptedToQueue),
        RejectedByModeGate: Volatile.Read(ref _rejectedByModeGate),
        RejectedByInstrumentGate: Volatile.Read(ref _rejectedByInstrumentGate),
        RejectedAfterDispose: Volatile.Read(ref _rejectedAfterDispose),
        QueueFullDrops: Volatile.Read(ref _queueFullDrops),
        NormalizationFailures: Volatile.Read(ref _normalizationFailures),
        ProcessedByWorker: Volatile.Read(ref _processedByWorker),
        CumulativeNewObservationCount: Volatile.Read(ref _cumulativeNewObservationCount),
        CumulativeUpdateCount: Volatile.Read(ref _cumulativeUpdateCount));
}

public sealed record TradeStreamProbeCounterSnapshot(
    long CallbackInvocationsOnNewTrade,
    long CallbackInvocationsOnNewTradesBatch,
    long CallbackInvocationsOnCumulativeTrade,
    long CallbackInvocationsOnUpdateCumulativeTrade,
    long ObservationsNormalizedOnNewTrade,
    long ObservationsNormalizedOnNewTradesBatch,
    long ObservationsNormalizedOnCumulativeTrade,
    long ObservationsNormalizedOnUpdateCumulativeTrade,
    long AcceptedToQueue,
    long RejectedByModeGate,
    long RejectedByInstrumentGate,
    long RejectedAfterDispose,
    long QueueFullDrops,
    long NormalizationFailures,
    long ProcessedByWorker,
    long CumulativeNewObservationCount,
    long CumulativeUpdateCount);
