using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Reference;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Reference;

/// <summary>
/// Phase 1C Developing revision monotonicity — registry is the authoritative StateVersion owner.
/// </summary>
public sealed class Phase1CDevelopingRevisionTests
{
    private const decimal Tick = 0.1m;
    private const string Instrument = "GCQ6";
    private const string Epoch = "GCQ6|tick=0.1";

    private static DateTime Utc(int day, int minute = 0) =>
        new(2026, 7, day, 12, minute, 0, DateTimeKind.Utc);

    private static PrimaryAuctionProfileSnapshot MakeAuction(
        string id, DateTime start, DateTime end, bool completed,
        decimal high, decimal low,
        decimal tpoPoc, decimal tpoVah, decimal tpoVal,
        decimal vpoc, decimal volVah, decimal volVal,
        decimal lastPx)
    {
        var tpo = new TpoProfileSnapshot(
            id, start, end, AuctionTimezoneResolver.IanaAmericaNewYork, new TimeSpan(8, 20, 0), 30,
            high, low, tpoPoc, tpoVah, tpoVal, 2, 1, null,
            new Dictionary<long, int> { [(long)(tpoPoc / Tick)] = 2 },
            ProfileDataQuality.Complete, "test", Array.Empty<string>());
        var vol = new VolumeProfileSnapshot(
            id, high, low, vpoc, volVah, volVal, 20m,
            new Dictionary<long, decimal>
            {
                [(long)(vpoc / Tick)] = 10m,
                [(long)(volVah / Tick)] = 5m,
                [(long)(volVal / Tick)] = 5m
            },
            PriceVolumeCapability.Exact, ProfileDataQuality.Complete, "test", Array.Empty<string>());
        return new PrimaryAuctionProfileSnapshot(
            AuctionProfileState.Ready, id, start, end, completed,
            tpo, vol, high, low, lastPx, end, null, ProfileDataQuality.Complete, Array.Empty<string>());
    }

    private static PrimaryProfileSetSnapshot Profiles(PrimaryAuctionProfileSnapshot cur, PrimaryAuctionProfileSnapshot prev) =>
        new(cur, prev, HistoricalInitializationState.Complete, 10,
            Array.Empty<string>(), Array.Empty<string>(), null, new[] { prev });

    private static StructuralReferenceHost Host() =>
        new(Tick, Instrument, Epoch, AtasTimestampNormalizer.PolicyVersion, new ReferencePolicyConfig(true));

    private static PrimaryAuctionProfileSnapshot Prev() =>
        MakeAuction("PI-2026-07-22", Utc(22), Utc(23), true,
            101.0m, 99.0m, 100.0m, 100.5m, 99.5m, 100.0m, 100.5m, 99.5m, 100.0m);

    private static PrimaryAuctionProfileSnapshot Cur(
        decimal high, decimal low, decimal tpoPoc, decimal tpoVah, decimal tpoVal,
        decimal vpoc, decimal volVah, decimal volVal, decimal lastPx) =>
        MakeAuction("PI-2026-07-23", Utc(23), Utc(24), false,
            high, low, tpoPoc, tpoVah, tpoVal, vpoc, volVah, volVal, lastPx);

    [Fact]
    public void New_developing_starts_at_state_version_one()
    {
        var host = Host();
        var s = host.Rebuild(Profiles(Cur(100.5m, 99.5m, 100.0m, 100.2m, 99.8m, 100.0m, 100.2m, 99.8m, 100.0m), Prev()), null, Utc(23));
        var vah = s.DevelopingReferences.Single(r => r.ReferenceType == ReferenceType.CurrentPrimaryVolumeVah);
        Assert.Equal(1, vah.StateVersion);
        Assert.Equal(StructuralReferenceModuleState.Ready, s.ModuleState);
    }

    [Fact]
    public void Identical_republish_keeps_version_and_does_not_bump_registry_revision_for_zone()
    {
        var host = Host();
        var profiles = Profiles(Cur(100.5m, 99.5m, 100.0m, 100.2m, 99.8m, 100.0m, 100.2m, 99.8m, 100.0m), Prev());
        var s1 = host.Rebuild(profiles, null, Utc(23));
        var rev1 = s1.RegistryRevision;
        var vah1 = s1.DevelopingReferences.Single(r => r.ReferenceType == ReferenceType.CurrentPrimaryVolumeVah);

        var s2 = host.Rebuild(profiles, null, Utc(23, 1));
        var vah2 = s2.DevelopingReferences.Single(r => r.ReferenceType == ReferenceType.CurrentPrimaryVolumeVah);
        Assert.Equal(vah1.ReferenceId, vah2.ReferenceId);
        Assert.Equal(vah1.ZoneLow, vah2.ZoneLow);
        Assert.Equal(vah1.StateVersion, vah2.StateVersion);
        // Fresh→Active may bump registry revision once; subsequent identical publish must not.
        var rev2 = s2.RegistryRevision;
        var s3 = host.Rebuild(profiles, null, Utc(23, 2));
        Assert.Equal(rev2, s3.RegistryRevision);
        Assert.Equal(vah1.StateVersion, s3.DevelopingReferences.Single(r => r.ReferenceType == ReferenceType.CurrentPrimaryVolumeVah).StateVersion);
        _ = rev1;
    }

    [Fact]
    public void Developing_zone_change_increments_state_version_by_exactly_one()
    {
        var host = Host();
        var s1 = host.Rebuild(Profiles(Cur(100.5m, 99.5m, 100.0m, 100.2m, 99.8m, 100.0m, 100.2m, 99.8m, 100.0m), Prev()), null, Utc(23));
        var vah1 = s1.DevelopingReferences.Single(r => r.ReferenceType == ReferenceType.CurrentPrimaryVolumeVah);
        var s2 = host.Rebuild(Profiles(Cur(100.5m, 99.5m, 100.0m, 100.2m, 99.8m, 100.0m, 100.4m, 99.8m, 100.0m), Prev()), null, Utc(23, 1));
        var vah2 = s2.DevelopingReferences.Single(r => r.ReferenceType == ReferenceType.CurrentPrimaryVolumeVah);
        Assert.Equal(vah1.ReferenceId, vah2.ReferenceId);
        Assert.Equal(vah1.StateVersion + 1, vah2.StateVersion);
        Assert.Equal(100.4m, vah2.ZoneLow);
        Assert.Equal(StructuralReferenceModuleState.Ready, s2.ModuleState);
    }

    [Fact]
    public void Developing_zone_A_B_C_A_remains_strictly_monotonic()
    {
        var host = Host();
        StructuralReferenceSnapshot Step(decimal volVah, int minute)
        {
            var s = host.Rebuild(
                Profiles(Cur(100.5m, 99.5m, 100.0m, 100.2m, 99.8m, 100.0m, volVah, 99.8m, 100.0m), Prev()),
                null, Utc(23, minute));
            Assert.NotEqual(StructuralReferenceModuleState.Invalid, s.ModuleState);
            return s.DevelopingReferences.Single(r => r.ReferenceType == ReferenceType.CurrentPrimaryVolumeVah);
        }

        var a1 = Step(100.2m, 0);
        var b = Step(100.4m, 1);
        var c = Step(100.6m, 2);
        var a2 = Step(100.2m, 3); // return to A — must not reuse version of a1

        Assert.Equal(a1.ReferenceId, a2.ReferenceId);
        Assert.True(b.StateVersion > a1.StateVersion);
        Assert.True(c.StateVersion > b.StateVersion);
        Assert.True(a2.StateVersion > c.StateVersion);
        Assert.Equal(100.2m, a2.ZoneLow);
        Assert.NotEqual(a1.StateVersion, a2.StateVersion);
    }

    [Fact]
    public void Recreating_extractor_batch_via_host_rebuild_does_not_reset_revision()
    {
        var host = Host();
        var s1 = host.Rebuild(Profiles(Cur(100.5m, 99.5m, 100.0m, 100.2m, 99.8m, 100.0m, 100.2m, 99.8m, 100.0m), Prev()), null, Utc(23));
        var s2 = host.Rebuild(Profiles(Cur(100.5m, 99.5m, 100.0m, 100.2m, 99.8m, 100.0m, 100.5m, 99.8m, 100.0m), Prev()), null, Utc(23, 1));
        var v2 = s2.DevelopingReferences.Single(r => r.ReferenceType == ReferenceType.CurrentPrimaryVolumeVah).StateVersion;
        Assert.True(v2 >= 2);

        // New extraction batch (fingerprint change) — still same host/registry ledger.
        var s3 = host.Rebuild(Profiles(Cur(100.5m, 99.5m, 100.0m, 100.2m, 99.8m, 100.0m, 100.7m, 99.8m, 100.0m), Prev()), null, Utc(23, 2));
        var v3 = s3.DevelopingReferences.Single(r => r.ReferenceType == ReferenceType.CurrentPrimaryVolumeVah).StateVersion;
        Assert.Equal(v2 + 1, v3);
        Assert.Equal(StructuralReferenceModuleState.Ready, s3.ModuleState);
        Assert.DoesNotContain(s3.KnownLimitations, l => l.StartsWith("STALE_REVISION", StringComparison.Ordinal));
        _ = s1;
    }

    [Fact]
    public void Fingerprint_change_reconciles_once_without_stale_and_trade_style_reuses()
    {
        var prev = Prev();
        var cur = Cur(100.5m, 99.5m, 100.0m, 100.2m, 99.8m, 100.0m, 100.2m, 99.8m, 100.0m);
        var profiles = Profiles(cur, prev);
        StructuralReferenceHost? host = null;
        ReferenceInputFingerprint? last = null;
        var rebuilds = 0;

        ReferenceInputFingerprint Fp(PrimaryProfileSetSnapshot p) =>
            ReferenceInputFingerprint.Build(true, p, null, Tick, Epoch, AtasTimestampNormalizer.PolicyVersion);

        void Maybe(PrimaryProfileSetSnapshot p)
        {
            var current = Fp(p);
            if (!ReferencePublishInitialization.ShouldProcess(true, p, host, last, current))
                return;
            host ??= Host();
            host.Configure(Tick, Instrument, Epoch, new ReferencePolicyConfig(true));
            var snap = host.Rebuild(p, null);
            Assert.NotEqual(StructuralReferenceModuleState.Invalid, snap.ModuleState);
            last = host.LastAppliedFingerprint;
            rebuilds++;
        }

        Maybe(profiles);
        Maybe(profiles);
        Maybe(profiles);
        Assert.Equal(1, rebuilds);

        var moved = Profiles(Cur(100.5m, 99.5m, 100.0m, 100.2m, 99.8m, 100.0m, 100.9m, 99.8m, 100.0m), prev);
        Maybe(moved);
        Assert.Equal(2, rebuilds);
        Assert.Equal(StructuralReferenceModuleState.Ready, host!.Current!.ModuleState);
    }

    [Fact]
    public void CurrentPrimaryVolumeVah_follows_same_rules_as_sibling_developing_types()
    {
        var host = Host();
        var s1 = host.Rebuild(Profiles(Cur(100.5m, 99.5m, 100.0m, 100.2m, 99.8m, 100.0m, 100.2m, 99.8m, 100.0m), Prev()), null, Utc(23));
        var s2 = host.Rebuild(Profiles(Cur(100.6m, 99.4m, 100.1m, 100.3m, 99.7m, 100.1m, 100.3m, 99.7m, 100.1m), Prev()), null, Utc(23, 1));

        ReferenceType[] types =
        [
            ReferenceType.CurrentPrimaryVolumeVah,
            ReferenceType.CurrentPrimaryTpoPoc,
            ReferenceType.CurrentPrimaryVolumeVal,
            ReferenceType.CurrentPrimaryAuctionHigh,
            ReferenceType.CurrentPrimaryAuctionLow
        ];

        foreach (var t in types)
        {
            var before = s1.DevelopingReferences.Single(r => r.ReferenceType == t);
            var after = s2.DevelopingReferences.Single(r => r.ReferenceType == t);
            Assert.Equal(before.ReferenceId, after.ReferenceId);
            Assert.Equal(before.StateVersion + 1, after.StateVersion);
        }

        Assert.Equal(StructuralReferenceModuleState.Ready, s2.ModuleState);
    }

    [Fact]
    public void Disable_reenable_retires_then_starts_fresh_version_one()
    {
        var host = Host();
        var profiles = Profiles(Cur(100.5m, 99.5m, 100.0m, 100.2m, 99.8m, 100.0m, 100.2m, 99.8m, 100.0m), Prev());
        host.Rebuild(profiles, null, Utc(23));
        host.Rebuild(Profiles(Cur(100.5m, 99.5m, 100.0m, 100.2m, 99.8m, 100.0m, 100.5m, 99.8m, 100.0m), Prev()), null, Utc(23, 1));

        host.Configure(Tick, Instrument, Epoch, new ReferencePolicyConfig(false));
        Assert.Equal(StructuralReferenceModuleState.Disabled, host.Current!.ModuleState);

        host.Configure(Tick, Instrument, Epoch, new ReferencePolicyConfig(true));
        var s = host.Rebuild(profiles, null, Utc(23, 2));
        var vah = s.DevelopingReferences.Single(r => r.ReferenceType == ReferenceType.CurrentPrimaryVolumeVah);
        Assert.Equal(1, vah.StateVersion);
        Assert.Equal(StructuralReferenceModuleState.Ready, s.ModuleState);
        Assert.DoesNotContain(s.KnownLimitations, l => l.StartsWith("STALE_REVISION", StringComparison.Ordinal));
    }

    [Fact]
    public void Authoritative_lower_stale_revision_still_rejected()
    {
        var reg = new StructuralReferenceRegistry(Tick, Epoch, AtasTimestampNormalizer.PolicyVersion);
        var grid = new PriceGrid(Tick);
        Assert.True(grid.TryToTickIndex(100.0m, out var t));
        var id = ReferenceIdentity.Build(Instrument, Epoch, "PI-B", ReferenceType.CurrentPrimaryVolumeVah, ReferenceMaturity.Developing);

        reg.Upsert(new StructuralReferenceSnapshot(
            id, ReferenceType.CurrentPrimaryVolumeVah, 100.0m, 100.0m, t, t,
            "PI-B", ReferenceSourceKind.CurrentPrimaryAuction, ReferenceSourceHorizon.CurrentPrimaryAuction,
            Utc(23), ReferenceMaturity.Developing, ReferenceStatus.Fresh, ReferenceEvidenceTier.ProfileDerived,
            Tick, Epoch, AtasTimestampNormalizer.PolicyVersion, 2, Utc(23), Utc(23)), Utc(23));

        var stale = new StructuralReferenceSnapshot(
            id, ReferenceType.CurrentPrimaryVolumeVah, 100.2m, 100.2m, t + 2, t + 2,
            "PI-B", ReferenceSourceKind.CurrentPrimaryAuction, ReferenceSourceHorizon.CurrentPrimaryAuction,
            Utc(23), ReferenceMaturity.Developing, ReferenceStatus.Active, ReferenceEvidenceTier.ProfileDerived,
            Tick, Epoch, AtasTimestampNormalizer.PolicyVersion, 1, Utc(23), Utc(23, 1));
        var ex = Assert.Throws<InvalidOperationException>(() => reg.Upsert(stale, Utc(23, 1)));
        Assert.StartsWith("STALE_REVISION:", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Registry_assigned_draft_version_zero_does_not_stale_after_moves()
    {
        var reg = new StructuralReferenceRegistry(Tick, Epoch, AtasTimestampNormalizer.PolicyVersion);
        var id = ReferenceIdentity.Build(Instrument, Epoch, "PI-B", ReferenceType.CurrentPrimaryVolumeVah, ReferenceMaturity.Developing);
        var grid = new PriceGrid(Tick);
        Assert.True(grid.TryToTickIndex(100.2m, out var t0));
        Assert.True(grid.TryToTickIndex(100.4m, out var t1));
        Assert.True(grid.TryToTickIndex(100.6m, out var t2));

        StructuralReferenceSnapshot Draft(long tick, decimal px) => new(
            id, ReferenceType.CurrentPrimaryVolumeVah, px, px, tick, tick,
            "PI-B", ReferenceSourceKind.CurrentPrimaryAuction, ReferenceSourceHorizon.CurrentPrimaryAuction,
            Utc(23), ReferenceMaturity.Developing, ReferenceStatus.Fresh, ReferenceEvidenceTier.ProfileDerived,
            Tick, Epoch, AtasTimestampNormalizer.PolicyVersion,
            StructuralReferenceSnapshot.RegistryAssignedStateVersion, Utc(23), Utc(23));

        reg.Upsert(Draft(t0, 100.2m), Utc(23));
        reg.Upsert(Draft(t1, 100.4m), Utc(23, 1));
        reg.Upsert(Draft(t2, 100.6m), Utc(23, 2));
        reg.Upsert(Draft(t0, 100.2m), Utc(23, 3));

        var cur = reg.Active.Single(r => r.ReferenceId == id);
        Assert.Equal(4, cur.StateVersion);
        Assert.Equal(100.2m, cur.ZoneLow);
    }

    [Fact]
    public void Confirmed_immutable_and_compat_fail_closed_unchanged()
    {
        var reg = new StructuralReferenceRegistry(Tick, Epoch, AtasTimestampNormalizer.PolicyVersion);
        var id = ReferenceIdentity.Build(Instrument, Epoch, "PI-A", ReferenceType.PreviousPrimaryTpoPoc, ReferenceMaturity.Confirmed);
        reg.Upsert(new StructuralReferenceSnapshot(
            id, ReferenceType.PreviousPrimaryTpoPoc, 100.0m, 100.0m, 1000, 1000,
            "PI-A", ReferenceSourceKind.PreviousPrimaryAuction, ReferenceSourceHorizon.PreviousPrimaryAuction,
            Utc(22), ReferenceMaturity.Confirmed, ReferenceStatus.Fresh, ReferenceEvidenceTier.ProfileDerived,
            Tick, Epoch, AtasTimestampNormalizer.PolicyVersion, 1, Utc(22), Utc(22)), Utc(22));

        Assert.Throws<InvalidOperationException>(() => reg.Upsert(new StructuralReferenceSnapshot(
            id, ReferenceType.PreviousPrimaryTpoPoc, 100.5m, 100.5m, 1005, 1005,
            "PI-A", ReferenceSourceKind.PreviousPrimaryAuction, ReferenceSourceHorizon.PreviousPrimaryAuction,
            Utc(22), ReferenceMaturity.Confirmed, ReferenceStatus.Active, ReferenceEvidenceTier.ProfileDerived,
            Tick, Epoch, AtasTimestampNormalizer.PolicyVersion, 1, Utc(22), Utc(22, 1)), Utc(22, 1)));

        Assert.Throws<InvalidOperationException>(() => reg.Upsert(new StructuralReferenceSnapshot(
            id, ReferenceType.PreviousPrimaryTpoPoc, 100.0m, 100.0m, 1000, 1000,
            "PI-A", ReferenceSourceKind.PreviousPrimaryAuction, ReferenceSourceHorizon.PreviousPrimaryAuction,
            Utc(22), ReferenceMaturity.Confirmed, ReferenceStatus.Fresh, ReferenceEvidenceTier.ProfileDerived,
            0.25m, Epoch, AtasTimestampNormalizer.PolicyVersion, 1, Utc(22), Utc(22)), Utc(22)));
    }
}
