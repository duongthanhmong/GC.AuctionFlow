using GC.AuctionFlow.Maturity;

namespace GC.AuctionFlow.Entry;

public enum EntryPolicyModuleState
{
    Disabled = 0,
    AwaitingMaturity = 1,
    Ready = 2,
    Invalid = 3
}

/// <summary>
/// Execution plan shapes (v1.2 §30.1).
///
/// Only <see cref="ObserveOnly"/> is emittable. v1.2 §30.2 states that location is
/// prepared in advance but the TRIGGER decides how to enter, and §30.6 lists ten
/// inputs the selector needs. None of those ten is available, so choosing any active
/// plan would be a guess wearing the costume of a decision.
/// </summary>
public enum EntryPlanKind
{
    /// <summary>Prepare and watch. The only honest output while the selector is blind.</summary>
    ObserveOnly = 0,

    // Reserved — every one of these requires selector inputs that do not exist.
    PassiveLimit = 100,
    MarketableLimit = 101,
    StopMarket = 102,
    StopLimit = 103,
    Market = 104,
    HybridStaged = 105
}

/// <summary>
/// The ten Order Type Selector inputs of v1.2 §30.6.
/// Naming them individually is the point: it makes the dependency chain explicit and
/// testable instead of hiding behind a single "not ready" flag.
/// </summary>
public enum EntrySelectorInput
{
    AuctionTempo = 0,
    Spread = 1,
    Depth = 2,
    DomPersistence = 3,
    DistanceToInvalidation = 4,
    Urgency = 5,
    ExpectedSlippage = 6,
    MissedTradeCost = 7,
    SignalMaturity = 8,
    CfdBrokerConstraints = 9
}

/// <summary>Why a selector input cannot be used.</summary>
public enum SelectorInputAvailability
{
    /// <summary>No module produces it.</summary>
    NotBuilt = 0,

    /// <summary>Requires MBO, which is BLOCKED.</summary>
    MboBlocked = 1,

    /// <summary>Requires the Historical Scanner (Phase 5A).</summary>
    RequiresCalibration = 2,

    /// <summary>Produced, but the value itself is NOT CALIBRATED.</summary>
    PresentButNotCalibrated = 3,

    /// <summary>Operator must supply it; none has been supplied.</summary>
    OperatorInputMissing = 4,

    /// <summary>Usable.</summary>
    Available = 100
}

/// <summary>
/// Phase 4B Entry Policy Engine (v1.2 §30).
/// The baseline places no orders (v1.2 §2.8); this only describes how one WOULD enter.
/// </summary>
public sealed class EntryPolicyConfig
{
    public const string PolicyVersion = "ENTRY_POLICY_V1";

    public const string LimitationSelectorInputsUnavailable = "ENTRY_SELECTOR_INPUTS_UNAVAILABLE";
    public const string LimitationPlanSelectionNotCalibrated = "ENTRY_PLAN_SELECTION_NOT_CALIBRATED";
    public const string LimitationNoOrderPlacement = "ENTRY_POLICY_PLACES_NO_ORDERS";
    public const string LimitationNoFixedOrderTypeRatio = "NO_FIXED_ORDER_TYPE_DISTRIBUTION";
    public const string LimitationNoBlindLimitAtReference = "NO_BLIND_LIMIT_ON_REFERENCE_TOUCH";
    public const string LimitationNoEntryPrice = "ENTRY_POLICY_PRODUCES_NO_PRICE";
    public const string LimitationLiveOnly = "LIVE_ONLY_HISTORY";

    /// <summary>All ten inputs of v1.2 §30.6 must be available before a plan is selectable.</summary>
    public const int RequiredSelectorInputs = 10;

    public EntryPolicyConfig(bool enabled = false)
    {
        Enabled = enabled;
    }

    public bool Enabled { get; }
    public string Version => PolicyVersion;
}

/// <summary>One selector input and why it can or cannot be used.</summary>
public sealed class SelectorInputStatus
{
    public SelectorInputStatus(EntrySelectorInput input, SelectorInputAvailability availability, string reason)
    {
        Input = input;
        Availability = availability;
        Reason = reason ?? "";
    }

    public EntrySelectorInput Input { get; }
    public SelectorInputAvailability Availability { get; }
    public string Reason { get; }
    public bool IsUsable => Availability == SelectorInputAvailability.Available;
}

/// <summary>
/// Immutable Phase 4B entry policy snapshot.
/// SelectedPlan is always ObserveOnly and carries no price, size or side.
/// </summary>
public sealed class EntryPolicySnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public EntryPolicySnapshot(
        EntryPolicyModuleState moduleState,
        string policyVersion,
        EntryPlanKind selectedPlan,
        IReadOnlyList<SelectorInputStatus> selectorInputs,
        int usableInputCount,
        DateTime observedAtUtc,
        IReadOnlyList<string> limitations)
    {
        ModuleState = moduleState;
        PolicyVersion = policyVersion ?? EntryPolicyConfig.PolicyVersion;
        SelectedPlan = selectedPlan;
        SelectorInputs = selectorInputs ?? Array.Empty<SelectorInputStatus>();
        UsableInputCount = usableInputCount;
        ObservedAtUtc = observedAtUtc;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public EntryPolicyModuleState ModuleState { get; }
    public string PolicyVersion { get; }

    /// <summary>Always ObserveOnly — every active plan is reserved.</summary>
    public EntryPlanKind SelectedPlan { get; }

    /// <summary>All ten §30.6 inputs, each with the reason it is or is not usable.</summary>
    public IReadOnlyList<SelectorInputStatus> SelectorInputs { get; }

    public int UsableInputCount { get; }
    public DateTime ObservedAtUtc { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;

    public bool SelectorReady =>
        UsableInputCount >= EntryPolicyConfig.RequiredSelectorInputs;

    public IReadOnlyList<EntrySelectorInput> MissingInputs =>
        SelectorInputs.Where(i => !i.IsUsable).Select(i => i.Input).ToArray();
}

/// <summary>
/// Phase 4B Entry Policy host (v1.2 §30).
///
/// v1.2 §30.6 lists ten inputs the order-type selector needs. None is currently
/// available: tempo, spread and urgency are unbuilt; depth and DOM persistence need
/// MBO, which is BLOCKED; expected slippage and missed-trade cost need the Historical
/// Scanner; distance-to-invalidation depends on invalidation, which is NOT CALIBRATED;
/// signal maturity is produced but always NotCalibrated; broker constraints are an
/// operator input that has not been supplied.
///
/// So the only emittable plan is ObserveOnly. That is the correct answer rather than a
/// gap to route around — v1.2 §30.7 forbids assuming any fixed order-type distribution,
/// and §30.3 forbids a blind limit merely because price touched a reference.
///
/// The value of this module is that the dependency chain is now explicit and testable.
/// </summary>
public sealed class EntryPolicyHost
{
    private EntryPolicyConfig _policy;
    private EntryPolicySnapshot? _published;

    public EntryPolicyHost(EntryPolicyConfig? policy = null)
    {
        _policy = policy ?? new EntryPolicyConfig(enabled: false);
    }

    public EntryPolicySnapshot? Current => _published;
    public EntryPolicyConfig Policy => _policy;

    public void Configure(EntryPolicyConfig policy)
    {
        if (policy is null) throw new ArgumentNullException(nameof(policy));
        _policy = policy;
        if (!_policy.Enabled)
            _published = DisabledSnapshot(DateTime.UtcNow);
    }

    public void Reset() => _published = null;

    public EntryPolicySnapshot Rebuild(
        SignalMaturitySetSnapshot? maturity,
        bool mboActive = false,
        DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;

        if (!_policy.Enabled)
        {
            _published = DisabledSnapshot(now);
            return _published;
        }

        var inputs = BuildSelectorInputs(maturity, mboActive);
        var usable = inputs.Count(i => i.IsUsable);

        var state = maturity is null
            ? EntryPolicyModuleState.AwaitingMaturity
            : EntryPolicyModuleState.Ready;

        _published = new EntryPolicySnapshot(
            state,
            EntryPolicyConfig.PolicyVersion,
            // The selector is blind, so preparation is the only honest plan.
            EntryPlanKind.ObserveOnly,
            inputs,
            usable,
            now,
            BuildLimitations());

        return _published;
    }

    /// <summary>
    /// Each input reports WHY it cannot be used. A single "not ready" flag would hide
    /// which dependency is actually blocking, and there are five different reasons here.
    /// </summary>
    private static IReadOnlyList<SelectorInputStatus> BuildSelectorInputs(
        SignalMaturitySetSnapshot? maturity, bool mboActive)
    {
        var maturityStatus = maturity is null
            ? new SelectorInputStatus(EntrySelectorInput.SignalMaturity,
                SelectorInputAvailability.NotBuilt, "Signal maturity module not running")
            : new SelectorInputStatus(EntrySelectorInput.SignalMaturity,
                SelectorInputAvailability.PresentButNotCalibrated,
                "Maturity produced but Fast/Standard/Confirmed are NOT CALIBRATED");

        var depthReason = mboActive
            ? "MBO active but depth aggregation not built"
            : "Requires MBO, which is BLOCKED";
        var depthAvailability = mboActive
            ? SelectorInputAvailability.NotBuilt
            : SelectorInputAvailability.MboBlocked;

        return new[]
        {
            new SelectorInputStatus(EntrySelectorInput.AuctionTempo,
                SelectorInputAvailability.NotBuilt, "Auction Tempo (v1.2 §25.2) not built"),
            new SelectorInputStatus(EntrySelectorInput.Spread,
                SelectorInputAvailability.NotBuilt, "Spread is not measured"),
            new SelectorInputStatus(EntrySelectorInput.Depth,
                depthAvailability, depthReason),
            new SelectorInputStatus(EntrySelectorInput.DomPersistence,
                depthAvailability, depthReason),
            new SelectorInputStatus(EntrySelectorInput.DistanceToInvalidation,
                SelectorInputAvailability.PresentButNotCalibrated,
                "Invalidation dimensions exist but all are NOT CALIBRATED (Phase 3C)"),
            new SelectorInputStatus(EntrySelectorInput.Urgency,
                SelectorInputAvailability.NotBuilt, "Urgency is not defined"),
            new SelectorInputStatus(EntrySelectorInput.ExpectedSlippage,
                SelectorInputAvailability.RequiresCalibration,
                "Needs the Historical Scanner (Phase 5A)"),
            new SelectorInputStatus(EntrySelectorInput.MissedTradeCost,
                SelectorInputAvailability.RequiresCalibration,
                "Needs the Historical Scanner (Phase 5A)"),
            maturityStatus,
            new SelectorInputStatus(EntrySelectorInput.CfdBrokerConstraints,
                SelectorInputAvailability.OperatorInputMissing,
                "Operator input; none supplied")
        };
    }

    private static IReadOnlyList<string> BuildLimitations() => new[]
    {
        EntryPolicyConfig.LimitationSelectorInputsUnavailable,
        EntryPolicyConfig.LimitationPlanSelectionNotCalibrated,
        // v1.2 §2.8: the baseline never places an order.
        EntryPolicyConfig.LimitationNoOrderPlacement,
        // v1.2 §30.7: no fixed order-type distribution may be assumed.
        EntryPolicyConfig.LimitationNoFixedOrderTypeRatio,
        // v1.2 §30.3: a reference touch alone never justifies a limit.
        EntryPolicyConfig.LimitationNoBlindLimitAtReference,
        EntryPolicyConfig.LimitationNoEntryPrice,
        EntryPolicyConfig.LimitationLiveOnly
    };

    private static EntryPolicySnapshot DisabledSnapshot(DateTime now) =>
        new(EntryPolicyModuleState.Disabled,
            EntryPolicyConfig.PolicyVersion,
            EntryPlanKind.ObserveOnly,
            Array.Empty<SelectorInputStatus>(),
            0, now,
            new[] { "MODULE_DISABLED" });
}
