using System.Text;
using GC.AuctionFlow.Recorder;
using Xunit;
using Xunit.Abstractions;

namespace GC.AuctionFlow.Tests.Unit.Recorder;

/// <summary>
/// Reading a recording back.
///
/// The frame codec already had tests, but every one of them decoded a buffer it had just
/// encoded in memory. Nothing had opened a real session file, so what the recorder put on
/// disk was an assumption held up by SHA-256 — and a hash proves a file is undamaged, not
/// that its contents mean anything. A recorder writing garbage steadily would have passed
/// every integrity check this project had.
/// </summary>
public sealed class SegmentReaderTests
{
    private readonly ITestOutputHelper _out;

    public SegmentReaderTests(ITestOutputHelper output) => _out = output;

    private static byte[] BuildSegment(params (RecorderFrameType Type, string Json)[] frames)
    {
        var buffer = new List<byte>();
        var header = new byte[ContainerFormat.ContainerHeaderSize];
        ContainerFormat.WriteContainerHeader(header, RawEventRecorderVersions.RawEventContainerVersion);
        buffer.AddRange(header);

        foreach (var (type, json) in frames)
            buffer.AddRange(FrameCodec.EncodeFrame(type, Encoding.UTF8.GetBytes(json)));

        return buffer.ToArray();
    }

    // ========== A: the reader itself ==========

    [Fact]
    public void A01_A_written_segment_reads_back_with_its_frames_intact()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".seg");
        File.WriteAllBytes(path, BuildSegment(
            (RecorderFrameType.SegmentHeader, """{"kind":"header"}"""),
            (RecorderFrameType.RawEvent, """{"payloadDiscriminator":"NewTrade","streamKind":"Trade"}"""),
            (RecorderFrameType.RawEvent, """{"payloadDiscriminator":"Depth","streamKind":"Dom"}"""),
            (RecorderFrameType.RawEvent, """{"payloadDiscriminator":"Depth","streamKind":"Dom"}"""),
            (RecorderFrameType.SegmentFooter, """{"kind":"footer"}""")));

        try
        {
            var result = SegmentReader.Read(path);

            Assert.Null(result.Error);
            Assert.Equal(5, result.TotalFrames);
            Assert.Equal(3, result.FramesByType[RecorderFrameType.RawEvent]);
            Assert.Equal(2, result.RawEventsByPayloadKind["Depth"]);
            Assert.Equal(1, result.RawEventsByPayloadKind["NewTrade"]);
            Assert.Equal(2, result.RawEventsByStream["Dom"]);

            // A sealed segment must end on a frame boundary.
            Assert.Equal(0, result.BytesTrailing);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// A partially written tail is where a session interrupted by process exit keeps its
    /// last frame. The reader must return everything before it rather than refusing the
    /// file, or an unclean shutdown would discard real events.
    /// </summary>
    [Fact]
    public void A02_A_truncated_tail_does_not_lose_the_frames_before_it()
    {
        var whole = BuildSegment(
            (RecorderFrameType.RawEvent, """{"payloadDiscriminator":"NewTrade","streamKind":"Trade"}"""),
            (RecorderFrameType.RawEvent, """{"payloadDiscriminator":"NewTrade","streamKind":"Trade"}"""));

        var truncated = whole.Take(whole.Length - 6).ToArray();
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".seg.tmp");
        File.WriteAllBytes(path, truncated);

        try
        {
            var result = SegmentReader.Read(path);

            Assert.Equal(1, result.FramesByType[RecorderFrameType.RawEvent]);
            Assert.Equal(FrameDecodeStatus.NeedMoreData, result.StoppedBecause);
            Assert.True(result.BytesTrailing > 0);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void A03_A_corrupted_frame_stops_the_read_rather_than_being_reported_as_data()
    {
        var bytes = BuildSegment(
            (RecorderFrameType.RawEvent, """{"payloadDiscriminator":"NewTrade","streamKind":"Trade"}"""));

        // Flip a payload byte; the CRC must catch it.
        bytes[^3] ^= 0xFF;

        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".seg");
        File.WriteAllBytes(path, bytes);

        try
        {
            var result = SegmentReader.Read(path);
            Assert.Equal(FrameDecodeStatus.FrameCrcMismatch, result.StoppedBecause);
            Assert.False(result.FramesByType.ContainsKey(RecorderFrameType.RawEvent));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void A04_A_file_that_is_not_a_segment_is_refused_clearly()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".seg");
        File.WriteAllText(path, "this is not a recording");

        try
        {
            var result = SegmentReader.Read(path);
            Assert.Equal(0, result.TotalFrames);
            Assert.NotEqual(FrameDecodeStatus.Ok, result.StoppedBecause);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // ========== B: the real spool, when this machine has one ==========

    /// <summary>
    /// Reads whatever the operator actually recorded.
    ///
    /// Skipped rather than failed when no spool exists, because the recording is local
    /// evidence and not every machine running these tests will have one. When it does
    /// exist this is the only check in the suite that looks at bytes the recorder wrote
    /// during a live session rather than bytes a test wrote a moment earlier.
    /// </summary>
    [Fact]
    public void B01_The_recorded_spool_reads_back_and_reports_what_is_in_it()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".gcae", "recorder", "sessions");

        if (!Directory.Exists(root))
        {
            _out.WriteLine("no recorder spool on this machine — nothing to verify");
            return;
        }

        var sessions = new DirectoryInfo(root).GetDirectories()
            .OrderByDescending(d => d.CreationTimeUtc)
            .Take(3)
            .ToArray();

        if (sessions.Length == 0)
        {
            _out.WriteLine("spool exists but holds no sessions");
            return;
        }

        foreach (var session in sessions)
        {
            var results = SegmentReader.ReadSession(session.FullName);
            if (results.Count == 0)
                continue;

            // A session ATAS is still writing has no sealed segment yet, and its live one
            // may legitimately hold no complete frame. That is the recorder mid-flight, not
            // a writer that produced nothing — asserting on it fails whenever this suite
            // runs beside a running platform. The invariant kept here is narrower and true:
            // a *sealed* segment must decode.
            if (!results.Any(r => r.Path.EndsWith(".seg", StringComparison.Ordinal)))
            {
                _out.WriteLine(session.Name[..8] + "  in flight, no sealed segment — skipped");
                continue;
            }

            var frames = results.Sum(r => r.TotalFrames);
            var kinds = results
                .SelectMany(r => r.RawEventsByPayloadKind)
                .GroupBy(p => p.Key, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Sum(p => p.Value), StringComparer.Ordinal);

            _out.WriteLine(session.Name[..8] + "  segments=" + results.Count + "  frames=" + frames);
            foreach (var kind in kinds.OrderByDescending(k => k.Value))
                _out.WriteLine("    " + kind.Key + " = " + kind.Value);

            foreach (var sample in results.SelectMany(r => r.SampleEvents).Take(2))
                _out.WriteLine("    sample: " + sample[..Math.Min(sample.Length, 400)]);

            // A sealed segment must end exactly on a frame boundary. Trailing bytes there
            // would mean the writer left something unreadable behind.
            foreach (var sealedSegment in results.Where(r => r.Path.EndsWith(".seg", StringComparison.Ordinal)))
            {
                Assert.Null(sealedSegment.Error);
                Assert.Equal(0, sealedSegment.BytesTrailing);
                Assert.Equal(FrameDecodeStatus.Ok, sealedSegment.StoppedBecause);
            }

            Assert.True(frames > 0, session.Name + " decoded to zero frames");
        }
    }
}
