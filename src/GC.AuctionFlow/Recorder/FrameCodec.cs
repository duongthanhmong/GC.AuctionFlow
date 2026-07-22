using System.Buffers.Binary;
using System.Text;

namespace GC.AuctionFlow.Recorder;

/// <summary>Versioned binary container prefix + CRC32C-protected frames.</summary>
public static class ContainerFormat
{
    /// <summary>ASCII "GCAR"</summary>
    public const uint ContainerMagic = 0x52414347;

    /// <summary>ASCII "GCF1"</summary>
    public const uint FrameMagic = 0x31464347;

    public const int ContainerHeaderSize = 8;
    public const int FrameHeaderSize = 16;
    public const int FrameCrcSize = 4;

    public static void WriteContainerHeader(Span<byte> dest, int containerVersion, ushort flags = 0)
    {
        if (dest.Length < ContainerHeaderSize) throw new ArgumentException("buffer too small");
        BinaryPrimitives.WriteUInt32LittleEndian(dest, ContainerMagic);
        BinaryPrimitives.WriteUInt16LittleEndian(dest.Slice(4), (ushort)containerVersion);
        BinaryPrimitives.WriteUInt16LittleEndian(dest.Slice(6), flags);
    }

    public static bool TryReadContainerHeader(ReadOnlySpan<byte> src, out int containerVersion, out ushort flags, out string? error)
    {
        containerVersion = 0;
        flags = 0;
        error = null;
        if (src.Length < ContainerHeaderSize)
        {
            error = "ShortContainerHeader";
            return false;
        }

        var magic = BinaryPrimitives.ReadUInt32LittleEndian(src);
        if (magic != ContainerMagic)
        {
            error = "InvalidContainerMagic";
            return false;
        }

        containerVersion = BinaryPrimitives.ReadUInt16LittleEndian(src.Slice(4));
        flags = BinaryPrimitives.ReadUInt16LittleEndian(src.Slice(6));
        if (containerVersion != RawEventRecorderVersions.RawEventContainerVersion)
        {
            error = "UnsupportedContainerVersion";
            return false;
        }

        return true;
    }

    public static int FrameTotalSize(int payloadLength) => FrameHeaderSize + payloadLength + FrameCrcSize;
}

public readonly struct DecodedFrame
{
    public DecodedFrame(RecorderFrameType frameType, ushort flags, byte[] payloadUtf8)
    {
        FrameType = frameType;
        Flags = flags;
        PayloadUtf8 = payloadUtf8;
    }

    public RecorderFrameType FrameType { get; }
    public ushort Flags { get; }
    public byte[] PayloadUtf8 { get; }
}

public enum FrameDecodeStatus
{
    Ok = 0,
    NeedMoreData = 1,
    InvalidFrameMagic = 2,
    InvalidFrameLength = 3,
    OversizedFrame = 4,
    FrameCrcMismatch = 5,
    UnsupportedFrameVersion = 6
}

public static class FrameCodec
{
    public static byte[] EncodeFrame(RecorderFrameType frameType, ReadOnlySpan<byte> utf8Payload, ushort flags = 0)
    {
        var total = ContainerFormat.FrameTotalSize(utf8Payload.Length);
        var buffer = new byte[total];
        WriteFrame(buffer, frameType, utf8Payload, flags);
        return buffer;
    }

    public static void WriteFrame(Span<byte> dest, RecorderFrameType frameType, ReadOnlySpan<byte> utf8Payload, ushort flags = 0)
    {
        var total = ContainerFormat.FrameTotalSize(utf8Payload.Length);
        if (dest.Length < total) throw new ArgumentException("buffer too small");

        BinaryPrimitives.WriteUInt32LittleEndian(dest, ContainerFormat.FrameMagic);
        BinaryPrimitives.WriteUInt16LittleEndian(dest.Slice(4), (ushort)RawEventRecorderVersions.FrameVersion);
        BinaryPrimitives.WriteUInt16LittleEndian(dest.Slice(6), (ushort)frameType);
        BinaryPrimitives.WriteUInt16LittleEndian(dest.Slice(8), flags);
        BinaryPrimitives.WriteUInt16LittleEndian(dest.Slice(10), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(dest.Slice(12), (uint)utf8Payload.Length);
        utf8Payload.CopyTo(dest.Slice(ContainerFormat.FrameHeaderSize));
        var covered = dest.Slice(0, ContainerFormat.FrameHeaderSize + utf8Payload.Length);
        var crc = Crc32C.Compute(covered);
        BinaryPrimitives.WriteUInt32LittleEndian(
            dest.Slice(ContainerFormat.FrameHeaderSize + utf8Payload.Length),
            crc);
    }

    public static FrameDecodeStatus TryDecodeFrame(
        ReadOnlySpan<byte> src,
        int maxPayloadBytes,
        out DecodedFrame frame,
        out int consumed,
        out string? detail)
    {
        frame = default;
        consumed = 0;
        detail = null;

        if (src.Length < ContainerFormat.FrameHeaderSize)
            return FrameDecodeStatus.NeedMoreData;

        var magic = BinaryPrimitives.ReadUInt32LittleEndian(src);
        if (magic != ContainerFormat.FrameMagic)
        {
            detail = "InvalidFrameMagic";
            return FrameDecodeStatus.InvalidFrameMagic;
        }

        var version = BinaryPrimitives.ReadUInt16LittleEndian(src.Slice(4));
        if (version != RawEventRecorderVersions.FrameVersion)
        {
            detail = "UnsupportedFrameVersion";
            return FrameDecodeStatus.UnsupportedFrameVersion;
        }

        var type = (RecorderFrameType)BinaryPrimitives.ReadUInt16LittleEndian(src.Slice(6));
        var flags = BinaryPrimitives.ReadUInt16LittleEndian(src.Slice(8));
        var payloadLength = BinaryPrimitives.ReadUInt32LittleEndian(src.Slice(12));

        if (payloadLength > (uint)maxPayloadBytes)
        {
            detail = "OversizedFrame";
            return FrameDecodeStatus.OversizedFrame;
        }

        var total = ContainerFormat.FrameTotalSize((int)payloadLength);
        if (total < ContainerFormat.FrameHeaderSize + ContainerFormat.FrameCrcSize)
        {
            detail = "InvalidFrameLength";
            return FrameDecodeStatus.InvalidFrameLength;
        }

        if (src.Length < total)
            return FrameDecodeStatus.NeedMoreData;

        var covered = src.Slice(0, ContainerFormat.FrameHeaderSize + (int)payloadLength);
        var expectedCrc = BinaryPrimitives.ReadUInt32LittleEndian(src.Slice(ContainerFormat.FrameHeaderSize + (int)payloadLength));
        var actualCrc = Crc32C.Compute(covered);
        if (expectedCrc != actualCrc)
        {
            detail = "FrameCrcMismatch";
            return FrameDecodeStatus.FrameCrcMismatch;
        }

        var payload = covered.Slice(ContainerFormat.FrameHeaderSize).ToArray();
        frame = new DecodedFrame(type, flags, payload);
        consumed = total;
        return FrameDecodeStatus.Ok;
    }

    public static string PayloadAsUtf8String(DecodedFrame frame) =>
        Encoding.UTF8.GetString(frame.PayloadUtf8);
}
