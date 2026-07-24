using System.ComponentModel;
using ATAS.DataFeedsCore;
using ATAS.Indicators;
using GC.AuctionFlow.Composite;
using GC.AuctionFlow.Core;
using GC.AuctionFlow.Directional;
using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Recorder;
using GC.AuctionFlow.Reference;
using GC.AuctionFlow.Runtime;
using GC.AuctionFlow.UI;
using OFT.Rendering.Context;

namespace GC.AuctionFlow.Atas;

/// <summary>
/// GC AuctionFlow Engine — Phase 0 probes/recorder + P0-08A DataGate/GPS + Phase 1A Primary TPO/VP.
/// P0-06C Decision B: OnCalculate must never write this[bar]/DataSeries.
/// Always base.OnDispose() in finally.
/// </summary>
[DisplayName(BuildInfo.VisibleIndicatorName)]
[Category("GC")]
public sealed class GcAuctionFlowIndicator : Indicator
{
    private readonly object _lifecycleGate = new();
    private TradeStreamProbe? _tradeProbe;
    private TradeStreamAtasMapper? _tradeMapper;
    private DomSemanticsProbe? _domProbe;
    private MboLifecycleProbe? _mboProbe;
    private TradeRecorderHost? _tradeRecorder;
    private GcaeRuntimeEngine? _runtime;
    private AuctionGpsCardRenderer? _gpsRenderer;
    private PrimaryProfileOverlayRenderer? _overlayRenderer;
    private PrimaryProfileHost? _profileHost;
    private CompositeProfileHost? _compositeHost;
    /// <summary>Last operator fingerprint successfully applied to the published Composite snapshot.</summary>
    private CompositeOperatorConfiguration? _lastAppliedCompositeConfiguration;
    private StructuralReferenceHost? _referenceHost;
    /// <summary>Last input fingerprint successfully applied to the published Structural Reference snapshot.</summary>
    private ReferenceInputFingerprint? _lastAppliedReferenceFingerprint;
    private DirectionalContextHost? _directionalHost;
    /// <summary>Last input fingerprint successfully applied to the published Directional Context snapshot.</summary>
    private DirectionalInputFingerprint? _lastAppliedDirectionalFingerprint;
    private AuctionEpisodeHost? _episodeHost;
    private EpisodeInputFingerprint? _lastAppliedEpisodeFingerprint;
    private Guid _sessionId;
    private int _disposed;
    private bool _instrumentCaptured;
    private bool _tradeObserved;
    private DateTime? _lastTradeCallbackUtc;
    private bool _drawingSubscribed;
    private long _barSourceVersion;
    private string _profileSettingsKey = "";

    public GcAuctionFlowIndicator()
    {
        DenyToChangePanel = true;
        EnableCustomDrawing = true;
        EnableTradeStreamProbe = false;
        EnableDomSemanticsProbe = false;
        EnableMboLifecycleProbe = false;
        EnableRawEventRecorder = false;
        EnableTradeRecording = true;
        EnableAuctionGpsCard = true;
        ShowAuctionGpsDiagnostics = false;
        EnablePrimaryProfile = true;
        EnablePrimaryProfileOverlay = true;
        ShowPreviousProfileLevels = true;
        EnableTpoParityDiagnostics = false;
        EnableTpoParityReferencePrice = false;
        TpoParityReferencePrice = 0m;
        EnableCompositeProfile = false;
        CompositeAnchorAuctionId = "";
        IncludeThroughLatestCompletedAuction = true;
        CompositeExcludedAuctionIds = "";
        EnableDevelopingCompositePreview = false;
        EnableCompositeOverlay = true;
        ShowCompositeDiagnostics = false;
        EnableShadowCompositeEvidence = false;
        EnableStructuralReferences = false;
        EnableStructuralReferenceOverlay = true;
        ShowStructuralReferenceDiagnostics = false;
        EnableDirectionalContext = false;
        ShowDirectionalContextDiagnostics = false;
        EnableOneTimeFraming = true;
        EnableAuctionEpisodes = false;
        ShowAuctionEpisodeDiagnostics = false;
        TpoPeriodMinutes = PrimaryAuctionClockConfig.DefaultPeriodMinutes;
        ValueAreaFraction = PrimaryAuctionClockConfig.DefaultValueAreaFraction;
        AnchorHourLocal = 8;
        AnchorMinuteLocal = 20;
        ExpectedTickSize = RuntimeGateConfig.DefaultExpectedTickSize;
        NearExpirationCalendarDays = RuntimeGateConfig.DefaultNearExpirationCalendarDays;
        GpsCardMarginX = 12;
        GpsCardMarginY = 12;
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

    [Category("Shared Gates")]
    [DisplayName("Expected Tick Size")]
    [Description("GC seed value, subject to sensitivity test. Default 0.1.")]
    public decimal ExpectedTickSize { get; set; }

    [Category("Shared Gates")]
    [DisplayName("Near Expiration Calendar Days")]
    [Description("Seed value, subject to sensitivity test.")]
    public int NearExpirationCalendarDays { get; set; }

    [Category("Trade Stream Probe")]
    [DisplayName("Enable Trade Stream Probe")]
    public bool EnableTradeStreamProbe { get; set; }

    [Category("DOM Semantics Probe")]
    [DisplayName("Enable DOM Semantics Probe")]
    public bool EnableDomSemanticsProbe { get; set; }

    [Category("MBO Lifecycle Probe")]
    [DisplayName("Enable MBO Lifecycle Probe")]
    [Description("First GC run: true with Live/OperatorDeclared, ExpectedInstrumentCode=GCQ6, Rithmic/OperatorDeclared.")]
    public bool EnableMboLifecycleProbe { get; set; }

    [Category("Raw Event Recorder")]
    [DisplayName("Enable Raw Event Recorder")]
    [Description("Master switch. Default false. Trade recording requires this true.")]
    public bool EnableRawEventRecorder { get; set; }

    [Category("Raw Event Recorder")]
    [DisplayName("Enable Trade Recording")]
    [Description("Effective only when Enable Raw Event Recorder is true. Default true.")]
    public bool EnableTradeRecording { get; set; }

    [Category("Shared Feed Declaration")]
    [DisplayName("Declared Feed Provider")]
    [Description("First GC run: Rithmic with OperatorDeclared provenance.")]
    public DeclaredFeedProvider DeclaredFeedProvider { get; set; }

    [Category("Shared Feed Declaration")]
    [DisplayName("Feed Provider Provenance")]
    public FeedProviderProvenance FeedProviderProvenance { get; set; }

    [Category("Auction GPS Card")]
    [DisplayName("Enable Auction GPS Card")]
    public bool EnableAuctionGpsCard { get; set; }

    [Category("Auction GPS Card")]
    [DisplayName("Show Auction GPS Diagnostics")]
    [Description("When true, shows reserved NOT AVAILABLE rows (Tactical/Location/Episode/Thesis).")]
    public bool ShowAuctionGpsDiagnostics { get; set; }

    [Category("Auction GPS Card")]
    [DisplayName("GPS Card Margin X")]
    public int GpsCardMarginX { get; set; }

    [Category("Auction GPS Card")]
    [DisplayName("GPS Card Margin Y")]
    public int GpsCardMarginY { get; set; }

    [Category("Primary Profile")]
    [DisplayName("Enable Primary Profile")]
    public bool EnablePrimaryProfile { get; set; }

    [Category("Primary Profile")]
    [DisplayName("Enable Primary Profile Overlay")]
    public bool EnablePrimaryProfileOverlay { get; set; }

    [Category("Primary Profile")]
    [DisplayName("Show Previous Profile Levels")]
    public bool ShowPreviousProfileLevels { get; set; }

    [Category("Primary Profile")]
    [DisplayName("Enable TPO Parity Diagnostics")]
    [Description("When true, GPS card shows bounded Classic TPO parity diagnostic rows. Default false. No file I/O.")]
    public bool EnableTpoParityDiagnostics { get; set; }

    [Category("Primary Profile")]
    [DisplayName("Enable TPO Parity Reference Price")]
    [Description("When true, uses TPO Parity Reference Price for diagnostics only. Default false.")]
    public bool EnableTpoParityReferencePrice { get; set; }

    [Category("Primary Profile")]
    [DisplayName("TPO Parity Reference Price")]
    [Description("Diagnostics-only ATAS TPO POC reference (e.g. 4130.2). Used only when Enable TPO Parity Reference Price is true. Does not affect POC, VA, or rendering.")]
    public decimal TpoParityReferencePrice { get; set; }

    [Category("Primary Profile")]
    [DisplayName("TPO Period Minutes")]
    [Description("Production default 30. Seed value, subject to sensitivity test.")]
    public int TpoPeriodMinutes { get; set; }

    [Category("Primary Profile")]
    [DisplayName("Value Area Fraction")]
    [Description("Conventional configurable default 0.70 — not a predictive GC edge.")]
    public decimal ValueAreaFraction { get; set; }

    [Category("Primary Profile")]
    [DisplayName("Anchor Hour Local (ET)")]
    public int AnchorHourLocal { get; set; }

    [Category("Primary Profile")]
    [DisplayName("Anchor Minute Local (ET)")]
    public int AnchorMinuteLocal { get; set; }

    [Category("Composite Profile")]
    [DisplayName("Enable Composite Profile")]
    [Description("OperatorAnchored confirmed composite. Default false. No hard N-day merge.")]
    public bool EnableCompositeProfile { get; set; }

    [Category("Composite Profile")]
    [DisplayName("Composite Anchor Auction Id")]
    [Description("e.g. PI-2026-07-20. Empty = COMPOSITE AWAITING ANCHOR.")]
    public string CompositeAnchorAuctionId { get; set; }

    [Category("Composite Profile")]
    [DisplayName("Include Through Latest Completed")]
    public bool IncludeThroughLatestCompletedAuction { get; set; }

    [Category("Composite Profile")]
    [DisplayName("Excluded Auction Ids")]
    [Description("Comma-separated AuctionIds excluded from confirmed composite.")]
    public string CompositeExcludedAuctionIds { get; set; }

    [Category("Composite Profile")]
    [DisplayName("Enable Developing Composite Preview")]
    [Description("Preview only — does not mutate confirmed composite.")]
    public bool EnableDevelopingCompositePreview { get; set; }

    [Category("Composite Profile")]
    [DisplayName("Enable Composite Overlay")]
    public bool EnableCompositeOverlay { get; set; }

    [Category("Composite Profile")]
    [DisplayName("Show Composite Diagnostics")]
    public bool ShowCompositeDiagnostics { get; set; }

    [Category("Composite Profile")]
    [DisplayName("Enable Shadow Composite Evidence")]
    [Description("Research/Shadow-only. Without calibrated thresholds → NOT CALIBRATED. Never mutates confirmed.")]
    public bool EnableShadowCompositeEvidence { get; set; }

    [Category("Structural References")]
    [DisplayName("Enable Structural References")]
    [Description("Profile-derived Structural References (REFERENCE_POLICY_V1). Default false. No score/tolerance/Episode.")]
    public bool EnableStructuralReferences { get; set; }

    [Category("Structural References")]
    [DisplayName("Enable Structural Reference Overlay")]
    [Description("Effective only when Structural References enabled. Default true.")]
    public bool EnableStructuralReferenceOverlay { get; set; }

    [Category("Structural References")]
    [DisplayName("Show Structural Reference Diagnostics")]
    [Description("Nearest above/below and registry diagnostics. Default false. No Long/Short/Episode/Thesis.")]
    public bool ShowStructuralReferenceDiagnostics { get; set; }

    [Category("Directional Auction Context")]
    [DisplayName("Enable Directional Context")]
    [Description("Multi-horizon descriptive Directional Context (DIRECTIONAL_CONTEXT_POLICY_V1). Default false. No Long/Short/score.")]
    public bool EnableDirectionalContext { get; set; }

    [Category("Directional Auction Context")]
    [DisplayName("Show Directional Context Diagnostics")]
    [Description("Evidence, conflicts, limitations, fingerprints. Default false. No Buy/Sell/Thesis.")]
    public bool ShowDirectionalContextDiagnostics { get; set; }

    [Category("Directional Auction Context")]
    [DisplayName("Enable One-Time Framing")]
    [Description("Completed TPO period OTF evidence when Directional Context enabled. Default true. Confirmed OTF reserved.")]
    public bool EnableOneTimeFraming { get; set; }

    [Category("Auction Episodes")]
    [DisplayName("Enable Auction Episodes")]
    [Description("Geometric Reference Excursion observation (AUCTION_EPISODE_POLICY_V1). Default false. No Acceptance/Sweep/Long-Short.")]
    public bool EnableAuctionEpisodes { get; set; }

    [Category("Auction Episodes")]
    [DisplayName("Show Auction Episode Diagnostics")]
    [Description("Episode metrics and limitations. Default false. No Acceptance/FAR/AAC/alerts.")]
    public bool ShowAuctionEpisodeDiagnostics { get; set; }

    protected override void OnCalculate(int bar, decimal value)
    {
        EnsureProbesStarted();
        EnsureRuntimeStarted();
        TryCaptureInstrument();
        TryCompleteRecorderStartup();
        TrySubscribeMboOnce();
        TryExecuteDeferredSnapshotPull();
        ProcessProfileBar(bar);
        if (bar >= CurrentBar)
        {
            ProcessComposite();
            ProcessStructuralReferences();
            ProcessDirectionalContext();
            ProcessAuctionEpisodes();
            PublishRuntimeSnapshot();
        }
    }

    protected override void OnRender(RenderContext context, DrawingLayouts drawingLayouts)
    {
        try
        {
            if (EnableAuctionGpsCard)
            {
                _gpsRenderer?.SetMargins(GpsCardMarginX, GpsCardMarginY);
                _gpsRenderer?.Render(context, drawingLayouts);
            }

            if (EnablePrimaryProfileOverlay)
                _overlayRenderer?.Render(context, drawingLayouts, ChartInfo);
        }
        catch
        {
            // Contained.
        }
    }

    protected override void OnNewTrade(MarketDataArg trade)
    {
        EnsureProbesStarted();
        EnsureRuntimeStarted();
        TryCaptureInstrument();
        TrySubscribeMboOnce();
        NoteTradeCallback();
        var probe = _tradeProbe;
        var mapper = _tradeMapper;
        if (probe is null || mapper is null || trade is null) return;
        try
        {
            var key = probe.GetObservedInstrument()?.IdentityKey ?? "Unknown";
            var obs = mapper.MapNewTrade(trade, TradeCallbackSource.OnNewTrade, probe.NextSequence(), key);
            probe.TryEnqueueNewTrade(obs, EnableTradeStreamProbe, DeclaredDataSourceMode, DataSourceModeProvenance, ExpectedInstrumentCode);
            TryRecordNewTrade(trade, RecorderCallbackSource.OnNewTrade);
            TryProcessEpisodeTrade(obs);
        }
        catch { probe.Counters.IncNormalizationFailures(); }
        PublishRuntimeSnapshot();
    }

    protected override void OnNewTrades(IEnumerable<MarketDataArg> trades)
    {
        EnsureProbesStarted();
        EnsureRuntimeStarted();
        TryCaptureInstrument();
        TrySubscribeMboOnce();
        NoteTradeCallback();
        var probe = _tradeProbe;
        var mapper = _tradeMapper;
        if (probe is null || mapper is null || trades is null) return;
        try
        {
            var key = probe.GetObservedInstrument()?.IdentityKey ?? "Unknown";
            var host = TryGetRecorderReady();
            if (host is not null)
            {
                var identity = ObservedInstrumentIdentityMapper.FromSnapshot(
                    probe.GetObservedInstrument() ?? new ObservedInstrumentSnapshot(
                        null, null, null, null, null, null, null, null, null, null, null));
                var ctx = host.Capture(RecorderCallbackSource.OnNewTrades);
                probe.Counters.IncCallback(TradeCallbackSource.OnNewTradesBatch);
                host.ProcessNewTradesBatch(
                    trades,
                    ctx,
                    identity,
                    DeclaredDataSourceMode.ToString(),
                    DataSourceModeProvenance.ToString(),
                    DeclaredFeedProvider.ToString(),
                    FeedProviderProvenance.ToString(),
                    probePerItem: (trade, _) =>
                    {
                        // Episode admission is independent of TradeStreamProbe enable/gates.
                        var obs = MapNormalizedNewTrade(trade, TradeCallbackSource.OnNewTradesBatch, key);
                        if (obs is not null)
                            TryProcessEpisodeTrade(obs);

                        if (!probe.IsAccepting) { probe.Counters.IncRejectedAfterDispose(); return; }
                        var gate = probe.EvaluateGates(EnableTradeStreamProbe, DeclaredDataSourceMode, DataSourceModeProvenance, ExpectedInstrumentCode);
                        if (!gate.Accepted)
                        {
                            if (gate.InstrumentGate) probe.Counters.IncRejectedByInstrumentGate();
                            else probe.Counters.IncRejectedByModeGate();
                            return;
                        }

                        if (obs is not null)
                            probe.TryEnqueueNewTradeAlreadyCounted(obs);
                    });
            }
            else
            {
                probe.Counters.IncCallback(TradeCallbackSource.OnNewTradesBatch);
                foreach (var trade in trades)
                {
                    if (trade is null) continue;

                    // One normalization → Episode always; probe enqueue only when gated.
                    var obs = MapNormalizedNewTrade(trade, TradeCallbackSource.OnNewTradesBatch, key);
                    if (obs is not null)
                        TryProcessEpisodeTrade(obs);

                    if (!probe.IsAccepting) { probe.Counters.IncRejectedAfterDispose(); continue; }
                    var gate = probe.EvaluateGates(EnableTradeStreamProbe, DeclaredDataSourceMode, DataSourceModeProvenance, ExpectedInstrumentCode);
                    if (!gate.Accepted)
                    {
                        if (gate.InstrumentGate) probe.Counters.IncRejectedByInstrumentGate();
                        else probe.Counters.IncRejectedByModeGate();
                        continue;
                    }

                    if (obs is not null)
                        probe.TryEnqueueNewTradeAlreadyCounted(obs);
                }
            }
        }
        catch { probe.Counters.IncNormalizationFailures(); }
        // P0-04B: do not call base.OnNewTrades
        PublishRuntimeSnapshot();
    }

    private NewTradeObservation? MapNormalizedNewTrade(
        MarketDataArg trade,
        TradeCallbackSource source,
        string instrumentIdentityKey)
    {
        var probe = _tradeProbe;
        var mapper = _tradeMapper;
        if (probe is null || mapper is null || trade is null)
            return null;
        return mapper.MapNewTrade(trade, source, probe.NextSequence(), instrumentIdentityKey);
    }

    protected override void OnCumulativeTrade(CumulativeTrade trade)
    {
        EnsureProbesStarted();
        EnsureRuntimeStarted();
        TryCaptureInstrument();
        TrySubscribeMboOnce();
        NoteTradeCallback();
        var probe = _tradeProbe;
        var mapper = _tradeMapper;
        if (probe is null || mapper is null || trade is null) return;
        try
        {
            var key = probe.GetObservedInstrument()?.IdentityKey ?? "Unknown";
            var obs = mapper.MapCumulative(trade, TradeCallbackSource.OnCumulativeTrade, probe.NextSequence(), key, true);
            probe.TryEnqueueCumulative(obs, EnableTradeStreamProbe, DeclaredDataSourceMode, DataSourceModeProvenance, ExpectedInstrumentCode);
            TryRecordCumulative(trade, RecorderCallbackSource.OnCumulativeTrade, assignInstanceId: true, isUpdate: false);
        }
        catch { probe.Counters.IncNormalizationFailures(); }
        PublishRuntimeSnapshot();
    }

    protected override void OnUpdateCumulativeTrade(CumulativeTrade trade)
    {
        EnsureProbesStarted();
        EnsureRuntimeStarted();
        TryCaptureInstrument();
        TrySubscribeMboOnce();
        NoteTradeCallback();
        var probe = _tradeProbe;
        var mapper = _tradeMapper;
        if (probe is null || mapper is null || trade is null) return;
        try
        {
            var key = probe.GetObservedInstrument()?.IdentityKey ?? "Unknown";
            var obs = mapper.MapCumulative(trade, TradeCallbackSource.OnUpdateCumulativeTrade, probe.NextSequence(), key, false);
            probe.TryEnqueueCumulative(obs, EnableTradeStreamProbe, DeclaredDataSourceMode, DataSourceModeProvenance, ExpectedInstrumentCode);
            TryRecordCumulative(trade, RecorderCallbackSource.OnUpdateCumulativeTrade, assignInstanceId: false, isUpdate: true);
        }
        catch { probe.Counters.IncNormalizationFailures(); }
        PublishRuntimeSnapshot();
    }

    protected override void MarketDepthChanged(MarketDataArg depth)
    {
        EnsureProbesStarted();
        TryCaptureInstrument();
        TrySubscribeMboOnce();
        var probe = _domProbe;
        if (probe is null || depth is null) return;
        try
        {
            var key = probe.GetObservedInstrument()?.IdentityKey ?? "Unknown";
            var (obs, detail) = DepthAtasMapper.Map(depth, DepthCallbackSource.MarketDepthChanged, probe.NextSequence(), key);
            if (probe.TryEnqueue(
                    obs, detail, EnableDomSemanticsProbe, DeclaredDataSourceMode, DataSourceModeProvenance,
                    ExpectedInstrumentCode, DeclaredFeedProvider, FeedProviderProvenance))
                probe.TryRequestSnapshotPull();
        }
        catch { probe.Counters.IncNormalizationFailures(); }
    }

    protected override void MarketDepthsChanged(IEnumerable<MarketDataArg> depths)
    {
        // P0-05: enumerate as MarketDepthsBatch. Do NOT call base.MarketDepthsChanged
        EnsureProbesStarted();
        TryCaptureInstrument();
        TrySubscribeMboOnce();
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
        EnsureProbesStarted();
        TryCaptureInstrument();
        TrySubscribeMboOnce();
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

    /// <summary>
    /// P0-06: batch MBO callback only. Base is EMPTY_RET — do not call base.
    /// Do not attach IOnlineDataProvider.MarketByOrdersChanged.
    /// Enumerate ATAS IEnumerable exactly once; copy primitives immediately; TryWrite; return.
    /// </summary>
    protected override void OnMarketByOrdersChanged(IEnumerable<MarketByOrder> values)
    {
        EnsureProbesStarted();
        TryCaptureInstrument();
        var probe = _mboProbe;
        if (probe is null) return;
        try
        {
            var (receiveUtc, sw, threadId) = MboAtasMapper.CaptureReceiveContext();
            probe.BeginCallback(receiveUtc);

            if (!probe.IsAccepting)
            {
                probe.Counters.IncRejectedAfterDispose();
                return;
            }

            var gate = probe.EvaluateGates(
                EnableMboLifecycleProbe, DeclaredDataSourceMode, DataSourceModeProvenance,
                ExpectedInstrumentCode, DeclaredFeedProvider, FeedProviderProvenance);
            if (!gate.Accepted)
            {
                if (gate.InstrumentGate) probe.Counters.IncRejectedByInstrumentGate();
                else probe.Counters.IncRejectedByModeGate();
                return;
            }

            if (values is null)
            {
                probe.Counters.IncNullBatch();
                return;
            }

            var key = probe.GetObservedInstrument()?.IdentityKey ?? "Unknown";
            var epoch = probe.SubscriptionEpoch;
            var enumerated = 0L;
            var any = false;
            foreach (var mbo in values)
            {
                any = true;
                enumerated++;
                if (mbo is null)
                {
                    probe.Counters.IncNullItem();
                    continue;
                }

                if (!probe.IsAccepting)
                {
                    probe.Counters.IncRejectedAfterDispose();
                    continue;
                }

                try
                {
                    var obs = MboAtasMapper.Map(
                        mbo, epoch, probe.NextSequence(), receiveUtc, sw, threadId, key);
                    probe.TryEnqueueMapped(obs);
                }
                catch
                {
                    probe.Counters.IncNormalizationFailures();
                }
            }

            probe.Counters.AddBatchItems(enumerated);
            if (!any)
                probe.Counters.IncEmptyBatch();
        }
        catch
        {
            probe.Counters.IncBatchEnumerationFailures();
        }
        // Do not call base.OnMarketByOrdersChanged
    }

    protected override void OnDispose()
    {
        try
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            // Final Invalid snapshot, then stop publication / release render before recorder/probes.
            try { PublishRuntimeSnapshot(); } catch { /* contained */ }

            TradeStreamProbe? trade;
            DomSemanticsProbe? dom;
            MboLifecycleProbe? mbo;
            TradeRecorderHost? recorder;
            GcaeRuntimeEngine? runtime;
            AuctionGpsCardRenderer? gps;
            PrimaryProfileOverlayRenderer? overlay;
            lock (_lifecycleGate)
            {
                trade = _tradeProbe;
                dom = _domProbe;
                mbo = _mboProbe;
                recorder = _tradeRecorder;
                runtime = _runtime;
                gps = _gpsRenderer;
                overlay = _overlayRenderer;
                _tradeProbe = null;
                _tradeMapper = null;
                _domProbe = null;
                _mboProbe = null;
                _tradeRecorder = null;
                _runtime = null;
                _gpsRenderer = null;
                _overlayRenderer = null;
                _profileHost = null;
                _compositeHost = null;
                _referenceHost = null;
                _directionalHost = null;
                _episodeHost = null;
                _lastAppliedCompositeConfiguration = null;
                _lastAppliedReferenceFingerprint = null;
                _lastAppliedDirectionalFingerprint = null;
                _lastAppliedEpisodeFingerprint = null;
            }

            try { runtime?.Stop(); } catch { /* contained */ }
            try { gps?.Dispose(); } catch { /* contained */ }
            try { overlay?.Dispose(); } catch { /* contained */ }

            // Recorder first: stop accepting → drain → finalize → dispose (locked Trade Recorder order)
            try { recorder?.StopAndDispose(); } catch { /* contained */ }
            DisposeTrade(trade);
            DisposeDom(dom);
            DisposeMbo(mbo);
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

    private void DisposeMbo(MboLifecycleProbe? probe)
    {
        if (probe is null) return;
        try
        {
            probe.StopAccepting();
            if (!probe.Drain(TimeSpan.FromMilliseconds(probe.Config.DrainTimeoutMilliseconds)))
            {
                probe.RecordLimitation("WorkerDrainTimeout");
                probe.RecordIntegrity("WorkerDrainTimeout");
            }

            var snapshot = probe.FreezeSnapshot(
                DeclaredDataSourceMode, DataSourceModeProvenance, ExpectedInstrumentCode,
                DeclaredFeedProvider, FeedProviderProvenance, _sessionId, EnableMboLifecycleProbe);
            try { MboLifecycleProbeArtifactWriter.WriteAtomic(snapshot); }
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
        if (_tradeProbe is not null && _domProbe is not null && _mboProbe is not null && _tradeRecorder is not null) return;
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

            if (_mboProbe is null)
                _mboProbe = new MboLifecycleProbe(new MboLifecycleProbeConfig());

            if (_tradeRecorder is null)
                _tradeRecorder = new TradeRecorderHost();
        }
    }

    private void EnsureRuntimeStarted()
    {
        if (Volatile.Read(ref _disposed) != 0)
            return;

        if (_runtime is not null && _gpsRenderer is not null && _overlayRenderer is not null)
        {
            EnsureDrawingSubscription();
            EnsureProfileHost();
            return;
        }

        lock (_lifecycleGate)
        {
            if (Volatile.Read(ref _disposed) != 0)
                return;

            _runtime ??= new GcaeRuntimeEngine(new RuntimeGateConfig(
                expectedTickSize: ExpectedTickSize > 0m ? ExpectedTickSize : RuntimeGateConfig.DefaultExpectedTickSize,
                nearExpirationCalendarDays: NearExpirationCalendarDays >= 0
                    ? NearExpirationCalendarDays
                    : RuntimeGateConfig.DefaultNearExpirationCalendarDays));

            _gpsRenderer ??= new AuctionGpsCardRenderer();
            _gpsRenderer.SetMargins(GpsCardMarginX, GpsCardMarginY);
            _overlayRenderer ??= new PrimaryProfileOverlayRenderer();
        }

        EnsureDrawingSubscription();
        EnsureProfileHost();
    }

    private void EnsureDrawingSubscription()
    {
        if ((!EnableAuctionGpsCard && !EnablePrimaryProfileOverlay) || _drawingSubscribed)
            return;
        try
        {
            EnableCustomDrawing = true;
            SubscribeToDrawingEvents(DrawingLayouts.Final | DrawingLayouts.LatestBar);
            _drawingSubscribed = true;
        }
        catch
        {
            // Contained.
        }
    }

    private void EnsureProfileHost()
    {
        if (!EnablePrimaryProfile)
            return;

        var tick = ExpectedTickSize > 0m ? ExpectedTickSize : RuntimeGateConfig.DefaultExpectedTickSize;
        var period = TpoPeriodMinutes > 0 ? TpoPeriodMinutes : PrimaryAuctionClockConfig.DefaultPeriodMinutes;
        var frac = ValueAreaFraction > 0m && ValueAreaFraction <= 1m
            ? ValueAreaFraction
            : PrimaryAuctionClockConfig.DefaultValueAreaFraction;
        var anchor = new TimeSpan(Math.Clamp(AnchorHourLocal, 0, 23), Math.Clamp(AnchorMinuteLocal, 0, 59), 0);
        var key = $"{tick}|{period}|{frac}|{anchor}|{AtasTimestampNormalizer.PolicyVersion}";

        if (_profileHost is not null && string.Equals(_profileSettingsKey, key, StringComparison.Ordinal))
            return;

        var cfg = new PrimaryAuctionClockConfig(
            AuctionTimezoneResolver.IanaAmericaNewYork,
            anchor,
            period,
            frac);

        if (_profileHost is null)
            _profileHost = new PrimaryProfileHost(tick, cfg);
        else
            _profileHost.Reset(tick, cfg);

        _profileSettingsKey = key;
    }

    private void ProcessProfileBar(int bar)
    {
        if (!EnablePrimaryProfile || Volatile.Read(ref _disposed) != 0)
            return;

        try
        {
            EnsureProfileHost();
            var host = _profileHost;
            if (host is null)
                return;

            IndicatorCandle? candle = null;
            try { candle = GetCandle(bar); }
            catch { return; }
            if (candle is null)
                return;

            var version = Interlocked.Increment(ref _barSourceVersion);
            // Deferred rebuild on first historical pass: ingest only until current bar (or bar replace).
            // Prevents O(n²) ClassicTpoEngine+VolumeProfile rebuilds when adding indicator to a long chart.
            var rebuildNow = bar >= CurrentBar || host.ContainsBar(bar);
            var wantTsDiag = rebuildNow || bar == 0 || (bar % 64) == 0;
            var obs = ProfileBarAtasMapper.TryMap(
                candle, bar, CurrentBar, version, out var tsDiag, includeTimestampDiagnostic: wantTsDiag);
            if (obs is null)
                return;

            if (tsDiag is not null)
                host.NoteTimestampDiagnostic(tsDiag);

            host.EnableTpoParityReferencePrice = EnableTpoParityReferencePrice;
            host.TpoParityReferencePrice = TpoParityReferencePrice;
            host.UpsertBar(
                obs,
                evaluationBarIndex: Math.Min(bar, CurrentBar),
                evaluationUtc: DateTimeOffset.UtcNow,
                rebuildNow: rebuildNow);
        }
        catch
        {
            // Contained — never escape into ATAS.
        }
    }

    private void ProcessComposite()
    {
        if (!EnableCompositeProfile || Volatile.Read(ref _disposed) != 0)
        {
            _compositeHost = null;
            _lastAppliedCompositeConfiguration = null;
            return;
        }

        try
        {
            var profiles = _profileHost?.Current;
            if (profiles is null)
                return;

            var tick = ExpectedTickSize > 0m ? ExpectedTickSize : RuntimeGateConfig.DefaultExpectedTickSize;
            var frac = ValueAreaFraction > 0m && ValueAreaFraction <= 1m
                ? ValueAreaFraction
                : PrimaryAuctionClockConfig.DefaultValueAreaFraction;
            var policy = BuildCompositePolicyFromSettings();

            _compositeHost ??= new CompositeProfileHost(tick, frac, policy);
            var identity = _tradeProbe?.GetObservedInstrument()?.IdentityKey
                           ?? (string.IsNullOrWhiteSpace(ExpectedInstrumentCode) ? "Unknown" : ExpectedInstrumentCode);
            var epoch = identity + "|tick=" + tick.ToString(System.Globalization.CultureInfo.InvariantCulture);
            _compositeHost.Configure(tick, frac, identity, epoch, policy);

            var developing = profiles.CurrentAuction is { IsCompleted: false } cur ? cur : null;
            _compositeHost.Rebuild(profiles.CompletedAuctions, developing);

            // Fingerprint advances only after a published snapshot exists.
            if (_compositeHost.Current is not null)
                _lastAppliedCompositeConfiguration = CompositeOperatorConfiguration.FromPolicy(policy);
        }
        catch
        {
            // Contained — do not advance fingerprint.
        }
    }

    private void ProcessStructuralReferences()
    {
        if (!EnableStructuralReferences || Volatile.Read(ref _disposed) != 0)
        {
            _referenceHost = null;
            _lastAppliedReferenceFingerprint = null;
            return;
        }

        try
        {
            var profiles = _profileHost?.Current;
            var tick = ExpectedTickSize > 0m ? ExpectedTickSize : RuntimeGateConfig.DefaultExpectedTickSize;
            var identity = _tradeProbe?.GetObservedInstrument()?.IdentityKey
                           ?? (string.IsNullOrWhiteSpace(ExpectedInstrumentCode) ? "Unknown" : ExpectedInstrumentCode);
            var epoch = identity + "|tick=" + tick.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var policy = new ReferencePolicyConfig(enabled: true);
            var composite = EnableCompositeProfile ? _compositeHost?.Current : null;

            _referenceHost ??= new StructuralReferenceHost(tick, identity, epoch, AtasTimestampNormalizer.PolicyVersion, policy);
            _referenceHost.Configure(tick, identity, epoch, policy, AtasTimestampNormalizer.PolicyVersion);
            _referenceHost.Rebuild(profiles, composite);

            if (_referenceHost.Current is not null && _referenceHost.LastAppliedFingerprint is { } fp)
                _lastAppliedReferenceFingerprint = fp;
        }
        catch
        {
            // Contained — do not advance fingerprint.
        }
    }

    private void ProcessDirectionalContext()
    {
        if (!EnableDirectionalContext || Volatile.Read(ref _disposed) != 0)
        {
            _directionalHost = null;
            _lastAppliedDirectionalFingerprint = null;
            return;
        }

        try
        {
            var profiles = _profileHost?.Current;
            var tick = ExpectedTickSize > 0m ? ExpectedTickSize : RuntimeGateConfig.DefaultExpectedTickSize;
            var identity = _tradeProbe?.GetObservedInstrument()?.IdentityKey
                           ?? (string.IsNullOrWhiteSpace(ExpectedInstrumentCode) ? "Unknown" : ExpectedInstrumentCode);
            var epoch = identity + "|tick=" + tick.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var policy = new DirectionalPolicyConfig(
                enabled: true,
                enableOneTimeFraming: EnableOneTimeFraming);
            var composite = EnableCompositeProfile ? _compositeHost?.Current : null;
            var references = EnableStructuralReferences ? _referenceHost?.Current : null;

            _directionalHost ??= new DirectionalContextHost(
                tick, identity, epoch, AtasTimestampNormalizer.PolicyVersion, policy);
            _directionalHost.Configure(tick, identity, epoch, policy, AtasTimestampNormalizer.PolicyVersion);
            _directionalHost.Rebuild(profiles, composite, references);

            if (_directionalHost.Current is not null && _directionalHost.LastAppliedFingerprint is { } fp)
                _lastAppliedDirectionalFingerprint = fp;
        }
        catch
        {
            // Contained — do not advance fingerprint.
        }
    }

    private void ProcessAuctionEpisodes()
    {
        if (!EnableAuctionEpisodes || Volatile.Read(ref _disposed) != 0)
        {
            _episodeHost = null;
            _lastAppliedEpisodeFingerprint = null;
            return;
        }

        try
        {
            var tick = ExpectedTickSize > 0m ? ExpectedTickSize : RuntimeGateConfig.DefaultExpectedTickSize;
            var identity = _tradeProbe?.GetObservedInstrument()?.IdentityKey
                           ?? (string.IsNullOrWhiteSpace(ExpectedInstrumentCode) ? "Unknown" : ExpectedInstrumentCode);
            var epoch = identity + "|tick=" + tick.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var policy = new EpisodePolicyConfig(enabled: true);
            var profiles = EnablePrimaryProfile ? _profileHost?.Current : null;
            var references = EnableStructuralReferences ? _referenceHost?.Current : null;
            var directional = EnableDirectionalContext ? _directionalHost?.Current : null;

            _episodeHost ??= new AuctionEpisodeHost(tick, identity, epoch, AtasTimestampNormalizer.PolicyVersion, policy);
            _episodeHost.Configure(tick, identity, epoch, policy, AtasTimestampNormalizer.PolicyVersion);
            _episodeHost.RebuildContext(profiles, references, directional);

            if (_episodeHost.Current is not null && _episodeHost.LastAppliedFingerprint is { } fp)
                _lastAppliedEpisodeFingerprint = fp;
        }
        catch
        {
            // Contained.
        }
    }

    private void TryProcessEpisodeTrade(NewTradeObservation obs)
    {
        if (!EnableAuctionEpisodes || obs is null || Volatile.Read(ref _disposed) != 0)
            return;
        try
        {
            EnsureAuctionEpisodesInitializedForPublish();
            var host = _episodeHost;
            if (host is null)
                return;

            var tick = ExpectedTickSize > 0m ? ExpectedTickSize : RuntimeGateConfig.DefaultExpectedTickSize;
            var identity = _tradeProbe?.GetObservedInstrument()?.IdentityKey
                           ?? (string.IsNullOrWhiteSpace(ExpectedInstrumentCode) ? "Unknown" : ExpectedInstrumentCode);
            var epoch = identity + "|tick=" + tick.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var grid = new PriceGrid(tick);
            var evt = EpisodeTradeEvent.TryFromNewTrade(
                obs, tick, epoch, AtasTimestampNormalizer.PolicyVersion,
                p => grid.TryToTickIndex(p, out var t) ? t : null);
            if (evt is null)
            {
                host.NoteMappingReject("EPISODE_TICK_MAPPING_FAILED");
                if (host.LastAppliedFingerprint is { } fpReject)
                    _lastAppliedEpisodeFingerprint = fpReject;
                return;
            }

            host.ProcessTrade(evt);
            if (host.LastAppliedFingerprint is { } fp)
                _lastAppliedEpisodeFingerprint = fp;
        }
        catch
        {
            // Contained.
        }
    }

    /// <summary>
    /// Ensure Structural Reference host matches current inputs before GPS publish.
    /// Rebuilds only when Current missing or input fingerprint changed.
    /// </summary>
    private void EnsureStructuralReferencesInitializedForPublish()
    {
        if (!EnableStructuralReferences || Volatile.Read(ref _disposed) != 0)
        {
            _referenceHost = null;
            _lastAppliedReferenceFingerprint = null;
            return;
        }

        var profiles = EnablePrimaryProfile ? _profileHost?.Current : null;
        var tick = ExpectedTickSize > 0m ? ExpectedTickSize : RuntimeGateConfig.DefaultExpectedTickSize;
        var identity = _tradeProbe?.GetObservedInstrument()?.IdentityKey
                       ?? (string.IsNullOrWhiteSpace(ExpectedInstrumentCode) ? "Unknown" : ExpectedInstrumentCode);
        var epoch = identity + "|tick=" + tick.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var composite = EnableCompositeProfile ? _compositeHost?.Current : null;
        var current = ReferenceInputFingerprint.Build(
            true,
            profiles,
            composite,
            tick,
            epoch,
            AtasTimestampNormalizer.PolicyVersion);

        if (!ReferencePublishInitialization.ShouldProcess(
                EnableStructuralReferences,
                profiles,
                _referenceHost,
                _lastAppliedReferenceFingerprint,
                current))
            return;

        ProcessStructuralReferences();
    }

    private void EnsureDirectionalContextInitializedForPublish()
    {
        if (!EnableDirectionalContext || Volatile.Read(ref _disposed) != 0)
        {
            _directionalHost = null;
            _lastAppliedDirectionalFingerprint = null;
            return;
        }

        var profiles = EnablePrimaryProfile ? _profileHost?.Current : null;
        var tick = ExpectedTickSize > 0m ? ExpectedTickSize : RuntimeGateConfig.DefaultExpectedTickSize;
        var identity = _tradeProbe?.GetObservedInstrument()?.IdentityKey
                       ?? (string.IsNullOrWhiteSpace(ExpectedInstrumentCode) ? "Unknown" : ExpectedInstrumentCode);
        var epoch = identity + "|tick=" + tick.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var composite = EnableCompositeProfile ? _compositeHost?.Current : null;
        var references = EnableStructuralReferences ? _referenceHost?.Current : null;
        var current = DirectionalInputFingerprint.Build(
            true,
            EnableOneTimeFraming,
            profiles,
            composite,
            references,
            tick,
            epoch,
            AtasTimestampNormalizer.PolicyVersion);

        if (!DirectionalPublishInitialization.ShouldProcess(
                EnableDirectionalContext,
                profiles,
                _directionalHost,
                _lastAppliedDirectionalFingerprint,
                current))
            return;

        ProcessDirectionalContext();
    }

    private void EnsureAuctionEpisodesInitializedForPublish()
    {
        if (!EnableAuctionEpisodes || Volatile.Read(ref _disposed) != 0)
        {
            _episodeHost = null;
            _lastAppliedEpisodeFingerprint = null;
            return;
        }

        var profiles = EnablePrimaryProfile ? _profileHost?.Current : null;
        var tick = ExpectedTickSize > 0m ? ExpectedTickSize : RuntimeGateConfig.DefaultExpectedTickSize;
        var identity = _tradeProbe?.GetObservedInstrument()?.IdentityKey
                       ?? (string.IsNullOrWhiteSpace(ExpectedInstrumentCode) ? "Unknown" : ExpectedInstrumentCode);
        var epoch = identity + "|tick=" + tick.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var references = EnableStructuralReferences ? _referenceHost?.Current : null;
        var directional = EnableDirectionalContext ? _directionalHost?.Current : null;
        var current = EpisodeInputFingerprint.Build(
            true,
            profiles?.CurrentAuction?.AuctionId,
            references,
            directional,
            "live",
            tick,
            epoch,
            AtasTimestampNormalizer.PolicyVersion);

        if (!EpisodePublishInitialization.ShouldProcess(
                EnableAuctionEpisodes,
                references,
                _episodeHost,
                _lastAppliedEpisodeFingerprint,
                current))
            return;

        ProcessAuctionEpisodes();
    }

    private CompositePolicyConfig BuildCompositePolicyFromSettings()
    {
        var excluded = (CompositeExcludedAuctionIds ?? "")
            .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return new CompositePolicyConfig(
            mode: CompositePolicyMode.OperatorAnchored,
            anchorAuctionId: CompositeAnchorAuctionId,
            includeThroughLatestCompletedAuction: IncludeThroughLatestCompletedAuction,
            excludedAuctionIds: excluded,
            enableDevelopingCompositePreview: EnableDevelopingCompositePreview,
            enableShadowEvidence: EnableShadowCompositeEvidence);
    }

    /// <summary>
    /// Ensure Composite host matches current operator settings before GPS publish.
    /// Rebuilds only when Current missing or normalized operator fingerprint changed.
    /// </summary>
    private void EnsureCompositeSnapshotInitializedForPublish()
    {
        if (!EnableCompositeProfile || Volatile.Read(ref _disposed) != 0)
        {
            _compositeHost = null;
            _lastAppliedCompositeConfiguration = null;
            return;
        }

        var profiles = _profileHost?.Current;
        if (profiles is null)
            return;

        var current = CompositeOperatorConfiguration.FromPolicy(BuildCompositePolicyFromSettings());
        if (!CompositePublishInitialization.ShouldProcess(
                EnableCompositeProfile,
                profiles,
                _compositeHost,
                _lastAppliedCompositeConfiguration,
                current))
            return;

        ProcessComposite();
    }

    private void NoteTradeCallback()
    {
        _tradeObserved = true;
        _lastTradeCallbackUtc = DateTime.UtcNow;
    }

    private void PublishRuntimeSnapshot()
    {
        if (!EnableAuctionGpsCard && !EnablePrimaryProfile && _runtime is null)
            return;

        try
        {
            EnsureRuntimeStarted();
            var runtime = _runtime;
            var renderer = _gpsRenderer;
            var overlay = _overlayRenderer;
            if (runtime is null)
                return;

            var probe = _tradeProbe;
            var recorder = _tradeRecorder;
            var disposed = Volatile.Read(ref _disposed) != 0;
            var startupFailures = recorder is null
                ? 0L
                : Interlocked.Read(ref recorder.Counters.RecorderStartupFailures);

            // Trade-style publishes: init or rebuild only when host/Current missing or operator config changed.
            EnsureCompositeSnapshotInitializedForPublish();
            EnsureStructuralReferencesInitializedForPublish();
            EnsureDirectionalContextInitializedForPublish();
            EnsureAuctionEpisodesInitializedForPublish();

            var profiles = EnablePrimaryProfile ? _profileHost?.Current : null;
            var composite = EnableCompositeProfile ? _compositeHost?.Current : null;
            var references = EnableStructuralReferences ? _referenceHost?.Current : null;
            var directional = EnableDirectionalContext ? _directionalHost?.Current : null;
            var episodes = EnableAuctionEpisodes ? _episodeHost?.Current : null;

            var snapshot = runtime.Publish(
                observed: probe?.GetObservedInstrument(),
                expectedInstrumentCode: ExpectedInstrumentCode ?? "",
                mode: DeclaredDataSourceMode,
                modeProvenance: DataSourceModeProvenance,
                provider: DeclaredFeedProvider,
                providerProvenance: FeedProviderProvenance,
                tradeObserved: _tradeObserved,
                lastTradeCallbackUtc: _lastTradeCallbackUtc,
                rawRecorderMasterEnabled: EnableRawEventRecorder,
                tradeRecordingEnabled: EnableTradeRecording,
                recorderAccepting: recorder?.IsAccepting == true,
                recorderFaulted: startupFailures > 0,
                recorderSessionPresent: recorder?.Session is not null,
                indicatorDisposed: disposed,
                profiles: profiles,
                enableTpoParityDiagnostics: EnableTpoParityDiagnostics,
                composite: composite,
                showCompositeDiagnostics: ShowCompositeDiagnostics,
                structuralReferences: references,
                showStructuralReferenceDiagnostics: ShowStructuralReferenceDiagnostics,
                directionalContext: directional,
                showDirectionalContextDiagnostics: ShowDirectionalContextDiagnostics,
                auctionEpisodes: episodes,
                showAuctionEpisodeDiagnostics: ShowAuctionEpisodeDiagnostics);

            if (EnableAuctionGpsCard && renderer is not null)
            {
                var vm = AuctionGpsCardMapper.FromSnapshot(snapshot, ShowAuctionGpsDiagnostics);
                renderer.Update(vm, ShowAuctionGpsDiagnostics);
                renderer.SetMargins(GpsCardMarginX, GpsCardMarginY);
            }
            else
            {
                renderer?.Update(null, false);
            }

            if (EnablePrimaryProfileOverlay && overlay is not null)
            {
                var ovm = PrimaryProfileOverlayViewModel.FromProfiles(
                    profiles,
                    ShowPreviousProfileLevels,
                    composite,
                    EnableCompositeOverlay,
                    showCompositePreview: EnableDevelopingCompositePreview,
                    structuralReferences: references,
                    enableStructuralReferenceOverlay: EnableStructuralReferences && EnableStructuralReferenceOverlay);
                overlay.Update(ovm);
            }
            else
            {
                overlay?.Update(null);
            }
        }
        catch
        {
            // Contained — never escape into ATAS.
        }
    }

    private void TryCompleteRecorderStartup()
    {
        if (Volatile.Read(ref _disposed) != 0)
            return;
        var host = _tradeRecorder;
        var probe = _tradeProbe;
        var mapper = _tradeMapper;
        if (host is null || probe is null || mapper is null)
            return;
        if (!EnableRawEventRecorder)
            return;

        // Refresh pending request with latest gates/identity, then complete I/O off Trade callbacks.
        _ = host.EnsureStarted(
            EnableRawEventRecorder,
            EnableTradeRecording,
            probe.GetObservedInstrument(),
            ExpectedInstrumentCode,
            DeclaredDataSourceMode,
            DataSourceModeProvenance,
            DeclaredFeedProvider,
            FeedProviderProvenance,
            _sessionId,
            mapper);
        host.TryCompleteStartupFromLifecycle();
    }

    private TradeRecorderHost? TryGetRecorderReady()
    {
        if (Volatile.Read(ref _disposed) != 0)
            return null;

        var host = _tradeRecorder;
        var probe = _tradeProbe;
        var mapper = _tradeMapper;
        if (host is null || probe is null || mapper is null)
            return null;

        if (!EnableRawEventRecorder)
            return null;

        var outcome = host.EnsureStarted(
            EnableRawEventRecorder,
            EnableTradeRecording,
            probe.GetObservedInstrument(),
            ExpectedInstrumentCode,
            DeclaredDataSourceMode,
            DataSourceModeProvenance,
            DeclaredFeedProvider,
            FeedProviderProvenance,
            _sessionId,
            mapper);

        if (outcome == Recorder.FanOut.RecorderSinkOutcome.NotConfigured
            || outcome == Recorder.FanOut.RecorderSinkOutcome.StreamDisabled
            || outcome == Recorder.FanOut.RecorderSinkOutcome.SessionNotStarted)
        {
            host.NoteCallbackBeforeStart();
            return null;
        }

        if (outcome == Recorder.FanOut.RecorderSinkOutcome.RejectedByGate)
        {
            host.NoteRejectedByGate();
            return null;
        }

        if (outcome == Recorder.FanOut.RecorderSinkOutcome.Faulted
            || outcome == Recorder.FanOut.RecorderSinkOutcome.StoppedAccepting)
        {
            host.NoteCallbackAfterStop();
            return null;
        }

        return host.IsAccepting ? host : null;
    }

    private void TryRecordNewTrade(MarketDataArg trade, RecorderCallbackSource source)
    {
        var host = TryGetRecorderReady();
        var probe = _tradeProbe;
        if (host is null || probe is null) return;
        var snap = probe.GetObservedInstrument();
        if (snap is null) return;
        var identity = ObservedInstrumentIdentityMapper.FromSnapshot(snap);
        var ctx = host.Capture(source);
        host.RecordNewTradeAfterProbe(
            trade, ctx, identity,
            DeclaredDataSourceMode.ToString(),
            DataSourceModeProvenance.ToString(),
            DeclaredFeedProvider.ToString(),
            FeedProviderProvenance.ToString());
    }

    private void TryRecordCumulative(CumulativeTrade trade, RecorderCallbackSource source, bool assignInstanceId, bool isUpdate)
    {
        var host = TryGetRecorderReady();
        var probe = _tradeProbe;
        if (host is null || probe is null) return;
        var snap = probe.GetObservedInstrument();
        if (snap is null) return;
        var identity = ObservedInstrumentIdentityMapper.FromSnapshot(snap);
        var ctx = host.Capture(source);
        host.RecordCumulativeAfterProbe(
            trade, ctx, assignInstanceId, isUpdate, identity,
            DeclaredDataSourceMode.ToString(),
            DataSourceModeProvenance.ToString(),
            DeclaredFeedProvider.ToString(),
            FeedProviderProvenance.ToString());
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
            _mboProbe?.SetObservedInstrument(snap);
            if (!string.Equals(snap.IdentityKey, "Unknown", StringComparison.Ordinal))
                _instrumentCaptured = true;
        }
        catch { }
    }

    private void TrySubscribeMboOnce()
    {
        var probe = _mboProbe;
        if (probe is null || !EnableMboLifecycleProbe) return;
        try
        {
            probe.TrySubscribeOnce(
                EnableMboLifecycleProbe,
                DeclaredDataSourceMode,
                DataSourceModeProvenance,
                ExpectedInstrumentCode,
                DeclaredFeedProvider,
                FeedProviderProvenance,
                startSubscribe: () => SubscribeMarketByOrderData());
        }
        catch (Exception ex)
        {
            probe.RecordIntegrity("SubscribeInvokeFailure:" + ex.GetType().Name);
        }
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
