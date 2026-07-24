using System.Globalization;
using System.Text;
using GC.AuctionFlow.Directional;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Reference;

namespace GC.AuctionFlow.Episode;

/// <summary>Lightweight Episode input identity for non-event initialization. No hash/I/O/JSON.</summary>
public readonly struct EpisodeInputFingerprint : IEquatable<EpisodeInputFingerprint>
{
    public EpisodeInputFingerprint(
        bool enabled,
        string primaryAuctionId,
        string referenceKey,
        string directionalKey,
        string tradeStreamEpoch,
        string policyVersion,
        decimal tickSize,
        string dataEpoch,
        string timestampPolicyVersion)
    {
        Enabled = enabled;
        PrimaryAuctionId = primaryAuctionId ?? "";
        ReferenceKey = referenceKey ?? "";
        DirectionalKey = directionalKey ?? "";
        TradeStreamEpoch = tradeStreamEpoch ?? "";
        PolicyVersion = policyVersion ?? "";
        TickSize = tickSize;
        DataEpoch = dataEpoch ?? "";
        TimestampPolicyVersion = timestampPolicyVersion ?? "";
    }

    public bool Enabled { get; }
    public string PrimaryAuctionId { get; }
    public string ReferenceKey { get; }
    public string DirectionalKey { get; }
    public string TradeStreamEpoch { get; }
    public string PolicyVersion { get; }
    public decimal TickSize { get; }
    public string DataEpoch { get; }
    public string TimestampPolicyVersion { get; }

    public static EpisodeInputFingerprint Build(
        bool enabled,
        string? primaryAuctionId,
        StructuralReferenceSetSnapshot? references,
        DirectionalContextSetSnapshot? directional,
        string tradeStreamEpoch,
        decimal tickSize,
        string dataEpoch,
        string timestampPolicyVersion,
        string policyVersion = EpisodePolicyConfig.PolicyVersion) =>
        new(
            enabled,
            primaryAuctionId ?? "",
            BuildReferenceKey(references),
            BuildDirectionalKey(directional),
            tradeStreamEpoch,
            policyVersion,
            tickSize,
            dataEpoch,
            timestampPolicyVersion);

    public static string BuildReferenceKey(StructuralReferenceSetSnapshot? references)
    {
        if (references is null)
            return "none";
        var sb = new StringBuilder(128);
        sb.Append(references.ModuleState);
        sb.Append("|R=");
        sb.Append(references.RegistryRevision.ToString(CultureInfo.InvariantCulture));
        sb.Append("|C=");
        sb.Append(references.ConfirmedReferences.Count.ToString(CultureInfo.InvariantCulture));
        return sb.ToString();
    }

    public static string BuildDirectionalKey(DirectionalContextSetSnapshot? directional)
    {
        if (directional is null || directional.Status == DirectionalModuleState.Disabled)
            return "none";
        return directional.Status + "|V=" + directional.SnapshotVersionToken.ToString(CultureInfo.InvariantCulture);
    }

    public bool Equals(EpisodeInputFingerprint other) =>
        Enabled == other.Enabled
        && string.Equals(PrimaryAuctionId, other.PrimaryAuctionId, StringComparison.Ordinal)
        && string.Equals(ReferenceKey, other.ReferenceKey, StringComparison.Ordinal)
        && string.Equals(DirectionalKey, other.DirectionalKey, StringComparison.Ordinal)
        && string.Equals(TradeStreamEpoch, other.TradeStreamEpoch, StringComparison.Ordinal)
        && string.Equals(PolicyVersion, other.PolicyVersion, StringComparison.Ordinal)
        && TickSize == other.TickSize
        && string.Equals(DataEpoch, other.DataEpoch, StringComparison.Ordinal)
        && string.Equals(TimestampPolicyVersion, other.TimestampPolicyVersion, StringComparison.Ordinal);

    public override bool Equals(object? obj) =>
        obj is EpisodeInputFingerprint other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(
            HashCode.Combine(Enabled, PrimaryAuctionId, ReferenceKey, DirectionalKey, TradeStreamEpoch),
            HashCode.Combine(PolicyVersion, TickSize, DataEpoch, TimestampPolicyVersion));

    public static bool operator ==(EpisodeInputFingerprint left, EpisodeInputFingerprint right) =>
        left.Equals(right);

    public static bool operator !=(EpisodeInputFingerprint left, EpisodeInputFingerprint right) =>
        !left.Equals(right);

    public override string ToString() =>
        Enabled + "|" + PrimaryAuctionId + "|" + ReferenceKey + "|" + DirectionalKey + "|" +
        TradeStreamEpoch + "|" + PolicyVersion + "|" + TickSize.ToString(CultureInfo.InvariantCulture) + "|" +
        DataEpoch + "|" + TimestampPolicyVersion;
}

public static class EpisodePublishInitialization
{
    public static bool ShouldProcess(
        bool enableEpisodes,
        bool referencesAvailable,
        bool episodeCurrentMissing,
        EpisodeInputFingerprint? lastSuccessfullyApplied,
        EpisodeInputFingerprint current)
    {
        if (!enableEpisodes)
            return false;
        if (!referencesAvailable)
            return episodeCurrentMissing || lastSuccessfullyApplied is null;
        if (episodeCurrentMissing || lastSuccessfullyApplied is null)
            return true;
        return !lastSuccessfullyApplied.Value.Equals(current);
    }

    public static bool ShouldProcess(
        bool enableEpisodes,
        StructuralReferenceSetSnapshot? references,
        AuctionEpisodeHost? host,
        EpisodeInputFingerprint? lastSuccessfullyApplied,
        EpisodeInputFingerprint current) =>
        ShouldProcess(
            enableEpisodes,
            references is not null,
            host is null || host.Current is null,
            lastSuccessfullyApplied,
            current);
}
