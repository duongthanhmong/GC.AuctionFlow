using System.Diagnostics;
using System.Text;
using System.Text.Json;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Recorder;
using GC.AuctionFlow.Recorder.FanOut;
using GC.AuctionFlow.Recorder.Payloads;

namespace GC.AuctionFlow.Tests.Unit.Recorder;

public sealed class WriterTotalAccountingCloseoutTests
{
    [Fact]
    public void Mixed_market_invocation_lifecycle_reconciles_WriterDequeuedTotal()
    {
        using var temp = new TempProfile();
        var sessionId = Guid.NewGuid();
        using var session = new RawEventRecorderSession(
            sessionId, Guid.NewGuid(), "Live", "OperatorDeclared", "Rithmic", "OperatorDeclared",
            new RecorderConfig(minimumFreeSpaceBytes: 0, maxSessionBytes: 64L * 1024 * 1024),
            userProfileOverride: temp.Root);

        session.NoteNormalizedObservation();
        session.NotePayloadItemEnumerated();
        Assert.True(session.TryWrite(TestFixtures.Draft(instrument: TestFixtures.Instrument("GCQ6", "A"))));

        // Force lifecycle by identity change on second market draft.
        session.NoteNormalizedObservation();
        session.NotePayloadItemEnumerated();
        Assert.True(session.TryWrite(TestFixtures.Draft(instrument: TestFixtures.Instrument("GCQ6", "B"), streamLocal: 2)));

        var inv = TradeToRawEventAdapter.ToInvocationResultDraft(
            new CallbackInvocationResultPayload(
                RecorderCallbackSource.OnNewTrade, 1, DateTime.UtcNow, 1, 1,
                false, true, 1, 0, 0, 0, 0, true, null),
            new CallbackCaptureContext(RecorderCallbackSource.OnNewTrade, 1, DateTime.UtcNow, 1, 1),
            TestFixtures.Instrument("GCQ6", "B"),
            sessionId, Guid.NewGuid(), "Live", "OperatorDeclared", "Rithmic", "OperatorDeclared");
        Assert.True(session.TryWrite(inv));

        RecorderTestWait.Until(
            () => session.Counters.MarketEventsWritten >= 2
                  && session.Counters.LifecycleIntegrityRecordsWritten >= 1
                  && session.Counters.InvocationResultsWritten >= 1,
            TimeSpan.FromSeconds(5));

        session.StopAccepting("test");
        var recon = session.DisposeAndReconcile();
        Assert.True(recon.Ok, string.Join("; ", recon.Mismatches));

        var c = session.Counters.Snapshot();
        Assert.Equal(
            c.MarketEventsWritten + c.InvocationResultsWritten + c.LifecycleIntegrityRecordsWritten
            + c.SerializationFailures + c.WriterDiscardedAfterFatalFault,
            c.WriterDequeuedTotal);
        Assert.True(c.MarketEventsWritten >= 2);
        Assert.Equal(1, c.InvocationResultsWritten);
        Assert.True(c.LifecycleIntegrityRecordsWritten >= 1);
        Assert.Equal(c.AcceptedToQueue, c.NormalizedObservations); // no inv in market normalized
        Assert.Equal(c.InvocationResultAcceptedToQueue, c.InvocationResultsWritten);
        Assert.Equal(0, c.UndrainedAtShutdown);
        Assert.Equal(0, c.WriterDiscardedAfterFatalFault);
    }

    [Fact]
    public void Clean_shutdown_WriterDequeuedTotal_equation_holds()
    {
        using var temp = new TempProfile();
        var sessionId = Guid.NewGuid();
        using var session = new RawEventRecorderSession(
            sessionId, Guid.NewGuid(), "Live", "OperatorDeclared", "Rithmic", "OperatorDeclared",
            new RecorderConfig(minimumFreeSpaceBytes: 0, maxSessionBytes: 64L * 1024 * 1024),
            userProfileOverride: temp.Root);

        session.NoteNormalizedObservation();
        session.NotePayloadItemEnumerated();
        Assert.True(session.TryWrite(TestFixtures.Draft()));
        RecorderTestWait.Until(() => session.Counters.RecordsWritten >= 1, TimeSpan.FromSeconds(5));
        session.StopAccepting("clean");
        var recon = session.DisposeAndReconcile();
        Assert.True(recon.Ok, string.Join("; ", recon.Mismatches));
        var c = session.Counters.Snapshot();
        Assert.Equal(
            c.MarketEventsWritten + c.InvocationResultsWritten + c.LifecycleIntegrityRecordsWritten
            + c.SerializationFailures + c.WriterDiscardedAfterFatalFault,
            c.WriterDequeuedTotal);
        Assert.Equal(c.AcceptedToQueue, c.RecordsWritten);
        Assert.Equal(0, c.WriterDiscardedAfterFatalFault);
        Assert.Equal(0, c.UndrainedAtShutdown);
    }

    [Fact]
    public void Serialization_failure_counts_in_WriterDequeuedTotal_without_double_write()
    {
        var c = new RecorderCounters
        {
            WriterDequeuedTotal = 3,
            MarketEventsWritten = 1,
            InvocationResultsWritten = 0,
            LifecycleIntegrityRecordsWritten = 0,
            SerializationFailures = 1,
            WriterDiscardedAfterFatalFault = 1,
            WriterDequeued = 3,
            RecordsWritten = 1,
            AcceptedToQueue = 3,
            NormalizedObservations = 3,
            PayloadItemsEnumerated = 3
        };
        var snap = c.Snapshot();
        var r = RecorderReconciliation.Evaluate(snap, cleanShutdown: false);
        Assert.True(r.Ok, string.Join("; ", r.Mismatches));
        Assert.Equal(
            c.MarketEventsWritten + c.InvocationResultsWritten + c.LifecycleIntegrityRecordsWritten
            + c.SerializationFailures + c.WriterDiscardedAfterFatalFault,
            c.WriterDequeuedTotal);
        Assert.Equal(c.RecordsWritten + c.SerializationFailures + c.WriterDiscardedAfterFatalFault, c.WriterDequeued);
    }

    [Fact]
    public void Fatal_discard_is_not_double_counted_as_written()
    {
        var c = new RecorderCounters
        {
            WriterDequeuedTotal = 2,
            MarketEventsWritten = 0,
            InvocationResultsWritten = 0,
            LifecycleIntegrityRecordsWritten = 0,
            SerializationFailures = 0,
            WriterDiscardedAfterFatalFault = 2,
            WriterDequeued = 2,
            RecordsWritten = 0,
            AcceptedToQueue = 2,
            NormalizedObservations = 2,
            PayloadItemsEnumerated = 2,
            QueueFullDrops = 0
        };
        var r = RecorderReconciliation.Evaluate(c.Snapshot(), cleanShutdown: false);
        Assert.True(r.Ok, string.Join("; ", r.Mismatches));
        Assert.Equal(0, c.MarketEventsWritten + c.InvocationResultsWritten + c.LifecycleIntegrityRecordsWritten);
    }
}

public sealed class CallbackStartupPerformanceCloseoutTests
{
    [Fact]
    public void EnsureStarted_on_callback_path_does_not_create_session_or_directories()
    {
        using var temp = new TempProfile();
        var host = new TradeRecorderHost();
        var mapper = new TradeStreamAtasMapper();
        var sessionId = Guid.NewGuid();
        var outcome = host.EnsureStarted(
            true, true,
            new ObservedInstrumentSnapshot("GCQ6", "1", "GCQ6", "COMEX", null, 0.1m, null, "GCQ6", "COMEX", 0.1m, null),
            "GCQ6",
            GC.AuctionFlow.Core.DataSourceMode.Live,
            GC.AuctionFlow.Core.DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic,
            FeedProviderProvenance.OperatorDeclared,
            sessionId,
            mapper,
            temp.Root);

        Assert.Equal(RecorderSinkOutcome.SessionNotStarted, outcome);
        Assert.Null(host.Session);
        Assert.False(Directory.Exists(RecorderStoragePaths.GetSessionDirectory(sessionId, temp.Root)));

        host.TryCompleteStartupFromLifecycle();
        Assert.NotNull(host.Session);
        Assert.True(host.IsAccepting);
        Assert.True(Directory.Exists(RecorderStoragePaths.GetSessionDirectory(sessionId, temp.Root)));
        host.Dispose();
    }

    [Fact]
    public void Trade_callback_host_source_has_no_blocking_wait_or_file_io_in_EnsureStarted()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "GC.AuctionFlow", "Recorder", "TradeRecorderHost.cs"));
        var text = File.ReadAllText(path);
        // EnsureStarted body must not construct RawEventRecorderSession (I/O); lifecycle method may.
        var ensureIdx = text.IndexOf("public FanOut.RecorderSinkOutcome EnsureStarted", StringComparison.Ordinal);
        var lifecycleIdx = text.IndexOf("TryCompleteStartupFromLifecycle", StringComparison.Ordinal);
        Assert.True(ensureIdx >= 0 && lifecycleIdx > ensureIdx);
        var ensureRegion = text[ensureIdx..lifecycleIdx];
        Assert.DoesNotContain("new RawEventRecorderSession", ensureRegion, StringComparison.Ordinal);
        Assert.DoesNotContain("EnsureSessionLayout", ensureRegion, StringComparison.Ordinal);
        Assert.DoesNotContain(".Wait(", ensureRegion, StringComparison.Ordinal);
        Assert.DoesNotContain("Task.Result", ensureRegion, StringComparison.Ordinal);
        Assert.DoesNotContain("Serialize", ensureRegion, StringComparison.Ordinal);
        Assert.Contains("new RawEventRecorderSession", text[lifecycleIdx..], StringComparison.Ordinal);
    }
}

public sealed class BackwardSchemaRecoveryCloseoutTests
{
    [Fact]
    public void Schema_1_1_0_footer_without_category_fields_is_recoverable()
    {
        // Simulate 1.1.0 footer JSON (no category fields / claim).
        var footerJson =
            """{"segmentId":"11111111-1111-1111-1111-111111111111","segmentOrdinal":1,"endedUtc":"2026-01-01T00:00:00Z","endedStopwatchTimestamp":1,"rawEventRecordCount":2,"firstWriterSequence":1,"lastWriterSequence":2,"bytesBeforeFooter":100,"completedNormally":true}"""u8.ToArray();
        var round = RecorderJson.DeserializeFooter(footerJson);
        Assert.NotNull(round);
        Assert.Equal(2, round!.RawEventRecordCount);
        Assert.False(round.CategoryCountsClaimed);
        Assert.Equal(0, round.MarketEventRecordCount);
        Assert.False(RecorderSchemaCompatibility.RequiresCategoryCounts("1.1.0"));
        Assert.True(RecorderSchemaCompatibility.RequiresCategoryCounts("1.2.0"));
    }

    [Fact]
    public void Schema_1_1_0_segment_recovers_without_claiming_category_counts()
    {
        using var temp = new TempProfile();
        var config = new RecorderConfig(minimumFreeSpaceBytes: 0, maxSessionBytes: 64L * 1024 * 1024);
        var sessionId = Guid.NewGuid();
        RecorderStoragePaths.EnsureSessionLayout(sessionId, temp.Root);
        var segmentsDir = RecorderStoragePaths.GetSegmentsDirectory(sessionId, temp.Root);
        Guid segId;
        using (var writer = new SegmentWriter(segmentsDir, config, new RecorderCounters()))
        {
            segId = Guid.NewGuid();
            writer.Open(sessionId, Guid.NewGuid(), segId, 1, 1, TestFixtures.Instrument(), "Live", "Op", "R", "Op", Stopwatch.GetTimestamp(), DateTime.UtcNow);
            writer.WriteRawEvent(RawEventEnvelope.FromDraft(TestFixtures.Draft(), segId, 1, 1, DateTime.UtcNow));
            writer.Complete(Stopwatch.GetTimestamp(), DateTime.UtcNow);
        }

        var path = Directory.GetFiles(segmentsDir, "*.seg").Single();
        var src = File.ReadAllBytes(path);
        var rebuilt = new List<byte>();
        rebuilt.AddRange(src.Take(ContainerFormat.ContainerHeaderSize));
        var offset = ContainerFormat.ContainerHeaderSize;
        SegmentHeaderRecord? oldHeader = null;
        SegmentFooterRecord? oldFooter = null;
        while (offset < src.Length)
        {
            Assert.Equal(FrameDecodeStatus.Ok, FrameCodec.TryDecodeFrame(src.AsSpan(offset), config.MaxFramePayloadBytes, out var frame, out var consumed, out _));
            if (frame.FrameType == RecorderFrameType.SegmentHeader)
            {
                oldHeader = RecorderJson.DeserializeHeader(frame.PayloadUtf8);
                Assert.NotNull(oldHeader);
                var h11 = new SegmentHeaderRecord(
                    "1.1.0",
                    oldHeader!.ContainerVersion,
                    oldHeader.SessionId,
                    oldHeader.RecorderProcessInstanceId,
                    oldHeader.SegmentId,
                    oldHeader.SegmentOrdinal,
                    oldHeader.ContractEpoch,
                    oldHeader.StartedUtc,
                    oldHeader.StartedStopwatchTimestamp,
                    oldHeader.Instrument,
                    oldHeader.DeclaredDataSourceMode,
                    oldHeader.ModeProvenance,
                    oldHeader.DeclaredProvider,
                    oldHeader.ProviderProvenance);
                rebuilt.AddRange(FrameCodec.EncodeFrame(RecorderFrameType.SegmentHeader, RecorderJson.SerializeHeader(h11)));
            }
            else if (frame.FrameType == RecorderFrameType.SegmentFooter)
            {
                oldFooter = RecorderJson.DeserializeFooter(frame.PayloadUtf8);
                Assert.NotNull(oldFooter);
                // Legacy 1.1.0 footer: no category fields, no claim.
                var footerJson = System.Text.Encoding.UTF8.GetBytes(
                    $"{{\"segmentId\":\"{oldFooter!.SegmentId}\",\"segmentOrdinal\":{oldFooter.SegmentOrdinal},\"endedUtc\":\"{oldFooter.EndedUtc:O}\",\"endedStopwatchTimestamp\":{oldFooter.EndedStopwatchTimestamp},\"rawEventRecordCount\":{oldFooter.RawEventRecordCount},\"firstWriterSequence\":{oldFooter.FirstWriterSequence},\"lastWriterSequence\":{oldFooter.LastWriterSequence},\"bytesBeforeFooter\":{oldFooter.BytesBeforeFooter},\"completedNormally\":{(oldFooter.CompletedNormally ? "true" : "false")}}}");
                rebuilt.AddRange(FrameCodec.EncodeFrame(RecorderFrameType.SegmentFooter, footerJson));
            }
            else
            {
                rebuilt.AddRange(src.Skip(offset).Take(consumed));
            }

            offset += consumed;
        }

        File.WriteAllBytes(path, rebuilt.ToArray());
        // Hash file may no longer match; recovery recomputes SHA when hash file invalid — delete sidecar hash.
        var hashPath = Path.Combine(segmentsDir, RecorderStoragePaths.SegmentHashFileName(segId));
        if (File.Exists(hashPath)) File.Delete(hashPath);

        Assert.True(RecoveryScanner.ValidateSegmentFile(path, config, out var info, out var cls, out var detail), detail);
        Assert.Equal(RecoveryClassification.TrustedComplete, cls);
        Assert.NotNull(info);
        Assert.False(info!.CategoryCountsKnown);
        Assert.Equal(0, info.MarketEventRecordCount);
        Assert.Equal(0, info.InvocationResultRecordCount);
        Assert.Equal(0, info.LifecycleIntegrityRecordCount);
        Assert.Equal(1, info.RecordCount);
    }

    [Fact]
    public void Schema_1_2_0_inconsistent_category_counts_rejected_by_ctor()
    {
        Assert.Throws<ArgumentException>(() =>
            new SegmentFooterRecord(
                Guid.NewGuid(), 1, DateTime.UtcNow, 1,
                rawEventRecordCount: 3,
                marketEventRecordCount: 1,
                invocationResultRecordCount: 0,
                lifecycleIntegrityRecordCount: 0,
                firstWriterSequence: 1,
                lastWriterSequence: 1,
                bytesBeforeFooter: 10,
                completedNormally: true,
                categoryCountsClaimed: true));
    }

    [Fact]
    public void Schema_1_2_0_footer_category_mismatch_vs_scan_rejected_by_recovery()
    {
        using var temp = new TempProfile();
        var config = new RecorderConfig(minimumFreeSpaceBytes: 0, maxSessionBytes: 64L * 1024 * 1024);
        var sessionId = Guid.NewGuid();
        RecorderStoragePaths.EnsureSessionLayout(sessionId, temp.Root);
        var segmentsDir = RecorderStoragePaths.GetSegmentsDirectory(sessionId, temp.Root);
        Guid segId;
        using (var writer = new SegmentWriter(segmentsDir, config, new RecorderCounters()))
        {
            segId = Guid.NewGuid();
            writer.Open(sessionId, Guid.NewGuid(), segId, 1, 1, TestFixtures.Instrument(), "Live", "Op", "R", "Op", Stopwatch.GetTimestamp(), DateTime.UtcNow);
            writer.WriteRawEvent(RawEventEnvelope.FromDraft(TestFixtures.Draft(), segId, 1, 1, DateTime.UtcNow));
            writer.Complete(Stopwatch.GetTimestamp(), DateTime.UtcNow);
        }

        var path = Directory.GetFiles(segmentsDir, "*.seg").Single();
        var src = File.ReadAllBytes(path);
        var rebuilt = new List<byte>();
        rebuilt.AddRange(src.Take(ContainerFormat.ContainerHeaderSize));
        var offset = ContainerFormat.ContainerHeaderSize;
        while (offset < src.Length)
        {
            Assert.Equal(FrameDecodeStatus.Ok, FrameCodec.TryDecodeFrame(src.AsSpan(offset), config.MaxFramePayloadBytes, out var frame, out var consumed, out _));
            if (frame.FrameType == RecorderFrameType.SegmentFooter)
            {
                var old = RecorderJson.DeserializeFooter(frame.PayloadUtf8)!;
                // Sum matches RawEventRecordCount but wrong category vs scanned market event.
                var bad = new SegmentFooterRecord(
                    old.SegmentId, old.SegmentOrdinal, old.EndedUtc, old.EndedStopwatchTimestamp,
                    rawEventRecordCount: 1,
                    marketEventRecordCount: 0,
                    invocationResultRecordCount: 1,
                    lifecycleIntegrityRecordCount: 0,
                    firstWriterSequence: old.FirstWriterSequence,
                    lastWriterSequence: old.LastWriterSequence,
                    bytesBeforeFooter: old.BytesBeforeFooter,
                    completedNormally: true,
                    categoryCountsClaimed: true);
                rebuilt.AddRange(FrameCodec.EncodeFrame(RecorderFrameType.SegmentFooter, RecorderJson.SerializeFooter(bad)));
            }
            else
            {
                rebuilt.AddRange(src.Skip(offset).Take(consumed));
            }

            offset += consumed;
        }

        File.WriteAllBytes(path, rebuilt.ToArray());
        Assert.False(RecoveryScanner.ValidateSegmentFile(path, config, out _, out var cls, out _));
        Assert.Equal(RecoveryClassification.ReconciliationMismatch, cls);
    }

    [Fact]
    public void Live_1_2_0_segment_requires_category_validation_in_recovery()
    {
        using var temp = new TempProfile();
        var config = new RecorderConfig(minimumFreeSpaceBytes: 0, maxSessionBytes: 64L * 1024 * 1024);
        var sessionId = Guid.NewGuid();
        RecorderStoragePaths.EnsureSessionLayout(sessionId, temp.Root);
        var segmentsDir = RecorderStoragePaths.GetSegmentsDirectory(sessionId, temp.Root);
        using (var writer = new SegmentWriter(segmentsDir, config, new RecorderCounters()))
        {
            var id = Guid.NewGuid();
            writer.Open(sessionId, Guid.NewGuid(), id, 1, 1, TestFixtures.Instrument(), "Live", "Op", "R", "Op", Stopwatch.GetTimestamp(), DateTime.UtcNow);
            writer.WriteRawEvent(RawEventEnvelope.FromDraft(TestFixtures.Draft(), id, 1, 1, DateTime.UtcNow));
            writer.Complete(Stopwatch.GetTimestamp(), DateTime.UtcNow);
        }

        var seg = Directory.GetFiles(segmentsDir, "*.seg").Single();
        Assert.True(RecoveryScanner.ValidateSegmentFile(seg, config, out var info, out var cls, out _));
        Assert.Equal(RecoveryClassification.TrustedComplete, cls);
        Assert.True(info!.CategoryCountsKnown);
        Assert.Equal(info.RecordCount, info.MarketEventRecordCount + info.InvocationResultRecordCount + info.LifecycleIntegrityRecordCount);
    }
}

public sealed class RawEnumStorageCloseoutTests
{
    [Fact]
    public void DirectionRaw_and_DataTypeRaw_are_Int64()
    {
        var p = new NewTradePayload(1, 1, 1, long.MaxValue - 3, "Buy", long.MaxValue - 4, "Trade", true, false, null, null, null);
        Assert.Equal(typeof(long), p.DirectionRaw.GetType());
        Assert.Equal(typeof(long), p.DataTypeRaw.GetType());
        Assert.Equal(long.MaxValue - 3, p.DirectionRaw);
        Assert.Equal("Buy", p.DirectionName);

        var adapter = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "GC.AuctionFlow", "Recorder", "TradeToRawEventAdapter.cs")));
        Assert.Contains("Convert.ToInt64", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("Convert.ToInt32", adapter, StringComparison.Ordinal);
    }
}
