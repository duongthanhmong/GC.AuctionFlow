using System;
using System.IO;
using System.Linq;
using GC.AuctionFlow.Foundation;
using GC.AuctionFlow.Orderflow;
using GC.AuctionFlow.Recorder;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Foundation;

/// <summary>
/// M1 deterministic input-foundation tests. Each maps to a KDK/MRBS/TTS requirement. Fixtures are
/// small point-in-time event streams built in code — no external data, no candle-to-trade conflation.
/// Synthetic ApprovedForProduction config/template are used only to exercise the Ready path; they are
/// not production approval.
/// </summary>
public sealed class InputFoundationTests
{
    private static readonly VersionStamp V = new("1.0.0", "1.0.0", "1.0.0");
    private static readonly DateTime T0 = new(2026, 7, 30, 14, 0, 0, DateTimeKind.Utc);

    private static FoundationConfig ApprovedConfig() =>
        new("1.0.0", FoundationConfig.DefaultProposed("1.0.0")
            .Select(p => p with { ApprovalStatus = ParameterApprovalStatus.ApprovedForProduction }).ToArray());

    private static SessionTemplate ApprovedTemplate() => new(
        "GC-RTH-ETH-TEST", "1.0.0", "America/New_York", "ETH", "RTH", "2026.1",
        SessionTemplateStatus.ApprovedForProduction, "synthetic test template — not production approval");

    private static CanonicalInputEvent Trade(
        long priceTicks, long qty, AggressorSide side, DateTime eventUtc,
        DedupCapability dedup = DedupCapability.NativeStableId, string? stableKey = null,
        long callbackOrder = 0, DataPhase phase = DataPhase.Live) =>
        new("GC", "GCZ6", priceTicks, 0.1m, qty, side, InputEventKind.Trade,
            eventUtc, eventUtc, SourceTimeKind.DeclaredUtc, phase, "rithmic-via-atas",
            stableKey ?? $"id{priceTicks}-{qty}-{eventUtc.Ticks}", dedup, callbackOrder, V);

    private static InputFoundationHost ReadyHost()
    {
        var h = new InputFoundationHost(V, ApprovedConfig(), ApprovedTemplate());
        h.Attach();
        h.SetContract("GCZ6", 0.1m, "test");
        h.NoteLiveTradeCapability(CapabilityAvailability.Available);
        h.NoteHistoricalTradeCapability(CapabilityAvailability.Available);
        h.NoteRequiredModuleReadiness(ModuleReadiness.Ready);
        return h;
    }

    // 1. Deterministic price-to-tick conversion and invalid tick-grid input.
    [Fact]
    public void T01_PriceToTick_deterministic_and_invalid_tick_rejected()
    {
        Assert.Equal(41133L, PriceTickMath.ToTicks(4113.3m, 0.1m));
        Assert.Equal(-5L, PriceTickMath.ToTicks(-0.05m, 0.01m)); // half away from zero, sign-symmetric
        Assert.False(PriceTickMath.IsValidTickSize(0m));
        Assert.False(PriceTickMath.IsValidTickSize(-0.1m));
        Assert.Throws<ArgumentOutOfRangeException>(() => PriceTickMath.ToTicks(1m, 0m));
    }

    // 2. Native aggressor mapping and preservation of Unknown.
    [Fact]
    public void T02_aggressor_native_and_unknown_preserved()
    {
        var buy = Trade(41133, 1, AggressorSide.Ask, T0);
        var unk = Trade(41133, 1, AggressorSide.Unknown, T0.AddSeconds(1));
        Assert.Equal(AggressorSide.Ask, buy.Aggressor);
        Assert.Equal(AggressorSide.Unknown, unk.Aggressor);
        var h = ReadyHost();
        h.Ingest(buy); h.Ingest(unk);
        var (snap, _) = h.Evaluate();
        Assert.Equal(1, snap.ClassifiedCount);
        Assert.Equal(1, snap.UnknownAggressorCount);
    }

    // 3. Canonical event version/provenance fields present.
    [Fact]
    public void T03_canonical_event_carries_versions_and_provenance()
    {
        var e = Trade(41133, 1, AggressorSide.Ask, T0);
        Assert.True(e.Versions.IsComplete);
        Assert.Equal(SourceTimeKind.DeclaredUtc, e.TimeProvenance);
        Assert.Equal(DataPhase.Live, e.Phase);
        Assert.Equal("rithmic-via-atas", e.SourceId);
        Assert.StartsWith("nid:", e.IdentityKey());
    }

    // 4. Deterministic ordering when event times are equal.
    [Fact]
    public void T04_equal_event_time_is_not_out_of_order_and_tiebreak_is_total()
    {
        var h = ReadyHost();
        var a = Trade(41133, 1, AggressorSide.Ask, T0, callbackOrder: 1);
        var b = Trade(41134, 1, AggressorSide.Bid, T0, callbackOrder: 2); // same time, different price
        h.Ingest(a); h.Ingest(b);
        var (snap, _) = h.Evaluate();
        Assert.Equal(0, snap.OutOfOrderCount);
        Assert.NotEqual(InputIntegrityMeter.OrderingKey(a), InputIntegrityMeter.OrderingKey(b));
    }

    // 5. Safe duplicate rejection where stable identity exists.
    [Fact]
    public void T05_native_duplicate_rejected()
    {
        var h = ReadyHost();
        var e = Trade(41133, 1, AggressorSide.Ask, T0, DedupCapability.NativeStableId, "STABLE-1");
        Assert.Equal(InputIntegrityMeter.Verdict.Accepted, h.Ingest(e));
        Assert.Equal(InputIntegrityMeter.Verdict.DuplicateRejected, h.Ingest(e));
        var (snap, _) = h.Evaluate();
        Assert.Equal(1, snap.DuplicateCount);
    }

    // 6. No false deduplication of distinct identical-looking prints (composite key).
    [Fact]
    public void T06_composite_key_does_not_collapse_identical_prints()
    {
        var h = ReadyHost();
        var p1 = Trade(41133, 1, AggressorSide.Ask, T0, DedupCapability.CompositeKey, "x", 1);
        var p2 = Trade(41133, 1, AggressorSide.Ask, T0, DedupCapability.CompositeKey, "x", 2); // legit identical print
        Assert.Equal(InputIntegrityMeter.Verdict.Accepted, h.Ingest(p1));
        Assert.NotEqual(InputIntegrityMeter.Verdict.DuplicateRejected, h.Ingest(p2));
        var (snap, _) = h.Evaluate();
        Assert.Equal(0, snap.DuplicateCount);
    }

    // 7. Explicit blocked capability when safe deduplication is impossible.
    [Fact]
    public void T07_non_deduplicable_blocks_ready_with_reason()
    {
        var h = ReadyHost();
        h.Ingest(Trade(41133, 1, AggressorSide.Ask, T0, DedupCapability.NotDeduplicable));
        var (snap, _) = h.Evaluate();
        Assert.Equal(1, snap.NonDeduplicableCount);
        Assert.NotEqual(LifecycleState.AnalysisReady, snap.LifecycleState);
        Assert.Contains(FoundationReasonCodes.DedupNotPossible, snap.ReasonCodes);
    }

    // 8. Historical/live overlap without double counting.
    [Fact]
    public void T08_overlap_dedup_no_double_count()
    {
        var h = ReadyHost();
        h.Ingest(Trade(41130, 1, AggressorSide.Ask, T0, DedupCapability.NativeStableId, "H1", phase: DataPhase.Historical));
        h.Ingest(Trade(41131, 1, AggressorSide.Ask, T0.AddSeconds(1), DedupCapability.NativeStableId, "L1"));
        h.Ingest(Trade(41130, 1, AggressorSide.Ask, T0, DedupCapability.NativeStableId, "H1")); // overlap of H1
        var (snap, _) = h.Evaluate();
        Assert.Equal(1, snap.DuplicateCount); // the overlapped H1 rejected, not double-counted
    }

    // 9. Reconnect/capture-epoch transition into Recovering.
    [Fact]
    public void T09_reconnect_enters_recovering_new_epoch()
    {
        var h = ReadyHost();
        h.Ingest(Trade(41133, 1, AggressorSide.Ask, T0));
        var epochBefore = h.CaptureEpoch;
        h.NoteReconnect("restore-1");
        var (snap, _) = h.Evaluate();
        Assert.Equal(epochBefore + 1, h.CaptureEpoch);
        Assert.Equal(LifecycleState.Recovering, snap.LifecycleState);
    }

    // 10. Unrecovered gap prevents AnalysisReady.
    [Fact]
    public void T10_unrecovered_gap_blocks_ready()
    {
        var h = ReadyHost();
        h.Ingest(Trade(41133, 1, AggressorSide.Ask, T0));
        h.NoteMeasuredIdentityGap();
        var (snap, _) = h.Evaluate();
        Assert.True(snap.HasUnrecoveredGap);
        Assert.NotEqual(LifecycleState.AnalysisReady, snap.LifecycleState);
        Assert.Contains(FoundationReasonCodes.UnrecoveredGapBlocksReady, snap.ReasonCodes);
    }

    // 11. Lifecycle transition legality.
    [Fact]
    public void T11_lifecycle_transition_legality()
    {
        var sm = new LifecycleStateMachine("1.0.0");
        Assert.Equal(LifecycleState.ColdStart, sm.State);
        Assert.Equal(LifecycleState.HistoricalWarmup, sm.OnAttach());
        Assert.Equal(LifecycleState.AnalysisReady, sm.TryBecomeReady(true, null));
        Assert.Equal(LifecycleState.Recovering, sm.OnReconnect());
        Assert.Equal(LifecycleState.Stopped, sm.OnStop());
        // Terminal cannot self-promote.
        Assert.Equal(LifecycleState.Stopped, sm.TryBecomeReady(true, null));
        Assert.All(sm.Log, t => Assert.False(string.IsNullOrEmpty(t.ReasonCode)));
    }

    // 12. No confirmed publication during warm-up, rebuild, recovery, or invalid state.
    [Fact]
    public void T12_no_confirmed_publish_outside_ready()
    {
        var sm = new LifecycleStateMachine("1.0.0");
        sm.OnAttach(); Assert.False(sm.AllowsConfirmedPublish);            // HistoricalWarmup
        sm.OnRebuild(); Assert.False(sm.AllowsConfirmedPublish);          // Rebuilding
        sm.OnReconnect(); Assert.False(sm.AllowsConfirmedPublish);        // Recovering
        sm.OnHardBlock(FoundationReasonCodes.VersionMissing);
        Assert.False(sm.AllowsConfirmedPublish);                          // Invalid
    }

    // 13. Unknown/unapproved session template blocks session-dependent output.
    [Fact]
    public void T13_unapproved_session_template_blocks()
    {
        var h = new InputFoundationHost(V, ApprovedConfig(), SessionTemplate.LegacyPrimaryAnchored24h);
        h.Attach(); h.SetContract("GCZ6", 0.1m, "test");
        h.NoteLiveTradeCapability(CapabilityAvailability.Available);
        h.NoteHistoricalTradeCapability(CapabilityAvailability.Available);
        h.NoteRequiredModuleReadiness(ModuleReadiness.Ready);
        h.Ingest(Trade(41133, 1, AggressorSide.Ask, T0));
        var (snap, _) = h.Evaluate();
        Assert.NotEqual(LifecycleState.AnalysisReady, snap.LifecycleState);
        Assert.Contains(FoundationReasonCodes.SessionTemplateReviewRequired, snap.ReasonCodes);
        Assert.Equal(SessionTemplateStatus.ReviewRequired, snap.SessionTemplateStatus);
    }

    // 14. Explicit synthetic session-template cases without promoting synthetic to production.
    [Fact]
    public void T14_synthetic_template_is_not_production_approval()
    {
        Assert.False(SessionTemplate.Unknown.IsApprovedForProduction);
        Assert.False(SessionTemplate.LegacyPrimaryAnchored24h.IsApprovedForProduction);
        Assert.Equal(FoundationReasonCodes.SessionTemplateUnknown, SessionTemplate.Unknown.BlockingReasonCode());
        // A synthetic approved template used in a test is explicitly labeled and only valid in-test.
        Assert.True(ApprovedTemplate().IsApprovedForProduction);
        Assert.Contains("not production approval", ApprovedTemplate().Provenance);
        // The legacy 24h anchor is never relabeled as the venue/primary session.
        Assert.Equal("UNSPECIFIED_PRIMARY", SessionTemplate.LegacyPrimaryAnchored24h.PrimarySessionIdentity);
    }

    // 15. Separate historical and live capability publication (never assumed equal).
    [Fact]
    public void T15_historical_and_live_capability_published_separately()
    {
        var h = ReadyHost();
        h.NoteHistoricalTradeCapability(CapabilityAvailability.Unproven);
        h.NoteLiveTradeCapability(CapabilityAvailability.Available);
        h.Ingest(Trade(41133, 1, AggressorSide.Ask, T0));
        var (snap, _) = h.Evaluate();
        Assert.Equal(CapabilityAvailability.Unproven, snap.HistoricalTradeCapability);
        Assert.Equal(CapabilityAvailability.Available, snap.LiveTradeCapability);
        Assert.Contains(FoundationReasonCodes.HistoricalTradeCapabilityUnproven, snap.ReasonCodes);
    }

    // 16. Required version absence invalidates the output.
    [Fact]
    public void T16_missing_version_invalidates()
    {
        var incomplete = new VersionStamp("1.0.0", "", "1.0.0");
        Assert.False(incomplete.IsComplete);
        Assert.Contains("ConfigVersion", incomplete.MissingFields());
        var h = new InputFoundationHost(incomplete, ApprovedConfig(), ApprovedTemplate());
        h.Attach(); h.SetContract("GCZ6", 0.1m, "test");
        h.NoteLiveTradeCapability(CapabilityAvailability.Available);
        h.NoteRequiredModuleReadiness(ModuleReadiness.Ready);
        h.Ingest(Trade(41133, 1, AggressorSide.Ask, T0));
        var (snap, _) = h.Evaluate();
        Assert.Equal(LifecycleState.Invalid, snap.LifecycleState);
        Assert.Contains(FoundationReasonCodes.VersionMissing, snap.ReasonCodes);
    }

    // 17. Late event produces a revision/reason rather than silently rewriting an old decision.
    [Fact]
    public void T17_late_event_creates_revision_not_silent_rewrite()
    {
        var h = ReadyHost();
        h.Ingest(Trade(41133, 1, AggressorSide.Ask, T0.AddSeconds(10)));
        var revBefore = h.Revision;
        var verdict = h.Ingest(Trade(41130, 1, AggressorSide.Ask, T0)); // arrives late (older event time)
        Assert.Equal(InputIntegrityMeter.Verdict.AcceptedLateRevision, verdict);
        Assert.Equal(revBefore + 1, h.Revision);
        var (snap, _) = h.Evaluate();
        Assert.Equal(1, snap.LateEventCount);
        Assert.Contains(FoundationReasonCodes.LateEventRevision, snap.ReasonCodes);
    }

    // 18. Same stream/config produces identical event sequence and snapshot hash.
    [Fact]
    public void T18_same_stream_same_hash()
    {
        string Run()
        {
            var h = ReadyHost();
            h.Ingest(Trade(41133, 1, AggressorSide.Ask, T0));
            h.Ingest(Trade(41134, 2, AggressorSide.Bid, T0.AddSeconds(1)));
            return h.Evaluate().Hash;
        }
        Assert.Equal(Run(), Run());
    }

    // 19. Restart/rebuild produces the same foundation hash for the same input.
    [Fact]
    public void T19_restart_rebuild_same_hash()
    {
        string Run(bool rebuild)
        {
            var h = ReadyHost();
            if (rebuild) h.BeginRebuild();
            h.Ingest(Trade(41133, 1, AggressorSide.Ask, T0));
            return h.Evaluate().Hash;
        }
        Assert.Equal(Run(false), Run(true)); // rebuild does not change the semantic hash of the same stream
    }

    // 20. Different config/session/contract/revision changes the hash.
    [Fact]
    public void T20_identity_change_changes_hash()
    {
        var baseHost = ReadyHost();
        baseHost.Ingest(Trade(41133, 1, AggressorSide.Ask, T0));
        var baseHash = baseHost.Evaluate().Hash;

        var otherContract = new InputFoundationHost(V, ApprovedConfig(), ApprovedTemplate());
        otherContract.Attach(); otherContract.SetContract("GCV6", 0.1m, "test");
        otherContract.NoteLiveTradeCapability(CapabilityAvailability.Available);
        otherContract.NoteRequiredModuleReadiness(ModuleReadiness.Ready);
        otherContract.Ingest(new CanonicalInputEvent("GC", "GCV6", 41133, 0.1m, 1, AggressorSide.Ask,
            InputEventKind.Trade, T0, T0, SourceTimeKind.DeclaredUtc, DataPhase.Live, "s",
            "k", DedupCapability.NativeStableId, 0, V));
        Assert.NotEqual(baseHash, otherContract.Evaluate().Hash);

        // reconnect epoch changes the hash too
        var reconnected = ReadyHost();
        reconnected.Ingest(Trade(41133, 1, AggressorSide.Ask, T0));
        reconnected.NoteReconnect();
        reconnected.Ingest(Trade(41133, 1, AggressorSide.Ask, T0));
        Assert.NotEqual(baseHash, reconnected.Evaluate().Hash);
    }

    // 21. Optional DOM/MBO/Options/CFD absence does not invalidate the baseline core.
    [Fact]
    public void T21_optional_absence_does_not_invalidate_core()
    {
        var h = ReadyHost();
        h.NoteOptionalModuleReadiness(ModuleReadiness.Unavailable); // DOM/MBO/Options/CFD absent
        h.Ingest(Trade(41133, 1, AggressorSide.Ask, T0));
        var (snap, _) = h.Evaluate();
        Assert.Equal(ModuleReadiness.Unavailable, snap.OptionalModuleReadiness);
        Assert.Equal(LifecycleState.AnalysisReady, snap.LifecycleState); // core still Ready
    }

    // (RecoveryScanner production-wire) corrupt/incomplete recorder segment fails closed.
    [Fact]
    public void T22_corrupt_recorder_segment_fails_closed_via_RecoveryScanner()
    {
        // Real RecoveryScanner call on a genuinely invalid segment file (no directories created).
        var tmp = Path.Combine(Path.GetTempPath(), "gcae_m1_" + Guid.NewGuid().ToString("N") + ".gcae");
        File.WriteAllBytes(tmp, new byte[] { 0x00, 0x01, 0x02, 0x03 }); // not a valid container
        try
        {
            var valid = RecoveryScanner.ValidateSegmentFile(tmp, new RecorderConfig(), out _, out _, out _);
            Assert.False(valid); // scanner classifies it corrupt/invalid
            var h = ReadyHost();
            h.Ingest(Trade(41133, 1, AggressorSide.Ask, T0));
            h.NoteRecorderRecovery(corruptOrIncompleteSegmentFound: !valid, restoreSnapshotId: "seg-recover-1");
            var (snap, _) = h.Evaluate();
            Assert.True(snap.HasUnrecoveredGap);
            Assert.Equal(LifecycleState.Recovering, snap.LifecycleState);
            Assert.NotEqual(LifecycleState.AnalysisReady, snap.LifecycleState);
        }
        finally { try { File.Delete(tmp); } catch { } }
    }

    // Positive control: a fully-coherent synthetic environment reaches AnalysisReady.
    [Fact]
    public void T23_coherent_environment_reaches_ready()
    {
        var h = ReadyHost();
        h.Ingest(Trade(41133, 1, AggressorSide.Ask, T0));
        var (snap, hash) = h.Evaluate();
        Assert.Equal(LifecycleState.AnalysisReady, snap.LifecycleState);
        Assert.Equal(DataState.Ready, snap.DataState);
        Assert.False(string.IsNullOrEmpty(hash));
        Assert.StartsWith("cap:", snap.CapabilitySnapshotId);
    }
}
