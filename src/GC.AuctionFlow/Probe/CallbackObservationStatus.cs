namespace GC.AuctionFlow.Probe;

/// <summary>
/// Per-callback observation in one operator run.
/// Zero invocations = NotObservedInTestWindow (serialized NOT_OBSERVED_IN_TEST_WINDOW).
/// Do not auto-classify as Unavailable.
/// </summary>
public enum CallbackObservationStatus
{
    NotObservedInTestWindow = 0,
    Observed = 1
}

public static class CallbackObservationStatusNames
{
    public const string NotObservedInTestWindow = "NOT_OBSERVED_IN_TEST_WINDOW";
    public const string Observed = "OBSERVED";

    public static string ToWire(CallbackObservationStatus status) => status switch
    {
        CallbackObservationStatus.Observed => Observed,
        _ => NotObservedInTestWindow
    };

    public static CallbackObservationStatus FromCount(long callbackInvocations) =>
        callbackInvocations > 0
            ? CallbackObservationStatus.Observed
            : CallbackObservationStatus.NotObservedInTestWindow;
}

/// <summary>Canonical gate reason codes for diagnostic artifacts.</summary>
public enum TradeStreamGateReason
{
    None = 0,
    ProbeDisabled = 1,
    ModeUnknown = 2,
    ProvenanceUnknown = 3,
    ExpectedInstrumentMissing = 4,
    InstrumentMismatch = 5,
    ObservedInstrumentMissing = 6
}

public static class TradeStreamGateReasonNames
{
    public static string? ToWire(TradeStreamGateReason reason) => reason switch
    {
        TradeStreamGateReason.None => null,
        TradeStreamGateReason.ProbeDisabled => "ProbeDisabled",
        TradeStreamGateReason.ModeUnknown => "ModeUnknown",
        TradeStreamGateReason.ProvenanceUnknown => "ProvenanceUnknown",
        TradeStreamGateReason.ExpectedInstrumentMissing => "ExpectedInstrumentMissing",
        TradeStreamGateReason.InstrumentMismatch => "InstrumentMismatch",
        TradeStreamGateReason.ObservedInstrumentMissing => "ObservedInstrumentMissing",
        _ => reason.ToString()
    };
}
