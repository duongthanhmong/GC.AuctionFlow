namespace GC.AuctionFlow.Probe;

public enum MboCallbackSource
{
    OnMarketByOrdersChangedBatch = 0
}

/// <summary>
/// Subscription Task outcome states. TaskCompleted ≠ callback presence.
/// Do not use a state named Succeeded.
/// </summary>
public enum MboSubscriptionState
{
    NotAttempted = 0,
    Attempted = 1,
    TaskCompleted = 2,
    TaskFaulted = 3,
    TaskCanceled = 4,
    StoppedAccepting = 5,
    Disposed = 6
}

public enum MboDerivedSide
{
    Unknown = 0,
    Bid = 1,
    Ask = 2
}

/// <summary>Interpreted lifecycle is always Unknown in P0-06 — raw enums stay separate.</summary>
public enum MboInterpretedLifecycleAction
{
    Unknown = 0
}

public enum MboResetEvidence
{
    NotObserved = 0,
    Suspected = 1,
    ObservedNative = 2
}

public readonly struct MboRawEventType
{
    public MboRawEventType(string rawName, int rawNumericValue, bool isKnownEnumMember)
    {
        RawName = rawName;
        RawNumericValue = rawNumericValue;
        IsKnownEnumMember = isKnownEnumMember;
    }

    public string RawName { get; }
    public int RawNumericValue { get; }
    public bool IsKnownEnumMember { get; }

    public static MboRawEventType FromKnown(string name, int numeric) =>
        new(name, numeric, isKnownEnumMember: true);

    public static MboRawEventType FromUnknown(int numeric) =>
        new($"Unknown({numeric})", numeric, isKnownEnumMember: false);
}

public static class MboKnownRawUpdateTypes
{
    // Exact ATAS.DataFeedsCore.MarketByOrderUpdateTypes — seed mirror for tests without ATAS.
    public const int Snapshot = 0;
    public const int New = 1;
    public const int Change = 2;
    public const int Delete = 3;

    public static bool TryGetKnownName(int numeric, out string name)
    {
        switch (numeric)
        {
            case Snapshot: name = nameof(Snapshot); return true;
            case New: name = nameof(New); return true;
            case Change: name = nameof(Change); return true;
            case Delete: name = nameof(Delete); return true;
            default: name = ""; return false;
        }
    }

    public static MboRawEventType Resolve(int numeric, string? runtimeName = null)
    {
        if (TryGetKnownName(numeric, out var known))
            return MboRawEventType.FromKnown(known, numeric);
        if (!string.IsNullOrWhiteSpace(runtimeName))
            return new MboRawEventType(runtimeName!, numeric, isKnownEnumMember: false);
        return MboRawEventType.FromUnknown(numeric);
    }
}

public static class MboKnownRawSideTypes
{
    public const int Bid = 0;
    public const int Ask = 1;
    public const int Trade = 2;

    public static bool TryGetKnownName(int numeric, out string name)
    {
        switch (numeric)
        {
            case Bid: name = nameof(Bid); return true;
            case Ask: name = nameof(Ask); return true;
            case Trade: name = nameof(Trade); return true;
            default: name = ""; return false;
        }
    }
}

public static class MboLifecycleProbeVersions
{
    public const string ProbeVersion = "0.0.6";
    public const string MboLifecycleProbeSchemaVersion = "1.0.1";
    public const string ContinuityDisclaimer =
        "Internal capture continuity does not establish exchange-feed completeness.";
    public const string ProviderUnsubscribeAvailability = "NotObserved";
    public const string AtasIndicatorsAssemblyVersion = "8.0.14.395";
    /// <summary>Seed value, subject to sensitivity test — initial time window from FirstCallbackReceiveUtc.</summary>
    public const double InitialTimeWindowDurationSeconds = 5.0;
    public const int FirstCallbackBatchRawTypeCap = 256;
    public const int InitialTimeWindowRawTypeCap = 256;
}
