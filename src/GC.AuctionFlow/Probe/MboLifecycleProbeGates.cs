using GC.AuctionFlow.Core;

namespace GC.AuctionFlow.Probe;

/// <summary>Fail-closed gates for MBO lifecycle probe. Rejection never becomes MBO capability PASS.</summary>
public static class MboLifecycleProbeGates
{
    public static GateDecision Evaluate(
        bool enable,
        DataSourceMode mode,
        DataSourceModeProvenance modeProvenance,
        string? expectedInstrumentCode,
        ObservedInstrumentSnapshot? observed,
        DeclaredFeedProvider feedProvider,
        FeedProviderProvenance feedProvenance)
    {
        if (!enable)
            return GateDecision.Reject(TradeStreamGateReason.ProbeDisabled, "EnableMboLifecycleProbe=false", false);

        if (mode == DataSourceMode.Unknown)
            return GateDecision.Reject(TradeStreamGateReason.ModeUnknown, "DeclaredDataSourceMode=Unknown", false);

        if (modeProvenance == DataSourceModeProvenance.Unknown)
            return GateDecision.Reject(TradeStreamGateReason.ProvenanceUnknown, "DataSourceModeProvenance=Unknown", false);

        if (feedProvider == DeclaredFeedProvider.Unknown)
            return GateDecision.Reject(TradeStreamGateReason.ModeUnknown, "DeclaredFeedProvider=Unknown", false);

        if (feedProvenance == FeedProviderProvenance.Unknown)
            return GateDecision.Reject(TradeStreamGateReason.ProvenanceUnknown, "FeedProviderProvenance=Unknown", false);

        if (string.IsNullOrWhiteSpace(expectedInstrumentCode))
            return GateDecision.Reject(
                TradeStreamGateReason.ExpectedInstrumentMissing,
                "ExpectedInstrumentMissing",
                true);

        if (observed is null)
            return GateDecision.Reject(
                TradeStreamGateReason.ObservedInstrumentMissing,
                "ObservedInstrumentMissing",
                true);

        var expected = expectedInstrumentCode.Trim();
        if (!string.Equals(expected, observed.IdentityKey, StringComparison.Ordinal))
        {
            return GateDecision.Reject(
                TradeStreamGateReason.InstrumentMismatch,
                $"InstrumentMismatch expected='{expected}' observedIdentityKey='{observed.IdentityKey}'",
                true);
        }

        return GateDecision.Accept();
    }

    public readonly struct GateDecision
    {
        private GateDecision(bool accepted, TradeStreamGateReason reason, string? detail, bool instrumentGate)
        {
            Accepted = accepted;
            Reason = reason;
            RejectReason = detail;
            InstrumentGate = instrumentGate;
        }

        public bool Accepted { get; }
        public TradeStreamGateReason Reason { get; }
        public string? RejectReason { get; }
        public bool InstrumentGate { get; }

        public static GateDecision Accept() => new(true, TradeStreamGateReason.None, null, false);

        public static GateDecision Reject(TradeStreamGateReason reason, string detail, bool instrumentGate) =>
            new(false, reason, detail, instrumentGate);
    }
}
