using GC.AuctionFlow.Profile;

namespace GC.AuctionFlow.Participation;

/// <summary>Spec §10.5. Pure time-based settlement proximity tag. No trading signal. No veto authority.</summary>
public static class SettlementProximityClassifier
{
    private static readonly TimeZoneInfo _et = AuctionTimezoneResolver.Resolve();

    public static SettlementProximitySnapshot Classify(DateTime utcTimestamp, SettlementProximityConfig? config = null)
    {
        config ??= new SettlementProximityConfig();
        var et = TimeZoneInfo.ConvertTimeFromUtc(utcTimestamp, _et);
        var offset = et.TimeOfDay - config.AnchorLocalTime;
        var tag = ClassifyOffset(offset, config);
        return new SettlementProximitySnapshot(
            tag, offset, AuctionTimezoneResolver.IanaAmericaNewYork, Array.Empty<string>());
    }

    private static SettlementProximityTag ClassifyOffset(TimeSpan offset, SettlementProximityConfig config)
    {
        var abs = offset < TimeSpan.Zero ? -offset : offset;
        if (abs <= config.SettlementTransitionWindow)
            return SettlementProximityTag.SettlementTransition;
        if (offset < TimeSpan.Zero && abs <= config.PreSettlementWindow)
            return SettlementProximityTag.PreSettlement;
        if (offset > TimeSpan.Zero && abs <= config.PostSettlementWindow)
            return SettlementProximityTag.PostSettlement;
        return SettlementProximityTag.Unknown;
    }
}
