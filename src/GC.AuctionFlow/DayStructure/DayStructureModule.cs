using GC.AuctionFlow.Profile;

namespace GC.AuctionFlow.DayStructure;

public enum DayStructureModuleState
{
    Disabled = 0,
    AwaitingProfile = 1,
    Ready = 2,
    Partial = 3,
    Invalid = 4
}

/// <summary>
/// Day structure (v1.2 §18.4, KDK Ch 11).
///
/// Every in-session label is a CANDIDATE that may be revised. KDK Ch 11 is explicit:
/// day type describes the auction after it has developed; it is never a mould the
/// market is obliged to fill.
///
/// Recognising any specific shape needs calibrated IB-width, range-extension and
/// value-migration rules, so all candidate labels are reserved and the module reports
/// <see cref="NotCalibrated"/>. v1.2 §18.4 calls this stage RESEARCH_ONLY: log the raw
/// features first, permit labels only after validation.
/// </summary>
public enum DayStructureState
{
    Unknown = 0,

    /// <summary>Session is developing; shape rules are not calibrated.</summary>
    NotCalibrated = 1,

    /// <summary>Session ended; the post-session label is still not calibrated.</summary>
    SessionComplete = 2,

    // Reserved — calibration required before any of these can be emitted.
    NormalCandidate = 100,
    NormalVariationCandidate = 101,
    NeutralCandidate = 102,
    NonTrendCandidate = 103,
    TrendUpCandidate = 104,
    TrendDownCandidate = 105,
    DoubleDistributionUpCandidate = 106,
    DoubleDistributionDownCandidate = 107,
    Transition = 108,
    PostSessionConfirmed = 109
}

/// <summary>
/// Where the session close sits within the session range.
/// The BAND is observable; what a band IMPLIES is not.
/// </summary>
public enum SessionCloseLocation
{
    Unavailable = 0,
    UpperThird = 1,
    MiddleThird = 2,
    LowerThird = 3
}

/// <summary>
/// Range extension beyond the initial balance (KDK Ch 11).
/// Direction is observable by comparison; "strong" versus "slight" is calibrated.
/// </summary>
public enum RangeExtensionDirection
{
    Unavailable = 0,
    None = 1,
    UpOnly = 2,
    DownOnly = 3,
    BothSides = 4
}

/// <summary>
/// Phase 1H Day Structure (v1.2 §18.4, KDK Ch 11). RESEARCH_ONLY.
/// </summary>
public sealed class DayStructurePolicyConfig
{
    public const string PolicyVersion = "DAY_STRUCTURE_POLICY_V1";

    public const string LimitationResearchOnly = "DAY_STRUCTURE_RESEARCH_ONLY";
    public const string LimitationShapeRulesNotCalibrated = "DAY_STRUCTURE_SHAPE_RULES_NOT_CALIBRATED";
    public const string LimitationIbWidthPercentileNotCalibrated = "IB_WIDTH_PERCENTILE_NOT_CALIBRATED";
    public const string LimitationDoubleDistributionNotCalibrated = "DOUBLE_DISTRIBUTION_RULE_NOT_CALIBRATED";
    public const string LimitationNoEntrySignal = "DAY_STRUCTURE_NEVER_PRODUCES_ENTRY";
    public const string LimitationNoHardVetoFeed = "DAY_STRUCTURE_DOES_NOT_FEED_HARD_VETO";
    public const string LimitationLabelIsCandidate = "DAY_STRUCTURE_LABEL_IS_CANDIDATE_ONLY";
    public const string LimitationIbUnavailable = "INITIAL_BALANCE_UNAVAILABLE";
    public const string LimitationEodLabelsWithheldIntraSession = "IB_EXTREME_EOD_LABELS_WITHHELD_INTRA_SESSION";
    public const string LimitationLiveOnly = "LIVE_ONLY_HISTORY";

    /// <summary>
    /// Completed TPO periods that constitute the initial balance.
    /// Two is the conventional reading, not a calibrated finding.
    /// </summary>
    public const int InitialBalancePeriodCount = 2;

    public DayStructurePolicyConfig(bool enabled = false)
    {
        Enabled = enabled;
    }

    public bool Enabled { get; }
    public string Version => PolicyVersion;
}

/// <summary>
/// IB extreme observations, split to avoid look-ahead (v1.2 §18.4.1).
///
/// The live feature may be read at any time. The end-of-day labels are only
/// meaningful once the session is complete, and are withheld until then — reading
/// them intra-session would leak the future into a live decision.
/// </summary>
public sealed class IbExtremeObservation
{
    public IbExtremeObservation(
        bool sessionComplete,
        bool? currentDayExtremeStillEqualsIbHigh,
        bool? currentDayExtremeStillEqualsIbLow,
        bool? ibHighWasDayHigh,
        bool? ibLowWasDayLow)
    {
        SessionComplete = sessionComplete;
        CurrentDayExtremeStillEqualsIbHigh = currentDayExtremeStillEqualsIbHigh;
        CurrentDayExtremeStillEqualsIbLow = currentDayExtremeStillEqualsIbLow;
        IbHighWasDayHigh = ibHighWasDayHigh;
        IbLowWasDayLow = ibLowWasDayLow;
    }

    public bool SessionComplete { get; }

    /// <summary>Live feature — safe intra-session.</summary>
    public bool? CurrentDayExtremeStillEqualsIbHigh { get; }

    /// <summary>Live feature — safe intra-session.</summary>
    public bool? CurrentDayExtremeStillEqualsIbLow { get; }

    /// <summary>End-of-day label. Null until the session completes.</summary>
    public bool? IbHighWasDayHigh { get; }

    /// <summary>End-of-day label. Null until the session completes.</summary>
    public bool? IbLowWasDayLow { get; }

    public bool EodLabelsAvailable => SessionComplete;
}

/// <summary>
/// Immutable Phase 1H day structure snapshot.
/// The state is always Unknown / NotCalibrated / SessionComplete.
/// </summary>
public sealed class DayStructureSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public DayStructureSnapshot(
        DayStructureModuleState moduleState,
        string policyVersion,
        string auctionId,
        DayStructureState state,
        int revisionCount,
        long? ibHighTick,
        long? ibLowTick,
        long? ibWidthTicks,
        decimal? ibWidthPercentile,
        long? sessionRangeTicks,
        RangeExtensionDirection rangeExtension,
        long? rangeExtensionUpTicks,
        long? rangeExtensionDownTicks,
        long? pocMigrationTicks,
        long? valueMigrationTicks,
        SessionCloseLocation closeLocation,
        bool sessionComplete,
        IbExtremeObservation ibExtreme,
        DateTime observedAtUtc,
        IReadOnlyList<string> limitations)
    {
        ModuleState = moduleState;
        PolicyVersion = policyVersion ?? DayStructurePolicyConfig.PolicyVersion;
        AuctionId = auctionId ?? "";
        State = state;
        RevisionCount = revisionCount;
        IbHighTick = ibHighTick;
        IbLowTick = ibLowTick;
        IbWidthTicks = ibWidthTicks;
        IbWidthPercentile = ibWidthPercentile;
        SessionRangeTicks = sessionRangeTicks;
        RangeExtension = rangeExtension;
        RangeExtensionUpTicks = rangeExtensionUpTicks;
        RangeExtensionDownTicks = rangeExtensionDownTicks;
        PocMigrationTicks = pocMigrationTicks;
        ValueMigrationTicks = valueMigrationTicks;
        CloseLocation = closeLocation;
        SessionComplete = sessionComplete;
        IbExtreme = ibExtreme;
        ObservedAtUtc = observedAtUtc;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public DayStructureModuleState ModuleState { get; }
    public string PolicyVersion { get; }
    public string AuctionId { get; }

    /// <summary>Never a shape label — every candidate name is reserved.</summary>
    public DayStructureState State { get; }

    /// <summary>How many times the in-session view has been revised (v1.2 §18.4).</summary>
    public int RevisionCount { get; }

    public long? IbHighTick { get; }
    public long? IbLowTick { get; }
    public long? IbWidthTicks { get; }

    /// <summary>Always null — the percentile needs a historical distribution (5A).</summary>
    public decimal? IbWidthPercentile { get; }

    public long? SessionRangeTicks { get; }
    public RangeExtensionDirection RangeExtension { get; }
    public long? RangeExtensionUpTicks { get; }
    public long? RangeExtensionDownTicks { get; }
    public long? PocMigrationTicks { get; }
    public long? ValueMigrationTicks { get; }
    public SessionCloseLocation CloseLocation { get; }
    public bool SessionComplete { get; }
    public IbExtremeObservation IbExtreme { get; }
    public DateTime ObservedAtUtc { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;

    public bool IbAvailable => IbWidthTicks.HasValue;
}

/// <summary>
/// Phase 1H Day Structure host (v1.2 §18.4, KDK Ch 11).
///
/// Logs the raw structural features of the developing session. It never names a day
/// type: KDK Ch 11 warns against predicting the type early to justify a trade, and
/// against clinging to a Trend Day label after price has re-entered value.
///
/// Phase 1A Profile is LOCKED, so this observes it rather than modifying it.
/// </summary>
public sealed class DayStructureHost
{
    private DayStructurePolicyConfig _policy;
    private DayStructureSnapshot? _published;
    private string? _currentAuctionId;
    private int _revisionCount;

    public DayStructureHost(DayStructurePolicyConfig? policy = null)
    {
        _policy = policy ?? new DayStructurePolicyConfig(enabled: false);
    }

    public DayStructureSnapshot? Current => _published;
    public DayStructurePolicyConfig Policy => _policy;

    public void Configure(DayStructurePolicyConfig policy)
    {
        if (policy is null) throw new ArgumentNullException(nameof(policy));
        _policy = policy;
        if (!_policy.Enabled)
        {
            ResetInternal();
            _published = DisabledSnapshot(DateTime.UtcNow);
        }
    }

    public void Reset()
    {
        ResetInternal();
        _published = null;
    }

    public DayStructureSnapshot Rebuild(
        PrimaryAuctionProfileSnapshot? auction,
        IReadOnlyList<CompletedTpoPeriodSnapshot>? completedPeriods,
        decimal tickSize,
        DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;

        if (!_policy.Enabled)
        {
            ResetInternal();
            _published = DisabledSnapshot(now);
            return _published;
        }

        if (auction is null || tickSize <= 0m)
        {
            _published = StatusSnapshot(DayStructureModuleState.AwaitingProfile, now);
            return _published;
        }

        // A new auction restarts the in-session view; revisions belong to one session.
        if (!string.Equals(_currentAuctionId, auction.AuctionId, StringComparison.Ordinal))
        {
            _currentAuctionId = auction.AuctionId;
            _revisionCount = 0;
        }
        else
        {
            _revisionCount++;
        }

        var (ibHigh, ibLow) = DeriveInitialBalance(completedPeriods, tickSize);
        long? ibWidth = ibHigh.HasValue && ibLow.HasValue ? ibHigh.Value - ibLow.Value : null;

        long? sessionHigh = ToTick(auction.ProfileHigh, tickSize);
        long? sessionLow = ToTick(auction.ProfileLow, tickSize);
        long? sessionRange = sessionHigh.HasValue && sessionLow.HasValue
            ? sessionHigh.Value - sessionLow.Value
            : null;

        long? extUp = ibHigh.HasValue && sessionHigh.HasValue
            ? Math.Max(0L, sessionHigh.Value - ibHigh.Value) : null;
        long? extDown = ibLow.HasValue && sessionLow.HasValue
            ? Math.Max(0L, ibLow.Value - sessionLow.Value) : null;

        var extension = DeriveExtension(extUp, extDown);
        var closeLocation = DeriveCloseLocation(
            ToTick(auction.LastObservedPrice, tickSize), sessionHigh, sessionLow);

        var ibExtreme = DeriveIbExtreme(auction.IsCompleted, ibHigh, ibLow, sessionHigh, sessionLow);

        var state = auction.IsCompleted
            ? DayStructureState.SessionComplete
            : DayStructureState.NotCalibrated;

        var moduleState = ibWidth.HasValue
            ? DayStructureModuleState.Ready
            : DayStructureModuleState.Partial;

        _published = new DayStructureSnapshot(
            moduleState,
            DayStructurePolicyConfig.PolicyVersion,
            auction.AuctionId,
            state,
            _revisionCount,
            ibHigh, ibLow, ibWidth,
            // The percentile needs a historical IB-width distribution — Phase 5A.
            null,
            sessionRange,
            extension, extUp, extDown,
            // POC and value migration within the session need the developing-profile
            // series, which Phase 1A does not expose as a time series.
            null, null,
            closeLocation,
            auction.IsCompleted,
            ibExtreme,
            now,
            BuildLimitations(ibWidth.HasValue, auction.IsCompleted));

        return _published;
    }

    // --- derivation ---

    /// <summary>
    /// Initial balance from the first N completed TPO periods. Two periods is the
    /// conventional reading, not a calibrated finding, and is declared as such.
    /// </summary>
    private static (long? high, long? low) DeriveInitialBalance(
        IReadOnlyList<CompletedTpoPeriodSnapshot>? periods, decimal tickSize)
    {
        if (periods is null) return (null, null);

        var ib = periods
            .OrderBy(p => p.PeriodIndex)
            .Take(DayStructurePolicyConfig.InitialBalancePeriodCount)
            .ToArray();

        // A partial initial balance is not an initial balance.
        if (ib.Length < DayStructurePolicyConfig.InitialBalancePeriodCount)
            return (null, null);

        return (ib.Max(p => p.PeriodHighTick), ib.Min(p => p.PeriodLowTick));
    }

    private static RangeExtensionDirection DeriveExtension(long? up, long? down)
    {
        if (!up.HasValue || !down.HasValue) return RangeExtensionDirection.Unavailable;
        var u = up.Value > 0L;
        var d = down.Value > 0L;
        if (u && d) return RangeExtensionDirection.BothSides;
        if (u) return RangeExtensionDirection.UpOnly;
        if (d) return RangeExtensionDirection.DownOnly;
        return RangeExtensionDirection.None;
    }

    private static SessionCloseLocation DeriveCloseLocation(long? close, long? high, long? low)
    {
        if (!close.HasValue || !high.HasValue || !low.HasValue) return SessionCloseLocation.Unavailable;

        var range = high.Value - low.Value;
        if (range <= 0L) return SessionCloseLocation.Unavailable;

        var position = (decimal)(close.Value - low.Value) / range;
        if (position >= 2m / 3m) return SessionCloseLocation.UpperThird;
        if (position <= 1m / 3m) return SessionCloseLocation.LowerThird;
        return SessionCloseLocation.MiddleThird;
    }

    /// <summary>
    /// v1.2 §18.4.1: the live feature and the end-of-day labels are separate layers.
    /// EOD labels stay null until the session completes — producing them intra-session
    /// would leak the outcome into a live decision.
    /// </summary>
    private static IbExtremeObservation DeriveIbExtreme(
        bool sessionComplete, long? ibHigh, long? ibLow, long? sessionHigh, long? sessionLow)
    {
        bool? stillHigh = ibHigh.HasValue && sessionHigh.HasValue
            ? sessionHigh.Value == ibHigh.Value : null;
        bool? stillLow = ibLow.HasValue && sessionLow.HasValue
            ? sessionLow.Value == ibLow.Value : null;

        if (!sessionComplete)
            return new IbExtremeObservation(false, stillHigh, stillLow, null, null);

        return new IbExtremeObservation(true, stillHigh, stillLow, stillHigh, stillLow);
    }

    private static long? ToTick(decimal? price, decimal tickSize) =>
        price.HasValue && tickSize > 0m
            ? (long)Math.Round(price.Value / tickSize, MidpointRounding.AwayFromZero)
            : null;

    // --- helpers ---

    private static IReadOnlyList<string> BuildLimitations(bool ibAvailable, bool sessionComplete)
    {
        var lim = new List<string>
        {
            DayStructurePolicyConfig.LimitationResearchOnly,
            DayStructurePolicyConfig.LimitationShapeRulesNotCalibrated,
            DayStructurePolicyConfig.LimitationIbWidthPercentileNotCalibrated,
            DayStructurePolicyConfig.LimitationDoubleDistributionNotCalibrated,
            DayStructurePolicyConfig.LimitationNoEntrySignal,
            DayStructurePolicyConfig.LimitationNoHardVetoFeed,
            DayStructurePolicyConfig.LimitationLabelIsCandidate,
            DayStructurePolicyConfig.LimitationLiveOnly
        };
        if (!ibAvailable) lim.Add(DayStructurePolicyConfig.LimitationIbUnavailable);
        if (!sessionComplete) lim.Add(DayStructurePolicyConfig.LimitationEodLabelsWithheldIntraSession);
        return lim;
    }

    private void ResetInternal()
    {
        _currentAuctionId = null;
        _revisionCount = 0;
    }

    private static DayStructureSnapshot DisabledSnapshot(DateTime now) =>
        new(DayStructureModuleState.Disabled,
            DayStructurePolicyConfig.PolicyVersion, "",
            DayStructureState.Unknown, 0,
            null, null, null, null, null,
            RangeExtensionDirection.Unavailable, null, null, null, null,
            SessionCloseLocation.Unavailable, false,
            new IbExtremeObservation(false, null, null, null, null),
            now, new[] { "MODULE_DISABLED" });

    private static DayStructureSnapshot StatusSnapshot(DayStructureModuleState state, DateTime now) =>
        new(state, DayStructurePolicyConfig.PolicyVersion, "",
            DayStructureState.Unknown, 0,
            null, null, null, null, null,
            RangeExtensionDirection.Unavailable, null, null, null, null,
            SessionCloseLocation.Unavailable, false,
            new IbExtremeObservation(false, null, null, null, null),
            now, BuildLimitations(ibAvailable: false, sessionComplete: false));
}
