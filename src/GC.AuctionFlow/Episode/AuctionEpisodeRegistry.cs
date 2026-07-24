using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Reference;

namespace GC.AuctionFlow.Episode;

/// <summary>
/// Active episode registry keyed by (PrimaryAuctionId, ReferenceId).
/// Owns StateVersion/EventRevision. Geometric observation only.
/// </summary>
public sealed class AuctionEpisodeRegistry
{
    private readonly Dictionary<string, MutableEpisode> _active = new(StringComparer.Ordinal);
    private readonly Queue<AuctionEpisodeSnapshot> _recentlyClosed = new();
    private readonly EpisodeEventLedger _ledger = new();
    private string _primaryAuctionId = "";
    private string _instrumentIdentity = "Unknown";
    private string _dataEpoch = "Unknown";
    private decimal _tickSize;
    private string _timestampPolicy;
    private long _registryRevision;
    private AuctionEpisodeSnapshot? _latestUpdated;
    private bool _anyTradeObserved;
    private bool _anyAggressorUnavailable;

    public AuctionEpisodeRegistry(decimal tickSize, string timestampPolicyVersion)
    {
        if (tickSize <= 0m) throw new ArgumentOutOfRangeException(nameof(tickSize));
        _tickSize = tickSize;
        _timestampPolicy = timestampPolicyVersion ?? AtasTimestampNormalizer.PolicyVersion;
    }

    public EpisodeEventLedger Ledger => _ledger;
    public long RegistryRevision => _registryRevision;
    public bool AnyTradeObserved => _anyTradeObserved;
    public bool AnyAggressorUnavailable => _anyAggressorUnavailable;
    public string PrimaryAuctionId => _primaryAuctionId;

    public void Configure(decimal tickSize, string instrumentIdentity, string dataEpoch, string timestampPolicyVersion)
    {
        if (tickSize <= 0m) throw new ArgumentOutOfRangeException(nameof(tickSize));
        _tickSize = tickSize;
        _instrumentIdentity = string.IsNullOrWhiteSpace(instrumentIdentity) ? "Unknown" : instrumentIdentity;
        _dataEpoch = string.IsNullOrWhiteSpace(dataEpoch) ? "Unknown" : dataEpoch;
        _timestampPolicy = string.IsNullOrWhiteSpace(timestampPolicyVersion)
            ? AtasTimestampNormalizer.PolicyVersion
            : timestampPolicyVersion;
    }

    public void Reset()
    {
        _active.Clear();
        _recentlyClosed.Clear();
        _ledger.Clear();
        _primaryAuctionId = "";
        _registryRevision = 0;
        _latestUpdated = null;
        _anyTradeObserved = false;
        _anyAggressorUnavailable = false;
    }

    public void OnPrimaryAuctionChanged(string newAuctionId, DateTime nowUtc)
    {
        if (string.Equals(_primaryAuctionId, newAuctionId, StringComparison.Ordinal))
            return;

        ExpireAllActive(nowUtc, EpisodePolicyConfig.LimitationPrimaryAuctionChanged);
        _active.Clear();
        _ledger.Clear(newAuctionId);
        _primaryAuctionId = newAuctionId ?? "";
        _anyTradeObserved = false;
        _anyAggressorUnavailable = false;
        _registryRevision++;
    }

    public void SyncEligibleReferences(
        IReadOnlyList<StructuralReferenceSnapshot> eligible,
        DateTime nowUtc)
    {
        var alive = new HashSet<string>(StringComparer.Ordinal);
        foreach (var r in eligible)
            alive.Add(r.ReferenceId);

        var retired = _active.Keys.Where(id => !alive.Contains(ExtractRefId(id))).ToArray();
        foreach (var key in retired)
        {
            if (_active.TryGetValue(key, out var ep))
            {
                CloseEpisode(ep, EpisodeState.EpisodeExpired, EpisodeResolution.Expired, nowUtc,
                    "REFERENCE_RETIRED_OR_INELIGIBLE");
                _active.Remove(key);
            }
        }
    }

    /// <summary>
    /// Admit one normalized trade. Geometry updates only on eligible touch/cross.
    /// Accepted trades set <see cref="AnyTradeObserved"/> even when ActiveEpisodes stays empty.
    /// </summary>
    public EpisodeTradeAdmissionResult ProcessTrade(
        EpisodeTradeEvent evt,
        IReadOnlyList<StructuralReferenceSnapshot> eligible,
        string? directionalProvenance,
        DateTime nowUtc)
    {
        if (evt is null)
            return EpisodeTradeAdmissionResult.InvalidEvent;

        if (!string.Equals(evt.TimestampPolicyVersion, _timestampPolicy, StringComparison.Ordinal)
            || evt.TickSize != _tickSize
            || !string.Equals(evt.DataEpoch, _dataEpoch, StringComparison.Ordinal))
        {
            InvalidateAll(nowUtc, "EPISODE_COMPATIBILITY_MISMATCH");
            return EpisodeTradeAdmissionResult.CompatibilityMismatch;
        }

        _ledger.EnsureAuction(_primaryAuctionId);
        var commit = _ledger.TryCommit(evt);
        if (commit != EpisodeTradeAdmissionResult.Accepted)
            return commit;

        _anyTradeObserved = true;
        if (!evt.AggressorClassified)
            _anyAggressorUnavailable = true;

        var changed = false;
        foreach (var reference in eligible)
        {
            if (ProcessTradeAgainstReference(evt, reference, directionalProvenance, nowUtc))
                changed = true;
        }

        if (changed)
            _registryRevision++;
        return EpisodeTradeAdmissionResult.Accepted;
    }

    private bool ProcessTradeAgainstReference(
        EpisodeTradeEvent evt,
        StructuralReferenceSnapshot reference,
        string? directionalProvenance,
        DateTime nowUtc)
    {
        var role = ReferenceInteractionRoleMapper.Map(reference.ReferenceType);
        if (role == ReferenceInteractionRole.Unsupported)
            return false;

        var refTick = reference.ZoneLowTick; // Phase 1C single-price: ZoneLow == ZoneHigh
        var side = ClassifySide(evt.PriceTick, reference.ZoneLowTick, reference.ZoneHighTick);
        var key = RegistryKey(_primaryAuctionId, reference.ReferenceId);

        if (!_active.TryGetValue(key, out var ep))
        {
            if (side == ReferenceSidePosition.AtReference)
            {
                ep = CreateNew(reference, role, evt, directionalProvenance, nowUtc, EpisodeState.Interacting, attemptCount: 0);
                _active[key] = ep;
                PublishLatest(ep);
                return true;
            }

            if (role is ReferenceInteractionRole.UpperBoundary or ReferenceInteractionRole.LowerBoundary
                && side == ReferenceInteractionRoleMapper.CanonicalOutsideSide(role))
            {
                ep = CreateNew(reference, role, evt, directionalProvenance, nowUtc, EpisodeState.OutsideAttempt, attemptCount: 1);
                ApplyOutsideMetrics(ep, evt, side, nowUtc, isNewOutsideSegment: true);
                _active[key] = ep;
                PublishLatest(ep);
                return true;
            }

            // First print not at/through reference — no episode yet (no approach threshold).
            return false;
        }

        // Existing episode
        var prevSide = ep.LastSide;
        var prevState = ep.State;
        ep.InteractionCount++;
        ep.LastProcessedEventId = evt.EventIdentity;
        ep.LastUpdatedAtUtc = nowUtc;
        if (directionalProvenance is not null)
            ep.DirectionalContextProvenance = directionalProvenance;

        if (!evt.AggressorClassified)
            ep.AggressorUnavailable = true;

        AccumulateLocalVolume(ep, evt);

        var semanticChanged = false;
        var metricChanged = false;

        if (role == ReferenceInteractionRole.Centerline)
        {
            metricChanged |= UpdateCenterline(ep, evt, side, prevSide);
        }
        else
        {
            metricChanged |= UpdateBoundary(ep, evt, role, side, prevSide, ref semanticChanged);
        }

        ep.LastSide = side;
        ep.UpdateDirection();

        if (semanticChanged || ep.State != prevState)
        {
            ep.StateVersion++;
            ep.EventRevision++;
        }
        else if (metricChanged || ep.InteractionCount > 0)
        {
            ep.EventRevision++;
        }

        PublishLatest(ep);
        return true;
    }

    private bool UpdateCenterline(
        MutableEpisode ep,
        EpisodeTradeEvent evt,
        ReferenceSidePosition side,
        ReferenceSidePosition prevSide)
    {
        var changed = false;
        if (side != ReferenceSidePosition.AtReference && prevSide != side)
        {
            if (prevSide != ReferenceSidePosition.AtReference && prevSide != side)
            {
                ep.CrossCount++;
                changed = true;
            }
            else if (prevSide == ReferenceSidePosition.AtReference)
            {
                // leaving reference into a side — first excursion from touch
                if (side == ReferenceSidePosition.Above) ep.UpExcursionCount++;
                if (side == ReferenceSidePosition.Below) ep.DownExcursionCount++;
                changed = true;
            }
        }

        if (side == ReferenceSidePosition.Above)
        {
            var dist = evt.PriceTick - ep.ReferencePriceTick;
            if (dist > ep.MaximumAboveDistanceTicks)
            {
                ep.MaximumAboveDistanceTicks = dist;
                changed = true;
            }
        }
        else if (side == ReferenceSidePosition.Below)
        {
            var dist = ep.ReferencePriceTick - evt.PriceTick;
            if (dist > ep.MaximumBelowDistanceTicks)
            {
                ep.MaximumBelowDistanceTicks = dist;
                changed = true;
            }
        }

        if (ep.State == EpisodeState.Interacting && side != ReferenceSidePosition.AtReference)
        {
            ep.State = EpisodeState.Developing;
            changed = true;
        }

        return changed;
    }

    private bool UpdateBoundary(
        MutableEpisode ep,
        EpisodeTradeEvent evt,
        ReferenceInteractionRole role,
        ReferenceSidePosition side,
        ReferenceSidePosition prevSide,
        ref bool semanticChanged)
    {
        var outside = ReferenceInteractionRoleMapper.CanonicalOutsideSide(role)!.Value;
        var inside = ReferenceInteractionRoleMapper.CanonicalInsideSide(role)!.Value;
        var changed = false;

        var wasOutside = prevSide == outside;
        var isOutside = side == outside;
        var wasInsideOrAt = prevSide == inside || prevSide == ReferenceSidePosition.AtReference;

        if (!wasOutside && isOutside)
        {
            // Entering canonical outside from inside/at/reentry → one new attempt.
            if (wasInsideOrAt
                || ep.State is EpisodeState.ReentryDeveloping
                    or EpisodeState.Interacting
                    or EpisodeState.Developing)
            {
                ep.AttemptCount++;
                semanticChanged = true;
            }

            ep.State = EpisodeState.OutsideAttempt;
            semanticChanged = true;
            ApplyOutsideMetrics(ep, evt, side, evt.ReceiveUtc, isNewOutsideSegment: true);
            changed = true;
        }
        else if (wasOutside && isOutside)
        {
            if (ep.State == EpisodeState.OutsideAttempt)
            {
                ep.State = EpisodeState.Developing;
                semanticChanged = true;
            }

            ApplyOutsideMetrics(ep, evt, side, evt.ReceiveUtc, isNewOutsideSegment: false);
            changed = true;
        }
        else if (wasOutside && (side == inside || side == ReferenceSidePosition.AtReference))
        {
            ep.CloseOutsideSegment(evt.ReceiveUtc);
            ep.State = EpisodeState.ReentryDeveloping;
            semanticChanged = true;
            changed = true;
        }

        return changed || semanticChanged;
    }

    private MutableEpisode CreateNew(
        StructuralReferenceSnapshot reference,
        ReferenceInteractionRole role,
        EpisodeTradeEvent evt,
        string? directionalProvenance,
        DateTime nowUtc,
        EpisodeState state,
        int attemptCount)
    {
        var episodeId = EpisodeIdentity.Build(
            _instrumentIdentity,
            _dataEpoch,
            _primaryAuctionId,
            reference.ReferenceId,
            evt.EventIdentity);

        var ep = new MutableEpisode(
            episodeId,
            _primaryAuctionId,
            reference,
            role,
            evt,
            directionalProvenance,
            nowUtc,
            state,
            attemptCount);

        AccumulateLocalVolume(ep, evt);
        return ep;
    }

    private static void ApplyOutsideMetrics(
        MutableEpisode ep,
        EpisodeTradeEvent evt,
        ReferenceSidePosition side,
        DateTime nowUtc,
        bool isNewOutsideSegment)
    {
        if (isNewOutsideSegment)
            ep.OpenOutsideSegment(nowUtc);

        var dist = side == ReferenceSidePosition.Above
            ? evt.PriceTick - ep.ReferencePriceTick
            : ep.ReferencePriceTick - evt.PriceTick;
        if (dist < 0) dist = 0;

        if (side == ReferenceSidePosition.Above && dist > ep.MaximumAboveDistanceTicks)
            ep.MaximumAboveDistanceTicks = dist;
        if (side == ReferenceSidePosition.Below && dist > ep.MaximumBelowDistanceTicks)
            ep.MaximumBelowDistanceTicks = dist;

        if (dist > (ep.MaximumCanonicalOutsideDistanceTicks ?? 0))
            ep.MaximumCanonicalOutsideDistanceTicks = dist;

        ep.CanonicalOutsideExecutedVolume += evt.Volume;
        ep.CanonicalOutsideTradeCount++;
        if (evt.AggressorClassified)
        {
            if (evt.IsBid) ep.CanonicalOutsideBidVolume = (ep.CanonicalOutsideBidVolume ?? 0m) + evt.Volume;
            if (evt.IsAsk) ep.CanonicalOutsideAskVolume = (ep.CanonicalOutsideAskVolume ?? 0m) + evt.Volume;
        }
    }

    private static void AccumulateLocalVolume(MutableEpisode ep, EpisodeTradeEvent evt)
    {
        if (!ep.LocalVolumes.TryGetValue(evt.PriceTick, out var v))
            v = 0m;
        ep.LocalVolumes[evt.PriceTick] = v + evt.Volume;
    }

    private void ExpireAllActive(DateTime nowUtc, string reason)
    {
        foreach (var ep in _active.Values.ToArray())
            CloseEpisode(ep, EpisodeState.EpisodeExpired, EpisodeResolution.Expired, nowUtc, reason);
        _active.Clear();
    }

    private void InvalidateAll(DateTime nowUtc, string reason)
    {
        foreach (var ep in _active.Values.ToArray())
            CloseEpisode(ep, EpisodeState.InvalidData, EpisodeResolution.InvalidData, nowUtc, reason);
        _active.Clear();
        _registryRevision++;
    }

    private void CloseEpisode(
        MutableEpisode ep,
        EpisodeState state,
        EpisodeResolution resolution,
        DateTime nowUtc,
        string limitation)
    {
        ep.CloseOutsideSegment(nowUtc);
        ep.State = state;
        ep.Resolution = resolution;
        ep.LastUpdatedAtUtc = nowUtc;
        ep.StateVersion++;
        ep.EventRevision++;
        ep.Limitations.Add(limitation);
        var snap = ep.ToSnapshot(_tickSize);
        EnqueueClosed(snap);
        _latestUpdated = snap;
    }

    private void PublishLatest(MutableEpisode ep) =>
        _latestUpdated = ep.ToSnapshot(_tickSize);

    private void EnqueueClosed(AuctionEpisodeSnapshot snap)
    {
        _recentlyClosed.Enqueue(snap);
        while (_recentlyClosed.Count > AuctionEpisodeSetSnapshot.RecentlyClosedCapacity)
            _recentlyClosed.Dequeue();
    }

    public IReadOnlyList<AuctionEpisodeSnapshot> SnapshotActive(decimal tickSize) =>
        _active.Values.Select(e => e.ToSnapshot(tickSize)).OrderBy(e => e.EpisodeId, StringComparer.Ordinal).ToArray();

    public IReadOnlyList<AuctionEpisodeSnapshot> SnapshotClosed() => _recentlyClosed.ToArray();

    public AuctionEpisodeSnapshot? LatestUpdated => _latestUpdated;

    public static ReferenceSidePosition ClassifySide(long priceTick, long zoneLowTick, long zoneHighTick)
    {
        if (priceTick >= zoneLowTick && priceTick <= zoneHighTick)
            return ReferenceSidePosition.AtReference;
        if (priceTick > zoneHighTick)
            return ReferenceSidePosition.Above;
        return ReferenceSidePosition.Below;
    }

    public static string RegistryKey(string auctionId, string referenceId) =>
        auctionId + "||" + referenceId;

    private static string ExtractRefId(string registryKey)
    {
        var idx = registryKey.IndexOf("||", StringComparison.Ordinal);
        return idx < 0 ? registryKey : registryKey[(idx + 2)..];
    }
}

internal sealed class MutableEpisode
{
    public MutableEpisode(
        string episodeId,
        string primaryAuctionId,
        StructuralReferenceSnapshot reference,
        ReferenceInteractionRole role,
        EpisodeTradeEvent first,
        string? directionalProvenance,
        DateTime nowUtc,
        EpisodeState state,
        int attemptCount)
    {
        EpisodeId = episodeId;
        PrimaryAuctionId = primaryAuctionId;
        ReferenceId = reference.ReferenceId;
        ReferenceType = reference.ReferenceType;
        ReferenceRole = role;
        ReferencePriceTick = reference.ZoneLowTick;
        ReferencePrice = reference.ZoneLow;
        ReferenceMaturity = reference.Maturity;
        SourceHorizon = reference.SourceHorizon;
        DirectionalContextProvenance = directionalProvenance;
        State = state;
        Resolution = EpisodeResolution.None;
        StartedAtUtc = nowUtc;
        LastUpdatedAtUtc = nowUtc;
        FirstInteractionEventId = first.EventIdentity;
        LastProcessedEventId = first.EventIdentity;
        AttemptCount = attemptCount;
        InteractionCount = 1;
        StateVersion = 1;
        EventRevision = 1;
        LastSide = AuctionEpisodeRegistry.ClassifySide(first.PriceTick, reference.ZoneLowTick, reference.ZoneHighTick);
        Limitations.Add(EpisodePolicyConfig.LimitationIntraAuctionResetNotCalibrated);
        Limitations.Add(EpisodePolicyConfig.LimitationApproachDistanceNotCalibrated);
        Limitations.Add(EpisodePolicyConfig.LimitationLocalValueNotAuthorized);
        if (!first.AggressorClassified)
            AggressorUnavailable = true;
    }

    public string EpisodeId { get; }
    public string PrimaryAuctionId { get; }
    public string ReferenceId { get; }
    public ReferenceType ReferenceType { get; }
    public ReferenceInteractionRole ReferenceRole { get; }
    public long ReferencePriceTick { get; }
    public decimal ReferencePrice { get; }
    public ReferenceMaturity ReferenceMaturity { get; }
    public ReferenceSourceHorizon SourceHorizon { get; }
    public string? DirectionalContextProvenance { get; set; }
    public EpisodeInteractionDirection InteractionDirection { get; private set; }
    public EpisodeState State { get; set; }
    public EpisodeResolution Resolution { get; set; }
    public DateTime StartedAtUtc { get; }
    public DateTime LastUpdatedAtUtc { get; set; }
    public string FirstInteractionEventId { get; }
    public string LastProcessedEventId { get; set; }
    public int AttemptCount { get; set; }
    public int InteractionCount { get; set; }
    public int CrossCount { get; set; }
    public int UpExcursionCount { get; set; }
    public int DownExcursionCount { get; set; }
    public long MaximumAboveDistanceTicks { get; set; }
    public long MaximumBelowDistanceTicks { get; set; }
    public long? MaximumCanonicalOutsideDistanceTicks { get; set; }
    public TimeSpan CanonicalOutsideDuration { get; private set; }
    public decimal CanonicalOutsideExecutedVolume { get; set; }
    public int CanonicalOutsideTradeCount { get; set; }
    public decimal? CanonicalOutsideBidVolume { get; set; }
    public decimal? CanonicalOutsideAskVolume { get; set; }
    public bool AggressorUnavailable { get; set; }
    public long StateVersion { get; set; }
    public long EventRevision { get; set; }
    public ReferenceSidePosition LastSide { get; set; }
    public List<string> Limitations { get; } = new();
    public Dictionary<long, decimal> LocalVolumes { get; } = new();

    private DateTime? _outsideSegmentStartUtc;

    public void OpenOutsideSegment(DateTime nowUtc)
    {
        if (_outsideSegmentStartUtc is null)
            _outsideSegmentStartUtc = nowUtc;
    }

    public void CloseOutsideSegment(DateTime nowUtc)
    {
        if (_outsideSegmentStartUtc is DateTime start)
        {
            var delta = nowUtc - start;
            if (delta > TimeSpan.Zero)
                CanonicalOutsideDuration += delta;
            _outsideSegmentStartUtc = null;
        }
    }

    public void UpdateDirection()
    {
        var up = MaximumAboveDistanceTicks > 0 || UpExcursionCount > 0;
        var down = MaximumBelowDistanceTicks > 0 || DownExcursionCount > 0;
        InteractionDirection = (up, down) switch
        {
            (true, true) => EpisodeInteractionDirection.Bidirectional,
            (true, false) => EpisodeInteractionDirection.Up,
            (false, true) => EpisodeInteractionDirection.Down,
            _ => EpisodeInteractionDirection.Unknown
        };
    }

    public AuctionEpisodeSnapshot ToSnapshot(decimal tickSize)
    {
        var duration = CanonicalOutsideDuration;
        if (_outsideSegmentStartUtc is DateTime openStart)
        {
            var extra = LastUpdatedAtUtc - openStart;
            if (extra > TimeSpan.Zero)
                duration += extra;
        }

        decimal? localPoc = null;
        if (LocalVolumes.Count > 0)
        {
            var pocTick = PocSelector.SelectPocTick(LocalVolumes, previousPocTick: null);
            localPoc = pocTick * tickSize;
        }

        decimal? delta = null;
        if (CanonicalOutsideBidVolume is not null || CanonicalOutsideAskVolume is not null)
            delta = (CanonicalOutsideAskVolume ?? 0m) - (CanonicalOutsideBidVolume ?? 0m);

        var aggressor = AggressorUnavailable
            ? AggressorEvidenceAvailability.Unavailable
            : (CanonicalOutsideBidVolume is null && CanonicalOutsideAskVolume is null
                ? AggressorEvidenceAvailability.Unavailable
                : AggressorEvidenceAvailability.Available);

        var quality = AggressorUnavailable || localPoc is null
            ? EpisodeDataQuality.Partial
            : EpisodeDataQuality.Complete;

        return new AuctionEpisodeSnapshot(
            EpisodeId,
            EpisodePolicyConfig.PolicyVersion,
            PrimaryAuctionId,
            ReferenceId,
            ReferenceType,
            ReferenceRole,
            ReferencePriceTick,
            ReferencePrice,
            ReferenceMaturity,
            SourceHorizon,
            DirectionalContextProvenance,
            InteractionDirection,
            State,
            Resolution,
            StartedAtUtc,
            LastUpdatedAtUtc,
            FirstInteractionEventId,
            LastProcessedEventId,
            AttemptCount,
            InteractionCount,
            CrossCount,
            UpExcursionCount,
            DownExcursionCount,
            MaximumAboveDistanceTicks,
            MaximumBelowDistanceTicks,
            MaximumCanonicalOutsideDistanceTicks,
            duration,
            CanonicalOutsideExecutedVolume,
            CanonicalOutsideTradeCount,
            CanonicalOutsideBidVolume,
            CanonicalOutsideAskVolume,
            delta,
            aggressor,
            localPoc,
            StateVersion,
            EventRevision,
            quality,
            Limitations.Distinct(StringComparer.Ordinal).ToArray());
    }
}
