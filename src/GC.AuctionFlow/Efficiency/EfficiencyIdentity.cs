using System.Globalization;
using System.Text;

namespace GC.AuctionFlow.Efficiency;

public static class EfficiencyIdentity
{
    public static string BuildAuction(
        string orderflowAuctionSnapshotId,
        string policyVersion = AuctionEfficiencyEvidencePolicyConfig.PolicyVersion)
    {
        var sb = new StringBuilder(320);
        sb.Append("AEFF|");
        sb.Append(Sanitize(orderflowAuctionSnapshotId));
        sb.Append('|');
        sb.Append(policyVersion);
        return sb.ToString();
    }

    public static string BuildEpisode(
        string episodeId,
        string policyVersion = AuctionEfficiencyEvidencePolicyConfig.PolicyVersion)
    {
        var sb = new StringBuilder(256);
        sb.Append("AEFFEP|");
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

/// <summary>Null-safe descriptive ratio. Never NaN/Infinity. Zero denom → null.</summary>
public static class EfficiencyRatio
{
    public static decimal? TryDivide(decimal? numerator, decimal? denominator)
    {
        if (!numerator.HasValue || !denominator.HasValue)
            return null;
        if (denominator.Value == 0m)
            return null;
        return numerator.Value / denominator.Value;
    }

    public static decimal? TryDivide(decimal numerator, decimal denominator)
    {
        if (denominator == 0m)
            return null;
        return numerator / denominator;
    }

    public static decimal? TryDivide(decimal numerator, long denominator)
    {
        if (denominator == 0)
            return null;
        return numerator / denominator;
    }

    public static decimal? TryDivide(long numerator, decimal denominator)
    {
        if (denominator == 0m)
            return null;
        return numerator / denominator;
    }

    public static decimal? TryDivide(TimeSpan numerator, long denominatorTicks)
    {
        if (denominatorTicks == 0)
            return null;
        return (decimal)numerator.TotalSeconds / denominatorTicks;
    }

    public static decimal? TryDivide(long? numerator, decimal denominator)
    {
        if (!numerator.HasValue || denominator == 0m)
            return null;
        return numerator.Value / denominator;
    }

    public static decimal? TryDivide(long? numerator, long denominator)
    {
        if (!numerator.HasValue || denominator == 0)
            return null;
        return (decimal)numerator.Value / denominator;
    }

    public static decimal? TryDivide(decimal numerator, long? denominator)
    {
        if (!denominator.HasValue || denominator.Value == 0)
            return null;
        return numerator / denominator.Value;
    }

    public static decimal? TryDivide(long numerator, long? denominator)
    {
        if (!denominator.HasValue || denominator.Value == 0)
            return null;
        return (decimal)numerator / denominator.Value;
    }

    public static decimal? TryDivide(TimeSpan? numerator, long? denominatorTicks)
    {
        if (!numerator.HasValue || !denominatorTicks.HasValue || denominatorTicks.Value == 0)
            return null;
        return (decimal)numerator.Value.TotalSeconds / denominatorTicks.Value;
    }
}

public readonly struct EfficiencyInputFingerprint : IEquatable<EfficiencyInputFingerprint>
{
    public EfficiencyInputFingerprint(
        bool enabled,
        string orderflowAuctionId,
        long orderflowEventRevision,
        long orderflowStateVersion,
        string clusterAuctionId,
        long clusterEventRevision,
        long clusterStateVersion,
        string episodeRevisionKey,
        string evidenceRevisionKey,
        string profileRevisionKey,
        string primaryAuctionId,
        string dataEpoch,
        decimal tickSize,
        string timestampPolicy,
        string policyVersion)
    {
        Enabled = enabled;
        OrderflowAuctionId = orderflowAuctionId ?? "";
        OrderflowEventRevision = orderflowEventRevision;
        OrderflowStateVersion = orderflowStateVersion;
        ClusterAuctionId = clusterAuctionId ?? "";
        ClusterEventRevision = clusterEventRevision;
        ClusterStateVersion = clusterStateVersion;
        EpisodeRevisionKey = episodeRevisionKey ?? "";
        EvidenceRevisionKey = evidenceRevisionKey ?? "";
        ProfileRevisionKey = profileRevisionKey ?? "";
        PrimaryAuctionId = primaryAuctionId ?? "";
        DataEpoch = dataEpoch ?? "";
        TickSize = tickSize;
        TimestampPolicy = timestampPolicy ?? "";
        PolicyVersion = policyVersion ?? "";
    }

    public bool Enabled { get; }
    public string OrderflowAuctionId { get; }
    public long OrderflowEventRevision { get; }
    public long OrderflowStateVersion { get; }
    public string ClusterAuctionId { get; }
    public long ClusterEventRevision { get; }
    public long ClusterStateVersion { get; }
    public string EpisodeRevisionKey { get; }
    public string EvidenceRevisionKey { get; }
    public string ProfileRevisionKey { get; }
    public string PrimaryAuctionId { get; }
    public string DataEpoch { get; }
    public decimal TickSize { get; }
    public string TimestampPolicy { get; }
    public string PolicyVersion { get; }

    public bool Equals(EfficiencyInputFingerprint other) =>
        Enabled == other.Enabled
        && string.Equals(OrderflowAuctionId, other.OrderflowAuctionId, StringComparison.Ordinal)
        && OrderflowEventRevision == other.OrderflowEventRevision
        && OrderflowStateVersion == other.OrderflowStateVersion
        && string.Equals(ClusterAuctionId, other.ClusterAuctionId, StringComparison.Ordinal)
        && ClusterEventRevision == other.ClusterEventRevision
        && ClusterStateVersion == other.ClusterStateVersion
        && string.Equals(EpisodeRevisionKey, other.EpisodeRevisionKey, StringComparison.Ordinal)
        && string.Equals(EvidenceRevisionKey, other.EvidenceRevisionKey, StringComparison.Ordinal)
        && string.Equals(ProfileRevisionKey, other.ProfileRevisionKey, StringComparison.Ordinal)
        && string.Equals(PrimaryAuctionId, other.PrimaryAuctionId, StringComparison.Ordinal)
        && string.Equals(DataEpoch, other.DataEpoch, StringComparison.Ordinal)
        && TickSize == other.TickSize
        && string.Equals(TimestampPolicy, other.TimestampPolicy, StringComparison.Ordinal)
        && string.Equals(PolicyVersion, other.PolicyVersion, StringComparison.Ordinal);

    public override bool Equals(object? obj) =>
        obj is EfficiencyInputFingerprint other && Equals(other);

    public override int GetHashCode()
    {
        var h = new HashCode();
        h.Add(Enabled);
        h.Add(OrderflowAuctionId);
        h.Add(OrderflowEventRevision);
        h.Add(OrderflowStateVersion);
        h.Add(ClusterAuctionId);
        h.Add(ClusterEventRevision);
        h.Add(ClusterStateVersion);
        h.Add(EpisodeRevisionKey);
        h.Add(EvidenceRevisionKey);
        h.Add(ProfileRevisionKey);
        h.Add(PrimaryAuctionId);
        h.Add(DataEpoch);
        h.Add(TickSize);
        h.Add(TimestampPolicy);
        h.Add(PolicyVersion);
        return h.ToHashCode();
    }

    public override string ToString() =>
        Enabled + "|" + OrderflowAuctionId + "|" + OrderflowEventRevision.ToString(CultureInfo.InvariantCulture)
        + "|" + ClusterAuctionId + "|" + ClusterEventRevision.ToString(CultureInfo.InvariantCulture)
        + "|" + EpisodeRevisionKey + "|" + EvidenceRevisionKey + "|" + ProfileRevisionKey
        + "|" + PrimaryAuctionId + "|" + DataEpoch + "|" + TickSize.ToString(CultureInfo.InvariantCulture)
        + "|" + TimestampPolicy + "|" + PolicyVersion;
}
