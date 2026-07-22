using GC.AuctionFlow.Recorder.Payloads;

namespace GC.AuctionFlow.Recorder.FanOut;

/// <summary>
/// Immutable primitive fan-out item for independent sinks. No ATAS types, no IEnumerable,
/// no provider/chart series objects, no object/dynamic/JsonElement market payloads beyond RawEventPayload.
/// </summary>
public sealed class PrimitiveFanOutItem
{
    public PrimitiveFanOutItem(
        CallbackCaptureContext callbackContext,
        int callbackItemOrdinal,
        long streamLocalCaptureSequence,
        ObservedInstrumentIdentity instrument,
        long sourceTimeTicks,
        DateTimeKind sourceDateTimeKind,
        RawEventPayloadKind payloadDiscriminator,
        RawEventPayload payload,
        RecorderIntegrityFlags integrityFlags,
        bool nativeSequenceAvailable)
    {
        if (callbackContext is null) throw new ArgumentNullException(nameof(callbackContext));
        if (instrument is null) throw new ArgumentNullException(nameof(instrument));
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        if (payload.PayloadKind != payloadDiscriminator)
            throw new ArgumentException("Payload kind must match discriminator.", nameof(payloadDiscriminator));

        CallbackContext = callbackContext;
        CallbackItemOrdinal = callbackItemOrdinal;
        StreamLocalCaptureSequence = streamLocalCaptureSequence;
        Instrument = instrument;
        SourceTimeTicks = sourceTimeTicks;
        SourceDateTimeKind = sourceDateTimeKind;
        PayloadDiscriminator = payloadDiscriminator;
        Payload = payload;
        IntegrityFlags = integrityFlags;
        NativeSequenceAvailable = nativeSequenceAvailable;
    }

    public CallbackCaptureContext CallbackContext { get; }
    public int CallbackItemOrdinal { get; }
    public long StreamLocalCaptureSequence { get; }
    public ObservedInstrumentIdentity Instrument { get; }
    public long SourceTimeTicks { get; }
    public DateTimeKind SourceDateTimeKind { get; }
    public RawEventPayloadKind PayloadDiscriminator { get; }
    public RawEventPayload Payload { get; }
    public RecorderIntegrityFlags IntegrityFlags { get; }
    public bool NativeSequenceAvailable { get; }
}

public enum CapabilitySinkOutcome
{
    Disabled = 1,
    Accepted = 2,
    Rejected = 3,
    Faulted = 4
}

public enum RecorderSinkOutcome
{
    NotConfigured = 1,
    StreamDisabled = 2,
    SessionNotStarted = 3,
    Accepted = 4,
    QueueFull = 5,
    StoppedAccepting = 6,
    Faulted = 7
}

public interface ICapabilityFanOutSink
{
    CapabilitySinkOutcome TryAccept(PrimitiveFanOutItem item);
}

public interface IRecorderDraftFanOutSink
{
    RecorderSinkOutcome TryAccept(PrimitiveFanOutItem item);
}

public readonly struct FanOutDispatchResult
{
    public FanOutDispatchResult(CapabilitySinkOutcome capability, RecorderSinkOutcome recorder)
    {
        Capability = capability;
        Recorder = recorder;
    }

    public CapabilitySinkOutcome Capability { get; }
    public RecorderSinkOutcome Recorder { get; }
}

/// <summary>
/// Callback-neutral coordinator. Operates only on immutable primitive items.
/// Deterministic call order: capability then recorder (infrastructure order only — not market/exchange order).
/// Independent TryAccept; capability disable/exception never suppresses recorder and vice versa.
/// No waiting, file I/O, serialization, hashing, or UI work.
/// </summary>
public sealed class PrimitiveFanOutCoordinator
{
    private readonly ICapabilityFanOutSink _capability;
    private readonly IRecorderDraftFanOutSink _recorder;

    public PrimitiveFanOutCoordinator(ICapabilityFanOutSink capability, IRecorderDraftFanOutSink recorder)
    {
        _capability = capability ?? throw new ArgumentNullException(nameof(capability));
        _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
    }

    public FanOutDispatchResult Dispatch(PrimitiveFanOutItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var cap = CapabilitySinkOutcome.Faulted;
        var rec = RecorderSinkOutcome.Faulted;

        try
        {
            cap = _capability.TryAccept(item);
        }
        catch
        {
            cap = CapabilitySinkOutcome.Faulted;
        }

        try
        {
            rec = _recorder.TryAccept(item);
        }
        catch
        {
            rec = RecorderSinkOutcome.Faulted;
        }

        return new FanOutDispatchResult(cap, rec);
    }
}

/// <summary>Test/disabled capability sink. Outcome is closed enum — never inferred from exception text.</summary>
public sealed class FixedCapabilityFanOutSink : ICapabilityFanOutSink
{
    private readonly CapabilitySinkOutcome _outcome;
    private readonly bool _throw;

    public FixedCapabilityFanOutSink(CapabilitySinkOutcome outcome, bool throwOnAccept = false)
    {
        _outcome = outcome;
        _throw = throwOnAccept;
    }

    public long AcceptCalls;

    public CapabilitySinkOutcome TryAccept(PrimitiveFanOutItem item)
    {
        Interlocked.Increment(ref AcceptCalls);
        if (_throw) throw new InvalidOperationException("CapabilitySinkFault");
        return _outcome;
    }
}

/// <summary>Test/disabled recorder draft sink. Outcome is closed enum — never inferred from exception text.</summary>
public sealed class FixedRecorderDraftFanOutSink : IRecorderDraftFanOutSink
{
    private readonly RecorderSinkOutcome _outcome;
    private readonly bool _throw;

    public FixedRecorderDraftFanOutSink(RecorderSinkOutcome outcome, bool throwOnAccept = false)
    {
        _outcome = outcome;
        _throw = throwOnAccept;
    }

    public long AcceptCalls;
    public PrimitiveFanOutItem? LastItem;

    public RecorderSinkOutcome TryAccept(PrimitiveFanOutItem item)
    {
        Interlocked.Increment(ref AcceptCalls);
        LastItem = item;
        if (_throw) throw new InvalidOperationException("RecorderSinkFault");
        return _outcome;
    }
}
