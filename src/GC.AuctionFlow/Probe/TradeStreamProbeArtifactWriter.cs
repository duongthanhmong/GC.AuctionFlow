using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GC.AuctionFlow.Core;
using GC.AuctionFlow.Logging;

namespace GC.AuctionFlow.Probe;

/// <summary>
/// Deterministic JSON export with atomic rename + companion SHA-256 (hash not inside hashed bytes).
/// </summary>
public static class TradeStreamProbeArtifactWriter
{
    public static readonly JsonSerializerOptions JsonOptions = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var o = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };
        o.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return o;
    }

    public static string GetCapabilityDirectory()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            SpoolRoot.DirectoryName,
            "capability");
        return root;
    }

    public static string SanitizeInstrument(string? instrument)
    {
        if (string.IsNullOrWhiteSpace(instrument))
            return "Unknown";
        var sb = new StringBuilder(instrument.Length);
        foreach (var ch in instrument.Trim())
        {
            if (char.IsAsciiLetterOrDigit(ch) || ch is '_' or '-')
                sb.Append(ch);
            else
                sb.Append('_');
        }
        return sb.Length == 0 ? "Unknown" : sb.ToString();
    }

    public static string BuildFileName(string? instrument, DateTime utcTimestamp, Guid sessionId)
    {
        var ts = utcTimestamp.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
        var shortId = sessionId.ToString("N")[..8];
        return $"TradeStreamProbe_{SanitizeInstrument(instrument)}_{ts}_{shortId}.json";
    }

    public static TradeStreamProbeArtifactDto ToDto(TradeStreamProbeSnapshot snapshot) =>
        TradeStreamProbeArtifactDto.FromSnapshot(snapshot);

    /// <summary>
    /// Serialize → temp → flush/close → atomic rename → SHA-256 companion.
    /// Does not include the file's own hash inside the hashed JSON.
    /// </summary>
    public static ArtifactWriteResult WriteAtomic(TradeStreamProbeSnapshot snapshot, string? directory = null)
    {
        var dir = directory ?? GetCapabilityDirectory();
        Directory.CreateDirectory(dir);

        var instrument = snapshot.ObservedInstrument?.IdentityKey ?? snapshot.ExpectedInstrumentCode;
        var fileName = BuildFileName(instrument, snapshot.ArtifactIdentity.CreatedUtc, snapshot.ArtifactIdentity.SessionId);
        var finalPath = Path.Combine(dir, fileName);
        var tempPath = finalPath + ".tmp";
        var shaPath = finalPath + ".sha256";

        var dto = ToDto(snapshot);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(dto, JsonOptions);

        File.WriteAllBytes(tempPath, bytes);
        using (var fs = new FileStream(tempPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            fs.Flush(true);
        }

        File.Move(tempPath, finalPath, overwrite: true);

        var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(finalPath)));
        var companion = $"{hash}  {Path.GetFileName(finalPath)}{Environment.NewLine}";
        File.WriteAllText(shaPath, companion, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        return new ArtifactWriteResult(finalPath, shaPath, hash, bytes.Length);
    }
}

public sealed record ArtifactWriteResult(
    string JsonPath,
    string Sha256Path,
    string Sha256Hex,
    int ByteLength);

/// <summary>DTO for deterministic serialization. No self-referential content hash field.</summary>
public sealed class TradeStreamProbeArtifactDto
{
    public string SchemaVersion { get; set; } = "";
    public string ProbeVersion { get; set; } = "";
    public string SessionId { get; set; } = "";
    public DateTime CreatedUtc { get; set; }
    public string ContinuityDisclaimer { get; set; } = "";
    public DataSourceMode DeclaredDataSourceMode { get; set; }
    public DataSourceModeProvenance DataSourceModeProvenance { get; set; }
    public string? ExpectedInstrumentCode { get; set; }
    public ObservedInstrumentDto? ObservedInstrument { get; set; }
    public string? LastGateReason { get; set; }
    public string? GateReason { get; set; }
    public bool CaptureAuthorized { get; set; }
    public bool LiveTradeCapabilityClaim { get; set; }
    public CallbackObservationReport CallbackObservations { get; set; } = null!;
    public TradeStreamProbeCounterSnapshot Counters { get; set; } = null!;
    public TradeStreamOverlapSnapshot Overlap { get; set; } = null!;
    public List<NewTradeObservationDto> NewTradeSamples { get; set; } = new();
    public List<CumulativeTradeObservationDto> CumulativeSamples { get; set; } = new();
    public List<string> KnownLimitations { get; set; } = new();
    public List<string> IntegrityEvents { get; set; } = new();
    public int QueueCapacity { get; set; }
    public int MaxNewTradeSamples { get; set; }
    public int MaxCumulativeSamples { get; set; }
    public string CallbackThreading { get; set; } = "Unknown";
    public string ClockSemantics { get; set; } = "Unknown";
    public string AuthoritativeStream { get; set; } = "None";
    public bool FingerprintsAreDiagnosticsOnly { get; set; } = true;
    public string FingerprintAlgorithm { get; set; } =
        "InvariantCulture pipe-joined fields; diagnostic only; not trade ID; not native sequence; no deletion.";

    public static TradeStreamProbeArtifactDto FromSnapshot(TradeStreamProbeSnapshot s) => new()
    {
        SchemaVersion = s.ArtifactIdentity.SchemaVersion,
        ProbeVersion = s.ArtifactIdentity.ProbeVersion,
        SessionId = s.ArtifactIdentity.SessionId.ToString("D"),
        CreatedUtc = DateTime.SpecifyKind(s.ArtifactIdentity.CreatedUtc, DateTimeKind.Utc),
        ContinuityDisclaimer = s.ArtifactIdentity.ContinuityDisclaimer,
        DeclaredDataSourceMode = s.DeclaredDataSourceMode,
        DataSourceModeProvenance = s.DataSourceModeProvenance,
        ExpectedInstrumentCode = s.ExpectedInstrumentCode,
        ObservedInstrument = s.ObservedInstrument is null ? null : ObservedInstrumentDto.From(s.ObservedInstrument),
        LastGateReason = s.LastGateReason,
        GateReason = s.GateReason,
        CaptureAuthorized = s.CaptureAuthorized,
        LiveTradeCapabilityClaim = s.LiveTradeCapabilityClaim,
        CallbackObservations = s.CallbackObservations,
        Counters = s.Counters,
        Overlap = s.Overlap,
        NewTradeSamples = s.NewTradeSamples.Select(NewTradeObservationDto.From).ToList(),
        CumulativeSamples = s.CumulativeSamples.Select(CumulativeTradeObservationDto.From).ToList(),
        KnownLimitations = s.KnownLimitations.ToList(),
        IntegrityEvents = s.IntegrityEvents.ToList(),
        QueueCapacity = s.QueueCapacity,
        MaxNewTradeSamples = s.MaxNewTradeSamples,
        MaxCumulativeSamples = s.MaxCumulativeSamples,
        CallbackThreading = s.CallbackThreading,
        ClockSemantics = s.ClockSemantics,
        AuthoritativeStream = s.AuthoritativeStream,
        FingerprintsAreDiagnosticsOnly = s.FingerprintsAreDiagnosticsOnly
    };
}

public sealed class ObservedInstrumentDto
{
    public string? SecurityCode { get; set; }
    public string? SecurityId { get; set; }
    public string? Instrument { get; set; }
    public string? Exchange { get; set; }
    public DateTime? Expiration { get; set; }
    public decimal? TickSize { get; set; }
    public string? UnderlyingSecurity { get; set; }
    public string? InstrumentInfoInstrument { get; set; }
    public string? InstrumentInfoExchange { get; set; }
    public decimal? InstrumentInfoTickSize { get; set; }
    public string? InstrumentInfoTimeZone { get; set; }
    public string IdentityKey { get; set; } = "Unknown";

    public static ObservedInstrumentDto From(ObservedInstrumentSnapshot s) => new()
    {
        SecurityCode = s.SecurityCode,
        SecurityId = s.SecurityId,
        Instrument = s.Instrument,
        Exchange = s.Exchange,
        Expiration = s.Expiration,
        TickSize = s.TickSize,
        UnderlyingSecurity = s.UnderlyingSecurity,
        InstrumentInfoInstrument = s.InstrumentInfoInstrument,
        InstrumentInfoExchange = s.InstrumentInfoExchange,
        InstrumentInfoTickSize = s.InstrumentInfoTickSize,
        InstrumentInfoTimeZone = s.InstrumentInfoTimeZone,
        IdentityKey = s.IdentityKey
    };
}

public sealed class NewTradeObservationDto
{
    public TradeCallbackSource CallbackSource { get; set; }
    public long LocalMonotonicSequence { get; set; }
    public long SourceTimeTicks { get; set; }
    public DateTimeKind SourceDateTimeKind { get; set; }
    public DateTime ReceiveUtc { get; set; }
    public long ReceiveStopwatchTimestamp { get; set; }
    public string ObservedInstrumentIdentityKey { get; set; } = "";
    public string CoreDiagnosticFingerprint { get; set; } = "";
    public string? ExtendedDiagnosticFingerprint { get; set; }
    public decimal Price { get; set; }
    public decimal Volume { get; set; }
    public decimal OriginPrice { get; set; }
    public string Direction { get; set; } = "";
    public string DataType { get; set; } = "";
    public bool IsAsk { get; set; }
    public bool IsBid { get; set; }
    public long? ExchangeOrderId { get; set; }
    public long? AggressorExchangeOrderId { get; set; }
    public decimal? OpenInterest { get; set; }

    public static NewTradeObservationDto From(NewTradeObservation o) => new()
    {
        CallbackSource = o.CallbackSource,
        LocalMonotonicSequence = o.LocalMonotonicSequence,
        SourceTimeTicks = o.SourceTimeTicks,
        SourceDateTimeKind = o.SourceDateTimeKind,
        ReceiveUtc = DateTime.SpecifyKind(o.ReceiveUtc, DateTimeKind.Utc),
        ReceiveStopwatchTimestamp = o.ReceiveStopwatchTimestamp,
        ObservedInstrumentIdentityKey = o.ObservedInstrumentIdentityKey,
        CoreDiagnosticFingerprint = o.CoreDiagnosticFingerprint,
        ExtendedDiagnosticFingerprint = o.ExtendedDiagnosticFingerprint,
        Price = o.Price,
        Volume = o.Volume,
        OriginPrice = o.OriginPrice,
        Direction = o.Direction,
        DataType = o.DataType,
        IsAsk = o.IsAsk,
        IsBid = o.IsBid,
        ExchangeOrderId = o.ExchangeOrderId,
        AggressorExchangeOrderId = o.AggressorExchangeOrderId,
        OpenInterest = o.OpenInterest
    };
}

public sealed class CumulativeTradeObservationDto
{
    public TradeCallbackSource CallbackSource { get; set; }
    public long LocalMonotonicSequence { get; set; }
    public long SourceTimeTicks { get; set; }
    public DateTimeKind SourceDateTimeKind { get; set; }
    public DateTime ReceiveUtc { get; set; }
    public long ReceiveStopwatchTimestamp { get; set; }
    public string ObservedInstrumentIdentityKey { get; set; } = "";
    public string ValueFingerprint { get; set; } = "";
    public decimal Volume { get; set; }
    public decimal FirstPrice { get; set; }
    public decimal LastPrice { get; set; }
    public string Direction { get; set; } = "";
    public int CopiedTickCount { get; set; }
    public long? ProcessLocalInstanceId { get; set; }
    public bool ProcessLocalInstanceIdObserved { get; set; }
    public List<ConstituentPrintSummaryDto> ConstituentPrintSummaries { get; set; } = new();

    public static CumulativeTradeObservationDto From(CumulativeTradeObservation o) => new()
    {
        CallbackSource = o.CallbackSource,
        LocalMonotonicSequence = o.LocalMonotonicSequence,
        SourceTimeTicks = o.SourceTimeTicks,
        SourceDateTimeKind = o.SourceDateTimeKind,
        ReceiveUtc = DateTime.SpecifyKind(o.ReceiveUtc, DateTimeKind.Utc),
        ReceiveStopwatchTimestamp = o.ReceiveStopwatchTimestamp,
        ObservedInstrumentIdentityKey = o.ObservedInstrumentIdentityKey,
        ValueFingerprint = o.ValueFingerprint,
        Volume = o.Volume,
        FirstPrice = o.FirstPrice,
        LastPrice = o.LastPrice,
        Direction = o.Direction,
        CopiedTickCount = o.CopiedTickCount,
        ProcessLocalInstanceId = o.ProcessLocalInstanceId,
        ProcessLocalInstanceIdObserved = o.ProcessLocalInstanceIdObserved,
        ConstituentPrintSummaries = o.ConstituentPrintSummaries
            .Select(c => new ConstituentPrintSummaryDto
            {
                SourceTimeTicks = c.SourceTimeTicks,
                Price = c.Price,
                Volume = c.Volume,
                Direction = c.Direction,
                DataType = c.DataType,
                CoreFingerprint = c.CoreFingerprint
            }).ToList()
    };
}

public sealed class ConstituentPrintSummaryDto
{
    public long SourceTimeTicks { get; set; }
    public decimal Price { get; set; }
    public decimal Volume { get; set; }
    public string Direction { get; set; } = "";
    public string DataType { get; set; } = "";
    public string CoreFingerprint { get; set; } = "";
}
