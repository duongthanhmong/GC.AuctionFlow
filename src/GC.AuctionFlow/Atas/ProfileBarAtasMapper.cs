using ATAS.Indicators;
using GC.AuctionFlow.Profile;

namespace GC.AuctionFlow.Atas;

/// <summary>
/// Maps ATAS IndicatorCandle / PriceVolumeInfo into ProfileBarObservation.
/// Observed ATAS API (8.0.14.395): Indicator.GetCandle(int), IndicatorCandle.GetAllPriceLevels(),
/// PriceVolumeInfo.{Price, Volume, Bid, Ask, Ticks}.
/// Timestamp: LiveObserved 2026-07-23 — IndicatorCandle.Time/LastTime contained clock is UTC
/// (Kind often Unspecified). Normalized exactly once via AtasTimestampNormalizer — no NY double-convert.
/// Bar Time is treated as bar start; LastTime as bar end (ATAS API names).
/// </summary>
public static class ProfileBarAtasMapper
{
    // Cached for historical bulk map — clock/TZ are config-stable for Primary Profile defaults.
    private static readonly PrimaryAuctionClock DefaultClock = new();
    private static readonly TimeZoneInfo EtZone = AuctionTimezoneResolver.Resolve();

    public static ProfileBarObservation? TryMap(
        IndicatorCandle? candle,
        int barIndex,
        int currentBar,
        long sourceVersion,
        out ProfileTimestampDiagnostic? timestampDiagnostic,
        bool includeTimestampDiagnostic = true)
    {
        timestampDiagnostic = null;
        if (candle is null)
            return null;

        var rawStart = candle.Time;
        var startUtc = AtasTimestampNormalizer.NormalizeCandleTime(rawStart);
        var endUtc = AtasTimestampNormalizer.NormalizeCandleTime(candle.LastTime);
        if (endUtc < startUtc)
            endUtc = startUtc;

        if (includeTimestampDiagnostic)
        {
            var et = TimeZoneInfo.ConvertTimeFromUtc(startUtc.UtcDateTime, EtZone);
            var point = DefaultClock.Resolve(startUtc);
            timestampDiagnostic = new ProfileTimestampDiagnostic(
                barIndex,
                rawStart,
                rawStart.Kind,
                startUtc,
                et,
                point.AuctionId,
                point.TpoPeriodIndex,
                AtasTimestampNormalizer.PolicyVersion,
                AtasTimestampNormalizer.CandleTimeSemantics,
                AtasTimestampNormalizer.CandleTimeProvenance);
        }

        var levels = new List<PriceVolumeObservation>();
        var capability = PriceVolumeCapability.Unavailable;

        try
        {
            // Observed API: GetAllPriceLevels() — enumerate once into primitives.
            var raw = candle.GetAllPriceLevels();
            if (raw is not null)
            {
                foreach (var pvi in raw)
                {
                    if (pvi is null) continue;
                    levels.Add(new PriceVolumeObservation(
                        price: pvi.Price,
                        executedVolume: pvi.Volume,
                        bidVolume: pvi.Bid,
                        askVolume: pvi.Ask,
                        tradeCount: pvi.Ticks,
                        provenance: "ATAS.PriceVolumeInfo"));
                }
            }

            capability = levels.Count > 0 ? PriceVolumeCapability.Exact : PriceVolumeCapability.Unavailable;
        }
        catch
        {
            capability = PriceVolumeCapability.Unavailable;
            levels.Clear();
        }

        return new ProfileBarObservation(
            barIndex,
            startUtc,
            endUtc,
            candle.Open,
            candle.High,
            candle.Low,
            candle.Close,
            candle.Volume,
            levels,
            capability,
            isHistorical: barIndex < currentBar,
            isCompleted: barIndex < currentBar,
            sourceVersion: sourceVersion,
            timestampPolicyVersion: AtasTimestampNormalizer.PolicyVersion);
    }
}
