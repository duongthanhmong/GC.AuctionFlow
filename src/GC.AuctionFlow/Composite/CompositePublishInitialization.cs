using GC.AuctionFlow.Profile;

namespace GC.AuctionFlow.Composite;

/// <summary>
/// Normalized Phase 1B Composite operator settings used for change detection on publish paths.
/// Built from <see cref="CompositePolicyConfig"/> so normalization cannot drift.
/// Value equality only — no hash/I/O/JSON.
/// </summary>
public readonly struct CompositeOperatorConfiguration : IEquatable<CompositeOperatorConfiguration>
{
    public CompositeOperatorConfiguration(
        string? anchorAuctionId,
        bool includeThroughLatestCompletedAuction,
        string excludedAuctionIdsKey,
        bool enableDevelopingCompositePreview,
        bool enableShadowEvidence,
        string policyVersion)
    {
        AnchorAuctionId = anchorAuctionId;
        IncludeThroughLatestCompletedAuction = includeThroughLatestCompletedAuction;
        ExcludedAuctionIdsKey = excludedAuctionIdsKey ?? "";
        EnableDevelopingCompositePreview = enableDevelopingCompositePreview;
        EnableShadowEvidence = enableShadowEvidence;
        PolicyVersion = policyVersion ?? "";
    }

    public string? AnchorAuctionId { get; }
    public bool IncludeThroughLatestCompletedAuction { get; }
    /// <summary>Comma-joined excluded ids already ordered/distinct by <see cref="CompositePolicyConfig"/>.</summary>
    public string ExcludedAuctionIdsKey { get; }
    public bool EnableDevelopingCompositePreview { get; }
    public bool EnableShadowEvidence { get; }
    public string PolicyVersion { get; }

    public static CompositeOperatorConfiguration FromPolicy(CompositePolicyConfig policy)
    {
        if (policy is null) throw new ArgumentNullException(nameof(policy));
        var excluded = policy.ExcludedAuctionIds;
        var key = excluded.Count == 0
            ? ""
            : string.Join(",", excluded);
        return new CompositeOperatorConfiguration(
            policy.AnchorAuctionId,
            policy.IncludeThroughLatestCompletedAuction,
            key,
            policy.EnableDevelopingCompositePreview,
            policy.EnableShadowEvidence,
            policy.Version);
    }

    public bool Equals(CompositeOperatorConfiguration other) =>
        string.Equals(AnchorAuctionId, other.AnchorAuctionId, StringComparison.Ordinal)
        && IncludeThroughLatestCompletedAuction == other.IncludeThroughLatestCompletedAuction
        && string.Equals(ExcludedAuctionIdsKey, other.ExcludedAuctionIdsKey, StringComparison.Ordinal)
        && EnableDevelopingCompositePreview == other.EnableDevelopingCompositePreview
        && EnableShadowEvidence == other.EnableShadowEvidence
        && string.Equals(PolicyVersion, other.PolicyVersion, StringComparison.Ordinal);

    public override bool Equals(object? obj) =>
        obj is CompositeOperatorConfiguration other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(
            AnchorAuctionId,
            IncludeThroughLatestCompletedAuction,
            ExcludedAuctionIdsKey,
            EnableDevelopingCompositePreview,
            EnableShadowEvidence,
            PolicyVersion);

    public static bool operator ==(CompositeOperatorConfiguration left, CompositeOperatorConfiguration right) =>
        left.Equals(right);

    public static bool operator !=(CompositeOperatorConfiguration left, CompositeOperatorConfiguration right) =>
        !left.Equals(right);
}

/// <summary>
/// Guard for Composite Configure/Rebuild on GPS publish paths.
/// Rebuilds when host/Current missing OR operator configuration fingerprint changed.
/// Does not rebuild on ordinary unchanged trade publishes.
/// </summary>
public static class CompositePublishInitialization
{
    /// <summary>
    /// Legacy one-shot missing-Current check (kept for narrow call sites).
    /// Prefer <see cref="ShouldProcess"/>.
    /// </summary>
    public static bool ShouldInitialize(
        bool enableCompositeProfile,
        bool primaryProfileAvailable,
        bool compositeCurrentMissing) =>
        enableCompositeProfile && primaryProfileAvailable && compositeCurrentMissing;

    public static bool ShouldInitialize(
        bool enableCompositeProfile,
        PrimaryProfileSetSnapshot? profiles,
        CompositeProfileHost? host) =>
        ShouldInitialize(
            enableCompositeProfile,
            profiles is not null,
            host is null || host.Current is null);

    /// <summary>
    /// True when Composite should Configure/Rebuild before publishing GPS.
    /// </summary>
    public static bool ShouldProcess(
        bool enableCompositeProfile,
        bool primaryProfileAvailable,
        bool compositeCurrentMissing,
        CompositeOperatorConfiguration? lastSuccessfullyApplied,
        CompositeOperatorConfiguration currentOperatorConfiguration)
    {
        if (!enableCompositeProfile || !primaryProfileAvailable)
            return false;
        if (compositeCurrentMissing || lastSuccessfullyApplied is null)
            return true;
        return !lastSuccessfullyApplied.Value.Equals(currentOperatorConfiguration);
    }

    public static bool ShouldProcess(
        bool enableCompositeProfile,
        PrimaryProfileSetSnapshot? profiles,
        CompositeProfileHost? host,
        CompositeOperatorConfiguration? lastSuccessfullyApplied,
        CompositeOperatorConfiguration currentOperatorConfiguration) =>
        ShouldProcess(
            enableCompositeProfile,
            profiles is not null,
            host is null || host.Current is null,
            lastSuccessfullyApplied,
            currentOperatorConfiguration);
}
