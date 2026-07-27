using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Recorder.Payloads;

namespace GC.AuctionFlow.Recorder;

/// <summary>
/// Turns MBO observations into recorder drafts.
///
/// v1.2 §46.5 lists MBO queue lifecycle, pulling, stacking, order cancellation and
/// iceberg/stop/sweep events among the things no downloaded history contains. They were
/// unrecordable for the life of the project — not because of a gate, but because no path
/// existed: `OnMarketByOrdersChanged` fed the lifecycle probe and nothing wrote frames.
///
/// The lock that sat over it was `P0-06D`, which attributed an abnormal chart bar to MBO
/// subscription. That attribution turned out to be wrong: the bar was caused by this
/// project's own `Flush(flushToDisk: true)` running inside `OnCalculate`, stalling the
/// platform's data pump. With that moved off-thread, enabling the MBO probe and restarting
/// reproduced nothing. The lock is therefore lifted for recording, and MBO capture becomes
/// an operator decision rather than a prohibition.
///
/// What is **not** lifted: `MboLifecycleCompletenessClaim` and
/// `StableMboBookReconstruction` remain false. Nothing has tested whether the MBO stream is
/// complete, and recording an incomplete stream is useful only if the recording says so —
/// which is why every frame carries the integrity flags the probe established.
/// </summary>
public static class MboToRawEventAdapter
{
    public static RawEventDraft? TryDraft(
        MboObservation? observation,
        CallbackCaptureContext context,
        ObservedInstrumentIdentity instrument,
        Guid sessionId,
        Guid processId,
        string declaredMode,
        string modeProvenance,
        string declaredProvider,
        string providerProvenance,
        int contractEpoch = 1)
    {
        if (observation is null)
            return null;

        var payload = new MboPayload(
            observation.RawTypeName,
            observation.RawTypeNumeric,
            observation.RawTypeIsKnownEnumMember,
            observation.RawSideName,
            observation.RawSideNumeric,
            observation.DerivedSide.ToString(),
            observation.ExchangeOrderId,
            observation.Price,
            observation.Volume,
            observation.Priority);

        // The interpreted lifecycle action is deliberately not folded into the payload.
        // It is the probe's reading of a raw type, and a recording that stores an
        // interpretation beside the raw value it came from invites a later reader to treat
        // the two as equally observed. The raw type and numeric are both here; anyone can
        // re-derive the action, and re-derive it differently.
        return new RawEventDraft(
            RawEventRecorderVersions.RawEventRecorderSchemaVersion,
            sessionId,
            processId,
            RecorderStreamKind.Mbo,
            context.CallbackSource,
            observation.LocalMonotonicSequence,
            context.CallbackInvocationSequence,
            callbackItemOrdinal: 0,
            subscriptionOrCaptureEpoch: observation.SubscriptionEpoch,
            contractEpoch,
            instrument,
            declaredMode,
            modeProvenance,
            declaredProvider,
            providerProvenance,
            observation.SourceTimeTicks,
            observation.SourceDateTimeKind,
            context.CallbackReceiveUtc,
            context.CallbackReceiveStopwatchTimestamp,
            context.CallbackManagedThreadId,
            RawEventPayloadKind.Mbo,
            payload,
            // Snapshot completion is not knowable from the callback stream alone — P0-06B
            // established that and it is unchanged. Flagging it on every frame keeps a
            // future reader from assuming the book was ever whole.
            RecorderIntegrityFlags.ProviderSnapshotCompletionUnknown
            | (observation.SourceDateTimeKind == DateTimeKind.Unspecified
                ? RecorderIntegrityFlags.SourceTimeKindUnspecified
                : RecorderIntegrityFlags.None),
            nativeSequenceAvailable: false);
    }
}
