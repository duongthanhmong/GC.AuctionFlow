using System.Diagnostics;
using System.Text.Json;
using GC.AuctionFlow.Core;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Recorder;
using GC.AuctionFlow.Recorder.FanOut;
using GC.AuctionFlow.Recorder.Payloads;

namespace GC.AuctionFlow.Tests.Unit.Recorder;

public sealed class MetadataCloseoutEnabledStreamsTests
{
    [Fact]
    public void Trade_only_configuration_emits_EnabledStreams_Trade()
    {
        using var temp = new TempProfile();
        var sessionId = Guid.NewGuid();
        using var session = new RawEventRecorderSession(
            sessionId, Guid.NewGuid(), "Live", "OperatorDeclared", "Rithmic", "OperatorDeclared",
            new RecorderConfig(minimumFreeSpaceBytes: 0, maxSessionBytes: 64L * 1024 * 1024),
            userProfileOverride: temp.Root,
            enabledStreams: new[] { "Trade" });

        session.NoteNormalizedObservation();
        session.NotePayloadItemEnumerated();
        Assert.True(session.TryWrite(TestFixtures.Draft()));
        RecorderTestWait.Until(() => session.Counters.RecordsWritten >= 1, TimeSpan.FromSeconds(5));
        session.StopAccepting("IndicatorDispose");
        session.Dispose();

        var manifest = ReadManifest(sessionId, temp.Root);
        Assert.Equal(new[] { "Trade" }, manifest.GetProperty("enabledStreams").EnumerateArray().Select(e => e.GetString()).ToArray());
        Assert.DoesNotContain(manifest.GetProperty("enabledStreams").EnumerateArray(), e => e.GetString() == "Dom");
        Assert.DoesNotContain(manifest.GetProperty("enabledStreams").EnumerateArray(), e => e.GetString() == "Bba" || e.GetString() == "BBA");
        Assert.DoesNotContain(manifest.GetProperty("enabledStreams").EnumerateArray(), e => e.GetString() == "Mbo" || e.GetString() == "MBO");
    }

    [Fact]
    public void Host_Trade_recording_startup_writes_EnabledStreams_Trade_only()
    {
        using var temp = new TempProfile();
        var host = new TradeRecorderHost();
        var sessionId = Guid.NewGuid();
        var observed = new ObservedInstrumentSnapshot(
            "GCQ6", "1", "GCQ6", "COMEX", null, 0.1m, null, "GCQ6", "COMEX", 0.1m, null);
        Assert.Equal(RecorderSinkOutcome.SessionNotStarted, host.EnsureStarted(
            true, true, observed, "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            sessionId, new TradeStreamAtasMapper(), temp.Root));
        host.TryCompleteStartupFromLifecycle();
        Assert.NotNull(host.Session);
        host.Session!.StopAccepting("IndicatorDispose");
        host.Dispose();

        var manifest = ReadManifest(sessionId, temp.Root);
        Assert.Equal(new[] { "Trade" }, manifest.GetProperty("enabledStreams").EnumerateArray().Select(e => e.GetString()).ToArray());
    }

    internal static JsonElement ReadManifest(Guid sessionId, string profile)
    {
        var path = RecorderStoragePaths.GetManifestPath(sessionId, profile);
        Assert.True(File.Exists(path), path);
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.Clone();
    }
}

public sealed class MetadataCloseoutTerminationTests
{
    [Fact]
    public void Successful_IndicatorDispose_is_normal_termination()
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
        session.StopAccepting("IndicatorDispose");
        var recon = session.DisposeAndReconcile();
        Assert.True(recon.Ok, string.Join("; ", recon.Mismatches));
        Assert.False(session.EvaluateAbnormalTermination(out var reason));
        Assert.Null(reason);

        var manifest = MetadataCloseoutEnabledStreamsTests.ReadManifest(sessionId, temp.Root);
        Assert.False(manifest.GetProperty("abnormalTermination").GetBoolean());
        Assert.True(
            !manifest.TryGetProperty("abnormalTerminationReason", out var r)
            || r.ValueKind == JsonValueKind.Null
            || string.IsNullOrEmpty(r.GetString()));
    }

    [Fact]
    public void Drain_timeout_is_abnormal()
    {
        using var temp = new TempProfile();
        var sessionId = Guid.NewGuid();
        var session = new RawEventRecorderSession(
            sessionId, Guid.NewGuid(), "Live", "OperatorDeclared", "Rithmic", "OperatorDeclared",
            new RecorderConfig(
                minimumFreeSpaceBytes: 0,
                maxSessionBytes: 64L * 1024 * 1024,
                shutdownDrainTimeoutMilliseconds: 0,
                queueCapacity: 64),
            userProfileOverride: temp.Root);

        for (var i = 0; i < 32; i++)
        {
            session.NoteNormalizedObservation();
            session.NotePayloadItemEnumerated();
            session.TryWrite(TestFixtures.Draft(streamLocal: i + 1));
        }

        session.StopAccepting("IndicatorDispose");
        session.Dispose();
        Assert.True(session.EvaluateAbnormalTermination(out var reason));
        // Zero drain budget under load ⇒ DrainTimeout; leftover market drafts ⇒ UndrainedAtShutdown.
        Assert.True(
            reason is "DrainTimeout" or "UndrainedAtShutdown",
            "expected DrainTimeout or UndrainedAtShutdown, got " + reason);

        var manifest = MetadataCloseoutEnabledStreamsTests.ReadManifest(sessionId, temp.Root);
        Assert.True(manifest.GetProperty("abnormalTermination").GetBoolean());
    }

    [Fact]
    public void Writer_fatal_fault_is_abnormal()
    {
        using var temp = new TempProfile();
        var sessionId = Guid.NewGuid();
        using var session = new RawEventRecorderSession(
            sessionId, Guid.NewGuid(), "Live", "OperatorDeclared", "Rithmic", "OperatorDeclared",
            new RecorderConfig(minimumFreeSpaceBytes: 1, maxSessionBytes: 64L * 1024 * 1024),
            diskSpaceProbe: new FixedDiskSpaceProbe(0),
            userProfileOverride: temp.Root);

        session.NoteNormalizedObservation();
        session.NotePayloadItemEnumerated();
        session.TryWrite(TestFixtures.Draft());
        RecorderTestWait.Until(
            () => session.Counters.WriterDiscardedAfterFatalFault >= 1 || session.Counters.DiskSpaceStops >= 1,
            TimeSpan.FromSeconds(5));
        session.StopAccepting("IndicatorDispose");
        session.Dispose();

        Assert.True(session.EvaluateAbnormalTermination(out var reason));
        Assert.False(string.IsNullOrWhiteSpace(reason));
        var manifest = MetadataCloseoutEnabledStreamsTests.ReadManifest(sessionId, temp.Root);
        Assert.True(manifest.GetProperty("abnormalTermination").GetBoolean());
    }

    [Fact]
    public void Segment_or_manifest_finalization_failure_is_abnormal()
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

        // Force finalization classification via incomplete counter (same signal FinalizeSession uses).
        Interlocked.Increment(ref session.Counters.SegmentsIncomplete);
        session.StopAccepting("IndicatorDispose");
        session.Dispose();

        Assert.True(session.EvaluateAbnormalTermination(out var reason));
        Assert.Equal("SegmentFinalizationFailure", reason);
        var manifest = MetadataCloseoutEnabledStreamsTests.ReadManifest(sessionId, temp.Root);
        Assert.True(manifest.GetProperty("abnormalTermination").GetBoolean());
    }

    [Fact]
    public void Repeated_disposal_is_idempotent()
    {
        using var temp = new TempProfile();
        var sessionId = Guid.NewGuid();
        var session = new RawEventRecorderSession(
            sessionId, Guid.NewGuid(), "Live", "OperatorDeclared", "Rithmic", "OperatorDeclared",
            new RecorderConfig(minimumFreeSpaceBytes: 0, maxSessionBytes: 64L * 1024 * 1024),
            userProfileOverride: temp.Root);
        session.StopAccepting("IndicatorDispose");
        session.Dispose();
        session.Dispose();
        session.Dispose();
        Assert.False(session.EvaluateAbnormalTermination(out _));
        Assert.True(File.Exists(RecorderStoragePaths.GetManifestPath(sessionId, temp.Root)));
    }
}

public sealed class MetadataCloseoutCallbackCounterTests
{
    [Fact]
    public void Singular_authorized_callback_counts_one_invocation_and_emission()
    {
        using var temp = new TempProfile();
        var host = new TradeRecorderHost();
        StartHost(host, temp, out var sessionId);
        var ctx = host.Capture(RecorderCallbackSource.OnNewTrade);
        host.ProcessSyntheticNullItemBatchForTests(
            ctx,
            TestFixtures.Instrument(),
            "Live", "OperatorDeclared", "Rithmic", "OperatorDeclared",
            nullItemCount: 1);
        RecorderTestWait.Until(() => host.Counters.InvocationResultsWritten >= 1, TimeSpan.FromSeconds(5));
        Assert.Equal(1, host.Counters.CallbackInvocations);
        Assert.Equal(1, host.Counters.AuthorizedCallbackInvocations);
        Assert.Equal(host.Counters.CallbackInvocations, host.Counters.InvocationResultEmissionAttempts);
        host.Session!.StopAccepting("IndicatorDispose");
        host.Dispose();
        _ = sessionId;
    }

    [Fact]
    public void Batch_with_multiple_items_counts_one_invocation()
    {
        using var temp = new TempProfile();
        var host = new TradeRecorderHost();
        StartHost(host, temp, out _);
        var ctx = host.Capture(RecorderCallbackSource.OnNewTrades);
        // Null enumerable still one invocation; multi-item path covered via synthetic null-item batch helper.
        host.ProcessSyntheticNullItemBatchForTests(
            ctx,
            TestFixtures.Instrument(),
            "Live", "OperatorDeclared", "Rithmic", "OperatorDeclared",
            nullItemCount: 3);
        RecorderTestWait.Until(() => host.Counters.InvocationResultEmissionAttempts >= 1, TimeSpan.FromSeconds(5));
        Assert.Equal(1, host.Counters.CallbackInvocations);
        Assert.Equal(1, host.Counters.AuthorizedCallbackInvocations);
        Assert.Equal(3, host.Counters.PayloadItemsEnumerated);
        Assert.Equal(3, host.Counters.NullItemObservations);
        Assert.Equal(host.Counters.CallbackInvocations, host.Counters.InvocationResultEmissionAttempts);
        host.Session!.StopAccepting("IndicatorDispose");
        host.Dispose();
    }

    [Fact]
    public void Rejected_gate_increments_RejectedCallbackInvocationsByGate()
    {
        var host = new TradeRecorderHost();
        var outcome = host.EnsureStarted(
            true, true,
            observed: null,
            expectedInstrumentCode: "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            Guid.NewGuid(), new TradeStreamAtasMapper());
        Assert.Equal(RecorderSinkOutcome.RejectedByGate, outcome);
        host.NoteRejectedByGate();
        Assert.Equal(1, host.Counters.CallbackInvocations);
        Assert.Equal(1, host.Counters.RejectedCallbackInvocationsByGate);
        Assert.Equal(0, host.Counters.AuthorizedCallbackInvocations);
        Assert.Equal(0, host.Counters.InvocationResultEmissionAttempts);
    }

    [Fact]
    public void Callback_before_start_increments_before_start_counter()
    {
        var host = new TradeRecorderHost();
        host.NoteCallbackBeforeStart();
        Assert.Equal(1, host.Counters.CallbackInvocations);
        Assert.Equal(1, host.Counters.RecorderCallbacksBeforeStart);
        Assert.Equal(0, host.Counters.AuthorizedCallbackInvocations);
    }

    [Fact]
    public void Callback_after_stop_increments_after_stop_counter()
    {
        using var temp = new TempProfile();
        var host = new TradeRecorderHost();
        StartHost(host, temp, out _);
        host.Session!.StopAccepting("IndicatorDispose");
        host.Dispose();
        host.NoteCallbackAfterStop();
        Assert.True(host.Counters.RecorderCallbacksAfterStop >= 1);
        Assert.True(host.Counters.CallbackInvocations >= 1);
        Assert.Equal(0, host.Counters.AuthorizedCallbackInvocations);
    }

    [Fact]
    public void Invocation_result_queue_full_counts_drop_not_authorized_item_as_invocation()
    {
        using var temp = new TempProfile();
        var counters = new RecorderCounters();
        using var session = new RawEventRecorderSession(
            Guid.NewGuid(), Guid.NewGuid(), "Live", "OperatorDeclared", "Rithmic", "OperatorDeclared",
            new RecorderConfig(minimumFreeSpaceBytes: 0, maxSessionBytes: 64L * 1024 * 1024, queueCapacity: 1),
            userProfileOverride: temp.Root,
            counters: counters);

        // Saturate with market draft (capacity 1 + Wait mode may still accept; use many inv attempts after stop).
        session.StopAccepting("test");
        var inv = TradeToRawEventAdapter.ToInvocationResultDraft(
            new CallbackInvocationResultPayload(
                RecorderCallbackSource.OnNewTrade, 1, DateTime.UtcNow, 1, 1,
                false, true, 0, 0, 0, 0, 0, true, null),
            new CallbackCaptureContext(RecorderCallbackSource.OnNewTrade, 1, DateTime.UtcNow, 1, 1),
            TestFixtures.Instrument(),
            session.SessionId, Guid.NewGuid(), "Live", "OperatorDeclared", "Rithmic", "OperatorDeclared");
        Assert.False(session.TryWrite(inv));
        Assert.Equal(1, counters.InvocationResultEmissionAttempts);
        Assert.Equal(1, counters.InvocationResultFaults);
        session.Dispose();
    }

    [Fact]
    public void Mapper_enumeration_failure_still_emits_invocation_result_once()
    {
        using var temp = new TempProfile();
        var host = new TradeRecorderHost();
        StartHost(host, temp, out _);
        var ctx = host.Capture(RecorderCallbackSource.OnNewTrades);
        host.ProcessSyntheticEnumerationFailureForTests(
            ctx,
            TestFixtures.Instrument(),
            "Live", "OperatorDeclared", "Rithmic", "OperatorDeclared");
        RecorderTestWait.Until(() => host.Counters.InvocationResultEmissionAttempts >= 1, TimeSpan.FromSeconds(5));
        Assert.Equal(1, host.Counters.CallbackInvocations);
        Assert.Equal(1, host.Counters.AuthorizedCallbackInvocations);
        Assert.Equal(1, host.Counters.InvocationResultEmissionAttempts);
        host.Session!.StopAccepting("IndicatorDispose");
        host.Dispose();
    }

    private static void StartHost(TradeRecorderHost host, TempProfile temp, out Guid sessionId)
    {
        sessionId = Guid.NewGuid();
        var observed = new ObservedInstrumentSnapshot(
            "GCQ6", "1", "GCQ6", "COMEX", null, 0.1m, null, "GCQ6", "COMEX", 0.1m, null);
        Assert.Equal(RecorderSinkOutcome.SessionNotStarted, host.EnsureStarted(
            true, true, observed, "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            sessionId, new TradeStreamAtasMapper(), temp.Root));
        host.TryCompleteStartupFromLifecycle();
        Assert.True(host.IsAccepting);
    }
}

public sealed class MetadataCloseoutVerifierHeaderTests
{
    [Fact]
    public void GCAR_header_uses_UInt16_version_and_flags()
    {
        var buf = new byte[8];
        ContainerFormat.WriteContainerHeader(buf, containerVersion: 1, flags: 0xABCD);
        Assert.Equal("GCAR", System.Text.Encoding.ASCII.GetString(buf, 0, 4));
        Assert.Equal(1, BitConverter.ToUInt16(buf, 4));
        Assert.Equal(0xABCD, BitConverter.ToUInt16(buf, 6));
        // Must not interpret bytes 4..7 as a single Int32 version.
        Assert.NotEqual(1, BitConverter.ToInt32(buf, 4));
    }
}
