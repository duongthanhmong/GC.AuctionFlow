using System.ComponentModel;
using System.Diagnostics;
using ATAS.Indicators;
using GC.AuctionFlow.Core;
using GC.AuctionFlow.Probe;

namespace GC.AuctionFlow.Atas;

/// <summary>
/// GC AuctionFlow Engine — P0-04/P0-04B Trade Stream Probe adapters.
/// IL evidence (ATAS 8.0.14.395): OnNewTrade/OnCumulativeTrade/OnUpdateCumulativeTrade = empty ret;
/// OnNewTrades base = non-trivial (foreach → virtual OnNewTrade).
/// P0-04B policy: OnNewTrades enumerates batch as OnNewTradesBatch and does NOT call base.OnNewTrades
/// (avoids synthetic singular OnNewTrade from base dispatch). Always base.OnDispose() in finally.
/// </summary>
[DisplayName(BuildInfo.VisibleIndicatorName)]
[Category("GC")]
public sealed class GcAuctionFlowIndicator : Indicator
{
    private readonly object _lifecycleGate = new();
    private TradeStreamProbe? _probe;
    private TradeStreamAtasMapper? _mapper;
    private Guid _sessionId;
    private int _disposed;
    private bool _instrumentCaptured;

    public GcAuctionFlowIndicator()
    {
        DenyToChangePanel = true;
        EnableCustomDrawing = false;
        EnableTradeStreamProbe = false;
        DeclaredDataSourceMode = DataSourceMode.Unknown;
        DataSourceModeProvenance = DataSourceModeProvenance.Unknown;
        ExpectedInstrumentCode = "";
    }

    [Category("Trade Stream Probe")]
    [DisplayName("Enable Trade Stream Probe")]
    [Description("Authorize trade-stream capture. Requires resolved mode, provenance, and ExpectedInstrumentCode.")]
    public bool EnableTradeStreamProbe { get; set; }

    [Category("Trade Stream Probe")]
    [DisplayName("Declared Data Source Mode")]
    public DataSourceMode DeclaredDataSourceMode { get; set; }

    [Category("Trade Stream Probe")]
    [DisplayName("Data Source Mode Provenance")]
    public DataSourceModeProvenance DataSourceModeProvenance { get; set; }

    [Category("Trade Stream Probe")]
    [DisplayName("Expected Instrument Code")]
    [Description("First operator run requires explicit GC contract code (e.g. GC specific code).")]
    public string ExpectedInstrumentCode { get; set; }

    /// <summary>Required abstract override — no market logic beyond probe wiring.</summary>
    protected override void OnCalculate(int bar, decimal value)
    {
        EnsureProbeStarted();
        TryCaptureInstrument();
    }

    protected override void OnNewTrade(MarketDataArg trade)
    {
        // Singular platform callback only. IL: empty base — no base call.
        // Do not treat batch items as singular (P0-04B: base.OnNewTrades is not invoked).
        EnsureProbeStarted();
        TryCaptureInstrument();
        var probe = _probe;
        var mapper = _mapper;
        if (probe is null || mapper is null || trade is null)
            return;

        try
        {
            var key = probe.GetObservedInstrument()?.IdentityKey ?? "Unknown";
            var obs = mapper.MapNewTrade(trade, TradeCallbackSource.OnNewTrade, probe.NextSequence(), key);
            probe.TryEnqueueNewTrade(
                obs,
                EnableTradeStreamProbe,
                DeclaredDataSourceMode,
                DataSourceModeProvenance,
                ExpectedInstrumentCode);
        }
        catch
        {
            probe.Counters.IncNormalizationFailures();
        }
    }

    protected override void OnNewTrades(IEnumerable<MarketDataArg> trades)
    {
        // P0-04B: enumerate and normalize as OnNewTradesBatch. Do NOT call base.OnNewTrades
        // (base would invoke virtual OnNewTrade per item and synthesize singular observations).
        EnsureProbeStarted();
        TryCaptureInstrument();
        var probe = _probe;
        var mapper = _mapper;
        if (probe is null || mapper is null || trades is null)
            return;

        try
        {
            var key = probe.GetObservedInstrument()?.IdentityKey ?? "Unknown";
            probe.Counters.IncCallback(TradeCallbackSource.OnNewTradesBatch);

            foreach (var trade in trades)
            {
                if (trade is null)
                    continue;

                if (!probe.IsAccepting)
                {
                    probe.Counters.IncRejectedAfterDispose();
                    continue;
                }

                var gate = probe.EvaluateGates(
                    EnableTradeStreamProbe,
                    DeclaredDataSourceMode,
                    DataSourceModeProvenance,
                    ExpectedInstrumentCode);
                if (!gate.Accepted)
                {
                    if (gate.InstrumentGate)
                        probe.Counters.IncRejectedByInstrumentGate();
                    else
                        probe.Counters.IncRejectedByModeGate();
                    continue;
                }

                var obs = mapper.MapNewTrade(
                    trade, TradeCallbackSource.OnNewTradesBatch, probe.NextSequence(), key);
                probe.TryEnqueueNewTradeAlreadyCounted(obs);
            }
        }
        catch
        {
            probe.Counters.IncNormalizationFailures();
        }
    }

    protected override void OnCumulativeTrade(CumulativeTrade trade)
    {
        // IL: empty base — no base call required.
        EnsureProbeStarted();
        TryCaptureInstrument();
        var probe = _probe;
        var mapper = _mapper;
        if (probe is null || mapper is null || trade is null)
            return;

        try
        {
            var key = probe.GetObservedInstrument()?.IdentityKey ?? "Unknown";
            var obs = mapper.MapCumulative(
                trade, TradeCallbackSource.OnCumulativeTrade, probe.NextSequence(), key, assignInstanceId: true);
            probe.TryEnqueueCumulative(
                obs,
                EnableTradeStreamProbe,
                DeclaredDataSourceMode,
                DataSourceModeProvenance,
                ExpectedInstrumentCode);
        }
        catch
        {
            probe.Counters.IncNormalizationFailures();
        }
    }

    protected override void OnUpdateCumulativeTrade(CumulativeTrade trade)
    {
        // IL: empty base — no base call required.
        // Must never increment new-execution count (handled in worker by source).
        EnsureProbeStarted();
        TryCaptureInstrument();
        var probe = _probe;
        var mapper = _mapper;
        if (probe is null || mapper is null || trade is null)
            return;

        try
        {
            var key = probe.GetObservedInstrument()?.IdentityKey ?? "Unknown";
            var obs = mapper.MapCumulative(
                trade, TradeCallbackSource.OnUpdateCumulativeTrade, probe.NextSequence(), key, assignInstanceId: false);
            probe.TryEnqueueCumulative(
                obs,
                EnableTradeStreamProbe,
                DeclaredDataSourceMode,
                DataSourceModeProvenance,
                ExpectedInstrumentCode);
        }
        catch
        {
            probe.Counters.IncNormalizationFailures();
        }
    }

    protected override void OnDispose()
    {
        try
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            TradeStreamProbe? probe;
            lock (_lifecycleGate)
            {
                probe = _probe;
                _probe = null;
                _mapper = null;
            }

            if (probe is null)
                return;

            probe.StopAccepting();
            var drained = probe.Drain(TimeSpan.FromMilliseconds(probe.Config.DrainTimeoutMilliseconds));
            if (!drained)
            {
                probe.RecordLimitation("WorkerDrainTimeout");
                probe.RecordIntegrityEvent("WorkerDrainTimeout");
            }

            var snapshot = probe.FreezeSnapshot(
                DeclaredDataSourceMode,
                DataSourceModeProvenance,
                ExpectedInstrumentCode,
                _sessionId,
                EnableTradeStreamProbe);

            try
            {
                TradeStreamProbeArtifactWriter.WriteAtomic(snapshot);
            }
            catch (Exception ex)
            {
                probe.RecordIntegrityEvent($"ArtifactExportFailure:{ex.GetType().Name}");
                probe.RecordLimitation($"Artifact export failed: {ex.GetType().Name}");
                // Best-effort: freeze again not needed; failure recorded on probe before dispose.
                Debug.WriteLine($"TradeStreamProbe artifact export failed: {ex.Message}");
            }

            probe.Dispose();
        }
        catch
        {
            // No exception may escape OnDispose.
        }
        finally
        {
            // IL: BaseIndicator.OnDispose empty, but policy requires always call in finally.
            try { base.OnDispose(); } catch { /* ignore */ }
        }
    }

    private void EnsureProbeStarted()
    {
        if (_probe is not null)
            return;

        lock (_lifecycleGate)
        {
            if (_probe is not null)
                return;
            _sessionId = Guid.NewGuid();
            var config = new TradeStreamProbeConfig();
            _probe = new TradeStreamProbe(config);
            _mapper = new TradeStreamAtasMapper(config);
        }
    }

    private void TryCaptureInstrument()
    {
        if (_instrumentCaptured)
            return;
        var probe = _probe;
        if (probe is null)
            return;

        try
        {
            IInstrumentInfo? info = null;
            try { info = InstrumentInfo; } catch { /* CẦN XÁC MINH TRÊN ATAS THẬT if missing */ }

            object? security = null;
            try { security = TradingManager?.Security; } catch { /* missing */ }

            var snap = TradeStreamAtasMapper.CaptureInstrument(info, security);
            probe.SetObservedInstrument(snap);
            if (!string.Equals(snap.IdentityKey, "Unknown", StringComparison.Ordinal))
                _instrumentCaptured = true;
        }
        catch
        {
            // leave Unknown
        }
    }
}
