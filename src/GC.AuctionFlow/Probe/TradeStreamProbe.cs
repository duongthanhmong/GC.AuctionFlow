using System.Threading.Channels;
using GC.AuctionFlow.Core;

namespace GC.AuctionFlow.Probe;

/// <summary>
/// Bounded non-blocking trade-stream probe core. Accepts pure immutable observations (no ATAS types).
/// </summary>
public sealed class TradeStreamProbe : IDisposable
{
    private readonly TradeStreamProbeConfig _config;
    private readonly Channel<object> _channel;
    private readonly TradeStreamProbeCounters _counters = new();
    private readonly TradeStreamOverlapStats _overlap;
    private readonly List<NewTradeObservation> _newSamples = new();
    private readonly List<CumulativeTradeObservation> _cumSamples = new();
    private readonly object _sampleGate = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task? _worker;
    private readonly List<string> _limitations = new();
    private readonly List<string> _integrityEvents = new();

    private int _accepting = 1;
    private int _disposed;
    private int _frozen;
    private TradeStreamProbeSnapshot? _frozenSnapshot;
    private ObservedInstrumentSnapshot? _instrument;
    private string? _lastGateReason;
    private int _lastGateReasonCode;
    private long _sequence;
    private string? _lastCumulativeValueFp;
    private long? _lastCumulativeInstanceId;

    public TradeStreamProbe(TradeStreamProbeConfig? config = null, bool startWorker = true)
    {
        _config = config ?? new TradeStreamProbeConfig();
        _overlap = new TradeStreamOverlapStats(_config.OverlapFingerprintSetCapacity);
        _channel = Channel.CreateBounded<object>(new BoundedChannelOptions(_config.QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
        _worker = startWorker ? Task.Run(ProcessLoopAsync) : null;
    }

    public TradeStreamProbeCounters Counters => _counters;
    public TradeStreamProbeConfig Config => _config;
    public bool IsAccepting => Volatile.Read(ref _accepting) == 1;
    public string? LastGateReason => Volatile.Read(ref _lastGateReason);
    public TradeStreamGateReason LastGateReasonCode =>
        (TradeStreamGateReason)Volatile.Read(ref _lastGateReasonCode);

    public void SetObservedInstrument(ObservedInstrumentSnapshot snapshot) =>
        Volatile.Write(ref _instrument, snapshot);

    public ObservedInstrumentSnapshot? GetObservedInstrument() => Volatile.Read(ref _instrument);

    /// <summary>
    /// Callback-path ingest: gate → TryWrite → return. Never blocks. Never I/O.
    /// </summary>
    public bool TryEnqueueNewTrade(
        NewTradeObservation observation,
        bool enableTradeStreamProbe,
        DataSourceMode mode,
        DataSourceModeProvenance provenance,
        string? expectedInstrumentCode)
    {
        _counters.IncCallback(observation.CallbackSource);

        if (Volatile.Read(ref _accepting) != 1)
        {
            _counters.IncRejectedAfterDispose();
            return false;
        }

        var gate = TradeStreamProbeGates.Evaluate(
            enableTradeStreamProbe, mode, provenance, expectedInstrumentCode, _instrument);
        if (!gate.Accepted)
        {
            Volatile.Write(ref _lastGateReason, gate.RejectReason);
            Volatile.Write(ref _lastGateReasonCode, (int)gate.Reason);
            if (gate.InstrumentGate)
                _counters.IncRejectedByInstrumentGate();
            else
                _counters.IncRejectedByModeGate();
            return false;
        }

        _counters.IncNormalized(observation.CallbackSource);
        return TryWrite(observation);
    }

    public bool TryEnqueueCumulative(
        CumulativeTradeObservation observation,
        bool enableTradeStreamProbe,
        DataSourceMode mode,
        DataSourceModeProvenance provenance,
        string? expectedInstrumentCode)
    {
        _counters.IncCallback(observation.CallbackSource);

        if (Volatile.Read(ref _accepting) != 1)
        {
            _counters.IncRejectedAfterDispose();
            return false;
        }

        var gate = TradeStreamProbeGates.Evaluate(
            enableTradeStreamProbe, mode, provenance, expectedInstrumentCode, _instrument);
        if (!gate.Accepted)
        {
            Volatile.Write(ref _lastGateReason, gate.RejectReason);
            Volatile.Write(ref _lastGateReasonCode, (int)gate.Reason);
            if (gate.InstrumentGate)
                _counters.IncRejectedByInstrumentGate();
            else
                _counters.IncRejectedByModeGate();
            return false;
        }

        _counters.IncNormalized(observation.CallbackSource);
        return TryWrite(observation);
    }

    /// <summary>
    /// Enqueue a new-trade observation that is already gated; does not increment CallbackInvocations
    /// (used for additional prints inside one OnNewTrades batch).
    /// </summary>
    public bool TryEnqueueNewTradeAlreadyCounted(NewTradeObservation observation)
    {
        if (Volatile.Read(ref _accepting) != 1)
        {
            _counters.IncRejectedAfterDispose();
            return false;
        }

        _counters.IncNormalized(observation.CallbackSource);
        return TryWrite(observation);
    }

    /// <summary>Test helper: enqueue already-gated immutable observation (skips operator gates).</summary>
    public bool TryEnqueueObservationForTest(object observation, bool countCallback = false)
    {
        if (Volatile.Read(ref _accepting) != 1)
        {
            _counters.IncRejectedAfterDispose();
            return false;
        }

        if (observation is NewTradeObservation n)
        {
            if (countCallback)
                _counters.IncCallback(n.CallbackSource);
            _counters.IncNormalized(n.CallbackSource);
        }
        else if (observation is CumulativeTradeObservation c)
        {
            if (countCallback)
                _counters.IncCallback(c.CallbackSource);
            _counters.IncNormalized(c.CallbackSource);
        }
        else
        {
            _counters.IncNormalizationFailures();
            return false;
        }

        return TryWrite(observation);
    }

    /// <summary>Evaluate gates without enqueue (for batch siblings).</summary>
    public TradeStreamProbeGates.GateDecision EvaluateGates(
        bool enableTradeStreamProbe,
        DataSourceMode mode,
        DataSourceModeProvenance provenance,
        string? expectedInstrumentCode)
    {
        var gate = TradeStreamProbeGates.Evaluate(
            enableTradeStreamProbe, mode, provenance, expectedInstrumentCode, _instrument);
        if (!gate.Accepted)
        {
            Volatile.Write(ref _lastGateReason, gate.RejectReason);
            Volatile.Write(ref _lastGateReasonCode, (int)gate.Reason);
        }

        return gate;
    }

    public long NextSequence() => Interlocked.Increment(ref _sequence);

    private bool TryWrite(object observation)
    {
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
                    lock (_sampleGate)
                        _integrityEvents.Add($"WorkerProcessFailure:{ex.GetType().Name}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // expected on dispose cancel before complete
        }
        catch (Exception ex)
        {
            lock (_sampleGate)
                _integrityEvents.Add($"WorkerLoopFailure:{ex.GetType().Name}");
        }
    }

    private void ProcessItem(object item)
    {
        switch (item)
        {
            case NewTradeObservation n:
                _overlap.ObserveNewTrade(n);
                lock (_sampleGate)
                {
                    if (_newSamples.Count < _config.MaxNewTradeSamples)
                        _newSamples.Add(n);
                }
                break;

            case CumulativeTradeObservation c:
                if (c.CallbackSource == TradeCallbackSource.OnCumulativeTrade)
                    _counters.IncCumulativeNewObservationCount();
                else if (c.CallbackSource == TradeCallbackSource.OnUpdateCumulativeTrade)
                    _counters.IncCumulativeUpdateCount();

                if (_lastCumulativeInstanceId is not null && c.ProcessLocalInstanceId is not null)
                {
                    var same = _lastCumulativeInstanceId == c.ProcessLocalInstanceId;
                    var fpMatch = string.Equals(_lastCumulativeValueFp, c.ValueFingerprint, StringComparison.Ordinal);
                    _overlap.ObserveCumulativeInstanceLinkage(same, fpMatch);
                }

                _lastCumulativeInstanceId = c.ProcessLocalInstanceId;
                _lastCumulativeValueFp = c.ValueFingerprint;
                _overlap.ObserveCumulative(c);

                lock (_sampleGate)
                {
                    if (_cumSamples.Count < _config.MaxCumulativeSamples)
                        _cumSamples.Add(c);
                }
                break;

            default:
                _counters.IncNormalizationFailures();
                break;
        }
    }

    public TradeStreamProbeSnapshot FreezeSnapshot(
        DataSourceMode mode,
        DataSourceModeProvenance provenance,
        string? expectedInstrumentCode,
        Guid sessionId,
        bool enableTradeStreamProbe = true)
    {
        if (Interlocked.Exchange(ref _frozen, 1) == 1 && _frozenSnapshot is not null)
            return _frozenSnapshot;

        NewTradeObservation[] news;
        CumulativeTradeObservation[] cums;
        string[] limitations;
        string[] integrity;
        lock (_sampleGate)
        {
            news = _newSamples.ToArray();
            cums = _cumSamples.ToArray();
            limitations = BuildLimitations();
            integrity = _integrityEvents.ToArray();
        }

        var artifactId = new TradeStreamProbeArtifactIdentity(
            schemaVersion: TradeStreamProbeVersions.TradeStreamProbeSchemaVersion,
            probeVersion: TradeStreamProbeVersions.ProbeVersion,
            sessionId: sessionId,
            createdUtc: DateTime.UtcNow,
            continuityDisclaimer: TradeStreamProbeVersions.ContinuityDisclaimer);

        var finalGate = TradeStreamProbeGates.Evaluate(
            enableTradeStreamProbe,
            mode,
            provenance,
            expectedInstrumentCode,
            _instrument);

        // CaptureAuthorized false for identity-discovery / mismatch — never LIVE capability PASS.
        var captureAuthorized = finalGate.Accepted;
        var gateReason = captureAuthorized ? TradeStreamGateReason.None : finalGate.Reason;
        var gateDetail = captureAuthorized
            ? null
            : (finalGate.RejectReason ?? _lastGateReason);

        var counterSnap = _counters.Snapshot();
        var callbackObservations = new CallbackObservationReport(
            OnNewTrade: CallbackObservationStatusNames.ToWire(
                CallbackObservationStatusNames.FromCount(counterSnap.CallbackInvocationsOnNewTrade)),
            OnNewTradesBatch: CallbackObservationStatusNames.ToWire(
                CallbackObservationStatusNames.FromCount(counterSnap.CallbackInvocationsOnNewTradesBatch)),
            OnCumulativeTrade: CallbackObservationStatusNames.ToWire(
                CallbackObservationStatusNames.FromCount(counterSnap.CallbackInvocationsOnCumulativeTrade)),
            OnUpdateCumulativeTrade: CallbackObservationStatusNames.ToWire(
                CallbackObservationStatusNames.FromCount(counterSnap.CallbackInvocationsOnUpdateCumulativeTrade)));

        var snap = new TradeStreamProbeSnapshot(
            artifactIdentity: artifactId,
            declaredDataSourceMode: mode,
            dataSourceModeProvenance: provenance,
            expectedInstrumentCode: expectedInstrumentCode,
            observedInstrument: _instrument,
            lastGateReason: gateDetail,
            gateReason: TradeStreamGateReasonNames.ToWire(gateReason),
            captureAuthorized: captureAuthorized,
            liveTradeCapabilityClaim: false,
            callbackObservations: callbackObservations,
            counters: counterSnap,
            overlap: _overlap.Snapshot(),
            newTradeSamples: news,
            cumulativeSamples: cums,
            knownLimitations: limitations,
            integrityEvents: integrity,
            queueCapacity: _config.QueueCapacity,
            maxNewTradeSamples: _config.MaxNewTradeSamples,
            maxCumulativeSamples: _config.MaxCumulativeSamples,
            callbackThreading: "Unknown",
            clockSemantics: "Unknown",
            authoritativeStream: "None",
            fingerprintsAreDiagnosticsOnly: true);

        _frozenSnapshot = snap;
        return snap;
    }

    private string[] BuildLimitations()
    {
        var list = new List<string>(_limitations)
        {
            TradeStreamProbeVersions.ContinuityDisclaimer,
            "Callback threading remains Unknown until operator measurement.",
            "Clock Kind / exchange-clock semantics remain Unknown until operator measurement.",
            "No trade stream is declared authoritative.",
            "Fingerprints are diagnostic evidence only — not trade IDs, not native sequence, not used for deletion.",
            "Process-local cumulative instance IDs are valid only within one indicator instance and are not exchange IDs.",
            "No GC runtime capability is claimed before operator verification.",
            "Bounded samples only; queue-full drops discard observations.",
            "No total executed volume field; volumes are not merged across streams.",
            "Zero callback count in a run is NOT_OBSERVED_IN_TEST_WINDOW (do not auto-classify as capability Unavailable).",
            "Rejected capture (CaptureAuthorized=false) is never a LIVE trade capability PASS."
        };
        return list.ToArray();
    }

    public void RecordLimitation(string text)
    {
        lock (_sampleGate)
            _limitations.Add(text);
    }

    public void RecordIntegrityEvent(string text)
    {
        lock (_sampleGate)
            _integrityEvents.Add(text);
    }

    /// <summary>
    /// Disposal order: stop accept → complete queue → drain (bounded) → freeze is caller's job → dispose resources.
    /// </summary>
    public void StopAccepting()
    {
        Interlocked.Exchange(ref _accepting, 0);
    }

    public bool Drain(TimeSpan timeout)
    {
        _channel.Writer.TryComplete();
        if (_worker is null)
            return true;
        return _worker.Wait(timeout);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        try
        {
            StopAccepting();
            _channel.Writer.TryComplete();
            if (_worker is not null)
            {
                if (!_worker.Wait(TimeSpan.FromMilliseconds(_config.DrainTimeoutMilliseconds)))
                {
                    try { _cts.Cancel(); } catch { /* ignore */ }
                    _worker.Wait(TimeSpan.FromMilliseconds(250));
                    RecordLimitation("WorkerDrainTimeout");
                    RecordIntegrityEvent("WorkerDrainTimeout");
                }
            }
        }
        catch (Exception ex)
        {
            try
            {
                RecordIntegrityEvent($"DisposeFailure:{ex.GetType().Name}");
            }
            catch
            {
                // ignore
            }
        }
        finally
        {
            try { _cts.Dispose(); } catch { /* ignore */ }
        }
    }
}
