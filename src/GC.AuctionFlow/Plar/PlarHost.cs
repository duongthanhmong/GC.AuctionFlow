using GC.AuctionFlow.Reference;

namespace GC.AuctionFlow.Plar;

/// <summary>
/// Phase 3E Path of Least Auction Resistance host (v1.2 §33, v1.3 §7.4).
///
/// Projects the structural reference set forward from the current price in both
/// directions and orders what lies ahead. Everything here is geometry: distance,
/// ordering, and role. Nothing estimates whether a barrier will hold — that needs
/// reaction history and adjacent-build research, neither of which is calibrated.
/// </summary>
public sealed class PlarHost
{
    private PlarPolicyConfig _policy;
    private PlarSetSnapshot? _published;
    private string? _lastFingerprintKey;
    private DateTime _createdAtUtc;

    public PlarHost(PlarPolicyConfig? policy = null)
    {
        _policy = policy ?? new PlarPolicyConfig(enabled: false);
    }

    public PlarSetSnapshot? Current => _published;
    public PlarPolicyConfig Policy => _policy;

    public void Configure(PlarPolicyConfig policy)
    {
        if (policy is null) throw new ArgumentNullException(nameof(policy));
        _policy = policy;
        if (!_policy.Enabled)
        {
            _published = DisabledSnapshot(DateTime.UtcNow);
            _lastFingerprintKey = null;
        }
    }

    public void Reset()
    {
        _published = null;
        _lastFingerprintKey = null;
        _createdAtUtc = default;
    }

    /// <summary>
    /// Rebuild both paths from the reference set at the given price.
    /// Fingerprint-gated on price and reference-set identity.
    /// </summary>
    public PlarSetSnapshot Rebuild(
        IReadOnlyList<StructuralReferenceSnapshot>? references,
        long? currentPriceTick,
        DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;

        if (!_policy.Enabled)
        {
            _published = DisabledSnapshot(now);
            return _published;
        }

        var fp = BuildFingerprintKey(references, currentPriceTick);
        if (_lastFingerprintKey is not null
            && string.Equals(_lastFingerprintKey, fp, StringComparison.Ordinal)
            && _published is not null)
            return _published;

        if (_createdAtUtc == default)
            _createdAtUtc = now;

        // Without a price there is no "ahead", so no path can be projected.
        // Without references there is nothing to project onto.
        var usable = references?.Where(IsUsable).ToArray() ?? Array.Empty<StructuralReferenceSnapshot>();
        if (!currentPriceTick.HasValue || usable.Length == 0)
        {
            _published = new PlarSetSnapshot(
                PlarModuleState.AwaitingReferences,
                PlarPolicyConfig.PolicyVersion,
                UnavailablePath(PathDirection.Up, currentPriceTick ?? 0L, now),
                UnavailablePath(PathDirection.Down, currentPriceTick ?? 0L, now),
                usable.Length,
                _createdAtUtc, now,
                BuildSetLimitations(referencesAvailable: usable.Length > 0));
            _lastFingerprintKey = fp;
            return _published;
        }

        var price = currentPriceTick.Value;
        var up = BuildPath(PathDirection.Up, price, usable, now);
        var down = BuildPath(PathDirection.Down, price, usable, now);

        _published = new PlarSetSnapshot(
            PlarModuleState.Ready,
            PlarPolicyConfig.PolicyVersion,
            up, down,
            usable.Length,
            _createdAtUtc, now,
            BuildSetLimitations(referencesAvailable: true));

        _lastFingerprintKey = fp;
        return _published;
    }

    // --- path construction ---

    private static AuctionPathSnapshot BuildPath(
        PathDirection direction, long priceTick,
        IReadOnlyList<StructuralReferenceSnapshot> references, DateTime nowUtc)
    {
        var ahead = new List<PathObstacleSnapshot>();

        foreach (var r in references)
        {
            // A reference is "ahead" when its near edge is beyond the current price
            // in the direction of travel. Price inside a zone means it is already
            // being interacted with, not ahead of us.
            long distance;
            if (direction == PathDirection.Up)
            {
                if (r.ZoneLowTick <= priceTick) continue;
                distance = r.ZoneLowTick - priceTick;
            }
            else
            {
                if (r.ZoneHighTick >= priceTick) continue;
                distance = priceTick - r.ZoneHighTick;
            }

            ahead.Add(new PathObstacleSnapshot(
                r.ReferenceId, r.ReferenceType, r.Maturity,
                ClassifyRole(r.ReferenceType),
                distance, r.ZoneLow, r.ZoneHigh,
                corridorIndex: 0,
                BarrierPermeability.NotCalibrated));
        }

        var ordered = ahead
            .OrderBy(o => o.DistanceTicks)
            .Take(PlarPolicyConfig.MaximumObstaclesPerPath)
            .Select((o, i) => new PathObstacleSnapshot(
                o.ReferenceId, o.ReferenceType, o.Maturity, o.Role,
                o.DistanceTicks, o.ZoneLow, o.ZoneHigh,
                corridorIndex: i, o.Permeability))
            .ToArray();

        var nearestBarrier = ordered.FirstOrDefault(o => o.IsBarrier);
        var nearestTarget = ordered.FirstOrDefault(o => o.IsTarget);
        var finalTarget = ordered.LastOrDefault(o => o.IsTarget);

        // References existed, so "nothing ahead" is a real answer, not missing data.
        var availability = nearestTarget is null
            ? TargetSpaceAvailability.NoTargetAhead
            : TargetSpaceAvailability.Available;

        long? remaining = nearestTarget?.DistanceTicks ?? 0L;

        return new AuctionPathSnapshot(
            BuildPathId(direction, priceTick),
            PlarPolicyConfig.PolicyVersion,
            direction, priceTick,
            ordered, nearestBarrier, nearestTarget, finalTarget,
            availability, remaining,
            PlarDataQuality.Complete,
            nowUtc,
            BuildPathLimitations());
    }

    /// <summary>
    /// v1.3 §7.4 G-FAR-006: POC is both a destination and an obstacle. Modelling it
    /// only as a target is the specific mistake the guard names, because a strong
    /// reaction at POC can end a rotation before the opposite edge is reached
    /// (KDK Ch 19 Rule 4).
    /// </summary>
    private static PathObstacleRole ClassifyRole(ReferenceType t) => t switch
    {
        ReferenceType.PreviousPrimaryTpoPoc => PathObstacleRole.TargetAndBarrier,
        ReferenceType.PreviousPrimaryVpoc => PathObstacleRole.TargetAndBarrier,
        ReferenceType.CurrentPrimaryTpoPoc => PathObstacleRole.TargetAndBarrier,
        ReferenceType.CurrentPrimaryVpoc => PathObstacleRole.TargetAndBarrier,
        ReferenceType.CompositeTpoPoc => PathObstacleRole.TargetAndBarrier,
        ReferenceType.CompositeVpoc => PathObstacleRole.TargetAndBarrier,

        // Value edges and range extremes are where rotations terminate.
        ReferenceType.PreviousPrimaryTpoVah => PathObstacleRole.Target,
        ReferenceType.PreviousPrimaryTpoVal => PathObstacleRole.Target,
        ReferenceType.PreviousPrimaryVolumeVah => PathObstacleRole.Target,
        ReferenceType.PreviousPrimaryVolumeVal => PathObstacleRole.Target,
        ReferenceType.PreviousPrimaryAuctionHigh => PathObstacleRole.Target,
        ReferenceType.PreviousPrimaryAuctionLow => PathObstacleRole.Target,
        ReferenceType.CurrentPrimaryTpoVah => PathObstacleRole.Target,
        ReferenceType.CurrentPrimaryTpoVal => PathObstacleRole.Target,
        ReferenceType.CurrentPrimaryVolumeVah => PathObstacleRole.Target,
        ReferenceType.CurrentPrimaryVolumeVal => PathObstacleRole.Target,
        ReferenceType.CurrentPrimaryAuctionHigh => PathObstacleRole.Target,
        ReferenceType.CurrentPrimaryAuctionLow => PathObstacleRole.Target,
        ReferenceType.CompositeTpoVah => PathObstacleRole.Target,
        ReferenceType.CompositeTpoVal => PathObstacleRole.Target,
        ReferenceType.CompositeVolumeVah => PathObstacleRole.Target,
        ReferenceType.CompositeVolumeVal => PathObstacleRole.Target,
        ReferenceType.CompositeRangeHigh => PathObstacleRole.Target,
        ReferenceType.CompositeRangeLow => PathObstacleRole.Target,

        _ => PathObstacleRole.Unknown
    };

    private static bool IsUsable(StructuralReferenceSnapshot r) =>
        r.Status == ReferenceStatus.Active;

    // --- helpers ---

    private static string BuildPathId(PathDirection direction, long priceTick) =>
        "PLAR|" + direction + "|" + priceTick.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static AuctionPathSnapshot UnavailablePath(
        PathDirection direction, long priceTick, DateTime nowUtc) =>
        new(BuildPathId(direction, priceTick),
            PlarPolicyConfig.PolicyVersion,
            direction, priceTick,
            Array.Empty<PathObstacleSnapshot>(),
            null, null, null,
            // Cannot measure is NOT the same as no room. G-LOC-002 depends on this.
            TargetSpaceAvailability.Unavailable,
            null,
            PlarDataQuality.Partial,
            nowUtc,
            BuildPathLimitations());

    private static string BuildFingerprintKey(
        IReadOnlyList<StructuralReferenceSnapshot>? references, long? priceTick)
    {
        var refPart = references is null || references.Count == 0
            ? "0"
            : references.Count + ":" + references.Max(r => r.StateVersion);
        return PlarPolicyConfig.PolicyVersion
               + "|P:" + (priceTick?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "")
               + "|R:" + refPart;
    }

    private static IReadOnlyList<string> BuildPathLimitations() => new[]
    {
        PlarPolicyConfig.LimitationNotCalibrated,
        PlarPolicyConfig.LimitationPermeabilityNotCalibrated,
        PlarPolicyConfig.LimitationReactionHistoryUnavailable,
        PlarPolicyConfig.LimitationAdjacentBuildResearchOnly,
        PlarPolicyConfig.LimitationNoProgressVerdict,
        PlarPolicyConfig.LimitationLiveOnly
    };

    private static IReadOnlyList<string> BuildSetLimitations(bool referencesAvailable)
    {
        var lim = new List<string>
        {
            PlarPolicyConfig.LimitationNotCalibrated,
            PlarPolicyConfig.LimitationPermeabilityNotCalibrated,
            PlarPolicyConfig.LimitationNoEntryPlan,
            PlarPolicyConfig.LimitationNoTakeProfitLadder,
            PlarPolicyConfig.LimitationNoProgressVerdict,
            PlarPolicyConfig.LimitationLiveOnly
        };
        if (!referencesAvailable)
            lim.Add(PlarPolicyConfig.LimitationReferencesUnavailable);
        return lim;
    }

    private static PlarSetSnapshot DisabledSnapshot(DateTime now) =>
        new(PlarModuleState.Disabled,
            PlarPolicyConfig.PolicyVersion,
            null, null, 0, now, now,
            new[] { "MODULE_DISABLED" });
}
