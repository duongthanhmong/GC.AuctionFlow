using System.Text;

namespace GC.AuctionFlow.Resolution;

public static class ResolutionIdentity
{
    /// <summary>
    /// Build a stable ResolutionId from an EvidenceId.
    /// Format: ARES|{sanitizedEvidenceId}|{policyVersion}
    /// </summary>
    public static string BuildFromEvidenceId(
        string evidenceId,
        string policyVersion = AuctionResolutionPolicyConfig.PolicyVersion)
    {
        var sb = new StringBuilder(256);
        sb.Append("ARES|");
        sb.Append(Sanitize(evidenceId));
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
/// Input fingerprint for the Resolution host — gates rebuild on evidence change.
/// Equality: enabled + evidenceRegistryRevision + evidenceInputFingerprint + policyVersion.
/// </summary>
public readonly struct ResolutionInputFingerprint : IEquatable<ResolutionInputFingerprint>
{
    public ResolutionInputFingerprint(
        bool enabled,
        long evidenceRegistryRevision,
        string evidenceInputFingerprint,
        string policyVersion)
    {
        Enabled = enabled;
        EvidenceRegistryRevision = evidenceRegistryRevision;
        EvidenceInputFingerprint = evidenceInputFingerprint ?? "";
        PolicyVersion = policyVersion ?? AuctionResolutionPolicyConfig.PolicyVersion;
    }

    public bool Enabled { get; }
    public long EvidenceRegistryRevision { get; }
    public string EvidenceInputFingerprint { get; }
    public string PolicyVersion { get; }

    public bool Equals(ResolutionInputFingerprint other) =>
        Enabled == other.Enabled &&
        EvidenceRegistryRevision == other.EvidenceRegistryRevision &&
        string.Equals(EvidenceInputFingerprint, other.EvidenceInputFingerprint, StringComparison.Ordinal) &&
        string.Equals(PolicyVersion, other.PolicyVersion, StringComparison.Ordinal);

    public override bool Equals(object? obj) =>
        obj is ResolutionInputFingerprint fp && Equals(fp);

    public override int GetHashCode() =>
        HashCode.Combine(Enabled, EvidenceRegistryRevision, EvidenceInputFingerprint, PolicyVersion);

    public override string ToString() =>
        $"enabled={Enabled}|evRev={EvidenceRegistryRevision}|evFp={EvidenceInputFingerprint}|pol={PolicyVersion}";
}
