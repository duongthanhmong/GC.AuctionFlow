namespace GC.AuctionFlow.Recorder;

/// <summary>Immutable recorder settings (seed values, subject to sensitivity test).</summary>
public sealed class RecorderConfig
{
    public const long DefaultMaxSegmentBytes = 64L * 1024 * 1024;
    public static readonly TimeSpan DefaultMaxSegmentDuration = TimeSpan.FromMinutes(5);
    public const int DefaultMaxRecordsPerSegment = 250_000;
    public const int DefaultQueueCapacity = 32_768;
    public static readonly TimeSpan DefaultFlushInterval = TimeSpan.FromSeconds(1);
    public const int DefaultShutdownDrainTimeoutMilliseconds = 2000;
    public const int DefaultMaxFramePayloadBytes = 1 * 1024 * 1024;
    public const long DefaultMinimumFreeSpaceBytes = 1L * 1024 * 1024 * 1024;
    public const long DefaultMaxSessionBytes = 8L * 1024 * 1024 * 1024;

    /// <summary>Upper bound for header/footer JSON payloads used in layout math (seed).</summary>
    public const int MetadataPayloadSizeCapBytes = 8192;

    public RecorderConfig(
        long maxSegmentBytes = DefaultMaxSegmentBytes,
        TimeSpan? maxSegmentDuration = null,
        int maxRecordsPerSegment = DefaultMaxRecordsPerSegment,
        int queueCapacity = DefaultQueueCapacity,
        TimeSpan? flushInterval = null,
        int shutdownDrainTimeoutMilliseconds = DefaultShutdownDrainTimeoutMilliseconds,
        int maxFramePayloadBytes = DefaultMaxFramePayloadBytes,
        long minimumFreeSpaceBytes = DefaultMinimumFreeSpaceBytes,
        long maxSessionBytes = DefaultMaxSessionBytes)
    {
        if (maxRecordsPerSegment < 1) throw new ArgumentOutOfRangeException(nameof(maxRecordsPerSegment));
        if (queueCapacity < 1) throw new ArgumentOutOfRangeException(nameof(queueCapacity));
        if (shutdownDrainTimeoutMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(shutdownDrainTimeoutMilliseconds));
        if (maxFramePayloadBytes < 256) throw new ArgumentOutOfRangeException(nameof(maxFramePayloadBytes));
        if (minimumFreeSpaceBytes < 0) throw new ArgumentOutOfRangeException(nameof(minimumFreeSpaceBytes));

        var minimumSegment = ComputeMinimumSegmentBytes(maxFramePayloadBytes);
        if (maxSegmentBytes < minimumSegment)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxSegmentBytes),
                maxSegmentBytes,
                $"MaxSegmentBytes must be >= {minimumSegment} (container+header+one event+footer reservation).");
        }

        if (maxSessionBytes < maxSegmentBytes) throw new ArgumentOutOfRangeException(nameof(maxSessionBytes));

        MaxSegmentBytes = maxSegmentBytes;
        MaxSegmentDuration = maxSegmentDuration ?? DefaultMaxSegmentDuration;
        MaxRecordsPerSegment = maxRecordsPerSegment;
        QueueCapacity = queueCapacity;
        FlushInterval = flushInterval ?? DefaultFlushInterval;
        ShutdownDrainTimeoutMilliseconds = shutdownDrainTimeoutMilliseconds;
        MaxFramePayloadBytes = maxFramePayloadBytes;
        MinimumFreeSpaceBytes = minimumFreeSpaceBytes;
        MaxSessionBytes = maxSessionBytes;
        FooterReserveBytes = ContainerFormat.FrameTotalSize(MetadataPayloadSizeCapBytes);
    }

    /// <summary>
    /// Minimum bytes for a valid completed segment that can hold one raw event.
    /// Invalid configs must fail before recording begins.
    /// </summary>
    public static long ComputeMinimumSegmentBytes(int maxFramePayloadBytes)
    {
        var meta = ContainerFormat.FrameTotalSize(MetadataPayloadSizeCapBytes);
        var minEvent = ContainerFormat.FrameTotalSize(Math.Min(64, maxFramePayloadBytes));
        return ContainerFormat.ContainerHeaderSize + meta + minEvent + meta;
    }

    public long MaxSegmentBytes { get; }
    public TimeSpan MaxSegmentDuration { get; }
    public int MaxRecordsPerSegment { get; }
    public int QueueCapacity { get; }
    public TimeSpan FlushInterval { get; }
    public int ShutdownDrainTimeoutMilliseconds { get; }
    public int MaxFramePayloadBytes { get; }
    public long MinimumFreeSpaceBytes { get; }
    public long MaxSessionBytes { get; }

    /// <summary>Bytes reserved so a footer frame can always be written without exceeding MaxSegmentBytes.</summary>
    public int FooterReserveBytes { get; }
}
