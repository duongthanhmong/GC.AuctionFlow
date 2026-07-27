using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using GC.AuctionFlow.Core;
using GC.AuctionFlow.Probe;

namespace GC.AuctionFlow.Tests.Unit;

/// <summary>P0-04B architecture / dispatch / identity-bootstrap tests.</summary>
public sealed class TradeStreamProbeP004BTests
{
    [Fact]
    public void Compiled_OnNewTrades_does_not_call_base_ExtendedIndicator_OnNewTrades()
    {
        // Resolve production assembly via Probe type (no ATAS type load required).
        var asmPath = typeof(TradeStreamProbe).Assembly.Location;
        Assert.False(string.IsNullOrWhiteSpace(asmPath));
        Assert.True(File.Exists(asmPath), $"Production assembly missing: {asmPath}");

        using var fs = File.OpenRead(asmPath);
        using var pe = new PEReader(fs);
        var md = pe.GetMetadataReader();

        TypeDefinition? indicatorType = null;
        foreach (var th in md.TypeDefinitions)
        {
            var t = md.GetTypeDefinition(th);
            var name = md.GetString(t.Name);
            var ns = md.GetString(t.Namespace);
            if (name == "GcAuctionFlowIndicator" && ns == "GC.AuctionFlow.Atas")
            {
                indicatorType = t;
                break;
            }
        }

        Assert.True(indicatorType.HasValue, "GcAuctionFlowIndicator type not found in production assembly");

        MethodDefinition? onNewTrades = null;
        foreach (var mh in indicatorType.Value.GetMethods())
        {
            var m = md.GetMethodDefinition(mh);
            if (md.GetString(m.Name) == "OnNewTrades")
            {
                onNewTrades = m;
                break;
            }
        }

        Assert.True(onNewTrades.HasValue, "OnNewTrades method not found");
        var rva = onNewTrades.Value.RelativeVirtualAddress;
        Assert.True(rva != 0, "OnNewTrades has no IL body");

        var body = pe.GetMethodBody(rva);
        var il = body.GetILContent().ToArray();
        var calls = DecodeCallTargets(md, il);

        Assert.DoesNotContain(
            calls,
            c => c.MethodName == "OnNewTrades" &&
                 (c.DeclaringType.EndsWith("ExtendedIndicator", StringComparison.Ordinal) ||
                  c.DeclaringType.Contains("ExtendedIndicator", StringComparison.Ordinal)));

        // Sanity: method body is non-empty (batch enumeration exists).
        Assert.True(il.Length > 1);
    }

    [Fact]
    public void Batch_of_N_produces_N_batch_observations_without_singular_auto_create()
    {
        using var probe = new TradeStreamProbe(new TradeStreamProbeConfig(queueCapacity: 64));
        probe.SetObservedInstrument(new ObservedInstrumentSnapshot(
            "GCZ5", null, null, null, null, null, null, null, null, null, null));

        const int n = 5;
        probe.Counters.IncCallback(TradeCallbackSource.OnNewTradesBatch);
        for (var i = 0; i < n; i++)
        {
            var core = TradeFingerprints.CoreNewTrade(100 + i, 10m + i, 1m, "Buy", "Trade");
            var obs = new NewTradeObservation(
                TradeCallbackSource.OnNewTradesBatch, i + 1, 100 + i, DateTimeKind.Unspecified,
                DateTime.UtcNow, 0, "GCZ5", core, TradeFingerprints.ExtendedNewTrade(core, null, null),
                10m + i, 1m, 10m + i, "Buy", "Trade", true, false, null, null, null);
            Assert.True(probe.TryEnqueueNewTradeAlreadyCounted(obs));
        }

        WaitProcessed(probe, n);
        var c = probe.Counters.Snapshot();
        Assert.Equal(1, c.CallbackInvocationsOnNewTradesBatch);
        Assert.Equal(n, c.ObservationsNormalizedOnNewTradesBatch);
        Assert.Equal(0, c.CallbackInvocationsOnNewTrade);
        Assert.Equal(0, c.ObservationsNormalizedOnNewTrade);

        var snap = probe.FreezeSnapshot(
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCZ5", Guid.NewGuid());
        Assert.Equal(n, snap.NewTradeSamples.Count);
        Assert.All(snap.NewTradeSamples, s => Assert.Equal(TradeCallbackSource.OnNewTradesBatch, s.CallbackSource));
        // Fingerprints retained for cross-source comparison; no dedup.
        Assert.Equal(n, snap.NewTradeSamples.Select(s => s.CoreDiagnosticFingerprint).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Observed_identity_captured_before_gate_missing_expected_exports_diagnostic()
    {
        using var probe = new TradeStreamProbe();
        var observed = new ObservedInstrumentSnapshot(
            "GCZ5", "sec-1", null, "COMEX", null, 0.1m, null, "GC", "COMEX", 0.1m, "America/Chicago");
        probe.SetObservedInstrument(observed);

        Assert.False(probe.TryEnqueueNewTrade(
            MakeTrade(TradeCallbackSource.OnNewTrade, 1),
            true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, expectedInstrumentCode: ""));

        Assert.Equal(1, probe.Counters.Snapshot().RejectedByInstrumentGate);
        Assert.Equal(TradeStreamGateReason.ExpectedInstrumentMissing, probe.LastGateReasonCode);
        Assert.Same(observed, probe.GetObservedInstrument());

        var snap = probe.FreezeSnapshot(
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "", Guid.NewGuid(),
            enableTradeStreamProbe: true);

        Assert.False(snap.CaptureAuthorized);
        Assert.Equal("ExpectedInstrumentMissing", snap.GateReason);
        Assert.NotNull(snap.ObservedInstrument);
        Assert.Equal("GCZ5", snap.ObservedInstrument!.IdentityKey);
        Assert.False(snap.LiveTradeCapabilityClaim);

        var dir = Path.Combine(Path.GetTempPath(), "gcae_p004b_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var written = TradeStreamProbeArtifactWriter.WriteAtomic(snap, dir);
            var json = File.ReadAllText(written.JsonPath);
            Assert.Contains("\"captureAuthorized\": false", json, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ExpectedInstrumentMissing", json, StringComparison.Ordinal);
            Assert.Contains("\"identityKey\": \"GCZ5\"", json, StringComparison.Ordinal);
            Assert.DoesNotContain("LIVE capability PASS", json, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void Instrument_mismatch_exports_identity_diagnostic_not_live_pass()
    {
        using var probe = new TradeStreamProbe();
        probe.SetObservedInstrument(new ObservedInstrumentSnapshot(
            "GCZ5", null, null, null, null, null, null, null, null, null, null));

        Assert.False(probe.TryEnqueueNewTrade(
            MakeTrade(TradeCallbackSource.OnNewTrade, 1),
            true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "ESZ5"));

        Assert.Equal(1, probe.Counters.Snapshot().RejectedByInstrumentGate);
        var snap = probe.FreezeSnapshot(
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "ESZ5", Guid.NewGuid());
        Assert.False(snap.CaptureAuthorized);
        Assert.Equal("InstrumentMismatch", snap.GateReason);
        Assert.Equal("GCZ5", snap.ObservedInstrument!.IdentityKey);
        Assert.False(snap.LiveTradeCapabilityClaim);
    }

    [Fact]
    public void Zero_callback_serializes_as_NOT_OBSERVED_IN_TEST_WINDOW()
    {
        using var probe = new TradeStreamProbe();
        probe.SetObservedInstrument(new ObservedInstrumentSnapshot(
            "GCZ5", null, null, null, null, null, null, null, null, null, null));
        var snap = probe.FreezeSnapshot(
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCZ5", Guid.NewGuid());

        Assert.Equal(CallbackObservationStatusNames.NotObservedInTestWindow, snap.CallbackObservations.OnNewTrade);
        Assert.Equal(CallbackObservationStatusNames.NotObservedInTestWindow, snap.CallbackObservations.OnNewTradesBatch);
        Assert.Equal(CallbackObservationStatusNames.NotObservedInTestWindow, snap.CallbackObservations.OnCumulativeTrade);
        Assert.Equal(CallbackObservationStatusNames.NotObservedInTestWindow, snap.CallbackObservations.OnUpdateCumulativeTrade);

        var json = System.Text.Json.JsonSerializer.Serialize(
            TradeStreamProbeArtifactWriter.ToDto(snap), TradeStreamProbeArtifactWriter.JsonOptions);
        Assert.Contains(CallbackObservationStatusNames.NotObservedInTestWindow, json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"onNewTrade\": \"Unavailable\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"onNewTradesBatch\": \"Unavailable\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Nonzero_callback_serializes_as_OBSERVED()
    {
        using var probe = new TradeStreamProbe();
        probe.SetObservedInstrument(new ObservedInstrumentSnapshot(
            "GCZ5", null, null, null, null, null, null, null, null, null, null));
        Assert.True(probe.TryEnqueueNewTrade(
            MakeTrade(TradeCallbackSource.OnNewTrade, 1),
            true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCZ5"));
        WaitProcessed(probe, 1);

        var snap = probe.FreezeSnapshot(
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCZ5", Guid.NewGuid());
        Assert.True(snap.CaptureAuthorized);
        Assert.Null(snap.GateReason);
        Assert.Equal(CallbackObservationStatusNames.Observed, snap.CallbackObservations.OnNewTrade);
        Assert.Equal(CallbackObservationStatusNames.NotObservedInTestWindow, snap.CallbackObservations.OnNewTradesBatch);

        var json = System.Text.Json.JsonSerializer.Serialize(
            TradeStreamProbeArtifactWriter.ToDto(snap), TradeStreamProbeArtifactWriter.JsonOptions);
        Assert.Contains("\"onNewTrade\": \"OBSERVED\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Singular_and_batch_remain_separate_for_overlap_study()
    {
        using var probe = new TradeStreamProbe();
        probe.SetObservedInstrument(new ObservedInstrumentSnapshot(
            "GCZ5", null, null, null, null, null, null, null, null, null, null));

        var ticks = 777L;
        var core = TradeFingerprints.CoreNewTrade(ticks, 1m, 1m, "Buy", "Trade");
        var singular = MakeTrade(TradeCallbackSource.OnNewTrade, 1, ticks, core);
        var batch = MakeTrade(TradeCallbackSource.OnNewTradesBatch, 2, ticks, core);

        Assert.True(probe.TryEnqueueNewTrade(singular, true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCZ5"));
        probe.Counters.IncCallback(TradeCallbackSource.OnNewTradesBatch);
        Assert.True(probe.TryEnqueueNewTradeAlreadyCounted(batch));
        WaitProcessed(probe, 2);

        var snap = probe.FreezeSnapshot(
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCZ5", Guid.NewGuid());
        Assert.Equal(1, snap.Counters.CallbackInvocationsOnNewTrade);
        Assert.Equal(1, snap.Counters.CallbackInvocationsOnNewTradesBatch);
        Assert.Equal(2, snap.NewTradeSamples.Count); // no dedup
        Assert.True(snap.Overlap.SingularBatchCoreOverlapHits >= 1);
    }

    private static NewTradeObservation MakeTrade(
        TradeCallbackSource source,
        long seq,
        long ticks = 10,
        string? core = null)
    {
        core ??= TradeFingerprints.CoreNewTrade(ticks, 1m, 1m, "Buy", "Trade");
        return new NewTradeObservation(
            source, seq, ticks, DateTimeKind.Unspecified, DateTime.UtcNow, 0, "GCZ5",
            core, TradeFingerprints.ExtendedNewTrade(core, null, null),
            1m, 1m, 1m, "Buy", "Trade", true, false, null, null, null);
    }

    private static void WaitProcessed(TradeStreamProbe probe, long min)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < 2000)
        {
            if (probe.Counters.Snapshot().ProcessedByWorker >= min)
                return;
            Thread.Sleep(10);
        }

        Assert.Fail($"Worker did not process {min}; got {probe.Counters.Snapshot().ProcessedByWorker}");
    }

    private readonly record struct CallTarget(string DeclaringType, string MethodName);

    private static List<CallTarget> DecodeCallTargets(MetadataReader md, byte[] il)
    {
        var list = new List<CallTarget>();
        var i = 0;
        while (i < il.Length)
        {
            var op = il[i++];

            // Two-byte opcodes. Only ldftn/ldvirtftn carry a token; the rest take either
            // nothing or a 2-byte local index. Skipping the second byte and guessing, as
            // this did before, desynchronises the whole scan the moment one appears.
            if (op == 0xFE)
            {
                if (i >= il.Length) break;
                var second = il[i++];
                i += second switch
                {
                    0x06 or 0x07 => 4,              // ldftn, ldvirtftn
                    0x09 or 0x0A or 0x0B or 0x0C or 0x0D or 0x0E => 2, // ldarg/starg/ldloc/stloc
                    0x15 or 0x1C => 4,              // initobj, sizeof
                    _ => 0,
                };
                continue;
            }

            // switch: a 4-byte count followed by that many 4-byte offsets.
            if (op == 0x45)
            {
                if (i + 4 > il.Length) break;
                var targets = BitConverter.ToUInt32(il, i);
                i += 4 + checked((int)(targets * 4));
                continue;
            }

            // call / callvirt
            if (op is 0x28 or 0x6F)
            {
                if (i + 4 > il.Length) break;
                var token = BitConverter.ToInt32(il, i);
                i += 4;

                // A call operand is always a MethodDef, MemberRef or MethodSpec. Anything
                // else means the scan drifted, and resolving it would either throw or
                // invent a call target that is not in the method.
                var tableId = (byte)(token >>> 24);
                if (tableId is not (0x06 or 0x0A or 0x2B))
                    continue;

                list.Add(ResolveMember(md, token));
                continue;
            }

            i += OperandSize(op);
        }

        return list;
    }

    private static CallTarget ResolveMember(MetadataReader md, int token)
    {
        var handle = MetadataTokens.EntityHandle(token);
        switch (handle.Kind)
        {
            case HandleKind.MemberReference:
            {
                var mr = md.GetMemberReference((MemberReferenceHandle)handle);
                var name = md.GetString(mr.Name);
                var parent = mr.Parent;
                var declaring = ParentTypeName(md, parent);
                return new CallTarget(declaring, name);
            }
            case HandleKind.MethodDefinition:
            {
                var m = md.GetMethodDefinition((MethodDefinitionHandle)handle);
                var name = md.GetString(m.Name);
                var type = md.GetTypeDefinition(m.GetDeclaringType());
                var ns = md.GetString(type.Namespace);
                var tn = md.GetString(type.Name);
                return new CallTarget(string.IsNullOrEmpty(ns) ? tn : ns + "." + tn, name);
            }
            case HandleKind.MethodSpecification:
            {
                var ms = md.GetMethodSpecification((MethodSpecificationHandle)handle);
                return ResolveMember(md, MetadataTokens.GetToken(ms.Method));
            }
            default:
                return new CallTarget(handle.Kind.ToString(), $"token:0x{token:X8}");
        }
    }

    private static string ParentTypeName(MetadataReader md, EntityHandle parent)
    {
        switch (parent.Kind)
        {
            case HandleKind.TypeReference:
            {
                var tr = md.GetTypeReference((TypeReferenceHandle)parent);
                var ns = md.GetString(tr.Namespace);
                var name = md.GetString(tr.Name);
                return string.IsNullOrEmpty(ns) ? name : ns + "." + name;
            }
            case HandleKind.TypeDefinition:
            {
                var td = md.GetTypeDefinition((TypeDefinitionHandle)parent);
                var ns = md.GetString(td.Namespace);
                var name = md.GetString(td.Name);
                return string.IsNullOrEmpty(ns) ? name : ns + "." + name;
            }
            case HandleKind.TypeSpecification:
                return "TypeSpec";
            default:
                return parent.Kind.ToString();
        }
    }

    private static int OperandSize(byte op) => op switch
    {
        0x00 or 0x01 or 0x02 or 0x03 or 0x04 or 0x05 or 0x06 or 0x07 or 0x08 or 0x09 or 0x0A or 0x0B or 0x0C or 0x0D => 0,
        0x0E or 0x0F or 0x10 or 0x11 or 0x12 or 0x13 => 1, // ldarg.s etc
        0x14 or 0x15 or 0x16 or 0x17 or 0x18 or 0x19 or 0x1A or 0x1B or 0x1C or 0x1D or 0x1E => 0,
        0x1F => 1, // ldc.i4.s
        0x20 => 4, // ldc.i4
        0x21 => 8, // ldc.i8
        0x22 => 4, // ldc.r4
        0x23 => 8, // ldc.r8
        0x25 or 0x26 or 0x27 => 0,
        0x28 or 0x29 or 0x6F or 0x73 or 0x74 or 0x75 or 0x79 or 0x7B or 0x7C or 0x80 or 0x81 or 0xA3 or 0xD0 => 4,
        0x2B or 0x2C or 0x2D or 0x2E or 0x2F or 0x30 or 0x31 or 0x32 or 0x33 or 0x34 or 0x35 or 0x36 or 0x37 => 1,
        0x38 or 0x39 or 0x3A or 0x3B or 0x3C or 0x3D or 0x3E or 0x3F or 0x40 or 0x41 or 0x42 or 0x43 or 0x44 => 4,
        0x6A or 0x6B or 0x6C or 0x6D or 0x6E => 0,
        0x70 or 0x71 or 0x72 => 4,
        // Field and token-bearing opcodes. stfld/ldsfld/ldsflda were absent, and because
        // the fallback below silently returned 0 the scan drifted four bytes and then read
        // an arbitrary word as a metadata token. That is how this decoder reported an
        // invalid token rather than an honest miss.
        0x7D or 0x7E or 0x7F => 4,      // stfld, ldsfld, ldsflda
        0x8C or 0x8D or 0x8F => 4,      // box, newarr, ldelema
        0xA2 or 0xA4 or 0xA5 => 4,      // stelem, stobj/ldobj family
        0xC2 or 0xC6 or 0xC8 or 0xD1 => 4,
        0x2A => 0, // ret

        // Everything else is a no-operand instruction. Unknown opcodes are treated as
        // no-operand deliberately, but the token reader below validates before resolving,
        // so a drift surfaces as a clear failure instead of a bogus call target.
        _ => 0
    };
}
