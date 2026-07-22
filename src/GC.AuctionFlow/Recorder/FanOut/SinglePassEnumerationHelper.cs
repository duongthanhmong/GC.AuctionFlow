using GC.AuctionFlow.Recorder.Payloads;

namespace GC.AuctionFlow.Recorder.FanOut;

/// <summary>
/// Result of a single-pass enumeration attempt (callback-neutral, test/infra only).
/// </summary>
public sealed class SinglePassEnumerationResult
{
    public SinglePassEnumerationResult(
        CallbackCaptureContext context,
        bool isBatch,
        bool enumerationCompleted,
        long payloadItemsEnumerated,
        long nullItemObservations,
        long normalizationFailures,
        long fanOutItemRejections,
        long fanOutItemFaults,
        bool finalItemCountKnown,
        string? enumerationFailureTypeSanitized,
        CallbackInvocationResultPayload invocationResult)
    {
        Context = context;
        IsBatch = isBatch;
        EnumerationCompleted = enumerationCompleted;
        PayloadItemsEnumerated = payloadItemsEnumerated;
        NullItemObservations = nullItemObservations;
        NormalizationFailures = normalizationFailures;
        FanOutItemRejections = fanOutItemRejections;
        FanOutItemFaults = fanOutItemFaults;
        FinalItemCountKnown = finalItemCountKnown;
        EnumerationFailureTypeSanitized = enumerationFailureTypeSanitized;
        InvocationResult = invocationResult;
    }

    public CallbackCaptureContext Context { get; }
    public bool IsBatch { get; }
    public bool EnumerationCompleted { get; }
    public long PayloadItemsEnumerated { get; }
    public long NullItemObservations { get; }
    public long NormalizationFailures { get; }
    public long FanOutItemRejections { get; }
    public long FanOutItemFaults { get; }
    public bool FinalItemCountKnown { get; }
    public string? EnumerationFailureTypeSanitized { get; }
    public CallbackInvocationResultPayload InvocationResult { get; }
}

/// <summary>
/// Callback-neutral single-pass enumeration helper for future Trade/DOM wiring.
/// Enumerates exactly once via GetEnumerator/MoveNext/Current/Dispose; never Count/ToList/ToArray.
/// Null raw items and mapper-failure positions consume ordinals (raw enumeration positions).
/// Sink rejection/fault is not classified as NormalizationFailures.
/// Already-emitted market items remain valid after later failure.
/// </summary>
public static class SinglePassEnumerationHelper
{
    /// <summary>Maps a non-null raw item to a fan-out item, or null on mapper failure.</summary>
    public delegate PrimitiveFanOutItem? MapRawItem<TInput>(
        TInput input,
        CallbackCaptureContext context,
        int callbackItemOrdinal);

    /// <summary>
    /// Returns true when accepted; false when rejected (not a mapper failure).
    /// Exceptions from the handler are FanOutItemFaults, not NormalizationFailures.
    /// </summary>
    public delegate bool FanOutItemHandler(PrimitiveFanOutItem item);

    public delegate void InvocationResultHandler(CallbackInvocationResultPayload result);

    public static SinglePassEnumerationResult EnumerateBatchOnce<TInput>(
        IEnumerable<TInput?>? batch,
        CallbackCaptureContext context,
        bool isBatch,
        MapRawItem<TInput> map,
        FanOutItemHandler? onItem,
        InvocationResultHandler? onInvocationResult)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(map);

        long items = 0;
        long nulls = 0;
        long normFails = 0;
        long fanOutRejects = 0;
        long fanOutFaults = 0;
        var completed = false;
        string? failType = null;
        var nextOrdinal = 0;

        if (batch is null)
        {
            completed = true;
            return Finish(
                context, isBatch, completed, items, nulls, normFails, fanOutRejects, fanOutFaults,
                failType, onInvocationResult);
        }

        IEnumerator<TInput?>? enumerator = null;
        try
        {
            try
            {
                enumerator = batch.GetEnumerator();
            }
            catch (Exception ex)
            {
                failType = SanitizeStage("GetEnumerator", ex);
                return Finish(
                    context, isBatch, false, 0, 0, 0, 0, 0, failType, onInvocationResult);
            }

            while (true)
            {
                bool moved;
                try
                {
                    moved = enumerator.MoveNext();
                }
                catch (Exception ex)
                {
                    failType = SanitizeStage("MoveNext", ex);
                    break;
                }

                if (!moved)
                {
                    completed = true;
                    break;
                }

                if (nextOrdinal == int.MaxValue)
                {
                    failType = "CallbackItemOrdinalExhausted";
                    break;
                }

                var ordinal = nextOrdinal++;
                items++;

                TInput? raw;
                try
                {
                    raw = enumerator.Current;
                }
                catch (Exception ex)
                {
                    failType = SanitizeStage("Current", ex);
                    break;
                }

                if (raw is null)
                {
                    nulls++;
                    continue;
                }

                PrimitiveFanOutItem? item;
                try
                {
                    item = map(raw, context, ordinal);
                }
                catch
                {
                    // Mapper throw: ordinal already consumed; not an enumeration-stage abort.
                    // Sink rejection must not land here — this is mapper-only.
                    normFails++;
                    continue;
                }

                if (item is null)
                {
                    normFails++;
                    continue;
                }

                if (onItem is null)
                    continue;

                try
                {
                    if (!onItem(item))
                        fanOutRejects++;
                }
                catch
                {
                    fanOutFaults++;
                }
            }
        }
        finally
        {
            enumerator?.Dispose();
        }

        return Finish(
            context, isBatch, completed, items, nulls, normFails, fanOutRejects, fanOutFaults,
            failType, onInvocationResult);
    }

    /// <summary>Singular non-null: ordinal 0, items=1, completed, final known.</summary>
    public static SinglePassEnumerationResult EnumerateSingularOnce<TInput>(
        TInput? raw,
        CallbackCaptureContext context,
        MapRawItem<TInput> map,
        FanOutItemHandler? onItem,
        InvocationResultHandler? onInvocationResult)
        where TInput : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(map);

        long items = 0;
        long nulls = 0;
        long normFails = 0;
        long fanOutRejects = 0;
        long fanOutFaults = 0;
        var completed = true;
        string? failType = null;

        if (raw is null)
        {
            // Null singular: no market event; PayloadItemsEnumerated=1, NullItemObservations=1.
            nulls = 1;
            items = 1;
        }
        else
        {
            items = 1;
            try
            {
                var item = map(raw, context, callbackItemOrdinal: 0);
                if (item is null)
                    normFails = 1;
                else if (onItem is not null)
                {
                    try
                    {
                        if (!onItem(item))
                            fanOutRejects = 1;
                    }
                    catch
                    {
                        fanOutFaults = 1;
                    }
                }
            }
            catch (Exception ex)
            {
                normFails = 1;
                failType = SanitizeStage("Mapper", ex);
                completed = false;
            }
        }

        return Finish(
            context, isBatch: false, completed, items, nulls, normFails, fanOutRejects, fanOutFaults,
            failType, onInvocationResult);
    }

    private static SinglePassEnumerationResult Finish(
        CallbackCaptureContext context,
        bool isBatch,
        bool completed,
        long items,
        long nulls,
        long normFails,
        long fanOutRejects,
        long fanOutFaults,
        string? failType,
        InvocationResultHandler? onInvocationResult)
    {
        // Successful completed enumeration => FinalItemCountKnown=true.
        // Failed/interrupted => FinalItemCountKnown=false.
        var finalKnown = completed && failType is null;
        var resultPayload = new CallbackInvocationResultPayload(
            context.CallbackSource,
            context.CallbackInvocationSequence,
            context.CallbackReceiveUtc,
            context.CallbackReceiveStopwatchTimestamp,
            context.CallbackManagedThreadId,
            isBatch,
            enumerationCompleted: completed,
            payloadItemsEnumerated: items,
            nullItemObservations: nulls,
            normalizationFailures: normFails,
            fanOutItemRejections: fanOutRejects,
            fanOutItemFaults: fanOutFaults,
            finalItemCountKnown: finalKnown,
            enumerationFailureTypeSanitized: failType);

        try
        {
            onInvocationResult?.Invoke(resultPayload);
        }
        catch
        {
            // Exactly one emission attempt; result-emission failure must not escape.
        }

        return new SinglePassEnumerationResult(
            context,
            isBatch,
            completed,
            items,
            nulls,
            normFails,
            fanOutRejects,
            fanOutFaults,
            finalKnown,
            failType,
            resultPayload);
    }

    /// <summary>Stage:TypeName only — no message, stack, or raw object.</summary>
    public static string SanitizeStage(string stage, Exception ex) =>
        stage + ":" + SanitizeExceptionType(ex);

    public static string SanitizeExceptionType(Exception ex)
    {
        var name = ex.GetType().Name;
        if (name.Length > 64) name = name[..64];
        return name;
    }
}
