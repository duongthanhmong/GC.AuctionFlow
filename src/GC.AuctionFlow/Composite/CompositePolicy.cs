namespace GC.AuctionFlow.Composite;

/// <summary>Production composite policy. No hard N-day automatic merge.</summary>
public enum CompositePolicyMode
{
    Disabled = 0,
    /// <summary>Confirmed composite from operator anchor through latest completed (minus exclusions).</summary>
    OperatorAnchored = 1,
    /// <summary>Research/shadow evidence only — must not mutate confirmed composite.</summary>
    ShadowEvidence = 2
}

public enum CompositeStatus
{
    Disabled = 0,
    AwaitingAnchor = 1,
    Building = 2,
    Partial = 3,
    Ready = 4,
    Invalid = 5
}

public enum CompositeCapabilityState
{
    Disabled = 0,
    AwaitingAnchor = 1,
    NotReady = 2,
    Partial = 3,
    Ready = 4,
    Invalid = 5
}

/// <summary>Shadow-only descriptive evidence. Never mutates confirmed composite.</summary>
public enum CompositeEvidenceState
{
    NotEvaluated = 0,
    InsufficientData = 1,
    MergeEvidencePresent = 2,
    SeparationEvidencePresent = 3,
    Conflicted = 4,
    Invalid = 5,
    NotCalibrated = 6
}

/// <summary>Operator-anchored composite settings. Shadow thresholds stay unset (not calibrated).</summary>
public sealed class CompositePolicyConfig
{
    public const string PolicyVersion = "COMPOSITE_POLICY_V1";

    public CompositePolicyConfig(
        CompositePolicyMode mode = CompositePolicyMode.OperatorAnchored,
        string? anchorAuctionId = null,
        bool includeThroughLatestCompletedAuction = true,
        IReadOnlyList<string>? excludedAuctionIds = null,
        bool enableDevelopingCompositePreview = false,
        bool enableShadowEvidence = false,
        decimal? shadowMinValueOverlapRatio = null,
        long? shadowMaxPocDisplacementTicks = null)
    {
        Mode = mode;
        AnchorAuctionId = string.IsNullOrWhiteSpace(anchorAuctionId) ? null : anchorAuctionId.Trim();
        IncludeThroughLatestCompletedAuction = includeThroughLatestCompletedAuction;
        ExcludedAuctionIds = (excludedAuctionIds ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        EnableDevelopingCompositePreview = enableDevelopingCompositePreview;
        EnableShadowEvidence = enableShadowEvidence;
        ShadowMinValueOverlapRatio = shadowMinValueOverlapRatio;
        ShadowMaxPocDisplacementTicks = shadowMaxPocDisplacementTicks;
    }

    public CompositePolicyMode Mode { get; }
    public string? AnchorAuctionId { get; }
    public bool IncludeThroughLatestCompletedAuction { get; }
    public IReadOnlyList<string> ExcludedAuctionIds { get; }
    public bool EnableDevelopingCompositePreview { get; }
    public bool EnableShadowEvidence { get; }
    /// <summary>Research/Shadow-only. Null = not calibrated.</summary>
    public decimal? ShadowMinValueOverlapRatio { get; }
    /// <summary>Research/Shadow-only. Null = not calibrated.</summary>
    public long? ShadowMaxPocDisplacementTicks { get; }
    public string Version => PolicyVersion;
}
