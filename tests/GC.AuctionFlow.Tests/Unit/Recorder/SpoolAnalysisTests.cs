using System.Text.Json;
using GC.AuctionFlow.Recorder;
using Xunit;
using Xunit.Abstractions;

namespace GC.AuctionFlow.Tests.Unit.Recorder;

/// <summary>
/// Reports what a recorded session actually contains.
///
/// Not an assertion suite — a lens. Counts alone said the recorder works; this answers the
/// questions that decide what can be done with the recording: over what window, at what
/// rate, and whether trades and quotes are interleaved finely enough that aggressor side
/// could be recovered offline. That last one was deferred at runtime because depth
/// callbacks carry no native sequence, and deferring it was only defensible if the data
/// would later let somebody measure it instead of guessing.
///
/// Skips when no spool exists.
/// </summary>
public sealed class SpoolAnalysisTests
{
    private readonly ITestOutputHelper _out;

    public SpoolAnalysisTests(ITestOutputHelper output) => _out = output;

    private sealed record Event(string Kind, string Stream, DateTime WriterUtc, long Sequence, string Json);

    [Fact]
    public void A01_Report_the_newest_recorded_session()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".gcae", "recorder", "sessions");

        if (!Directory.Exists(root))
        {
            _out.WriteLine("no spool");
            return;
        }

        var session = new DirectoryInfo(root).GetDirectories()
            .OrderByDescending(d => d.CreationTimeUtc)
            .FirstOrDefault();
        if (session is null) { _out.WriteLine("no sessions"); return; }

        // Re-read with samples raised so ordering can be examined rather than sniffed.
        var events = new List<Event>();
        var segDir = Path.Combine(session.FullName, RecorderStoragePaths.SegmentsDirectoryName);
        foreach (var file in Directory.GetFiles(segDir, "*.seg")
                     .Concat(Directory.GetFiles(segDir, "*.seg.tmp"))
                     .OrderBy(p => p, StringComparer.Ordinal))
        {
            foreach (var json in DecodeRawEvents(file))
            {
                var kind = Read(json, "payloadDiscriminator") ?? "Unknown";
                var stream = Read(json, "streamKind") ?? "Unknown";
                var utc = ReadUtc(json, "writerDequeuedUtc");
                var seq = ReadLong(json, "recorderGlobalLocalSequence");
                if (utc is not null)
                    events.Add(new Event(kind, stream, utc.Value, seq, json));
            }
        }

        if (events.Count == 0) { _out.WriteLine("session decoded to no events"); return; }

        var ordered = events.OrderBy(e => e.Sequence).ToArray();
        var first = ordered[0].WriterUtc;
        var last = ordered[^1].WriterUtc;
        var span = last - first;

        _out.WriteLine("session : " + session.Name[..8]);
        _out.WriteLine("window  : " + first.ToString("HH:mm:ss") + " -> " + last.ToString("HH:mm:ss")
                       + " UTC  (" + span.TotalMinutes.ToString("F1") + " min)");
        _out.WriteLine("events  : " + ordered.Length);

        foreach (var g in ordered.GroupBy(e => e.Kind).OrderByDescending(g => g.Count()))
            _out.WriteLine("   " + g.Key.PadRight(26) + g.Count().ToString().PadLeft(8)
                           + "   " + (g.Count() / Math.Max(span.TotalMinutes, 1)).ToString("F0") + "/min");

        // The alignment question: between one trade and the next, how many quote updates
        // land? If the answer is routinely zero, no quote was in force at the trade and
        // side cannot be recovered. If it is one or more, the raw material is there.
        var trades = ordered.Where(e => e.Kind is "NewTrade" or "CumulativeTradeNew").ToArray();
        var quotes = ordered.Where(e => e.Kind == "BestBidAsk").ToArray();

        _out.WriteLine("");
        _out.WriteLine("trades  : " + trades.Length + "   quotes: " + quotes.Length);

        if (trades.Length > 1 && quotes.Length > 0)
        {
            var quoteSeqs = quotes.Select(q => q.Sequence).ToArray();
            var withPrecedingQuote = 0;
            foreach (var t in trades)
            {
                var idx = Array.BinarySearch(quoteSeqs, t.Sequence);
                if (idx < 0) idx = ~idx;
                if (idx > 0) withPrecedingQuote++;
            }

            var pct = 100.0 * withPrecedingQuote / trades.Length;
            _out.WriteLine("trades preceded by a quote in the same session: "
                           + withPrecedingQuote + " / " + trades.Length + "  (" + pct.ToString("F1") + "%)");
        }

        // Direction, the field the aggressor pipeline does not read.
        var dirs = new Dictionary<string, int>(StringComparer.Ordinal);
        var sided = 0;
        foreach (var t in ordered.Where(e => e.Kind is "NewTrade" or "CumulativeTradeNew"))
        {
            var d = ReadNested(t.Json, "payload", "directionName") ?? "(absent)";
            dirs[d] = dirs.TryGetValue(d, out var c) ? c + 1 : 1;
            var isAsk = ReadNestedBool(t.Json, "payload", "isAsk");
            var isBid = ReadNestedBool(t.Json, "payload", "isBid");
            if (isAsk || isBid) sided++;
        }

        _out.WriteLine("");
        _out.WriteLine("directionName across trades:");
        foreach (var d in dirs.OrderByDescending(d => d.Value))
            _out.WriteLine("   " + d.Key.PadRight(14) + d.Value.ToString().PadLeft(8));
        _out.WriteLine("trades where isAsk||isBid is true: " + sided);

        _out.WriteLine("");
        foreach (var kind in new[] { "NewTrade", "BestBidAsk" })
        {
            var sample = ordered.FirstOrDefault(e => e.Kind == kind);
            if (sample is not null)
                _out.WriteLine(kind + " payload:\n" + sample.Json);
        }
    }

    private static IEnumerable<string> DecodeRawEvents(string path)
    {
        var result = SegmentReader.Read(path);
        if (result.Error is not null) yield break;

        // SegmentReader keeps only a sample; re-walk the file for the full set.
        var bytes = File.ReadAllBytes(path);
        if (!ContainerFormat.TryReadContainerHeader(bytes, out _, out _, out _)) yield break;

        var offset = ContainerFormat.ContainerHeaderSize;
        while (offset < bytes.Length)
        {
            var status = FrameCodec.TryDecodeFrame(
                bytes.AsSpan(offset), 1 << 20, out var frame, out var consumed, out _);
            if (status != FrameDecodeStatus.Ok) yield break;

            offset += consumed;
            if (frame.FrameType == RecorderFrameType.RawEvent)
                yield return System.Text.Encoding.UTF8.GetString(frame.PayloadUtf8);
        }
    }

    private static string? Read(string json, string name)
    {
        try
        {
            using var d = JsonDocument.Parse(json);
            return d.RootElement.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
                ? v.GetString() : null;
        }
        catch (JsonException) { return null; }
    }

    private static DateTime? ReadUtc(string json, string name)
    {
        var raw = Read(json, name);
        return DateTime.TryParse(raw, null,
            System.Globalization.DateTimeStyles.AdjustToUniversal
            | System.Globalization.DateTimeStyles.AssumeUniversal, out var v) ? v : null;
    }

    private static string? ReadNested(string json, string parent, string name)
    {
        try
        {
            using var d = JsonDocument.Parse(json);
            return d.RootElement.TryGetProperty(parent, out var p)
                   && p.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
                ? v.GetString() : null;
        }
        catch (JsonException) { return null; }
    }

    private static bool ReadNestedBool(string json, string parent, string name)
    {
        try
        {
            using var d = JsonDocument.Parse(json);
            return d.RootElement.TryGetProperty(parent, out var p)
                   && p.TryGetProperty(name, out var v)
                   && v.ValueKind == JsonValueKind.True;
        }
        catch (JsonException) { return false; }
    }

    private static long ReadLong(string json, string name)
    {
        try
        {
            using var d = JsonDocument.Parse(json);
            return d.RootElement.TryGetProperty(name, out var v) && v.TryGetInt64(out var l) ? l : 0;
        }
        catch (JsonException) { return 0; }
    }
}
