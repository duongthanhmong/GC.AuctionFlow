namespace GC.AuctionFlow.Episode;

/// <summary>Phase 1E Auction Episode Observation policy. Geometric observation only — no acceptance/signal.</summary>
public sealed class EpisodePolicyConfig
{
    public const string PolicyVersion = "AUCTION_EPISODE_POLICY_V1";

    public const string LimitationIntraAuctionResetNotCalibrated = "INTRA_AUCTION_EPISODE_RESET_NOT_CALIBRATED";
    public const string LimitationApproachDistanceNotCalibrated = "APPROACH_DISTANCE_NOT_CALIBRATED";
    public const string LimitationDevelopingNotAuthorized = "DEVELOPING_REFERENCE_EPISODES_NOT_AUTHORIZED";
    public const string LimitationHistoryLiveOnly = "EPISODE_HISTORY: LIVE_ONLY";
    public const string LimitationLocalValueNotAuthorized = "EPISODE_LOCAL_VALUE_POLICY_NOT_AUTHORIZED";
    public const string LimitationPrimaryAuctionChanged = "PRIMARY_AUCTION_CHANGED";

    public EpisodePolicyConfig(bool enabled = false)
    {
        Enabled = enabled;
    }

    public bool Enabled { get; }
    public string Version => PolicyVersion;
}
