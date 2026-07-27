using ATAS.Indicators;
using GC.AuctionFlow.Core;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Recorder.FanOut;
using GC.AuctionFlow.Recorder.Payloads;

namespace GC.AuctionFlow.Recorder;

/// <summary>
/// P0-07C3BC Trade recorder host: startup gate, dual probe/recorder fan-out, disposal.
/// Master disabled ⇒ NotConfigured. Startup failure does not suppress probe.
/// Callback path never creates directories / opens files / serializes / waits — only gates + pending request.
/// </summary>
public sealed class TradeRecorderHost : IDisposable
{
    private readonly object _gate = new();
    private readonly CallbackInvocationSequenceProvider _sequences = new();
    private RawEventRecorderSession? _session;
    private TradeToRawEventAdapter? _adapter;
    private Guid _processId = Guid.NewGuid();
    private bool _startupFailed;
    private bool _disposed;
    private int _contractEpoch = 1;
    private PendingRecorderStart? _pendingStart;

    private sealed class PendingRecorderStart
    {
        public required ObservedInstrumentSnapshot Observed { get; init; }
        public required string ExpectedInstrumentCode { get; init; }
        public required DataSourceMode DeclaredMode { get; init; }
        public required DataSourceModeProvenance ModeProvenance { get; init; }
        public required DeclaredFeedProvider DeclaredProvider { get; init; }
        public required FeedProviderProvenance ProviderProvenance { get; init; }
        public required Guid SessionId { get; init; }
        public required TradeStreamAtasMapper TradeMapper { get; init; }
        public string? UserProfileOverride { get; init; }
        public required bool EnableTradeRecording { get; init; }
    }

    public RecorderCounters Counters { get; } = new();
    public RawEventRecorderSession? Session => _session;
    public bool IsAccepting => _session is { IsAccepting: true };

    /// <summary>Empty until a session starts. Callers must check <see cref="IsAccepting"/> first.</summary>
    public Guid SessionId => _session?.SessionId ?? Guid.Empty;

    public Guid ProcessId => _processId;

    /// <summary>
    /// Callback-safe: gate checks + enqueue pending start only. Never directory/file/JSON/hash/Wait.
    /// Returns SessionNotStarted until lifecycle completes startup.
    /// </summary>
    public FanOut.RecorderSinkOutcome EnsureStarted(
        bool enableRawEventRecorder,
        bool enableTradeRecording,
        ObservedInstrumentSnapshot? observed,
        string? expectedInstrumentCode,
        DataSourceMode declaredMode,
        DataSourceModeProvenance modeProvenance,
        DeclaredFeedProvider declaredProvider,
        FeedProviderProvenance providerProvenance,
        Guid sessionId,
        TradeStreamAtasMapper tradeMapper,
        string? userProfileOverride = null)
    {
        if (_disposed)
            return FanOut.RecorderSinkOutcome.StoppedAccepting;

        if (!enableRawEventRecorder)
            return FanOut.RecorderSinkOutcome.NotConfigured;

        if (!enableTradeRecording)
            return FanOut.RecorderSinkOutcome.StreamDisabled;

        _ = MboOperationalLock.MboRecordingEnabled;

        if (_startupFailed)
            return FanOut.RecorderSinkOutcome.Faulted;

        if (_session is not null)
            return _session.IsAccepting
                ? FanOut.RecorderSinkOutcome.Accepted
                : FanOut.RecorderSinkOutcome.StoppedAccepting;

        var tradeGate = TradeStreamProbeGates.Evaluate(
            enableTradeStreamProbe: true,
            declaredMode,
            modeProvenance,
            expectedInstrumentCode,
            observed);
        if (!tradeGate.Accepted)
            return FanOut.RecorderSinkOutcome.RejectedByGate;

        if (declaredProvider == DeclaredFeedProvider.Unknown || providerProvenance == FeedProviderProvenance.Unknown)
            return FanOut.RecorderSinkOutcome.RejectedByGate;

        if (observed is null || string.Equals(observed.IdentityKey, "Unknown", StringComparison.Ordinal))
            return FanOut.RecorderSinkOutcome.RejectedByGate;

        // Queue non-blocking startup request for OnCalculate lifecycle — no I/O here.
        lock (_gate)
        {
            if (_disposed)
                return FanOut.RecorderSinkOutcome.StoppedAccepting;
            if (_startupFailed)
                return FanOut.RecorderSinkOutcome.Faulted;
            if (_session is not null)
                return _session.IsAccepting
                    ? FanOut.RecorderSinkOutcome.Accepted
                    : FanOut.RecorderSinkOutcome.StoppedAccepting;

            _pendingStart = new PendingRecorderStart
            {
                Observed = observed,
                ExpectedInstrumentCode = expectedInstrumentCode ?? "",
                DeclaredMode = declaredMode,
                ModeProvenance = modeProvenance,
                DeclaredProvider = declaredProvider,
                ProviderProvenance = providerProvenance,
                SessionId = sessionId,
                TradeMapper = tradeMapper,
                UserProfileOverride = userProfileOverride,
                EnableTradeRecording = enableTradeRecording
            };
        }

        return FanOut.RecorderSinkOutcome.SessionNotStarted;
    }

    /// <summary>
    /// Lifecycle path (OnCalculate): may create session directories / start worker. Not called from Trade callbacks.
    /// </summary>
    public void TryCompleteStartupFromLifecycle()
    {
        PendingRecorderStart? pending;
        lock (_gate)
        {
            if (_disposed || _startupFailed || _session is not null)
                return;
            pending = _pendingStart;
            if (pending is null)
                return;
        }

        try
        {
            var cfg = new RecorderConfig();
            _ = ObservedInstrumentIdentityMapper.FromSnapshot(pending.Observed);
            var enabledStreams = pending.EnableTradeRecording
                ? new[] { "Trade" }
                : Array.Empty<string>();
            var adapter = new TradeToRawEventAdapter(pending.TradeMapper);
            var session = new RawEventRecorderSession(
                pending.SessionId,
                _processId,
                pending.DeclaredMode.ToString(),
                pending.ModeProvenance.ToString(),
                pending.DeclaredProvider.ToString(),
                pending.ProviderProvenance.ToString(),
                cfg,
                counters: Counters,
                userProfileOverride: pending.UserProfileOverride,
                enabledStreams: enabledStreams);

            lock (_gate)
            {
                if (_disposed || _startupFailed || _session is not null)
                {
                    try { session.Dispose(); } catch { /* contained */ }
                    return;
                }

                _adapter = adapter;
                _session = session;
                _pendingStart = null;
            }
        }
        catch
        {
            lock (_gate)
            {
                _startupFailed = true;
                _pendingStart = null;
                _session = null;
                _adapter = null;
            }

            Interlocked.Increment(ref Counters.RecorderStartupFailures);
        }
    }

    public void NoteCallbackBeforeStart()
    {
        Interlocked.Increment(ref Counters.CallbackInvocations);
        Interlocked.Increment(ref Counters.RecorderCallbacksBeforeStart);
    }

    public void NoteCallbackAfterStop()
    {
        Interlocked.Increment(ref Counters.CallbackInvocations);
        Interlocked.Increment(ref Counters.RecorderCallbacksAfterStop);
    }

    public void NoteRejectedByGate()
    {
        Interlocked.Increment(ref Counters.CallbackInvocations);
        Interlocked.Increment(ref Counters.RejectedCallbackInvocationsByGate);
    }

    private void NoteAuthorizedCallbackInvocation()
    {
        Interlocked.Increment(ref Counters.CallbackInvocations);
        Interlocked.Increment(ref Counters.AuthorizedCallbackInvocations);
    }

    public CallbackCaptureContext Capture(RecorderCallbackSource source) =>
        CallbackCaptureContext.CaptureNow(source, _sequences);

    /// <summary>
    /// Dual-map singular NewTrade: probe enqueue (caller) remains separate; this writes recorder market + inv-result.
    /// </summary>
    public void RecordNewTradeAfterProbe(
        MarketDataArg? trade,
        CallbackCaptureContext context,
        ObservedInstrumentIdentity instrument,
        string declaredMode,
        string modeProvenance,
        string declaredProvider,
        string providerProvenance)
    {
        if (_session is null || _adapter is null || !_session.IsAccepting)
        {
            NoteCallbackAfterStop();
            return;
        }

        NoteAuthorizedCallbackInvocation();

        long enumerated = 0;
        long nulls = 0;
        long normFails = 0;
        long fanOutRejects = 0;
        long fanOutFaults = 0;
        var completed = true;
        string? failType = null;

        if (trade is null)
        {
            enumerated = 1;
            nulls = 1;
            Interlocked.Increment(ref Counters.PayloadItemsEnumerated);
            Interlocked.Increment(ref Counters.NullItemObservations);
            EmitInvocationResult(
                context, false, true, enumerated, nulls, normFails, fanOutRejects, fanOutFaults, true, null,
                instrument, declaredMode, modeProvenance, declaredProvider, providerProvenance);
            return;
        }

        enumerated = 1;
        try
        {
            var item = _adapter.TryMapNewTrade(
                trade, context, 0, instrument,
                declaredMode, modeProvenance, declaredProvider, providerProvenance,
                _session.SessionId, _processId);
            if (item is null)
            {
                normFails = 1;
                Interlocked.Increment(ref Counters.PayloadItemsEnumerated);
                Interlocked.Increment(ref Counters.NormalizationFailures);
            }
            else
            {
                Interlocked.Increment(ref Counters.PayloadItemsEnumerated);
                ApplyRecorderItem(item, declaredMode, modeProvenance, declaredProvider, providerProvenance,
                    ref fanOutRejects, ref fanOutFaults);
            }
        }
        catch (Exception ex)
        {
            normFails = 1;
            failType = SinglePassEnumerationHelper.SanitizeStage("Mapper", ex);
            completed = false;
            Interlocked.Increment(ref Counters.PayloadItemsEnumerated);
            Interlocked.Increment(ref Counters.NormalizationFailures);
        }

        EmitInvocationResult(
            context, false, completed, enumerated, nulls, normFails, fanOutRejects, fanOutFaults,
            completed && failType is null, failType,
            instrument, declaredMode, modeProvenance, declaredProvider, providerProvenance);
    }

    /// <summary>
    /// Single-pass batch: for each raw item dual-maps probe (via callback) and recorder.
    /// </summary>
    /// <summary>
    /// Writes one depth or best-bid/ask callback.
    ///
    /// Passive only: the caller hands over what the platform already delivered. Nothing
    /// here subscribes or requests, which is what keeps it clear of the P0-06D finding.
    /// </summary>
    public bool ProcessDepthCallback(
        RawEventDraft? draft)
    {
        if (draft is null)
            return false;

        if (_session is null)
        {
            NoteCallbackBeforeStart();
            return false;
        }

        if (!_session.IsAccepting)
        {
            NoteCallbackAfterStop();
            return false;
        }

        NoteAuthorizedCallbackInvocation();

        try
        {
            Interlocked.Increment(ref Counters.NormalizedObservations);
            return _session.TryAcceptDraft(draft) == FanOut.RecorderSinkOutcome.Accepted;
        }
        catch
        {
            return false;
        }
    }

    public void ProcessNewTradesBatch(
        IEnumerable<MarketDataArg?>? trades,
        CallbackCaptureContext context,
        ObservedInstrumentIdentity instrument,
        string declaredMode,
        string modeProvenance,
        string declaredProvider,
        string providerProvenance,
        Action<MarketDataArg, int> probePerItem)
    {
        if (_session is null || _adapter is null)
        {
            // Still must not double-enumerate for probe — caller handles probe-only path when recorder absent.
            NoteCallbackBeforeStart();
            return;
        }

        if (!_session.IsAccepting)
        {
            NoteCallbackAfterStop();
            return;
        }

        NoteAuthorizedCallbackInvocation();

        long enumerated = 0;
        long nulls = 0;
        long normFails = 0;
        long fanOutRejects = 0;
        long fanOutFaults = 0;
        var completed = false;
        string? failType = null;
        var nextOrdinal = 0;

        if (trades is null)
        {
            EmitInvocationResult(
                context, true, true, 0, 0, 0, 0, 0, true, null,
                instrument, declaredMode, modeProvenance, declaredProvider, providerProvenance);
            return;
        }

        IEnumerator<MarketDataArg?>? e = null;
        try
        {
            try { e = trades.GetEnumerator(); }
            catch (Exception ex)
            {
                failType = SinglePassEnumerationHelper.SanitizeStage("GetEnumerator", ex);
                EmitInvocationResult(
                    context, true, false, 0, 0, 0, 0, 0, false, failType,
                    instrument, declaredMode, modeProvenance, declaredProvider, providerProvenance);
                return;
            }

            while (true)
            {
                bool moved;
                try { moved = e.MoveNext(); }
                catch (Exception ex)
                {
                    failType = SinglePassEnumerationHelper.SanitizeStage("MoveNext", ex);
                    break;
                }

                if (!moved)
                {
                    completed = true;
                    break;
                }

                if (nextOrdinal == int.MaxValue)
                {
                    failType = "CallbackItemOrdinalExhausted";
                    break;
                }

                var ordinal = nextOrdinal++;
                enumerated++;

                MarketDataArg? raw;
                try { raw = e.Current; }
                catch (Exception ex)
                {
                    failType = SinglePassEnumerationHelper.SanitizeStage("Current", ex);
                    break;
                }

                if (raw is null)
                {
                    nulls++;
                    Interlocked.Increment(ref Counters.PayloadItemsEnumerated);
                    Interlocked.Increment(ref Counters.NullItemObservations);
                    continue;
                }

                // Dual map: probe first (existing behavior), then recorder — independent containment.
                try { probePerItem(raw, ordinal); }
                catch { /* probe fault contained */ }

                try
                {
                    var item = _adapter.TryMapNewTrade(
                        raw, context, ordinal, instrument,
                        declaredMode, modeProvenance, declaredProvider, providerProvenance,
                        _session.SessionId, _processId);
                    if (item is null)
                    {
                        normFails++;
                        Interlocked.Increment(ref Counters.PayloadItemsEnumerated);
                        Interlocked.Increment(ref Counters.NormalizationFailures);
                    }
                    else
                    {
                        Interlocked.Increment(ref Counters.PayloadItemsEnumerated);
                        ApplyRecorderItem(item, declaredMode, modeProvenance, declaredProvider, providerProvenance,
                            ref fanOutRejects, ref fanOutFaults);
                    }
                }
                catch
                {
                    normFails++;
                    Interlocked.Increment(ref Counters.PayloadItemsEnumerated);
                    Interlocked.Increment(ref Counters.NormalizationFailures);
                }
            }
        }
        finally
        {
            e?.Dispose();
        }

        EmitInvocationResult(
            context, true, completed, enumerated, nulls, normFails, fanOutRejects, fanOutFaults,
            completed && failType is null, failType,
            instrument, declaredMode, modeProvenance, declaredProvider, providerProvenance);
    }

    public void RecordCumulativeAfterProbe(
        CumulativeTrade? trade,
        CallbackCaptureContext context,
        bool assignInstanceId,
        bool isUpdate,
        ObservedInstrumentIdentity instrument,
        string declaredMode,
        string modeProvenance,
        string declaredProvider,
        string providerProvenance)
    {
        if (_session is null || _adapter is null || !_session.IsAccepting)
        {
            NoteCallbackAfterStop();
            return;
        }

        NoteAuthorizedCallbackInvocation();

        long enumerated = 0;
        long nulls = 0;
        long normFails = 0;
        long fanOutRejects = 0;
        long fanOutFaults = 0;
        var completed = true;
        string? failType = null;

        if (trade is null)
        {
            EmitInvocationResult(
                context, false, true, 1, 1, 0, 0, 0, true, null,
                instrument, declaredMode, modeProvenance, declaredProvider, providerProvenance);
            return;
        }

        enumerated = 1;
        try
        {
            var item = _adapter.TryMapCumulative(
                trade, context, 0, assignInstanceId, isUpdate, instrument,
                declaredMode, modeProvenance, declaredProvider, providerProvenance,
                _session.SessionId, _processId);
            if (item is null)
            {
                normFails = 1;
                Interlocked.Increment(ref Counters.PayloadItemsEnumerated);
                Interlocked.Increment(ref Counters.NormalizationFailures);
            }
            else
            {
                Interlocked.Increment(ref Counters.PayloadItemsEnumerated);
                ApplyRecorderItem(item, declaredMode, modeProvenance, declaredProvider, providerProvenance,
                    ref fanOutRejects, ref fanOutFaults);
            }
        }
        catch (Exception ex)
        {
            normFails = 1;
            failType = SinglePassEnumerationHelper.SanitizeStage("Mapper", ex);
            completed = false;
            Interlocked.Increment(ref Counters.PayloadItemsEnumerated);
            Interlocked.Increment(ref Counters.NormalizationFailures);
        }

        EmitInvocationResult(
            context, false, completed, enumerated, nulls, normFails, fanOutRejects, fanOutFaults,
            completed && failType is null, failType,
            instrument, declaredMode, modeProvenance, declaredProvider, providerProvenance);
    }

    private void ApplyRecorderItem(
        PrimitiveFanOutItem item,
        string declaredMode,
        string modeProvenance,
        string declaredProvider,
        string providerProvenance,
        ref long fanOutRejects,
        ref long fanOutFaults)
    {
        try
        {
            var draft = TradeToRawEventAdapter.ToDraft(
                item, _session!.SessionId, _processId,
                declaredMode, modeProvenance, declaredProvider, providerProvenance, _contractEpoch);
            Interlocked.Increment(ref Counters.NormalizedObservations);
            var outcome = _session.TryAcceptDraft(draft);
            if (outcome == FanOut.RecorderSinkOutcome.Accepted)
                return;
            fanOutRejects++;
        }
        catch
        {
            fanOutFaults++;
        }
    }

    private void EmitInvocationResult(
        CallbackCaptureContext context,
        bool isBatch,
        bool enumerationCompleted,
        long enumerated,
        long nulls,
        long normFails,
        long fanOutRejects,
        long fanOutFaults,
        bool finalKnown,
        string? failType,
        ObservedInstrumentIdentity instrument,
        string declaredMode,
        string modeProvenance,
        string declaredProvider,
        string providerProvenance)
    {
        if (_session is null)
            return;

        try
        {
            var payload = new CallbackInvocationResultPayload(
                context.CallbackSource,
                context.CallbackInvocationSequence,
                context.CallbackReceiveUtc,
                context.CallbackReceiveStopwatchTimestamp,
                context.CallbackManagedThreadId,
                isBatch,
                enumerationCompleted,
                enumerated,
                nulls,
                normFails,
                fanOutRejects,
                fanOutFaults,
                finalKnown,
                failType);
            var draft = TradeToRawEventAdapter.ToInvocationResultDraft(
                payload, context, instrument,
                _session.SessionId, _processId,
                declaredMode, modeProvenance, declaredProvider, providerProvenance);
            _ = _session.TryWrite(draft);
        }
        catch
        {
            Interlocked.Increment(ref Counters.InvocationResultFaults);
        }
    }

    public void StopAndDispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                _session?.StopAccepting("IndicatorDispose");
                _session?.Dispose();
            }
            catch { /* contained */ }
            _session = null;
            _adapter = null;
        }
    }

    public void Dispose() => StopAndDispose();

    /// <summary>
    /// Test helper: one authorized batch invocation with N null items (avoids ATAS types in unit tests).
    /// </summary>
    public void ProcessSyntheticNullItemBatchForTests(
        CallbackCaptureContext context,
        ObservedInstrumentIdentity instrument,
        string declaredMode,
        string modeProvenance,
        string declaredProvider,
        string providerProvenance,
        int nullItemCount)
    {
        if (_session is null || _adapter is null || !_session.IsAccepting)
        {
            NoteCallbackAfterStop();
            return;
        }

        NoteAuthorizedCallbackInvocation();
        for (var i = 0; i < nullItemCount; i++)
        {
            Interlocked.Increment(ref Counters.PayloadItemsEnumerated);
            Interlocked.Increment(ref Counters.NullItemObservations);
        }

        EmitInvocationResult(
            context, true, true, nullItemCount, nullItemCount, 0, 0, 0, true, null,
            instrument, declaredMode, modeProvenance, declaredProvider, providerProvenance);
    }

    /// <summary>
    /// Test helper: authorized invocation with enumeration failure result (no ATAS types).
    /// </summary>
    public void ProcessSyntheticEnumerationFailureForTests(
        CallbackCaptureContext context,
        ObservedInstrumentIdentity instrument,
        string declaredMode,
        string modeProvenance,
        string declaredProvider,
        string providerProvenance)
    {
        if (_session is null || _adapter is null || !_session.IsAccepting)
        {
            NoteCallbackAfterStop();
            return;
        }

        NoteAuthorizedCallbackInvocation();
        Interlocked.Increment(ref Counters.BatchEnumerationFailures);
        EmitInvocationResult(
            context, true, false, 0, 0, 0, 0, 0, false, "GetEnumerator:InvalidOperationException",
            instrument, declaredMode, modeProvenance, declaredProvider, providerProvenance);
    }
}
