using System.Text.Json.Serialization;
using GC.AuctionFlow.Recorder.Payloads;

namespace GC.AuctionFlow.Recorder;

/// <summary>
/// Callback-side immutable primitive draft. No SegmentId / writer global sequence.
/// Never retains ATAS objects.
/// ReceiveUtc is a schema 1.0.0 compatibility alias exactly equal to CallbackReceiveUtc.
/// </summary>
public sealed class RawEventDraft
{
    public RawEventDraft(
        string recorderSchemaVersion,
        Guid sessionId,
        Guid recorderProcessInstanceId,
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
        if (payload.PayloadKind != payloadDiscriminator)
            throw new ArgumentException("Payload kind must match discriminator.", nameof(payloadDiscriminator));
        if (instrument is null) throw new ArgumentNullException(nameof(instrument));

        RecorderSchemaVersion = recorderSchemaVersion;
        SessionId = sessionId;
        RecorderProcessInstanceId = recorderProcessInstanceId;
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

    public string RecorderSchemaVersion { get; }
    public Guid SessionId { get; }
    public Guid RecorderProcessInstanceId { get; }
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

    /// <summary>Compatibility alias: exactly CallbackReceiveUtc (schema 1.0.0 name).</summary>
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
