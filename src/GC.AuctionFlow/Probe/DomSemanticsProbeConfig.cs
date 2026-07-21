namespace GC.AuctionFlow.Probe;

/// <summary>
/// Explicit bounded config for depth bursts (seed values, subject to sensitivity test).
/// Default queue 16384 — higher than trade probe due to observed ES depth EPS peaks.
/// </summary>
public sealed class DomSemanticsProbeConfig
{
    public const int DefaultQueueCapacity = 16384;
    public const int DefaultMaxSamples = 256;
    public const int DefaultMaxObservedLevels = 512;
    public const int DefaultDrainTimeoutMilliseconds = 2000;
    public const int DefaultOverlapFingerprintSetCapacity = 8192;

    public DomSemanticsProbeConfig(
        int queueCapacity = DefaultQueueCapacity,
        int maxSamples = DefaultMaxSamples,
        int maxObservedLevels = DefaultMaxObservedLevels,
        int drainTimeoutMilliseconds = DefaultDrainTimeoutMilliseconds,
        int overlapFingerprintSetCapacity = DefaultOverlapFingerprintSetCapacity)
    {
        if (queueCapacity < 1) throw new ArgumentOutOfRangeException(nameof(queueCapacity));
        if (maxSamples < 0) throw new ArgumentOutOfRangeException(nameof(maxSamples));
        if (maxObservedLevels < 1) throw new ArgumentOutOfRangeException(nameof(maxObservedLevels));
        if (drainTimeoutMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(drainTimeoutMilliseconds));
        if (overlapFingerprintSetCapacity < 1) throw new ArgumentOutOfRangeException(nameof(overlapFingerprintSetCapacity));

        QueueCapacity = queueCapacity;
        MaxSamples = maxSamples;
        MaxObservedLevels = maxObservedLevels;
        DrainTimeoutMilliseconds = drainTimeoutMilliseconds;
        OverlapFingerprintSetCapacity = overlapFingerprintSetCapacity;
    }

    public int QueueCapacity { get; }
    public int MaxSamples { get; }
    public int MaxObservedLevels { get; }
    public int DrainTimeoutMilliseconds { get; }
    public int OverlapFingerprintSetCapacity { get; }
}
