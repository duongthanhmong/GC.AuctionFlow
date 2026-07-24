using System.Globalization;
using System.Text;

namespace GC.AuctionFlow.Evidence;

/// <summary>AREV|{EpisodeId}|ACCEPTANCE_REENTRY_EVIDENCE_POLICY_V1</summary>
public static class EvidenceIdentity
{
    public static string Build(
        string episodeId,
        string policyVersion = AcceptanceReentryEvidencePolicyConfig.PolicyVersion)
    {
        var sb = new StringBuilder(256);
        sb.Append("AREV|");
        sb.Append(Sanitize(episodeId));
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

public static class EvidenceRatio
{
    /// <summary>Descriptive ratio only. Zero denominator → null. Never NaN/Infinity.</summary>
    public static decimal? TryCompute(decimal numerator, decimal denominator)
    {
        if (denominator == 0m)
            return null;
        return numerator / denominator;
    }

    public static decimal? TryCompute(long numerator, long denominator)
    {
        if (denominator == 0)
            return null;
        return TryCompute((decimal)numerator, (decimal)denominator);
    }

    public static decimal? TryCompute(TimeSpan numerator, TimeSpan denominator)
    {
        if (denominator.Ticks == 0)
            return null;
        return TryCompute(numerator.Ticks, denominator.Ticks);
    }
}
