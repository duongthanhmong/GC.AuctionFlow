namespace GC.AuctionFlow.UI;

/// <summary>
/// Internal status snapshot placeholder. No panel rendering in P0-02 (drawn panel deferred to P0-09).
/// </summary>
public sealed record StatusSnapshot(
    string Phase,
    string Message);
