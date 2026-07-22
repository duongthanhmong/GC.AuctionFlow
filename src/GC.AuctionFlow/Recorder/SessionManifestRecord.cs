namespace GC.AuctionFlow.Recorder;

public sealed class SessionManifestRecord
{
    public SessionManifestRecord(
        int manifestGeneration,
        string recorderSchemaVersion,
        int containerVersion,
        Guid sessionId,
        Guid recorderProcessInstanceId,
        DateTime startUtc,
        DateTime? stopUtc,
        IReadOnlyList<int> contractEpochs,
        string declaredDataSourceMode,
        string modeProvenance,
        string declaredProvider,
        string providerProvenance,
        ObservedInstrumentIdentity? lastInstrument,
        IReadOnlyList<string> enabledStreams,
        IReadOnlyList<DisabledStreamRecord> disabledStreams,
        bool mboSchemaSupported,
        bool mboRecordingEnabled,
        string mboIsolationRequirement,
        string mboOperationalBlockReason,
        IReadOnlyList<ManifestSegmentRecord> segments,
        long? firstWriterSequence,
        long? lastWriterSequence,
        long recordsWritten,
        long bytesWritten,
        RecorderCountersSnapshot counters,
        bool abnormalTermination,
        string? abnormalTerminationReason,
        IReadOnlyList<string> knownLimitations,
        CapabilityClaimsForcedFalse capabilityClaimsForcedFalse,
        string continuityDisclaimer)
    {
        ManifestGeneration = manifestGeneration;
        RecorderSchemaVersion = recorderSchemaVersion;
        ContainerVersion = containerVersion;
        SessionId = sessionId;
        RecorderProcessInstanceId = recorderProcessInstanceId;
        StartUtc = startUtc;
        StopUtc = stopUtc;
        ContractEpochs = contractEpochs;
        DeclaredDataSourceMode = declaredDataSourceMode;
        ModeProvenance = modeProvenance;
        DeclaredProvider = declaredProvider;
        ProviderProvenance = providerProvenance;
        LastInstrument = lastInstrument;
        EnabledStreams = enabledStreams;
        DisabledStreams = disabledStreams;
        MboSchemaSupported = mboSchemaSupported;
        MboRecordingEnabled = mboRecordingEnabled;
        MboIsolationRequirement = mboIsolationRequirement;
        MboOperationalBlockReason = mboOperationalBlockReason;
        Segments = segments;
        FirstWriterSequence = firstWriterSequence;
        LastWriterSequence = lastWriterSequence;
        RecordsWritten = recordsWritten;
        BytesWritten = bytesWritten;
        Counters = counters;
        AbnormalTermination = abnormalTermination;
        AbnormalTerminationReason = abnormalTerminationReason;
        KnownLimitations = knownLimitations;
        CapabilityClaimsForcedFalse = capabilityClaimsForcedFalse;
        ContinuityDisclaimer = continuityDisclaimer;
    }

    public int ManifestGeneration { get; }
    public string RecorderSchemaVersion { get; }
    public int ContainerVersion { get; }
    public Guid SessionId { get; }
    public Guid RecorderProcessInstanceId { get; }
    public DateTime StartUtc { get; }
    public DateTime? StopUtc { get; }
    public IReadOnlyList<int> ContractEpochs { get; }
    public string DeclaredDataSourceMode { get; }
    public string ModeProvenance { get; }
    public string DeclaredProvider { get; }
    public string ProviderProvenance { get; }
    public ObservedInstrumentIdentity? LastInstrument { get; }
    public IReadOnlyList<string> EnabledStreams { get; }
    public IReadOnlyList<DisabledStreamRecord> DisabledStreams { get; }
    public bool MboSchemaSupported { get; }
    public bool MboRecordingEnabled { get; }
    public string MboIsolationRequirement { get; }
    public string MboOperationalBlockReason { get; }
    public IReadOnlyList<ManifestSegmentRecord> Segments { get; }
    public long? FirstWriterSequence { get; }
    public long? LastWriterSequence { get; }
    public long RecordsWritten { get; }
    public long BytesWritten { get; }
    public RecorderCountersSnapshot Counters { get; }
    public bool AbnormalTermination { get; }
    public string? AbnormalTerminationReason { get; }
    public IReadOnlyList<string> KnownLimitations { get; }
    public CapabilityClaimsForcedFalse CapabilityClaimsForcedFalse { get; }
    public string ContinuityDisclaimer { get; }
}

public sealed class DisabledStreamRecord
{
    public DisabledStreamRecord(string stream, string reason)
    {
        Stream = stream;
        Reason = reason;
    }

    public string Stream { get; }
    public string Reason { get; }
}

public sealed class ManifestSegmentRecord
{
    public ManifestSegmentRecord(
        Guid segmentId,
        int segmentOrdinal,
        string fileName,
        string sha256Hex,
        long recordCount,
        long marketEventRecordCount,
        long invocationResultRecordCount,
        long lifecycleIntegrityRecordCount,
        long byteLength,
        long firstWriterSequence,
        long lastWriterSequence,
        int contractEpoch,
        bool categoryCountsKnown = true)
    {
        if (categoryCountsKnown
            && recordCount != marketEventRecordCount + invocationResultRecordCount + lifecycleIntegrityRecordCount)
            throw new ArgumentException("RecordCount must equal category sum when category counts are known.");

        SegmentId = segmentId;
        SegmentOrdinal = segmentOrdinal;
        FileName = fileName;
        Sha256Hex = sha256Hex;
        RecordCount = recordCount;
        MarketEventRecordCount = marketEventRecordCount;
        InvocationResultRecordCount = invocationResultRecordCount;
        LifecycleIntegrityRecordCount = lifecycleIntegrityRecordCount;
        ByteLength = byteLength;
        FirstWriterSequence = firstWriterSequence;
        LastWriterSequence = lastWriterSequence;
        ContractEpoch = contractEpoch;
        CategoryCountsKnown = categoryCountsKnown;
    }

    public Guid SegmentId { get; }
    public int SegmentOrdinal { get; }
    public string FileName { get; }
    public string Sha256Hex { get; }
    /// <summary>Compatible total — equals RawEventRecordCount when categories known.</summary>
    public long RecordCount { get; }
    public long MarketEventRecordCount { get; }
    public long InvocationResultRecordCount { get; }
    public long LifecycleIntegrityRecordCount { get; }
    public long ByteLength { get; }
    public long FirstWriterSequence { get; }
    public long LastWriterSequence { get; }
    public int ContractEpoch { get; }
    public bool CategoryCountsKnown { get; }
}

public sealed class CapabilityClaimsForcedFalse
{
    public CapabilityClaimsForcedFalse()
    {
        TradeFidelity = false;
        DomFidelity = false;
        MboLifecycleCompleteness = false;
        StableBookReconstruction = false;
        HistoricalCapability = false;
        ReplayCapability = false;
        ExchangeFeedCompleteness = false;
    }

    public bool TradeFidelity { get; }
    public bool DomFidelity { get; }
    public bool MboLifecycleCompleteness { get; }
    public bool StableBookReconstruction { get; }
    public bool HistoricalCapability { get; }
    public bool ReplayCapability { get; }
    public bool ExchangeFeedCompleteness { get; }
}

public sealed class RecorderCountersSnapshot
{
    public RecorderCountersSnapshot(
        long callbackInvocations,
        long authorizedCallbackInvocations,
        long rejectedCallbackInvocationsByGate,
        long rejectedCallbackInvocationsAfterDispose,
        long nullBatchCallbacks,
        long emptyBatchCallbacks,
        long batchEnumerationFailures,
        long payloadItemsEnumerated,
        long nullItemObservations,
        long normalizedObservations,
        long normalizationFailures,
        long acceptedToQueue,
        long queueFullDrops,
        long invocationResultEmissionAttempts,
        long invocationResultAcceptedToQueue,
        long invocationResultQueueFullDrops,
        long invocationResultFaults,
        long invocationResultsWritten,
        long writerDequeued,
        long writerDequeuedTotal,
        long marketEventsWritten,
        long lifecycleIntegrityRecordsWritten,
        long recordsWritten,
        long serializationFailures,
        long writerDiscardedAfterFatalFault,
        long undrainedAtShutdown,
        long bytesWritten,
        long segmentsCompleted,
        long segmentsIncomplete,
        long diskSpaceStops,
        long workerFaults,
        long recoveryEvents,
        long segmentWriteFailures,
        long flushFailures,
        long hashFailures,
        long manifestFailures,
        long recorderCallbacksBeforeStart,
        long recorderCallbacksAfterStop,
        long recorderStartupFailures)
    {
        CallbackInvocations = callbackInvocations;
        AuthorizedCallbackInvocations = authorizedCallbackInvocations;
        RejectedCallbackInvocationsByGate = rejectedCallbackInvocationsByGate;
        RejectedCallbackInvocationsAfterDispose = rejectedCallbackInvocationsAfterDispose;
        NullBatchCallbacks = nullBatchCallbacks;
        EmptyBatchCallbacks = emptyBatchCallbacks;
        BatchEnumerationFailures = batchEnumerationFailures;
        PayloadItemsEnumerated = payloadItemsEnumerated;
        NullItemObservations = nullItemObservations;
        NormalizedObservations = normalizedObservations;
        NormalizationFailures = normalizationFailures;
        AcceptedToQueue = acceptedToQueue;
        QueueFullDrops = queueFullDrops;
        InvocationResultEmissionAttempts = invocationResultEmissionAttempts;
        InvocationResultAcceptedToQueue = invocationResultAcceptedToQueue;
        InvocationResultQueueFullDrops = invocationResultQueueFullDrops;
        InvocationResultFaults = invocationResultFaults;
        InvocationResultsWritten = invocationResultsWritten;
        WriterDequeued = writerDequeued;
        WriterDequeuedTotal = writerDequeuedTotal;
        MarketEventsWritten = marketEventsWritten;
        LifecycleIntegrityRecordsWritten = lifecycleIntegrityRecordsWritten;
        RecordsWritten = recordsWritten;
        SerializationFailures = serializationFailures;
        WriterDiscardedAfterFatalFault = writerDiscardedAfterFatalFault;
        UndrainedAtShutdown = undrainedAtShutdown;
        BytesWritten = bytesWritten;
        SegmentsCompleted = segmentsCompleted;
        SegmentsIncomplete = segmentsIncomplete;
        DiskSpaceStops = diskSpaceStops;
        WorkerFaults = workerFaults;
        RecoveryEvents = recoveryEvents;
        SegmentWriteFailures = segmentWriteFailures;
        FlushFailures = flushFailures;
        HashFailures = hashFailures;
        ManifestFailures = manifestFailures;
        RecorderCallbacksBeforeStart = recorderCallbacksBeforeStart;
        RecorderCallbacksAfterStop = recorderCallbacksAfterStop;
        RecorderStartupFailures = recorderStartupFailures;
    }

    public long CallbackInvocations { get; }
    public long AuthorizedCallbackInvocations { get; }
    public long RejectedCallbackInvocationsByGate { get; }
    public long RejectedCallbackInvocationsAfterDispose { get; }
    public long NullBatchCallbacks { get; }
    public long EmptyBatchCallbacks { get; }
    public long BatchEnumerationFailures { get; }
    public long PayloadItemsEnumerated { get; }
    public long NullItemObservations { get; }
    public long NormalizedObservations { get; }
    public long NormalizationFailures { get; }
    public long AcceptedToQueue { get; }
    public long QueueFullDrops { get; }
    public long InvocationResultEmissionAttempts { get; }
    public long InvocationResultAcceptedToQueue { get; }
    public long InvocationResultQueueFullDrops { get; }
    public long InvocationResultFaults { get; }
    public long InvocationResultsWritten { get; }
    public long WriterDequeued { get; }
    public long WriterDequeuedTotal { get; }
    public long MarketEventsWritten { get; }
    public long LifecycleIntegrityRecordsWritten { get; }
    public long RecordsWritten { get; }
    public long SerializationFailures { get; }
    public long WriterDiscardedAfterFatalFault { get; }
    public long UndrainedAtShutdown { get; }
    public long BytesWritten { get; }
    public long SegmentsCompleted { get; }
    public long SegmentsIncomplete { get; }
    public long DiskSpaceStops { get; }
    public long WorkerFaults { get; }
    public long RecoveryEvents { get; }
    public long SegmentWriteFailures { get; }
    public long FlushFailures { get; }
    public long HashFailures { get; }
    public long ManifestFailures { get; }
    public long RecorderCallbacksBeforeStart { get; }
    public long RecorderCallbacksAfterStop { get; }
    public long RecorderStartupFailures { get; }
}
