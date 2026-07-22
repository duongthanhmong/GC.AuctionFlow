namespace GC.AuctionFlow.Recorder;

/// <summary>Mutable counters with non-throwing reconciliation.</summary>
public sealed class RecorderCounters
{
    public long CallbackInvocations;
    public long AuthorizedCallbackInvocations;
    public long RejectedCallbackInvocationsByGate;
    public long RejectedCallbackInvocationsAfterDispose;
    public long NullBatchCallbacks;
    public long EmptyBatchCallbacks;
    public long BatchEnumerationFailures;

    public long PayloadItemsEnumerated;
    public long NullItemObservations;
    public long NormalizedObservations;
    public long NormalizationFailures;
    public long AcceptedToQueue;
    public long QueueFullDrops;

    public long InvocationResultEmissionAttempts;
    public long InvocationResultAcceptedToQueue;
    public long InvocationResultQueueFullDrops;
    public long InvocationResultFaults;
    public long InvocationResultsWritten;

    /// <summary>Market-channel dequeues only (excludes invocation-result drafts).</summary>
    public long WriterDequeued;

    /// <summary>
    /// Every writer-handled unit: market dequeue, invocation-result dequeue, and inline lifecycle writes.
    /// </summary>
    public long WriterDequeuedTotal;

    public long MarketEventsWritten;
    public long LifecycleIntegrityRecordsWritten;
    public long RecordsWritten;
    public long SerializationFailures;
    public long WriterDiscardedAfterFatalFault;
    public long UndrainedAtShutdown;
    public long BytesWritten;
    public long SegmentsCompleted;
    public long SegmentsIncomplete;
    public long DiskSpaceStops;
    public long WorkerFaults;
    public long RecoveryEvents;
    public long SegmentWriteFailures;
    public long FlushFailures;
    public long HashFailures;
    public long ManifestFailures;

    public long RecorderCallbacksBeforeStart;
    public long RecorderCallbacksAfterStop;
    public long RecorderStartupFailures;

    public RecorderCountersSnapshot Snapshot() =>
        new(
            CallbackInvocations,
            AuthorizedCallbackInvocations,
            RejectedCallbackInvocationsByGate,
            RejectedCallbackInvocationsAfterDispose,
            NullBatchCallbacks,
            EmptyBatchCallbacks,
            BatchEnumerationFailures,
            PayloadItemsEnumerated,
            NullItemObservations,
            NormalizedObservations,
            NormalizationFailures,
            AcceptedToQueue,
            QueueFullDrops,
            InvocationResultEmissionAttempts,
            InvocationResultAcceptedToQueue,
            InvocationResultQueueFullDrops,
            InvocationResultFaults,
            InvocationResultsWritten,
            WriterDequeued,
            WriterDequeuedTotal,
            MarketEventsWritten,
            LifecycleIntegrityRecordsWritten,
            RecordsWritten,
            SerializationFailures,
            WriterDiscardedAfterFatalFault,
            UndrainedAtShutdown,
            BytesWritten,
            SegmentsCompleted,
            SegmentsIncomplete,
            DiskSpaceStops,
            WorkerFaults,
            RecoveryEvents,
            SegmentWriteFailures,
            FlushFailures,
            HashFailures,
            ManifestFailures,
            RecorderCallbacksBeforeStart,
            RecorderCallbacksAfterStop,
            RecorderStartupFailures);
}

public sealed class ReconciliationResult
{
    public ReconciliationResult(bool ok, IReadOnlyList<string> mismatches)
    {
        Ok = ok;
        Mismatches = mismatches;
    }

    public bool Ok { get; }
    public IReadOnlyList<string> Mismatches { get; }
}

/// <summary>
/// Internal accounting only — not exchange-feed completeness.
/// Never throws; used from production cleanup paths.
/// </summary>
public static class RecorderReconciliation
{
    public static ReconciliationResult Evaluate(RecorderCountersSnapshot c, bool cleanShutdown)
    {
        var mismatches = new List<string>();

        if (c.PayloadItemsEnumerated != c.NormalizedObservations + c.NormalizationFailures + c.NullItemObservations)
        {
            mismatches.Add(
                $"PayloadItemsEnumerated({c.PayloadItemsEnumerated}) != Normalized({c.NormalizedObservations})+NormFail({c.NormalizationFailures})+NullItems({c.NullItemObservations})");
        }

        if (c.NormalizedObservations != c.AcceptedToQueue + c.QueueFullDrops)
        {
            mismatches.Add(
                $"NormalizedObservations({c.NormalizedObservations}) != Accepted({c.AcceptedToQueue})+Drops({c.QueueFullDrops})");
        }

        if (c.AcceptedToQueue != c.WriterDequeued + c.UndrainedAtShutdown)
        {
            mismatches.Add(
                $"AcceptedToQueue({c.AcceptedToQueue}) != WriterDequeued({c.WriterDequeued})+Undrained({c.UndrainedAtShutdown})");
        }

        // Market WriterDequeued reconciles to market+lifecycle RecordsWritten path (compat).
        if (c.WriterDequeued != c.RecordsWritten + c.SerializationFailures + c.WriterDiscardedAfterFatalFault)
        {
            mismatches.Add(
                $"WriterDequeued({c.WriterDequeued}) != Written({c.RecordsWritten})+SerFail({c.SerializationFailures})+Discarded({c.WriterDiscardedAfterFatalFault})");
        }

        if (c.RecordsWritten != c.MarketEventsWritten + c.LifecycleIntegrityRecordsWritten)
        {
            mismatches.Add(
                $"RecordsWritten({c.RecordsWritten}) != Market({c.MarketEventsWritten})+Lifecycle({c.LifecycleIntegrityRecordsWritten})");
        }

        // Total writer invariant across all categories (no double-count).
        if (c.WriterDequeuedTotal
            != c.MarketEventsWritten
            + c.InvocationResultsWritten
            + c.LifecycleIntegrityRecordsWritten
            + c.SerializationFailures
            + c.WriterDiscardedAfterFatalFault)
        {
            mismatches.Add(
                $"WriterDequeuedTotal({c.WriterDequeuedTotal}) != MarketWritten({c.MarketEventsWritten})+InvWritten({c.InvocationResultsWritten})+LifecycleWritten({c.LifecycleIntegrityRecordsWritten})+SerFail({c.SerializationFailures})+Discarded({c.WriterDiscardedAfterFatalFault})");
        }

        if (c.InvocationResultEmissionAttempts
            != c.InvocationResultAcceptedToQueue + c.InvocationResultQueueFullDrops + c.InvocationResultFaults)
        {
            mismatches.Add(
                $"InvocationResultAttempts({c.InvocationResultEmissionAttempts}) != Accepted({c.InvocationResultAcceptedToQueue})+QueueFull({c.InvocationResultQueueFullDrops})+Faults({c.InvocationResultFaults})");
        }

        if (cleanShutdown)
        {
            if (c.UndrainedAtShutdown != 0)
                mismatches.Add($"CleanShutdown UndrainedAtShutdown={c.UndrainedAtShutdown}");
            if (c.WriterDiscardedAfterFatalFault != 0)
                mismatches.Add($"CleanShutdown WriterDiscardedAfterFatalFault={c.WriterDiscardedAfterFatalFault}");
            if (c.RecordsWritten != c.AcceptedToQueue)
                mismatches.Add($"CleanShutdown RecordsWritten({c.RecordsWritten}) != AcceptedToQueue({c.AcceptedToQueue})");
            if (c.InvocationResultsWritten != c.InvocationResultAcceptedToQueue)
                mismatches.Add(
                    $"CleanShutdown InvocationResultsWritten({c.InvocationResultsWritten}) != InvocationResultAcceptedToQueue({c.InvocationResultAcceptedToQueue})");
        }

        return new ReconciliationResult(mismatches.Count == 0, mismatches);
    }
}
