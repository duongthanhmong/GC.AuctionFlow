namespace GC.AuctionFlow.Memory;

/// <summary>
/// Phase 1I Price Memory / Retest Ledger (v1.3 §13, KDK Ch 27).
/// Records how often each reference has been tested and how each test ended.
/// It never concludes that a reference is strong or weak.
/// </summary>
public sealed class PriceMemoryPolicyConfig
{
    public const string PolicyVersion = "PRICE_MEMORY_POLICY_V1";

    public const string LimitationStrengthNotCalibrated = "REFERENCE_STRENGTH_NOT_CALIBRATED";
    public const string LimitationOutcomeNotCalibrated = "REFERENCE_TEST_OUTCOME_NOT_CALIBRATED";
    public const string LimitationNoDirectionalDecayRule = "NO_ONE_DIRECTIONAL_REFERENCE_DECAY_RULE";
    public const string LimitationReplenishmentUnavailable = "LIQUIDITY_REPLENISHMENT_UNOBSERVABLE_MBO_BLOCKED";
    public const string LimitationLiveOnly = "LIVE_ONLY_HISTORY";
    public const string LimitationMemoryStartsAtIndicatorStart = "PRICE_MEMORY_STARTS_AT_INDICATOR_START";

    /// <summary>Test records retained per reference before the oldest is dropped.</summary>
    public const int TestRecordCapacity = 64;

    /// <summary>References tracked before the least recently tested is dropped.</summary>
    public const int ReferenceCapacity = 256;

    public PriceMemoryPolicyConfig(bool enabled = false)
    {
        Enabled = enabled;
    }

    public bool Enabled { get; }
    public string Version => PolicyVersion;
}
