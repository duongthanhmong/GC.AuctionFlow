namespace GC.AuctionFlow.Runtime;

/// <summary>Operational seeds for P0-08A contract/data gate (subject to sensitivity test).</summary>
public sealed class RuntimeGateConfig
{
    /// <summary>GC COMEX tick size seed value, subject to sensitivity test.</summary>
    public const decimal DefaultExpectedTickSize = 0.1m;

    /// <summary>Near-expiration calendar-day threshold seed value, subject to sensitivity test.</summary>
    public const int DefaultNearExpirationCalendarDays = 14;

    public RuntimeGateConfig(
        decimal expectedTickSize = DefaultExpectedTickSize,
        int nearExpirationCalendarDays = DefaultNearExpirationCalendarDays)
    {
        if (expectedTickSize <= 0m)
            throw new ArgumentOutOfRangeException(nameof(expectedTickSize));
        if (nearExpirationCalendarDays < 0)
            throw new ArgumentOutOfRangeException(nameof(nearExpirationCalendarDays));

        ExpectedTickSize = expectedTickSize;
        NearExpirationCalendarDays = nearExpirationCalendarDays;
    }

    public decimal ExpectedTickSize { get; }
    public int NearExpirationCalendarDays { get; }
}
