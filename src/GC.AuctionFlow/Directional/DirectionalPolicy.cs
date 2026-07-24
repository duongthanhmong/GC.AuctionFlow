namespace GC.AuctionFlow.Directional;

/// <summary>Phase 1D Directional Context policy. Categorical evidence only — no score/threshold.</summary>
public sealed class DirectionalPolicyConfig
{
    public const string PolicyVersion = "DIRECTIONAL_CONTEXT_POLICY_V1";

    public DirectionalPolicyConfig(
        bool enabled = false,
        bool enableOneTimeFraming = true)
    {
        Enabled = enabled;
        EnableOneTimeFraming = enableOneTimeFraming;
    }

    public bool Enabled { get; }
    /// <summary>When Directional enabled, OTF defaults ON. No confirmation-count setting.</summary>
    public bool EnableOneTimeFraming { get; }
    public string Version => PolicyVersion;
}
