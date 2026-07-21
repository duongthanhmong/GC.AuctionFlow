namespace GC.AuctionFlow.Probe;

/// <summary>
/// Explicit bounded configuration constants (seed values, subject to sensitivity test).
/// </summary>
public sealed class TradeStreamProbeConfig
{
    /// <summary>Bounded channel capacity. Seed value, subject to sensitivity test.</summary>
    public const int DefaultQueueCapacity = 4096;

    /// <summary>Max retained new-trade samples in snapshot. Seed value, subject to sensitivity test.</summary>
    public const int DefaultMaxNewTradeSamples = 256;

    /// <summary>Max retained cumulative samples. Seed value, subject to sensitivity test.</summary>
    public const int DefaultMaxCumulativeSamples = 128;

    /// <summary>Max constituent prints copied per cumulative observation. Seed value, subject to sensitivity test.</summary>
    public const int DefaultMaxConstituentPrintsPerCumulative = 32;

    /// <summary>Worker drain timeout on dispose (ms). Seed value, subject to sensitivity test.</summary>
    public const int DefaultDrainTimeoutMilliseconds = 2000;

    /// <summary>Max core fingerprints retained for overlap sets. Seed value, subject to sensitivity test.</summary>
    public const int DefaultOverlapFingerprintSetCapacity = 4096;

    public TradeStreamProbeConfig(
        int queueCapacity = DefaultQueueCapacity,
        int maxNewTradeSamples = DefaultMaxNewTradeSamples,
        int maxCumulativeSamples = DefaultMaxCumulativeSamples,
        int maxConstituentPrintsPerCumulative = DefaultMaxConstituentPrintsPerCumulative,
        int drainTimeoutMilliseconds = DefaultDrainTimeoutMilliseconds,
        int overlapFingerprintSetCapacity = DefaultOverlapFingerprintSetCapacity,
        bool copyConstituentPrintSummaries = true)
    {
        if (queueCapacity < 1)
            throw new ArgumentOutOfRangeException(nameof(queueCapacity));
        if (maxNewTradeSamples < 0)
            throw new ArgumentOutOfRangeException(nameof(maxNewTradeSamples));
        if (maxCumulativeSamples < 0)
            throw new ArgumentOutOfRangeException(nameof(maxCumulativeSamples));
        if (maxConstituentPrintsPerCumulative < 0)
            throw new ArgumentOutOfRangeException(nameof(maxConstituentPrintsPerCumulative));
        if (drainTimeoutMilliseconds < 0)
            throw new ArgumentOutOfRangeException(nameof(drainTimeoutMilliseconds));
        if (overlapFingerprintSetCapacity < 1)
            throw new ArgumentOutOfRangeException(nameof(overlapFingerprintSetCapacity));

        QueueCapacity = queueCapacity;
        MaxNewTradeSamples = maxNewTradeSamples;
        MaxCumulativeSamples = maxCumulativeSamples;
        MaxConstituentPrintsPerCumulative = maxConstituentPrintsPerCumulative;
        DrainTimeoutMilliseconds = drainTimeoutMilliseconds;
        OverlapFingerprintSetCapacity = overlapFingerprintSetCapacity;
        CopyConstituentPrintSummaries = copyConstituentPrintSummaries;
    }

    public int QueueCapacity { get; }
    public int MaxNewTradeSamples { get; }
    public int MaxCumulativeSamples { get; }
    public int MaxConstituentPrintsPerCumulative { get; }
    public int DrainTimeoutMilliseconds { get; }
    public int OverlapFingerprintSetCapacity { get; }
    public bool CopyConstituentPrintSummaries { get; }
}
