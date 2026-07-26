namespace GC.AuctionFlow.Execution;

/// <summary>
/// CFD execution-map state (v1.2 §34.5).
///
/// The analysis market is GC; the execution market is a separate CFD. Mapping between
/// them requires basis, and basis requires a CFD price the indicator does not receive.
/// </summary>
public enum CfdMappingState
{
    /// <summary>Basis measurable and fresh.</summary>
    Valid = 0,

    /// <summary>Basis measurable but stale or volatile.</summary>
    Degraded = 1,

    /// <summary>Basis not measurable. GC analysis remains valid; the map does not.</summary>
    Invalid = 2
}

/// <summary>Why basis cannot be established.</summary>
public enum BasisAvailability
{
    /// <summary>No CFD price reaches the indicator. ATAS supplies GC only.</summary>
    NoCfdPriceFeed = 0,

    /// <summary>CFD price present but stale beyond usefulness.</summary>
    Stale = 1,

    /// <summary>Measured.</summary>
    Available = 100
}

/// <summary>
/// Phase 4C CFD Mapping (v1.2 §34).
/// Only Executable RR may drive execution feasibility (§34.2), and Executable RR needs
/// basis, spread and slippage — none of which exist here.
/// </summary>
public sealed class CfdMappingPolicyConfig
{
    public const string PolicyVersion = "CFD_MAPPING_POLICY_V1";

    public const string LimitationNoCfdPriceFeed = "CFD_PRICE_FEED_ABSENT";
    public const string LimitationBasisUnmeasurable = "BASIS_UNMEASURABLE_WITHOUT_CFD_PRICE";
    public const string LimitationExecutableRrUnavailable = "EXECUTABLE_RR_UNAVAILABLE";
    public const string LimitationTheoreticalRrNotForExecution = "THEORETICAL_RR_NOT_VALID_FOR_EXECUTION";
    public const string LimitationSpreadUnavailable = "CFD_SPREAD_UNAVAILABLE";
    public const string LimitationSlippageNotCalibrated = "EXPECTED_SLIPPAGE_NOT_CALIBRATED";
    public const string LimitationNoCfdPriceEmitted = "CFD_MAPPING_EMITS_NO_CFD_PRICE";
    public const string LimitationLiveOnly = "LIVE_ONLY_HISTORY";

    /// <summary>v1.2 §34.5 message when the map cannot be formed.</summary>
    public const string OperatorMessageInvalid = "GC ANALYSIS VALID / CFD EXECUTION MAP INVALID";

    public CfdMappingPolicyConfig(bool enabled = false)
    {
        Enabled = enabled;
    }

    public bool Enabled { get; }
    public string Version => PolicyVersion;
}

/// <summary>
/// Immutable Phase 4C CFD mapping snapshot.
///
/// Every estimated CFD level of v1.2 §34.4 is null. Emitting one without basis would
/// hand the operator a number that looks like a price and is not.
/// </summary>
public sealed class CfdMappingSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public CfdMappingSnapshot(
        CfdMappingState state,
        string policyVersion,
        BasisAvailability basisAvailability,
        decimal? currentBasis,
        decimal? rollingMedianBasis,
        decimal? basisVolatility,
        TimeSpan? basisStaleness,
        decimal? cfdSpread,
        decimal? expectedSlippage,
        decimal? executableRewardToRisk,
        string operatorMessage,
        DateTime observedAtUtc,
        IReadOnlyList<string> limitations)
    {
        State = state;
        PolicyVersion = policyVersion ?? CfdMappingPolicyConfig.PolicyVersion;
        BasisAvailability = basisAvailability;
        CurrentBasis = currentBasis;
        RollingMedianBasis = rollingMedianBasis;
        BasisVolatility = basisVolatility;
        BasisStaleness = basisStaleness;
        CfdSpread = cfdSpread;
        ExpectedSlippage = expectedSlippage;
        ExecutableRewardToRisk = executableRewardToRisk;
        OperatorMessage = operatorMessage ?? "";
        ObservedAtUtc = observedAtUtc;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public CfdMappingState State { get; }
    public string PolicyVersion { get; }
    public BasisAvailability BasisAvailability { get; }

    // v1.2 §34.3 tracked values. All null while there is no CFD price.
    public decimal? CurrentBasis { get; }
    public decimal? RollingMedianBasis { get; }
    public decimal? BasisVolatility { get; }
    public TimeSpan? BasisStaleness { get; }
    public decimal? CfdSpread { get; }
    public decimal? ExpectedSlippage { get; }

    /// <summary>v1.2 §34.2: only this may drive execution feasibility. Null here.</summary>
    public decimal? ExecutableRewardToRisk { get; }

    /// <summary>What the operator must be told, verbatim from v1.2 §34.5.</summary>
    public string OperatorMessage { get; }

    public DateTime ObservedAtUtc { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;

    /// <summary>False whenever the map cannot support a real execution decision.</summary>
    public bool ExecutionFeasibilityDeterminable =>
        State == CfdMappingState.Valid && ExecutableRewardToRisk.HasValue;
}

/// <summary>
/// Phase 4C CFD Mapping host (v1.2 §34).
///
/// v1.2 §2.7 splits the markets: GC is analysed, a CFD is executed. Translating between
/// them needs basis, and basis is CFD price minus GC price. ATAS delivers GC only, so
/// basis is unmeasurable and the map is INVALID.
///
/// That is a safety output, not a failure. It tells the operator the auction analysis
/// stands while the mechanical translation to their broker does not — exactly the
/// message v1.2 §34.5 prescribes.
/// </summary>
public sealed class CfdMappingHost
{
    private CfdMappingPolicyConfig _policy;
    private CfdMappingSnapshot? _published;

    public CfdMappingHost(CfdMappingPolicyConfig? policy = null)
    {
        _policy = policy ?? new CfdMappingPolicyConfig(enabled: false);
    }

    public CfdMappingSnapshot? Current => _published;
    public CfdMappingPolicyConfig Policy => _policy;

    public void Configure(CfdMappingPolicyConfig policy)
    {
        if (policy is null) throw new ArgumentNullException(nameof(policy));
        _policy = policy;
        if (!_policy.Enabled)
            _published = DisabledSnapshot(DateTime.UtcNow);
    }

    public void Reset() => _published = null;

    /// <summary>
    /// Rebuild the mapping. <paramref name="cfdPrice"/> is accepted so the shape is
    /// correct for the day a feed exists, but no such feed is wired.
    /// </summary>
    public CfdMappingSnapshot Rebuild(
        decimal? gcPrice = null,
        decimal? cfdPrice = null,
        DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;

        if (!_policy.Enabled)
        {
            _published = DisabledSnapshot(now);
            return _published;
        }

        // Basis needs BOTH prices. One of them alone tells us nothing about the offset.
        var haveBoth = gcPrice.HasValue && cfdPrice.HasValue;

        if (!haveBoth)
        {
            _published = new CfdMappingSnapshot(
                CfdMappingState.Invalid,
                CfdMappingPolicyConfig.PolicyVersion,
                BasisAvailability.NoCfdPriceFeed,
                null, null, null, null, null, null, null,
                CfdMappingPolicyConfig.OperatorMessageInvalid,
                now,
                BuildLimitations());
            return _published;
        }

        // A single observation gives a basis but not its median, volatility or staleness,
        // and spread and slippage remain unavailable. Executable RR therefore stays null
        // and the map is Degraded rather than Valid — one sample is not a distribution.
        _published = new CfdMappingSnapshot(
            CfdMappingState.Degraded,
            CfdMappingPolicyConfig.PolicyVersion,
            BasisAvailability.Available,
            cfdPrice!.Value - gcPrice!.Value,
            null, null, null, null, null, null,
            CfdMappingPolicyConfig.OperatorMessageInvalid,
            now,
            BuildLimitations());

        return _published;
    }

    private static IReadOnlyList<string> BuildLimitations() => new[]
    {
        CfdMappingPolicyConfig.LimitationNoCfdPriceFeed,
        CfdMappingPolicyConfig.LimitationBasisUnmeasurable,
        CfdMappingPolicyConfig.LimitationExecutableRrUnavailable,
        // v1.2 §34.2: theoretical RR must never stand in for executable RR.
        CfdMappingPolicyConfig.LimitationTheoreticalRrNotForExecution,
        CfdMappingPolicyConfig.LimitationSpreadUnavailable,
        CfdMappingPolicyConfig.LimitationSlippageNotCalibrated,
        CfdMappingPolicyConfig.LimitationNoCfdPriceEmitted,
        CfdMappingPolicyConfig.LimitationLiveOnly
    };

    private static CfdMappingSnapshot DisabledSnapshot(DateTime now) =>
        new(CfdMappingState.Invalid,
            CfdMappingPolicyConfig.PolicyVersion,
            BasisAvailability.NoCfdPriceFeed,
            null, null, null, null, null, null, null,
            CfdMappingPolicyConfig.OperatorMessageInvalid,
            now,
            new[] { "MODULE_DISABLED" });
}
