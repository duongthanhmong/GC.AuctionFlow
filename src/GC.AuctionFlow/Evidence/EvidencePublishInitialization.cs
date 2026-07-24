using System.Globalization;
using System.Text;
using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Profile;

namespace GC.AuctionFlow.Evidence;

public readonly struct EvidenceInputFingerprint : IEquatable<EvidenceInputFingerprint>
{
    public EvidenceInputFingerprint(
        bool enabled,
        string primaryAuctionId,
        string episodeKey,
        string policyVersion,
        decimal tickSize,
        string dataEpoch,
        string timestampPolicyVersion)
    {
        Enabled = enabled;
        PrimaryAuctionId = primaryAuctionId ?? "";
        EpisodeKey = episodeKey ?? "";
        PolicyVersion = policyVersion ?? "";
        TickSize = tickSize;
        DataEpoch = dataEpoch ?? "";
        TimestampPolicyVersion = timestampPolicyVersion ?? "";
    }

    public bool Enabled { get; }
    public string PrimaryAuctionId { get; }
    public string EpisodeKey { get; }
    public string PolicyVersion { get; }
    public decimal TickSize { get; }
    public string DataEpoch { get; }
    public string TimestampPolicyVersion { get; }

    public static EvidenceInputFingerprint Build(
        bool enabled,
        AuctionEpisodeSetSnapshot? episodes,
        decimal tickSize,
        string dataEpoch,
        string timestampPolicyVersion,
        string policyVersion = AcceptanceReentryEvidencePolicyConfig.PolicyVersion) =>
        new(
            enabled,
            episodes?.PrimaryAuctionId ?? "",
            BuildEpisodeKey(episodes),
            policyVersion,
            tickSize,
            dataEpoch,
            timestampPolicyVersion);

    public static string BuildEpisodeKey(AuctionEpisodeSetSnapshot? episodes)
    {
        if (episodes is null)
            return "none";
        var sb = new StringBuilder(96);
        sb.Append(episodes.ModuleState);
        sb.Append("|R=");
        sb.Append(episodes.RegistryRevision.ToString(CultureInfo.InvariantCulture));
        sb.Append("|A=");
        sb.Append(episodes.ActiveEpisodes.Count.ToString(CultureInfo.InvariantCulture));
        return sb.ToString();
    }

    public bool Equals(EvidenceInputFingerprint other) =>
        Enabled == other.Enabled
        && string.Equals(PrimaryAuctionId, other.PrimaryAuctionId, StringComparison.Ordinal)
        && string.Equals(EpisodeKey, other.EpisodeKey, StringComparison.Ordinal)
        && string.Equals(PolicyVersion, other.PolicyVersion, StringComparison.Ordinal)
        && TickSize == other.TickSize
        && string.Equals(DataEpoch, other.DataEpoch, StringComparison.Ordinal)
        && string.Equals(TimestampPolicyVersion, other.TimestampPolicyVersion, StringComparison.Ordinal);

    public override bool Equals(object? obj) =>
        obj is EvidenceInputFingerprint other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(Enabled, PrimaryAuctionId, EpisodeKey, PolicyVersion, TickSize, DataEpoch, TimestampPolicyVersion);

    public override string ToString() =>
        Enabled + "|" + PrimaryAuctionId + "|" + EpisodeKey + "|" + PolicyVersion + "|" +
        TickSize.ToString(CultureInfo.InvariantCulture) + "|" + DataEpoch + "|" + TimestampPolicyVersion;
}

public static class EvidencePublishInitialization
{
    public static bool ShouldProcess(
        bool enable,
        AuctionEpisodeSetSnapshot? episodes,
        AcceptanceReentryEvidenceHost? host,
        EvidenceInputFingerprint? lastApplied,
        EvidenceInputFingerprint current)
    {
        if (!enable)
            return false;
        if (host is null || host.Current is null || lastApplied is null)
            return true;
        return !lastApplied.Value.Equals(current);
    }
}
