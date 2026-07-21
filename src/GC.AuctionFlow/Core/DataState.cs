namespace GC.AuctionFlow.Core;

/// <summary>Spec §9.3 — analysis gate state. No scoring.</summary>
public enum DataState
{
    Invalid = 0,
    Degraded = 1,
    Ready = 2
}
