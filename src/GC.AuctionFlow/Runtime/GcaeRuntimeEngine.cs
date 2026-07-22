using GC.AuctionFlow.Core;
using GC.AuctionFlow.Probe;

namespace GC.AuctionFlow.Runtime;

/// <summary>
/// Owns snapshot construction, publication, and transition diagnostics.
/// No ATAS types. No file I/O. No mutable probe/recorder objects exposed.
/// </summary>
public sealed class GcaeRuntimeEngine
{
    private readonly RuntimeGateConfig _config;
    private readonly GcaeRuntimeSnapshotPublisher _publisher = new();
    private readonly RuntimeTransitionLedger _ledger = new();
    private long _publicationSequence;
    private GcaeRuntimeSnapshot? _previous;

    public GcaeRuntimeEngine(RuntimeGateConfig? config = null)
    {
        _config = config ?? new RuntimeGateConfig();
    }

    public RuntimeGateConfig Config => _config;
    public GcaeRuntimeSnapshotPublisher Publisher => _publisher;
    public RuntimeTransitionLedger Transitions => _ledger;
    public GcaeRuntimeSnapshot? Current => _publisher.Current;

    public GcaeRuntimeSnapshot Publish(
        ObservedInstrumentSnapshot? observed,
        string expectedInstrumentCode,
        DataSourceMode mode,
        DataSourceModeProvenance modeProvenance,
        DeclaredFeedProvider provider,
        FeedProviderProvenance providerProvenance,
        bool tradeObserved,
        DateTime? lastTradeCallbackUtc,
        bool rawRecorderMasterEnabled,
        bool tradeRecordingEnabled,
        bool recorderAccepting,
        bool recorderFaulted,
        bool recorderSessionPresent,
        bool indicatorDisposed,
        DateTime? timestampUtc = null)
    {
        var now = timestampUtc ?? DateTime.UtcNow;
        var contract = ContractSnapshotBuilder.Build(observed, expectedInstrumentCode, _config, now);
        var capability = RuntimeCapabilitySnapshotBuilder.Build(
            mode, modeProvenance, provider, providerProvenance,
            instrumentIdentityAvailable: observed is not null
                && !string.Equals(observed.IdentityKey, "Unknown", StringComparison.Ordinal),
            tradeObserved,
            lastTradeCallbackUtc,
            rawRecorderMasterEnabled,
            tradeRecordingEnabled,
            recorderAccepting,
            recorderFaulted,
            recorderSessionPresent);

        var gate = DataGateEngine.Evaluate(contract, capability, _config, indicatorDisposed, now);
        var seq = Interlocked.Increment(ref _publicationSequence);
        var recorderSummary = capability.RecorderState.ToString().ToUpperInvariant();

        var limitations = new List<string>();
        limitations.AddRange(contract.KnownLimitations);
        limitations.AddRange(capability.KnownLimitations);
        limitations.AddRange(gate.KnownLimitations);

        var snapshot = new GcaeRuntimeSnapshot(
            gate,
            contract,
            capability,
            ParticipationRegimePlaceholderState.NotAvailable,
            ProfilePlaceholderState.NotReady,
            ReferencePlaceholderState.NotAvailable,
            recorderSummary,
            now,
            seq,
            limitations.Distinct(StringComparer.Ordinal).ToArray());

        RecordTransitions(_previous, snapshot);
        _previous = snapshot;
        _publisher.Publish(snapshot);
        return snapshot;
    }

    public void Stop()
    {
        _publisher.Clear();
        _previous = null;
    }

    private void RecordTransitions(GcaeRuntimeSnapshot? previous, GcaeRuntimeSnapshot next)
    {
        var id = next.Contract.IdentityKey;
        var ver = next.Version;
        var ts = next.TimestampUtc;

        void Track(string component, string? oldVal, string newVal, string reason)
            => _ledger.TryAddIfChanged(component, oldVal, newVal, reason, id, ver, ts);

        Track("DataState", previous?.DataGate.DataState.ToString(), next.DataGate.DataState.ToString(), next.DataGate.PrimaryReasonCode);
        Track("InstrumentMatchState", previous?.Contract.InstrumentMatchState.ToString(), next.Contract.InstrumentMatchState.ToString(), next.DataGate.PrimaryReasonCode);
        Track("ExpirationState", previous?.Contract.ExpirationState.ToString(), next.Contract.ExpirationState.ToString(), next.DataGate.PrimaryReasonCode);
        Track("RollState", previous?.Contract.RollState.ToString(), next.Contract.RollState.ToString(), next.DataGate.PrimaryReasonCode);
        Track("TradeStreamState", previous?.Capability.TradeStreamState.ToString(), next.Capability.TradeStreamState.ToString(), next.DataGate.PrimaryReasonCode);
        Track("RecorderState", previous?.Capability.RecorderState.ToString(), next.Capability.RecorderState.ToString(), next.DataGate.PrimaryReasonCode);
        Track(
            "ProviderModeGate",
            previous is null ? null : $"{previous.Capability.DataSourceMode}/{previous.Capability.FeedProvider}",
            $"{next.Capability.DataSourceMode}/{next.Capability.FeedProvider}",
            next.DataGate.PrimaryReasonCode);
    }
}
