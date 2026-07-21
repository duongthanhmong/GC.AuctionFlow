using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GC.AuctionFlow.Logging;

namespace GC.AuctionFlow.Probe;

public static class DomSemanticsProbeArtifactWriter
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

    public static string GetCapabilityDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            SpoolRoot.DirectoryName,
            "capability");

    public static string SanitizeInstrument(string? instrument)
    {
        if (string.IsNullOrWhiteSpace(instrument)) return "Unknown";
        var sb = new StringBuilder(instrument.Length);
        foreach (var ch in instrument.Trim())
            sb.Append(char.IsAsciiLetterOrDigit(ch) || ch is '_' or '-' ? ch : '_');
        return sb.Length == 0 ? "Unknown" : sb.ToString();
    }

    public static string BuildFileName(string? instrument, DateTime utc, Guid sessionId)
    {
        var ts = utc.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
        return $"DomSemanticsProbe_{SanitizeInstrument(instrument)}_{ts}_{sessionId.ToString("N")[..8]}.json";
    }

    public static ArtifactWriteResult WriteAtomic(DomSemanticsProbeSnapshot snapshot, string? directory = null)
    {
        var dir = directory ?? GetCapabilityDirectory();
        Directory.CreateDirectory(dir);
        var instrument = snapshot.ObservedInstrument?.IdentityKey ?? snapshot.ExpectedInstrumentCode;
        var finalPath = Path.Combine(dir, BuildFileName(instrument, snapshot.CreatedUtc, snapshot.SessionId));
        var tempPath = finalPath + ".tmp";
        var shaPath = finalPath + ".sha256";

        var bytes = JsonSerializer.SerializeToUtf8Bytes(snapshot, JsonOptions);
        File.WriteAllBytes(tempPath, bytes);
        using (var fs = new FileStream(tempPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            fs.Flush(true);

        File.Move(tempPath, finalPath, overwrite: true);
        var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(finalPath)));
        File.WriteAllText(shaPath, $"{hash}  {Path.GetFileName(finalPath)}{Environment.NewLine}",
            new UTF8Encoding(false));
        return new ArtifactWriteResult(finalPath, shaPath, hash, bytes.Length);
    }
}
