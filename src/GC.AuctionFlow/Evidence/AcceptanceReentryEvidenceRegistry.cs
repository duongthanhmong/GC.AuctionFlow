using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Reference;

namespace GC.AuctionFlow.Evidence;

/// <summary>
/// Phase 1F evidence registry. Observes Episode measurement events only.
/// Does not mutate Episode State or Resolution.
/// </summary>
public sealed class AcceptanceReentryEvidenceRegistry
{
    private readonly Dictionary<string, MutableEvidence> _active = new(StringComparer.Ordinal);
    private readonly Queue<AcceptanceReentryEvidenceSnapshot> _recentlyClosed = new();
    private AcceptanceReentryEvidenceSnapshot? _latest;
    private string _primaryAuctionId = "";
    private long _registryRevision;

    public long RegistryRevision => _registryRevision;
    public string PrimaryAuctionId => _primaryAuctionId;
    public AcceptanceReentryEvidenceSnapshot? LatestUpdated => _latest;

    public void Reset()
    {
        _active.Clear();
        _recentlyClosed.Clear();
        _latest = null;
        _primaryAuctionId = "";
        _registryRevision = 0;
    }

    public void OnPrimaryAuctionChanged(string newAuctionId, DateTime nowUtc)
    {
        if (string.Equals(_primaryAuctionId, newAuctionId, StringComparison.Ordinal))
            return;

        foreach (var ev in _active.Values.ToArray())
        {
            ev.Freeze(nowUtc, EvidenceMeasurementStatus.Frozen);
            EnqueueClosed(ev.ToSnapshot(nowUtc));
        }

        _active.Clear();
        _primaryAuctionId = newAuctionId ?? "";
        _registryRevision++;
    }

    public bool ProcessMeasurementEvent(EpisodeMeasurementEvent evt, AuctionEpisodeSnapshot? episodeSnapshot, DateTime nowUtc)
    {
        if (evt is null || string.IsNullOrEmpty(evt.EpisodeId))
            return false;

        if (!_active.TryGetValue(evt.EpisodeId, out var evidence))
        {
            if (episodeSnapshot is null)
                return false;
            evidence = new MutableEvidence(EvidenceIdentity.Build(evt.EpisodeId), episodeSnapshot, nowUtc);
            _active[evt.EpisodeId] = evidence;
            _registryRevision++;
        }

        var changed = evidence.ApplyEvent(evt, nowUtc);
        if (episodeSnapshot is not null)
            evidence.SyncEpisodeProvenance(episodeSnapshot, nowUtc);
        if (!changed)
            return false;

        _latest = evidence.ToSnapshot(nowUtc);
        return true;
    }

    /// <summary>Sync provenance and freeze closed episodes. Does not create evidence without measurement events.</summary>
    public void SyncFromEpisodeSet(AuctionEpisodeSetSnapshot? episodes, DateTime nowUtc)
    {
        if (episodes is null)
            return;

        OnPrimaryAuctionChanged(episodes.PrimaryAuctionId, nowUtc);

        var alive = new HashSet<string>(
            episodes.ActiveEpisodes.Select(e => e.EpisodeId),
            StringComparer.Ordinal);

        foreach (var ep in episodes.ActiveEpisodes)
        {
            if (_active.TryGetValue(ep.EpisodeId, out var evidence))
                evidence.SyncEpisodeProvenance(ep, nowUtc);
        }

        foreach (var closed in episodes.RecentlyClosedEpisodes)
        {
            if (_active.TryGetValue(closed.EpisodeId, out var evidence))
            {
                evidence.SyncEpisodeProvenance(closed, nowUtc);
                evidence.Freeze(nowUtc,
                    closed.State == EpisodeState.InvalidData
                        ? EvidenceMeasurementStatus.Invalid
                        : EvidenceMeasurementStatus.Frozen);
                EnqueueClosed(evidence.ToSnapshot(nowUtc));
                _active.Remove(closed.EpisodeId);
            }
        }

        foreach (var orphan in _active.Keys.Where(id => !alive.Contains(id)).ToArray())
        {
            if (_active.TryGetValue(orphan, out var evidence))
            {
                evidence.Freeze(nowUtc, EvidenceMeasurementStatus.Frozen);
                EnqueueClosed(evidence.ToSnapshot(nowUtc));
                _active.Remove(orphan);
            }
        }
    }

    private void EnqueueClosed(AcceptanceReentryEvidenceSnapshot snap)
    {
        _recentlyClosed.Enqueue(snap);
        _latest = snap;
        while (_recentlyClosed.Count > AcceptanceReentryEvidenceSetSnapshot.RecentlyClosedCapacity)
            _recentlyClosed.Dequeue();
        _registryRevision++;
    }

    public IReadOnlyList<AcceptanceReentryEvidenceSnapshot> SnapshotActive(DateTime nowUtc) =>
        _active.Values.Select(e => e.ToSnapshot(nowUtc)).OrderBy(e => e.EvidenceId, StringComparer.Ordinal).ToArray();

    public IReadOnlyList<AcceptanceReentryEvidenceSnapshot> SnapshotClosed() => _recentlyClosed.ToArray();
}
