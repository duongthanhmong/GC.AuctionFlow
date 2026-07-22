namespace GC.AuctionFlow.Recorder;

/// <summary>P0-07B raw event recorder identity. Does not change probe schema versions.</summary>
public static class RawEventRecorderVersions
{
    public const string RawEventRecorderSchemaVersion = "1.0.0";
    public const int RawEventContainerVersion = 1;
    public const int FrameVersion = 1;
    public const int ManifestGeneration = 1;

    public const string ContinuityDisclaimer =
        "Internal capture continuity does not establish exchange-feed completeness.";
}
