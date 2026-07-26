namespace GC.AuctionFlow.Plar;

/// <summary>
/// Phase 3E Path of Least Auction Resistance (v1.2 §33, v1.3 §7.4).
/// Geometry and ordering only — no friction estimate, no entry, no TP ladder.
/// </summary>
public sealed class PlarPolicyConfig
{
    public const string PolicyVersion = "PLAR_POLICY_V1";

    public const string LimitationNotCalibrated = "PLAR_FRICTION_NOT_CALIBRATED";
    public const string LimitationPermeabilityNotCalibrated = "BARRIER_PERMEABILITY_NOT_CALIBRATED";
    public const string LimitationReactionHistoryUnavailable = "BARRIER_REACTION_HISTORY_UNAVAILABLE";
    public const string LimitationAdjacentBuildResearchOnly = "ADJACENT_BUILD_RATIO_RESEARCH_ONLY";
    public const string LimitationNoEntryPlan = "PLAR_NO_ENTRY_PLAN";
    public const string LimitationNoTakeProfitLadder = "PLAR_NO_TAKE_PROFIT_LADDER";
    public const string LimitationNoProgressVerdict = "PLAR_PROGRESS_MILESTONE_NOT_CALIBRATED";
    public const string LimitationDirectionUnavailable = "PLAR_DIRECTION_UNAVAILABLE";
    public const string LimitationReferencesUnavailable = "PLAR_REFERENCES_UNAVAILABLE";
    public const string LimitationLiveOnly = "LIVE_ONLY_HISTORY";

    /// <summary>v1.2 §33.2 corridor depth: Barrier 1..3 then Final Target.</summary>
    public const int CorridorBarrierCapacity = 3;

    /// <summary>Obstacles retained per path before truncation.</summary>
    public const int MaximumObstaclesPerPath = 32;

    public PlarPolicyConfig(bool enabled = false)
    {
        Enabled = enabled;
    }

    public bool Enabled { get; }
    public string Version => PolicyVersion;
}
