using System.Diagnostics;
using System.Threading.Channels;
using GC.AuctionFlow.Recorder.Payloads;

namespace GC.AuctionFlow.Recorder;

/// <summary>
/// P0-07B recorder session: bounded queue + dedicated worker. No ATAS callback adapters.
/// </summary>
public sealed class RawEventRecorderSession : IDisposable
{
    private readonly RecorderConfig _config;
    private readonly RecorderCounters _counters;
    private readonly IDiskSpaceProbe _disk;
    private readonly string _sessionDir;
    private readonly string _segmentsDir;
    private readonly Channel<RawEventDraft> _channel;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _worker;
    private readonly object _gate = new();
    private readonly List<CompletedSegmentInfo> _completed = new();
    private readonly List<int> _contractEpochs = new() { 1 };

    private readonly Guid _sessionId;
    private readonly Guid _processId;
    private readonly DateTime _startUtc;
    private readonly string _declaredMode;
    private readonly string _modeProvenance;
    private readonly string _declaredProvider;
    private readonly string _providerProvenance;
    private readonly string? _userProfileOverride;

    private SegmentWriter? _writer;
    private ObservedInstrumentIdentity? _currentIdentity;
    private int _contractEpoch = 1;
    private int _segmentOrdinal;
    private long _writerSequence;
    private long _sessionBytes;
    private bool _accepting = true;
    private bool _fatal;
    private bool _disposed;
    private string? _stopReason;
    private DateTime? _stopUtc;

    public RawEventRecorderSession(
        Guid sessionId,
        Guid processInstanceId,
        string declaredDataSourceMode,
        string modeProvenance,
        string declaredProvider,
        string providerProvenance,
        RecorderConfig? config = null,
        IDiskSpaceProbe? diskSpaceProbe = null,
        string? userProfileOverride = null,
        RecorderCounters? counters = null)
    {
        _sessionId = sessionId;
        _processId = processInstanceId;
        _declaredMode = declaredDataSourceMode;
        _modeProvenance = modeProvenance;
        _declaredProvider = declaredProvider;
        _providerProvenance = providerProvenance;
        _config = config ?? new RecorderConfig();
        _disk = diskSpaceProbe ?? new DriveInfoDiskSpaceProbe();
        _counters = counters ?? new RecorderCounters();
        _userProfileOverride = userProfileOverride;
        _startUtc = DateTime.UtcNow;

        RecorderStoragePaths.EnsureSessionLayout(sessionId, userProfileOverride);
        _sessionDir = RecorderStoragePaths.GetSessionDirectory(sessionId, userProfileOverride);
        _segmentsDir = RecorderStoragePaths.GetSegmentsDirectory(sessionId, userProfileOverride);

        // Wait mode + TryWrite: full channel returns false without blocking (callback-safe).
        _channel = Channel.CreateBounded<RawEventDraft>(new BoundedChannelOptions(_config.QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

        _writer = new SegmentWriter(_segmentsDir, _config, _counters);
        _worker = Task.Run(WorkerLoopAsync);
    }

    public Guid SessionId => _sessionId;
    public RecorderCounters Counters => _counters;
    public bool IsAccepting => _accepting && !_disposed && !_fatal;
    public int ContractEpoch => _contractEpoch;
    public IReadOnlyList<CompletedSegmentInfo> CompletedSegments
    {
        get { lock (_gate) return _completed.ToList(); }
    }

    /// <summary>
    /// Non-blocking ingest. Does not perform file I/O.
    /// Market drafts update AcceptedToQueue/QueueFullDrops.
    /// Invocation-result drafts update InvocationResult* counters only (same channel).
    /// </summary>
    public bool TryWrite(RawEventDraft draft)
    {
        if (draft is null) return false;

        var isInvocationResult = draft.PayloadDiscriminator == RawEventPayloadKind.CallbackInvocationResult;
        if (isInvocationResult)
            Interlocked.Increment(ref _counters.InvocationResultEmissionAttempts);

        if (_disposed || !_accepting || _fatal)
        {
            if (isInvocationResult)
                Interlocked.Increment(ref _counters.InvocationResultFaults);
            else
                Interlocked.Increment(ref _counters.RejectedCallbackInvocationsAfterDispose);
            return false;
        }

        try
        {
            if (_channel.Writer.TryWrite(draft))
            {
                if (isInvocationResult)
                    Interlocked.Increment(ref _counters.InvocationResultAcceptedToQueue);
                else
                    Interlocked.Increment(ref _counters.AcceptedToQueue);
                return true;
            }

            if (isInvocationResult)
                Interlocked.Increment(ref _counters.InvocationResultQueueFullDrops);
            else
                Interlocked.Increment(ref _counters.QueueFullDrops);
            return false;
        }
        catch
        {
            if (isInvocationResult)
                Interlocked.Increment(ref _counters.InvocationResultFaults);
            return false;
        }
    }

    /// <summary>Recorder sink outcome helper for fan-out (no market NormalizedObservations).</summary>
    public FanOut.RecorderSinkOutcome TryAcceptDraft(RawEventDraft draft)
    {
        if (!MboOperationalLock.MboRecordingEnabled && draft.StreamKind == RecorderStreamKind.Mbo)
            return FanOut.RecorderSinkOutcome.StreamDisabled;
        if (_disposed)
            return FanOut.RecorderSinkOutcome.StoppedAccepting;
        if (!_accepting || _fatal)
            return FanOut.RecorderSinkOutcome.StoppedAccepting;
        if (_writer is null)
            return FanOut.RecorderSinkOutcome.SessionNotStarted;
        return TryWrite(draft) ? FanOut.RecorderSinkOutcome.Accepted : FanOut.RecorderSinkOutcome.QueueFull;
    }

    /// <summary>Test helper: mark one normalized observation accepted path.</summary>
    public void NoteNormalizedObservation() => Interlocked.Increment(ref _counters.NormalizedObservations);

    public void NotePayloadItemEnumerated() => Interlocked.Increment(ref _counters.PayloadItemsEnumerated);

    public void StopAccepting(string reason)
    {
        _accepting = false;
        _stopReason = reason;
    }

    public ReconciliationResult DisposeAndReconcile()
    {
        Dispose();
        return RecorderReconciliation.Evaluate(_counters.Snapshot(), cleanShutdown: _stopReason is null && !_fatal);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _accepting = false;
        _channel.Writer.TryComplete();

        try
        {
            if (!_worker.Wait(_config.ShutdownDrainTimeoutMilliseconds))
            {
                while (_channel.Reader.TryRead(out var leftover))
                {
                    if (leftover.PayloadDiscriminator == RawEventPayloadKind.CallbackInvocationResult)
                        ProcessInvocationResultDraft(leftover);
                    else
                        Interlocked.Increment(ref _counters.UndrainedAtShutdown);
                }
            }
            else
            {
                while (_channel.Reader.TryRead(out var leftover))
                {
                    if (leftover.PayloadDiscriminator == RawEventPayloadKind.CallbackInvocationResult)
                        ProcessInvocationResultDraft(leftover);
                    else
                        Interlocked.Increment(ref _counters.UndrainedAtShutdown);
                }
            }
        }
        catch
        {
            Interlocked.Increment(ref _counters.WorkerFaults);
        }

        try
        {
            FinalizeSession_NoThrow();
        }
        catch
        {
            // never escape
        }

        try { _cts.Cancel(); } catch { /* ignore */ }
        try { _cts.Dispose(); } catch { /* ignore */ }
        try { _writer?.Dispose(); } catch { /* ignore */ }
        _writer = null;
    }

    private async Task WorkerLoopAsync()
    {
        var lastFlush = Stopwatch.GetTimestamp();
        try
        {
            while (await _channel.Reader.WaitToReadAsync(_cts.Token).ConfigureAwait(false))
            {
                while (_channel.Reader.TryRead(out var draft))
                {
                    ProcessDraft(draft);
                    var now = Stopwatch.GetTimestamp();
                    if (TimeSpan.FromSeconds((now - lastFlush) / (double)Stopwatch.Frequency) >= _config.FlushInterval)
                    {
                        // flush is per-write Flush(true) on complete; open stream flush optional
                        lastFlush = now;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // shutdown
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _counters.WorkerFaults);
            _fatal = true;
            _accepting = false;
            _stopReason = "WorkerFault:" + ex.GetType().Name;
            // discard remaining
            while (_channel.Reader.TryRead(out _))
            {
                Interlocked.Increment(ref _counters.WriterDequeuedTotal);
                Interlocked.Increment(ref _counters.WriterDiscardedAfterFatalFault);
            }
        }
    }

    private void ProcessDraft(RawEventDraft draft)
    {
        if (draft.PayloadDiscriminator == RawEventPayloadKind.CallbackInvocationResult)
        {
            ProcessInvocationResultDraft(draft);
            return;
        }

        Interlocked.Increment(ref _counters.WriterDequeued);
        Interlocked.Increment(ref _counters.WriterDequeuedTotal);
        if (_fatal || _writer is null)
        {
            Interlocked.Increment(ref _counters.WriterDiscardedAfterFatalFault);
            return;
        }

        if (!EnsureDiskAndSessionBudget())
        {
            Interlocked.Increment(ref _counters.WriterDiscardedAfterFatalFault);
            return;
        }

        try
        {
            HandleIdentity(draft);
            EnsureSegmentOpen(draft);

            var seq = Interlocked.Increment(ref _writerSequence);
            var dequeuedUtc = DateTime.UtcNow;
            var envelope = RawEventEnvelope.FromDraft(
                CloneDraftWithEpoch(draft, _contractEpoch),
                _writer!.CurrentSegmentId,
                _writer.CurrentSegmentOrdinal,
                seq,
                dequeuedUtc);

            byte[] payloadUtf8;
            try
            {
                payloadUtf8 = RecorderJson.SerializeEnvelope(envelope);
            }
            catch
            {
                Interlocked.Increment(ref _counters.SerializationFailures);
                return;
            }

            if (payloadUtf8.Length > _config.MaxFramePayloadBytes)
            {
                Interlocked.Increment(ref _counters.SerializationFailures);
                return;
            }

            var nextFrameBytes = ContainerFormat.FrameTotalSize(payloadUtf8.Length);
            if (_writer.WouldExceedLimits(nextFrameBytes, _config.FooterReserveBytes, Stopwatch.GetTimestamp()))
            {
                CompleteCurrentSegment();
                EnsureSegmentOpen(draft);
                envelope = RawEventEnvelope.FromDraft(
                    CloneDraftWithEpoch(draft, _contractEpoch),
                    _writer.CurrentSegmentId,
                    _writer.CurrentSegmentOrdinal,
                    seq,
                    dequeuedUtc);
            }

            if (!_writer.TryWriteRawEvent(envelope, out var serializationFailed))
            {
                if (!serializationFailed)
                    Interlocked.Increment(ref _counters.WriterDiscardedAfterFatalFault);
                Interlocked.Increment(ref _counters.SegmentWriteFailures);
                Interlocked.Increment(ref _counters.WorkerFaults);
                _fatal = true;
                _accepting = false;
                _stopReason = "WriteFault";
                try { _writer.AbandonIncomplete(); } catch { /* preserve tmp */ }
            }
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _counters.SegmentWriteFailures);
            Interlocked.Increment(ref _counters.WorkerFaults);
            Interlocked.Increment(ref _counters.WriterDiscardedAfterFatalFault);
            _fatal = true;
            _accepting = false;
            _stopReason = "WriteFault:" + ex.GetType().Name;
            try { _writer?.AbandonIncomplete(); } catch { /* preserve tmp */ }
        }
    }

    /// <summary>
    /// Same writer ordering as market drafts. Does not touch market Normalized/Accepted/RecordsWritten.
    /// </summary>
    private void ProcessInvocationResultDraft(RawEventDraft draft)
    {
        Interlocked.Increment(ref _counters.WriterDequeuedTotal);

        if (_fatal || _writer is null)
        {
            Interlocked.Increment(ref _counters.WriterDiscardedAfterFatalFault);
            return;
        }

        if (!EnsureDiskAndSessionBudget())
        {
            Interlocked.Increment(ref _counters.WriterDiscardedAfterFatalFault);
            return;
        }

        try
        {
            HandleIdentity(draft);
            EnsureSegmentOpen(draft);

            var seq = Interlocked.Increment(ref _writerSequence);
            var dequeuedUtc = DateTime.UtcNow;
            var envelope = RawEventEnvelope.FromDraft(
                CloneDraftWithEpoch(draft, _contractEpoch),
                _writer!.CurrentSegmentId,
                _writer.CurrentSegmentOrdinal,
                seq,
                dequeuedUtc);

            byte[] payloadUtf8;
            try
            {
                payloadUtf8 = RecorderJson.SerializeEnvelope(envelope);
            }
            catch
            {
                Interlocked.Increment(ref _counters.SerializationFailures);
                return;
            }

            if (payloadUtf8.Length > _config.MaxFramePayloadBytes)
            {
                Interlocked.Increment(ref _counters.SerializationFailures);
                return;
            }

            var nextFrameBytes = ContainerFormat.FrameTotalSize(payloadUtf8.Length);
            if (_writer.WouldExceedLimits(nextFrameBytes, _config.FooterReserveBytes, Stopwatch.GetTimestamp()))
            {
                CompleteCurrentSegment();
                EnsureSegmentOpen(draft);
                envelope = RawEventEnvelope.FromDraft(
                    CloneDraftWithEpoch(draft, _contractEpoch),
                    _writer.CurrentSegmentId,
                    _writer.CurrentSegmentOrdinal,
                    seq,
                    dequeuedUtc);
            }

            if (!_writer.TryWriteRawEvent(envelope, out var serializationFailed))
            {
                if (serializationFailed)
                    Interlocked.Increment(ref _counters.SerializationFailures);
                else
                    Interlocked.Increment(ref _counters.WriterDiscardedAfterFatalFault);
            }
        }
        catch
        {
            Interlocked.Increment(ref _counters.WriterDiscardedAfterFatalFault);
        }
    }

    private static RawEventDraft CloneDraftWithEpoch(RawEventDraft d, int epoch) =>
        new(
            d.RecorderSchemaVersion,
            d.SessionId,
            d.RecorderProcessInstanceId,
            d.StreamKind,
            d.CallbackSource,
            d.StreamLocalCaptureSequence,
            d.CallbackInvocationSequence,
            d.CallbackItemOrdinal,
            d.SubscriptionOrCaptureEpoch,
            epoch,
            d.Instrument,
            d.DeclaredDataSourceMode,
            d.ModeProvenance,
            d.DeclaredProvider,
            d.ProviderProvenance,
            d.SourceTimeTicks,
            d.SourceDateTimeKind,
            d.CallbackReceiveUtc,
            d.CallbackReceiveStopwatchTimestamp,
            d.CallbackManagedThreadId,
            d.PayloadDiscriminator,
            d.Payload,
            d.IntegrityFlags,
            d.NativeSequenceAvailable);

    private void HandleIdentity(RawEventDraft draft)
    {
        if (_currentIdentity is null)
        {
            _currentIdentity = draft.Instrument;
            return;
        }

        if (_currentIdentity.Equals(draft.Instrument))
            return;

        // Close old segment, advance epoch, open new segment, write lifecycle as first RawEvent.
        if (_writer is { IsOpen: true })
            CompleteCurrentSegment();

        var previousIdentity = _currentIdentity;
        var previous = _contractEpoch;
        _contractEpoch++;
        _contractEpochs.Add(_contractEpoch);
        _currentIdentity = draft.Instrument;

        var receiveUtc = DateTime.UtcNow;
        var sw = Stopwatch.GetTimestamp();
        var lifecycle = new RawEventDraft(
            RawEventRecorderVersions.RawEventRecorderSchemaVersion,
            _sessionId,
            _processId,
            RecorderStreamKind.Lifecycle,
            RecorderCallbackSource.RecorderWorker,
            streamLocalCaptureSequence: previous,
            callbackInvocationSequence: 0,
            callbackItemOrdinal: 0,
            subscriptionOrCaptureEpoch: null,
            contractEpoch: _contractEpoch,
            instrument: draft.Instrument,
            declaredDataSourceMode: _declaredMode,
            modeProvenance: _modeProvenance,
            declaredProvider: _declaredProvider,
            providerProvenance: _providerProvenance,
            sourceTimeTicks: receiveUtc.Ticks,
            sourceDateTimeKind: DateTimeKind.Utc,
            callbackReceiveUtc: receiveUtc,
            callbackReceiveStopwatchTimestamp: sw,
            callbackManagedThreadId: Environment.CurrentManagedThreadId,
            payloadDiscriminator: RawEventPayloadKind.RecorderLifecycle,
            payload: new RecorderLifecyclePayload(
                RecorderLifecycleEventKind.ContractEpochAdvanced,
                "Contract identity tuple changed",
                previous,
                _contractEpoch,
                relatedSegmentId: null,
                previousIdentityTuple: previousIdentity.FormatTuple(),
                newIdentityTuple: draft.Instrument.FormatTuple()),
            integrityFlags: RecorderIntegrityFlags.ContractIdentityChanged,
            nativeSequenceAvailable: false);

        EnsureSegmentOpen(lifecycle);
        var seq = Interlocked.Increment(ref _writerSequence);
        var env = RawEventEnvelope.FromDraft(
            lifecycle,
            _writer!.CurrentSegmentId,
            _writer.CurrentSegmentOrdinal,
            seq,
            DateTime.UtcNow);
        if (!_writer.TryWriteRawEvent(env, out var serFail))
        {
            Interlocked.Increment(ref _counters.WriterDequeuedTotal);
            if (!serFail)
                Interlocked.Increment(ref _counters.WriterDiscardedAfterFatalFault);
            else
                Interlocked.Increment(ref _counters.SerializationFailures);
            Interlocked.Increment(ref _counters.SegmentWriteFailures);
            Interlocked.Increment(ref _counters.WorkerFaults);
            _fatal = true;
            _accepting = false;
            _stopReason = "IdentityLifecycleWriteFault";
            try { _writer.AbandonIncomplete(); } catch { /* preserve */ }
            return;
        }

        Interlocked.Increment(ref _counters.WriterDequeued);
        Interlocked.Increment(ref _counters.WriterDequeuedTotal);
        Interlocked.Increment(ref _counters.AcceptedToQueue);
        Interlocked.Increment(ref _counters.NormalizedObservations);
        Interlocked.Increment(ref _counters.PayloadItemsEnumerated);
    }

    private void EnsureSegmentOpen(RawEventDraft draft)
    {
        if (_writer is { IsOpen: true }) return;
        _segmentOrdinal++;
        var segmentId = Guid.NewGuid();
        _writer!.Open(
            _sessionId,
            _processId,
            segmentId,
            _segmentOrdinal,
            _contractEpoch,
            draft.Instrument,
            _declaredMode,
            _modeProvenance,
            _declaredProvider,
            _providerProvenance,
            Stopwatch.GetTimestamp(),
            DateTime.UtcNow);
    }

    private void CompleteCurrentSegment()
    {
        if (_writer is not { IsOpen: true }) return;
        var completed = _writer.Complete(Stopwatch.GetTimestamp(), DateTime.UtcNow);
        lock (_gate)
        {
            _completed.Add(completed);
            _sessionBytes += completed.ByteLength;
        }

        WriteManifest_NoThrow(stopUtc: null, abnormal: false, reason: null);
    }

    private bool EnsureDiskAndSessionBudget()
    {
        try
        {
            var free = _disk.GetAvailableBytes(_segmentsDir);
            if (free < _config.MinimumFreeSpaceBytes)
            {
                Interlocked.Increment(ref _counters.DiskSpaceStops);
                _accepting = false;
                _stopReason = "DiskSpaceStop";
                return false;
            }

            if (_sessionBytes >= _config.MaxSessionBytes)
            {
                Interlocked.Increment(ref _counters.DiskSpaceStops);
                _accepting = false;
                _stopReason = "MaxSessionBytes";
                return false;
            }

            return true;
        }
        catch
        {
            Interlocked.Increment(ref _counters.DiskSpaceStops);
            _accepting = false;
            _stopReason = "DiskProbeFault";
            return false;
        }
    }

    private void FinalizeSession_NoThrow()
    {
        try
        {
            if (_writer is { IsOpen: true })
                CompleteCurrentSegment();
        }
        catch
        {
            try { _writer?.AbandonIncomplete(); } catch { /* preserve */ }
            Interlocked.Increment(ref _counters.SegmentsIncomplete);
        }

        _stopUtc = DateTime.UtcNow;
        WriteManifest_NoThrow(_stopUtc, abnormal: _fatal || _stopReason is not null, reason: _stopReason);
    }

    private void WriteManifest_NoThrow(DateTime? stopUtc, bool abnormal, string? reason)
    {
        try
        {
            List<CompletedSegmentInfo> snapshot;
            lock (_gate) snapshot = _completed.ToList();

            // Only list segments that have both file + hash (completed list already guarantees)
            var segments = snapshot.Select(s => new ManifestSegmentRecord(
                s.SegmentId,
                s.SegmentOrdinal,
                s.FileName,
                s.Sha256Hex,
                s.RecordCount,
                s.MarketEventRecordCount,
                s.InvocationResultRecordCount,
                s.LifecycleIntegrityRecordCount,
                s.ByteLength,
                s.FirstWriterSequence,
                s.LastWriterSequence,
                s.ContractEpoch,
                s.CategoryCountsKnown)).ToList();

            var manifest = new SessionManifestRecord(
                RawEventRecorderVersions.ManifestGeneration,
                RawEventRecorderVersions.RawEventRecorderSchemaVersion,
                RawEventRecorderVersions.RawEventContainerVersion,
                _sessionId,
                _processId,
                _startUtc,
                stopUtc,
                _contractEpochs.ToList(),
                _declaredMode,
                _modeProvenance,
                _declaredProvider,
                _providerProvenance,
                _currentIdentity,
                new[] { "Trade", "Dom" },
                new[] { new DisabledStreamRecord("Mbo", MboOperationalLock.MboOperationalBlockReason) },
                MboOperationalLock.MboSchemaSupported,
                MboOperationalLock.MboRecordingEnabled,
                MboOperationalLock.MboIsolationRequirement.ToString(),
                MboOperationalLock.MboOperationalBlockReason,
                segments,
                snapshot.Count == 0 ? null : snapshot.Min(s => s.FirstWriterSequence),
                snapshot.Count == 0 ? null : snapshot.Max(s => s.LastWriterSequence),
                _counters.RecordsWritten,
                _counters.BytesWritten,
                _counters.Snapshot(),
                abnormal,
                reason,
                new[]
                {
                    "LOCAL_CAPTURE_NOT_EXCHANGE_COMPLETENESS",
                    "SEGMENTS_AUTHORITATIVE_MANIFEST_RECOVERABLE_INDEX",
                    "MBO_PRIMARY_PROCESS_BLOCKED"
                },
                new CapabilityClaimsForcedFalse(),
                RawEventRecorderVersions.ContinuityDisclaimer);

            ManifestWriter.WriteAtomic(
                RecorderStoragePaths.GetManifestPath(_sessionId, _userProfileOverride),
                RecorderStoragePaths.GetManifestHashPath(_sessionId, _userProfileOverride),
                manifest,
                _counters);
        }
        catch
        {
            // ManifestFailures already counted inside writer on throw; ensure count if swallowed earlier
        }
    }
}
