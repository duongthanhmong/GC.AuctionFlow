using System.IO;
using System.Text;
using System.Text.Json;

namespace GC.AuctionFlow.Recorder;

/// <summary>
/// What one segment file contained.
/// </summary>
public sealed class SegmentReadResult
{
    public SegmentReadResult(
        string path,
        int containerVersion,
        IReadOnlyDictionary<RecorderFrameType, int> framesByType,
        IReadOnlyDictionary<string, int> rawEventsByPayloadKind,
        IReadOnlyDictionary<string, int> rawEventsByStream,
        long bytesConsumed,
        long bytesTrailing,
        FrameDecodeStatus stoppedBecause,
        IReadOnlyList<string> sampleEvents,
        string? error)
    {
        Path = path;
        ContainerVersion = containerVersion;
        FramesByType = framesByType;
        RawEventsByPayloadKind = rawEventsByPayloadKind;
        RawEventsByStream = rawEventsByStream;
        BytesConsumed = bytesConsumed;
        BytesTrailing = bytesTrailing;
        StoppedBecause = stoppedBecause;
        SampleEvents = sampleEvents;
        Error = error;
    }

    public string Path { get; }
    public int ContainerVersion { get; }
    public IReadOnlyDictionary<RecorderFrameType, int> FramesByType { get; }

    /// <summary>Raw events grouped by their payload discriminator, as written.</summary>
    public IReadOnlyDictionary<string, int> RawEventsByPayloadKind { get; }

    public IReadOnlyDictionary<string, int> RawEventsByStream { get; }

    public long BytesConsumed { get; }

    /// <summary>
    /// Bytes after the last whole frame. Non-zero on a segment that was still being
    /// written, which is normal for a `.tmp`; non-zero on a sealed segment is not.
    /// </summary>
    public long BytesTrailing { get; }

    public FrameDecodeStatus StoppedBecause { get; }

    /// <summary>A few decoded payloads, verbatim, so a human can see what is in there.</summary>
    public IReadOnlyList<string> SampleEvents { get; }

    public string? Error { get; }

    public int TotalFrames => FramesByType.Values.Sum();
}

/// <summary>
/// Reads a recorded `.seg` file back.
///
/// The frame codec was already proven — `FrameCodec.TryDecodeFrame` has tests — but every
/// one of those tests decodes a buffer it had just encoded in memory. Nothing had ever
/// opened a real session file and read it through, so what the recorder actually wrote to
/// disk was an assumption. A SHA-256 proves a file is undamaged; it says nothing about
/// whether the contents mean anything, and a recorder writing garbage steadily would pass
/// every integrity check the project has.
///
/// This closes that gap, and it is the first half of what v1.2 §46.3 promises: enough raw
/// features on disk to re-run rule versions without re-collecting data. Until something
/// could read them back, that promise was unkept.
/// </summary>
public static class SegmentReader
{
    /// <summary>Decoded payloads kept as samples. A window for a human, not a dataset.</summary>
    public const int SampleLimit = 5;

    public static SegmentReadResult Read(string path, int maxFramePayloadBytes = 1 << 20)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var byType = new Dictionary<RecorderFrameType, int>();
        var byKind = new Dictionary<string, int>(StringComparer.Ordinal);
        var byStream = new Dictionary<string, int>(StringComparer.Ordinal);
        var samples = new List<string>();

        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(path);
        }
        catch (Exception ex)
        {
            return new SegmentReadResult(
                path, 0, byType, byKind, byStream, 0, 0,
                FrameDecodeStatus.NeedMoreData, samples, ex.GetType().Name + ": " + ex.Message);
        }

        if (!ContainerFormat.TryReadContainerHeader(bytes, out var containerVersion, out _, out var headerError))
        {
            return new SegmentReadResult(
                path, 0, byType, byKind, byStream, 0, bytes.LongLength,
                FrameDecodeStatus.InvalidFrameMagic, samples,
                headerError ?? "container header unreadable");
        }

        var offset = ContainerFormat.ContainerHeaderSize;
        var status = FrameDecodeStatus.Ok;

        while (offset < bytes.Length)
        {
            status = FrameCodec.TryDecodeFrame(
                bytes.AsSpan(offset), maxFramePayloadBytes, out var frame, out var consumed, out _);

            if (status != FrameDecodeStatus.Ok)
                break;

            offset += consumed;
            byType[frame.FrameType] = byType.TryGetValue(frame.FrameType, out var n) ? n + 1 : 1;

            if (frame.FrameType != RecorderFrameType.RawEvent)
                continue;

            var json = Encoding.UTF8.GetString(frame.PayloadUtf8);

            // Grouped by reading the discriminator out of the payload rather than by
            // trusting the manifest: the point of reading a file back is to find out what
            // it says about itself, not to confirm what we already believed.
            var kind = TryReadString(json, "payloadDiscriminator") ?? "Unknown";
            var stream = TryReadString(json, "streamKind") ?? "Unknown";
            byKind[kind] = byKind.TryGetValue(kind, out var k) ? k + 1 : 1;
            byStream[stream] = byStream.TryGetValue(stream, out var s) ? s + 1 : 1;

            if (samples.Count < SampleLimit)
                samples.Add(json);
        }

        return new SegmentReadResult(
            path, containerVersion, byType, byKind, byStream,
            bytesConsumed: offset,
            bytesTrailing: bytes.LongLength - offset,
            stoppedBecause: status,
            samples,
            error: null);
    }

    /// <summary>
    /// Reads every sealed segment of a session, plus the unsealed tail if present.
    ///
    /// A `.tmp` is read too, and its trailing bytes are expected — that is where a session
    /// interrupted by process exit keeps its last partial frame. Skipping it would quietly
    /// discard real events.
    /// </summary>
    public static IReadOnlyList<SegmentReadResult> ReadSession(
        string sessionDirectory, int maxFramePayloadBytes = 1 << 20)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionDirectory);

        var segments = Path.Combine(sessionDirectory, RecorderStoragePaths.SegmentsDirectoryName);
        if (!Directory.Exists(segments))
            return Array.Empty<SegmentReadResult>();

        return Directory.GetFiles(segments, "*.seg")
            .Concat(Directory.GetFiles(segments, "*.seg.tmp"))
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(p => Read(p, maxFramePayloadBytes))
            .ToArray();
    }

    private static string? TryReadString(string json, string property)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty(property, out var value))
                return null;

            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.ToString(),
                _ => null,
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
