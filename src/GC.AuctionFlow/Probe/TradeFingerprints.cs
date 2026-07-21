using System.Globalization;
using System.Text;

namespace GC.AuctionFlow.Probe;

/// <summary>
/// Diagnostic fingerprints only. Not a trade ID, not a native sequence, never used for deletion.
/// Algorithm: invariant CultureInfo pipe-joined fields (deterministic string encoding).
/// </summary>
public static class TradeFingerprints
{
    /// <summary>
    /// Core new-trade: sourceTimeTicks | price | volume | direction | dataType
    /// </summary>
    public static string CoreNewTrade(
        long sourceTimeTicks,
        decimal price,
        decimal volume,
        string direction,
        string dataType)
    {
        var sb = new StringBuilder(96);
        AppendLong(sb, sourceTimeTicks);
        sb.Append('|');
        AppendDecimal(sb, price);
        sb.Append('|');
        AppendDecimal(sb, volume);
        sb.Append('|');
        sb.Append(direction);
        sb.Append('|');
        sb.Append(dataType);
        return sb.ToString();
    }

    /// <summary>
    /// Extended new-trade: core | ExchangeOrderId | AggressorExchangeOrderId
    /// </summary>
    public static string ExtendedNewTrade(
        string coreFingerprint,
        long? exchangeOrderId,
        long? aggressorExchangeOrderId)
    {
        var sb = new StringBuilder(coreFingerprint.Length + 48);
        sb.Append(coreFingerprint);
        sb.Append('|');
        AppendNullableLong(sb, exchangeOrderId);
        sb.Append('|');
        AppendNullableLong(sb, aggressorExchangeOrderId);
        return sb.ToString();
    }

    /// <summary>
    /// Cumulative value: sourceTimeTicks | volume | firstPrice | lastPrice | direction | tickCount
    /// </summary>
    public static string CumulativeValue(
        long sourceTimeTicks,
        decimal volume,
        decimal firstPrice,
        decimal lastPrice,
        string direction,
        int tickCount)
    {
        var sb = new StringBuilder(112);
        AppendLong(sb, sourceTimeTicks);
        sb.Append('|');
        AppendDecimal(sb, volume);
        sb.Append('|');
        AppendDecimal(sb, firstPrice);
        sb.Append('|');
        AppendDecimal(sb, lastPrice);
        sb.Append('|');
        sb.Append(direction);
        sb.Append('|');
        AppendLong(sb, tickCount);
        return sb.ToString();
    }

    private static void AppendLong(StringBuilder sb, long value) =>
        sb.Append(value.ToString(CultureInfo.InvariantCulture));

    private static void AppendNullableLong(StringBuilder sb, long? value)
    {
        if (value is null)
            sb.Append("null");
        else
            AppendLong(sb, value.Value);
    }

    private static void AppendDecimal(StringBuilder sb, decimal value) =>
        sb.Append(value.ToString(CultureInfo.InvariantCulture));
}
