namespace GC.AuctionFlow.Reference;

/// <summary>Phase 1C Structural Reference policy. Profile-derived, exact-tick, no score/tolerance.</summary>
public sealed class ReferencePolicyConfig
{
    public const string PolicyVersion = "REFERENCE_POLICY_V1";

    public ReferencePolicyConfig(bool enabled = false)
    {
        Enabled = enabled;
    }

    public bool Enabled { get; }
    public string Version => PolicyVersion;
}
