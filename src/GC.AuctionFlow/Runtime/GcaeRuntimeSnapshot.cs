namespace GC.AuctionFlow.Runtime;

/// <summary>Immutable UI-facing runtime snapshot. No mutable probe/recorder objects.</summary>
public sealed class GcaeRuntimeSnapshot
{
    public const string SnapshotVersion = "0.1.0";

    public GcaeRuntimeSnapshot(
        DataGateSnapshot dataGate,
        ContractSnapshot contract,
        RuntimeCapabilitySnapshot capability,
        ParticipationRegimePlaceholderState participationRegime,
        ProfilePlaceholderState profile,
        ReferencePlaceholderState reference,
        string recorderDiagnosticSummary,
        DateTime timestampUtc,
        long publicationSequence,
        IReadOnlyList<string> knownLimitations)
    {
        DataGate = dataGate ?? throw new ArgumentNullException(nameof(dataGate));
        Contract = contract ?? throw new ArgumentNullException(nameof(contract));
        Capability = capability ?? throw new ArgumentNullException(nameof(capability));
        ParticipationRegime = participationRegime;
        Profile = profile;
        Reference = reference;
        RecorderDiagnosticSummary = recorderDiagnosticSummary ?? "";
        TimestampUtc = timestampUtc;
        PublicationSequence = publicationSequence;
        KnownLimitations = knownLimitations ?? Array.Empty<string>();
    }

    public DataGateSnapshot DataGate { get; }
    public ContractSnapshot Contract { get; }
    public RuntimeCapabilitySnapshot Capability { get; }
    public ParticipationRegimePlaceholderState ParticipationRegime { get; }
    public ProfilePlaceholderState Profile { get; }
    public ReferencePlaceholderState Reference { get; }
    public string RecorderDiagnosticSummary { get; }
    public DateTime TimestampUtc { get; }
    public long PublicationSequence { get; }
    public string Version => SnapshotVersion;
    public IReadOnlyList<string> KnownLimitations { get; }
}

/// <summary>Thread-safe non-blocking publisher of immutable runtime snapshots.</summary>
public sealed class GcaeRuntimeSnapshotPublisher
{
    private GcaeRuntimeSnapshot? _current;
    private long _sequence;

    public long CurrentSequence => Volatile.Read(ref _sequence);

    public GcaeRuntimeSnapshot? Current => Volatile.Read(ref _current);

    public GcaeRuntimeSnapshot Publish(GcaeRuntimeSnapshot snapshot)
    {
        if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));
        Volatile.Write(ref _current, snapshot);
        Interlocked.Exchange(ref _sequence, snapshot.PublicationSequence);
        return snapshot;
    }

    public void Clear()
    {
        Volatile.Write(ref _current, null);
    }
}
