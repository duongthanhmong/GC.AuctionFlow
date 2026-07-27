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
        IReadOnlyList<string>? extraLimitations = null,
        long tradesObserved = 0,
        long tradesWithAggressorSide = 0,
        long depthCallbacksObserved = 0,
        bool mboRecordingUnlocked = false)
    {
        var limitations = new List<string>
        {
            "NOT_EXCHANGE_FEED_COMPLETENESS",
            "NOT_HISTORICAL_FIDELITY_CLAIM",
            "NOT_REPLAY_FIDELITY_CLAIM",
        };
        if (extraLimitations is not null)
            limitations.AddRange(extraLimitations);

        var tradeState = tradeObserved
            ? RuntimeCapabilityState.Available
            : RuntimeCapabilityState.Unknown;

        // Measured rather than assumed.
        //
        // These three were pinned to Unknown / Unavailable / Blocked, which reported a
        // policy decision as though it were an observation. The feed hands IsAsk and IsBid
        // to every trade callback, so whether aggressor side is available was always a
        // countable fact — it was simply never counted, and the card said "unknown" when
        // the answer was sitting in the callback arguments.
        //
        // The classification is threshold-free: none / some / all, no fraction chosen.
        var bidAskState =
            tradesObserved == 0 ? RuntimeCapabilityState.Unknown
            : tradesWithAggressorSide == 0 ? RuntimeCapabilityState.Unavailable
            : tradesWithAggressorSide < tradesObserved ? RuntimeCapabilityState.Partial
            : RuntimeCapabilityState.Available;

        if (bidAskState is RuntimeCapabilityState.Unknown or RuntimeCapabilityState.Unavailable)
            limitations.Add("BIDASK_NOT_VALIDATED");

        // Depth callbacks arrive without any subscription of ours, so their absence is
        // evidence about the feed rather than about our configuration.
        var domState = depthCallbacksObserved > 0
            ? RuntimeCapabilityState.Partial
            : RuntimeCapabilityState.Unavailable;

        if (domState != RuntimeCapabilityState.Partial)
            limitations.Add("DOM_NOT_VALIDATED");

        // Blocked is the P0-06D operational lock, not an absence. Keeping the two distinct
        // matters: Blocked means we chose not to look, Unavailable means we looked and it
        // was not there.
        var mboState = mboRecordingUnlocked
            ? RuntimeCapabilityState.NotReady
            : RuntimeCapabilityState.Blocked;

        if (!mboRecordingUnlocked)
            limitations.Add("MBO_BLOCKED_ISOLATED_ENVIRONMENT_ONLY");

        var recorderState = ClassifyRecorder(
            rawRecorderMasterEnabled, tradeRecordingEnabled, recorderSessionPresent, recorderAccepting, recorderFaulted);

        return new RuntimeCapabilitySnapshot(
            tradeStreamState: tradeState,
            bidAskClassificationState: bidAskState,
            profileState: profileState,
            domState: domState,
            mboState: mboState,
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
