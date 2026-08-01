using System.Text.RegularExpressions;

namespace GC.AuctionFlow.Foundation;

/// <summary>
/// MRBS VER-001..003: AlgorithmVersion, ConfigVersion and DataSchemaVersion are three independent
/// SemVer fields. A record missing any is invalid — never defaulted. Every foundation snapshot and
/// hash references all three (TTS §9.4).
/// </summary>
public readonly record struct VersionStamp(
    string AlgorithmVersion,
    string ConfigVersion,
    string DataSchemaVersion)
{
    private static readonly Regex SemVer =
        new(@"^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.\-]+)?$", RegexOptions.Compiled);

    /// <summary>True only when all three are present and SemVer-shaped. Absence is invalid (VER-001).</summary>
    public bool IsComplete =>
        SemVer.IsMatch(AlgorithmVersion ?? "") &&
        SemVer.IsMatch(ConfigVersion ?? "") &&
        SemVer.IsMatch(DataSchemaVersion ?? "");

    /// <summary>Names of the missing/invalid version fields, for reason codes. Empty when complete.</summary>
    public IReadOnlyList<string> MissingFields()
    {
        var missing = new List<string>(3);
        if (!SemVer.IsMatch(AlgorithmVersion ?? "")) missing.Add(nameof(AlgorithmVersion));
        if (!SemVer.IsMatch(ConfigVersion ?? "")) missing.Add(nameof(ConfigVersion));
        if (!SemVer.IsMatch(DataSchemaVersion ?? "")) missing.Add(nameof(DataSchemaVersion));
        return missing;
    }

    /// <summary>Stable, order-fixed canonical string used inside the deterministic foundation hash.</summary>
    public string Canonical() =>
        $"alg={AlgorithmVersion};cfg={ConfigVersion};schema={DataSchemaVersion}";
}
