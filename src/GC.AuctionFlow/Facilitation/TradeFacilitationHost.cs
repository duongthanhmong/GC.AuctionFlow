using GC.AuctionFlow.Efficiency;

namespace GC.AuctionFlow.Facilitation;

/// <summary>
/// Phase 2F Trade Facilitation host.
/// Consumes Phase 2C AuctionEfficiencyEvidenceSetSnapshot immutably.
/// Computes raw TradeFacilitationIndex components for future calibration.
/// All classification states → NotCalibrated — thresholds not yet calibrated.
/// </summary>
public sealed class TradeFacilitationHost
{
    private TradeFacilitationPolicyConfig _policy;
    private TradeFacilitationSetSnapshot? _published;
    private string? _lastFingerprintKey;
    private DateTime _createdAtUtc;
    private readonly List<TradeFacilitationSnapshot> _closedList = new();
    private readonly HashSet<string> _closedIds = new(StringComparer.Ordinal);

    public TradeFacilitationHost(TradeFacilitationPolicyConfig? policy = null)
    {
        _policy = policy ?? new TradeFacilitationPolicyConfig(enabled: false);
    }

    public TradeFacilitationSetSnapshot? Current => _published;
    public TradeFacilitationPolicyConfig Policy => _policy;

    public void Configure(TradeFacilitationPolicyConfig policy)
    {
        if (policy is null) throw new ArgumentNullException(nameof(policy));
        _policy = policy;
        if (!_policy.Enabled)
        {
            ResetInternal();
            _published = DisabledSnapshot(DateTime.UtcNow);
            _lastFingerprintKey = null;
        }
    }

    public void Reset()
    {
        ResetInternal();
        _published = null;
        _lastFingerprintKey = null;
        _createdAtUtc = default;
    }

    /// <summary>
    /// Rebuild from current efficiency evidence set.
    /// Fingerprint-gated: returns cached when efficiency unchanged.
    /// All classification states → NotCalibrated (no thresholds calibrated in Phase 2F).
    /// </summary>
    public TradeFacilitationSetSnapshot Rebuild(
        AuctionEfficiencyEvidenceSetSnapshot? efficiency,
        DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;

        if (!_policy.Enabled)
        {
            ResetInternal();
            _published = DisabledSnapshot(now);
            return _published;
        }

        var fp = BuildFingerprintKey(efficiency);
        if (_lastFingerprintKey is not null && string.Equals(_lastFingerprintKey, fp, StringComparison.Ordinal) && _published is not null)
            return _published;

        if (efficiency is null || efficiency.ModuleState == EfficiencyModuleState.Disabled)
        {
            _published = StatusSnapshot(TradeFacilitationModuleState.AwaitingEfficiency, now);
            _lastFingerprintKey = fp;
            return _published;
        }

        if (efficiency.ModuleState == EfficiencyModuleState.Invalid)
        {
            _published = StatusSnapshot(TradeFacilitationModuleState.Invalid, now,
                new[] { "EFFICIENCY_INPUT_INVALID" });
            _lastFingerprintKey = fp;
            return _published;
        }

        if (_createdAtUtc == default)
            _createdAtUtc = now;

        TradeFacilitationSnapshot? currentAuction = null;
        if (efficiency.CurrentAuctionEvidence is { } curEff)
            currentAuction = MapToFacilitation(curEff, now);

        var activeEpisodes = new List<TradeFacilitationSnapshot>(efficiency.ActiveEpisodeEvidence.Count);
        long maxRev = -1L;
        TradeFacilitationSnapshot? latest = currentAuction;
        int ready = 0, partial = 0;

        if (currentAuction is not null)
        {
            if (currentAuction.EventRevision >= maxRev)
            {
                maxRev = currentAuction.EventRevision;
                latest = currentAuction;
            }
            CountQuality(currentAuction, ref ready, ref partial);
        }

        foreach (var ep in efficiency.ActiveEpisodeEvidence)
        {
            var tf = MapToFacilitation(ep, now);
            activeEpisodes.Add(tf);
            if (tf.EventRevision >= maxRev)
            {
                maxRev = tf.EventRevision;
                latest = tf;
            }
            CountQuality(tf, ref ready, ref partial);
        }

        foreach (var ep in efficiency.RecentlyClosedEpisodeEvidence)
        {
            var tfId = BuildSnapshotId(ep.SnapshotId);
            if (_closedIds.Add(tfId))
            {
                _closedList.Add(MapToFacilitation(ep, now));
                if (_closedList.Count > TradeFacilitationSetSnapshot.RecentlyClosedCapacity)
                {
                    var removed = _closedList[0];
                    _closedList.RemoveAt(0);
                    _closedIds.Remove(removed.SnapshotId);
                }
            }
        }

        TradeFacilitationModuleState state;
        var hasAny = currentAuction is not null || activeEpisodes.Count > 0;
        if (!hasAny)
            state = TradeFacilitationModuleState.AwaitingEfficiency;
        else if (partial > 0
                 || efficiency.ModuleState == EfficiencyModuleState.Partial
                 || efficiency.ModuleState == EfficiencyModuleState.AwaitingEpisode
                 || efficiency.ModuleState == EfficiencyModuleState.AwaitingOrderflow)
            state = TradeFacilitationModuleState.Partial;
        else
            state = TradeFacilitationModuleState.Ready;

        _published = new TradeFacilitationSetSnapshot(
            state,
            TradeFacilitationPolicyConfig.PolicyVersion,
            currentAuction,
            activeEpisodes,
            _closedList.ToArray(),
            latest,
            ready,
            partial,
            _createdAtUtc,
            now,
            BuildSetLimitations());

        _lastFingerprintKey = fp;
        return _published;
    }

    private static TradeFacilitationSnapshot MapToFacilitation(
        AuctionEfficiencyEvidenceSnapshot eff, DateTime nowUtc)
    {
        var tfId = BuildSnapshotId(eff.SnapshotId);

        // Extract direction-consistent effort from the effort vector.
        decimal? dirConsistentVolume = eff.ResultDirection switch
        {
            EfficiencyResultDirection.Up   => eff.Effort.AskVolume,
            EfficiencyResultDirection.Down => eff.Effort.BidVolume,
            _                              => null
        };

        // Ratio of direction-consistent volume to total classified volume.
        decimal? dirConsistentRatio = null;
        if (dirConsistentVolume.HasValue && eff.Effort.ClassifiedVolume > 0m)
            dirConsistentRatio = dirConsistentVolume.Value / eff.Effort.ClassifiedVolume;

        var favorableTicks = eff.Result.MaximumFavorableProgressTicks;

        // Raw ticks per direction-consistent contract (research value — not calibrated).
        decimal? ticksPerUnit = null;
        if (favorableTicks.HasValue && dirConsistentVolume.HasValue && dirConsistentVolume.Value > 0m)
            ticksPerUnit = (decimal)favorableTicks.Value / dirConsistentVolume.Value;

        var quality = eff.DataQuality switch
        {
            EfficiencyDataQuality.Invalid => TradeFacilitationDataQuality.Invalid,
            EfficiencyDataQuality.Partial => TradeFacilitationDataQuality.Partial,
            _                             => TradeFacilitationDataQuality.Complete
        };

        // --- Structure component (v1.3 §5.2): did POC / value migrate WITH the attempt? ---
        // Volume POC is preferred over TPO POC: it reflects executed activity rather than
        // time distribution, which is the question facilitation actually asks.
        var pocMigration = eff.Result.VolumePocMigrationTicks ?? eff.Result.TpoPocMigrationTicks;
        var valueMigration = eff.Result.VolumeValueCentroidMigrationTicks
                             ?? eff.Result.TpoValueCentroidMigrationTicks;
        var structureAlignment = DeriveAlignment(eff.ResultDirection, pocMigration ?? valueMigration);

        // --- Maintenance component (v1.3 §5.2): did pullbacks hold? ---
        var retainedTicks = eff.Result.ProgressRetainedTicks;
        var retentionRatio = eff.Result.ProgressRetentionRatio;
        var timeAtExcursion = eff.Result.TimeAtMaximumExcursion;

        // G-TF-002: count what is actually measurable. A verdict needs all four.
        var componentCount = 0;
        if (dirConsistentVolume.HasValue) componentCount++;                       // Activity
        if (favorableTicks.HasValue) componentCount++;                            // Progress
        if (structureAlignment is FacilitationComponentAlignment.Aligned
            or FacilitationComponentAlignment.Opposed
            or FacilitationComponentAlignment.Flat) componentCount++;             // Structure
        if (retentionRatio.HasValue) componentCount++;                            // Maintenance

        var lim = new List<string>
        {
            TradeFacilitationPolicyConfig.LimitationNotCalibrated,
            TradeFacilitationPolicyConfig.LimitationHealthyNotCalibrated,
            TradeFacilitationPolicyConfig.LimitationFailingNotCalibrated,
            TradeFacilitationPolicyConfig.LimitationLiveOnly
        };
        if (!dirConsistentVolume.HasValue)
            lim.Add(TradeFacilitationPolicyConfig.LimitationEffortUnavailable);
        if (!favorableTicks.HasValue)
            lim.Add(TradeFacilitationPolicyConfig.LimitationProgressUnavailable);
        if (eff.ResultDirection == EfficiencyResultDirection.Unknown)
            lim.Add(TradeFacilitationPolicyConfig.LimitationDirectionUnavailable);
        if (structureAlignment is FacilitationComponentAlignment.Unavailable
            or FacilitationComponentAlignment.Unknown)
            lim.Add(TradeFacilitationPolicyConfig.LimitationStructureUnavailable);
        if (!retentionRatio.HasValue)
            lim.Add(TradeFacilitationPolicyConfig.LimitationMaintenanceUnavailable);
        if (componentCount < TradeFacilitationPolicyConfig.RequiredComponents)
            lim.Add(TradeFacilitationPolicyConfig.LimitationComponentsIncomplete);

        return new TradeFacilitationSnapshot(
            tfId,
            TradeFacilitationPolicyConfig.PolicyVersion,
            eff.SnapshotId,
            eff.PrimaryAuctionId,
            eff.EpisodeId,
            eff.ReferenceId,
            eff.ScopeType,
            TradeFacilitationClassificationState.NotCalibrated,
            quality,
            dirConsistentVolume,
            dirConsistentRatio,
            favorableTicks,
            ticksPerUnit,
            pocMigration,
            valueMigration,
            structureAlignment,
            retainedTicks,
            retentionRatio,
            timeAtExcursion,
            componentCount,
            eff.StateVersion,
            eff.EventRevision,
            nowUtc,
            lim);
    }

    private static string BuildSnapshotId(string efficiencySnapshotId) =>
        "TF:" + efficiencySnapshotId;

    private static void CountQuality(TradeFacilitationSnapshot tf, ref int ready, ref int partial)
    {
        switch (tf.DataQuality)
        {
            case TradeFacilitationDataQuality.Complete: ready++;   break;
            case TradeFacilitationDataQuality.Partial:  partial++; break;
        }
    }

    private static string BuildFingerprintKey(AuctionEfficiencyEvidenceSetSnapshot? efficiency) =>
        TradeFacilitationPolicyConfig.PolicyVersion
        + "|" + (efficiency?.InputFingerprint?.ToString() ?? "")
        + "|" + (efficiency?.ModuleState.ToString() ?? "");

    private static IReadOnlyList<string> BuildSetLimitations() => new[]
    {
        TradeFacilitationPolicyConfig.LimitationNotCalibrated,
        TradeFacilitationPolicyConfig.LimitationHealthyNotCalibrated,
        TradeFacilitationPolicyConfig.LimitationFailingNotCalibrated,
        TradeFacilitationPolicyConfig.LimitationLiveOnly,
        TradeFacilitationPolicyConfig.LimitationNoFarAac
    };

    private void ResetInternal()
    {
        _closedList.Clear();
        _closedIds.Clear();
    }

    private TradeFacilitationSetSnapshot DisabledSnapshot(DateTime now) =>
        new TradeFacilitationSetSnapshot(
            TradeFacilitationModuleState.Disabled,
            TradeFacilitationPolicyConfig.PolicyVersion,
            null,
            Array.Empty<TradeFacilitationSnapshot>(),
            Array.Empty<TradeFacilitationSnapshot>(),
            null, 0, 0, now, now,
            new[] { "MODULE_DISABLED" });

    private TradeFacilitationSetSnapshot StatusSnapshot(
        TradeFacilitationModuleState state, DateTime now, string[]? extra = null)
    {
        if (_createdAtUtc == default) _createdAtUtc = now;
        var lim = new List<string>
        {
            TradeFacilitationPolicyConfig.LimitationNotCalibrated,
            TradeFacilitationPolicyConfig.LimitationLiveOnly
        };
        if (extra is not null) lim.AddRange(extra);
        return new TradeFacilitationSetSnapshot(
            state,
            TradeFacilitationPolicyConfig.PolicyVersion,
            null,
            Array.Empty<TradeFacilitationSnapshot>(),
            _closedList.ToArray(),
            null, 0, 0,
            _createdAtUtc, now,
            lim);
    }

    /// <summary>
    /// Sign comparison between a structural migration and the attempted direction.
    /// Deliberately magnitude-free: whether a migration is LARGE ENOUGH to count is
    /// the calibrated question (v1.3 G-TF-004, stratified by regime).
    /// </summary>
    private static FacilitationComponentAlignment DeriveAlignment(
        EfficiencyResultDirection direction, long? migrationTicks)
    {
        if (!migrationTicks.HasValue)
            return FacilitationComponentAlignment.Unavailable;

        if (direction is EfficiencyResultDirection.Unknown or EfficiencyResultDirection.Flat)
            return FacilitationComponentAlignment.Unknown;

        var m = migrationTicks.Value;
        if (m == 0L) return FacilitationComponentAlignment.Flat;

        var followsAttempt = direction == EfficiencyResultDirection.Up ? m > 0L : m < 0L;
        return followsAttempt
            ? FacilitationComponentAlignment.Aligned
            : FacilitationComponentAlignment.Opposed;
    }
}
