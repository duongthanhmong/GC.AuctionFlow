namespace GC.AuctionFlow.Probe;

/// <summary>Copied instrument identity fields. Missing fields remain null.</summary>
public sealed class ObservedInstrumentSnapshot
{
    public ObservedInstrumentSnapshot(
        string? securityCode,
        string? securityId,
        string? instrument,
        string? exchange,
        DateTime? expiration,
        decimal? tickSize,
        string? underlyingSecurity,
        string? instrumentInfoInstrument,
        string? instrumentInfoExchange,
        decimal? instrumentInfoTickSize,
        string? instrumentInfoTimeZone)
    {
        SecurityCode = securityCode;
        SecurityId = securityId;
        Instrument = instrument;
        Exchange = exchange;
        Expiration = expiration;
        TickSize = tickSize;
        UnderlyingSecurity = underlyingSecurity;
        InstrumentInfoInstrument = instrumentInfoInstrument;
        InstrumentInfoExchange = instrumentInfoExchange;
        InstrumentInfoTickSize = instrumentInfoTickSize;
        InstrumentInfoTimeZone = instrumentInfoTimeZone;
        IdentityKey = BuildIdentityKey(securityCode, instrumentInfoInstrument, instrument);
    }

    public string? SecurityCode { get; }
    public string? SecurityId { get; }
    public string? Instrument { get; }
    public string? Exchange { get; }
    public DateTime? Expiration { get; }
    public decimal? TickSize { get; }
    public string? UnderlyingSecurity { get; }
    public string? InstrumentInfoInstrument { get; }
    public string? InstrumentInfoExchange { get; }
    public decimal? InstrumentInfoTickSize { get; }
    public string? InstrumentInfoTimeZone { get; }

    /// <summary>Generic identity key for gate comparison (not ContractIdentityResolver).</summary>
    public string IdentityKey { get; }

    public static string BuildIdentityKey(
        string? securityCode,
        string? instrumentInfoInstrument,
        string? instrument)
    {
        if (!string.IsNullOrWhiteSpace(securityCode))
            return securityCode.Trim();
        if (!string.IsNullOrWhiteSpace(instrumentInfoInstrument))
            return instrumentInfoInstrument.Trim();
        if (!string.IsNullOrWhiteSpace(instrument))
            return instrument.Trim();
        return "Unknown";
    }
}
