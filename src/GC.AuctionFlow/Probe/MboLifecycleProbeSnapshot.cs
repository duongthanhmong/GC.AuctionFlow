using GC.AuctionFlow.Core;

namespace GC.AuctionFlow.Probe;

public sealed class MboLifecycleProbeSnapshot
{
    public MboLifecycleProbeSnapshot(
        string schemaVersion,
        string probeVersion,
        Guid sessionId,
        DateTime createdUtc,
        string continuityDisclaimer,
        string atasIndicatorsAssemblyVersion,
        DataSourceMode declaredDataSourceMode,
        DataSourceModeProvenance dataSourceModeProvenance,
        DeclaredFeedProvider declaredFeedProvider,
        FeedProviderProvenance feedProviderProvenance,
        string? expectedInstrumentCode,
        ObservedInstrumentSnapshot? observedInstrument,
        bool captureAuthorized,
        string? gateReason,
        string? lastGateReason,
        string apiSubscriptionMethod,
        string apiCallbackMethod,
        long captureSubscriptionEpoch,
        long finalClosedEpoch,
        MboSubscriptionState subscriptionState,
        int subscriptionAttemptCount,
        int duplicateSubscribeSuppressed,
        int subscribeTriggerCheckCount,
        DateTime? subscriptionAttemptUtc,
        DateTime? subscriptionTaskCompletedUtc,
        string? taskFaultType,
        string? taskFaultMessageSanitized,
        bool taskCanceled,
        DateTime? firstCallbackReceiveUtc,
        TimeSpan? attemptToTaskCompletion,
        TimeSpan? attemptToFirstCallback,
        string callbackObservationStatus,
        bool mboRuntimeEventPresenceObserved,
        IReadOnlyList<MboRawEnumDefinition> rawEnumDefinitions,
        MboLifecycleCounterSnapshot counters,
        MboOrderObservationStateSummary orderObservationState,
        MboSnapshotStudyResult snapshotStudy,
        MboPriorityStudyResult priorityStudy,
        long sourceTimeNonDecreasing,
        long sourceTimeDecreasing,
        IReadOnlyDictionary<int, long> managedThreadIdCounts,
        IReadOnlyDictionary<string, long> dateTimeKindCounts,
        IReadOnlyDictionary<string, Dictionary<string, long>> rawEventTypeByThread,
        IReadOnlyList<MboObservation> samples,
        IReadOnlyList<string> knownLimitations,
        IReadOnlyList<string> integrityEvents,
        int queueCapacity,
        bool providerUnsubscribePerformed,
        string providerUnsubscribeAvailability,
        bool liveMboCapabilityClaim,
        bool mboLifecycleCompletenessClaim,
        bool stableMboBookReconstruction,
        bool nativeSequenceAvailable,
        bool icebergInferenceAvailable,
        bool spoofingInferenceAvailable,
        bool queuePositionInferenceAvailable,
        bool executionInferenceAvailable,
        bool snapshotCompletionKnown,
        string interpretedLifecycleAction)
    {
        SchemaVersion = schemaVersion;
        ProbeVersion = probeVersion;
        SessionId = sessionId;
        CreatedUtc = createdUtc;
        ContinuityDisclaimer = continuityDisclaimer;
        AtasIndicatorsAssemblyVersion = atasIndicatorsAssemblyVersion;
        DeclaredDataSourceMode = declaredDataSourceMode;
        DataSourceModeProvenance = dataSourceModeProvenance;
        DeclaredFeedProvider = declaredFeedProvider;
        FeedProviderProvenance = feedProviderProvenance;
        ExpectedInstrumentCode = expectedInstrumentCode;
        ObservedInstrument = observedInstrument;
        CaptureAuthorized = captureAuthorized;
        GateReason = gateReason;
        LastGateReason = lastGateReason;
        ApiSubscriptionMethod = apiSubscriptionMethod;
        ApiCallbackMethod = apiCallbackMethod;
        CaptureSubscriptionEpoch = captureSubscriptionEpoch;
        FinalClosedEpoch = finalClosedEpoch;
        SubscriptionState = subscriptionState;
        SubscriptionAttemptCount = subscriptionAttemptCount;
        DuplicateSubscribeSuppressed = duplicateSubscribeSuppressed;
        SubscribeTriggerCheckCount = subscribeTriggerCheckCount;
        SubscriptionAttemptUtc = subscriptionAttemptUtc;
        SubscriptionTaskCompletedUtc = subscriptionTaskCompletedUtc;
        TaskFaultType = taskFaultType;
        TaskFaultMessageSanitized = taskFaultMessageSanitized;
        TaskCanceled = taskCanceled;
        FirstCallbackReceiveUtc = firstCallbackReceiveUtc;
        AttemptToTaskCompletion = attemptToTaskCompletion;
        AttemptToFirstCallback = attemptToFirstCallback;
        CallbackObservationStatus = callbackObservationStatus;
        MboRuntimeEventPresenceObserved = mboRuntimeEventPresenceObserved;
        RawEnumDefinitions = rawEnumDefinitions;
        Counters = counters;
        OrderObservationState = orderObservationState;
        SnapshotStudy = snapshotStudy;
        PriorityStudy = priorityStudy;
        SourceTimeNonDecreasing = sourceTimeNonDecreasing;
        SourceTimeDecreasing = sourceTimeDecreasing;
        ManagedThreadIdCounts = managedThreadIdCounts;
        DateTimeKindCounts = dateTimeKindCounts;
        RawEventTypeByThread = rawEventTypeByThread;
        Samples = samples;
        KnownLimitations = knownLimitations;
        IntegrityEvents = integrityEvents;
        QueueCapacity = queueCapacity;
        ProviderUnsubscribePerformed = providerUnsubscribePerformed;
        ProviderUnsubscribeAvailability = providerUnsubscribeAvailability;
        LiveMboCapabilityClaim = liveMboCapabilityClaim;
        MboLifecycleCompletenessClaim = mboLifecycleCompletenessClaim;
        StableMboBookReconstruction = stableMboBookReconstruction;
        NativeSequenceAvailable = nativeSequenceAvailable;
        IcebergInferenceAvailable = icebergInferenceAvailable;
        SpoofingInferenceAvailable = spoofingInferenceAvailable;
        QueuePositionInferenceAvailable = queuePositionInferenceAvailable;
        ExecutionInferenceAvailable = executionInferenceAvailable;
        SnapshotCompletionKnown = snapshotCompletionKnown;
        InterpretedLifecycleAction = interpretedLifecycleAction;
    }

    public string SchemaVersion { get; }
    public string ProbeVersion { get; }
    public Guid SessionId { get; }
    public DateTime CreatedUtc { get; }
    public string ContinuityDisclaimer { get; }
    public string AtasIndicatorsAssemblyVersion { get; }
    public DataSourceMode DeclaredDataSourceMode { get; }
    public DataSourceModeProvenance DataSourceModeProvenance { get; }
    public DeclaredFeedProvider DeclaredFeedProvider { get; }
    public FeedProviderProvenance FeedProviderProvenance { get; }
    public string? ExpectedInstrumentCode { get; }
    public ObservedInstrumentSnapshot? ObservedInstrument { get; }
    public bool CaptureAuthorized { get; }
    public string? GateReason { get; }
    public string? LastGateReason { get; }
    public string ApiSubscriptionMethod { get; }
    public string ApiCallbackMethod { get; }
    public long CaptureSubscriptionEpoch { get; }
    public long FinalClosedEpoch { get; }
    public MboSubscriptionState SubscriptionState { get; }
    public int SubscriptionAttemptCount { get; }
    public int DuplicateSubscribeSuppressed { get; }
    public int SubscribeTriggerCheckCount { get; }
    public DateTime? SubscriptionAttemptUtc { get; }
    public DateTime? SubscriptionTaskCompletedUtc { get; }
    public string? TaskFaultType { get; }
    public string? TaskFaultMessageSanitized { get; }
    public bool TaskCanceled { get; }
    public DateTime? FirstCallbackReceiveUtc { get; }
    public TimeSpan? AttemptToTaskCompletion { get; }
    public TimeSpan? AttemptToFirstCallback { get; }
    public string CallbackObservationStatus { get; }
    public bool MboRuntimeEventPresenceObserved { get; }
    public IReadOnlyList<MboRawEnumDefinition> RawEnumDefinitions { get; }
    public MboLifecycleCounterSnapshot Counters { get; }
    public MboOrderObservationStateSummary OrderObservationState { get; }
    public MboSnapshotStudyResult SnapshotStudy { get; }
    public MboPriorityStudyResult PriorityStudy { get; }
    public long SourceTimeNonDecreasing { get; }
    public long SourceTimeDecreasing { get; }
    public IReadOnlyDictionary<int, long> ManagedThreadIdCounts { get; }
    public IReadOnlyDictionary<string, long> DateTimeKindCounts { get; }
    public IReadOnlyDictionary<string, Dictionary<string, long>> RawEventTypeByThread { get; }
    public IReadOnlyList<MboObservation> Samples { get; }
    public IReadOnlyList<string> KnownLimitations { get; }
    public IReadOnlyList<string> IntegrityEvents { get; }
    public int QueueCapacity { get; }
    public bool ProviderUnsubscribePerformed { get; }
    public string ProviderUnsubscribeAvailability { get; }
    public bool LiveMboCapabilityClaim { get; }
    public bool MboLifecycleCompletenessClaim { get; }
    public bool StableMboBookReconstruction { get; }
    public bool NativeSequenceAvailable { get; }
    public bool IcebergInferenceAvailable { get; }
    public bool SpoofingInferenceAvailable { get; }
    public bool QueuePositionInferenceAvailable { get; }
    public bool ExecutionInferenceAvailable { get; }
    public bool SnapshotCompletionKnown { get; }
    public string InterpretedLifecycleAction { get; }
}

public sealed record MboRawEnumDefinition(string EnumName, string MemberName, int NumericValue);

public sealed record MboSnapshotStudyResult(
    DateTime? FirstSnapshotReceiveUtc,
    DateTime? LastSnapshotReceiveUtc,
    long SnapshotObservationCount,
    int DistinctNonzeroIdsWithSnapshot,
    long ZeroIdsWithSnapshot,
    long FirstCallbackBatchItemCount,
    IReadOnlyList<int> FirstCallbackBatchRawTypeNumerics,
    double InitialTimeWindowDurationSeconds,
    long InitialTimeWindowItemCount,
    IReadOnlyList<int> InitialTimeWindowRawTypeNumerics,
    bool NonSnapshotArrivedBeforeLastSnapshot,
    TimeSpan? DurationFirstToLastSnapshot,
    bool SnapshotSeenAfterInitialTimeWindow,
    bool SnapshotCompletionKnown,
    string? Limitation);

public sealed record MboPriorityStudyResult(
    long ZeroPriorityCount,
    long NegativePriorityCount,
    long? MinPriority,
    long? MaxPriority,
    int DistinctPriorityValues,
    string OpaqueNote);
