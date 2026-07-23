namespace GC.AuctionFlow.Profile;

public sealed class PrimaryAuctionProfileSnapshot
{
    public const string SnapshotVersion = "1.0.0";

    public PrimaryAuctionProfileSnapshot(
        AuctionProfileState profileState,
        string auctionId,
        DateTime auctionStartUtc,
        DateTime auctionEndUtc,
        bool isCompleted,
        TpoProfileSnapshot? tpoProfile,
        VolumeProfileSnapshot? volumeProfile,
        decimal? profileHigh,
        decimal? profileLow,
        decimal? lastObservedPrice,
        DateTime lastUpdatedUtc,
        (int MinBar, int MaxBar)? sourceBarRange,
        ProfileDataQuality dataQuality,
        IReadOnlyList<string> knownLimitations)
    {
        ProfileState = profileState;
        AuctionId = auctionId;
        AuctionStartUtc = auctionStartUtc;
        AuctionEndUtc = auctionEndUtc;
        IsCompleted = isCompleted;
        TpoProfile = tpoProfile;
        VolumeProfile = volumeProfile;
        ProfileHigh = profileHigh;
        ProfileLow = profileLow;
        LastObservedPrice = lastObservedPrice;
        LastUpdatedUtc = lastUpdatedUtc;
        SourceBarRange = sourceBarRange;
        DataQuality = dataQuality;
        KnownLimitations = knownLimitations ?? Array.Empty<string>();
    }

    public AuctionProfileState ProfileState { get; }
    public string AuctionId { get; }
    public DateTime AuctionStartUtc { get; }
    public DateTime AuctionEndUtc { get; }
    public bool IsCompleted { get; }
    public TpoProfileSnapshot? TpoProfile { get; }
    public VolumeProfileSnapshot? VolumeProfile { get; }
    public decimal? ProfileHigh { get; }
    public decimal? ProfileLow { get; }
    public decimal? LastObservedPrice { get; }
    public DateTime LastUpdatedUtc { get; }
    public (int MinBar, int MaxBar)? SourceBarRange { get; }
    public ProfileDataQuality DataQuality { get; }
    public IReadOnlyList<string> KnownLimitations { get; }
    public string Version => SnapshotVersion;
}

public sealed class PrimaryProfileSetSnapshot
{
    public const string SnapshotVersion = "1.0.1";

    public PrimaryProfileSetSnapshot(
        PrimaryAuctionProfileSnapshot? currentAuction,
        PrimaryAuctionProfileSnapshot? previousAuction,
        HistoricalInitializationState historicalInitializationState,
        int lastProcessedBarIndex,
        IReadOnlyList<string> knownLimitations,
        IReadOnlyList<string> revisionEvents,
        ProfileTimestampDiagnostic? latestTimestampDiagnostic = null)
    {
        CurrentAuction = currentAuction;
        PreviousAuction = previousAuction;
        HistoricalInitializationState = historicalInitializationState;
        LastProcessedBarIndex = lastProcessedBarIndex;
        KnownLimitations = knownLimitations ?? Array.Empty<string>();
        RevisionEvents = revisionEvents ?? Array.Empty<string>();
        LatestTimestampDiagnostic = latestTimestampDiagnostic;
    }

    public PrimaryAuctionProfileSnapshot? CurrentAuction { get; }
    public PrimaryAuctionProfileSnapshot? PreviousAuction { get; }
    public HistoricalInitializationState HistoricalInitializationState { get; }
    public int LastProcessedBarIndex { get; }
    public IReadOnlyList<string> KnownLimitations { get; }
    public IReadOnlyList<string> RevisionEvents { get; }
    public ProfileTimestampDiagnostic? LatestTimestampDiagnostic { get; }
    public string Version => SnapshotVersion;
}

/// <summary>
/// Versioned bar ledger with replace-by-bar-index. Rebuilds current/previous auctions deterministically.
/// No lookahead: only bars with BarIndex &lt;= evaluationBarIndex are used.
/// Bars normalized under a prior timestamp policy are rejected and trigger ledger clear.
/// </summary>
public sealed class PrimaryProfileHost
{
    public const int TimestampDiagnosticCapacity = 8;

    private readonly Dictionary<int, ProfileBarObservation> _bars = new();
    private readonly List<string> _revisionEvents = new();
    private readonly Queue<ProfileTimestampDiagnostic> _timestampDiagnostics = new();
    private readonly Dictionary<int, int> _barRevisionCounts = new();
    private readonly List<string> _rejectedBars = new();
    private int _duplicateBarIndexObservations;
    private PrimaryAuctionClockConfig _config;
    private decimal _tickSize;
    private PrimaryProfileSetSnapshot? _published;
    private long? _prevTpoPocTick;
    private long? _prevVolPocTick;
    private ProfileTimestampDiagnostic? _latestTimestampDiagnostic;

    /// <summary>ATAS UI: enable diagnostics-only reference price. Default false.</summary>
    public bool EnableTpoParityReferencePrice { get; set; }

    /// <summary>ATAS UI raw decimal (Property Grid editable). Used only when EnableTpoParityReferencePrice is true.</summary>
    public decimal TpoParityReferencePrice { get; set; }

    public PrimaryProfileHost(decimal tickSize, PrimaryAuctionClockConfig? config = null)
    {
        if (tickSize <= 0m) throw new ArgumentOutOfRangeException(nameof(tickSize));
        _tickSize = tickSize;
        _config = config ?? new PrimaryAuctionClockConfig();
    }

    public PrimaryProfileSetSnapshot? Current => _published;
    public IReadOnlyList<string> RevisionEvents => _revisionEvents;
    public ProfileTimestampDiagnostic? LatestTimestampDiagnostic => _latestTimestampDiagnostic;
    public IReadOnlyList<string> RejectedBars => _rejectedBars;

    public void NoteTimestampDiagnostic(ProfileTimestampDiagnostic? diagnostic)
    {
        if (diagnostic is null) return;
        _latestTimestampDiagnostic = diagnostic;
        _timestampDiagnostics.Enqueue(diagnostic);
        while (_timestampDiagnostics.Count > TimestampDiagnosticCapacity)
            _timestampDiagnostics.Dequeue();
    }

    public void Reset(decimal? tickSize = null, PrimaryAuctionClockConfig? config = null)
    {
        if (tickSize is decimal t)
        {
            if (t <= 0m) throw new ArgumentOutOfRangeException(nameof(tickSize));
            _tickSize = t;
        }

        if (config is not null)
            _config = config;

        _bars.Clear();
        _revisionEvents.Clear();
        _timestampDiagnostics.Clear();
        _barRevisionCounts.Clear();
        _rejectedBars.Clear();
        _duplicateBarIndexObservations = 0;
        _latestTimestampDiagnostic = null;
        _revisionEvents.Add("RESET:" + DateTime.UtcNow.ToString("o") + ":policy=" + AtasTimestampNormalizer.PolicyVersion);
        _published = null;
        _prevTpoPocTick = null;
        _prevVolPocTick = null;
    }

    /// <summary>
    /// Ingest bar by index. When <paramref name="rebuildNow"/> is false, only the ledger is updated
    /// (historical bulk load). Rebuild once at the live/current bar — avoids O(n²) on chart add.
    /// Final distribution remains identical to rebuild-every-bar (tests cover this).
    /// </summary>
    public PrimaryProfileSetSnapshot UpsertBar(
        ProfileBarObservation observation,
        int evaluationBarIndex,
        DateTimeOffset evaluationUtc,
        bool rebuildNow = true)
    {
        if (observation is null) throw new ArgumentNullException(nameof(observation));

        if (!string.Equals(observation.TimestampPolicyVersion, AtasTimestampNormalizer.PolicyVersion, StringComparison.Ordinal))
        {
            var mismatch =
                $"TIMESTAMP_POLICY_MISMATCH:bar={observation.BarIndex}:obs={observation.TimestampPolicyVersion}:host={AtasTimestampNormalizer.PolicyVersion}";
            Reset(_tickSize, _config);
            _rejectedBars.Add(mismatch);
            _revisionEvents.Add(mismatch);
            // Do not re-ingest stale observation under old policy.
            return Rebuild(evaluationBarIndex, evaluationUtc);
        }

        if (observation.BarIndex > evaluationBarIndex)
        {
            var reject = $"LOOKAHEAD_REJECTED:bar={observation.BarIndex}:eval={evaluationBarIndex}";
            _rejectedBars.Add(reject);
            _revisionEvents.Add(reject);
            return rebuildNow ? Rebuild(evaluationBarIndex, evaluationUtc) : (_published ?? Rebuild(evaluationBarIndex, evaluationUtc));
        }

        if (_bars.TryGetValue(observation.BarIndex, out var existing))
        {
            _duplicateBarIndexObservations++;
            if (existing.SourceVersion != observation.SourceVersion
                && (existing.Open != observation.Open
                    || existing.High != observation.High
                    || existing.Low != observation.Low
                    || existing.Close != observation.Close
                    || existing.TotalVolume != observation.TotalVolume
                    || existing.PriceVolumes.Count != observation.PriceVolumes.Count))
            {
                _revisionEvents.Add($"BAR_REVISION:bar={observation.BarIndex}:v{existing.SourceVersion}->v{observation.SourceVersion}");
                _barRevisionCounts.TryGetValue(observation.BarIndex, out var rc);
                _barRevisionCounts[observation.BarIndex] = rc + 1;
            }
        }

        _bars[observation.BarIndex] = observation;
        if (!rebuildNow)
            return _published ?? new PrimaryProfileSetSnapshot(
                null, null,
                HistoricalInitializationState.InProgress,
                evaluationBarIndex,
                new[] { "HISTORICAL_INGEST_DEFERRED_REBUILD", "TIMESTAMP_POLICY=" + AtasTimestampNormalizer.PolicyVersion },
                _revisionEvents.Count > 64 ? _revisionEvents.TakeLast(64).ToArray() : _revisionEvents.ToArray(),
                _latestTimestampDiagnostic);

        return Rebuild(evaluationBarIndex, evaluationUtc);
    }

    public bool ContainsBar(int barIndex) => _bars.ContainsKey(barIndex);

    public PrimaryProfileSetSnapshot Rebuild(int evaluationBarIndex, DateTimeOffset evaluationUtc)
    {
        var grid = new PriceGrid(_tickSize);
        var clock = new PrimaryAuctionClock(_config);
        var bars = _bars.Values
            .Where(b => b.BarIndex <= evaluationBarIndex
                        && string.Equals(b.TimestampPolicyVersion, AtasTimestampNormalizer.PolicyVersion, StringComparison.Ordinal))
            .OrderBy(b => b.BarIndex)
            .ToList();

        if (bars.Count == 0)
        {
            _published = new PrimaryProfileSetSnapshot(
                null, null,
                HistoricalInitializationState.NotStarted,
                evaluationBarIndex,
                new[] { "NO_BARS", "TIMESTAMP_POLICY=" + AtasTimestampNormalizer.PolicyVersion },
                _revisionEvents.ToArray(),
                _latestTimestampDiagnostic);
            return _published;
        }

        var byAuction = new Dictionary<string, List<ProfileBarObservation>>(StringComparer.Ordinal);
        var auctionMeta = new Dictionary<string, AuctionClockPoint>(StringComparer.Ordinal);

        foreach (var bar in bars)
        {
            var point = clock.Resolve(bar.StartUtc);
            if (!byAuction.TryGetValue(point.AuctionId, out var list))
            {
                list = new List<ProfileBarObservation>();
                byAuction[point.AuctionId] = list;
                auctionMeta[point.AuctionId] = point;
            }

            list.Add(bar);
        }

        var orderedAuctions = auctionMeta.Values
            .OrderBy(a => a.AuctionStartUtc)
            .ToList();

        PrimaryAuctionProfileSnapshot? current = null;
        PrimaryAuctionProfileSnapshot? previous = null;

        if (orderedAuctions.Count > 0)
        {
            var curPoint = orderedAuctions[^1];
            var prevPoint = orderedAuctions.Count > 1 ? orderedAuctions[^2] : null;

            if (prevPoint is not null)
            {
                previous = BuildAuction(
                    prevPoint,
                    byAuction[prevPoint.AuctionId],
                    clock,
                    grid,
                    evaluationUtc,
                    isCompleted: true,
                    previousTpoPocTick: null,
                    previousVolPocTick: null);
                if (previous.TpoProfile?.TpoPoc is decimal pp)
                    _prevTpoPocTick = grid.ToTickIndex(pp);
                if (previous.VolumeProfile?.VolumePoc is decimal vp)
                    _prevVolPocTick = grid.ToTickIndex(vp);
            }

            current = BuildAuction(
                curPoint,
                byAuction[curPoint.AuctionId],
                clock,
                grid,
                evaluationUtc,
                isCompleted: evaluationUtc.UtcDateTime >= curPoint.AuctionEndUtc,
                previousTpoPocTick: _prevTpoPocTick,
                previousVolPocTick: _prevVolPocTick);
        }

        var init = evaluationBarIndex >= bars.Max(b => b.BarIndex)
            ? HistoricalInitializationState.Complete
            : HistoricalInitializationState.InProgress;

        _published = new PrimaryProfileSetSnapshot(
            current,
            previous,
            init,
            evaluationBarIndex,
            new[]
            {
                "CURRENT_PREVIOUS_ONLY",
                "NO_COMPOSITE",
                "NO_STRUCTURAL_REFERENCES",
                "TIMESTAMP_POLICY=" + AtasTimestampNormalizer.PolicyVersion,
                "TIMESTAMP_SEMANTICS=" + AtasTimestampNormalizer.CandleTimeSemantics,
                "TIMESTAMP_PROVENANCE=" + AtasTimestampNormalizer.CandleTimeProvenance
            },
            _revisionEvents.Count > 64 ? _revisionEvents.TakeLast(64).ToArray() : _revisionEvents.ToArray(),
            _latestTimestampDiagnostic);
        return _published;
    }

    private PrimaryAuctionProfileSnapshot BuildAuction(
        AuctionClockPoint point,
        List<ProfileBarObservation> bars,
        PrimaryAuctionClock clock,
        PriceGrid grid,
        DateTimeOffset evaluationUtc,
        bool isCompleted,
        long? previousTpoPocTick,
        long? previousVolPocTick)
    {
        var lastBar = bars.Count == 0 ? -1 : bars.Max(b => b.BarIndex);
        var referenceResolution = TpoParityReferencePriceResolver.Resolve(
            EnableTpoParityReferencePrice,
            TpoParityReferencePrice,
            _tickSize);
        var resolvedReference = referenceResolution.NormalizedPrice;

        var tpo = ClassicTpoEngine.Build(
            point.AuctionId,
            point.AuctionStartUtc,
            point.AuctionEndUtc,
            bars,
            clock,
            grid,
            _config.ValueAreaFraction,
            previousTpoPocTick,
            evaluationUtc,
            lastProcessedBar: lastBar,
            parityReferencePrice: resolvedReference,
            barRevisionCounts: _barRevisionCounts);

        // Attach ledger forensics (diagnostics only — does not change POC/VA).
        if (tpo.ParityDiagnostic is not null)
        {
            var ledger = BuildLedgerAudit(point, bars, evaluationUtc, lastBar);
            var rejectReason = referenceResolution.IsActive
                ? null
                : (string.Equals(referenceResolution.Reason, TpoParityReferencePriceResolver.RejectDisabled, StringComparison.Ordinal)
                    ? null
                    : referenceResolution.Reason);
            var diag = tpo.ParityDiagnostic.WithLedgerAndReference(
                ledger,
                tpo.ParityDiagnostic.ReferenceTarget,
                resolvedReference,
                rejectReason);
            tpo = tpo.WithParityDiagnostic(diag);
        }

        var vol = VolumeProfileEngine.Build(
            point.AuctionId,
            point.AuctionStartUtc,
            point.AuctionEndUtc,
            bars,
            grid,
            _config.ValueAreaFraction,
            previousVolPocTick);

        var limitations = new List<string>();
        limitations.AddRange(tpo.KnownLimitations);
        limitations.AddRange(vol.KnownLimitations);
        limitations.Add("TIMESTAMP_POLICY=" + AtasTimestampNormalizer.PolicyVersion);

        var tpoReady = tpo.TpoPoc is not null && tpo.TotalTpoCount > 0;
        var volReady = vol.PriceVolumeCapability == PriceVolumeCapability.Exact && vol.VolumePoc is not null;

        AuctionProfileState state;
        if (!tpoReady)
            state = AuctionProfileState.NotReady;
        else if (tpoReady && volReady)
            state = AuctionProfileState.Ready;
        else
            state = AuctionProfileState.Partial;

        var high = Max(tpo.ProfileHigh, vol.ProfileHigh);
        var low = Min(tpo.ProfileLow, vol.ProfileLow);
        var lastPx = bars.OrderBy(b => b.BarIndex).LastOrDefault()?.Close;
        var range = bars.Count == 0 ? ((int, int)?)null : (bars.Min(b => b.BarIndex), bars.Max(b => b.BarIndex));

        return new PrimaryAuctionProfileSnapshot(
            state,
            point.AuctionId,
            point.AuctionStartUtc,
            point.AuctionEndUtc,
            isCompleted,
            tpo,
            vol,
            high,
            low,
            lastPx,
            evaluationUtc.UtcDateTime,
            range,
            state == AuctionProfileState.Ready ? ProfileDataQuality.Complete
                : state == AuctionProfileState.Partial ? ProfileDataQuality.Partial
                : ProfileDataQuality.Unknown,
            limitations.Distinct(StringComparer.Ordinal).ToArray());
    }

    private TpoLedgerAuditDiagnostic BuildLedgerAudit(
        AuctionClockPoint point,
        List<ProfileBarObservation> bars,
        DateTimeOffset evaluationUtc,
        int lastBar)
    {
        var auctionBars = bars
            .Where(b => b.StartUtc.UtcDateTime >= point.AuctionStartUtc
                        && b.StartUtc.UtcDateTime < point.AuctionEndUtc)
            .OrderBy(b => b.BarIndex)
            .ToList();

        var endExclusive = evaluationUtc.UtcDateTime < point.AuctionEndUtc
            ? evaluationUtc.UtcDateTime
            : point.AuctionEndUtc;
        var expectedStarts = new List<DateTimeOffset>();
        for (var t = point.AuctionStartUtc; t < endExclusive; t = t.AddMinutes(TpoM5SlotForensics.ChartMinutes))
            expectedStarts.Add(new DateTimeOffset(DateTime.SpecifyKind(t, DateTimeKind.Utc)));

        var observedStarts = new HashSet<DateTime>(auctionBars.Select(b => b.StartUtc.UtcDateTime));
        var missing = expectedStarts.Where(s => !observedStarts.Contains(s.UtcDateTime)).ToArray();
        var revised = _barRevisionCounts.Keys.OrderBy(i => i).ToArray();
        var rejected = _rejectedBars.Count > 64 ? _rejectedBars.TakeLast(64).ToArray() : _rejectedBars.ToArray();

        return new TpoLedgerAuditDiagnostic(
            auctionBars.Count,
            expectedStarts.Count,
            missing.Length,
            missing,
            _duplicateBarIndexObservations,
            revised,
            rejected,
            lastBar,
            auctionBars.Any(b => b.BarIndex == lastBar));
    }

    private static decimal? Max(decimal? a, decimal? b) =>
        a is null ? b : b is null ? a : Math.Max(a.Value, b.Value);

    private static decimal? Min(decimal? a, decimal? b) =>
        a is null ? b : b is null ? a : Math.Min(a.Value, b.Value);
}
