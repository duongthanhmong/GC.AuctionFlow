namespace Aos.LevelEngine.Spool;

/// <summary>IEEE CRC-32 — must match Phase 0 / Sidecar.</summary>
public static class Crc32Util
{
    private static readonly uint[] Table = CreateTable();

    private static uint[] CreateTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            var crc = i;
            for (var j = 0; j < 8; j++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
            table[i] = crc;
        }
        return table;
    }

    public static uint HashToUInt32(ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFFu;
        foreach (var b in data)
            crc = Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return crc ^ 0xFFFFFFFFu;
    }
}
