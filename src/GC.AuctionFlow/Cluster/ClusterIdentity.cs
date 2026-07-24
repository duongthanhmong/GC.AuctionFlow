using System.Globalization;
using System.Text;

namespace GC.AuctionFlow.Cluster;

public static class ClusterRawIdentity
{
    public static string BuildAuction(
        string orderflowAuctionSnapshotId,
        string policyVersion = ClusterRawFeaturePolicyConfig.PolicyVersion)
    {
        var sb = new StringBuilder(320);
        sb.Append("CLRAW|");
        sb.Append(Sanitize(orderflowAuctionSnapshotId));
        sb.Append('|');
        sb.Append(policyVersion);
        return sb.ToString();
    }

    public static string BuildPriceLevel(
        string clusterRawSnapshotId,
        long priceTick,
        string policyVersion = ClusterRawFeaturePolicyConfig.PolicyVersion)
    {
        var sb = new StringBuilder(320);
        sb.Append("CLRAWPX|");
        sb.Append(Sanitize(clusterRawSnapshotId));
        sb.Append('|');
        sb.Append(priceTick.ToString(CultureInfo.InvariantCulture));
        sb.Append('|');
        sb.Append(policyVersion);
        return sb.ToString();
    }

    public static string BuildEpisode(
        string orderflowEpisodeSnapshotId,
        string policyVersion = ClusterRawFeaturePolicyConfig.PolicyVersion)
    {
        var sb = new StringBuilder(320);
        sb.Append("CLRAWEP|");
        sb.Append(Sanitize(orderflowEpisodeSnapshotId));
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

/// <summary>
/// Raw descriptive ratio. No [0,1] cap. Never NaN/Infinity. Denominator zero → null.
/// </summary>
public static class ClusterRawRatio
{
    public static decimal? TryCompute(decimal? numerator, decimal? denominator, out string? unavailableReason)
    {
        unavailableReason = null;
        if (!numerator.HasValue || !denominator.HasValue)
        {
            unavailableReason = numerator.HasValue
                ? ClusterRawFeaturePolicyConfig.LimitationBidUnavailable
                : ClusterRawFeaturePolicyConfig.LimitationAskUnavailable;
            return null;
        }

        if (denominator.Value == 0m)
        {
            unavailableReason = ClusterRawFeaturePolicyConfig.LimitationOpposingDenomZero;
            return null;
        }

        // decimal division never yields NaN/Infinity; denominator-zero already rejected.
        return numerator.Value / denominator.Value;
    }
}

/// <summary>
/// EMPIRICAL_MIDRANK_V1: 1-based midranks for ties; percentile = (midrank-1)/(N-1) when N&gt;1 else 0.
/// </summary>
public static class EmpiricalMidrankV1
{
    public static void Compute(
        IReadOnlyList<decimal> values,
        out int[] ranksOneBased,
        out decimal?[] percentiles,
        out int populationSize)
    {
        populationSize = values?.Count ?? 0;
        ranksOneBased = new int[populationSize];
        percentiles = new decimal?[populationSize];
        if (populationSize == 0)
            return;

        var order = Enumerable.Range(0, populationSize)
            .OrderBy(i => values![i])
            .ThenBy(i => i)
            .ToArray();

        var midranks = new decimal[populationSize];
        var i = 0;
        while (i < populationSize)
        {
            var j = i;
            while (j + 1 < populationSize && values![order[j + 1]] == values[order[i]])
                j++;
            // 1-based positions i+1 .. j+1 → midrank average
            var mid = ((i + 1) + (j + 1)) / 2m;
            for (var k = i; k <= j; k++)
                midranks[order[k]] = mid;
            i = j + 1;
        }

        for (var idx = 0; idx < populationSize; idx++)
        {
            ranksOneBased[idx] = (int)decimal.Round(midranks[idx], MidpointRounding.AwayFromZero);
            if (populationSize == 1)
                percentiles[idx] = 0m;
            else
            {
                var p = (midranks[idx] - 1m) / (populationSize - 1m);
                if (p < 0m) p = 0m;
                if (p > 1m) p = 1m;
                percentiles[idx] = p;
            }
        }
    }
}
