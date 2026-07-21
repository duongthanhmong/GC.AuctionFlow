using GC.AuctionFlow.Core;

namespace GC.AuctionFlow.Probe;

/// <summary>Operator gate evaluation. Fail-closed for Unknown mode/provenance and instrument issues.</summary>
public static class TradeStreamProbeGates
{
    public static GateDecision Evaluate(
        bool enableTradeStreamProbe,
        DataSourceMode declaredDataSourceMode,
        DataSourceModeProvenance dataSourceModeProvenance,
        string? expectedInstrumentCode,
        ObservedInstrumentSnapshot? observed)
    {
        if (!enableTradeStreamProbe)
            return GateDecision.Reject(
                TradeStreamGateReason.ProbeDisabled,
                "EnableTradeStreamProbe=false",
                instrumentGate: false);

        if (declaredDataSourceMode == DataSourceMode.Unknown)
            return GateDecision.Reject(
                TradeStreamGateReason.ModeUnknown,
                "DeclaredDataSourceMode=Unknown",
                instrumentGate: false);

        if (dataSourceModeProvenance == DataSourceModeProvenance.Unknown)
            return GateDecision.Reject(
                TradeStreamGateReason.ProvenanceUnknown,
                "DataSourceModeProvenance=Unknown",
                instrumentGate: false);

        // Observed identity is evaluated only after bootstrap may have captured it.
        // Missing/mismatched expected code is an instrument gate (not a LIVE capability PASS).
        if (string.IsNullOrWhiteSpace(expectedInstrumentCode))
            return GateDecision.Reject(
                TradeStreamGateReason.ExpectedInstrumentMissing,
                "ExpectedInstrumentMissing — first operator run requires explicit contract code",
                instrumentGate: true);

        if (observed is null)
            return GateDecision.Reject(
                TradeStreamGateReason.ObservedInstrumentMissing,
                "ObservedInstrumentMissing",
                instrumentGate: true);

        var expected = expectedInstrumentCode.Trim();
        if (!string.Equals(expected, observed.IdentityKey, StringComparison.Ordinal))
        {
            return GateDecision.Reject(
                TradeStreamGateReason.InstrumentMismatch,
                $"InstrumentMismatch expected='{expected}' observedIdentityKey='{observed.IdentityKey}'",
                instrumentGate: true);
        }

        return GateDecision.Accept();
    }

    public readonly struct GateDecision
    {
        private GateDecision(
            bool accepted,
            TradeStreamGateReason reason,
            string? detail,
            bool instrumentGate)
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

        /// <summary>Backward-compatible alias for instrument-related rejects.</summary>
        public bool InstrumentMismatch =>
            Reason is TradeStreamGateReason.InstrumentMismatch
                or TradeStreamGateReason.ExpectedInstrumentMissing
                or TradeStreamGateReason.ObservedInstrumentMissing;

        public static GateDecision Accept() =>
            new(true, TradeStreamGateReason.None, null, false);

        public static GateDecision Reject(
            TradeStreamGateReason reason,
            string detail,
            bool instrumentGate) =>
            new(false, reason, detail, instrumentGate);
    }
}
