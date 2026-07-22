namespace GC.AuctionFlow.Recorder;

public sealed class SegmentHeaderRecord
{
    public SegmentHeaderRecord(
        string recorderSchemaVersion,
        int containerVersion,
        Guid sessionId,
        Guid recorderProcessInstanceId,
        Guid segmentId,
        int segmentOrdinal,
        int contractEpoch,
        DateTime startedUtc,
        long startedStopwatchTimestamp,
        ObservedInstrumentIdentity instrument,
        string declaredDataSourceMode,
        string modeProvenance,
        string declaredProvider,
        string providerProvenance)
    {
        RecorderSchemaVersion = recorderSchemaVersion;
        ContainerVersion = containerVersion;
        SessionId = sessionId;
        RecorderProcessInstanceId = recorderProcessInstanceId;
        SegmentId = segmentId;
        SegmentOrdinal = segmentOrdinal;
        ContractEpoch = contractEpoch;
        StartedUtc = startedUtc;
        StartedStopwatchTimestamp = startedStopwatchTimestamp;
        Instrument = instrument;
        DeclaredDataSourceMode = declaredDataSourceMode;
        ModeProvenance = modeProvenance;
        DeclaredProvider = declaredProvider;
        ProviderProvenance = providerProvenance;
    }

    public string RecorderSchemaVersion { get; }
    public int ContainerVersion { get; }
    public Guid SessionId { get; }
    public Guid RecorderProcessInstanceId { get; }
    public Guid SegmentId { get; }
    public int SegmentOrdinal { get; }
    public int ContractEpoch { get; }
    public DateTime StartedUtc { get; }
    public long StartedStopwatchTimestamp { get; }
    public ObservedInstrumentIdentity Instrument { get; }
    public string DeclaredDataSourceMode { get; }
    public string ModeProvenance { get; }
    public string DeclaredProvider { get; }
    public string ProviderProvenance { get; }
}

public sealed class SegmentFooterRecord
{
    public SegmentFooterRecord(
        Guid segmentId,
        int segmentOrdinal,
        DateTime endedUtc,
        long endedStopwatchTimestamp,
        long rawEventRecordCount,
        long firstWriterSequence,
        long lastWriterSequence,
        long bytesBeforeFooter,
        bool completedNormally)
    {
        SegmentId = segmentId;
        SegmentOrdinal = segmentOrdinal;
        EndedUtc = endedUtc;
        EndedStopwatchTimestamp = endedStopwatchTimestamp;
        // RawEvent frames only — excludes SegmentHeader and SegmentFooter frames.
        RawEventRecordCount = rawEventRecordCount;
        FirstWriterSequence = firstWriterSequence;
        LastWriterSequence = lastWriterSequence;
        // Container prefix + header frame + raw-event frames; excludes this footer frame.
        BytesBeforeFooter = bytesBeforeFooter;
        CompletedNormally = completedNormally;
    }

    public Guid SegmentId { get; }
    public int SegmentOrdinal { get; }
    public DateTime EndedUtc { get; }
    public long EndedStopwatchTimestamp { get; }
    public long RawEventRecordCount { get; }
    public long FirstWriterSequence { get; }
    public long LastWriterSequence { get; }
    public long BytesBeforeFooter { get; }
    public bool CompletedNormally { get; }
}
