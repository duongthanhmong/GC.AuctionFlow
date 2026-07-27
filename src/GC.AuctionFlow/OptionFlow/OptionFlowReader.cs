using System.IO;
using GC.AuctionFlow.Data;

namespace GC.AuctionFlow.OptionFlow;

/// <summary>Outcome of one read: a context (or null) plus a human diagnostic.</summary>
public sealed record OptionFlowReadOutcome(GexContext? Context, string Diagnostic)
{
    public bool HasContext => Context is not null;
}

/// <summary>
/// Reads <c>artifacts/optionflow/&lt;PRODUCT&gt;/levels.json</c> and returns a
/// validated <see cref="GexContext"/>, or null with a diagnostic. Returning null
/// is a first-class, expected outcome (missing / stale / schema mismatch / parse
/// error): callers MUST behave identically to the no-GEX path in that case.
///
/// Pure IO + parse; no ATAS dependency, so it is unit-testable in isolation.
/// </summary>
public static class OptionFlowReader
{
    public static string LevelsPath(string dataRoot, string product) =>
        Path.Combine(dataRoot, product, "levels.json");

    /// <param name="now">Injected clock (testable).</param>
    /// <param name="maxAge">Freshness window; older ⇒ null (stale never renders).</param>
    public static OptionFlowReadOutcome Read(
        string dataRoot, string product, DateTimeOffset now, TimeSpan maxAge)
    {
        var path = LevelsPath(dataRoot, product);

        if (!File.Exists(path))
            return new OptionFlowReadOutcome(null, $"OptionFlow file absent: {path}");

        string json;
        try
        {
            // Shared, non-locking read: the sidecar writes atomically (tmp+replace),
            // so we never block it and never see a torn file.
            using var fs = new FileStream(
                path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs);
            json = sr.ReadToEnd();
        }
        catch (IOException ex)
        {
            return new OptionFlowReadOutcome(null, $"OptionFlow read IO error: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return new OptionFlowReadOutcome(null, $"OptionFlow read access error: {ex.Message}");
        }

        OptionFlowDocDto? dto;
        try
        {
            dto = OptionFlowJson.Deserialize(json);
        }
        catch (System.Text.Json.JsonException ex)
        {
            return new OptionFlowReadOutcome(null, $"OptionFlow parse error: {ex.Message}");
        }

        SchemaValidationResult validation = GexContext.TryCreate(dto, out var context);
        if (!validation.IsValid || context is null)
            return new OptionFlowReadOutcome(null,
                "OptionFlow invalid: " + string.Join("; ", validation.Errors));

        long ageSeconds = now.ToUnixTimeSeconds() - context.PublishedAtEpoch;
        if (ageSeconds > (long)maxAge.TotalSeconds)
            return new OptionFlowReadOutcome(null,
                $"OptionFlow stale: {ageSeconds}s old (max {(long)maxAge.TotalSeconds}s)");
        if (ageSeconds < -300)
            return new OptionFlowReadOutcome(null,
                $"OptionFlow timestamp in the future by {-ageSeconds}s — rejected");

        return new OptionFlowReadOutcome(context, $"OptionFlow LIVE ({ageSeconds}s old)");
    }
}
