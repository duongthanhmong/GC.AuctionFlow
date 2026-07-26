using GC.AuctionFlow.Cluster;
using GC.AuctionFlow.Composite;
using GC.AuctionFlow.Directional;
using GC.AuctionFlow.EffortResult;
using GC.AuctionFlow.Efficiency;
using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Evidence;
using GC.AuctionFlow.Orderflow;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Reference;
using GC.AuctionFlow.Resolution;

namespace GC.AuctionFlow.Runtime;

/// <summary>Immutable UI-facing runtime snapshot. No mutable probe/recorder/profile engines.</summary>
public sealed class GcaeRuntimeSnapshot
{
    public const string SnapshotVersion = "0.12.0";

    public GcaeRuntimeSnapshot(
        DataGateSnapshot dataGate,
        ContractSnapshot contract,
        RuntimeCapabilitySnapshot capability,
        ParticipationRegimePlaceholderState participationRegime,
        ProfilePlaceholderState profile,
        ReferencePlaceholderState reference,
        PrimaryProfileSetSnapshot? profiles,
        string recorderDiagnosticSummary,
        DateTime timestampUtc,
        long publicationSequence,
        IReadOnlyList<string> knownLimitations,
        bool enableTpoParityDiagnostics = false,
        CompositeSetSnapshot? composite = null,
        bool showCompositeDiagnostics = false,
        StructuralReferenceSetSnapshot? structuralReferences = null,
        bool showStructuralReferenceDiagnostics = false,
        DirectionalContextSetSnapshot? directionalContext = null,
        bool showDirectionalContextDiagnostics = false,
        AuctionEpisodeSetSnapshot? auctionEpisodes = null,
        bool showAuctionEpisodeDiagnostics = false,
        AcceptanceReentryEvidenceSetSnapshot? acceptanceReentryEvidence = null,
        bool showAcceptanceReentryEvidenceDiagnostics = false,
        ExecutedOrderflowSetSnapshot? executedOrderflow = null,
        bool showExecutedOrderflowDiagnostics = false,
        ClusterRawSetSnapshot? clusterRaw = null,
        bool showClusterRawDiagnostics = false,
        AuctionEfficiencyEvidenceSetSnapshot? auctionEfficiency = null,
        bool showAuctionEfficiencyDiagnostics = false,
        AuctionResolutionSetSnapshot? auctionResolution = null,
        bool showAuctionResolutionDiagnostics = false,
        EffortResultClassificationSetSnapshot? effortResult = null,
        bool showEffortResultDiagnostics = false)
    {
        DataGate = dataGate ?? throw new ArgumentNullException(nameof(dataGate));
        Contract = contract ?? throw new ArgumentNullException(nameof(contract));
        Capability = capability ?? throw new ArgumentNullException(nameof(capability));
        ParticipationRegime = participationRegime;
        Profile = profile;
        Reference = reference;
        Profiles = profiles;
        RecorderDiagnosticSummary = recorderDiagnosticSummary ?? "";
        TimestampUtc = timestampUtc;
        PublicationSequence = publicationSequence;
        KnownLimitations = knownLimitations ?? Array.Empty<string>();
        EnableTpoParityDiagnostics = enableTpoParityDiagnostics;
        Composite = composite;
        ShowCompositeDiagnostics = showCompositeDiagnostics;
        StructuralReferences = structuralReferences;
        ShowStructuralReferenceDiagnostics = showStructuralReferenceDiagnostics;
        DirectionalContext = directionalContext;
        ShowDirectionalContextDiagnostics = showDirectionalContextDiagnostics;
        AuctionEpisodes = auctionEpisodes;
        ShowAuctionEpisodeDiagnostics = showAuctionEpisodeDiagnostics;
        AcceptanceReentryEvidence = acceptanceReentryEvidence;
        ShowAcceptanceReentryEvidenceDiagnostics = showAcceptanceReentryEvidenceDiagnostics;
        ExecutedOrderflow = executedOrderflow;
        ShowExecutedOrderflowDiagnostics = showExecutedOrderflowDiagnostics;
        ClusterRaw = clusterRaw;
        ShowClusterRawDiagnostics = showClusterRawDiagnostics;
        AuctionEfficiency = auctionEfficiency;
        ShowAuctionEfficiencyDiagnostics = showAuctionEfficiencyDiagnostics;
        AuctionResolution = auctionResolution;
        ShowAuctionResolutionDiagnostics = showAuctionResolutionDiagnostics;
        EffortResult = effortResult;
        ShowEffortResultDiagnostics = showEffortResultDiagnostics;
    }

    public DataGateSnapshot DataGate { get; }
    public ContractSnapshot Contract { get; }
    public RuntimeCapabilitySnapshot Capability { get; }
    public ParticipationRegimePlaceholderState ParticipationRegime { get; }
    public ProfilePlaceholderState Profile { get; }
    public ReferencePlaceholderState Reference { get; }
    public PrimaryProfileSetSnapshot? Profiles { get; }
    public CompositeSetSnapshot? Composite { get; }
    public StructuralReferenceSetSnapshot? StructuralReferences { get; }
    public DirectionalContextSetSnapshot? DirectionalContext { get; }
    public AuctionEpisodeSetSnapshot? AuctionEpisodes { get; }
    public AcceptanceReentryEvidenceSetSnapshot? AcceptanceReentryEvidence { get; }
    public ExecutedOrderflowSetSnapshot? ExecutedOrderflow { get; }
    public ClusterRawSetSnapshot? ClusterRaw { get; }
    public AuctionEfficiencyEvidenceSetSnapshot? AuctionEfficiency { get; }
    public AuctionResolutionSetSnapshot? AuctionResolution { get; }
    public EffortResultClassificationSetSnapshot? EffortResult { get; }
    public string RecorderDiagnosticSummary { get; }
    public DateTime TimestampUtc { get; }
    public long PublicationSequence { get; }
    public string Version => SnapshotVersion;
    public IReadOnlyList<string> KnownLimitations { get; }
    public bool EnableTpoParityDiagnostics { get; }
    public bool ShowCompositeDiagnostics { get; }
    public bool ShowStructuralReferenceDiagnostics { get; }
    public bool ShowDirectionalContextDiagnostics { get; }
    public bool ShowAuctionEpisodeDiagnostics { get; }
    public bool ShowAcceptanceReentryEvidenceDiagnostics { get; }
    public bool ShowExecutedOrderflowDiagnostics { get; }
    public bool ShowClusterRawDiagnostics { get; }
    public bool ShowAuctionEfficiencyDiagnostics { get; }
    public bool ShowAuctionResolutionDiagnostics { get; }
    public bool ShowEffortResultDiagnostics { get; }
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
