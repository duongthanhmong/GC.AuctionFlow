using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GC.AuctionFlow.Recorder;

public interface IDiskSpaceProbe
{
    long GetAvailableBytes(string path);
}

public sealed class DriveInfoDiskSpaceProbe : IDiskSpaceProbe
{
    public long GetAvailableBytes(string path)
    {
        var full = Path.GetFullPath(path);
        var root = Path.GetPathRoot(full);
        if (string.IsNullOrEmpty(root))
            return long.MaxValue;
        var drive = new DriveInfo(root);
        return drive.AvailableFreeSpace;
    }
}

public sealed class FixedDiskSpaceProbe : IDiskSpaceProbe
{
    private long _available;

    public FixedDiskSpaceProbe(long availableBytes) => _available = availableBytes;

    public long GetAvailableBytes(string path) => Interlocked.Read(ref _available);

    public void SetAvailableBytes(long value) => Interlocked.Exchange(ref _available, value);
}

public sealed class CompletedSegmentInfo
{
    public CompletedSegmentInfo(
        Guid segmentId,
        int segmentOrdinal,
        string fileName,
        string fullPath,
        string sha256Hex,
        long recordCount,
        long byteLength,
        long firstWriterSequence,
        long lastWriterSequence,
        int contractEpoch)
    {
        SegmentId = segmentId;
        SegmentOrdinal = segmentOrdinal;
        FileName = fileName;
        FullPath = fullPath;
        Sha256Hex = sha256Hex;
        RecordCount = recordCount;
        ByteLength = byteLength;
        FirstWriterSequence = firstWriterSequence;
        LastWriterSequence = lastWriterSequence;
        ContractEpoch = contractEpoch;
    }

    public Guid SegmentId { get; }
    public int SegmentOrdinal { get; }
    public string FileName { get; }
    public string FullPath { get; }
    public string Sha256Hex { get; }
    public long RecordCount { get; }
    public long ByteLength { get; }
    public long FirstWriterSequence { get; }
    public long LastWriterSequence { get; }
    public int ContractEpoch { get; }
}

/// <summary>Writes a single segment: tmp → framed records → footer → rename → sha256 → done.</summary>
public sealed class SegmentWriter : IDisposable
{
    private readonly string _segmentsDir;
    private readonly RecorderConfig _config;
    private readonly RecorderCounters _counters;
    private FileStream? _stream;
    private Guid _segmentId;
    private int _segmentOrdinal;
    private int _contractEpoch;
    private long _recordCount;
    private long _firstWriterSequence = -1;
    private long _lastWriterSequence = -1;
    private long _bytesWritten;
    private long _startedStopwatch;
    private DateTime _startedUtc;
    private ObservedInstrumentIdentity? _instrument;
    private string _mode = "";
    private string _modeProv = "";
    private string _provider = "";
    private string _providerProv = "";
    private Guid _sessionId;
    private Guid _processId;
    private bool _open;
    private bool _disposed;

    public SegmentWriter(string segmentsDirectory, RecorderConfig config, RecorderCounters counters)
    {
        _segmentsDir = segmentsDirectory;
        _config = config;
        _counters = counters;
        Directory.CreateDirectory(_segmentsDir);
    }

    public bool IsOpen => _open;
    public Guid CurrentSegmentId => _segmentId;
    public int CurrentSegmentOrdinal => _segmentOrdinal;
    public long RecordCount => _recordCount;
    public long BytesWritten => _bytesWritten;
    public long StartedStopwatchTimestamp => _startedStopwatch;

    public void Open(
        Guid sessionId,
        Guid processId,
        Guid segmentId,
        int segmentOrdinal,
        int contractEpoch,
        ObservedInstrumentIdentity instrument,
        string declaredMode,
        string modeProvenance,
        string declaredProvider,
        string providerProvenance,
        long startedStopwatchTimestamp,
        DateTime startedUtc)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_open) throw new InvalidOperationException("Segment already open.");

        _sessionId = sessionId;
        _processId = processId;
        _segmentId = segmentId;
        _segmentOrdinal = segmentOrdinal;
        _contractEpoch = contractEpoch;
        _instrument = instrument;
        _mode = declaredMode;
        _modeProv = modeProvenance;
        _provider = declaredProvider;
        _providerProv = providerProvenance;
        _startedStopwatch = startedStopwatchTimestamp;
        _startedUtc = startedUtc;
        _recordCount = 0;
        _firstWriterSequence = -1;
        _lastWriterSequence = -1;
        _bytesWritten = 0;

        var tmpPath = Path.Combine(_segmentsDir, RecorderStoragePaths.SegmentTempFileName(segmentId));
        _stream = new FileStream(tmpPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        var headerBuf = new byte[ContainerFormat.ContainerHeaderSize];
        ContainerFormat.WriteContainerHeader(headerBuf, RawEventRecorderVersions.RawEventContainerVersion);
        _stream.Write(headerBuf);
        _bytesWritten += headerBuf.Length;

        var header = new SegmentHeaderRecord(
            RawEventRecorderVersions.RawEventRecorderSchemaVersion,
            RawEventRecorderVersions.RawEventContainerVersion,
            sessionId,
            processId,
            segmentId,
            segmentOrdinal,
            contractEpoch,
            startedUtc,
            startedStopwatchTimestamp,
            instrument,
            declaredMode,
            modeProvenance,
            declaredProvider,
            providerProvenance);
        WriteFrame(RecorderFrameType.SegmentHeader, RecorderJson.SerializeHeader(header));
        _open = true;
    }

    /// <summary>
    /// Prospective size check before writing the next RawEvent.
    /// Reserves footer frame space so completion cannot exceed MaxSegmentBytes.
    /// Duration uses Stopwatch elapsed time (not wall-clock UTC).
    /// </summary>
    public bool WouldExceedLimits(int nextRawEventFrameTotalBytes, int footerReserveBytes, long nowStopwatchTimestamp)
    {
        if (!_open) return false;
        if (_recordCount >= _config.MaxRecordsPerSegment) return true;
        if (_bytesWritten + nextRawEventFrameTotalBytes + footerReserveBytes > _config.MaxSegmentBytes)
            return true;
        var elapsedTicks = nowStopwatchTimestamp - _startedStopwatch;
        if (elapsedTicks < 0) elapsedTicks = 0;
        var elapsed = TimeSpan.FromSeconds(elapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency);
        return elapsed >= _config.MaxSegmentDuration;
    }

    /// <summary>
    /// Writes one RawEvent frame. RecordsWritten counts RawEvent frames only (not header/footer).
    /// Returns false on serialization failure (counted) or IO failure (caller must count discard).
    /// </summary>
    public bool TryWriteRawEvent(RawEventEnvelope envelope, out bool serializationFailed)
    {
        serializationFailed = false;
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_open || _stream is null) throw new InvalidOperationException("Segment not open.");

        byte[] payload;
        try
        {
            payload = RecorderJson.SerializeEnvelope(envelope);
        }
        catch
        {
            serializationFailed = true;
            Interlocked.Increment(ref _counters.SerializationFailures);
            return false;
        }

        if (payload.Length > _config.MaxFramePayloadBytes)
        {
            serializationFailed = true;
            Interlocked.Increment(ref _counters.SerializationFailures);
            return false;
        }

        try
        {
            WriteFrame(RecorderFrameType.RawEvent, payload);
        }
        catch
        {
            return false;
        }

        _recordCount++;
        if (_firstWriterSequence < 0) _firstWriterSequence = envelope.RecorderGlobalLocalSequence;
        _lastWriterSequence = envelope.RecorderGlobalLocalSequence;
        Interlocked.Increment(ref _counters.RecordsWritten);
        return true;
    }

    public void WriteRawEvent(RawEventEnvelope envelope)
    {
        if (!TryWriteRawEvent(envelope, out var serFail))
            throw new InvalidOperationException(serFail ? "SerializationFailure" : "SegmentWriteFailure");
    }

    public CompletedSegmentInfo Complete(long endedStopwatchTimestamp, DateTime endedUtc)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_open || _stream is null) throw new InvalidOperationException("Segment not open.");

        var bytesBeforeFooter = _bytesWritten;
        var footer = new SegmentFooterRecord(
            _segmentId,
            _segmentOrdinal,
            endedUtc,
            endedStopwatchTimestamp,
            rawEventRecordCount: _recordCount,
            firstWriterSequence: _firstWriterSequence < 0 ? 0 : _firstWriterSequence,
            lastWriterSequence: _lastWriterSequence < 0 ? 0 : _lastWriterSequence,
            bytesBeforeFooter: bytesBeforeFooter,
            completedNormally: true);
        WriteFrame(RecorderFrameType.SegmentFooter, RecorderJson.SerializeFooter(footer));

        try
        {
            // Durable flush before close/rename (flushToDisk: true).
            _stream.Flush(flushToDisk: true);
        }
        catch
        {
            Interlocked.Increment(ref _counters.FlushFailures);
            throw;
        }

        _stream.Dispose();
        _stream = null;
        _open = false;

        var tmpPath = Path.Combine(_segmentsDir, RecorderStoragePaths.SegmentTempFileName(_segmentId));
        var finalName = RecorderStoragePaths.SegmentFileName(_segmentId);
        var finalPath = Path.Combine(_segmentsDir, finalName);
        File.Move(tmpPath, finalPath);

        string sha;
        try
        {
            sha = ComputeSha256Hex(finalPath);
            WriteSha256Atomic(_segmentsDir, _segmentId, sha);
        }
        catch
        {
            Interlocked.Increment(ref _counters.HashFailures);
            Interlocked.Increment(ref _counters.SegmentsIncomplete);
            throw;
        }

        var info = new FileInfo(finalPath);
        // BytesWritten counter / CompletedSegmentInfo.ByteLength = final .seg file length.
        Interlocked.Increment(ref _counters.SegmentsCompleted);
        Interlocked.Add(ref _counters.BytesWritten, info.Length);

        return new CompletedSegmentInfo(
            _segmentId,
            _segmentOrdinal,
            finalName,
            finalPath,
            sha,
            _recordCount,
            info.Length,
            _firstWriterSequence < 0 ? 0 : _firstWriterSequence,
            _lastWriterSequence < 0 ? 0 : _lastWriterSequence,
            _contractEpoch);
    }

    public void AbandonIncomplete()
    {
        if (_stream is not null)
        {
            try { _stream.Dispose(); } catch { /* preserve tmp */ }
            _stream = null;
        }

        if (_open)
        {
            Interlocked.Increment(ref _counters.SegmentsIncomplete);
            _open = false;
        }
    }

    private void WriteFrame(RecorderFrameType type, byte[] utf8Payload)
    {
        if (_stream is null) throw new InvalidOperationException("No stream.");
        var frame = FrameCodec.EncodeFrame(type, utf8Payload);
        _stream.Write(frame);
        _bytesWritten += frame.Length;
    }

    public static string ComputeSha256Hex(string path)
    {
        using var fs = File.OpenRead(path);
        var hash = SHA256.HashData(fs);
        return Convert.ToHexString(hash);
    }

    public static void WriteSha256Atomic(string segmentsDir, Guid segmentId, string sha256Hex)
    {
        var tmp = Path.Combine(segmentsDir, RecorderStoragePaths.SegmentHashTempFileName(segmentId));
        var final = Path.Combine(segmentsDir, RecorderStoragePaths.SegmentHashFileName(segmentId));
        // Deterministic companion encoding: uppercase hex (Convert.ToHexString), two spaces, file name, LF only.
        var content = sha256Hex + "  " + RecorderStoragePaths.SegmentFileName(segmentId) + "\n";
        File.WriteAllText(tmp, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        using (var fs = new FileStream(tmp, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            fs.Flush(flushToDisk: true);
        File.Move(tmp, final, overwrite: true);
    }

    public static bool TryReadSha256File(string hashPath, out string sha256Hex)
    {
        sha256Hex = "";
        if (!File.Exists(hashPath)) return false;
        var text = File.ReadAllText(hashPath).Trim();
        if (text.Length < 64) return false;
        sha256Hex = text[..64];
        return sha256Hex.Length == 64
               && sha256Hex.All(c => Uri.IsHexDigit(c));
    }

    public void Dispose()
    {
        if (_disposed) return;
        AbandonIncomplete();
        _disposed = true;
    }
}
