namespace GC.AuctionFlow.Probe;

public sealed class MboLifecycleProbeCounters
{
    private long _callbackInvocations;
    private long _batchItemsEnumerated;
    private long _emptyBatchCallbacks;
    private long _nullBatchCallbacks;
    private long _nullItemObservations;
    private long _batchEnumerationFailures;
    private long _normalizationFailures;
    private long _acceptedToQueue;
    private long _processedByWorker;
    private long _queueFullDrops;
    private long _rejectedByModeGate;
    private long _rejectedByInstrumentGate;
    private long _rejectedAfterDispose;
    private long _rejectedByEpochGate;
    private long _sideBid;
    private long _sideAsk;
    private long _sideUnknown;
    private long _zeroExchangeOrderId;
    private long _nonzeroExchangeOrderId;
    private long _rawSnapshot;
    private long _rawNew;
    private long _rawChange;
    private long _rawDelete;
    private long _rawUnknownType;
    private long _priorityZero;
    private long _priorityNegative;

    public void IncCallbackInvocations() => Interlocked.Increment(ref _callbackInvocations);
    public void AddBatchItems(long n) => Interlocked.Add(ref _batchItemsEnumerated, n);
    public void IncEmptyBatch() => Interlocked.Increment(ref _emptyBatchCallbacks);
    public void IncNullBatch() => Interlocked.Increment(ref _nullBatchCallbacks);
    public void IncNullItem() => Interlocked.Increment(ref _nullItemObservations);
    public void IncBatchEnumerationFailures() => Interlocked.Increment(ref _batchEnumerationFailures);
    public void IncNormalizationFailures() => Interlocked.Increment(ref _normalizationFailures);
    public void IncAcceptedToQueue() => Interlocked.Increment(ref _acceptedToQueue);
    public void IncProcessedByWorker() => Interlocked.Increment(ref _processedByWorker);
    public void IncQueueFullDrops() => Interlocked.Increment(ref _queueFullDrops);
    public void IncRejectedByModeGate() => Interlocked.Increment(ref _rejectedByModeGate);
    public void IncRejectedByInstrumentGate() => Interlocked.Increment(ref _rejectedByInstrumentGate);
    public void IncRejectedAfterDispose() => Interlocked.Increment(ref _rejectedAfterDispose);
    public void IncRejectedByEpochGate() => Interlocked.Increment(ref _rejectedByEpochGate);

    public void IncSide(MboDerivedSide side)
    {
        switch (side)
        {
            case MboDerivedSide.Bid: Interlocked.Increment(ref _sideBid); break;
            case MboDerivedSide.Ask: Interlocked.Increment(ref _sideAsk); break;
            default: Interlocked.Increment(ref _sideUnknown); break;
        }
    }

    public void IncExchangeOrderId(long id)
    {
        if (id == 0) Interlocked.Increment(ref _zeroExchangeOrderId);
        else Interlocked.Increment(ref _nonzeroExchangeOrderId);
    }

    public void IncRawType(int numeric)
    {
        switch (numeric)
        {
            case MboKnownRawUpdateTypes.Snapshot: Interlocked.Increment(ref _rawSnapshot); break;
            case MboKnownRawUpdateTypes.New: Interlocked.Increment(ref _rawNew); break;
            case MboKnownRawUpdateTypes.Change: Interlocked.Increment(ref _rawChange); break;
            case MboKnownRawUpdateTypes.Delete: Interlocked.Increment(ref _rawDelete); break;
            default: Interlocked.Increment(ref _rawUnknownType); break;
        }
    }

    public void IncPriority(long priority)
    {
        if (priority == 0) Interlocked.Increment(ref _priorityZero);
        else if (priority < 0) Interlocked.Increment(ref _priorityNegative);
    }

    public MboLifecycleCounterSnapshot Snapshot() => new(
        CallbackInvocations: Volatile.Read(ref _callbackInvocations),
        BatchItemsEnumerated: Volatile.Read(ref _batchItemsEnumerated),
        EmptyBatchCallbacks: Volatile.Read(ref _emptyBatchCallbacks),
        NullBatchCallbacks: Volatile.Read(ref _nullBatchCallbacks),
        NullItemObservations: Volatile.Read(ref _nullItemObservations),
        BatchEnumerationFailures: Volatile.Read(ref _batchEnumerationFailures),
        NormalizationFailures: Volatile.Read(ref _normalizationFailures),
        AcceptedToQueue: Volatile.Read(ref _acceptedToQueue),
        ProcessedByWorker: Volatile.Read(ref _processedByWorker),
        QueueFullDrops: Volatile.Read(ref _queueFullDrops),
        RejectedByModeGate: Volatile.Read(ref _rejectedByModeGate),
        RejectedByInstrumentGate: Volatile.Read(ref _rejectedByInstrumentGate),
        RejectedAfterDispose: Volatile.Read(ref _rejectedAfterDispose),
        RejectedByEpochGate: Volatile.Read(ref _rejectedByEpochGate),
        SideBid: Volatile.Read(ref _sideBid),
        SideAsk: Volatile.Read(ref _sideAsk),
        SideUnknown: Volatile.Read(ref _sideUnknown),
        ZeroExchangeOrderId: Volatile.Read(ref _zeroExchangeOrderId),
        NonzeroExchangeOrderId: Volatile.Read(ref _nonzeroExchangeOrderId),
        RawSnapshot: Volatile.Read(ref _rawSnapshot),
        RawNew: Volatile.Read(ref _rawNew),
        RawChange: Volatile.Read(ref _rawChange),
        RawDelete: Volatile.Read(ref _rawDelete),
        RawUnknownType: Volatile.Read(ref _rawUnknownType),
        PriorityZero: Volatile.Read(ref _priorityZero),
        PriorityNegative: Volatile.Read(ref _priorityNegative));
}

public sealed record MboLifecycleCounterSnapshot(
    long CallbackInvocations,
    long BatchItemsEnumerated,
    long EmptyBatchCallbacks,
    long NullBatchCallbacks,
    long NullItemObservations,
    long BatchEnumerationFailures,
    long NormalizationFailures,
    long AcceptedToQueue,
    long ProcessedByWorker,
    long QueueFullDrops,
    long RejectedByModeGate,
    long RejectedByInstrumentGate,
    long RejectedAfterDispose,
    long RejectedByEpochGate,
    long SideBid,
    long SideAsk,
    long SideUnknown,
    long ZeroExchangeOrderId,
    long NonzeroExchangeOrderId,
    long RawSnapshot,
    long RawNew,
    long RawChange,
    long RawDelete,
    long RawUnknownType,
    long PriorityZero,
    long PriorityNegative);
