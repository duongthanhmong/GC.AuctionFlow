namespace GC.AuctionFlow.Participation;

public sealed record SettlementProximitySnapshot(
    SettlementProximityTag Tag,
    TimeSpan? OffsetFromAnchor,
    string TimezoneId,
    IReadOnlyList<string> KnownLimitations);

public sealed record ThinParticipationSnapshot(
    ThinParticipationLabel Label,
    IReadOnlyList<string> KnownLimitations);

public sealed class ParticipationSetSnapshot
{
    public ParticipationSetSnapshot(
        SettlementProximitySnapshot settlementProximity,
        ThinParticipationSnapshot thinParticipation)
    {
        SettlementProximity = settlementProximity ?? throw new ArgumentNullException(nameof(settlementProximity));
        ThinParticipation = thinParticipation ?? throw new ArgumentNullException(nameof(thinParticipation));
    }

    public SettlementProximitySnapshot SettlementProximity { get; }
    public ThinParticipationSnapshot ThinParticipation { get; }

    public IReadOnlyList<string> KnownLimitations =>
        SettlementProximity.KnownLimitations
            .Concat(ThinParticipation.KnownLimitations)
            .ToList();
}
