namespace GC.AuctionFlow.Participation;

/// <summary>Spec §10.4. PRODUCTION_CORE detection/tagging only. Percentile thresholds NOT calibrated.</summary>
public enum ThinParticipationLabel
{
    NotCalibrated = 0,
    NormalParticipation = 1,
    ReducedParticipation = 2,
    ThinParticipation = 3,
    DislocatedParticipation = 4
}

/// <summary>Spec §10.5. Regime context only — no fade, no veto, no dealer-forcing inference.</summary>
public enum SettlementProximityTag
{
    Unknown = 0,
    PreSettlement = 1,
    SettlementTransition = 2,
    PostSettlement = 3
}
