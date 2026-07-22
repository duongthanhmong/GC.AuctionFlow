namespace GC.AuctionFlow.Probe;

/// <summary>
/// Default queue 32768 — seed value, subject to sensitivity test (MBO bursts).
/// MaxTrackedNonzeroIds 65536 — seed value, subject to sensitivity test.
/// </summary>
public sealed class MboLifecycleProbeConfig
{
    public const int DefaultQueueCapacity = 32768;
    public const int DefaultMaxSamples = 256;
    public const int DefaultDrainTimeoutMilliseconds = 2000;
    public const int DefaultMaxTrackedNonzeroIds = 65536;

    public MboLifecycleProbeConfig(
        int queueCapacity = DefaultQueueCapacity,
        int maxSamples = DefaultMaxSamples,
        int drainTimeoutMilliseconds = DefaultDrainTimeoutMilliseconds,
        int maxTrackedNonzeroIds = DefaultMaxTrackedNonzeroIds)
    {
        if (queueCapacity < 1) throw new ArgumentOutOfRangeException(nameof(queueCapacity));
        if (maxSamples < 0) throw new ArgumentOutOfRangeException(nameof(maxSamples));
        if (drainTimeoutMilliseconds < 1) throw new ArgumentOutOfRangeException(nameof(drainTimeoutMilliseconds));
        if (maxTrackedNonzeroIds < 1) throw new ArgumentOutOfRangeException(nameof(maxTrackedNonzeroIds));
        QueueCapacity = queueCapacity;
        MaxSamples = maxSamples;
        DrainTimeoutMilliseconds = drainTimeoutMilliseconds;
        MaxTrackedNonzeroIds = maxTrackedNonzeroIds;
    }

    public int QueueCapacity { get; }
    public int MaxSamples { get; }
    public int DrainTimeoutMilliseconds { get; }
    public int MaxTrackedNonzeroIds { get; }

    /// <summary>Alias for MaxTrackedNonzeroIds (historical name).</summary>
    public int MaxTrackedOrderIds => MaxTrackedNonzeroIds;
}
