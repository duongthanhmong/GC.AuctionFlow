using GC.AuctionFlow.Core;
using GC.AuctionFlow.Profile;

namespace GC.AuctionFlow.Reference;

/// <summary>
/// Exact-tick confluence only. Adjacent ticks do not group. No strength score.
/// </summary>
public static class ReferenceConfluenceBuilder
{
    public static IReadOnlyList<ReferenceConfluenceGroup> Build(
        IReadOnlyList<StructuralReferenceSnapshot> active)
    {
        if (active is null || active.Count == 0)
            return Array.Empty<ReferenceConfluenceGroup>();

        var groups = active
            .GroupBy(r => (r.ZoneLowTick, r.ZoneHighTick))
            .OrderBy(g => g.Key.ZoneLowTick)
            .ThenBy(g => g.Key.ZoneHighTick)
            .Select(g =>
            {
                var members = g
                    .OrderBy(x => x.ReferenceId, StringComparer.Ordinal)
                    .ToArray();
                var first = members[0];
                return new ReferenceConfluenceGroup(
                    first.ZoneLow,
                    first.ZoneHigh,
                    first.ZoneLowTick,
                    first.ZoneHighTick,
                    members.Select(m => m.ReferenceId).ToArray(),
                    members.Select(m => m.ReferenceType).ToArray(),
                    members.Count(m => m.Maturity == ReferenceMaturity.Confirmed),
                    members.Count(m => m.Maturity == ReferenceMaturity.Developing),
                    members.Select(m => m.SourceHorizon).Distinct().OrderBy(h => h).ToArray());
            })
            .Where(g => g.ConstituentCount >= 1)
            .ToArray();

        return groups;
    }
}

/// <summary>
/// Nearest active references above/below current price. Descriptive only — no support/resistance.
/// </summary>
public static class NearestReferenceBuilder
{
    public static NearestReferenceView Build(
        IReadOnlyList<StructuralReferenceSnapshot> active,
        decimal? currentPrice,
        PriceGrid grid)
    {
        if (active is null)
            active = Array.Empty<StructuralReferenceSnapshot>();

        if (currentPrice is null || !grid.TryToTickIndex(currentPrice.Value, out var pxTick))
        {
            return new NearestReferenceView(
                currentPrice, null,
                Array.Empty<StructuralReferenceSnapshot>(),
                Array.Empty<StructuralReferenceSnapshot>(),
                Array.Empty<StructuralReferenceSnapshot>(),
                null, null);
        }

        var containing = active
            .Where(r => r.ZoneLowTick <= pxTick && pxTick <= r.ZoneHighTick)
            .OrderBy(r => r.ReferenceId, StringComparer.Ordinal)
            .ToArray();

        var belowCandidates = active
            .Where(r => r.ZoneHighTick < pxTick)
            .GroupBy(r => r.ZoneHighTick)
            .OrderByDescending(g => g.Key)
            .FirstOrDefault();

        var aboveCandidates = active
            .Where(r => r.ZoneLowTick > pxTick)
            .GroupBy(r => r.ZoneLowTick)
            .OrderBy(g => g.Key)
            .FirstOrDefault();

        var below = belowCandidates is null
            ? Array.Empty<StructuralReferenceSnapshot>()
            : belowCandidates.OrderBy(r => r.ReferenceId, StringComparer.Ordinal).ToArray();
        var above = aboveCandidates is null
            ? Array.Empty<StructuralReferenceSnapshot>()
            : aboveCandidates.OrderBy(r => r.ReferenceId, StringComparer.Ordinal).ToArray();

        long? distBelow = below.Length == 0 ? null : pxTick - below[0].ZoneHighTick;
        long? distAbove = above.Length == 0 ? null : above[0].ZoneLowTick - pxTick;

        return new NearestReferenceView(
            grid.ToPrice(pxTick),
            pxTick,
            below,
            above,
            containing,
            distBelow,
            distAbove);
    }
}

/// <summary>
/// In-memory deterministic registry keyed by ReferenceId.
/// Confirmed immutable; Developing revisable; epoch/tick/policy fail-closed.
/// </summary>
public sealed class StructuralReferenceRegistry
{
    private readonly Dictionary<string, StructuralReferenceSnapshot> _active = new(StringComparer.Ordinal);
    private readonly List<StructuralReferenceSnapshot> _retired = new();
    private long _revision;
    private string _dataEpoch = "Unknown";
    private decimal _tickSize;
    private string _timestampPolicyVersion = AtasTimestampNormalizer.PolicyVersion;

    public StructuralReferenceRegistry(decimal tickSize, string dataEpoch, string timestampPolicyVersion)
    {
        if (tickSize <= 0m) throw new ArgumentOutOfRangeException(nameof(tickSize));
        _tickSize = tickSize;
        _dataEpoch = string.IsNullOrWhiteSpace(dataEpoch) ? "Unknown" : dataEpoch;
        _timestampPolicyVersion = string.IsNullOrWhiteSpace(timestampPolicyVersion)
            ? AtasTimestampNormalizer.PolicyVersion
            : timestampPolicyVersion;
    }

    public long Revision => _revision;
    public IReadOnlyCollection<StructuralReferenceSnapshot> Active => _active.Values;
    public IReadOnlyList<StructuralReferenceSnapshot> RetiredOrExpired => _retired;

    public void Configure(decimal tickSize, string dataEpoch, string timestampPolicyVersion)
    {
        if (tickSize <= 0m) throw new ArgumentOutOfRangeException(nameof(tickSize));
        var epoch = string.IsNullOrWhiteSpace(dataEpoch) ? "Unknown" : dataEpoch;
        var policy = string.IsNullOrWhiteSpace(timestampPolicyVersion)
            ? AtasTimestampNormalizer.PolicyVersion
            : timestampPolicyVersion;

        if (_tickSize != tickSize
            || !string.Equals(_dataEpoch, epoch, StringComparison.Ordinal)
            || !string.Equals(_timestampPolicyVersion, policy, StringComparison.Ordinal))
        {
            RetireAll(ReferenceStatus.Retired, "CONFIG_OR_EPOCH_CHANGE");
            _tickSize = tickSize;
            _dataEpoch = epoch;
            _timestampPolicyVersion = policy;
        }
    }

    public void Reset()
    {
        RetireAll(ReferenceStatus.Retired, "RESET");
    }

    /// <summary>
    /// Reconcile extracted candidates. Returns active confirmed/developing after reconciliation.
    /// </summary>
    public (IReadOnlyList<StructuralReferenceSnapshot> Confirmed, IReadOnlyList<StructuralReferenceSnapshot> Developing) Reconcile(
        string instrumentIdentity,
        IReadOnlyList<ExtractedReferenceCandidate> candidates,
        DateTime nowUtc)
    {
        if (candidates is null) throw new ArgumentNullException(nameof(candidates));
        var grid = new PriceGrid(_tickSize);
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var incomingSourceKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var c in candidates)
        {
            if (!grid.TryToTickIndex(c.Price, out var tick))
                throw new InvalidOperationException("TICK_ALIGN_FAIL:" + c.Type + ":" + c.Price);

            var id = ReferenceIdentity.Build(
                instrumentIdentity,
                _dataEpoch,
                c.SourceId,
                c.Type,
                c.Maturity,
                ReferencePolicyConfig.PolicyVersion);

            seenIds.Add(id);
            incomingSourceKeys.Add(SourceKey(c.SourceKind, c.SourceId));

            var price = grid.ToPrice(tick);
            var features = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["TestCount"] = "0",
                ["ReactionHistory"] = "NotEvaluated",
                ["ExecutedActivity"] = "NotEvaluated",
                ["ProfileConfluence"] = "ExactTickOnly",
                ["Age"] = "NotEvaluated"
            };

            var draft = new StructuralReferenceSnapshot(
                id,
                c.Type,
                price,
                price,
                tick,
                tick,
                c.SourceId,
                c.SourceKind,
                c.SourceHorizon,
                c.SourceCreatedAtUtc,
                c.Maturity,
                ReferenceStatus.Fresh,
                ReferenceEvidenceTier.ProfileDerived,
                _tickSize,
                _dataEpoch,
                _timestampPolicyVersion,
                // Registry owns monotonic revision — drafts must not reset to 1.
                stateVersion: StructuralReferenceSnapshot.RegistryAssignedStateVersion,
                createdAtUtc: nowUtc,
                lastUpdatedAtUtc: nowUtc,
                features,
                EvidenceProvenance.ObservedApi);

            Upsert(draft, nowUtc);
        }

        // Expire active refs whose source is no longer present in this reconciliation.
        var toExpire = _active.Values
            .Where(r => !seenIds.Contains(r.ReferenceId)
                        && !incomingSourceKeys.Contains(SourceKey(r.SourceKind, r.SourceId)))
            .Select(r => r.ReferenceId)
            .ToArray();

        // Also expire refs whose source remains but type omitted (e.g. volume became unavailable).
        var omitted = _active.Values
            .Where(r => !seenIds.Contains(r.ReferenceId)
                        && incomingSourceKeys.Contains(SourceKey(r.SourceKind, r.SourceId)))
            .Select(r => r.ReferenceId)
            .ToArray();

        foreach (var id in toExpire.Concat(omitted).Distinct(StringComparer.Ordinal))
            Expire(id, nowUtc);

        var confirmed = _active.Values
            .Where(r => r.Maturity == ReferenceMaturity.Confirmed
                        && r.Status is ReferenceStatus.Fresh or ReferenceStatus.Active)
            .OrderBy(r => r.ReferenceId, StringComparer.Ordinal)
            .ToArray();
        var developing = _active.Values
            .Where(r => r.Maturity == ReferenceMaturity.Developing
                        && r.Status is ReferenceStatus.Fresh or ReferenceStatus.Active)
            .OrderBy(r => r.ReferenceId, StringComparer.Ordinal)
            .ToArray();
        return (confirmed, developing);
    }

    public void Upsert(StructuralReferenceSnapshot incoming, DateTime nowUtc)
    {
        if (incoming is null) throw new ArgumentNullException(nameof(incoming));
        FailClosedCompatibility(incoming);

        if (!_active.TryGetValue(incoming.ReferenceId, out var existing))
        {
            var initial = incoming.StateVersion > 0 ? incoming.StateVersion : 1;
            if (incoming.StateVersion == initial
                && incoming.Status is ReferenceStatus.Fresh or ReferenceStatus.Active)
            {
                _active[incoming.ReferenceId] = incoming;
            }
            else
            {
                _active[incoming.ReferenceId] = incoming.WithLifecycle(
                    ReferenceStatus.Fresh,
                    initial,
                    nowUtc,
                    incoming.ZoneLow,
                    incoming.ZoneHigh,
                    incoming.ZoneLowTick,
                    incoming.ZoneHighTick);
            }

            _revision++;
            return;
        }

        // Authoritative external revision only. RegistryAssignedStateVersion (0) means
        // "desired payload" — registry remains the monotonic owner.
        if (incoming.StateVersion > 0 && incoming.StateVersion < existing.StateVersion)
            throw new InvalidOperationException("STALE_REVISION:" + incoming.ReferenceId);

        if (existing.Maturity == ReferenceMaturity.Confirmed)
        {
            if (existing.SameImmutablePayload(incoming)
                || (existing.SameNormalizedZone(incoming)
                    && existing.ReferenceType == incoming.ReferenceType
                    && string.Equals(existing.SourceId, incoming.SourceId, StringComparison.Ordinal)))
            {
                // Idempotent republish — keep existing; may refresh Active status only.
                if (existing.Status == ReferenceStatus.Fresh)
                {
                    _active[incoming.ReferenceId] = existing.WithLifecycle(ReferenceStatus.Active, existing.StateVersion, nowUtc);
                    _revision++;
                }
                return;
            }

            throw new InvalidOperationException("CONFIRMED_IMMUTABLE:" + incoming.ReferenceId);
        }

        // Developing: zone change increments StateVersion; identical zone is idempotent.
        if (existing.SameNormalizedZone(incoming))
        {
            if (existing.Status == ReferenceStatus.Fresh)
            {
                _active[incoming.ReferenceId] = existing.WithLifecycle(ReferenceStatus.Active, existing.StateVersion, nowUtc);
                _revision++;
            }
            return;
        }

        // Registry-owned monotonic bump. Authoritative external ahead-of-ledger revisions allowed.
        var nextVersion = existing.StateVersion + 1;
        if (incoming.StateVersion > nextVersion)
            nextVersion = incoming.StateVersion;

        _active[incoming.ReferenceId] = existing.WithLifecycle(
            ReferenceStatus.Active,
            nextVersion,
            nowUtc,
            incoming.ZoneLow,
            incoming.ZoneHigh,
            incoming.ZoneLowTick,
            incoming.ZoneHighTick);
        _revision++;
    }

    private void Expire(string referenceId, DateTime nowUtc)
    {
        if (!_active.Remove(referenceId, out var existing))
            return;
        var expired = existing.WithLifecycle(ReferenceStatus.Expired, existing.StateVersion, nowUtc);
        _retired.Add(expired);
        _revision++;
    }

    private void RetireAll(ReferenceStatus status, string reason)
    {
        foreach (var r in _active.Values.OrderBy(x => x.ReferenceId, StringComparer.Ordinal))
        {
            _retired.Add(r.WithLifecycle(status, r.StateVersion, DateTime.UtcNow));
        }

        _active.Clear();
        _revision++;
        _ = reason;
    }

    private void FailClosedCompatibility(StructuralReferenceSnapshot incoming)
    {
        if (incoming.TickSize != _tickSize)
            throw new InvalidOperationException("TICK_SIZE_MISMATCH:" + incoming.ReferenceId);
        if (!string.Equals(incoming.DataEpoch, _dataEpoch, StringComparison.Ordinal))
            throw new InvalidOperationException("EPOCH_MISMATCH:" + incoming.ReferenceId);
        if (!string.Equals(incoming.TimestampPolicyVersion, _timestampPolicyVersion, StringComparison.Ordinal))
            throw new InvalidOperationException("TIMESTAMP_POLICY_MISMATCH:" + incoming.ReferenceId);
        if (incoming.Status is not (ReferenceStatus.Fresh or ReferenceStatus.Active
            or ReferenceStatus.Expired or ReferenceStatus.Retired))
            throw new InvalidOperationException("FORBIDDEN_LIFECYCLE_STATUS:" + incoming.Status);
    }

    private static string SourceKey(ReferenceSourceKind kind, string sourceId) =>
        kind + "|" + sourceId;
}
