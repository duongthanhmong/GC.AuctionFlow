using System.Text.Json;
using System.Text.Json.Serialization;
using GC.AuctionFlow.Core;

namespace GC.AuctionFlow.Data;

/// <summary>
/// Deterministic System.Text.Json settings for capability contracts.
/// Enums as stable strings; no polymorphic converters.
/// </summary>
public static class CapabilityJson
{
    public static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            AllowTrailingCommas = false,
            ReadCommentHandling = JsonCommentHandling.Disallow
        };

        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }

    public static string SerializeSnapshot(CapabilitySnapshotDto dto) =>
        JsonSerializer.Serialize(dto, CreateOptions());

    public static CapabilitySnapshotDto? DeserializeSnapshot(string json) =>
        JsonSerializer.Deserialize<CapabilitySnapshotDto>(json, CreateOptions());
}

/// <summary>
/// DTO for round-trip serialization. Validated into <see cref="CapabilitySnapshot"/> via TryCreate.
/// </summary>
public sealed class CapabilitySnapshotDto
{
    public string SchemaVersion { get; set; } = "";
    public string ProbeVersion { get; set; } = "";
    public Guid SessionId { get; set; }
    public DateTimeOffset GeneratedUtc { get; set; }
    public string InstrumentId { get; set; } = "";
    public DataState DataState { get; set; }
    public DataSourceMode DataSourceMode { get; set; }
    public DataSourceModeProvenance DataSourceModeProvenance { get; set; }
    public List<CapabilityCellDto> Capabilities { get; set; } = new();
    public List<string> KnownLimitations { get; set; } = new();
    public List<EvidenceArtifactIdentityDto> EvidenceIdentities { get; set; } = new();

    public static CapabilitySnapshotDto FromSnapshot(CapabilitySnapshot snapshot) =>
        new()
        {
            SchemaVersion = snapshot.SchemaVersion,
            ProbeVersion = snapshot.ProbeVersion,
            SessionId = snapshot.SessionId,
            GeneratedUtc = snapshot.GeneratedUtc,
            InstrumentId = snapshot.InstrumentId,
            DataState = snapshot.DataState,
            DataSourceMode = snapshot.DataSourceMode,
            DataSourceModeProvenance = snapshot.DataSourceModeProvenance,
            Capabilities = snapshot.Capabilities.Values
                .OrderBy(c => c.Kind.ToString(), StringComparer.Ordinal)
                .Select(CapabilityCellDto.FromCell)
                .ToList(),
            KnownLimitations = snapshot.KnownLimitations.ToList(),
            EvidenceIdentities = snapshot.EvidenceIdentities
                .Select(EvidenceArtifactIdentityDto.FromIdentity)
                .ToList()
        };

    public SchemaValidationResult TryToSnapshot(out CapabilitySnapshot? snapshot) =>
        CapabilitySnapshot.TryCreate(
            SchemaVersion,
            ProbeVersion,
            SessionId,
            GeneratedUtc,
            InstrumentId,
            DataState,
            DataSourceMode,
            DataSourceModeProvenance,
            Capabilities.Select(c => c.ToCell()),
            KnownLimitations,
            EvidenceIdentities.Select(e => e.ToIdentity()),
            out snapshot);
}

public sealed class CapabilityCellDto
{
    public CapabilityKind Kind { get; set; }
    public CapabilityAvailability Availability { get; set; }
    public DataCoverage Coverage { get; set; }
    public ComputabilityState Computability { get; set; }
    public FidelityState Fidelity { get; set; }
    public SequenceEvidence Sequence { get; set; }
    public EvidenceProvenance Provenance { get; set; }
    public DateTimeOffset? ObservedUtc { get; set; }
    public List<string> NotesOrLimitationCodes { get; set; } = new();
    public MboLifecycleState? MboLifecycle { get; set; }

    public static CapabilityCellDto FromCell(CapabilityCell cell) =>
        new()
        {
            Kind = cell.Kind,
            Availability = cell.Availability,
            Coverage = cell.Coverage,
            Computability = cell.Computability,
            Fidelity = cell.Fidelity,
            Sequence = cell.Sequence,
            Provenance = cell.Provenance,
            ObservedUtc = cell.ObservedUtc,
            NotesOrLimitationCodes = cell.NotesOrLimitationCodes.ToList(),
            MboLifecycle = cell.MboLifecycle
        };

    public CapabilityCell ToCell() =>
        new(
            Kind,
            Availability,
            Coverage,
            Computability,
            Fidelity,
            Sequence,
            Provenance,
            ObservedUtc,
            NotesOrLimitationCodes.ToArray(),
            MboLifecycle);
}

public sealed class EvidenceArtifactIdentityDto
{
    public string ArtifactNameOrPath { get; set; } = "";
    public long? FileSizeBytes { get; set; }
    public string? Sha256Hex { get; set; }
    public string? SchemaVersion { get; set; }
    public string? ProbeVersion { get; set; }
    public string? SessionId { get; set; }
    public DateTimeOffset? GeneratedUtc { get; set; }
    public string? InstrumentId { get; set; }

    public static EvidenceArtifactIdentityDto FromIdentity(EvidenceArtifactIdentity id) =>
        new()
        {
            ArtifactNameOrPath = id.ArtifactNameOrPath,
            FileSizeBytes = id.FileSizeBytes,
            Sha256Hex = id.Sha256Hex,
            SchemaVersion = id.SchemaVersion,
            ProbeVersion = id.ProbeVersion,
            SessionId = id.SessionId,
            GeneratedUtc = id.GeneratedUtc,
            InstrumentId = id.InstrumentId
        };

    public EvidenceArtifactIdentity ToIdentity() =>
        new(
            ArtifactNameOrPath,
            FileSizeBytes,
            Sha256Hex,
            SchemaVersion,
            ProbeVersion,
            SessionId,
            GeneratedUtc,
            InstrumentId);
}
