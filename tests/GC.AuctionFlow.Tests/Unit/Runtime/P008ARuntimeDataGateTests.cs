using GC.AuctionFlow.Core;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Runtime;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Runtime;

public sealed class P008ARuntimeDataGateTests
{
    private static readonly DateTime Now = new(2026, 7, 21, 12, 0, 0, DateTimeKind.Utc);
    private static readonly RuntimeGateConfig Config = new(0.1m, 14);

    private static ObservedInstrumentSnapshot Gcq6(
        decimal? tick = 0.1m,
        DateTime? expiry = null,
        string? exchange = "COMEX") =>
        new(
            securityCode: "GCQ6",
            securityId: "GCQ6-ID",
            instrument: "GCQ6",
            exchange: exchange,
            expiration: expiry ?? new DateTime(2026, 8, 27),
            tickSize: tick,
            underlyingSecurity: "GC",
            instrumentInfoInstrument: "GCQ6",
            instrumentInfoExchange: exchange,
            instrumentInfoTickSize: tick,
            instrumentInfoTimeZone: null);

    [Fact]
    public void Contract_GCQ6_matches_expected_GCQ6()
    {
        var c = ContractSnapshotBuilder.Build(Gcq6(), "GCQ6", Config, Now);
        Assert.Equal(InstrumentMatchState.Match, c.InstrumentMatchState);
        Assert.Equal("GCQ6", c.SecurityCode);
        Assert.Equal(ExpirationState.Valid, c.ExpirationState);
        Assert.Equal(RollState.Unknown, c.RollState);
        Assert.Contains("NO_INVENTED_NEXT_CONTRACT_VOLUME", c.KnownLimitations);
        Assert.DoesNotContain(c.KnownLimitations, x => x.Contains("NEXT_CONTRACT", StringComparison.Ordinal) && x.Contains("VOLUME=", StringComparison.Ordinal));
    }

    [Fact]
    public void Contract_expected_instrument_mismatch()
    {
        var c = ContractSnapshotBuilder.Build(Gcq6(), "GCU6", Config, Now);
        Assert.Equal(InstrumentMatchState.Mismatch, c.InstrumentMatchState);
    }

    [Fact]
    public void Contract_unknown_identity()
    {
        var c = ContractSnapshotBuilder.Build(null, "GCQ6", Config, Now);
        Assert.Equal(InstrumentMatchState.Unknown, c.InstrumentMatchState);
        Assert.Equal("Unknown", c.IdentityKey);
    }

    [Fact]
    public void Contract_nonpositive_tick_size()
    {
        var c = ContractSnapshotBuilder.Build(Gcq6(tick: 0m), "GCQ6", Config, Now);
        Assert.Equal(0m, c.TickSize);
        var gate = DataGateEngine.Evaluate(
            c, Cap(), Config, indicatorDisposed: false, Now);
        Assert.Equal(DataState.Invalid, gate.DataState);
        Assert.Equal(DataGateReasonCodes.TickSizeInvalid, gate.PrimaryReasonCode);
    }

    [Fact]
    public void Contract_expected_tick_size_mismatch()
    {
        var c = ContractSnapshotBuilder.Build(Gcq6(tick: 0.25m), "GCQ6", Config, Now);
        var gate = DataGateEngine.Evaluate(c, Cap(), Config, false, Now);
        Assert.Equal(DataState.Invalid, gate.DataState);
        Assert.Contains(DataGateReasonCodes.TickSizeMismatch, gate.AllReasonCodes);
    }

    [Fact]
    public void Contract_valid_near_and_expired_expiration()
    {
        var valid = ContractSnapshotBuilder.ClassifyExpiration(new DateTime(2026, 8, 27), Now, 14);
        Assert.Equal(ExpirationState.Valid, valid.State);

        var near = ContractSnapshotBuilder.ClassifyExpiration(Now.Date.AddDays(7), Now, 14);
        Assert.Equal(ExpirationState.NearExpiration, near.State);

        var expired = ContractSnapshotBuilder.ClassifyExpiration(Now.Date.AddDays(-1), Now, 14);
        Assert.Equal(ExpirationState.Expired, expired.State);
    }

    [Fact]
    public void Contract_unavailable_roll_evidence_remains_Unknown()
    {
        var c = ContractSnapshotBuilder.Build(Gcq6(), "GCQ6", Config, Now);
        Assert.Equal(RollState.Unknown, c.RollState);
        Assert.DoesNotContain(c.KnownLimitations, x => x.Contains("ActiveRoll=", StringComparison.Ordinal));
    }

    [Fact]
    public void DataGate_invalid_instrument_mismatch()
    {
        var c = ContractSnapshotBuilder.Build(Gcq6(), "GCU6", Config, Now);
        var gate = DataGateEngine.Evaluate(c, Cap(tradeObserved: true), Config, false, Now);
        Assert.Equal(DataState.Invalid, gate.DataState);
        Assert.Equal(DataGateReasonCodes.InstrumentMismatch, gate.PrimaryReasonCode);
    }

    [Fact]
    public void DataGate_expired_contract_invalid()
    {
        var c = ContractSnapshotBuilder.Build(Gcq6(expiry: Now.Date.AddDays(-2)), "GCQ6", Config, Now);
        var gate = DataGateEngine.Evaluate(c, Cap(tradeObserved: true), Config, false, Now);
        Assert.Equal(DataState.Invalid, gate.DataState);
        Assert.Contains(DataGateReasonCodes.ContractExpired, gate.AllReasonCodes);
    }

    [Fact]
    public void DataGate_provider_mode_conflict_invalid()
    {
        var c = ContractSnapshotBuilder.Build(Gcq6(), "GCQ6", Config, Now);
        var cap = Cap(
            mode: DataSourceMode.Live,
            modeProv: DataSourceModeProvenance.Unknown,
            provider: DeclaredFeedProvider.Unknown,
            tradeObserved: true);
        var gate = DataGateEngine.Evaluate(c, cap, Config, false, Now);
        Assert.Equal(DataState.Invalid, gate.DataState);
        Assert.Contains(DataGateReasonCodes.ProviderModeConflict, gate.AllReasonCodes);
    }

    [Fact]
    public void DataGate_valid_identity_profile_unavailable_is_Degraded()
    {
        var c = ContractSnapshotBuilder.Build(Gcq6(), "GCQ6", Config, Now);
        var gate = DataGateEngine.Evaluate(c, Cap(tradeObserved: true), Config, false, Now);
        Assert.Equal(DataState.Degraded, gate.DataState);
        Assert.Equal(DataGateReasonCodes.ProfileNotReady, gate.PrimaryReasonCode);
    }

    [Fact]
    public void DataGate_no_trade_observed_degraded()
    {
        var c = ContractSnapshotBuilder.Build(Gcq6(), "GCQ6", Config, Now);
        var gate = DataGateEngine.Evaluate(c, Cap(tradeObserved: false), Config, false, Now);
        Assert.Equal(DataState.Degraded, gate.DataState);
        Assert.Contains(DataGateReasonCodes.TradeNotObserved, gate.AllReasonCodes);
        Assert.Equal(DataGateReasonCodes.ProfileNotReady, gate.PrimaryReasonCode);
    }

    [Fact]
    public void DataGate_disposed_host_invalid()
    {
        var c = ContractSnapshotBuilder.Build(Gcq6(), "GCQ6", Config, Now);
        var gate = DataGateEngine.Evaluate(c, Cap(tradeObserved: true), Config, indicatorDisposed: true, Now);
        Assert.Equal(DataState.Invalid, gate.DataState);
        Assert.Equal(DataGateReasonCodes.IndicatorDisposed, gate.PrimaryReasonCode);
    }

    [Fact]
    public void DataGate_hard_invalid_not_overridden_by_favorable_capabilities()
    {
        var c = ContractSnapshotBuilder.Build(Gcq6(), "GCU6", Config, Now);
        var favorable = new RuntimeCapabilitySnapshot(
            RuntimeCapabilityState.Available,
            RuntimeCapabilityState.Available,
            RuntimeCapabilityState.Ready,
            RuntimeCapabilityState.Available,
            RuntimeCapabilityState.Blocked,
            DataSourceMode.Live,
            DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic,
            FeedProviderProvenance.OperatorDeclared,
            Now,
            true,
            RuntimeCapabilityState.Recording,
            Array.Empty<string>());
        var gate = DataGateEngine.Evaluate(c, favorable, Config, false, Now);
        Assert.Equal(DataState.Invalid, gate.DataState);
        Assert.DoesNotContain(DataGateReasonCodes.ProfileNotReady, gate.AllReasonCodes);
    }

    [Fact]
    public void Runtime_snapshot_immutable_versioned_atomic_no_mutable_recorder()
    {
        var engine = new GcaeRuntimeEngine(Config);
        var snap = engine.Publish(
            Gcq6(), "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            tradeObserved: true, lastTradeCallbackUtc: Now,
            rawRecorderMasterEnabled: false, tradeRecordingEnabled: true,
            recorderAccepting: false, recorderFaulted: false, recorderSessionPresent: false,
            indicatorDisposed: false, timestampUtc: Now);

        Assert.Equal(GcaeRuntimeSnapshot.SnapshotVersion, snap.Version);
        Assert.Same(snap, engine.Current);
        Assert.Equal(ProfilePlaceholderState.NotReady, snap.Profile);
        Assert.Equal(ReferencePlaceholderState.NotAvailable, snap.Reference);
        Assert.Contains("PROFILE_SET_ABSENT", snap.KnownLimitations);
        Assert.Null(snap.GetType().GetProperty("TradeRecorderHost"));
        Assert.Null(snap.GetType().GetProperty("TradeStreamProbe"));
    }

    [Fact]
    public void GpsCard_valid_GCQ6_Rithmic_Live_shows_degraded_profile_mbo_blocked()
    {
        var engine = new GcaeRuntimeEngine(Config);
        var snap = engine.Publish(
            Gcq6(), "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            tradeObserved: true, lastTradeCallbackUtc: Now,
            rawRecorderMasterEnabled: false, tradeRecordingEnabled: true,
            recorderAccepting: false, recorderFaulted: false, recorderSessionPresent: false,
            indicatorDisposed: false, timestampUtc: Now);

        var vm = AuctionGpsCardMapper.FromSnapshot(snap, showDiagnostics: false);
        Assert.Equal("GC AUCTIONFLOW ENGINE", vm.Title);
        Assert.Equal("DATA: DEGRADED", vm.DataLine);
        Assert.Equal("REASON: PROFILE NOT READY", vm.ReasonLine);
        Assert.Equal("CONTRACT: GCQ6", vm.ContractLine);
        Assert.Equal("EXCHANGE: COMEX", vm.ExchangeLine);
        Assert.Equal("EXPIRY: 2026-08-27", vm.ExpiryLine);
        Assert.Equal("TICK SIZE: 0.1", vm.TickSizeLine);
        Assert.Equal("MODE: LIVE", vm.ModeLine);
        Assert.Equal("PROVIDER: RITHMIC", vm.ProviderLine);
        Assert.Equal("TRADES: OBSERVED", vm.TradesLine);
        Assert.Equal("PROFILE: NOT READY", vm.ProfileLine);
        Assert.Equal("MBO: BLOCKED", vm.MboLine);
        Assert.Equal(DataState.Degraded, vm.DataState);
        Assert.Empty(vm.DiagnosticRows);
        Assert.DoesNotContain(vm.AllLines(false), l => l.Contains("FAR", StringComparison.Ordinal));
        Assert.DoesNotContain(vm.AllLines(false), l => l.Contains("AAC", StringComparison.Ordinal));
        Assert.DoesNotContain(vm.AllLines(false), l => l.Contains("ENTRY", StringComparison.Ordinal));
        Assert.DoesNotContain(vm.AllLines(false), l => l.Contains("TARGET", StringComparison.Ordinal));
    }

    [Fact]
    public void GpsCard_invalid_state_distinguishable_and_diagnostics_toggle()
    {
        var engine = new GcaeRuntimeEngine(Config);
        var snap = engine.Publish(
            Gcq6(), "GCU6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            tradeObserved: true, lastTradeCallbackUtc: Now,
            rawRecorderMasterEnabled: false, tradeRecordingEnabled: true,
            recorderAccepting: false, recorderFaulted: false, recorderSessionPresent: false,
            indicatorDisposed: false, timestampUtc: Now);

        var vm = AuctionGpsCardMapper.FromSnapshot(snap, showDiagnostics: true);
        Assert.Equal(DataState.Invalid, vm.DataState);
        Assert.Equal("DATA: INVALID", vm.DataLine);
        Assert.Equal(15, vm.DiagnosticRows.Count);
        Assert.Contains(vm.DiagnosticRows, r => r.StartsWith("FAR:", StringComparison.Ordinal));
        Assert.Contains(vm.DiagnosticRows, r => r.StartsWith("AAC:", StringComparison.Ordinal));
        Assert.Contains(vm.DiagnosticRows, r => r.StartsWith("ACCEPTANCE/REENTRY EVIDENCE:", StringComparison.Ordinal));
        Assert.Contains(vm.DiagnosticRows, r => r.StartsWith("ORDERFLOW:", StringComparison.Ordinal));
        Assert.Contains(vm.DiagnosticRows, r => r.StartsWith("CLUSTER RAW:", StringComparison.Ordinal));
        Assert.Contains(vm.DiagnosticRows, r => r.StartsWith("AUCTION EFFICIENCY:", StringComparison.Ordinal));
        Assert.Contains(vm.DiagnosticRows, r => r.StartsWith("RESOLUTION:", StringComparison.Ordinal));
        Assert.Contains(vm.DiagnosticRows, r => r.StartsWith("EFFORT RESULT:", StringComparison.Ordinal));
        Assert.Equal(vm.AllLines(false).Count + 15, vm.AllLines(true).Count);
    }

    [Fact]
    public void Transition_ledger_does_not_duplicate_unchanged_state()
    {
        var engine = new GcaeRuntimeEngine(Config);
        engine.Publish(
            Gcq6(), "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, Now, false, true, false, false, false, false, timestampUtc: Now);
        var count1 = engine.Transitions.Count;
        engine.Publish(
            Gcq6(), "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, Now, false, true, false, false, false, false, timestampUtc: Now);
        Assert.Equal(count1, engine.Transitions.Count);
    }

    [Fact]
    public void Renderer_source_has_no_file_io_wait_or_dataseries()
    {
        var root = FindRepoRoot();
        var path = Path.Combine(root, "src", "GC.AuctionFlow", "Atas", "AuctionGpsCardRenderer.cs");
        var src = File.ReadAllText(path);
        Assert.DoesNotContain("File.", src, StringComparison.Ordinal);
        Assert.DoesNotContain("StreamWriter", src, StringComparison.Ordinal);
        Assert.DoesNotContain("Thread.Sleep", src, StringComparison.Ordinal);
        Assert.DoesNotContain("Wait(", src, StringComparison.Ordinal);
        Assert.DoesNotContain("JsonSerializer", src, StringComparison.Ordinal);
        Assert.DoesNotMatch(new System.Text.RegularExpressions.Regex(@"\b(Value|Candle)?DataSeries\b\s*[.=]"), src);
    }

    [Fact]
    public void Indicator_source_enables_gps_card_without_dom_bba_mbo_recording_subscription_for_card()
    {
        var root = FindRepoRoot();
        var path = Path.Combine(root, "src", "GC.AuctionFlow", "Atas", "GcAuctionFlowIndicator.cs");
        var src = File.ReadAllText(path);
        Assert.Contains("EnableAuctionGpsCard = true", src, StringComparison.Ordinal);
        Assert.Contains("EnableCustomDrawing = true", src, StringComparison.Ordinal);
        Assert.Contains("OnRender", src, StringComparison.Ordinal);
        Assert.Contains("PublishRuntimeSnapshot", src, StringComparison.Ordinal);
        Assert.DoesNotContain("EnableDomRecording", src, StringComparison.Ordinal);
        Assert.DoesNotContain("EnableMboRecording", src, StringComparison.Ordinal);
        Assert.Contains("EnableMboLifecycleProbe", src, StringComparison.Ordinal);
    }

    [Fact]
    public void Snapshot_publisher_clear_is_idempotent()
    {
        var engine = new GcaeRuntimeEngine(Config);
        engine.Publish(
            Gcq6(), "GCQ6",
            DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, Now, false, true, false, false, false, false, timestampUtc: Now);
        engine.Stop();
        engine.Stop();
        Assert.Null(engine.Current);
    }

    private static RuntimeCapabilitySnapshot Cap(
        bool tradeObserved = false,
        DataSourceMode mode = DataSourceMode.Live,
        DataSourceModeProvenance modeProv = DataSourceModeProvenance.OperatorDeclared,
        DeclaredFeedProvider provider = DeclaredFeedProvider.Rithmic,
        FeedProviderProvenance providerProv = FeedProviderProvenance.OperatorDeclared) =>
        RuntimeCapabilitySnapshotBuilder.Build(
            mode, modeProv, provider, providerProv,
            instrumentIdentityAvailable: true,
            tradeObserved,
            lastTradeCallbackUtc: tradeObserved ? Now : null,
            rawRecorderMasterEnabled: false,
            tradeRecordingEnabled: true,
            recorderAccepting: false,
            recorderFaulted: false,
            recorderSessionPresent: false);

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "GC.AuctionFlow.sln"))
                || File.Exists(Path.Combine(dir.FullName, "src", "GC.AuctionFlow", "GC.AuctionFlow.csproj")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repo root not found");
    }
}
