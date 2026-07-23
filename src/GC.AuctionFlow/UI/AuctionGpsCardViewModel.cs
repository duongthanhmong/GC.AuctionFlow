using System.Globalization;
using GC.AuctionFlow.Core;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Runtime;

namespace GC.AuctionFlow.UI;

/// <summary>Presentation model for Auction GPS Card. Unit-testable without ATAS rendering.</summary>
public sealed class AuctionGpsCardViewModel
{
    public AuctionGpsCardViewModel(
        string title,
        string dataLine,
        string reasonLine,
        string contractLine,
        string exchangeLine,
        string expiryLine,
        string tickSizeLine,
        string modeLine,
        string providerLine,
        string tradesLine,
        string bidAskLine,
        string profileLine,
        IReadOnlyList<string> profileDetailLines,
        string rollLine,
        string recorderLine,
        string mboLine,
        IReadOnlyList<string> diagnosticRows,
        DataState dataState,
        long snapshotPublicationSequence)
    {
        Title = title;
        DataLine = dataLine;
        ReasonLine = reasonLine;
        ContractLine = contractLine;
        ExchangeLine = exchangeLine;
        ExpiryLine = expiryLine;
        TickSizeLine = tickSizeLine;
        ModeLine = modeLine;
        ProviderLine = providerLine;
        TradesLine = tradesLine;
        BidAskLine = bidAskLine;
        ProfileLine = profileLine;
        ProfileDetailLines = profileDetailLines ?? Array.Empty<string>();
        RollLine = rollLine;
        RecorderLine = recorderLine;
        MboLine = mboLine;
        DiagnosticRows = diagnosticRows ?? Array.Empty<string>();
        DataState = dataState;
        SnapshotPublicationSequence = snapshotPublicationSequence;
    }

    public string Title { get; }
    public string DataLine { get; }
    public string ReasonLine { get; }
    public string ContractLine { get; }
    public string ExchangeLine { get; }
    public string ExpiryLine { get; }
    public string TickSizeLine { get; }
    public string ModeLine { get; }
    public string ProviderLine { get; }
    public string TradesLine { get; }
    public string BidAskLine { get; }
    public string ProfileLine { get; }
    public IReadOnlyList<string> ProfileDetailLines { get; }
    public string RollLine { get; }
    public string RecorderLine { get; }
    public string MboLine { get; }
    public IReadOnlyList<string> DiagnosticRows { get; }
    public DataState DataState { get; }
    public long SnapshotPublicationSequence { get; }

    public IReadOnlyList<string> AllLines(bool includeDiagnostics)
    {
        var lines = new List<string>
        {
            Title,
            DataLine,
            ReasonLine,
            ContractLine,
            ExchangeLine,
            ExpiryLine,
            TickSizeLine,
            ModeLine,
            ProviderLine,
            TradesLine,
            BidAskLine,
            ProfileLine
        };
        lines.AddRange(ProfileDetailLines);
        lines.Add(RollLine);
        lines.Add(RecorderLine);
        lines.Add(MboLine);
        if (includeDiagnostics)
            lines.AddRange(DiagnosticRows);
        return lines;
    }
}

public static class AuctionGpsCardMapper
{
    public static AuctionGpsCardViewModel FromSnapshot(GcaeRuntimeSnapshot snapshot, bool showDiagnostics)
    {
        if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));

        var c = snapshot.Contract;
        var g = snapshot.DataGate;
        var cap = snapshot.Capability;
        var cur = snapshot.Profiles?.CurrentAuction;
        var prev = snapshot.Profiles?.PreviousAuction;

        var contractCode = !string.IsNullOrWhiteSpace(c.SecurityCode)
            ? c.SecurityCode!
            : (string.IsNullOrWhiteSpace(c.IdentityKey) || c.IdentityKey == "Unknown" ? "UNKNOWN" : c.IdentityKey);

        var expiry = c.ContractExpiration is DateTime dt
            ? dt.ToString("yyyy-MM-dd")
            : "UNKNOWN";

        var tick = c.TickSize is decimal t ? t.ToString(CultureInfo.InvariantCulture) : "UNKNOWN";

        var trades = cap.LastTradeCallbackUtc is not null || cap.TradeStreamState == RuntimeCapabilityState.Available
            ? "OBSERVED"
            : "NOT OBSERVED";

        var bidAsk = cap.BidAskClassificationState switch
        {
            RuntimeCapabilityState.Available or RuntimeCapabilityState.Ready => "AVAILABLE",
            RuntimeCapabilityState.Partial => "PARTIAL",
            _ => "UNKNOWN"
        };

        var roll = c.RollState switch
        {
            RollState.NormalContract => "NORMAL",
            RollState.EarlyRoll => "EARLY",
            RollState.ActiveRoll => "ACTIVE",
            RollState.PostRoll => "POST",
            _ => "UNKNOWN"
        };

        var recorder = cap.RecorderState switch
        {
            RuntimeCapabilityState.Off => "OFF",
            RuntimeCapabilityState.Ready => "READY",
            RuntimeCapabilityState.Recording => "RECORDING",
            RuntimeCapabilityState.Faulted => "FAULTED",
            RuntimeCapabilityState.NotConfigured => "NOT CONFIGURED",
            _ => cap.RecorderState.ToString().ToUpperInvariant()
        };

        var enableTpoParityDiagnostics = snapshot.EnableTpoParityDiagnostics;
        var (profileLine, profileDetails) = BuildProfileLines(
            cur, prev, showDiagnostics, snapshot.Profiles?.LatestTimestampDiagnostic, enableTpoParityDiagnostics);

        var diagnostics = showDiagnostics
            ? new List<string>
            {
                "STRUCTURAL CONTEXT: NOT AVAILABLE",
                "TACTICAL CONTEXT: NOT AVAILABLE",
                "LOCATION: NOT AVAILABLE",
                "EPISODE: NOT AVAILABLE",
                "THESIS: NOT AVAILABLE"
            }
            : new List<string>();

        var reason = string.IsNullOrWhiteSpace(g.PrimaryReasonCode)
            ? "REASON: —"
            : "REASON: " + HumanizeReason(g.PrimaryReasonCode);

        return new AuctionGpsCardViewModel(
            title: "GC AUCTIONFLOW ENGINE",
            dataLine: "DATA: " + g.DataState.ToString().ToUpperInvariant(),
            reasonLine: reason,
            contractLine: "CONTRACT: " + contractCode.ToUpperInvariant(),
            exchangeLine: "EXCHANGE: " + (string.IsNullOrWhiteSpace(c.Exchange) ? "UNKNOWN" : c.Exchange!.ToUpperInvariant()),
            expiryLine: "EXPIRY: " + expiry,
            tickSizeLine: "TICK SIZE: " + tick,
            modeLine: "MODE: " + cap.DataSourceMode.ToString().ToUpperInvariant(),
            providerLine: "PROVIDER: " + cap.FeedProvider.ToString().ToUpperInvariant(),
            tradesLine: "TRADES: " + trades,
            bidAskLine: "BID/ASK: " + bidAsk,
            profileLine: profileLine,
            profileDetailLines: profileDetails,
            rollLine: "ROLL: " + roll,
            recorderLine: "RECORDER: " + recorder,
            mboLine: "MBO: BLOCKED",
            diagnosticRows: diagnostics,
            dataState: g.DataState,
            snapshotPublicationSequence: snapshot.PublicationSequence);
    }

    private static (string Line, IReadOnlyList<string> Details) BuildProfileLines(
        PrimaryAuctionProfileSnapshot? cur,
        PrimaryAuctionProfileSnapshot? prev,
        bool showDiagnostics,
        ProfileTimestampDiagnostic? tsDiag,
        bool enableTpoParityDiagnostics)
    {
        if (cur is null || cur.ProfileState == AuctionProfileState.NotReady)
        {
            if (showDiagnostics && tsDiag is not null)
                return ("PROFILE: NOT READY", tsDiag.ToGpsDiagnosticRows());
            return ("PROFILE: NOT READY", Array.Empty<string>());
        }

        var details = new List<string>();
        if (cur.ProfileState == AuctionProfileState.Ready)
        {
            details.Add("CURRENT AUCTION: " + cur.AuctionId);
            details.Add("TPO POC: " + Fmt(cur.TpoProfile?.TpoPoc));
            details.Add("VPOC: " + Fmt(cur.VolumeProfile?.VolumePoc));
            details.Add("TPO VALUE: VAL " + Fmt(cur.TpoProfile?.TpoVal) + " / VAH " + Fmt(cur.TpoProfile?.TpoVah));
            details.Add("VOLUME VALUE: VAL " + Fmt(cur.VolumeProfile?.VolumeVal) + " / VAH " + Fmt(cur.VolumeProfile?.VolumeVah));
            details.Add("AUCTION RANGE: " + Fmt(cur.ProfileLow) + "-" + Fmt(cur.ProfileHigh));
            details.Add("PREVIOUS TPO POC: " + Fmt(prev?.TpoProfile?.TpoPoc));
            details.Add("PREVIOUS VPOC: " + Fmt(prev?.VolumeProfile?.VolumePoc));
            AddDiag(details, cur, showDiagnostics, tsDiag, enableTpoParityDiagnostics);
            return ("PROFILE: READY", details);
        }

        if (cur.ProfileState == AuctionProfileState.Partial)
        {
            details.Add("CURRENT AUCTION: " + cur.AuctionId);
            details.Add("TPO POC: " + Fmt(cur.TpoProfile?.TpoPoc));
            details.Add("VPOC: UNAVAILABLE");
            details.Add("PROFILE LIMITATION: PRICE VOLUME DATA UNAVAILABLE");
            details.Add("TPO VALUE: VAL " + Fmt(cur.TpoProfile?.TpoVal) + " / VAH " + Fmt(cur.TpoProfile?.TpoVah));
            details.Add("AUCTION RANGE: " + Fmt(cur.ProfileLow) + "-" + Fmt(cur.ProfileHigh));
            AddDiag(details, cur, showDiagnostics, tsDiag, enableTpoParityDiagnostics);
            return ("PROFILE: PARTIAL", details);
        }

        return ("PROFILE: INVALID", Array.Empty<string>());
    }

    private static void AddDiag(
        List<string> details,
        PrimaryAuctionProfileSnapshot cur,
        bool showDiagnostics,
        ProfileTimestampDiagnostic? tsDiag,
        bool enableTpoParityDiagnostics)
    {
        // Bounded TPO parity GPS rows — independent of structural GPS diagnostics.
        if (enableTpoParityDiagnostics && cur.TpoProfile?.ParityDiagnostic is { } parity)
            details.AddRange(parity.ToGpsDiagnosticRows());

        if (!showDiagnostics) return;

        if (tsDiag is not null)
            details.AddRange(tsDiag.ToGpsDiagnosticRows());

        if (cur.TpoProfile is null) return;
        var t = cur.TpoProfile;
        details.Add("ANCHOR TZ: " + t.AnchorTimezone);
        details.Add("ANCHOR LOCAL: " + t.AnchorLocalTime.ToString(@"hh\:mm"));
        details.Add("TPO PERIOD MIN: " + t.PeriodMinutes);
        details.Add("COMPLETED PERIODS: " + t.CompletedPeriodCount);
        details.Add("DEVELOPING PERIOD: " + (t.DevelopingPeriodIndex?.ToString() ?? "—"));
        details.Add("TOTAL TPOS: " + t.TotalTpoCount);
        details.Add("TOTAL EXECUTED VOLUME: " + Fmt(cur.VolumeProfile?.TotalExecutedVolume));
        details.Add("PROFILE SNAPSHOT: " + cur.Version);
    }

    private static string Fmt(decimal? v) =>
        v is null ? "—" : v.Value.ToString("0.0", CultureInfo.InvariantCulture);

    private static string HumanizeReason(string code) => code switch
    {
        DataGateReasonCodes.ProfileNotReady => "PROFILE NOT READY",
        DataGateReasonCodes.ProfilePartial => "PROFILE PARTIAL",
        DataGateReasonCodes.ProfileInvalid => "PROFILE INVALID",
        DataGateReasonCodes.InstrumentMismatch => "INSTRUMENT MISMATCH",
        DataGateReasonCodes.InstrumentUnknown => "INSTRUMENT UNKNOWN",
        DataGateReasonCodes.TickSizeInvalid => "TICK SIZE INVALID",
        DataGateReasonCodes.TickSizeMismatch => "TICK SIZE MISMATCH",
        DataGateReasonCodes.ContractExpired => "CONTRACT EXPIRED",
        DataGateReasonCodes.ProviderModeConflict => "PROVIDER/MODE CONFLICT",
        DataGateReasonCodes.IndicatorDisposed => "INDICATOR DISPOSED",
        DataGateReasonCodes.IdentityCorruption => "IDENTITY CORRUPTION",
        DataGateReasonCodes.BidAskUnknownOrPartial => "BID/ASK UNKNOWN OR PARTIAL",
        DataGateReasonCodes.RollStateUnknown => "ROLL STATE UNKNOWN",
        DataGateReasonCodes.TradeNotObserved => "TRADE NOT OBSERVED",
        DataGateReasonCodes.SourceProvenanceIncomplete => "SOURCE PROVENANCE INCOMPLETE",
        _ => code.Replace('_', ' ')
    };
}
