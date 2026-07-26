namespace GC.AuctionFlow.Participation;

/// <summary>
/// Spec §10.4. PRODUCTION_CORE detection/tagging only.
/// Percentile thresholds (volume, trade count, range, depth, spread) are NOT calibrated.
/// Weight reduction and composite exclusion are RESEARCH_ONLY / DISABLED_BY_DEFAULT.
/// </summary>
public static class ThinParticipationClassifier
{
    public const string Limitation = "THIN_PARTICIPATION_PERCENTILE_THRESHOLDS_NOT_CALIBRATED";

    public static ThinParticipationSnapshot ClassifyNotCalibrated() =>
        new(ThinParticipationLabel.NotCalibrated, new[] { Limitation });
}
