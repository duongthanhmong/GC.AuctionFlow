using GC.AuctionFlow.Entry;

namespace GC.AuctionFlow.Execution;

public enum RiskModuleState
{
    Disabled = 0,
    AwaitingInputs = 1,
    Ready = 2,
    Invalid = 3
}

/// <summary>
/// Account risk state (v1.2 §36).
/// Every transition needs realised-outcome history, which does not exist yet.
/// </summary>
public enum RiskState
{
    Unknown = 0,
    NotCalibrated = 1,

    // Reserved — calibration and an outcome ledger are required.
    Normal = 100,
    Reduced = 101,
    Recovery = 102,
    Locked = 103
}

/// <summary>
/// v1.2 §35.2 Trade Affordability Gate.
///
/// The rule that matters is the prohibition: if the structural stop is too far, reduce
/// size or skip the trade. Never pull the stop into noise to fit the account.
/// </summary>
public enum TradeAffordability
{
    /// <summary>Cannot be evaluated — sizing inputs missing.</summary>
    Unknown = 0,
    NotCalibrated = 1,

    // Reserved.
    Affordable = 100,
    RequiresSizeReduction = 101,
    NotAffordable = 102
}

/// <summary>The operator inputs of v1.2 §35.3.</summary>
public enum RiskInput
{
    AccountEquity = 0,
    AccountCurrency = 1,
    MaximumRiskPerTrade = 2,
    MaximumDailyLoss = 3,
    MaximumWeeklyLoss = 4,
    MaximumOpenRisk = 5,
    CfdValuePerPoint = 6,
    MinimumLotStep = 7,
    Spread = 8,
    Slippage = 9,
    BasisAllowance = 10
}

/// <summary>
/// Phase 4A Position Sizing and Account Risk (v1.2 §35-36).
///
/// The mandatory order is Thesis -> Structural Invalidation -> Stop Distance ->
/// CFD Effective Stop Distance -> Risk Per Lot -> Allowed Size, and it may not be
/// reversed. The chain breaks at step two: invalidation is calibration-gated, so no
/// stop distance exists and nothing downstream can be computed.
/// </summary>
public sealed class RiskPolicyConfig
{
    public const string PolicyVersion = "RISK_POLICY_V1";

    public const string LimitationSizingChainBroken = "SIZING_CHAIN_REQUIRES_CALIBRATED_INVALIDATION";
    public const string LimitationOperatorInputsMissing = "RISK_OPERATOR_INPUTS_MISSING";
    public const string LimitationNoStopDistance = "STOP_DISTANCE_UNAVAILABLE";
    public const string LimitationNoSize = "RISK_MODULE_EMITS_NO_POSITION_SIZE";
    public const string LimitationNeverTightenStopToFitAccount = "NEVER_TIGHTEN_STOP_TO_FIT_ACCOUNT";
    public const string LimitationSizingOrderIsMandatory = "SIZING_ORDER_MUST_NOT_BE_REVERSED";
    public const string LimitationRiskStateNotCalibrated = "RISK_STATE_NOT_CALIBRATED";
    public const string LimitationDrawdownLedgerAbsent = "REALISED_OUTCOME_LEDGER_ABSENT";
    public const string LimitationLiveOnly = "LIVE_ONLY_HISTORY";

    /// <summary>All eleven §35.3 inputs are operator-supplied; none is present.</summary>
    public const int RequiredOperatorInputs = 11;

    public RiskPolicyConfig(bool enabled = false)
    {
        Enabled = enabled;
    }

    public bool Enabled { get; }
    public string Version => PolicyVersion;
}

/// <summary>
/// Immutable Phase 4A risk snapshot.
/// No size, no stop distance, no risk-per-lot. All of it needs a calibrated
/// invalidation distance and eleven operator inputs.
/// </summary>
public sealed class RiskSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public RiskSnapshot(
        RiskModuleState moduleState,
        string policyVersion,
        RiskState riskState,
        TradeAffordability affordability,
        bool invalidationDistanceAvailable,
        IReadOnlyList<RiskInput> missingOperatorInputs,
        EntryPlanKind entryPlan,
        CfdMappingState cfdMapping,
        DateTime observedAtUtc,
        IReadOnlyList<string> limitations)
    {
        ModuleState = moduleState;
        PolicyVersion = policyVersion ?? RiskPolicyConfig.PolicyVersion;
        RiskState = riskState;
        Affordability = affordability;
        InvalidationDistanceAvailable = invalidationDistanceAvailable;
        MissingOperatorInputs = missingOperatorInputs ?? Array.Empty<RiskInput>();
        EntryPlan = entryPlan;
        CfdMapping = cfdMapping;
        ObservedAtUtc = observedAtUtc;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public RiskModuleState ModuleState { get; }
    public string PolicyVersion { get; }

    /// <summary>Always NotCalibrated — needs a realised-outcome ledger.</summary>
    public RiskState RiskState { get; }

    /// <summary>Always Unknown or NotCalibrated.</summary>
    public TradeAffordability Affordability { get; }

    /// <summary>Step two of the mandatory chain. False while invalidation is gated.</summary>
    public bool InvalidationDistanceAvailable { get; }

    public IReadOnlyList<RiskInput> MissingOperatorInputs { get; }
    public EntryPlanKind EntryPlan { get; }
    public CfdMappingState CfdMapping { get; }
    public DateTime ObservedAtUtc { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;

    /// <summary>
    /// The chain of v1.2 §35 is only intact when invalidation distance exists, every
    /// operator input is present, and the CFD map is valid.
    /// </summary>
    public bool SizingChainIntact =>
        InvalidationDistanceAvailable
        && MissingOperatorInputs.Count == 0
        && CfdMapping == CfdMappingState.Valid;
}

/// <summary>
/// Phase 4A Risk host (v1.2 §35-36).
///
/// v1.2 §35 fixes the order: Thesis -> Structural Invalidation -> Stop Distance ->
/// CFD Effective Stop Distance -> Risk Per Lot -> Allowed Size, and forbids reversing
/// it. Reversing it is how an account-first stop gets pulled into noise.
///
/// The chain breaks immediately after Thesis: invalidation is calibration-gated
/// (Phase 3C), so there is no stop distance, and without a stop distance there is no
/// risk per lot and no size. Eleven operator inputs are also absent, and the CFD map is
/// INVALID for want of a basis.
///
/// This module therefore emits no size. It reports exactly where the chain breaks.
/// </summary>
public sealed class RiskHost
{
    private RiskPolicyConfig _policy;
    private RiskSnapshot? _published;

    public RiskHost(RiskPolicyConfig? policy = null)
    {
        _policy = policy ?? new RiskPolicyConfig(enabled: false);
    }

    public RiskSnapshot? Current => _published;
    public RiskPolicyConfig Policy => _policy;

    public void Configure(RiskPolicyConfig policy)
    {
        if (policy is null) throw new ArgumentNullException(nameof(policy));
        _policy = policy;
        if (!_policy.Enabled)
            _published = DisabledSnapshot(DateTime.UtcNow);
    }

    public void Reset() => _published = null;

    public RiskSnapshot Rebuild(
        EntryPolicySnapshot? entry,
        CfdMappingSnapshot? cfd,
        DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;

        if (!_policy.Enabled)
        {
            _published = DisabledSnapshot(now);
            return _published;
        }

        // Step two of the chain. Invalidation dimensions exist (Phase 3C) but every one
        // of them is NOT CALIBRATED, so no distance can be derived from them.
        const bool invalidationDistanceAvailable = false;

        // All eleven §35.3 inputs are operator-supplied and none is wired.
        var missing = Enum.GetValues<RiskInput>().ToArray();

        var state = entry is null || cfd is null
            ? RiskModuleState.AwaitingInputs
            : RiskModuleState.Ready;

        _published = new RiskSnapshot(
            state,
            RiskPolicyConfig.PolicyVersion,
            RiskState.NotCalibrated,
            TradeAffordability.NotCalibrated,
            invalidationDistanceAvailable,
            missing,
            entry?.SelectedPlan ?? EntryPlanKind.ObserveOnly,
            cfd?.State ?? CfdMappingState.Invalid,
            now,
            BuildLimitations());

        return _published;
    }

    private static IReadOnlyList<string> BuildLimitations() => new[]
    {
        RiskPolicyConfig.LimitationSizingChainBroken,
        RiskPolicyConfig.LimitationOperatorInputsMissing,
        RiskPolicyConfig.LimitationNoStopDistance,
        RiskPolicyConfig.LimitationNoSize,
        // v1.2 §35.2: reduce size or skip the trade; never tighten the stop to fit.
        RiskPolicyConfig.LimitationNeverTightenStopToFitAccount,
        // v1.2 §35: the order is mandatory and must not be reversed.
        RiskPolicyConfig.LimitationSizingOrderIsMandatory,
        RiskPolicyConfig.LimitationRiskStateNotCalibrated,
        RiskPolicyConfig.LimitationDrawdownLedgerAbsent,
        RiskPolicyConfig.LimitationLiveOnly
    };

    private static RiskSnapshot DisabledSnapshot(DateTime now) =>
        new(RiskModuleState.Disabled,
            RiskPolicyConfig.PolicyVersion,
            RiskState.Unknown,
            TradeAffordability.Unknown,
            false,
            Enum.GetValues<RiskInput>().ToArray(),
            EntryPlanKind.ObserveOnly,
            CfdMappingState.Invalid,
            now,
            new[] { "MODULE_DISABLED" });
}
