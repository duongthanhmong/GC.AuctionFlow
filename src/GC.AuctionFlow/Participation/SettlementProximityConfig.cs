namespace GC.AuctionFlow.Participation;

/// <summary>Spec §10.5. Settlement anchor at 13:30 ET with configurable proximity windows.</summary>
public sealed class SettlementProximityConfig
{
    public static readonly TimeSpan DefaultAnchorLocalTime = new(13, 30, 0);

    public TimeSpan AnchorLocalTime { get; init; } = DefaultAnchorLocalTime;
    public TimeSpan PreSettlementWindow { get; init; } = TimeSpan.FromMinutes(30);
    public TimeSpan SettlementTransitionWindow { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan PostSettlementWindow { get; init; } = TimeSpan.FromMinutes(30);
}
