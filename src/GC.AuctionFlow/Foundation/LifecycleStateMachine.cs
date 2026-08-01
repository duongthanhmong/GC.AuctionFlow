namespace GC.AuctionFlow.Foundation;

/// <summary>One deterministic lifecycle transition, with cause.</summary>
public readonly record struct LifecycleTransition(
    LifecycleState From,
    LifecycleState To,
    string ReasonCode,
    string? RestoreSnapshotId,
    string ConfigVersion);

/// <summary>
/// TTS §10.1 deterministic lifecycle. Transitions are pure functions of (current state, signal); no
/// wall-clock, no thread input. Only <see cref="LifecycleState.AnalysisReady"/> permits a confirmed
/// downstream publish (<see cref="AllowsConfirmedPublish"/>). Reaching AnalysisReady additionally
/// requires that all mandatory capabilities are coherent — the host supplies that via
/// <see cref="TryBecomeReady"/>. This machine does NOT enable any analysis module; it only gates them.
/// </summary>
public sealed class LifecycleStateMachine
{
    public LifecycleState State { get; private set; } = LifecycleState.ColdStart;
    public string? RestoreSnapshotId { get; private set; }
    public string ConfigVersion { get; private set; }

    private readonly List<LifecycleTransition> _log = new();
    public IReadOnlyList<LifecycleTransition> Log => _log;

    public LifecycleStateMachine(string configVersion) => ConfigVersion = configVersion;

    /// <summary>Confirmed downstream publication is allowed only in AnalysisReady.</summary>
    public bool AllowsConfirmedPublish => State == LifecycleState.AnalysisReady;

    private LifecycleState Move(LifecycleState to, string reason, string? restoreSnapshotId = null)
    {
        if (restoreSnapshotId is not null) RestoreSnapshotId = restoreSnapshotId;
        _log.Add(new LifecycleTransition(State, to, reason, RestoreSnapshotId, ConfigVersion));
        State = to;
        return State;
    }

    /// <summary>Attach mid-session / cold attach → warm-up.</summary>
    public LifecycleState OnAttach() =>
        Move(LifecycleState.HistoricalWarmup, FoundationReasonCodes.WarmupIncomplete);

    /// <summary>Begin a full rebuild (e.g. deferred first pass).</summary>
    public LifecycleState OnRebuild() =>
        Move(LifecycleState.Rebuilding, FoundationReasonCodes.Rebuilding);

    /// <summary>Reconnect or capture-epoch change → recovery; can never remain silently Ready.</summary>
    public LifecycleState OnReconnect() =>
        Move(LifecycleState.Recovering, FoundationReasonCodes.RecoveringAfterReconnect);

    /// <summary>An integrity discontinuity (measured gap, corrupt segment) → recovery.</summary>
    public LifecycleState OnIntegrityDiscontinuity(string reasonCode) =>
        Move(LifecycleState.Recovering, reasonCode);

    /// <summary>Contract/config/session identity incompatibility invalidates any stale readiness.</summary>
    public LifecycleState OnIdentityIncompatibility() =>
        Move(LifecycleState.Invalid, FoundationReasonCodes.StaleReadinessInvalidatedByIdentity);

    /// <summary>A hard data block (bad contract/tick/versions) → invalid.</summary>
    public LifecycleState OnHardBlock(string reasonCode) =>
        Move(LifecycleState.Invalid, reasonCode);

    /// <summary>A recoverable degradation (e.g. session template not approved) → degraded, not ready.</summary>
    public LifecycleState OnDegrade(string reasonCode) =>
        Move(LifecycleState.Degraded, reasonCode);

    /// <summary>Disposal.</summary>
    public LifecycleState OnStop() =>
        Move(LifecycleState.Stopped, FoundationReasonCodes.IndicatorStopped);

    /// <summary>Config change re-versions restored state and drops readiness back to rebuild.</summary>
    public LifecycleState OnConfigChanged(string newConfigVersion)
    {
        ConfigVersion = newConfigVersion;
        return Move(LifecycleState.Rebuilding, FoundationReasonCodes.Rebuilding);
    }

    /// <summary>
    /// Attempt to reach AnalysisReady. Succeeds only from a non-terminal, non-invalid state AND when the
    /// host reports all mandatory capabilities coherent (no hard block, no unrecovered gap, no
    /// non-dedupable overlap, warm-up complete, all required modules ready). Otherwise stays put.
    /// </summary>
    public LifecycleState TryBecomeReady(bool mandatoryCoherent, string? blockingReason)
    {
        if (State is LifecycleState.Stopped or LifecycleState.Invalid)
            return State; // terminal / hard-blocked: cannot self-promote
        if (mandatoryCoherent)
            return Move(LifecycleState.AnalysisReady, "FND_READY", RestoreSnapshotId);
        // not coherent: express why, remain gated (degraded if we were ready, else keep warming)
        var reason = blockingReason ?? FoundationReasonCodes.WarmupIncomplete;
        if (State == LifecycleState.AnalysisReady)
            return Move(LifecycleState.Degraded, reason);
        return Move(State == LifecycleState.ColdStart ? LifecycleState.HistoricalWarmup : State, reason);
    }
}
