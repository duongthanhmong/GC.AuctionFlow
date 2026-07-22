using System.Globalization;

namespace GC.AuctionFlow.Recorder;

/// <summary>Exact observed instrument primitives. Never silently merge contracts.</summary>
public sealed class ObservedInstrumentIdentity : IEquatable<ObservedInstrumentIdentity>
{
    public ObservedInstrumentIdentity(
        string securityCode,
        string securityId,
        string exchange,
        string? contractExpiration,
        decimal tickSize,
        string identityKey)
    {
        SecurityCode = securityCode ?? string.Empty;
        SecurityId = securityId ?? string.Empty;
        Exchange = exchange ?? string.Empty;
        ContractExpiration = contractExpiration;
        TickSize = tickSize;
        IdentityKey = identityKey ?? string.Empty;
    }

    public string SecurityCode { get; }
    public string SecurityId { get; }
    public string Exchange { get; }
    public string? ContractExpiration { get; }
    public decimal TickSize { get; }
    public string IdentityKey { get; }

    public bool Equals(ObservedInstrumentIdentity? other)
    {
        if (other is null) return false;
        return string.Equals(SecurityCode, other.SecurityCode, StringComparison.Ordinal)
               && string.Equals(SecurityId, other.SecurityId, StringComparison.Ordinal)
               && string.Equals(Exchange, other.Exchange, StringComparison.Ordinal)
               && string.Equals(ContractExpiration, other.ContractExpiration, StringComparison.Ordinal)
               && TickSize == other.TickSize
               && string.Equals(IdentityKey, other.IdentityKey, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj) => Equals(obj as ObservedInstrumentIdentity);

    public override int GetHashCode() =>
        HashCode.Combine(SecurityCode, SecurityId, Exchange, ContractExpiration, TickSize, IdentityKey);

    public string FormatTuple() =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{SecurityCode}|{SecurityId}|{Exchange}|{ContractExpiration}|{TickSize}|{IdentityKey}");
}
