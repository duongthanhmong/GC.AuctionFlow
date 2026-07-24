using GC.AuctionFlow.Composite;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Reference;

namespace GC.AuctionFlow.Directional;

/// <summary>
/// Directional Context host. Consumes Primary / Composite / Reference snapshots only.
/// Never consumed by Profile/Composite/Reference. No Episode/Acceptance/Orderflow.
/// </summary>
public sealed class DirectionalContextHost
{
    private DirectionalPolicyConfig _policy;
    private decimal _tickSize;
    private string _contractIdentity = "Unknown";
    private string _contractEpoch = "Unknown";
    private string _timestampPolicyVersion;
    private DirectionalContextSetSnapshot? _published;
    private DirectionalInputFingerprint? _lastFingerprint;
    private DateTime _createdAtUtc;
    private long _structuralStateVersion;
    private long _tacticalStateVersion;
    private string? _tacticalIdentity;
    private string? _lastStructuralCategorical;
    private string? _lastTacticalCategorical;
    private StructuralAggregationResult? _cachedStructural;
    private string? _cachedStructuralEvidenceKey;

    public DirectionalContextHost(
        decimal tickSize,
        string contractIdentity,
        string contractEpoch,
        string? timestampPolicyVersion = null,
        DirectionalPolicyConfig? policy = null)
    {
        if (tickSize <= 0m) throw new ArgumentOutOfRangeException(nameof(tickSize));
        _tickSize = tickSize;
        _contractIdentity = string.IsNullOrWhiteSpace(contractIdentity) ? "Unknown" : contractIdentity;
        _contractEpoch = string.IsNullOrWhiteSpace(contractEpoch) ? "Unknown" : contractEpoch;
        _timestampPolicyVersion = string.IsNullOrWhiteSpace(timestampPolicyVersion)
            ? AtasTimestampNormalizer.PolicyVersion
            : timestampPolicyVersion;
        _policy = policy ?? new DirectionalPolicyConfig(enabled: false);
    }

    public DirectionalContextSetSnapshot? Current => _published;
    public DirectionalPolicyConfig Policy => _policy;
    public DirectionalInputFingerprint? LastAppliedFingerprint => _lastFingerprint;

    public void Configure(
        decimal tickSize,
        string contractIdentity,
        string contractEpoch,
        DirectionalPolicyConfig policy,
        string? timestampPolicyVersion = null)
    {
        if (tickSize <= 0m) throw new ArgumentOutOfRangeException(nameof(tickSize));
        if (policy is null) throw new ArgumentNullException(nameof(policy));

        _tickSize = tickSize;
        _contractIdentity = string.IsNullOrWhiteSpace(contractIdentity) ? "Unknown" : contractIdentity;
        _contractEpoch = string.IsNullOrWhiteSpace(contractEpoch) ? "Unknown" : contractEpoch;
        _timestampPolicyVersion = string.IsNullOrWhiteSpace(timestampPolicyVersion)
            ? AtasTimestampNormalizer.PolicyVersion
            : timestampPolicyVersion;
        _policy = policy;

        if (!_policy.Enabled)
        {
            ResetCaches();
            _published = DisabledSnapshot(DateTime.UtcNow);
            _lastFingerprint = null;
        }
    }

    public void Reset()
    {
        ResetCaches();
        _published = null;
        _lastFingerprint = null;
    }

    public DirectionalContextSetSnapshot Rebuild(
        PrimaryProfileSetSnapshot? profiles,
        CompositeSetSnapshot? composite,
        StructuralReferenceSetSnapshot? references,
        DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        var fingerprint = DirectionalInputFingerprint.Build(
            _policy.Enabled,
            _policy.EnableOneTimeFraming,
            profiles,
            composite,
            references,
            _tickSize,
            _contractEpoch,
            _timestampPolicyVersion,
            _policy.Version);

        if (!_policy.Enabled)
        {
            ResetCaches();
            _published = DisabledSnapshot(now);
            _lastFingerprint = fingerprint;
            return _published;
        }

        if (profiles is null)
        {
            ResetCaches();
            _published = AwaitingSnapshot(fingerprint, now);
            _lastFingerprint = fingerprint;
            return _published;
        }

        if (!string.Equals(_timestampPolicyVersion, AtasTimestampNormalizer.PolicyVersion, StringComparison.Ordinal))
        {
            ResetCaches();
            _published = InvalidSnapshot(fingerprint, now, "TIMESTAMP_POLICY_MISMATCH");
            _lastFingerprint = fingerprint;
            return _published;
        }

        // Price-only update: reuse structural/OTF evidence when evidence key unchanged.
        if (_published is not null
            && _lastFingerprint is { } prevFp
            && prevFp.EvidenceEquals(fingerprint)
            && _published.Status is DirectionalModuleState.Ready or DirectionalModuleState.Partial)
        {
            var priceOnly = PriceLocationClassifier.Build(
                profiles.CurrentAuction?.LastObservedPrice,
                profiles.CurrentAuction,
                profiles.PreviousAuction,
                composite,
                _tickSize);
            _published = CloneWithPriceLocation(_published, priceOnly, fingerprint, now);
            _lastFingerprint = fingerprint;
            return _published;
        }

        var limitations = new List<string>();
        if (references is null)
            limitations.Add("ReferenceCapabilityUnavailable");

        var structural = GetOrBuildStructural(profiles, fingerprint.EvidenceKey);
        var tacticalPair = PairwiseAuctionComparer.Compare(
            profiles.PreviousAuction,
            profiles.CurrentAuction,
            _tickSize,
            _timestampPolicyVersion);

        var otf = OneTimeFramingTracker.Evaluate(
            profiles.CurrentAuction,
            _policy.EnableOneTimeFraming);

        var priceLoc = PriceLocationClassifier.Build(
            profiles.CurrentAuction?.LastObservedPrice,
            profiles.CurrentAuction,
            profiles.PreviousAuction,
            composite,
            _tickSize);

        var structuralHorizon = BuildStructuralHorizon(structural, priceLoc);
        var tacticalHorizon = BuildTacticalHorizon(tacticalPair, profiles, priceLoc, otf.State);

        UpdateStateVersions(structuralHorizon, tacticalHorizon, profiles.CurrentAuction?.AuctionId);

        structuralHorizon = WithStateVersion(structuralHorizon, _structuralStateVersion);
        tacticalHorizon = WithStateVersion(tacticalHorizon, _tacticalStateVersion);

        var volumePartial = !tacticalPair.ExactVolumeAvailable
                            || structural.PairwiseEvidence.Any(p => !p.ExactVolumeAvailable);
        var status = ResolveStatus(profiles, structural, tacticalPair, volumePartial, limitations);

        if (volumePartial)
            limitations.Add("EXACT_VOLUME_PARTIAL");
        limitations.AddRange(structural.Limitations);
        limitations.AddRange(tacticalPair.Limitations);
        limitations.AddRange(otf.KnownLimitations);
        limitations.Add("NO_WEEKLY_MONTHLY_HORIZON");
        limitations.Add("EXECUTION_CONTEXT_UNAVAILABLE");
        limitations.Add("NO_ACCEPTANCE_EPISODE_ORDERFLOW_INPUTS");

        var execution = new ExecutionContextSnapshot(
            ExecutionContextAvailability.NotAvailable,
            DirectionalAuctionState.Unknown,
            new[] { "EXECUTION_REQUIRES_EPISODE_AND_ORDERFLOW", "PHASE_1D_NOT_AUTHORIZED" });

        if (_createdAtUtc == default)
            _createdAtUtc = now;

        _published = new DirectionalContextSetSnapshot(
            status,
            _policy.Version,
            _contractIdentity,
            _contractEpoch,
            _tickSize,
            _timestampPolicyVersion,
            structuralHorizon,
            tacticalHorizon,
            otf,
            execution,
            priceLoc,
            fingerprint.ToString(),
            SnapshotToken(structuralHorizon, tacticalHorizon, otf, status),
            _createdAtUtc,
            now,
            limitations.Distinct(StringComparer.Ordinal).ToArray());
        _lastFingerprint = fingerprint;
        return _published;
    }

    private StructuralAggregationResult GetOrBuildStructural(PrimaryProfileSetSnapshot profiles, string evidenceKey)
    {
        if (_cachedStructural is not null
            && string.Equals(_cachedStructuralEvidenceKey, evidenceKey, StringComparison.Ordinal))
            return _cachedStructural;

        // Exclude developing current from completed ledger if present.
        var completed = profiles.CompletedAuctions
            .Where(a => a.IsCompleted)
            .Where(a => profiles.CurrentAuction is null
                        || a.AuctionId != profiles.CurrentAuction.AuctionId
                        || profiles.CurrentAuction.IsCompleted)
            .ToArray();

        _cachedStructural = StructuralDirectionalAggregator.Aggregate(completed, _tickSize, _timestampPolicyVersion);
        _cachedStructuralEvidenceKey = evidenceKey;
        return _cachedStructural;
    }

    private DirectionalHorizonContextSnapshot BuildStructuralHorizon(
        StructuralAggregationResult structural,
        ProfileLocationContextSnapshot priceLoc)
    {
        var lastPair = structural.PairwiseEvidence.Count > 0
            ? structural.PairwiseEvidence[^1]
            : null;
        var evidence = new List<string> { "COMPLETED_PRIMARY_HISTORY" };
        if (lastPair is not null)
        {
            evidence.Add("TPO_VALUE=" + lastPair.TpoValueRelationship);
            evidence.Add("TPO_POC=" + lastPair.TpoPocMigration);
            evidence.Add(lastPair.ExactVolumeAvailable ? "EXACT_VOLUME" : "VOLUME_UNAVAILABLE");
        }

        return new DirectionalHorizonContextSnapshot(
            DirectionalHorizon.StructuralMultiDay,
            structural.State,
            structural.CompatibleTransitionCount >= 2 ? DirectionalMaturity.Confirmed : DirectionalMaturity.Unavailable,
            structural.SourceAuctionIds,
            lastPair?.TpoValueRelationship ?? ValueRelationship.Unavailable,
            lastPair?.VolumeValueRelationship ?? ValueRelationship.Unavailable,
            lastPair?.TpoPocMigration ?? MigrationDirection.Unavailable,
            lastPair?.VolumePocMigration ?? MigrationDirection.Unavailable,
            lastPair?.TpoValueMidpointMigration ?? MigrationDirection.Unavailable,
            lastPair?.VolumeValueMidpointMigration ?? MigrationDirection.Unavailable,
            priceLoc,
            null,
            evidence,
            lastPair?.Conflicts ?? Array.Empty<string>(),
            structural.Limitations,
            0,
            lastPair,
            structural.CompatibleTransitionCount);
    }

    private static DirectionalHorizonContextSnapshot BuildTacticalHorizon(
        PairwiseAuctionComparisonEvidence pair,
        PrimaryProfileSetSnapshot profiles,
        ProfileLocationContextSnapshot priceLoc,
        OneTimeFramingState otfState)
    {
        var ids = new List<string>();
        if (profiles.PreviousAuction is not null) ids.Add(profiles.PreviousAuction.AuctionId);
        if (profiles.CurrentAuction is not null) ids.Add(profiles.CurrentAuction.AuctionId);

        var evidence = new List<string>
        {
            "CURRENT_DEVELOPING_PRIMARY",
            "PREVIOUS_COMPLETED_PRIMARY",
            "TPO_VALUE=" + pair.TpoValueRelationship,
            "TPO_POC=" + pair.TpoPocMigration,
            pair.ExactVolumeAvailable ? "EXACT_VOLUME" : "VOLUME_UNAVAILABLE"
        };

        var limitations = pair.Limitations.ToList();
        if (otfState == OneTimeFramingState.Broken)
            limitations.Add("OTF_BROKEN_WHILE_MIGRATION_UNRESOLVED");

        var state = pair.ClassifiedState;
        if (state == DirectionalAuctionState.Unknown
            && otfState == OneTimeFramingState.Broken
            && pair.Compatible)
            state = DirectionalAuctionState.Transition;

        return new DirectionalHorizonContextSnapshot(
            DirectionalHorizon.TacticalCurrentPrimary,
            state,
            DirectionalMaturity.Developing,
            ids,
            pair.TpoValueRelationship,
            pair.VolumeValueRelationship,
            pair.TpoPocMigration,
            pair.VolumePocMigration,
            pair.TpoValueMidpointMigration,
            pair.VolumeValueMidpointMigration,
            priceLoc,
            otfState,
            evidence,
            pair.Conflicts,
            limitations,
            0,
            pair,
            completedTransitionCount: pair.Compatible ? 1 : 0);
    }

    private void UpdateStateVersions(
        DirectionalHorizonContextSnapshot structural,
        DirectionalHorizonContextSnapshot tactical,
        string? currentAuctionId)
    {
        var structuralKey = structural.State + "|" + structural.TpoValueMigration + "|" + structural.TpoPocMigration
                            + "|" + structural.CompletedTransitionCount + "|" + string.Join(",", structural.SourceAuctionIds);
        if (!string.Equals(_lastStructuralCategorical, structuralKey, StringComparison.Ordinal))
        {
            _structuralStateVersion++;
            _lastStructuralCategorical = structuralKey;
        }

        if (!string.Equals(_tacticalIdentity, currentAuctionId, StringComparison.Ordinal))
        {
            _tacticalIdentity = currentAuctionId;
            _tacticalStateVersion = 1;
            _lastTacticalCategorical = null;
        }

        var tacticalKey = tactical.State + "|" + tactical.TpoValueMigration + "|" + tactical.TpoPocMigration
                          + "|" + tactical.VolumePocMigration + "|" + tactical.TpoValueMidpointMigration
                          + "|" + (tactical.PrimaryPairwise?.CurrentTpoPoc?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "-")
                          + "|" + (tactical.PrimaryPairwise?.CurrentTpoVah?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "-")
                          + "|" + string.Join(",", tactical.Conflicts);
        if (!string.Equals(_lastTacticalCategorical, tacticalKey, StringComparison.Ordinal))
        {
            if (_lastTacticalCategorical is not null)
                _tacticalStateVersion++;
            else if (_tacticalStateVersion == 0)
                _tacticalStateVersion = 1;
            _lastTacticalCategorical = tacticalKey;
        }
    }

    private static DirectionalHorizonContextSnapshot WithStateVersion(
        DirectionalHorizonContextSnapshot src,
        long version) =>
        new(
            src.Horizon, src.State, src.Maturity, src.SourceAuctionIds,
            src.TpoValueMigration, src.VolumeValueMigration, src.TpoPocMigration, src.VolumePocMigration,
            src.TpoValueMidpointMigration, src.VolumeValueMidpointMigration,
            src.PriceLocation, src.OneTimeFramingState, src.EvidenceComponents, src.Conflicts,
            src.KnownLimitations, version, src.PrimaryPairwise, src.CompletedTransitionCount);

    private static DirectionalModuleState ResolveStatus(
        PrimaryProfileSetSnapshot profiles,
        StructuralAggregationResult structural,
        PairwiseAuctionComparisonEvidence tactical,
        bool volumePartial,
        List<string> limitations)
    {
        if (profiles.CurrentAuction is null && profiles.CompletedAuctions.Count == 0)
            return DirectionalModuleState.AwaitingProfile;

        if (!tactical.Compatible && structural.CompatibleTransitionCount < 2)
        {
            if (profiles.CurrentAuction is null)
                return DirectionalModuleState.AwaitingProfile;
            limitations.Add("DIRECTIONAL_EVIDENCE_PARTIAL");
            return DirectionalModuleState.Partial;
        }

        if (volumePartial)
            return DirectionalModuleState.Partial;

        if (structural.State != DirectionalAuctionState.Unknown || tactical.Compatible)
            return DirectionalModuleState.Ready;

        return DirectionalModuleState.Partial;
    }

    private static long SnapshotToken(
        DirectionalHorizonContextSnapshot structural,
        DirectionalHorizonContextSnapshot tactical,
        OneTimeFramingSnapshot otf,
        DirectionalModuleState status) =>
        unchecked(structural.StateVersion * 1_000_003L + tactical.StateVersion * 1_007L + otf.StateVersion + (long)status);

    private DirectionalContextSetSnapshot CloneWithPriceLocation(
        DirectionalContextSetSnapshot src,
        ProfileLocationContextSnapshot price,
        DirectionalInputFingerprint fingerprint,
        DateTime now)
    {
        var structural = new DirectionalHorizonContextSnapshot(
            src.StructuralContext.Horizon, src.StructuralContext.State, src.StructuralContext.Maturity,
            src.StructuralContext.SourceAuctionIds, src.StructuralContext.TpoValueMigration,
            src.StructuralContext.VolumeValueMigration, src.StructuralContext.TpoPocMigration,
            src.StructuralContext.VolumePocMigration, src.StructuralContext.TpoValueMidpointMigration,
            src.StructuralContext.VolumeValueMidpointMigration, price, src.StructuralContext.OneTimeFramingState,
            src.StructuralContext.EvidenceComponents, src.StructuralContext.Conflicts,
            src.StructuralContext.KnownLimitations, src.StructuralContext.StateVersion,
            src.StructuralContext.PrimaryPairwise, src.StructuralContext.CompletedTransitionCount);
        var tactical = new DirectionalHorizonContextSnapshot(
            src.TacticalContext.Horizon, src.TacticalContext.State, src.TacticalContext.Maturity,
            src.TacticalContext.SourceAuctionIds, src.TacticalContext.TpoValueMigration,
            src.TacticalContext.VolumeValueMigration, src.TacticalContext.TpoPocMigration,
            src.TacticalContext.VolumePocMigration, src.TacticalContext.TpoValueMidpointMigration,
            src.TacticalContext.VolumeValueMidpointMigration, price, src.TacticalContext.OneTimeFramingState,
            src.TacticalContext.EvidenceComponents, src.TacticalContext.Conflicts,
            src.TacticalContext.KnownLimitations, src.TacticalContext.StateVersion,
            src.TacticalContext.PrimaryPairwise, src.TacticalContext.CompletedTransitionCount);

        return new DirectionalContextSetSnapshot(
            src.Status, src.PolicyVersion, src.InstrumentIdentity, src.DataEpoch, src.TickSize, src.TimestampPolicy,
            structural, tactical, src.OneTimeFraming, src.ExecutionContext, price,
            fingerprint.ToString(), src.SnapshotVersionToken, src.CreatedAtUtc, now, src.Limitations);
    }

    private void ResetCaches()
    {
        _cachedStructural = null;
        _cachedStructuralEvidenceKey = null;
        _structuralStateVersion = 0;
        _tacticalStateVersion = 0;
        _tacticalIdentity = null;
        _lastStructuralCategorical = null;
        _lastTacticalCategorical = null;
        _createdAtUtc = default;
    }

    private DirectionalContextSetSnapshot DisabledSnapshot(DateTime now) =>
        EmptySet(DirectionalModuleState.Disabled, "none", now, new[] { "DIRECTIONAL_CONTEXT_DISABLED" });

    private DirectionalContextSetSnapshot AwaitingSnapshot(DirectionalInputFingerprint fp, DateTime now) =>
        EmptySet(DirectionalModuleState.AwaitingProfile, fp.ToString(), now, new[] { "AWAITING_PRIMARY_PROFILE" });

    private DirectionalContextSetSnapshot InvalidSnapshot(DirectionalInputFingerprint fp, DateTime now, string reason) =>
        EmptySet(DirectionalModuleState.Invalid, fp.ToString(), now, new[] { reason });

    private DirectionalContextSetSnapshot EmptySet(
        DirectionalModuleState status,
        string fingerprint,
        DateTime now,
        IReadOnlyList<string> limitations)
    {
        var emptyLoc = new ProfileLocationContextSnapshot(
            PriceValueLocation.Unavailable, PriceValueLocation.Unavailable,
            PriceValueLocation.Unavailable, PriceValueLocation.Unavailable, PriceValueLocation.Unavailable);
        var emptyHorizon = new DirectionalHorizonContextSnapshot(
            DirectionalHorizon.StructuralMultiDay,
            DirectionalAuctionState.Unknown,
            DirectionalMaturity.Unavailable,
            Array.Empty<string>(),
            ValueRelationship.Unavailable,
            ValueRelationship.Unavailable,
            MigrationDirection.Unavailable,
            MigrationDirection.Unavailable,
            MigrationDirection.Unavailable,
            MigrationDirection.Unavailable,
            emptyLoc,
            null,
            Array.Empty<string>(),
            Array.Empty<string>(),
            limitations,
            0);
        var tactical = new DirectionalHorizonContextSnapshot(
            DirectionalHorizon.TacticalCurrentPrimary,
            DirectionalAuctionState.Unknown,
            status == DirectionalModuleState.Disabled ? DirectionalMaturity.Unavailable : DirectionalMaturity.Developing,
            Array.Empty<string>(),
            ValueRelationship.Unavailable,
            ValueRelationship.Unavailable,
            MigrationDirection.Unavailable,
            MigrationDirection.Unavailable,
            MigrationDirection.Unavailable,
            MigrationDirection.Unavailable,
            emptyLoc,
            OneTimeFramingState.Unknown,
            Array.Empty<string>(),
            Array.Empty<string>(),
            limitations,
            0);
        var otf = new OneTimeFramingSnapshot(
            OneTimeFramingState.Unknown, null, null, 0, 0, Array.Empty<string>(), 0, 0, 0, null,
            new[] { OneTimeFramingTracker.ConfirmationLimitation }, 0);
        var exec = new ExecutionContextSnapshot(
            ExecutionContextAvailability.NotAvailable,
            DirectionalAuctionState.Unknown,
            new[] { "EXECUTION_CONTEXT_UNAVAILABLE" });

        if (_createdAtUtc == default)
            _createdAtUtc = now;

        return new DirectionalContextSetSnapshot(
            status, _policy.Version, _contractIdentity, _contractEpoch, _tickSize, _timestampPolicyVersion,
            emptyHorizon, tactical, otf, exec, emptyLoc, fingerprint, 0, _createdAtUtc, now, limitations);
    }
}
