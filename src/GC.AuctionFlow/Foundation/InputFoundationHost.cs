using GC.AuctionFlow.Orderflow;

namespace GC.AuctionFlow.Foundation;

/// <summary>
/// M1 orchestrator for the deterministic input foundation. The ATAS composition root feeds it canonical
/// events and lifecycle signals; it owns the integrity meter, lifecycle machine, and foundation hash,
/// and publishes a <see cref="FoundationCapabilitySnapshot"/> that gates readiness truthfully.
///
/// It is deterministic and ATAS-free (unit-testable): no wall-clock, no thread, no filesystem in its
/// semantic state. Recovery is production-wired by the caller via <see cref="NoteRecorderRecovery"/>
/// (the indicator runs <c>RecoveryScanner</c> and passes the classification in) so this stays pure.
///
/// It enables no analysis module. It only decides whether the (default-off) modules may be trusted,
/// and fails closed: while CONF-001 is unresolved there is no ApprovedForProduction session template,
/// so production never reaches AnalysisReady — which is the correct, truthful outcome, not a defect.
/// </summary>
public sealed class InputFoundationHost
{
    private readonly VersionStamp _versions;
    private readonly FoundationConfig _config;
    private readonly InputIntegrityMeter _meter = new();
    private readonly FoundationHash _hash = new();
    private readonly LifecycleStateMachine _lifecycle;

    private SessionTemplate _sessionTemplate;
    private string _contractId = "";
    private string _sourceId = "";
    private decimal _tickSize;
    private bool _contractAvailable;
    private bool _hasUnrecoveredGap;
    private long _classified, _unknownAggressor;
    private long _revision;
    private long _captureEpoch;
    private CapabilityAvailability _historicalTradeCapability = CapabilityAvailability.Unproven;
    private CapabilityAvailability _liveTradeCapability = CapabilityAvailability.Unknown;
    private ModuleReadiness _requiredModuleReadiness = ModuleReadiness.NotReady;
    private ModuleReadiness _optionalModuleReadiness = ModuleReadiness.Unavailable;
    private string? _restoreSnapshotId;

    public InputFoundationHost(VersionStamp versions, FoundationConfig config, SessionTemplate sessionTemplate)
    {
        _versions = versions;
        _config = config;
        _sessionTemplate = sessionTemplate;
        _lifecycle = new LifecycleStateMachine(config.ConfigVersion);
    }

    public LifecycleState LifecycleState => _lifecycle.State;
    public long CaptureEpoch => _captureEpoch;
    public long Revision => _revision;

    /// <summary>Cold attach mid-session → warm-up.</summary>
    public void Attach() => _lifecycle.OnAttach();

    /// <summary>Begin a deterministic rebuild pass.</summary>
    public void BeginRebuild() => _lifecycle.OnRebuild();

    /// <summary>Establish/validate contract identity and tick size (hard block if missing/invalid).</summary>
    public void SetContract(string contractId, decimal tickSize, string sourceId)
    {
        _contractId = contractId ?? "";
        _sourceId = sourceId ?? "";
        _tickSize = tickSize;
        _contractAvailable = !string.IsNullOrWhiteSpace(_contractId);
        if (!_contractAvailable)
            _lifecycle.OnHardBlock(FoundationReasonCodes.ContractIdentityMissing);
        else if (!PriceTickMath.IsValidTickSize(_tickSize))
            _lifecycle.OnHardBlock(FoundationReasonCodes.TickSizeInvalid);
    }

    /// <summary>Set/replace the session template. An identity change invalidates stale readiness.</summary>
    public void SetSessionTemplate(SessionTemplate template)
    {
        if (!string.Equals(_sessionTemplate.TemplateId, template.TemplateId, StringComparison.Ordinal) ||
            !string.Equals(_sessionTemplate.TemplateVersion, template.TemplateVersion, StringComparison.Ordinal))
        {
            if (_lifecycle.State == LifecycleState.AnalysisReady)
                _lifecycle.OnIdentityIncompatibility();
        }
        _sessionTemplate = template;
    }

    public void NoteHistoricalTradeCapability(CapabilityAvailability c) => _historicalTradeCapability = c;
    public void NoteLiveTradeCapability(CapabilityAvailability c) => _liveTradeCapability = c;
    public void NoteRequiredModuleReadiness(ModuleReadiness r) => _requiredModuleReadiness = r;
    public void NoteOptionalModuleReadiness(ModuleReadiness r) => _optionalModuleReadiness = r;

    /// <summary>Ingest one canonical event: integrity accounting + semantic hash. Returns the verdict.</summary>
    public InputIntegrityMeter.Verdict Ingest(in CanonicalInputEvent e)
    {
        var verdict = _meter.Observe(e);
        if (verdict != InputIntegrityMeter.Verdict.DuplicateRejected)
        {
            _hash.Append(e);
            if (e.Kind == InputEventKind.Trade)
            {
                if (e.Aggressor == AggressorSide.Unknown) _unknownAggressor++;
                else _classified++;
            }
            if (verdict == InputIntegrityMeter.Verdict.AcceptedLateRevision) _revision++;
        }
        return verdict;
    }

    /// <summary>Reconnect / capture-epoch change: new epoch, cannot silently double-count, enters recovery.</summary>
    public void NoteReconnect(string? restoreSnapshotId = null)
    {
        _captureEpoch++;
        _revision++;
        _restoreSnapshotId = restoreSnapshotId;
        _meter.ResetForNewEpoch();
        _hash.ResetForNewEpoch();
        _lifecycle.OnReconnect();
    }

    /// <summary>A directly-observed native-id gap (not a process-local counter). Blocks Ready until recovered.</summary>
    public void NoteMeasuredIdentityGap()
    {
        _meter.RecordMeasuredIdentityGap();
        _hasUnrecoveredGap = true;
        _lifecycle.OnIntegrityDiscontinuity(FoundationReasonCodes.UnrecoveredGapBlocksReady);
    }

    /// <summary>
    /// Production-wire for <c>RecoveryScanner</c>: the indicator scans the recorder session directory and
    /// passes whether a corrupt/incomplete segment was found. Corrupt ⇒ fail closed (recovery + gap),
    /// never a silent replay.
    /// </summary>
    public void NoteRecorderRecovery(bool corruptOrIncompleteSegmentFound, string? restoreSnapshotId = null)
    {
        _restoreSnapshotId = restoreSnapshotId;
        if (corruptOrIncompleteSegmentFound)
        {
            _hasUnrecoveredGap = true;
            _lifecycle.OnIntegrityDiscontinuity(FoundationReasonCodes.RecorderSegmentCorruptFailedClosed);
        }
    }

    /// <summary>Clear the unrecovered-gap flag only when the caller proves coherent resynchronization.</summary>
    public void MarkGapRecovered(string restoreSnapshotId)
    {
        _hasUnrecoveredGap = false;
        _restoreSnapshotId = restoreSnapshotId;
    }

    public void Stop() => _lifecycle.OnStop();

    /// <summary>
    /// Evaluate coherence, advance the lifecycle, and publish the deterministic foundation snapshot + hash.
    /// AnalysisReady requires: contract+tick+versions valid, an ApprovedForProduction session template,
    /// live trade capability available, no unrecovered gap, no non-dedupable overlap, warm-up complete,
    /// required modules ready, AND all M1 config parameters production-approved (so no proposed seed can
    /// unlock Ready). Historical tick parity is published truthfully as Unproven and does NOT force Ready.
    /// </summary>
    public (FoundationCapabilitySnapshot Snapshot, string Hash) Evaluate()
    {
        var counts = _meter.Snapshot();
        var reasons = new List<string>();

        // Version completeness (VER-001).
        if (!_versions.IsComplete)
        {
            reasons.Add(FoundationReasonCodes.VersionMissing);
            _lifecycle.OnHardBlock(FoundationReasonCodes.VersionMissing);
        }
        // Contract / tick hard blocks.
        if (!_contractAvailable) reasons.Add(FoundationReasonCodes.ContractIdentityMissing);
        if (!PriceTickMath.IsValidTickSize(_tickSize)) reasons.Add(FoundationReasonCodes.TickSizeInvalid);
        // Session template (session-dependent block; CONF-001 unresolved keeps this Degraded in prod).
        var sessionReason = _sessionTemplate.BlockingReasonCode();
        if (sessionReason is not null) reasons.Add(sessionReason);
        // Integrity.
        if (_hasUnrecoveredGap) reasons.Add(FoundationReasonCodes.UnrecoveredGapBlocksReady);
        if (counts.NonDeduplicable > 0) reasons.Add(FoundationReasonCodes.DedupNotPossible);
        if (counts.OutOfOrder > 0) reasons.Add(FoundationReasonCodes.OutOfOrderObserved);
        if (counts.LateEvents > 0) reasons.Add(FoundationReasonCodes.LateEventRevision);
        // Capability distinction.
        if (_historicalTradeCapability != CapabilityAvailability.Available)
            reasons.Add(FoundationReasonCodes.HistoricalTradeCapabilityUnproven);
        // Config governance (no proposed seed unlocks Ready).
        if (!_config.AllProductionApproved)
            reasons.Add(FoundationReasonCodes.UnapprovedParameterCannotUnlockReady);
        // Warm-up / required modules.
        var warmupComplete = counts.Accepted >= 1;
        if (!warmupComplete) reasons.Add(FoundationReasonCodes.WarmupIncomplete);
        if (_requiredModuleReadiness != ModuleReadiness.Ready)
            reasons.Add(FoundationReasonCodes.RequiredModuleRebuilding);

        var mandatoryCoherent =
            _versions.IsComplete &&
            _contractAvailable &&
            PriceTickMath.IsValidTickSize(_tickSize) &&
            _sessionTemplate.IsApprovedForProduction &&
            _liveTradeCapability == CapabilityAvailability.Available &&
            !_hasUnrecoveredGap &&
            counts.NonDeduplicable == 0 &&
            warmupComplete &&
            _requiredModuleReadiness == ModuleReadiness.Ready &&
            _config.AllProductionApproved;

        var blockingReason = reasons.Count > 0 ? reasons[0] : null;
        _lifecycle.TryBecomeReady(mandatoryCoherent, blockingReason);

        var lifecycle = _lifecycle.State;
        var snap = new FoundationCapabilitySnapshot(
            CapabilitySnapshotId: ComputeSnapshotId(counts, lifecycle),
            SourceId: _sourceId,
            ContractId: _contractId,
            ContractIdentityAvailable: _contractAvailable,
            TickSize: _tickSize,
            TickSizeValid: PriceTickMath.IsValidTickSize(_tickSize),
            SessionTemplateId: _sessionTemplate.TemplateId,
            SessionTemplateVersion: _sessionTemplate.TemplateVersion,
            SessionTemplateStatus: _sessionTemplate.Status,
            HistoricalTradeCapability: _historicalTradeCapability,
            LiveTradeCapability: _liveTradeCapability,
            DedupCapability: counts.NonDeduplicable > 0 ? DedupCapability.NotDeduplicable : DedupCapability.NativeStableId,
            ClassifiedCount: _classified,
            UnknownAggressorCount: _unknownAggressor,
            DuplicateCount: counts.Duplicates,
            OutOfOrderCount: counts.OutOfOrder,
            LateEventCount: counts.LateEvents,
            NonDeduplicableCount: counts.NonDeduplicable,
            MeasuredIdentityGapCount: counts.MeasuredIdentityGaps,
            HasUnrecoveredGap: _hasUnrecoveredGap,
            LastValidEventTimeUtc: counts.LastValidEventTimeUtc,
            LifecycleState: lifecycle,
            RequiredModuleReadiness: _requiredModuleReadiness,
            OptionalModuleReadiness: _optionalModuleReadiness,
            DataState: FoundationCapabilitySnapshot.ToDataState(lifecycle),
            Versions: _versions,
            ReasonCodes: reasons);

        var hash = _hash.Compute(_versions, _contractId, _sessionTemplate, _revision, _captureEpoch);
        return (snap, hash);
    }

    private string ComputeSnapshotId(InputIntegrityCounts counts, LifecycleState lifecycle)
    {
        // Deterministic id: no wall-clock, no thread — a fold of the semantic gate inputs.
        var payload = string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"{_contractId}|{(int)lifecycle}|{counts.Accepted}|{counts.Duplicates}|{counts.OutOfOrder}|{counts.LateEvents}|{_revision}|{_captureEpoch}|{_sessionTemplate.Canonical()}|{_versions.Canonical()}");
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(payload));
        return "cap:" + Convert.ToHexStringLower(bytes)[..16];
    }
}
