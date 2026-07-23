using GC.AuctionFlow.Core;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Profile;

namespace GC.AuctionFlow.Runtime;

/// <summary>Runtime capability snapshot distinct from historical research claims.</summary>
public sealed class RuntimeCapabilitySnapshot
{
    public const string SnapshotVersion = "0.2.0";

    public RuntimeCapabilitySnapshot(
        RuntimeCapabilityState tradeStreamState,
        RuntimeCapabilityState bidAskClassificationState,
        RuntimeCapabilityState profileState,
        RuntimeCapabilityState domState,
        RuntimeCapabilityState mboState,
        DataSourceMode dataSourceMode,
        DataSourceModeProvenance dataSourceModeProvenance,
        DeclaredFeedProvider feedProvider,
        FeedProviderProvenance feedProviderProvenance,
        DateTime? lastTradeCallbackUtc,
        bool instrumentIdentityAvailable,
        RuntimeCapabilityState recorderState,
        IReadOnlyList<string> knownLimitations)
    {
        TradeStreamState = tradeStreamState;
        BidAskClassificationState = bidAskClassificationState;
        ProfileState = profileState;
        DomState = domState;
        MboState = mboState;
        DataSourceMode = dataSourceMode;
        DataSourceModeProvenance = dataSourceModeProvenance;
        FeedProvider = feedProvider;
        FeedProviderProvenance = feedProviderProvenance;
        LastTradeCallbackUtc = lastTradeCallbackUtc;
        InstrumentIdentityAvailable = instrumentIdentityAvailable;
        RecorderState = recorderState;
        KnownLimitations = knownLimitations ?? Array.Empty<string>();
    }

    public RuntimeCapabilityState TradeStreamState { get; }
    public RuntimeCapabilityState BidAskClassificationState { get; }
    public RuntimeCapabilityState ProfileState { get; }
    public RuntimeCapabilityState DomState { get; }
    public RuntimeCapabilityState MboState { get; }
    public DataSourceMode DataSourceMode { get; }
    public DataSourceModeProvenance DataSourceModeProvenance { get; }
    public DeclaredFeedProvider FeedProvider { get; }
    public FeedProviderProvenance FeedProviderProvenance { get; }
    public DateTime? LastTradeCallbackUtc { get; }
    public bool InstrumentIdentityAvailable { get; }
    public RuntimeCapabilityState RecorderState { get; }
    public IReadOnlyList<string> KnownLimitations { get; }
    public string Version => SnapshotVersion;
}

public static class RuntimeCapabilitySnapshotBuilder
{
    public static RuntimeCapabilitySnapshot Build(
        DataSourceMode mode,
        DataSourceModeProvenance modeProvenance,
        DeclaredFeedProvider provider,
        FeedProviderProvenance providerProvenance,
        bool instrumentIdentityAvailable,
        bool tradeObserved,
        DateTime? lastTradeCallbackUtc,
        bool rawRecorderMasterEnabled,
        bool tradeRecordingEnabled,
        bool recorderAccepting,
        bool recorderFaulted,
        bool recorderSessionPresent,
        RuntimeCapabilityState profileState = RuntimeCapabilityState.NotReady,
        IReadOnlyList<string>? extraLimitations = null)
    {
        var limitations = new List<string>
        {
            "NOT_EXCHANGE_FEED_COMPLETENESS",
            "NOT_HISTORICAL_FIDELITY_CLAIM",
            "NOT_REPLAY_FIDELITY_CLAIM",
            "BIDASK_NOT_VALIDATED",
            "DOM_NOT_VALIDATED",
            "MBO_BLOCKED_ISOLATED_ENVIRONMENT_ONLY"
        };
        if (extraLimitations is not null)
            limitations.AddRange(extraLimitations);

        var tradeState = tradeObserved
            ? RuntimeCapabilityState.Available
            : RuntimeCapabilityState.Unknown;

        var recorderState = ClassifyRecorder(
            rawRecorderMasterEnabled, tradeRecordingEnabled, recorderSessionPresent, recorderAccepting, recorderFaulted);

        return new RuntimeCapabilitySnapshot(
            tradeStreamState: tradeState,
            bidAskClassificationState: RuntimeCapabilityState.Unknown,
            profileState: profileState,
            domState: RuntimeCapabilityState.Unavailable,
            mboState: RuntimeCapabilityState.Blocked,
            dataSourceMode: mode,
            dataSourceModeProvenance: modeProvenance,
            feedProvider: provider,
            feedProviderProvenance: providerProvenance,
            lastTradeCallbackUtc: lastTradeCallbackUtc,
            instrumentIdentityAvailable: instrumentIdentityAvailable,
            recorderState: recorderState,
            knownLimitations: limitations);
    }

    public static RuntimeCapabilityState MapProfileState(AuctionProfileState? state) => state switch
    {
        null => RuntimeCapabilityState.NotReady,
        AuctionProfileState.NotReady => RuntimeCapabilityState.NotReady,
        AuctionProfileState.Partial => RuntimeCapabilityState.Partial,
        AuctionProfileState.Ready => RuntimeCapabilityState.Ready,
        AuctionProfileState.Invalid => RuntimeCapabilityState.Invalid,
        _ => RuntimeCapabilityState.NotReady
    };

    public static ProfilePlaceholderState MapPlaceholder(AuctionProfileState? state) => state switch
    {
        null => ProfilePlaceholderState.NotReady,
        AuctionProfileState.NotReady => ProfilePlaceholderState.NotReady,
        AuctionProfileState.Partial => ProfilePlaceholderState.Partial,
        AuctionProfileState.Ready => ProfilePlaceholderState.Ready,
        AuctionProfileState.Invalid => ProfilePlaceholderState.Invalid,
        _ => ProfilePlaceholderState.NotReady
    };

    private static RuntimeCapabilityState ClassifyRecorder(
        bool master,
        bool tradeEnabled,
        bool sessionPresent,
        bool accepting,
        bool faulted)
    {
        if (faulted) return RuntimeCapabilityState.Faulted;
        if (!master) return RuntimeCapabilityState.Off;
        if (!tradeEnabled) return RuntimeCapabilityState.NotConfigured;
        if (!sessionPresent) return RuntimeCapabilityState.Unavailable;
        if (accepting) return RuntimeCapabilityState.Recording;
        return RuntimeCapabilityState.Ready;
    }
}
