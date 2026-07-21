using GC.AuctionFlow.Core;

namespace GC.AuctionFlow.Probe;

public sealed class DomSemanticsProbeSnapshot
{
    public DomSemanticsProbeSnapshot(
        string schemaVersion,
        string probeVersion,
        Guid sessionId,
        DateTime createdUtc,
        string continuityDisclaimer,
        DataSourceMode declaredDataSourceMode,
        DataSourceModeProvenance dataSourceModeProvenance,
        DeclaredFeedProvider declaredFeedProvider,
        FeedProviderProvenance feedProviderProvenance,
        string? expectedInstrumentCode,
        ObservedInstrumentSnapshot? observedInstrument,
        bool captureAuthorized,
        string? gateReason,
        string? lastGateReason,
        IReadOnlyList<string> apiCallbackSourcesUsed,
        DomSemanticsCounterSnapshot counters,
        IReadOnlyList<DepthObservation> samples,
        DepthLevelObservationStateSummary levelObservationState,
        SnapshotPullStudyResult snapshotPullStudy,
        decimal? lastBestBidPrice,
        decimal? lastBestAskPrice,
        long singularBatchFingerprintOverlapHits,
        long fingerprintCollisionsObserved,
        long sourceTimeNonDecreasingSingular,
        long sourceTimeDecreasingSingular,
        long sourceTimeNonDecreasingBatch,
        long sourceTimeDecreasingBatch,
        IReadOnlyDictionary<int, long> managedThreadIdCounts,
        IReadOnlyDictionary<string, Dictionary<string, long>> dateTimeKindBySource,
        IReadOnlyList<DepthLifecycleMarker> lifecycleMarkers,
        DepthResetEvidence resetEvidence,
        DepthCallbackObservationReport callbackObservations,
        IReadOnlyList<string> knownLimitations,
        IReadOnlyList<string> integrityEvents,
        int queueCapacity,
        string volumeMeaning,
        string zeroVolumeMeaning,
        string updateAction,
        bool stableBookReconstruction,
        bool historicalDomCapabilityClaim,
        bool replayDomCapabilityClaim,
        bool liveDomCapabilityClaim)
    {
        SchemaVersion = schemaVersion;
        ProbeVersion = probeVersion;
        SessionId = sessionId;
        CreatedUtc = createdUtc;
        ContinuityDisclaimer = continuityDisclaimer;
        DeclaredDataSourceMode = declaredDataSourceMode;
        DataSourceModeProvenance = dataSourceModeProvenance;
        DeclaredFeedProvider = declaredFeedProvider;
        FeedProviderProvenance = feedProviderProvenance;
        ExpectedInstrumentCode = expectedInstrumentCode;
        ObservedInstrument = observedInstrument;
        CaptureAuthorized = captureAuthorized;
        GateReason = gateReason;
        LastGateReason = lastGateReason;
        ApiCallbackSourcesUsed = apiCallbackSourcesUsed;
        Counters = counters;
        Samples = samples;
        LevelObservationState = levelObservationState;
        SnapshotPullStudy = snapshotPullStudy;
        LastBestBidPrice = lastBestBidPrice;
        LastBestAskPrice = lastBestAskPrice;
        SingularBatchFingerprintOverlapHits = singularBatchFingerprintOverlapHits;
        FingerprintCollisionsObserved = fingerprintCollisionsObserved;
        SourceTimeNonDecreasingSingular = sourceTimeNonDecreasingSingular;
        SourceTimeDecreasingSingular = sourceTimeDecreasingSingular;
        SourceTimeNonDecreasingBatch = sourceTimeNonDecreasingBatch;
        SourceTimeDecreasingBatch = sourceTimeDecreasingBatch;
        ManagedThreadIdCounts = managedThreadIdCounts;
        DateTimeKindBySource = dateTimeKindBySource;
        LifecycleMarkers = lifecycleMarkers;
        ResetEvidence = resetEvidence;
        CallbackObservations = callbackObservations;
        KnownLimitations = knownLimitations;
        IntegrityEvents = integrityEvents;
        QueueCapacity = queueCapacity;
        VolumeMeaning = volumeMeaning;
        ZeroVolumeMeaning = zeroVolumeMeaning;
        UpdateAction = updateAction;
        StableBookReconstruction = stableBookReconstruction;
        HistoricalDomCapabilityClaim = historicalDomCapabilityClaim;
        ReplayDomCapabilityClaim = replayDomCapabilityClaim;
        LiveDomCapabilityClaim = liveDomCapabilityClaim;
    }

    public string SchemaVersion { get; }
    public string ProbeVersion { get; }
    public Guid SessionId { get; }
    public DateTime CreatedUtc { get; }
    public string ContinuityDisclaimer { get; }
    public DataSourceMode DeclaredDataSourceMode { get; }
    public DataSourceModeProvenance DataSourceModeProvenance { get; }
    public DeclaredFeedProvider DeclaredFeedProvider { get; }
    public FeedProviderProvenance FeedProviderProvenance { get; }
    public string? ExpectedInstrumentCode { get; }
    public ObservedInstrumentSnapshot? ObservedInstrument { get; }
    public bool CaptureAuthorized { get; }
    public string? GateReason { get; }
    public string? LastGateReason { get; }
    public IReadOnlyList<string> ApiCallbackSourcesUsed { get; }
    public DomSemanticsCounterSnapshot Counters { get; }
    public IReadOnlyList<DepthObservation> Samples { get; }
    public DepthLevelObservationStateSummary LevelObservationState { get; }
    public SnapshotPullStudyResult SnapshotPullStudy { get; }
    public decimal? LastBestBidPrice { get; }
    public decimal? LastBestAskPrice { get; }
    public long SingularBatchFingerprintOverlapHits { get; }
    public long FingerprintCollisionsObserved { get; }
    public long SourceTimeNonDecreasingSingular { get; }
    public long SourceTimeDecreasingSingular { get; }
    public long SourceTimeNonDecreasingBatch { get; }
    public long SourceTimeDecreasingBatch { get; }
    public IReadOnlyDictionary<int, long> ManagedThreadIdCounts { get; }
    public IReadOnlyDictionary<string, Dictionary<string, long>> DateTimeKindBySource { get; }
    public IReadOnlyList<DepthLifecycleMarker> LifecycleMarkers { get; }
    public DepthResetEvidence ResetEvidence { get; }
    public DepthCallbackObservationReport CallbackObservations { get; }
    public IReadOnlyList<string> KnownLimitations { get; }
    public IReadOnlyList<string> IntegrityEvents { get; }
    public int QueueCapacity { get; }
    public string VolumeMeaning { get; }
    public string ZeroVolumeMeaning { get; }
    public string UpdateAction { get; }
    public bool StableBookReconstruction { get; }
    public bool HistoricalDomCapabilityClaim { get; }
    public bool ReplayDomCapabilityClaim { get; }
    public bool LiveDomCapabilityClaim { get; }
}
