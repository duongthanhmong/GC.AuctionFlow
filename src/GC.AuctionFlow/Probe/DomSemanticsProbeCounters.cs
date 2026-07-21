namespace GC.AuctionFlow.Probe;

public sealed class DomSemanticsProbeCounters
{
    private long _acceptedToQueue;
    private long _processedByWorker;
    private long _rejectedByModeGate;
    private long _rejectedByInstrumentGate;
    private long _rejectedAfterDispose;
    private long _queueFullDrops;
    private long _normalizationFailures;

    private long _cbSingular;
    private long _cbBatch;
    private long _cbBest;
    private long _cbSnapshot;

    private long _normSingular;
    private long _normBatch;
    private long _normBest;
    private long _normSnapshot;

    private long _consistentBid;
    private long _consistentAsk;
    private long _conflictingSide;
    private long _unexpectedTrade;
    private long _unknownSide;
    private long _zeroVolume;

    public void IncCallback(DepthCallbackSource s)
    {
        switch (s)
        {
            case DepthCallbackSource.MarketDepthChanged: Interlocked.Increment(ref _cbSingular); break;
            case DepthCallbackSource.MarketDepthsBatch: Interlocked.Increment(ref _cbBatch); break;
            case DepthCallbackSource.BestBidAskChanged: Interlocked.Increment(ref _cbBest); break;
            case DepthCallbackSource.SnapshotPull: Interlocked.Increment(ref _cbSnapshot); break;
        }
    }

    public void IncNormalized(DepthCallbackSource s)
    {
        switch (s)
        {
            case DepthCallbackSource.MarketDepthChanged: Interlocked.Increment(ref _normSingular); break;
            case DepthCallbackSource.MarketDepthsBatch: Interlocked.Increment(ref _normBatch); break;
            case DepthCallbackSource.BestBidAskChanged: Interlocked.Increment(ref _normBest); break;
            case DepthCallbackSource.SnapshotPull: Interlocked.Increment(ref _normSnapshot); break;
        }
    }

    public void IncAcceptedToQueue() => Interlocked.Increment(ref _acceptedToQueue);
    public void IncProcessedByWorker() => Interlocked.Increment(ref _processedByWorker);
    public void IncRejectedByModeGate() => Interlocked.Increment(ref _rejectedByModeGate);
    public void IncRejectedByInstrumentGate() => Interlocked.Increment(ref _rejectedByInstrumentGate);
    public void IncRejectedAfterDispose() => Interlocked.Increment(ref _rejectedAfterDispose);
    public void IncQueueFullDrops() => Interlocked.Increment(ref _queueFullDrops);
    public void IncNormalizationFailures() => Interlocked.Increment(ref _normalizationFailures);
    public void IncZeroVolume() => Interlocked.Increment(ref _zeroVolume);

    public void IncSideDetail(string detail)
    {
        switch (detail)
        {
            case "ConsistentBid": Interlocked.Increment(ref _consistentBid); break;
            case "ConsistentAsk": Interlocked.Increment(ref _consistentAsk); break;
            case "ConflictingSideFields": Interlocked.Increment(ref _conflictingSide); break;
            case "UnexpectedTradeDataType": Interlocked.Increment(ref _unexpectedTrade); break;
            default: Interlocked.Increment(ref _unknownSide); break;
        }
    }

    public DomSemanticsCounterSnapshot Snapshot() => new(
        CallbackInvocationsMarketDepthChanged: Volatile.Read(ref _cbSingular),
        CallbackInvocationsMarketDepthsBatch: Volatile.Read(ref _cbBatch),
        CallbackInvocationsBestBidAskChanged: Volatile.Read(ref _cbBest),
        CallbackInvocationsSnapshotPull: Volatile.Read(ref _cbSnapshot),
        ObservationsNormalizedMarketDepthChanged: Volatile.Read(ref _normSingular),
        ObservationsNormalizedMarketDepthsBatch: Volatile.Read(ref _normBatch),
        ObservationsNormalizedBestBidAskChanged: Volatile.Read(ref _normBest),
        ObservationsNormalizedSnapshotPull: Volatile.Read(ref _normSnapshot),
        AcceptedToQueue: Volatile.Read(ref _acceptedToQueue),
        ProcessedByWorker: Volatile.Read(ref _processedByWorker),
        RejectedByModeGate: Volatile.Read(ref _rejectedByModeGate),
        RejectedByInstrumentGate: Volatile.Read(ref _rejectedByInstrumentGate),
        RejectedAfterDispose: Volatile.Read(ref _rejectedAfterDispose),
        QueueFullDrops: Volatile.Read(ref _queueFullDrops),
        NormalizationFailures: Volatile.Read(ref _normalizationFailures),
        ConsistentBid: Volatile.Read(ref _consistentBid),
        ConsistentAsk: Volatile.Read(ref _consistentAsk),
        ConflictingSideFields: Volatile.Read(ref _conflictingSide),
        UnexpectedTradeDataType: Volatile.Read(ref _unexpectedTrade),
        UnknownSide: Volatile.Read(ref _unknownSide),
        ZeroVolumeObservations: Volatile.Read(ref _zeroVolume));
}

public sealed record DomSemanticsCounterSnapshot(
    long CallbackInvocationsMarketDepthChanged,
    long CallbackInvocationsMarketDepthsBatch,
    long CallbackInvocationsBestBidAskChanged,
    long CallbackInvocationsSnapshotPull,
    long ObservationsNormalizedMarketDepthChanged,
    long ObservationsNormalizedMarketDepthsBatch,
    long ObservationsNormalizedBestBidAskChanged,
    long ObservationsNormalizedSnapshotPull,
    long AcceptedToQueue,
    long ProcessedByWorker,
    long RejectedByModeGate,
    long RejectedByInstrumentGate,
    long RejectedAfterDispose,
    long QueueFullDrops,
    long NormalizationFailures,
    long ConsistentBid,
    long ConsistentAsk,
    long ConflictingSideFields,
    long UnexpectedTradeDataType,
    long UnknownSide,
    long ZeroVolumeObservations);
