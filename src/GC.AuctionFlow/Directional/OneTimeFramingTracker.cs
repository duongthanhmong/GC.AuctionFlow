using GC.AuctionFlow.Profile;

namespace GC.AuctionFlow.Directional;

/// <summary>
/// One-Time Framing from completed TPO periods only.
/// ConfirmedUp/ConfirmedDown reserved — Phase 1D does not invent a confirmation threshold.
/// </summary>
public static class OneTimeFramingTracker
{
    public const string ConfirmationLimitation = "OTF_CONFIRMATION_NOT_CALIBRATED";

    public static OneTimeFramingSnapshot Evaluate(
        PrimaryAuctionProfileSnapshot? auction,
        bool enabled)
    {
        if (!enabled)
        {
            return new OneTimeFramingSnapshot(
                OneTimeFramingState.Unknown,
                null,
                null,
                0,
                0,
                Array.Empty<string>(),
                0,
                0,
                0,
                null,
                new[] { "OTF_DISABLED" },
                0);
        }

        if (auction?.TpoProfile is null)
        {
            return new OneTimeFramingSnapshot(
                OneTimeFramingState.Unknown,
                null,
                null,
                0,
                0,
                Array.Empty<string>(),
                0,
                0,
                0,
                null,
                new[] { "OTF_SOURCE_UNAVAILABLE", ConfirmationLimitation },
                0);
        }

        var tpo = auction.TpoProfile;
        var periods = (tpo.CompletedPeriods ?? Array.Empty<CompletedTpoPeriodSnapshot>())
            .OrderBy(p => p.PeriodIndex)
            .ToArray();

        var limitations = new List<string> { ConfirmationLimitation };
        if (periods.Length == 0)
            limitations.Add("OTF_NO_COMPLETED_PERIODS");

        var state = OneTimeFramingState.Unknown;
        var upStreak = 0;
        var downStreak = 0;
        var runUp = 0;
        var runDown = 0;

        for (var i = 1; i < periods.Length; i++)
        {
            var prev = periods[i - 1];
            var cur = periods[i];
            var upEv = cur.PeriodLowTick >= prev.PeriodLowTick;
            var downEv = cur.PeriodHighTick <= prev.PeriodHighTick;

            if (upEv && downEv)
            {
                state = OneTimeFramingState.Mixed;
                runUp = 0;
                runDown = 0;
                continue;
            }

            if (upEv)
            {
                if (state is OneTimeFramingState.DevelopingDown)
                {
                    state = OneTimeFramingState.Mixed;
                    runUp = 1;
                    runDown = 0;
                }
                else if (state is OneTimeFramingState.Broken or OneTimeFramingState.Mixed)
                {
                    state = OneTimeFramingState.DevelopingUp;
                    runUp = 1;
                    runDown = 0;
                }
                else
                {
                    state = OneTimeFramingState.DevelopingUp;
                    runUp++;
                    runDown = 0;
                }
            }
            else if (downEv)
            {
                if (state is OneTimeFramingState.DevelopingUp)
                {
                    state = OneTimeFramingState.Mixed;
                    runDown = 1;
                    runUp = 0;
                }
                else if (state is OneTimeFramingState.Broken or OneTimeFramingState.Mixed)
                {
                    state = OneTimeFramingState.DevelopingDown;
                    runDown = 1;
                    runUp = 0;
                }
                else
                {
                    state = OneTimeFramingState.DevelopingDown;
                    runDown++;
                    runUp = 0;
                }
            }
            else
            {
                if (state is OneTimeFramingState.DevelopingUp or OneTimeFramingState.DevelopingDown)
                    state = OneTimeFramingState.Broken;
                runUp = 0;
                runDown = 0;
            }
        }

        upStreak = runUp;
        downStreak = runDown;

        // Phase 1D: never emit Confirmed* — reserved until calibrated policy.
        if (state is OneTimeFramingState.ConfirmedUp or OneTimeFramingState.ConfirmedDown)
            state = state == OneTimeFramingState.ConfirmedUp
                ? OneTimeFramingState.DevelopingUp
                : OneTimeFramingState.DevelopingDown;

        var lastTwo = periods.Length == 0
            ? Array.Empty<string>()
            : periods.TakeLast(Math.Min(2, periods.Length)).Select(p => p.PeriodId).ToArray();

        DateTime? lastTs = periods.Length > 0 ? periods[^1].PeriodEndUtc : null;

        var categoricalKey =
            state + "|" + upStreak + "|" + downStreak + "|" + periods.Length + "|" +
            string.Join(",", lastTwo);

        return new OneTimeFramingSnapshot(
            state,
            auction.AuctionId,
            tpo.AnchorTimezone + "|" + tpo.AnchorLocalTime.ToString(@"hh\:mm", System.Globalization.CultureInfo.InvariantCulture),
            tpo.PeriodMinutes,
            periods.Length,
            lastTwo,
            upStreak,
            downStreak,
            StateVersionFromKey(categoricalKey),
            lastTs,
            limitations.Distinct(StringComparer.Ordinal).ToArray(),
            StateVersionFromKey(categoricalKey));
    }

    private static long StateVersionFromKey(string key)
    {
        // Deterministic non-cryptographic version token from categorical identity (no SHA I/O).
        unchecked
        {
            long h = 17;
            foreach (var ch in key)
                h = h * 31 + ch;
            return h == 0 ? 1 : Math.Abs(h);
        }
    }
}
