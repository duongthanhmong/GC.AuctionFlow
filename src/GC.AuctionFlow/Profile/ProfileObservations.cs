namespace GC.AuctionFlow.Profile;

/// <summary>Per-price executed volume primitive. No ATAS types.</summary>
public sealed class PriceVolumeObservation
{
    public PriceVolumeObservation(
        decimal price,
        decimal executedVolume,
        decimal? bidVolume,
        decimal? askVolume,
        int? tradeCount,
        string provenance)
    {
        Price = price;
        ExecutedVolume = executedVolume;
        BidVolume = bidVolume;
        AskVolume = askVolume;
        TradeCount = tradeCount;
        Provenance = provenance ?? "";
    }

    public decimal Price { get; }
    public decimal ExecutedVolume { get; }
    public decimal? BidVolume { get; }
    public decimal? AskVolume { get; }
    public int? TradeCount { get; }
    public string Provenance { get; }
}

public enum PriceVolumeCapability
{
    Unknown = 0,
    Unavailable = 1,
    Exact = 2
}

public enum ProfileDataQuality
{
    Unknown = 0,
    Partial = 1,
    Complete = 2,
    Invalid = 3
}

public enum AuctionProfileState
{
    NotReady = 0,
    Partial = 1,
    Ready = 2,
    Invalid = 3
}

public enum HistoricalInitializationState
{
    NotStarted = 0,
    InProgress = 1,
    Complete = 2
}

/// <summary>Normalized bar input for Profile Core. No ATAS Candle / PriceVolumeInfo.</summary>
public sealed class ProfileBarObservation
{
    public ProfileBarObservation(
        int barIndex,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        decimal open,
        decimal high,
        decimal low,
        decimal close,
        decimal totalVolume,
        IReadOnlyList<PriceVolumeObservation> priceVolumes,
        PriceVolumeCapability priceVolumeCapability,
        bool isHistorical,
        bool isCompleted,
        long sourceVersion,
        string timestampPolicyVersion = AtasTimestampNormalizer.PolicyVersion)
    {
        BarIndex = barIndex;
        StartUtc = startUtc.ToUniversalTime();
        EndUtc = endUtc.ToUniversalTime();
        Open = open;
        High = high;
        Low = low;
        Close = close;
        TotalVolume = totalVolume;
        PriceVolumes = priceVolumes ?? Array.Empty<PriceVolumeObservation>();
        PriceVolumeCapability = priceVolumeCapability;
        IsHistorical = isHistorical;
        IsCompleted = isCompleted;
        SourceVersion = sourceVersion;
        TimestampPolicyVersion = timestampPolicyVersion ?? AtasTimestampNormalizer.PolicyVersion;
    }

    public int BarIndex { get; }
    public DateTimeOffset StartUtc { get; }
    public DateTimeOffset EndUtc { get; }
    public decimal Open { get; }
    public decimal High { get; }
    public decimal Low { get; }
    public decimal Close { get; }
    public decimal TotalVolume { get; }
    public IReadOnlyList<PriceVolumeObservation> PriceVolumes { get; }
    public PriceVolumeCapability PriceVolumeCapability { get; }
    public bool IsHistorical { get; }
    public bool IsCompleted { get; }
    public long SourceVersion { get; }
    public string TimestampPolicyVersion { get; }
}
