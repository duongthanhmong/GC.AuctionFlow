using GC.AuctionFlow.Core;

namespace GC.AuctionFlow.Probe;

public sealed class TradeStreamProbeArtifactIdentity
{
    public TradeStreamProbeArtifactIdentity(
        string schemaVersion,
        string probeVersion,
        Guid sessionId,
        DateTime createdUtc,
        string continuityDisclaimer)
    {
        SchemaVersion = schemaVersion;
        ProbeVersion = probeVersion;
        SessionId = sessionId;
        CreatedUtc = createdUtc;
        ContinuityDisclaimer = continuityDisclaimer;
    }

    public string SchemaVersion { get; }
    public string ProbeVersion { get; }
    public Guid SessionId { get; }
    public DateTime CreatedUtc { get; }
    public string ContinuityDisclaimer { get; }
}

public sealed record CallbackObservationReport(
    string OnNewTrade,
    string OnNewTradesBatch,
    string OnCumulativeTrade,
    string OnUpdateCumulativeTrade);

/// <summary>Immutable freeze snapshot for diagnostic artifact export.</summary>
public sealed class TradeStreamProbeSnapshot
{
    public TradeStreamProbeSnapshot(
        TradeStreamProbeArtifactIdentity artifactIdentity,
        DataSourceMode declaredDataSourceMode,
        DataSourceModeProvenance dataSourceModeProvenance,
        string? expectedInstrumentCode,
        ObservedInstrumentSnapshot? observedInstrument,
        string? lastGateReason,
        string? gateReason,
        bool captureAuthorized,
        bool liveTradeCapabilityClaim,
        CallbackObservationReport callbackObservations,
        TradeStreamProbeCounterSnapshot counters,
        TradeStreamOverlapSnapshot overlap,
        IReadOnlyList<NewTradeObservation> newTradeSamples,
        IReadOnlyList<CumulativeTradeObservation> cumulativeSamples,
        IReadOnlyList<string> knownLimitations,
        IReadOnlyList<string> integrityEvents,
        int queueCapacity,
        int maxNewTradeSamples,
        int maxCumulativeSamples,
        string callbackThreading,
        string clockSemantics,
        string authoritativeStream,
        bool fingerprintsAreDiagnosticsOnly)
    {
        ArtifactIdentity = artifactIdentity;
        DeclaredDataSourceMode = declaredDataSourceMode;
        DataSourceModeProvenance = dataSourceModeProvenance;
        ExpectedInstrumentCode = expectedInstrumentCode;
        ObservedInstrument = observedInstrument;
        LastGateReason = lastGateReason;
        GateReason = gateReason;
        CaptureAuthorized = captureAuthorized;
        LiveTradeCapabilityClaim = liveTradeCapabilityClaim;
        CallbackObservations = callbackObservations;
        Counters = counters;
        Overlap = overlap;
        NewTradeSamples = newTradeSamples;
        CumulativeSamples = cumulativeSamples;
        KnownLimitations = knownLimitations;
        IntegrityEvents = integrityEvents;
        QueueCapacity = queueCapacity;
        MaxNewTradeSamples = maxNewTradeSamples;
        MaxCumulativeSamples = maxCumulativeSamples;
        CallbackThreading = callbackThreading;
        ClockSemantics = clockSemantics;
        AuthoritativeStream = authoritativeStream;
        FingerprintsAreDiagnosticsOnly = fingerprintsAreDiagnosticsOnly;
    }

    public TradeStreamProbeArtifactIdentity ArtifactIdentity { get; }
    public DataSourceMode DeclaredDataSourceMode { get; }
    public DataSourceModeProvenance DataSourceModeProvenance { get; }
    public string? ExpectedInstrumentCode { get; }
    public ObservedInstrumentSnapshot? ObservedInstrument { get; }
    public string? LastGateReason { get; }

    /// <summary>Canonical code: ExpectedInstrumentMissing | InstrumentMismatch | …</summary>
    public string? GateReason { get; }

    public bool CaptureAuthorized { get; }

    /// <summary>Always false in P0-04 — rejected capture never becomes LIVE capability PASS.</summary>
    public bool LiveTradeCapabilityClaim { get; }

    public CallbackObservationReport CallbackObservations { get; }
    public TradeStreamProbeCounterSnapshot Counters { get; }
    public TradeStreamOverlapSnapshot Overlap { get; }
    public IReadOnlyList<NewTradeObservation> NewTradeSamples { get; }
    public IReadOnlyList<CumulativeTradeObservation> CumulativeSamples { get; }
    public IReadOnlyList<string> KnownLimitations { get; }
    public IReadOnlyList<string> IntegrityEvents { get; }
    public int QueueCapacity { get; }
    public int MaxNewTradeSamples { get; }
    public int MaxCumulativeSamples { get; }
    public string CallbackThreading { get; }
    public string ClockSemantics { get; }
    public string AuthoritativeStream { get; }
    public bool FingerprintsAreDiagnosticsOnly { get; }
}
