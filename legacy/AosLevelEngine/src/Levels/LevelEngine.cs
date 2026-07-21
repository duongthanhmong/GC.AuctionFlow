using System.Text.Json;
using Aos.LevelEngine.Profiles;
using Aos.LevelEngine.Session;

namespace Aos.LevelEngine.Levels;

/// <summary>Completed-session / overnight inputs — no lookahead past batch freeze rules.</summary>
public sealed class PriorSessionInputs
{
    public required string ContractCode { get; init; }
    public required DateOnly SessionDateEt { get; init; }

    public decimal? PriorRthHigh { get; init; }
    public decimal? PriorRthLow { get; init; }
    public string? PriorRthSessionId { get; init; }

    public decimal? WeeklyHigh { get; init; }
    public decimal? WeeklyLow { get; init; }
    public decimal? CompositeHigh { get; init; }
    public decimal? CompositeLow { get; init; }

    public IReadOnlyDictionary<decimal, long>? PriorRthVolumeByPrice { get; init; }

    /// <summary>Candidate nPOC mids with zone widths applied at create; activity checked separately.</summary>
    public IReadOnlyList<NpocCandidate>? UntestedPriorPocs { get; init; }

    /// <summary>Overnight high/low — must be computed only from exchange times &lt; 09:30 ET.</summary>
    public decimal? OvernightHigh { get; init; }
    public decimal? OvernightLow { get; init; }

    /// <summary>IB high/low — only from trades in [09:30, 10:30) ET.</summary>
    public decimal? InitialBalanceHigh { get; init; }
    public decimal? InitialBalanceLow { get; init; }
}

public sealed class NpocCandidate
{
    public required decimal MidPrice { get; init; }
    public required decimal ZoneLower { get; init; }
    public required decimal ZoneUpper { get; init; }
    public required string SourceSession { get; init; }
    public required IReadOnlyList<(DateTime TimestampExchange, decimal Price)> TradesSinceFormation { get; init; }
}

public sealed class LevelEngineSnapshot
{
    public required Guid SnapshotId { get; init; }
    public required string LevelEngineVersion { get; init; }
    public required string InterpretationVersion { get; init; }
    public required string InstrumentProfileId { get; init; }
    public required string ContractCode { get; init; }
    public required DateOnly SessionDateEt { get; init; }
    public required LevelBatch Batch { get; init; }
    public required DateTime FrozenAtExchangeTime { get; init; }
    public required IReadOnlyList<FrozenZone> Levels { get; init; }
    public required IReadOnlyList<LevelCluster> Clusters { get; init; }
    public required IReadOnlyList<LevelOmission> Omissions { get; init; }
}

/// <summary>
/// DRAFT→v1 patch: three event-time batches. No mid-session levels except IB_SET.
/// OUT OF SCOPE: VWAP, developing POC/HVN/LVN, intraday swings, setup-derived.
/// </summary>
public sealed class LevelSetEngine
{
    public const string EngineVersion = "0.2.0";
    public const string InterpretationVersion = "1.1.0";

    private readonly InstrumentProfile _profile;
    private readonly TimeZoneInfo _tz;
    private readonly List<FrozenZone> _levels = new();
    private readonly List<LevelOmission> _omissions = new();
    private IReadOnlyList<LevelCluster> _clusters = Array.Empty<LevelCluster>();
    private bool _presessionDone;
    private bool _openDone;
    private bool _ibDone;
    private string? _contractCode;
    private DateOnly _sessionDate;

    public LevelSetEngine(InstrumentProfile profile)
    {
        _profile = profile;
        if (profile.LevelEngine.AllowMidSessionLevelCreation)
            throw new InvalidOperationException("FAIL STARTUP: AllowMidSessionLevelCreation must be false for ES v0.1.");
        _tz = ExchangeClock.Resolve(profile.Timezone);
    }

    public IReadOnlyList<FrozenZone> Levels => _levels;
    public IReadOnlyList<LevelOmission> Omissions => _omissions;
    public IReadOnlyList<LevelCluster> Clusters => _clusters;

    /// <summary>PRESESSION_SET — freeze by exchange time ≤ 09:29:59 ET.</summary>
    public LevelEngineSnapshot FreezePreSessionSet(
        PriorSessionInputs inputs,
        DateTime freezeExchangeTime,
        DateTime frozenAtUtc)
    {
        if (_presessionDone)
            throw new InvalidOperationException("PRESESSION_SET already frozen.");
        if (!ExchangeClock.IsBefore(freezeExchangeTime, _tz, "09:30"))
            throw new InvalidOperationException(
                "PRESESSION_SET must freeze at exchange time < 09:30:00 ET (latest 09:29:59).");

        _contractCode = inputs.ContractCode;
        _sessionDate = inputs.SessionDateEt;
        var batch = LevelBatch.PRESESSION_SET;

        TryAdd(batch, LevelTypeKind.Pdh, inputs.PriorRthHigh, freezeExchangeTime, frozenAtUtc, inputs.PriorRthSessionId);
        TryAdd(batch, LevelTypeKind.Pdl, inputs.PriorRthLow, freezeExchangeTime, frozenAtUtc, inputs.PriorRthSessionId);
        TryAdd(batch, LevelTypeKind.WeeklyHigh, inputs.WeeklyHigh, freezeExchangeTime, frozenAtUtc, "Weekly");
        TryAdd(batch, LevelTypeKind.WeeklyLow, inputs.WeeklyLow, freezeExchangeTime, frozenAtUtc, "Weekly");
        TryAdd(batch, LevelTypeKind.CompositeBoundary, inputs.CompositeHigh, freezeExchangeTime, frozenAtUtc,
            $"CompositeLookback={_profile.LevelEngine.CompositeLookbackSessions}");
        TryAdd(batch, LevelTypeKind.CompositeBoundary, inputs.CompositeLow, freezeExchangeTime, frozenAtUtc,
            $"CompositeLookback={_profile.LevelEngine.CompositeLookbackSessions}");

        if (inputs.PriorRthVolumeByPrice is null)
        {
            Omit(LevelTypeKind.Poc, LevelOmissionReason.MissingHistoricalData, "PriorRthVolumeByPrice missing");
            Omit(LevelTypeKind.Vah, LevelOmissionReason.MissingHistoricalData, "PriorRthVolumeByPrice missing");
            Omit(LevelTypeKind.Val, LevelOmissionReason.MissingHistoricalData, "PriorRthVolumeByPrice missing");
        }
        else
        {
            var va = VolumeProfile.Compute(inputs.PriorRthVolumeByPrice, _profile);
            if (va is null)
            {
                Omit(LevelTypeKind.Poc, LevelOmissionReason.MissingHistoricalData, "empty volume profile");
            }
            else
            {
                TryAdd(batch, LevelTypeKind.Poc, va.Value.Poc, freezeExchangeTime, frozenAtUtc, inputs.PriorRthSessionId);
                TryAdd(batch, LevelTypeKind.Vah, va.Value.Vah, freezeExchangeTime, frozenAtUtc, inputs.PriorRthSessionId);
                TryAdd(batch, LevelTypeKind.Val, va.Value.Val, freezeExchangeTime, frozenAtUtc, inputs.PriorRthSessionId);
            }
        }

        if (inputs.UntestedPriorPocs is not null)
        {
            foreach (var n in inputs.UntestedPriorPocs)
            {
                if (!NpocActivity.RemainsActive(n.ZoneLower, n.ZoneUpper, n.TradesSinceFormation))
                {
                    Omit(LevelTypeKind.Npoc, LevelOmissionReason.NpocAlreadyTested, n.SourceSession);
                    continue;
                }
                TryAdd(batch, LevelTypeKind.Npoc, n.MidPrice, freezeExchangeTime, frozenAtUtc, n.SourceSession);
            }
        }

        // Single-print: CẦN_XÁC_MINH — do not implement fake TPO
        if (_profile.LevelEngine.SinglePrintEdgesEnabled)
        {
            if (_profile.LevelEngine.SinglePrintStatus.Contains("CAN_XAC_MINH", StringComparison.OrdinalIgnoreCase)
                || _profile.LevelEngine.SinglePrintStatus.Contains("CẦN", StringComparison.OrdinalIgnoreCase))
            {
                Omit(LevelTypeKind.SinglePrintEdge, LevelOmissionReason.SinglePrintNotVerified,
                    "CẦN XÁC MINH TRÊN ATAS THẬT — TPO source not verified; not implemented");
            }
        }
        else
        {
            Omit(LevelTypeKind.SinglePrintEdge, LevelOmissionReason.SinglePrintNotVerified,
                _profile.LevelEngine.SinglePrintStatus);
        }

        Recluster();
        _presessionDone = true;
        return Snapshot(batch, freezeExchangeTime);
    }

    /// <summary>
    /// OPEN_SET (ONH/ONL) — freeze at first valid event ≥ 09:30:00 ET,
    /// using ONLY overnight data with exchange time &lt; 09:30:00 ET.
    /// </summary>
    public LevelEngineSnapshot FreezeOpenSet(
        decimal? overnightHighFromDataBefore930,
        decimal? overnightLowFromDataBefore930,
        DateTime triggeringEventExchangeTime,
        DateTime frozenAtUtc,
        bool callerAttestsDataStrictlyBefore930)
    {
        if (!_presessionDone)
            throw new InvalidOperationException("PRESESSION_SET required first.");
        if (_openDone)
            throw new InvalidOperationException("OPEN_SET already frozen.");
        if (!ExchangeClock.IsAtOrAfter(triggeringEventExchangeTime, _tz, "09:30"))
            throw new InvalidOperationException("OPEN_SET freezes only at event exchange time >= 09:30:00 ET.");
        if (!callerAttestsDataStrictlyBefore930)
            throw new InvalidOperationException(
                "OPEN_SET requires overnight High/Low computed only from TimestampExchange < 09:30:00 ET.");

        var batch = LevelBatch.OPEN_SET;
        TryAdd(batch, LevelTypeKind.Onh, overnightHighFromDataBefore930, triggeringEventExchangeTime, frozenAtUtc, "Overnight");
        TryAdd(batch, LevelTypeKind.Onl, overnightLowFromDataBefore930, triggeringEventExchangeTime, frozenAtUtc, "Overnight");
        Recluster();
        _openDone = true;
        return Snapshot(batch, triggeringEventExchangeTime);
    }

    /// <summary>
    /// IB_SET — freeze at first valid event ≥ 10:30:00 ET,
    /// using ONLY trades in [09:30:00, 10:30:00) ET.
    /// </summary>
    public LevelEngineSnapshot FreezeIbSet(
        decimal? ibHighFromIbWindow,
        decimal? ibLowFromIbWindow,
        DateTime triggeringEventExchangeTime,
        DateTime frozenAtUtc,
        bool callerAttestsTradesInIbWindowOnly)
    {
        if (!_openDone)
            throw new InvalidOperationException("OPEN_SET required first.");
        if (_ibDone)
            throw new InvalidOperationException("IB_SET already frozen.");
        if (!ExchangeClock.IsAtOrAfter(triggeringEventExchangeTime, _tz, "10:30"))
            throw new InvalidOperationException("IB_SET freezes only at event exchange time >= 10:30:00 ET.");
        if (!callerAttestsTradesInIbWindowOnly)
            throw new InvalidOperationException(
                "IB_SET requires High/Low from trades with TimestampExchange in [09:30, 10:30) ET only.");

        var batch = LevelBatch.IB_SET;
        TryAdd(batch, LevelTypeKind.Ibh, ibHighFromIbWindow, triggeringEventExchangeTime, frozenAtUtc, "InitialBalance");
        TryAdd(batch, LevelTypeKind.Ibl, ibLowFromIbWindow, triggeringEventExchangeTime, frozenAtUtc, "InitialBalance");
        Recluster();
        _ibDone = true;
        return Snapshot(batch, triggeringEventExchangeTime);
    }

    public void RejectOutOfScopeLevel(string kind) =>
        throw new InvalidOperationException($"OUT OF SCOPE for Level Engine v1: {kind}");

    public string WriteLevelManifestJson()
    {
        var entries = _levels.Select(z => new LevelManifestEntry
        {
            LevelId = z.LevelId,
            LevelType = z.LevelType.ToString(),
            SourceSession = z.SourceSession,
            SourcePrice = z.SourcePrice,
            ZoneLower = z.ZoneLower,
            ZoneUpper = z.ZoneUpper,
            FrozenAtExchangeTime = z.FrozenAtExchangeTime,
            StructuralGrade = z.StructuralGrade.ToString(),
            FreshnessState = z.FreshnessState.ToString(),
            OmissionReason = null
        }).ToList();

        foreach (var o in _omissions)
        {
            entries.Add(new LevelManifestEntry
            {
                LevelId = Guid.Empty,
                LevelType = o.LevelType.ToString(),
                SourceSession = null,
                SourcePrice = 0,
                ZoneLower = 0,
                ZoneUpper = 0,
                FrozenAtExchangeTime = default,
                StructuralGrade = "",
                FreshnessState = "",
                OmissionReason = $"{o.Reason}:{o.Detail}"
            });
        }

        return JsonSerializer.Serialize(new
        {
            contractCode = _contractCode,
            sessionDateEt = _sessionDate.ToString("yyyy-MM-dd"),
            levelEngineVersion = EngineVersion,
            interpretationVersion = InterpretationVersion,
            instrumentProfileId = _profile.ProfileId,
            levels = entries
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    private void TryAdd(
        LevelBatch batch,
        LevelTypeKind kind,
        decimal? mid,
        DateTime freezeEx,
        DateTime freezeUtc,
        string? sourceSession)
    {
        if (mid is null)
        {
            Omit(kind, LevelOmissionReason.MissingHistoricalData, "null source price — no interpolation");
            return;
        }

        _levels.Add(ZoneFactory.Create(_profile, kind, batch, mid.Value, freezeEx, freezeUtc, sourceSession));
    }

    private void Omit(LevelTypeKind kind, LevelOmissionReason reason, string? detail) =>
        _omissions.Add(new LevelOmission { LevelType = kind, Reason = reason, Detail = detail });

    private void Recluster()
    {
        foreach (var z in _levels)
            z.ClearClusterAssignment();
        _clusters = ClusterBuilder.AssignClusters(_levels, _profile);
    }

    private LevelEngineSnapshot Snapshot(LevelBatch batch, DateTime freezeEx) => new()
    {
        SnapshotId = Guid.NewGuid(),
        LevelEngineVersion = EngineVersion,
        InterpretationVersion = InterpretationVersion,
        InstrumentProfileId = _profile.ProfileId,
        ContractCode = _contractCode ?? "",
        SessionDateEt = _sessionDate,
        Batch = batch,
        FrozenAtExchangeTime = freezeEx,
        Levels = _levels.ToList(),
        Clusters = _clusters,
        Omissions = _omissions.ToList()
    };
}
