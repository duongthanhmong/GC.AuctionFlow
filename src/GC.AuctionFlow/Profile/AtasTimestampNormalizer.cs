using System.Globalization;

namespace GC.AuctionFlow.Profile;

/// <summary>Contained-clock semantics of an ATAS DateTime — not inferred from DateTime.Kind alone.</summary>
public enum AtasTimestampSemantics
{
    Utc = 0,
    ExchangeLocal = 1,
    ChartLocal = 2,
    MachineLocal = 3,
    Unknown = 4
}

public enum AtasTimestampProvenance
{
    ApiVerified = 0,
    LiveObserved = 1,
    OperatorDeclared = 2,
    Unknown = 3
}

/// <summary>
/// Single ATAS → UTC normalization boundary for profile bars.
/// LiveObserved 2026-07-23 (GCQ6/Rithmic): IndicatorCandle.Time Kind often Unspecified,
/// but the contained wall-clock value is UTC. Treating it as America/New_York caused +4h
/// (period index 40 vs expected 32 at ~00:29 ET).
/// PolicyVersion must bump when the rule changes so bar ledgers rebuild cleanly.
/// </summary>
public static class AtasTimestampNormalizer
{
    /// <summary>Bump when normalization rule changes — invalidates prior bar observations.</summary>
    public const string PolicyVersion = "ATAS_CANDLE_TIME_UTC_V1";

    /// <summary>LiveObserved: ATAS IndicatorCandle.Time / LastTime wall clock is UTC.</summary>
    public const AtasTimestampSemantics CandleTimeSemantics = AtasTimestampSemantics.Utc;

    public const AtasTimestampProvenance CandleTimeProvenance = AtasTimestampProvenance.LiveObserved;

    public static DateTimeOffset NormalizeToUtc(
        DateTime raw,
        AtasTimestampSemantics semantics,
        AtasTimestampProvenance provenance,
        TimeZoneInfo? exchangeOrChartZone = null)
    {
        if (semantics == AtasTimestampSemantics.Unknown)
            throw new InvalidOperationException(
                "Unknown ATAS timestamp semantics must not be silently converted to UTC or New York local.");

        return semantics switch
        {
            AtasTimestampSemantics.Utc => AsUtcOffset(raw),
            AtasTimestampSemantics.MachineLocal => raw.Kind == DateTimeKind.Utc
                ? new DateTimeOffset(raw, TimeSpan.Zero)
                : new DateTimeOffset(DateTime.SpecifyKind(
                    raw.Kind == DateTimeKind.Local ? raw : DateTime.SpecifyKind(raw, DateTimeKind.Local),
                    DateTimeKind.Local)).ToUniversalTime(),
            AtasTimestampSemantics.ExchangeLocal or AtasTimestampSemantics.ChartLocal =>
                ConvertLocalWallClockToUtc(raw, exchangeOrChartZone
                    ?? throw new ArgumentNullException(nameof(exchangeOrChartZone),
                        "Exchange/Chart local semantics require an explicit timezone.")),
            _ => throw new InvalidOperationException("Unsupported timestamp semantics: " + semantics)
        };
    }

    /// <summary>Normalize IndicatorCandle.Time using the live-observed UTC policy.</summary>
    public static DateTimeOffset NormalizeCandleTime(DateTime raw) =>
        NormalizeToUtc(raw, CandleTimeSemantics, CandleTimeProvenance);

    private static DateTimeOffset AsUtcOffset(DateTime raw)
    {
        // Contained clock is UTC. Kind alone does not authorize a zone conversion.
        if (raw.Kind == DateTimeKind.Utc)
            return new DateTimeOffset(raw);
        if (raw.Kind == DateTimeKind.Local)
            return new DateTimeOffset(raw.ToUniversalTime());
        // Unspecified: wall clock already UTC — attach +00:00 once (no double conversion).
        return new DateTimeOffset(DateTime.SpecifyKind(raw, DateTimeKind.Utc));
    }

    private static DateTimeOffset ConvertLocalWallClockToUtc(DateTime raw, TimeZoneInfo zone)
    {
        var unspecified = DateTime.SpecifyKind(
            raw.Kind == DateTimeKind.Utc ? raw : raw,
            DateTimeKind.Unspecified);
        if (raw.Kind == DateTimeKind.Utc)
            throw new InvalidOperationException("UTC Kind cannot be treated as exchange/chart local without explicit policy.");

        if (zone.IsAmbiguousTime(unspecified))
        {
            var earlier = zone.GetAmbiguousTimeOffsets(unspecified).Min();
            return new DateTimeOffset(unspecified, earlier).ToUniversalTime();
        }

        if (zone.IsInvalidTime(unspecified))
        {
            var adjusted = unspecified.AddHours(1);
            return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(adjusted, zone));
        }

        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(unspecified, zone));
    }
}

/// <summary>Bounded per-bar timestamp diagnostic for GPS / live closeout.</summary>
public sealed class ProfileTimestampDiagnostic
{
    public ProfileTimestampDiagnostic(
        int barIndex,
        DateTime rawTime,
        DateTimeKind rawKind,
        DateTimeOffset normalizedUtc,
        DateTime normalizedEt,
        string auctionId,
        int tpoPeriodIndex,
        string policyVersion,
        AtasTimestampSemantics semantics,
        AtasTimestampProvenance provenance)
    {
        BarIndex = barIndex;
        RawTime = rawTime;
        RawKind = rawKind;
        NormalizedUtc = normalizedUtc;
        NormalizedEt = normalizedEt;
        AuctionId = auctionId;
        TpoPeriodIndex = tpoPeriodIndex;
        PolicyVersion = policyVersion;
        Semantics = semantics;
        Provenance = provenance;
    }

    public int BarIndex { get; }
    public DateTime RawTime { get; }
    public DateTimeKind RawKind { get; }
    public DateTimeOffset NormalizedUtc { get; }
    public DateTime NormalizedEt { get; }
    public string AuctionId { get; }
    public int TpoPeriodIndex { get; }
    public string PolicyVersion { get; }
    public AtasTimestampSemantics Semantics { get; }
    public AtasTimestampProvenance Provenance { get; }

    public IReadOnlyList<string> ToGpsDiagnosticRows() =>
        new[]
        {
            "RAW BAR TIME: " + RawTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            "RAW TIME KIND: " + RawKind,
            "NORMALIZED UTC: " + NormalizedUtc.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + "Z",
            "NORMALIZED ET: " + NormalizedEt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            "TPO PERIOD INDEX: " + TpoPeriodIndex.ToString(CultureInfo.InvariantCulture),
            "TS POLICY: " + PolicyVersion + " / " + Semantics + " / " + Provenance
        };
}
