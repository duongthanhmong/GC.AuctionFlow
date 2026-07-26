namespace GC.AuctionFlow.EffortResult;

public static class EffortResultIdentity
{
    /// <summary>
    /// Build a stable ClassificationId from the source efficiency snapshot ID.
    /// Format: ERCL|{sanitizedEfficiencyId}|{policyVersion}
    /// </summary>
    public static string BuildFromEfficiencyId(
        string efficiencyId,
        string policyVersion = EffortResultClassifierPolicyConfig.PolicyVersion)
    {
        var sanitized = string.IsNullOrWhiteSpace(efficiencyId)
            ? "Unknown"
            : efficiencyId.Replace("|", "_", StringComparison.Ordinal);
        return $"ERCL|{sanitized}|{policyVersion}";
    }
}

/// <summary>
/// Fingerprint gating EffortResultClassifierHost rebuilds.
/// Rebuild only when efficiency set InputFingerprint changes.
/// </summary>
public readonly struct EffortResultInputFingerprint : IEquatable<EffortResultInputFingerprint>
{
    private readonly bool _enabled;
    private readonly string _efficiencySetFp;
    private readonly string _policyVersion;

    public EffortResultInputFingerprint(bool enabled, string efficiencySetFp, string policyVersion)
    {
        _enabled = enabled;
        _efficiencySetFp = efficiencySetFp ?? "";
        _policyVersion = policyVersion ?? "";
    }

    public bool Equals(EffortResultInputFingerprint other) =>
        _enabled == other._enabled
        && string.Equals(_efficiencySetFp, other._efficiencySetFp, StringComparison.Ordinal)
        && string.Equals(_policyVersion, other._policyVersion, StringComparison.Ordinal);

    public override bool Equals(object? obj) =>
        obj is EffortResultInputFingerprint other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(_enabled, _efficiencySetFp, _policyVersion);

    public override string ToString() =>
        _enabled + "|" + _efficiencySetFp + "|" + _policyVersion;
}
