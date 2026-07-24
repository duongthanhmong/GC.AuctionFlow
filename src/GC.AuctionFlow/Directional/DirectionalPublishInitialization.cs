using System.Globalization;
using System.Text;
using GC.AuctionFlow.Composite;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Reference;

namespace GC.AuctionFlow.Directional;

/// <summary>
/// Lightweight Directional input identity. Value equality only — no hash/I/O/JSON.
/// Cosmetic settings (diagnostics overlay) are excluded.
/// </summary>
public readonly struct DirectionalInputFingerprint : IEquatable<DirectionalInputFingerprint>
{
    public DirectionalInputFingerprint(
        bool enabled,
        bool enableOneTimeFraming,
        string profilesKey,
        string completedLedgerKey,
        string compositeKey,
        string referenceKey,
        string completedTpoKey,
        long? currentPriceTick,
        string policyVersion,
        decimal tickSize,
        string dataEpoch,
        string timestampPolicyVersion)
    {
        Enabled = enabled;
        EnableOneTimeFraming = enableOneTimeFraming;
        ProfilesKey = profilesKey ?? "";
        CompletedLedgerKey = completedLedgerKey ?? "";
        CompositeKey = compositeKey ?? "";
        ReferenceKey = referenceKey ?? "";
        CompletedTpoKey = completedTpoKey ?? "";
        CurrentPriceTick = currentPriceTick;
        PolicyVersion = policyVersion ?? "";
        TickSize = tickSize;
        DataEpoch = dataEpoch ?? "";
        TimestampPolicyVersion = timestampPolicyVersion ?? "";
    }

    public bool Enabled { get; }
    public bool EnableOneTimeFraming { get; }
    public string ProfilesKey { get; }
    public string CompletedLedgerKey { get; }
    public string CompositeKey { get; }
    public string ReferenceKey { get; }
    public string CompletedTpoKey { get; }
    public long? CurrentPriceTick { get; }
    public string PolicyVersion { get; }
    public decimal TickSize { get; }
    public string DataEpoch { get; }
    public string TimestampPolicyVersion { get; }

    /// <summary>History/evidence identity excluding live price tick.</summary>
    public string EvidenceKey =>
        Enabled + "|" + EnableOneTimeFraming + "|" + ProfilesKey + "|" + CompletedLedgerKey + "|" +
        CompositeKey + "|" + ReferenceKey + "|" + CompletedTpoKey + "|" + PolicyVersion + "|" +
        TickSize.ToString(CultureInfo.InvariantCulture) + "|" + DataEpoch + "|" + TimestampPolicyVersion;

    public static DirectionalInputFingerprint Build(
        bool enabled,
        bool enableOneTimeFraming,
        PrimaryProfileSetSnapshot? profiles,
        CompositeSetSnapshot? composite,
        StructuralReferenceSetSnapshot? references,
        decimal tickSize,
        string dataEpoch,
        string timestampPolicyVersion,
        string policyVersion = DirectionalPolicyConfig.PolicyVersion)
    {
        long? priceTick = null;
        var px = profiles?.CurrentAuction?.LastObservedPrice;
        if (px is decimal p && tickSize > 0m)
        {
            var grid = new PriceGrid(tickSize);
            if (grid.TryToTickIndex(p, out var t))
                priceTick = t;
        }

        return new DirectionalInputFingerprint(
            enabled,
            enableOneTimeFraming,
            BuildCurrentProfilesKey(profiles),
            BuildCompletedLedgerKey(profiles),
            BuildCompositeKey(composite),
            BuildReferenceKey(references, enabled),
            BuildCompletedTpoKey(profiles),
            priceTick,
            policyVersion,
            tickSize,
            dataEpoch,
            timestampPolicyVersion);
    }

    public static string BuildCurrentProfilesKey(PrimaryProfileSetSnapshot? profiles)
    {
        if (profiles is null)
            return "none";
        var sb = new StringBuilder(128);
        AppendAuction(sb, "CUR", profiles.CurrentAuction);
        sb.Append(';');
        AppendAuction(sb, "PREV", profiles.PreviousAuction);
        return sb.ToString();
    }

    public static string BuildCompletedLedgerKey(PrimaryProfileSetSnapshot? profiles)
    {
        if (profiles is null)
            return "none";
        var sb = new StringBuilder(256);
        sb.Append("N=");
        sb.Append(profiles.CompletedAuctions.Count.ToString(CultureInfo.InvariantCulture));
        foreach (var a in profiles.CompletedAuctions
                     .OrderBy(x => x.AuctionStartUtc)
                     .ThenBy(x => x.AuctionId, StringComparer.Ordinal))
        {
            sb.Append(';');
            AppendAuction(sb, "C", a);
        }

        return sb.ToString();
    }

    public static string BuildCompletedTpoKey(PrimaryProfileSetSnapshot? profiles)
    {
        var tpo = profiles?.CurrentAuction?.TpoProfile;
        if (tpo is null)
            return "none";
        var sb = new StringBuilder(128);
        sb.Append(tpo.AuctionId);
        sb.Append("|P=");
        sb.Append(tpo.CompletedPeriodCount.ToString(CultureInfo.InvariantCulture));
        sb.Append("|V=");
        sb.Append(tpo.Version);
        foreach (var p in tpo.CompletedPeriods.OrderBy(x => x.PeriodIndex))
        {
            sb.Append(';');
            sb.Append(p.PeriodIndex.ToString(CultureInfo.InvariantCulture));
            sb.Append(':');
            sb.Append(p.PeriodHighTick.ToString(CultureInfo.InvariantCulture));
            sb.Append('-');
            sb.Append(p.PeriodLowTick.ToString(CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }

    public static string BuildCompositeKey(CompositeSetSnapshot? composite) =>
        ReferenceInputFingerprint.BuildCompositeKey(composite);

    public static string BuildReferenceKey(StructuralReferenceSetSnapshot? references, bool directionalEnabled)
    {
        if (references is null)
            return directionalEnabled ? "ReferenceCapabilityUnavailable" : "none";
        return references.ModuleState + "|R=" + references.RegistryRevision.ToString(CultureInfo.InvariantCulture)
               + "|V=" + references.Version;
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
        sb.Append(a.Version);
        sb.Append('|');
        AppendDec(sb, a.TpoProfile?.TpoPoc);
        sb.Append('|');
        AppendDec(sb, a.TpoProfile?.TpoVah);
        sb.Append('|');
        AppendDec(sb, a.TpoProfile?.TpoVal);
        sb.Append('|');
        AppendDec(sb, a.VolumeProfile?.VolumePoc);
        sb.Append('|');
        sb.Append(a.VolumeProfile?.PriceVolumeCapability.ToString() ?? "none");
    }

    private static void AppendDec(StringBuilder sb, decimal? v)
    {
        if (v is null)
            sb.Append('-');
        else
            sb.Append(v.Value.ToString(CultureInfo.InvariantCulture));
    }

    public bool Equals(DirectionalInputFingerprint other) =>
        Enabled == other.Enabled
        && EnableOneTimeFraming == other.EnableOneTimeFraming
        && string.Equals(ProfilesKey, other.ProfilesKey, StringComparison.Ordinal)
        && string.Equals(CompletedLedgerKey, other.CompletedLedgerKey, StringComparison.Ordinal)
        && string.Equals(CompositeKey, other.CompositeKey, StringComparison.Ordinal)
        && string.Equals(ReferenceKey, other.ReferenceKey, StringComparison.Ordinal)
        && string.Equals(CompletedTpoKey, other.CompletedTpoKey, StringComparison.Ordinal)
        && CurrentPriceTick == other.CurrentPriceTick
        && string.Equals(PolicyVersion, other.PolicyVersion, StringComparison.Ordinal)
        && TickSize == other.TickSize
        && string.Equals(DataEpoch, other.DataEpoch, StringComparison.Ordinal)
        && string.Equals(TimestampPolicyVersion, other.TimestampPolicyVersion, StringComparison.Ordinal);

    public bool EvidenceEquals(DirectionalInputFingerprint other) =>
        string.Equals(EvidenceKey, other.EvidenceKey, StringComparison.Ordinal);

    public override bool Equals(object? obj) =>
        obj is DirectionalInputFingerprint other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(
            HashCode.Combine(Enabled, EnableOneTimeFraming, ProfilesKey, CompletedLedgerKey, CompositeKey, ReferenceKey),
            HashCode.Combine(CompletedTpoKey, CurrentPriceTick, PolicyVersion, TickSize, DataEpoch, TimestampPolicyVersion));

    public static bool operator ==(DirectionalInputFingerprint left, DirectionalInputFingerprint right) =>
        left.Equals(right);

    public static bool operator !=(DirectionalInputFingerprint left, DirectionalInputFingerprint right) =>
        !left.Equals(right);

    public override string ToString() =>
        EvidenceKey + "|PX=" + (CurrentPriceTick?.ToString(CultureInfo.InvariantCulture) ?? "-");
}

public static class DirectionalPublishInitialization
{
    public static bool ShouldProcess(
        bool enableDirectionalContext,
        bool primaryProfileAvailable,
        bool directionalCurrentMissing,
        DirectionalInputFingerprint? lastSuccessfullyApplied,
        DirectionalInputFingerprint current)
    {
        if (!enableDirectionalContext)
            return false;
        if (!primaryProfileAvailable)
            return directionalCurrentMissing || lastSuccessfullyApplied is null;
        if (directionalCurrentMissing || lastSuccessfullyApplied is null)
            return true;
        return !lastSuccessfullyApplied.Value.Equals(current);
    }

    public static bool ShouldProcess(
        bool enableDirectionalContext,
        PrimaryProfileSetSnapshot? profiles,
        DirectionalContextHost? host,
        DirectionalInputFingerprint? lastSuccessfullyApplied,
        DirectionalInputFingerprint current) =>
        ShouldProcess(
            enableDirectionalContext,
            profiles is not null,
            host is null || host.Current is null,
            lastSuccessfullyApplied,
            current);
}
