using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using ATAS.DataFeedsCore;
using ATAS.Indicators;
using Aos.LevelEngine.Profiles;

namespace Aos.LevelEngine.Atas;

/// <summary>
/// ATAS shell: Historical Capability Probe first (no level computation yet).
/// Bridge to AosLevelEngine core (net8) hosted in net10 ATAS process.
/// </summary>
[DisplayName("AOS Level Engine — Historical Capability Probe")]
[Category("AOS")]
public sealed class AosLevelEngineAtasIndicator : Indicator
{
    public const string ShellBuild = "LE-ATAS-H04";

    /// <summary>seed value, subject to sensitivity test — FixedProfile wait budget.</summary>
    private const int FixedProfileTimeoutMs = 30_000;

    private InstrumentProfile? _profile;
    private int _lastHandledExportNonce;
    private int _lastAutoBars = -1;
    private bool _exportInFlight;
    private bool _pendingFromRecalculate;

    [DisplayName("Export Nonce")]
    [Description("Increment (1,2,3…) then Apply to force a new HistoricalCapability export. Repeatable.")]
    public int ExportNonce { get; set; }

    [DisplayName("Export HistoricalCapability")]
    [Description("Tick then Apply → one export, then auto-clears. Tick again for another run (no one-shot block).")]
    public bool ExportHistoricalCapability { get; set; }

    [DisplayName("Include RequestFixedProfile(LastDay)")]
    [Description("When exporting, await RequestFixedProfileAsync(LastDay) and embed result in HistoricalCapability.json.")]
    public bool IncludeFixedProfileRequest { get; set; } = true;

    [DisplayName("Try RequestFixedProfile(LastDay) alone")]
    [Description("Standalone FixedProfile attempt (also clears after Apply). Prefer Include on export.")]
    public bool TryRequestFixedProfile { get; set; }

    [DisplayName("Status")]
    public string Status { get; private set; } = "Idle " + ShellBuild;

    public AosLevelEngineAtasIndicator()
    {
        DenyToChangePanel = true;
        EnableCustomDrawing = false;
    }

    protected override void OnInitialize()
    {
        try
        {
            _profile = InstrumentProfileLoader.LoadEmbeddedEs();
            // Each initialize schedules a fresh export (repeatable; no sticky "already ran" flag).
            ExportNonce = _lastHandledExportNonce + 1;
            Status =
                $"{ShellBuild} init {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z " +
                $"profile={_profile.ProfileId} contract={InstrumentInfo?.Instrument ?? "(pending)"} " +
                $"exportNonce={ExportNonce}";
            WriteLog("OnInitialize OK " + Status);
        }
        catch (Exception ex)
        {
            _profile = null;
            Status = $"{ShellBuild} FAIL STARTUP {DateTime.UtcNow:O}: {ex.Message}";
            WriteLog(Status);
        }
    }

    protected override void OnRecalculate()
    {
        // Apply / property change typically triggers recalculate — arm export without waiting for a new tick.
        _pendingFromRecalculate = ExportHistoricalCapability
                                  || TryRequestFixedProfile
                                  || ExportNonce != _lastHandledExportNonce;
        base.OnRecalculate();
    }

    protected override void OnCalculate(int bar, decimal value)
    {
        if (_profile is null) return;
        if (_exportInFlight) return;

        // Checkbox / nonce / Apply-recalculate all trigger a new run. No _probeRan gate.
        var wantExport = ExportHistoricalCapability || ExportNonce != _lastHandledExportNonce;
        if (_pendingFromRecalculate)
        {
            _pendingFromRecalculate = false;
            wantExport = ExportHistoricalCapability || ExportNonce != _lastHandledExportNonce;
        }

        // Auto-export when bar count jumps significantly (history reload).
        // seed value, subject to sensitivity test — 500 bars delta
        const int significantBarDelta = 500;
        if (!wantExport && CurrentBar >= 0 && _lastAutoBars >= 0
            && Math.Abs(CurrentBar - _lastAutoBars) >= significantBarDelta)
        {
            ExportNonce = _lastHandledExportNonce + 1;
            wantExport = true;
            WriteLog($"Auto-export: CurrentBar delta {_lastAutoBars}→{CurrentBar} nonce={ExportNonce}");
        }

        if (wantExport)
        {
            if (CurrentBar < 0)
            {
                Status = $"{ShellBuild} {Stamp()} waiting for bars…";
                return;
            }

            // Clear checkbox after arming (one-shot UX) — nonce remains so next increment re-runs.
            ExportHistoricalCapability = false;
            _ = RunExportPipelineAsync(includeFixedProfile: IncludeFixedProfileRequest);
        }
        else if (TryRequestFixedProfile)
        {
            TryRequestFixedProfile = false;
            _ = AttemptFixedProfileOnlyAsync();
        }
    }

    [Obsolete("ATAS marks this obsolete — prefer RequestFixedProfileAsync.")]
    [SuppressMessage("Design", "CS0672")]
    protected override void OnFixedProfilesResponse(IndicatorCandle candle, FixedProfilePeriods period)
    {
        WriteFixedProfileSample(candle, period, "legacy-OnFixedProfilesResponse");
    }

    [Obsolete("ATAS marks this obsolete — prefer RequestFixedProfileAsync.")]
    [SuppressMessage("Design", "CS0672")]
    protected override void OnFixedProfilesResponse(
        IndicatorCandle candle, IndicatorCandle candle2, FixedProfilePeriods period)
    {
        WriteFixedProfileSample(candle, period, "legacy-OnFixedProfilesResponse-both");
        WriteLog($"both-candles t2={candle2?.Time:o}");
    }

    private async Task RunExportPipelineAsync(bool includeFixedProfile)
    {
        if (_exportInFlight) return;
        _exportInFlight = true;
        var handledNonce = ExportNonce;
        try
        {
            Status = $"{ShellBuild} {Stamp()} export start nonce={handledNonce}…";
            WriteLog(Status);

            FixedProfileRuntimeSection? fp = null;
            if (includeFixedProfile)
                fp = await RequestFixedProfileRuntimeAsync().ConfigureAwait(true);

            var path = RunProbeAndWrite(fp);
            _lastHandledExportNonce = handledNonce;
            _lastAutoBars = CurrentBar;
            Status =
                $"{ShellBuild} {Stamp()} OK wrote {path} nonce={handledNonce} " +
                $"fp={fp?.Outcome ?? "SKIPPED"} bars={CurrentBar}";
            WriteLog(Status);
        }
        catch (Exception ex)
        {
            Status = $"{ShellBuild} {Stamp()} FAIL: {ex.GetType().Name}: {ex.Message}";
            WriteLog(Status + Environment.NewLine + ex);
            // Do NOT advance _lastHandledExportNonce — user can re-Apply same nonce or tick checkbox.
        }
        finally
        {
            _exportInFlight = false;
        }
    }

    private async Task AttemptFixedProfileOnlyAsync()
    {
        if (_exportInFlight) return;
        _exportInFlight = true;
        try
        {
            var fp = await RequestFixedProfileRuntimeAsync().ConfigureAwait(true);
            var dir = AosDir();
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "FixedProfileResponse_last.json");
            File.WriteAllText(path, JsonSerializer.Serialize(fp, new JsonSerializerOptions { WriteIndented = true }));
            Status = $"{ShellBuild} {Stamp()} FixedProfile-only {fp.Outcome} → {path}";
            WriteLog(Status);
        }
        catch (Exception ex)
        {
            Status = $"{ShellBuild} {Stamp()} FixedProfile-only FAIL: {ex.GetType().Name}: {ex.Message}";
            WriteLog(Status);
        }
        finally
        {
            _exportInFlight = false;
        }
    }

    private async Task<FixedProfileRuntimeSection> RequestFixedProfileRuntimeAsync()
    {
        var section = new FixedProfileRuntimeSection
        {
            Attempted = true,
            Period = FixedProfilePeriods.LastDay.ToString()
        };
        var sw = Stopwatch.StartNew();
        try
        {
            Status = $"{ShellBuild} {Stamp()} RequestFixedProfileAsync(LastDay)…";
            WriteLog(Status);
            var req = new FixedProfileRequest(FixedProfilePeriods.LastDay);
            var task = RequestFixedProfileAsync(req);
            var completed = await Task.WhenAny(task, Task.Delay(FixedProfileTimeoutMs)).ConfigureAwait(true);
            sw.Stop();
            section.LatencyMs = sw.ElapsedMilliseconds;

            if (completed != task)
            {
                section.Outcome = "TIMEOUT";
                section.ResponseReturned = false;
                section.FailureReason = $"No response within {FixedProfileTimeoutMs} ms (seed value, subject to sensitivity test).";
                return section;
            }

            FixedProfileResponse? response;
            try { response = await task.ConfigureAwait(true); }
            catch (Exception ex)
            {
                section.Outcome = "EXCEPTION";
                section.ResponseReturned = false;
                section.FailureReason = $"{ex.GetType().Name}: {ex.Message}";
                return section;
            }

            section.ResponseReturned = true;
            section.ResponseNull = response is null;
            if (response is null)
            {
                section.Outcome = "NULL_RESPONSE";
                section.FailureReason = "RequestFixedProfileAsync completed with null FixedProfileResponse.";
                return section;
            }

            // FixedProfileResponse is a struct — unwrap Nullable before members.
            var fp = response.Value;
            section.Scaled = SampleCandle(fp.Scaled);
            section.Original = SampleCandle(fp.Original);
            PickVahValPoc(section);
            section.Outcome = "OK";
            return section;
        }
        catch (Exception ex)
        {
            sw.Stop();
            section.LatencyMs = sw.ElapsedMilliseconds;
            section.Outcome = "EXCEPTION";
            section.FailureReason = $"{ex.GetType().Name}: {ex.Message}";
            return section;
        }
    }

    private static FixedProfileCandleSample? SampleCandle(IndicatorCandle? candle)
    {
        if (candle is null) return null;
        int? levels = null;
        try
        {
            var list = candle.GetAllPriceLevels();
            levels = list?.Count();
        }
        catch
        {
            levels = null;
        }

        decimal? vah = null, val = null, poc = null;
        try
        {
            var va = candle.ValueArea;
            if (va is not null)
            {
                vah = va.ValueAreaHigh;
                val = va.ValueAreaLow;
            }
        }
        catch { /* ignore */ }

        try
        {
            var mv = candle.MaxVolumePriceInfo;
            if (mv is not null)
                poc = mv.Price;
        }
        catch { /* ignore */ }

        return new FixedProfileCandleSample
        {
            TimeIso = candle.Time.ToString("o", CultureInfo.InvariantCulture),
            TimeKind = candle.Time.Kind.ToString(),
            TimeUtc = DateTime.SpecifyKind(candle.Time, DateTimeKind.Utc),
            High = candle.High,
            Low = candle.Low,
            Volume = candle.Volume,
            PriceLevelCount = levels,
            ValueAreaHigh = vah,
            ValueAreaLow = val,
            MaxVolumePrice = poc
        };
    }

    private static void PickVahValPoc(FixedProfileRuntimeSection section)
    {
        var prefer = section.Scaled ?? section.Original;
        section.ComputedVah = prefer?.ValueAreaHigh;
        section.ComputedVal = prefer?.ValueAreaLow;
        section.ComputedPoc = prefer?.MaxVolumePrice;
        section.VahValPocComputable =
            section.ComputedVah is not null
            && section.ComputedVal is not null
            && section.ComputedPoc is not null;
    }

    private string RunProbeAndWrite(FixedProfileRuntimeSection? fixedProfile)
    {
        int? totalBars = null;
        try
        {
            var prop = typeof(Indicator).GetProperty("TotalBars",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.FlattenHierarchy);
            if (prop is not null)
                totalBars = (int?)prop.GetValue(this);
        }
        catch (Exception ex)
        {
            WriteLog("TotalBars reflection: " + ex.Message);
        }

        string? chartTf = null;
        string? chartType = null;
        try
        {
            chartTf = ChartInfo?.TimeFrame?.ToString();
            chartType = ChartInfo?.ChartType.ToString();
        }
        catch
        {
#pragma warning disable CS0618
            chartTf = TimeFrame?.ToString();
            chartType = ChartType.ToString();
#pragma warning restore CS0618
        }

        var host = new HistoricalProbeHost
        {
            CurrentBar = CurrentBar,
            TotalBars = totalBars,
            GetCandle = i =>
            {
                try { return GetCandle(i); }
                catch { return null; }
            },
            InstrumentInfoInstrument = InstrumentInfo?.Instrument,
            IndicatorInstrument = InstrumentInfo?.Instrument,
            Exchange = InstrumentInfo?.Exchange,
            TickSize = InstrumentInfo?.TickSize,
            ChartTimeFrame = chartTf,
            ChartType = chartType,
            ContractCandidates = CollectContractCandidates(),
            FixedProfileRuntime = fixedProfile,
            DeclaredMode = Aos.LevelEngine.Identity.DeclaredDataSourceMode.LIVE,
            SecurityCode = TrySecurity(s => s.Code),
            SecurityId = TrySecurity(s => s.SecurityId),
            SecurityExpiration = TrySecurityExpiration()
        };

        var report = HistoricalCapabilityProbe.Run(host, _profile!);
        if (totalBars is null)
            report.Notes.Add("TotalBars not accessible as public API on Indicator — reported via reflection if found; else null.");

        try
        {
            return HistoricalCapabilityProbe.WriteReport(report);
        }
        catch (Exception ex)
        {
            // Surface lock/permission errors into Status (caller sets Status on catch too).
            throw new IOException($"Write HistoricalCapability.json failed: {ex.Message}", ex);
        }
    }

    private string? TrySecurity(Func<Security, string?> pick)
    {
        try
        {
            var sec = TradingManager?.Security;
            return sec is null ? null : pick(sec);
        }
        catch { return null; }
    }

    private DateTime? TrySecurityExpiration()
    {
        try
        {
            var sec = TradingManager?.Security;
            return sec?.Expiration;
        }
        catch { return null; }
    }

    private List<ContractCodeCandidate> CollectContractCandidates()
    {
        var list = new List<ContractCodeCandidate>();

        void Add(string source, string property, object? value, string evidence)
        {
            list.Add(new ContractCodeCandidate
            {
                Source = source,
                Property = property,
                Value = value?.ToString(),
                Evidence = evidence
            });
        }

        // IInstrumentInfo — only these members verified on interface.
        try
        {
            Add("InstrumentInfo", "Instrument", InstrumentInfo?.Instrument,
                "ATAS.Indicators.IInstrumentInfo.get_Instrument");
            Add("InstrumentInfo", "Exchange", InstrumentInfo?.Exchange,
                "ATAS.Indicators.IInstrumentInfo.get_Exchange");
            Add("InstrumentInfo", "TickSize", InstrumentInfo?.TickSize,
                "ATAS.Indicators.IInstrumentInfo.get_TickSize");
            Add("InstrumentInfo", "TimeZone", InstrumentInfo?.TimeZone,
                "ATAS.Indicators.IInstrumentInfo.get_TimeZone");
        }
        catch (Exception ex)
        {
            Add("InstrumentInfo", "(error)", ex.Message, "runtime");
        }

        // TradingManager.Security — full contract candidates (Code / Expiration / SecurityId).
        try
        {
            Security? sec = null;
            try { sec = TradingManager?.Security; }
            catch { /* property may throw if TM null */ }

            if (sec is null)
            {
                Add("TradingManager.Security", "(null)", null,
                    "ITradingManager.get_Security — null at probe time");
            }
            else
            {
                Add("TradingManager.Security", "Code", sec.Code,
                    "ATAS.DataFeedsCore.Security.Code — docs: Id may be CLK4 / ESU6 style");
                Add("TradingManager.Security", "Instrument", sec.Instrument,
                    "ATAS.DataFeedsCore.Security.Instrument");
                Add("TradingManager.Security", "SecurityId", sec.SecurityId,
                    "ATAS.DataFeedsCore.Security.SecurityId — e.g. CODE@Exchange");
                Add("TradingManager.Security", "Exchange", sec.Exchange,
                    "ATAS.DataFeedsCore.Security.Exchange");
                Add("TradingManager.Security", "Expiration", sec.Expiration.ToString("o", CultureInfo.InvariantCulture),
                    "ATAS.DataFeedsCore.Security.Expiration (DateTime) — not ExpirationDate");
                Add("TradingManager.Security", "UnderlyingSecurity.Code", sec.UnderlyingSecurity?.Code,
                    "ATAS.DataFeedsCore.Security.UnderlyingSecurity");
                Add("TradingManager.Security", "ToString", sec.ToString(),
                    "Security.ToString() — instrument or code+exchange");
            }
        }
        catch (Exception ex)
        {
            Add("TradingManager.Security", "(error)", ex.Message, "runtime");
        }

        // IChart / ChartInfo — timeframe only; no contract code on chart API.
        try
        {
            Add("ChartInfo", "TimeFrame", ChartInfo?.TimeFrame,
                "ATAS.Indicators.IChart.get_TimeFrame");
            Add("ChartInfo", "ChartType", ChartInfo?.ChartType,
                "ATAS.Indicators.IChart.get_ChartType");
        }
        catch (Exception ex)
        {
            Add("ChartInfo", "(error)", ex.Message, "runtime");
        }

        // Explicitly: ExpirationDate / ContractCode not found on InstrumentInfo or IChart.
        Add("NOT_FOUND", "InstrumentInfo.ExpirationDate", null,
            "No ExpirationDate on IInstrumentInfo in ATAS.Indicators.dll");
        Add("NOT_FOUND", "InstrumentInfo.ContractCode", null,
            "No ContractCode on IInstrumentInfo in ATAS.Indicators.dll");
        Add("NOT_FOUND", "ChartInfo.ContractCode", null,
            "No ContractCode on IChart in ATAS.Indicators.dll");

        return list;
    }

    private void WriteFixedProfileSample(IndicatorCandle? candle, FixedProfilePeriods period, string source)
    {
        try
        {
            var dir = AosDir();
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "FixedProfileResponse_last.json");
            File.WriteAllText(path, JsonSerializer.Serialize(new
            {
                receivedUtc = DateTime.UtcNow.ToString("O"),
                source,
                period = period.ToString(),
                candleTime = candle?.Time.ToString("o"),
                candleTimeKind = candle?.Time.Kind.ToString(),
                high = candle?.High,
                low = candle?.Low,
                volume = candle?.Volume
            }, new JsonSerializerOptions { WriteIndented = true }));
            Status = $"{ShellBuild} {Stamp()} {source}({period}) → {path}";
            WriteLog(Status);
        }
        catch (Exception ex)
        {
            Status = $"{ShellBuild} {Stamp()} FixedProfile sample write FAIL: {ex.Message}";
            WriteLog(Status);
        }
    }

    private static string Stamp() => DateTime.UtcNow.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "Z";

    private static string AosDir() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aos");

    private static void WriteLog(string line)
    {
        try
        {
            var dir = AosDir();
            Directory.CreateDirectory(dir);
            File.AppendAllText(
                Path.Combine(dir, "level_engine_atas.log"),
                DateTime.UtcNow.ToString("O") + " " + line + Environment.NewLine);
        }
        catch
        {
            // ignore log I/O
        }
    }
}
