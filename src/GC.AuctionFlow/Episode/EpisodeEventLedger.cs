namespace GC.AuctionFlow.Episode;

/// <summary>Per-Primary-Auction processed-event ledger. Cleared on auction transition.</summary>
public sealed class EpisodeEventLedger
{
    private readonly HashSet<string> _processed = new(StringComparer.Ordinal);
    private long _maxSequence = -1;
    private string? _auctionId;

    public string? AuctionId => _auctionId;
    public int Count => _processed.Count;
    public long MaxSequence => _maxSequence;

    public void EnsureAuction(string primaryAuctionId)
    {
        if (string.Equals(_auctionId, primaryAuctionId, StringComparison.Ordinal))
            return;
        Clear(primaryAuctionId);
    }

    public void Clear(string? primaryAuctionId = null)
    {
        _processed.Clear();
        _maxSequence = -1;
        _auctionId = primaryAuctionId;
    }

    /// <summary>
    /// Commit a new event identity. Duplicate and out-of-order remain rejected (idempotent / fail-closed).
    /// </summary>
    public EpisodeTradeAdmissionResult TryCommit(EpisodeTradeEvent evt)
    {
        if (evt is null)
            return EpisodeTradeAdmissionResult.InvalidEvent;
        if (_processed.Contains(evt.EventIdentity))
            return EpisodeTradeAdmissionResult.Duplicate;
        if (_maxSequence >= 0 && evt.LocalMonotonicSequence < _maxSequence)
            return EpisodeTradeAdmissionResult.OutOfOrder;

        _processed.Add(evt.EventIdentity);
        if (evt.LocalMonotonicSequence > _maxSequence)
            _maxSequence = evt.LocalMonotonicSequence;
        return EpisodeTradeAdmissionResult.Accepted;
    }
}
