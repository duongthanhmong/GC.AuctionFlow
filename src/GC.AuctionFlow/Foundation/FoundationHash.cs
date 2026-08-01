using System.Security.Cryptography;
using System.Text;

namespace GC.AuctionFlow.Foundation;

/// <summary>
/// TTS §4.7 deterministic foundation identity. Hashes the *semantic* input identity only:
/// ordered canonical event contributions + the three versions + contract + session-template identity
/// + revision. It deliberately excludes processing time, machine-local time, thread id, filesystem
/// path and any UI/diagnostic state. Same semantic stream + same versions ⇒ same hash; any version,
/// contract, session-template, or revision change ⇒ a different hash.
/// </summary>
public sealed class FoundationHash
{
    private readonly StringBuilder _events = new();
    private long _eventCount;

    /// <summary>Append one accepted event's semantic contribution, in arrival order.</summary>
    public void Append(in CanonicalInputEvent e)
    {
        _events.Append(e.HashContribution()).Append('\n');
        _eventCount++;
    }

    public long EventCount => _eventCount;

    /// <summary>Reset for a new capture epoch; the epoch marker is folded into the digest by the caller.</summary>
    public void ResetForNewEpoch() { _events.Clear(); _eventCount = 0; }

    /// <summary>
    /// Compute the deterministic digest over the fixed field order. <paramref name="revision"/> makes a
    /// late-event revision change the hash without rewriting history; <paramref name="captureEpoch"/>
    /// distinguishes reconnect epochs.
    /// </summary>
    public string Compute(
        VersionStamp versions,
        string contractId,
        SessionTemplate sessionTemplate,
        long revision,
        long captureEpoch)
    {
        var header = string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"v={versions.Canonical()}|contract={contractId}|session={sessionTemplate.Canonical()}|rev={revision}|epoch={captureEpoch}|n={_eventCount}\n");
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(header + _events.ToString()));
        return Convert.ToHexStringLower(bytes);
    }
}
