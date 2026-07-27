using System.Diagnostics;
using System.Reflection;
using System.Text;
using GC.AuctionFlow.Recorder;
using GC.AuctionFlow.Recorder.Payloads;

namespace GC.AuctionFlow.Tests.Unit.Recorder;

public sealed class RawEventDraftEnvelopeTests
{
    [Fact]
    public void Draft_has_no_SegmentId_or_writer_global_sequence()
    {
        var draftType = typeof(RawEventDraft);
        Assert.Null(draftType.GetProperty("SegmentId"));
        Assert.Null(draftType.GetProperty("RecorderGlobalLocalSequence"));
        Assert.Null(draftType.GetProperty("WriterDequeuedUtc"));
        Assert.Null(draftType.GetProperty("SegmentOrdinal"));
    }

    [Fact]
    public void Writer_assigns_SegmentId_and_global_sequence_without_mutating_draft()
    {
        var draft = TestFixtures.Draft();
        var segmentId = Guid.NewGuid();
        var env = RawEventEnvelope.FromDraft(draft, segmentId, 1, 42, DateTime.UtcNow);
        Assert.Equal(segmentId, env.SegmentId);
        Assert.Equal(42, env.RecorderGlobalLocalSequence);
        Assert.Equal(1, env.SegmentOrdinal);
        Assert.Equal(draft.StreamLocalCaptureSequence, env.StreamLocalCaptureSequence);
        Assert.Same(draft.Payload, env.Payload);
    }

    [Fact]
    public void NativeSequenceAvailable_defaults_false()
    {
        Assert.False(TestFixtures.Draft().NativeSequenceAvailable);
    }
}

public sealed class PayloadTaxonomyTests
{
    [Fact]
    public void Cumulative_payloads_do_not_claim_constituent_ticks()
    {
        var neu = new CumulativeTradeNewPayload(1, 2, 3, 1, "Buy", false, null, null, false);
        var upd = new CumulativeTradeUpdatePayload(1, 2, 3, 1, "Buy", false, null, null, false);
        Assert.False(neu.CumulativeTickConstituentsRecorded);
        Assert.False(upd.CumulativeTickConstituentsRecorded);
        Assert.False(neu.ReportedTickCountAvailable);
        Assert.Null(neu.ReportedTickCount);
        Assert.False(upd.ReportedTickCountAvailable);
        Assert.Null(upd.ReportedTickCount);
    }

    [Fact]
    public void Dom_snapshot_provider_completion_always_false()
    {
        var id = Guid.NewGuid();
        Assert.False(new DomSnapshotRequestPayload(id, DateTime.UtcNow).ProviderSnapshotCompletionKnown);
        Assert.False(new DomSnapshotItemPayload(id, 0, 1, 1, "Bid", true, false, "Bid").ProviderSnapshotCompletionKnown);
        Assert.False(new DomSnapshotLocalEnumerationResultPayload(id, true, true, true, 1, null).ProviderSnapshotCompletionKnown);
    }

    [Fact]
    public void Closed_discriminator_kinds_are_exact()
    {
        Assert.Equal(RawEventPayloadKind.NewTrade, new NewTradePayload(1, 1, 1, 1, "Buy", 2, "Trade", true, false, null, null, null).PayloadKind);
        Assert.Equal(RawEventPayloadKind.Mbo, new MboPayload("Snapshot", 0, true, "Bid", 1, "Bid", 1, 1, 1, 1).PayloadKind);
        Assert.Equal(RawEventPayloadKind.RecorderLifecycle, new RecorderLifecyclePayload(RecorderLifecycleEventKind.SessionStarted, "x", null, null, null).PayloadKind);
    }
}

public sealed class FramingAndCrcTests
{
    [Fact]
    public void Deterministic_source_generated_json_bytes()
    {
        var env = RawEventEnvelope.FromDraft(
            TestFixtures.Draft(),
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            1,
            7,
            DateTime.Parse("2026-07-22T00:00:00Z").ToUniversalTime());
        var a = RecorderJson.SerializeEnvelope(env);
        var b = RecorderJson.SerializeEnvelope(env);
        Assert.Equal(a, b);
        var json = Encoding.UTF8.GetString(a);
        Assert.DoesNotContain("\n  ", json);
    }

    [Fact]
    public void Mandatory_crc32c_detects_payload_corruption()
    {
        var payload = Encoding.UTF8.GetBytes("{\"x\":1}");
        var frame = FrameCodec.EncodeFrame(RecorderFrameType.RawEvent, payload);
        frame[ContainerFormat.FrameHeaderSize] ^= 0xFF;
        var status = FrameCodec.TryDecodeFrame(frame, 1024 * 1024, out _, out _, out _);
        Assert.Equal(FrameDecodeStatus.FrameCrcMismatch, status);
    }

    [Fact]
    public void Frame_header_type_corruption_fails_crc()
    {
        var frame = FrameCodec.EncodeFrame(RecorderFrameType.SegmentHeader, "{}"u8.ToArray());
        frame[6] ^= 0xFF;
        var status = FrameCodec.TryDecodeFrame(frame, 1024 * 1024, out _, out _, out _);
        Assert.Equal(FrameDecodeStatus.FrameCrcMismatch, status);
    }

    [Fact]
    public void Truncated_frame_needs_more_data()
    {
        var frame = FrameCodec.EncodeFrame(RecorderFrameType.RawEvent, "hello"u8.ToArray());
        var status = FrameCodec.TryDecodeFrame(frame.AsSpan(0, frame.Length - 1), 1024 * 1024, out _, out _, out _);
        Assert.Equal(FrameDecodeStatus.NeedMoreData, status);
    }

    [Fact]
    public void Oversized_frame_rejected_before_allocation()
    {
        var header = new byte[ContainerFormat.FrameHeaderSize];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(header, ContainerFormat.FrameMagic);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(4), 1);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(6), (ushort)RecorderFrameType.RawEvent);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(12), (uint)(2 * 1024 * 1024));
        var status = FrameCodec.TryDecodeFrame(header, maxPayloadBytes: 1024 * 1024, out _, out _, out _);
        Assert.Equal(FrameDecodeStatus.OversizedFrame, status);
    }

    [Fact]
    public void Footer_is_framed_record()
    {
        var footer = new SegmentFooterRecord(Guid.NewGuid(), 1, DateTime.UtcNow, 1, 0, 0, 0, 0, 0, 0, 0, true);
        var bytes = RecorderJson.SerializeFooter(footer);
        var frame = FrameCodec.EncodeFrame(RecorderFrameType.SegmentFooter, bytes);
        Assert.Equal(FrameDecodeStatus.Ok, FrameCodec.TryDecodeFrame(frame, 1024 * 1024, out var decoded, out _, out _));
        Assert.Equal(RecorderFrameType.SegmentFooter, decoded.FrameType);
    }

    [Fact]
    public void Invalid_frame_magic_detected()
    {
        var frame = FrameCodec.EncodeFrame(RecorderFrameType.RawEvent, "x"u8.ToArray());
        frame[0] = 0;
        Assert.Equal(FrameDecodeStatus.InvalidFrameMagic, FrameCodec.TryDecodeFrame(frame, 1024, out _, out _, out _));
    }
}

public sealed class SegmentLifecycleTests
{
    [Fact]
    public void Tmp_file_is_never_trusted_and_completion_ordering_holds()
    {
        using var temp = new TempProfile();
        var counters = new RecorderCounters();
        var config = new RecorderConfig(maxSegmentBytes: 1024 * 1024, maxRecordsPerSegment: 100, maxSegmentDuration: TimeSpan.FromMinutes(5), queueCapacity: 64, minimumFreeSpaceBytes: 0, maxSessionBytes: 10L * 1024 * 1024);
        var sessionId = Guid.NewGuid();
        RecorderStoragePaths.EnsureSessionLayout(sessionId, temp.Root);
        var segmentsDir = RecorderStoragePaths.GetSegmentsDirectory(sessionId, temp.Root);
        using var writer = new SegmentWriter(segmentsDir, config, counters);
        var instrument = TestFixtures.Instrument();
        writer.Open(sessionId, Guid.NewGuid(), Guid.NewGuid(), 1, 1, instrument, "Live", "Operator", "Rithmic", "Operator", Stopwatch.GetTimestamp(), DateTime.UtcNow);

        Assert.Single(Directory.GetFiles(segmentsDir, "*.seg.tmp"));
        var scanWhileOpen = RecoveryScanner.ScanSessionDirectory(RecorderStoragePaths.GetSessionDirectory(sessionId, temp.Root), config, copyQuarantine: false);
        Assert.Contains(scanWhileOpen.Findings, f => f.Classification == RecoveryClassification.IncompleteTemporary);
        Assert.Empty(scanWhileOpen.TrustedSegments);

        writer.WriteRawEvent(RawEventEnvelope.FromDraft(TestFixtures.Draft(instrument: instrument), writer.CurrentSegmentId, 1, 1, DateTime.UtcNow));
        var completed = writer.Complete(Stopwatch.GetTimestamp(), DateTime.UtcNow);
        Assert.True(File.Exists(completed.FullPath));
        Assert.True(File.Exists(Path.Combine(segmentsDir, RecorderStoragePaths.SegmentHashFileName(completed.SegmentId))));
        Assert.False(File.Exists(Path.Combine(segmentsDir, RecorderStoragePaths.SegmentTempFileName(completed.SegmentId))));
        Assert.Equal(SegmentWriter.ComputeSha256Hex(completed.FullPath), completed.Sha256Hex);
    }

    [Fact]
    public void Rotation_by_records_and_stopwatch_duration()
    {
        using var temp = new TempProfile();
        var config = new RecorderConfig(
            maxSegmentBytes: 64L * 1024 * 1024,
            maxRecordsPerSegment: 2,
            maxSegmentDuration: TimeSpan.FromHours(1),
            queueCapacity: 128,
            shutdownDrainTimeoutMilliseconds: 2000,
            minimumFreeSpaceBytes: 0,
            maxSessionBytes: 64L * 1024 * 1024);
        using var session = new RawEventRecorderSession(
            Guid.NewGuid(), Guid.NewGuid(), "Live", "OperatorDeclared", "Rithmic", "OperatorDeclared",
            config, new FixedDiskSpaceProbe(long.MaxValue), temp.Root);

        for (var i = 0; i < 5; i++)
        {
            session.NotePayloadItemEnumerated();
            session.NoteNormalizedObservation();
            Assert.True(session.TryWrite(TestFixtures.Draft(streamLocal: i + 1)));
        }

        RecorderTestWait.Until(() => session.Counters.RecordsWritten >= 5, TimeSpan.FromSeconds(5));
        session.Dispose();
        Assert.True(session.CompletedSegments.Count >= 2);

        var counters = new RecorderCounters();
        var durConfig = new RecorderConfig(maxSegmentBytes: 64L * 1024 * 1024, maxRecordsPerSegment: 1_000_000, maxSegmentDuration: TimeSpan.FromMilliseconds(1), queueCapacity: 8, minimumFreeSpaceBytes: 0, maxSessionBytes: 64L * 1024 * 1024);
        var sid = Guid.NewGuid();
        RecorderStoragePaths.EnsureSessionLayout(sid, temp.Root);
        using var w = new SegmentWriter(RecorderStoragePaths.GetSegmentsDirectory(sid, temp.Root), durConfig, counters);
        var start = Stopwatch.GetTimestamp();
        w.Open(sid, Guid.NewGuid(), Guid.NewGuid(), 1, 1, TestFixtures.Instrument(), "Live", "Op", "R", "Op", start, DateTime.UtcNow);
        Thread.Sleep(5);
        Assert.True(w.WouldExceedLimits(16, durConfig.FooterReserveBytes, Stopwatch.GetTimestamp()));
    }
}

public sealed class SessionAccountingAndIdentityTests
{
    [Fact]
    public void Writer_sequence_follows_dequeue_order()
    {
        using var temp = new TempProfile();
        var config = new RecorderConfig(queueCapacity: 64, minimumFreeSpaceBytes: 0, maxSessionBytes: 64L * 1024 * 1024, maxRecordsPerSegment: 1000);
        using var session = new RawEventRecorderSession(
            Guid.NewGuid(), Guid.NewGuid(), "Live", "OperatorDeclared", "Rithmic", "OperatorDeclared",
            config, new FixedDiskSpaceProbe(long.MaxValue), temp.Root);

        for (var i = 0; i < 10; i++)
        {
            session.NotePayloadItemEnumerated();
            session.NoteNormalizedObservation();
            Assert.True(session.TryWrite(TestFixtures.Draft(streamLocal: 100 + i)));
        }

        RecorderTestWait.Until(() => session.Counters.RecordsWritten >= 10, TimeSpan.FromSeconds(5));
        session.Dispose();

        var seg = session.CompletedSegments.Single();
        var bytes = File.ReadAllBytes(seg.FullPath);
        var offset = ContainerFormat.ContainerHeaderSize;
        long? prev = null;
        while (offset < bytes.Length)
        {
            Assert.Equal(FrameDecodeStatus.Ok, FrameCodec.TryDecodeFrame(bytes.AsSpan(offset), 1024 * 1024, out var frame, out var consumed, out _));
            offset += consumed;
            if (frame.FrameType != RecorderFrameType.RawEvent) continue;
            var env = RecorderJson.DeserializeEnvelope(frame.PayloadUtf8)!;
            if (prev is not null)
                Assert.Equal(prev.Value + 1, env.RecorderGlobalLocalSequence);
            prev = env.RecorderGlobalLocalSequence;
            Assert.True(env.StreamLocalCaptureSequence >= 100);
        }
    }

    [Fact]
    public void Reconciliation_equations_hold_on_clean_shutdown()
    {
        using var temp = new TempProfile();
        var config = new RecorderConfig(queueCapacity: 64, minimumFreeSpaceBytes: 0, maxSessionBytes: 64L * 1024 * 1024);
        var session = new RawEventRecorderSession(
            Guid.NewGuid(), Guid.NewGuid(), "Live", "OperatorDeclared", "Rithmic", "OperatorDeclared",
            config, new FixedDiskSpaceProbe(long.MaxValue), temp.Root);

        for (var i = 0; i < 3; i++)
        {
            session.NotePayloadItemEnumerated();
            session.NoteNormalizedObservation();
            Assert.True(session.TryWrite(TestFixtures.Draft(streamLocal: i + 1)));
        }

        RecorderTestWait.Until(() => session.Counters.RecordsWritten >= 3, TimeSpan.FromSeconds(5));
        var result = session.DisposeAndReconcile();
        Assert.True(result.Ok, string.Join("; ", result.Mismatches));
    }

    [Fact]
    public void Queue_full_drops_are_counted()
    {
        using var temp = new TempProfile();
        var config = new RecorderConfig(queueCapacity: 1, minimumFreeSpaceBytes: 0, maxSessionBytes: 64L * 1024 * 1024, shutdownDrainTimeoutMilliseconds: 50);
        using var session = new RawEventRecorderSession(
            Guid.NewGuid(), Guid.NewGuid(), "Live", "OperatorDeclared", "Rithmic", "OperatorDeclared",
            config, new FixedDiskSpaceProbe(long.MaxValue), temp.Root);

        for (var i = 0; i < 500; i++)
        {
            session.NotePayloadItemEnumerated();
            session.NoteNormalizedObservation();
            session.TryWrite(TestFixtures.Draft(streamLocal: i + 1));
        }

        Assert.True(session.Counters.QueueFullDrops > 0);
        session.Dispose();
    }

    [Fact]
    public void Contract_identity_change_closes_segment_and_increments_epoch()
    {
        using var temp = new TempProfile();
        var config = new RecorderConfig(queueCapacity: 64, minimumFreeSpaceBytes: 0, maxSessionBytes: 64L * 1024 * 1024, maxRecordsPerSegment: 1000);
        using var session = new RawEventRecorderSession(
            Guid.NewGuid(), Guid.NewGuid(), "Live", "OperatorDeclared", "Rithmic", "OperatorDeclared",
            config, new FixedDiskSpaceProbe(long.MaxValue), temp.Root);

        session.NotePayloadItemEnumerated();
        session.NoteNormalizedObservation();
        Assert.True(session.TryWrite(TestFixtures.Draft(instrument: TestFixtures.Instrument("GCQ6"))));
        RecorderTestWait.Until(() => session.Counters.RecordsWritten >= 1, TimeSpan.FromSeconds(5));

        session.NotePayloadItemEnumerated();
        session.NoteNormalizedObservation();
        Assert.True(session.TryWrite(TestFixtures.Draft(instrument: TestFixtures.Instrument("GCZ6", "GCZ6|1"))));
        RecorderTestWait.Until(() => session.ContractEpoch >= 2, TimeSpan.FromSeconds(5));
        RecorderTestWait.Until(() => session.Counters.RecordsWritten >= 3, TimeSpan.FromSeconds(5));
        session.Dispose();
        Assert.True(session.ContractEpoch >= 2);
        Assert.True(session.CompletedSegments.Count >= 2);
    }

    [Fact]
    public void Disk_full_stops_accepting_without_throwing()
    {
        using var temp = new TempProfile();
        var disk = new FixedDiskSpaceProbe(100);
        var config = new RecorderConfig(queueCapacity: 64, minimumFreeSpaceBytes: 1L * 1024 * 1024 * 1024, maxSessionBytes: 64L * 1024 * 1024);
        using var session = new RawEventRecorderSession(
            Guid.NewGuid(), Guid.NewGuid(), "Live", "OperatorDeclared", "Rithmic", "OperatorDeclared",
            config, disk, temp.Root);

        session.NotePayloadItemEnumerated();
        session.NoteNormalizedObservation();
        session.TryWrite(TestFixtures.Draft());
        RecorderTestWait.Until(() => session.Counters.DiskSpaceStops >= 1 || session.Counters.WriterDiscardedAfterFatalFault >= 1, TimeSpan.FromSeconds(5));
        session.Dispose();
        Assert.True(session.Counters.DiskSpaceStops >= 1);
    }

    [Fact]
    public void Idempotent_dispose_and_no_write_after_dispose()
    {
        using var temp = new TempProfile();
        var config = new RecorderConfig(queueCapacity: 32, minimumFreeSpaceBytes: 0, maxSessionBytes: 64L * 1024 * 1024);
        var session = new RawEventRecorderSession(
            Guid.NewGuid(), Guid.NewGuid(), "Live", "OperatorDeclared", "Rithmic", "OperatorDeclared",
            config, new FixedDiskSpaceProbe(long.MaxValue), temp.Root);
        session.Dispose();
        session.Dispose();
        Assert.False(session.TryWrite(TestFixtures.Draft()));
        Assert.True(session.Counters.RejectedCallbackInvocationsAfterDispose >= 1);
    }

    [Fact]
    public void Manifest_rebuild_from_trusted_segments()
    {
        using var temp = new TempProfile();
        var config = new RecorderConfig(queueCapacity: 64, minimumFreeSpaceBytes: 0, maxSessionBytes: 64L * 1024 * 1024);
        var sessionId = Guid.NewGuid();
        var processId = Guid.NewGuid();
        using (var session = new RawEventRecorderSession(
                   sessionId, processId, "Live", "OperatorDeclared", "Rithmic", "OperatorDeclared",
                   config, new FixedDiskSpaceProbe(long.MaxValue), temp.Root))
        {
            session.NotePayloadItemEnumerated();
            session.NoteNormalizedObservation();
            Assert.True(session.TryWrite(TestFixtures.Draft()));
            RecorderTestWait.Until(() => session.Counters.RecordsWritten >= 1, TimeSpan.FromSeconds(5));
        }

        var sessionDir = RecorderStoragePaths.GetSessionDirectory(sessionId, temp.Root);
        var scan = RecoveryScanner.ScanSessionDirectory(sessionDir, config);
        Assert.NotEmpty(scan.TrustedSegments);
        var rebuilt = RecoveryScanner.RebuildManifestFromTrusted(
            sessionId, processId, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow,
            scan.TrustedSegments, new RecorderCounters().Snapshot(), TestFixtures.Instrument(),
            "Live", "OperatorDeclared", "Rithmic", "OperatorDeclared");
        Assert.Equal(scan.TrustedSegments.Count, rebuilt.Segments.Count);
        Assert.False(rebuilt.MboRecordingEnabled);
        Assert.False(rebuilt.CapabilityClaimsForcedFalse.TradeFidelity);
    }
}

public sealed class MboLockAndHygieneTests
{
    [Fact]
    public void Mbo_schema_present_but_recording_blocked()
    {
        Assert.True(MboOperationalLock.MboSchemaSupported);
        Assert.False(MboOperationalLock.MboRecordingEnabledDefault);
        Assert.Equal(MboIsolationRequirement.OperatorDecision, MboOperationalLock.MboIsolationRequirement);
        // The reason is retained but withdrawn: the side effect was our own fsync.
        Assert.Contains("WITHDRAWN", MboOperationalLock.MboOperationalBlockReason, StringComparison.Ordinal);
    }

    [Fact]
    public void Recorder_contracts_have_no_ATAS_type_references()
    {
        Type[] contracts =
        [
            typeof(RawEventDraft),
            typeof(RawEventEnvelope),
            typeof(ObservedInstrumentIdentity),
            typeof(NewTradePayload),
            typeof(CumulativeTradeNewPayload),
            typeof(CumulativeTradeUpdatePayload),
            typeof(DepthPayload),
            typeof(BestBidAskPayload),
            typeof(DomSnapshotRequestPayload),
            typeof(DomSnapshotItemPayload),
            typeof(DomSnapshotLocalEnumerationResultPayload),
            typeof(MboPayload),
            typeof(RecorderLifecyclePayload),
            typeof(RecorderIntegrityPayload),
            typeof(SegmentHeaderRecord),
            typeof(SegmentFooterRecord),
            typeof(SessionManifestRecord)
        ];

        foreach (var type in contracts)
        {
            Assert.StartsWith("GC.AuctionFlow.Recorder", type.Namespace, StringComparison.Ordinal);
            foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var name = p.PropertyType.FullName ?? p.PropertyType.Name;
                Assert.DoesNotContain("ATAS.", name, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Recorder_source_has_no_MBO_subscription_or_chart_writes()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "GC.AuctionFlow", "Recorder"));
        Assert.True(Directory.Exists(root), root);
        foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("SubscribeMarketByOrderData", text, StringComparison.Ordinal);
            Assert.DoesNotContain("DataSeries", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Versions_locked_and_probe_versions_unchanged()
    {
        Assert.Equal("1.2.0", RawEventRecorderVersions.RawEventRecorderSchemaVersion);
        Assert.Equal(1, RawEventRecorderVersions.RawEventContainerVersion);
        Assert.Equal("1.0.1", GC.AuctionFlow.Probe.TradeStreamProbeVersions.TradeStreamProbeSchemaVersion);
        Assert.Equal("0.0.6", GC.AuctionFlow.Core.CapabilitySchemaVersions.ProbeVersionPlaceholder);
        Assert.Equal("0.0.6+P0-06", GC.AuctionFlow.Core.BuildInfo.Version);
    }

    [Fact]
    public void Reconciliation_does_not_throw_on_mismatch()
    {
        var bad = new RecorderCountersSnapshot(
            0, 0, 0, 0, 0, 0, 0, 5, 0, 1, 0, 0, 0,
            0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        var result = RecorderReconciliation.Evaluate(bad, cleanShutdown: true);
        Assert.False(result.Ok);
        Assert.NotEmpty(result.Mismatches);
    }
}

public sealed class RecoveryFaultTests
{
    [Fact]
    public void Truncated_segment_is_not_trusted()
    {
        using var temp = new TempProfile();
        var config = new RecorderConfig(minimumFreeSpaceBytes: 0, maxSessionBytes: 64L * 1024 * 1024);
        var sessionId = Guid.NewGuid();
        RecorderStoragePaths.EnsureSessionLayout(sessionId, temp.Root);
        var segmentsDir = RecorderStoragePaths.GetSegmentsDirectory(sessionId, temp.Root);
        var counters = new RecorderCounters();
        using (var writer = new SegmentWriter(segmentsDir, config, counters))
        {
            var id = Guid.NewGuid();
            writer.Open(sessionId, Guid.NewGuid(), id, 1, 1, TestFixtures.Instrument(), "Live", "Op", "R", "Op", Stopwatch.GetTimestamp(), DateTime.UtcNow);
            writer.WriteRawEvent(RawEventEnvelope.FromDraft(TestFixtures.Draft(), id, 1, 1, DateTime.UtcNow));
            writer.Complete(Stopwatch.GetTimestamp(), DateTime.UtcNow);
        }

        var seg = Directory.GetFiles(segmentsDir, "*.seg").Single();
        var bytes = File.ReadAllBytes(seg);
        File.WriteAllBytes(seg, bytes.AsSpan(0, Math.Max(ContainerFormat.ContainerHeaderSize + 8, bytes.Length / 2)).ToArray());
        var stem = Path.GetFileNameWithoutExtension(seg);
        SegmentWriter.WriteSha256Atomic(segmentsDir, Guid.Parse(stem), SegmentWriter.ComputeSha256Hex(seg));

        var scan = RecoveryScanner.ScanSessionDirectory(RecorderStoragePaths.GetSessionDirectory(sessionId, temp.Root), config);
        Assert.Empty(scan.TrustedSegments);
    }
}

internal static class TestFixtures
{
    public static ObservedInstrumentIdentity Instrument(string code = "GCQ6", string identityKey = "GCQ6|COMEX|1") =>
        new(code, "1", "COMEX", "2026-08", 0.1m, identityKey);

    public static RawEventDraft Draft(
        ObservedInstrumentIdentity? instrument = null,
        long streamLocal = 1,
        RecorderCallbackSource source = RecorderCallbackSource.OnNewTrade,
        long invocationSequence = 1,
        int itemOrdinal = 0) =>
        new(
            RawEventRecorderVersions.RawEventRecorderSchemaVersion,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            RecorderStreamKind.Trade,
            source,
            streamLocal,
            invocationSequence,
            itemOrdinal,
            subscriptionOrCaptureEpoch: null,
            contractEpoch: 1,
            instrument: instrument ?? Instrument(),
            declaredDataSourceMode: "Live",
            modeProvenance: "OperatorDeclared",
            declaredProvider: "Rithmic",
            providerProvenance: "OperatorDeclared",
            sourceTimeTicks: 638000000000000000,
            sourceDateTimeKind: DateTimeKind.Unspecified,
            callbackReceiveUtc: DateTime.Parse("2026-07-22T12:00:00Z").ToUniversalTime(),
            callbackReceiveStopwatchTimestamp: 123,
            callbackManagedThreadId: 1,
            payloadDiscriminator: RawEventPayloadKind.NewTrade,
            payload: new NewTradePayload(2400.1m, 1m, 2400.1m, 1, "Buy", 2, "Trade", true, false, null, null, null),
            integrityFlags: RecorderIntegrityFlags.NativeSequenceAbsent | RecorderIntegrityFlags.SourceTimeKindUnspecified,
            nativeSequenceAvailable: false);
}
internal sealed class TempProfile : IDisposable
{
    public TempProfile()
    {
        Root = Path.Combine(Path.GetTempPath(), "gcae-p007b-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    public string Root { get; }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
        }
        catch
        {
            // best-effort
        }
    }
}

internal static class RecorderTestWait
{
    public static void Until(Func<bool> condition, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            if (condition()) return;
            Thread.Sleep(10);
        }

        Assert.True(condition(), "Condition not met within timeout");
    }
}
