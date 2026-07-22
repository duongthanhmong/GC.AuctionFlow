namespace GC.AuctionFlow.Recorder;

/// <summary>
/// Primary-process MBO recording lock (P0-06D). Schema may exist; recording stays disabled.
/// </summary>
public static class MboOperationalLock
{
    public const bool MboSchemaSupported = true;
    public const bool MboRecordingEnabled = false;
    public const MboIsolationRequirement MboIsolationRequirement = Recorder.MboIsolationRequirement.IsolatedEnvironmentOnly;

    public const string MboOperationalBlockReason =
        "Same-process GC chart-data side effect observed during fresh MBO subscription.";
}

public enum MboIsolationRequirement
{
    IsolatedEnvironmentOnly = 1
}
