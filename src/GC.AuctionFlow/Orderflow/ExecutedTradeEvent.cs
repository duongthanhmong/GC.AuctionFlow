using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Probe;

namespace GC.AuctionFlow.Orderflow;

/// <summary>
/// Authoritative normalized executed-trade event for Phase 2A.
/// Built only from NewTradeObservation — never from candles or cumulative totals.
/// </summary>
public sealed class ExecutedTradeEvent
{
    public ExecutedTradeEvent(
        string eventId,
        long eventSequence,
        long eventRevision,
        string instrumentIdentity,
        string dataEpoch,
        string primaryAuctionId,
        DateTime exchangeTimestampUtc,
        DateTime? receiveTimestampUtc,
        long normalizedPriceTick,
        decimal decimalPrice,
        decimal executedVolume,
        AggressorSide aggressorSide,
        bool aggressorClassified,
        decimal tickSize,
        string timestampPolicy,
        OrderflowSourceCallbackKind sourceCallbackKind,
        bool isHistorical,
        string provenance,
        IReadOnlyList<string> associatedEpisodeIds)
    {
        EventId = eventId ?? "";
        EventSequence = eventSequence;
        EventRevision = eventRevision;
        InstrumentIdentity = instrumentIdentity ?? "";
        DataEpoch = dataEpoch ?? "";
        PrimaryAuctionId = primaryAuctionId ?? "";
        ExchangeTimestampUtc = exchangeTimestampUtc;
        ReceiveTimestampUtc = receiveTimestampUtc;
        NormalizedPriceTick = normalizedPriceTick;
        DecimalPrice = decimalPrice;
        ExecutedVolume = executedVolume;
        AggressorSide = aggressorSide;
        AggressorClassified = aggressorClassified;
        TickSize = tickSize;
        TimestampPolicy = timestampPolicy ?? "";
        SourceCallbackKind = sourceCallbackKind;
        IsHistorical = isHistorical;
        Provenance = provenance ?? "";
        AssociatedEpisodeIds = associatedEpisodeIds ?? Array.Empty<string>();
    }

    public string EventId { get; }
    public long EventSequence { get; }
    public long EventRevision { get; }
    public string InstrumentIdentity { get; }
    public string DataEpoch { get; }
    public string PrimaryAuctionId { get; }
    public DateTime ExchangeTimestampUtc { get; }
    public DateTime? ReceiveTimestampUtc { get; }
    public long NormalizedPriceTick { get; }
    public decimal DecimalPrice { get; }
    public decimal ExecutedVolume { get; }
    public AggressorSide AggressorSide { get; }
    public bool AggressorClassified { get; }
    public decimal TickSize { get; }
    public string TimestampPolicy { get; }
    public OrderflowSourceCallbackKind SourceCallbackKind { get; }
    public bool IsHistorical { get; }
    public string Provenance { get; }
    public IReadOnlyList<string> AssociatedEpisodeIds { get; }

    public static ExecutedTradeEvent? TryFromNewTrade(
        NewTradeObservation obs,
        decimal tickSize,
        string dataEpoch,
        string primaryAuctionId,
        string timestampPolicyVersion,
        Func<decimal, long?> toTick,
        IReadOnlyList<string>? associatedEpisodeIds = null)
    {
        if (obs is null || tickSize <= 0m)
            return null;
        var tick = toTick(obs.Price);
        if (tick is null)
            return null;

        var classified = obs.IsAsk || obs.IsBid;
        AggressorSide side;
        if (obs.IsAsk && !obs.IsBid)
            side = AggressorSide.Ask;
        else if (obs.IsBid && !obs.IsAsk)
            side = AggressorSide.Bid;
        else
            side = AggressorSide.Unknown; // ambiguous or unclassified — never invent

        var eventId = EpisodeIdentity.BuildEventIdentity(obs.LocalMonotonicSequence, obs.CoreDiagnosticFingerprint);
        var source = obs.CallbackSource == TradeCallbackSource.OnNewTradesBatch
            ? OrderflowSourceCallbackKind.OnNewTradesBatch
            : OrderflowSourceCallbackKind.OnNewTrade;

        // Exchange wall time is not converted here; receive clock is authoritative for timing when Kind unknown.
        var exchangeUtc = obs.ReceiveUtc;
        return new ExecutedTradeEvent(
            eventId,
            obs.LocalMonotonicSequence,
            eventRevision: 1,
            obs.ObservedInstrumentIdentityKey,
            dataEpoch,
            primaryAuctionId,
            exchangeUtc,
            obs.ReceiveUtc,
            tick.Value,
            obs.Price,
            obs.Volume,
            side,
            classified && side != AggressorSide.Unknown,
            tickSize,
            timestampPolicyVersion,
            source,
            isHistorical: false,
            provenance: "NewTradeObservation|" + source,
                associatedEpisodeIds ?? Array.Empty<string>());
    }
}
