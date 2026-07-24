using System.Globalization;
using System.Text;

namespace GC.AuctionFlow.Orderflow;

public static class OrderflowIdentity
{
    public static string BuildAuction(
        string instrumentIdentity,
        string dataEpoch,
        string primaryAuctionId,
        string policyVersion = ExecutedOrderflowPolicyConfig.PolicyVersion)
    {
        var sb = new StringBuilder(320);
        sb.Append("OFLOW|");
        sb.Append(Sanitize(instrumentIdentity));
        sb.Append('|');
        sb.Append(Sanitize(dataEpoch));
        sb.Append('|');
        sb.Append(Sanitize(primaryAuctionId));
        sb.Append('|');
        sb.Append(policyVersion);
        return sb.ToString();
    }

    public static string BuildEpisode(
        string episodeId,
        string policyVersion = ExecutedOrderflowPolicyConfig.PolicyVersion)
    {
        var sb = new StringBuilder(256);
        sb.Append("OFLOWEP|");
        sb.Append(Sanitize(episodeId));
        sb.Append('|');
        sb.Append(policyVersion);
        return sb.ToString();
    }

    public static string BuildPriceLevel(
        string auctionSnapshotId,
        long priceTick,
        string policyVersion = ExecutedOrderflowPolicyConfig.PolicyVersion)
    {
        var sb = new StringBuilder(320);
        sb.Append("OFLOWPX|");
        sb.Append(Sanitize(auctionSnapshotId));
        sb.Append('|');
        sb.Append(priceTick.ToString(CultureInfo.InvariantCulture));
        sb.Append('|');
        sb.Append(policyVersion);
        return sb.ToString();
    }

    private static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Unknown";
        return value.Replace("|", "_", StringComparison.Ordinal);
    }
}

public static class OrderflowRatio
{
    public static decimal? TryCompute(decimal numerator, decimal denominator)
    {
        if (denominator == 0m)
            return null;
        var r = numerator / denominator;
        if (r < 0m) return 0m;
        if (r > 1m) return 1m;
        return r;
    }
}
