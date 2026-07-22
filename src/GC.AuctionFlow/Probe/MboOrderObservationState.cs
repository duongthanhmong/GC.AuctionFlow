using System.Globalization;

namespace GC.AuctionFlow.Probe;

/// <summary>
/// Diagnostic last-observation state keyed by epoch+instrument+nonzero ExchangeOrderId.
/// Not an order book. Raw Delete does not remove entries. Bounded — does not evict on saturation.
/// </summary>
public sealed class MboOrderObservationState
{
    private readonly int _maxIds;
    private readonly Dictionary<string, MboOrderIdDiag> _byKey = new(StringComparer.Ordinal);
    private readonly object _gate = new();
    private long _untrackedNonzeroIdDueToStateCapacity;
    private bool _stateCapacityReached;

    public MboOrderObservationState(int maxTrackedNonzeroIds)
    {
        _maxIds = maxTrackedNonzeroIds;
    }

    public int ConfiguredCapacity => _maxIds;

    public void Observe(MboObservation obs)
    {
        if (obs.ExchangeOrderId == 0)
            return;

        var key = obs.CorrelationKey;
        lock (_gate)
        {
            if (!_byKey.TryGetValue(key, out var diag))
            {
                if (_byKey.Count >= _maxIds)
                {
                    _stateCapacityReached = true;
                    _untrackedNonzeroIdDueToStateCapacity++;
                    return;
                }

                diag = new MboOrderIdDiag(obs.SubscriptionEpoch, obs.InstrumentIdentityKey, obs.ExchangeOrderId);
                _byKey[key] = diag;
            }

            diag.Apply(obs);
        }
    }

    public MboOrderObservationStateSummary Snapshot()
    {
        lock (_gate)
        {
            var entries = _byKey.Values.Select(d => d.ToSummary()).ToArray();
            var transitions = new Dictionary<string, long>(StringComparer.Ordinal);
            long priceChanges = 0, volumeChanges = 0, priorityChanges = 0, sideChanges = 0;
            long deleteThenNewSuspected = 0;
            foreach (var d in _byKey.Values)
            {
                foreach (var kv in d.TransitionCounts)
                {
                    transitions.TryGetValue(kv.Key, out var c);
                    transitions[kv.Key] = c + kv.Value;
                }

                priceChanges += d.PriceChangeCount;
                volumeChanges += d.VolumeChangeCount;
                priorityChanges += d.PriorityChangeCount;
                sideChanges += d.SideChangeCount;
                deleteThenNewSuspected += d.DeleteThenNewSuspected;
            }

            return new MboOrderObservationStateSummary(
                MaxTrackedNonzeroIds: _maxIds,
                TrackedNonzeroIdCount: _byKey.Count,
                StateCapacityReached: _stateCapacityReached,
                UntrackedNonzeroIdDueToStateCapacity: _untrackedNonzeroIdDueToStateCapacity,
                Entries: entries,
                RawTransitionCounts: transitions,
                PriceChangeObservations: priceChanges,
                VolumeChangeObservations: volumeChanges,
                PriorityChangeObservations: priorityChanges,
                SideChangeObservations: sideChanges,
                IdReuseSuspectedDeleteThenNew: deleteThenNewSuspected,
                NamingNote: "MboOrderObservationState is diagnostic only — not an exchange order book.");
        }
    }
}

internal sealed class MboOrderIdDiag
{
    public MboOrderIdDiag(long epoch, string instrumentKey, long exchangeOrderId)
    {
        SubscriptionEpoch = epoch;
        InstrumentIdentityKey = instrumentKey;
        ExchangeOrderId = exchangeOrderId;
    }

    public long SubscriptionEpoch { get; }
    public string InstrumentIdentityKey { get; }
    public long ExchangeOrderId { get; }
    public int FirstRawTypeNumeric { get; private set; }
    public string FirstRawTypeName { get; private set; } = "";
    public int LastRawTypeNumeric { get; private set; }
    public string LastRawTypeName { get; private set; } = "";
    public long FirstSourceTimeTicks { get; private set; }
    public long LastSourceTimeTicks { get; private set; }
    public DateTime FirstReceiveUtc { get; private set; }
    public DateTime LastReceiveUtc { get; private set; }
    public decimal FirstPrice { get; private set; }
    public decimal LastPrice { get; private set; }
    public decimal FirstVolume { get; private set; }
    public decimal LastVolume { get; private set; }
    public long FirstPriority { get; private set; }
    public long LastPriority { get; private set; }
    public int FirstSideNumeric { get; private set; }
    public int LastSideNumeric { get; private set; }
    public long ObservationCount { get; private set; }
    public long PriceChangeCount { get; private set; }
    public long VolumeChangeCount { get; private set; }
    public long PriorityChangeCount { get; private set; }
    public long SideChangeCount { get; private set; }
    public long DeleteThenNewSuspected { get; private set; }
    public Dictionary<int, long> RawTypeCounts { get; } = new();
    public Dictionary<string, long> TransitionCounts { get; } = new(StringComparer.Ordinal);
    private bool _initialized;
    private bool _sawDelete;

    public void Apply(MboObservation obs)
    {
        if (!_initialized)
        {
            FirstRawTypeNumeric = obs.RawTypeNumeric;
            FirstRawTypeName = obs.RawTypeName;
            FirstSourceTimeTicks = obs.SourceTimeTicks;
            FirstReceiveUtc = obs.ReceiveUtc;
            FirstPrice = obs.Price;
            FirstVolume = obs.Volume;
            FirstPriority = obs.Priority;
            FirstSideNumeric = obs.RawSideNumeric;
            _initialized = true;
        }
        else
        {
            var from = LastRawTypeName;
            var to = obs.RawTypeName;
            var key = string.Create(CultureInfo.InvariantCulture, $"{from}->{to}");
            TransitionCounts.TryGetValue(key, out var tc);
            TransitionCounts[key] = tc + 1;

            if (LastPrice != obs.Price) PriceChangeCount++;
            if (LastVolume != obs.Volume) VolumeChangeCount++;
            if (LastPriority != obs.Priority) PriorityChangeCount++;
            if (LastSideNumeric != obs.RawSideNumeric) SideChangeCount++;

            if (_sawDelete && obs.RawTypeNumeric == MboKnownRawUpdateTypes.New)
                DeleteThenNewSuspected++;
        }

        LastRawTypeNumeric = obs.RawTypeNumeric;
        LastRawTypeName = obs.RawTypeName;
        LastSourceTimeTicks = obs.SourceTimeTicks;
        LastReceiveUtc = obs.ReceiveUtc;
        LastPrice = obs.Price;
        LastVolume = obs.Volume;
        LastPriority = obs.Priority;
        LastSideNumeric = obs.RawSideNumeric;
        ObservationCount++;

        RawTypeCounts.TryGetValue(obs.RawTypeNumeric, out var rtc);
        RawTypeCounts[obs.RawTypeNumeric] = rtc + 1;

        if (obs.RawTypeNumeric == MboKnownRawUpdateTypes.Delete)
            _sawDelete = true;
    }

    public MboOrderIdDiagSummary ToSummary() => new(
        CorrelationKey: MboCorrelationKey.Build(SubscriptionEpoch, InstrumentIdentityKey, ExchangeOrderId),
        SubscriptionEpoch: SubscriptionEpoch,
        InstrumentIdentityKey: InstrumentIdentityKey,
        ExchangeOrderId: ExchangeOrderId,
        FirstRawTypeName: FirstRawTypeName,
        FirstRawTypeNumeric: FirstRawTypeNumeric,
        LastRawTypeName: LastRawTypeName,
        LastRawTypeNumeric: LastRawTypeNumeric,
        FirstSourceTimeTicks: FirstSourceTimeTicks,
        LastSourceTimeTicks: LastSourceTimeTicks,
        FirstReceiveUtc: FirstReceiveUtc,
        LastReceiveUtc: LastReceiveUtc,
        FirstPrice: FirstPrice,
        LastPrice: LastPrice,
        FirstVolume: FirstVolume,
        LastVolume: LastVolume,
        FirstPriority: FirstPriority,
        LastPriority: LastPriority,
        FirstSideNumeric: FirstSideNumeric,
        LastSideNumeric: LastSideNumeric,
        ObservationCount: ObservationCount,
        RawTypeCounts: new Dictionary<int, long>(RawTypeCounts),
        TransitionCounts: new Dictionary<string, long>(TransitionCounts, StringComparer.Ordinal),
        PriceChangeCount: PriceChangeCount,
        VolumeChangeCount: VolumeChangeCount,
        PriorityChangeCount: PriorityChangeCount,
        SideChangeCount: SideChangeCount,
        DeleteThenNewSuspected: DeleteThenNewSuspected,
        ObservationSpanReceive: LastReceiveUtc - FirstReceiveUtc);
}

public sealed record MboOrderIdDiagSummary(
    string CorrelationKey,
    long SubscriptionEpoch,
    string InstrumentIdentityKey,
    long ExchangeOrderId,
    string FirstRawTypeName,
    int FirstRawTypeNumeric,
    string LastRawTypeName,
    int LastRawTypeNumeric,
    long FirstSourceTimeTicks,
    long LastSourceTimeTicks,
    DateTime FirstReceiveUtc,
    DateTime LastReceiveUtc,
    decimal FirstPrice,
    decimal LastPrice,
    decimal FirstVolume,
    decimal LastVolume,
    long FirstPriority,
    long LastPriority,
    int FirstSideNumeric,
    int LastSideNumeric,
    long ObservationCount,
    IReadOnlyDictionary<int, long> RawTypeCounts,
    IReadOnlyDictionary<string, long> TransitionCounts,
    long PriceChangeCount,
    long VolumeChangeCount,
    long PriorityChangeCount,
    long SideChangeCount,
    long DeleteThenNewSuspected,
    TimeSpan ObservationSpanReceive);

public sealed record MboOrderObservationStateSummary(
    int MaxTrackedNonzeroIds,
    int TrackedNonzeroIdCount,
    bool StateCapacityReached,
    long UntrackedNonzeroIdDueToStateCapacity,
    IReadOnlyList<MboOrderIdDiagSummary> Entries,
    IReadOnlyDictionary<string, long> RawTransitionCounts,
    long PriceChangeObservations,
    long VolumeChangeObservations,
    long PriorityChangeObservations,
    long SideChangeObservations,
    long IdReuseSuspectedDeleteThenNew,
    string NamingNote);
