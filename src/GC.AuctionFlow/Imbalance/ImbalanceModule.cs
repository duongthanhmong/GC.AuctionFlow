using GC.AuctionFlow.Cluster;
using GC.AuctionFlow.Directional;

namespace GC.AuctionFlow.Imbalance;

/// <summary>
/// Phase 2H Imbalance classification (KDK Ch 25).
/// Ratio and minimum-volume rules are both calibrated, so no verdict is ever emitted.
/// </summary>
public sealed class ImbalancePolicyConfig
{
    public const string PolicyVersion = "IMBALANCE_POLICY_V1";

    public const string LimitationRatioRuleNotCalibrated = "IMBALANCE_RATIO_RULE_NOT_CALIBRATED";
    public const string LimitationMinimumVolumeNotCalibrated = "IMBALANCE_MINIMUM_VOLUME_NOT_CALIBRATED";
    public const string LimitationStackedRuleNotCalibrated = "STACKED_IMBALANCE_RULE_NOT_CALIBRATED";
    public const string LimitationNotAcceptanceEvidence = "IMBALANCE_IS_NOT_ACCEPTANCE_EVIDENCE";
    public const string LimitationNotPermanentSupportResistance = "IMBALANCE_IS_NOT_PERMANENT_SUPPORT_RESISTANCE";
    public const string LimitationAggressorCoveragePartial = "IMBALANCE_AGGRESSOR_COVERAGE_PARTIAL";
    public const string LimitationLocationUnavailable = "IMBALANCE_LOCATION_CONTEXT_UNAVAILABLE";
    public const string LimitationLiveOnly = "LIVE_ONLY_HISTORY";

    /// <summary>Levels surfaced per rebuild before truncation.</summary>
    public const int MaximumLevelsReported = 64;

    public ImbalancePolicyConfig(bool enabled = false)
    {
        Enabled = enabled;
    }

    public bool Enabled { get; }
    public string Version => PolicyVersion;
}

/// <summary>
/// One price level's execution asymmetry, in context.
///
/// KDK Ch 25: a very large ratio on a very small volume can be meaningless, so the
/// ratio is never carried without the volume it was computed from.
/// </summary>
public sealed class ImbalanceLevelSnapshot
{
    public ImbalanceLevelSnapshot(
        long priceTick,
        decimal decimalPrice,
        decimal? samePriceAskToBidRatio,
        decimal? samePriceBidToAskRatio,
        decimal? diagonalAskToBidBelowRatio,
        decimal? diagonalBidToAskAboveRatio,
        decimal classifiedVolume,
        decimal unknownAggressorVolume,
        decimal? aggressorCoverageRatio,
        ClusterRawDominantSide rawDominantSide,
        int consecutiveRawDominanceTicks,
        ImbalanceQualification qualification,
        StackedImbalanceState stacked,
        ImbalanceLocationContext locationContext)
    {
        PriceTick = priceTick;
        DecimalPrice = decimalPrice;
        SamePriceAskToBidRatio = samePriceAskToBidRatio;
        SamePriceBidToAskRatio = samePriceBidToAskRatio;
        DiagonalAskToBidBelowRatio = diagonalAskToBidBelowRatio;
        DiagonalBidToAskAboveRatio = diagonalBidToAskAboveRatio;
        ClassifiedVolume = classifiedVolume;
        UnknownAggressorVolume = unknownAggressorVolume;
        AggressorCoverageRatio = aggressorCoverageRatio;
        RawDominantSide = rawDominantSide;
        ConsecutiveRawDominanceTicks = consecutiveRawDominanceTicks;
        Qualification = qualification;
        Stacked = stacked;
        LocationContext = locationContext;
    }

    public long PriceTick { get; }
    public decimal DecimalPrice { get; }

    // Both comparison modes are carried: KDK Ch 25 names same-price and diagonal as
    // different questions, and does not rank one as universally correct.
    public decimal? SamePriceAskToBidRatio { get; }
    public decimal? SamePriceBidToAskRatio { get; }
    public decimal? DiagonalAskToBidBelowRatio { get; }
    public decimal? DiagonalBidToAskAboveRatio { get; }

    /// <summary>The volume the ratios were computed from. Never omitted.</summary>
    public decimal ClassifiedVolume { get; }

    /// <summary>Unknown-aggressor volume, disclosed rather than ignored (KDK Ch 25).</summary>
    public decimal UnknownAggressorVolume { get; }

    public decimal? AggressorCoverageRatio { get; }
    public ClusterRawDominantSide RawDominantSide { get; }

    /// <summary>Observable run length. How many levels make a stack is calibrated.</summary>
    public int ConsecutiveRawDominanceTicks { get; }

    /// <summary>Always Unknown / Unavailable / NotCalibrated.</summary>
    public ImbalanceQualification Qualification { get; }

    /// <summary>Always Unknown / Unavailable / NotCalibrated.</summary>
    public StackedImbalanceState Stacked { get; }

    public ImbalanceLocationContext LocationContext { get; }

    /// <summary>True when no ratio could be formed at all.</summary>
    public bool RatiosUnavailable =>
        !SamePriceAskToBidRatio.HasValue && !SamePriceBidToAskRatio.HasValue
        && !DiagonalAskToBidBelowRatio.HasValue && !DiagonalBidToAskAboveRatio.HasValue;
}

/// <summary>
/// Immutable Phase 2H imbalance set.
/// QualifiedCount and StackedCount are always 0 — no rule is calibrated.
/// </summary>
public sealed class ImbalanceSetSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public ImbalanceSetSnapshot(
        ImbalanceModuleState moduleState,
        string policyVersion,
        IReadOnlyList<ImbalanceLevelSnapshot> levels,
        int qualifiedCount,
        int stackedCount,
        int levelsWithoutRatioCount,
        ImbalanceLocationContext locationContext,
        ImbalanceDataQuality dataQuality,
        DateTime observedAtUtc,
        IReadOnlyList<string> limitations)
    {
        ModuleState = moduleState;
        PolicyVersion = policyVersion ?? ImbalancePolicyConfig.PolicyVersion;
        Levels = levels ?? Array.Empty<ImbalanceLevelSnapshot>();
        QualifiedCount = qualifiedCount;
        StackedCount = stackedCount;
        LevelsWithoutRatioCount = levelsWithoutRatioCount;
        LocationContext = locationContext;
        DataQuality = dataQuality;
        ObservedAtUtc = observedAtUtc;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public ImbalanceModuleState ModuleState { get; }
    public string PolicyVersion { get; }
    public IReadOnlyList<ImbalanceLevelSnapshot> Levels { get; }

    /// <summary>Always 0 — the qualifying rule is not calibrated.</summary>
    public int QualifiedCount { get; }

    /// <summary>Always 0 — the stacking rule is not calibrated.</summary>
    public int StackedCount { get; }

    /// <summary>Levels where aggressor classification produced no ratio at all.</summary>
    public int LevelsWithoutRatioCount { get; }

    public ImbalanceLocationContext LocationContext { get; }
    public ImbalanceDataQuality DataQuality { get; }
    public DateTime ObservedAtUtc { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;

    /// <summary>Longest observed same-side run across the reported levels.</summary>
    public int MaximumConsecutiveDominanceTicks =>
        Levels.Count == 0 ? 0 : Levels.Max(l => l.ConsecutiveRawDominanceTicks);
}

/// <summary>
/// Phase 2H Imbalance host (KDK Ch 25).
///
/// Phase 2B already measures every ratio this needs. What this phase adds is the
/// classification GATE and the location context KDK Ch 25 requires: imbalance
/// mid-value may be nothing more than part of a rotation, while imbalance at a
/// boundary inside an episode is more notable.
///
/// It emits no verdict. Both the ratio rule and the minimum-volume rule are
/// calibrated, and KDK is explicit that a huge ratio on tiny volume is meaningless.
/// </summary>
public sealed class ImbalanceHost
{
    private ImbalancePolicyConfig _policy;
    private ImbalanceSetSnapshot? _published;

    public ImbalanceHost(ImbalancePolicyConfig? policy = null)
    {
        _policy = policy ?? new ImbalancePolicyConfig(enabled: false);
    }

    public ImbalanceSetSnapshot? Current => _published;
    public ImbalancePolicyConfig Policy => _policy;

    public void Configure(ImbalancePolicyConfig policy)
    {
        if (policy is null) throw new ArgumentNullException(nameof(policy));
        _policy = policy;
        if (!_policy.Enabled)
            _published = DisabledSnapshot(DateTime.UtcNow);
    }

    public void Reset() => _published = null;

    public ImbalanceSetSnapshot Rebuild(
        ClusterRawSetSnapshot? cluster,
        ProfileLocationContextSnapshot? location = null,
        DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;

        if (!_policy.Enabled)
        {
            _published = DisabledSnapshot(now);
            return _published;
        }

        // Location is independent of cluster availability: knowing where price sits
        // does not require a populated ladder, so it is resolved before any early
        // return. Reporting Unavailable while the location IS known would be a lie.
        var locationContext = MapLocation(location);

        if (cluster is null || cluster.ModuleState == ClusterRawModuleState.Disabled)
        {
            _published = StatusSnapshot(ImbalanceModuleState.AwaitingCluster, now, locationContext);
            return _published;
        }

        if (cluster.ModuleState == ClusterRawModuleState.Invalid)
        {
            _published = StatusSnapshot(ImbalanceModuleState.Invalid, now, locationContext,
                new[] { "CLUSTER_INPUT_INVALID" });
            return _published;
        }

        var source = cluster.CurrentAuction?.PriceLevels ?? Array.Empty<ClusterRawPriceLevelSnapshot>();

        var levels = source
            .Take(ImbalancePolicyConfig.MaximumLevelsReported)
            .Select(l => MapLevel(l, locationContext))
            .ToArray();

        var withoutRatio = levels.Count(l => l.RatiosUnavailable);
        var quality = levels.Length == 0
            ? ImbalanceDataQuality.Partial
            : withoutRatio == levels.Length
                ? ImbalanceDataQuality.Partial
                : ImbalanceDataQuality.Complete;

        var state = levels.Length == 0
            ? ImbalanceModuleState.AwaitingCluster
            : quality == ImbalanceDataQuality.Partial
                ? ImbalanceModuleState.Partial
                : ImbalanceModuleState.Ready;

        _published = new ImbalanceSetSnapshot(
            state,
            ImbalancePolicyConfig.PolicyVersion,
            levels,
            // No rule is calibrated, so nothing can be counted as qualified or stacked.
            0, 0,
            withoutRatio,
            locationContext,
            quality,
            now,
            BuildLimitations(locationContext, withoutRatio > 0));

        return _published;
    }

    // --- mapping ---

    private static ImbalanceLevelSnapshot MapLevel(
        ClusterRawPriceLevelSnapshot l, ImbalanceLocationContext locationContext)
    {
        // A ratio needs classified volume on both sides. Without aggressor
        // classification there is no ratio at all — that is Unavailable, not zero.
        var anyRatio = l.SamePriceAskToBidRatio.HasValue || l.SamePriceBidToAskRatio.HasValue
                       || l.DiagonalAskToBidBelowRatio.HasValue || l.DiagonalBidToAskAboveRatio.HasValue;

        var qualification = anyRatio
            ? ImbalanceQualification.NotCalibrated
            : ImbalanceQualification.Unavailable;

        var stacked = anyRatio
            ? StackedImbalanceState.NotCalibrated
            : StackedImbalanceState.Unavailable;

        return new ImbalanceLevelSnapshot(
            l.PriceTick, l.DecimalPrice,
            l.SamePriceAskToBidRatio, l.SamePriceBidToAskRatio,
            l.DiagonalAskToBidBelowRatio, l.DiagonalBidToAskAboveRatio,
            l.ClassifiedVolume, l.UnknownAggressorVolume, l.AggressorCoverageRatio,
            l.RawDominantSide, l.ConsecutiveRawDominanceTicks,
            qualification, stacked, locationContext);
    }

    /// <summary>
    /// KDK Ch 25 "Vị trí". Volume profile is preferred over TPO for the same reason as
    /// elsewhere: it reflects executed activity rather than time distribution.
    /// </summary>
    private static ImbalanceLocationContext MapLocation(ProfileLocationContextSnapshot? location)
    {
        if (location is null) return ImbalanceLocationContext.Unavailable;

        var loc = location.CurrentPrimaryVolume != PriceValueLocation.Unavailable
            ? location.CurrentPrimaryVolume
            : location.CurrentPrimaryTpo;

        return loc switch
        {
            PriceValueLocation.InsideValue => ImbalanceLocationContext.MidValue,
            PriceValueLocation.AtPoc => ImbalanceLocationContext.MidValue,
            PriceValueLocation.AtValueHigh => ImbalanceLocationContext.ValueBoundary,
            PriceValueLocation.AtValueLow => ImbalanceLocationContext.ValueBoundary,
            PriceValueLocation.AboveValue => ImbalanceLocationContext.OutsideValue,
            PriceValueLocation.BelowValue => ImbalanceLocationContext.OutsideValue,
            _ => ImbalanceLocationContext.Unavailable
        };
    }

    // --- helpers ---

    private static IReadOnlyList<string> BuildLimitations(
        ImbalanceLocationContext locationContext, bool anyRatioMissing)
    {
        var lim = new List<string>
        {
            ImbalancePolicyConfig.LimitationRatioRuleNotCalibrated,
            ImbalancePolicyConfig.LimitationMinimumVolumeNotCalibrated,
            ImbalancePolicyConfig.LimitationStackedRuleNotCalibrated,
            // AP-018: imbalance measures execution asymmetry, never acceptance.
            ImbalancePolicyConfig.LimitationNotAcceptanceEvidence,
            // KDK Ch 25 mistake: treating a stack as permanent support or resistance.
            ImbalancePolicyConfig.LimitationNotPermanentSupportResistance,
            ImbalancePolicyConfig.LimitationLiveOnly
        };
        if (anyRatioMissing) lim.Add(ImbalancePolicyConfig.LimitationAggressorCoveragePartial);
        if (locationContext == ImbalanceLocationContext.Unavailable)
            lim.Add(ImbalancePolicyConfig.LimitationLocationUnavailable);
        return lim;
    }

    private static ImbalanceSetSnapshot DisabledSnapshot(DateTime now) =>
        new(ImbalanceModuleState.Disabled,
            ImbalancePolicyConfig.PolicyVersion,
            Array.Empty<ImbalanceLevelSnapshot>(),
            0, 0, 0,
            ImbalanceLocationContext.Unavailable,
            ImbalanceDataQuality.Partial,
            now,
            new[] { "MODULE_DISABLED" });

    private static ImbalanceSetSnapshot StatusSnapshot(
        ImbalanceModuleState state, DateTime now,
        ImbalanceLocationContext locationContext, string[]? extra = null)
    {
        var lim = new List<string>
        {
            ImbalancePolicyConfig.LimitationRatioRuleNotCalibrated,
            ImbalancePolicyConfig.LimitationMinimumVolumeNotCalibrated,
            ImbalancePolicyConfig.LimitationNotAcceptanceEvidence,
            ImbalancePolicyConfig.LimitationNotPermanentSupportResistance,
            ImbalancePolicyConfig.LimitationLiveOnly
        };
        if (locationContext == ImbalanceLocationContext.Unavailable)
            lim.Add(ImbalancePolicyConfig.LimitationLocationUnavailable);
        if (extra is not null) lim.AddRange(extra);
        return new ImbalanceSetSnapshot(
            state, ImbalancePolicyConfig.PolicyVersion,
            Array.Empty<ImbalanceLevelSnapshot>(),
            0, 0, 0,
            locationContext,
            ImbalanceDataQuality.Partial,
            now, lim);
    }
}
