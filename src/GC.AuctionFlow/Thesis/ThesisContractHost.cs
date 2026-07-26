using GC.AuctionFlow.Maturity;

namespace GC.AuctionFlow.Thesis;

/// <summary>
/// Phase 3C Thesis Contract host.
/// Consumes the Phase 3B SignalMaturitySetSnapshot immutably and declares, for
/// every candidate, the contract required by v1.2 §32: five horizons, expected
/// behaviour, five invalidation dimensions, source-of-move, and the structured
/// list of evidence the thesis is still missing.
///
/// Nothing here is executable. Contract state is always NotCalibrated, expiry is
/// always null, and no price, stop, target or size is ever produced.
/// </summary>
public sealed class ThesisContractHost
{
    private ThesisContractPolicyConfig _policy;
    private ThesisContractSetSnapshot? _published;
    private string? _lastFingerprintKey;
    private DateTime _createdAtUtc;
    private readonly List<ThesisContractSnapshot> _closedList = new();
    private readonly HashSet<string> _closedIds = new(StringComparer.Ordinal);

    public ThesisContractHost(ThesisContractPolicyConfig? policy = null)
    {
        _policy = policy ?? new ThesisContractPolicyConfig(enabled: false);
    }

    public ThesisContractSetSnapshot? Current => _published;
    public ThesisContractPolicyConfig Policy => _policy;

    public void Configure(ThesisContractPolicyConfig policy)
    {
        if (policy is null) throw new ArgumentNullException(nameof(policy));
        _policy = policy;
        if (!_policy.Enabled)
        {
            ResetInternal();
            _published = DisabledSnapshot(DateTime.UtcNow);
            _lastFingerprintKey = null;
        }
    }

    public void Reset()
    {
        ResetInternal();
        _published = null;
        _lastFingerprintKey = null;
        _createdAtUtc = default;
    }

    /// <summary>
    /// Rebuild contracts from the current signal maturity set.
    /// Fingerprint-gated: returns cached when maturity is unchanged.
    /// </summary>
    public ThesisContractSetSnapshot Rebuild(
        SignalMaturitySetSnapshot? maturity,
        DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;

        if (!_policy.Enabled)
        {
            ResetInternal();
            _published = DisabledSnapshot(now);
            return _published;
        }

        var fp = BuildFingerprintKey(maturity);
        if (_lastFingerprintKey is not null
            && string.Equals(_lastFingerprintKey, fp, StringComparison.Ordinal)
            && _published is not null)
            return _published;

        if (maturity is null || maturity.ModuleState == MaturityModuleState.Disabled)
        {
            _published = StatusSnapshot(ThesisContractModuleState.AwaitingMaturity, now);
            _lastFingerprintKey = fp;
            return _published;
        }

        if (maturity.ModuleState == MaturityModuleState.Invalid)
        {
            _published = StatusSnapshot(ThesisContractModuleState.Invalid, now,
                new[] { "MATURITY_INPUT_INVALID" });
            _lastFingerprintKey = fp;
            return _published;
        }

        if (_createdAtUtc == default)
            _createdAtUtc = now;

        var active = new List<ThesisContractSnapshot>(maturity.ActiveCandidates.Count);
        long maxRev = -1L;
        ThesisContractSnapshot? latest = null;
        int partial = 0;

        foreach (var sm in maturity.ActiveCandidates)
        {
            var c = MapToContract(sm, now);
            active.Add(c);
            if (c.EventRevision >= maxRev)
            {
                maxRev = c.EventRevision;
                latest = c;
            }
            if (c.DataQuality == ThesisContractDataQuality.Partial) partial++;
        }

        foreach (var sm in maturity.RecentlyClosed)
        {
            var c = MapToContract(sm, now);
            if (!_closedIds.Add(c.ContractId)) continue;
            _closedList.Add(c);
            if (_closedList.Count > ThesisContractSetSnapshot.RecentlyClosedCapacity)
            {
                var removed = _closedList[0];
                _closedList.RemoveAt(0);
                _closedIds.Remove(removed.ContractId);
            }
        }

        ThesisContractModuleState state;
        if (active.Count == 0)
            state = ThesisContractModuleState.AwaitingMaturity;
        else if (partial > 0 || maturity.ModuleState == MaturityModuleState.Partial)
            state = ThesisContractModuleState.Partial;
        else
            state = ThesisContractModuleState.Ready;

        _published = new ThesisContractSetSnapshot(
            state,
            ThesisContractPolicyConfig.PolicyVersion,
            active,
            _closedList.ToArray(),
            latest,
            active.Count,
            0, 0,
            ThesisContractPolicyConfig.ProtectiveStopAuthorized,
            _createdAtUtc,
            now,
            BuildSetLimitations());

        _lastFingerprintKey = fp;
        return _published;
    }

    // --- mapping ---

    private static ThesisContractSnapshot MapToContract(SignalMaturitySnapshot sm, DateTime nowUtc)
    {
        var family = sm.ThesisFamily switch
        {
            "FAR" => ThesisFamily.Far,
            "AAC" => ThesisFamily.Aac,
            _     => ThesisFamily.Unknown
        };

        var quality = sm.DataQuality switch
        {
            MaturityDataQuality.Invalid => ThesisContractDataQuality.Invalid,
            MaturityDataQuality.Partial => ThesisContractDataQuality.Partial,
            _                           => ThesisContractDataQuality.Complete
        };

        return new ThesisContractSnapshot(
            BuildContractId(sm.SnapshotId),
            ThesisContractPolicyConfig.PolicyVersion,
            sm.SnapshotId,
            sm.ThesisId,
            family,
            sm.Direction,
            BuildHorizons(),
            // Source-of-move requires the multi-horizon map (v1.2 §11.3), which is
            // not authorized yet. Reporting Unknown is the truthful answer.
            SourceOfMove.Unknown,
            sm.ExpectedBehavior,
            // v1.2 §32.1 ExpiresAt: duration is NOT CALIBRATED — never fabricated.
            null,
            BuildInvalidations(),
            BuildMissingEvidence(sm),
            // No post-entry information exists in Phase 3C, so no gate condition
            // can be satisfied. All four flags stay false rather than defaulting true.
            new ThesisConsistencyGate(false, false, false, false),
            ThesisContractState.NotCalibrated,
            true,
            quality,
            sm.StateVersion,
            sm.EventRevision,
            nowUtc,
            BuildContractLimitations());
    }

    /// <summary>
    /// All five roles of v1.2 §11.2 are always declared. The multi-horizon map is
    /// not authorized yet, so every source is Unavailable — the roles exist so a
    /// contract can never silently omit one.
    /// </summary>
    private static IReadOnlyList<ThesisHorizonDeclaration> BuildHorizons() => new[]
    {
        new ThesisHorizonDeclaration(ThesisHorizonRole.Context,    ThesisHorizonSource.Unavailable),
        new ThesisHorizonDeclaration(ThesisHorizonRole.Thesis,     ThesisHorizonSource.Unavailable),
        new ThesisHorizonDeclaration(ThesisHorizonRole.Trigger,    ThesisHorizonSource.Unavailable),
        new ThesisHorizonDeclaration(ThesisHorizonRole.Management, ThesisHorizonSource.Unavailable),
        new ThesisHorizonDeclaration(ThesisHorizonRole.Target,     ThesisHorizonSource.Unavailable)
    };

    /// <summary>
    /// All five dimensions of v1.2 §32.2 + v1.3 §11.3. Every dimension is
    /// Monitoring with its own NOT_CALIBRATED limitation: the engine watches the
    /// dimension but cannot yet decide that it has fired.
    /// </summary>
    private static IReadOnlyList<InvalidationDimensionDeclaration> BuildInvalidations() => new[]
    {
        new InvalidationDimensionDeclaration(InvalidationDimension.Price,
            InvalidationDimensionState.NotCalibrated,
            ThesisContractPolicyConfig.LimitationPriceInvalidationNotCalibrated),
        new InvalidationDimensionDeclaration(InvalidationDimension.Auction,
            InvalidationDimensionState.NotCalibrated,
            ThesisContractPolicyConfig.LimitationAuctionInvalidationNotCalibrated),
        new InvalidationDimensionDeclaration(InvalidationDimension.Time,
            InvalidationDimensionState.NotCalibrated,
            ThesisContractPolicyConfig.LimitationTimeInvalidationNotCalibrated),
        new InvalidationDimensionDeclaration(InvalidationDimension.Context,
            InvalidationDimensionState.NotCalibrated,
            ThesisContractPolicyConfig.LimitationContextInvalidationNotCalibrated),
        new InvalidationDimensionDeclaration(InvalidationDimension.Evidence,
            InvalidationDimensionState.NotCalibrated,
            ThesisContractPolicyConfig.LimitationEvidenceInvalidationNotCalibrated)
    };

    /// <summary>
    /// v1.3 G-THE-001: a thesis must be able to state what it lacks.
    /// Never empty in Phase 3C.
    /// </summary>
    private static IReadOnlyList<MissingEvidenceKind> BuildMissingEvidence(SignalMaturitySnapshot sm)
    {
        var missing = new List<MissingEvidenceKind>
        {
            MissingEvidenceKind.AcceptanceResolutionNotCalibrated,
            MissingEvidenceKind.ReentryResolutionNotCalibrated,
            MissingEvidenceKind.OldValueReclaimNotObserved,
            MissingEvidenceKind.TradeFacilitationNotCalibrated,
            MissingEvidenceKind.EffortResultNotCalibrated,
            MissingEvidenceKind.TargetSpaceUnavailable,
            MissingEvidenceKind.HorizonMapUnavailable,
            MissingEvidenceKind.MboBlocked
        };
        if (sm.MaturityLevel != SignalMaturityLevel.NotCalibrated
            || sm.BlockingReasons.Count > 0)
            missing.Add(MissingEvidenceKind.SignalMaturityNotCalibrated);
        return missing;
    }

    // --- helpers ---

    private static string BuildContractId(string maturitySnapshotId) => "TC:" + maturitySnapshotId;

    private static string BuildFingerprintKey(SignalMaturitySetSnapshot? maturity) =>
        ThesisContractPolicyConfig.PolicyVersion
        + "|" + (maturity?.ModuleState.ToString() ?? "")
        + "|" + (maturity?.LastUpdatedAtUtc.Ticks.ToString() ?? "")
        + "|" + (maturity?.ActiveCandidates.Count.ToString() ?? "");

    private static IReadOnlyList<string> BuildContractLimitations() => new[]
    {
        ThesisContractPolicyConfig.LimitationNotCalibrated,
        ThesisContractPolicyConfig.LimitationCompleteNotCalibrated,
        ThesisContractPolicyConfig.LimitationExecutableNotAuthorized,
        ThesisContractPolicyConfig.LimitationPriceInvalidationNotCalibrated,
        ThesisContractPolicyConfig.LimitationAuctionInvalidationNotCalibrated,
        ThesisContractPolicyConfig.LimitationTimeInvalidationNotCalibrated,
        ThesisContractPolicyConfig.LimitationContextInvalidationNotCalibrated,
        ThesisContractPolicyConfig.LimitationEvidenceInvalidationNotCalibrated,
        ThesisContractPolicyConfig.LimitationExpiryNotCalibrated,
        ThesisContractPolicyConfig.LimitationHorizonMapUnavailable,
        ThesisContractPolicyConfig.LimitationNoProtectiveStop,
        ThesisContractPolicyConfig.LimitationLiveOnly
    };

    private static IReadOnlyList<string> BuildSetLimitations() => new[]
    {
        ThesisContractPolicyConfig.LimitationNotCalibrated,
        ThesisContractPolicyConfig.LimitationCompleteNotCalibrated,
        ThesisContractPolicyConfig.LimitationExecutableNotAuthorized,
        ThesisContractPolicyConfig.LimitationExpiryNotCalibrated,
        ThesisContractPolicyConfig.LimitationHorizonMapUnavailable,
        ThesisContractPolicyConfig.LimitationNoProtectiveStop,
        ThesisContractPolicyConfig.LimitationNoTargetPath,
        ThesisContractPolicyConfig.LimitationNoRiskSizing,
        ThesisContractPolicyConfig.LimitationLiveOnly
    };

    private void ResetInternal()
    {
        _closedList.Clear();
        _closedIds.Clear();
    }

    private ThesisContractSetSnapshot DisabledSnapshot(DateTime now) =>
        new ThesisContractSetSnapshot(
            ThesisContractModuleState.Disabled,
            ThesisContractPolicyConfig.PolicyVersion,
            Array.Empty<ThesisContractSnapshot>(),
            Array.Empty<ThesisContractSnapshot>(),
            null, 0, 0, 0,
            ThesisContractPolicyConfig.ProtectiveStopAuthorized,
            now, now,
            new[] { "MODULE_DISABLED" });

    private ThesisContractSetSnapshot StatusSnapshot(
        ThesisContractModuleState state, DateTime now, string[]? extra = null)
    {
        if (_createdAtUtc == default) _createdAtUtc = now;
        var lim = new List<string>
        {
            ThesisContractPolicyConfig.LimitationNotCalibrated,
            ThesisContractPolicyConfig.LimitationExecutableNotAuthorized,
            ThesisContractPolicyConfig.LimitationLiveOnly
        };
        if (extra is not null) lim.AddRange(extra);
        return new ThesisContractSetSnapshot(
            state,
            ThesisContractPolicyConfig.PolicyVersion,
            Array.Empty<ThesisContractSnapshot>(),
            _closedList.ToArray(),
            null, 0, 0, 0,
            ThesisContractPolicyConfig.ProtectiveStopAuthorized,
            _createdAtUtc, now,
            lim);
    }
}
