using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using GC.AuctionFlow.Core;
using GC.AuctionFlow.Probe;

namespace GC.AuctionFlow.Tests.Unit;

public sealed class DomSemanticsProbeTests
{
    private static ObservedInstrumentSnapshot Instr(string code = "GCQ6") =>
        new(code, null, null, null, null, null, null, null, null, null, null);

    private static DepthObservation Obs(
        DepthCallbackSource source,
        long seq,
        decimal price,
        decimal volume,
        string dataType,
        bool isBid,
        bool isAsk,
        long ticks = 100,
        DateTimeKind kind = DateTimeKind.Unspecified)
    {
        var side = DepthSideClassifier.Classify(dataType, isBid, isAsk, out _);
        var fp = DepthFingerprints.Core(ticks, price, volume, dataType, isBid, isAsk);
        return new DepthObservation(
            source, seq, ticks, kind, DateTime.UtcNow, Stopwatch.GetTimestamp(),
            Environment.CurrentManagedThreadId, "GCQ6", price, volume, dataType, isBid, isAsk, side, fp);
    }

    private static DomSemanticsProbe Open(DomSemanticsProbeConfig? cfg = null, bool worker = true)
    {
        var p = new DomSemanticsProbe(cfg ?? new DomSemanticsProbeConfig(queueCapacity: 64), worker);
        p.SetObservedInstrument(Instr());
        return p;
    }

    private static bool Enq(DomSemanticsProbe p, DepthObservation o, bool countCb = true)
    {
        DepthSideClassifier.Classify(o.RawDataType, o.IsBid, o.IsAsk, out var detail);
        return p.TryEnqueue(
            o, detail, true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCQ6",
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared, countCb);
    }

    [Fact]
    public void Compiled_MarketDepthsChanged_does_not_call_base()
    {
        var asmPath = typeof(DomSemanticsProbe).Assembly.Location;
        using var fs = File.OpenRead(asmPath);
        using var pe = new PEReader(fs);
        var md = pe.GetMetadataReader();
        TypeDefinition? type = null;
        foreach (var th in md.TypeDefinitions)
        {
            var t = md.GetTypeDefinition(th);
            if (md.GetString(t.Name) == "GcAuctionFlowIndicator" && md.GetString(t.Namespace) == "GC.AuctionFlow.Atas")
            {
                type = t;
                break;
            }
        }

        Assert.True(type.HasValue);
        MethodDefinition? method = null;
        foreach (var mh in type.Value.GetMethods())
        {
            var m = md.GetMethodDefinition(mh);
            if (md.GetString(m.Name) == "MarketDepthsChanged") { method = m; break; }
        }

        Assert.True(method.HasValue);
        var il = pe.GetMethodBody(method.Value.RelativeVirtualAddress).GetILContent().ToArray();
        var calls = new List<string>();
        for (var i = 0; i < il.Length;)
        {
            var op = il[i++];
            if (op is 0x28 or 0x6F)
            {
                if (i + 4 > il.Length) break;
                var token = BitConverter.ToInt32(il, i); i += 4;
                var handle = MetadataTokens.EntityHandle(token);
                if (handle.Kind == HandleKind.MemberReference)
                    calls.Add(md.GetString(md.GetMemberReference((MemberReferenceHandle)handle).Name));
                else if (handle.Kind == HandleKind.MethodDefinition)
                    calls.Add(md.GetString(md.GetMethodDefinition((MethodDefinitionHandle)handle).Name));
            }
            else if (op == 0xFE) { if (i < il.Length) i++; }
            else if (op is >= 0x0E and <= 0x13 or 0x1F or (>= 0x2B and <= 0x37)) i += 1;
            else if (op is 0x20 or (>= 0x38 and <= 0x44) or 0x70 or 0x71 or 0x72 or 0x73 or 0x74 or 0x75 or 0x79 or 0x7B or 0x7C or 0x80 or 0x8C or 0x8D or 0xA3 or 0xD0) i += 4;
            else if (op is 0x21 or 0x23) i += 8;
        }

        // Must not call ExtendedIndicator.MarketDepthsChanged (would appear as MarketDepthsChanged callvirt to base).
        // Derived method may not call MarketDepthsChanged at all.
        Assert.DoesNotContain(calls, c => c == "MarketDepthsChanged");
    }

    [Fact]
    public void Batch_items_stay_MarketDepthsBatch_without_singular_synthesis()
    {
        using var p = Open();
        p.Counters.IncCallback(DepthCallbackSource.MarketDepthsBatch);
        for (var i = 0; i < 3; i++)
        {
            var o = Obs(DepthCallbackSource.MarketDepthsBatch, i + 1, 100 + i, 1m, "Bid", true, false, 10 + i);
            DepthSideClassifier.Classify(o.RawDataType, o.IsBid, o.IsAsk, out var d);
            Assert.True(p.TryEnqueueAlreadyCounted(o, d));
        }

        Wait(p, 3);
        var c = p.Counters.Snapshot();
        Assert.Equal(1, c.CallbackInvocationsMarketDepthsBatch);
        Assert.Equal(3, c.ObservationsNormalizedMarketDepthsBatch);
        Assert.Equal(0, c.CallbackInvocationsMarketDepthChanged);
        var snap = Freeze(p);
        Assert.All(snap.Samples, s => Assert.Equal(DepthCallbackSource.MarketDepthsBatch, s.CallbackSource));
    }

    [Fact]
    public void Side_classification_and_conflicts()
    {
        Assert.Equal(DepthSide.Bid, DepthSideClassifier.Classify("Bid", true, false, out var d1));
        Assert.Equal("ConsistentBid", d1);
        Assert.Equal(DepthSide.Ask, DepthSideClassifier.Classify("Ask", false, true, out var d2));
        Assert.Equal("ConsistentAsk", d2);
        Assert.Equal(DepthSide.Unknown, DepthSideClassifier.Classify("Bid", false, true, out var d3));
        Assert.Equal("ConflictingSideFields", d3);
        Assert.Equal(DepthSide.Unknown, DepthSideClassifier.Classify("Trade", true, false, out var d4));
        Assert.Equal("UnexpectedTradeDataType", d4);
    }

    [Fact]
    public void UpdateAction_always_Unknown_and_zero_volume_does_not_remove_level()
    {
        using var p = Open();
        Assert.True(Enq(p, Obs(DepthCallbackSource.MarketDepthChanged, 1, 50m, 5m, "Bid", true, false)));
        Assert.True(Enq(p, Obs(DepthCallbackSource.MarketDepthChanged, 2, 50m, 0m, "Bid", true, false)));
        Wait(p, 2);
        var snap = Freeze(p);
        Assert.All(snap.Samples, s => Assert.Equal(DepthUpdateAction.Unknown, s.UpdateAction));
        Assert.Equal("Unknown", snap.VolumeMeaning);
        Assert.Equal("Unknown", snap.ZeroVolumeMeaning);
        Assert.False(snap.StableBookReconstruction);
        Assert.True(snap.LevelObservationState.BidLevelCount >= 1);
        Assert.Equal(1, snap.Counters.ZeroVolumeObservations);
        Assert.Null(typeof(DomSemanticsProbe).Assembly.GetType("GC.AuctionFlow.Probe.OrderBook"));
        Assert.Null(typeof(DomSemanticsProbe).Assembly.GetType("GC.AuctionFlow.Probe.DepthBook"));
        Assert.NotNull(typeof(DepthLevelObservationState));
    }

    [Fact]
    public void No_fingerprint_dedup_and_sources_separate()
    {
        using var p = Open();
        var a = Obs(DepthCallbackSource.MarketDepthChanged, 1, 1m, 1m, "Ask", false, true, 9);
        var b = Obs(DepthCallbackSource.MarketDepthsBatch, 2, 1m, 1m, "Ask", false, true, 9);
        Assert.Equal(a.DiagnosticFingerprint, b.DiagnosticFingerprint);
        Assert.True(Enq(p, a));
        p.Counters.IncCallback(DepthCallbackSource.MarketDepthsBatch);
        DepthSideClassifier.Classify(b.RawDataType, b.IsBid, b.IsAsk, out var d);
        Assert.True(p.TryEnqueueAlreadyCounted(b, d));
        Wait(p, 2);
        var snap = Freeze(p);
        Assert.Equal(2, snap.Samples.Count);
        Assert.True(snap.SingularBatchFingerprintOverlapHits >= 1);
    }

    [Fact]
    public void Bounded_queue_forced_drops()
    {
        using var p = new DomSemanticsProbe(new DomSemanticsProbeConfig(queueCapacity: 2), startWorker: false);
        p.SetObservedInstrument(Instr());
        var accepted = 0;
        for (var i = 0; i < 20; i++)
        {
            if (Enq(p, Obs(DepthCallbackSource.MarketDepthChanged, i, 1m, 1m, "Bid", true, false, i)))
                accepted++;
        }

        Assert.Equal(2, accepted);
        Assert.Equal(18, p.Counters.Snapshot().QueueFullDrops);
    }

    [Fact]
    public void Identity_gate_and_capability_claims_false()
    {
        using var p = Open();
        Assert.False(p.TryEnqueue(
            Obs(DepthCallbackSource.MarketDepthChanged, 1, 1m, 1m, "Bid", true, false),
            "ConsistentBid", true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "",
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared));
        Assert.Equal(1, p.Counters.Snapshot().RejectedByInstrumentGate);
        var snap = Freeze(p, expected: "", enable: true);
        Assert.False(snap.CaptureAuthorized);
        Assert.Equal("ExpectedInstrumentMissing", snap.GateReason);
        Assert.False(snap.LiveDomCapabilityClaim);
        Assert.False(snap.HistoricalDomCapabilityClaim);
        Assert.False(snap.ReplayDomCapabilityClaim);
        Assert.NotNull(snap.ObservedInstrument);
    }

    [Fact]
    public void Callback_observation_status_and_DateTimeKind_preserved()
    {
        using var p = Open();
        var o = Obs(DepthCallbackSource.MarketDepthChanged, 1, 1m, 1m, "Bid", true, false, kind: DateTimeKind.Local);
        Assert.Equal(DateTimeKind.Local, o.SourceDateTimeKind);
        Assert.True(Enq(p, o));
        Wait(p, 1);
        var snap = Freeze(p);
        Assert.Equal(CallbackObservationStatusNames.Observed, snap.CallbackObservations.MarketDepthChanged);
        Assert.Equal(CallbackObservationStatusNames.NotObservedInTestWindow, snap.CallbackObservations.MarketDepthsBatch);
    }

    [Fact]
    public void Snapshot_pull_study_and_artifact_sha()
    {
        using var p = Open();
        Assert.True(p.TryRequestSnapshotPull());
        Assert.True(p.TryConsumeSnapshotPullRequest());
        var items = new[]
        {
            Obs(DepthCallbackSource.SnapshotPull, 1, 10m, 2m, "Bid", true, false),
            Obs(DepthCallbackSource.SnapshotPull, 2, 11m, 0m, "Ask", false, true)
        };
        p.ApplySnapshotPullResult(items, DateTime.UtcNow);
        var snap = Freeze(p);
        Assert.True(snap.SnapshotPullStudy.Executed);
        Assert.False(snap.SnapshotPullStudy.SnapshotCompletionKnown);
        Assert.Equal(2, snap.SnapshotPullStudy.ItemCount);
        Assert.Contains(snap.KnownLimitations, x => x.Contains("Snapshot completeness", StringComparison.Ordinal));
        Assert.Contains(DomSemanticsProbeVersions.ContinuityDisclaimer, snap.KnownLimitations);
        Assert.False(snap.SnapshotPullStudy.SnapshotCompletionKnown);
        Assert.Contains("completion cannot be known", snap.SnapshotPullStudy.Limitation ?? "", StringComparison.Ordinal);

        var dir = Path.Combine(Path.GetTempPath(), "gcae_dom_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var w = DomSemanticsProbeArtifactWriter.WriteAtomic(snap, dir);
            var json = File.ReadAllText(w.JsonPath);
            Assert.Contains("\"schemaVersion\": \"1.0.0\"", json, StringComparison.Ordinal);
            Assert.Contains("\"probeVersion\": \"0.0.5\"", json, StringComparison.Ordinal);
            Assert.Contains("\"liveDomCapabilityClaim\": false", json, StringComparison.Ordinal);
            Assert.DoesNotContain("\"fileSha256\"", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("pulling", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("iceberg", json, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(w.JsonPath))), w.Sha256Hex);
            var a = JsonSerializer.SerializeToUtf8Bytes(snap, DomSemanticsProbeArtifactWriter.JsonOptions);
            var b = JsonSerializer.SerializeToUtf8Bytes(snap, DomSemanticsProbeArtifactWriter.JsonOptions);
            Assert.Equal(a, b);
        }
        finally { try { Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void Dispose_idempotent_no_enqueue_after()
    {
        var p = Open();
        p.StopAccepting();
        Assert.False(Enq(p, Obs(DepthCallbackSource.MarketDepthChanged, 1, 1m, 1m, "Bid", true, false)));
        p.Dispose();
        p.Dispose();
        Assert.True(p.Counters.Snapshot().RejectedAfterDispose >= 1);
    }

    [Fact]
    public void Versions_and_trade_schema_unchanged()
    {
        Assert.Equal("0.0.5", DomSemanticsProbeVersions.ProbeVersion);
        Assert.Equal("1.0.0", DomSemanticsProbeVersions.DomSemanticsProbeSchemaVersion);
        Assert.Equal("1.0.1", TradeStreamProbeVersions.TradeStreamProbeSchemaVersion);
        Assert.Equal("0.0.4", TradeStreamProbeVersions.ProbeVersion);
        Assert.Equal("P0-05", BuildInfo.Phase);
    }

    [Fact]
    public void BestBidAsk_stays_separate_source()
    {
        using var p = Open();
        Assert.True(Enq(p, Obs(DepthCallbackSource.BestBidAskChanged, 1, 99m, 1m, "Bid", true, false)));
        Wait(p, 1);
        var snap = Freeze(p);
        Assert.Equal(1, snap.Counters.CallbackInvocationsBestBidAskChanged);
        Assert.Equal(0, snap.Counters.CallbackInvocationsMarketDepthChanged);
        Assert.Equal(99m, snap.LastBestBidPrice);
    }

    private static DomSemanticsProbeSnapshot Freeze(
        DomSemanticsProbe p,
        string expected = "GCQ6",
        bool enable = true) =>
        p.FreezeSnapshot(
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, expected,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared, Guid.NewGuid(), enable);

    private static void Wait(DomSemanticsProbe p, long min)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < 2000)
        {
            if (p.Counters.Snapshot().ProcessedByWorker >= min) return;
            Thread.Sleep(10);
        }

        Assert.Fail($"processed={p.Counters.Snapshot().ProcessedByWorker} expected>={min}");
    }
}
