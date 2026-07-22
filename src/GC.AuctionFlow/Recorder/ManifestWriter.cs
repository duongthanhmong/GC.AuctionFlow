using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GC.AuctionFlow.Recorder;

public static class ManifestWriter
{
    public static void WriteAtomic(string manifestPath, string hashPath, SessionManifestRecord manifest, RecorderCounters counters)
    {
        try
        {
            var bytes = RecorderJson.SerializeManifest(manifest);
            var tmp = manifestPath + ".tmp";
            File.WriteAllBytes(tmp, bytes);
            using (var fs = new FileStream(tmp, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                fs.Flush(flushToDisk: true);
            File.Move(tmp, manifestPath, overwrite: true);

            var sha = Convert.ToHexString(SHA256.HashData(bytes));
            var hashTmp = hashPath + ".tmp";
            // Deterministic: uppercase hex, two spaces, file name, LF only.
            var line = sha + "  " + Path.GetFileName(manifestPath) + "\n";
            File.WriteAllText(hashTmp, line, new UTF8Encoding(false));
            using (var fs = new FileStream(hashTmp, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                fs.Flush(flushToDisk: true);
            File.Move(hashTmp, hashPath, overwrite: true);
        }
        catch
        {
            Interlocked.Increment(ref counters.ManifestFailures);
            throw;
        }
    }
}
