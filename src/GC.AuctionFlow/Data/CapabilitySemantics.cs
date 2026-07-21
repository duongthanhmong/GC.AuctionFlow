using GC.AuctionFlow.Core;

namespace GC.AuctionFlow.Data;

/// <summary>
/// Semantic invariants for capability contracts. No confidence score; no runtime probing.
/// </summary>
public static class CapabilitySemantics
{
    /// <summary>
    /// Storage / declaration rules. Unknown mode may be recorded explicitly;
    /// it must never be accepted as a resolved Live declaration.
    /// </summary>
    public static SchemaValidationResult ValidateDataSourceModeDeclaration(
        DataSourceMode mode,
        DataSourceModeProvenance provenance)
    {
        var errors = new List<string>();

        if (mode == DataSourceMode.Live && provenance == DataSourceModeProvenance.Unknown)
        {
            errors.Add(
                $"{KnownLimitationCodes.ModeWithoutProvenanceUnresolved}: " +
                "Live mode with Unknown provenance remains unresolved and is rejected.");
        }

        if (mode == DataSourceMode.Live && provenance == DataSourceModeProvenance.Inferred)
        {
            errors.Add(
                "DataSourceMode.Live requires OperatorDeclared or ObservedApi provenance; Inferred is rejected.");
        }

        return errors.Count == 0
            ? SchemaValidationResult.Ok()
            : SchemaValidationResult.Fail(errors);
    }

    /// <summary>Unknown DataSourceMode cannot masquerade as Live.</summary>
    public static bool IsLive(DataSourceMode mode) => mode == DataSourceMode.Live;

    public static bool IsModeResolved(DataSourceMode mode, DataSourceModeProvenance provenance) =>
        mode != DataSourceMode.Unknown && provenance != DataSourceModeProvenance.Unknown;

    public static SchemaValidationResult ValidateCell(CapabilityCell cell)
    {
        var errors = new List<string>();

        if (cell.Availability == CapabilityAvailability.Available
            && cell.Fidelity == FidelityState.Validated
            && cell.Provenance == EvidenceProvenance.Inferred)
        {
            errors.Add(
                $"{KnownLimitationCodes.IndicatorMenuPresenceNotCapabilityEvidence}: " +
                "hard Available+Validated facts must not use Inferred provenance.");
        }

        if (cell.Kind == CapabilityKind.Mbo)
        {
            var lifecycle = cell.MboLifecycle ?? MboLifecycleState.Unknown;
            if (lifecycle is MboLifecycleState.EventPresenceOnly or MboLifecycleState.Unknown
                && cell.Fidelity == FidelityState.Validated)
            {
                errors.Add(
                    $"{KnownLimitationCodes.MboEventPresenceNotLifecycle}: " +
                    "MBO event presence does not imply lifecycle completeness / Validated fidelity.");
            }
        }

        if (cell.NotesOrLimitationCodes.Any(c =>
                string.Equals(
                    c,
                    KnownLimitationCodes.IndicatorMenuPresenceNotCapabilityEvidence,
                    StringComparison.Ordinal))
            && cell.Availability == CapabilityAvailability.Available
            && cell.Provenance == EvidenceProvenance.Inferred)
        {
            errors.Add(
                $"{KnownLimitationCodes.IndicatorMenuPresenceNotCapabilityEvidence}: " +
                "hard capability facts must not be inferred from indicator menu presence.");
        }

        return errors.Count == 0
            ? SchemaValidationResult.Ok()
            : SchemaValidationResult.Fail(errors);
    }

    /// <summary>NoNativeSequence alone does not force DataState.Invalid.</summary>
    public static bool DoesNoNativeSequenceForceInvalid(SequenceEvidence sequence) =>
        sequence == SequenceEvidence.NoNativeSequence && false;

    /// <summary>Local capture continuity is not exchange-feed completeness.</summary>
    public static bool ImpliesExchangeFeedCompleteness(SequenceEvidence sequence) =>
        sequence == SequenceEvidence.NativeSequenceAvailable;

    /// <summary>Available does not imply Validated.</summary>
    public static bool IsFidelityValidated(CapabilityAvailability availability, FidelityState fidelity) =>
        availability == CapabilityAvailability.Available && fidelity == FidelityState.Validated;

    public static bool AreTradeStreamsDistinct(TradeStreamKind a, TradeStreamKind b) => a != b;

    public static bool IsMboLifecycleComplete(MboLifecycleState state) =>
        state == MboLifecycleState.LifecycleValidated;
}
