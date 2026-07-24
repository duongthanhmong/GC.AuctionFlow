using GC.AuctionFlow.Profile;

namespace GC.AuctionFlow.Directional;

/// <summary>
/// Exact-tick pairwise comparison + conservative DirectionalAuctionState decision table.
/// Missing exact volume → Unavailable (never zero/inferred). No ATR/tolerance/score.
/// </summary>
public static class PairwiseAuctionComparer
{
    public static PairwiseAuctionComparisonEvidence Compare(
        PrimaryAuctionProfileSnapshot? previous,
        PrimaryAuctionProfileSnapshot? current,
        decimal tickSize,
        string? expectedTimestampPolicy = null)
    {
        var limitations = new List<string>();
        var conflicts = new List<string>();
        _ = expectedTimestampPolicy; // reserved for host-level policy gate

        if (previous is null || current is null)
        {
            limitations.Add("PAIRWISE_EVIDENCE_UNAVAILABLE");
            return UnknownPair("", "", limitations);
        }

        if (tickSize <= 0m)
        {
            limitations.Add("TICK_SIZE_INVALID");
            return UnknownPair(previous.AuctionId, current.AuctionId, limitations);
        }

        if (!string.Equals(previous.AuctionId, current.AuctionId, StringComparison.Ordinal)
            && previous.AuctionEndUtc > current.AuctionStartUtc
            && previous.IsCompleted && current.IsCompleted)
        {
            // Allow equal boundaries; fail closed on clear lookahead overlap of completed auctions.
            limitations.Add("AUCTION_TEMPORAL_OVERLAP");
        }

        var grid = new PriceGrid(tickSize);
        var prevTpo = previous.TpoProfile;
        var curTpo = current.TpoProfile;

        if (prevTpo is null || curTpo is null
            || prevTpo.TpoPoc is null || curTpo.TpoPoc is null
            || prevTpo.TpoVah is null || curTpo.TpoVah is null
            || prevTpo.TpoVal is null || curTpo.TpoVal is null)
        {
            limitations.Add("TPO_VALUE_EVIDENCE_UNAVAILABLE");
            return UnknownPair(previous.AuctionId, current.AuctionId, limitations);
        }

        if (!TryTicks(grid, prevTpo.TpoVal.Value, prevTpo.TpoVah.Value, prevTpo.TpoPoc.Value,
                out var pVal, out var pVah, out var pPoc)
            || !TryTicks(grid, curTpo.TpoVal.Value, curTpo.TpoVah.Value, curTpo.TpoPoc.Value,
                out var cVal, out var cVah, out var cPoc))
        {
            limitations.Add("TPO_TICK_ALIGNMENT_FAILED");
            return UnknownPair(previous.AuctionId, current.AuctionId, limitations);
        }

        if (pVal > pVah || cVal > cVah)
        {
            limitations.Add("TPO_VALUE_INVERTED");
            return UnknownPair(previous.AuctionId, current.AuctionId, limitations);
        }

        var tpoRel = ClassifyValueRelationship(pVal, pVah, cVal, cVah);
        var tpoMid = CompareSum(cVal + cVah, pVal + pVah);
        var tpoPocMig = CompareTicks(cPoc, pPoc);

        var exactVol = IsExactVolume(previous.VolumeProfile) && IsExactVolume(current.VolumeProfile);
        MigrationDirection volMid = MigrationDirection.Unavailable;
        MigrationDirection volPocMig = MigrationDirection.Unavailable;
        ValueRelationship volRel = ValueRelationship.Unavailable;
        decimal? pVpoc = null, cVpoc = null, pVolVah = null, pVolVal = null, cVolVah = null, cVolVal = null;

        if (exactVol)
        {
            pVpoc = previous.VolumeProfile!.VolumePoc;
            cVpoc = current.VolumeProfile!.VolumePoc;
            pVolVah = previous.VolumeProfile.VolumeVah;
            pVolVal = previous.VolumeProfile.VolumeVal;
            cVolVah = current.VolumeProfile.VolumeVah;
            cVolVal = current.VolumeProfile.VolumeVal;

            if (pVpoc is null || cVpoc is null || pVolVah is null || pVolVal is null
                || cVolVah is null || cVolVal is null
                || !TryTicks(grid, pVolVal.Value, pVolVah.Value, pVpoc.Value, out var pvVal, out var pvVah, out var pvPoc)
                || !TryTicks(grid, cVolVal.Value, cVolVah.Value, cVpoc.Value, out var cvVal, out var cvVah, out var cvPoc)
                || pvVal > pvVah || cvVal > cvVah)
            {
                exactVol = false;
                limitations.Add("EXACT_VOLUME_INCOMPLETE");
                volMid = MigrationDirection.Unavailable;
                volPocMig = MigrationDirection.Unavailable;
                volRel = ValueRelationship.Unavailable;
                pVpoc = cVpoc = pVolVah = pVolVal = cVolVah = cVolVal = null;
            }
            else
            {
                volRel = ClassifyValueRelationship(pvVal, pvVah, cvVal, cvVah);
                volMid = CompareSum(cvVal + cvVah, pvVal + pvVah);
                volPocMig = CompareTicks(cvPoc, pvPoc);
            }
        }
        else
        {
            limitations.Add("EXACT_VOLUME_UNAVAILABLE");
        }

        var rangeMig = MigrationDirection.Unavailable;
        if (previous.ProfileHigh is decimal ph && previous.ProfileLow is decimal pl
            && current.ProfileHigh is decimal ch && current.ProfileLow is decimal cl
            && grid.TryToTickIndex(ph, out var pHi) && grid.TryToTickIndex(pl, out var pLo)
            && grid.TryToTickIndex(ch, out var cHi) && grid.TryToTickIndex(cl, out var cLo)
            && pHi >= pLo && cHi >= cLo)
        {
            rangeMig = CompareTicks(cHi - cLo, pHi - pLo);
        }

        CollectConflicts(tpoPocMig, volPocMig, tpoMid, volMid, conflicts);
        var state = ClassifyState(tpoRel, tpoPocMig, tpoMid, volPocMig, volMid, exactVol, conflicts, cPoc, pVal, pVah, cVal, cVah);

        return new PairwiseAuctionComparisonEvidence(
            previous.AuctionId,
            current.AuctionId,
            prevTpo.TpoPoc,
            curTpo.TpoPoc,
            prevTpo.TpoVah,
            prevTpo.TpoVal,
            curTpo.TpoVah,
            curTpo.TpoVal,
            pVpoc,
            cVpoc,
            pVolVah,
            pVolVal,
            cVolVah,
            cVolVal,
            tpoMid,
            volMid,
            tpoRel,
            volRel,
            tpoPocMig,
            volPocMig,
            rangeMig,
            exactVol,
            compatible: true,
            conflicts,
            limitations.Distinct(StringComparer.Ordinal).ToArray(),
            state);
    }

    private static DirectionalAuctionState ClassifyState(
        ValueRelationship tpoRel,
        MigrationDirection tpoPoc,
        MigrationDirection tpoMid,
        MigrationDirection volPoc,
        MigrationDirection volMid,
        bool exactVol,
        List<string> conflicts,
        long currentPocTick,
        long prevVal,
        long prevVah,
        long curVal,
        long curVah)
    {
        if (conflicts.Count > 0)
            return DirectionalAuctionState.Conflicted;

        var volNotLower = !exactVol || (volPoc != MigrationDirection.Lower && volMid != MigrationDirection.Lower);
        var volNotHigher = !exactVol || (volPoc != MigrationDirection.Higher && volMid != MigrationDirection.Higher);
        var volNotContradictUp = !exactVol || (volPoc != MigrationDirection.Lower && volMid != MigrationDirection.Lower);
        var volNotContradictDown = !exactVol || (volPoc != MigrationDirection.Higher && volMid != MigrationDirection.Higher);

        if (tpoRel == ValueRelationship.FullyAbove
            && tpoPoc == MigrationDirection.Higher
            && volNotLower
            && conflicts.Count == 0)
            return DirectionalAuctionState.UpDiscovery;

        if (tpoRel == ValueRelationship.FullyBelow
            && tpoPoc == MigrationDirection.Lower
            && volNotHigher
            && conflicts.Count == 0)
            return DirectionalAuctionState.DownDiscovery;

        var overlaps = tpoRel is ValueRelationship.Overlapping
            or ValueRelationship.OverlappingHigher
            or ValueRelationship.OverlappingLower
            or ValueRelationship.Inside
            or ValueRelationship.Equal
            or ValueRelationship.Outside;

        if (overlaps
            && tpoPoc == MigrationDirection.Higher
            && tpoMid == MigrationDirection.Higher
            && volNotContradictUp)
            return DirectionalAuctionState.UpRotation;

        if (overlaps
            && tpoPoc == MigrationDirection.Lower
            && tpoMid == MigrationDirection.Lower
            && volNotContradictDown)
            return DirectionalAuctionState.DownRotation;

        var balanceShape = tpoRel is ValueRelationship.Overlapping
            or ValueRelationship.OverlappingHigher
            or ValueRelationship.OverlappingLower
            or ValueRelationship.Inside
            or ValueRelationship.Equal;

        var pocUnchanged = tpoPoc == MigrationDirection.Unchanged;
        var pocInsideOverlap = IsInsideOverlap(currentPocTick, prevVal, prevVah, curVal, curVah);
        var noDirectionalMigration =
            tpoPoc is not (MigrationDirection.Higher or MigrationDirection.Lower)
            && tpoMid is not (MigrationDirection.Higher or MigrationDirection.Lower);

        if (balanceShape
            && conflicts.Count == 0
            && (pocUnchanged || (pocInsideOverlap && noDirectionalMigration) || noDirectionalMigration))
            return DirectionalAuctionState.Balance;

        if (tpoRel == ValueRelationship.Unavailable
            || tpoPoc == MigrationDirection.Unavailable)
            return DirectionalAuctionState.Unknown;

        return DirectionalAuctionState.Transition;
    }

    private static bool IsInsideOverlap(long poc, long pVal, long pVah, long cVal, long cVah)
    {
        var oLo = Math.Max(pVal, cVal);
        var oHi = Math.Min(pVah, cVah);
        if (oLo > oHi)
            return poc >= Math.Min(pVal, cVal) && poc <= Math.Max(pVah, cVah);
        return poc >= oLo && poc <= oHi;
    }

    private static void CollectConflicts(
        MigrationDirection tpoPoc,
        MigrationDirection volPoc,
        MigrationDirection tpoMid,
        MigrationDirection volMid,
        List<string> conflicts)
    {
        if (IsOpposite(tpoPoc, volPoc))
            conflicts.Add("TPO_POC_VS_VPOC_OPPOSITE");
        if (IsOpposite(tpoMid, volMid))
            conflicts.Add("TPO_MID_VS_VOLUME_MID_OPPOSITE");
    }

    private static bool IsOpposite(MigrationDirection a, MigrationDirection b) =>
        (a == MigrationDirection.Higher && b == MigrationDirection.Lower)
        || (a == MigrationDirection.Lower && b == MigrationDirection.Higher);

    public static ValueRelationship ClassifyValueRelationship(long prevVal, long prevVah, long curVal, long curVah)
    {
        if (curVal > prevVah)
            return ValueRelationship.FullyAbove;
        if (curVah < prevVal)
            return ValueRelationship.FullyBelow;
        if (curVal == prevVal && curVah == prevVah)
            return ValueRelationship.Equal;
        if (curVal >= prevVal && curVah <= prevVah)
            return ValueRelationship.Inside;
        if (prevVal >= curVal && prevVah <= curVah)
            return ValueRelationship.Outside;

        var prevMid = prevVal + prevVah;
        var curMid = curVal + curVah;
        if (curMid > prevMid)
            return ValueRelationship.OverlappingHigher;
        if (curMid < prevMid)
            return ValueRelationship.OverlappingLower;
        return ValueRelationship.Overlapping;
    }

    public static MigrationDirection CompareTicks(long current, long previous)
    {
        if (current > previous) return MigrationDirection.Higher;
        if (current < previous) return MigrationDirection.Lower;
        return MigrationDirection.Unchanged;
    }

    private static MigrationDirection CompareSum(long currentSum, long previousSum) =>
        CompareTicks(currentSum, previousSum);

    private static bool IsExactVolume(VolumeProfileSnapshot? vol) =>
        vol is not null
        && vol.PriceVolumeCapability == PriceVolumeCapability.Exact
        && vol.VolumePoc is not null
        && vol.VolumeVah is not null
        && vol.VolumeVal is not null;

    private static bool TryTicks(
        PriceGrid grid,
        decimal val,
        decimal vah,
        decimal poc,
        out long valTick,
        out long vahTick,
        out long pocTick)
    {
        valTick = vahTick = pocTick = 0;
        return grid.TryToTickIndex(val, out valTick)
               && grid.TryToTickIndex(vah, out vahTick)
               && grid.TryToTickIndex(poc, out pocTick);
    }

    private static PairwiseAuctionComparisonEvidence UnknownPair(
        string prevId,
        string curId,
        IReadOnlyList<string> limitations) =>
        new(
            prevId, curId,
            null, null, null, null, null, null,
            null, null, null, null, null, null,
            MigrationDirection.Unavailable,
            MigrationDirection.Unavailable,
            ValueRelationship.Unavailable,
            ValueRelationship.Unavailable,
            MigrationDirection.Unavailable,
            MigrationDirection.Unavailable,
            MigrationDirection.Unavailable,
            exactVolumeAvailable: false,
            compatible: false,
            Array.Empty<string>(),
            limitations,
            DirectionalAuctionState.Unknown);
}
