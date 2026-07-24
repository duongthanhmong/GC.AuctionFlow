using GC.AuctionFlow.Reference;

namespace GC.AuctionFlow.Episode;

/// <summary>Maps ReferenceType → interaction role. Semantic type only — never label text.</summary>
public static class ReferenceInteractionRoleMapper
{
    public static ReferenceInteractionRole Map(ReferenceType type) => type switch
    {
        ReferenceType.PreviousPrimaryAuctionHigh => ReferenceInteractionRole.UpperBoundary,
        ReferenceType.PreviousPrimaryTpoVah => ReferenceInteractionRole.UpperBoundary,
        ReferenceType.PreviousPrimaryVolumeVah => ReferenceInteractionRole.UpperBoundary,
        ReferenceType.CompositeRangeHigh => ReferenceInteractionRole.UpperBoundary,
        ReferenceType.CompositeTpoVah => ReferenceInteractionRole.UpperBoundary,
        ReferenceType.CompositeVolumeVah => ReferenceInteractionRole.UpperBoundary,

        ReferenceType.PreviousPrimaryAuctionLow => ReferenceInteractionRole.LowerBoundary,
        ReferenceType.PreviousPrimaryTpoVal => ReferenceInteractionRole.LowerBoundary,
        ReferenceType.PreviousPrimaryVolumeVal => ReferenceInteractionRole.LowerBoundary,
        ReferenceType.CompositeRangeLow => ReferenceInteractionRole.LowerBoundary,
        ReferenceType.CompositeTpoVal => ReferenceInteractionRole.LowerBoundary,
        ReferenceType.CompositeVolumeVal => ReferenceInteractionRole.LowerBoundary,

        ReferenceType.PreviousPrimaryTpoPoc => ReferenceInteractionRole.Centerline,
        ReferenceType.PreviousPrimaryVpoc => ReferenceInteractionRole.Centerline,
        ReferenceType.CompositeTpoPoc => ReferenceInteractionRole.Centerline,
        ReferenceType.CompositeVpoc => ReferenceInteractionRole.Centerline,

        // Current Primary (Developing) — excluded from eligibility; role unsupported if presented.
        _ => ReferenceInteractionRole.Unsupported
    };

    public static bool IsPhase1EEligibleType(ReferenceType type) =>
        Map(type) is not ReferenceInteractionRole.Unsupported;

    /// <summary>Canonical outside side for boundary roles. Centerline has none.</summary>
    public static ReferenceSidePosition? CanonicalOutsideSide(ReferenceInteractionRole role) => role switch
    {
        ReferenceInteractionRole.UpperBoundary => ReferenceSidePosition.Above,
        ReferenceInteractionRole.LowerBoundary => ReferenceSidePosition.Below,
        _ => null
    };

    public static ReferenceSidePosition? CanonicalInsideSide(ReferenceInteractionRole role) => role switch
    {
        ReferenceInteractionRole.UpperBoundary => ReferenceSidePosition.Below,
        ReferenceInteractionRole.LowerBoundary => ReferenceSidePosition.Above,
        _ => null
    };
}
