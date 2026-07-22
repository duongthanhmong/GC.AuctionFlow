using System.Diagnostics;

namespace GC.AuctionFlow.Recorder;

/// <summary>
/// Immutable primitive-only callback entry context. Captured once per callback invocation.
/// CallbackInvocationSequence is process/session-local per CallbackSource — not exchange sequence.
/// First value is 1. Overflow does not wrap: Next throws when long.MaxValue is reached.
/// </summary>
public sealed class CallbackCaptureContext
{
    public CallbackCaptureContext(
        RecorderCallbackSource callbackSource,
        long callbackInvocationSequence,
        DateTime callbackReceiveUtc,
        long callbackReceiveStopwatchTimestamp,
        int callbackManagedThreadId)
    {
        CallbackSource = callbackSource;
        CallbackInvocationSequence = callbackInvocationSequence;
        CallbackReceiveUtc = callbackReceiveUtc;
        CallbackReceiveStopwatchTimestamp = callbackReceiveStopwatchTimestamp;
        CallbackManagedThreadId = callbackManagedThreadId;
    }

    public RecorderCallbackSource CallbackSource { get; }
    public long CallbackInvocationSequence { get; }
    public DateTime CallbackReceiveUtc { get; }
    public long CallbackReceiveStopwatchTimestamp { get; }
    public int CallbackManagedThreadId { get; }

    /// <summary>Capture receive stamps once and assign next invocation sequence for the source.</summary>
    public static CallbackCaptureContext CaptureNow(
        RecorderCallbackSource source,
        CallbackInvocationSequenceProvider sequences) =>
        new(
            source,
            sequences.Next(source),
            DateTime.UtcNow,
            Stopwatch.GetTimestamp(),
            Environment.CurrentManagedThreadId);
}

/// <summary>
/// Thread-safe per-CallbackSource invocation counters.
/// Fixed array indexed by closed enum values — not a persisted dictionary.
/// First Next() returns 1. At long.MaxValue, Next throws (no silent wrap to negative/reuse).
/// </summary>
public sealed class CallbackInvocationSequenceProvider
{
    private readonly long[] _counters;

    public CallbackInvocationSequenceProvider()
    {
        var max = 0;
        foreach (RecorderCallbackSource v in Enum.GetValues(typeof(RecorderCallbackSource)))
        {
            var i = (int)v;
            if (i > max) max = i;
        }

        _counters = new long[max + 1];
    }

    /// <summary>Next monotonic sequence for the source. Starts at 1. Throws if exhausted.</summary>
    public long Next(RecorderCallbackSource source)
    {
        var index = (int)source;
        if ((uint)index >= (uint)_counters.Length)
            throw new ArgumentOutOfRangeException(nameof(source));

        while (true)
        {
            var current = Interlocked.Read(ref _counters[index]);
            if (current == long.MaxValue)
            {
                throw new InvalidOperationException(
                    "CallbackInvocationSequenceExhausted: per-CallbackSource counter reached long.MaxValue; no wrap.");
            }

            var next = current + 1;
            if (Interlocked.CompareExchange(ref _counters[index], next, current) == current)
                return next;
        }
    }

    public long Current(RecorderCallbackSource source)
    {
        var index = (int)source;
        if ((uint)index >= (uint)_counters.Length)
            throw new ArgumentOutOfRangeException(nameof(source));
        return Interlocked.Read(ref _counters[index]);
    }

    /// <summary>Test helper: force counter to a value (e.g. long.MaxValue) for overflow tests.</summary>
    public void ForceCurrentForTests(RecorderCallbackSource source, long value)
    {
        var index = (int)source;
        if ((uint)index >= (uint)_counters.Length)
            throw new ArgumentOutOfRangeException(nameof(source));
        Interlocked.Exchange(ref _counters[index], value);
    }
}
