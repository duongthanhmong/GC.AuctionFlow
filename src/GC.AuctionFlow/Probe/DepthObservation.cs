namespace GC.AuctionFlow.Probe;

/// <summary>
/// Immutable primitive-only depth observation. Never retains ATAS payload references.
/// Source Time is not converted to UTC. DepthUpdateAction is always Unknown in P0-05.
/// </summary>
public sealed class DepthObservation
{
    public DepthObservation(
        DepthCallbackSource callbackSource,
        long localMonotonicSequence,
        long sourceTimeTicks,
        DateTimeKind sourceDateTimeKind,
        DateTime receiveUtc,
        long receiveStopwatchTimestamp,
        int callbackManagedThreadId,
        string observedInstrumentIdentityKey,
        decimal price,
        decimal volume,
        string rawDataType,
        bool isBid,
        bool isAsk,
        DepthSide derivedSide,
        string diagnosticFingerprint)
    {
        CallbackSource = callbackSource;
        LocalMonotonicSequence = localMonotonicSequence;
        SourceTimeTicks = sourceTimeTicks;
        SourceDateTimeKind = sourceDateTimeKind;
        ReceiveUtc = receiveUtc;
        ReceiveStopwatchTimestamp = receiveStopwatchTimestamp;
        CallbackManagedThreadId = callbackManagedThreadId;
        ObservedInstrumentIdentityKey = observedInstrumentIdentityKey;
        Price = price;
        Volume = volume;
        RawDataType = rawDataType;
        IsBid = isBid;
        IsAsk = isAsk;
        DerivedSide = derivedSide;
        UpdateAction = DepthUpdateAction.Unknown;
        DiagnosticFingerprint = diagnosticFingerprint;
    }

    public DepthCallbackSource CallbackSource { get; }
    public long LocalMonotonicSequence { get; }
    public long SourceTimeTicks { get; }
    public DateTimeKind SourceDateTimeKind { get; }
    public DateTime ReceiveUtc { get; }
    public long ReceiveStopwatchTimestamp { get; }
    public int CallbackManagedThreadId { get; }
    public string ObservedInstrumentIdentityKey { get; }
    public decimal Price { get; }
    public decimal Volume { get; }
    public string RawDataType { get; }
    public bool IsBid { get; }
    public bool IsAsk { get; }
    public DepthSide DerivedSide { get; }
    public DepthUpdateAction UpdateAction { get; }
    public string DiagnosticFingerprint { get; }
}

/// <summary>Conservative side derivation — conflicts become Unknown; never silently pick one field.</summary>
public static class DepthSideClassifier
{
    public static DepthSide Classify(string rawDataType, bool isBid, bool isAsk, out string detail)
    {
        var dt = rawDataType ?? "";
        if (string.Equals(dt, "Trade", StringComparison.Ordinal))
        {
            detail = "UnexpectedTradeDataType";
            return DepthSide.Unknown;
        }

        if (string.Equals(dt, "Bid", StringComparison.Ordinal))
        {
            if (isBid && !isAsk)
            {
                detail = "ConsistentBid";
                return DepthSide.Bid;
            }

            detail = "ConflictingSideFields";
            return DepthSide.Unknown;
        }

        if (string.Equals(dt, "Ask", StringComparison.Ordinal))
        {
            if (isAsk && !isBid)
            {
                detail = "ConsistentAsk";
                return DepthSide.Ask;
            }

            detail = "ConflictingSideFields";
            return DepthSide.Unknown;
        }

        detail = "UnknownDataType";
        return DepthSide.Unknown;
    }
}
