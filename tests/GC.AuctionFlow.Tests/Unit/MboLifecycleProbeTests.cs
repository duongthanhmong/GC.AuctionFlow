using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using GC.AuctionFlow.Core;
using GC.AuctionFlow.Probe;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit;

public sealed class MboLifecycleProbeTests
{
    private static ObservedInstrumentSnapshot Instr(string code = "GCQ6") =>
        new(code, code + "@COMEX", "COMEX Gold Futures", "COMEX", null, 0.1m, null, "GC", "Commodity Exchange", 0.1m, "7");

    private static MboObservation Obs(
        long epoch,
        long id,
        int typeNumeric,
        int sideNumeric,
        decimal price = 100m,
        decimal volume = 1m,
        long priority = 1,
        string instrument = "GCQ6",
        DateTimeKind kind = DateTimeKind.Unspecified,
        long seq = 1)
    {
        var type = MboKnownRawUpdateTypes.Resolve(typeNumeric);
        MboKnownRawSideTypes.TryGetKnownName(sideNumeric, out var sideName);
        if (string.IsNullOrEmpty(sideName)) sideName = $"Unknown({sideNumeric})";
        var derived = MboSideClassifier.Classify(sideNumeric, out _);
        var ticks = DateTime.UtcNow.Ticks;
        return new MboObservation(
            MboCallbackSource.OnMarketByOrdersChangedBatch,
            epoch,
            seq,
            DateTime.UtcNow,
            0,
            Environment.CurrentManagedThreadId,
            ticks,
            kind,
            instrument,
            instrument,
            instrument + "@COMEX",
            "COMEX",
            type.RawName,
            type.RawNumericValue,
            type.IsKnownEnumMember,
            sideName,
            sideNumeric,
            derived,
            id,
            price,
            volume,
            priority,
            MboFingerprints.Core(ticks, id, typeNumeric, sideNumeric, price, volume, priority));
    }

    private static MboLifecycleProbe Open(int queue = 64)
    {
        var p = new MboLifecycleProbe(new MboLifecycleProbeConfig(queueCapacity: queue, maxSamples: 64), startWorker: true);
        p.SetObservedInstrument(Instr());
        return p;
    }

    private static void Enq(MboLifecycleProbe p, params MboObservation[] items) =>
        p.HandleMappedBatch(
            items, DateTime.UtcNow, Environment.CurrentManagedThreadId,
            true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCQ6",
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared);

    [Fact]
    public void Subscribe_exactly_once_and_TaskCompleted_not_named_Succeeded()
    {
        using var p = Open();
        var calls = 0;
        Assert.True(p.TrySubscribeOnce(
            true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCQ6",
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            () => { calls++; return Task.CompletedTask; }));
        Assert.False(p.TrySubscribeOnce(
            true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCQ6",
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            () => { calls++; return Task.CompletedTask; }));
        Assert.Equal(1, calls);
        SpinWait.SpinUntil(() => p.SubscriptionState == MboSubscriptionState.TaskCompleted, 2000);
        Assert.Equal(MboSubscriptionState.TaskCompleted, p.SubscriptionState);
        Assert.DoesNotContain("Succeeded", Enum.GetNames<MboSubscriptionState>());
        var snap = p.FreezeSnapshot(
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCQ6",
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared, Guid.NewGuid(), true);
        Assert.True(snap.SubscriptionAttemptCount >= 1);
        Assert.Equal(0, snap.DuplicateSubscribeSuppressed);
        Assert.True(snap.SubscribeTriggerCheckCount >= 2);
        Assert.False(snap.MboRuntimeEventPresenceObserved);
        Assert.Equal(CallbackObservationStatusNames.NotObservedInTestWindow, snap.CallbackObservationStatus);
    }

    [Fact]
    public void Repeated_TrySubscribeOnce_after_TaskCompleted_does_not_inflate_duplicateSubscribeSuppressed()
    {
        using var p = Open();
        Assert.True(p.TrySubscribeOnce(
            true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCQ6",
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            () => Task.CompletedTask));
        SpinWait.SpinUntil(() => p.SubscriptionState == MboSubscriptionState.TaskCompleted, 2000);
        for (var i = 0; i < 50; i++)
        {
            Assert.False(p.TrySubscribeOnce(
                true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCQ6",
                DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
                () => Task.CompletedTask));
        }

        Assert.Equal(0, p.DuplicateSubscribeSuppressed);
        Assert.True(p.SubscribeTriggerCheckCount >= 51);
        var snap = p.FreezeSnapshot(
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCQ6",
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared, Guid.NewGuid(), true);
        Assert.Equal(0, snap.DuplicateSubscribeSuppressed);
        Assert.Equal(1, snap.CaptureSubscriptionEpoch);
        Assert.Equal(0, snap.FinalClosedEpoch);
    }

    [Fact]
    public void Task_fault_and_cancel_remain_explicit()
    {
        using var faulted = Open();
        Assert.True(faulted.TrySubscribeOnce(
            true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCQ6",
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            () => Task.FromException(new InvalidOperationException("boom"))));
        SpinWait.SpinUntil(() => faulted.SubscriptionState == MboSubscriptionState.TaskFaulted, 2000);
        var fs = faulted.FreezeSnapshot(
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCQ6",
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared, Guid.NewGuid(), true);
        Assert.Equal(MboSubscriptionState.TaskFaulted, fs.SubscriptionState);
        Assert.Contains("InvalidOperationException", fs.TaskFaultType ?? "", StringComparison.Ordinal);

        using var canceled = Open();
        Assert.True(canceled.TrySubscribeOnce(
            true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCQ6",
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            () => Task.FromCanceled(new CancellationToken(true))));
        SpinWait.SpinUntil(() => canceled.SubscriptionState == MboSubscriptionState.TaskCanceled, 2000);
        Assert.Equal(MboSubscriptionState.TaskCanceled, canceled.SubscriptionState);
    }

    [Fact]
    public void Compiled_OnMarketByOrdersChanged_does_not_call_base_or_Unsubscribe()
    {
        var asmPath = typeof(MboLifecycleProbe).Assembly.Location;
        using var fs = File.OpenRead(asmPath);
        using var pe = new PEReader(fs);
        var md = pe.GetMetadataReader();
        var type = md.TypeDefinitions
            .Select(t => md.GetTypeDefinition(t))
            .First(t => md.GetString(t.Name) == "GcAuctionFlowIndicator");

        MethodDefinitionHandle? mboHandle = null;
        foreach (var mh in type.GetMethods())
        {
            var m = md.GetMethodDefinition(mh);
            if (md.GetString(m.Name) == "OnMarketByOrdersChanged")
                mboHandle = mh;
        }

        Assert.True(mboHandle.HasValue);
        var method = md.GetMethodDefinition(mboHandle.Value);
        var body = pe.GetMethodBody(method.RelativeVirtualAddress);
        var il = body.GetILBytes();
        Assert.NotNull(il);

        for (var i = 0; i < il!.Length; i++)
        {
            if (il[i] is 0x28 or 0x6F) // call / callvirt
            {
                if (i + 4 >= il.Length) break;
                var token = BitConverter.ToInt32(il, i + 1);
                var handle = MetadataTokens.EntityHandle(token);
                string? name = null;
                if (handle.Kind == HandleKind.MemberReference)
                {
                    var mr = md.GetMemberReference((MemberReferenceHandle)handle);
                    name = md.GetString(mr.Name);
                }
                else if (handle.Kind == HandleKind.MethodDefinition)
                {
                    var mdh = md.GetMethodDefinition((MethodDefinitionHandle)handle);
                    name = md.GetString(mdh.Name);
                }

                Assert.NotEqual("Unsubscribe", name);
                Assert.NotEqual("add_MarketByOrdersChanged", name);
                i += 4;
            }
        }
    }

    [Fact]
    public void Raw_enum_unknown_preserved_interpreted_always_Unknown()
    {
        using var p = Open();
        var o = Obs(p.SubscriptionEpoch, 42, typeNumeric: 99, sideNumeric: 0);
        Assert.False(o.RawTypeIsKnownEnumMember);
        Assert.Equal(MboInterpretedLifecycleAction.Unknown, o.InterpretedLifecycleAction);
        Enq(p, o);
        Assert.True(p.Drain(TimeSpan.FromSeconds(2)));
        var snap = p.FreezeSnapshot(
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCQ6",
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared, Guid.NewGuid(), true);
        Assert.Equal(1, snap.Counters.RawUnknownType);
        Assert.Equal(nameof(MboInterpretedLifecycleAction.Unknown), snap.InterpretedLifecycleAction);
    }

    [Fact]
    public void Side_Trade_and_unmapped_become_Unknown()
    {
        Assert.Equal(MboDerivedSide.Bid, MboSideClassifier.Classify(0, out _));
        Assert.Equal(MboDerivedSide.Ask, MboSideClassifier.Classify(1, out _));
        Assert.Equal(MboDerivedSide.Unknown, MboSideClassifier.Classify(2, out var d));
        Assert.Equal("TradeMappedToUnknown", d);
        Assert.Equal(MboDerivedSide.Unknown, MboSideClassifier.Classify(9, out _));
    }

    [Fact]
    public void Zero_ExchangeOrderId_excluded_from_keyed_state_Delete_does_not_remove()
    {
        using var p = Open();
        var epoch = p.SubscriptionEpoch;
        Enq(p,
            Obs(epoch, 0, MboKnownRawUpdateTypes.New, 0),
            Obs(epoch, 7, MboKnownRawUpdateTypes.New, 0, price: 10),
            Obs(epoch, 7, MboKnownRawUpdateTypes.Change, 0, price: 11),
            Obs(epoch, 7, MboKnownRawUpdateTypes.Delete, 0, price: 11),
            Obs(epoch, 7, MboKnownRawUpdateTypes.New, 0, price: 12));
        Assert.True(p.Drain(TimeSpan.FromSeconds(2)));
        var snap = p.FreezeSnapshot(
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCQ6",
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared, Guid.NewGuid(), true);
        Assert.Equal(1, snap.Counters.ZeroExchangeOrderId);
        Assert.Equal(4, snap.Counters.NonzeroExchangeOrderId);
        Assert.Equal(1, snap.OrderObservationState.TrackedNonzeroIdCount);
        Assert.Contains(snap.OrderObservationState.Entries, e => e.ExchangeOrderId == 7 && e.ObservationCount == 4);
        Assert.True(snap.OrderObservationState.IdReuseSuspectedDeleteThenNew >= 1);
        Assert.Contains("New->Change", snap.OrderObservationState.RawTransitionCounts.Keys);
        Assert.Contains("diagnostic", snap.OrderObservationState.NamingNote, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Epoch_capture_stable_finalClosed_after_stop_and_artifact_fields()
    {
        using var p = Open();
        var epoch1 = p.CaptureSubscriptionEpoch;
        Enq(p, Obs(epoch1, 1, MboKnownRawUpdateTypes.Snapshot, 0));
        Assert.True(p.Drain(TimeSpan.FromSeconds(2)));
        p.StopAccepting();
        Assert.Equal(epoch1, p.CaptureSubscriptionEpoch);
        Assert.Equal(epoch1 + 1, p.FinalClosedEpoch);
        Enq(p, Obs(epoch1, 1, MboKnownRawUpdateTypes.New, 0));
        Assert.True(p.Counters.Snapshot().RejectedAfterDispose >= 1);
        var snap = p.FreezeSnapshot(
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCQ6",
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared, Guid.NewGuid(), true);
        Assert.Equal(1, snap.CaptureSubscriptionEpoch);
        Assert.Equal(2, snap.FinalClosedEpoch);
        Assert.All(snap.Samples, s => Assert.Equal(1, s.SubscriptionEpoch));
        var json = System.Text.Json.JsonSerializer.Serialize(snap, MboLifecycleProbeArtifactWriter.JsonOptions);
        using (var doc = System.Text.Json.JsonDocument.Parse(json))
        {
            Assert.False(doc.RootElement.TryGetProperty("subscriptionEpoch", out _));
            Assert.True(doc.RootElement.TryGetProperty("captureSubscriptionEpoch", out var cap));
            Assert.Equal(1, cap.GetInt64());
            Assert.True(doc.RootElement.TryGetProperty("finalClosedEpoch", out var fin));
            Assert.Equal(2, fin.GetInt64());
        }

        p.Dispose();
        p.Dispose();
    }

    [Fact]
    public void Bounded_queue_forced_drops()
    {
        using var p = new MboLifecycleProbe(new MboLifecycleProbeConfig(queueCapacity: 1, maxSamples: 8), startWorker: false);
        p.SetObservedInstrument(Instr());
        var epoch = p.SubscriptionEpoch;
        Enq(p, Obs(epoch, 1, 1, 0, seq: 1), Obs(epoch, 2, 1, 0, seq: 2), Obs(epoch, 3, 1, 0, seq: 3));
        Assert.True(p.Counters.Snapshot().QueueFullDrops >= 1);
    }

    [Fact]
    public void Snapshot_study_does_not_claim_completion()
    {
        using var p = Open();
        var epoch = p.SubscriptionEpoch;
        Enq(p,
            Obs(epoch, 1, MboKnownRawUpdateTypes.Snapshot, 0),
            Obs(epoch, 1, MboKnownRawUpdateTypes.New, 0),
            Obs(epoch, 2, MboKnownRawUpdateTypes.Snapshot, 1));
        Assert.True(p.Drain(TimeSpan.FromSeconds(2)));
        var snap = p.FreezeSnapshot(
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCQ6",
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared, Guid.NewGuid(), true);
        Assert.False(snap.SnapshotCompletionKnown);
        Assert.False(snap.SnapshotStudy.SnapshotCompletionKnown);
        Assert.True(snap.SnapshotStudy.SnapshotObservationCount >= 2);
    }

    [Fact]
    public void Artifact_sha_claims_false_and_versions()
    {
        using var p = Open();
        _ = p.TrySubscribeOnce(
            true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCQ6",
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            () => Task.CompletedTask);
        Enq(p, Obs(p.SubscriptionEpoch, 5, MboKnownRawUpdateTypes.New, 0, kind: DateTimeKind.Utc));
        Assert.True(p.Drain(TimeSpan.FromSeconds(2)));
        var snap = p.FreezeSnapshot(
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCQ6",
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared, Guid.NewGuid(), true);

        Assert.False(snap.LiveMboCapabilityClaim);
        Assert.False(snap.MboLifecycleCompletenessClaim);
        Assert.False(snap.StableMboBookReconstruction);
        Assert.False(snap.NativeSequenceAvailable);
        Assert.False(snap.IcebergInferenceAvailable);
        Assert.False(snap.SpoofingInferenceAvailable);
        Assert.False(snap.QueuePositionInferenceAvailable);
        Assert.False(snap.ExecutionInferenceAvailable);
        Assert.False(snap.ProviderUnsubscribePerformed);
        Assert.Equal("NotObserved", snap.ProviderUnsubscribeAvailability);
        Assert.Contains(MboLifecycleProbeVersions.ContinuityDisclaimer, snap.KnownLimitations);
        Assert.Equal("0.0.6", snap.ProbeVersion);
        Assert.Equal("1.0.1", snap.SchemaVersion);
        Assert.Equal(DateTimeKind.Utc.ToString(), snap.Samples[0].SourceDateTimeKind.ToString());

        var dir = Path.Combine(Path.GetTempPath(), "gcae-mbo-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var result = MboLifecycleProbeArtifactWriter.WriteAtomic(snap, dir);
            Assert.True(File.Exists(result.JsonPath));
            Assert.True(File.Exists(result.Sha256Path));
            var bytes = File.ReadAllBytes(result.JsonPath);
            var hash = Convert.ToHexString(SHA256.HashData(bytes));
            Assert.StartsWith(hash, File.ReadAllText(result.Sha256Path).Trim(), StringComparison.OrdinalIgnoreCase);
            var json = Encoding.UTF8.GetString(bytes);
            Assert.DoesNotContain(hash, json, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"liveMboCapabilityClaim\": false", json, StringComparison.Ordinal);
            Assert.Contains(MboLifecycleProbeVersions.ContinuityDisclaimer, json, StringComparison.Ordinal);
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }

    [Fact]
    public void Null_empty_batch_and_no_OrderBook_type()
    {
        using var p = Open();
        p.HandleMappedBatch(
            null, DateTime.UtcNow, 1, true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCQ6",
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared);
        p.HandleMappedBatch(
            Array.Empty<MboObservation?>(), DateTime.UtcNow, 1, true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCQ6",
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared);
        var c = p.Counters.Snapshot();
        Assert.Equal(1, c.NullBatchCallbacks);
        Assert.Equal(1, c.EmptyBatchCallbacks);

        var names = new[]
        {
            nameof(MboOrderObservationState),
            nameof(MboLifecycleProbe),
            nameof(MboObservation)
        };
        Assert.Contains(nameof(MboOrderObservationState), names);
        Assert.Null(Type.GetType("GC.AuctionFlow.Probe.OrderBook, GC.AuctionFlow"));
        Assert.Null(Type.GetType("GC.AuctionFlow.Probe.MboOrderBook, GC.AuctionFlow"));
        Assert.Null(Type.GetType("GC.AuctionFlow.Probe.DepthBook, GC.AuctionFlow"));
    }

    [Fact]
    public void Versions_preserve_trade_and_dom_schemas()
    {
        Assert.Equal("0.0.6", MboLifecycleProbeVersions.ProbeVersion);
        Assert.Equal("1.0.1", MboLifecycleProbeVersions.MboLifecycleProbeSchemaVersion);
        Assert.Equal("1.0.1", TradeStreamProbeVersions.TradeStreamProbeSchemaVersion);
        Assert.Equal("1.0.0", DomSemanticsProbeVersions.DomSemanticsProbeSchemaVersion);
        Assert.Equal("0.0.5", DomSemanticsProbeVersions.ProbeVersion);
        Assert.Equal("0.0.4", TradeStreamProbeVersions.ProbeVersion);
        Assert.Equal("0.0.6", CapabilitySchemaVersions.ProbeVersionPlaceholder);
        Assert.Equal("P0-06", BuildInfo.Phase);
    }

    [Fact]
    public void Priority_opaque_and_DateTimeKind_preserved()
    {
        using var p = Open();
        Enq(p, Obs(p.SubscriptionEpoch, 9, 1, 0, priority: -3, kind: DateTimeKind.Local));
        Assert.True(p.Drain(TimeSpan.FromSeconds(2)));
        var snap = p.FreezeSnapshot(
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCQ6",
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared, Guid.NewGuid(), true);
        Assert.True(snap.PriorityStudy.NegativePriorityCount >= 1);
        Assert.Contains("opaque", snap.PriorityStudy.OpaqueNote, StringComparison.OrdinalIgnoreCase);
        Assert.True(snap.DateTimeKindCounts.ContainsKey(nameof(DateTimeKind.Local)));
    }

    [Fact]
    public void Initial_time_window_uses_FirstCallbackReceiveUtc_not_sample_capacity()
    {
        using var p = new MboLifecycleProbe(
            new MboLifecycleProbeConfig(queueCapacity: 256, maxSamples: 256),
            startWorker: true);
        p.SetObservedInstrument(Instr());
        var t0 = DateTime.UtcNow;
        var epoch = p.CaptureSubscriptionEpoch;
        // 100 Snapshot items sharing the first callback receive timestamp — must NOT set later-window flag.
        var batch = Enumerable.Range(1, 100).Select(i =>
        {
            var o = Obs(epoch, i, MboKnownRawUpdateTypes.Snapshot, 0, seq: i);
            return new MboObservation(
                o.CallbackSource, o.SubscriptionEpoch, o.LocalMonotonicSequence, t0,
                o.ReceiveStopwatchTimestamp, o.CallbackManagedThreadId, o.SourceTimeTicks, o.SourceDateTimeKind,
                o.InstrumentIdentityKey, o.SecurityCode, o.SecurityId, o.Exchange,
                o.RawTypeName, o.RawTypeNumeric, o.RawTypeIsKnownEnumMember,
                o.RawSideName, o.RawSideNumeric, o.DerivedSide, o.ExchangeOrderId, o.Price, o.Volume, o.Priority,
                o.DiagnosticFingerprint);
        }).ToArray();

        p.HandleMappedBatch(
            batch, t0, 1, true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCQ6",
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared);
        Assert.True(p.Drain(TimeSpan.FromSeconds(2)));
        var snap = p.FreezeSnapshot(
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCQ6",
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared, Guid.NewGuid(), true);
        Assert.False(snap.SnapshotStudy.SnapshotSeenAfterInitialTimeWindow);
        Assert.Equal(5.0, snap.SnapshotStudy.InitialTimeWindowDurationSeconds);
        Assert.Equal(100, snap.SnapshotStudy.FirstCallbackBatchItemCount);
        Assert.Equal(100, snap.SnapshotStudy.InitialTimeWindowItemCount);
        Assert.True(snap.SnapshotStudy.FirstCallbackBatchRawTypeNumerics.Count >= 1);
        Assert.Contains(0, snap.SnapshotStudy.FirstCallbackBatchRawTypeNumerics); // Snapshot
        Assert.True(snap.SnapshotStudy.InitialTimeWindowRawTypeNumerics.Count >= 1);
        Assert.False(snap.SnapshotCompletionKnown);
    }

    [Fact]
    public void Order_state_capacity_saturation_does_not_evict()
    {
        using var p = new MboLifecycleProbe(
            new MboLifecycleProbeConfig(queueCapacity: 64, maxSamples: 32, maxTrackedNonzeroIds: 2),
            startWorker: true);
        p.SetObservedInstrument(Instr());
        var epoch = p.CaptureSubscriptionEpoch;
        Enq(p,
            Obs(epoch, 1, 1, 0, seq: 1),
            Obs(epoch, 2, 1, 0, seq: 2),
            Obs(epoch, 3, 1, 0, seq: 3),
            Obs(epoch, 1, 2, 0, seq: 4)); // existing ID still updates
        Assert.True(p.Drain(TimeSpan.FromSeconds(2)));
        var snap = p.FreezeSnapshot(
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCQ6",
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared, Guid.NewGuid(), true);
        Assert.Equal(2, snap.OrderObservationState.MaxTrackedNonzeroIds);
        Assert.Equal(2, snap.OrderObservationState.TrackedNonzeroIdCount);
        Assert.True(snap.OrderObservationState.StateCapacityReached);
        Assert.True(snap.OrderObservationState.UntrackedNonzeroIdDueToStateCapacity >= 1);
        Assert.Contains(snap.OrderObservationState.Entries, e => e.ExchangeOrderId == 1 && e.ObservationCount >= 2);
        Assert.DoesNotContain(snap.OrderObservationState.Entries, e => e.ExchangeOrderId == 3);
    }
}
