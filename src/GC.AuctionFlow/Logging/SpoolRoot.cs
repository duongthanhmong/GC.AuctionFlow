namespace GC.AuctionFlow.Logging;

/// <summary>
/// Spool root constant only. No writer in P0-02.
/// </summary>
public static class SpoolRoot
{
    /// <summary>Approved root: %USERPROFILE%\.gcae\ — never .aos.</summary>
    public const string DirectoryName = ".gcae";
}
