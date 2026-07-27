using System.Reflection;
using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Reference;
using GC.AuctionFlow.Research;
using GC.AuctionFlow.Runtime;
using GC.AuctionFlow.UI;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Research;

/// <summary>
/// Phase 5A Historical Scanner (v1.2 §46, v1.3 §14.1).
///
/// This module is the project's single remaining unlock, which makes it the one most
/// worth constraining. `G-CAL-001` forbids unlocking any `[C]` state with a threshold
/// chosen by whoever wrote the code, and the most likely way that rule gets broken is not
/// malice but drift: a percentile helper added for convenience, a default sample size, a
/// study that reports a base rate "just for reference".
///
/// So most of what follows asserts absence.
/// </summary>
public sealed class Phase5AHistoricalScannerTests
{
    private static DateTime Utc(int sec = 0) =>
        new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc).AddSeconds(sec);

    /// <summary>
    /// ATAS.Indicators cannot load in the test host, so GetTypes throws part-way through
    /// the engine assembly. The partial result is still complete for every type this file
    /// asks about — none of them derive from an ATAS type.
    /// </summary>
    private static IEnumerable<Type> EngineTypes
    {
        get
        {
            var engine = typeof(HistoricalScannerHost).Assembly;
            try { return engine.GetTypes(); }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t is not null).Select(t => t!);
            }
        }
    }

    private static HistoricalScannerHost Host() =>
        new(new HistoricalScannerPolicyConfig(enabled: true));

    private static AuctionEpisodeSnapshot Episode(
        string id,
        ReferenceType type = ReferenceType.PreviousPrimaryTpoPoc,
        EpisodeDataQuality quality = EpisodeDataQuality.Complete,
        EpisodeState state = EpisodeState.EpisodeExpired,
        AggressorEvidenceAvailability aggressor = AggressorEvidenceAvailability.Available,
        EpisodeInteractionDirection direction = EpisodeInteractionDirection.Up,
        long above = 12,
        long below = 4) =>
        new(
            episodeId: id,
            policyVersion: "EPISODE_POLICY_V1",
            primaryAuctionId: "PI-2026-07-26",
            referenceId: "REF-1",
            referenceType: type,
            referenceRole: ReferenceInteractionRole.Centerline,
            referencePriceTick: 40_930,
            referencePrice: 4093.0m,
            referenceMaturity: ReferenceMaturity.Confirmed,
            sourceHorizon: ReferenceSourceHorizon.PreviousPrimaryAuction,
            directionalContextProvenance: null,
            interactionDirection: direction,
            state: state,
            resolution: EpisodeResolution.Expired,
            startedAtUtc: Utc(),
            lastUpdatedAtUtc: Utc(30),
            firstInteractionEventId: "E1",
            lastProcessedEventId: "E9",
            attemptCount: 2,
            interactionCount: 5,
            crossCount: 1,
            upExcursionCount: 3,
            downExcursionCount: 1,
            maximumAboveDistanceTicks: above,
            maximumBelowDistanceTicks: below,
            maximumCanonicalOutsideDistanceTicks: 9,
            canonicalOutsideDuration: TimeSpan.FromSeconds(20),
            canonicalOutsideExecutedVolume: 400m,
            canonicalOutsideTradeCount: 25,
            canonicalOutsideBidVolume: 180m,
            canonicalOutsideAskVolume: 220m,
            canonicalOutsideDelta: 40m,
            aggressorEvidenceAvailability: aggressor,
            localPoc: null,
            stateVersion: 7,
            eventRevision: 11,
            dataQuality: quality,
            limitations: Array.Empty<string>());

    private static AuctionEpisodeSetSnapshot Closed(params AuctionEpisodeSnapshot[] episodes) =>
        new(
            moduleState: EpisodeModuleState.Ready,
            policyVersion: "EPISODE_POLICY_V1",
            historyMode: EpisodeHistoryMode.LiveOnly,
            primaryAuctionId: "PI-2026-07-26",
            eligibleReferenceCount: 16,
            interactedReferenceCount: 3,
            activeEpisodes: Array.Empty<AuctionEpisodeSnapshot>(),
            recentlyClosedEpisodes: episodes,
            latestUpdatedEpisode: episodes.LastOrDefault(),
            episodeCountsByState: new Dictionary<EpisodeState, int>(),
            inputFingerprint: "fp",
            registryRevision: 1,
            createdAtUtc: Utc(),
            lastUpdatedAtUtc: Utc(30),
            limitations: Array.Empty<string>());

    // ========== A: the scanner derives nothing ==========

    [Fact]
    public void A01_PolicyVersion_is_historical_scanner_v1() =>
        Assert.Equal("HISTORICAL_SCANNER_POLICY_V1", HistoricalScannerPolicyConfig.PolicyVersion);

    /// <summary>
    /// No numeric constant that could act as a threshold.
    ///
    /// The retention capacity is an int and is exempt by name, because it bounds memory
    /// rather than gating a decision. Anything decimal or double would have no such
    /// excuse — a percentile, a minimum sample, a tolerance.
    /// </summary>
    [Fact]
    public void A02_No_threshold_constant_exists()
    {
        foreach (var type in new[]
                 {
                     typeof(HistoricalScannerPolicyConfig), typeof(CalibrationProtocol),
                     typeof(ScannerStudyRegistry), typeof(HistoricalScannerSnapshot),
                 })
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.FieldType == typeof(string) || field.FieldType.IsArray) continue;
            if (field.FieldType == typeof(decimal) || field.FieldType == typeof(double))
                Assert.Fail(type.Name + "." + field.Name + " is a numeric threshold");

            if (field.FieldType == typeof(int))
                Assert.True(
                    field.Name is "RowCapacity" or "TotalSteps",
                    type.Name + "." + field.Name + " is an unexplained integer constant");
        }
    }

    /// <summary>
    /// No member may compute a statistic.
    ///
    /// A percentile or base rate on this module would be step two of the protocol done
    /// inside the DLL, on axes that do not exist, and the result would look like an
    /// observation rather than the coder-chosen number it would actually be.
    /// </summary>
    [Fact]
    public void A03_No_statistic_is_computed_anywhere_in_the_module()
    {
        var banned = new[] { "Percentile", "Median", "Mean", "Average", "StdDev", "BaseRate", "Quantile", "Threshold" };

        // Listed explicitly rather than reflected out of the assembly: enumerating members
        // across a partially-loadable assembly resolves signatures and trips over the
        // absent ATAS reference. Enum types are excluded deliberately — IbWidthPercentile
        // names a study that is blocked, not a percentile anyone computed.
        var moduleTypes = new[]
        {
            typeof(HistoricalScannerHost), typeof(HistoricalScannerSnapshot),
            typeof(HistoricalScannerPolicyConfig), typeof(CalibrationProtocol),
            typeof(StratificationAxisStatus), typeof(ScannerStudy),
            typeof(ScannerStudyRegistry), typeof(EpisodeDatasetRecord),
        };

        foreach (var type in moduleTypes)
        {
            Assert.Equal("GC.AuctionFlow.Research", type.Namespace);

            foreach (var member in type.GetMembers(
                         BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                // Limitation constants declare an absence. LimitationNoThresholdDerived
                // is the module saying it derives no threshold, not one it derived.
                if (member.Name.StartsWith("Limitation", StringComparison.Ordinal)) continue;

                foreach (var word in banned)
                    Assert.False(member.Name.Contains(word, StringComparison.OrdinalIgnoreCase),
                        type.Name + "." + member.Name + " computes a statistic; that is step "
                        + "two of G-CAL-002 and it does not happen in this build");
            }
        }
    }

    // ========== B: the calibration gate cannot open ==========

    [Fact]
    public void B01_Unlock_is_never_permitted()
    {
        Assert.False(CalibrationProtocol.Evaluate(hasCollectedRows: false).UnlockPermitted);
        Assert.False(CalibrationProtocol.Evaluate(hasCollectedRows: true).UnlockPermitted);
    }

    /// <summary>
    /// The protocol blocks at step two, not step three.
    ///
    /// This is the finding of the phase. `G-CAL-002` requires a distribution stratified by
    /// ReferenceType x ParticipationRegime x VolatilityRegime, and only ReferenceType
    /// exists as a usable key: ThinParticipationLabel is itself calibration-gated, and no
    /// volatility regime classifier exists at all. Pooling across the two missing axes
    /// would produce a distribution that looks complete and is not.
    /// </summary>
    [Fact]
    public void B02_Protocol_blocks_at_stratification_not_at_sample_criteria()
    {
        var protocol = CalibrationProtocol.Evaluate(hasCollectedRows: true);

        Assert.Equal(CalibrationProtocolStep.StratifiedDistribution, protocol.BlockedAt);
        Assert.Equal(1, protocol.StepsSatisfied);
        Assert.Equal(
            new[] { StratificationAxis.ParticipationRegime, StratificationAxis.VolatilityRegime },
            protocol.MissingAxes);
    }

    [Fact]
    public void B03_With_no_rows_the_protocol_blocks_at_collection()
    {
        var protocol = CalibrationProtocol.Evaluate(hasCollectedRows: false);
        Assert.Equal(CalibrationProtocolStep.CollectRawFeatures, protocol.BlockedAt);
        Assert.Equal(0, protocol.StepsSatisfied);
    }

    /// <summary>Each axis says why it is where it is, rather than leaving the reader to guess.</summary>
    [Fact]
    public void B04_Every_axis_reports_a_reason()
    {
        foreach (var axis in CalibrationProtocol.Evaluate(true).StratificationAxes)
            Assert.False(string.IsNullOrWhiteSpace(axis.Reason));
    }

    [Fact]
    public void B05_Reference_type_is_the_only_usable_axis()
    {
        var axes = CalibrationProtocol.Evaluate(true).StratificationAxes;
        Assert.Single(axes, a => a.CanStratify);
        Assert.Equal(StratificationAxis.ReferenceType, axes.Single(a => a.CanStratify).Axis);
    }

    [Fact]
    public void B06_Protocol_has_six_steps() =>
        Assert.Equal(6, Enum.GetValues<CalibrationProtocolStep>().Length);

    // ========== C: the studies stop where the spec says they stop ==========

    [Fact]
    public void C01_All_nine_first_workload_studies_are_declared() =>
        Assert.Equal(9, ScannerStudyRegistry.Evaluate(admissibleRows: 0).Count);

    /// <summary>
    /// There is no Calibrated state to reach.
    ///
    /// Its absence is the point: a study that produced a threshold would belong to the
    /// DECISION_LOG, and the member existing here would let a later edit shortcut steps
    /// four through six.
    /// </summary>
    [Fact]
    public void C02_No_study_state_means_calibrated()
    {
        foreach (var name in Enum.GetNames<ScannerStudyState>())
            Assert.DoesNotContain("Calibrat", name, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void C03_No_study_ever_progresses_past_awaiting_sample_criteria()
    {
        foreach (var rows in new[] { 0, 1, 10_000 })
        foreach (var study in ScannerStudyRegistry.Evaluate(rows))
            Assert.True(study.State is ScannerStudyState.AwaitingPrerequisite
                            or ScannerStudyState.Collecting
                            or ScannerStudyState.AwaitingSampleCriteria,
                study.Kind + " reached " + study.State);
    }

    /// <summary>A blocked study must name what it is blocked on, or it is unactionable.</summary>
    [Fact]
    public void C04_Every_blocked_study_names_its_prerequisite()
    {
        foreach (var study in ScannerStudyRegistry.Evaluate(admissibleRows: 500)
                     .Where(s => s.State == ScannerStudyState.AwaitingPrerequisite))
            Assert.False(string.IsNullOrWhiteSpace(study.BlockedOn), study.Kind + " has no reason");
    }

    /// <summary>
    /// A study whose input is itself calibration-gated is blocked, not collecting.
    /// Building a distribution on a gated input calibrates one gate with another.
    /// </summary>
    [Fact]
    public void C05_Studies_resting_on_a_gated_input_are_blocked()
    {
        var studies = ScannerStudyRegistry.Evaluate(admissibleRows: 500)
            .ToDictionary(s => s.Kind);

        Assert.Equal(ScannerStudyState.AwaitingPrerequisite,
            studies[ScannerStudyKind.ThinParticipationEffect].State);
        Assert.Equal(ScannerStudyState.AwaitingPrerequisite,
            studies[ScannerStudyKind.OneTimeFramingPersistence].State);
    }

    // ========== D: the dataset records measurements, not labels ==========

    [Fact]
    public void D01_A_closed_episode_becomes_one_row()
    {
        var host = Host();
        host.Rebuild(Closed(Episode("EP-1")), Utc(60));

        Assert.Equal(1, host.Current!.RowsCollected);
        Assert.Equal(HistoricalScannerState.Collecting, host.Current.State);
    }

    /// <summary>
    /// The closed-episode list is a rolling window, so the same episode arrives on many
    /// publishes. Folding it twice would double-count it into every distribution ever
    /// built on this dataset.
    /// </summary>
    [Fact]
    public void D02_The_same_episode_is_never_folded_twice()
    {
        var host = Host();
        var set = Closed(Episode("EP-1"), Episode("EP-2"));

        host.Rebuild(set, Utc(60));
        host.Rebuild(set, Utc(90));
        host.Rebuild(Closed(Episode("EP-2"), Episode("EP-3")), Utc(120));

        Assert.Equal(3, host.Current!.RowsCollected);
    }

    [Fact]
    public void D03_Invalid_data_is_recorded_not_discarded()
    {
        var host = Host();
        host.Rebuild(Closed(Episode("EP-1", quality: EpisodeDataQuality.Invalid)), Utc(60));

        // Recorded, so a later rule version can reconsider it; not admissible, so nothing
        // counts it as usable evidence today.
        Assert.Equal(1, host.Current!.RowsCollected);
        Assert.Equal(0, host.Current.AdmissibleRows);
        Assert.Equal(DatasetRowAdmissibility.InvalidData, host.Rows[0].Admissibility);
    }

    [Fact]
    public void D04_Missing_aggressor_evidence_is_marked_not_zeroed()
    {
        var host = Host();
        host.Rebuild(
            Closed(Episode("EP-1", aggressor: AggressorEvidenceAvailability.Unavailable)), Utc(60));

        Assert.Equal(DatasetRowAdmissibility.AggressorEvidenceMissing, host.Rows[0].Admissibility);
    }

    /// <summary>
    /// Excursions are projected onto the interaction direction, and only when there is
    /// one. Signing an excursion against an undetermined direction would invent the sign.
    /// </summary>
    [Fact]
    public void D05_Excursions_are_signed_only_when_direction_is_known()
    {
        var up = EpisodeDatasetRecord.FromEpisode(
            Episode("EP-1", direction: EpisodeInteractionDirection.Up, above: 12, below: 4));
        Assert.Equal(12, up.FavourableExcursionTicks);
        Assert.Equal(4, up.AdverseExcursionTicks);

        var down = EpisodeDatasetRecord.FromEpisode(
            Episode("EP-2", direction: EpisodeInteractionDirection.Down, above: 12, below: 4));
        Assert.Equal(4, down.FavourableExcursionTicks);
        Assert.Equal(12, down.AdverseExcursionTicks);

        foreach (var direction in new[]
                 { EpisodeInteractionDirection.Unknown, EpisodeInteractionDirection.Bidirectional })
        {
            var row = EpisodeDatasetRecord.FromEpisode(Episode("EP-3", direction: direction));
            Assert.Null(row.FavourableExcursionTicks);
            Assert.Null(row.AdverseExcursionTicks);
        }
    }

    /// <summary>
    /// No row field may carry a verdict. A dataset that labels its own rows has already
    /// applied the rule it exists to help discover.
    /// </summary>
    [Fact]
    public void D06_No_row_field_carries_a_verdict()
    {
        var banned = new[]
            { "Healthy", "Failing", "Strong", "Weak", "Success", "Win", "Loss", "Quality", "Score", "Grade" };

        foreach (var property in typeof(EpisodeDatasetRecord).GetProperties())
        foreach (var word in banned)
        {
            // DataQuality is the episode's own upstream availability flag, not a verdict
            // about the market.
            if (property.Name == "DataQuality") continue;
            Assert.False(property.Name.Contains(word, StringComparison.OrdinalIgnoreCase),
                "EpisodeDatasetRecord." + property.Name + " labels the row");
        }
    }

    // ========== E: the dataset tells the truth about its own size ==========

    /// <summary>
    /// Retention is a memory bound, not a revision of history.
    ///
    /// A dataset that saw 3000 episodes and reports 2048 because of a capacity limit is
    /// lying about sample size — and sample size is exactly what a calibration decision
    /// would rest on. Same reasoning as the price memory ledger's TotalTests.
    /// </summary>
    [Fact]
    public void E01_Retention_does_not_rewrite_the_observed_count()
    {
        var host = Host();
        var capacity = HistoricalScannerPolicyConfig.RowCapacity;

        for (var i = 0; i < capacity + 50; i++)
            host.Rebuild(Closed(Episode("EP-" + i)), Utc(60));

        Assert.Equal(capacity + 50, host.Current!.RowsCollected);
        Assert.Equal(capacity, host.Rows.Count);
        Assert.Equal(50, host.Current.RowsDroppedToCapacity);
    }

    [Fact]
    public void E02_Rows_are_counted_per_reference_type()
    {
        var host = Host();
        host.Rebuild(
            Closed(
                Episode("EP-1", ReferenceType.PreviousPrimaryTpoPoc),
                Episode("EP-2", ReferenceType.PreviousPrimaryTpoPoc),
                Episode("EP-3", ReferenceType.CurrentPrimaryVpoc)),
            Utc(60));

        Assert.Equal(2, host.Current!.RowsByReferenceType[ReferenceType.PreviousPrimaryTpoPoc]);
        Assert.Equal(1, host.Current.RowsByReferenceType[ReferenceType.CurrentPrimaryVpoc]);
    }

    [Fact]
    public void E03_Disabled_host_publishes_a_disabled_snapshot()
    {
        var host = new HistoricalScannerHost(new HistoricalScannerPolicyConfig(enabled: false));
        host.Rebuild(Closed(Episode("EP-1")), Utc(60));

        Assert.Equal(HistoricalScannerState.Disabled, host.Current!.State);
        Assert.Equal(0, host.Current.RowsCollected);
    }

    [Fact]
    public void E04_No_episodes_yields_awaiting_not_collecting()
    {
        var host = Host();
        host.Rebuild(null, Utc(60));
        Assert.Equal(HistoricalScannerState.AwaitingEpisodes, host.Current!.State);
    }

    [Fact]
    public void E05_Configure_null_throws() =>
        Assert.Throws<ArgumentNullException>(() => Host().Configure(null!));

    /// <summary>Every standing limitation is published, including that this is research only.</summary>
    [Fact]
    public void E06_Standing_limitations_are_published()
    {
        var host = Host();
        host.Rebuild(Closed(Episode("EP-1")), Utc(60));

        foreach (var limitation in HistoricalScannerPolicyConfig.StandingLimitations)
            Assert.Contains(limitation, host.Current!.Limitations);

        Assert.Contains(
            HistoricalScannerPolicyConfig.LimitationStratificationIncomplete,
            host.Current!.Limitations);
    }

    // ========== F: runtime and card ==========

    [Fact]
    public void F01_Runtime_schema_is_0_26_0() =>
        Assert.Equal("0.26.0", GcaeRuntimeSnapshot.SnapshotVersion);

    [Fact]
    public void F02_Card_reports_the_blocking_step_not_just_a_row_count()
    {
        var host = Host();
        host.Rebuild(Closed(Episode("EP-1")), Utc(60));

        var line = AuctionGpsCardMapper.ScannerLine(host.Current);

        // Episode and bar rows are a pair, never a total — see Phase5ABBarReplayTests D01.
        Assert.Contains("1 EP / 0 BAR", line, StringComparison.Ordinal);
        Assert.Contains("CAL STEP 2/6", line, StringComparison.Ordinal);
        Assert.Contains("STRATIFIEDDISTRIBUTION", line, StringComparison.Ordinal);
    }

    [Fact]
    public void F03_Card_distinguishes_absent_from_disabled()
    {
        Assert.Equal("SCANNER: NOT AVAILABLE", AuctionGpsCardMapper.ScannerLine(null));

        var host = new HistoricalScannerHost(new HistoricalScannerPolicyConfig(enabled: false));
        host.Rebuild(null, Utc());
        Assert.Equal("SCANNER: DISABLED", AuctionGpsCardMapper.ScannerLine(host.Current));
    }

    /// <summary>
    /// The scanner must not have unlocked anything on its way in.
    ///
    /// 5A collects the data that would eventually inform an unlock. The unlock itself is
    /// five further steps away and happens outside this build, so the reserved set must
    /// come through this phase untouched.
    ///
    /// The total is pinned as a tripwire rather than asserted for its own sake: any
    /// change to it means a calibration-gated member was added or unlocked, and that is a
    /// `G-CAL-003` event requiring a schema bump and a retired-limitation entry, never a
    /// silent consequence of something else.
    /// </summary>
    [Fact]
    public void F04_No_reserved_enum_member_was_unlocked()
    {
        var reserved = EngineTypes
            .Where(t => t.IsEnum)
            .SelectMany(t => Enum.GetValues(t).Cast<object>()
                .Select(v => (Type: t, Value: Convert.ToInt64(v))))
            .Where(m => m.Value >= 100L)
            .ToArray();

        Assert.Equal(107, reserved.Length);

        // 5A itself introduces no gated state. Every state it can report is observable
        // today — that is the difference between "not calibrated" and "not implemented".
        Assert.DoesNotContain(reserved, m => m.Type.Namespace == "GC.AuctionFlow.Research");
    }
}
