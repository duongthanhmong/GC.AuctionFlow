using GC.AuctionFlow.Profile;

namespace GC.AuctionFlow.UI;

/// <summary>Immutable overlay levels for primary profile lines. No calculation / no mutable engines.</summary>
public sealed class PrimaryProfileOverlayViewModel
{
    public PrimaryProfileOverlayViewModel(IReadOnlyList<ProfileOverlayLevel> levels)
    {
        Levels = levels ?? Array.Empty<ProfileOverlayLevel>();
    }

    public IReadOnlyList<ProfileOverlayLevel> Levels { get; }

    public static PrimaryProfileOverlayViewModel FromProfiles(
        PrimaryProfileSetSnapshot? profiles,
        bool showPrevious)
    {
        var list = new List<ProfileOverlayLevel>();
        if (profiles?.CurrentAuction is { } cur)
            AddAuction(list, cur, "Current");
        if (showPrevious && profiles?.PreviousAuction is { } prev)
            AddAuction(list, prev, "Previous");
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
}

public enum ProfileOverlayKind
{
    TpoPoc = 0,
    TpoVah = 1,
    TpoVal = 2,
    Vpoc = 3,
    VolumeVah = 4,
    VolumeVal = 5
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
