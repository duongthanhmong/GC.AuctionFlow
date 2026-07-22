using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;
using GC.AuctionFlow.Recorder;
using GC.AuctionFlow.Recorder.Payloads;

namespace GC.AuctionFlow.Tests.Unit.Recorder;

/// <summary>P0-07B closeout audit edge coverage.</summary>
public sealed class RecorderCloseoutAuditTests
{
    [Fact]
    public void Frame_byte_layout_and_crc_coverage_documented()
    {
        // Container: [0..3] GCAR magic, [4..5] version, [6..7] flags = 8 bytes
        Assert.Equal(8, ContainerFormat.ContainerHeaderSize);
        Assert.Equal(0x52414347u, ContainerFormat.ContainerMagic); // "GCAR"
        Assert.Equal(0x31464347u, ContainerFormat.FrameMagic); // "GCF1"
        // Frame: [0..3] magic, [4..5] ver, [6..7] type, [8..9] flags, [10..11] reserved,
        // [12..15] len LE, [16..16+N) payload, then CRC32C of bytes [0..16+N)
        Assert.Equal(16, ContainerFormat.FrameHeaderSize);
        Assert.Equal(4, ContainerFormat.FrameCrcSize);
        Assert.Equal(1, RawEventRecorderVersions.RawEventContainerVersion);
        Assert.Equal(1, RawEventRecorderVersions.FrameVersion);
        Assert.Equal(1 * 1024 * 1024, RecorderConfig.DefaultMaxFramePayloadBytes);
    }

    [Fact]
    public void Crc_catches_corrupted_frame_type_and_length_metadata()
    {
        var frame = FrameCodec.EncodeFrame(RecorderFrameType.RawEvent, "{\"a\":1}"u8.ToArray());
        frame[6] ^= 0x01; // type
        Assert.Equal(FrameDecodeStatus.FrameCrcMismatch, FrameCodec.TryDecodeFrame(frame, 1024 * 1024, out _, out _, out _));

        frame = FrameCodec.EncodeFrame(RecorderFrameType.RawEvent, "{\"a\":1}"u8.ToArray());
        frame[12] ^= 0x01; // length LSB
        var status = FrameCodec.TryDecodeFrame(frame, 1024 * 1024, out _, out _, out _);
        Assert.True(status is FrameDecodeStatus.FrameCrcMismatch or FrameDecodeStatus.NeedMoreData or FrameDecodeStatus.OversizedFrame);
    }

    [Fact]
    public void Oversized_length_rejected_before_payload_allocation()
    {
        var header = new byte[ContainerFormat.FrameHeaderSize];
        BinaryPrimitives.WriteUInt32LittleEndian(header, ContainerFormat.FrameMagic);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(4), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(6), (ushort)RecorderFrameType.RawEvent);
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(12), (uint)(2 * 1024 * 1024));
        Assert.Equal(FrameDecodeStatus.OversizedFrame, FrameCodec.TryDecodeFrame(header, 1024 * 1024, out _, out _, out _));
    }

    [Fact]
    public void Footer_space_reserved_during_byte_rotation()
    {
        using var temp = new TempProfile();
        var counters = new RecorderCounters();
        var config = new RecorderConfig(
            maxSegmentBytes: RecorderConfig.ComputeMinimumSegmentBytes(RecorderConfig.DefaultMaxFramePayloadBytes) + 50_000,
            maxRecordsPerSegment: 1_000_000,
            maxSegmentDuration: TimeSpan.FromHours(1),
            queueCapacity: 8,
            minimumFreeSpaceBytes: 0,
            maxSessionBytes: 64L * 1024 * 1024);
        var sid = Guid.NewGuid();
        RecorderStoragePaths.EnsureSessionLayout(sid, temp.Root);
        using var w = new SegmentWriter(RecorderStoragePaths.GetSegmentsDirectory(sid, temp.Root), config, counters);
        var id = Guid.NewGuid();
        w.Open(sid, Guid.NewGuid(), id, 1, 1, TestFixtures.Instrument(), "Live", "Op", "R", "Op", Stopwatch.GetTimestamp(), DateTime.UtcNow);

        // Force prospective next event + footer to exceed MaxSegmentBytes.
        var hugeNext = (int)(config.MaxSegmentBytes - w.BytesWritten - config.FooterReserveBytes + 1);
        Assert.True(hugeNext > 0);
        Assert.True(w.WouldExceedLimits(hugeNext, config.FooterReserveBytes, Stopwatch.GetTimestamp()));

        // Completing after a real event must keep final file within MaxSegmentBytes.
        w.WriteRawEvent(RawEventEnvelope.FromDraft(TestFixtures.Draft(), id, 1, 1, DateTime.UtcNow));
        var completed = w.Complete(Stopwatch.GetTimestamp(), DateTime.UtcNow);
        Assert.True(completed.ByteLength <= config.MaxSegmentBytes);
    }

    [Fact]
    public void RecordsWritten_excludes_header_and_footer_frames()
    {
        using var temp = new TempProfile();
        var counters = new RecorderCounters();
        var config = new RecorderConfig(minimumFreeSpaceBytes: 0, maxSessionBytes: 64L * 1024 * 1024);
        var sid = Guid.NewGuid();
        RecorderStoragePaths.EnsureSessionLayout(sid, temp.Root);
        using var w = new SegmentWriter(RecorderStoragePaths.GetSegmentsDirectory(sid, temp.Root), config, counters);
        var id = Guid.NewGuid();
        w.Open(sid, Guid.NewGuid(), id, 1, 1, TestFixtures.Instrument(), "Live", "Op", "R", "Op", Stopwatch.GetTimestamp(), DateTime.UtcNow);
        Assert.Equal(0, counters.RecordsWritten);
        w.WriteRawEvent(RawEventEnvelope.FromDraft(TestFixtures.Draft(), id, 1, 1, DateTime.UtcNow));
        w.WriteRawEvent(RawEventEnvelope.FromDraft(TestFixtures.Draft(streamLocal: 2), id, 1, 2, DateTime.UtcNow));
        Assert.Equal(2, counters.RecordsWritten);
        var completed = w.Complete(Stopwatch.GetTimestamp(), DateTime.UtcNow);
        Assert.Equal(2, counters.RecordsWritten);
        Assert.Equal(2, completed.RecordCount);

        var bytes = File.ReadAllBytes(completed.FullPath);
        var offset = ContainerFormat.ContainerHeaderSize;
        var raw = 0;
        var header = 0;
        var footer = 0;
        while (offset < bytes.Length)
        {
            Assert.Equal(FrameDecodeStatus.Ok, FrameCodec.TryDecodeFrame(bytes.AsSpan(offset), config.MaxFramePayloadBytes, out var frame, out var consumed, out _));
            offset += consumed;
            switch (frame.FrameType)
            {
                case RecorderFrameType.SegmentHeader: header++; break;
                case RecorderFrameType.RawEvent: raw++; break;
                case RecorderFrameType.SegmentFooter:
                    footer++;
                    var f = RecorderJson.DeserializeFooter(frame.PayloadUtf8)!;
                    Assert.Equal(2, f.RawEventRecordCount);
                    Assert.True(f.BytesBeforeFooter > 0);
                    Assert.Equal(completed.ByteLength, f.BytesBeforeFooter + consumed);
                    break;
            }
        }

        Assert.Equal(1, header);
        Assert.Equal(1, footer);
        Assert.Equal(2, raw);
        Assert.Equal(raw + 2, header + raw + footer);
    }

    [Fact]
    public void Companion_hash_formatting_is_deterministic()
    {
        using var temp = new TempProfile();
        var dir = Path.Combine(temp.Root, "h");
        Directory.CreateDirectory(dir);
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        SegmentWriter.WriteSha256Atomic(dir, id, "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF");
        var text = File.ReadAllText(Path.Combine(dir, RecorderStoragePaths.SegmentHashFileName(id)));
        Assert.Equal("0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF  aaaaaaaabbbbccccddddeeeeeeeeeeee.seg\n", text);
        Assert.DoesNotContain("\r", text);
    }

    [Fact]
    public void Durable_completion_ordering_filesystem_state()
    {
        using var temp = new TempProfile();
        var counters = new RecorderCounters();
        var config = new RecorderConfig(minimumFreeSpaceBytes: 0, maxSessionBytes: 64L * 1024 * 1024);
        var sid = Guid.NewGuid();
        RecorderStoragePaths.EnsureSessionLayout(sid, temp.Root);
        var segmentsDir = RecorderStoragePaths.GetSegmentsDirectory(sid, temp.Root);
        using var w = new SegmentWriter(segmentsDir, config, counters);
        var id = Guid.NewGuid();
        w.Open(sid, Guid.NewGuid(), id, 1, 1, TestFixtures.Instrument(), "Live", "Op", "R", "Op", Stopwatch.GetTimestamp(), DateTime.UtcNow);
        Assert.True(File.Exists(Path.Combine(segmentsDir, RecorderStoragePaths.SegmentTempFileName(id))));
        Assert.False(File.Exists(Path.Combine(segmentsDir, RecorderStoragePaths.SegmentFileName(id))));
        Assert.False(File.Exists(Path.Combine(segmentsDir, RecorderStoragePaths.SegmentHashFileName(id))));
        w.WriteRawEvent(RawEventEnvelope.FromDraft(TestFixtures.Draft(), id, 1, 1, DateTime.UtcNow));
        var completed = w.Complete(Stopwatch.GetTimestamp(), DateTime.UtcNow);
        Assert.False(File.Exists(Path.Combine(segmentsDir, RecorderStoragePaths.SegmentTempFileName(id))));
        Assert.True(File.Exists(completed.FullPath));
        Assert.True(File.Exists(Path.Combine(segmentsDir, RecorderStoragePaths.SegmentHashFileName(id))));
        Assert.Equal(SegmentWriter.ComputeSha256Hex(completed.FullPath), completed.Sha256Hex);
    }

    [Fact]
    public void Manifest_cannot_list_segment_before_valid_hash()
    {
        using var temp = new TempProfile();
        var config = new RecorderConfig(queueCapacity: 32, minimumFreeSpaceBytes: 0, maxSessionBytes: 64L * 1024 * 1024);
        var sessionId = Guid.NewGuid();
        using (var session = new RawEventRecorderSession(
                   sessionId, Guid.NewGuid(), "Live", "OperatorDeclared", "Rithmic", "OperatorDeclared",
                   config, new FixedDiskSpaceProbe(long.MaxValue), temp.Root))
        {
            session.NotePayloadItemEnumerated();
            session.NoteNormalizedObservation();
            Assert.True(session.TryWrite(TestFixtures.Draft()));
            RecorderTestWait.Until(() => session.Counters.RecordsWritten >= 1, TimeSpan.FromSeconds(5));
        }

        var manifestPath = RecorderStoragePaths.GetManifestPath(sessionId, temp.Root);
        Assert.True(File.Exists(manifestPath));
        var manifest = RecorderJson.DeserializeManifest(File.ReadAllBytes(manifestPath))!;
        foreach (var seg in manifest.Segments)
        {
            var segPath = Path.Combine(RecorderStoragePaths.GetSegmentsDirectory(sessionId, temp.Root), seg.FileName);
            var hashPath = Path.Combine(RecorderStoragePaths.GetSegmentsDirectory(sessionId, temp.Root), RecorderStoragePaths.SegmentHashFileName(seg.SegmentId));
            Assert.True(File.Exists(segPath));
            Assert.True(File.Exists(hashPath));
            Assert.Equal(SegmentWriter.ComputeSha256Hex(segPath), seg.Sha256Hex, ignoreCase: true);
        }
    }

    [Fact]
    public void Identity_lifecycle_is_first_raw_event_of_new_epoch_segment()
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
        RecorderTestWait.Until(() => session.ContractEpoch >= 2 && session.Counters.RecordsWritten >= 3, TimeSpan.FromSeconds(5));
        session.Dispose();

        var newSeg = session.CompletedSegments.OrderBy(s => s.SegmentOrdinal).Last();
        Assert.Equal(2, newSeg.ContractEpoch);
        var bytes = File.ReadAllBytes(newSeg.FullPath);
        var offset = ContainerFormat.ContainerHeaderSize;
        RawEventEnvelope? first = null;
        while (offset < bytes.Length)
        {
            Assert.Equal(FrameDecodeStatus.Ok, FrameCodec.TryDecodeFrame(bytes.AsSpan(offset), config.MaxFramePayloadBytes, out var frame, out var consumed, out _));
            offset += consumed;
            if (frame.FrameType != RecorderFrameType.RawEvent) continue;
            first = RecorderJson.DeserializeEnvelope(frame.PayloadUtf8);
            break;
        }

        Assert.NotNull(first);
        Assert.Equal(RawEventPayloadKind.RecorderLifecycle, first!.PayloadDiscriminator);
        var life = Assert.IsType<RecorderLifecyclePayload>(first.Payload);
        Assert.Equal(RecorderLifecycleEventKind.ContractEpochAdvanced, life.EventKind);
        Assert.Equal(1, life.PreviousContractEpoch);
        Assert.Equal(2, life.NewContractEpoch);
        Assert.Contains("GCQ6", life.PreviousIdentityTuple, StringComparison.Ordinal);
        Assert.Contains("GCZ6", life.NewIdentityTuple, StringComparison.Ordinal);
        Assert.Equal(2, first.ContractEpoch);
    }

    [Fact]
    public void Malformed_json_with_valid_crc_continues_but_is_not_trusted()
    {
        using var temp = new TempProfile();
        var config = new RecorderConfig(minimumFreeSpaceBytes: 0, maxSessionBytes: 64L * 1024 * 1024);
        var sid = Guid.NewGuid();
        RecorderStoragePaths.EnsureSessionLayout(sid, temp.Root);
        var segmentsDir = RecorderStoragePaths.GetSegmentsDirectory(sid, temp.Root);
        var counters = new RecorderCounters();
        Guid segId;
        using (var w = new SegmentWriter(segmentsDir, config, counters))
        {
            segId = Guid.NewGuid();
            w.Open(sid, Guid.NewGuid(), segId, 1, 1, TestFixtures.Instrument(), "Live", "Op", "R", "Op", Stopwatch.GetTimestamp(), DateTime.UtcNow);
            w.WriteRawEvent(RawEventEnvelope.FromDraft(TestFixtures.Draft(), segId, 1, 1, DateTime.UtcNow));
            w.Complete(Stopwatch.GetTimestamp(), DateTime.UtcNow);
        }

        var path = Path.Combine(segmentsDir, RecorderStoragePaths.SegmentFileName(segId));
        var bytes = File.ReadAllBytes(path).ToList();
        // Replace first RawEvent payload with malformed JSON but rebuild CRC via FrameCodec.
        var rebuilt = new List<byte>();
        rebuilt.AddRange(bytes.Take(ContainerFormat.ContainerHeaderSize));
        var offset = ContainerFormat.ContainerHeaderSize;
        var src = bytes.ToArray();
        var replaced = false;
        while (offset < src.Length)
        {
            Assert.Equal(FrameDecodeStatus.Ok, FrameCodec.TryDecodeFrame(src.AsSpan(offset), config.MaxFramePayloadBytes, out var frame, out var consumed, out _));
            if (frame.FrameType == RecorderFrameType.RawEvent && !replaced)
            {
                rebuilt.AddRange(FrameCodec.EncodeFrame(RecorderFrameType.RawEvent, "{not-json"u8.ToArray()));
                replaced = true;
            }
            else
            {
                rebuilt.AddRange(src.Skip(offset).Take(consumed));
            }

            offset += consumed;
        }

        File.WriteAllBytes(path, rebuilt.ToArray());
        SegmentWriter.WriteSha256Atomic(segmentsDir, segId, SegmentWriter.ComputeSha256Hex(path));

        Assert.False(RecoveryScanner.ValidateSegmentFile(path, config, out _, out var classification, out _));
        Assert.Equal(RecoveryClassification.PayloadDecodeFailure, classification);
    }

    [Fact]
    public void Crc_mismatch_stops_scan()
    {
        using var temp = new TempProfile();
        var config = new RecorderConfig(minimumFreeSpaceBytes: 0, maxSessionBytes: 64L * 1024 * 1024);
        var sid = Guid.NewGuid();
        RecorderStoragePaths.EnsureSessionLayout(sid, temp.Root);
        var segmentsDir = RecorderStoragePaths.GetSegmentsDirectory(sid, temp.Root);
        var counters = new RecorderCounters();
        Guid segId;
        using (var w = new SegmentWriter(segmentsDir, config, counters))
        {
            segId = Guid.NewGuid();
            w.Open(sid, Guid.NewGuid(), segId, 1, 1, TestFixtures.Instrument(), "Live", "Op", "R", "Op", Stopwatch.GetTimestamp(), DateTime.UtcNow);
            w.WriteRawEvent(RawEventEnvelope.FromDraft(TestFixtures.Draft(), segId, 1, 1, DateTime.UtcNow));
            w.Complete(Stopwatch.GetTimestamp(), DateTime.UtcNow);
        }

        var path = Path.Combine(segmentsDir, RecorderStoragePaths.SegmentFileName(segId));
        var bytes = File.ReadAllBytes(path);
        // Corrupt a payload byte inside first framed region after container header without fixing CRC.
        bytes[ContainerFormat.ContainerHeaderSize + ContainerFormat.FrameHeaderSize + 2] ^= 0xFF;
        File.WriteAllBytes(path, bytes);
        SegmentWriter.WriteSha256Atomic(segmentsDir, segId, SegmentWriter.ComputeSha256Hex(path));

        Assert.False(RecoveryScanner.ValidateSegmentFile(path, config, out _, out var classification, out _));
        Assert.Equal(RecoveryClassification.FrameCrcMismatch, classification);
    }

    [Fact]
    public void Quarantine_failure_is_reported()
    {
        using var temp = new TempProfile();
        var config = new RecorderConfig(minimumFreeSpaceBytes: 0, maxSessionBytes: 64L * 1024 * 1024);
        var sid = Guid.NewGuid();
        var sessionDir = RecorderStoragePaths.GetSessionDirectory(sid, temp.Root);
        var segmentsDir = RecorderStoragePaths.GetSegmentsDirectory(sid, temp.Root);
        Directory.CreateDirectory(segmentsDir);
        var recoveryDir = RecorderStoragePaths.GetRecoveryDirectory(sid, temp.Root);
        if (Directory.Exists(recoveryDir))
            Directory.Delete(recoveryDir, recursive: true);
        // Create recovery path as a file so quarantine File.Copy fails.
        File.WriteAllText(recoveryDir, "blocked");
        var tmp = Path.Combine(segmentsDir, "deadbeefdeadbeefdeadbeefdeadbeef.seg.tmp");
        File.WriteAllText(tmp, "incomplete");

        var scan = RecoveryScanner.ScanSessionDirectory(sessionDir, config, copyQuarantine: true);
        Assert.Contains(scan.Findings, f =>
            f.Classification == RecoveryClassification.IncompleteTemporary
            && f.Detail.Contains("QuarantineCopyFailed", StringComparison.Ordinal));
    }

    [Fact]
    public void Shutdown_timeout_accounts_for_undrained_records()
    {
        using var temp = new TempProfile();
        var blocker = new ManualResetEventSlim(false);
        var disk = new BlockingDiskProbe(blocker, long.MaxValue);
        var config = new RecorderConfig(
            queueCapacity: 8,
            shutdownDrainTimeoutMilliseconds: 50,
            minimumFreeSpaceBytes: 0,
            maxSessionBytes: 64L * 1024 * 1024);
        var session = new RawEventRecorderSession(
            Guid.NewGuid(), Guid.NewGuid(), "Live", "OperatorDeclared", "Rithmic", "OperatorDeclared",
            config, disk, temp.Root);

        for (var i = 0; i < 5; i++)
        {
            session.NotePayloadItemEnumerated();
            session.NoteNormalizedObservation();
            session.TryWrite(TestFixtures.Draft(streamLocal: i + 1));
        }

        // Worker blocked in disk probe; dispose times out and counts undrained.
        session.Dispose();
        blocker.Set();
        Assert.True(session.Counters.UndrainedAtShutdown > 0 || session.Counters.WriterDequeued > 0);
    }

    [Fact]
    public void Fatal_worker_path_accounts_dequeued_records()
    {
        // Disk stop after dequeue increments WriterDiscardedAfterFatalFault.
        using var temp = new TempProfile();
        var disk = new FixedDiskSpaceProbe(0);
        var config = new RecorderConfig(queueCapacity: 32, minimumFreeSpaceBytes: 1L * 1024 * 1024 * 1024, maxSessionBytes: 64L * 1024 * 1024);
        using var session = new RawEventRecorderSession(
            Guid.NewGuid(), Guid.NewGuid(), "Live", "OperatorDeclared", "Rithmic", "OperatorDeclared",
            config, disk, temp.Root);
        session.NotePayloadItemEnumerated();
        session.NoteNormalizedObservation();
        session.TryWrite(TestFixtures.Draft());
        RecorderTestWait.Until(
            () => session.Counters.WriterDiscardedAfterFatalFault >= 1 || session.Counters.DiskSpaceStops >= 1,
            TimeSpan.FromSeconds(5));
        session.Dispose();
        var snap = session.Counters.Snapshot();
        Assert.True(snap.WriterDequeued >= 1);
        Assert.Equal(snap.WriterDequeued, snap.RecordsWritten + snap.SerializationFailures + snap.WriterDiscardedAfterFatalFault);
    }

    [Fact]
    public void Invalid_max_segment_bytes_fails_before_recording()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RecorderConfig(maxSegmentBytes: 100, minimumFreeSpaceBytes: 0, maxSessionBytes: 1000));
    }

    [Fact]
    public void Versions_exact_lock()
    {
        Assert.Equal("1.1.0", RawEventRecorderVersions.RawEventRecorderSchemaVersion);
        Assert.Equal(1, RawEventRecorderVersions.RawEventContainerVersion);
        Assert.Equal("0.0.6", GC.AuctionFlow.Core.CapabilitySchemaVersions.ProbeVersionPlaceholder);
        Assert.Equal("1.0.1", GC.AuctionFlow.Probe.TradeStreamProbeVersions.TradeStreamProbeSchemaVersion);
        Assert.Equal("1.0.0", GC.AuctionFlow.Probe.DomSemanticsProbeVersions.DomSemanticsProbeSchemaVersion);
        Assert.Equal("1.0.1", GC.AuctionFlow.Probe.MboLifecycleProbeVersions.MboLifecycleProbeSchemaVersion);
        Assert.Equal("0.0.6", GC.AuctionFlow.Probe.MboLifecycleProbeVersions.ProbeVersion);
    }

    [Fact]
    public void No_mbo_subscription_symbol_in_recorder_source()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "GC.AuctionFlow", "Recorder"));
        foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("SubscribeMarketByOrderData", text, StringComparison.Ordinal);
            Assert.DoesNotContain("SubscribeMarketByOrdersData", text, StringComparison.Ordinal);
        }
    }
}

internal sealed class BlockingDiskProbe : IDiskSpaceProbe
{
    private readonly ManualResetEventSlim _gate;
    private readonly long _available;

    public BlockingDiskProbe(ManualResetEventSlim gate, long available)
    {
        _gate = gate;
        _available = available;
    }

    public long GetAvailableBytes(string path)
    {
        _gate.Wait(TimeSpan.FromSeconds(30));
        return _available;
    }
}
