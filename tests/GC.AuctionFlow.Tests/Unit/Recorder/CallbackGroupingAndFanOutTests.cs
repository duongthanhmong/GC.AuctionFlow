using System.Text;
using System.Text.Json;
using GC.AuctionFlow.Recorder;
using GC.AuctionFlow.Recorder.FanOut;
using GC.AuctionFlow.Recorder.Payloads;

namespace GC.AuctionFlow.Tests.Unit.Recorder;

public sealed class CallbackGroupingContractTests
{
    [Fact]
    public void Invocation_sequences_are_independent_per_CallbackSource()
    {
        var p = new CallbackInvocationSequenceProvider();
        Assert.Equal(1, p.Next(RecorderCallbackSource.OnNewTrade));
        Assert.Equal(2, p.Next(RecorderCallbackSource.OnNewTrade));
        Assert.Equal(1, p.Next(RecorderCallbackSource.OnNewTrades));
        Assert.Equal(1, p.Next(RecorderCallbackSource.MarketDepthChanged));
        Assert.Equal(3, p.Next(RecorderCallbackSource.OnNewTrade));
    }

    [Fact]
    public void Invocation_sequence_first_value_is_one()
    {
        var p = new CallbackInvocationSequenceProvider();
        Assert.Equal(0, p.Current(RecorderCallbackSource.OnNewTrade));
        Assert.Equal(1, p.Next(RecorderCallbackSource.OnNewTrade));
    }

    [Fact]
    public void Invocation_sequence_is_thread_safe_unique()
    {
        var p = new CallbackInvocationSequenceProvider();
        const int threads = 8;
        const int per = 500;
        var bag = new System.Collections.Concurrent.ConcurrentBag<long>();
        Parallel.For(0, threads, _ =>
        {
            for (var i = 0; i < per; i++)
                bag.Add(p.Next(RecorderCallbackSource.OnNewTrades));
        });
        Assert.Equal(threads * per, bag.Count);
        Assert.Equal(threads * per, p.Current(RecorderCallbackSource.OnNewTrades));
        Assert.Equal(1, bag.Min());
        Assert.Equal(threads * per, bag.Max());
    }

    [Fact]
    public void Invocation_sequence_overflow_does_not_wrap()
    {
        var p = new CallbackInvocationSequenceProvider();
        p.ForceCurrentForTests(RecorderCallbackSource.OnNewTrade, long.MaxValue);
        var ex = Assert.Throws<InvalidOperationException>(() => p.Next(RecorderCallbackSource.OnNewTrade));
        Assert.Contains("CallbackInvocationSequenceExhausted", ex.Message, StringComparison.Ordinal);
        Assert.Equal(long.MaxValue, p.Current(RecorderCallbackSource.OnNewTrade));
    }

    [Fact]
    public void Same_callback_receive_stamps_copied_to_every_batch_item()
    {
        var ctx = new CallbackCaptureContext(
            RecorderCallbackSource.OnNewTrades, 9,
            DateTime.Parse("2026-07-22T15:00:00Z").ToUniversalTime(), 777, 42);
        var ordinals = new List<int>();
        var stamps = new List<(DateTime, long, int)>();

        var inputs = new object?[] { new object(), null, new object() };
        SinglePassEnumerationHelper.EnumerateBatchOnce(
            inputs,
            ctx,
            isBatch: true,
            map: (o, c, ord) =>
            {
                ordinals.Add(ord);
                stamps.Add((c.CallbackReceiveUtc, c.CallbackReceiveStopwatchTimestamp, c.CallbackManagedThreadId));
                return MakeItem(c, ord, 100 + ord);
            },
            onItem: null,
            onInvocationResult: null);

        Assert.Equal(new[] { 0, 2 }, ordinals);
        Assert.All(stamps, s =>
        {
            Assert.Equal(ctx.CallbackReceiveUtc, s.Item1);
            Assert.Equal(ctx.CallbackReceiveStopwatchTimestamp, s.Item2);
            Assert.Equal(ctx.CallbackManagedThreadId, s.Item3);
        });
    }

    [Fact]
    public void Null_raw_items_consume_ordinal_and_gaps_are_honest()
    {
        var ctx = Capture(RecorderCallbackSource.MarketDepthsBatch);
        var marketOrdinals = new List<int>();
        var result = SinglePassEnumerationHelper.EnumerateBatchOnce(
            new object?[] { new object(), null, new object(), null },
            ctx,
            true,
            (o, c, ord) =>
            {
                marketOrdinals.Add(ord);
                return MakeItem(c, ord, ord);
            },
            null,
            null);

        Assert.Equal(new[] { 0, 2 }, marketOrdinals);
        Assert.Equal(4, result.PayloadItemsEnumerated);
        Assert.Equal(2, result.NullItemObservations);
        Assert.True(result.FinalItemCountKnown);
        Assert.Equal(4, result.InvocationResult.PayloadItemsEnumerated);
    }

    [Fact]
    public void Mapper_failure_consumes_ordinal_and_is_not_sink_rejection()
    {
        var ctx = Capture(RecorderCallbackSource.OnNewTrades);
        var ordinals = new List<int>();
        var result = SinglePassEnumerationHelper.EnumerateBatchOnce(
            new object?[] { new object(), new object(), new object() },
            ctx,
            true,
            (o, c, ord) =>
            {
                ordinals.Add(ord);
                if (ord == 1) return null;
                return MakeItem(c, ord, ord);
            },
            _ => true,
            null);

        Assert.Equal(new[] { 0, 1, 2 }, ordinals);
        Assert.Equal(1, result.NormalizationFailures);
        Assert.Equal(0, result.FanOutItemRejections);
        Assert.Equal(0, result.FanOutItemFaults);
        Assert.True(result.FinalItemCountKnown);
    }

    [Fact]
    public void Sink_rejection_is_not_classified_as_mapper_failure()
    {
        var result = SinglePassEnumerationHelper.EnumerateBatchOnce(
            new object?[] { new object(), new object() },
            Capture(RecorderCallbackSource.OnNewTrades),
            true,
            (o, c, ord) => MakeItem(c, ord, ord),
            _ => false,
            null);

        Assert.Equal(0, result.NormalizationFailures);
        Assert.Equal(2, result.FanOutItemRejections);
        Assert.Equal(0, result.FanOutItemFaults);
    }

    [Fact]
    public void Sink_fault_is_not_classified_as_mapper_failure()
    {
        var result = SinglePassEnumerationHelper.EnumerateBatchOnce(
            new object?[] { new object() },
            Capture(RecorderCallbackSource.OnNewTrades),
            true,
            (o, c, ord) => MakeItem(c, ord, ord),
            _ => throw new InvalidOperationException("sink"),
            null);

        Assert.Equal(0, result.NormalizationFailures);
        Assert.Equal(1, result.FanOutItemFaults);
    }

    [Fact]
    public void Stream_local_sequence_is_per_normalized_item_not_invocation()
    {
        var ctx = Capture(RecorderCallbackSource.OnNewTrades);
        var locals = new List<long>();
        long next = 0;
        SinglePassEnumerationHelper.EnumerateBatchOnce(
            new object?[] { new object(), new object() },
            ctx,
            true,
            (o, c, ord) =>
            {
                var seq = Interlocked.Increment(ref next);
                locals.Add(seq);
                return MakeItem(c, ord, seq);
            },
            null,
            null);
        Assert.Equal(new long[] { 1, 2 }, locals);
        Assert.Equal(1, ctx.CallbackInvocationSequence);
    }

    [Fact]
    public void Single_enumeration_only_no_Count_ToList_second_pass()
    {
        var ctx = Capture(RecorderCallbackSource.OnNewTrades);
        var enumCount = 0;
        var once = new CountingEnumerable(new object?[] { new object(), new object() }, () => enumCount++);
        SinglePassEnumerationHelper.EnumerateBatchOnce(once, ctx, true, (o, c, ord) => MakeItem(c, ord, ord), null, null);
        Assert.Equal(1, enumCount);
    }

    [Fact]
    public void GetEnumerator_failure_result()
    {
        var result = SinglePassEnumerationHelper.EnumerateBatchOnce(
            new GetEnumeratorThrowingEnumerable(),
            Capture(RecorderCallbackSource.OnNewTrades),
            true,
            (o, c, ord) => MakeItem(c, ord, ord),
            null,
            null);
        Assert.False(result.EnumerationCompleted);
        Assert.False(result.FinalItemCountKnown);
        Assert.Equal(0, result.PayloadItemsEnumerated);
        Assert.StartsWith("GetEnumerator:", result.EnumerationFailureTypeSanitized);
    }

    [Fact]
    public void MoveNext_failure_result_retains_partial_items()
    {
        var produced = new List<int>();
        var result = SinglePassEnumerationHelper.EnumerateBatchOnce(
            new ThrowingAfterEnumerable(2),
            Capture(RecorderCallbackSource.OnNewTrades),
            true,
            (o, c, ord) =>
            {
                produced.Add(ord);
                return MakeItem(c, ord, ord);
            },
            null,
            null);

        Assert.Equal(2, produced.Count);
        Assert.False(result.EnumerationCompleted);
        Assert.False(result.FinalItemCountKnown);
        Assert.StartsWith("MoveNext:", result.EnumerationFailureTypeSanitized);
        Assert.Equal(2, result.PayloadItemsEnumerated);
    }

    [Fact]
    public void Current_failure_result()
    {
        var result = SinglePassEnumerationHelper.EnumerateBatchOnce(
            new CurrentThrowingEnumerable(),
            Capture(RecorderCallbackSource.OnNewTrades),
            true,
            (o, c, ord) => MakeItem(c, ord, ord),
            null,
            null);
        Assert.False(result.EnumerationCompleted);
        Assert.False(result.FinalItemCountKnown);
        Assert.StartsWith("Current:", result.EnumerationFailureTypeSanitized);
        Assert.Equal(1, result.PayloadItemsEnumerated);
    }

    [Fact]
    public void Partial_evidence_retained_after_enumeration_failure()
    {
        var ctx = Capture(RecorderCallbackSource.OnNewTrades);
        var produced = new List<int>();
        var result = SinglePassEnumerationHelper.EnumerateBatchOnce(
            new ThrowingAfterEnumerable(2),
            ctx,
            true,
            (o, c, ord) =>
            {
                produced.Add(ord);
                return MakeItem(c, ord, ord);
            },
            null,
            null);

        Assert.Equal(2, produced.Count);
        Assert.False(result.EnumerationCompleted);
        Assert.False(result.FinalItemCountKnown);
        Assert.NotNull(result.EnumerationFailureTypeSanitized);
    }

    [Fact]
    public void Exactly_one_invocation_result_emission_attempt_and_sink_failure_contained()
    {
        var ctx = Capture(RecorderCallbackSource.OnNewTrade);
        var emissions = 0;
        var result = SinglePassEnumerationHelper.EnumerateSingularOnce(
            new object(),
            ctx,
            (o, c, ord) => MakeItem(c, ord, 1),
            null,
            _ =>
            {
                emissions++;
                throw new InvalidOperationException("handler");
            });
        Assert.Equal(1, emissions);
        Assert.NotNull(result.InvocationResult);
    }

    [Fact]
    public void Successful_result_has_known_final_count_failed_unknown()
    {
        var ok = SinglePassEnumerationHelper.EnumerateBatchOnce(
            new object?[] { new object() },
            Capture(RecorderCallbackSource.OnNewTrades),
            true,
            (o, c, ord) => MakeItem(c, ord, 1),
            null,
            null);
        Assert.True(ok.FinalItemCountKnown);
        Assert.True(ok.EnumerationCompleted);

        var bad = SinglePassEnumerationHelper.EnumerateBatchOnce(
            new ThrowingAfterEnumerable(0),
            Capture(RecorderCallbackSource.OnNewTrades),
            true,
            (o, c, ord) => MakeItem(c, ord, 1),
            null,
            null);
        Assert.False(bad.FinalItemCountKnown);
        Assert.False(bad.EnumerationCompleted);
    }

    [Fact]
    public void Singular_callback_grouping_semantics()
    {
        var ctx = Capture(RecorderCallbackSource.OnNewTrade);
        var result = SinglePassEnumerationHelper.EnumerateSingularOnce(
            new object(),
            ctx,
            (o, c, ord) =>
            {
                Assert.Equal(0, ord);
                return MakeItem(c, ord, 1);
            },
            null,
            null);
        Assert.False(result.IsBatch);
        Assert.Equal(1, result.PayloadItemsEnumerated);
        Assert.True(result.EnumerationCompleted);
        Assert.True(result.FinalItemCountKnown);
        Assert.Equal(0, result.NullItemObservations);
    }

    [Fact]
    public void Null_singular_payload_without_inventing_market_event()
    {
        var items = 0;
        var result = SinglePassEnumerationHelper.EnumerateSingularOnce<object>(
            null,
            Capture(RecorderCallbackSource.OnNewTrade),
            (o, c, ord) =>
            {
                items++;
                return MakeItem(c, ord, 1);
            },
            null,
            null);
        Assert.Equal(0, items);
        Assert.Equal(1, result.NullItemObservations);
        Assert.Equal(1, result.PayloadItemsEnumerated);
        Assert.True(result.EnumerationCompleted);
        Assert.True(result.FinalItemCountKnown);
        Assert.Equal(0, result.NormalizationFailures);
    }

    [Fact]
    public void Callback_result_deterministic_serialization_aliases_absent()
    {
        var p = new CallbackInvocationResultPayload(
            RecorderCallbackSource.OnNewTrades, 3,
            DateTime.Parse("2026-07-22T12:00:00Z").ToUniversalTime(), 99, 7,
            true, true, 4, 1, 0, 0, 0, true, null);
        var draft = new RawEventDraft(
            RawEventRecorderVersions.RawEventRecorderSchemaVersion,
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            Guid.Parse("11111111-2222-3333-4444-555555555555"),
            RecorderStreamKind.Trade,
            RecorderCallbackSource.OnNewTrades,
            streamLocalCaptureSequence: 0,
            callbackInvocationSequence: 3,
            callbackItemOrdinal: 0,
            subscriptionOrCaptureEpoch: null,
            contractEpoch: 1,
            instrument: TestFixtures.Instrument(),
            declaredDataSourceMode: "Live",
            modeProvenance: "OperatorDeclared",
            declaredProvider: "Rithmic",
            providerProvenance: "OperatorDeclared",
            sourceTimeTicks: 0,
            sourceDateTimeKind: DateTimeKind.Utc,
            callbackReceiveUtc: p.CallbackReceiveUtc,
            callbackReceiveStopwatchTimestamp: p.CallbackReceiveStopwatchTimestamp,
            callbackManagedThreadId: p.CallbackManagedThreadId,
            payloadDiscriminator: RawEventPayloadKind.CallbackInvocationResult,
            payload: p,
            integrityFlags: RecorderIntegrityFlags.None,
            nativeSequenceAvailable: false);
        var env = RawEventEnvelope.FromDraft(draft, Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), 1, 1, DateTime.Parse("2026-07-22T12:01:00Z").ToUniversalTime());
        Assert.Equal(env.CallbackReceiveUtc, env.ReceiveUtc);
        Assert.Equal(env.CallbackReceiveStopwatchTimestamp, env.ReceiveStopwatchTimestamp);
        var a = RecorderJson.SerializeEnvelope(env);
        var b = RecorderJson.SerializeEnvelope(env);
        Assert.Equal(a, b);
        var json = Encoding.UTF8.GetString(a);
        Assert.Contains("\"kind\":\"CallbackInvocationResult\"", json, StringComparison.Ordinal);
        Assert.Contains("\"callbackInvocationSequence\":3", json, StringComparison.Ordinal);
        Assert.Contains("\"callbackItemOrdinal\":0", json, StringComparison.Ordinal);
        Assert.Contains("\"callbackReceiveUtc\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"receiveUtc\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"receiveStopwatchTimestamp\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\n  ", json);
        Assert.Contains("Z", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ReceiveUtc_alias_equals_CallbackReceiveUtc()
    {
        var d = TestFixtures.Draft();
        Assert.Equal(d.CallbackReceiveUtc, d.ReceiveUtc);
        Assert.Equal(d.CallbackReceiveStopwatchTimestamp, d.ReceiveStopwatchTimestamp);
        var env = RawEventEnvelope.FromDraft(d, Guid.NewGuid(), 1, 1, DateTime.UtcNow);
        Assert.Equal(env.CallbackReceiveUtc, env.ReceiveUtc);
    }

    [Fact]
    public void Record_category_discriminator_distinguishes_kinds()
    {
        Assert.Equal(RawEventRecordCategory.MarketEvent, RawEventRecordCategoryClassifier.Classify(RawEventPayloadKind.NewTrade));
        Assert.Equal(RawEventRecordCategory.CallbackInvocationResult, RawEventRecordCategoryClassifier.Classify(RawEventPayloadKind.CallbackInvocationResult));
        Assert.Equal(RawEventRecordCategory.LifecycleIntegrity, RawEventRecordCategoryClassifier.Classify(RawEventPayloadKind.RecorderLifecycle));
        Assert.Equal(RawEventRecordCategory.LifecycleIntegrity, RawEventRecordCategoryClassifier.Classify(RawEventPayloadKind.RecorderIntegrity));
    }

    [Fact]
    public void Schema_version_is_1_1_0_and_probes_unchanged()
    {
        Assert.Equal("1.2.0", RawEventRecorderVersions.RawEventRecorderSchemaVersion);
        Assert.Equal(1, RawEventRecorderVersions.RawEventContainerVersion);
        Assert.Equal("0.0.6", GC.AuctionFlow.Core.CapabilitySchemaVersions.ProbeVersionPlaceholder);
        Assert.Equal("1.0.1", GC.AuctionFlow.Probe.TradeStreamProbeVersions.TradeStreamProbeSchemaVersion);
        Assert.Equal("1.0.0", GC.AuctionFlow.Probe.DomSemanticsProbeVersions.DomSemanticsProbeSchemaVersion);
        Assert.Equal("1.0.1", GC.AuctionFlow.Probe.MboLifecycleProbeVersions.MboLifecycleProbeSchemaVersion);
        Assert.Contains("unsupported for live trust", RawEventRecorderVersions.Schema100UnsupportedNote, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void No_LINQ_materialization_symbols_in_helper_source()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "GC.AuctionFlow", "Recorder", "FanOut", "SinglePassEnumerationHelper.cs"));
        Assert.True(File.Exists(path), path);
        var text = File.ReadAllText(path);
        Assert.DoesNotContain(".Count()", text, StringComparison.Ordinal);
        Assert.DoesNotContain(".LongCount(", text, StringComparison.Ordinal);
        Assert.DoesNotContain(".ToArray(", text, StringComparison.Ordinal);
        Assert.DoesNotContain(".ToList(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("foreach", text, StringComparison.Ordinal);
    }

    private static CallbackCaptureContext Capture(RecorderCallbackSource s) =>
        new(s, 1, DateTime.Parse("2026-07-22T12:00:00Z").ToUniversalTime(), 55, 3);

    private static PrimitiveFanOutItem MakeItem(CallbackCaptureContext c, int ord, long streamLocal) =>
        new(
            c,
            ord,
            streamLocal,
            TestFixtures.Instrument(),
            1,
            DateTimeKind.Unspecified,
            RawEventPayloadKind.NewTrade,
            new NewTradePayload(1, 1, 1, 1, "Buy", 2, "Trade", true, false, null, null, null),
            RecorderIntegrityFlags.NativeSequenceAbsent,
            false);
}

public sealed class FanOutCoordinatorTests
{
    [Fact]
    public void Capability_accept_recorder_reject_independent()
    {
        var cap = new FixedCapabilityFanOutSink(CapabilitySinkOutcome.Accepted);
        var rec = new FixedRecorderDraftFanOutSink(RecorderSinkOutcome.QueueFull);
        var r = new PrimitiveFanOutCoordinator(cap, rec).Dispatch(SampleItem());
        Assert.Equal(CapabilitySinkOutcome.Accepted, r.Capability);
        Assert.Equal(RecorderSinkOutcome.QueueFull, r.Recorder);
        Assert.Equal(1, cap.AcceptCalls);
        Assert.Equal(1, rec.AcceptCalls);
    }

    [Fact]
    public void Capability_reject_recorder_accept()
    {
        var r = new PrimitiveFanOutCoordinator(
            new FixedCapabilityFanOutSink(CapabilitySinkOutcome.Rejected),
            new FixedRecorderDraftFanOutSink(RecorderSinkOutcome.Accepted)).Dispatch(SampleItem());
        Assert.Equal(CapabilitySinkOutcome.Rejected, r.Capability);
        Assert.Equal(RecorderSinkOutcome.Accepted, r.Recorder);
    }

    [Fact]
    public void Capability_fault_recorder_accept()
    {
        var r = new PrimitiveFanOutCoordinator(
            new FixedCapabilityFanOutSink(CapabilitySinkOutcome.Accepted, throwOnAccept: true),
            new FixedRecorderDraftFanOutSink(RecorderSinkOutcome.Accepted)).Dispatch(SampleItem());
        Assert.Equal(CapabilitySinkOutcome.Faulted, r.Capability);
        Assert.Equal(RecorderSinkOutcome.Accepted, r.Recorder);
    }

    [Fact]
    public void Recorder_fault_capability_accept()
    {
        var r = new PrimitiveFanOutCoordinator(
            new FixedCapabilityFanOutSink(CapabilitySinkOutcome.Accepted),
            new FixedRecorderDraftFanOutSink(RecorderSinkOutcome.Accepted, throwOnAccept: true)).Dispatch(SampleItem());
        Assert.Equal(CapabilitySinkOutcome.Accepted, r.Capability);
        Assert.Equal(RecorderSinkOutcome.Faulted, r.Recorder);
    }

    [Fact]
    public void Disabled_recorder_returns_NotConfigured()
    {
        var r = new PrimitiveFanOutCoordinator(
            new FixedCapabilityFanOutSink(CapabilitySinkOutcome.Disabled),
            new FixedRecorderDraftFanOutSink(RecorderSinkOutcome.NotConfigured)).Dispatch(SampleItem());
        Assert.Equal(RecorderSinkOutcome.NotConfigured, r.Recorder);
    }

    [Fact]
    public void Enabled_but_not_started_returns_SessionNotStarted()
    {
        var r = new PrimitiveFanOutCoordinator(
            new FixedCapabilityFanOutSink(CapabilitySinkOutcome.Accepted),
            new FixedRecorderDraftFanOutSink(RecorderSinkOutcome.SessionNotStarted)).Dispatch(SampleItem());
        Assert.Equal(RecorderSinkOutcome.SessionNotStarted, r.Recorder);
    }

    [Fact]
    public void Deterministic_sink_order_capability_then_recorder()
    {
        var order = new List<string>();
        var cap = new OrderingCapabilitySink(order);
        var rec = new OrderingRecorderSink(order);
        _ = new PrimitiveFanOutCoordinator(cap, rec).Dispatch(SampleItem());
        Assert.Equal(new[] { "capability", "recorder" }, order);
    }

    [Fact]
    public void New_contracts_have_no_ATAS_type_references()
    {
        Type[] types =
        [
            typeof(CallbackCaptureContext),
            typeof(CallbackInvocationSequenceProvider),
            typeof(PrimitiveFanOutItem),
            typeof(PrimitiveFanOutCoordinator),
            typeof(CallbackInvocationResultPayload),
            typeof(SinglePassEnumerationHelper)
        ];
        foreach (var t in types)
        {
            foreach (var p in t.GetProperties())
            {
                var n = p.PropertyType.FullName ?? "";
                Assert.DoesNotContain("ATAS.", n, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Indicator_has_trade_recorder_settings_but_no_dom_bba_mbo_recorder_settings()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "GC.AuctionFlow", "Atas", "GcAuctionFlowIndicator.cs"));
        Assert.True(File.Exists(path), path);
        var text = File.ReadAllText(path);
        Assert.Contains("EnableRawEventRecorder", text, StringComparison.Ordinal);
        Assert.Contains("EnableTradeRecording", text, StringComparison.Ordinal);
        Assert.Contains("TradeRecorderHost", text, StringComparison.Ordinal);
        // Narrowed deliberately, and only as far as the evidence supports.
        //
        // This originally forbade any depth, quote or MBO recording setting. The rationale
        // was P0-06D: a chart side effect reproduced on a second GCQ6 chart that did not
        // even have the indicator attached. But that was observed during an *active MBO
        // subscription*, and the lock was written wider than its own evidence — passive
        // receipt of MarketDepthChanged and OnBestBidAskChanged subscribes to nothing and
        // requests nothing.
        //
        // v1.2 §46.5 lists DOM changes among the things no downloaded history contains, so
        // leaving them unrecorded discards them permanently. Passive depth and quote
        // recording is therefore allowed; the two things that carried the risk are still
        // forbidden below, because both are *actions* rather than observation.
        // The existing MBO subscribe path lives in this file and stays gated; the sibling
        // test forbids any new one appearing under Recorder/, which is where it would
        // matter. Asserting its absence here would only forbid what is already present.
        Assert.DoesNotContain("EnableMboRecording", text, StringComparison.Ordinal);
        Assert.DoesNotContain("EnableDomSnapshotRecording", text, StringComparison.Ordinal);

        // The passive path must stay passive: recording must never trigger a snapshot pull.
        var recordDepth = text.IndexOf("private void TryRecordDepth", StringComparison.Ordinal);
        if (recordDepth >= 0)
        {
            var end = text.IndexOf("\n    }", recordDepth, StringComparison.Ordinal);
            var body = text[recordDepth..(end < 0 ? text.Length : end)];
            Assert.DoesNotContain("TryRequestSnapshotPull", body, StringComparison.Ordinal);
            Assert.DoesNotContain("Subscribe", body, StringComparison.Ordinal);
        }
        Assert.Contains("// P0-04B: do not call base.OnNewTrades", text, StringComparison.Ordinal);
        Assert.DoesNotContain("base.OnNewTrades", text.Replace("// P0-04B: do not call base.OnNewTrades", "", StringComparison.Ordinal), StringComparison.Ordinal);
    }

    [Fact]
    public void Recorder_sources_have_no_new_MBO_subscribe_or_chart_write()
    {
        var recorderDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "GC.AuctionFlow", "Recorder"));
        foreach (var file in Directory.GetFiles(recorderDir, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("SubscribeMarketByOrderData", text, StringComparison.Ordinal);
            Assert.DoesNotContain("this[bar]", text, StringComparison.Ordinal);
        }
        Assert.True(MboOperationalLock.MboSchemaSupported);
        Assert.False(MboOperationalLock.MboRecordingEnabled);
        Assert.Equal(MboIsolationRequirement.IsolatedEnvironmentOnly, MboOperationalLock.MboIsolationRequirement);
    }

    [Fact]
    public void Mbo_lock_schema_yes_recording_no()
    {
        Assert.True(MboOperationalLock.MboSchemaSupported);
        Assert.False(MboOperationalLock.MboRecordingEnabled);
        Assert.Equal(MboIsolationRequirement.IsolatedEnvironmentOnly, MboOperationalLock.MboIsolationRequirement);
    }

    [Fact]
    public void No_BestBidAsk_mapper_type_in_recorder_fanout()
    {
        var fanOutDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "GC.AuctionFlow", "Recorder", "FanOut"));
        foreach (var file in Directory.GetFiles(fanOutDir, "*.cs"))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("BestBidAskMapper", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapBestBidAsk", text, StringComparison.Ordinal);
        }
    }

    private static PrimitiveFanOutItem SampleItem()
    {
        var c = new CallbackCaptureContext(RecorderCallbackSource.OnNewTrade, 1, DateTime.UtcNow, 1, 1);
        return new PrimitiveFanOutItem(
            c, 0, 1, TestFixtures.Instrument(), 1, DateTimeKind.Utc,
            RawEventPayloadKind.NewTrade,
            new NewTradePayload(1, 1, 1, 1, "Buy", 2, "Trade", true, false, null, null, null),
            RecorderIntegrityFlags.None, false);
    }

    private sealed class OrderingCapabilitySink : ICapabilityFanOutSink
    {
        private readonly List<string> _order;
        public OrderingCapabilitySink(List<string> order) => _order = order;
        public CapabilitySinkOutcome TryAccept(PrimitiveFanOutItem item)
        {
            _order.Add("capability");
            return CapabilitySinkOutcome.Accepted;
        }
    }

    private sealed class OrderingRecorderSink : IRecorderDraftFanOutSink
    {
        private readonly List<string> _order;
        public OrderingRecorderSink(List<string> order) => _order = order;
        public RecorderSinkOutcome TryAccept(PrimitiveFanOutItem item)
        {
            _order.Add("recorder");
            return RecorderSinkOutcome.Accepted;
        }
    }
}

internal sealed class CountingEnumerable : IEnumerable<object?>
{
    private readonly object?[] _items;
    private readonly Action _onGetEnumerator;

    public CountingEnumerable(object?[] items, Action onGetEnumerator)
    {
        _items = items;
        _onGetEnumerator = onGetEnumerator;
    }

    public IEnumerator<object?> GetEnumerator()
    {
        _onGetEnumerator();
        return ((IEnumerable<object?>)_items).GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

internal sealed class GetEnumeratorThrowingEnumerable : IEnumerable<object?>
{
    public IEnumerator<object?> GetEnumerator() => throw new InvalidOperationException("GetEnumeratorFault");
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

internal sealed class CurrentThrowingEnumerable : IEnumerable<object?>
{
    public IEnumerator<object?> GetEnumerator() => new Enum();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class Enum : IEnumerator<object?>
    {
        private int _i;
        public object? Current => throw new InvalidOperationException("CurrentFault");
        public bool MoveNext() => _i++ == 0;
        public void Reset() => _i = 0;
        public void Dispose() { }
    }
}

internal sealed class ThrowingAfterEnumerable : IEnumerable<object?>
{
    private readonly int _okCount;

    public ThrowingAfterEnumerable(int okCount) => _okCount = okCount;

    public IEnumerator<object?> GetEnumerator() => new ThrowingEnumerator(_okCount);

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class ThrowingEnumerator : IEnumerator<object?>
    {
        private readonly int _ok;
        private int _i = -1;

        public ThrowingEnumerator(int ok) => _ok = ok;

        public object? Current => new object();

        public bool MoveNext()
        {
            _i++;
            if (_i < _ok) return true;
            throw new InvalidOperationException("EnumerationFault");
        }

        public void Reset() => _i = -1;
        public void Dispose() { }
    }
}
