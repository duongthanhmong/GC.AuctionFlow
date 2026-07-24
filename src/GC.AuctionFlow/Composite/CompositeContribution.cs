using GC.AuctionFlow.Profile;
using System.Globalization;
using System.Text;

namespace GC.AuctionFlow.Composite;

/// <summary>Immutable completed (or developing-preview) auction contribution from Phase 1A snapshots only.</summary>
public sealed class CompositeAuctionContribution
{
    public const string ContributionSchemaVersion = "1.0.0";

    public CompositeAuctionContribution(
        string auctionId,
        DateTime auctionStartUtc,
        DateTime auctionEndUtc,
        DateOnly localAuctionDate,
        bool isCompleted,
        decimal? profileHigh,
        decimal? profileLow,
        decimal? tpoPoc,
        decimal? volumePoc,
        decimal? tpoVah,
        decimal? tpoVal,
        decimal? volumeVah,
        decimal? volumeVal,
        int totalTpoCount,
        decimal totalExecutedVolume,
        IReadOnlyDictionary<long, int> priceLevelTpoCounts,
        IReadOnlyDictionary<long, decimal> priceLevelVolumes,
        PriceVolumeCapability priceVolumeCapability,
        ProfileDataQuality dataQuality,
        IReadOnlyList<string> knownLimitations,
        string sourceSnapshotVersion,
        string timestampPolicyVersion,
        string provenance,
        string contributionVersion,
        string contractIdentity,
        string contractEpoch,
        decimal tickSize)
    {
        AuctionId = auctionId;
        AuctionStartUtc = auctionStartUtc;
        AuctionEndUtc = auctionEndUtc;
        LocalAuctionDate = localAuctionDate;
        IsCompleted = isCompleted;
        ProfileHigh = profileHigh;
        ProfileLow = profileLow;
        TpoPoc = tpoPoc;
        VolumePoc = volumePoc;
        TpoVah = tpoVah;
        TpoVal = tpoVal;
        VolumeVah = volumeVah;
        VolumeVal = volumeVal;
        TotalTpoCount = totalTpoCount;
        TotalExecutedVolume = totalExecutedVolume;
        PriceLevelTpoCounts = priceLevelTpoCounts;
        PriceLevelVolumes = priceLevelVolumes;
        PriceVolumeCapability = priceVolumeCapability;
        DataQuality = dataQuality;
        KnownLimitations = knownLimitations ?? Array.Empty<string>();
        SourceSnapshotVersion = sourceSnapshotVersion;
        TimestampPolicyVersion = timestampPolicyVersion;
        Provenance = provenance;
        ContributionVersion = contributionVersion;
        ContractIdentity = contractIdentity;
        ContractEpoch = contractEpoch;
        TickSize = tickSize;
    }

    public string AuctionId { get; }
    public DateTime AuctionStartUtc { get; }
    public DateTime AuctionEndUtc { get; }
    public DateOnly LocalAuctionDate { get; }
    public bool IsCompleted { get; }
    public decimal? ProfileHigh { get; }
    public decimal? ProfileLow { get; }
    public decimal? TpoPoc { get; }
    public decimal? VolumePoc { get; }
    public decimal? TpoVah { get; }
    public decimal? TpoVal { get; }
    public decimal? VolumeVah { get; }
    public decimal? VolumeVal { get; }
    public int TotalTpoCount { get; }
    public decimal TotalExecutedVolume { get; }
    public IReadOnlyDictionary<long, int> PriceLevelTpoCounts { get; }
    public IReadOnlyDictionary<long, decimal> PriceLevelVolumes { get; }
    public PriceVolumeCapability PriceVolumeCapability { get; }
    public ProfileDataQuality DataQuality { get; }
    public IReadOnlyList<string> KnownLimitations { get; }
    public string SourceSnapshotVersion { get; }
    public string TimestampPolicyVersion { get; }
    public string Provenance { get; }
    public string ContributionVersion { get; }
    public string ContractIdentity { get; }
    public string ContractEpoch { get; }
    public decimal TickSize { get; }
    public string Version => ContributionSchemaVersion;

    public static CompositeAuctionContribution FromPrimaryAuction(
        PrimaryAuctionProfileSnapshot auction,
        string contractIdentity,
        string contractEpoch,
        decimal tickSize)
    {
        if (auction is null) throw new ArgumentNullException(nameof(auction));
        var tpo = auction.TpoProfile;
        var vol = auction.VolumeProfile;
        var tpoCounts = tpo?.PriceLevelTpoCounts ?? (IReadOnlyDictionary<long, int>)new Dictionary<long, int>();
        var volumes = vol?.PriceLevelVolumes ?? (IReadOnlyDictionary<long, decimal>)new Dictionary<long, decimal>();
        var localDate = DateOnly.TryParse(
            auction.AuctionId.StartsWith("PI-", StringComparison.Ordinal) ? auction.AuctionId[3..] : "",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var d)
            ? d
            : DateOnly.FromDateTime(auction.AuctionStartUtc);

        var contributionVersion = string.Join("|",
            auction.Version,
            tpo?.Version ?? "none",
            vol?.Version ?? "none",
            (tpo?.TotalTpoCount ?? 0).ToString(CultureInfo.InvariantCulture),
            (vol?.TotalExecutedVolume ?? 0m).ToString(CultureInfo.InvariantCulture),
            tpo?.TpoPoc?.ToString(CultureInfo.InvariantCulture) ?? "",
            vol?.VolumePoc?.ToString(CultureInfo.InvariantCulture) ?? "",
            AtasTimestampNormalizer.PolicyVersion);

        return new CompositeAuctionContribution(
            auction.AuctionId,
            auction.AuctionStartUtc,
            auction.AuctionEndUtc,
            localDate,
            auction.IsCompleted,
            auction.ProfileHigh,
            auction.ProfileLow,
            tpo?.TpoPoc,
            vol?.VolumePoc,
            tpo?.TpoVah,
            tpo?.TpoVal,
            vol?.VolumeVah,
            vol?.VolumeVal,
            tpo?.TotalTpoCount ?? 0,
            vol?.TotalExecutedVolume ?? 0m,
            tpoCounts,
            volumes,
            vol?.PriceVolumeCapability ?? PriceVolumeCapability.Unavailable,
            auction.DataQuality,
            auction.KnownLimitations,
            auction.Version,
            AtasTimestampNormalizer.PolicyVersion,
            "PrimaryAuctionProfileSnapshot",
            contributionVersion,
            contractIdentity,
            contractEpoch,
            tickSize);
    }
}

/// <summary>Deterministic completed-auction ledger. Replace-by-version; no callback-count append.</summary>
public sealed class CompletedAuctionLedger
{
    private readonly Dictionary<string, CompositeAuctionContribution> _byId = new(StringComparer.Ordinal);
    private readonly List<string> _events = new();

    public IReadOnlyList<string> Events => _events;
    public int Count => _byId.Count;

    public void Clear(string reason)
    {
        _byId.Clear();
        _events.Add("LEDGER_CLEAR:" + reason);
        TrimEvents();
    }

    /// <summary>Upsert completed contribution. Developing auctions are rejected. Returns whether membership changed.</summary>
    public bool Upsert(CompositeAuctionContribution contribution)
    {
        if (contribution is null) throw new ArgumentNullException(nameof(contribution));
        if (!contribution.IsCompleted)
        {
            _events.Add("REJECT_DEVELOPING:" + contribution.AuctionId);
            TrimEvents();
            return false;
        }

        if (_byId.Count > 0)
        {
            var sample = _byId.Values.First();
            if (!string.Equals(sample.ContractEpoch, contribution.ContractEpoch, StringComparison.Ordinal)
                || sample.TickSize != contribution.TickSize
                || !string.Equals(sample.TimestampPolicyVersion, contribution.TimestampPolicyVersion, StringComparison.Ordinal))
            {
                _events.Add("EPOCH_OR_POLICY_MISMATCH:" + contribution.AuctionId);
                TrimEvents();
                throw new InvalidOperationException(
                    "Contract epoch / tick / timestamp policy mismatch for " + contribution.AuctionId);
            }
        }

        if (_byId.TryGetValue(contribution.AuctionId, out var existing))
        {
            var cmp = string.CompareOrdinal(contribution.ContributionVersion, existing.ContributionVersion);
            if (cmp == 0)
                return false;
            if (cmp < 0)
            {
                _events.Add("REJECT_LOWER_VERSION:" + contribution.AuctionId + ":" + contribution.ContributionVersion);
                TrimEvents();
                return false;
            }

            _byId[contribution.AuctionId] = contribution;
            _events.Add("REPLACE:" + contribution.AuctionId + ":" + contribution.ContributionVersion);
            TrimEvents();
            return true;
        }

        _byId[contribution.AuctionId] = contribution;
        _events.Add("ADD:" + contribution.AuctionId + ":" + contribution.ContributionVersion);
        TrimEvents();
        return true;
    }

    public bool TryGet(string auctionId, out CompositeAuctionContribution? contribution) =>
        _byId.TryGetValue(auctionId, out contribution);

    public IReadOnlyList<CompositeAuctionContribution> AllOrdered() =>
        _byId.Values.OrderBy(c => c.AuctionStartUtc).ThenBy(c => c.AuctionId, StringComparer.Ordinal).ToArray();

    private void TrimEvents()
    {
        while (_events.Count > 128)
            _events.RemoveAt(0);
    }
}

public static class CompositeIdBuilder
{
    public static string Build(
        string contractIdentity,
        string contractEpoch,
        string anchorAuctionId,
        IReadOnlyList<string> includedAuctionIdsOrdered,
        string policyVersion)
    {
        var sb = new StringBuilder();
        sb.Append("CMP|").Append(contractIdentity).Append('|').Append(contractEpoch)
            .Append('|').Append(anchorAuctionId).Append('|').Append(policyVersion).Append('|');
        for (var i = 0; i < includedAuctionIdsOrdered.Count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(includedAuctionIdsOrdered[i]);
        }

        return sb.ToString();
    }
}
