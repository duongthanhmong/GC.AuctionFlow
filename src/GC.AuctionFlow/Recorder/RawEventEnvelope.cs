using System.Text.Json.Serialization;
using GC.AuctionFlow.Recorder.Payloads;

namespace GC.AuctionFlow.Recorder;

/// <summary>
/// Writer-side immutable envelope.
/// RecorderGlobalLocalSequence is writer dequeue order only — not callback total order,
/// not source-time order, not exchange sequence, and not exchange causality.
/// ReceiveUtc is a compatibility alias exactly equal to CallbackReceiveUtc.
/// </summary>
public sealed class RawEventEnvelope
{
    public RawEventEnvelope(
        string recorderSchemaVersion,
        Guid sessionId,
        Guid recorderProcessInstanceId,
        Guid segmentId,
        int segmentOrdinal,
        long recorderGlobalLocalSequence,
        DateTime writerDequeuedUtc,
        RecorderStreamKind streamKind,
        RecorderCallbackSource callbackSource,
        long streamLocalCaptureSequence,
        long callbackInvocationSequence,
        int callbackItemOrdinal,
        long? subscriptionOrCaptureEpoch,
        int contractEpoch,
        ObservedInstrumentIdentity instrument,
        string declaredDataSourceMode,
        string modeProvenance,
        string declaredProvider,
        string providerProvenance,
        long sourceTimeTicks,
        DateTimeKind sourceDateTimeKind,
        DateTime callbackReceiveUtc,
        long callbackReceiveStopwatchTimestamp,
        int callbackManagedThreadId,
        RawEventPayloadKind payloadDiscriminator,
        RawEventPayload payload,
        RecorderIntegrityFlags integrityFlags,
        bool nativeSequenceAvailable)
    {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        if (instrument is null) throw new ArgumentNullException(nameof(instrument));

        RecorderSchemaVersion = recorderSchemaVersion;
        SessionId = sessionId;
        RecorderProcessInstanceId = recorderProcessInstanceId;
        SegmentId = segmentId;
        SegmentOrdinal = segmentOrdinal;
        RecorderGlobalLocalSequence = recorderGlobalLocalSequence;
        WriterDequeuedUtc = writerDequeuedUtc;
        StreamKind = streamKind;
        CallbackSource = callbackSource;
        StreamLocalCaptureSequence = streamLocalCaptureSequence;
        CallbackInvocationSequence = callbackInvocationSequence;
        CallbackItemOrdinal = callbackItemOrdinal;
        SubscriptionOrCaptureEpoch = subscriptionOrCaptureEpoch;
        ContractEpoch = contractEpoch;
        Instrument = instrument;
        DeclaredDataSourceMode = declaredDataSourceMode ?? string.Empty;
        ModeProvenance = modeProvenance ?? string.Empty;
        DeclaredProvider = declaredProvider ?? string.Empty;
        ProviderProvenance = providerProvenance ?? string.Empty;
        SourceTimeTicks = sourceTimeTicks;
        SourceDateTimeKind = sourceDateTimeKind;
        CallbackReceiveUtc = callbackReceiveUtc;
        CallbackReceiveStopwatchTimestamp = callbackReceiveStopwatchTimestamp;
        CallbackManagedThreadId = callbackManagedThreadId;
        PayloadDiscriminator = payloadDiscriminator;
        Payload = payload;
        IntegrityFlags = integrityFlags;
        NativeSequenceAvailable = nativeSequenceAvailable;
    }

    public static RawEventEnvelope FromDraft(
        RawEventDraft draft,
        Guid segmentId,
        int segmentOrdinal,
        long recorderGlobalLocalSequence,
        DateTime writerDequeuedUtc)
    {
        ArgumentNullException.ThrowIfNull(draft);
        return new RawEventEnvelope(
            draft.RecorderSchemaVersion,
            draft.SessionId,
            draft.RecorderProcessInstanceId,
            segmentId,
            segmentOrdinal,
            recorderGlobalLocalSequence,
            writerDequeuedUtc,
            draft.StreamKind,
            draft.CallbackSource,
            draft.StreamLocalCaptureSequence,
            draft.CallbackInvocationSequence,
            draft.CallbackItemOrdinal,
            draft.SubscriptionOrCaptureEpoch,
            draft.ContractEpoch,
            draft.Instrument,
            draft.DeclaredDataSourceMode,
            draft.ModeProvenance,
            draft.DeclaredProvider,
            draft.ProviderProvenance,
            draft.SourceTimeTicks,
            draft.SourceDateTimeKind,
            draft.CallbackReceiveUtc,
            draft.CallbackReceiveStopwatchTimestamp,
            draft.CallbackManagedThreadId,
            draft.PayloadDiscriminator,
            draft.Payload,
            draft.IntegrityFlags,
            draft.NativeSequenceAvailable);
    }

    public string RecorderSchemaVersion { get; }
    public Guid SessionId { get; }
    public Guid RecorderProcessInstanceId { get; }
    public Guid SegmentId { get; }
    public int SegmentOrdinal { get; }
    public long RecorderGlobalLocalSequence { get; }
    public DateTime WriterDequeuedUtc { get; }
    public RecorderStreamKind StreamKind { get; }
    public RecorderCallbackSource CallbackSource { get; }
    public long StreamLocalCaptureSequence { get; }
    public long CallbackInvocationSequence { get; }
    public int CallbackItemOrdinal { get; }
    public long? SubscriptionOrCaptureEpoch { get; }
    public int ContractEpoch { get; }
    public ObservedInstrumentIdentity Instrument { get; }
    public string DeclaredDataSourceMode { get; }
    public string ModeProvenance { get; }
    public string DeclaredProvider { get; }
    public string ProviderProvenance { get; }
    public long SourceTimeTicks { get; }
    public DateTimeKind SourceDateTimeKind { get; }
    public DateTime CallbackReceiveUtc { get; }
    public long CallbackReceiveStopwatchTimestamp { get; }
    public int CallbackManagedThreadId { get; }

    /// <summary>Compatibility alias: exactly CallbackReceiveUtc.</summary>
    [JsonIgnore]
    public DateTime ReceiveUtc => CallbackReceiveUtc;

    /// <summary>Compatibility alias: exactly CallbackReceiveStopwatchTimestamp.</summary>
    [JsonIgnore]
    public long ReceiveStopwatchTimestamp => CallbackReceiveStopwatchTimestamp;

    public RawEventPayloadKind PayloadDiscriminator { get; }
    public RawEventPayload Payload { get; }
    public RecorderIntegrityFlags IntegrityFlags { get; }
    public bool NativeSequenceAvailable { get; }
}
