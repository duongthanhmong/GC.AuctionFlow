using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using GC.AuctionFlow.Core;
using GC.AuctionFlow.Probe;

namespace GC.AuctionFlow.Tests.Unit;

public sealed class TradeStreamProbeTests
{
    private static NewTradeObservation NewTrade(
        TradeCallbackSource source,
        long seq,
        long ticks,
        DateTimeKind kind,
        decimal price,
        decimal volume,
        string direction = "Buy",
        string dataType = "Trade",
        long? exchangeOrderId = null,
        long? aggressorId = null,
        string instrumentKey = "GCZ5")
    {
        var core = TradeFingerprints.CoreNewTrade(ticks, price, volume, direction, dataType);
        var ext = TradeFingerprints.ExtendedNewTrade(core, exchangeOrderId, aggressorId);
        return new NewTradeObservation(
            source, seq, ticks, kind, DateTime.UtcNow, Stopwatch.GetTimestamp(),
            instrumentKey, core, ext, price, volume, price, direction, dataType,
            isAsk: direction == "Buy", isBid: direction == "Sell",
            exchangeOrderId, aggressorId, openInterest: null);
    }

    private static CumulativeTradeObservation Cum(
        TradeCallbackSource source,
        long seq,
        long ticks,
        DateTimeKind kind,
        decimal volume,
        decimal first,
        decimal last,
        int tickCount,
        long? instanceId,
        string direction = "Buy",
        IReadOnlyList<ConstituentPrintSummary>? constituents = null,
        string instrumentKey = "GCZ5")
    {
        var fp = TradeFingerprints.CumulativeValue(ticks, volume, first, last, direction, tickCount);
        return new CumulativeTradeObservation(
            source, seq, ticks, kind, DateTime.UtcNow, Stopwatch.GetTimestamp(),
            instrumentKey, fp, volume, first, last, direction, tickCount,
            instanceId, processLocalInstanceIdObserved: instanceId.HasValue, constituents);
    }

    private static ObservedInstrumentSnapshot Instr(string code) =>
        new(code, null, null, null, null, null, null, null, null, null, null);

    private static TradeStreamProbe CreateOpenProbe(TradeStreamProbeConfig? config = null)
    {
        var probe = new TradeStreamProbe(config ?? new TradeStreamProbeConfig(queueCapacity: 64, drainTimeoutMilliseconds: 500));
        probe.SetObservedInstrument(Instr("GCZ5"));
        return probe;
    }

    [Fact]
    public void Callback_source_counters_remain_separate()
    {
        using var probe = CreateOpenProbe();
        Assert.True(probe.TryEnqueueNewTrade(
            NewTrade(TradeCallbackSource.OnNewTrade, 1, 10, DateTimeKind.Unspecified, 1m, 1m),
            true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCZ5"));
        Assert.True(probe.TryEnqueueNewTrade(
            NewTrade(TradeCallbackSource.OnNewTradesBatch, 2, 11, DateTimeKind.Unspecified, 1m, 1m),
            true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCZ5"));
        Assert.True(probe.TryEnqueueCumulative(
            Cum(TradeCallbackSource.OnCumulativeTrade, 3, 12, DateTimeKind.Unspecified, 1m, 1m, 1m, 0, 1),
            true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCZ5"));
        Assert.True(probe.TryEnqueueCumulative(
            Cum(TradeCallbackSource.OnUpdateCumulativeTrade, 4, 13, DateTimeKind.Unspecified, 1m, 1m, 1m, 0, 1),
            true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCZ5"));

        WaitProcessed(probe, 4);
        var c = probe.Counters.Snapshot();
        Assert.Equal(1, c.CallbackInvocationsOnNewTrade);
        Assert.Equal(1, c.CallbackInvocationsOnNewTradesBatch);
        Assert.Equal(1, c.CallbackInvocationsOnCumulativeTrade);
        Assert.Equal(1, c.CallbackInvocationsOnUpdateCumulativeTrade);
    }

    [Fact]
    public void Singular_and_batch_fingerprints_comparable_without_deduplication()
    {
        using var probe = CreateOpenProbe();
        var a = NewTrade(TradeCallbackSource.OnNewTrade, 1, 100, DateTimeKind.Utc, 10m, 2m);
        var b = NewTrade(TradeCallbackSource.OnNewTradesBatch, 2, 100, DateTimeKind.Utc, 10m, 2m);
        Assert.Equal(a.CoreDiagnosticFingerprint, b.CoreDiagnosticFingerprint);
        probe.TryEnqueueNewTrade(a, true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCZ5");
        probe.TryEnqueueNewTrade(b, true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCZ5");
        WaitProcessed(probe, 2);
        var snap = probe.FreezeSnapshot(DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCZ5", Guid.NewGuid());
        Assert.Equal(2, snap.NewTradeSamples.Count); // no deletion
        Assert.True(snap.Overlap.SingularBatchCoreOverlapHits >= 1);
        Assert.True(snap.Overlap.FingerprintCollisionsObserved >= 0);
    }

    [Fact]
    public void Core_vs_extended_fingerprint_behavior()
    {
        var core = TradeFingerprints.CoreNewTrade(1, 2m, 3m, "Buy", "Trade");
        var ext1 = TradeFingerprints.ExtendedNewTrade(core, 10, 20);
        var ext2 = TradeFingerprints.ExtendedNewTrade(core, null, null);
        Assert.StartsWith(core + "|", ext1, StringComparison.Ordinal);
        Assert.Equal(core + "|null|null", ext2);
        Assert.NotEqual(ext1, ext2);
    }

    [Fact]
    public void Deterministic_invariant_fingerprinting()
    {
        var a = TradeFingerprints.CoreNewTrade(5, 1.25m, 3.5m, "Sell", "Trade");
        var b = TradeFingerprints.CoreNewTrade(5, 1.25m, 3.5m, "Sell", "Trade");
        Assert.Equal(a, b);
        Assert.Equal("5|1.25|3.5|Sell|Trade", a);
        Assert.Equal(
            "9|1|2|3|Buy|4",
            TradeFingerprints.CumulativeValue(9, 1m, 2m, 3m, "Buy", 4));
    }

    [Fact]
    public void No_event_deletion_on_fingerprint_collision()
    {
        using var probe = CreateOpenProbe(new TradeStreamProbeConfig(maxNewTradeSamples: 10));
        var t1 = NewTrade(TradeCallbackSource.OnNewTrade, 1, 50, DateTimeKind.Local, 1m, 1m);
        var t2 = NewTrade(TradeCallbackSource.OnNewTrade, 2, 50, DateTimeKind.Local, 1m, 1m);
        probe.TryEnqueueNewTrade(t1, true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCZ5");
        probe.TryEnqueueNewTrade(t2, true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCZ5");
        WaitProcessed(probe, 2);
        var snap = probe.FreezeSnapshot(DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCZ5", Guid.NewGuid());
        Assert.Equal(2, snap.NewTradeSamples.Count);
        Assert.True(snap.Overlap.FingerprintCollisionsObserved >= 1);
    }

    [Fact]
    public void Cumulative_new_vs_update_separation_and_update_never_increments_new_execution()
    {
        using var probe = CreateOpenProbe();
        probe.TryEnqueueCumulative(
            Cum(TradeCallbackSource.OnCumulativeTrade, 1, 1, DateTimeKind.Unspecified, 1m, 1m, 1m, 0, 7),
            true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCZ5");
        probe.TryEnqueueCumulative(
            Cum(TradeCallbackSource.OnUpdateCumulativeTrade, 2, 2, DateTimeKind.Unspecified, 2m, 1m, 2m, 1, 7),
            true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCZ5");
        WaitProcessed(probe, 2);
        var c = probe.Counters.Snapshot();
        Assert.Equal(1, c.CumulativeNewObservationCount);
        Assert.Equal(1, c.CumulativeUpdateCount);
        Assert.Equal(1, c.ObservationsNormalizedOnCumulativeTrade);
        Assert.Equal(1, c.ObservationsNormalizedOnUpdateCumulativeTrade);
    }

    [Fact]
    public void Process_local_cumulative_instance_linkage_limitations()
    {
        using var probe = CreateOpenProbe();
        probe.TryEnqueueCumulative(
            Cum(TradeCallbackSource.OnCumulativeTrade, 1, 1, DateTimeKind.Unspecified, 1m, 1m, 1m, 0, 42),
            true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCZ5");
        probe.TryEnqueueCumulative(
            Cum(TradeCallbackSource.OnUpdateCumulativeTrade, 2, 1, DateTimeKind.Unspecified, 1m, 1m, 1m, 0, 99),
            true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCZ5");
        WaitProcessed(probe, 2);
        var o = probe.FreezeSnapshot(DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCZ5", Guid.NewGuid()).Overlap;
        Assert.True(o.CumulativeInstanceIdChangedWithSameValueFingerprint >= 1);
        Assert.Contains(
            probe.FreezeSnapshot(DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCZ5", Guid.NewGuid())
                .KnownLimitations,
            x => x.Contains("Process-local cumulative", StringComparison.Ordinal));
    }

    [Fact]
    public void Mode_gate_rejection()
    {
        using var probe = CreateOpenProbe();
        Assert.False(probe.TryEnqueueNewTrade(
            NewTrade(TradeCallbackSource.OnNewTrade, 1, 1, DateTimeKind.Utc, 1m, 1m),
            true, DataSourceMode.Unknown, DataSourceModeProvenance.OperatorDeclared, "GCZ5"));
        Assert.Equal(1, probe.Counters.Snapshot().RejectedByModeGate);
    }

    [Fact]
    public void Empty_expected_instrument_is_instrument_gate_not_mode_gate()
    {
        using var probe = CreateOpenProbe();
        Assert.False(probe.TryEnqueueNewTrade(
            NewTrade(TradeCallbackSource.OnNewTrade, 1, 1, DateTimeKind.Utc, 1m, 1m),
            true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, ""));
        Assert.Equal(1, probe.Counters.Snapshot().RejectedByInstrumentGate);
        Assert.Equal(0, probe.Counters.Snapshot().RejectedByModeGate);
        Assert.Equal(TradeStreamGateReason.ExpectedInstrumentMissing, probe.LastGateReasonCode);
    }

    [Fact]
    public void Provenance_gate_rejection()
    {
        using var probe = CreateOpenProbe();
        Assert.False(probe.TryEnqueueNewTrade(
            NewTrade(TradeCallbackSource.OnNewTrade, 1, 1, DateTimeKind.Utc, 1m, 1m),
            true, DataSourceMode.Live, DataSourceModeProvenance.Unknown, "GCZ5"));
        Assert.Equal(1, probe.Counters.Snapshot().RejectedByModeGate);
    }

    [Fact]
    public void Generic_instrument_mismatch_rejection()
    {
        using var probe = CreateOpenProbe();
        Assert.False(probe.TryEnqueueNewTrade(
            NewTrade(TradeCallbackSource.OnNewTrade, 1, 1, DateTimeKind.Utc, 1m, 1m),
            true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "ESZ5"));
        Assert.Equal(1, probe.Counters.Snapshot().RejectedByInstrumentGate);
        Assert.Contains("InstrumentMismatch", probe.LastGateReason, StringComparison.Ordinal);
    }

    [Fact]
    public void Bounded_queue_full_behavior_and_non_blocking_enqueue()
    {
        var config = new TradeStreamProbeConfig(queueCapacity: 2, drainTimeoutMilliseconds: 500, maxNewTradeSamples: 10);
        using var probe = new TradeStreamProbe(config, startWorker: false);
        probe.SetObservedInstrument(Instr("GCZ5"));

        var sw = Stopwatch.StartNew();
        var accepted = 0;
        var attempts = 50;
        for (var i = 0; i < attempts; i++)
        {
            if (probe.TryEnqueueNewTrade(
                    NewTrade(TradeCallbackSource.OnNewTrade, i, i, DateTimeKind.Utc, 1m, 1m),
                    true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCZ5"))
                accepted++;
        }

        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 1000, "Enqueue path must not block waiting for queue space");
        var c = probe.Counters.Snapshot();
        Assert.Equal(2, accepted);
        Assert.Equal(2, c.AcceptedToQueue);
        Assert.Equal(attempts - 2, c.QueueFullDrops);
        Assert.Equal(attempts, c.CallbackInvocationsOnNewTrade);
    }

    [Fact]
    public void Explicit_drop_rejection_counters()
    {
        using var probe = CreateOpenProbe(new TradeStreamProbeConfig(queueCapacity: 1, drainTimeoutMilliseconds: 500));
        probe.TryEnqueueNewTrade(
            NewTrade(TradeCallbackSource.OnNewTrade, 1, 1, DateTimeKind.Utc, 1m, 1m),
            false, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCZ5");
        Assert.True(probe.Counters.Snapshot().RejectedByModeGate >= 1);

        probe.StopAccepting();
        probe.TryEnqueueNewTrade(
            NewTrade(TradeCallbackSource.OnNewTrade, 2, 2, DateTimeKind.Utc, 1m, 1m),
            true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCZ5");
        Assert.True(probe.Counters.Snapshot().RejectedAfterDispose >= 1);
    }

    [Fact]
    public void Immutable_payload_snapshots_preserve_DateTimeKind_and_source_ticks()
    {
        var obs = NewTrade(TradeCallbackSource.OnNewTrade, 1, 123456789, DateTimeKind.Local, 9m, 1m);
        Assert.Equal(DateTimeKind.Local, obs.SourceDateTimeKind);
        Assert.Equal(123456789, obs.SourceTimeTicks);
        // Source time is stored as ticks + Kind; not converted to UTC.
        Assert.NotEqual(DateTimeKind.Utc, obs.SourceDateTimeKind);
    }

    [Fact]
    public void Dispose_idempotent_and_no_enqueue_after_dispose_begins()
    {
        var probe = CreateOpenProbe();
        probe.StopAccepting();
        Assert.False(probe.TryEnqueueNewTrade(
            NewTrade(TradeCallbackSource.OnNewTrade, 1, 1, DateTimeKind.Utc, 1m, 1m),
            true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCZ5"));
        probe.Dispose();
        probe.Dispose(); // idempotent
        Assert.True(probe.Counters.Snapshot().RejectedAfterDispose >= 1);
    }

    [Fact]
    public void Bounded_drain_timeout_behavior()
    {
        using var probe = CreateOpenProbe(new TradeStreamProbeConfig(drainTimeoutMilliseconds: 1));
        probe.StopAccepting();
        var drained = probe.Drain(TimeSpan.FromMilliseconds(1));
        // May or may not finish in 1ms; must return (no indefinite wait).
        _ = drained;
        probe.Dispose();
    }

    [Fact]
    public void Artifact_deterministic_serialization_atomic_export_and_sha256()
    {
        using var probe = CreateOpenProbe();
        probe.TryEnqueueNewTrade(
            NewTrade(TradeCallbackSource.OnNewTrade, 1, 10, DateTimeKind.Unspecified, 1m, 1m),
            true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCZ5");
        WaitProcessed(probe, 1);
        var session = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var snap = probe.FreezeSnapshot(DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCZ5", session);

        var dir = Path.Combine(Path.GetTempPath(), "gcae_probe_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var r1 = TradeStreamProbeArtifactWriter.WriteAtomic(snap, dir);
            var r2 = TradeStreamProbeArtifactWriter.WriteAtomic(snap, dir);
            Assert.True(File.Exists(r1.JsonPath));
            Assert.True(File.Exists(r1.Sha256Path));
            var json = File.ReadAllText(r1.JsonPath);
            Assert.Contains(TradeStreamProbeVersions.ContinuityDisclaimer, json, StringComparison.Ordinal);
            Assert.Contains("does not establish exchange-feed completeness", json, StringComparison.Ordinal);
            Assert.DoesNotContain("totalExecutedVolume", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"contentSha256\"", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"fileSha256\"", json, StringComparison.OrdinalIgnoreCase);

            var bytes = File.ReadAllBytes(r1.JsonPath);
            var expected = Convert.ToHexString(SHA256.HashData(bytes));
            Assert.Equal(expected, r1.Sha256Hex);
            Assert.Contains(expected, File.ReadAllText(r1.Sha256Path), StringComparison.Ordinal);

            // Deterministic DTO bytes for same snapshot (CreatedUtc fixed in snap).
            var dtoBytes1 = JsonSerializer.SerializeToUtf8Bytes(
                TradeStreamProbeArtifactWriter.ToDto(snap), TradeStreamProbeArtifactWriter.JsonOptions);
            var dtoBytes2 = JsonSerializer.SerializeToUtf8Bytes(
                TradeStreamProbeArtifactWriter.ToDto(snap), TradeStreamProbeArtifactWriter.JsonOptions);
            Assert.Equal(dtoBytes1, dtoBytes2);
            Assert.Equal(r1.Sha256Hex, r2.Sha256Hex);
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void Snapshot_states_no_authoritative_volume_and_diagnostics_only()
    {
        using var probe = CreateOpenProbe();
        var snap = probe.FreezeSnapshot(DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCZ5", Guid.NewGuid());
        Assert.Equal("None", snap.AuthoritativeStream);
        Assert.True(snap.FingerprintsAreDiagnosticsOnly);
        Assert.Equal("Unknown", snap.CallbackThreading);
        Assert.Equal("Unknown", snap.ClockSemantics);
        Assert.True(snap.CaptureAuthorized);
        Assert.False(snap.LiveTradeCapabilityClaim);
        Assert.Contains(snap.KnownLimitations, x => x == TradeStreamProbeVersions.ContinuityDisclaimer);
        Assert.Contains(snap.KnownLimitations, x => x.Contains("No total executed volume", StringComparison.Ordinal));
    }

    [Fact]
    public void No_trading_logic_types_in_probe_assembly_surface()
    {
        var probeTypeNames = new[]
        {
            nameof(TradeStreamProbe),
            nameof(NewTradeObservation),
            nameof(CumulativeTradeObservation),
            nameof(TradeFingerprints),
            nameof(TradeStreamProbeGates),
            nameof(TradeStreamOverlapStats),
            nameof(TradeStreamProbeArtifactWriter)
        };
        Assert.All(probeTypeNames, n =>
        {
            Assert.DoesNotContain("Signal", n, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Alert", n, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Telegram", n, StringComparison.OrdinalIgnoreCase);
        });
        Assert.Null(typeof(TradeStreamProbe).Assembly.GetType("GC.AuctionFlow.Probe.SignalEngine"));
        Assert.Null(typeof(TradeStreamProbe).Assembly.GetType("GC.AuctionFlow.Atas.DomProbe"));
        Assert.Null(typeof(TradeStreamProbe).Assembly.GetType("GC.AuctionFlow.Probe.FarEngine"));
    }

    [Fact]
    public void Core_contracts_have_no_atas_payload_type_references()
    {
        var coreTypes = new[]
        {
            typeof(NewTradeObservation),
            typeof(CumulativeTradeObservation),
            typeof(TradeStreamProbe),
            typeof(TradeFingerprints)
        };
        foreach (var t in coreTypes)
        {
            foreach (var f in t.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public))
            {
                var ft = f.FieldType.FullName ?? "";
                Assert.DoesNotContain("ATAS.Indicators.MarketDataArg", ft, StringComparison.Ordinal);
                Assert.DoesNotContain("ATAS.Indicators.CumulativeTrade", ft, StringComparison.Ordinal);
                Assert.DoesNotContain("ATAS.DataFeedsCore", ft, StringComparison.Ordinal);
            }

            foreach (var p in t.GetProperties())
            {
                var pt = p.PropertyType.FullName ?? "";
                Assert.DoesNotContain("ATAS.Indicators.MarketDataArg", pt, StringComparison.Ordinal);
                Assert.DoesNotContain("ATAS.Indicators.CumulativeTrade", pt, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Config_bounds_are_explicit_and_validated()
    {
        Assert.Equal(4096, TradeStreamProbeConfig.DefaultQueueCapacity);
        Assert.Equal(256, TradeStreamProbeConfig.DefaultMaxNewTradeSamples);
        Assert.Throws<ArgumentOutOfRangeException>(() => new TradeStreamProbeConfig(queueCapacity: 0));
    }

    [Fact]
    public void Trade_probe_versions_remain_0_0_4_schema_1_0_1()
    {
        Assert.Equal("0.0.4", TradeStreamProbeVersions.ProbeVersion);
        Assert.Equal("1.0.1", TradeStreamProbeVersions.TradeStreamProbeSchemaVersion);
        Assert.Equal("0.0.5", CapabilitySchemaVersions.ProbeVersionPlaceholder);
        Assert.Equal("P0-05", BuildInfo.Phase);
    }

    private static void WaitProcessed(TradeStreamProbe probe, long min)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < 2000)
        {
            if (probe.Counters.Snapshot().ProcessedByWorker >= min)
                return;
            Thread.Sleep(10);
        }

        Assert.Fail($"Worker did not process {min} items in time; got {probe.Counters.Snapshot().ProcessedByWorker}");
    }
}
