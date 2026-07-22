using System.IO;
using GC.AuctionFlow.Logging;

namespace GC.AuctionFlow.Recorder;

public static class RecorderStoragePaths
{
    public const string RecorderDirectoryName = "recorder";
    public const string SessionsDirectoryName = "sessions";
    public const string SegmentsDirectoryName = "segments";
    public const string RecoveryDirectoryName = "recovery";
    public const string ManifestFileName = "manifest.json";
    public const string ManifestHashFileName = "manifest.json.sha256";

    public static string GetRecorderRoot(string? userProfile = null)
    {
        var profile = userProfile ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(profile, SpoolRoot.DirectoryName, RecorderDirectoryName);
    }

    public static string GetSessionDirectory(Guid sessionId, string? userProfile = null) =>
        Path.Combine(GetRecorderRoot(userProfile), SessionsDirectoryName, sessionId.ToString("N"));

    public static string GetSegmentsDirectory(Guid sessionId, string? userProfile = null) =>
        Path.Combine(GetSessionDirectory(sessionId, userProfile), SegmentsDirectoryName);

    public static string GetRecoveryDirectory(Guid sessionId, string? userProfile = null) =>
        Path.Combine(GetSessionDirectory(sessionId, userProfile), RecoveryDirectoryName);

    public static string GetManifestPath(Guid sessionId, string? userProfile = null) =>
        Path.Combine(GetSessionDirectory(sessionId, userProfile), ManifestFileName);

    public static string GetManifestHashPath(Guid sessionId, string? userProfile = null) =>
        Path.Combine(GetSessionDirectory(sessionId, userProfile), ManifestHashFileName);

    public static void EnsureSessionLayout(Guid sessionId, string? userProfile = null)
    {
        Directory.CreateDirectory(GetSegmentsDirectory(sessionId, userProfile));
        Directory.CreateDirectory(GetRecoveryDirectory(sessionId, userProfile));
    }

    public static string SegmentFileName(Guid segmentId) => $"{segmentId:N}.seg";
    public static string SegmentTempFileName(Guid segmentId) => $"{segmentId:N}.seg.tmp";
    public static string SegmentHashFileName(Guid segmentId) => $"{segmentId:N}.seg.sha256";
    public static string SegmentHashTempFileName(Guid segmentId) => $"{segmentId:N}.seg.sha256.tmp";
}
