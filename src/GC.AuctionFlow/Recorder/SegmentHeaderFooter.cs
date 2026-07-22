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
        long marketEventRecordCount,
        long invocationResultRecordCount,
        long lifecycleIntegrityRecordCount,
        long firstWriterSequence,
        long lastWriterSequence,
        long bytesBeforeFooter,
        bool completedNormally,
        bool categoryCountsClaimed = false)
    {
        if (categoryCountsClaimed
            && rawEventRecordCount != marketEventRecordCount + invocationResultRecordCount + lifecycleIntegrityRecordCount)
        {
            throw new ArgumentException(
                "RawEventRecordCount must equal MarketEventRecordCount + InvocationResultRecordCount + LifecycleIntegrityRecordCount.");
        }

        SegmentId = segmentId;
        SegmentOrdinal = segmentOrdinal;
        EndedUtc = endedUtc;
        EndedStopwatchTimestamp = endedStopwatchTimestamp;
        RawEventRecordCount = rawEventRecordCount;
        MarketEventRecordCount = marketEventRecordCount;
        InvocationResultRecordCount = invocationResultRecordCount;
        LifecycleIntegrityRecordCount = lifecycleIntegrityRecordCount;
        FirstWriterSequence = firstWriterSequence;
        LastWriterSequence = lastWriterSequence;
        BytesBeforeFooter = bytesBeforeFooter;
        CompletedNormally = completedNormally;
        CategoryCountsClaimed = categoryCountsClaimed;
    }

    public Guid SegmentId { get; }
    public int SegmentOrdinal { get; }
    public DateTime EndedUtc { get; }
    public long EndedStopwatchTimestamp { get; }
    public long RawEventRecordCount { get; }
    public long MarketEventRecordCount { get; }
    public long InvocationResultRecordCount { get; }
    public long LifecycleIntegrityRecordCount { get; }
    public long FirstWriterSequence { get; }
    public long LastWriterSequence { get; }
    public long BytesBeforeFooter { get; }
    public bool CompletedNormally { get; }

    /// <summary>
    /// True for schema 1.2.0+ writers. Schema 1.1.0 recovered footers leave this false and do not claim category splits.
    /// </summary>
    public bool CategoryCountsClaimed { get; }
}

/// <summary>Schema version helpers for recovery compatibility (container framing unchanged).</summary>
public static class RecorderSchemaCompatibility
{
    public static bool RequiresCategoryCounts(string? recorderSchemaVersion)
    {
        if (string.IsNullOrWhiteSpace(recorderSchemaVersion))
            return false;
        // 1.2.0+ requires category validation; 1.1.0 / 1.0.0 do not claim category counts.
        return Compare(recorderSchemaVersion, "1.2.0") >= 0;
    }

    public static int Compare(string a, string b)
    {
        var pa = Parse(a);
        var pb = Parse(b);
        for (var i = 0; i < 3; i++)
        {
            var d = pa[i].CompareTo(pb[i]);
            if (d != 0) return d;
        }

        return 0;
    }

    private static int[] Parse(string v)
    {
        var parts = v.Split('.');
        var r = new int[3];
        for (var i = 0; i < 3 && i < parts.Length; i++)
            _ = int.TryParse(parts[i], out r[i]);
        return r;
    }
}
