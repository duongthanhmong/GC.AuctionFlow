using System.Text.Json.Serialization;

namespace GC.AuctionFlow.Recorder.Payloads;

/// <summary>
/// Closed payload taxonomy. No object/dynamic/JsonElement market bags.
/// Cumulative tick constituents are not represented in P0-07B.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(NewTradePayload), "NewTrade")]
[JsonDerivedType(typeof(CumulativeTradeNewPayload), "CumulativeTradeNew")]
[JsonDerivedType(typeof(CumulativeTradeUpdatePayload), "CumulativeTradeUpdate")]
[JsonDerivedType(typeof(DepthPayload), "Depth")]
[JsonDerivedType(typeof(BestBidAskPayload), "BestBidAsk")]
[JsonDerivedType(typeof(DomSnapshotRequestPayload), "DomSnapshotRequest")]
[JsonDerivedType(typeof(DomSnapshotItemPayload), "DomSnapshotItem")]
[JsonDerivedType(typeof(DomSnapshotLocalEnumerationResultPayload), "DomSnapshotLocalEnumerationResult")]
[JsonDerivedType(typeof(MboPayload), "Mbo")]
[JsonDerivedType(typeof(RecorderLifecyclePayload), "RecorderLifecycle")]
[JsonDerivedType(typeof(RecorderIntegrityPayload), "RecorderIntegrity")]
[JsonDerivedType(typeof(CallbackInvocationResultPayload), "CallbackInvocationResult")]
public abstract class RawEventPayload
{
    [JsonIgnore]
    public abstract RawEventPayloadKind PayloadKind { get; }
}

public sealed class NewTradePayload : RawEventPayload
{
    public NewTradePayload(
        decimal price,
        decimal volume,
        decimal originPrice,
        long directionRaw,
        string directionName,
        long dataTypeRaw,
        string dataTypeName,
        bool isAsk,
        bool isBid,
        long? exchangeOrderId,
        long? aggressorExchangeOrderId,
        decimal? openInterest)
    {
        Price = price;
        Volume = volume;
        OriginPrice = originPrice;
        DirectionRaw = directionRaw;
        DirectionName = directionName ?? string.Empty;
        DataTypeRaw = dataTypeRaw;
        DataTypeName = dataTypeName ?? string.Empty;
        IsAsk = isAsk;
        IsBid = isBid;
        ExchangeOrderId = exchangeOrderId;
        AggressorExchangeOrderId = aggressorExchangeOrderId;
        OpenInterest = openInterest;
    }

    public override RawEventPayloadKind PayloadKind => RawEventPayloadKind.NewTrade;
    public decimal Price { get; }
    public decimal Volume { get; }
    public decimal OriginPrice { get; }
    public long DirectionRaw { get; }
    public string DirectionName { get; }
    public long DataTypeRaw { get; }
    public string DataTypeName { get; }
    public bool IsAsk { get; }
    public bool IsBid { get; }
    public long? ExchangeOrderId { get; }
    public long? AggressorExchangeOrderId { get; }
    /// <summary>Raw OpenInterest field copy — not a validated OI claim.</summary>
    public decimal? OpenInterest { get; }
}

/// <summary>
/// Cumulative trade aggregate. Constituent ticks are not recorded.
/// CumulativeTickConstituentsRecorded is always false.
/// ReportedTickCount is null when no independent aggregate tick-count property exists (never zero-for-unknown).
/// </summary>
public sealed class CumulativeTradeNewPayload : RawEventPayload
{
    public CumulativeTradeNewPayload(
        decimal volume,
        decimal firstPrice,
        decimal lastPrice,
        long directionRaw,
        string directionName,
        bool reportedTickCountAvailable,
        int? reportedTickCount,
        long? processLocalInstanceId,
        bool processLocalInstanceIdObserved)
    {
        Volume = volume;
        FirstPrice = firstPrice;
        LastPrice = lastPrice;
        DirectionRaw = directionRaw;
        DirectionName = directionName ?? string.Empty;
        ReportedTickCountAvailable = reportedTickCountAvailable;
        ReportedTickCount = reportedTickCountAvailable ? reportedTickCount : null;
        ProcessLocalInstanceId = processLocalInstanceId;
        ProcessLocalInstanceIdObserved = processLocalInstanceIdObserved;
        CumulativeTickConstituentsRecorded = false;
    }

    public override RawEventPayloadKind PayloadKind => RawEventPayloadKind.CumulativeTradeNew;
    public decimal Volume { get; }
    public decimal FirstPrice { get; }
    public decimal LastPrice { get; }
    public long DirectionRaw { get; }
    public string DirectionName { get; }
    public bool ReportedTickCountAvailable { get; }
    public int? ReportedTickCount { get; }
    public long? ProcessLocalInstanceId { get; }
    public bool ProcessLocalInstanceIdObserved { get; }
    public bool CumulativeTickConstituentsRecorded { get; }
}

/// <summary>
/// Cumulative trade update aggregate. Constituent ticks are not recorded.
/// </summary>
public sealed class CumulativeTradeUpdatePayload : RawEventPayload
{
    public CumulativeTradeUpdatePayload(
        decimal volume,
        decimal firstPrice,
        decimal lastPrice,
        long directionRaw,
        string directionName,
        bool reportedTickCountAvailable,
        int? reportedTickCount,
        long? processLocalInstanceId,
        bool processLocalInstanceIdObserved)
    {
        Volume = volume;
        FirstPrice = firstPrice;
        LastPrice = lastPrice;
        DirectionRaw = directionRaw;
        DirectionName = directionName ?? string.Empty;
        ReportedTickCountAvailable = reportedTickCountAvailable;
        ReportedTickCount = reportedTickCountAvailable ? reportedTickCount : null;
        ProcessLocalInstanceId = processLocalInstanceId;
        ProcessLocalInstanceIdObserved = processLocalInstanceIdObserved;
        CumulativeTickConstituentsRecorded = false;
    }

    public override RawEventPayloadKind PayloadKind => RawEventPayloadKind.CumulativeTradeUpdate;
    public decimal Volume { get; }
    public decimal FirstPrice { get; }
    public decimal LastPrice { get; }
    public long DirectionRaw { get; }
    public string DirectionName { get; }
    public bool ReportedTickCountAvailable { get; }
    public int? ReportedTickCount { get; }
    public long? ProcessLocalInstanceId { get; }
    public bool ProcessLocalInstanceIdObserved { get; }
    public bool CumulativeTickConstituentsRecorded { get; }
}

public sealed class DepthPayload : RawEventPayload
{
    public DepthPayload(
        decimal price,
        decimal volume,
        string rawDataType,
        bool isBid,
        bool isAsk,
        string derivedSide)
    {
        Price = price;
        Volume = volume;
        RawDataType = rawDataType ?? string.Empty;
        IsBid = isBid;
        IsAsk = isAsk;
        DerivedSide = derivedSide ?? string.Empty;
        UpdateAction = "Unknown";
    }

    public override RawEventPayloadKind PayloadKind => RawEventPayloadKind.Depth;
    public decimal Price { get; }
    public decimal Volume { get; }
    public string RawDataType { get; }
    public bool IsBid { get; }
    public bool IsAsk { get; }
    public string DerivedSide { get; }
    public string UpdateAction { get; }
}

public sealed class BestBidAskPayload : RawEventPayload
{
    public BestBidAskPayload(
        decimal bidPrice,
        decimal bidVolume,
        decimal askPrice,
        decimal askVolume,
        string rawDataType)
    {
        BidPrice = bidPrice;
        BidVolume = bidVolume;
        AskPrice = askPrice;
        AskVolume = askVolume;
        RawDataType = rawDataType ?? string.Empty;
    }

    public override RawEventPayloadKind PayloadKind => RawEventPayloadKind.BestBidAsk;
    public decimal BidPrice { get; }
    public decimal BidVolume { get; }
    public decimal AskPrice { get; }
    public decimal AskVolume { get; }
    public string RawDataType { get; }
}

public sealed class DomSnapshotRequestPayload : RawEventPayload
{
    public DomSnapshotRequestPayload(Guid snapshotRequestId, DateTime requestUtc)
    {
        SnapshotRequestId = snapshotRequestId;
        RequestUtc = requestUtc;
        ProviderSnapshotCompletionKnown = false;
    }

    public override RawEventPayloadKind PayloadKind => RawEventPayloadKind.DomSnapshotRequest;
    public Guid SnapshotRequestId { get; }
    public DateTime RequestUtc { get; }
    public bool ProviderSnapshotCompletionKnown { get; }
}

public sealed class DomSnapshotItemPayload : RawEventPayload
{
    public DomSnapshotItemPayload(
        Guid snapshotRequestId,
        int itemIndex,
        decimal price,
        decimal volume,
        string rawDataType,
        bool isBid,
        bool isAsk,
        string derivedSide)
    {
        SnapshotRequestId = snapshotRequestId;
        ItemIndex = itemIndex;
        Price = price;
        Volume = volume;
        RawDataType = rawDataType ?? string.Empty;
        IsBid = isBid;
        IsAsk = isAsk;
        DerivedSide = derivedSide ?? string.Empty;
        ProviderSnapshotCompletionKnown = false;
    }

    public override RawEventPayloadKind PayloadKind => RawEventPayloadKind.DomSnapshotItem;
    public Guid SnapshotRequestId { get; }
    public int ItemIndex { get; }
    public decimal Price { get; }
    public decimal Volume { get; }
    public string RawDataType { get; }
    public bool IsBid { get; }
    public bool IsAsk { get; }
    public string DerivedSide { get; }
    public bool ProviderSnapshotCompletionKnown { get; }
}

/// <summary>
/// Local enumeration result only. ProviderSnapshotCompletionKnown remains false.
/// </summary>
public sealed class DomSnapshotLocalEnumerationResultPayload : RawEventPayload
{
    public DomSnapshotLocalEnumerationResultPayload(
        Guid snapshotRequestId,
        bool attempted,
        bool executed,
        bool enumerationCompletedLocally,
        int itemCount,
        string? enumerationFailure)
    {
        SnapshotRequestId = snapshotRequestId;
        Attempted = attempted;
        Executed = executed;
        EnumerationCompletedLocally = enumerationCompletedLocally;
        ItemCount = itemCount;
        EnumerationFailure = enumerationFailure;
        ProviderSnapshotCompletionKnown = false;
    }

    public override RawEventPayloadKind PayloadKind => RawEventPayloadKind.DomSnapshotLocalEnumerationResult;
    public Guid SnapshotRequestId { get; }
    public bool Attempted { get; }
    public bool Executed { get; }
    public bool EnumerationCompletedLocally { get; }
    public int ItemCount { get; }
    public string? EnumerationFailure { get; }
    public bool ProviderSnapshotCompletionKnown { get; }
}

/// <summary>Schema-capable MBO payload. Primary-process recording remains blocked.</summary>
public sealed class MboPayload : RawEventPayload
{
    public MboPayload(
        string rawTypeName,
        int rawTypeNumeric,
        bool rawTypeIsKnownEnumMember,
        string rawSideName,
        int rawSideNumeric,
        string derivedSide,
        long exchangeOrderId,
        decimal price,
        decimal volume,
        long priority)
    {
        RawTypeName = rawTypeName ?? string.Empty;
        RawTypeNumeric = rawTypeNumeric;
        RawTypeIsKnownEnumMember = rawTypeIsKnownEnumMember;
        InterpretedLifecycleAction = "Unknown";
        RawSideName = rawSideName ?? string.Empty;
        RawSideNumeric = rawSideNumeric;
        DerivedSide = derivedSide ?? string.Empty;
        ExchangeOrderId = exchangeOrderId;
        Price = price;
        Volume = volume;
        Priority = priority;
        SnapshotCompletionKnown = false;
    }

    public override RawEventPayloadKind PayloadKind => RawEventPayloadKind.Mbo;
    public string RawTypeName { get; }
    public int RawTypeNumeric { get; }
    public bool RawTypeIsKnownEnumMember { get; }
    public string InterpretedLifecycleAction { get; }
    public string RawSideName { get; }
    public int RawSideNumeric { get; }
    public string DerivedSide { get; }
    public long ExchangeOrderId { get; }
    public decimal Price { get; }
    public decimal Volume { get; }
    public long Priority { get; }
    public bool SnapshotCompletionKnown { get; }
}

public sealed class RecorderLifecyclePayload : RawEventPayload
{
    public RecorderLifecyclePayload(
        RecorderLifecycleEventKind eventKind,
        string detail,
        int? previousContractEpoch,
        int? newContractEpoch,
        Guid? relatedSegmentId,
        string? previousIdentityTuple = null,
        string? newIdentityTuple = null)
    {
        EventKind = eventKind;
        Detail = detail ?? string.Empty;
        PreviousContractEpoch = previousContractEpoch;
        NewContractEpoch = newContractEpoch;
        RelatedSegmentId = relatedSegmentId;
        PreviousIdentityTuple = previousIdentityTuple;
        NewIdentityTuple = newIdentityTuple;
    }

    public override RawEventPayloadKind PayloadKind => RawEventPayloadKind.RecorderLifecycle;
    public RecorderLifecycleEventKind EventKind { get; }
    public string Detail { get; }
    public int? PreviousContractEpoch { get; }
    public int? NewContractEpoch { get; }
    public Guid? RelatedSegmentId { get; }
    public string? PreviousIdentityTuple { get; }
    public string? NewIdentityTuple { get; }
}

public sealed class RecorderIntegrityPayload : RawEventPayload
{
    public RecorderIntegrityPayload(
        RecorderIntegrityEventKind eventKind,
        string detail,
        string? recoveryClassification)
    {
        EventKind = eventKind;
        Detail = detail ?? string.Empty;
        RecoveryClassification = recoveryClassification;
    }

    public override RawEventPayloadKind PayloadKind => RawEventPayloadKind.RecorderIntegrity;
    public RecorderIntegrityEventKind EventKind { get; }
    public string Detail { get; }
    public string? RecoveryClassification { get; }
}

/// <summary>
/// Local recorder metadata after a callback enumeration attempt — not a market event.
/// Absence of this record means callback completion is unknown.
/// </summary>
public sealed class CallbackInvocationResultPayload : RawEventPayload
{
    public CallbackInvocationResultPayload(
        RecorderCallbackSource callbackSource,
        long callbackInvocationSequence,
        DateTime callbackReceiveUtc,
        long callbackReceiveStopwatchTimestamp,
        int callbackManagedThreadId,
        bool isBatch,
        bool enumerationCompleted,
        long payloadItemsEnumerated,
        long nullItemObservations,
        long normalizationFailures,
        long fanOutItemRejections,
        long fanOutItemFaults,
        bool finalItemCountKnown,
        string? enumerationFailureTypeSanitized)
    {
        CallbackSource = callbackSource;
        CallbackInvocationSequence = callbackInvocationSequence;
        CallbackReceiveUtc = callbackReceiveUtc;
        CallbackReceiveStopwatchTimestamp = callbackReceiveStopwatchTimestamp;
        CallbackManagedThreadId = callbackManagedThreadId;
        IsBatch = isBatch;
        EnumerationCompleted = enumerationCompleted;
        PayloadItemsEnumerated = payloadItemsEnumerated;
        NullItemObservations = nullItemObservations;
        NormalizationFailures = normalizationFailures;
        FanOutItemRejections = fanOutItemRejections;
        FanOutItemFaults = fanOutItemFaults;
        FinalItemCountKnown = finalItemCountKnown;
        EnumerationFailureTypeSanitized = enumerationFailureTypeSanitized;
    }

    public override RawEventPayloadKind PayloadKind => RawEventPayloadKind.CallbackInvocationResult;
    public RecorderCallbackSource CallbackSource { get; }
    public long CallbackInvocationSequence { get; }
    public DateTime CallbackReceiveUtc { get; }
    public long CallbackReceiveStopwatchTimestamp { get; }
    public int CallbackManagedThreadId { get; }
    public bool IsBatch { get; }
    public bool EnumerationCompleted { get; }
    public long PayloadItemsEnumerated { get; }
    public long NullItemObservations { get; }
    public long NormalizationFailures { get; }
    /// <summary>Sink returned false/rejected — not a mapper failure.</summary>
    public long FanOutItemRejections { get; }
    /// <summary>Sink threw — not a mapper failure.</summary>
    public long FanOutItemFaults { get; }
    public bool FinalItemCountKnown { get; }
    public string? EnumerationFailureTypeSanitized { get; }
}
