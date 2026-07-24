using GC.AuctionFlow.Probe;

namespace GC.AuctionFlow.Episode;

/// <summary>
/// Normalized trade print for Episode Observation.
/// Built from NewTradeObservation — never from candles/GPS/overlay text.
/// </summary>
public sealed class EpisodeTradeEvent
{
    public EpisodeTradeEvent(
        string eventIdentity,
        long localMonotonicSequence,
        long sourceTimeTicks,
        DateTime receiveUtc,
        long priceTick,
        decimal price,
        decimal volume,
        bool isAsk,
        bool isBid,
        bool aggressorClassified,
        string instrumentIdentity,
        string dataEpoch,
        decimal tickSize,
        string timestampPolicyVersion)
    {
        EventIdentity = eventIdentity ?? "";
        LocalMonotonicSequence = localMonotonicSequence;
        SourceTimeTicks = sourceTimeTicks;
        ReceiveUtc = receiveUtc;
        PriceTick = priceTick;
        Price = price;
        Volume = volume;
        IsAsk = isAsk;
        IsBid = isBid;
        AggressorClassified = aggressorClassified;
        InstrumentIdentity = instrumentIdentity ?? "";
        DataEpoch = dataEpoch ?? "";
        TickSize = tickSize;
        TimestampPolicyVersion = timestampPolicyVersion ?? "";
    }

    public string EventIdentity { get; }
    public long LocalMonotonicSequence { get; }
    public long SourceTimeTicks { get; }
    public DateTime ReceiveUtc { get; }
    public long PriceTick { get; }
    public decimal Price { get; }
    public decimal Volume { get; }
    public bool IsAsk { get; }
    public bool IsBid { get; }
    public bool AggressorClassified { get; }
    public string InstrumentIdentity { get; }
    public string DataEpoch { get; }
    public decimal TickSize { get; }
    public string TimestampPolicyVersion { get; }

    public static EpisodeTradeEvent? TryFromNewTrade(
        NewTradeObservation obs,
        decimal tickSize,
        string dataEpoch,
        string timestampPolicyVersion,
        Func<decimal, long?> toTick)
    {
        if (obs is null || tickSize <= 0m)
            return null;
        var tick = toTick(obs.Price);
        if (tick is null)
            return null;

        var classified = obs.IsAsk || obs.IsBid;
        var id = EpisodeIdentity.BuildEventIdentity(obs.LocalMonotonicSequence, obs.CoreDiagnosticFingerprint);
        return new EpisodeTradeEvent(
            id,
            obs.LocalMonotonicSequence,
            obs.SourceTimeTicks,
            obs.ReceiveUtc,
            tick.Value,
            obs.Price,
            obs.Volume,
            obs.IsAsk,
            obs.IsBid,
            classified,
            obs.ObservedInstrumentIdentityKey,
            dataEpoch,
            tickSize,
            timestampPolicyVersion);
    }
}
