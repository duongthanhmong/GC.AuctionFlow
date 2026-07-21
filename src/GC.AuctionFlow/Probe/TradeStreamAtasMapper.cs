using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using ATAS.Indicators;

namespace GC.AuctionFlow.Probe;

/// <summary>
/// Thin ATAS → immutable observation mapping. Process-local cumulative IDs only.
/// Does not retain payload references in returned observations.
/// </summary>
public sealed class TradeStreamAtasMapper
{
    private readonly TradeStreamProbeConfig _config;
    private readonly ConditionalWeakTable<object, CumIdBox> _cumIds = new();
    private long _nextCumId;

    public TradeStreamAtasMapper(TradeStreamProbeConfig? config = null)
    {
        _config = config ?? new TradeStreamProbeConfig();
    }

    public int MaxConstituentPrints => _config.MaxConstituentPrintsPerCumulative;

    public long GetOrAssignProcessLocalCumulativeId(object cumulativeTradeInstance)
    {
        return _cumIds.GetValue(cumulativeTradeInstance, _ => new CumIdBox(Interlocked.Increment(ref _nextCumId))).Id;
    }

    public bool TryGetProcessLocalCumulativeId(object cumulativeTradeInstance, out long id)
    {
        if (_cumIds.TryGetValue(cumulativeTradeInstance, out var box))
        {
            id = box.Id;
            return true;
        }

        id = 0;
        return false;
    }

    public NewTradeObservation MapNewTrade(
        MarketDataArg trade,
        TradeCallbackSource source,
        long sequence,
        string instrumentIdentityKey)
    {
        // Copy primitives immediately; do not retain `trade`.
        var time = trade.Time;
        var price = trade.Price;
        var volume = trade.Volume;
        var origin = trade.OriginPrice;
        var direction = trade.Direction.ToString();
        var dataType = trade.DataType.ToString();
        var isAsk = trade.IsAsk;
        var isBid = trade.IsBid;
        var exchOid = trade.ExchangeOrderId;
        var aggrOid = trade.AggressorExchangeOrderId;
        var oi = trade.OpenInterest;

        var core = TradeFingerprints.CoreNewTrade(time.Ticks, price, volume, direction, dataType);
        var ext = TradeFingerprints.ExtendedNewTrade(core, exchOid, aggrOid);

        return new NewTradeObservation(
            callbackSource: source,
            localMonotonicSequence: sequence,
            sourceTimeTicks: time.Ticks,
            sourceDateTimeKind: time.Kind,
            receiveUtc: DateTime.UtcNow,
            receiveStopwatchTimestamp: Stopwatch.GetTimestamp(),
            observedInstrumentIdentityKey: instrumentIdentityKey,
            coreDiagnosticFingerprint: core,
            extendedDiagnosticFingerprint: ext,
            price: price,
            volume: volume,
            originPrice: origin,
            direction: direction,
            dataType: dataType,
            isAsk: isAsk,
            isBid: isBid,
            exchangeOrderId: exchOid,
            aggressorExchangeOrderId: aggrOid,
            openInterest: oi);
    }

    public CumulativeTradeObservation MapCumulative(
        CumulativeTrade trade,
        TradeCallbackSource source,
        long sequence,
        string instrumentIdentityKey,
        bool assignInstanceId)
    {
        var time = trade.Time;
        var volume = trade.Volume;
        var first = trade.FirstPrice;
        var last = trade.Lastprice; // exact ATAS casing
        var direction = trade.Direction.ToString();

        List<MarketDataArg>? ticksRef = null;
        try { ticksRef = trade.Ticks; } catch { ticksRef = null; }

        var tickCount = ticksRef?.Count ?? 0;
        var valueFp = TradeFingerprints.CumulativeValue(
            time.Ticks, volume, first, last, direction, tickCount);

        long? processLocalId;
        if (assignInstanceId)
            processLocalId = GetOrAssignProcessLocalCumulativeId(trade);
        else if (TryGetProcessLocalCumulativeId(trade, out var existing))
            processLocalId = existing;
        else
            // Reference identity changed / unseen — assign new id; value FP remains heuristic only.
            processLocalId = GetOrAssignProcessLocalCumulativeId(trade);

        IReadOnlyList<ConstituentPrintSummary> constituents = Array.Empty<ConstituentPrintSummary>();
        if (_config.CopyConstituentPrintSummaries && ticksRef is not null && ticksRef.Count > 0)
        {
            var take = Math.Min(tickCount, _config.MaxConstituentPrintsPerCumulative);
            var list = new List<ConstituentPrintSummary>(take);
            for (var i = 0; i < take; i++)
            {
                var t = ticksRef[i];
                var tTime = t.Time;
                var tPrice = t.Price;
                var tVol = t.Volume;
                var tDir = t.Direction.ToString();
                var tDt = t.DataType.ToString();
                var core = TradeFingerprints.CoreNewTrade(tTime.Ticks, tPrice, tVol, tDir, tDt);
                list.Add(new ConstituentPrintSummary(tTime.Ticks, tPrice, tVol, tDir, tDt, core));
            }

            constituents = list;
        }

        return new CumulativeTradeObservation(
            callbackSource: source,
            localMonotonicSequence: sequence,
            sourceTimeTicks: time.Ticks,
            sourceDateTimeKind: time.Kind,
            receiveUtc: DateTime.UtcNow,
            receiveStopwatchTimestamp: Stopwatch.GetTimestamp(),
            observedInstrumentIdentityKey: instrumentIdentityKey,
            valueFingerprint: valueFp,
            volume: volume,
            firstPrice: first,
            lastPrice: last,
            direction: direction,
            copiedTickCount: tickCount,
            processLocalInstanceId: processLocalId,
            processLocalInstanceIdObserved: true,
            constituentPrintSummaries: constituents);
    }

    public static ObservedInstrumentSnapshot CaptureInstrument(IInstrumentInfo? info, object? security)
    {
        string? code = null;
        string? securityId = null;
        string? instrument = null;
        string? exchange = null;
        DateTime? expiration = null;
        decimal? tickSize = null;
        string? underlying = null;

        if (security is not null)
        {
            code = ReadStringProp(security, "Code");
            securityId = ReadStringProp(security, "SecurityId");
            instrument = ReadStringProp(security, "Instrument");
            exchange = ReadStringProp(security, "Exchange");
            expiration = ReadDateTimeProp(security, "Expiration");
            tickSize = ReadDecimalProp(security, "TickSize");
            underlying = ReadStringProp(security, "UnderlyingSecurity");
        }

        return new ObservedInstrumentSnapshot(
            securityCode: code,
            securityId: securityId,
            instrument: instrument,
            exchange: exchange,
            expiration: expiration,
            tickSize: tickSize,
            underlyingSecurity: underlying,
            instrumentInfoInstrument: info?.Instrument,
            instrumentInfoExchange: info?.Exchange,
            instrumentInfoTickSize: info?.TickSize,
            instrumentInfoTimeZone: info?.TimeZone.ToString());
    }

    private static string? ReadStringProp(object target, string name)
    {
        try
        {
            var p = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (p is null)
                return null;
            var v = p.GetValue(target);
            return v?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static DateTime? ReadDateTimeProp(object target, string name)
    {
        try
        {
            var p = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (p is null)
                return null;
            var v = p.GetValue(target);
            if (v is DateTime dt)
                return dt;
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static decimal? ReadDecimalProp(object target, string name)
    {
        try
        {
            var p = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (p is null)
                return null;
            var v = p.GetValue(target);
            if (v is null)
                return null;
            return Convert.ToDecimal(v, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }

    private sealed class CumIdBox
    {
        public CumIdBox(long id) => Id = id;
        public long Id { get; }
    }
}
