using System.Text.RegularExpressions;

namespace GC.AuctionFlow.Data;

/// <summary>
/// Identity of a capability evidence artifact. Does not read or hash files in P0-03.
/// </summary>
public sealed record EvidenceArtifactIdentity(
    string ArtifactNameOrPath,
    long? FileSizeBytes,
    string? Sha256Hex,
    string? SchemaVersion,
    string? ProbeVersion,
    string? SessionId,
    DateTimeOffset? GeneratedUtc,
    string? InstrumentId)
{
    private static readonly Regex Sha256HexPattern = new(
        "^[0-9a-fA-F]{64}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>True when Sha256Hex is present and exactly 64 hexadecimal characters.</summary>
    public bool HasValidSha256Format =>
        Sha256Hex is not null && Sha256HexPattern.IsMatch(Sha256Hex);

    public SchemaValidationResult ValidateContract()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(ArtifactNameOrPath))
            errors.Add("ArtifactNameOrPath is required.");

        if (FileSizeBytes is < 0)
            errors.Add("FileSizeBytes cannot be negative.");

        // null = omitted (allowed). Empty or malformed = rejected.
        if (Sha256Hex is not null && !HasValidSha256Format)
            errors.Add("Sha256Hex must be exactly 64 hexadecimal characters when provided.");

        if (string.IsNullOrWhiteSpace(InstrumentId))
            errors.Add("InstrumentId is required and must remain explicit.");

        return errors.Count == 0
            ? SchemaValidationResult.Ok()
            : SchemaValidationResult.Fail(errors);
    }

    /// <summary>
    /// ES evidence cannot be treated as GC evidence by rewriting InstrumentId.
    /// </summary>
    public static SchemaValidationResult RejectInstrumentRelabel(
        EvidenceArtifactIdentity original,
        string claimedInstrumentId)
    {
        if (string.IsNullOrWhiteSpace(original.InstrumentId))
            return SchemaValidationResult.Fail("Original InstrumentId is missing.");

        if (string.Equals(original.InstrumentId, claimedInstrumentId, StringComparison.OrdinalIgnoreCase))
            return SchemaValidationResult.Ok();

        return SchemaValidationResult.Fail(
            $"{Core.KnownLimitationCodes.EsEvidenceNotGcProof}: cannot relabel InstrumentId " +
            $"'{original.InstrumentId}' as '{claimedInstrumentId}'.");
    }
}
