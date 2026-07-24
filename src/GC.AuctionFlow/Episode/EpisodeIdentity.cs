using System.Globalization;
using System.Text;

namespace GC.AuctionFlow.Episode;

/// <summary>
/// Deterministic EpisodeId.
/// EP|{instrument}|{epoch}|{primaryAuctionId}|{referenceId}|{firstInteractionEventIdentity}|AUCTION_EPISODE_POLICY_V1
/// </summary>
public static class EpisodeIdentity
{
    public static string Build(
        string instrumentIdentity,
        string dataEpoch,
        string primaryAuctionId,
        string referenceId,
        string firstInteractionEventIdentity,
        string policyVersion = EpisodePolicyConfig.PolicyVersion)
    {
        var sb = new StringBuilder(256);
        sb.Append("EP|");
        sb.Append(Sanitize(instrumentIdentity));
        sb.Append('|');
        sb.Append(Sanitize(dataEpoch));
        sb.Append('|');
        sb.Append(Sanitize(primaryAuctionId));
        sb.Append('|');
        sb.Append(Sanitize(referenceId));
        sb.Append('|');
        sb.Append(Sanitize(firstInteractionEventIdentity));
        sb.Append('|');
        sb.Append(policyVersion);
        return sb.ToString();
    }

    public static string BuildEventIdentity(long localMonotonicSequence, string coreDiagnosticFingerprint)
    {
        return "SEQ=" + localMonotonicSequence.ToString(CultureInfo.InvariantCulture)
               + "|FP=" + (coreDiagnosticFingerprint ?? "");
    }

    private static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Unknown";
        return value.Replace("|", "_", StringComparison.Ordinal);
    }
}
