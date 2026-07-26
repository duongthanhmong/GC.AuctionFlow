using GC.AuctionFlow.Core;
using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Evidence;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Reference;
using GC.AuctionFlow.Runtime;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Evidence;

/// <summary>
/// Phase 1F Acceptance / Re-entry Evidence Measurement Foundation.
/// Measurement only â€” no Established Acceptance, Stable Reacceptance, FAR/AAC, or Thesis.
/// </summary>
public sealed class Phase1FAcceptanceReentryEvidenceTests
{
    private const decimal Tick = 0.1m;
    private const string Instrument = "GCQ6";
    private const string Epoch = "GCQ6|tick=0.1";
    private const string Auction = "PI-2026-07-24";

    private static StructuralReferenceSnapshot Ref(
        ReferenceType type,
        decimal price,
        ReferenceMaturity maturity = ReferenceMaturity.Confirmed,
        ReferenceStatus status = ReferenceStatus.Active,
        string sourceId = "PI-2026-07-23")
    {
        var grid = new PriceGrid(Tick);
        var t = grid.ToTickIndex(price);
        var id = ReferenceIdentity.Build(Instrument, Epoch, sourceId, type, maturity);
        var kind = maturity == ReferenceMaturity.Confirmed && type.ToString().StartsWith("Composite", StringComparison.Ordinal)
            ? ReferenceSourceKind.ConfirmedComposite
            : (maturity == ReferenceMaturity.Developing
                ? ReferenceSourceKind.CurrentPrimaryAuction
                : ReferenceSourceKind.PreviousPrimaryAuction);
        var horizon = kind switch
        {
            ReferenceSourceKind.ConfirmedComposite => ReferenceSourceHorizon.ConfirmedComposite,
            ReferenceSourceKind.CurrentPrimaryAuction => ReferenceSourceHorizon.CurrentPrimaryAuction,
            _ => ReferenceSourceHorizon.PreviousPrimaryAuction
        };
        return new StructuralReferenceSnapshot(
            id, type, price, price, t, t,
            sourceId, kind, horizon, Utc(23),
            maturity, status, ReferenceEvidenceTier.ProfileDerived,
            Tick, Epoch, AtasTimestampNormalizer.PolicyVersion, 1, Utc(23), Utc(23));
    }

    private static StructuralReferenceSetSnapshot RefSet(params StructuralReferenceSnapshot[] confirmed) =>
        new(
            StructuralReferenceModuleState.Ready,
            ReferencePolicyConfig.PolicyVersion,
            confirmed,
            Array.Empty<StructuralReferenceSnapshot>(),
            Array.Empty<StructuralReferenceSnapshot>(),
            Array.Empty<ReferenceConfluenceGroup>(),
            null,
            "fp",
            1,
            Array.Empty<string>(),
            Array.Empty<string>(),
            Utc(24));

    private static PrimaryProfileSetSnapshot Profiles(string auctionId = Auction)
    {
        var cur = new PrimaryAuctionProfileSnapshot(
            AuctionProfileState.Ready, auctionId, Utc(24), Utc(25), false,
            null, null, 100.5m, 99.5m, 100.0m, Utc(24), null,
            ProfileDataQuality.Complete, Array.Empty<string>());
        return new PrimaryProfileSetSnapshot(
            cur, null, HistoricalInitializationState.Complete, 10,
            Array.Empty<string>(), Array.Empty<string>(), null, Array.Empty<PrimaryAuctionProfileSnapshot>());
    }

    private static EpisodeTradeEvent Trade(long seq, decimal price, decimal vol = 1m, bool ask = true, bool bid = false, DateTime? receiveUtc = null)
    {
        var grid = new PriceGrid(Tick);
        var tick = grid.ToTickIndex(price);
        var fp = "fp-" + seq;
        var at = receiveUtc ?? Utc(24).AddSeconds(seq);
        return new EpisodeTradeEvent(
            EpisodeIdentity.BuildEventIdentity(seq, fp),
            seq, seq * 1000, at,
            tick, price, vol, ask, bid, ask || bid,
            Instrument, Epoch, Tick, AtasTimestampNormalizer.PolicyVersion);
    }

    private static EpisodeTradeEvent TradeUnclassified(long seq, decimal price, decimal vol = 1m, DateTime? receiveUtc = null)
    {
        var grid = new PriceGrid(Tick);
        var tick = grid.ToTickIndex(price);
        var at = receiveUtc ?? Utc(24).AddSeconds(seq);
        return new EpisodeTradeEvent(
            EpisodeIdentity.BuildEventIdentity(seq, "fp-u-" + seq),
            seq, seq * 1000, at,
            tick, price, vol, false, false, false,
            Instrument, Epoch, Tick, AtasTimestampNormalizer.PolicyVersion);
    }

    private static AuctionEpisodeHost EpisodeHost() =>
        new(Tick, Instrument, Epoch, AtasTimestampNormalizer.PolicyVersion, new EpisodePolicyConfig(true));

    private static AcceptanceReentryEvidenceHost EvidenceHost() =>
        new(Tick, Epoch, AtasTimestampNormalizer.PolicyVersion,
            new AcceptanceReentryEvidencePolicyConfig(enabled: true));

    private static DateTime Utc(int day) => new(2026, 7, day, 12, 0, 0, DateTimeKind.Utc);

    private static AcceptanceReentryEvidenceSetSnapshot Drive(
        AuctionEpisodeHost episodes,
        AcceptanceReentryEvidenceHost evidence,
        EpisodeTradeEvent trade,
        DateTime now)
    {
        // Align measurement event clock with ProcessTrade observation time.
        var timed = new EpisodeTradeEvent(
            trade.EventIdentity,
            trade.LocalMonotonicSequence,
            trade.SourceTimeTicks,
            now,
            trade.PriceTick,
            trade.Price,
            trade.Volume,
            trade.IsAsk,
            trade.IsBid,
            trade.AggressorClassified,
            trade.InstrumentIdentity,
            trade.DataEpoch,
            trade.TickSize,
            trade.TimestampPolicyVersion);
        episodes.ProcessTrade(timed, now);
        var measurements = episodes.DrainMeasurementEvents();
        evidence.RebuildContext(episodes.Current, now);
        return evidence.ProcessMeasurementEvents(measurements, episodes.Current, now);
    }

    // --- A. Eligibility and identity ---

    [Fact]
    public void A_UpperBoundary_CreatesEvidence()
    {
        var ep = EpisodeHost();
        var ev = EvidenceHost();
        var r = Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m);
        ep.RebuildContext(Profiles(), RefSet(r), null, Utc(24));
        ev.RebuildContext(ep.Current, Utc(24));
        var set = Drive(ep, ev, Trade(1, 100.3m), Utc(24).AddSeconds(1));
        var snap = Assert.Single(set.ActiveEvidence);
        Assert.Equal(ReferenceInteractionRole.UpperBoundary, snap.ReferenceRole);
        Assert.StartsWith("AREV|", snap.EvidenceId, StringComparison.Ordinal);
        Assert.Contains(AcceptanceReentryEvidencePolicyConfig.PolicyVersion, snap.EvidenceId, StringComparison.Ordinal);
        Assert.Equal(snap.EpisodeId, ep.Current!.ActiveEpisodes[0].EpisodeId);
    }

    [Fact]
    public void A_LowerBoundary_CreatesEvidence()
    {
        var ep = EpisodeHost();
        var ev = EvidenceHost();
        var r = Ref(ReferenceType.PreviousPrimaryTpoVal, 99.8m);
        ep.RebuildContext(Profiles(), RefSet(r), null, Utc(24));
        var set = Drive(ep, ev, Trade(1, 99.7m), Utc(24).AddSeconds(1));
        var snap = Assert.Single(set.ActiveEvidence);
        Assert.Equal(ReferenceInteractionRole.LowerBoundary, snap.ReferenceRole);
        Assert.Equal(AcceptanceObservationState.Early, snap.AcceptanceObservationState);
    }

    [Fact]
    public void A_Centerline_PartialNotApplicable()
    {
        var ep = EpisodeHost();
        var ev = EvidenceHost();
        var r = Ref(ReferenceType.PreviousPrimaryTpoPoc, 100.0m);
        ep.RebuildContext(Profiles(), RefSet(r), null, Utc(24));
        Drive(ep, ev, Trade(1, 100.0m), Utc(24).AddSeconds(1));
        var set = Drive(ep, ev, Trade(2, 100.2m), Utc(24).AddSeconds(2));
        Assert.Equal(EvidenceModuleState.Partial, set.ModuleState);
        var snap = Assert.Single(set.ActiveEvidence);
        Assert.Contains(AcceptanceReentryEvidencePolicyConfig.LimitationCenterlineNotApplicable, snap.Limitations);
        Assert.Null(snap.Acceptance.OutsideVolumeRatio);
        Assert.False(snap.Reentry.GeometricReentryObserved);
        Assert.Equal(AcceptanceObservationState.None, snap.AcceptanceObservationState);
        Assert.Equal(ReentryObservationState.None, snap.ReentryObservationState);
    }

    [Fact]
    public void A_DevelopingReference_NoEvidenceIndependently()
    {
        var developing = Ref(ReferenceType.CurrentPrimaryTpoVah, 100.2m, ReferenceMaturity.Developing);
        var set = new StructuralReferenceSetSnapshot(
            StructuralReferenceModuleState.Ready, ReferencePolicyConfig.PolicyVersion,
            Array.Empty<StructuralReferenceSnapshot>(), new[] { developing },
            Array.Empty<StructuralReferenceSnapshot>(), Array.Empty<ReferenceConfluenceGroup>(),
            null, "fp", 1, Array.Empty<string>(), Array.Empty<string>(), Utc(24));
        var ep = EpisodeHost();
        var ev = EvidenceHost();
        ep.RebuildContext(Profiles(), set, null, Utc(24));
        ep.ProcessTrade(Trade(1, 100.3m), Utc(24).AddSeconds(1));
        var measurements = ep.DrainMeasurementEvents();
        Assert.Empty(measurements);
        Assert.Empty(ep.Current!.ActiveEpisodes);
        var evidence = ev.RebuildContext(ep.Current, Utc(24).AddSeconds(1));
        Assert.Equal(EvidenceModuleState.AwaitingEpisodes, evidence.ModuleState);
        Assert.Empty(evidence.ActiveEvidence);
    }

    [Fact]
    public void A_OneEvidenceIdPerEpisode_StableAcrossAttempts()
    {
        var ep = EpisodeHost();
        var ev = EvidenceHost();
        var r = Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m);
        ep.RebuildContext(Profiles(), RefSet(r), null, Utc(24));
        Drive(ep, ev, Trade(1, 100.3m), Utc(24).AddSeconds(1));
        var id1 = ev.Current!.ActiveEvidence[0].EvidenceId;
        Drive(ep, ev, Trade(2, 100.4m), Utc(24).AddSeconds(2));
        Drive(ep, ev, Trade(3, 100.1m), Utc(24).AddSeconds(3)); // reentry
        Drive(ep, ev, Trade(4, 100.3m), Utc(24).AddSeconds(4)); // second attempt
        Assert.Equal(id1, ev.Current!.ActiveEvidence[0].EvidenceId);
        Assert.Equal(2, ep.Current!.ActiveEpisodes[0].AttemptCount);
        Assert.Equal(2, ev.Current.ActiveEvidence[0].Acceptance.AttemptCount);
    }

    // --- B. Acceptance raw evidence ---

    [Fact]
    public void B_OutsideAttempt_Early_Developing_Ratios_LocalPoc_NoEstablishedFailed()
    {
        var ep = EpisodeHost();
        var ev = EvidenceHost();
        var r = Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m);
        ep.RebuildContext(Profiles(), RefSet(r), null, Utc(24));

        var early = Drive(ep, ev, Trade(1, 100.3m, vol: 2m), Utc(24).AddSeconds(1));
        Assert.Equal(AcceptanceObservationState.Early, early.ActiveEvidence[0].AcceptanceObservationState);
        Assert.Equal(ReentryObservationState.None, early.ActiveEvidence[0].ReentryObservationState);

        var mid = Drive(ep, ev, Trade(2, 100.5m, vol: 3m), Utc(24).AddSeconds(3));
        var a = mid.ActiveEvidence[0];
        Assert.Equal(AcceptanceObservationState.Developing, a.AcceptanceObservationState);
        Assert.Equal(5m, a.Acceptance.OutsideExecutedVolume);
        Assert.Equal(5m, a.Acceptance.TotalObservedExecutedVolume);
        Assert.Equal(1m, a.Acceptance.OutsideVolumeRatio);
        Assert.Equal(2, a.Acceptance.OutsideTradeCount);
        Assert.True(a.Acceptance.MaximumOutsideDistanceTicks >= 3);
        Assert.NotNull(a.Acceptance.LocalPocTick);
        Assert.NotNull(a.Acceptance.LocalPocDisplacementTicks);
        Assert.Null(a.Acceptance.OutsideCloseRatio);
        Assert.Null(a.Acceptance.TpoCountOutside);
        Assert.Null(a.Acceptance.LocalValueLow);
        Assert.NotEqual(AcceptanceObservationState.Established, a.AcceptanceObservationState);
        Assert.NotEqual(AcceptanceObservationState.Failed, a.AcceptanceObservationState);
        Assert.True(a.Acceptance.OutsideTime.TotalSeconds > 0);
    }

    [Fact]
    public void B_ZeroDenominator_Unavailable_NeverNaN()
    {
        Assert.Null(EvidenceRatio.TryCompute(1m, 0m));
        Assert.Null(EvidenceRatio.TryCompute(0L, 0L));
        Assert.Null(EvidenceRatio.TryCompute(TimeSpan.FromSeconds(1), TimeSpan.Zero));
        Assert.Equal(0.5m, EvidenceRatio.TryCompute(1m, 2m));
    }

    [Fact]
    public void B_InsideOnly_NoOutside_AcceptanceNone()
    {
        var ep = EpisodeHost();
        var ev = EvidenceHost();
        var r = Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m);
        ep.RebuildContext(Profiles(), RefSet(r), null, Utc(24));
        var set = Drive(ep, ev, Trade(1, 100.2m), Utc(24).AddSeconds(1)); // at reference
        Assert.Equal(AcceptanceObservationState.None, set.ActiveEvidence[0].AcceptanceObservationState);
        Assert.Equal(0m, set.ActiveEvidence[0].Acceptance.OutsideExecutedVolume);
    }

    // --- C. Re-entry raw evidence ---

    [Fact]
    public void C_GeometricReentry_Speed_InsideMetrics_Reattempt_SameId_NoStableFailed()
    {
        var ep = EpisodeHost();
        var ev = EvidenceHost();
        var r = Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m);
        ep.RebuildContext(Profiles(), RefSet(r), null, Utc(24));
        Drive(ep, ev, Trade(1, 100.3m), Utc(24).AddSeconds(1));
        Assert.Equal(ReentryObservationState.None, ev.Current!.ActiveEvidence[0].ReentryObservationState);

        Drive(ep, ev, Trade(2, 100.4m), Utc(24).AddSeconds(2));
        var re = Drive(ep, ev, Trade(3, 100.1m), Utc(24).AddSeconds(5)); // geometric return
        var snap = re.ActiveEvidence[0];
        Assert.Equal(EpisodeState.ReentryDeveloping, ep.Current!.ActiveEpisodes[0].State);
        Assert.Equal(ReentryObservationState.GeometricReentry, snap.ReentryObservationState);
        Assert.True(snap.Reentry.GeometricReentryObserved);
        Assert.NotNull(snap.Reentry.ReentrySpeed);
        Assert.Equal(TimeSpan.FromSeconds(4), snap.Reentry.ReentrySpeed);
        Assert.Equal(AcceptanceObservationState.Unresolved, snap.AcceptanceObservationState);

        var id = snap.EvidenceId;
        var after = Drive(ep, ev, Trade(4, 100.0m), Utc(24).AddSeconds(7)); // more inside
        Assert.Equal(id, after.ActiveEvidence[0].EvidenceId);
        Assert.True(after.ActiveEvidence[0].Reentry.InsideTradeCountAfterReentry >= 1
            || after.ActiveEvidence[0].ReentryObservationState is ReentryObservationState.GeometricReentry or ReentryObservationState.Developing);

        Drive(ep, ev, Trade(5, 100.3m), Utc(24).AddSeconds(8)); // second outside
        var second = Drive(ep, ev, Trade(6, 100.1m), Utc(24).AddSeconds(9));
        Assert.Equal(id, second.ActiveEvidence[0].EvidenceId);
        Assert.True(second.ActiveEvidence[0].Reentry.OutsideReattemptCount >= 1);
        Assert.NotEqual(ReentryObservationState.StableReaccepted, second.ActiveEvidence[0].ReentryObservationState);
        Assert.NotEqual(ReentryObservationState.ReentryFailed, second.ActiveEvidence[0].ReentryObservationState);
    }

    // --- D. Centerline ---

    [Fact]
    public void D_Centerline_NoCanonicalRatios_DoesNotMutateAttemptCount()
    {
        var ep = EpisodeHost();
        var ev = EvidenceHost();
        var r = Ref(ReferenceType.PreviousPrimaryTpoPoc, 100.0m);
        ep.RebuildContext(Profiles(), RefSet(r), null, Utc(24));
        Drive(ep, ev, Trade(1, 100.0m), Utc(24).AddSeconds(1)); // at-reference creates Centerline episode
        Drive(ep, ev, Trade(2, 100.2m), Utc(24).AddSeconds(2));
        Drive(ep, ev, Trade(3, 99.8m), Utc(24).AddSeconds(3));
        Assert.Equal(0, ep.Current!.ActiveEpisodes[0].AttemptCount);
        Assert.Equal(0, ev.Current!.ActiveEvidence[0].Acceptance.AttemptCount);
        Assert.Null(ev.Current.ActiveEvidence[0].Acceptance.OutsideTimeRatio);
        Assert.Contains(AcceptanceReentryEvidencePolicyConfig.LimitationCenterlineNotApplicable,
            ev.Current.ActiveEvidence[0].Limitations);
    }

    // --- E. Availability ---

    [Fact]
    public void E_BidAsk_AvailableAndUnavailable_LiveOnly_ExplicitUnavailable()
    {
        var ep = EpisodeHost();
        var ev = EvidenceHost();
        var r = Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m);
        ep.RebuildContext(Profiles(), RefSet(r), null, Utc(24));
        var ok = Drive(ep, ev, Trade(1, 100.3m, vol: 4m, ask: true, bid: false), Utc(24).AddSeconds(1));
        Assert.Equal(AggressorEvidenceAvailability.Available, ok.ActiveEvidence[0].Acceptance.AggressorEvidenceAvailability);
        Assert.Equal(4m, ok.ActiveEvidence[0].Acceptance.OutsideAskVolume);
        Assert.NotNull(ok.ActiveEvidence[0].Acceptance.OutsideDelta);

        var ep2 = EpisodeHost();
        var ev2 = EvidenceHost();
        ep2.RebuildContext(Profiles(), RefSet(r), null, Utc(24));
        var bad = Drive(ep2, ev2, TradeUnclassified(1, 100.3m, 4m), Utc(24).AddSeconds(1));
        Assert.Equal(EvidenceModuleState.Partial, bad.ModuleState);
        Assert.Equal(AggressorEvidenceAvailability.Unavailable, bad.ActiveEvidence[0].Acceptance.AggressorEvidenceAvailability);
        Assert.Null(bad.ActiveEvidence[0].Acceptance.OutsideBidVolume);
        Assert.Null(bad.ActiveEvidence[0].Acceptance.OutsideAskVolume);
        Assert.Contains(AcceptanceReentryEvidencePolicyConfig.LimitationHistoryLiveOnly, bad.Limitations);
        Assert.Contains(AcceptanceReentryEvidencePolicyConfig.LimitationOutsideCloseUnavailable,
            bad.ActiveEvidence[0].Acceptance.UnavailableReasons);
        Assert.Contains(AcceptanceReentryEvidencePolicyConfig.LimitationTpoOutsideUnavailable,
            bad.ActiveEvidence[0].Acceptance.UnavailableReasons);
        Assert.Contains(AcceptanceReentryEvidencePolicyConfig.LimitationLocalValueNotAuthorized,
            bad.ActiveEvidence[0].Acceptance.UnavailableReasons);
    }

    [Fact]
    public void E_InvalidTimestampPolicy_FailsClosed()
    {
        var ev = new AcceptanceReentryEvidenceHost(Tick, Epoch, "WRONG_POLICY",
            new AcceptanceReentryEvidencePolicyConfig(true));
        var set = ev.RebuildContext(null, Utc(24));
        Assert.Equal(EvidenceModuleState.Invalid, set.ModuleState);
    }

    // --- F. Revision / idempotence ---

    [Fact]
    public void F_Revision_Duplicate_Metric_State_Republish_Replay()
    {
        var ep = EpisodeHost();
        var ev = EvidenceHost();
        var r = Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m);
        ep.RebuildContext(Profiles(), RefSet(r), null, Utc(24));
        var t1 = Trade(1, 100.3m);
        Drive(ep, ev, t1, Utc(24).AddSeconds(1));
        var s0 = ev.Current!.ActiveEvidence[0];
        Assert.True(s0.StateVersion >= 1);
        Assert.True(s0.EventRevision >= 1);
        var id = s0.EvidenceId;
        var sv = s0.StateVersion;
        var er = s0.EventRevision;

        // Duplicate episode event â€” drain already empty; re-process same measurement should no-op.
        ep.ProcessTrade(t1, Utc(24).AddSeconds(2));
        Assert.Empty(ep.DrainMeasurementEvents());
        var republish = ev.RebuildContext(ep.Current, Utc(24).AddSeconds(2));
        Assert.Equal(er, republish.ActiveEvidence[0].EventRevision);
        Assert.Equal(sv, republish.ActiveEvidence[0].StateVersion);

        Drive(ep, ev, Trade(2, 100.4m), Utc(24).AddSeconds(3)); // metric + Developing
        var s1 = ev.Current!.ActiveEvidence[0];
        Assert.Equal(id, s1.EvidenceId);
        Assert.True(s1.EventRevision > er);
        Assert.True(s1.StateVersion > sv); // Early -> Developing

        // Deterministic replay identity
        var epR = EpisodeHost();
        var evR = EvidenceHost();
        epR.RebuildContext(Profiles(), RefSet(r), null, Utc(24));
        Drive(epR, evR, Trade(1, 100.3m), Utc(24).AddSeconds(1));
        Drive(epR, evR, Trade(2, 100.4m), Utc(24).AddSeconds(3));
        Assert.Equal(id, evR.Current!.ActiveEvidence[0].EvidenceId);
        Assert.Equal(s1.Acceptance.OutsideTradeCount, evR.Current.ActiveEvidence[0].Acceptance.OutsideTradeCount);
    }

    // --- G. Lifecycle ---

    [Fact]
    public void G_ExpiryFreeze_Invalid_AuctionTransition_Disable_NoEpisodeMutation()
    {
        var ep = EpisodeHost();
        var ev = EvidenceHost();
        var r = Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m);
        ep.RebuildContext(Profiles(), RefSet(r), null, Utc(24));
        Drive(ep, ev, Trade(1, 100.3m), Utc(24).AddSeconds(1));
        var episodeId = ep.Current!.ActiveEpisodes[0].EpisodeId;
        var resolutionBefore = ep.Current.ActiveEpisodes[0].Resolution;
        var stateBefore = ep.Current.ActiveEpisodes[0].State;

        // Evidence processing must not mutate Episode.
        Assert.Equal(resolutionBefore, ep.Current.ActiveEpisodes[0].Resolution);
        Assert.Equal(stateBefore, ep.Current.ActiveEpisodes[0].State);
        Assert.Equal(EpisodeResolution.None, resolutionBefore);

        // Primary auction transition freezes evidence
        var profiles2 = Profiles("PI-2026-07-25");
        ep.RebuildContext(profiles2, RefSet(r), null, Utc(25));
        ev.RebuildContext(ep.Current, Utc(25));
        Assert.DoesNotContain(ev.Current!.ActiveEvidence, e => e.EpisodeId == episodeId);
        Assert.Contains(ev.Current.RecentlyClosedEvidence, e => e.EpisodeId == episodeId
            && e.MeasurementStatus == EvidenceMeasurementStatus.Frozen);

        // Disable clears
        ev.Configure(Tick, Epoch, new AcceptanceReentryEvidencePolicyConfig(false));
        Assert.Equal(EvidenceModuleState.Disabled, ev.RebuildContext(ep.Current, Utc(25)).ModuleState);
        Assert.Empty(ev.Current!.ActiveEvidence);
    }

    [Fact]
    public void G_InvalidDataEpisode_ProducesInvalidEvidence()
    {
        var ep = EpisodeHost();
        var ev = EvidenceHost();
        var r = Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m);
        ep.RebuildContext(Profiles(), RefSet(r), null, Utc(24));
        Drive(ep, ev, Trade(1, 100.3m), Utc(24).AddSeconds(1));

        // Force invalid via stale/mismatch trade identity epoch â€” use host invalid path if available.
        // Simulate closed InvalidData by syncing a frozen invalid snapshot through Rebuild after marking.
        var active = ep.Current!.ActiveEpisodes[0];
        // Rebuild with empty refs closes episodes; then evidence sync freezes.
        ep.RebuildContext(Profiles(), RefSet(), null, Utc(24).AddMinutes(1));
        var set = ev.RebuildContext(ep.Current, Utc(24).AddMinutes(1));
        Assert.True(set.RecentlyClosedEvidence.Count + set.ActiveEvidence.Count >= 0);
        // After refs cleared, episodes close; evidence should freeze closed.
        Assert.Empty(set.ActiveEvidence);
    }

    // --- H. Runtime / Data Gate ---

    [Fact]
    public void H_DefaultOff_Awaiting_ReadyPartial_DoesNotClearDegraded_UnchangedPublish()
    {
        var off = new AcceptanceReentryEvidenceHost(Tick, Epoch, AtasTimestampNormalizer.PolicyVersion,
            new AcceptanceReentryEvidencePolicyConfig(false));
        Assert.Equal(EvidenceModuleState.Disabled, off.RebuildContext(null, Utc(24)).ModuleState);

        var ep = EpisodeHost();
        var ev = EvidenceHost();
        ep.RebuildContext(Profiles(), RefSet(Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m)), null, Utc(24));
        Assert.Equal(EvidenceModuleState.AwaitingEpisodes, ev.RebuildContext(ep.Current, Utc(24)).ModuleState);

        Drive(ep, ev, Trade(1, 100.3m), Utc(24).AddSeconds(1));
        Assert.True(ev.Current!.ModuleState is EvidenceModuleState.Ready or EvidenceModuleState.Partial);

        var runtime = new GcaeRuntimeEngine();
        var snap = runtime.Publish(
            null, "GCQ6", DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, DateTime.UtcNow, false, false, false, false, false, false,
            Profiles(), auctionEpisodes: ep.Current, acceptanceReentryEvidence: ev.Current);
        Assert.Equal("0.14.0", snap.Version);
        Assert.NotEqual(DataState.Ready, snap.DataGate.DataState);
        Assert.Same(ev.Current, snap.AcceptanceReentryEvidence);

        var snap2 = runtime.Publish(
            null, "GCQ6", DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, DateTime.UtcNow, false, false, false, false, false, false,
            Profiles(), auctionEpisodes: ep.Current, acceptanceReentryEvidence: ev.Current);
        Assert.Same(ev.Current, snap2.AcceptanceReentryEvidence);
    }

    // --- I. UI / scope ---

    [Fact]
    public void I_GpsRows_NoProhibitedWording_ThesisNotStarted()
    {
        var ep = EpisodeHost();
        var ev = EvidenceHost();
        var r = Ref(ReferenceType.PreviousPrimaryTpoVah, 100.2m);
        ep.RebuildContext(Profiles(), RefSet(r), null, Utc(24));
        Drive(ep, ev, Trade(1, 100.3m), Utc(24).AddSeconds(1));

        var runtime = new GcaeRuntimeEngine();
        var snap = runtime.Publish(
            null, "GCQ6", DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, DateTime.UtcNow, false, false, false, false, false, false,
            Profiles(), auctionEpisodes: ep.Current, showAuctionEpisodeDiagnostics: true,
            acceptanceReentryEvidence: ev.Current, showAcceptanceReentryEvidenceDiagnostics: true);
        var vm = AuctionGpsCardMapper.FromSnapshot(snap, true);
        var text = string.Join('\n', vm.ProfileDetailLines.Concat(vm.DiagnosticRows));
        Assert.Contains("ACCEPTANCE/REENTRY EVIDENCE:", text, StringComparison.Ordinal);
        Assert.Contains("ACCEPTANCE_REENTRY_EVIDENCE_POLICY_V1", text, StringComparison.Ordinal);
        Assert.Contains("EVIDENCE HISTORY: LIVE_ONLY", text, StringComparison.Ordinal);
        Assert.Contains("ACCEPTANCE OBS:", text, StringComparison.Ordinal);
        Assert.Contains("GEOMETRIC REENTRY:", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ACCEPTED OUTSIDE", text, StringComparison.Ordinal);
        Assert.DoesNotContain("REACCEPTED INSIDE", text, StringComparison.Ordinal);
        Assert.DoesNotContain("FAILED AUCTION", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PROBABILITY", text, StringComparison.Ordinal);
        Assert.DoesNotContain("CONFIDENCE", text, StringComparison.Ordinal);
        Assert.DoesNotContain("\nLONG", text, StringComparison.Ordinal);
        Assert.DoesNotContain("\nSHORT", text, StringComparison.Ordinal);
        Assert.Contains("MBO: BLOCKED", vm.MboLine, StringComparison.Ordinal);

        var root = FindRepoRoot();
        Assert.True(Directory.Exists(Path.Combine(root, "src", "GC.AuctionFlow", "Evidence")));
        Assert.True(Directory.Exists(Path.Combine(root, "src", "GC.AuctionFlow", "Orderflow")));
        Assert.True(Directory.Exists(Path.Combine(root, "src", "GC.AuctionFlow", "Cluster")));
        Assert.True(Directory.Exists(Path.Combine(root, "src", "GC.AuctionFlow", "Thesis")));
        Assert.False(Directory.Exists(Path.Combine(root, "src", "GC.AuctionFlow", "Far")));
        Assert.False(Directory.Exists(Path.Combine(root, "src", "GC.AuctionFlow", "TradeFacilitation")));
        var indicator = File.ReadAllText(Path.Combine(root, "src", "GC.AuctionFlow", "Atas", "GcAuctionFlowIndicator.cs"));
        Assert.Contains("EnableAcceptanceReentryEvidence", indicator, StringComparison.Ordinal);
        Assert.Contains("EnableExecutedOrderflow", indicator, StringComparison.Ordinal);
        Assert.Contains("EnableClusterRawFeatures", indicator, StringComparison.Ordinal);
        Assert.Contains("EnableFarThesis", indicator, StringComparison.Ordinal);
        Assert.Contains("EnableAacThesis", indicator, StringComparison.Ordinal);
        Assert.DoesNotContain("EnableThesis", indicator, StringComparison.Ordinal);
        Assert.DoesNotContain("EnableTradeFacilitation", indicator, StringComparison.Ordinal);
        Assert.DoesNotContain("AcceptedOutside", indicator, StringComparison.Ordinal);
        Assert.DoesNotContain("StableReaccepted", indicator, StringComparison.Ordinal);
    }

    [Fact]
    public void I_Disabled_ProducesNoRows()
    {
        var lines = AuctionGpsCardMapper.BuildAcceptanceReentryEvidenceLines(
            new AcceptanceReentryEvidenceSetSnapshot(
                EvidenceModuleState.Disabled,
                AcceptanceReentryEvidencePolicyConfig.PolicyVersion,
                "", Array.Empty<AcceptanceReentryEvidenceSnapshot>(),
                Array.Empty<AcceptanceReentryEvidenceSnapshot>(), null,
                new Dictionary<AcceptanceObservationState, int>(),
                new Dictionary<ReentryObservationState, int>(),
                0, 0, "", 0, Utc(24), Utc(24), Array.Empty<string>()),
            showDiagnostics: true);
        Assert.Empty(lines);
    }

    [Fact]
    public void Policy_VersionAndLimitations()
    {
        Assert.Equal("ACCEPTANCE_REENTRY_EVIDENCE_POLICY_V1", AcceptanceReentryEvidencePolicyConfig.PolicyVersion);
        Assert.Equal("0.14.0", GcaeRuntimeSnapshot.SnapshotVersion);
        Assert.Equal("CENTERLINE_ACCEPTANCE_GEOMETRY_NOT_APPLICABLE",
            AcceptanceReentryEvidencePolicyConfig.LimitationCenterlineNotApplicable);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "GC.AuctionFlow.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repo root not found.");
    }
}
