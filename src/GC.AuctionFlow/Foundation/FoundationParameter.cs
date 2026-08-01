using System.Security.Cryptography;
using System.Text;

namespace GC.AuctionFlow.Foundation;

/// <summary>
/// TTS §9: a behavioral configuration value with governance metadata and a deterministic hash. No
/// inline constant may act as approved production configuration merely by existing. A parameter whose
/// approval is not <see cref="ParameterApprovalStatus.ApprovedForProduction"/> can never unlock
/// AnalysisReady or a confirmed conclusion.
/// </summary>
public sealed record FoundationParameter(
    string ParameterId,
    string Name,
    string Module,
    string Value,
    string Unit,
    ParameterSourceType SourceType,
    ParameterApprovalStatus ApprovalStatus,
    string Owner,
    string KdkRefs,
    string MrbsRefs,
    string ConfigVersion)
{
    /// <summary>True only for a production-approved parameter (TTS §9.2).</summary>
    public bool IsProductionApproved => ApprovalStatus == ParameterApprovalStatus.ApprovedForProduction;

    /// <summary>Readable canonical representation, order-fixed, for the deterministic hash.</summary>
    public string Canonical() =>
        $"id={ParameterId};name={Name};module={Module};value={Value};unit={Unit};src={(int)SourceType};approval={(int)ApprovalStatus};cfg={ConfigVersion}";

    /// <summary>Deterministic SHA-256 of the canonical representation (TTS §9.1).</summary>
    public string Hash()
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(Canonical()));
        return Convert.ToHexStringLower(bytes);
    }
}

/// <summary>
/// The minimal, versioned M1 foundation configuration. Every behavioral value the milestone touches
/// (late-event grace, warm-up minimum, history window) is a governed <see cref="FoundationParameter"/>,
/// not an inline seed. All ship as PROPOSED_SEED / Proposed — so, by rule, they cannot back a
/// production-Ready gate: <see cref="AllProductionApproved"/> is false and the lifecycle keeps
/// session/confirmation blocked until an owner approves them. No general config framework is added.
/// </summary>
public sealed class FoundationConfig
{
    public string ConfigVersion { get; }
    public IReadOnlyList<FoundationParameter> Parameters { get; }

    public FoundationConfig(string configVersion, IReadOnlyList<FoundationParameter>? parameters = null)
    {
        ConfigVersion = configVersion;
        Parameters = parameters ?? DefaultProposed(configVersion);
    }

    /// <summary>The M1 parameters, all PROPOSED_SEED — present for transparency, not for production authority.</summary>
    public static IReadOnlyList<FoundationParameter> DefaultProposed(string configVersion) => new[]
    {
        new FoundationParameter("FND-LATE-GRACE-MS", "LateEventGraceMs", "InputFoundation",
            "0", "milliseconds", ParameterSourceType.PROPOSED_SEED, ParameterApprovalStatus.Proposed,
            "unassigned", "KDK Ch40", "MRBS §40.3", configVersion),
        new FoundationParameter("FND-WARMUP-MIN-EVENTS", "WarmupMinEvents", "InputFoundation",
            "1", "count", ParameterSourceType.PROPOSED_SEED, ParameterApprovalStatus.Proposed,
            "unassigned", "KDK Ch89", "MRBS §18 DQ-003", configVersion),
        new FoundationParameter("FND-HISTORY-WINDOW-BARS", "HistoryWindowBars", "InputFoundation",
            "0", "bars", ParameterSourceType.PROPOSED_SEED, ParameterApprovalStatus.Proposed,
            "unassigned", "KDK Ch20-23", "MRBS §26 Phase1", configVersion),
    };

    /// <summary>
    /// True only if EVERY behavioral parameter is production-approved. M1 ships all as Proposed, so this
    /// is false — which is exactly why no proposed seed can silently unlock AnalysisReady.
    /// </summary>
    public bool AllProductionApproved => Parameters.Count > 0 && Parameters.All(p => p.IsProductionApproved);

    /// <summary>Deterministic SHA-256 over all parameter hashes + config version, order-fixed.</summary>
    public string Hash()
    {
        var sb = new StringBuilder();
        sb.Append("cfg=").Append(ConfigVersion).Append(';');
        foreach (var p in Parameters.OrderBy(p => p.ParameterId, StringComparer.Ordinal))
            sb.Append(p.Hash()).Append(';');
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexStringLower(bytes);
    }
}
