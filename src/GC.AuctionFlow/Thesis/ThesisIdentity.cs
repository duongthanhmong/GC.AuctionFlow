namespace GC.AuctionFlow.Thesis;

public static class ThesisIdentity
{
    public static string BuildFarId(string evidenceId,
        string policyVersion = FarThesisPolicyConfig.PolicyVersion)
    {
        var sanitized = string.IsNullOrWhiteSpace(evidenceId)
            ? "Unknown"
            : evidenceId.Replace("|", "_", StringComparison.Ordinal);
        return $"FAR|{sanitized}|{policyVersion}";
    }

    public static string BuildAacId(string evidenceId,
        string policyVersion = AacThesisPolicyConfig.PolicyVersion)
    {
        var sanitized = string.IsNullOrWhiteSpace(evidenceId)
            ? "Unknown"
            : evidenceId.Replace("|", "_", StringComparison.Ordinal);
        return $"AAC|{sanitized}|{policyVersion}";
    }
}

public readonly struct FarThesisInputFingerprint : IEquatable<FarThesisInputFingerprint>
{
    private readonly bool _enabled;
    private readonly long _evidenceRegistryRevision;
    private readonly string _evidenceInputFp;
    private readonly string _policyVersion;

    public FarThesisInputFingerprint(
        bool enabled,
        long evidenceRegistryRevision,
        string evidenceInputFp,
        string policyVersion)
    {
        _enabled = enabled;
        _evidenceRegistryRevision = evidenceRegistryRevision;
        _evidenceInputFp = evidenceInputFp ?? "";
        _policyVersion = policyVersion ?? "";
    }

    public bool Equals(FarThesisInputFingerprint other) =>
        _enabled == other._enabled
        && _evidenceRegistryRevision == other._evidenceRegistryRevision
        && string.Equals(_evidenceInputFp, other._evidenceInputFp, StringComparison.Ordinal)
        && string.Equals(_policyVersion, other._policyVersion, StringComparison.Ordinal);

    public override bool Equals(object? obj) =>
        obj is FarThesisInputFingerprint other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(_enabled, _evidenceRegistryRevision, _evidenceInputFp, _policyVersion);

    public override string ToString() =>
        _enabled + "|" + _evidenceRegistryRevision + "|" + _evidenceInputFp + "|" + _policyVersion;
}

public readonly struct AacThesisInputFingerprint : IEquatable<AacThesisInputFingerprint>
{
    private readonly bool _enabled;
    private readonly long _evidenceRegistryRevision;
    private readonly string _evidenceInputFp;
    private readonly string _policyVersion;

    public AacThesisInputFingerprint(
        bool enabled,
        long evidenceRegistryRevision,
        string evidenceInputFp,
        string policyVersion)
    {
        _enabled = enabled;
        _evidenceRegistryRevision = evidenceRegistryRevision;
        _evidenceInputFp = evidenceInputFp ?? "";
        _policyVersion = policyVersion ?? "";
    }

    public bool Equals(AacThesisInputFingerprint other) =>
        _enabled == other._enabled
        && _evidenceRegistryRevision == other._evidenceRegistryRevision
        && string.Equals(_evidenceInputFp, other._evidenceInputFp, StringComparison.Ordinal)
        && string.Equals(_policyVersion, other._policyVersion, StringComparison.Ordinal);

    public override bool Equals(object? obj) =>
        obj is AacThesisInputFingerprint other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(_enabled, _evidenceRegistryRevision, _evidenceInputFp, _policyVersion);

    public override string ToString() =>
        _enabled + "|" + _evidenceRegistryRevision + "|" + _evidenceInputFp + "|" + _policyVersion;
}
