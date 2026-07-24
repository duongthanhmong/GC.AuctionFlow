using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Reference;

namespace GC.AuctionFlow.Evidence;

/// <summary>
/// Accumulates raw Acceptance/Re-entry evidence from EpisodeMeasurementEvent.
/// Never mutates Episode State or Resolution.
/// </summary>
internal sealed class MutableEvidence
{
    private DateTime? _outsideSegmentStart;
    private DateTime? _insideSegmentStart;
    private readonly Dictionary<long, decimal> _localVolumes = new();
    private long? _localPocAtReentry;

    public MutableEvidence(
        string evidenceId,
        AuctionEpisodeSnapshot episode,
        DateTime nowUtc)
    {
        EvidenceId = evidenceId;
        EpisodeId = episode.EpisodeId;
        PrimaryAuctionId = episode.PrimaryAuctionId;
        ReferenceId = episode.ReferenceId;
        ReferenceType = episode.ReferenceType;
        ReferenceRole = episode.ReferenceRole;
        ReferencePriceTick = episode.ReferencePriceTick;
        ReferencePrice = episode.ReferencePrice;
        CanonicalOutsideDirection = ReferenceInteractionRoleMapper.CanonicalOutsideSide(episode.ReferenceRole);
        EpisodeState = episode.State;
        EpisodeResolution = episode.Resolution;
        StartedAtUtc = nowUtc;
        LastUpdatedAtUtc = nowUtc;
        DirectionalProvenance = episode.DirectionalContextProvenance;
        StateVersion = 1;
        EventRevision = 1;
        MeasurementStatus = EvidenceMeasurementStatus.Active;

        Limitations.Add(AcceptanceReentryEvidencePolicyConfig.LimitationHistoryLiveOnly);
        Limitations.Add(AcceptanceReentryEvidencePolicyConfig.LimitationOutsideCloseUnavailable);
        Limitations.Add(AcceptanceReentryEvidencePolicyConfig.LimitationTpoOutsideUnavailable);
        Limitations.Add(AcceptanceReentryEvidencePolicyConfig.LimitationLocalValueNotAuthorized);
        Limitations.Add(AcceptanceReentryEvidencePolicyConfig.LimitationOldValueReclaimNotCalibrated);
        Limitations.Add(AcceptanceReentryEvidencePolicyConfig.LimitationRetestHoldNotCalibrated);
        Limitations.Add(AcceptanceReentryEvidencePolicyConfig.LimitationOppositeAggressionNotAuthorized);
        Limitations.Add(AcceptanceReentryEvidencePolicyConfig.LimitationLocalValueRebuildNotAuthorized);
        Limitations.Add(AcceptanceReentryEvidencePolicyConfig.LimitationOldDirectionRequiresOrderflow);
        Limitations.Add(AcceptanceReentryEvidencePolicyConfig.LimitationStableReacceptanceNotCalibrated);

        if (episode.ReferenceRole == ReferenceInteractionRole.Centerline)
            Limitations.Add(AcceptanceReentryEvidencePolicyConfig.LimitationCenterlineNotApplicable);
    }

    public string EvidenceId { get; }
    public string EpisodeId { get; }
    public string PrimaryAuctionId { get; }
    public string ReferenceId { get; }
    public ReferenceType ReferenceType { get; }
    public ReferenceInteractionRole ReferenceRole { get; }
    public long ReferencePriceTick { get; }
    public decimal ReferencePrice { get; }
    public ReferenceSidePosition? CanonicalOutsideDirection { get; }
    public EpisodeState EpisodeState { get; set; }
    public EpisodeResolution EpisodeResolution { get; set; }
    public EvidenceMeasurementStatus MeasurementStatus { get; set; }
    public AcceptanceObservationState AcceptanceObservation { get; private set; }
    public ReentryObservationState ReentryObservation { get; private set; }
    public DateTime StartedAtUtc { get; }
    public DateTime LastUpdatedAtUtc { get; set; }
    public DateTime? FirstOutsideAtUtc { get; private set; }
    public DateTime? FirstGeometricReentryAtUtc { get; private set; }
    public DateTime? LastOutsideAtUtc { get; private set; }
    public DateTime? LastInsideAtUtc { get; private set; }
    public TimeSpan OutsideTime { get; private set; }
    public TimeSpan InsideTimeAfterReentry { get; private set; }
    public decimal OutsideVolume { get; private set; }
    public decimal TotalVolume { get; private set; }
    public long OutsideTradeCount { get; private set; }
    public long TotalTradeCount { get; private set; }
    public int OutsideSegmentCount { get; private set; }
    public int AttemptCount { get; private set; }
    public long MaximumOutsideDistanceTicks { get; private set; }
    public long? CurrentDistanceTicks { get; private set; }
    public decimal? OutsideBid { get; private set; }
    public decimal? OutsideAsk { get; private set; }
    public bool AggressorUnavailable { get; private set; }
    public decimal InsideVolumeAfterReentry { get; private set; }
    public long InsideTradeCountAfterReentry { get; private set; }
    public decimal? InsideBidAfterReentry { get; private set; }
    public decimal? InsideAskAfterReentry { get; private set; }
    public int SubsequentReferenceTests { get; private set; }
    public int OutsideReattemptCount { get; private set; }
    public long MaximumDistanceReturnedInsideTicks { get; private set; }
    public ReferenceSidePosition? LatestSide { get; private set; }
    public int ConsecutiveOutsideEvents { get; private set; }
    public string? DirectionalProvenance { get; set; }
    public long StateVersion { get; set; }
    public long EventRevision { get; set; }
    public List<string> Limitations { get; } = new();
    public HashSet<string> ProcessedEventIds { get; } = new(StringComparer.Ordinal);

    public bool ApplyEvent(EpisodeMeasurementEvent evt, DateTime nowUtc)
    {
        if (MeasurementStatus != EvidenceMeasurementStatus.Active)
            return false;
        if (!ProcessedEventIds.Add(evt.EventIdentity))
            return false;

        var prevAcc = AcceptanceObservation;
        var prevRe = ReentryObservation;
        var isCenterline = ReferenceRole == ReferenceInteractionRole.Centerline;
        var outsideSide = CanonicalOutsideDirection;
        var isOutside = outsideSide.HasValue && evt.Side == outsideSide.Value;
        var isInsideOrAt = !isOutside;

        EpisodeState = evt.StateAfter;
        LastUpdatedAtUtc = nowUtc;
        LatestSide = evt.Side;
        CurrentDistanceTicks = evt.DistanceFromReferenceTicks;
        TotalVolume += evt.Volume;
        TotalTradeCount++;
        AttemptCount = evt.AttemptCount;

        if (!evt.AggressorClassified)
            AggressorUnavailable = true;

        if (!_localVolumes.TryGetValue(evt.PriceTick, out var lv))
            lv = 0m;
        _localVolumes[evt.PriceTick] = lv + evt.Volume;

        if (!isCenterline && outsideSide.HasValue)
        {
            if (evt.OutsideSegmentOpened || (isOutside && FirstOutsideAtUtc is null))
            {
                if (FirstOutsideAtUtc is null)
                    FirstOutsideAtUtc = evt.EventUtc;
                OutsideSegmentCount++;
                CloseInsideSegment(evt.EventUtc);
                _outsideSegmentStart = evt.EventUtc;
                ConsecutiveOutsideEvents = 1;
            }
            else if (isOutside)
            {
                ConsecutiveOutsideEvents++;
            }

            if (isOutside)
            {
                LastOutsideAtUtc = evt.EventUtc;
                OutsideVolume += evt.Volume;
                OutsideTradeCount++;
                if (evt.DistanceFromReferenceTicks > MaximumOutsideDistanceTicks)
                    MaximumOutsideDistanceTicks = evt.DistanceFromReferenceTicks;
                if (evt.AggressorClassified)
                {
                    if (evt.IsBid) OutsideBid = (OutsideBid ?? 0m) + evt.Volume;
                    if (evt.IsAsk) OutsideAsk = (OutsideAsk ?? 0m) + evt.Volume;
                }
            }

            if (evt.OutsideSegmentClosed || evt.GeometricReentryObserved)
            {
                CloseOutsideSegment(evt.EventUtc);
                ConsecutiveOutsideEvents = 0;
                LastInsideAtUtc = evt.EventUtc;
                if (FirstGeometricReentryAtUtc is null && evt.GeometricReentryObserved)
                {
                    FirstGeometricReentryAtUtc = evt.EventUtc;
                    _localPocAtReentry = ComputeLocalPoc();
                }
                _insideSegmentStart = evt.EventUtc;
            }
            else if (isInsideOrAt && FirstGeometricReentryAtUtc is not null && _outsideSegmentStart is null)
            {
                LastInsideAtUtc = evt.EventUtc;
                InsideVolumeAfterReentry += evt.Volume;
                InsideTradeCountAfterReentry++;
                if (evt.AggressorClassified)
                {
                    if (evt.IsBid) InsideBidAfterReentry = (InsideBidAfterReentry ?? 0m) + evt.Volume;
                    if (evt.IsAsk) InsideAskAfterReentry = (InsideAskAfterReentry ?? 0m) + evt.Volume;
                }

                var insideDist = evt.Side == ReferenceSidePosition.AtReference
                    ? 0
                    : evt.DistanceFromReferenceTicks;
                if (insideDist > MaximumDistanceReturnedInsideTicks)
                    MaximumDistanceReturnedInsideTicks = insideDist;

                if (evt.Side == ReferenceSidePosition.AtReference)
                    SubsequentReferenceTests++;
            }

            if (evt.AttemptIncremented && AttemptCount > 1 && FirstGeometricReentryAtUtc is not null)
                OutsideReattemptCount++;
        }

        // Observation mapping from Episode state (no numeric thresholds).
        if (isCenterline)
        {
            AcceptanceObservation = AcceptanceObservationState.None;
            ReentryObservation = ReentryObservationState.None;
        }
        else
        {
            AcceptanceObservation = MapAcceptance(evt.StateAfter, FirstOutsideAtUtc is not null, FirstGeometricReentryAtUtc is not null);
            ReentryObservation = MapReentry(evt.StateAfter, FirstGeometricReentryAtUtc is not null, OutsideReattemptCount);
        }

        var semantic = prevAcc != AcceptanceObservation || prevRe != ReentryObservation;
        if (semantic)
            StateVersion++;
        EventRevision++;
        return true;
    }

    /// <summary>Copy Episode provenance only. Does not freeze — registry owns freeze lifecycle.</summary>
    public void SyncEpisodeProvenance(AuctionEpisodeSnapshot episode, DateTime nowUtc)
    {
        EpisodeState = episode.State;
        EpisodeResolution = episode.Resolution;
        AttemptCount = episode.AttemptCount;
        LastUpdatedAtUtc = nowUtc;
        if (episode.DirectionalContextProvenance is not null)
            DirectionalProvenance = episode.DirectionalContextProvenance;
    }

    public void Freeze(DateTime nowUtc, EvidenceMeasurementStatus status)
    {
        if (MeasurementStatus != EvidenceMeasurementStatus.Active)
            return;
        CloseOutsideSegment(nowUtc);
        CloseInsideSegment(nowUtc);
        MeasurementStatus = status;
        LastUpdatedAtUtc = nowUtc;
        StateVersion++;
        EventRevision++;
    }

    private void CloseOutsideSegment(DateTime at)
    {
        if (_outsideSegmentStart is DateTime start)
        {
            var d = at - start;
            if (d > TimeSpan.Zero)
                OutsideTime += d;
            _outsideSegmentStart = null;
        }
    }

    private void CloseInsideSegment(DateTime at)
    {
        if (_insideSegmentStart is DateTime start && FirstGeometricReentryAtUtc is not null)
        {
            var d = at - start;
            if (d > TimeSpan.Zero)
                InsideTimeAfterReentry += d;
            _insideSegmentStart = null;
        }
    }

    private static AcceptanceObservationState MapAcceptance(EpisodeState state, bool hadOutside, bool hadReentry)
    {
        if (!hadOutside)
            return AcceptanceObservationState.None;
        if (hadOutside && hadReentry)
            return AcceptanceObservationState.Unresolved;
        return state switch
        {
            EpisodeState.OutsideAttempt => AcceptanceObservationState.Early,
            EpisodeState.Developing => AcceptanceObservationState.Developing,
            EpisodeState.ReentryDeveloping => AcceptanceObservationState.Unresolved,
            EpisodeState.InvalidData => AcceptanceObservationState.Unknown,
            _ => hadOutside ? AcceptanceObservationState.Developing : AcceptanceObservationState.None
        };
    }

    private static ReentryObservationState MapReentry(EpisodeState state, bool hadReentry, int reattempts)
    {
        if (!hadReentry)
            return ReentryObservationState.None;
        if (reattempts > 0 && state is EpisodeState.OutsideAttempt or EpisodeState.Developing)
            return ReentryObservationState.Unresolved;
        if (state == EpisodeState.ReentryDeveloping)
            return ReentryObservationState.GeometricReentry;
        // Additional inside activity after geometric reentry (measurement maturity only).
        return ReentryObservationState.Developing;
    }

    private long? ComputeLocalPoc()
    {
        if (_localVolumes.Count == 0)
            return null;
        long? bestTick = null;
        decimal bestVol = -1m;
        foreach (var kv in _localVolumes.OrderBy(k => k.Key))
        {
            if (kv.Value > bestVol)
            {
                bestVol = kv.Value;
                bestTick = kv.Key;
            }
        }
        return bestTick;
    }

    public AcceptanceReentryEvidenceSnapshot ToSnapshot(DateTime nowUtc)
    {
        // Include open segments without permanently closing.
        var outsideTime = OutsideTime;
        if (_outsideSegmentStart is DateTime os)
        {
            var d = nowUtc - os;
            if (d > TimeSpan.Zero) outsideTime += d;
        }

        var insideTime = InsideTimeAfterReentry;
        if (_insideSegmentStart is DateTime ins && FirstGeometricReentryAtUtc is not null)
        {
            var d = nowUtc - ins;
            if (d > TimeSpan.Zero) insideTime += d;
        }

        var totalDuration = LastUpdatedAtUtc > StartedAtUtc
            ? LastUpdatedAtUtc - StartedAtUtc
            : TimeSpan.Zero;
        if (totalDuration < outsideTime)
            totalDuration = outsideTime + insideTime;

        var localPoc = ComputeLocalPoc();
        long? localDisp = localPoc is long lp ? lp - ReferencePriceTick : null;
        long? localRel = localDisp;
        long? localChange = localPoc is long lp2 && _localPocAtReentry is long at
            ? lp2 - at
            : null;

        TimeSpan? reentrySpeed = FirstOutsideAtUtc is DateTime fo && FirstGeometricReentryAtUtc is DateTime fr
            ? fr - fo
            : null;

        TimeSpan? elapsedSinceInside = LastInsideAtUtc is DateTime li
            ? nowUtc - li
            : null;

        var aggressor = AggressorUnavailable
            ? AggressorEvidenceAvailability.Unavailable
            : AggressorEvidenceAvailability.Available;

        var unavailableAcc = new List<string>
        {
            AcceptanceReentryEvidencePolicyConfig.LimitationOutsideCloseUnavailable,
            AcceptanceReentryEvidencePolicyConfig.LimitationTpoOutsideUnavailable,
            AcceptanceReentryEvidencePolicyConfig.LimitationLocalValueNotAuthorized,
            AcceptanceReentryEvidencePolicyConfig.LimitationOldValueReclaimNotCalibrated,
            AcceptanceReentryEvidencePolicyConfig.LimitationRetestHoldNotCalibrated
        };
        var unavailableRe = new List<string>
        {
            AcceptanceReentryEvidencePolicyConfig.LimitationOppositeAggressionNotAuthorized,
            AcceptanceReentryEvidencePolicyConfig.LimitationLocalValueRebuildNotAuthorized,
            AcceptanceReentryEvidencePolicyConfig.LimitationOldDirectionRequiresOrderflow,
            AcceptanceReentryEvidencePolicyConfig.LimitationStableReacceptanceNotCalibrated
        };

        var isCenterline = ReferenceRole == ReferenceInteractionRole.Centerline;
        AcceptanceEvidenceVector acc;
        ReentryEvidenceVector re;

        if (isCenterline)
        {
            acc = new AcceptanceEvidenceVector(
                TimeSpan.Zero, totalDuration, null,
                0m, TotalVolume, null,
                0, TotalTradeCount, null,
                0, AttemptCount, 0, CurrentDistanceTicks,
                null, null, null, aggressor,
                localPoc, localDisp, LatestSide, 0, null,
                null, null, null, null, null, null, null,
                new[] { AcceptanceReentryEvidencePolicyConfig.LimitationCenterlineNotApplicable });
            re = new ReentryEvidenceVector(
                false, null, null, 0, null, TimeSpan.Zero, 0m, 0,
                null, null, null, aggressor, 0, 0, LatestSide,
                localPoc, localRel, null,
                null, null, null, null, null,
                new[] { AcceptanceReentryEvidencePolicyConfig.LimitationCenterlineNotApplicable });
        }
        else
        {
            decimal? outsideDelta = OutsideBid is null && OutsideAsk is null
                ? null
                : (OutsideAsk ?? 0m) - (OutsideBid ?? 0m);
            decimal? insideDelta = InsideBidAfterReentry is null && InsideAskAfterReentry is null
                ? null
                : (InsideAskAfterReentry ?? 0m) - (InsideBidAfterReentry ?? 0m);

            acc = new AcceptanceEvidenceVector(
                outsideTime, totalDuration, EvidenceRatio.TryCompute(outsideTime, totalDuration),
                OutsideVolume, TotalVolume, EvidenceRatio.TryCompute(OutsideVolume, TotalVolume),
                OutsideTradeCount, TotalTradeCount, EvidenceRatio.TryCompute(OutsideTradeCount, TotalTradeCount),
                OutsideSegmentCount, AttemptCount, MaximumOutsideDistanceTicks, CurrentDistanceTicks,
                AggressorUnavailable ? null : OutsideBid,
                AggressorUnavailable ? null : OutsideAsk,
                AggressorUnavailable ? null : outsideDelta,
                aggressor, localPoc, localDisp, LatestSide, ConsecutiveOutsideEvents, elapsedSinceInside,
                null, null, null, null, null, null, null, unavailableAcc);
            re = new ReentryEvidenceVector(
                FirstGeometricReentryAtUtc is not null,
                FirstGeometricReentryAtUtc,
                reentrySpeed,
                MaximumDistanceReturnedInsideTicks,
                LatestSide is ReferenceSidePosition.AtReference
                    ? 0
                    : (FirstGeometricReentryAtUtc is not null && LatestSide != CanonicalOutsideDirection
                        ? CurrentDistanceTicks
                        : null),
                insideTime,
                InsideVolumeAfterReentry,
                InsideTradeCountAfterReentry,
                AggressorUnavailable ? null : InsideBidAfterReentry,
                AggressorUnavailable ? null : InsideAskAfterReentry,
                AggressorUnavailable ? null : insideDelta,
                aggressor,
                SubsequentReferenceTests,
                OutsideReattemptCount,
                LatestSide,
                localPoc,
                localRel,
                localChange,
                null, null, null, null, null,
                unavailableRe);
        }

        var availability = new Dictionary<string, EvidenceComponentAvailability>(StringComparer.Ordinal)
        {
            ["OutsideTime"] = EvidenceComponentAvailability.Available,
            ["OutsideVolume"] = EvidenceComponentAvailability.Available,
            ["OutsideTradeCount"] = EvidenceComponentAvailability.Available,
            ["OutsideCloseRatio"] = EvidenceComponentAvailability.Unavailable,
            ["TpoCountOutside"] = EvidenceComponentAvailability.Unavailable,
            ["LocalValue"] = EvidenceComponentAvailability.NotCalibrated,
            ["BidAsk"] = AggressorUnavailable
                ? EvidenceComponentAvailability.Unavailable
                : EvidenceComponentAvailability.Available,
            ["GeometricReentry"] = isCenterline
                ? EvidenceComponentAvailability.NotApplicable
                : EvidenceComponentAvailability.Available,
            ["StableReacceptance"] = EvidenceComponentAvailability.NotCalibrated
        };

        return new AcceptanceReentryEvidenceSnapshot(
            EvidenceId,
            AcceptanceReentryEvidencePolicyConfig.PolicyVersion,
            EpisodeId,
            PrimaryAuctionId,
            ReferenceId,
            ReferenceType,
            ReferenceRole,
            ReferencePriceTick,
            ReferencePrice,
            CanonicalOutsideDirection,
            EpisodeState,
            EpisodeResolution,
            MeasurementStatus,
            AcceptanceObservation,
            ReentryObservation,
            StartedAtUtc,
            LastUpdatedAtUtc,
            FirstOutsideAtUtc,
            FirstGeometricReentryAtUtc,
            LastOutsideAtUtc,
            LastInsideAtUtc,
            totalDuration,
            TotalVolume,
            TotalTradeCount,
            acc,
            re,
            StateVersion,
            EventRevision,
            AggressorUnavailable ? EpisodeDataQuality.Partial : EpisodeDataQuality.Complete,
            availability,
            Limitations.Distinct(StringComparer.Ordinal).ToArray(),
            DirectionalProvenance);
    }
}
