using GC.AuctionFlow.Profile;

namespace GC.AuctionFlow.Composite;

/// <summary>
/// Operator-anchored Composite host. Consumes immutable Phase 1A auction snapshots only.
/// No hard N-day merge. Shadow evidence never mutates confirmed composite.
/// </summary>
public sealed class CompositeProfileHost
{
    private readonly CompletedAuctionLedger _ledger = new();
    private readonly List<string> _transitions = new();
    private CompositePolicyConfig _policy;
    private decimal _tickSize;
    private decimal _valueAreaFraction;
    private string _contractIdentity = "Unknown";
    private string _contractEpoch = "Unknown";
    private CompositeSetSnapshot? _published;
    private string? _lastCompositeId;

    public CompositeProfileHost(
        decimal tickSize,
        decimal valueAreaFraction = PrimaryAuctionClockConfig.DefaultValueAreaFraction,
        CompositePolicyConfig? policy = null)
    {
        if (tickSize <= 0m) throw new ArgumentOutOfRangeException(nameof(tickSize));
        _tickSize = tickSize;
        _valueAreaFraction = valueAreaFraction;
        _policy = policy ?? new CompositePolicyConfig(CompositePolicyMode.Disabled);
    }

    public CompositeSetSnapshot? Current => _published;
    public CompositePolicyConfig Policy => _policy;

    public void Configure(
        decimal tickSize,
        decimal valueAreaFraction,
        string contractIdentity,
        string contractEpoch,
        CompositePolicyConfig policy)
    {
        if (tickSize <= 0m) throw new ArgumentOutOfRangeException(nameof(tickSize));
        if (policy is null) throw new ArgumentNullException(nameof(policy));

        var epochChanged = !string.Equals(_contractEpoch, contractEpoch, StringComparison.Ordinal)
                           || _tickSize != tickSize
                           || !string.Equals(_policy.Version, policy.Version, StringComparison.Ordinal);

        _tickSize = tickSize;
        _valueAreaFraction = valueAreaFraction;
        _contractIdentity = string.IsNullOrWhiteSpace(contractIdentity) ? "Unknown" : contractIdentity;
        _contractEpoch = string.IsNullOrWhiteSpace(contractEpoch) ? "Unknown" : contractEpoch;
        _policy = policy;

        if (epochChanged)
        {
            _ledger.Clear("CONFIG_OR_EPOCH_CHANGE");
            Note("POLICY_OR_EPOCH_REBUILD");
        }
    }

    public void Reset()
    {
        _ledger.Clear("RESET");
        _published = null;
        _lastCompositeId = null;
        Note("RESET");
    }

    /// <summary>
    /// Ingest completed primary auctions and optional developing current. Rebuilds confirmed (+ preview).
    /// </summary>
    public CompositeSetSnapshot Rebuild(
        IReadOnlyList<PrimaryAuctionProfileSnapshot> completedAuctions,
        PrimaryAuctionProfileSnapshot? developingCurrent)
    {
        if (_policy.Mode == CompositePolicyMode.Disabled)
        {
            _published = DisabledSnapshot();
            return _published;
        }

        // Ingest completed only into ledger.
        foreach (var a in completedAuctions.OrderBy(x => x.AuctionStartUtc))
        {
            if (!a.IsCompleted) continue;
            try
            {
                var c = CompositeAuctionContribution.FromPrimaryAuction(a, _contractIdentity, _contractEpoch, _tickSize);
                if (_ledger.Upsert(c))
                    Note("CONTRIBUTION_UPSERT:" + c.AuctionId);
            }
            catch (InvalidOperationException ex)
            {
                Note("INVALID:" + ex.Message);
                _published = InvalidSnapshot(ex.Message);
                return _published;
            }
        }

        if (_policy.Mode == CompositePolicyMode.OperatorAnchored
            && string.IsNullOrWhiteSpace(_policy.AnchorAuctionId))
        {
            _published = AwaitingAnchorSnapshot();
            return _published;
        }

        var all = _ledger.AllOrdered();
        var included = SelectIncluded(all);
        if (included.Count == 0)
        {
            _published = new CompositeSetSnapshot(
                new ConfirmedCompositeProfileSnapshot(
                    compositeId: "CMP|EMPTY",
                    policyMode: _policy.Mode,
                    policyVersion: _policy.Version,
                    contractIdentity: _contractIdentity,
                    contractEpoch: _contractEpoch,
                    tickSize: _tickSize,
                    anchorAuctionId: _policy.AnchorAuctionId,
                    firstAuctionId: null,
                    lastAuctionId: null,
                    includedAuctionIds: Array.Empty<string>(),
                    excludedAuctionIds: _policy.ExcludedAuctionIds,
                    completedContributionCount: 0,
                    compositeStartUtc: null,
                    compositeEndUtc: null,
                    compositeStatus: string.IsNullOrWhiteSpace(_policy.AnchorAuctionId)
                        ? CompositeStatus.AwaitingAnchor
                        : CompositeStatus.Building,
                    aggregate: null,
                    capability: string.IsNullOrWhiteSpace(_policy.AnchorAuctionId)
                        ? CompositeCapabilityState.AwaitingAnchor
                        : CompositeCapabilityState.NotReady,
                    evidenceState: CompositeEvidenceState.NotEvaluated,
                    knownLimitations: new[] { "NO_INCLUDED_CONTRIBUTIONS", "ANCHOR=" + (_policy.AnchorAuctionId ?? "") },
                    provenance: "CompositeProfileHost"),
                null,
                Array.Empty<CompositeMergeEvidence>(),
                SnapshotTransitions());
            return _published;
        }

        // Anchor must be present in included set.
        if (_policy.Mode == CompositePolicyMode.OperatorAnchored
            && !included.Any(c => string.Equals(c.AuctionId, _policy.AnchorAuctionId, StringComparison.Ordinal)))
        {
            _published = new CompositeSetSnapshot(
                new ConfirmedCompositeProfileSnapshot(
                    "CMP|ANCHOR_NOT_LOADED",
                    _policy.Mode,
                    _policy.Version,
                    _contractIdentity,
                    _contractEpoch,
                    _tickSize,
                    _policy.AnchorAuctionId,
                    null, null,
                    Array.Empty<string>(),
                    _policy.ExcludedAuctionIds,
                    0, null, null,
                    CompositeStatus.AwaitingAnchor,
                    null,
                    CompositeCapabilityState.AwaitingAnchor,
                    CompositeEvidenceState.NotEvaluated,
                    new[] { "ANCHOR_NOT_LOADED:" + _policy.AnchorAuctionId },
                    "CompositeProfileHost"),
                null,
                Array.Empty<CompositeMergeEvidence>(),
                SnapshotTransitions());
            return _published;
        }

        var aggregate = CompositeAggregator.Aggregate(included, _tickSize, _valueAreaFraction);
        var includedIds = included.Select(c => c.AuctionId).ToArray();
        var gapNotes = ReportHistoryGaps(included);
        var compositeId = CompositeIdBuilder.Build(
            _contractIdentity,
            _contractEpoch,
            _policy.AnchorAuctionId ?? includedIds[0],
            includedIds,
            _policy.Version);

        if (!string.Equals(_lastCompositeId, compositeId, StringComparison.Ordinal))
        {
            Note("COMPOSITE_ID:" + compositeId);
            _lastCompositeId = compositeId;
        }

        var status = ResolveStatus(aggregate);
        var capability = MapCapability(status);
        var grid = new PriceGrid(_tickSize);

        var evidence = new List<CompositeMergeEvidence>();
        for (var i = 1; i < included.Count; i++)
            evidence.Add(CompositeMergeEvidenceCalculator.ForAdjacent(included[i - 1], included[i], grid));

        var evidenceState = CompositeMergeEvidenceCalculator.EvaluateShadow(_policy, evidence);
        if (_policy.EnableShadowEvidence
            && _policy.ShadowMinValueOverlapRatio is null
            && _policy.ShadowMaxPocDisplacementTicks is null)
            evidenceState = CompositeEvidenceState.NotCalibrated;

        var confirmed = new ConfirmedCompositeProfileSnapshot(
            compositeId,
            _policy.Mode,
            _policy.Version,
            _contractIdentity,
            _contractEpoch,
            _tickSize,
            _policy.AnchorAuctionId,
            includedIds[0],
            includedIds[^1],
            includedIds,
            _policy.ExcludedAuctionIds,
            included.Count,
            included[0].AuctionStartUtc,
            included[^1].AuctionEndUtc,
            status,
            aggregate,
            capability,
            evidenceState,
            aggregate.KnownLimitations.Concat(gapNotes).Concat(new[]
            {
                "NO_HARD_N_DAY_PRODUCTION_MERGE",
                "SHADOW_DOES_NOT_MUTATE_CONFIRMED",
                "TIMESTAMP_POLICY=" + AtasTimestampNormalizer.PolicyVersion
            }).Distinct(StringComparer.Ordinal).ToArray(),
            "CompositeProfileHost/OperatorAnchored");

        DevelopingCompositePreviewSnapshot? preview = null;
        if (_policy.EnableDevelopingCompositePreview
            && developingCurrent is not null
            && !developingCurrent.IsCompleted
            && status is CompositeStatus.Ready or CompositeStatus.Partial)
        {
            var dev = CompositeAuctionContribution.FromPrimaryAuction(
                developingCurrent, _contractIdentity, _contractEpoch, _tickSize);
            var withDev = included.Concat(new[] { dev }).ToArray();
            var previewAgg = CompositeAggregator.Aggregate(withDev, _tickSize, _valueAreaFraction);
            var tpoShare = previewAgg.TotalTpoCount > 0
                ? (decimal)dev.TotalTpoCount / previewAgg.TotalTpoCount
                : 0m;
            var volShare = previewAgg.TotalExecutedVolume > 0m
                ? dev.TotalExecutedVolume / previewAgg.TotalExecutedVolume
                : 0m;
            preview = new DevelopingCompositePreviewSnapshot(
                compositeId,
                developingCurrent.AuctionId,
                previewAgg,
                tpoShare,
                volShare,
                new[] { "DEVELOPING_PREVIEW_ONLY", "DOES_NOT_MUTATE_CONFIRMED" });
            Note("PREVIEW_ON:" + developingCurrent.AuctionId);
        }

        _published = new CompositeSetSnapshot(confirmed, preview, evidence, SnapshotTransitions());
        return _published;
    }

    private List<CompositeAuctionContribution> SelectIncluded(IReadOnlyList<CompositeAuctionContribution> all)
    {
        var excluded = new HashSet<string>(_policy.ExcludedAuctionIds, StringComparer.Ordinal);
        if (_policy.Mode != CompositePolicyMode.OperatorAnchored || _policy.AnchorAuctionId is null)
            return all.Where(c => !excluded.Contains(c.AuctionId)).ToList();

        var anchor = all.FirstOrDefault(c => string.Equals(c.AuctionId, _policy.AnchorAuctionId, StringComparison.Ordinal));
        if (anchor is null)
            return new List<CompositeAuctionContribution>();

        var list = all
            .Where(c => c.AuctionStartUtc >= anchor.AuctionStartUtc)
            .Where(c => !excluded.Contains(c.AuctionId))
            .ToList();

        if (!_policy.IncludeThroughLatestCompletedAuction)
        {
            // Anchor-only when not including through latest.
            list = list.Where(c => string.Equals(c.AuctionId, _policy.AnchorAuctionId, StringComparison.Ordinal)).ToList();
        }

        return list;
    }

    /// <summary>Report calendar gaps between consecutive included local auction dates (loaded-history honesty).</summary>
    public static IReadOnlyList<string> ReportHistoryGaps(IReadOnlyList<CompositeAuctionContribution> included)
    {
        if (included.Count < 2)
            return Array.Empty<string>();
        var notes = new List<string>();
        for (var i = 1; i < included.Count; i++)
        {
            var prev = included[i - 1].LocalAuctionDate;
            var next = included[i].LocalAuctionDate;
            var gapDays = next.DayNumber - prev.DayNumber;
            if (gapDays > 1)
                notes.Add("MISSING_HISTORY_GAP:" + prev.ToString("yyyy-MM-dd") + "->" + next.ToString("yyyy-MM-dd") + ":days=" + gapDays);
        }

        return notes;
    }

    private static CompositeStatus ResolveStatus(CompositeAggregateResult aggregate)
    {
        if (aggregate.TpoCounts.Count == 0 || aggregate.TpoPoc is null)
            return CompositeStatus.Building;
        if (aggregate.DataQuality == ProfileDataQuality.Partial
            || aggregate.PriceVolumeCapability != PriceVolumeCapability.Exact
            || aggregate.VolumePoc is null)
            return CompositeStatus.Partial;
        return CompositeStatus.Ready;
    }

    private static CompositeCapabilityState MapCapability(CompositeStatus status) => status switch
    {
        CompositeStatus.Disabled => CompositeCapabilityState.Disabled,
        CompositeStatus.AwaitingAnchor => CompositeCapabilityState.AwaitingAnchor,
        CompositeStatus.Building => CompositeCapabilityState.NotReady,
        CompositeStatus.Partial => CompositeCapabilityState.Partial,
        CompositeStatus.Ready => CompositeCapabilityState.Ready,
        CompositeStatus.Invalid => CompositeCapabilityState.Invalid,
        _ => CompositeCapabilityState.NotReady
    };

    private CompositeSetSnapshot DisabledSnapshot() =>
        new(
            new ConfirmedCompositeProfileSnapshot(
                "CMP|DISABLED", CompositePolicyMode.Disabled, _policy.Version,
                _contractIdentity, _contractEpoch, _tickSize, null, null, null,
                Array.Empty<string>(), Array.Empty<string>(), 0, null, null,
                CompositeStatus.Disabled, null, CompositeCapabilityState.Disabled,
                CompositeEvidenceState.NotEvaluated,
                new[] { "COMPOSITE_DISABLED" }, "CompositeProfileHost"),
            null, Array.Empty<CompositeMergeEvidence>(), SnapshotTransitions());

    private CompositeSetSnapshot AwaitingAnchorSnapshot() =>
        new(
            new ConfirmedCompositeProfileSnapshot(
                "CMP|AWAITING_ANCHOR", CompositePolicyMode.OperatorAnchored, _policy.Version,
                _contractIdentity, _contractEpoch, _tickSize, null, null, null,
                Array.Empty<string>(), _policy.ExcludedAuctionIds, 0, null, null,
                CompositeStatus.AwaitingAnchor, null, CompositeCapabilityState.AwaitingAnchor,
                CompositeEvidenceState.NotEvaluated,
                new[] { "COMPOSITE_AWAITING_ANCHOR" }, "CompositeProfileHost"),
            null, Array.Empty<CompositeMergeEvidence>(), SnapshotTransitions());

    private CompositeSetSnapshot InvalidSnapshot(string reason) =>
        new(
            new ConfirmedCompositeProfileSnapshot(
                "CMP|INVALID", _policy.Mode, _policy.Version,
                _contractIdentity, _contractEpoch, _tickSize, _policy.AnchorAuctionId, null, null,
                Array.Empty<string>(), _policy.ExcludedAuctionIds, 0, null, null,
                CompositeStatus.Invalid, null, CompositeCapabilityState.Invalid,
                CompositeEvidenceState.Invalid,
                new[] { reason }, "CompositeProfileHost"),
            null, Array.Empty<CompositeMergeEvidence>(), SnapshotTransitions());

    private void Note(string ev)
    {
        _transitions.Add(ev);
        while (_transitions.Count > 64)
            _transitions.RemoveAt(0);
    }

    private IReadOnlyList<string> SnapshotTransitions() => _transitions.ToArray();
}
