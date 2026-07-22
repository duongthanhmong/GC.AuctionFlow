namespace GC.AuctionFlow.Recorder;

/// <summary>P0-07C2: RawEventRecorderSchemaVersion 1.1.0 (callback grouping). Container unchanged.</summary>
public static class RawEventRecorderVersions
{
    public const string RawEventRecorderSchemaVersion = "1.1.0";
    public const string PreviousRawEventRecorderSchemaVersion = "1.0.0";
    public const int RawEventContainerVersion = 1;
    public const int FrameVersion = 1;
    public const int ManifestGeneration = 1;

    public const string ContinuityDisclaimer =
        "Internal capture continuity does not establish exchange-feed completeness.";

    /// <summary>
    /// Schema 1.0.0 live evidence was never produced; 1.0.0 fixtures are unsupported for trust.
    /// </summary>
    public const string Schema100UnsupportedNote =
        "RawEventRecorderSchemaVersion 1.0.0 is unsupported for live trust; use 1.1.0+.";
}
