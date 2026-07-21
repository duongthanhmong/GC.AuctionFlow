using System.Collections.Immutable;
using GC.AuctionFlow.Core;

namespace GC.AuctionFlow.Data;

/// <summary>
/// Immutable capability snapshot (spec §7 DataCapabilityEngine → CapabilitySnapshot).
/// Duplicate <see cref="CapabilityKind"/> entries are rejected at construction.
/// </summary>
public sealed class CapabilitySnapshot
{
    private readonly ImmutableDictionary<CapabilityKind, CapabilityCell> _cells;

    private CapabilitySnapshot(
        string schemaVersion,
        string probeVersion,
        Guid sessionId,
        DateTimeOffset generatedUtc,
        string instrumentId,
        DataState dataState,
        DataSourceMode dataSourceMode,
        DataSourceModeProvenance dataSourceModeProvenance,
        ImmutableDictionary<CapabilityKind, CapabilityCell> cells,
        ImmutableArray<string> knownLimitations,
        ImmutableArray<EvidenceArtifactIdentity> evidenceIdentities)
    {
        SchemaVersion = schemaVersion;
        ProbeVersion = probeVersion;
        SessionId = sessionId;
        GeneratedUtc = generatedUtc;
        InstrumentId = instrumentId;
        DataState = dataState;
        DataSourceMode = dataSourceMode;
        DataSourceModeProvenance = dataSourceModeProvenance;
        _cells = cells;
        KnownLimitations = knownLimitations;
        EvidenceIdentities = evidenceIdentities;
    }

    public string SchemaVersion { get; }
    public string ProbeVersion { get; }
    public Guid SessionId { get; }
    public DateTimeOffset GeneratedUtc { get; }
    public string InstrumentId { get; }
    public DataState DataState { get; }
    public DataSourceMode DataSourceMode { get; }
    public DataSourceModeProvenance DataSourceModeProvenance { get; }
    public IReadOnlyDictionary<CapabilityKind, CapabilityCell> Capabilities => _cells;
    public IReadOnlyList<string> KnownLimitations { get; }
    public IReadOnlyList<EvidenceArtifactIdentity> EvidenceIdentities { get; }

    public bool TryGet(CapabilityKind kind, out CapabilityCell cell) =>
        _cells.TryGetValue(kind, out cell!);

    /// <summary>
    /// Builds a snapshot or returns validation errors. Does not invent capabilities.
    /// </summary>
    public static SchemaValidationResult TryCreate(
        string schemaVersion,
        string probeVersion,
        Guid sessionId,
        DateTimeOffset generatedUtc,
        string instrumentId,
        DataState dataState,
        DataSourceMode dataSourceMode,
        DataSourceModeProvenance dataSourceModeProvenance,
        IEnumerable<CapabilityCell> cells,
        IEnumerable<string>? knownLimitations,
        IEnumerable<EvidenceArtifactIdentity>? evidenceIdentities,
        out CapabilitySnapshot? snapshot)
    {
        snapshot = null;
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(schemaVersion))
            errors.Add("SchemaVersion is required.");
        else if (!IsSupportedSchemaVersion(schemaVersion))
            errors.Add($"Unsupported or invalid SchemaVersion '{schemaVersion}'.");

        if (string.IsNullOrWhiteSpace(probeVersion))
            errors.Add("ProbeVersion is required.");

        if (sessionId == Guid.Empty)
            errors.Add("SessionId must be a non-empty GUID.");

        if (string.IsNullOrWhiteSpace(instrumentId))
            errors.Add("InstrumentId is required and must remain explicit.");

        var modeCheck = CapabilitySemantics.ValidateDataSourceModeDeclaration(
            dataSourceMode, dataSourceModeProvenance);
        if (!modeCheck.IsValid)
            errors.AddRange(modeCheck.Errors);

        var map = ImmutableDictionary.CreateBuilder<CapabilityKind, CapabilityCell>();
        foreach (var cell in cells)
        {
            if (map.ContainsKey(cell.Kind))
            {
                errors.Add($"Duplicate CapabilityKind '{cell.Kind}' is rejected.");
                continue;
            }

            var cellCheck = CapabilitySemantics.ValidateCell(cell);
            if (!cellCheck.IsValid)
                errors.AddRange(cellCheck.Errors);

            map[cell.Kind] = cell with
            {
                NotesOrLimitationCodes = cell.NotesOrLimitationCodes.ToImmutableArray()
            };
        }

        var evidenceList = ImmutableArray.CreateBuilder<EvidenceArtifactIdentity>();
        if (evidenceIdentities is not null)
        {
            foreach (var evidence in evidenceIdentities)
            {
                var ev = evidence.ValidateContract();
                if (!ev.IsValid)
                    errors.AddRange(ev.Errors);
                evidenceList.Add(evidence);
            }
        }

        if (errors.Count > 0)
            return SchemaValidationResult.Fail(errors);

        snapshot = new CapabilitySnapshot(
            schemaVersion.Trim(),
            probeVersion.Trim(),
            sessionId,
            generatedUtc,
            instrumentId.Trim(),
            dataState,
            dataSourceMode,
            dataSourceModeProvenance,
            map.ToImmutable(),
            (knownLimitations ?? Array.Empty<string>()).ToImmutableArray(),
            evidenceList.ToImmutable());

        return SchemaValidationResult.Ok();
    }

    public static bool IsSupportedSchemaVersion(string schemaVersion) =>
        string.Equals(schemaVersion.Trim(), CapabilitySchemaVersions.SchemaVersion, StringComparison.Ordinal);
}
