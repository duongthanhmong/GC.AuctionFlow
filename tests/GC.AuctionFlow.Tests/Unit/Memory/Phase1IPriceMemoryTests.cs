using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Evidence;
using GC.AuctionFlow.Memory;
using GC.AuctionFlow.Reference;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Memory;

/// <summary>
/// Phase 1I Price Memory / Retest Ledger — v1.3 §13, KDK Ch 27.
///
/// Records how often each reference has been tested and how each test ended.
/// The central invariant is G-REF-001: there is NO one-directional rule that a level
/// tested many times becomes weaker, or stronger. Liquidity can be replenished between
/// tests, so a raw count carries no directional meaning.
/// </summary>
public sealed class Phase1IPriceMemoryTests
{
    /// <summary>Offset in seconds from a fixed base. Uses AddSeconds so tests may
    /// exceed 59 without constructing an invalid DateTime.</summary>
    private static DateTime Utc(int sec = 0) =>
        new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc).AddSeconds(sec);

    // ---------- helpers ----------

    private static AuctionEpisodeSnapshot Episode(
        string episodeId,
        string referenceId,
        EpisodeState state = EpisodeState.Interacting,
        EpisodeResolution resolution = EpisodeResolution.None,
        int attemptCount = 0,
        int startSec = 0,
        int updatedSec = 1) =>
        new(
            episodeId: episodeId,
            policyVersion: EpisodePolicyConfig.PolicyVersion,
            primaryAuctionId: "PI-1",
            referenceId: referenceId,
            referenceType: ReferenceType.PreviousPrimaryTpoPoc,
            referenceRole: ReferenceInteractionRole.UpperBoundary,
            referencePriceTick: 24000L,
            referencePrice: 2400.0m,
            referenceMaturity: ReferenceMaturity.Confirmed,
            sourceHorizon: ReferenceSourceHorizon.PreviousPrimaryAuction,
            directionalContextProvenance: null,
            interactionDirection: EpisodeInteractionDirection.Unknown,
            state: state,
            resolution: resolution,
            startedAtUtc: Utc(startSec),
            lastUpdatedAtUtc: Utc(updatedSec),
            firstInteractionEventId: "EV-A",
            lastProcessedEventId: "EV-B",
            attemptCount: attemptCount,
            interactionCount: 1,
            crossCount: 0,
            upExcursionCount: 0,
            downExcursionCount: 0,
            maximumAboveDistanceTicks: 0L,
            maximumBelowDistanceTicks: 0L,
            maximumCanonicalOutsideDistanceTicks: null,
            canonicalOutsideDuration: TimeSpan.Zero,
            canonicalOutsideExecutedVolume: 0m,
            canonicalOutsideTradeCount: 0,
            canonicalOutsideBidVolume: null,
            canonicalOutsideAskVolume: null,
            canonicalOutsideDelta: null,
            aggressorEvidenceAvailability: AggressorEvidenceAvailability.Unavailable,
            localPoc: null,
            stateVersion: 1L,
            eventRevision: 1L,
            dataQuality: EpisodeDataQuality.Complete,
            limitations: Array.Empty<string>());

    private static AuctionEpisodeSetSnapshot Set(
        EpisodeModuleState state = EpisodeModuleState.Ready,
        AuctionEpisodeSnapshot[]? active = null,
        AuctionEpisodeSnapshot[]? closed = null) =>
        new(
            moduleState: state,
            policyVersion: EpisodePolicyConfig.PolicyVersion,
            historyMode: EpisodeHistoryMode.LiveOnly,
            primaryAuctionId: "PI-1",
            eligibleReferenceCount: 1,
            interactedReferenceCount: 1,
            activeEpisodes: active ?? Array.Empty<AuctionEpisodeSnapshot>(),
            recentlyClosedEpisodes: closed ?? Array.Empty<AuctionEpisodeSnapshot>(),
            latestUpdatedEpisode: null,
            episodeCountsByState: new Dictionary<EpisodeState, int>(),
            inputFingerprint: "fp-1i",
            registryRevision: 1L,
            createdAtUtc: Utc(),
            lastUpdatedAtUtc: Utc(1),
            limitations: Array.Empty<string>());

    private static PriceMemoryHost EnabledHost() =>
        new(new PriceMemoryPolicyConfig(enabled: true));

    // ========== A: Policy + enums ==========

    [Fact]
    public void A01_PolicyVersion_is_price_memory_v1() =>
        Assert.Equal("PRICE_MEMORY_POLICY_V1", PriceMemoryPolicyConfig.PolicyVersion);

    [Fact]
    public void A02_Module_default_is_disabled() =>
        Assert.False(new PriceMemoryPolicyConfig().Enabled);

    [Fact]
    public void A03_Strength_verdicts_are_reserved()
    {
        Assert.Equal(1, (int)ReferenceStrengthState.NotCalibrated);
        Assert.True((int)ReferenceStrengthState.Strengthening >= 100);
        Assert.True((int)ReferenceStrengthState.Weakening >= 100);
        Assert.True((int)ReferenceStrengthState.Stable >= 100);
    }

    [Fact]
    public void A04_Held_and_broken_outcomes_are_reserved()
    {
        Assert.True((int)ReferenceTestOutcome.HeldRejected >= 100);
        Assert.True((int)ReferenceTestOutcome.Broken >= 100);
    }

    [Fact]
    public void A05_Replenishment_default_is_unavailable() =>
        Assert.Equal(LiquidityReplenishmentObservability.Unavailable,
            default(LiquidityReplenishmentObservability));

    [Fact]
    public void A06_Limitation_constants_are_stable()
    {
        Assert.Equal("REFERENCE_STRENGTH_NOT_CALIBRATED", PriceMemoryPolicyConfig.LimitationStrengthNotCalibrated);
        Assert.Equal("NO_ONE_DIRECTIONAL_REFERENCE_DECAY_RULE", PriceMemoryPolicyConfig.LimitationNoDirectionalDecayRule);
        Assert.Equal("LIQUIDITY_REPLENISHMENT_UNOBSERVABLE_MBO_BLOCKED", PriceMemoryPolicyConfig.LimitationReplenishmentUnavailable);
        Assert.Equal("PRICE_MEMORY_STARTS_AT_INDICATOR_START", PriceMemoryPolicyConfig.LimitationMemoryStartsAtIndicatorStart);
    }

    // ========== B: G-REF-001 — count without inference ==========

    /// <summary>
    /// The core guard. A reference tested once and one tested fifty times must report
    /// the SAME strength state, because KDK Ch 27 says liquidity can be replenished
    /// between tests and no one-directional rule exists.
    /// </summary>
    [Fact]
    public void B01_Test_count_never_changes_the_strength_state()
    {
        var host = EnabledHost();
        for (var i = 0; i < 50; i++)
            host.Rebuild(Set(active: new[] { Episode("EP-" + i, "REF-1") }), Utc(i));

        var mem = host.Current!.ForReference("REF-1")!;
        Assert.Equal(50, mem.TestCount);
        Assert.Equal(ReferenceStrengthState.NotCalibrated, mem.StrengthState);
    }

    [Fact]
    public void B02_Single_test_reports_the_same_strength_state()
    {
        var host = EnabledHost();
        host.Rebuild(Set(active: new[] { Episode("EP-1", "REF-1") }), Utc());
        Assert.Equal(ReferenceStrengthState.NotCalibrated,
            host.Current!.ForReference("REF-1")!.StrengthState);
    }

    [Fact]
    public void B03_No_strength_verdict_is_ever_emitted()
    {
        var host = EnabledHost();
        foreach (var st in Enum.GetValues<EpisodeState>())
            host.Rebuild(Set(active: new[] { Episode("EP-" + st, "REF-" + st, st) }), Utc());

        Assert.All(host.Current!.References,
            r => Assert.True((int)r.StrengthState < 100, r.ReferenceId + " emitted a verdict"));
    }

    [Fact]
    public void B04_Decay_rule_is_explicitly_disclaimed()
    {
        var host = EnabledHost();
        host.Rebuild(Set(active: new[] { Episode("EP-1", "REF-1") }), Utc());
        Assert.Contains(PriceMemoryPolicyConfig.LimitationNoDirectionalDecayRule,
            host.Current!.Limitations);
        Assert.Contains(PriceMemoryPolicyConfig.LimitationNoDirectionalDecayRule,
            host.Current!.ForReference("REF-1")!.Limitations);
    }

    // ========== C: First test vs retest ==========

    [Fact]
    public void C01_First_test_has_no_retests()
    {
        var host = EnabledHost();
        host.Rebuild(Set(active: new[] { Episode("EP-1", "REF-1") }), Utc());
        var mem = host.Current!.ForReference("REF-1")!;
        Assert.Equal(1, mem.TestCount);
        Assert.Equal(0, mem.RetestCount);
        Assert.False(mem.HasBeenRetested);
    }

    [Fact]
    public void C02_Second_episode_is_a_retest()
    {
        var host = EnabledHost();
        host.Rebuild(Set(active: new[] { Episode("EP-1", "REF-1") }), Utc());
        host.Rebuild(Set(active: new[] { Episode("EP-2", "REF-1", startSec: 10, updatedSec: 11) }), Utc(11));

        var mem = host.Current!.ForReference("REF-1")!;
        Assert.Equal(2, mem.TestCount);
        Assert.Equal(1, mem.RetestCount);
        Assert.True(mem.HasBeenRetested);
    }

    [Fact]
    public void C03_Retest_count_is_never_negative()
    {
        var host = EnabledHost();
        host.Rebuild(Set(), Utc());
        Assert.All(host.Current!.References, r => Assert.True(r.RetestCount >= 0));
    }

    /// <summary>
    /// The same episode is folded repeatedly as it evolves. One test must stay one test.
    /// </summary>
    [Fact]
    public void C04_Refolding_the_same_episode_does_not_inflate_the_count()
    {
        var host = EnabledHost();
        for (var i = 0; i < 10; i++)
            host.Rebuild(Set(active: new[] { Episode("EP-1", "REF-1", updatedSec: i) }), Utc(i));

        Assert.Equal(1, host.Current!.ForReference("REF-1")!.TestCount);
    }

    [Fact]
    public void C05_Refolding_updates_the_record_in_place()
    {
        var host = EnabledHost();
        host.Rebuild(Set(active: new[] { Episode("EP-1", "REF-1", EpisodeState.Interacting) }), Utc());
        host.Rebuild(Set(active: new[] {
            Episode("EP-1", "REF-1", EpisodeState.OutsideAttempt, attemptCount: 3, updatedSec: 5) }), Utc(5));

        var mem = host.Current!.ForReference("REF-1")!;
        Assert.Single(mem.Records);
        Assert.Equal(3, mem.Records[0].AttemptCount);
        Assert.Equal(EpisodeState.OutsideAttempt, mem.Records[0].FinalState);
    }

    [Fact]
    public void C06_First_and_last_test_timestamps_are_tracked()
    {
        var host = EnabledHost();
        host.Rebuild(Set(active: new[] { Episode("EP-1", "REF-1", startSec: 5, updatedSec: 6) }), Utc(6));
        host.Rebuild(Set(active: new[] { Episode("EP-2", "REF-1", startSec: 1, updatedSec: 30) }), Utc(30));

        var mem = host.Current!.ForReference("REF-1")!;
        Assert.Equal(Utc(1), mem.FirstTestAtUtc);
        Assert.Equal(Utc(30), mem.LastTestAtUtc);
    }

    [Fact]
    public void C07_Different_references_have_separate_ledgers()
    {
        var host = EnabledHost();
        host.Rebuild(Set(active: new[] {
            Episode("EP-1", "REF-1"),
            Episode("EP-2", "REF-2") }), Utc());

        Assert.Equal(2, host.Current!.TrackedReferenceCount);
        Assert.Equal(1, host.Current!.ForReference("REF-1")!.TestCount);
        Assert.Equal(1, host.Current!.ForReference("REF-2")!.TestCount);
    }

    // ========== D: Outcomes ==========

    [Theory]
    [InlineData(EpisodeState.Interacting, ReferenceTestOutcome.InProgress)]
    [InlineData(EpisodeState.OutsideAttempt, ReferenceTestOutcome.InProgress)]
    [InlineData(EpisodeState.Developing, ReferenceTestOutcome.InProgress)]
    [InlineData(EpisodeState.ReentryDeveloping, ReferenceTestOutcome.InProgress)]
    [InlineData(EpisodeState.EpisodeExpired, ReferenceTestOutcome.Expired)]
    [InlineData(EpisodeState.InvalidData, ReferenceTestOutcome.InvalidData)]
    public void D01_Episode_state_maps_to_outcome(EpisodeState st, ReferenceTestOutcome expected)
    {
        var host = EnabledHost();
        host.Rebuild(Set(active: new[] { Episode("EP-1", "REF-1", st) }), Utc());
        Assert.Equal(expected, host.Current!.ForReference("REF-1")!.Records[0].Outcome);
    }

    [Fact]
    public void D02_Reserved_episode_state_yields_not_calibrated()
    {
        var host = EnabledHost();
        host.Rebuild(Set(active: new[] {
            Episode("EP-1", "REF-1", EpisodeState.AcceptanceOutside) }), Utc());
        Assert.Equal(ReferenceTestOutcome.NotCalibrated,
            host.Current!.ForReference("REF-1")!.Records[0].Outcome);
    }

    [Fact]
    public void D03_Held_or_broken_is_never_emitted()
    {
        var host = EnabledHost();
        foreach (var st in Enum.GetValues<EpisodeState>())
        foreach (var res in Enum.GetValues<EpisodeResolution>())
            host.Rebuild(Set(active: new[] {
                Episode("EP-" + st + res, "REF-" + st + res, st, res) }), Utc());

        foreach (var r in host.Current!.References)
        foreach (var rec in r.Records)
            Assert.True((int)rec.Outcome < 100, "emitted a calibrated outcome: " + rec.Outcome);
    }

    [Fact]
    public void D04_In_progress_test_is_not_closed()
    {
        var host = EnabledHost();
        host.Rebuild(Set(active: new[] { Episode("EP-1", "REF-1", EpisodeState.Interacting) }), Utc());
        var mem = host.Current!.ForReference("REF-1")!;
        Assert.False(mem.Records[0].IsClosed);
        Assert.Equal(0, mem.ClosedTestCount);
    }

    // ========== E: Liquidity replenishment stays unobservable ==========

    /// <summary>
    /// KDK Ch 27 names replenishment as the reason a repeat test is not automatically
    /// weaker. It is Tier-3 data and MBO is BLOCKED, so it must never be inferred.
    /// </summary>
    [Fact]
    public void E01_Replenishment_is_always_unavailable()
    {
        var host = EnabledHost();
        for (var i = 0; i < 5; i++)
            host.Rebuild(Set(active: new[] { Episode("EP-" + i, "REF-1") }), Utc(i));

        Assert.Equal(LiquidityReplenishmentObservability.Unavailable,
            host.Current!.ForReference("REF-1")!.Replenishment);
        Assert.Contains(PriceMemoryPolicyConfig.LimitationReplenishmentUnavailable,
            host.Current!.Limitations);
    }

    // ========== F: LIVE_ONLY window ==========

    [Fact]
    public void F01_Memory_window_start_is_recorded()
    {
        var host = EnabledHost();
        host.Rebuild(Set(active: new[] { Episode("EP-1", "REF-1") }), Utc(7));
        Assert.Equal(Utc(7), host.Current!.MemoryStartedAtUtc);
    }

    [Fact]
    public void F02_Live_only_window_is_disclaimed()
    {
        var host = EnabledHost();
        host.Rebuild(Set(active: new[] { Episode("EP-1", "REF-1") }), Utc());
        Assert.Contains(PriceMemoryPolicyConfig.LimitationLiveOnly, host.Current!.Limitations);
        Assert.Contains(PriceMemoryPolicyConfig.LimitationMemoryStartsAtIndicatorStart,
            host.Current!.Limitations);
    }

    // ========== G: Host lifecycle ==========

    [Fact]
    public void G01_Disabled_host_publishes_disabled()
    {
        var host = new PriceMemoryHost(new PriceMemoryPolicyConfig(enabled: false));
        var set = host.Rebuild(Set(active: new[] { Episode("EP-1", "REF-1") }), Utc());
        Assert.Equal(MemoryModuleState.Disabled, set.ModuleState);
        Assert.Empty(set.References);
    }

    [Fact]
    public void G02_Configure_null_throws() =>
        Assert.Throws<ArgumentNullException>(() => EnabledHost().Configure(null!));

    [Fact]
    public void G03_Reset_clears_the_ledger()
    {
        var host = EnabledHost();
        host.Rebuild(Set(active: new[] { Episode("EP-1", "REF-1") }), Utc());
        host.Reset();
        Assert.Null(host.Current);
    }

    /// <summary>Memory that forgets between rebuilds is not memory.</summary>
    [Fact]
    public void G04_Ledger_survives_an_empty_rebuild()
    {
        var host = EnabledHost();
        host.Rebuild(Set(active: new[] { Episode("EP-1", "REF-1") }), Utc());
        host.Rebuild(Set(), Utc(5));

        Assert.Equal(1, host.Current!.ForReference("REF-1")!.TestCount);
    }

    [Fact]
    public void G05_Null_episodes_yield_awaiting()
    {
        var set = EnabledHost().Rebuild(null, Utc());
        Assert.Equal(MemoryModuleState.AwaitingEpisodes, set.ModuleState);
    }

    [Fact]
    public void G06_Invalid_episodes_yield_invalid()
    {
        var set = EnabledHost().Rebuild(Set(EpisodeModuleState.Invalid), Utc());
        Assert.Equal(MemoryModuleState.Invalid, set.ModuleState);
        Assert.Contains("EPISODE_INPUT_INVALID", set.Limitations);
    }

    [Fact]
    public void G07_Closed_episodes_are_folded_too()
    {
        var host = EnabledHost();
        host.Rebuild(Set(closed: new[] {
            Episode("EP-1", "REF-1", EpisodeState.EpisodeExpired) }), Utc());
        Assert.Equal(1, host.Current!.ForReference("REF-1")!.TestCount);
    }

    [Fact]
    public void G08_Record_capacity_is_enforced()
    {
        var host = EnabledHost();
        var over = PriceMemoryPolicyConfig.TestRecordCapacity + 10;
        for (var i = 0; i < over; i++)
            host.Rebuild(Set(active: new[] { Episode("EP-" + i, "REF-1") }), Utc(i));

        var mem = host.Current!.ForReference("REF-1")!;

        // Records are capped...
        Assert.Equal(PriceMemoryPolicyConfig.TestRecordCapacity, mem.Records.Count);

        // ...but the count is not. Truncating storage must never rewrite history:
        // a reference tested 74 times that reports 64 is a quiet lie.
        Assert.Equal(over, mem.TestCount);
        Assert.Equal(over - 1, mem.RetestCount);
    }

    [Fact]
    public void G09_Episodes_without_a_reference_are_ignored()
    {
        var host = EnabledHost();
        host.Rebuild(Set(active: new[] { Episode("EP-1", "") }), Utc());
        Assert.Equal(0, host.Current!.TrackedReferenceCount);
    }

    // ========== H: GPS card ==========

    [Fact]
    public void H01_MemoryLines_null_is_empty() =>
        Assert.Empty(AuctionGpsCardMapper.BuildPriceMemoryLines(null, false));

    [Fact]
    public void H02_MemoryLines_disabled_is_single_row()
    {
        var host = new PriceMemoryHost(new PriceMemoryPolicyConfig(enabled: false));
        var rows = AuctionGpsCardMapper.BuildPriceMemoryLines(host.Rebuild(null, Utc()), false);
        Assert.Single(rows);
        Assert.Equal("MEMORY: DISABLED", rows[0]);
    }

    [Fact]
    public void H03_MemoryLines_declare_strength_not_calibrated()
    {
        var host = EnabledHost();
        host.Rebuild(Set(active: new[] { Episode("EP-1", "REF-1") }), Utc());
        var rows = AuctionGpsCardMapper.BuildPriceMemoryLines(host.Current, false);
        Assert.Contains("REFERENCE STRENGTH: NOT CALIBRATED", rows);
        Assert.Contains("LIQUIDITY REPLENISHMENT: UNOBSERVABLE (MBO BLOCKED)", rows);
    }

    [Fact]
    public void H04_MemoryLines_show_counts()
    {
        var host = EnabledHost();
        host.Rebuild(Set(active: new[] { Episode("EP-1", "REF-1") }), Utc());
        host.Rebuild(Set(active: new[] { Episode("EP-2", "REF-1", startSec: 9, updatedSec: 10) }), Utc(10));
        var rows = AuctionGpsCardMapper.BuildPriceMemoryLines(host.Current, false);
        Assert.Contains("TESTS OBSERVED: 2 (1 RETESTED)", rows);
        Assert.Contains("LAST REF TESTS: 2 (1 RETESTS)", rows);
    }

    [Fact]
    public void H05_MemoryLines_never_claim_a_reference_is_weak_or_strong()
    {
        var host = EnabledHost();
        for (var i = 0; i < 20; i++)
            host.Rebuild(Set(active: new[] { Episode("EP-" + i, "REF-1") }), Utc(i));

        var text = string.Join(" | ", AuctionGpsCardMapper.BuildPriceMemoryLines(host.Current, true));
        Assert.DoesNotContain("WEAKENING", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("STRENGTHENING", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EXHAUSTED", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void H06_Diagnostics_disclose_the_live_only_window()
    {
        var host = EnabledHost();
        host.Rebuild(Set(active: new[] { Episode("EP-1", "REF-1") }), Utc());
        var rows = AuctionGpsCardMapper.BuildPriceMemoryLines(host.Current, true);
        Assert.Contains("MEMORY WINDOW: LIVE ONLY (PRE-START TESTS UNKNOWN)", rows);
    }
}
