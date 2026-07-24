using GC.AuctionFlow.Composite;
using GC.AuctionFlow.Profile;

namespace GC.AuctionFlow.UI;

/// <summary>Immutable overlay levels for primary + composite profile lines. No calculation / no mutable engines.</summary>
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
        bool showCompositePreview = true)
    {
        var list = new List<ProfileOverlayLevel>();
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
    CompositeVolumeVal = 11
}

public sealed class ProfileOverlayLevel
{
    public ProfileOverlayLevel(string label, decimal price, ProfileOverlayKind kind)
    {
        Label = label;
        Price = price;
        Kind = kind;
    }

    public string Label { get; }
    public decimal Price { get; }
    public ProfileOverlayKind Kind { get; }
}
