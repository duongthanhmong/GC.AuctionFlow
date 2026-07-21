using Aos.LevelEngine.Levels;
using Aos.LevelEngine.Profiles;

namespace Aos.LevelEngine.Interactions;

/// <summary>
/// Explicit state machine: ARMED | ACTIVE | RESET_PENDING | CLOSED.
/// One trade may open N interactions (overlapping zones) with shared ConcurrentInteractionGroupId.
/// </summary>
public sealed class InteractionLogger
{
    public const string LoggerVersion = "0.2.0";
    public const string OutcomeComputedByVersion = "0.2.0";

    private readonly InstrumentProfile _profile;
    private readonly RotationRState _rotationR;
    private readonly string _contractCode;
    private readonly Guid _sessionId;
    private readonly Guid _processInstanceId;
    private readonly string _mode;
    private readonly string _levelEngineVersion;
    private readonly InteractionOutcomeStore _outcomes = new();

    private readonly Dictionary<Guid, ZoneRuntime> _rt = new();
    private readonly List<LevelInteractionRecord> _completed = new();

    public InteractionLogger(
        InstrumentProfile profile,
        RotationRState rotationR,
        string contractCode,
        Guid sessionId,
        Guid processInstanceId,
        string declaredMode,
        string levelEngineVersion)
    {
        if (!rotationR.IsFrozen)
            throw new ArgumentException("RotationR must be frozen.");
        _profile = profile;
        _rotationR = rotationR;
        _contractCode = contractCode;
        _sessionId = sessionId;
        _processInstanceId = processInstanceId;
        _mode = declaredMode;
        _levelEngineVersion = levelEngineVersion;
    }

    public IReadOnlyList<LevelInteractionRecord> Completed => _completed;
    public InteractionOutcomeStore Outcomes => _outcomes;
    public int ResetTicks => _profile.ComputeResetTicks(_rotationR.RotationRTicks);
    public int ExitDistanceTicks => _rotationR.RotationRTicks;
    public int RotationR => _rotationR.RotationRTicks;

    private sealed class ZoneRuntime
    {
        public InteractionState State = InteractionState.ARMED;
        public ActiveInteraction? Active;
        // RESET_PENDING tracking (monotonic)
        public bool ResetOutside;
        public bool ResetFarEnough;
        public long? ResetMonoStart;
        public decimal? LastOutsidePrice;
    }

    private sealed class ActiveInteraction
    {
        public required Guid InteractionId;
        public required FrozenZone Zone;
        public required ApproachDirection Direction;
        public string? StartReason;
        public required DateTime StartExchange;
        public required long StartMono;
        public required decimal ReferencePrice;
        public Guid? ConcurrentGroupId;
        public double ApproachSpeed; // filled when approach path is instrumented
        public int? PenetrationDepth;
        public int MaxExcursionInside;
        public long VolumeInZone;
        public long VolumeAbove;
        public long VolumeBelow;
        public int TradeCount;
        public long? SeqStart;
        public long? SeqEnd;
        public decimal Highest;
        public decimal Lowest;
        public byte MarketValidity = 1;
        public byte TimingValidity = 1;
    }

    public void ArmLevels(IEnumerable<FrozenZone> zones)
    {
        foreach (var z in zones)
        {
            if (z.IsInactive) continue;
            if (z.EffectiveGrade is not (StructuralGrade.A or StructuralGrade.B)) continue;
            if (!_rt.ContainsKey(z.LevelId))
                _rt[z.LevelId] = new ZoneRuntime { State = InteractionState.ARMED };
        }
    }

    /// <summary>
    /// Feed one trade across all armed/active levels. Returns newly closed records.
    /// </summary>
    public IReadOnlyList<LevelInteractionRecord> OnTrade(
        IReadOnlyList<FrozenZone> zones,
        decimal price,
        long volume,
        DateTime timestampExchange,
        long monotonicTicks,
        long? localSeq,
        byte marketValidity,
        byte timingValidity,
        bool sessionJustOpened = false)
    {
        var closed = new List<LevelInteractionRecord>();
        Guid? groupId = null;
        var openedThisTrade = new List<Guid>();

        foreach (var zone in zones)
        {
            if (!_rt.TryGetValue(zone.LevelId, out var rt))
            {
                if (zone.IsInactive) continue;
                if (zone.EffectiveGrade is not (StructuralGrade.A or StructuralGrade.B)) continue;
                rt = new ZoneRuntime { State = InteractionState.ARMED };
                _rt[zone.LevelId] = rt;
            }

            if (rt.State == InteractionState.CLOSED)
                rt.State = InteractionState.RESET_PENDING;

            if (rt.State == InteractionState.ACTIVE && rt.Active is not null)
            {
                var c = UpdateActive(rt, zone, price, volume, timestampExchange, monotonicTicks,
                    localSeq, marketValidity, timingValidity);
                if (c is not null) closed.Add(c);
                continue;
            }

            if (rt.State == InteractionState.RESET_PENDING)
            {
                TrackReset(rt, zone, price, monotonicTicks);
                if (ResetFullySatisfied(rt, monotonicTicks) && zone.ContainsTradePrice(price))
                {
                    // fall through to ARMED→ACTIVE on this same trade after reset complete
                    rt.State = InteractionState.ARMED;
                    ClearReset(rt);
                }
                else
                    continue;
            }

            if (rt.State == InteractionState.ARMED && zone.ContainsTradePrice(price))
            {
                groupId ??= Guid.NewGuid();
                var dir = InferApproach(rt, zone, price, sessionJustOpened);
                var startReason = sessionJustOpened && zone.ContainsTradePrice(price)
                    ? "SESSION_OPEN_INSIDE_ZONE"
                    : null;
                if (sessionJustOpened && dir == ApproachDirection.UNKNOWN)
                    startReason = "SESSION_OPEN_INSIDE_ZONE";

                // If we have no prior outside history and session open inside → UNKNOWN
                if (rt.LastOutsidePrice is null && sessionJustOpened)
                {
                    dir = ApproachDirection.UNKNOWN;
                    startReason = "SESSION_OPEN_INSIDE_ZONE";
                }

                var refPx = ReferencePriceSelector.SelectOrMid(dir, zone);
                rt.Active = new ActiveInteraction
                {
                    InteractionId = Guid.NewGuid(),
                    Zone = zone,
                    Direction = dir,
                    StartReason = startReason,
                    StartExchange = timestampExchange,
                    StartMono = monotonicTicks,
                    ReferencePrice = refPx,
                    ConcurrentGroupId = groupId,
                    Highest = price,
                    Lowest = price,
                    SeqStart = localSeq,
                    SeqEnd = localSeq,
                    MarketValidity = marketValidity,
                    TimingValidity = timingValidity
                };
                Accumulate(rt.Active, zone, price, volume);
                rt.State = InteractionState.ACTIVE;
                openedThisTrade.Add(zone.LevelId);
            }
            else if (rt.State == InteractionState.ARMED && !zone.ContainsTradePrice(price))
            {
                rt.LastOutsidePrice = price;
            }
        }

        _ = openedThisTrade;
        return closed;
    }

    public LevelInteractionRecord? NotifyProcessRestart(DateTime timestampExchange, long monotonicTicks)
    {
        LevelInteractionRecord? last = null;
        foreach (var (id, rt) in _rt.ToList())
        {
            if (rt.State == InteractionState.ACTIVE && rt.Active is not null)
                last = Complete(rt, rt.Active.Zone, timestampExchange, monotonicTicks, InteractionCloseReason.ProcessRestart);
        }
        return last;
    }

    public IReadOnlyList<LevelInteractionRecord> EndSession(DateTime timestampExchange, long monotonicTicks)
    {
        var list = new List<LevelInteractionRecord>();
        foreach (var rt in _rt.Values.ToList())
        {
            if (rt.State == InteractionState.ACTIVE && rt.Active is not null)
            {
                var c = Complete(rt, rt.Active.Zone, timestampExchange, monotonicTicks, InteractionCloseReason.SessionEnd);
                if (c is not null) list.Add(c);
            }
        }
        return list;
    }

    /// <summary>Test helper: continuous reset tracking.</summary>
    public (bool satisfied, long elapsedMono) ProbeReset(
        FrozenZone zone,
        IReadOnlyList<(decimal price, long mono)> outsideSamples,
        long frequency)
    {
        var rt = new ZoneRuntime { State = InteractionState.RESET_PENDING };
        foreach (var (price, mono) in outsideSamples)
            TrackReset(rt, zone, price, mono);
        var ok = ResetFullySatisfied(rt, outsideSamples[^1].mono);
        var elapsed = rt.ResetMonoStart is null ? 0 : outsideSamples[^1].mono - rt.ResetMonoStart.Value;
        return (ok, elapsed);
    }

    private static ApproachDirection InferApproach(ZoneRuntime rt, FrozenZone zone, decimal price, bool sessionOpen)
    {
        if (sessionOpen && zone.ContainsTradePrice(price) && rt.LastOutsidePrice is null)
            return ApproachDirection.UNKNOWN;
        if (rt.LastOutsidePrice is null)
            return ApproachDirection.UNKNOWN;
        if (rt.LastOutsidePrice < zone.ZoneLower) return ApproachDirection.FROM_BELOW;
        if (rt.LastOutsidePrice > zone.ZoneUpper) return ApproachDirection.FROM_ABOVE;
        return ApproachDirection.UNKNOWN;
    }

    private void TrackReset(ZoneRuntime rt, FrozenZone zone, decimal price, long mono)
    {
        var outside = !zone.ContainsTradePrice(price);
        var dist = DistanceFromNearestEdgeTicks(zone, price);
        var far = outside && dist >= ResetTicks;

        if (!(outside && far))
        {
            // Violate any condition → timer reset to 0
            ClearReset(rt);
            rt.LastOutsidePrice = outside ? price : rt.LastOutsidePrice;
            return;
        }

        rt.ResetOutside = true;
        rt.ResetFarEnough = true;
        rt.LastOutsidePrice = price;
        rt.ResetMonoStart ??= mono;
    }

    private bool ResetFullySatisfied(ZoneRuntime rt, long monoNow)
    {
        if (!rt.ResetOutside || !rt.ResetFarEnough || rt.ResetMonoStart is null)
            return false;
        // seed value, subject to sensitivity test (Interaction.ResetTimeMinutes)
        var need = TimeSpan.FromMinutes(_profile.Interaction.ResetTimeMinutes);
        var freq = System.Diagnostics.Stopwatch.Frequency;
        var elapsed = TimeSpan.FromSeconds((double)(monoNow - rt.ResetMonoStart.Value) / freq);
        return elapsed >= need;
    }

    private static void ClearReset(ZoneRuntime rt)
    {
        rt.ResetOutside = false;
        rt.ResetFarEnough = false;
        rt.ResetMonoStart = null;
    }

    private decimal DistanceFromNearestEdgeTicks(FrozenZone zone, decimal price)
    {
        if (price < zone.ZoneLower)
            return (zone.ZoneLower - price) / _profile.TickSize;
        if (price > zone.ZoneUpper)
            return (price - zone.ZoneUpper) / _profile.TickSize;
        return 0;
    }

    private LevelInteractionRecord? UpdateActive(
        ZoneRuntime rt,
        FrozenZone zone,
        decimal price,
        long volume,
        DateTime timestampExchange,
        long monotonicTicks,
        long? localSeq,
        byte marketValidity,
        byte timingValidity)
    {
        var act = rt.Active!;
        if (marketValidity == 0)
            return Complete(rt, zone, timestampExchange, monotonicTicks, InteractionCloseReason.MarketDataInvalid);

        Accumulate(act, zone, price, volume);
        act.SeqEnd = localSeq ?? act.SeqEnd;
        act.MarketValidity = marketValidity;
        act.TimingValidity = timingValidity;

        // seed value, subject to sensitivity test (Interaction.MaxInteractionTimeMinutes)
        var maxMin = _profile.Interaction.MaxInteractionTimeMinutes
            ?? throw new InvalidOperationException("Interaction.MaxInteractionTimeMinutes required");
        if ((timestampExchange - act.StartExchange).TotalMinutes >= maxMin)
            return Complete(rt, zone, timestampExchange, monotonicTicks, InteractionCloseReason.MaxInteractionTime);

        // ExitDistance from zone EDGE
        if (price >= zone.ZoneUpper + ExitDistanceTicks * _profile.TickSize
            || price <= zone.ZoneLower - ExitDistanceTicks * _profile.TickSize)
            return Complete(rt, zone, timestampExchange, monotonicTicks, InteractionCloseReason.ExitDistance);

        return null;
    }

    private void Accumulate(ActiveInteraction act, FrozenZone zone, decimal price, long volume)
    {
        var tick = _profile.TickSize;
        var rBand = RotationR * tick;
        act.TradeCount++;
        if (price > act.Highest) act.Highest = price;
        if (price < act.Lowest) act.Lowest = price;

        if (price >= zone.ZoneLower && price <= zone.ZoneUpper)
            act.VolumeInZone += volume;
        else if (price > zone.ZoneUpper && price <= zone.ZoneUpper + rBand)
            act.VolumeAbove += volume;
        else if (price >= zone.ZoneLower - rBand && price < zone.ZoneLower)
            act.VolumeBelow += volume;

        act.PenetrationDepth = act.Direction switch
        {
            ApproachDirection.FROM_BELOW => (int)decimal.Round(
                Math.Max(0, act.Highest - zone.ZoneLower) / tick, 0, MidpointRounding.AwayFromZero),
            ApproachDirection.FROM_ABOVE => (int)decimal.Round(
                Math.Max(0, zone.ZoneUpper - act.Lowest) / tick, 0, MidpointRounding.AwayFromZero),
            ApproachDirection.UNKNOWN => null,
            _ => null
        };

        var hi = Math.Min(act.Highest, zone.ZoneUpper);
        var lo = Math.Max(act.Lowest, zone.ZoneLower);
        if (hi >= lo)
        {
            var inside = (int)decimal.Round((hi - lo) / tick, 0, MidpointRounding.AwayFromZero);
            if (inside > act.MaxExcursionInside) act.MaxExcursionInside = inside;
        }
    }

    private LevelInteractionRecord Complete(
        ZoneRuntime rt,
        FrozenZone zone,
        DateTime endExchange,
        long endMono,
        InteractionCloseReason reason)
    {
        var act = rt.Active!;
        rt.Active = null;
        rt.State = InteractionState.CLOSED;
        ClearReset(rt);
        // Immediately enter RESET_PENDING for re-arm path
        rt.State = InteractionState.RESET_PENDING;

        zone.RecordCompletedInteraction();

        var rec = new LevelInteractionRecord
        {
            InteractionId = act.InteractionId,
            SessionId = _sessionId,
            ProcessInstanceId = _processInstanceId,
            DeclaredDataSourceMode = _mode,
            InstrumentProfileId = _profile.ProfileId,
            LevelEngineVersion = _levelEngineVersion,
            InterpretationVersion = LevelSetEngine.InterpretationVersion,
            ContractCode = _contractCode,
            LevelId = zone.LevelId,
            LevelType = zone.LevelType.ToString(),
            StructuralGrade = zone.StructuralGrade.ToString(),
            EffectiveGrade = zone.EffectiveGrade?.ToString(),
            FreshnessState = zone.FreshnessState.ToString(),
            ConfluenceCount = zone.ConfluenceCount,
            ZoneUpper = zone.ZoneUpper,
            ZoneLower = zone.ZoneLower,
            ZoneMidPrice = zone.MidPrice,
            FrozenAtExchangeTime = zone.FrozenAtExchangeTime,
            ClusterId = zone.ClusterId,
            IsOverwideCluster = zone.IsOverwideCluster,
            ConcurrentInteractionGroupId = act.ConcurrentGroupId,
            StartTimeExchange = act.StartExchange,
            EndTimeExchange = endExchange,
            MonotonicStartTicks = act.StartMono,
            MonotonicEndTicks = endMono,
            ApproachDirection = act.Direction,
            StartReason = act.StartReason,
            ApproachSpeedTicksPerMin = act.ApproachSpeed,
            PenetrationDepthTicks = act.PenetrationDepth,
            MaxExcursionInsideZone = act.MaxExcursionInside,
            VolumeInZone = act.VolumeInZone,
            VolumeAbove = act.VolumeAbove,
            VolumeBelow = act.VolumeBelow,
            TradeCount = act.TradeCount,
            NewTradeRange = new StreamRange
            {
                StreamType = "NewTrade",
                ProcessInstanceId = _processInstanceId,
                LocalSequenceStart = act.SeqStart,
                LocalSequenceEnd = act.SeqEnd
            },
            MarketDataValidity = act.MarketValidity,
            TimingDiagnosticValidity = act.TimingValidity,
            ReferencePrice = act.ReferencePrice,
            CloseReason = reason.ToString()
        };
        _completed.Add(rec);

        // Seed PENDING outcome rows (append-only); horizons filled later without updating this interaction row.
        SeedPendingOutcomes(rec, endExchange);
        return rec;
    }

    private void SeedPendingOutcomes(LevelInteractionRecord rec, DateTime endExchange)
    {
        void Seed(HorizonType h, DateTime? target, OutcomeStatus status, string? why) =>
            _outcomes.Append(
                rec.InteractionId, h, target, endExchange, rec.ReferencePrice,
                null, null, null, null, status, why, OutcomeComputedByVersion);

        Seed(HorizonType.M5, rec.StartTimeExchange.AddMinutes(5), OutcomeStatus.PENDING, null);
        Seed(HorizonType.M15, rec.StartTimeExchange.AddMinutes(15), OutcomeStatus.PENDING, null);
        Seed(HorizonType.M30, rec.StartTimeExchange.AddMinutes(30), OutcomeStatus.PENDING, null);
        Seed(HorizonType.NEXT_INTERACTION_SAME_LEVEL, null, OutcomeStatus.PENDING, "PENDING_NEXT_INTERACTION_SAME_LEVEL");
        Seed(HorizonType.SESSION_END, null, OutcomeStatus.PENDING, "SESSION_NOT_ENDED");
    }

    /// <summary>Recompute a horizon by appending a new OutcomeVersion (never updates).</summary>
    public InteractionOutcomeRecord AppendHorizonOutcome(
        Guid interactionId,
        HorizonType horizon,
        DateTime observedUntil,
        DateTime? target,
        int? upTicks,
        int? downTicks,
        OutcomeStatus status,
        string? truncationReason)
    {
        var r = _rotationR.RotationRTicks;
        return _outcomes.Append(
            interactionId, horizon, target, observedUntil,
            _completed.First(c => c.InteractionId == interactionId).ReferencePrice,
            upTicks, downTicks,
            upTicks is null || r == 0 ? null : (double)upTicks.Value / r,
            downTicks is null || r == 0 ? null : (double)downTicks.Value / r,
            status, truncationReason, OutcomeComputedByVersion);
    }
}
