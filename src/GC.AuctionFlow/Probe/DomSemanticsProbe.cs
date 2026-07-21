using System.Threading.Channels;
using GC.AuctionFlow.Core;

namespace GC.AuctionFlow.Probe;

/// <summary>
/// Bounded non-blocking DOM semantics probe. Pure observations — no ATAS types in core.
/// Does not reconstruct a stable order book; VolumeMeaning/ZeroVolumeMeaning remain Unknown.
/// </summary>
public sealed class DomSemanticsProbe : IDisposable
{
    private readonly DomSemanticsProbeConfig _config;
    private readonly Channel<DepthObservation> _channel;
    private readonly DomSemanticsProbeCounters _counters = new();
    private readonly DepthLevelObservationState _levelState;
    private readonly List<DepthObservation> _samples = new();
    private readonly HashSet<string> _singularFp = new(StringComparer.Ordinal);
    private readonly HashSet<string> _batchFp = new(StringComparer.Ordinal);
    private readonly Dictionary<int, long> _threadCounts = new();
    private readonly Dictionary<DepthCallbackSource, Dictionary<DateTimeKind, long>> _kindBySource = new();
    private readonly object _sampleGate = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task? _worker;
    private readonly List<string> _limitations = new();
    private readonly List<string> _integrity = new();
    private readonly List<DepthLifecycleMarker> _lifecycle = new();

    private int _accepting = 1;
    private int _disposed;
    private int _frozen;
    private int _snapshotPullRequested;
    private ObservedInstrumentSnapshot? _instrument;
    private string? _lastGateReason;
    private int _lastGateReasonCode;
    private long _sequence;
    private long _singularBatchOverlap;
    private long _fingerprintCollisions;
    private long? _lastTicksSingular;
    private long? _lastTicksBatch;
    private long _orderNonDecSingular;
    private long _orderDecSingular;
    private long _orderNonDecBatch;
    private long _orderDecBatch;
    private decimal? _lastBestBid;
    private decimal? _lastBestAsk;
    private DomSemanticsProbeSnapshot? _frozenSnapshot;
    private SnapshotPullStudyResult _snapshotStudy = SnapshotPullStudyResult.NotRequested();

    public DomSemanticsProbe(DomSemanticsProbeConfig? config = null, bool startWorker = true)
    {
        _config = config ?? new DomSemanticsProbeConfig();
        _levelState = new DepthLevelObservationState(_config.MaxObservedLevels);
        _channel = Channel.CreateBounded<DepthObservation>(new BoundedChannelOptions(_config.QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
        _worker = startWorker ? Task.Run(ProcessLoopAsync) : null;
        _lifecycle.Add(DepthLifecycleMarker.Attached);
    }

    public DomSemanticsProbeCounters Counters => _counters;
    public DomSemanticsProbeConfig Config => _config;
    public bool IsAccepting => Volatile.Read(ref _accepting) == 1;
    public string? LastGateReason => Volatile.Read(ref _lastGateReason);
    public TradeStreamGateReason LastGateReasonCode =>
        (TradeStreamGateReason)Volatile.Read(ref _lastGateReasonCode);

    public void SetObservedInstrument(ObservedInstrumentSnapshot snapshot) =>
        Volatile.Write(ref _instrument, snapshot);

    public ObservedInstrumentSnapshot? GetObservedInstrument() => Volatile.Read(ref _instrument);

    public long NextSequence() => Interlocked.Increment(ref _sequence);

    /// <summary>Atomically request deferred snapshot pull (callback must not pull).</summary>
    public bool TryRequestSnapshotPull() =>
        Interlocked.CompareExchange(ref _snapshotPullRequested, 1, 0) == 0;

    public bool IsSnapshotPullPending => Volatile.Read(ref _snapshotPullRequested) == 1;

    /// <summary>Consume pending request for OnCalculate execution (1 → 2).</summary>
    public bool TryConsumeSnapshotPullRequest() =>
        Interlocked.CompareExchange(ref _snapshotPullRequested, 2, 1) == 1;

    public void MarkSnapshotPullNotExecuted()
    {
        lock (_sampleGate)
        {
            if (!_snapshotStudy.Executed)
                _snapshotStudy = SnapshotPullStudyResult.NotExecuted();
        }
    }

    public void RecordLifecycle(DepthLifecycleMarker marker)
    {
        lock (_sampleGate)
            _lifecycle.Add(marker);
    }

    public void RecordLimitation(string text)
    {
        lock (_sampleGate) _limitations.Add(text);
    }

    public void RecordIntegrity(string text)
    {
        lock (_sampleGate) _integrity.Add(text);
    }

    public bool TryEnqueue(
        DepthObservation observation,
        string sideDetail,
        bool enable,
        DataSourceMode mode,
        DataSourceModeProvenance modeProvenance,
        string? expectedInstrumentCode,
        DeclaredFeedProvider feedProvider,
        FeedProviderProvenance feedProvenance,
        bool countCallback = true)
    {
        if (countCallback)
            _counters.IncCallback(observation.CallbackSource);

        if (Volatile.Read(ref _accepting) != 1)
        {
            _counters.IncRejectedAfterDispose();
            return false;
        }

        var gate = DomSemanticsProbeGates.Evaluate(
            enable, mode, modeProvenance, expectedInstrumentCode, _instrument, feedProvider, feedProvenance);
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

        _counters.IncSideDetail(sideDetail);
        if (observation.Volume == 0m)
            _counters.IncZeroVolume();

        _counters.IncNormalized(observation.CallbackSource);
        if (_channel.Writer.TryWrite(observation))
        {
            _counters.IncAcceptedToQueue();
            return true;
        }

        _counters.IncQueueFullDrops();
        return false;
    }

    /// <summary>Already-gated batch sibling (callback already counted).</summary>
    public bool TryEnqueueAlreadyCounted(DepthObservation observation, string sideDetail)
    {
        if (Volatile.Read(ref _accepting) != 1)
        {
            _counters.IncRejectedAfterDispose();
            return false;
        }

        _counters.IncSideDetail(sideDetail);
        if (observation.Volume == 0m)
            _counters.IncZeroVolume();
        _counters.IncNormalized(observation.CallbackSource);
        if (_channel.Writer.TryWrite(observation))
        {
            _counters.IncAcceptedToQueue();
            return true;
        }

        _counters.IncQueueFullDrops();
        return false;
    }

    public DomSemanticsProbeGates.GateDecision EvaluateGates(
        bool enable,
        DataSourceMode mode,
        DataSourceModeProvenance modeProvenance,
        string? expected,
        DeclaredFeedProvider feed,
        FeedProviderProvenance feedProv)
    {
        var gate = DomSemanticsProbeGates.Evaluate(enable, mode, modeProvenance, expected, _instrument, feed, feedProv);
        if (!gate.Accepted)
        {
            Volatile.Write(ref _lastGateReason, gate.RejectReason);
            Volatile.Write(ref _lastGateReasonCode, (int)gate.Reason);
        }

        return gate;
    }

    public void ApplySnapshotPullResult(IReadOnlyList<DepthObservation> items, DateTime executedUtc)
    {
        _counters.IncCallback(DepthCallbackSource.SnapshotPull);
        foreach (var _ in items)
            _counters.IncNormalized(DepthCallbackSource.SnapshotPull);

        lock (_sampleGate)
        {
            var bid = items.Where(i => i.DerivedSide == DepthSide.Bid).ToList();
            var ask = items.Where(i => i.DerivedSide == DepthSide.Ask).ToList();
            _snapshotStudy = new SnapshotPullStudyResult(
                Attempted: true,
                Executed: true,
                Empty: items.Count == 0,
                ItemCount: items.Count,
                BidCount: bid.Count,
                AskCount: ask.Count,
                UnknownSideCount: items.Count(i => i.DerivedSide == DepthSide.Unknown),
                ZeroVolumeCount: items.Count(i => i.Volume == 0m),
                BidMinPrice: bid.Count == 0 ? null : bid.Min(i => i.Price),
                BidMaxPrice: bid.Count == 0 ? null : bid.Max(i => i.Price),
                AskMinPrice: ask.Count == 0 ? null : ask.Min(i => i.Price),
                AskMaxPrice: ask.Count == 0 ? null : ask.Max(i => i.Price),
                ExecutedUtc: executedUtc,
                SnapshotCompletionKnown: false,
                Limitation: "Snapshot completion cannot be known from GetMarketDepthSnapshot() alone.");
        }
        // Do not merge snapshot items into callback samples or level-observation state.
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

    private void ProcessItem(DepthObservation obs)
    {
        if (obs.CallbackSource is DepthCallbackSource.MarketDepthChanged or DepthCallbackSource.MarketDepthsBatch)
            _levelState.Observe(obs);

        if (obs.CallbackSource == DepthCallbackSource.BestBidAskChanged)
        {
            if (obs.DerivedSide == DepthSide.Bid) _lastBestBid = obs.Price;
            if (obs.DerivedSide == DepthSide.Ask) _lastBestAsk = obs.Price;
        }

        lock (_sampleGate)
        {
            if (_samples.Count < _config.MaxSamples)
                _samples.Add(obs);

            if (!_threadCounts.TryGetValue(obs.CallbackManagedThreadId, out var tc))
                tc = 0;
            _threadCounts[obs.CallbackManagedThreadId] = tc + 1;

            if (!_kindBySource.TryGetValue(obs.CallbackSource, out var kindMap))
            {
                kindMap = new Dictionary<DateTimeKind, long>();
                _kindBySource[obs.CallbackSource] = kindMap;
            }

            kindMap.TryGetValue(obs.SourceDateTimeKind, out var kc);
            kindMap[obs.SourceDateTimeKind] = kc + 1;

            if (obs.CallbackSource == DepthCallbackSource.MarketDepthChanged)
            {
                if (!_singularFp.Add(obs.DiagnosticFingerprint))
                    _fingerprintCollisions++;
                else if (_singularFp.Count > _config.OverlapFingerprintSetCapacity)
                    _singularFp.Remove(obs.DiagnosticFingerprint);

                if (_batchFp.Contains(obs.DiagnosticFingerprint))
                    _singularBatchOverlap++;

                TrackOrder(ref _lastTicksSingular, obs.SourceTimeTicks, ref _orderNonDecSingular, ref _orderDecSingular);
            }
            else if (obs.CallbackSource == DepthCallbackSource.MarketDepthsBatch)
            {
                if (!_batchFp.Add(obs.DiagnosticFingerprint))
                    _fingerprintCollisions++;
                else if (_batchFp.Count > _config.OverlapFingerprintSetCapacity)
                    _batchFp.Remove(obs.DiagnosticFingerprint);

                if (_singularFp.Contains(obs.DiagnosticFingerprint))
                    _singularBatchOverlap++;

                TrackOrder(ref _lastTicksBatch, obs.SourceTimeTicks, ref _orderNonDecBatch, ref _orderDecBatch);
            }
        }
    }

    private static void TrackOrder(ref long? last, long ticks, ref long nonDec, ref long dec)
    {
        if (last is null) { last = ticks; return; }
        if (ticks >= last.Value) nonDec++;
        else dec++;
        last = ticks;
    }

    public DomSemanticsProbeSnapshot FreezeSnapshot(
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

        if (Volatile.Read(ref _snapshotPullRequested) == 1 && !_snapshotStudy.Executed)
            MarkSnapshotPullNotExecuted();

        DepthObservation[] samples;
        string[] limitations;
        string[] integrity;
        DepthLifecycleMarker[] lifecycle;
        SnapshotPullStudyResult snapshotStudy;
        Dictionary<int, long> threads;
        Dictionary<string, Dictionary<string, long>> kinds;
        long overlap;
        long fpCollisions;
        lock (_sampleGate)
        {
            samples = _samples.ToArray();
            limitations = BuildLimitations();
            integrity = _integrity.ToArray();
            lifecycle = _lifecycle.ToArray();
            snapshotStudy = _snapshotStudy;
            threads = new Dictionary<int, long>(_threadCounts);
            kinds = _kindBySource.ToDictionary(
                kv => kv.Key.ToString(),
                kv => kv.Value.ToDictionary(x => x.Key.ToString(), x => x.Value));
            overlap = _singularBatchOverlap;
            fpCollisions = _fingerprintCollisions;
        }

        var gate = DomSemanticsProbeGates.Evaluate(
            enable, mode, modeProvenance, expectedInstrumentCode, _instrument, feedProvider, feedProvenance);
        var captureAuthorized = gate.Accepted;
        var counters = _counters.Snapshot();

        var callbackObs = new DepthCallbackObservationReport(
            MarketDepthChanged: Status(counters.CallbackInvocationsMarketDepthChanged),
            MarketDepthsBatch: Status(counters.CallbackInvocationsMarketDepthsBatch),
            BestBidAskChanged: Status(counters.CallbackInvocationsBestBidAskChanged),
            SnapshotPull: Status(counters.CallbackInvocationsSnapshotPull));

        var snap = new DomSemanticsProbeSnapshot(
            schemaVersion: DomSemanticsProbeVersions.DomSemanticsProbeSchemaVersion,
            probeVersion: DomSemanticsProbeVersions.ProbeVersion,
            sessionId: sessionId,
            createdUtc: DateTime.UtcNow,
            continuityDisclaimer: DomSemanticsProbeVersions.ContinuityDisclaimer,
            declaredDataSourceMode: mode,
            dataSourceModeProvenance: modeProvenance,
            declaredFeedProvider: feedProvider,
            feedProviderProvenance: feedProvenance,
            expectedInstrumentCode: expectedInstrumentCode,
            observedInstrument: _instrument,
            captureAuthorized: captureAuthorized,
            gateReason: captureAuthorized ? null : TradeStreamGateReasonNames.ToWire(gate.Reason),
            lastGateReason: gate.RejectReason ?? _lastGateReason,
            apiCallbackSourcesUsed: new[]
            {
                "MarketDepthChanged", "MarketDepthsChanged", "OnBestBidAskChanged", "MarketDepthInfo.GetMarketDepthSnapshot"
            },
            counters: counters,
            samples: samples,
            levelObservationState: _levelState.Snapshot(),
            snapshotPullStudy: snapshotStudy,
            lastBestBidPrice: _lastBestBid,
            lastBestAskPrice: _lastBestAsk,
            singularBatchFingerprintOverlapHits: overlap,
            fingerprintCollisionsObserved: fpCollisions,
            sourceTimeNonDecreasingSingular: _orderNonDecSingular,
            sourceTimeDecreasingSingular: _orderDecSingular,
            sourceTimeNonDecreasingBatch: _orderNonDecBatch,
            sourceTimeDecreasingBatch: _orderDecBatch,
            managedThreadIdCounts: threads,
            dateTimeKindBySource: kinds,
            lifecycleMarkers: lifecycle,
            resetEvidence: DepthResetEvidence.NotObserved,
            callbackObservations: callbackObs,
            knownLimitations: limitations,
            integrityEvents: integrity,
            queueCapacity: _config.QueueCapacity,
            volumeMeaning: DomSemanticsProbeVersions.VolumeMeaning,
            zeroVolumeMeaning: DomSemanticsProbeVersions.ZeroVolumeMeaning,
            updateAction: nameof(DepthUpdateAction.Unknown),
            stableBookReconstruction: false,
            historicalDomCapabilityClaim: false,
            replayDomCapabilityClaim: false,
            liveDomCapabilityClaim: false);

        _frozenSnapshot = snap;
        return snap;
    }

    private static string Status(long count) =>
        count > 0
            ? CallbackObservationStatusNames.Observed
            : CallbackObservationStatusNames.NotObservedInTestWindow;

    private string[] BuildLimitations()
    {
        var list = new List<string>(_limitations)
        {
            DomSemanticsProbeVersions.ContinuityDisclaimer,
            "No explicit depth action on MarketDataArg — DepthUpdateAction=Unknown.",
            "No level index; no native sequence on MarketDataArg.",
            "No Indicator-path reset marker — DepthResetEvidence=NotObserved.",
            "Snapshot completeness unknown.",
            "VolumeMeaning=Unknown; ZeroVolumeMeaning=Unknown.",
            "StableBookReconstruction=false — DepthLevelObservationState is diagnostic last-observation only.",
            "LiveDomCapabilityClaim=false; HistoricalDomCapabilityClaim=false; ReplayDomCapabilityClaim=false.",
            "Callback threading and clock Kind remain measurement subjects.",
            "Zero callback count is NOT_OBSERVED_IN_TEST_WINDOW — not Unavailable."
        };
        return list.ToArray();
    }

    public void StopAccepting() => Interlocked.Exchange(ref _accepting, 0);

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

public sealed record DepthCallbackObservationReport(
    string MarketDepthChanged,
    string MarketDepthsBatch,
    string BestBidAskChanged,
    string SnapshotPull);

public sealed record SnapshotPullStudyResult(
    bool Attempted,
    bool Executed,
    bool Empty,
    int ItemCount,
    int BidCount,
    int AskCount,
    int UnknownSideCount,
    int ZeroVolumeCount,
    decimal? BidMinPrice,
    decimal? BidMaxPrice,
    decimal? AskMinPrice,
    decimal? AskMaxPrice,
    DateTime? ExecutedUtc,
    bool SnapshotCompletionKnown,
    string? Limitation)
{
    public static SnapshotPullStudyResult NotRequested() =>
        new(false, false, false, 0, 0, 0, 0, 0, null, null, null, null, null, false, null);

    public static SnapshotPullStudyResult NotExecuted() =>
        new(true, false, false, 0, 0, 0, 0, 0, null, null, null, null, null, false, "SnapshotPullNotExecuted");
}
