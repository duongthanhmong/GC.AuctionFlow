using System.Buffers.Binary;

namespace Aos.LevelEngine.Spool;

/// <summary>Minimal SchemaVersion-2 envelope writer matching Phase 0 FixedSize=94.</summary>
public static class SpoolEnvelopeV2
{
    public const int FixedSize = 94;
    public const ushort SchemaVersion = 2;

    public static byte[] BuildHeader(
        Guid eventId,
        long sequence,
        Guid sessionId,
        DateTime tsExchange,
        DateTime tsReceived,
        byte payloadType,
        int payloadLength,
        uint crc32,
        Guid processInstanceId,
        byte declaredMode,
        long monoTicks,
        byte marketValidity,
        byte timingValidity)
    {
        var buf = new byte[FixedSize];
        var o = 0;
        eventId.TryWriteBytes(buf.AsSpan(o, 16)); o += 16;
        BinaryPrimitives.WriteInt64LittleEndian(buf.AsSpan(o, 8), sequence); o += 8;
        sessionId.TryWriteBytes(buf.AsSpan(o, 16)); o += 16;
        BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(o, 2), SchemaVersion); o += 2;
        BinaryPrimitives.WriteInt64LittleEndian(buf.AsSpan(o, 8), tsExchange.Ticks); o += 8;
        BinaryPrimitives.WriteInt64LittleEndian(buf.AsSpan(o, 8), tsReceived.Ticks); o += 8;
        buf[o++] = payloadType;
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(o, 4), payloadLength); o += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(o, 4), crc32); o += 4;
        processInstanceId.TryWriteBytes(buf.AsSpan(o, 16)); o += 16;
        buf[o++] = declaredMode;
        BinaryPrimitives.WriteInt64LittleEndian(buf.AsSpan(o, 8), monoTicks); o += 8;
        buf[o++] = marketValidity;
        buf[o] = timingValidity;
        return buf;
    }

    public static uint Crc32(ReadOnlySpan<byte> payload) => Crc32Util.HashToUInt32(payload);
}

/// <summary>Append-only spool for Level Engine interpretation events.</summary>
public sealed class LevelEngineSpoolWriter : IDisposable
{
    private readonly string _dir;
    private readonly string _instrument;
    private readonly Guid _sessionId;
    private readonly Guid _processId;
    private readonly byte _mode;
    private FileStream? _fs;
    private BinaryWriter? _bw;
    private long _seq;
    private long _first = -1;
    private long _last = -1;
    private string? _path;
    private bool _disposed;

    public string? CurrentPath => _path;

    public LevelEngineSpoolWriter(string spoolRoot, string modePartition, string instrument,
        Guid sessionId, Guid processId, byte declaredModeByte)
    {
        _dir = Path.Combine(spoolRoot, modePartition);
        _instrument = instrument;
        _sessionId = sessionId;
        _processId = processId;
        _mode = declaredModeByte;
        Directory.CreateDirectory(_dir);
        Open();
    }

    public long Write(LevelEnginePayloadType type, DateTime tsExchange, ReadOnlySpan<byte> payload)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var seq = ++_seq;
        if (_first < 0) _first = seq;
        _last = seq;
        var crc = SpoolEnvelopeV2.Crc32(payload);
        var hdr = SpoolEnvelopeV2.BuildHeader(
            Guid.NewGuid(), seq, _sessionId, tsExchange, DateTime.UtcNow,
            (byte)type, payload.Length, crc, _processId, _mode,
            System.Diagnostics.Stopwatch.GetTimestamp(), 1, 1);
        _bw!.Write(hdr);
        _bw.Write(payload);
        _bw.Flush();
        _fs!.Flush(true);
        return seq;
    }

    private void Open()
    {
        var name = $"{DateTime.UtcNow:yyyyMMdd}_{_instrument}_open_{Guid.NewGuid():N}_v01.aosspool";
        _path = Path.Combine(_dir, name);
        _fs = new FileStream(_path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        _bw = new BinaryWriter(_fs);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _bw?.Flush();
        _fs?.Flush(true);
        _bw?.Dispose();
        _fs?.Dispose();
        if (_path is not null && _first >= 0 && File.Exists(_path))
        {
            var sealedName = $"{DateTime.UtcNow:yyyyMMdd}_{_instrument}_{_first}_{_last}_v01.aosspool";
            var dest = Path.Combine(_dir, sealedName);
            try { File.Move(_path, dest); _path = dest; }
            catch { /* leave open name if locked */ }
        }
    }
}
