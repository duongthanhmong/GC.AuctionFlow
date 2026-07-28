namespace GC.AuctionFlow.OptionFlow;

/// <summary>
/// Throttled cache over <see cref="OptionFlowReader"/>. The render thread must not
/// do file IO, so the indicator calls <see cref="Refresh"/> at most once per
/// <c>refreshInterval</c> (from OnCalculate), and the renderer draws from the
/// cached <see cref="Current"/>. A null Current means "no GEX" — draw nothing.
///
/// The reader is injectable so this is unit-testable without touching disk.
/// </summary>
public sealed class OptionFlowProvider
{
    public delegate OptionFlowReadOutcome ReadFn(
        string dataRoot, string product, DateTimeOffset now, TimeSpan maxAge);

    private readonly string _dataRoot;
    private readonly TimeSpan _maxAge;
    private readonly TimeSpan _refreshInterval;
    private readonly ReadFn _read;

    private DateTimeOffset _lastRefresh = DateTimeOffset.MinValue;

    public OptionFlowProvider(
        string dataRoot,
        TimeSpan maxAge,
        TimeSpan refreshInterval,
        ReadFn? read = null)
    {
        _dataRoot = dataRoot;
        _maxAge = maxAge;
        _refreshInterval = refreshInterval;
        _read = read ?? OptionFlowReader.Read;
    }

    public GexContext? Current { get; private set; }
    public string Diagnostic { get; private set; } = "OptionFlow not read yet";

    /// <summary>Refresh if the interval elapsed. Returns true if a read happened.</summary>
    public bool Refresh(string product, DateTimeOffset now)
    {
        if (now - _lastRefresh < _refreshInterval)
            return false;
        _lastRefresh = now;

        var outcome = _read(_dataRoot, product, now, _maxAge);
        Current = outcome.Context;      // null on any failure -> no-GEX path
        Diagnostic = outcome.Diagnostic;
        return true;
    }
}
