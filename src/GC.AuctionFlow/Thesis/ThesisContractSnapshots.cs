using GC.AuctionFlow.Maturity;

namespace GC.AuctionFlow.Thesis;

/// <summary>
/// One declared thesis horizon (v1.2 §11.2). All five roles must be present
/// on a contract; a role whose horizon cannot be resolved reports
/// <see cref="ThesisHorizonSource.Unavailable"/> rather than being omitted.
/// </summary>
public sealed class ThesisHorizonDeclaration
{
    public ThesisHorizonDeclaration(ThesisHorizonRole role, ThesisHorizonSource source)
    {
        Role = role;
        Source = source;
    }

    public ThesisHorizonRole Role { get; }
    public ThesisHorizonSource Source { get; }
    public bool IsResolved => Source != ThesisHorizonSource.Unavailable;
}

/// <summary>
/// One invalidation dimension (v1.2 §32.2 + v1.3 §11.3 Evidence).
/// Carries no price: analytical invalidation is not a protective stop
/// (v1.2 §32.3), and stop calculation is not authorized in Phase 3C.
/// </summary>
public sealed class InvalidationDimensionDeclaration
{
    public InvalidationDimensionDeclaration(
        InvalidationDimension dimension,
        InvalidationDimensionState state,
        string limitation)
    {
        Dimension = dimension;
        State = state;
        Limitation = limitation ?? "";
    }

    public InvalidationDimension Dimension { get; }
    public InvalidationDimensionState State { get; }
    public string Limitation { get; }
}

/// <summary>
/// Thesis consistency law (v1.3 §11.4 / G-THE-006).
/// New information may only change a live trade when all four hold.
/// Four separate flags — deliberately not collapsed into one score.
/// </summary>
public sealed class ThesisConsistencyGate
{
    public ThesisConsistencyGate(
        bool relatesToThesis,
        bool producedPriceResult,
        bool wasMaintained,
        bool withinDeclaredHorizon)
    {
        RelatesToThesis = relatesToThesis;
        ProducedPriceResult = producedPriceResult;
        WasMaintained = wasMaintained;
        WithinDeclaredHorizon = withinDeclaredHorizon;
    }

    /// <summary>(a) directly relates to the thesis.</summary>
    public bool RelatesToThesis { get; }
    /// <summary>(b) produced a result on price.</summary>
    public bool ProducedPriceResult { get; }
    /// <summary>(c) was maintained.</summary>
    public bool WasMaintained { get; }
    /// <summary>(d) occurred within the declared horizon.</summary>
    public bool WithinDeclaredHorizon { get; }

    public bool AllSatisfied =>
        RelatesToThesis && ProducedPriceResult && WasMaintained && WithinDeclaredHorizon;
}

/// <summary>
/// Immutable Phase 3C thesis contract for one maturity scope.
/// Contract state is always NotCalibrated — Complete/Executable are reserved.
/// Carries no entry, stop, target or size field by design.
/// </summary>
public sealed class ThesisContractSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public ThesisContractSnapshot(
        string contractId,
        string policyVersion,
        string maturityId,
        string thesisId,
        ThesisFamily family,
        ThesisDirection direction,
        IReadOnlyList<ThesisHorizonDeclaration> horizons,
        SourceOfMove sourceOfMove,
        ExpectedBehaviorContractKind expectedBehavior,
        DateTime? expiresAtUtc,
        IReadOnlyList<InvalidationDimensionDeclaration> invalidations,
        IReadOnlyList<MissingEvidenceKind> missingEvidence,
        ThesisConsistencyGate consistencyGate,
        ThesisContractState contractState,
        bool notCalibrated,
        ThesisContractDataQuality dataQuality,
        long stateVersion,
        long eventRevision,
        DateTime observedAtUtc,
        IReadOnlyList<string> limitations)
    {
        ContractId = contractId ?? "";
        PolicyVersion = policyVersion ?? ThesisContractPolicyConfig.PolicyVersion;
        MaturityId = maturityId ?? "";
        ThesisId = thesisId ?? "";
        Family = family;
        Direction = direction;
        Horizons = horizons ?? Array.Empty<ThesisHorizonDeclaration>();
        SourceOfMove = sourceOfMove;
        ExpectedBehavior = expectedBehavior;
        ExpiresAtUtc = expiresAtUtc;
        Invalidations = invalidations ?? Array.Empty<InvalidationDimensionDeclaration>();
        MissingEvidence = missingEvidence ?? Array.Empty<MissingEvidenceKind>();
        ConsistencyGate = consistencyGate;
        ContractState = contractState;
        NotCalibrated = notCalibrated;
        DataQuality = dataQuality;
        StateVersion = stateVersion;
        EventRevision = eventRevision;
        ObservedAtUtc = observedAtUtc;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public string ContractId { get; }
    public string PolicyVersion { get; }
    public string MaturityId { get; }
    public string ThesisId { get; }
    public ThesisFamily Family { get; }
    public ThesisDirection Direction { get; }
    /// <summary>Always five entries — v1.2 §11.2.</summary>
    public IReadOnlyList<ThesisHorizonDeclaration> Horizons { get; }
    public SourceOfMove SourceOfMove { get; }
    public ExpectedBehaviorContractKind ExpectedBehavior { get; }
    /// <summary>Always null in Phase 3C — expiry duration NOT CALIBRATED.</summary>
    public DateTime? ExpiresAtUtc { get; }
    /// <summary>Always five entries — v1.2 §32.2 + v1.3 §11.3.</summary>
    public IReadOnlyList<InvalidationDimensionDeclaration> Invalidations { get; }
    /// <summary>Structured, never free text (v1.3 G-THE-001). Never empty in Phase 3C.</summary>
    public IReadOnlyList<MissingEvidenceKind> MissingEvidence { get; }
    public ThesisConsistencyGate ConsistencyGate { get; }
    public ThesisContractState ContractState { get; }
    public bool NotCalibrated { get; }
    public ThesisContractDataQuality DataQuality { get; }
    public long StateVersion { get; }
    public long EventRevision { get; }
    public DateTime ObservedAtUtc { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;

    public bool AllHorizonRolesDeclared =>
        Horizons.Count == ThesisContractPolicyConfig.RequiredHorizonRoles;

    public bool AllInvalidationDimensionsDeclared =>
        Invalidations.Count == ThesisContractPolicyConfig.RequiredInvalidationDimensions;
}

/// <summary>
/// Immutable Phase 3C thesis contract set.
/// CompleteCount and ExecutableCount are always 0 — NOT CALIBRATED.
/// </summary>
public sealed class ThesisContractSetSnapshot
{
    public const string SnapshotVersion = "1.0.0";
    public const int RecentlyClosedCapacity = 64;

    public ThesisContractSetSnapshot(
        ThesisContractModuleState moduleState,
        string policyVersion,
        IReadOnlyList<ThesisContractSnapshot> activeContracts,
        IReadOnlyList<ThesisContractSnapshot> recentlyClosed,
        ThesisContractSnapshot? latestUpdated,
        int declaredCount,
        int completeCount,
        int executableCount,
        bool protectiveStopAuthorized,
        DateTime createdAtUtc,
        DateTime lastUpdatedAtUtc,
        IReadOnlyList<string> limitations)
    {
        ModuleState = moduleState;
        PolicyVersion = policyVersion ?? ThesisContractPolicyConfig.PolicyVersion;
        ActiveContracts = activeContracts ?? Array.Empty<ThesisContractSnapshot>();
        RecentlyClosed = recentlyClosed ?? Array.Empty<ThesisContractSnapshot>();
        LatestUpdated = latestUpdated;
        DeclaredCount = declaredCount;
        CompleteCount = completeCount;
        ExecutableCount = executableCount;
        ProtectiveStopAuthorized = protectiveStopAuthorized;
        CreatedAtUtc = createdAtUtc;
        LastUpdatedAtUtc = lastUpdatedAtUtc;
        Limitations = limitations ?? Array.Empty<string>();
    }

    public ThesisContractModuleState ModuleState { get; }
    public string PolicyVersion { get; }
    public IReadOnlyList<ThesisContractSnapshot> ActiveContracts { get; }
    public IReadOnlyList<ThesisContractSnapshot> RecentlyClosed { get; }
    public ThesisContractSnapshot? LatestUpdated { get; }
    public int DeclaredCount { get; }
    /// <summary>Always 0 — NOT CALIBRATED.</summary>
    public int CompleteCount { get; }
    /// <summary>Always 0 — NOT CALIBRATED.</summary>
    public int ExecutableCount { get; }
    /// <summary>Always false — v1.2 §32.3/§32.4 not authorized in Phase 3C.</summary>
    public bool ProtectiveStopAuthorized { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime LastUpdatedAtUtc { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string Version => SnapshotVersion;
}
