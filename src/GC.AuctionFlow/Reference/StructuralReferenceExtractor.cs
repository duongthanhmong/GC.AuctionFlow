using GC.AuctionFlow.Composite;
using GC.AuctionFlow.Profile;

namespace GC.AuctionFlow.Reference;

/// <summary>Candidate extracted from profile/composite before registry upsert.</summary>
public sealed class ExtractedReferenceCandidate
{
    public ExtractedReferenceCandidate(
        ReferenceType type,
        decimal price,
        string sourceId,
        ReferenceSourceKind sourceKind,
        ReferenceSourceHorizon sourceHorizon,
        DateTime sourceCreatedAtUtc,
        ReferenceMaturity maturity)
    {
        Type = type;
        Price = price;
        SourceId = sourceId;
        SourceKind = sourceKind;
        SourceHorizon = sourceHorizon;
        SourceCreatedAtUtc = sourceCreatedAtUtc;
        Maturity = maturity;
    }

    public ReferenceType Type { get; }
    public decimal Price { get; }
    public string SourceId { get; }
    public ReferenceSourceKind SourceKind { get; }
    public ReferenceSourceHorizon SourceHorizon { get; }
    public DateTime SourceCreatedAtUtc { get; }
    public ReferenceMaturity Maturity { get; }
}

/// <summary>Result of deterministic extraction from Primary + Confirmed Composite only.</summary>
public sealed class StructuralReferenceExtractionResult
{
    public StructuralReferenceExtractionResult(
        IReadOnlyList<ExtractedReferenceCandidate> candidates,
        IReadOnlyList<string> unavailableVolumeReasons,
        IReadOnlyList<string> knownLimitations,
        decimal? currentPrice,
        StructuralReferenceModuleState suggestedState)
    {
        Candidates = candidates ?? Array.Empty<ExtractedReferenceCandidate>();
        UnavailableVolumeReasons = unavailableVolumeReasons ?? Array.Empty<string>();
        KnownLimitations = knownLimitations ?? Array.Empty<string>();
        CurrentPrice = currentPrice;
        SuggestedState = suggestedState;
    }

    public IReadOnlyList<ExtractedReferenceCandidate> Candidates { get; }
    public IReadOnlyList<string> UnavailableVolumeReasons { get; }
    public IReadOnlyList<string> KnownLimitations { get; }
    public decimal? CurrentPrice { get; }
    public StructuralReferenceModuleState SuggestedState { get; }
}

/// <summary>
/// Extracts profile-derived Structural References. No GPS parsing, no ATAS built-in, no Preview Composite.
/// </summary>
public static class StructuralReferenceExtractor
{
    public static StructuralReferenceExtractionResult Extract(
        PrimaryProfileSetSnapshot? profiles,
        CompositeSetSnapshot? composite,
        bool enabled)
    {
        if (!enabled)
        {
            return new StructuralReferenceExtractionResult(
                Array.Empty<ExtractedReferenceCandidate>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                null,
                StructuralReferenceModuleState.Disabled);
        }

        if (profiles is null)
        {
            return new StructuralReferenceExtractionResult(
                Array.Empty<ExtractedReferenceCandidate>(),
                Array.Empty<string>(),
                new[] { "AWAITING_PRIMARY_PROFILE" },
                null,
                StructuralReferenceModuleState.AwaitingPrimary);
        }

        var candidates = new List<ExtractedReferenceCandidate>();
        var unavailable = new List<string>();
        var limitations = new List<string>();
        var partial = false;

        var previous = profiles.PreviousAuction;
        if (previous is not null && previous.IsCompleted)
        {
            AddPrimary(
                candidates, unavailable, ref partial,
                previous,
                ReferenceMaturity.Confirmed,
                ReferenceSourceKind.PreviousPrimaryAuction,
                ReferenceSourceHorizon.PreviousPrimaryAuction,
                isCurrent: false);
        }
        else
        {
            limitations.Add("PREVIOUS_PRIMARY_ABSENT");
        }

        var current = profiles.CurrentAuction;
        if (current is not null && current.ProfileState is not AuctionProfileState.NotReady and not AuctionProfileState.Invalid)
        {
            AddPrimary(
                candidates, unavailable, ref partial,
                current,
                ReferenceMaturity.Developing,
                ReferenceSourceKind.CurrentPrimaryAuction,
                ReferenceSourceHorizon.CurrentPrimaryAuction,
                isCurrent: true);
        }
        else if (current is null || current.ProfileState == AuctionProfileState.NotReady)
        {
            return new StructuralReferenceExtractionResult(
                Array.Empty<ExtractedReferenceCandidate>(),
                unavailable,
                new[] { "AWAITING_PRIMARY_PROFILE" },
                null,
                StructuralReferenceModuleState.AwaitingPrimary);
        }
        else
        {
            limitations.Add("CURRENT_PRIMARY_INVALID");
            return new StructuralReferenceExtractionResult(
                Array.Empty<ExtractedReferenceCandidate>(),
                unavailable,
                limitations,
                null,
                StructuralReferenceModuleState.Invalid);
        }

        // Developing Composite Preview must never contribute Phase 1C references.
        if (composite?.Preview is not null)
            limitations.Add("COMPOSITE_PREVIEW_IGNORED_FOR_REFERENCES");

        var conf = composite?.Confirmed;
        if (conf is not null
            && conf.CompositeStatus is CompositeStatus.Ready or CompositeStatus.Partial)
        {
            AddComposite(candidates, unavailable, ref partial, conf);
            if (conf.CompositeStatus == CompositeStatus.Partial)
                partial = true;
        }

        var currentPrice = current.LastObservedPrice
                           ?? current.TpoProfile?.TpoPoc
                           ?? current.ProfileHigh;

        var state = candidates.Count == 0
            ? StructuralReferenceModuleState.AwaitingPrimary
            : partial
                ? StructuralReferenceModuleState.Partial
                : StructuralReferenceModuleState.Ready;

        return new StructuralReferenceExtractionResult(
            candidates,
            unavailable.Distinct(StringComparer.Ordinal).ToArray(),
            limitations.Distinct(StringComparer.Ordinal).ToArray(),
            currentPrice,
            state);
    }

    private static void AddPrimary(
        List<ExtractedReferenceCandidate> candidates,
        List<string> unavailable,
        ref bool partial,
        PrimaryAuctionProfileSnapshot auction,
        ReferenceMaturity maturity,
        ReferenceSourceKind kind,
        ReferenceSourceHorizon horizon,
        bool isCurrent)
    {
        var sourceCreated = auction.AuctionStartUtc;
        var sourceId = auction.AuctionId;

        void Add(ReferenceType type, decimal? price)
        {
            if (price is not decimal p)
                return;
            // Never fabricate zero-price references from missing data.
            candidates.Add(new ExtractedReferenceCandidate(
                type, p, sourceId, kind, horizon, sourceCreated, maturity));
        }

        if (isCurrent)
        {
            Add(ReferenceType.CurrentPrimaryAuctionHigh, auction.ProfileHigh);
            Add(ReferenceType.CurrentPrimaryAuctionLow, auction.ProfileLow);
            Add(ReferenceType.CurrentPrimaryTpoPoc, auction.TpoProfile?.TpoPoc);
            Add(ReferenceType.CurrentPrimaryTpoVah, auction.TpoProfile?.TpoVah);
            Add(ReferenceType.CurrentPrimaryTpoVal, auction.TpoProfile?.TpoVal);

            if (auction.VolumeProfile?.PriceVolumeCapability == PriceVolumeCapability.Exact)
            {
                Add(ReferenceType.CurrentPrimaryVpoc, auction.VolumeProfile.VolumePoc);
                Add(ReferenceType.CurrentPrimaryVolumeVah, auction.VolumeProfile.VolumeVah);
                Add(ReferenceType.CurrentPrimaryVolumeVal, auction.VolumeProfile.VolumeVal);
            }
            else
            {
                partial = true;
                unavailable.Add("CURRENT_PRIMARY_VOLUME_NOT_EXACT");
            }
        }
        else
        {
            Add(ReferenceType.PreviousPrimaryAuctionHigh, auction.ProfileHigh);
            Add(ReferenceType.PreviousPrimaryAuctionLow, auction.ProfileLow);
            Add(ReferenceType.PreviousPrimaryTpoPoc, auction.TpoProfile?.TpoPoc);
            Add(ReferenceType.PreviousPrimaryTpoVah, auction.TpoProfile?.TpoVah);
            Add(ReferenceType.PreviousPrimaryTpoVal, auction.TpoProfile?.TpoVal);

            if (auction.VolumeProfile?.PriceVolumeCapability == PriceVolumeCapability.Exact)
            {
                Add(ReferenceType.PreviousPrimaryVpoc, auction.VolumeProfile.VolumePoc);
                Add(ReferenceType.PreviousPrimaryVolumeVah, auction.VolumeProfile.VolumeVah);
                Add(ReferenceType.PreviousPrimaryVolumeVal, auction.VolumeProfile.VolumeVal);
            }
            else
            {
                partial = true;
                unavailable.Add("PREVIOUS_PRIMARY_VOLUME_NOT_EXACT");
            }
        }

        if (auction.ProfileState == AuctionProfileState.Partial)
            partial = true;
    }

    private static void AddComposite(
        List<ExtractedReferenceCandidate> candidates,
        List<string> unavailable,
        ref bool partial,
        ConfirmedCompositeProfileSnapshot conf)
    {
        var sourceId = conf.CompositeId;
        var sourceCreated = conf.CompositeStartUtc ?? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        var kind = ReferenceSourceKind.ConfirmedComposite;
        var horizon = ReferenceSourceHorizon.ConfirmedComposite;
        var maturity = ReferenceMaturity.Confirmed;

        void Add(ReferenceType type, decimal? price)
        {
            if (price is not decimal p)
                return;
            candidates.Add(new ExtractedReferenceCandidate(
                type, p, sourceId, kind, horizon, sourceCreated, maturity));
        }

        Add(ReferenceType.CompositeTpoPoc, conf.TpoPoc);
        Add(ReferenceType.CompositeTpoVah, conf.TpoVah);
        Add(ReferenceType.CompositeTpoVal, conf.TpoVal);

        // Explicit Composite Range High/Low from Phase 1B snapshot fields (not weekly/swing).
        Add(ReferenceType.CompositeRangeHigh, conf.ProfileHigh);
        Add(ReferenceType.CompositeRangeLow, conf.ProfileLow);

        if (conf.Aggregate?.PriceVolumeCapability == PriceVolumeCapability.Exact)
        {
            Add(ReferenceType.CompositeVpoc, conf.VolumePoc);
            Add(ReferenceType.CompositeVolumeVah, conf.VolumeVah);
            Add(ReferenceType.CompositeVolumeVal, conf.VolumeVal);
        }
        else
        {
            partial = true;
            unavailable.Add("COMPOSITE_VOLUME_NOT_EXACT");
        }
    }
}
