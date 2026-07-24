using System.Globalization;
using System.Text;
using GC.AuctionFlow.Composite;
using GC.AuctionFlow.Profile;

namespace GC.AuctionFlow.Reference;

/// <summary>
/// Normalized Structural Reference input identity for publish-path change detection.
/// Value equality only — no hash/I/O/JSON.
/// </summary>
public readonly struct ReferenceInputFingerprint : IEquatable<ReferenceInputFingerprint>
{
    public ReferenceInputFingerprint(
        bool enabled,
        string profilesKey,
        string compositeKey,
        string policyVersion,
        decimal tickSize,
        string dataEpoch,
        string timestampPolicyVersion)
    {
        Enabled = enabled;
        ProfilesKey = profilesKey ?? "";
        CompositeKey = compositeKey ?? "";
        PolicyVersion = policyVersion ?? "";
        TickSize = tickSize;
        DataEpoch = dataEpoch ?? "";
        TimestampPolicyVersion = timestampPolicyVersion ?? "";
    }

    public bool Enabled { get; }
    public string ProfilesKey { get; }
    public string CompositeKey { get; }
    public string PolicyVersion { get; }
    public decimal TickSize { get; }
    public string DataEpoch { get; }
    public string TimestampPolicyVersion { get; }

    public static ReferenceInputFingerprint Build(
        bool enabled,
        PrimaryProfileSetSnapshot? profiles,
        CompositeSetSnapshot? composite,
        decimal tickSize,
        string dataEpoch,
        string timestampPolicyVersion,
        string policyVersion = ReferencePolicyConfig.PolicyVersion)
    {
        return new ReferenceInputFingerprint(
            enabled,
            BuildProfilesKey(profiles),
            BuildCompositeKey(composite),
            policyVersion,
            tickSize,
            dataEpoch,
            timestampPolicyVersion);
    }

    public static string BuildProfilesKey(PrimaryProfileSetSnapshot? profiles)
    {
        if (profiles is null)
            return "none";
        var sb = new StringBuilder(256);
        AppendAuction(sb, "CUR", profiles.CurrentAuction);
        sb.Append(';');
        AppendAuction(sb, "PREV", profiles.PreviousAuction);
        sb.Append(";N=");
        sb.Append(profiles.CompletedAuctions.Count.ToString(CultureInfo.InvariantCulture));
        return sb.ToString();
    }

    public static string BuildCompositeKey(CompositeSetSnapshot? composite)
    {
        if (composite?.Confirmed is not { } conf)
            return "none";
        // Preview intentionally excluded — must not drive Structural Reference identity.
        var sb = new StringBuilder(128);
        sb.Append(conf.CompositeId);
        sb.Append('|');
        sb.Append(conf.CompositeStatus);
        sb.Append('|');
        AppendDec(sb, conf.TpoPoc);
        sb.Append('|');
        AppendDec(sb, conf.TpoVah);
        sb.Append('|');
        AppendDec(sb, conf.TpoVal);
        sb.Append('|');
        AppendDec(sb, conf.VolumePoc);
        sb.Append('|');
        AppendDec(sb, conf.VolumeVah);
        sb.Append('|');
        AppendDec(sb, conf.VolumeVal);
        sb.Append('|');
        AppendDec(sb, conf.ProfileHigh);
        sb.Append('|');
        AppendDec(sb, conf.ProfileLow);
        sb.Append('|');
        sb.Append(conf.Aggregate?.PriceVolumeCapability.ToString() ?? "none");
        return sb.ToString();
    }

    private static void AppendAuction(StringBuilder sb, string tag, PrimaryAuctionProfileSnapshot? a)
    {
        sb.Append(tag);
        sb.Append('=');
        if (a is null)
        {
            sb.Append("none");
            return;
        }

        sb.Append(a.AuctionId);
        sb.Append('|');
        sb.Append(a.IsCompleted ? "C" : "D");
        sb.Append('|');
        sb.Append(a.ProfileState);
        sb.Append('|');
        AppendDec(sb, a.ProfileHigh);
        sb.Append('|');
        AppendDec(sb, a.ProfileLow);
        sb.Append('|');
        AppendDec(sb, a.TpoProfile?.TpoPoc);
        sb.Append('|');
        AppendDec(sb, a.TpoProfile?.TpoVah);
        sb.Append('|');
        AppendDec(sb, a.TpoProfile?.TpoVal);
        sb.Append('|');
        AppendDec(sb, a.VolumeProfile?.VolumePoc);
        sb.Append('|');
        AppendDec(sb, a.VolumeProfile?.VolumeVah);
        sb.Append('|');
        AppendDec(sb, a.VolumeProfile?.VolumeVal);
        sb.Append('|');
        sb.Append(a.VolumeProfile?.PriceVolumeCapability.ToString() ?? "none");
        sb.Append('|');
        AppendDec(sb, a.LastObservedPrice);
    }

    private static void AppendDec(StringBuilder sb, decimal? v)
    {
        if (v is null)
            sb.Append('-');
        else
            sb.Append(v.Value.ToString(CultureInfo.InvariantCulture));
    }

    public bool Equals(ReferenceInputFingerprint other) =>
        Enabled == other.Enabled
        && string.Equals(ProfilesKey, other.ProfilesKey, StringComparison.Ordinal)
        && string.Equals(CompositeKey, other.CompositeKey, StringComparison.Ordinal)
        && string.Equals(PolicyVersion, other.PolicyVersion, StringComparison.Ordinal)
        && TickSize == other.TickSize
        && string.Equals(DataEpoch, other.DataEpoch, StringComparison.Ordinal)
        && string.Equals(TimestampPolicyVersion, other.TimestampPolicyVersion, StringComparison.Ordinal);

    public override bool Equals(object? obj) =>
        obj is ReferenceInputFingerprint other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(Enabled, ProfilesKey, CompositeKey, PolicyVersion, TickSize, DataEpoch, TimestampPolicyVersion);

    public static bool operator ==(ReferenceInputFingerprint left, ReferenceInputFingerprint right) =>
        left.Equals(right);

    public static bool operator !=(ReferenceInputFingerprint left, ReferenceInputFingerprint right) =>
        !left.Equals(right);

    public override string ToString() =>
        Enabled + "|" + ProfilesKey + "|" + CompositeKey + "|" + PolicyVersion + "|" +
        TickSize.ToString(CultureInfo.InvariantCulture) + "|" + DataEpoch + "|" + TimestampPolicyVersion;
}

/// <summary>
/// Guard for Structural Reference rebuild on GPS publish paths.
/// Rebuilds when host/Current missing OR input fingerprint changed.
/// Ordinary unchanged trade publishes reuse.
/// </summary>
public static class ReferencePublishInitialization
{
    public static bool ShouldProcess(
        bool enableStructuralReferences,
        bool primaryProfileAvailable,
        bool referenceCurrentMissing,
        ReferenceInputFingerprint? lastSuccessfullyApplied,
        ReferenceInputFingerprint current)
    {
        if (!enableStructuralReferences)
            return false;
        if (!primaryProfileAvailable)
            return referenceCurrentMissing || lastSuccessfullyApplied is null;
        if (referenceCurrentMissing || lastSuccessfullyApplied is null)
            return true;
        return !lastSuccessfullyApplied.Value.Equals(current);
    }

    public static bool ShouldProcess(
        bool enableStructuralReferences,
        PrimaryProfileSetSnapshot? profiles,
        StructuralReferenceHost? host,
        ReferenceInputFingerprint? lastSuccessfullyApplied,
        ReferenceInputFingerprint current) =>
        ShouldProcess(
            enableStructuralReferences,
            profiles is not null,
            host is null || host.Current is null,
            lastSuccessfullyApplied,
            current);
}
