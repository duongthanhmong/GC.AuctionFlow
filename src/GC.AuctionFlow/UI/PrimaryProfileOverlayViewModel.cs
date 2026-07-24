using GC.AuctionFlow.Composite;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Reference;

namespace GC.AuctionFlow.UI;

/// <summary>
/// Immutable overlay levels for primary + composite + structural reference lines.
/// When Structural Reference Overlay is ON, Primary/Composite visual levels are omitted
/// to avoid duplicate constituent labels at the same exact zones (REFERENCE_OVERLAY_POLICY_V1).
/// Engine snapshots are unchanged.
/// </summary>
public sealed class PrimaryProfileOverlayViewModel
{
    public PrimaryProfileOverlayViewModel(IReadOnlyList<ProfileOverlayLevel> levels)
    {
        Levels = levels ?? Array.Empty<ProfileOverlayLevel>();
    }

    public IReadOnlyList<ProfileOverlayLevel> Levels { get; }

    public static PrimaryProfileOverlayViewModel FromProfiles(
        PrimaryProfileSetSnapshot? profiles,
        bool showPrevious,
        CompositeSetSnapshot? composite = null,
        bool enableCompositeOverlay = false,
        bool showCompositePreview = true,
        StructuralReferenceSetSnapshot? structuralReferences = null,
        bool enableStructuralReferenceOverlay = false)
    {
        var list = new List<ProfileOverlayLevel>();

        var refOverlayActive = enableStructuralReferenceOverlay
            && structuralReferences is not null
            && structuralReferences.ModuleState is StructuralReferenceModuleState.Ready or StructuralReferenceModuleState.Partial;

        if (refOverlayActive)
        {
            // Restrained Structural Reference overlay owns the chart lines/labels.
            // Primary/Composite profile overlays are suppressed while REF overlay is ON
            // to prevent duplicate constituent labels at the same exact zones.
            AddStructuralReferences(list, structuralReferences!);
            return new PrimaryProfileOverlayViewModel(list);
        }

        if (profiles?.CurrentAuction is { } cur)
            AddAuction(list, cur, "Current");
        if (showPrevious && profiles?.PreviousAuction is { } prev)
            AddAuction(list, prev, "Previous");

        if (enableCompositeOverlay && composite?.Confirmed is { } conf
            && conf.CompositeStatus is CompositeStatus.Ready or CompositeStatus.Partial)
        {
            AddAggregate(list, conf.TpoPoc, conf.TpoVah, conf.TpoVal, conf.VolumePoc, conf.VolumeVah, conf.VolumeVal, "Confirmed Composite");
            if (showCompositePreview && composite.Preview is { } preview)
                AddAggregate(list,
                    preview.Aggregate.TpoPoc, preview.Aggregate.TpoVah, preview.Aggregate.TpoVal,
                    preview.Aggregate.VolumePoc, preview.Aggregate.VolumeVah, preview.Aggregate.VolumeVal,
                    "Preview Composite");
        }

        return new PrimaryProfileOverlayViewModel(list);
    }

    private static void AddStructuralReferences(List<ProfileOverlayLevel> list, StructuralReferenceSetSnapshot set)
    {
        var currentTick = set.Nearest?.CurrentPriceTick;
        // Unique-zone rows only (REFERENCE_OVERLAY_POLICY_V1). Label collision is applied
        // in the renderer with real chart Y so viewport/scale drives suppression deterministically.
        var rows = ReferenceOverlayDisplayPolicy.BuildUniqueZoneRows(set, currentTick);

        foreach (var row in rows)
        {
            list.Add(new ProfileOverlayLevel(
                row.Label,
                row.Price,
                ProfileOverlayKind.StructuralReference,
                row.ConstituentReferenceIds,
                showLabel: row.ShowLabel,
                showLine: row.ShowLine,
                overlayPriority: row.Priority,
                maturityToken: row.MaturityToken,
                overlayPolicyVersion: ReferenceOverlayDisplayPolicy.PolicyVersion));
        }
    }

    private static void AddAuction(List<ProfileOverlayLevel> list, PrimaryAuctionProfileSnapshot a, string scope)
    {
        if (a.TpoProfile?.TpoPoc is decimal tpoPoc)
            list.Add(new ProfileOverlayLevel(scope + " TPO POC", tpoPoc, ProfileOverlayKind.TpoPoc));
        if (a.TpoProfile?.TpoVah is decimal tpoVah)
            list.Add(new ProfileOverlayLevel(scope + " TPO VAH", tpoVah, ProfileOverlayKind.TpoVah));
        if (a.TpoProfile?.TpoVal is decimal tpoVal)
            list.Add(new ProfileOverlayLevel(scope + " TPO VAL", tpoVal, ProfileOverlayKind.TpoVal));
        if (a.VolumeProfile?.VolumePoc is decimal vpoc)
            list.Add(new ProfileOverlayLevel(scope + " VPOC", vpoc, ProfileOverlayKind.Vpoc));
        if (a.VolumeProfile?.VolumeVah is decimal vvah)
            list.Add(new ProfileOverlayLevel(scope + " VOL VAH", vvah, ProfileOverlayKind.VolumeVah));
        if (a.VolumeProfile?.VolumeVal is decimal vval)
            list.Add(new ProfileOverlayLevel(scope + " VOL VAL", vval, ProfileOverlayKind.VolumeVal));
    }

    private static void AddAggregate(
        List<ProfileOverlayLevel> list,
        decimal? tpoPoc, decimal? tpoVah, decimal? tpoVal,
        decimal? volPoc, decimal? volVah, decimal? volVal,
        string scope)
    {
        if (tpoPoc is decimal tp)
            list.Add(new ProfileOverlayLevel(scope + " TPO POC", tp, ProfileOverlayKind.CompositeTpoPoc));
        if (tpoVah is decimal th)
            list.Add(new ProfileOverlayLevel(scope + " TPO VAH", th, ProfileOverlayKind.CompositeTpoVah));
        if (tpoVal is decimal tl)
            list.Add(new ProfileOverlayLevel(scope + " TPO VAL", tl, ProfileOverlayKind.CompositeTpoVal));
        if (volPoc is decimal vp)
            list.Add(new ProfileOverlayLevel(scope + " VPOC", vp, ProfileOverlayKind.CompositeVpoc));
        if (volVah is decimal vh)
            list.Add(new ProfileOverlayLevel(scope + " VOL VAH", vh, ProfileOverlayKind.CompositeVolumeVah));
        if (volVal is decimal vl)
            list.Add(new ProfileOverlayLevel(scope + " VOL VAL", vl, ProfileOverlayKind.CompositeVolumeVal));
    }
}

public enum ProfileOverlayKind
{
    TpoPoc = 0,
    TpoVah = 1,
    TpoVal = 2,
    Vpoc = 3,
    VolumeVah = 4,
    VolumeVal = 5,
    CompositeTpoPoc = 6,
    CompositeTpoVah = 7,
    CompositeTpoVal = 8,
    CompositeVpoc = 9,
    CompositeVolumeVah = 10,
    CompositeVolumeVal = 11,
    StructuralReference = 12
}

public sealed class ProfileOverlayLevel
{
    public ProfileOverlayLevel(
        string label,
        decimal price,
        ProfileOverlayKind kind,
        IReadOnlyList<string>? constituentReferenceIds = null,
        bool showLabel = true,
        bool showLine = true,
        int overlayPriority = 0,
        string? maturityToken = null,
        string? overlayPolicyVersion = null)
    {
        Label = label;
        Price = price;
        Kind = kind;
        ConstituentReferenceIds = constituentReferenceIds ?? Array.Empty<string>();
        ShowLabel = showLabel;
        ShowLine = showLine;
        OverlayPriority = overlayPriority;
        MaturityToken = maturityToken ?? "";
        OverlayPolicyVersion = overlayPolicyVersion ?? "";
    }

    public string Label { get; }
    public decimal Price { get; }
    public ProfileOverlayKind Kind { get; }
    public IReadOnlyList<string> ConstituentReferenceIds { get; }
    public bool ShowLabel { get; }
    public bool ShowLine { get; }
    public int OverlayPriority { get; }
    public string MaturityToken { get; }
    public string OverlayPolicyVersion { get; }
}
