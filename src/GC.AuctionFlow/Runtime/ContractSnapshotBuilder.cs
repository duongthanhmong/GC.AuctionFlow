using GC.AuctionFlow.Probe;

namespace GC.AuctionFlow.Runtime;

/// <summary>Builds ContractSnapshot from observed identity without inventing roll calendars.</summary>
public static class ContractSnapshotBuilder
{
    public static ContractSnapshot Build(
        ObservedInstrumentSnapshot? observed,
        string expectedInstrumentCode,
        RuntimeGateConfig config,
        DateTime timestampUtc)
    {
        var limitations = new List<string>
        {
            "NO_INVENTED_NEXT_CONTRACT_VOLUME",
            "NO_EXTERNAL_ROLL_CALENDAR",
            "ROLL_ACTIVE_REQUIRES_EVIDENCE"
        };

        if (observed is null)
        {
            return new ContractSnapshot(
                timestampUtc,
                null, null, null, null, null,
                "Unknown",
                expectedInstrumentCode ?? "",
                InstrumentMatchState.Unknown,
                ExpirationState.Unknown,
                null,
                RollState.Unknown,
                limitations,
                "MissingObservedInstrument");
        }

        var identityKey = string.IsNullOrWhiteSpace(observed.IdentityKey) ? "Unknown" : observed.IdentityKey.Trim();
        var expected = (expectedInstrumentCode ?? "").Trim();
        var code = FirstNonEmpty(observed.SecurityCode, observed.InstrumentInfoInstrument, observed.Instrument);
        var exchange = FirstNonEmpty(observed.Exchange, observed.InstrumentInfoExchange);
        var tick = observed.TickSize ?? observed.InstrumentInfoTickSize;
        var expiration = observed.Expiration;

        var match = ClassifyMatch(identityKey, expected);
        var (expState, days) = ClassifyExpiration(expiration, timestampUtc, config.NearExpirationCalendarDays);
        // No next-contract volume / calendar scrape in P0-08A → Unknown unless we only know identity is present.
        var roll = RollState.Unknown;
        if (match == InstrumentMatchState.Match && expState is ExpirationState.Valid or ExpirationState.NearExpiration)
            roll = RollState.Unknown; // still Unknown: ActiveRoll/NormalContract not claimed without roll evidence

        var provenance = "ObservedInstrumentSnapshot";
        if (identityKey == "Unknown")
            limitations.Add("IDENTITY_KEY_UNKNOWN");

        return new ContractSnapshot(
            timestampUtc,
            code,
            observed.SecurityId,
            exchange,
            expiration,
            tick,
            identityKey,
            expected,
            match,
            expState,
            days,
            roll,
            limitations,
            provenance);
    }

    public static InstrumentMatchState ClassifyMatch(string identityKey, string expectedInstrumentCode)
    {
        if (string.IsNullOrWhiteSpace(expectedInstrumentCode))
            return InstrumentMatchState.Unknown;
        if (string.IsNullOrWhiteSpace(identityKey) || string.Equals(identityKey, "Unknown", StringComparison.Ordinal))
            return InstrumentMatchState.Unknown;
        return string.Equals(identityKey.Trim(), expectedInstrumentCode.Trim(), StringComparison.OrdinalIgnoreCase)
            ? InstrumentMatchState.Match
            : InstrumentMatchState.Mismatch;
    }

    public static (ExpirationState State, int? Days) ClassifyExpiration(
        DateTime? contractExpiration,
        DateTime timestampUtc,
        int nearExpirationCalendarDays)
    {
        if (contractExpiration is null)
            return (ExpirationState.Unknown, null);

        var expDate = contractExpiration.Value.Date;
        var today = timestampUtc.Date;
        var days = (expDate - today).Days;
        if (days < 0)
            return (ExpirationState.Expired, days);
        if (days <= nearExpirationCalendarDays)
            return (ExpirationState.NearExpiration, days);
        return (ExpirationState.Valid, days);
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v))
                return v.Trim();
        }

        return null;
    }
}
