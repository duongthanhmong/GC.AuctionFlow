namespace GC.AuctionFlow.Recorder;

/// <summary>P0-07C3BC: RawEventRecorderSchemaVersion 1.2.0 (category counts + Trade integration). Container unchanged.</summary>
public static class RawEventRecorderVersions
{
    public const string RawEventRecorderSchemaVersion = "1.2.0";
    public const string PreviousRawEventRecorderSchemaVersion = "1.1.0";
    public const string Schema110UnsupportedForCategoryCountsNote =
        "RawEventRecorderSchemaVersion 1.1.0 footers lack category counts; 1.2.0+ required for category reconciliation.";
    public const string Schema100UnsupportedNote =
        "RawEventRecorderSchemaVersion 1.0.0 is unsupported for live trust; use 1.2.0+.";
    public const int RawEventContainerVersion = 1;
    public const int FrameVersion = 1;
    public const int ManifestGeneration = 1;

    public const string ContinuityDisclaimer =
        "Internal capture continuity does not establish exchange-feed completeness.";
}
