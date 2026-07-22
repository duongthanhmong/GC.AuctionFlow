using GC.AuctionFlow.Probe;

namespace GC.AuctionFlow.Recorder;

/// <summary>Maps probe ObservedInstrumentSnapshot to recorder ObservedInstrumentIdentity (primitives only).</summary>
public static class ObservedInstrumentIdentityMapper
{
    public static ObservedInstrumentIdentity FromSnapshot(ObservedInstrumentSnapshot snap)
    {
        ArgumentNullException.ThrowIfNull(snap);
        var code = FirstNonEmpty(snap.SecurityCode, snap.InstrumentInfoInstrument, snap.Instrument) ?? "";
        var id = snap.SecurityId ?? "";
        var exchange = FirstNonEmpty(snap.Exchange, snap.InstrumentInfoExchange) ?? "";
        string? expiration = snap.Expiration?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var tick = snap.TickSize ?? snap.InstrumentInfoTickSize ?? 0m;
        var key = snap.IdentityKey;
        return new ObservedInstrumentIdentity(code, id, exchange, expiration, tick, key);
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v))
                return v.Trim();
        }

        return null;
    }
}
