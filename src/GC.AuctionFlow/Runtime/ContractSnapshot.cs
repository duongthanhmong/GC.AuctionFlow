namespace GC.AuctionFlow.Runtime;

/// <summary>Immutable versioned contract identity + expiration/roll classification.</summary>
public sealed class ContractSnapshot
{
    public const string SnapshotVersion = "0.1.0";

    public ContractSnapshot(
        DateTime timestampUtc,
        string? securityCode,
        string? securityId,
        string? exchange,
        DateTime? contractExpiration,
        decimal? tickSize,
        string identityKey,
        string expectedInstrumentCode,
        InstrumentMatchState instrumentMatchState,
        ExpirationState expirationState,
        int? daysToExpiration,
        RollState rollState,
        IReadOnlyList<string> knownLimitations,
        string sourceProvenance)
    {
        TimestampUtc = timestampUtc;
        SecurityCode = securityCode;
        SecurityId = securityId;
        Exchange = exchange;
        ContractExpiration = contractExpiration;
        TickSize = tickSize;
        IdentityKey = identityKey ?? "Unknown";
        ExpectedInstrumentCode = expectedInstrumentCode ?? "";
        InstrumentMatchState = instrumentMatchState;
        ExpirationState = expirationState;
        DaysToExpiration = daysToExpiration;
        RollState = rollState;
        KnownLimitations = knownLimitations ?? Array.Empty<string>();
        SourceProvenance = sourceProvenance ?? "Unknown";
    }

    public DateTime TimestampUtc { get; }
    public string? SecurityCode { get; }
    public string? SecurityId { get; }
    public string? Exchange { get; }
    public DateTime? ContractExpiration { get; }
    public decimal? TickSize { get; }
    public string IdentityKey { get; }
    public string ExpectedInstrumentCode { get; }
    public InstrumentMatchState InstrumentMatchState { get; }
    public ExpirationState ExpirationState { get; }
    public int? DaysToExpiration { get; }
    public RollState RollState { get; }
    public IReadOnlyList<string> KnownLimitations { get; }
    public string SourceProvenance { get; }
    public string Version => SnapshotVersion;
}
