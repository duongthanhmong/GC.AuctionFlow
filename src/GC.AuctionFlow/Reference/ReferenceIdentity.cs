using System.Globalization;
using System.Text;

namespace GC.AuctionFlow.Reference;

/// <summary>
/// Deterministic ReferenceId. Zone is NOT part of identity — Developing levels may migrate.
/// Format: REF|{instrument}|{epoch}|{sourceId}|{referenceType}|{maturity}|REFERENCE_POLICY_V1
/// </summary>
public static class ReferenceIdentity
{
    public static string Build(
        string instrumentIdentity,
        string dataEpoch,
        string sourceId,
        ReferenceType type,
        ReferenceMaturity maturity,
        string policyVersion = ReferencePolicyConfig.PolicyVersion)
    {
        var sb = new StringBuilder(160);
        sb.Append("REF|");
        sb.Append(Sanitize(instrumentIdentity));
        sb.Append('|');
        sb.Append(Sanitize(dataEpoch));
        sb.Append('|');
        sb.Append(Sanitize(sourceId));
        sb.Append('|');
        sb.Append(type.ToString());
        sb.Append('|');
        sb.Append(maturity.ToString());
        sb.Append('|');
        sb.Append(string.IsNullOrWhiteSpace(policyVersion) ? ReferencePolicyConfig.PolicyVersion : policyVersion.Trim());
        return sb.ToString();
    }

    private static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Unknown";
        // Keep identity parseable — strip '|' that would break the format.
        return value.Trim().Replace("|", "_", StringComparison.Ordinal);
    }

    public static string FormatZoneKey(decimal zoneLow, decimal zoneHigh) =>
        zoneLow.ToString(CultureInfo.InvariantCulture) + "|" + zoneHigh.ToString(CultureInfo.InvariantCulture);
}
