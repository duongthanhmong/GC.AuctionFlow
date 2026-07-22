namespace GC.AuctionFlow.Recorder;

public enum RecorderStreamKind
{
    Trade = 1,
    Dom = 2,
    Mbo = 3,
    Lifecycle = 4,
    Integrity = 5
}

public enum RecorderCallbackSource
{
    OnNewTrade = 1,
    OnNewTrades = 2,
    OnCumulativeTrade = 3,
    OnUpdateCumulativeTrade = 4,
    MarketDepthChanged = 5,
    MarketDepthsBatch = 6,
    BestBidAskChanged = 7,
    DomSnapshotRequest = 8,
    DomSnapshotEnumeration = 9,
    OnMarketByOrdersChanged = 10,
    RecorderWorker = 11
}

public enum RawEventPayloadKind
{
    NewTrade = 1,
    CumulativeTradeNew = 2,
    CumulativeTradeUpdate = 3,
    Depth = 4,
    BestBidAsk = 5,
    DomSnapshotRequest = 6,
    DomSnapshotItem = 7,
    DomSnapshotLocalEnumerationResult = 8,
    Mbo = 9,
    RecorderLifecycle = 10,
    RecorderIntegrity = 11
}

public enum RecorderFrameType : ushort
{
    SegmentHeader = 1,
    RawEvent = 2,
    SegmentFooter = 3
}

[Flags]
public enum RecorderIntegrityFlags : uint
{
    None = 0,
    NativeSequenceAbsent = 1 << 0,
    SourceTimeKindUnspecified = 1 << 1,
    ProviderSnapshotCompletionUnknown = 1 << 2,
    MboRecordingBlocked = 1 << 3,
    ContractIdentityChanged = 1 << 4,
    DiskSafetyStop = 1 << 5,
    WorkerFault = 1 << 6
}

public enum RecorderLifecycleEventKind
{
    SessionStarted = 1,
    SessionStopped = 2,
    SegmentOpened = 3,
    SegmentClosed = 4,
    ContractEpochAdvanced = 5,
    DiskSpaceStop = 6,
    WorkerFault = 7,
    CaptureStoppedAccepting = 8
}

public enum RecorderIntegrityEventKind
{
    SequenceDiscontinuity = 1,
    HashMismatch = 2,
    FrameFault = 3,
    ReconciliationMismatch = 4,
    QuarantineCreated = 5,
    AbnormalTermination = 6
}

public enum RecoveryClassification
{
    IncompleteTemporary = 1,
    HashMissing = 2,
    OrphanHash = 3,
    HashMismatch = 4,
    ManifestLag = 5,
    InvalidContainerHeader = 6,
    InvalidFrameMagic = 7,
    InvalidFrameLength = 8,
    OversizedFrame = 9,
    FrameCrcMismatch = 10,
    PayloadDecodeFailure = 11,
    MissingFooter = 12,
    SequenceDiscontinuity = 13,
    DuplicateSegmentIdentity = 14,
    TrustedComplete = 15
}
