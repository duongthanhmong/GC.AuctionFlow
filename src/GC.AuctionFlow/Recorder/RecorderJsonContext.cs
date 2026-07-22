using System.Text.Json;
using System.Text.Json.Serialization;
using GC.AuctionFlow.Recorder.Payloads;

namespace GC.AuctionFlow.Recorder;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(RawEventEnvelope))]
[JsonSerializable(typeof(RawEventPayload))]
[JsonSerializable(typeof(ObservedInstrumentIdentity))]
[JsonSerializable(typeof(SegmentHeaderRecord))]
[JsonSerializable(typeof(SegmentFooterRecord))]
[JsonSerializable(typeof(SessionManifestRecord))]
[JsonSerializable(typeof(ManifestSegmentRecord))]
[JsonSerializable(typeof(DisabledStreamRecord))]
[JsonSerializable(typeof(CapabilityClaimsForcedFalse))]
[JsonSerializable(typeof(RecorderCountersSnapshot))]
[JsonSerializable(typeof(NewTradePayload))]
[JsonSerializable(typeof(CumulativeTradeNewPayload))]
[JsonSerializable(typeof(CumulativeTradeUpdatePayload))]
[JsonSerializable(typeof(DepthPayload))]
[JsonSerializable(typeof(BestBidAskPayload))]
[JsonSerializable(typeof(DomSnapshotRequestPayload))]
[JsonSerializable(typeof(DomSnapshotItemPayload))]
[JsonSerializable(typeof(DomSnapshotLocalEnumerationResultPayload))]
[JsonSerializable(typeof(MboPayload))]
[JsonSerializable(typeof(RecorderLifecyclePayload))]
[JsonSerializable(typeof(RecorderIntegrityPayload))]
internal partial class RecorderJsonContext : JsonSerializerContext
{
}

public static class RecorderJson
{
    internal static RecorderJsonContext Context { get; } = RecorderJsonContext.Default;

    public static byte[] SerializeEnvelope(RawEventEnvelope envelope) =>
        JsonSerializer.SerializeToUtf8Bytes(envelope, Context.RawEventEnvelope);

    public static byte[] SerializeHeader(SegmentHeaderRecord header) =>
        JsonSerializer.SerializeToUtf8Bytes(header, Context.SegmentHeaderRecord);

    public static byte[] SerializeFooter(SegmentFooterRecord footer) =>
        JsonSerializer.SerializeToUtf8Bytes(footer, Context.SegmentFooterRecord);

    public static byte[] SerializeManifest(SessionManifestRecord manifest) =>
        JsonSerializer.SerializeToUtf8Bytes(manifest, Context.SessionManifestRecord);

    public static RawEventEnvelope? DeserializeEnvelope(ReadOnlySpan<byte> utf8) =>
        JsonSerializer.Deserialize(utf8, Context.RawEventEnvelope);

    public static SegmentHeaderRecord? DeserializeHeader(ReadOnlySpan<byte> utf8) =>
        JsonSerializer.Deserialize(utf8, Context.SegmentHeaderRecord);

    public static SegmentFooterRecord? DeserializeFooter(ReadOnlySpan<byte> utf8) =>
        JsonSerializer.Deserialize(utf8, Context.SegmentFooterRecord);

    public static SessionManifestRecord? DeserializeManifest(ReadOnlySpan<byte> utf8) =>
        JsonSerializer.Deserialize(utf8, Context.SessionManifestRecord);
}
