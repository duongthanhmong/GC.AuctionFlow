using System.Globalization;
using System.Text;

namespace GC.AuctionFlow.Probe;

/// <summary>
/// Diagnostic fingerprint only: sourceTimeTicks | price | volume | rawDataType | isBid | isAsk
/// Not a price-level ID, exchange sequence, or unique event ID. Never used for deletion/merge.
/// </summary>
public static class DepthFingerprints
{
    public static string Core(
        long sourceTimeTicks,
        decimal price,
        decimal volume,
        string rawDataType,
        bool isBid,
        bool isAsk)
    {
        var sb = new StringBuilder(96);
        sb.Append(sourceTimeTicks.ToString(CultureInfo.InvariantCulture));
        sb.Append('|');
        sb.Append(price.ToString(CultureInfo.InvariantCulture));
        sb.Append('|');
        sb.Append(volume.ToString(CultureInfo.InvariantCulture));
        sb.Append('|');
        sb.Append(rawDataType);
        sb.Append('|');
        sb.Append(isBid ? "1" : "0");
        sb.Append('|');
        sb.Append(isAsk ? "1" : "0");
        return sb.ToString();
    }
}
