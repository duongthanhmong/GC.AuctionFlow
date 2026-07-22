using System.Globalization;

namespace GC.AuctionFlow.Probe;

/// <summary>
/// Immutable primitive-only MBO observation. Never retains ATAS payload references.
/// InterpretedLifecycleAction is always Unknown in P0-06.
/// </summary>
public sealed class MboObservation
{
    public MboObservation(
        MboCallbackSource callbackSource,
        long subscriptionEpoch,
        long localMonotonicSequence,
        DateTime receiveUtc,
        long receiveStopwatchTimestamp,
        int callbackManagedThreadId,
        long sourceTimeTicks,
        DateTimeKind sourceDateTimeKind,
        string instrumentIdentityKey,
        string? securityCode,
        string? securityId,
        string? exchange,
        string rawTypeName,
        int rawTypeNumeric,
        bool rawTypeIsKnownEnumMember,
        string rawSideName,
        int rawSideNumeric,
        MboDerivedSide derivedSide,
        long exchangeOrderId,
        decimal price,
        decimal volume,
        long priority,
        string diagnosticFingerprint)
    {
        CallbackSource = callbackSource;
        SubscriptionEpoch = subscriptionEpoch;
        LocalMonotonicSequence = localMonotonicSequence;
        ReceiveUtc = receiveUtc;
        ReceiveStopwatchTimestamp = receiveStopwatchTimestamp;
        CallbackManagedThreadId = callbackManagedThreadId;
        SourceTimeTicks = sourceTimeTicks;
        SourceDateTimeKind = sourceDateTimeKind;
        InstrumentIdentityKey = instrumentIdentityKey;
        SecurityCode = securityCode;
        SecurityId = securityId;
        Exchange = exchange;
        RawTypeName = rawTypeName;
        RawTypeNumeric = rawTypeNumeric;
        RawTypeIsKnownEnumMember = rawTypeIsKnownEnumMember;
        InterpretedLifecycleAction = MboInterpretedLifecycleAction.Unknown;
        RawSideName = rawSideName;
        RawSideNumeric = rawSideNumeric;
        DerivedSide = derivedSide;
        ExchangeOrderId = exchangeOrderId;
        Price = price;
        Volume = volume;
        Priority = priority;
        DiagnosticFingerprint = diagnosticFingerprint;
    }

    public MboCallbackSource CallbackSource { get; }
    public long SubscriptionEpoch { get; }
    public long LocalMonotonicSequence { get; }
    public DateTime ReceiveUtc { get; }
    public long ReceiveStopwatchTimestamp { get; }
    public int CallbackManagedThreadId { get; }
    public long SourceTimeTicks { get; }
    public DateTimeKind SourceDateTimeKind { get; }
    public string InstrumentIdentityKey { get; }
    public string? SecurityCode { get; }
    public string? SecurityId { get; }
    public string? Exchange { get; }
    public string RawTypeName { get; }
    public int RawTypeNumeric { get; }
    public bool RawTypeIsKnownEnumMember { get; }
    public MboInterpretedLifecycleAction InterpretedLifecycleAction { get; }
    public string RawSideName { get; }
    public int RawSideNumeric { get; }
    public MboDerivedSide DerivedSide { get; }
    public long ExchangeOrderId { get; }
    public decimal Price { get; }
    public decimal Volume { get; }
    public long Priority { get; }
    public string DiagnosticFingerprint { get; }

    /// <summary>Diagnostic correlation key: epoch + instrument + ExchangeOrderId (nonzero IDs only for keyed state).</summary>
    public string CorrelationKey =>
        MboCorrelationKey.Build(SubscriptionEpoch, InstrumentIdentityKey, ExchangeOrderId);
}

public static class MboCorrelationKey
{
    public static string Build(long epoch, string instrumentIdentityKey, long exchangeOrderId) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{epoch}|{instrumentIdentityKey}|{exchangeOrderId}");
}

public static class MboSideClassifier
{
    public static MboDerivedSide Classify(int rawSideNumeric, out string detail)
    {
        if (rawSideNumeric == MboKnownRawSideTypes.Bid)
        {
            detail = "Bid";
            return MboDerivedSide.Bid;
        }

        if (rawSideNumeric == MboKnownRawSideTypes.Ask)
        {
            detail = "Ask";
            return MboDerivedSide.Ask;
        }

        if (rawSideNumeric == MboKnownRawSideTypes.Trade)
        {
            detail = "TradeMappedToUnknown";
            return MboDerivedSide.Unknown;
        }

        detail = "UnmappedSideNumeric";
        return MboDerivedSide.Unknown;
    }
}

public static class MboFingerprints
{
    /// <summary>Diagnostic only — not an order ID, sequence, or merge key.</summary>
    public static string Core(
        long sourceTimeTicks,
        long exchangeOrderId,
        int rawTypeNumeric,
        int rawSideNumeric,
        decimal price,
        decimal volume,
        long priority) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{sourceTimeTicks}|{exchangeOrderId}|{rawTypeNumeric}|{rawSideNumeric}|{price}|{volume}|{priority}");
}
