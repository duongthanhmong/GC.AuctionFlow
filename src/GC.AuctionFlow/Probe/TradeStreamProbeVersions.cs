namespace GC.AuctionFlow.Probe;

/// <summary>Trade-stream probe identity versions (P0-04).</summary>
public static class TradeStreamProbeVersions
{
    public const string ProbeVersion = "0.0.4";
    public const string TradeStreamProbeSchemaVersion = "1.0.1";

    /// <summary>
    /// Continuity disclaimer required on every artifact.
    /// Internal capture continuity does not establish exchange-feed completeness.
    /// </summary>
    public const string ContinuityDisclaimer =
        "Internal capture continuity does not establish exchange-feed completeness.";
}
