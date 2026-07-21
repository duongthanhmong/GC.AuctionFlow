namespace GC.AuctionFlow.Probe;

/// <summary>
/// Bounded overlap / distribution statistics. No volume merge. No authoritative stream declaration.
/// Fingerprint matches are diagnostic overlap only — never delete events.
/// </summary>
public sealed class TradeStreamOverlapStats
{
    private readonly int _setCapacity;
    private readonly HashSet<string> _singularCore = new(StringComparer.Ordinal);
    private readonly HashSet<string> _batchCore = new(StringComparer.Ordinal);
    private readonly HashSet<string> _cumulativeValueNew = new(StringComparer.Ordinal);
    private readonly HashSet<string> _cumulativeValueUpdate = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    private long _singularBatchCoreOverlapHits;
    private long _newTradesVsCumulativeConstituentHits;
    private long _cumulativeNewVsUpdateValueHits;
    private long _exchangeOrderIdPopulatedOnNewTrade;
    private long _exchangeOrderIdMissingOnNewTrade;
    private long _exchangeOrderIdPopulatedOnBatch;
    private long _exchangeOrderIdMissingOnBatch;
    private long _kindUnspecified;
    private long _kindUtc;
    private long _kindLocal;
    private long _kindLocalOnNewTrade;
    private long _kindUtcOnNewTrade;
    private long _kindUnspecifiedOnNewTrade;
    private long _kindLocalOnBatch;
    private long _kindUtcOnBatch;
    private long _kindUnspecifiedOnBatch;
    private long _kindLocalOnCum;
    private long _kindUtcOnCum;
    private long _kindUnspecifiedOnCum;
    private long _sourceTimeNonDecreasing;
    private long _sourceTimeDecreasing;
    private long? _lastSourceTimeTicks;
    private long _fingerprintCollisionsObserved;
    private long _cumulativeInstanceIdChangedWithSameValueFp;
    private long _cumulativeInstanceIdRetained;

    public TradeStreamOverlapStats(int setCapacity)
    {
        _setCapacity = setCapacity;
    }

    public void ObserveNewTrade(NewTradeObservation obs)
    {
        lock (_gate)
        {
            ObserveKind(obs.CallbackSource, obs.SourceDateTimeKind);
            ObserveSourceTimeOrder(obs.SourceTimeTicks);

            if (obs.ExchangeOrderId.HasValue)
            {
                if (obs.CallbackSource == TradeCallbackSource.OnNewTrade)
                    _exchangeOrderIdPopulatedOnNewTrade++;
                else
                    _exchangeOrderIdPopulatedOnBatch++;
            }
            else
            {
                if (obs.CallbackSource == TradeCallbackSource.OnNewTrade)
                    _exchangeOrderIdMissingOnNewTrade++;
                else
                    _exchangeOrderIdMissingOnBatch++;
            }

            if (obs.CallbackSource == TradeCallbackSource.OnNewTrade)
            {
                if (!TryAddBounded(_singularCore, obs.CoreDiagnosticFingerprint))
                    _fingerprintCollisionsObserved++;
                if (_batchCore.Contains(obs.CoreDiagnosticFingerprint))
                    _singularBatchCoreOverlapHits++;
            }
            else if (obs.CallbackSource == TradeCallbackSource.OnNewTradesBatch)
            {
                if (!TryAddBounded(_batchCore, obs.CoreDiagnosticFingerprint))
                    _fingerprintCollisionsObserved++;
                if (_singularCore.Contains(obs.CoreDiagnosticFingerprint))
                    _singularBatchCoreOverlapHits++;
            }
        }
    }

    public void ObserveCumulative(CumulativeTradeObservation obs)
    {
        lock (_gate)
        {
            ObserveKind(obs.CallbackSource, obs.SourceDateTimeKind);
            ObserveSourceTimeOrder(obs.SourceTimeTicks);

            if (obs.CallbackSource == TradeCallbackSource.OnCumulativeTrade)
            {
                if (!TryAddBounded(_cumulativeValueNew, obs.ValueFingerprint))
                    _fingerprintCollisionsObserved++;
                if (_cumulativeValueUpdate.Contains(obs.ValueFingerprint))
                    _cumulativeNewVsUpdateValueHits++;
            }
            else if (obs.CallbackSource == TradeCallbackSource.OnUpdateCumulativeTrade)
            {
                if (!TryAddBounded(_cumulativeValueUpdate, obs.ValueFingerprint))
                    _fingerprintCollisionsObserved++;
                if (_cumulativeValueNew.Contains(obs.ValueFingerprint))
                    _cumulativeNewVsUpdateValueHits++;
            }

            foreach (var c in obs.ConstituentPrintSummaries)
            {
                if (_singularCore.Contains(c.CoreFingerprint) || _batchCore.Contains(c.CoreFingerprint))
                    _newTradesVsCumulativeConstituentHits++;
            }
        }
    }

    public void ObserveCumulativeInstanceLinkage(bool sameInstanceId, bool valueFingerprintMatched)
    {
        lock (_gate)
        {
            if (sameInstanceId)
                _cumulativeInstanceIdRetained++;
            else if (valueFingerprintMatched)
                _cumulativeInstanceIdChangedWithSameValueFp++;
        }
    }

    private void ObserveKind(TradeCallbackSource source, DateTimeKind kind)
    {
        switch (kind)
        {
            case DateTimeKind.Utc:
                _kindUtc++;
                break;
            case DateTimeKind.Local:
                _kindLocal++;
                break;
            default:
                _kindUnspecified++;
                break;
        }

        switch (source)
        {
            case TradeCallbackSource.OnNewTrade:
                switch (kind)
                {
                    case DateTimeKind.Utc: _kindUtcOnNewTrade++; break;
                    case DateTimeKind.Local: _kindLocalOnNewTrade++; break;
                    default: _kindUnspecifiedOnNewTrade++; break;
                }
                break;
            case TradeCallbackSource.OnNewTradesBatch:
                switch (kind)
                {
                    case DateTimeKind.Utc: _kindUtcOnBatch++; break;
                    case DateTimeKind.Local: _kindLocalOnBatch++; break;
                    default: _kindUnspecifiedOnBatch++; break;
                }
                break;
            case TradeCallbackSource.OnCumulativeTrade:
            case TradeCallbackSource.OnUpdateCumulativeTrade:
                switch (kind)
                {
                    case DateTimeKind.Utc: _kindUtcOnCum++; break;
                    case DateTimeKind.Local: _kindLocalOnCum++; break;
                    default: _kindUnspecifiedOnCum++; break;
                }
                break;
        }
    }

    private void ObserveSourceTimeOrder(long ticks)
    {
        if (_lastSourceTimeTicks is null)
        {
            _lastSourceTimeTicks = ticks;
            return;
        }

        if (ticks >= _lastSourceTimeTicks.Value)
            _sourceTimeNonDecreasing++;
        else
            _sourceTimeDecreasing++;

        _lastSourceTimeTicks = ticks;
    }

    /// <returns>false when fingerprint already present (collision observed; event not deleted).</returns>
    private bool TryAddBounded(HashSet<string> set, string fingerprint)
    {
        if (set.Contains(fingerprint))
            return false;

        if (set.Count >= _setCapacity)
            return true; // capacity drop — not a collision

        set.Add(fingerprint);
        return true;
    }

    public TradeStreamOverlapSnapshot Snapshot()
    {
        lock (_gate)
        {
            return new TradeStreamOverlapSnapshot(
                SingularBatchCoreOverlapHits: _singularBatchCoreOverlapHits,
                SingularCoreFingerprintsRetained: _singularCore.Count,
                BatchCoreFingerprintsRetained: _batchCore.Count,
                NewTradesVsCumulativeConstituentHits: _newTradesVsCumulativeConstituentHits,
                CumulativeNewVsUpdateValueHits: _cumulativeNewVsUpdateValueHits,
                FingerprintCollisionsObserved: _fingerprintCollisionsObserved,
                ExchangeOrderIdPopulatedOnNewTrade: _exchangeOrderIdPopulatedOnNewTrade,
                ExchangeOrderIdMissingOnNewTrade: _exchangeOrderIdMissingOnNewTrade,
                ExchangeOrderIdPopulatedOnBatch: _exchangeOrderIdPopulatedOnBatch,
                ExchangeOrderIdMissingOnBatch: _exchangeOrderIdMissingOnBatch,
                DateTimeKindUnspecified: _kindUnspecified,
                DateTimeKindUtc: _kindUtc,
                DateTimeKindLocal: _kindLocal,
                DateTimeKindUnspecifiedOnNewTrade: _kindUnspecifiedOnNewTrade,
                DateTimeKindUtcOnNewTrade: _kindUtcOnNewTrade,
                DateTimeKindLocalOnNewTrade: _kindLocalOnNewTrade,
                DateTimeKindUnspecifiedOnBatch: _kindUnspecifiedOnBatch,
                DateTimeKindUtcOnBatch: _kindUtcOnBatch,
                DateTimeKindLocalOnBatch: _kindLocalOnBatch,
                DateTimeKindUnspecifiedOnCumulative: _kindUnspecifiedOnCum,
                DateTimeKindUtcOnCumulative: _kindUtcOnCum,
                DateTimeKindLocalOnCumulative: _kindLocalOnCum,
                SourceTimeNonDecreasingObservations: _sourceTimeNonDecreasing,
                SourceTimeDecreasingObservations: _sourceTimeDecreasing,
                CumulativeInstanceIdRetained: _cumulativeInstanceIdRetained,
                CumulativeInstanceIdChangedWithSameValueFingerprint: _cumulativeInstanceIdChangedWithSameValueFp);
        }
    }
}

public sealed record TradeStreamOverlapSnapshot(
    long SingularBatchCoreOverlapHits,
    int SingularCoreFingerprintsRetained,
    int BatchCoreFingerprintsRetained,
    long NewTradesVsCumulativeConstituentHits,
    long CumulativeNewVsUpdateValueHits,
    long FingerprintCollisionsObserved,
    long ExchangeOrderIdPopulatedOnNewTrade,
    long ExchangeOrderIdMissingOnNewTrade,
    long ExchangeOrderIdPopulatedOnBatch,
    long ExchangeOrderIdMissingOnBatch,
    long DateTimeKindUnspecified,
    long DateTimeKindUtc,
    long DateTimeKindLocal,
    long DateTimeKindUnspecifiedOnNewTrade,
    long DateTimeKindUtcOnNewTrade,
    long DateTimeKindLocalOnNewTrade,
    long DateTimeKindUnspecifiedOnBatch,
    long DateTimeKindUtcOnBatch,
    long DateTimeKindLocalOnBatch,
    long DateTimeKindUnspecifiedOnCumulative,
    long DateTimeKindUtcOnCumulative,
    long DateTimeKindLocalOnCumulative,
    long SourceTimeNonDecreasingObservations,
    long SourceTimeDecreasingObservations,
    long CumulativeInstanceIdRetained,
    long CumulativeInstanceIdChangedWithSameValueFingerprint);
