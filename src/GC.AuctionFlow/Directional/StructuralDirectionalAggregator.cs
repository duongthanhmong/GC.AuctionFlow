using GC.AuctionFlow.Profile;

namespace GC.AuctionFlow.Directional;

/// <summary>Structural multi-day aggregation from completed auctions only. No fixed N-day window.</summary>
public static class StructuralDirectionalAggregator
{
    public static StructuralAggregationResult Aggregate(
        IReadOnlyList<PrimaryAuctionProfileSnapshot> completedAuctions,
        decimal tickSize,
        string? timestampPolicy = null)
    {
        var limitations = new List<string>();
        if (completedAuctions is null || completedAuctions.Count == 0)
        {
            limitations.Add("INSUFFICIENT_COMPLETED_HISTORY");
            return new StructuralAggregationResult(
                DirectionalAuctionState.Unknown,
                0,
                Array.Empty<PairwiseAuctionComparisonEvidence>(),
                Array.Empty<string>(),
                limitations);
        }

        var ordered = completedAuctions
            .Where(a => a.IsCompleted)
            .OrderBy(a => a.AuctionStartUtc)
            .ThenBy(a => a.AuctionId, StringComparer.Ordinal)
            .ToArray();

        if (ordered.Length != completedAuctions.Count(a => a.IsCompleted))
            limitations.Add("DEVELOPING_AUCTION_EXCLUDED");

        var pairs = new List<PairwiseAuctionComparisonEvidence>();
        for (var i = 1; i < ordered.Length; i++)
        {
            var evidence = PairwiseAuctionComparer.Compare(ordered[i - 1], ordered[i], tickSize, timestampPolicy);
            if (evidence.Compatible)
                pairs.Add(evidence);
        }

        if (pairs.Count < 2)
        {
            limitations.Add("INSUFFICIENT_COMPLETED_HISTORY");
            limitations.Add("COMPATIBLE_TRANSITION_COUNT=" + pairs.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return new StructuralAggregationResult(
                DirectionalAuctionState.Unknown,
                pairs.Count,
                pairs,
                ordered.Select(a => a.AuctionId).ToArray(),
                limitations.Distinct(StringComparer.Ordinal).ToArray());
        }

        var states = pairs.Select(p => p.ClassifiedState).ToArray();
        var aggregated = AggregateStates(states);
        return new StructuralAggregationResult(
            aggregated,
            pairs.Count,
            pairs,
            ordered.Select(a => a.AuctionId).ToArray(),
            limitations.Distinct(StringComparer.Ordinal).ToArray());
    }

    public static DirectionalAuctionState AggregateStates(IReadOnlyList<DirectionalAuctionState> states)
    {
        if (states is null || states.Count == 0)
            return DirectionalAuctionState.Unknown;

        var upward = states.All(IsUpward);
        var downward = states.All(IsDownward);
        var allBalance = states.All(s => s == DirectionalAuctionState.Balance);
        var hasUp = states.Any(IsUpward);
        var hasDown = states.Any(IsDownward);
        var hasConflicted = states.Any(s => s == DirectionalAuctionState.Conflicted);

        if (hasConflicted)
            return DirectionalAuctionState.Conflicted;
        if (hasUp && hasDown)
            return DirectionalAuctionState.Conflicted;
        if (upward)
            return states.Any(s => s == DirectionalAuctionState.UpDiscovery)
                ? DirectionalAuctionState.UpDiscovery
                : DirectionalAuctionState.UpRotation;
        if (downward)
            return states.Any(s => s == DirectionalAuctionState.DownDiscovery)
                ? DirectionalAuctionState.DownDiscovery
                : DirectionalAuctionState.DownRotation;
        if (allBalance)
            return DirectionalAuctionState.Balance;
        return DirectionalAuctionState.Transition;
    }

    private static bool IsUpward(DirectionalAuctionState s) =>
        s is DirectionalAuctionState.UpDiscovery or DirectionalAuctionState.UpRotation;

    private static bool IsDownward(DirectionalAuctionState s) =>
        s is DirectionalAuctionState.DownDiscovery or DirectionalAuctionState.DownRotation;
}

public sealed class StructuralAggregationResult
{
    public StructuralAggregationResult(
        DirectionalAuctionState state,
        int compatibleTransitionCount,
        IReadOnlyList<PairwiseAuctionComparisonEvidence> pairwiseEvidence,
        IReadOnlyList<string> sourceAuctionIds,
        IReadOnlyList<string> limitations)
    {
        State = state;
        CompatibleTransitionCount = compatibleTransitionCount;
        PairwiseEvidence = pairwiseEvidence ?? Array.Empty<PairwiseAuctionComparisonEvidence>();
        SourceAuctionIds = sourceAuctionIds ?? Array.Empty<string>();
        Limitations = limitations ?? Array.Empty<string>();
    }

    public DirectionalAuctionState State { get; }
    public int CompatibleTransitionCount { get; }
    public IReadOnlyList<PairwiseAuctionComparisonEvidence> PairwiseEvidence { get; }
    public IReadOnlyList<string> SourceAuctionIds { get; }
    public IReadOnlyList<string> Limitations { get; }
}
