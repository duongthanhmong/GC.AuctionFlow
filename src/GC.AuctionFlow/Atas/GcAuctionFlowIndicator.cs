using System.ComponentModel;
using System.Diagnostics;
using ATAS.Indicators;
using GC.AuctionFlow.Core;
using GC.AuctionFlow.Probe;

namespace GC.AuctionFlow.Atas;

/// <summary>
/// GC AuctionFlow Engine — Phase 0 probes (trade P0-04 + DOM semantics P0-05).
/// IL (ATAS 8.0.14.395): MarketDepthsChanged HAS WORK (foreach → MarketDepthChanged);
/// MarketDepthChanged / OnBestBidAskChanged EMPTY_RET.
/// P0-05 policy: do NOT call base.MarketDepthsChanged. Always base.OnDispose() in finally.
/// </summary>
[DisplayName(BuildInfo.VisibleIndicatorName)]
[Category("GC")]
public sealed class GcAuctionFlowIndicator : Indicator
{
    private readonly object _lifecycleGate = new();
    private TradeStreamProbe? _tradeProbe;
    private TradeStreamAtasMapper? _tradeMapper;
    private DomSemanticsProbe? _domProbe;
    private Guid _sessionId;
    private int _disposed;
    private bool _instrumentCaptured;

    public GcAuctionFlowIndicator()
    {
        DenyToChangePanel = true;
        EnableCustomDrawing = false;
        EnableTradeStreamProbe = false;
        EnableDomSemanticsProbe = false;
        DeclaredDataSourceMode = DataSourceMode.Unknown;
        DataSourceModeProvenance = DataSourceModeProvenance.Unknown;
        ExpectedInstrumentCode = "";
        DeclaredFeedProvider = DeclaredFeedProvider.Unknown;
        FeedProviderProvenance = FeedProviderProvenance.Unknown;
    }

    [Category("Shared Gates")]
    [DisplayName("Declared Data Source Mode")]
    public DataSourceMode DeclaredDataSourceMode { get; set; }

    [Category("Shared Gates")]
    [DisplayName("Data Source Mode Provenance")]
    public DataSourceModeProvenance DataSourceModeProvenance { get; set; }

    [Category("Shared Gates")]
    [DisplayName("Expected Instrument Code")]
    public string ExpectedInstrumentCode { get; set; }

    [Category("Trade Stream Probe")]
    [DisplayName("Enable Trade Stream Probe")]
    public bool EnableTradeStreamProbe { get; set; }

    [Category("DOM Semantics Probe")]
    [DisplayName("Enable DOM Semantics Probe")]
    public bool EnableDomSemanticsProbe { get; set; }

    [Category("DOM Semantics Probe")]
    [DisplayName("Declared Feed Provider")]
    [Description("First GC run: Rithmic with OperatorDeclared provenance.")]
    public DeclaredFeedProvider DeclaredFeedProvider { get; set; }

    [Category("DOM Semantics Probe")]
    [DisplayName("Feed Provider Provenance")]
    public FeedProviderProvenance FeedProviderProvenance { get; set; }

    protected override void OnCalculate(int bar, decimal value)
    {
        EnsureProbesStarted();
        TryCaptureInstrument();
        TryExecuteDeferredSnapshotPull();
    }

    protected override void OnNewTrade(MarketDataArg trade)
    {
        EnsureProbesStarted();
        TryCaptureInstrument();
        var probe = _tradeProbe;
        var mapper = _tradeMapper;
        if (probe is null || mapper is null || trade is null) return;
        try
        {
            var key = probe.GetObservedInstrument()?.IdentityKey ?? "Unknown";
            var obs = mapper.MapNewTrade(trade, TradeCallbackSource.OnNewTrade, probe.NextSequence(), key);
            probe.TryEnqueueNewTrade(obs, EnableTradeStreamProbe, DeclaredDataSourceMode, DataSourceModeProvenance, ExpectedInstrumentCode);
        }
        catch { probe.Counters.IncNormalizationFailures(); }
    }

    protected override void OnNewTrades(IEnumerable<MarketDataArg> trades)
    {
        EnsureProbesStarted();
        TryCaptureInstrument();
        var probe = _tradeProbe;
        var mapper = _tradeMapper;
        if (probe is null || mapper is null || trades is null) return;
        try
        {
            var key = probe.GetObservedInstrument()?.IdentityKey ?? "Unknown";
            probe.Counters.IncCallback(TradeCallbackSource.OnNewTradesBatch);
            foreach (var trade in trades)
            {
                if (trade is null) continue;
                if (!probe.IsAccepting) { probe.Counters.IncRejectedAfterDispose(); continue; }
                var gate = probe.EvaluateGates(EnableTradeStreamProbe, DeclaredDataSourceMode, DataSourceModeProvenance, ExpectedInstrumentCode);
                if (!gate.Accepted)
                {
                    if (gate.InstrumentGate) probe.Counters.IncRejectedByInstrumentGate();
                    else probe.Counters.IncRejectedByModeGate();
                    continue;
                }
                var obs = mapper.MapNewTrade(trade, TradeCallbackSource.OnNewTradesBatch, probe.NextSequence(), key);
                probe.TryEnqueueNewTradeAlreadyCounted(obs);
            }
        }
        catch { probe.Counters.IncNormalizationFailures(); }
        // P0-04B: do not call base.OnNewTrades
    }

    protected override void OnCumulativeTrade(CumulativeTrade trade)
    {
        EnsureProbesStarted();
        TryCaptureInstrument();
        var probe = _tradeProbe;
        var mapper = _tradeMapper;
        if (probe is null || mapper is null || trade is null) return;
        try
        {
            var key = probe.GetObservedInstrument()?.IdentityKey ?? "Unknown";
            var obs = mapper.MapCumulative(trade, TradeCallbackSource.OnCumulativeTrade, probe.NextSequence(), key, true);
            probe.TryEnqueueCumulative(obs, EnableTradeStreamProbe, DeclaredDataSourceMode, DataSourceModeProvenance, ExpectedInstrumentCode);
        }
        catch { probe.Counters.IncNormalizationFailures(); }
    }

    protected override void OnUpdateCumulativeTrade(CumulativeTrade trade)
    {
        EnsureProbesStarted();
        TryCaptureInstrument();
        var probe = _tradeProbe;
        var mapper = _tradeMapper;
        if (probe is null || mapper is null || trade is null) return;
        try
        {
            var key = probe.GetObservedInstrument()?.IdentityKey ?? "Unknown";
            var obs = mapper.MapCumulative(trade, TradeCallbackSource.OnUpdateCumulativeTrade, probe.NextSequence(), key, false);
            probe.TryEnqueueCumulative(obs, EnableTradeStreamProbe, DeclaredDataSourceMode, DataSourceModeProvenance, ExpectedInstrumentCode);
        }
        catch { probe.Counters.IncNormalizationFailures(); }
    }

    protected override void MarketDepthChanged(MarketDataArg depth)
    {
        // Singular platform callback only. IL: empty base — no base call.
        EnsureProbesStarted();
        TryCaptureInstrument();
        var probe = _domProbe;
        if (probe is null || depth is null) return;
        try
        {
            var key = probe.GetObservedInstrument()?.IdentityKey ?? "Unknown";
            var (obs, detail) = DepthAtasMapper.Map(depth, DepthCallbackSource.MarketDepthChanged, probe.NextSequence(), key);
            var accepted = probe.TryEnqueue(
                obs, detail, EnableDomSemanticsProbe, DeclaredDataSourceMode, DataSourceModeProvenance,
                ExpectedInstrumentCode, DeclaredFeedProvider, FeedProviderProvenance);
            if (accepted)
                probe.TryRequestSnapshotPull();
        }
        catch { probe.Counters.IncNormalizationFailures(); }
    }

    protected override void MarketDepthsChanged(IEnumerable<MarketDataArg> depths)
    {
        // P0-05: enumerate as MarketDepthsBatch. Do NOT call base.MarketDepthsChanged
        // (base foreach invokes virtual MarketDepthChanged per IL evidence).
        EnsureProbesStarted();
        TryCaptureInstrument();
        var probe = _domProbe;
        if (probe is null || depths is null) return;
        try
        {
            var key = probe.GetObservedInstrument()?.IdentityKey ?? "Unknown";
            probe.Counters.IncCallback(DepthCallbackSource.MarketDepthsBatch);
            var anyAccepted = false;
            foreach (var depth in depths)
            {
                if (depth is null) continue;
                if (!probe.IsAccepting) { probe.Counters.IncRejectedAfterDispose(); continue; }
                var gate = probe.EvaluateGates(
                    EnableDomSemanticsProbe, DeclaredDataSourceMode, DataSourceModeProvenance,
                    ExpectedInstrumentCode, DeclaredFeedProvider, FeedProviderProvenance);
                if (!gate.Accepted)
                {
                    if (gate.InstrumentGate) probe.Counters.IncRejectedByInstrumentGate();
                    else probe.Counters.IncRejectedByModeGate();
                    continue;
                }

                var (obs, detail) = DepthAtasMapper.Map(depth, DepthCallbackSource.MarketDepthsBatch, probe.NextSequence(), key);
                if (probe.TryEnqueueAlreadyCounted(obs, detail))
                    anyAccepted = true;
            }

            if (anyAccepted)
                probe.TryRequestSnapshotPull();
        }
        catch { probe.Counters.IncNormalizationFailures(); }
    }

    protected override void OnBestBidAskChanged(MarketDataArg depth)
    {
        // IL: empty base — no base call.
        EnsureProbesStarted();
        TryCaptureInstrument();
        var probe = _domProbe;
        if (probe is null || depth is null) return;
        try
        {
            var key = probe.GetObservedInstrument()?.IdentityKey ?? "Unknown";
            var (obs, detail) = DepthAtasMapper.Map(depth, DepthCallbackSource.BestBidAskChanged, probe.NextSequence(), key);
            probe.TryEnqueue(
                obs, detail, EnableDomSemanticsProbe, DeclaredDataSourceMode, DataSourceModeProvenance,
                ExpectedInstrumentCode, DeclaredFeedProvider, FeedProviderProvenance);
        }
        catch { probe.Counters.IncNormalizationFailures(); }
    }

    protected override void OnDispose()
    {
        try
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            TradeStreamProbe? trade;
            DomSemanticsProbe? dom;
            lock (_lifecycleGate)
            {
                trade = _tradeProbe;
                dom = _domProbe;
                _tradeProbe = null;
                _tradeMapper = null;
                _domProbe = null;
            }

            DisposeTrade(trade);
            DisposeDom(dom);
        }
        catch { }
        finally
        {
            try { base.OnDispose(); } catch { }
        }
    }

    private void DisposeTrade(TradeStreamProbe? probe)
    {
        if (probe is null) return;
        try
        {
            probe.StopAccepting();
            if (!probe.Drain(TimeSpan.FromMilliseconds(probe.Config.DrainTimeoutMilliseconds)))
            {
                probe.RecordLimitation("WorkerDrainTimeout");
                probe.RecordIntegrityEvent("WorkerDrainTimeout");
            }

            var snapshot = probe.FreezeSnapshot(
                DeclaredDataSourceMode, DataSourceModeProvenance, ExpectedInstrumentCode, _sessionId, EnableTradeStreamProbe);
            try { TradeStreamProbeArtifactWriter.WriteAtomic(snapshot); }
            catch (Exception ex)
            {
                probe.RecordIntegrityEvent("ArtifactExportFailure:" + ex.GetType().Name);
            }

            probe.Dispose();
        }
        catch { }
    }

    private void DisposeDom(DomSemanticsProbe? probe)
    {
        if (probe is null) return;
        try
        {
            probe.RecordLifecycle(DepthLifecycleMarker.Removed);
            probe.StopAccepting();
            if (!probe.Drain(TimeSpan.FromMilliseconds(probe.Config.DrainTimeoutMilliseconds)))
            {
                probe.RecordLimitation("WorkerDrainTimeout");
                probe.RecordIntegrity("WorkerDrainTimeout");
            }

            var snapshot = probe.FreezeSnapshot(
                DeclaredDataSourceMode, DataSourceModeProvenance, ExpectedInstrumentCode,
                DeclaredFeedProvider, FeedProviderProvenance, _sessionId, EnableDomSemanticsProbe);
            try { DomSemanticsProbeArtifactWriter.WriteAtomic(snapshot); }
            catch (Exception ex)
            {
                probe.RecordIntegrity("ArtifactExportFailure:" + ex.GetType().Name);
            }

            probe.Dispose();
        }
        catch { }
    }

    private void EnsureProbesStarted()
    {
        if (_tradeProbe is not null && _domProbe is not null) return;
        lock (_lifecycleGate)
        {
            if (_sessionId == Guid.Empty)
                _sessionId = Guid.NewGuid();
            if (_tradeProbe is null)
            {
                var cfg = new TradeStreamProbeConfig();
                _tradeProbe = new TradeStreamProbe(cfg);
                _tradeMapper = new TradeStreamAtasMapper(cfg);
            }

            if (_domProbe is null)
                _domProbe = new DomSemanticsProbe(new DomSemanticsProbeConfig());
        }
    }

    private void TryCaptureInstrument()
    {
        if (_instrumentCaptured) return;
        try
        {
            IInstrumentInfo? info = null;
            try { info = InstrumentInfo; } catch { }
            object? security = null;
            try { security = TradingManager?.Security; } catch { }
            var snap = TradeStreamAtasMapper.CaptureInstrument(info, security);
            _tradeProbe?.SetObservedInstrument(snap);
            _domProbe?.SetObservedInstrument(snap);
            if (!string.Equals(snap.IdentityKey, "Unknown", StringComparison.Ordinal))
                _instrumentCaptured = true;
        }
        catch { }
    }

    private void TryExecuteDeferredSnapshotPull()
    {
        var probe = _domProbe;
        if (probe is null || !EnableDomSemanticsProbe) return;
        if (!probe.TryConsumeSnapshotPullRequest()) return;

        try
        {
            IEnumerable<MarketDataArg>? snapEnum = null;
            try
            {
                var info = MarketDepthInfo;
                if (info is null)
                {
                    probe.RecordLimitation("MarketDepthInfoNull");
                    probe.ApplySnapshotPullResult(Array.Empty<DepthObservation>(), DateTime.UtcNow);
                    return;
                }

                snapEnum = info.GetMarketDepthSnapshot();
            }
            catch (Exception ex)
            {
                probe.RecordLimitation("GetMarketDepthSnapshotFailed:" + ex.GetType().Name);
                probe.ApplySnapshotPullResult(Array.Empty<DepthObservation>(), DateTime.UtcNow);
                return;
            }

            var key = probe.GetObservedInstrument()?.IdentityKey ?? "Unknown";
            var list = new List<DepthObservation>();
            if (snapEnum is not null)
            {
                foreach (var d in snapEnum)
                {
                    if (d is null) continue;
                    var (obs, _) = DepthAtasMapper.Map(d, DepthCallbackSource.SnapshotPull, probe.NextSequence(), key);
                    list.Add(obs);
                }
            }

            probe.ApplySnapshotPullResult(list, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            probe.RecordIntegrity("SnapshotPullFailure:" + ex.GetType().Name);
            probe.MarkSnapshotPullNotExecuted();
        }
    }
}