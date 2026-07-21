namespace GC.AuctionFlow.Data;

/// <summary>Contract validation outcome. No confidence score.</summary>
public sealed record SchemaValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors)
{
    public static SchemaValidationResult Ok() =>
        new(true, Array.Empty<string>());

    public static SchemaValidationResult Fail(params string[] errors) =>
        new(false, errors);

    public static SchemaValidationResult Fail(IEnumerable<string> errors) =>
        new(false, errors.ToArray());
}
