using System.Text.Json;
using System.Text.Json.Serialization;

namespace GC.AuctionFlow.OptionFlow;

/// <summary>
/// Deterministic System.Text.Json settings for the OptionFlow sidecar contract
/// (<c>gcae-optionflow-v1</c>). The sidecar writes snake_case keys, so this uses
/// SnakeCaseLower — distinct from the CamelCase capability contracts. Read-only;
/// the DLL never writes these files.
/// </summary>
public static class OptionFlowJson
{
    public static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = false,
            ReadCommentHandling = JsonCommentHandling.Disallow,
            AllowTrailingCommas = false,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
        };
        return options;
    }

    public static OptionFlowDocDto? Deserialize(string json) =>
        JsonSerializer.Deserialize<OptionFlowDocDto>(json, CreateOptions());
}
