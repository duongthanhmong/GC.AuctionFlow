using System.Reflection;
using GC.AuctionFlow.Composite;
using GC.AuctionFlow.Efficiency;
using GC.AuctionFlow.Profile;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Profile;

/// <summary>
/// WP01A DEC-T03: regression guards only. No production behaviour is changed by this work
/// package; these tests pin what already exists so that scope-naming work later cannot move
/// it silently.
///
/// The collision analysis found nine points where a session "scope" would collide with
/// something already in the codebase. Four of them are pinned here, and the one that matters
/// most is `COL-04`: the `PI-` identity prefix is not a label, it is PARSED, and a rename
/// degrades to a wrong-but-plausible date rather than failing.
/// </summary>
public sealed class Wp01aT03ContractGuardTests
{
    private static readonly DateTime Utc = new(2026, 7, 27, 14, 0, 0, DateTimeKind.Utc);

    // ========== COL-04: the identity is a consumed contract ==========

    /// <summary>
    /// `AuctionId` remains `PI-yyyy-MM-dd`.
    ///
    /// Not cosmetic. The string is threaded through episodes, efficiency, effort/result and
    /// the persisted research dataset, and it is parsed downstream.
    /// </summary>
    [Fact]
    public void T01_AuctionId_is_PI_yyyy_MM_dd()
    {
        var clock = new PrimaryAuctionClock();
        var point = clock.Resolve(new DateTimeOffset(2026, 7, 27, 14, 0, 0, TimeSpan.Zero));

        Assert.Equal("PI-2026-07-27", point.AuctionId);
        Assert.Equal(new DateOnly(2026, 7, 27), point.LocalAuctionDate);
    }

    /// <summary>
    /// `CompositeContribution` must keep resolving the date FROM THE IDENTITY.
    ///
    /// `CompositeAuctionContribution.FromPrimaryAuction` reads
    /// `AuctionId.StartsWith("PI-") ? AuctionId[3..] : ""` and falls back to the UTC start
    /// date when the parse fails. On a normal 08:20 ET auction the two coincide, so a broken
    /// parse would look correct — which is exactly why this fixture separates them: the
    /// auction is labelled 2026-07-27 while its UTC start lands on 2026-07-26.
    ///
    /// If the prefix is renamed, or the parse degrades to an empty string, the local date
    /// silently becomes the UTC date and this test fails. That is the whole point.
    /// </summary>
    [Fact]
    public void T02_Composite_contribution_resolves_the_date_from_the_PI_prefix()
    {
        var auction = Auction("PI-2026-07-27", new DateTime(2026, 7, 26, 23, 0, 0, DateTimeKind.Utc));

        var contribution = CompositeAuctionContribution.FromPrimaryAuction(
            auction, contractIdentity: "GCQ6", contractEpoch: "GCQ6|tick=0.1", tickSize: 0.1m);

        Assert.Equal(new DateOnly(2026, 7, 27), contribution.LocalAuctionDate);
    }

    /// <summary>
    /// The negative half of `T02`: an identity without the prefix degrades SILENTLY.
    ///
    /// This documents the failure mode rather than approving it. It is here so that anyone
    /// proposing a rename can see, in a passing test, that the parse does not throw — it
    /// returns a different date and carries on.
    /// </summary>
    [Fact]
    public void T03_An_identity_without_the_prefix_degrades_to_the_utc_date_without_failing()
    {
        var auction = Auction("SCOPE-2026-07-27", new DateTime(2026, 7, 26, 23, 0, 0, DateTimeKind.Utc));

        var contribution = CompositeAuctionContribution.FromPrimaryAuction(
            auction, contractIdentity: "GCQ6", contractEpoch: "GCQ6|tick=0.1", tickSize: 0.1m);

        Assert.Equal(new DateOnly(2026, 7, 26), contribution.LocalAuctionDate);
        Assert.NotEqual(new DateOnly(2026, 7, 27), contribution.LocalAuctionDate);
    }

    // ========== E-T03-05: the ambiguous-time behaviour, pinned as it actually is ==========

    /// <summary>
    /// The current implementation selects `06:30Z` for a synthetic 2026 fall-back `01:30`
    /// anchor — the LATER of the two occurrences.
    ///
    /// `PrimaryAuctionClock.LocalToUtc` comments that it chooses "the earlier offset
    /// occurrence" and names the local `earlier`, but `offsets.Min()` returns `-5h`, so
    /// `local - (-5h)` lands on 06:30Z rather than 05:30Z. This test pins the behaviour as
    /// measured, not as described. It is deliberately not a fix: whether the code or the
    /// comment is wrong is a semantic decision that has not been made.
    ///
    /// The production 08:20 anchor never falls in an ambiguous interval, which is why this
    /// needs a synthetic config to reach at all.
    /// </summary>
    [Fact]
    public void T04_Ambiguous_fall_back_anchor_resolves_to_the_later_utc_occurrence()
    {
        var clock = new PrimaryAuctionClock(
            new PrimaryAuctionClockConfig(anchorLocalTime: new TimeSpan(1, 30, 0)));

        // Midday on the fall-back date, so the auction date is 2026-11-01 and its start is
        // the ambiguous 01:30 local.
        var point = clock.Resolve(new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 11, 1), point.LocalAuctionDate);
        Assert.Equal(new DateTime(2026, 11, 1, 6, 30, 0, DateTimeKind.Utc), point.AuctionStartUtc);
        Assert.NotEqual(new DateTime(2026, 11, 1, 5, 30, 0, DateTimeKind.Utc), point.AuctionStartUtc);
    }

    /// <summary>The production anchor is not ambiguous, so `T04` cannot affect it.</summary>
    [Fact]
    public void T05_The_production_anchor_never_falls_in_an_ambiguous_interval()
    {
        var tz = AuctionTimezoneResolver.Resolve(AuctionTimezoneResolver.IanaAmericaNewYork);

        for (var d = new DateTime(2026, 1, 1); d < new DateTime(2027, 1, 1); d = d.AddDays(1))
        {
            var anchor = d.Date + PrimaryAuctionClockConfig.DefaultAnchorLocalTime;
            Assert.False(tz.IsAmbiguousTime(anchor), $"08:20 was ambiguous on {d:yyyy-MM-dd}");
            Assert.False(tz.IsInvalidTime(anchor), $"08:20 was invalid on {d:yyyy-MM-dd}");
        }
    }

    // ========== CONF-001: representation, not behaviour ==========

    /// <summary>
    /// `08:20 America/New_York` is the same instant as `07:20 America/Chicago`, every day,
    /// including both DST transitions.
    ///
    /// This is what makes a Chicago venue label a representation change rather than a
    /// migration — and what makes `08:20 Chicago` a different decision entirely.
    /// </summary>
    [Fact]
    public void T06_0820_New_York_equals_0720_Chicago_and_differs_from_0820_Chicago()
    {
        var ny = AuctionTimezoneResolver.Resolve(AuctionTimezoneResolver.IanaAmericaNewYork);
        var chicago = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "Central Standard Time" : "America/Chicago");

        for (var d = new DateTime(2026, 1, 1); d < new DateTime(2027, 2, 5); d = d.AddDays(1))
        {
            var nyUtc = TimeZoneInfo.ConvertTimeToUtc(d.Date + new TimeSpan(8, 20, 0), ny);
            var chiUtc = TimeZoneInfo.ConvertTimeToUtc(d.Date + new TimeSpan(7, 20, 0), chicago);
            var chiSameClock = TimeZoneInfo.ConvertTimeToUtc(d.Date + new TimeSpan(8, 20, 0), chicago);

            Assert.Equal(nyUtc, chiUtc);
            Assert.Equal(TimeSpan.FromHours(1), chiSameClock - nyUtc);
        }
    }

    // ========== COL-01 / COL-02 / COL-03: the name is already taken ==========

    /// <summary>
    /// No new type or public property named `ScopeType` may appear.
    ///
    /// `EfficiencyScopeType` already means CurrentPrimaryAuction / ActiveEpisode /
    /// ClosedEpisode, three published snapshots expose a property literally called
    /// `ScopeType`, and the GPS card renders it. A session scope must pick a different name;
    /// this test is what makes that a build failure rather than a code-review opinion.
    /// </summary>
    [Fact]
    public void T07_No_new_ScopeType_type_or_property_is_introduced()
    {
        var assembly = typeof(PrimaryAuctionClock).Assembly;

        // GcAuctionFlowIndicator derives from an ATAS type that cannot load in the test host,
        // so GetTypes() throws with the loadable types attached. Taking those is the standard
        // reading, and it still covers every type this guard is about.
        Type[] loaded;
        try
        {
            loaded = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            loaded = ex.Types.Where(t => t is not null).Select(t => t!).ToArray();
        }

        Assert.NotEmpty(loaded);

        var types = loaded
            .Where(t => t.Name.Contains("ScopeType", StringComparison.Ordinal))
            .Select(t => t.FullName!)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(new[] { "GC.AuctionFlow.Efficiency.EfficiencyScopeType" }, types);

        // Properties named ScopeType must all still carry EfficiencyScopeType. A session
        // scope arriving under this name would change what the card renders.
        var declaring = loaded
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance
                                             | BindingFlags.DeclaredOnly))
            .Where(p => p.Name == "ScopeType")
            .ToArray();

        Assert.NotEmpty(declaring);
        Assert.All(declaring, p => Assert.Equal(typeof(EfficiencyScopeType), p.PropertyType));
    }

    /// <summary>`EfficiencyScopeType`'s members and values are unchanged.</summary>
    [Fact]
    public void T08_EfficiencyScopeType_members_are_unchanged()
    {
        Assert.Equal(0, (int)EfficiencyScopeType.CurrentPrimaryAuction);
        Assert.Equal(1, (int)EfficiencyScopeType.ActiveEpisode);
        Assert.Equal(2, (int)EfficiencyScopeType.ClosedEpisode);
        Assert.Equal(3, Enum.GetValues<EfficiencyScopeType>().Length);
    }

    /// <summary>
    /// The recorder's `SessionId` still means one ATAS run, and is still a `Guid`.
    ///
    /// `COL-05`: a trading-session identity must not reuse this name or this type.
    /// </summary>
    [Fact]
    public void T09_Recorder_SessionId_remains_a_run_scoped_guid()
    {
        var property = typeof(GC.AuctionFlow.Data.CapabilitySnapshot)
            .GetProperty("SessionId", BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(property);
        Assert.Equal(typeof(Guid), property!.PropertyType);
    }

    /// <summary>
    /// `COL-06`: composed identities still compose. `EffortResultIdentity` embeds the
    /// efficiency id, which embeds the auction id, so a change to one propagates into all of
    /// them.
    /// </summary>
    [Fact]
    public void T10_Composed_identities_are_unchanged()
    {
        var id = GC.AuctionFlow.EffortResult.EffortResultIdentity.BuildFromEfficiencyId(
            "EFF|PI-2026-07-27|EP-1", "TESTPOLICY");

        Assert.Equal("ERCL|EFF_PI-2026-07-27_EP-1|TESTPOLICY", id);
    }

    private static PrimaryAuctionProfileSnapshot Auction(string auctionId, DateTime startUtc) =>
        new(
            AuctionProfileState.Ready,
            auctionId,
            startUtc,
            startUtc.AddHours(24),
            isCompleted: true,
            tpoProfile: null,
            volumeProfile: null,
            profileHigh: null,
            profileLow: null,
            lastObservedPrice: null,
            lastUpdatedUtc: startUtc.AddHours(24),
            sourceBarRange: null,
            dataQuality: ProfileDataQuality.Complete,
            knownLimitations: Array.Empty<string>());
}
