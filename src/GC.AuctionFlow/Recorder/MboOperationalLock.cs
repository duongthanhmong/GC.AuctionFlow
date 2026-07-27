namespace GC.AuctionFlow.Recorder;

/// <summary>
/// Primary-process MBO recording lock (P0-06D). Schema may exist; recording stays disabled.
/// </summary>
public static class MboOperationalLock
{
    public const bool MboSchemaSupported = true;
    /// <summary>
    /// Default only. The operator decides per session — see the indicator's
    /// "Enable MBO Recording" setting.
    ///
    /// This was a hard-coded false because P0-06D attributed an abnormal chart bar to MBO
    /// subscription. That attribution was wrong: the bar came from this project's own
    /// Flush(flushToDisk: true) inside OnCalculate. With that moved off-thread, enabling
    /// the MBO probe and restarting reproduced nothing, so the prohibition no longer rests
    /// on evidence. Recording stays off by default because MBO completeness is still
    /// unproven, not because capture is believed unsafe.
    /// </summary>
    public const bool MboRecordingEnabledDefault = false;
    public const MboIsolationRequirement MboIsolationRequirement = Recorder.MboIsolationRequirement.OperatorDecision;

    /// <summary>
    /// Kept as the historical reason, and marked as withdrawn.
    ///
    /// The side effect was real; the attribution was not. It was caused by
    /// Flush(flushToDisk: true) running inside OnCalculate in this project's own recorder
    /// startup, which stalled the platform's data pump. After moving that off-thread, a
    /// re-test with the MBO probe enabled reproduced nothing.
    /// </summary>
    public const string MboOperationalBlockReason =
        "WITHDRAWN 2026-07-27: same-process chart-data side effect was this project's own "
        + "fsync on the ATAS thread, not MBO subscription.";
}

public enum MboIsolationRequirement
{
    /// <summary>
    /// Retained so prior manifests and evidence documents still read correctly. No longer
    /// asserted: the observation behind it was this project's own fsync on the ATAS thread.
    /// </summary>
    IsolatedEnvironmentOnly = 1,

    /// <summary>
    /// Capture is the operator's decision. Nothing in evidence forbids it; what remains
    /// unproven is MBO *completeness*, which is a reason to distrust the contents rather
    /// than to refuse the recording.
    /// </summary>
    OperatorDecision = 2
}
