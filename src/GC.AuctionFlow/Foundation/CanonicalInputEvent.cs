using GC.AuctionFlow.Orderflow;

namespace GC.AuctionFlow.Foundation;

/// <summary>
/// TTS §5.2 canonical normalized input event. The adapter converts an ATAS callback into this exactly
/// once; the foundation reasons only about these. Price is integer ticks; aggressor keeps Unknown
/// honest; time carries provenance; identity declares its dedup capability; all three versions ride
/// along. This record adds NO classification.
/// </summary>
public readonly record struct CanonicalInputEvent(
    string InstrumentId,
    string ContractId,
    long PriceTicks,
    decimal TickSize,
    long Quantity,
    AggressorSide Aggressor,
    InputEventKind Kind,
    DateTime EventTimeUtc,
    DateTime ReceiveTimeUtc,
    SourceTimeKind TimeProvenance,
    DataPhase Phase,
    string SourceId,
    string? StableKey,
    DedupCapability Dedup,
    long CallbackLocalOrder,
    VersionStamp Versions)
{
    /// <summary>
    /// The identity a dedup pass keys on. Native id when declared stable; otherwise a documented
    /// deterministic composite key: contract | eventTimeUtc(ticks) | priceTicks | qty | aggressor.
    /// This composite can legitimately collide on identical prints — <see cref="Dedup"/> says so, and
    /// the meter never silently collapses collisions into one when <see cref="DedupCapability.CompositeKey"/>.
    /// </summary>
    public string IdentityKey()
    {
        if (Dedup == DedupCapability.NativeStableId && !string.IsNullOrEmpty(StableKey))
            return "nid:" + StableKey;
        // Composite key. EventTime as UTC ticks (integer) — never a float price, never machine time.
        return string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"ck:{ContractId}|{EventTimeUtc.ToUniversalTime().Ticks}|{PriceTicks}|{Quantity}|{(int)Aggressor}");
    }

    /// <summary>Stable, order-fixed serialization contributed to the deterministic foundation hash.
    /// Excludes ReceiveTime, callback order, and every nondeterministic diagnostic on purpose.</summary>
    public string HashContribution() =>
        string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"{(int)Kind}|{ContractId}|{EventTimeUtc.ToUniversalTime().Ticks}|{PriceTicks}|{Quantity}|{(int)Aggressor}|{(int)Phase}|{(int)Dedup}|{IdentityKey()}");
}
