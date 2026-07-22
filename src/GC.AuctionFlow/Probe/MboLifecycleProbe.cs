using System.Threading.Channels;
using GC.AuctionFlow.Core;

namespace GC.AuctionFlow.Probe;

/// <summary>
/// Bounded non-blocking MBO lifecycle semantics probe. Diagnostic only — no production book,
/// no lifecycle interpretation beyond raw enums, no ATAS retention in core processing.
/// </summary>
public sealed class MboLifecycleProbe : IDisposable
{
    private readonly MboLifecycleProbeConfig _config;
    private readonly Channel<MboObservation> _channel;
    private readonly MboLifecycleProbeCounters _counters = new();
    private readonly MboOrderObservationState _orderState;
    private readonly List<MboObservation> _samples = new();
    private readonly Dictionary<int, long> _threadCounts = new();
    private readonly Dictionary<int, Dictionary<int, long>> _rawTypeByThread = new();
    private readonly Dictionary<DateTimeKind, long> _kindCounts = new();
    private readonly HashSet<long> _distinctPriorities = new();
    private readonly object _sampleGate = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task? _worker;
    private readonly List<string> _limitations = new();
    private readonly List<string> _integrity = new();

    private int _accepting = 1;
    private int _disposed;
    private int _frozen;
    private readonly long _captureSubscriptionEpoch = 1;
    private long _finalClosedEpoch;
    private int _subscriptionState = (int)MboSubscriptionState.NotAttempted;
    private int _subscribeAttemptCount;
    private int _duplicateSubscribeSuppressed;
    private int _subscribeTriggerCheckCount;
    private ObservedInstrumentSnapshot? _instrument;
    private string? _lastGateReason;
    private int _lastGateReasonCode;
    private long _sequence;
    private long? _lastSourceTicks;
    private long _sourceNonDecreasing;
    private long _sourceDecreasing;
    private long? _priorityMin;
    private long? _priorityMax;
    private DateTime? _subscriptionAttemptUtc;
    private DateTime? _subscriptionTaskCompletedUtc;
    private DateTime? _firstCallbackReceiveUtc;
    private string? _taskFaultType;
    private string? _taskFaultMessageSanitized;
    private bool _taskCanceled;
    private MboLifecycleProbeSnapshot? _frozenSnapshot;
    private int _firstCallbackBatchActive;

    // Snapshot / initial-window study
    private DateTime? _firstSnapshotReceiveUtc;
    private DateTime? _lastSnapshotReceiveUtc;
    private long _snapshotObsCount;
    private readonly HashSet<long> _snapshotNonzeroIds = new();
    private long _snapshotZeroIds;
    private bool _nonSnapshotBeforeLastSnapshot;
    private bool _snapshotSeenAfterInitialTimeWindow;
    private readonly List<int> _firstCallbackBatchRawTypes = new();
    private readonly List<int> _initialTimeWindowRawTypes = new();
    private long _firstCallbackBatchItemCount;
    private long _initialTimeWindowItemCount;

    public MboLifecycleProbe(MboLifecycleProbeConfig? config = null, bool startWorker = true)
    {
        _config = config ?? new MboLifecycleProbeConfig();
        _orderState = new MboOrderObservationState(_config.MaxTrackedNonzeroIds);
        _channel = Channel.CreateBounded<MboObservation>(new BoundedChannelOptions(_config.QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
        _worker = startWorker ? Task.Run(ProcessLoopAsync) : null;
    }

    public MboLifecycleProbeCounters Counters => _counters;
    public MboLifecycleProbeConfig Config => _config;
    public bool IsAccepting => Volatile.Read(ref _accepting) == 1;
    /// <summary>Epoch stamped on observations during capture. Stable for the instance capture lifetime.</summary>
    public long CaptureSubscriptionEpoch => _captureSubscriptionEpoch;
    /// <summary>Alias for CaptureSubscriptionEpoch — used by indicator mapping.</summary>
    public long SubscriptionEpoch => _captureSubscriptionEpoch;
    public long FinalClosedEpoch => Volatile.Read(ref _finalClosedEpoch);
    public MboSubscriptionState SubscriptionState =>
        (MboSubscriptionState)Volatile.Read(ref _subscriptionState);
    public string? LastGateReason => Volatile.Read(ref _lastGateReason);
    public int SubscribeTriggerCheckCount => Volatile.Read(ref _subscribeTriggerCheckCount);
    public int DuplicateSubscribeSuppressed => Volatile.Read(ref _duplicateSubscribeSuppressed);

    public void SetObservedInstrument(ObservedInstrumentSnapshot snapshot) =>
        Volatile.Write(ref _instrument, snapshot);

    public ObservedInstrumentSnapshot? GetObservedInstrument() => Volatile.Read(ref _instrument);

    public long NextSequence() => Interlocked.Increment(ref _sequence);

    public void RecordLimitation(string text)
    {
        lock (_sampleGate) _limitations.Add(text);
    }

    public void RecordIntegrity(string text)
    {
        lock (_sampleGate) _integrity.Add(text);
    }

    public MboLifecycleProbeGates.GateDecision EvaluateGates(
        bool enable,
        DataSourceMode mode,
        DataSourceModeProvenance modeProvenance,
        string? expected,
        DeclaredFeedProvider feed,
        FeedProviderProvenance feedProv)
    {
        var gate = MboLifecycleProbeGates.Evaluate(
            enable, mode, modeProvenance, expected, _instrument, feed, feedProv);
        if (!gate.Accepted)
        {
            Volatile.Write(ref _lastGateReason, gate.RejectReason);
            Volatile.Write(ref _lastGateReasonCode, (int)gate.Reason);
        }

        return gate;
    }

    /// <summary>
    /// Subscribe exactly once per instance after gates pass. Invokes startSubscribe which must
    /// return the ATAS Task from SubscribeMarketByOrderData(). Observes every Task outcome.
    /// Ordinary later OnCalculate checks early-return without inflating duplicateSubscribeSuppressed.
    /// </summary>
    public bool TrySubscribeOnce(
        bool enable,
        DataSourceMode mode,
        DataSourceModeProvenance modeProvenance,
        string? expectedInstrumentCode,
        DeclaredFeedProvider feedProvider,
        FeedProviderProvenance feedProvenance,
        Func<Task> startSubscribe)
    {
        if (Volatile.Read(ref _disposed) == 1 || Volatile.Read(ref _accepting) != 1)
            return false;

        Interlocked.Increment(ref _subscribeTriggerCheckCount);

        // Cheap early-return after first dispatch — not a duplicate subscription attempt.
        if (Volatile.Read(ref _subscriptionState) != (int)MboSubscriptionState.NotAttempted)
            return false;

        var gate = EvaluateGates(enable, mode, modeProvenance, expectedInstrumentCode, feedProvider, feedProvenance);
        if (!gate.Accepted)
            return false;

        // Atomic: NotAttempted → Attempted. Failure here is a genuine concurrent/lifecycle duplicate.
        if (Interlocked.CompareExchange(
                ref _subscriptionState,
                (int)MboSubscriptionState.Attempted,
                (int)MboSubscriptionState.NotAttempted) != (int)MboSubscriptionState.NotAttempted)
        {
            Interlocked.Increment(ref _duplicateSubscribeSuppressed);
            return false;
        }

        Interlocked.Increment(ref _subscribeAttemptCount);
        var attemptUtc = DateTime.UtcNow;
        lock (_sampleGate)
            _subscriptionAttemptUtc = attemptUtc;

        Task task;
        try
        {
            task = startSubscribe() ?? Task.CompletedTask;
        }
        catch (Exception ex)
        {
            RecordTaskFault(ex);
            return true;
        }

        _ = ObserveSubscriptionTaskAsync(task);
        return true;
    }

    private async Task ObserveSubscriptionTaskAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
            var completedUtc = DateTime.UtcNow;
            lock (_sampleGate)
                _subscriptionTaskCompletedUtc = completedUtc;

            // Do not reopen acceptance if already stopped/disposed.
            var current = Volatile.Read(ref _subscriptionState);
            if (current is (int)MboSubscriptionState.StoppedAccepting or (int)MboSubscriptionState.Disposed)
                return;

            Interlocked.CompareExchange(
                ref _subscriptionState,
                (int)MboSubscriptionState.TaskCompleted,
                (int)MboSubscriptionState.Attempted);
        }
        catch (OperationCanceledException)
        {
            lock (_sampleGate)
            {
                _taskCanceled = true;
                _subscriptionTaskCompletedUtc = DateTime.UtcNow;
            }

            var current = Volatile.Read(ref _subscriptionState);
            if (current is (int)MboSubscriptionState.StoppedAccepting or (int)MboSubscriptionState.Disposed)
                return;
            Interlocked.CompareExchange(
                ref _subscriptionState,
                (int)MboSubscriptionState.TaskCanceled,
                (int)MboSubscriptionState.Attempted);
        }
        catch (Exception ex)
        {
            RecordTaskFault(ex);
        }
    }

    private void RecordTaskFault(Exception ex)
    {
        lock (_sampleGate)
        {
            _taskFaultType = ex.GetType().FullName;
            _taskFaultMessageSanitized = SanitizeMessage(ex.Message);
            _subscriptionTaskCompletedUtc = DateTime.UtcNow;
        }

        var current = Volatile.Read(ref _subscriptionState);
        if (current is (int)MboSubscriptionState.StoppedAccepting or (int)MboSubscriptionState.Disposed)
            return;
        Interlocked.CompareExchange(
            ref _subscriptionState,
            (int)MboSubscriptionState.TaskFaulted,
            (int)MboSubscriptionState.Attempted);
    }

    private static string SanitizeMessage(string? message)
    {
        if (string.IsNullOrEmpty(message)) return "";
        var t = message.Trim();
        return t.Length <= 240 ? t : t[..240];
    }

    public void BeginCallback(DateTime receiveUtc)
    {
        var n = Interlocked.Increment(ref _callbackBeginCount);
        _counters.IncCallbackInvocations();
        lock (_sampleGate)
        {
            _firstCallbackReceiveUtc ??= receiveUtc;
            _firstCallbackBatchActive = n == 1 ? 1 : 0;
        }
    }

    private int _callbackBeginCount;

    /// <summary>
    /// Test/helper path: process already-mapped observations as one batch (enumerate once semantics).
    /// </summary>
    public void HandleMappedBatch(
        IEnumerable<MboObservation?>? observations,
        DateTime receiveUtc,
        int threadId,
        bool enable,
        DataSourceMode mode,
        DataSourceModeProvenance modeProvenance,
        string? expectedInstrumentCode,
        DeclaredFeedProvider feedProvider,
        FeedProviderProvenance feedProvenance)
    {
        try
        {
            BeginCallback(receiveUtc);

            if (Volatile.Read(ref _accepting) != 1)
            {
                _counters.IncRejectedAfterDispose();
                return;
            }

            var epoch = _captureSubscriptionEpoch;
            var gate = EvaluateGates(enable, mode, modeProvenance, expectedInstrumentCode, feedProvider, feedProvenance);
            if (!gate.Accepted)
            {
                if (gate.InstrumentGate) _counters.IncRejectedByInstrumentGate();
                else _counters.IncRejectedByModeGate();
                return;
            }

            if (observations is null)
            {
                _counters.IncNullBatch();
                return;
            }

            var enumerated = 0;
            var any = false;
            foreach (var obs in observations)
            {
                any = true;
                enumerated++;
                if (obs is null)
                {
                    _counters.IncNullItem();
                    continue;
                }

                if (obs.SubscriptionEpoch != epoch)
                {
                    _counters.IncRejectedByEpochGate();
                    continue;
                }

                if (Volatile.Read(ref _accepting) != 1)
                {
                    _counters.IncRejectedAfterDispose();
                    continue;
                }

                EnqueueObservation(obs);
            }

            _counters.AddBatchItems(enumerated);
            if (!any)
                _counters.IncEmptyBatch();
        }
        catch
        {
            _counters.IncBatchEnumerationFailures();
        }
    }

    public bool TryEnqueueMapped(MboObservation observation)
    {
        if (Volatile.Read(ref _accepting) != 1)
        {
            _counters.IncRejectedAfterDispose();
            return false;
        }

        if (observation.SubscriptionEpoch != _captureSubscriptionEpoch)
        {
            _counters.IncRejectedByEpochGate();
            return false;
        }

        return EnqueueObservation(observation);
    }

    private bool EnqueueObservation(MboObservation observation)
    {
        lock (_sampleGate)
        {
            if (_firstCallbackBatchActive == 1)
            {
                _firstCallbackBatchItemCount++;
                if (_firstCallbackBatchRawTypes.Count < MboLifecycleProbeVersions.FirstCallbackBatchRawTypeCap)
                    _firstCallbackBatchRawTypes.Add(observation.RawTypeNumeric);
            }
        }

        _counters.IncSide(observation.DerivedSide);
        _counters.IncExchangeOrderId(observation.ExchangeOrderId);
        _counters.IncRawType(observation.RawTypeNumeric);
        _counters.IncPriority(observation.Priority);

        if (_channel.Writer.TryWrite(observation))
        {
            _counters.IncAcceptedToQueue();
            return true;
        }

        _counters.IncQueueFullDrops();
        return false;
    }

    private async Task ProcessLoopAsync()
    {
        try
        {
            await foreach (var item in _channel.Reader.ReadAllAsync(_cts.Token).ConfigureAwait(false))
            {
                try
                {
                    ProcessItem(item);
                    _counters.IncProcessedByWorker();
                }
                catch (Exception ex)
                {
                    _counters.IncNormalizationFailures();
                    RecordIntegrity("WorkerProcessFailure:" + ex.GetType().Name);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            RecordIntegrity("WorkerLoopFailure:" + ex.GetType().Name);
        }
    }

    private void ProcessItem(MboObservation obs)
    {
        _orderState.Observe(obs);

        lock (_sampleGate)
        {
            if (_samples.Count < _config.MaxSamples)
                _samples.Add(obs);

            _threadCounts.TryGetValue(obs.CallbackManagedThreadId, out var tc);
            _threadCounts[obs.CallbackManagedThreadId] = tc + 1;

            if (!_rawTypeByThread.TryGetValue(obs.CallbackManagedThreadId, out var byType))
            {
                byType = new Dictionary<int, long>();
                _rawTypeByThread[obs.CallbackManagedThreadId] = byType;
            }

            byType.TryGetValue(obs.RawTypeNumeric, out var rtc);
            byType[obs.RawTypeNumeric] = rtc + 1;

            _kindCounts.TryGetValue(obs.SourceDateTimeKind, out var kc);
            _kindCounts[obs.SourceDateTimeKind] = kc + 1;

            if (_lastSourceTicks is null)
                _lastSourceTicks = obs.SourceTimeTicks;
            else if (obs.SourceTimeTicks >= _lastSourceTicks.Value)
                _sourceNonDecreasing++;
            else
                _sourceDecreasing++;
            _lastSourceTicks = obs.SourceTimeTicks;

            _distinctPriorities.Add(obs.Priority);
            if (_priorityMin is null || obs.Priority < _priorityMin) _priorityMin = obs.Priority;
            if (_priorityMax is null || obs.Priority > _priorityMax) _priorityMax = obs.Priority;

            UpdateSnapshotStudy(obs);
        }
    }

    private void UpdateSnapshotStudy(MboObservation obs)
    {
        if (obs.RawTypeNumeric == MboKnownRawUpdateTypes.Snapshot)
        {
            _snapshotObsCount++;
            _firstSnapshotReceiveUtc ??= obs.ReceiveUtc;
            _lastSnapshotReceiveUtc = obs.ReceiveUtc;
            if (obs.ExchangeOrderId == 0) _snapshotZeroIds++;
            else _snapshotNonzeroIds.Add(obs.ExchangeOrderId);
        }
        else if (_lastSnapshotReceiveUtc is not null
                 && obs.ReceiveUtc <= _lastSnapshotReceiveUtc.Value)
        {
            _nonSnapshotBeforeLastSnapshot = true;
        }

        // Window starts at FirstCallbackReceiveUtc (seed 5s). Receive timestamps only — not sample index.
        if (_firstCallbackReceiveUtc is not null)
        {
            var elapsedSec = (obs.ReceiveUtc - _firstCallbackReceiveUtc.Value).TotalSeconds;
            if (elapsedSec <= MboLifecycleProbeVersions.InitialTimeWindowDurationSeconds)
            {
                _initialTimeWindowItemCount++;
                if (_initialTimeWindowRawTypes.Count < MboLifecycleProbeVersions.InitialTimeWindowRawTypeCap)
                    _initialTimeWindowRawTypes.Add(obs.RawTypeNumeric);
            }

            if (obs.RawTypeNumeric == MboKnownRawUpdateTypes.Snapshot
                && elapsedSec > MboLifecycleProbeVersions.InitialTimeWindowDurationSeconds)
            {
                _snapshotSeenAfterInitialTimeWindow = true;
            }
        }
    }

    public MboLifecycleProbeSnapshot FreezeSnapshot(
        DataSourceMode mode,
        DataSourceModeProvenance modeProvenance,
        string? expectedInstrumentCode,
        DeclaredFeedProvider feedProvider,
        FeedProviderProvenance feedProvenance,
        Guid sessionId,
        bool enable)
    {
        if (Interlocked.Exchange(ref _frozen, 1) == 1 && _frozenSnapshot is not null)
            return _frozenSnapshot;

        MboObservation[] samples;
        string[] limitations;
        string[] integrity;
        Dictionary<int, long> threads;
        Dictionary<string, long> kinds;
        Dictionary<string, Dictionary<string, long>> rawByThread;
        MboOrderObservationStateSummary orderSummary;
        MboSnapshotStudyResult snapshotStudy;
        long srcNonDec, srcDec;
        long? pMin, pMax;
        int distinctPriority;
        lock (_sampleGate)
        {
            samples = _samples.ToArray();
            limitations = BuildLimitations();
            integrity = _integrity.ToArray();
            threads = new Dictionary<int, long>(_threadCounts);
            kinds = _kindCounts.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);
            rawByThread = _rawTypeByThread.ToDictionary(
                kv => kv.Key.ToString(System.Globalization.CultureInfo.InvariantCulture),
                kv => kv.Value.ToDictionary(
                    x => x.Key.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    x => x.Value));
            orderSummary = _orderState.Snapshot();
            srcNonDec = _sourceNonDecreasing;
            srcDec = _sourceDecreasing;
            pMin = _priorityMin;
            pMax = _priorityMax;
            distinctPriority = _distinctPriorities.Count;
            snapshotStudy = BuildSnapshotStudy_NoLock();
        }

        var gate = MboLifecycleProbeGates.Evaluate(
            enable, mode, modeProvenance, expectedInstrumentCode, _instrument, feedProvider, feedProvenance);
        var counters = _counters.Snapshot();
        var callbackStatus = counters.CallbackInvocations > 0
            ? CallbackObservationStatusNames.Observed
            : CallbackObservationStatusNames.NotObservedInTestWindow;

        DateTime? attempt;
        DateTime? taskDone;
        DateTime? firstCb;
        string? faultType;
        string? faultMsg;
        bool taskCanceled;
        lock (_sampleGate)
        {
            attempt = _subscriptionAttemptUtc;
            taskDone = _subscriptionTaskCompletedUtc;
            firstCb = _firstCallbackReceiveUtc;
            faultType = _taskFaultType;
            faultMsg = _taskFaultMessageSanitized;
            taskCanceled = _taskCanceled;
        }

        var snap = new MboLifecycleProbeSnapshot(
            schemaVersion: MboLifecycleProbeVersions.MboLifecycleProbeSchemaVersion,
            probeVersion: MboLifecycleProbeVersions.ProbeVersion,
            sessionId: sessionId,
            createdUtc: DateTime.UtcNow,
            continuityDisclaimer: MboLifecycleProbeVersions.ContinuityDisclaimer,
            atasIndicatorsAssemblyVersion: MboLifecycleProbeVersions.AtasIndicatorsAssemblyVersion,
            declaredDataSourceMode: mode,
            dataSourceModeProvenance: modeProvenance,
            declaredFeedProvider: feedProvider,
            feedProviderProvenance: feedProvenance,
            expectedInstrumentCode: expectedInstrumentCode,
            observedInstrument: _instrument,
            captureAuthorized: gate.Accepted,
            gateReason: gate.Accepted ? null : TradeStreamGateReasonNames.ToWire(gate.Reason),
            lastGateReason: gate.RejectReason ?? _lastGateReason,
            apiSubscriptionMethod: "ExtendedIndicator.SubscribeMarketByOrderData",
            apiCallbackMethod: "ExtendedIndicator.OnMarketByOrdersChanged",
            captureSubscriptionEpoch: _captureSubscriptionEpoch,
            finalClosedEpoch: Volatile.Read(ref _finalClosedEpoch),
            subscriptionState: SubscriptionState,
            subscriptionAttemptCount: Volatile.Read(ref _subscribeAttemptCount),
            duplicateSubscribeSuppressed: Volatile.Read(ref _duplicateSubscribeSuppressed),
            subscribeTriggerCheckCount: Volatile.Read(ref _subscribeTriggerCheckCount),
            subscriptionAttemptUtc: attempt,
            subscriptionTaskCompletedUtc: taskDone,
            taskFaultType: faultType,
            taskFaultMessageSanitized: faultMsg,
            taskCanceled: taskCanceled,
            firstCallbackReceiveUtc: firstCb,
            attemptToTaskCompletion: attempt is not null && taskDone is not null ? taskDone - attempt : null,
            attemptToFirstCallback: attempt is not null && firstCb is not null ? firstCb - attempt : null,
            callbackObservationStatus: callbackStatus,
            mboRuntimeEventPresenceObserved: counters.CallbackInvocations > 0,
            rawEnumDefinitions: new[]
            {
                new MboRawEnumDefinition("MarketByOrderUpdateTypes", "Snapshot", 0),
                new MboRawEnumDefinition("MarketByOrderUpdateTypes", "New", 1),
                new MboRawEnumDefinition("MarketByOrderUpdateTypes", "Change", 2),
                new MboRawEnumDefinition("MarketByOrderUpdateTypes", "Delete", 3),
                new MboRawEnumDefinition("MarketDataType", "Bid", 0),
                new MboRawEnumDefinition("MarketDataType", "Ask", 1),
                new MboRawEnumDefinition("MarketDataType", "Trade", 2)
            },
            counters: counters,
            orderObservationState: orderSummary,
            snapshotStudy: snapshotStudy,
            priorityStudy: new MboPriorityStudyResult(
                ZeroPriorityCount: counters.PriorityZero,
                NegativePriorityCount: counters.PriorityNegative,
                MinPriority: pMin,
                MaxPriority: pMax,
                DistinctPriorityValues: distinctPriority,
                OpaqueNote: "Priority is an opaque long — no queue-position or fill-probability inference."),
            sourceTimeNonDecreasing: srcNonDec,
            sourceTimeDecreasing: srcDec,
            managedThreadIdCounts: threads,
            dateTimeKindCounts: kinds,
            rawEventTypeByThread: rawByThread,
            samples: samples,
            knownLimitations: limitations,
            integrityEvents: integrity,
            queueCapacity: _config.QueueCapacity,
            providerUnsubscribePerformed: false,
            providerUnsubscribeAvailability: MboLifecycleProbeVersions.ProviderUnsubscribeAvailability,
            liveMboCapabilityClaim: false,
            mboLifecycleCompletenessClaim: false,
            stableMboBookReconstruction: false,
            nativeSequenceAvailable: false,
            icebergInferenceAvailable: false,
            spoofingInferenceAvailable: false,
            queuePositionInferenceAvailable: false,
            executionInferenceAvailable: false,
            snapshotCompletionKnown: false,
            interpretedLifecycleAction: nameof(MboInterpretedLifecycleAction.Unknown));

        _frozenSnapshot = snap;
        return snap;
    }

    private MboSnapshotStudyResult BuildSnapshotStudy_NoLock()
    {
        TimeSpan? duration = null;
        if (_firstSnapshotReceiveUtc is not null && _lastSnapshotReceiveUtc is not null)
            duration = _lastSnapshotReceiveUtc - _firstSnapshotReceiveUtc;

        return new MboSnapshotStudyResult(
            FirstSnapshotReceiveUtc: _firstSnapshotReceiveUtc,
            LastSnapshotReceiveUtc: _lastSnapshotReceiveUtc,
            SnapshotObservationCount: _snapshotObsCount,
            DistinctNonzeroIdsWithSnapshot: _snapshotNonzeroIds.Count,
            ZeroIdsWithSnapshot: _snapshotZeroIds,
            FirstCallbackBatchItemCount: _firstCallbackBatchItemCount,
            FirstCallbackBatchRawTypeNumerics: _firstCallbackBatchRawTypes.ToArray(),
            InitialTimeWindowDurationSeconds: MboLifecycleProbeVersions.InitialTimeWindowDurationSeconds,
            InitialTimeWindowItemCount: _initialTimeWindowItemCount,
            InitialTimeWindowRawTypeNumerics: _initialTimeWindowRawTypes.ToArray(),
            NonSnapshotArrivedBeforeLastSnapshot: _nonSnapshotBeforeLastSnapshot,
            DurationFirstToLastSnapshot: duration,
            SnapshotSeenAfterInitialTimeWindow: _snapshotSeenAfterInitialTimeWindow,
            SnapshotCompletionKnown: false,
            Limitation: "SnapshotCompletionKnown=false — no explicit MBO snapshot completion API observed. "
                + "Initial time window is 5s from FirstCallbackReceiveUtc (seed value, subject to sensitivity test).");
    }

    private string[] BuildLimitations()
    {
        var list = new List<string>(_limitations)
        {
            MboLifecycleProbeVersions.ContinuityDisclaimer,
            "No MBO-specific unsubscribe API observed; ProviderUnsubscribePerformed=false.",
            "Raw MarketByOrderUpdateTypes names do not prove exchange lifecycle semantics.",
            "MboInterpretedLifecycleAction=Unknown always in P0-06.",
            "ExchangeOrderId uniqueness/stability/reuse remains Unknown.",
            "Priority remains opaque — no queue-position inference.",
            "NativeSequenceAvailable=false.",
            "SnapshotCompletionKnown=false.",
            "StableMboBookReconstruction=false — MboOrderObservationState is diagnostic only.",
            "LiveMboCapabilityClaim=false; MboLifecycleCompletenessClaim=false.",
            "Historical MBO and Replay MBO remain Unknown.",
            "Zero callback count is NOT_OBSERVED_IN_TEST_WINDOW — not Unavailable.",
            "TaskCompleted does not prove callback presence or provider completeness.",
            "ExchangeOrderId==0 is counted explicitly and excluded from keyed correlation state.",
            "captureSubscriptionEpoch is the observation/correlation epoch; finalClosedEpoch is post-stop only.",
            "duplicateSubscribeSuppressed counts genuine concurrent duplicates only — not later OnCalculate checks.",
            "MboOrderObservationState capacity is bounded; saturation does not evict existing IDs."
        };
        return list.ToArray();
    }

    public void StopAccepting()
    {
        Interlocked.Exchange(ref _accepting, 0);
        // Close epoch marker for artifact consumers — observations remain on captureSubscriptionEpoch.
        Volatile.Write(ref _finalClosedEpoch, _captureSubscriptionEpoch + 1);
        var state = Volatile.Read(ref _subscriptionState);
        if (state is not (int)MboSubscriptionState.Disposed)
            Volatile.Write(ref _subscriptionState, (int)MboSubscriptionState.StoppedAccepting);
    }

    public bool Drain(TimeSpan timeout)
    {
        _channel.Writer.TryComplete();
        if (_worker is null) return true;
        return _worker.Wait(timeout);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;
        try
        {
            StopAccepting();
            Volatile.Write(ref _subscriptionState, (int)MboSubscriptionState.Disposed);
            _channel.Writer.TryComplete();
            if (_worker is not null)
            {
                if (!_worker.Wait(TimeSpan.FromMilliseconds(_config.DrainTimeoutMilliseconds)))
                {
                    try { _cts.Cancel(); } catch { }
                    _worker.Wait(TimeSpan.FromMilliseconds(250));
                    RecordLimitation("WorkerDrainTimeout");
                    RecordIntegrity("WorkerDrainTimeout");
                }
            }
        }
        catch (Exception ex)
        {
            try { RecordIntegrity("DisposeFailure:" + ex.GetType().Name); } catch { }
        }
        finally
        {
            try { _cts.Dispose(); } catch { }
        }
    }
}
