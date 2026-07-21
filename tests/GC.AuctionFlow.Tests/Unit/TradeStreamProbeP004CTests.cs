using System.Text.Json;
using GC.AuctionFlow.Core;
using GC.AuctionFlow.Probe;

namespace GC.AuctionFlow.Tests.Unit;

/// <summary>P0-04C semantic closeout: cumulativeNewObservationCount naming.</summary>
public sealed class TradeStreamProbeP004CTests
{
    private static ObservedInstrumentSnapshot Instr() =>
        new("GCQ6", null, null, null, null, null, null, null, null, null, null);

    private static CumulativeTradeObservation Cum(TradeCallbackSource source, long seq, long? instanceId) =>
        new(
            source, seq, seq, DateTimeKind.Unspecified, DateTime.UtcNow, 0, "GCQ6",
            TradeFingerprints.CumulativeValue(seq, 1m, 1m, 1m, "Buy", 0),
            1m, 1m, 1m, "Buy", 0, instanceId, true, null);

    [Fact]
    public void OnCumulativeTrade_increments_cumulativeNewObservationCount()
    {
        using var probe = new TradeStreamProbe();
        probe.SetObservedInstrument(Instr());
        Assert.True(probe.TryEnqueueCumulative(
            Cum(TradeCallbackSource.OnCumulativeTrade, 1, 1),
            true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCQ6"));
        Wait(probe, 1);
        Assert.Equal(1, probe.Counters.Snapshot().CumulativeNewObservationCount);
        Assert.Equal(0, probe.Counters.Snapshot().CumulativeUpdateCount);
    }

    [Fact]
    public void OnUpdateCumulativeTrade_does_not_increment_cumulativeNewObservationCount()
    {
        using var probe = new TradeStreamProbe();
        probe.SetObservedInstrument(Instr());
        Assert.True(probe.TryEnqueueCumulative(
            Cum(TradeCallbackSource.OnCumulativeTrade, 1, 7),
            true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCQ6"));
        Assert.True(probe.TryEnqueueCumulative(
            Cum(TradeCallbackSource.OnUpdateCumulativeTrade, 2, 7),
            true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCQ6"));
        Wait(probe, 2);
        var c = probe.Counters.Snapshot();
        Assert.Equal(1, c.CumulativeNewObservationCount);
        Assert.Equal(1, c.CumulativeUpdateCount);
    }

    [Fact]
    public void Artifact_has_no_newExecutionCount_and_no_unique_exchange_execution_claim()
    {
        using var probe = new TradeStreamProbe();
        probe.SetObservedInstrument(Instr());
        Assert.True(probe.TryEnqueueCumulative(
            Cum(TradeCallbackSource.OnCumulativeTrade, 1, 1),
            true, DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCQ6"));
        Wait(probe, 1);

        var snap = probe.FreezeSnapshot(
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCQ6", Guid.NewGuid());
        Assert.Equal("1.0.1", snap.ArtifactIdentity.SchemaVersion);

        var dto = TradeStreamProbeArtifactWriter.ToDto(snap);
        var json = JsonSerializer.Serialize(dto, TradeStreamProbeArtifactWriter.JsonOptions);

        Assert.Contains("\"cumulativeNewObservationCount\": 1", json, StringComparison.Ordinal);
        Assert.DoesNotContain("newExecutionCount", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("uniqueExecution", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("totalExchangeExecution", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("totalExecutedVolume", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"schemaVersion\": \"1.0.1\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Deterministic_serialization_remains_stable()
    {
        using var probe = new TradeStreamProbe();
        probe.SetObservedInstrument(Instr());
        var session = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var snap = probe.FreezeSnapshot(
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared, "GCQ6", session);

        // Fix CreatedUtc instability: serialize DTO twice from same snapshot instance.
        var a = JsonSerializer.SerializeToUtf8Bytes(
            TradeStreamProbeArtifactWriter.ToDto(snap), TradeStreamProbeArtifactWriter.JsonOptions);
        var b = JsonSerializer.SerializeToUtf8Bytes(
            TradeStreamProbeArtifactWriter.ToDto(snap), TradeStreamProbeArtifactWriter.JsonOptions);
        Assert.Equal(a, b);
        Assert.Equal("1.0.1", TradeStreamProbeVersions.TradeStreamProbeSchemaVersion);
    }

    private static void Wait(TradeStreamProbe probe, long min)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < 2000)
        {
            if (probe.Counters.Snapshot().ProcessedByWorker >= min)
                return;
            Thread.Sleep(10);
        }

        Assert.Fail($"Worker processed {probe.Counters.Snapshot().ProcessedByWorker}, expected >= {min}");
    }
}
