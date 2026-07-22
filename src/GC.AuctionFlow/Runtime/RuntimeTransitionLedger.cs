namespace GC.AuctionFlow.Runtime;

public sealed class RuntimeTransitionRecord
{
    public RuntimeTransitionRecord(
        DateTime timestampUtc,
        string component,
        string oldState,
        string newState,
        string reasonCode,
        string instrumentIdentity,
        string snapshotVersion)
    {
        TimestampUtc = timestampUtc;
        Component = component;
        OldState = oldState;
        NewState = newState;
        ReasonCode = reasonCode;
        InstrumentIdentity = instrumentIdentity;
        SnapshotVersion = snapshotVersion;
    }

    public DateTime TimestampUtc { get; }
    public string Component { get; }
    public string OldState { get; }
    public string NewState { get; }
    public string ReasonCode { get; }
    public string InstrumentIdentity { get; }
    public string SnapshotVersion { get; }
}

/// <summary>Bounded in-memory transition ledger. No per-tick file I/O.</summary>
public sealed class RuntimeTransitionLedger
{
    public const int DefaultCapacity = 256;

    private readonly object _gate = new();
    private readonly Queue<RuntimeTransitionRecord> _items = new();
    private readonly int _capacity;

    public RuntimeTransitionLedger(int capacity = DefaultCapacity)
    {
        _capacity = Math.Max(16, capacity);
    }

    public int Count
    {
        get { lock (_gate) return _items.Count; }
    }

    public bool TryAddIfChanged(
        string component,
        string? oldState,
        string newState,
        string reasonCode,
        string instrumentIdentity,
        string snapshotVersion,
        DateTime timestampUtc)
    {
        oldState ??= "";
        newState ??= "";
        if (string.Equals(oldState, newState, StringComparison.Ordinal))
            return false;

        var record = new RuntimeTransitionRecord(
            timestampUtc, component, oldState, newState, reasonCode, instrumentIdentity, snapshotVersion);
        lock (_gate)
        {
            _items.Enqueue(record);
            while (_items.Count > _capacity)
                _items.Dequeue();
        }

        return true;
    }

    public IReadOnlyList<RuntimeTransitionRecord> Snapshot()
    {
        lock (_gate)
            return _items.ToArray();
    }
}
