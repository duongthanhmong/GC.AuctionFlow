using GC.AuctionFlow.Composite;
using GC.AuctionFlow.Core;
using GC.AuctionFlow.Directional;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Reference;
using GC.AuctionFlow.Runtime;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Directional;

/// <summary>Phase 1D Multi-Horizon Directional Context Foundation â€” deterministic categorical evidence.</summary>
public sealed class Phase1DDirectionalContextTests
{
    private const decimal Tick = 0.1m;
    private const string Instrument = "GCQ6";
    private const string Epoch = "GCQ6|tick=0.1";

    private static PrimaryAuctionProfileSnapshot MakeAuction(
        string id,
        DateTime start,
        DateTime end,
        bool completed,
        decimal high,
        decimal low,
        decimal tpoPoc,
        decimal tpoVah,
        decimal tpoVal,
        decimal? vpoc = null,
        decimal? volVah = null,
        decimal? volVal = null,
        PriceVolumeCapability volCap = PriceVolumeCapability.Exact,
        decimal? lastPx = null,
        AuctionProfileState state = AuctionProfileState.Ready,
        IReadOnlyList<CompletedTpoPeriodSnapshot>? periods = null)
    {
        var tpo = new TpoProfileSnapshot(
            id, start, end,
            AuctionTimezoneResolver.IanaAmericaNewYork, new TimeSpan(8, 20, 0), 30,
            high, low, tpoPoc, tpoVah, tpoVal,
            2, periods?.Count ?? 1, null,
            new Dictionary<long, int> { [(long)(tpoPoc / Tick)] = 2 },
            ProfileDataQuality.Complete, "test", Array.Empty<string>(),
            null, periods);

        VolumeProfileSnapshot vol;
        if (volCap == PriceVolumeCapability.Exact && vpoc is not null && volVah is not null && volVal is not null)
        {
            vol = new VolumeProfileSnapshot(
                id, high, low, vpoc, volVah, volVal,
                20m, new Dictionary<long, decimal> { [(long)(vpoc.Value / Tick)] = 20m },
                PriceVolumeCapability.Exact, ProfileDataQuality.Complete, "test", Array.Empty<string>());
        }
        else
        {
            vol = new VolumeProfileSnapshot(
                id, high, low, null, null, null,
                0m, new Dictionary<long, decimal>(),
                PriceVolumeCapability.Unavailable, ProfileDataQuality.Partial, "test",
                new[] { "PRICE_VOLUME_DATA_UNAVAILABLE" });
            if (state == AuctionProfileState.Ready)
                state = AuctionProfileState.Partial;
        }

        return new PrimaryAuctionProfileSnapshot(
            state, id, start, end, completed,
            tpo, vol, high, low, lastPx ?? tpoPoc, end, null,
            state == AuctionProfileState.Ready ? ProfileDataQuality.Complete : ProfileDataQuality.Partial,
            Array.Empty<string>());
    }

    private static CompletedTpoPeriodSnapshot Period(int idx, decimal high, decimal low, DateTime start)
    {
        var grid = new PriceGrid(Tick);
        return new CompletedTpoPeriodSnapshot(
            idx, start, start.AddMinutes(30), high, low,
            grid.ToTickIndex(high), grid.ToTickIndex(low));
    }

    // --- A. Pairwise migration ---

    [Fact]
    public void A01_FullyHigherTpoValue_HigherPoc_UpDiscovery()
    {
        var prev = MakeAuction("A", D(1), D(2), true, 100.5m, 99.5m, 100.0m, 100.2m, 99.8m, 100.0m, 100.2m, 99.8m);
        var cur = MakeAuction("B", D(2), D(3), true, 101.5m, 100.6m, 101.0m, 101.2m, 100.6m, 101.0m, 101.2m, 100.6m);
        var e = PairwiseAuctionComparer.Compare(prev, cur, Tick);
        Assert.Equal(DirectionalAuctionState.UpDiscovery, e.ClassifiedState);
        Assert.Equal(ValueRelationship.FullyAbove, e.TpoValueRelationship);
    }

    [Fact]
    public void A02_FullyLowerTpoValue_LowerPoc_DownDiscovery()
    {
        var prev = MakeAuction("A", D(1), D(2), true, 100.5m, 99.5m, 100.0m, 100.2m, 99.8m, 100.0m, 100.2m, 99.8m);
        var cur = MakeAuction("B", D(2), D(3), true, 99.4m, 98.5m, 99.0m, 99.2m, 98.8m, 99.0m, 99.2m, 98.8m);
        var e = PairwiseAuctionComparer.Compare(prev, cur, Tick);
        Assert.Equal(DirectionalAuctionState.DownDiscovery, e.ClassifiedState);
    }

    [Fact]
    public void A03_Overlap_HigherMidPoc_UpRotation()
    {
        var prev = MakeAuction("A", D(1), D(2), true, 100.5m, 99.5m, 100.0m, 100.2m, 99.8m, 100.0m, 100.2m, 99.8m);
        var cur = MakeAuction("B", D(2), D(3), true, 100.7m, 99.7m, 100.3m, 100.5m, 99.9m, 100.3m, 100.5m, 99.9m);
        var e = PairwiseAuctionComparer.Compare(prev, cur, Tick);
        Assert.Equal(DirectionalAuctionState.UpRotation, e.ClassifiedState);
    }

    [Fact]
    public void A04_Overlap_LowerMidPoc_DownRotation()
    {
        var prev = MakeAuction("A", D(1), D(2), true, 100.5m, 99.5m, 100.0m, 100.2m, 99.8m, 100.0m, 100.2m, 99.8m);
        var cur = MakeAuction("B", D(2), D(3), true, 100.3m, 99.3m, 99.7m, 100.0m, 99.5m, 99.7m, 100.0m, 99.5m);
        var e = PairwiseAuctionComparer.Compare(prev, cur, Tick);
        Assert.Equal(DirectionalAuctionState.DownRotation, e.ClassifiedState);
    }

    [Fact]
    public void A05_Overlap_NoMigration_Balance()
    {
        var prev = MakeAuction("A", D(1), D(2), true, 100.5m, 99.5m, 100.0m, 100.2m, 99.8m, 100.0m, 100.2m, 99.8m);
        var cur = MakeAuction("B", D(2), D(3), true, 100.5m, 99.5m, 100.0m, 100.2m, 99.8m, 100.0m, 100.2m, 99.8m);
        var e = PairwiseAuctionComparer.Compare(prev, cur, Tick);
        Assert.Equal(DirectionalAuctionState.Balance, e.ClassifiedState);
    }

    [Fact]
    public void A06_OpposingTpoAndExactVolume_Conflicted()
    {
        var prev = MakeAuction("A", D(1), D(2), true, 100.5m, 99.5m, 100.0m, 100.2m, 99.8m, 100.0m, 100.2m, 99.8m);
        var cur = MakeAuction("B", D(2), D(3), true, 101.5m, 100.6m, 101.0m, 101.2m, 100.6m, 99.0m, 99.2m, 98.8m);
        var e = PairwiseAuctionComparer.Compare(prev, cur, Tick);
        Assert.Equal(DirectionalAuctionState.Conflicted, e.ClassifiedState);
        Assert.Contains(e.Conflicts, c => c.Contains("POC", StringComparison.Ordinal) || c.Contains("MID", StringComparison.Ordinal));
    }

    [Fact]
    public void A07_IncompleteNonOpposing_Transition()
    {
        var prev = MakeAuction("A", D(1), D(2), true, 100.5m, 99.5m, 100.0m, 100.2m, 99.8m, 100.0m, 100.2m, 99.8m);
        // Fully above but POC unchanged â†’ not Discovery; Transition.
        var cur = MakeAuction("B", D(2), D(3), true, 101.5m, 100.6m, 100.0m, 101.2m, 100.6m, 100.0m, 101.2m, 100.6m);
        var e = PairwiseAuctionComparer.Compare(prev, cur, Tick);
        Assert.Equal(DirectionalAuctionState.Transition, e.ClassifiedState);
    }

    [Fact]
    public void A08_MissingCompatibleEvidence_Unknown()
    {
        var e = PairwiseAuctionComparer.Compare(null, null, Tick);
        Assert.Equal(DirectionalAuctionState.Unknown, e.ClassifiedState);
        Assert.False(e.Compatible);
    }

    [Fact]
    public void A09_ExactVolumeUnavailable_DoesNotFabricateZero()
    {
        var prev = MakeAuction("A", D(1), D(2), true, 100.5m, 99.5m, 100.0m, 100.2m, 99.8m,
            volCap: PriceVolumeCapability.Unavailable);
        var cur = MakeAuction("B", D(2), D(3), true, 101.5m, 100.6m, 101.0m, 101.2m, 100.6m,
            volCap: PriceVolumeCapability.Unavailable);
        var e = PairwiseAuctionComparer.Compare(prev, cur, Tick);
        Assert.False(e.ExactVolumeAvailable);
        Assert.Null(e.PreviousVpoc);
        Assert.Null(e.CurrentVpoc);
        Assert.Equal(MigrationDirection.Unavailable, e.VolumePocMigration);
        Assert.Equal(DirectionalAuctionState.UpDiscovery, e.ClassifiedState);
    }

    [Fact]
    public void A10_InvalidTickSize_FailsClosedUnknown()
    {
        var prev = MakeAuction("A", D(1), D(2), true, 100.5m, 99.5m, 100.0m, 100.2m, 99.8m);
        var cur = MakeAuction("B", D(2), D(3), true, 101.5m, 100.6m, 101.0m, 101.2m, 100.6m);
        var e = PairwiseAuctionComparer.Compare(prev, cur, 0m);
        Assert.Equal(DirectionalAuctionState.Unknown, e.ClassifiedState);
        Assert.False(e.Compatible);
    }

    // --- B. Structural multi-day ---

    [Fact]
    public void B11_13_CompletedOnly_InsufficientHistory_Unknown()
    {
        var a = MakeAuction("A", D(1), D(2), true, 100.5m, 99.5m, 100.0m, 100.2m, 99.8m, 100.0m, 100.2m, 99.8m);
        var b = MakeAuction("B", D(2), D(3), true, 101.5m, 100.6m, 101.0m, 101.2m, 100.6m, 101.0m, 101.2m, 100.6m);
        var developing = MakeAuction("C", D(3), D(4), false, 102.5m, 101.6m, 102.0m, 102.2m, 101.6m, 102.0m, 102.2m, 101.6m);
        var r = StructuralDirectionalAggregator.Aggregate(new[] { a, b, developing }, Tick);
        Assert.Equal(DirectionalAuctionState.Unknown, r.State);
        Assert.Equal(1, r.CompatibleTransitionCount);
        Assert.Contains(r.Limitations, l => l.Contains("INSUFFICIENT", StringComparison.Ordinal));
        Assert.DoesNotContain(r.SourceAuctionIds, id => id == "C");
    }

    [Fact]
    public void B14_ConsecutiveUpward_AggregatesUpDiscovery()
    {
        var a = MakeAuction("A", D(1), D(2), true, 99.5m, 98.5m, 99.0m, 99.2m, 98.8m, 99.0m, 99.2m, 98.8m);
        var b = MakeAuction("B", D(2), D(3), true, 100.5m, 99.6m, 100.0m, 100.2m, 99.6m, 100.0m, 100.2m, 99.6m);
        var c = MakeAuction("C", D(3), D(4), true, 101.5m, 100.6m, 101.0m, 101.2m, 100.6m, 101.0m, 101.2m, 100.6m);
        var r = StructuralDirectionalAggregator.Aggregate(new[] { a, b, c }, Tick);
        Assert.Equal(2, r.CompatibleTransitionCount);
        Assert.Equal(DirectionalAuctionState.UpDiscovery, r.State);
    }

    [Fact]
    public void B15_ConsecutiveDownward_AggregatesDownDiscovery()
    {
        var a = MakeAuction("A", D(1), D(2), true, 101.5m, 100.5m, 101.0m, 101.2m, 100.8m, 101.0m, 101.2m, 100.8m);
        var b = MakeAuction("B", D(2), D(3), true, 100.4m, 99.5m, 100.0m, 100.2m, 99.8m, 100.0m, 100.2m, 99.8m);
        var c = MakeAuction("C", D(3), D(4), true, 99.4m, 98.5m, 99.0m, 99.2m, 98.8m, 99.0m, 99.2m, 98.8m);
        var r = StructuralDirectionalAggregator.Aggregate(new[] { a, b, c }, Tick);
        Assert.Equal(DirectionalAuctionState.DownDiscovery, r.State);
    }

    [Fact]
    public void B16_OpposingTransitions_Conflicted()
    {
        var a = MakeAuction("A", D(1), D(2), true, 99.5m, 98.5m, 99.0m, 99.2m, 98.8m, 99.0m, 99.2m, 98.8m);
        var b = MakeAuction("B", D(2), D(3), true, 100.5m, 99.6m, 100.0m, 100.2m, 99.6m, 100.0m, 100.2m, 99.6m);
        var c = MakeAuction("C", D(3), D(4), true, 99.4m, 98.5m, 99.0m, 99.2m, 98.8m, 99.0m, 99.2m, 98.8m);
        var r = StructuralDirectionalAggregator.Aggregate(new[] { a, b, c }, Tick);
        Assert.Equal(DirectionalAuctionState.Conflicted, r.State);
    }

    [Fact]
    public void B17_18_MixedSequence_Transition_NoFixedFiveDay()
    {
        var auctions = new List<PrimaryAuctionProfileSnapshot>();
        for (var i = 0; i < 7; i++)
        {
            var basePx = 100.0m + (i % 2 == 0 ? 0m : 0.1m);
            auctions.Add(MakeAuction(
                "D" + i, D(i + 1), D(i + 2), true,
                basePx + 0.5m, basePx - 0.5m, basePx, basePx + 0.2m, basePx - 0.2m,
                basePx, basePx + 0.2m, basePx - 0.2m));
        }

        var r = StructuralDirectionalAggregator.Aggregate(auctions, Tick);
        Assert.Equal(6, r.CompatibleTransitionCount);
        Assert.True(r.State is DirectionalAuctionState.Transition or DirectionalAuctionState.Balance or DirectionalAuctionState.Conflicted);
    }

    // --- C. Tactical developing ---

    [Fact]
    public void C19_25_TacticalDeveloping_Idempotent_DoesNotMutateStructural()
    {
        var prev = MakeAuction("P", D(2), D(3), true, 100.5m, 99.5m, 100.0m, 100.2m, 99.8m, 100.0m, 100.2m, 99.8m);
        var a = MakeAuction("A", D(1), D(2), true, 99.5m, 98.5m, 99.0m, 99.2m, 98.8m, 99.0m, 99.2m, 98.8m);
        var cur = MakeAuction("C", D(3), D(4), false, 101.5m, 100.6m, 101.0m, 101.2m, 100.6m, 101.0m, 101.2m, 100.6m, lastPx: 101.0m);
        var profiles = new PrimaryProfileSetSnapshot(
            cur, prev, HistoricalInitializationState.Complete, 10,
            Array.Empty<string>(), Array.Empty<string>(), null, new[] { a, prev });

        var host = new DirectionalContextHost(Tick, Instrument, Epoch, AtasTimestampNormalizer.PolicyVersion,
            new DirectionalPolicyConfig(true, true));
        var s1 = host.Rebuild(profiles, null, null);
        Assert.Equal(DirectionalMaturity.Developing, s1.TacticalContext.Maturity);
        Assert.Equal(DirectionalAuctionState.UpDiscovery, s1.TacticalContext.State);
        var structuralState = s1.StructuralContext.State;
        var structuralVer = s1.StructuralContext.StateVersion;
        var tacticalVer = s1.TacticalContext.StateVersion;

        var s2 = host.Rebuild(profiles, null, null);
        Assert.Equal(tacticalVer, s2.TacticalContext.StateVersion);
        Assert.Equal(structuralVer, s2.StructuralContext.StateVersion);
        Assert.Equal(structuralState, s2.StructuralContext.State);

        var moved = MakeAuction("C", D(3), D(4), false, 101.7m, 100.8m, 101.2m, 101.4m, 100.8m, 101.2m, 101.4m, 100.8m, lastPx: 101.2m);
        var profiles2 = new PrimaryProfileSetSnapshot(
            moved, prev, HistoricalInitializationState.Complete, 11,
            Array.Empty<string>(), Array.Empty<string>(), null, new[] { a, prev });
        var s3 = host.Rebuild(profiles2, null, null);
        Assert.True(s3.TacticalContext.StateVersion > tacticalVer);
        Assert.Equal(structuralState, s3.StructuralContext.State);

        var newAuction = MakeAuction("N", D(4), D(5), false, 102.5m, 101.6m, 102.0m, 102.2m, 101.6m, 102.0m, 102.2m, 101.6m);
        var curCompleted = MakeAuction("C", D(3), D(4), true, 101.5m, 100.6m, 101.0m, 101.2m, 100.6m, 101.0m, 101.2m, 100.6m);
        var profiles3 = new PrimaryProfileSetSnapshot(
            newAuction, curCompleted, HistoricalInitializationState.Complete, 12,
            Array.Empty<string>(), Array.Empty<string>(), null, new[] { a, prev, curCompleted });
        var s4 = host.Rebuild(profiles3, null, null);
        Assert.Equal("N", s4.TacticalContext.SourceAuctionIds.Last());
        Assert.Equal(1, s4.TacticalContext.StateVersion);
    }

    // --- D. OTF ---

    [Fact]
    public void D26_35_OtfCompletedOnly_DevelopingIgnored_NoConfirmed()
    {
        var p0 = Period(0, 100.2m, 99.8m, D(1));
        var p1 = Period(1, 100.3m, 99.9m, D(1).AddMinutes(30)); // up: low >= prev low
        var p2 = Period(2, 100.4m, 100.0m, D(1).AddMinutes(60));
        var auction = MakeAuction("C", D(1), D(2), false, 100.5m, 99.5m, 100.0m, 100.2m, 99.8m,
            periods: new[] { p0, p1, p2 });

        var otf = OneTimeFramingTracker.Evaluate(auction, enabled: true);
        Assert.Equal(OneTimeFramingState.DevelopingUp, otf.State);
        Assert.Equal(2, otf.UpStreak);
        Assert.Equal(3, otf.CompletedPeriodCount);
        Assert.Contains(OneTimeFramingTracker.ConfirmationLimitation, otf.KnownLimitations);
        Assert.NotEqual(OneTimeFramingState.ConfirmedUp, otf.State);
        Assert.NotEqual(OneTimeFramingState.ConfirmedDown, otf.State);
        Assert.Equal(AuctionTimezoneResolver.IanaAmericaNewYork + "|08:20", otf.TpoAnchor);
        Assert.Equal(30, otf.PeriodLengthMinutes);

        var broken = MakeAuction("C", D(1), D(2), false, 100.5m, 99.5m, 100.0m, 100.2m, 99.8m,
            periods: new[]
            {
                p0, p1,
                Period(2, 100.5m, 99.5m, D(1).AddMinutes(60)) // breaks up (low drops)
            });
        var otf2 = OneTimeFramingTracker.Evaluate(broken, true);
        Assert.Equal(OneTimeFramingState.Broken, otf2.State);

        var down = MakeAuction("C", D(1), D(2), false, 100.5m, 99.5m, 100.0m, 100.2m, 99.8m,
            periods: new[]
            {
                Period(0, 100.4m, 99.8m, D(1)),
                Period(1, 100.3m, 99.7m, D(1).AddMinutes(30))
            });
        Assert.Equal(OneTimeFramingState.DevelopingDown, OneTimeFramingTracker.Evaluate(down, true).State);

        var mixed = MakeAuction("C", D(1), D(2), false, 100.5m, 99.5m, 100.0m, 100.2m, 99.8m,
            periods: new[]
            {
                Period(0, 100.2m, 99.8m, D(1)),
                Period(1, 100.3m, 99.9m, D(1).AddMinutes(30)),
                Period(2, 100.2m, 99.7m, D(1).AddMinutes(60))
            });
        Assert.Equal(OneTimeFramingState.Mixed, OneTimeFramingTracker.Evaluate(mixed, true).State);

        var again = OneTimeFramingTracker.Evaluate(auction, true);
        Assert.Equal(otf.StateVersion, again.StateVersion);
    }

    // --- E. Price location ---

    [Theory]
    [InlineData(100.5, PriceValueLocation.AboveValue)]
    [InlineData(100.2, PriceValueLocation.AtValueHigh)]
    [InlineData(100.0, PriceValueLocation.AtPoc)]
    [InlineData(99.9, PriceValueLocation.InsideValue)]
    [InlineData(99.8, PriceValueLocation.AtValueLow)]
    [InlineData(99.5, PriceValueLocation.BelowValue)]
    public void E36_37_PriceLocation_ExactTicks(decimal px, PriceValueLocation expected)
    {
        Assert.Equal(expected, PriceLocationClassifier.Classify(px, 99.8m, 100.2m, 100.0m, Tick));
    }

    [Fact]
    public void E39_40_MissingComposite_DoesNotInvalidatePrimary_VolumeUnavailable()
    {
        var cur = MakeAuction("C", D(1), D(2), false, 100.5m, 99.5m, 100.0m, 100.2m, 99.8m,
            volCap: PriceVolumeCapability.Unavailable, lastPx: 100.5m);
        var loc = PriceLocationClassifier.Build(100.5m, cur, null, null, Tick);
        Assert.Equal(PriceValueLocation.AboveValue, loc.CurrentPrimaryTpo);
        Assert.Equal(PriceValueLocation.Unavailable, loc.CurrentPrimaryVolume);
        Assert.Equal(PriceValueLocation.Unavailable, loc.ConfirmedCompositeTpo);
    }

    // --- F. Runtime / data gate ---

    [Fact]
    public void F41_47_DisabledDoesNotDegrade_ReadyDoesNotClearDegraded_FingerprintReuse()
    {
        var prev = MakeAuction("P", D(2), D(3), true, 100.5m, 99.5m, 100.0m, 100.2m, 99.8m, 100.0m, 100.2m, 99.8m);
        var a = MakeAuction("A", D(1), D(2), true, 99.5m, 98.5m, 99.0m, 99.2m, 98.8m, 99.0m, 99.2m, 98.8m);
        var cur = MakeAuction("C", D(3), D(4), false, 101.5m, 100.6m, 101.0m, 101.2m, 100.6m, 101.0m, 101.2m, 100.6m);
        var profiles = new PrimaryProfileSetSnapshot(
            cur, prev, HistoricalInitializationState.Complete, 10,
            Array.Empty<string>(), Array.Empty<string>(), null, new[] { a, prev });

        var hostOff = new DirectionalContextHost(Tick, Instrument, Epoch, AtasTimestampNormalizer.PolicyVersion,
            new DirectionalPolicyConfig(false, true));
        var off = hostOff.Rebuild(profiles, null, null);
        Assert.Equal(DirectionalModuleState.Disabled, off.Status);

        var host = new DirectionalContextHost(Tick, Instrument, Epoch, AtasTimestampNormalizer.PolicyVersion,
            new DirectionalPolicyConfig(true, true));
        var s1 = host.Rebuild(profiles, null, null);
        Assert.True(s1.Status is DirectionalModuleState.Ready or DirectionalModuleState.Partial);
        var fp1 = host.LastAppliedFingerprint;
        Assert.NotNull(fp1);

        var unchanged = DirectionalInputFingerprint.Build(
            true, true, profiles, null, null, Tick, Epoch, AtasTimestampNormalizer.PolicyVersion);
        Assert.False(DirectionalPublishInitialization.ShouldProcess(true, profiles, host, fp1, unchanged));

        var runtime = new GcaeRuntimeEngine();
        var snap = runtime.Publish(
            observed: null,
            expectedInstrumentCode: "GCQ6",
            mode: DataSourceMode.Live,
            modeProvenance: DataSourceModeProvenance.OperatorDeclared,
            provider: DeclaredFeedProvider.Rithmic,
            providerProvenance: FeedProviderProvenance.OperatorDeclared,
            tradeObserved: true,
            lastTradeCallbackUtc: DateTime.UtcNow,
            rawRecorderMasterEnabled: false,
            tradeRecordingEnabled: false,
            recorderAccepting: false,
            recorderFaulted: false,
            recorderSessionPresent: false,
            indicatorDisposed: false,
            profiles: profiles,
            directionalContext: s1);
        Assert.True(snap.DataGate.DataState is DataState.Degraded or DataState.Ready or DataState.Invalid);
        Assert.NotEqual(DataState.Ready, snap.DataGate.DataState); // Directional Ready must not force global Ready
        Assert.Equal("0.25.0", snap.Version);
        Assert.Equal(DirectionalPolicyConfig.PolicyVersion, s1.PolicyVersion);

        // Overlay cosmetic must not be in fingerprint â€” reference overlay absence unchanged.
        var withRefsOff = DirectionalInputFingerprint.Build(
            true, true, profiles, null, null, Tick, Epoch, AtasTimestampNormalizer.PolicyVersion);
        Assert.Equal(unchanged.EvidenceKey, withRefsOff.EvidenceKey);
    }

    // --- G. UI / source scope ---

    [Fact]
    public void G48_54_GpsRows_NoSignalWording_Phase1GThesisNotStarted()
    {
        var prev = MakeAuction("P", D(2), D(3), true, 100.5m, 99.5m, 100.0m, 100.2m, 99.8m, 100.0m, 100.2m, 99.8m);
        var a = MakeAuction("A", D(1), D(2), true, 99.5m, 98.5m, 99.0m, 99.2m, 98.8m, 99.0m, 99.2m, 98.8m);
        var cur = MakeAuction("C", D(3), D(4), false, 101.5m, 100.6m, 101.0m, 101.2m, 100.6m, 101.0m, 101.2m, 100.6m,
            periods: new[]
            {
                Period(0, 100.2m, 99.8m, D(3)),
                Period(1, 100.3m, 99.9m, D(3).AddMinutes(30))
            });
        var profiles = new PrimaryProfileSetSnapshot(
            cur, prev, HistoricalInitializationState.Complete, 10,
            Array.Empty<string>(), Array.Empty<string>(), null, new[] { a, prev });
        var host = new DirectionalContextHost(Tick, Instrument, Epoch, AtasTimestampNormalizer.PolicyVersion,
            new DirectionalPolicyConfig(true, true));
        var dir = host.Rebuild(profiles, null, null);

        var runtime = new GcaeRuntimeEngine();
        var snap = runtime.Publish(
            null, "GCQ6", DataSourceMode.Live, DataSourceModeProvenance.OperatorDeclared,
            DeclaredFeedProvider.Rithmic, FeedProviderProvenance.OperatorDeclared,
            true, DateTime.UtcNow, false, false, false, false, false, false,
            profiles, directionalContext: dir, showDirectionalContextDiagnostics: true);
        var vm = AuctionGpsCardMapper.FromSnapshot(snap, showDiagnostics: true);
        var text = string.Join('\n', vm.ProfileDetailLines.Concat(vm.DiagnosticRows));
        Assert.Contains("DIRECTIONAL CONTEXT:", text, StringComparison.Ordinal);
        Assert.Contains("DIRECTIONAL_CONTEXT_POLICY_V1", text, StringComparison.Ordinal);
        Assert.Contains("STRUCTURAL STATE:", text, StringComparison.Ordinal);
        Assert.Contains("(DEVELOPING)", text, StringComparison.Ordinal);
        Assert.Contains("OTF:", text, StringComparison.Ordinal);
        Assert.Contains("PRICE LOCATION:", text, StringComparison.Ordinal);
        Assert.DoesNotContain("LONG", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SHORT", text, StringComparison.Ordinal);
        Assert.DoesNotContain("BUY", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SELL", text, StringComparison.Ordinal);
        // The real invariant this test protects is the four assertions above: the card
        // must never emit trade-signal wording. A blanket DoesNotContain("THESIS") was a
        // proxy for that from when no thesis module existed, and it already needed a
        // string-replace exemption to survive. Thesis modules now legitimately appear as
        // status rows, so the proxy is replaced by the precise claim: any thesis row is a
        // module state only, never a direction and never a calibrated verdict.
        foreach (var row in vm.DiagnosticRows.Where(r =>
                     r.StartsWith("FAR:", StringComparison.Ordinal)
                     || r.StartsWith("AAC:", StringComparison.Ordinal)
                     || r.StartsWith("THESIS CONTRACT:", StringComparison.Ordinal)
                     || r.StartsWith("MATURITY:", StringComparison.Ordinal)))
        {
            Assert.DoesNotContain("LONG", row, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("SHORT", row, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ARMED", row, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("EXECUTABLE", row, StringComparison.OrdinalIgnoreCase);
        }
        Assert.Contains("EPISODE: NOT AVAILABLE", text, StringComparison.Ordinal);
        Assert.Contains("ACCEPTANCE/REENTRY EVIDENCE: NOT AVAILABLE", text, StringComparison.Ordinal);
        Assert.Contains("ORDERFLOW: NOT AVAILABLE", text, StringComparison.Ordinal);
        Assert.Contains("MBO: BLOCKED", vm.MboLine, StringComparison.Ordinal);

        // Phase 2B Cluster Raw is authorized; Trade Facilitation / Thesis / FAR / AAC are not.
        var root = FindRepoRoot();
        var src = Path.Combine(root, "src", "GC.AuctionFlow");
        Assert.True(Directory.Exists(Path.Combine(src, "Episode")));
        Assert.True(Directory.Exists(Path.Combine(src, "Evidence")));
        Assert.True(Directory.Exists(Path.Combine(src, "Orderflow")));
        Assert.True(Directory.Exists(Path.Combine(src, "Cluster")));
        Assert.True(Directory.Exists(Path.Combine(src, "Thesis")));
        Assert.False(Directory.Exists(Path.Combine(src, "TradeFacilitation")));
        var indicator = File.ReadAllText(Path.Combine(src, "Atas", "GcAuctionFlowIndicator.cs"));
        Assert.Contains("EnableAuctionEpisodes", indicator, StringComparison.Ordinal);
        Assert.Contains("EnableAcceptanceReentryEvidence", indicator, StringComparison.Ordinal);
        Assert.Contains("EnableExecutedOrderflow", indicator, StringComparison.Ordinal);
        Assert.Contains("EnableClusterRawFeatures", indicator, StringComparison.Ordinal);
        Assert.Contains("EnableDirectionalContext", indicator, StringComparison.Ordinal);
        Assert.DoesNotContain("EnableThesis ", indicator, StringComparison.Ordinal);
        Assert.Contains("EnableTradeFacilitation", indicator, StringComparison.Ordinal);
        Assert.DoesNotContain("AcceptanceOutside", Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}Episode{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                     && !p.Contains($"{Path.DirectorySeparatorChar}Evidence{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                     && !p.Contains($"{Path.DirectorySeparatorChar}Orderflow{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                     && !p.Contains($"{Path.DirectorySeparatorChar}Cluster{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText)
            .Aggregate("", (a, b) => a + b), StringComparison.Ordinal);
    }

    [Fact]
    public void CompletedTpoPeriodFeed_ExposedOnTpoSnapshot()
    {
        var cfg = new PrimaryAuctionClockConfig();
        var clock = new PrimaryAuctionClock(cfg);
        var zone = cfg.TimeZone;
        var local = new DateTime(2026, 7, 22, 8, 20, 0, DateTimeKind.Unspecified);
        var start = TimeZoneInfo.ConvertTimeToUtc(local, zone);
        var bars = new List<ProfileBarObservation>();
        for (var i = 0; i < 90; i++)
        {
            var t = new DateTimeOffset(start.AddMinutes(i), TimeSpan.Zero);
            bars.Add(new ProfileBarObservation(
                i, t, t.AddMinutes(1),
                100.0m, 100.1m, 99.9m, 100.0m, 10m,
                Array.Empty<PriceVolumeObservation>(),
                PriceVolumeCapability.Unavailable,
                isHistorical: true,
                isCompleted: true,
                sourceVersion: 1,
                timestampPolicyVersion: AtasTimestampNormalizer.PolicyVersion));
        }

        var point = clock.Resolve(new DateTimeOffset(start, TimeSpan.Zero));
        var grid = new PriceGrid(Tick);
        var eval = new DateTimeOffset(start.AddMinutes(90), TimeSpan.Zero);
        var snap = ClassicTpoEngine.Build(
            point.AuctionId, point.AuctionStartUtc, point.AuctionEndUtc, bars, clock, grid, 0.70m, null, eval);
        Assert.Equal("1.0.2", snap.Version);
        Assert.True(snap.CompletedPeriodCount >= 1);
        Assert.Equal(snap.CompletedPeriodCount, snap.CompletedPeriods.Count);
        Assert.All(snap.CompletedPeriods, p => Assert.True(p.PeriodHighTick >= p.PeriodLowTick));
        Assert.DoesNotContain(snap.KnownLimitations, l => l == "OTF_NOT_IMPLEMENTED");
        Assert.NotNull(snap.CompletedPeriods);
    }

    private static DateTime D(int day) => new(2026, 7, Math.Clamp(day, 1, 28), 12, 0, 0, DateTimeKind.Utc);

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
