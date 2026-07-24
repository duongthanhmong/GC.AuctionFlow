using GC.AuctionFlow.Atas;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Reference;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Reference;

/// <summary>Phase 1C Structural Reference overlay readability (REFERENCE_OVERLAY_POLICY_V1).</summary>
public sealed class Phase1CStructuralReferenceOverlayTests
{
    private const decimal Tick = 0.1m;
    private const string Instrument = "GCQ6";
    private const string Epoch = "GCQ6|tick=0.1";

    private static PrimaryAuctionProfileSnapshot MakeAuction(
        string id, DateTime start, DateTime end, bool completed,
        decimal high = 100.5m, decimal low = 99.5m,
        decimal tpoPoc = 100.0m, decimal tpoVah = 100.2m, decimal tpoVal = 99.8m,
        decimal? vpoc = 100.0m, decimal? volVah = 100.2m, decimal? volVal = 99.8m,
        decimal? lastPx = 100.0m)
    {
        var tpo = new TpoProfileSnapshot(
            id, start, end, AuctionTimezoneResolver.IanaAmericaNewYork, new TimeSpan(8, 20, 0), 30,
            high, low, tpoPoc, tpoVah, tpoVal, 2, 1, null,
            new Dictionary<long, int> { [(long)(tpoPoc / Tick)] = 2 },
            ProfileDataQuality.Complete, "test", Array.Empty<string>());
        var vol = new VolumeProfileSnapshot(
            id, high, low, vpoc, volVah, volVal, 20m,
            new Dictionary<long, decimal> { [(long)((vpoc ?? tpoPoc) / Tick)] = 20m },
            PriceVolumeCapability.Exact, ProfileDataQuality.Complete, "test", Array.Empty<string>());
        return new PrimaryAuctionProfileSnapshot(
            AuctionProfileState.Ready, id, start, end, completed,
            tpo, vol, high, low, lastPx, end, null, ProfileDataQuality.Complete, Array.Empty<string>());
    }

    private static PrimaryProfileSetSnapshot Profiles(PrimaryAuctionProfileSnapshot cur, PrimaryAuctionProfileSnapshot? prev) =>
        new(cur, prev, HistoricalInitializationState.Complete, 10,
            Array.Empty<string>(), Array.Empty<string>(), null,
            prev is null ? Array.Empty<PrimaryAuctionProfileSnapshot>() : new[] { prev });

    private static StructuralReferenceHost Host() =>
        new(Tick, Instrument, Epoch, AtasTimestampNormalizer.PolicyVersion, new ReferencePolicyConfig(true));

    private static DateTime Utc(int day) => new(2026, 7, day, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Exact_zone_confluence_produces_one_line_one_label_retains_constituent_ids()
    {
        var host = Host();
        var prev = MakeAuction("PI-2026-07-20", Utc(20), Utc(21), true, tpoPoc: 100.0m, vpoc: 100.0m);
        var cur = MakeAuction("PI-2026-07-21", Utc(21), Utc(22), false, tpoPoc: 100.0m, vpoc: 100.2m);
        var refs = host.Rebuild(Profiles(cur, prev), null);
        var engineCount = refs.ActiveReferences.Count;
        var group = refs.ConfluenceGroups.Single(g => g.ZoneLow == 100.0m && g.ConstituentCount >= 2);

        var ovm = PrimaryProfileOverlayViewModel.FromProfiles(
            Profiles(cur, prev), true, null, enableCompositeOverlay: true, showCompositePreview: false,
            refs, enableStructuralReferenceOverlay: true);

        var atZone = ovm.Levels.Where(l => l.Price == 100.0m).ToArray();
        Assert.Single(atZone);
        Assert.Equal(ProfileOverlayKind.StructuralReference, atZone[0].Kind);
        Assert.True(atZone[0].ShowLine);
        Assert.True(atZone[0].ConstituentReferenceIds.Count >= 2);
        Assert.Equal(group.ConstituentReferenceIds.Count, atZone[0].ConstituentReferenceIds.Count);
        Assert.All(group.ConstituentReferenceIds, id => Assert.Contains(id, atZone[0].ConstituentReferenceIds));
        Assert.Equal(engineCount, refs.ActiveReferences.Count); // engine unchanged
    }

    [Fact]
    public void Confluence_does_not_also_render_individual_constituent_or_primary_composite_labels()
    {
        var host = Host();
        var prev = MakeAuction("PI-2026-07-20", Utc(20), Utc(21), true, tpoPoc: 100.0m, vpoc: 100.0m);
        var cur = MakeAuction("PI-2026-07-21", Utc(21), Utc(22), false, tpoPoc: 100.0m, vpoc: 100.2m);
        var refs = host.Rebuild(Profiles(cur, prev), null);

        var ovm = PrimaryProfileOverlayViewModel.FromProfiles(
            Profiles(cur, prev), showPrevious: true, null,
            enableCompositeOverlay: true, showCompositePreview: false,
            refs, enableStructuralReferenceOverlay: true);

        Assert.All(ovm.Levels, l => Assert.Equal(ProfileOverlayKind.StructuralReference, l.Kind));
        Assert.DoesNotContain(ovm.Levels, l => l.Label.StartsWith("Current ", StringComparison.Ordinal));
        Assert.DoesNotContain(ovm.Levels, l => l.Label.StartsWith("Previous ", StringComparison.Ordinal));
        Assert.DoesNotContain(ovm.Levels, l => l.Label.StartsWith("Confirmed Composite", StringComparison.Ordinal));
        Assert.Equal(1, ovm.Levels.Count(l => l.Price == 100.0m));
    }

    [Fact]
    public void Adjacent_distinct_ticks_remain_separate_zones()
    {
        var host = Host();
        var prev = MakeAuction("PI-2026-07-20", Utc(20), Utc(21), true, tpoPoc: 100.0m, tpoVah: 100.1m, vpoc: 100.0m, volVah: 100.1m);
        var cur = MakeAuction("PI-2026-07-21", Utc(21), Utc(22), false, tpoPoc: 100.2m, tpoVah: 100.3m, vpoc: 100.2m, volVah: 100.3m, lastPx: 100.2m);
        var refs = host.Rebuild(Profiles(cur, prev), null);
        Assert.True(refs.ConfluenceGroups.Count(g => g.ZoneLow is 100.0m or 100.1m or 100.2m or 100.3m) >= 2);

        var ovm = PrimaryProfileOverlayViewModel.FromProfiles(
            Profiles(cur, prev), true, null, false, false, refs, true);
        var prices = ovm.Levels.Select(l => l.Price).Distinct().OrderBy(p => p).ToArray();
        Assert.Contains(100.0m, prices);
        Assert.Contains(100.1m, prices);
        Assert.True(prices.Count(p => p == 100.0m) == 1);
        Assert.True(prices.Count(p => p == 100.1m) == 1);
    }

    [Fact]
    public void Maturity_wording_confirmed_developing_mixed_explicit()
    {
        var host = Host();
        var prev = MakeAuction("PI-2026-07-20", Utc(20), Utc(21), true,
            high: 101.0m, low: 99.0m, tpoPoc: 100.0m, tpoVah: 100.5m, tpoVal: 99.5m, vpoc: 99.0m, volVah: 100.5m, volVal: 99.5m);
        var cur = MakeAuction("PI-2026-07-21", Utc(21), Utc(22), false,
            high: 102.0m, low: 98.0m, tpoPoc: 100.0m, tpoVah: 101.5m, tpoVal: 98.5m, vpoc: 100.8m, volVah: 101.5m, volVal: 98.5m, lastPx: 100.0m);
        var refs = host.Rebuild(Profiles(cur, prev), null);
        var ovm = PrimaryProfileOverlayViewModel.FromProfiles(
            Profiles(cur, prev), true, null, false, false, refs, true);

        Assert.Contains(ovm.Levels, l => l.Label.Contains("CONFIRMED", StringComparison.Ordinal));
        Assert.Contains(ovm.Levels, l => l.Label.Contains("DEVELOPING", StringComparison.Ordinal));
        Assert.Contains(ovm.Levels, l => l.Price == 100.0m && l.Label.Contains("MIXED", StringComparison.Ordinal));
        Assert.All(ovm.Levels.Where(l => l.Kind == ProfileOverlayKind.StructuralReference), l =>
        {
            Assert.True(
                l.Label.Contains("CONFIRMED", StringComparison.Ordinal)
                || l.Label.Contains("DEVELOPING", StringComparison.Ordinal)
                || l.Label.Contains("MIXED", StringComparison.Ordinal));
        });
    }

    [Fact]
    public void Label_priority_deterministic_and_collision_suppresses_lower()
    {
        // Two zones mapped to colliding Y (same pixel band); higher priority keeps label.
        var highPri = new ReferenceOverlayDisplayRow(
            100.0m, 1000, 1000, "REF MIXED ×2 | CMP TPO POC + CUR TPO POC | 100.0", "MIXED", 2,
            new[] { "id-a", "id-b" },
            new[] { ReferenceType.CompositeTpoPoc, ReferenceType.CurrentPrimaryTpoPoc },
            priority: 1500, showLabel: true, showLine: true);
        var lowPri = new ReferenceOverlayDisplayRow(
            100.1m, 1001, 1001, "REF DEVELOPING | CUR HIGH | 100.1", "DEVELOPING", 1,
            new[] { "id-c" },
            new[] { ReferenceType.CurrentPrimaryAuctionHigh },
            priority: 10, showLabel: true, showLine: true);

        // Priority-ordered input (highest first).
        var collided = ReferenceOverlayDisplayPolicy.ApplyLabelCollision(
            new[] { highPri, lowPri },
            price => price == 100.0m ? 100 : 105, // within DefaultMinLabelSeparationPx=14
            minSeparationPx: 14);

        var kept = collided.Single(r => r.Price == 100.0m);
        var suppressed = collided.Single(r => r.Price == 100.1m);
        Assert.True(kept.ShowLabel);
        Assert.False(suppressed.ShowLabel);
        Assert.True(suppressed.ShowLine); // line retained
        Assert.Equal(ReferenceOverlayDisplayPolicy.PolicyVersion, kept.OverlayPolicyVersion);

        // Swap Y far apart → both labels shown.
        var far = ReferenceOverlayDisplayPolicy.ApplyLabelCollision(
            new[] { highPri, lowPri },
            price => price == 100.0m ? 100 : 200,
            minSeparationPx: 14);
        Assert.All(far, r => Assert.True(r.ShowLabel));
    }

    [Fact]
    public void Priority_multi_constituent_outranks_single_developing()
    {
        var multi = new ReferenceConfluenceGroup(
            100.0m, 100.0m, 1000, 1000,
            new[] { "a", "b" },
            new[] { ReferenceType.PreviousPrimaryTpoPoc, ReferenceType.CurrentPrimaryTpoPoc },
            confirmedCount: 1, developingCount: 1,
            new[] { ReferenceSourceHorizon.PreviousPrimaryAuction, ReferenceSourceHorizon.CurrentPrimaryAuction });
        var singleDev = new ReferenceConfluenceGroup(
            100.5m, 100.5m, 1005, 1005,
            new[] { "c" },
            new[] { ReferenceType.CurrentPrimaryAuctionHigh },
            confirmedCount: 0, developingCount: 1,
            new[] { ReferenceSourceHorizon.CurrentPrimaryAuction });
        Assert.True(ReferenceOverlayDisplayPolicy.ComputePriority(multi) >
                    ReferenceOverlayDisplayPolicy.ComputePriority(singleDev));
    }

    [Fact]
    public void Suppression_does_not_remove_engine_snapshot_references()
    {
        var host = Host();
        var prev = MakeAuction("PI-2026-07-20", Utc(20), Utc(21), true);
        var cur = MakeAuction("PI-2026-07-21", Utc(21), Utc(22), false);
        var refs = host.Rebuild(Profiles(cur, prev), null);
        var beforeConfirmed = refs.ConfirmedReferences.Count;
        var beforeDeveloping = refs.DevelopingReferences.Count;
        var beforeGroups = refs.ConfluenceGroups.Count;

        var ovm = PrimaryProfileOverlayViewModel.FromProfiles(
            Profiles(cur, prev), true, null, false, false, refs, true);
        _ = ovm.Levels.Count(l => l.ShowLabel);

        Assert.Equal(beforeConfirmed, refs.ConfirmedReferences.Count);
        Assert.Equal(beforeDeveloping, refs.DevelopingReferences.Count);
        Assert.Equal(beforeGroups, refs.ConfluenceGroups.Count);
        Assert.Equal(beforeGroups, ovm.Levels.Count); // one row per confluence group (lines may keep suppressed labels)
    }

    [Fact]
    public void No_forbidden_wording_in_overlay_policy()
    {
        var host = Host();
        var prev = MakeAuction("PI-2026-07-20", Utc(20), Utc(21), true, tpoPoc: 100.0m, vpoc: 100.0m);
        var cur = MakeAuction("PI-2026-07-21", Utc(21), Utc(22), false, tpoPoc: 100.0m, vpoc: 100.3m);
        var refs = host.Rebuild(Profiles(cur, prev), null);
        var ovm = PrimaryProfileOverlayViewModel.FromProfiles(
            Profiles(cur, prev), true, null, false, false, refs, true);

        foreach (var l in ovm.Levels)
        {
            Assert.DoesNotContain("LONG", l.Label, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("SHORT", l.Label, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("SUPPORT", l.Label, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("RESISTANCE", l.Label, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("EPISODE", l.Label, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("THESIS", l.Label, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PROBABILITY", l.Label, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("SCORE", l.Label, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Equal(ReferenceOverlayDisplayPolicy.PolicyVersion, ovm.Levels[0].OverlayPolicyVersion);
    }

    [Fact]
    public void Overlay_off_produces_no_reference_objects_on_produces_one_set()
    {
        var host = Host();
        var prev = MakeAuction("PI-2026-07-20", Utc(20), Utc(21), true);
        var cur = MakeAuction("PI-2026-07-21", Utc(21), Utc(22), false);
        var refs = host.Rebuild(Profiles(cur, prev), null);
        var profiles = Profiles(cur, prev);

        var off = PrimaryProfileOverlayViewModel.FromProfiles(
            profiles, true, null, false, false, refs, enableStructuralReferenceOverlay: false);
        Assert.DoesNotContain(off.Levels, l => l.Kind == ProfileOverlayKind.StructuralReference);

        var on = PrimaryProfileOverlayViewModel.FromProfiles(
            profiles, true, null, false, false, refs, enableStructuralReferenceOverlay: true);
        Assert.NotEmpty(on.Levels);
        Assert.All(on.Levels, l => Assert.Equal(ProfileOverlayKind.StructuralReference, l.Kind));
        Assert.Equal(refs.ConfluenceGroups.Count, on.Levels.Count);

        var on2 = PrimaryProfileOverlayViewModel.FromProfiles(
            profiles, true, null, false, false, refs, enableStructuralReferenceOverlay: true);
        Assert.Equal(on.Levels.Count, on2.Levels.Count);
        Assert.Equal(
            on.Levels.Select(l => l.Price).OrderBy(p => p).ToArray(),
            on2.Levels.Select(l => l.Price).OrderBy(p => p).ToArray());
    }

    [Fact]
    public void Renderer_resolve_collision_keeps_higher_priority_label()
    {
        var levels = new[]
        {
            new ProfileOverlayLevel("REF MIXED ×2 | A | 100.0", 100.0m, ProfileOverlayKind.StructuralReference,
                new[] { "a", "b" }, showLabel: true, showLine: true, overlayPriority: 1500, maturityToken: "MIXED",
                overlayPolicyVersion: ReferenceOverlayDisplayPolicy.PolicyVersion),
            new ProfileOverlayLevel("REF DEVELOPING | C | 100.1", 100.1m, ProfileOverlayKind.StructuralReference,
                new[] { "c" }, showLabel: true, showLine: true, overlayPriority: 10, maturityToken: "DEVELOPING",
                overlayPolicyVersion: ReferenceOverlayDisplayPolicy.PolicyVersion)
        };

        var resolved = PrimaryProfileOverlayRenderer.ResolveLevelsForRender(
            levels,
            price => price == 100.0m ? 50 : 55);

        Assert.True(resolved.Single(l => l.Price == 100.0m).ShowLabel);
        Assert.False(resolved.Single(l => l.Price == 100.1m).ShowLabel);
        Assert.True(resolved.Single(l => l.Price == 100.1m).ShowLine);
    }
}
