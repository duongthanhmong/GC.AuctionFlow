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

    public long WriterDequeued;
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
            WriterDequeued,
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
            ManifestFailures);
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

        if (c.WriterDequeued != c.RecordsWritten + c.SerializationFailures + c.WriterDiscardedAfterFatalFault)
        {
            mismatches.Add(
                $"WriterDequeued({c.WriterDequeued}) != Written({c.RecordsWritten})+SerFail({c.SerializationFailures})+Discarded({c.WriterDiscardedAfterFatalFault})");
        }

        if (cleanShutdown)
        {
            if (c.UndrainedAtShutdown != 0)
                mismatches.Add($"CleanShutdown UndrainedAtShutdown={c.UndrainedAtShutdown}");
            if (c.WriterDiscardedAfterFatalFault != 0)
                mismatches.Add($"CleanShutdown WriterDiscardedAfterFatalFault={c.WriterDiscardedAfterFatalFault}");
            if (c.RecordsWritten != c.AcceptedToQueue)
                mismatches.Add($"CleanShutdown RecordsWritten({c.RecordsWritten}) != AcceptedToQueue({c.AcceptedToQueue})");
        }

        return new ReconciliationResult(mismatches.Count == 0, mismatches);
    }
}
