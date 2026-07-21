using System.Text.Json;
using System.Text.Json.Serialization;
using Aos.LevelEngine.Interactions;
using Aos.LevelEngine.Levels;

namespace Aos.LevelEngine.Spool;

public enum LevelEnginePayloadType : byte
{
    LevelSnapshot = 7,
    InteractionRecord = 8,
    RotationRFreeze = 9,
    InteractionOutcome = 10
}

public static class InterpretationJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public static byte[] SerializeSnapshot(LevelEngineSnapshot snap) =>
        JsonSerializer.SerializeToUtf8Bytes(snap, Options);

    public static byte[] SerializeInteraction(LevelInteractionRecord r) =>
        JsonSerializer.SerializeToUtf8Bytes(r, Options);

    public static byte[] SerializeOutcome(InteractionOutcomeRecord r) =>
        JsonSerializer.SerializeToUtf8Bytes(r, Options);

    public static byte[] SerializeRotationR(RotationRState r) =>
        JsonSerializer.SerializeToUtf8Bytes(r, Options);
}
