using GC.AuctionFlow.Core;

namespace GC.AuctionFlow.Data;

/// <summary>
/// One capability matrix cell. Axes are independent — never compressed into a score.
/// </summary>
public sealed record CapabilityCell(
    CapabilityKind Kind,
    CapabilityAvailability Availability,
    DataCoverage Coverage,
    ComputabilityState Computability,
    FidelityState Fidelity,
    SequenceEvidence Sequence,
    EvidenceProvenance Provenance,
    DateTimeOffset? ObservedUtc,
    IReadOnlyList<string> NotesOrLimitationCodes,
    MboLifecycleState? MboLifecycle = null);
