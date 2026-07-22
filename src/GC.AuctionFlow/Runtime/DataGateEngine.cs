using GC.AuctionFlow.Core;

namespace GC.AuctionFlow.Runtime;

/// <summary>Immutable DataGate evaluation result.</summary>
public sealed class DataGateSnapshot
{
    public const string SnapshotVersion = "0.1.0";

    public DataGateSnapshot(
        DataState dataState,
        string primaryReasonCode,
        IReadOnlyList<string> allReasonCodes,
        DateTime timestampUtc,
        IReadOnlyList<string> knownLimitations)
    {
        DataState = dataState;
        PrimaryReasonCode = primaryReasonCode ?? "";
        AllReasonCodes = allReasonCodes ?? Array.Empty<string>();
        TimestampUtc = timestampUtc;
        KnownLimitations = knownLimitations ?? Array.Empty<string>();
    }

    public DataState DataState { get; }
    public string PrimaryReasonCode { get; }
    public IReadOnlyList<string> AllReasonCodes { get; }
    public DateTime TimestampUtc { get; }
    public string Version => SnapshotVersion;
    public IReadOnlyList<string> KnownLimitations { get; }
}

/// <summary>Deterministic DataGate for P0-08A. Hard INVALID cannot be overridden by favorable capabilities.</summary>
public static class DataGateEngine
{
    public static DataGateSnapshot Evaluate(
        ContractSnapshot contract,
        RuntimeCapabilitySnapshot capability,
        RuntimeGateConfig config,
        bool indicatorDisposed,
        DateTime timestampUtc)
    {
        var invalid = new List<string>();
        var degraded = new List<string>();
        var limitations = new List<string>
        {
            "PROFILE_NOT_IMPLEMENTED_IN_P0_08A",
            "READY_REQUIRES_ALL_MANDATORY_CAPABILITIES"
        };

        if (indicatorDisposed)
            invalid.Add(DataGateReasonCodes.IndicatorDisposed);

        if (contract.InstrumentMatchState == InstrumentMatchState.Mismatch)
            invalid.Add(DataGateReasonCodes.InstrumentMismatch);

        if (contract.InstrumentMatchState == InstrumentMatchState.Unknown
            || string.Equals(contract.IdentityKey, "Unknown", StringComparison.Ordinal))
            invalid.Add(DataGateReasonCodes.InstrumentUnknown);

        if (contract.TickSize is null || contract.TickSize <= 0m)
            invalid.Add(DataGateReasonCodes.TickSizeInvalid);
        else if (contract.TickSize != config.ExpectedTickSize)
            invalid.Add(DataGateReasonCodes.TickSizeMismatch);

        if (contract.ExpirationState == ExpirationState.Expired)
            invalid.Add(DataGateReasonCodes.ContractExpired);

        if (IsProviderModeConflict(capability))
            invalid.Add(DataGateReasonCodes.ProviderModeConflict);

        if (string.IsNullOrWhiteSpace(contract.IdentityKey)
            || (contract.InstrumentMatchState == InstrumentMatchState.Match
                && string.IsNullOrWhiteSpace(contract.SecurityCode)
                && string.Equals(contract.IdentityKey, "Unknown", StringComparison.Ordinal)))
        {
            invalid.Add(DataGateReasonCodes.IdentityCorruption);
        }

        // Degraded (only considered when not Invalid)
        if (capability.ProfileState is RuntimeCapabilityState.NotReady or RuntimeCapabilityState.Unavailable)
            degraded.Add(DataGateReasonCodes.ProfileNotReady);

        if (capability.BidAskClassificationState is RuntimeCapabilityState.Unknown or RuntimeCapabilityState.Partial)
            degraded.Add(DataGateReasonCodes.BidAskUnknownOrPartial);

        if (contract.RollState == RollState.Unknown)
            degraded.Add(DataGateReasonCodes.RollStateUnknown);

        if (capability.TradeStreamState != RuntimeCapabilityState.Available
            && capability.LastTradeCallbackUtc is null)
            degraded.Add(DataGateReasonCodes.TradeNotObserved);

        if (capability.DataSourceModeProvenance == DataSourceModeProvenance.Unknown
            || capability.FeedProviderProvenance == Probe.FeedProviderProvenance.Unknown
            || capability.DataSourceMode == DataSourceMode.Unknown
            || capability.FeedProvider == Probe.DeclaredFeedProvider.Unknown)
        {
            degraded.Add(DataGateReasonCodes.SourceProvenanceIncomplete);
        }

        if (invalid.Count > 0)
        {
            var unique = Dedup(invalid);
            return new DataGateSnapshot(
                DataState.Invalid,
                unique[0],
                unique,
                timestampUtc,
                limitations);
        }

        if (degraded.Count > 0)
        {
            var unique = Dedup(degraded);
            // Prefer PROFILE_NOT_READY as primary when present (expected P0-08A card state).
            var primary = unique.Contains(DataGateReasonCodes.ProfileNotReady)
                ? DataGateReasonCodes.ProfileNotReady
                : unique[0];
            if (primary != unique[0])
            {
                unique.Remove(primary);
                unique.Insert(0, primary);
            }

            return new DataGateSnapshot(
                DataState.Degraded,
                primary,
                unique,
                timestampUtc,
                limitations);
        }

        return new DataGateSnapshot(
            DataState.Ready,
            "",
            Array.Empty<string>(),
            timestampUtc,
            limitations);
    }

    private static bool IsProviderModeConflict(RuntimeCapabilitySnapshot capability)
    {
        // Explicit conflict: Live claimed without OperatorDeclared / ObservedApi provenance, or Unknown provider with Live.
        if (capability.DataSourceMode == DataSourceMode.Live
            && capability.DataSourceModeProvenance == DataSourceModeProvenance.Unknown)
            return true;
        if (capability.DataSourceMode == DataSourceMode.Live
            && capability.FeedProvider == Probe.DeclaredFeedProvider.Unknown)
            return true;
        if (capability.DataSourceModeProvenance == DataSourceModeProvenance.Inferred
            && capability.DataSourceMode == DataSourceMode.Live
            && capability.FeedProviderProvenance == Probe.FeedProviderProvenance.Unknown)
            return true;
        return false;
    }

    private static List<string> Dedup(List<string> codes)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var list = new List<string>();
        foreach (var c in codes)
        {
            if (seen.Add(c))
                list.Add(c);
        }

        return list;
    }
}
