namespace GC.AuctionFlow.Recorder;

/// <summary>Castagnoli CRC-32C (mandatory frame protection).</summary>
public static class Crc32C
{
    private static readonly uint[] Table = CreateTable();

    private static uint[] CreateTable()
    {
        // Reflected Castagnoli polynomial
        const uint poly = 0x82F63B78u;
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            var crc = i;
            for (var j = 0; j < 8; j++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ poly : crc >> 1;
            table[i] = crc;
        }

        return table;
    }

    public static uint Compute(ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFFu;
        foreach (var b in data)
            crc = Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return crc ^ 0xFFFFFFFFu;
    }
}
