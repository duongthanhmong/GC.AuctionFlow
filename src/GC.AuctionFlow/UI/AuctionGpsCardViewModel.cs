using System.Globalization;
using GC.AuctionFlow.Cluster;
using GC.AuctionFlow.Composite;
using GC.AuctionFlow.Core;
using GC.AuctionFlow.Directional;
using GC.AuctionFlow.EffortResult;
using GC.AuctionFlow.Efficiency;
using GC.AuctionFlow.Facilitation;
using GC.AuctionFlow.Imbalance;
using GC.AuctionFlow.Maturity;
using GC.AuctionFlow.Memory;
using GC.AuctionFlow.Plar;
using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Evidence;
using GC.AuctionFlow.Orderflow;
using GC.AuctionFlow.Participation;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Reference;
using GC.AuctionFlow.Resolution;
using GC.AuctionFlow.Runtime;
using GC.AuctionFlow.Thesis;

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

        var details = profileDetails.ToList();
        details.AddRange(BuildCompositeLines(snapshot.Composite, snapshot.ShowCompositeDiagnostics));
        details.AddRange(BuildStructuralReferenceLines(
            snapshot.StructuralReferences,
            snapshot.ShowStructuralReferenceDiagnostics));
        details.AddRange(BuildDirectionalContextLines(
            snapshot.DirectionalContext,
            snapshot.ShowDirectionalContextDiagnostics));
        details.AddRange(BuildAuctionEpisodeLines(
            snapshot.AuctionEpisodes,
            snapshot.ShowAuctionEpisodeDiagnostics));
        details.AddRange(BuildAcceptanceReentryEvidenceLines(
            snapshot.AcceptanceReentryEvidence,
            snapshot.ShowAcceptanceReentryEvidenceDiagnostics));
        details.AddRange(BuildExecutedOrderflowLines(
            snapshot.ExecutedOrderflow,
            snapshot.ShowExecutedOrderflowDiagnostics));
        details.AddRange(BuildClusterRawLines(
            snapshot.ClusterRaw,
            snapshot.ShowClusterRawDiagnostics));
        details.AddRange(BuildAuctionEfficiencyLines(
            snapshot.AuctionEfficiency,
            snapshot.ShowAuctionEfficiencyDiagnostics));
        details.AddRange(BuildAuctionResolutionLines(
            snapshot.AuctionResolution,
            snapshot.ShowAuctionResolutionDiagnostics));
        details.AddRange(BuildEffortResultLines(
            snapshot.EffortResult,
            snapshot.ShowEffortResultDiagnostics));
        details.AddRange(BuildFarThesisLines(
            snapshot.FarThesis,
            snapshot.ShowFarThesisDiagnostics));
        details.AddRange(BuildAacThesisLines(
            snapshot.AacThesis,
            snapshot.ShowAacThesisDiagnostics));
        details.AddRange(BuildParticipationLines(snapshot.Participation));
        details.AddRange(BuildTradeFacilitationLines(snapshot.TradeFacilitation, false));
        details.AddRange(BuildSignalMaturityLines(snapshot.SignalMaturity, snapshot.ShowSignalMaturityDiagnostics));
        details.AddRange(BuildThesisContractLines(snapshot.ThesisContract, snapshot.ShowThesisContractDiagnostics));
        details.AddRange(BuildPlarLines(snapshot.Plar, snapshot.ShowPlarDiagnostics));
        details.AddRange(BuildPriceMemoryLines(snapshot.PriceMemory, snapshot.ShowPriceMemoryDiagnostics));
        details.AddRange(BuildImbalanceLines(snapshot.Imbalance, snapshot.ShowImbalanceDiagnostics));

        var diagnostics = showDiagnostics
            ? new List<string>
            {
                snapshot.DirectionalContext is null
                    ? "TACTICAL CONTEXT: NOT AVAILABLE"
                    : "TACTICAL CONTEXT: " + FormatDirectionalState(snapshot.DirectionalContext.TacticalContext.State)
                      + " (DEVELOPING)",
                snapshot.DirectionalContext is null
                    ? "LOCATION: NOT AVAILABLE"
                    : "LOCATION: " + snapshot.DirectionalContext.PriceLocation.CurrentPrimaryTpo,
                snapshot.AuctionEpisodes is null
                    ? "EPISODE: NOT AVAILABLE"
                    : "EPISODE: " + snapshot.AuctionEpisodes.ModuleState.ToString().ToUpperInvariant(),
                snapshot.AcceptanceReentryEvidence is null
                    ? "ACCEPTANCE/REENTRY EVIDENCE: NOT AVAILABLE"
                    : "ACCEPTANCE/REENTRY EVIDENCE: " + snapshot.AcceptanceReentryEvidence.ModuleState.ToString().ToUpperInvariant(),
                snapshot.ExecutedOrderflow is null
                    ? "ORDERFLOW: NOT AVAILABLE"
                    : "ORDERFLOW: " + snapshot.ExecutedOrderflow.ModuleState.ToString().ToUpperInvariant(),
                snapshot.ClusterRaw is null
                    ? "CLUSTER RAW: NOT AVAILABLE"
                    : "CLUSTER RAW: " + snapshot.ClusterRaw.ModuleState.ToString().ToUpperInvariant(),
                snapshot.AuctionEfficiency is null
                    ? "AUCTION EFFICIENCY: NOT AVAILABLE"
                    : "AUCTION EFFICIENCY: " + snapshot.AuctionEfficiency.ModuleState.ToString().ToUpperInvariant(),
                snapshot.AuctionResolution is null
                    ? "RESOLUTION: NOT AVAILABLE"
                    : "RESOLUTION: " + snapshot.AuctionResolution.ModuleState.ToString().ToUpperInvariant(),
                snapshot.EffortResult is null
                    ? "EFFORT RESULT: NOT AVAILABLE"
                    : "EFFORT RESULT: " + snapshot.EffortResult.ModuleState.ToString().ToUpperInvariant(),
                snapshot.FarThesis is null
                    ? "FAR: NOT AVAILABLE"
                    : "FAR: " + snapshot.FarThesis.ModuleState.ToString().ToUpperInvariant(),
                snapshot.AacThesis is null
                    ? "AAC: NOT AVAILABLE"
                    : "AAC: " + snapshot.AacThesis.ModuleState.ToString().ToUpperInvariant(),
                snapshot.TradeFacilitation is null
                    ? "TRADE FACILITATION: NOT AVAILABLE"
                    : "TRADE FACILITATION: " + snapshot.TradeFacilitation.ModuleState.ToString().ToUpperInvariant(),
                snapshot.SignalMaturity is null
                    ? "MATURITY: NOT AVAILABLE"
                    : "MATURITY: " + snapshot.SignalMaturity.ModuleState.ToString().ToUpperInvariant(),
                snapshot.ThesisContract is null
                    ? "CONTRACT: NOT AVAILABLE"
                    : "CONTRACT: " + snapshot.ThesisContract.ModuleState.ToString().ToUpperInvariant(),
                snapshot.Plar is null
                    ? "PATH: NOT AVAILABLE"
                    : "PATH: " + snapshot.Plar.ModuleState.ToString().ToUpperInvariant(),
                snapshot.PriceMemory is null
                    ? "MEMORY: NOT AVAILABLE"
                    : "MEMORY: " + snapshot.PriceMemory.ModuleState.ToString().ToUpperInvariant(),
                snapshot.Imbalance is null
                    ? "IMBALANCE: NOT AVAILABLE"
                    : "IMBALANCE: " + snapshot.Imbalance.ModuleState.ToString().ToUpperInvariant()
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
            profileDetailLines: details,
            rollLine: "ROLL: " + roll,
            recorderLine: "RECORDER: " + recorder,
            mboLine: "MBO: BLOCKED",
            diagnosticRows: diagnostics,
            dataState: g.DataState,
            snapshotPublicationSequence: snapshot.PublicationSequence);
    }

    public static IReadOnlyList<string> BuildCompositeLines(CompositeSetSnapshot? set, bool showDiagnostics)
    {
        if (set is null)
            return Array.Empty<string>();

        var conf = set.Confirmed;
        var rows = new List<string>();

        switch (conf.CompositeStatus)
        {
            case CompositeStatus.Disabled:
                rows.Add("COMPOSITE: DISABLED");
                break;
            case CompositeStatus.AwaitingAnchor:
                rows.Add("COMPOSITE: AWAITING ANCHOR");
                rows.Add("COMPOSITE POLICY: OPERATOR ANCHORED");
                break;
            case CompositeStatus.Building:
                rows.Add("COMPOSITE: NOT READY");
                break;
            case CompositeStatus.Partial:
                rows.Add("COMPOSITE: PARTIAL");
                rows.Add("COMPOSITE ID: " + conf.CompositeId);
                rows.Add("COMPOSITE AUCTIONS: " + conf.CompletedContributionCount.ToString(CultureInfo.InvariantCulture));
                rows.Add("COMPOSITE RANGE: " + Fmt(conf.ProfileLow) + "-" + Fmt(conf.ProfileHigh));
                rows.Add("COMPOSITE TPO POC: " + Fmt(conf.TpoPoc));
                rows.Add("COMPOSITE VPOC: UNAVAILABLE");
                rows.Add("COMPOSITE TPO VALUE: VAL " + Fmt(conf.TpoVal) + " / VAH " + Fmt(conf.TpoVah));
                rows.Add("COMPOSITE LIMITATION: " + (conf.KnownLimitations.FirstOrDefault(l => l.Contains("VOLUME", StringComparison.Ordinal)) ?? "PARTIAL"));
                break;
            case CompositeStatus.Ready:
                rows.Add("COMPOSITE: READY");
                rows.Add("COMPOSITE ID: " + conf.CompositeId);
                rows.Add("COMPOSITE AUCTIONS: " + conf.CompletedContributionCount.ToString(CultureInfo.InvariantCulture));
                rows.Add("COMPOSITE RANGE: " + Fmt(conf.ProfileLow) + "-" + Fmt(conf.ProfileHigh));
                rows.Add("COMPOSITE TPO POC: " + Fmt(conf.TpoPoc));
                rows.Add("COMPOSITE VPOC: " + Fmt(conf.VolumePoc));
                rows.Add("COMPOSITE TPO VALUE: VAL " + Fmt(conf.TpoVal) + " / VAH " + Fmt(conf.TpoVah));
                rows.Add("COMPOSITE VOL VALUE: VAL " + Fmt(conf.VolumeVal) + " / VAH " + Fmt(conf.VolumeVah));
                break;
            case CompositeStatus.Invalid:
                rows.Add("COMPOSITE: INVALID");
                rows.Add("COMPOSITE LIMITATION: " + (conf.KnownLimitations.FirstOrDefault() ?? "INVALID"));
                break;
            default:
                rows.Add("COMPOSITE: " + conf.CompositeStatus.ToString().ToUpperInvariant());
                break;
        }

        if (conf.CompositeStatus is CompositeStatus.Ready or CompositeStatus.Partial)
        {
            rows.Add("COMPOSITE PREVIEW: " + (set.Preview is not null ? "ON" : "OFF"));
            rows.Add("COMPOSITE EVIDENCE: " + FormatEvidence(conf.EvidenceState));
        }

        if (showDiagnostics && conf.CompositeStatus is not CompositeStatus.Disabled)
        {
            rows.Add("COMPOSITE ANCHOR: " + (conf.AnchorAuctionId ?? "—"));
            rows.Add("COMPOSITE INCLUDED: " + (conf.IncludedAuctionIds.Count == 0 ? "—" : string.Join(",", conf.IncludedAuctionIds)));
            rows.Add("COMPOSITE EXCLUDED: " + (conf.ExcludedAuctionIds.Count == 0 ? "—" : string.Join(",", conf.ExcludedAuctionIds)));
            rows.Add("COMPOSITE POLICY VER: " + conf.PolicyVersion);
            rows.Add("COMPOSITE SNAPSHOT: " + conf.Version);
            if (set.MergeEvidence.Count > 0 && set.MergeEvidence[^1].TpoValueOverlapRatio is decimal ov)
                rows.Add("COMPOSITE LAST TPO OVERLAP: " + ov.ToString("0.000", CultureInfo.InvariantCulture));
        }

        return rows;
    }

    public static IReadOnlyList<string> BuildStructuralReferenceLines(
        StructuralReferenceSetSnapshot? set,
        bool showDiagnostics)
    {
        if (set is null || set.ModuleState == StructuralReferenceModuleState.Disabled)
            return Array.Empty<string>();

        var rows = new List<string>();
        switch (set.ModuleState)
        {
            case StructuralReferenceModuleState.AwaitingPrimary:
                rows.Add("REFERENCES: AWAITING PRIMARY");
                rows.Add("REFERENCE POLICY: " + set.PolicyVersion);
                break;
            case StructuralReferenceModuleState.Partial:
                rows.Add("REFERENCES: PARTIAL");
                rows.Add("REFERENCE POLICY: " + set.PolicyVersion);
                rows.Add("CONFIRMED REFERENCES: " + set.ConfirmedReferences.Count.ToString(CultureInfo.InvariantCulture));
                rows.Add("DEVELOPING REFERENCES: " + set.DevelopingReferences.Count.ToString(CultureInfo.InvariantCulture));
                rows.Add("CONFLUENCE GROUPS: " + set.ConfluenceGroups.Count.ToString(CultureInfo.InvariantCulture));
                break;
            case StructuralReferenceModuleState.Ready:
                rows.Add("REFERENCES: READY");
                rows.Add("REFERENCE POLICY: " + set.PolicyVersion);
                rows.Add("CONFIRMED REFERENCES: " + set.ConfirmedReferences.Count.ToString(CultureInfo.InvariantCulture));
                rows.Add("DEVELOPING REFERENCES: " + set.DevelopingReferences.Count.ToString(CultureInfo.InvariantCulture));
                rows.Add("CONFLUENCE GROUPS: " + set.ConfluenceGroups.Count.ToString(CultureInfo.InvariantCulture));
                break;
            case StructuralReferenceModuleState.Invalid:
                rows.Add("REFERENCES: INVALID");
                rows.Add("REFERENCE POLICY: " + set.PolicyVersion);
                rows.Add("REFERENCE LIMITATION: " + (set.KnownLimitations.FirstOrDefault() ?? "INVALID"));
                break;
            default:
                rows.Add("REFERENCES: " + set.ModuleState.ToString().ToUpperInvariant());
                break;
        }

        if (showDiagnostics
            && set.ModuleState is StructuralReferenceModuleState.Ready or StructuralReferenceModuleState.Partial)
        {
            var nearest = set.Nearest;
            if (nearest is not null)
            {
                if (nearest.NearestBelow.Count > 0)
                {
                    var b = nearest.NearestBelow[0];
                    rows.Add(
                        "REF NEAREST BELOW: " + b.ReferenceId + " | " + b.ReferenceType + " | " +
                        Fmt(b.ZoneLow) + " | " + (nearest.DistanceBelowTicks?.ToString(CultureInfo.InvariantCulture) ?? "—") + "t");
                }
                else
                {
                    rows.Add("REF NEAREST BELOW: —");
                }

                if (nearest.NearestAbove.Count > 0)
                {
                    var a = nearest.NearestAbove[0];
                    rows.Add(
                        "REF NEAREST ABOVE: " + a.ReferenceId + " | " + a.ReferenceType + " | " +
                        Fmt(a.ZoneLow) + " | " + (nearest.DistanceAboveTicks?.ToString(CultureInfo.InvariantCulture) ?? "—") + "t");
                }
                else
                {
                    rows.Add("REF NEAREST ABOVE: —");
                }

                if (nearest.ContainingExact.Count > 0)
                {
                    rows.Add(
                        "REF AT PRICE: " +
                        string.Join(",", nearest.ContainingExact.Select(x => x.ReferenceId)));
                }
            }

            if (set.UnavailableVolumeReasons.Count > 0)
                rows.Add("REF VOLUME UNAVAILABLE: " + string.Join(",", set.UnavailableVolumeReasons));
            rows.Add("REF REGISTRY REV: " + set.RegistryRevision.ToString(CultureInfo.InvariantCulture));
            rows.Add("REF INPUT: " + Truncate(set.InputFingerprint, 96));
        }

        return rows;
    }

    public static IReadOnlyList<string> BuildDirectionalContextLines(
        DirectionalContextSetSnapshot? set,
        bool showDiagnostics)
    {
        if (set is null || set.Status == DirectionalModuleState.Disabled)
            return Array.Empty<string>();

        var rows = new List<string>();
        switch (set.Status)
        {
            case DirectionalModuleState.AwaitingProfile:
                rows.Add("DIRECTIONAL CONTEXT: AWAITING PROFILE");
                rows.Add("DIRECTIONAL POLICY: " + set.PolicyVersion);
                break;
            case DirectionalModuleState.Partial:
                rows.Add("DIRECTIONAL CONTEXT: PARTIAL");
                rows.Add("DIRECTIONAL POLICY: " + set.PolicyVersion);
                AppendDirectionalCoreRows(rows, set);
                break;
            case DirectionalModuleState.Ready:
                rows.Add("DIRECTIONAL CONTEXT: READY");
                rows.Add("DIRECTIONAL POLICY: " + set.PolicyVersion);
                AppendDirectionalCoreRows(rows, set);
                break;
            case DirectionalModuleState.Invalid:
                rows.Add("DIRECTIONAL CONTEXT: INVALID");
                rows.Add("DIRECTIONAL POLICY: " + set.PolicyVersion);
                rows.Add("DIRECTIONAL LIMITATION: " + (set.Limitations.FirstOrDefault() ?? "INVALID"));
                break;
            default:
                rows.Add("DIRECTIONAL CONTEXT: " + set.Status.ToString().ToUpperInvariant());
                break;
        }

        if (showDiagnostics
            && set.Status is DirectionalModuleState.Ready or DirectionalModuleState.Partial)
        {
            var tac = set.TacticalContext;
            var pair = tac.PrimaryPairwise;
            rows.Add("DIR TRANSITIONS: " + set.StructuralContext.CompletedTransitionCount.ToString(CultureInfo.InvariantCulture));
            rows.Add("DIR AUCTIONS: " + (tac.SourceAuctionIds.Count == 0 ? "—" : string.Join(",", tac.SourceAuctionIds)));
            rows.Add("DIR TPO VALUE: " + tac.TpoValueMigration);
            rows.Add("DIR TPO POC: " + tac.TpoPocMigration);
            rows.Add("DIR VPOC: " + (pair?.ExactVolumeAvailable == true
                ? tac.VolumePocMigration.ToString()
                : "UNAVAILABLE"));
            rows.Add("DIR PRIMARY LOC: " + set.PriceLocation.CurrentPrimaryTpo);
            rows.Add("DIR PREV LOC: " + set.PriceLocation.PreviousPrimaryTpo);
            rows.Add("DIR COMPOSITE LOC: " + set.PriceLocation.ConfirmedCompositeTpo);
            rows.Add("DIR OTF PERIODS: " + set.OneTimeFraming.CompletedPeriodCount.ToString(CultureInfo.InvariantCulture)
                     + " UP=" + set.OneTimeFraming.UpStreak.ToString(CultureInfo.InvariantCulture)
                     + " DN=" + set.OneTimeFraming.DownStreak.ToString(CultureInfo.InvariantCulture));
            rows.Add("DIR STRUCT VER: " + set.StructuralContext.StateVersion.ToString(CultureInfo.InvariantCulture));
            rows.Add("DIR TACT VER: " + set.TacticalContext.StateVersion.ToString(CultureInfo.InvariantCulture));
            rows.Add("DIR INPUT: " + Truncate(set.InputFingerprint, 96));
            if (tac.Conflicts.Count > 0)
                rows.Add("DIR CONFLICTS: " + string.Join(",", tac.Conflicts));
            if (set.Limitations.Count > 0)
                rows.Add("DIR LIMITATIONS: " + Truncate(string.Join(",", set.Limitations.Take(4)), 96));
        }

        return rows;
    }

    private static void AppendDirectionalCoreRows(List<string> rows, DirectionalContextSetSnapshot set)
    {
        rows.Add("STRUCTURAL STATE: " + FormatDirectionalState(set.StructuralContext.State));
        rows.Add("TACTICAL STATE: " + FormatDirectionalState(set.TacticalContext.State) + " (DEVELOPING)");
        rows.Add("OTF: " + FormatOtfState(set.OneTimeFraming.State));
        rows.Add("PRICE LOCATION: " + set.PriceLocation.CurrentPrimaryTpo);
    }

    private static string FormatDirectionalState(DirectionalAuctionState state) =>
        state.ToString().ToUpperInvariant();

    private static string FormatOtfState(OneTimeFramingState state) =>
        state.ToString().ToUpperInvariant();

    public static IReadOnlyList<string> BuildAuctionEpisodeLines(
        AuctionEpisodeSetSnapshot? set,
        bool showDiagnostics)
    {
        if (set is null || set.ModuleState == EpisodeModuleState.Disabled)
            return Array.Empty<string>();

        var rows = new List<string>();
        switch (set.ModuleState)
        {
            case EpisodeModuleState.AwaitingReferences:
                rows.Add("EPISODES: AWAITING REFERENCES");
                rows.Add("EPISODE POLICY: " + set.PolicyVersion);
                break;
            case EpisodeModuleState.AwaitingTrades:
                rows.Add("EPISODES: AWAITING TRADES");
                rows.Add("EPISODE POLICY: " + set.PolicyVersion);
                rows.Add("ELIGIBLE REFERENCES: " + set.EligibleReferenceCount.ToString(CultureInfo.InvariantCulture));
                break;
            case EpisodeModuleState.Partial:
                rows.Add("EPISODES: PARTIAL");
                rows.Add("EPISODE POLICY: " + set.PolicyVersion);
                AppendEpisodeCoreRows(rows, set);
                break;
            case EpisodeModuleState.Ready:
                rows.Add("EPISODES: READY");
                rows.Add("EPISODE POLICY: " + set.PolicyVersion);
                AppendEpisodeCoreRows(rows, set);
                break;
            case EpisodeModuleState.Invalid:
                rows.Add("EPISODES: INVALID");
                rows.Add("EPISODE POLICY: " + set.PolicyVersion);
                rows.Add("EPISODE LIMITATION: " + (set.Limitations.FirstOrDefault() ?? "INVALID"));
                break;
            default:
                rows.Add("EPISODES: " + set.ModuleState.ToString().ToUpperInvariant());
                break;
        }

        if (showDiagnostics
            && set.ModuleState is EpisodeModuleState.Ready or EpisodeModuleState.Partial or EpisodeModuleState.AwaitingTrades)
        {
            rows.Add("EPISODE HISTORY: " + (set.HistoryMode == EpisodeHistoryMode.LiveOnly ? "LIVE_ONLY" : "EXACT_REPLAY"));
            rows.Add("EPISODE AUCTION: " + (string.IsNullOrEmpty(set.PrimaryAuctionId) ? "—" : set.PrimaryAuctionId));
            rows.Add("EPISODE REGISTRY REV: " + set.RegistryRevision.ToString(CultureInfo.InvariantCulture));
            var latest = set.LatestUpdatedEpisode;
            if (latest is not null)
            {
                rows.Add("EPISODE ID: " + Truncate(latest.EpisodeId, 96));
                rows.Add("EPISODE REF ID: " + Truncate(latest.ReferenceId, 64));
                rows.Add("EPISODE ATTEMPTS: " + latest.AttemptCount.ToString(CultureInfo.InvariantCulture));
                rows.Add("EPISODE CROSSES: " + latest.CrossCount.ToString(CultureInfo.InvariantCulture));
                rows.Add("EPISODE MAX ABOVE: " + latest.MaximumAboveDistanceTicks.ToString(CultureInfo.InvariantCulture) + "t");
                rows.Add("EPISODE MAX BELOW: " + latest.MaximumBelowDistanceTicks.ToString(CultureInfo.InvariantCulture) + "t");
                rows.Add("EPISODE OUT VOL: " + latest.CanonicalOutsideExecutedVolume.ToString(CultureInfo.InvariantCulture));
                rows.Add("EPISODE OUT TRADES: " + latest.CanonicalOutsideTradeCount.ToString(CultureInfo.InvariantCulture));
                rows.Add("EPISODE AGGRESSOR: " + latest.AggressorEvidenceAvailability);
                rows.Add("EPISODE LOCAL POC: " + (latest.LocalPoc?.ToString(CultureInfo.InvariantCulture) ?? "—"));
                rows.Add("EPISODE STATE VER: " + latest.StateVersion.ToString(CultureInfo.InvariantCulture));
                rows.Add("EPISODE EVENT REV: " + latest.EventRevision.ToString(CultureInfo.InvariantCulture));
            }

            if (set.Limitations.Count > 0)
                rows.Add("EPISODE LIMITATIONS: " + Truncate(string.Join(",", set.Limitations.Take(4)), 96));

            rows.Add("EPISODE TRADE EVENTS SEEN: " + set.TradeEventsSeen.ToString(CultureInfo.InvariantCulture));
            rows.Add("EPISODE TRADE EVENTS ACCEPTED: " + set.TradeEventsAccepted.ToString(CultureInfo.InvariantCulture));
            rows.Add("EPISODE TRADE EVENTS DUPLICATE: " + set.TradeEventsDuplicate.ToString(CultureInfo.InvariantCulture));
            rows.Add("EPISODE TRADE EVENTS REJECTED: " + set.TradeEventsRejected.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrEmpty(set.LastTradeRejectReason))
                rows.Add("LAST EPISODE TRADE REJECT REASON: " + set.LastTradeRejectReason);
            if (!string.IsNullOrEmpty(set.LastAcceptedTradeEventId))
                rows.Add("LAST ACCEPTED TRADE EVENT ID: " + Truncate(set.LastAcceptedTradeEventId, 72));
            if (set.LastAcceptedTradeSequence is long seq)
                rows.Add("LAST ACCEPTED TRADE SEQUENCE: " + seq.ToString(CultureInfo.InvariantCulture));
        }

        return rows;
    }

    private static void AppendEpisodeCoreRows(List<string> rows, AuctionEpisodeSetSnapshot set)
    {
        rows.Add("ELIGIBLE REFERENCES: " + set.EligibleReferenceCount.ToString(CultureInfo.InvariantCulture));
        rows.Add("ACTIVE EPISODES: " + set.ActiveEpisodes.Count.ToString(CultureInfo.InvariantCulture));
        var latest = set.LatestUpdatedEpisode;
        rows.Add("LATEST EPISODE: " + (latest is null ? "NONE" : latest.State.ToString().ToUpperInvariant()));
        if (latest is not null)
        {
            rows.Add("EPISODE REF: " + latest.ReferenceType + " @ " + latest.ReferencePrice.ToString(CultureInfo.InvariantCulture));
            rows.Add("EPISODE ROLE: " + FormatEpisodeRole(latest.ReferenceRole));
            rows.Add("EPISODE STATE: " + latest.State.ToString().ToUpperInvariant());
            rows.Add("EPISODE DIRECTION: " + latest.InteractionDirection.ToString().ToUpperInvariant());
            rows.Add("ATTEMPTS: " + latest.AttemptCount.ToString(CultureInfo.InvariantCulture));
            var maxExc = latest.MaximumCanonicalOutsideDistanceTicks
                         ?? Math.Max(latest.MaximumAboveDistanceTicks, latest.MaximumBelowDistanceTicks);
            rows.Add("MAX EXCURSION: " + maxExc.ToString(CultureInfo.InvariantCulture) + " ticks");
        }
    }

    public static IReadOnlyList<string> BuildAcceptanceReentryEvidenceLines(
        AcceptanceReentryEvidenceSetSnapshot? set,
        bool showDiagnostics)
    {
        if (set is null || set.ModuleState == EvidenceModuleState.Disabled)
            return Array.Empty<string>();

        var rows = new List<string>();
        switch (set.ModuleState)
        {
            case EvidenceModuleState.AwaitingEpisodes:
                rows.Add("ACCEPTANCE/REENTRY EVIDENCE: AWAITING EPISODE");
                rows.Add("EVIDENCE POLICY: " + set.PolicyVersion);
                break;
            case EvidenceModuleState.Partial:
                rows.Add("ACCEPTANCE/REENTRY EVIDENCE: PARTIAL");
                rows.Add("EVIDENCE POLICY: " + set.PolicyVersion);
                AppendEvidenceCoreRows(rows, set);
                break;
            case EvidenceModuleState.Ready:
                rows.Add("ACCEPTANCE/REENTRY EVIDENCE: READY");
                rows.Add("EVIDENCE POLICY: " + set.PolicyVersion);
                AppendEvidenceCoreRows(rows, set);
                break;
            case EvidenceModuleState.Invalid:
                rows.Add("ACCEPTANCE/REENTRY EVIDENCE: INVALID");
                rows.Add("EVIDENCE POLICY: " + set.PolicyVersion);
                rows.Add("EVIDENCE LIMITATION: " + (set.Limitations.FirstOrDefault() ?? "INVALID"));
                break;
            default:
                rows.Add("ACCEPTANCE/REENTRY EVIDENCE: " + set.ModuleState.ToString().ToUpperInvariant());
                break;
        }

        rows.Add("EVIDENCE HISTORY: LIVE_ONLY");

        if (showDiagnostics
            && set.ModuleState is EvidenceModuleState.Ready or EvidenceModuleState.Partial or EvidenceModuleState.AwaitingEpisodes)
        {
            rows.Add("EVIDENCE REGISTRY REV: " + set.RegistryRevision.ToString(CultureInfo.InvariantCulture));
            var latest = set.LatestUpdatedEvidence;
            if (latest is not null)
            {
                rows.Add("EVIDENCE ID: " + Truncate(latest.EvidenceId, 96));
                rows.Add("EVIDENCE EPISODE ID: " + Truncate(latest.EpisodeId, 72));
                rows.Add("EVIDENCE REF ID: " + Truncate(latest.ReferenceId, 64));
                rows.Add("EVIDENCE ATTEMPTS: " + latest.Acceptance.AttemptCount.ToString(CultureInfo.InvariantCulture));
                rows.Add("EVIDENCE OUT SEGMENTS: " + latest.Acceptance.OutsideSegmentCount.ToString(CultureInfo.InvariantCulture));
                rows.Add("EVIDENCE OUT TIME: " + latest.Acceptance.OutsideTime.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture) + "s");
                rows.Add("EVIDENCE OUT VOL: " + latest.Acceptance.OutsideExecutedVolume.ToString(CultureInfo.InvariantCulture));
                rows.Add("EVIDENCE OUT TRADES: " + latest.Acceptance.OutsideTradeCount.ToString(CultureInfo.InvariantCulture));
                rows.Add("EVIDENCE AGGRESSOR: " + latest.Acceptance.AggressorEvidenceAvailability);
                rows.Add("EVIDENCE LOCAL POC: " + (latest.Acceptance.LocalPocTick?.ToString(CultureInfo.InvariantCulture) ?? "—"));
                rows.Add("EVIDENCE STATE VER: " + latest.StateVersion.ToString(CultureInfo.InvariantCulture));
                rows.Add("EVIDENCE EVENT REV: " + latest.EventRevision.ToString(CultureInfo.InvariantCulture));
                rows.Add("EVIDENCE REATTEMPTS: " + latest.Reentry.OutsideReattemptCount.ToString(CultureInfo.InvariantCulture));
                rows.Add("EVIDENCE REF TESTS: " + latest.Reentry.SubsequentReferenceTestCount.ToString(CultureInfo.InvariantCulture));
            }

            if (set.Limitations.Count > 0)
                rows.Add("EVIDENCE LIMITATIONS: " + Truncate(string.Join(",", set.Limitations.Take(4)), 96));
        }

        return rows;
    }

    public static IReadOnlyList<string> BuildExecutedOrderflowLines(
        ExecutedOrderflowSetSnapshot? set,
        bool showDiagnostics)
    {
        if (set is null || set.ModuleState == OrderflowModuleState.Disabled)
            return Array.Empty<string>();

        var rows = new List<string>();
        switch (set.ModuleState)
        {
            case OrderflowModuleState.AwaitingTrades:
                rows.Add("ORDERFLOW: AWAITING TRADES");
                rows.Add("ORDERFLOW POLICY: " + set.PolicyVersion);
                break;
            case OrderflowModuleState.Partial:
                rows.Add("ORDERFLOW: PARTIAL");
                rows.Add("ORDERFLOW POLICY: " + set.PolicyVersion);
                AppendOrderflowCoreRows(rows, set);
                break;
            case OrderflowModuleState.Ready:
                rows.Add("ORDERFLOW: READY");
                rows.Add("ORDERFLOW POLICY: " + set.PolicyVersion);
                AppendOrderflowCoreRows(rows, set);
                break;
            case OrderflowModuleState.Invalid:
                rows.Add("ORDERFLOW: INVALID");
                rows.Add("ORDERFLOW POLICY: " + set.PolicyVersion);
                rows.Add("ORDERFLOW LIMITATION: " + (set.Limitations.FirstOrDefault() ?? "INVALID"));
                break;
            default:
                rows.Add("ORDERFLOW: " + set.ModuleState.ToString().ToUpperInvariant());
                break;
        }

        rows.Add("ORDERFLOW HISTORY: LIVE_ONLY");

        if (showDiagnostics
            && set.ModuleState is OrderflowModuleState.Ready or OrderflowModuleState.Partial or OrderflowModuleState.AwaitingTrades)
        {
            var a = set.CurrentAuction;
            if (a is not null)
            {
                rows.Add("ORDERFLOW SNAPSHOT ID: " + Truncate(a.SnapshotId, 96));
                rows.Add("ORDERFLOW STATE VER: " + a.StateVersion.ToString(CultureInfo.InvariantCulture));
                rows.Add("ORDERFLOW EVENT REV: " + a.EventRevision.ToString(CultureInfo.InvariantCulture));
                rows.Add("ORDERFLOW PRICE LEVELS: " + set.PriceLevels.Count.ToString(CultureInfo.InvariantCulture));
                if (a.MinimumTradeInterval is TimeSpan min)
                    rows.Add("ORDERFLOW MIN INTERVAL: " + min.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture) + "ms");
                if (a.LatestTradeInterval is TimeSpan latest)
                    rows.Add("ORDERFLOW LATEST INTERVAL: " + latest.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture) + "ms");
            }

            rows.Add("ORDERFLOW EVENTS SEEN: " + set.EventsSeen.ToString(CultureInfo.InvariantCulture));
            rows.Add("ORDERFLOW EVENTS ACCEPTED: " + set.EventsAccepted.ToString(CultureInfo.InvariantCulture));
            rows.Add("ORDERFLOW EVENTS DUP: " + set.EventsDuplicated.ToString(CultureInfo.InvariantCulture));
            rows.Add("ORDERFLOW EVENTS REJECTED: " + set.EventsRejected.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrEmpty(set.LastRejectionReason))
                rows.Add("ORDERFLOW LAST REJECT: " + set.LastRejectionReason);
            if (set.Limitations.Count > 0)
                rows.Add("ORDERFLOW LIMITATIONS: " + Truncate(string.Join(",", set.Limitations.Take(4)), 96));
        }

        return rows;
    }

    public static IReadOnlyList<string> BuildClusterRawLines(
        ClusterRawSetSnapshot? set,
        bool showDiagnostics)
    {
        if (set is null || set.ModuleState == ClusterRawModuleState.Disabled)
            return Array.Empty<string>();

        var rows = new List<string>();
        switch (set.ModuleState)
        {
            case ClusterRawModuleState.AwaitingOrderflow:
                rows.Add("CLUSTER RAW: AWAITING ORDERFLOW");
                rows.Add("CLUSTER RAW POLICY: " + set.PolicyVersion);
                break;
            case ClusterRawModuleState.Partial:
                rows.Add("CLUSTER RAW: PARTIAL");
                rows.Add("CLUSTER RAW POLICY: " + set.PolicyVersion);
                AppendClusterRawCoreRows(rows, set);
                break;
            case ClusterRawModuleState.Ready:
                rows.Add("CLUSTER RAW: READY");
                rows.Add("CLUSTER RAW POLICY: " + set.PolicyVersion);
                AppendClusterRawCoreRows(rows, set);
                break;
            case ClusterRawModuleState.Invalid:
                rows.Add("CLUSTER RAW: INVALID");
                rows.Add("CLUSTER RAW POLICY: " + set.PolicyVersion);
                rows.Add("CLUSTER RAW LIMITATION: " + (set.Limitations.FirstOrDefault() ?? "INVALID"));
                break;
            default:
                rows.Add("CLUSTER RAW: " + set.ModuleState.ToString().ToUpperInvariant());
                break;
        }

        rows.Add("CLUSTER RAW HISTORY: LIVE_ONLY");

        if (showDiagnostics
            && set.ModuleState is ClusterRawModuleState.Ready or ClusterRawModuleState.Partial or ClusterRawModuleState.AwaitingOrderflow)
        {
            var a = set.CurrentAuction;
            if (a is not null)
            {
                rows.Add("CLUSTER RAW SNAPSHOT ID: " + Truncate(a.SnapshotId, 96));
                rows.Add("CLUSTER RAW OFLOW ID: " + Truncate(a.OrderflowAuctionSnapshotId, 96));
                rows.Add("CLUSTER RAW STATE VER: " + a.StateVersion.ToString(CultureInfo.InvariantCulture));
                rows.Add("CLUSTER RAW EVENT REV: " + a.EventRevision.ToString(CultureInfo.InvariantCulture));
                rows.Add("CLUSTER RAW RANK METHOD: " + set.RankMethod);
            }

            rows.Add("CLUSTER RAW INPUT OFLOW REV: " + set.InputOrderflowEventRevision.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrEmpty(set.LastRejectionReason))
                rows.Add("CLUSTER RAW LAST REJECT: " + set.LastRejectionReason);
            if (set.RejectedStaleRevisionCount > 0)
                rows.Add("CLUSTER RAW STALE REJECTS: " + set.RejectedStaleRevisionCount.ToString(CultureInfo.InvariantCulture));
            if (set.Limitations.Count > 0)
                rows.Add("CLUSTER RAW LIMITATIONS: " + Truncate(string.Join(",", set.Limitations.Take(4)), 96));
        }

        return rows;
    }

    public static IReadOnlyList<string> BuildAuctionEfficiencyLines(
        AuctionEfficiencyEvidenceSetSnapshot? set,
        bool showDiagnostics)
    {
        if (set is null || set.ModuleState == EfficiencyModuleState.Disabled)
            return Array.Empty<string>();

        var rows = new List<string>();
        switch (set.ModuleState)
        {
            case EfficiencyModuleState.AwaitingOrderflow:
                rows.Add("AUCTION EFFICIENCY: AWAITING ORDERFLOW");
                rows.Add("EFFICIENCY POLICY: " + set.PolicyVersion);
                break;
            case EfficiencyModuleState.AwaitingEpisode:
                rows.Add("AUCTION EFFICIENCY: PARTIAL");
                rows.Add("EFFICIENCY POLICY: " + set.PolicyVersion);
                rows.Add("EFFICIENCY SCOPE: CURRENT AUCTION");
                rows.Add("EPISODE EFFICIENCY: AWAITING EPISODE");
                AppendAuctionEfficiencyCoreRows(rows, set.CurrentAuctionEvidence);
                break;
            case EfficiencyModuleState.Partial:
                rows.Add("AUCTION EFFICIENCY: PARTIAL");
                rows.Add("EFFICIENCY POLICY: " + set.PolicyVersion);
                AppendAuctionEfficiencyCoreRows(rows, set.CurrentAuctionEvidence);
                AppendEpisodeEfficiencyRows(rows, set);
                break;
            case EfficiencyModuleState.Ready:
                rows.Add("AUCTION EFFICIENCY: READY");
                rows.Add("EFFICIENCY POLICY: " + set.PolicyVersion);
                AppendAuctionEfficiencyCoreRows(rows, set.CurrentAuctionEvidence);
                AppendEpisodeEfficiencyRows(rows, set);
                break;
            case EfficiencyModuleState.Invalid:
                rows.Add("AUCTION EFFICIENCY: INVALID");
                rows.Add("EFFICIENCY POLICY: " + set.PolicyVersion);
                rows.Add("EFFICIENCY LIMITATION: " + (set.Limitations.FirstOrDefault() ?? "INVALID"));
                break;
            default:
                rows.Add("AUCTION EFFICIENCY: " + set.ModuleState.ToString().ToUpperInvariant());
                break;
        }

        rows.Add("EFFICIENCY HISTORY: LIVE_ONLY");
        rows.Add("EFFICIENCY CLASSIFICATION: NOT CALIBRATED");

        if (showDiagnostics
            && set.ModuleState is EfficiencyModuleState.Ready or EfficiencyModuleState.Partial
                or EfficiencyModuleState.AwaitingEpisode or EfficiencyModuleState.AwaitingOrderflow)
        {
            var a = set.CurrentAuctionEvidence;
            if (a is not null)
            {
                rows.Add("EFFICIENCY SNAPSHOT ID: " + Truncate(a.SnapshotId, 96));
                rows.Add("EFFICIENCY STATE VER: " + a.StateVersion.ToString(CultureInfo.InvariantCulture));
                rows.Add("EFFICIENCY EVENT REV: " + a.EventRevision.ToString(CultureInfo.InvariantCulture));
                rows.Add("EFFICIENCY FINGERPRINT: " + Truncate(a.InputFingerprint, 96));
                rows.Add("EFFORT ASK/BID/UNK: "
                         + FormatOptionalVolume(a.Effort.AskVolume) + "/"
                         + FormatOptionalVolume(a.Effort.BidVolume) + "/"
                         + a.Effort.UnknownAggressorVolume.ToString(CultureInfo.InvariantCulture));
                rows.Add("RESULT FIRST/LATEST: "
                         + (a.Result.FirstPriceTick?.ToString(CultureInfo.InvariantCulture) ?? "—") + "/"
                         + (a.Result.LatestPriceTick?.ToString(CultureInfo.InvariantCulture) ?? "—"));
                rows.Add("TPO POC START/LATEST: "
                         + (a.Result.DevelopingTpoPocStartTick?.ToString(CultureInfo.InvariantCulture) ?? "—") + "/"
                         + (a.Result.DevelopingTpoPocLatestTick?.ToString(CultureInfo.InvariantCulture) ?? "—"));
            }

            if (set.InputFingerprint.HasValue)
                rows.Add("EFFICIENCY INPUT FP: " + Truncate(set.InputFingerprint.Value.ToString(), 96));
            if (!string.IsNullOrEmpty(set.LastRejectionReason))
                rows.Add("EFFICIENCY LAST REJECT: " + set.LastRejectionReason);
            if (set.Limitations.Count > 0)
                rows.Add("EFFICIENCY LIMITATIONS: " + Truncate(string.Join(",", set.Limitations.Take(6)), 96));
        }

        return rows;
    }

    public static IReadOnlyList<string> BuildAuctionResolutionLines(
        AuctionResolutionSetSnapshot? set, bool showDiagnostics)
    {
        if (set is null) return Array.Empty<string>();
        var rows = new List<string>();

        switch (set.ModuleState)
        {
            case ResolutionModuleState.Disabled:
                rows.Add("RESOLUTION: DISABLED");
                return rows;
            case ResolutionModuleState.AwaitingEvidence:
                rows.Add("RESOLUTION: AWAITING EVIDENCE");
                rows.Add("RESOLUTION POLICY: " + set.PolicyVersion);
                rows.Add("RESOLUTION CONCLUSION: NOT CALIBRATED");
                return rows;
            case ResolutionModuleState.Invalid:
                rows.Add("RESOLUTION: INVALID");
                return rows;
        }

        var state = set.ModuleState == ResolutionModuleState.Ready ? "READY" : "PARTIAL";
        rows.Add("RESOLUTION: " + state + " (" + set.ActiveResolutions.Count + " ACTIVE)");
        rows.Add("RESOLUTION POLICY: " + set.PolicyVersion);
        rows.Add("RESOLUTION CONCLUSION: NOT CALIBRATED");

        var latest = set.LatestUpdated;
        if (latest is not null)
        {
            rows.Add("ACCEPTANCE RESOLUTION: " + latest.AcceptanceResolution.ToString().ToUpperInvariant());
            rows.Add("REENTRY RESOLUTION: " + latest.ReentryResolution.ToString().ToUpperInvariant());
            rows.AddRange(BuildOldValueReclaimLines(latest.OldValueReclaim, showDiagnostics));
            if (!string.IsNullOrEmpty(latest.ReferenceId))
                rows.Add("RESOLUTION REF: " + Truncate(latest.ReferenceId, 48));
        }

        if (showDiagnostics)
        {
            rows.Add("RESOLUTION ACTIVE: " + set.ActiveResolutions.Count.ToString(CultureInfo.InvariantCulture));
            rows.Add("RESOLUTION CLOSED: " + set.RecentlyClosedResolutions.Count.ToString(CultureInfo.InvariantCulture));
            rows.Add("RESOLUTION READY: " + set.ReadyCount.ToString(CultureInfo.InvariantCulture)
                     + " PARTIAL: " + set.PartialCount.ToString(CultureInfo.InvariantCulture));
            if (latest is not null)
            {
                rows.Add("RESOLUTION ID: " + Truncate(latest.ResolutionId, 56));
                rows.Add("RESOLUTION ACC OBS: " + latest.InputAcceptanceObservation.ToString().ToUpperInvariant());
                rows.Add("RESOLUTION REENTRY OBS: " + latest.InputReentryObservation.ToString().ToUpperInvariant());
                rows.Add("RESOLUTION STATE VER: " + latest.StateVersion.ToString(CultureInfo.InvariantCulture));
                rows.Add("RESOLUTION EVENT REV: " + latest.EventRevision.ToString(CultureInfo.InvariantCulture));
            }
        }

        return rows;
    }

    public static IReadOnlyList<string> BuildEffortResultLines(
        EffortResultClassificationSetSnapshot? set, bool showDiagnostics)
    {
        if (set is null) return Array.Empty<string>();
        var rows = new List<string>();

        switch (set.ModuleState)
        {
            case EffortResultModuleState.Disabled:
                rows.Add("EFFORT RESULT: DISABLED");
                return rows;
            case EffortResultModuleState.AwaitingEfficiency:
                rows.Add("EFFORT RESULT: AWAITING EFFICIENCY");
                rows.Add("EFFORT RESULT POLICY: " + set.PolicyVersion);
                rows.Add("EFFORT RESULT CLASSIFICATION: NOT CALIBRATED");
                return rows;
            case EffortResultModuleState.Invalid:
                rows.Add("EFFORT RESULT: INVALID");
                return rows;
        }

        var state = set.ModuleState == EffortResultModuleState.Ready ? "READY" : "PARTIAL";
        var total = (set.CurrentAuctionClassification is not null ? 1 : 0)
                    + set.ActiveEpisodeClassifications.Count;
        rows.Add("EFFORT RESULT: " + state + " (" + total + " SCOPE)");
        rows.Add("EFFORT RESULT POLICY: " + set.PolicyVersion);
        rows.Add("EFFORT RESULT CLASSIFICATION: NOT CALIBRATED");

        var latest = set.LatestUpdated;
        if (latest is not null)
        {
            rows.Add("EFFORT RESULT SCOPE: " + latest.ScopeType.ToString().ToUpperInvariant());
            if (!string.IsNullOrEmpty(latest.ReferenceId))
                rows.Add("EFFORT RESULT REF: " + Truncate(latest.ReferenceId, 48));
        }

        if (showDiagnostics && latest is not null)
        {
            rows.Add("EFFORT RESULT READY: " + set.ReadyCount.ToString(CultureInfo.InvariantCulture)
                     + " PARTIAL: " + set.PartialCount.ToString(CultureInfo.InvariantCulture));
            rows.Add("EFFORT RESULT ID: " + Truncate(latest.ClassificationId, 56));
            rows.Add("EFFORT RESULT STATE VER: " + latest.StateVersion.ToString(CultureInfo.InvariantCulture));
            rows.Add("EFFORT RESULT EVENT REV: " + latest.EventRevision.ToString(CultureInfo.InvariantCulture));
        }

        return rows;
    }

    public static IReadOnlyList<string> BuildFarThesisLines(
        FarThesisSetSnapshot? set, bool showDiagnostics)
    {
        if (set is null) return Array.Empty<string>();
        var rows = new List<string>();

        switch (set.ModuleState)
        {
            case ThesisModuleState.Disabled:
                rows.Add("FAR THESIS: DISABLED");
                return rows;
            case ThesisModuleState.AwaitingEvidence:
                rows.Add("FAR THESIS: AWAITING EVIDENCE");
                rows.Add("FAR THESIS POLICY: " + set.PolicyVersion);
                rows.Add("FAR THESIS STATE: NOT CALIBRATED");
                return rows;
            case ThesisModuleState.Invalid:
                rows.Add("FAR THESIS: INVALID");
                return rows;
        }

        var state = set.ModuleState == ThesisModuleState.Ready ? "READY" : "PARTIAL";
        rows.Add("FAR THESIS: " + state + " (" + set.ActiveTheses.Count + " ACTIVE)");
        rows.Add("FAR THESIS POLICY: " + set.PolicyVersion);
        rows.Add("FAR THESIS STATE: NOT CALIBRATED");

        var latest = set.LatestUpdated;
        if (latest is not null)
        {
            rows.Add("FAR STATE: " + latest.FarState.ToString().ToUpperInvariant());
            rows.Add("FAR DIRECTION: " + latest.Direction.ToString().ToUpperInvariant());
            if (latest.NotCalibrated)
                rows.Add("FAR CALIBRATION: NOT CALIBRATED");
            if (!string.IsNullOrEmpty(latest.ReferenceId))
                rows.Add("FAR REF: " + Truncate(latest.ReferenceId, 48));
        }

        if (showDiagnostics && latest is not null)
        {
            rows.Add("FAR ACTIVE: " + set.ActiveTheses.Count.ToString(CultureInfo.InvariantCulture));
            rows.Add("FAR CLOSED: " + set.RecentlyClosedTheses.Count.ToString(CultureInfo.InvariantCulture));
            rows.Add("FAR ID: " + Truncate(latest.ThesisId, 56));
            rows.Add("FAR STATE VER: " + latest.StateVersion.ToString(CultureInfo.InvariantCulture));
            rows.Add("FAR EVENT REV: " + latest.EventRevision.ToString(CultureInfo.InvariantCulture));
        }

        return rows;
    }

    public static IReadOnlyList<string> BuildAacThesisLines(
        AacThesisSetSnapshot? set, bool showDiagnostics)
    {
        if (set is null) return Array.Empty<string>();
        var rows = new List<string>();

        switch (set.ModuleState)
        {
            case ThesisModuleState.Disabled:
                rows.Add("AAC THESIS: DISABLED");
                return rows;
            case ThesisModuleState.AwaitingEvidence:
                rows.Add("AAC THESIS: AWAITING EVIDENCE");
                rows.Add("AAC THESIS POLICY: " + set.PolicyVersion);
                rows.Add("AAC THESIS STATE: NOT CALIBRATED");
                return rows;
            case ThesisModuleState.Invalid:
                rows.Add("AAC THESIS: INVALID");
                return rows;
        }

        var state = set.ModuleState == ThesisModuleState.Ready ? "READY" : "PARTIAL";
        rows.Add("AAC THESIS: " + state + " (" + set.ActiveTheses.Count + " ACTIVE)");
        rows.Add("AAC THESIS POLICY: " + set.PolicyVersion);
        rows.Add("AAC THESIS STATE: NOT CALIBRATED");

        var latest = set.LatestUpdated;
        if (latest is not null)
        {
            rows.Add("AAC STATE: " + latest.AacState.ToString().ToUpperInvariant());
            rows.Add("AAC DIRECTION: " + latest.Direction.ToString().ToUpperInvariant());
            if (latest.NotCalibrated)
                rows.Add("AAC CALIBRATION: NOT CALIBRATED");
            if (!string.IsNullOrEmpty(latest.ReferenceId))
                rows.Add("AAC REF: " + Truncate(latest.ReferenceId, 48));
        }

        if (showDiagnostics && latest is not null)
        {
            rows.Add("AAC ACTIVE: " + set.ActiveTheses.Count.ToString(CultureInfo.InvariantCulture));
            rows.Add("AAC CLOSED: " + set.RecentlyClosedTheses.Count.ToString(CultureInfo.InvariantCulture));
            rows.Add("AAC ID: " + Truncate(latest.ThesisId, 56));
            rows.Add("AAC STATE VER: " + latest.StateVersion.ToString(CultureInfo.InvariantCulture));
            rows.Add("AAC EVENT REV: " + latest.EventRevision.ToString(CultureInfo.InvariantCulture));
        }

        return rows;
    }

    public static IReadOnlyList<string> BuildParticipationLines(ParticipationSetSnapshot? set)
    {
        if (set is null)
            return Array.Empty<string>();

        var rows = new List<string>
        {
            "SETTLEMENT TAG: " + FormatSettlementTag(set.SettlementProximity.Tag),
            "THIN PARTICIPATION: " + FormatThinParticipation(set.ThinParticipation.Label)
        };
        return rows;
    }

    public static IReadOnlyList<string> BuildTradeFacilitationLines(
        TradeFacilitationSetSnapshot? set, bool showDiagnostics)
    {
        if (set is null) return Array.Empty<string>();
        var rows = new List<string>();

        switch (set.ModuleState)
        {
            case TradeFacilitationModuleState.Disabled:
                rows.Add("TRADE FACILITATION: DISABLED");
                return rows;
            case TradeFacilitationModuleState.AwaitingEfficiency:
                rows.Add("TRADE FACILITATION: AWAITING EFFICIENCY");
                rows.Add("TRADE FACILITATION POLICY: " + set.PolicyVersion);
                rows.Add("TRADE FACILITATION INDEX: NOT CALIBRATED");
                return rows;
            case TradeFacilitationModuleState.Invalid:
                rows.Add("TRADE FACILITATION: INVALID");
                return rows;
        }

        var state = set.ModuleState == TradeFacilitationModuleState.Ready ? "READY" : "PARTIAL";
        var total = (set.CurrentAuctionFacilitation is not null ? 1 : 0)
                    + set.ActiveEpisodeFacilitations.Count;
        rows.Add("TRADE FACILITATION: " + state + " (" + total + " SCOPE)");
        rows.Add("TRADE FACILITATION POLICY: " + set.PolicyVersion);
        rows.Add("TRADE FACILITATION INDEX: NOT CALIBRATED");

        var latest = set.LatestUpdated;
        if (latest is not null)
        {
            rows.Add("TRADE FACILITATION SCOPE: " + latest.ScopeType.ToString().ToUpperInvariant());
            if (!string.IsNullOrEmpty(latest.ReferenceId))
                rows.Add("TRADE FACILITATION REF: " + Truncate(latest.ReferenceId, 48));
            rows.Add("DIR CONSISTENT EFFORT: "
                + (latest.DirectionConsistentEffortVolume.HasValue
                    ? latest.DirectionConsistentEffortVolume.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : "unavailable"));
            rows.Add("FACILITATION COMPONENTS: "
                     + latest.AvailableComponentCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
                     + "/" + TradeFacilitationPolicyConfig.RequiredComponents.ToString(System.Globalization.CultureInfo.InvariantCulture)
                     + (latest.ComponentsComplete ? "" : " INCOMPLETE"));
            rows.Add("STRUCTURE ALIGNMENT: " + latest.StructureAlignment.ToString().ToUpperInvariant());
            rows.Add("MAINTENANCE RETENTION: "
                     + (latest.MaintenanceProgressRetentionRatio.HasValue
                         ? FormatOptionalRatio(latest.MaintenanceProgressRetentionRatio)
                         : "unavailable"));
            rows.Add("FAVORABLE PROGRESS: "
                + (latest.AchievedFavorableProgressTicks.HasValue
                    ? latest.AchievedFavorableProgressTicks.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) + " ticks"
                    : "unavailable"));
        }

        if (showDiagnostics && latest is not null)
        {
            rows.Add("TRADE FACILITATION READY: " + set.ReadyCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
                     + " PARTIAL: " + set.PartialCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
            rows.Add("TRADE FACILITATION ID: " + Truncate(latest.SnapshotId, 56));
            rows.Add("TRADE FACILITATION STATE VER: " + latest.StateVersion.ToString(System.Globalization.CultureInfo.InvariantCulture));
            rows.Add("TRADE FACILITATION EVENT REV: " + latest.EventRevision.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (latest.FavorableProgressPerDirectionUnit.HasValue)
                rows.Add("FAV PROGRESS PER UNIT: " + FormatOptionalRatio(latest.FavorableProgressPerDirectionUnit));
            if (latest.DirectionConsistentEffortRatio.HasValue)
                rows.Add("DIR CONSISTENT RATIO: " + FormatOptionalRatio(latest.DirectionConsistentEffortRatio));
        }

        return rows;
    }

    private static string FormatSettlementTag(SettlementProximityTag tag) => tag switch
    {
        SettlementProximityTag.PreSettlement => "PRE_SETTLEMENT",
        SettlementProximityTag.SettlementTransition => "SETTLEMENT_TRANSITION",
        SettlementProximityTag.PostSettlement => "POST_SETTLEMENT",
        _ => "UNKNOWN"
    };

    private static string FormatThinParticipation(ThinParticipationLabel label) => label switch
    {
        ThinParticipationLabel.NotCalibrated => "NOT CALIBRATED",
        ThinParticipationLabel.NormalParticipation => "NORMAL",
        ThinParticipationLabel.ReducedParticipation => "REDUCED",
        ThinParticipationLabel.ThinParticipation => "THIN",
        ThinParticipationLabel.DislocatedParticipation => "DISLOCATED",
        _ => label.ToString().ToUpperInvariant()
    };

    private static void AppendAuctionEfficiencyCoreRows(List<string> rows, AuctionEfficiencyEvidenceSnapshot? a)
    {
        if (a is null)
            return;

        rows.Add("EFFICIENCY COVERAGE: " + a.CoverageMode.ToString().ToUpperInvariant());
        rows.Add("EFFORT VOLUME: " + a.Effort.TotalExecutedVolume.ToString(CultureInfo.InvariantCulture));
        rows.Add("EFFORT TRADES: " + a.Effort.TradeCount.ToString(CultureInfo.InvariantCulture));
        rows.Add("EFFORT CLASSIFIED DELTA: "
                 + (a.Effort.AskVolume is null || a.Effort.BidVolume is null
                     ? "unavailable"
                     : a.Effort.ClassifiedDelta.ToString(CultureInfo.InvariantCulture)));
        rows.Add("RESULT NET PROGRESS: "
                 + (a.Result.NetPriceProgressTicks?.ToString(CultureInfo.InvariantCulture) ?? "unavailable") + " ticks");
        rows.Add("RESULT RANGE: "
                 + (a.Result.GrossRangeTicks?.ToString(CultureInfo.InvariantCulture) ?? "unavailable") + " ticks");
        rows.Add("RESULT FAVORABLE / ADVERSE: "
                 + (a.Result.MaximumFavorableProgressTicks?.ToString(CultureInfo.InvariantCulture) ?? "unavailable")
                 + " / "
                 + (a.Result.MaximumAdverseProgressTicks?.ToString(CultureInfo.InvariantCulture) ?? "unavailable"));
        rows.Add("RESULT RETAINED: "
                 + (a.Result.ProgressRetainedTicks?.ToString(CultureInfo.InvariantCulture) ?? "unavailable") + " ticks");
        rows.Add("PROGRESS PER CONTRACT: "
                 + FormatOptionalRatio(a.RawRelationships.NetProgressPerExecutedContract));
        rows.Add("PROGRESS PER TRADE: "
                 + FormatOptionalRatio(a.RawRelationships.NetProgressPerTrade));
    }

    private static void AppendEpisodeEfficiencyRows(List<string> rows, AuctionEfficiencyEvidenceSetSnapshot set)
    {
        var ep = set.ActiveEpisodeEvidence.FirstOrDefault() ?? set.LatestUpdatedEvidence;
        if (ep is null || ep.ScopeType == EfficiencyScopeType.CurrentPrimaryAuction)
            return;

        rows.Add("EPISODE EFFICIENCY: " + ep.MeasurementStatus.ToString().ToUpperInvariant());
        if (!string.IsNullOrEmpty(ep.ReferenceId))
            rows.Add("EPISODE REF: " + Truncate(ep.ReferenceId, 48));
        rows.Add("EPISODE DIRECTION: " + ep.ResultDirection.ToString().ToUpperInvariant());
        rows.Add("EPISODE EFFORT VOLUME: " + ep.Effort.TotalExecutedVolume.ToString(CultureInfo.InvariantCulture));
        rows.Add("EPISODE RESULT PROGRESS: "
                 + (ep.Result.NetPriceProgressTicks?.ToString(CultureInfo.InvariantCulture) ?? "unavailable") + " ticks");
        rows.Add("EPISODE OUTSIDE RATIOS: "
                 + FormatOptionalRatio(ep.Result.OutsideTimeRatio) + "/"
                 + FormatOptionalRatio(ep.Result.OutsideVolumeRatio) + "/"
                 + FormatOptionalRatio(ep.Result.OutsideTradeRatio));
        rows.Add("EPISODE GEOMETRIC REENTRY: "
                 + (ep.Result.GeometricReentryObserved is null
                     ? "unavailable"
                     : ep.Result.GeometricReentryObserved.Value ? "YES" : "NO"));
        rows.Add("EPISODE POC MIGRATION: "
                 + (ep.Result.TpoPocMigrationTicks?.ToString(CultureInfo.InvariantCulture) ?? "unavailable")
                 + "/"
                 + (ep.Result.VolumePocMigrationTicks?.ToString(CultureInfo.InvariantCulture) ?? "unavailable"));
        rows.Add("EPISODE VALUE MIGRATION: "
                 + (ep.Result.TpoValueCentroidMigrationTicks?.ToString(CultureInfo.InvariantCulture) ?? "unavailable")
                 + "/"
                 + (ep.Result.VolumeValueCentroidMigrationTicks?.ToString(CultureInfo.InvariantCulture) ?? "unavailable"));
    }

    private static void AppendClusterRawCoreRows(List<string> rows, ClusterRawSetSnapshot set)
    {
        var a = set.CurrentAuction;
        rows.Add("CLUSTER RAW COVERAGE: " + (a?.CoverageMode.ToString().ToUpperInvariant() ?? "UNKNOWN"));
        rows.Add("CLUSTER PRICE LEVELS: " + (a?.PriceLevelCount.ToString(CultureInfo.InvariantCulture) ?? "0"));
        rows.Add("CLASSIFIED LEVELS: " + (a?.ClassifiedLevelCount.ToString(CultureInfo.InvariantCulture) ?? "0"));
        rows.Add("UNKNOWN-ONLY LEVELS: " + (a?.UnknownOnlyLevelCount.ToString(CultureInfo.InvariantCulture) ?? "0"));
        rows.Add("CLUSTER CLASSIFICATION: NOT CALIBRATED");

        if (a is null || a.PriceLevels.Count == 0)
            return;

        ClusterRawPriceLevelSnapshot? latest = null;
        if (!string.IsNullOrEmpty(a.LatestUpdatedLevelId))
            latest = a.PriceLevels.FirstOrDefault(l => string.Equals(l.LevelId, a.LatestUpdatedLevelId, StringComparison.Ordinal));
        latest ??= a.LatestPriceTick.HasValue
            ? a.PriceLevels.FirstOrDefault(l => l.PriceTick == a.LatestPriceTick.Value)
            : a.PriceLevels[^1];

        if (latest is not null)
        {
            rows.Add("LATEST CLUSTER TICK: " + latest.PriceTick.ToString(CultureInfo.InvariantCulture)
                     + " (" + latest.DecimalPrice.ToString(CultureInfo.InvariantCulture) + ")");
            rows.Add("LEVEL EXECUTED VOLUME: " + latest.ExecutedVolume.ToString(CultureInfo.InvariantCulture));
            rows.Add("LEVEL TRADES: " + latest.TradeCount.ToString(CultureInfo.InvariantCulture));
            rows.Add("LEVEL ASK/BID/UNKNOWN: "
                     + FormatOptionalVolume(latest.AskVolume) + "/"
                     + FormatOptionalVolume(latest.BidVolume) + "/"
                     + latest.UnknownAggressorVolume.ToString(CultureInfo.InvariantCulture));
            rows.Add("LEVEL CLASSIFIED DELTA: " + latest.ClassifiedDelta.ToString(CultureInfo.InvariantCulture));
            rows.Add("RAW DOMINANT SIDE: " + latest.RawDominantSide.ToString().ToUpperInvariant());
            rows.Add("SAME-PRICE A/B RATIO: " + FormatOptionalRatio(latest.SamePriceAskToBidRatio));
            rows.Add("DIAGONAL A/B RATIO: " + FormatOptionalRatio(latest.DiagonalAskToBidBelowRatio));
            rows.Add("VOLUME RANK: " + latest.VolumeRank.ToString(CultureInfo.InvariantCulture)
                     + "/" + latest.VolumeRankPopulation.ToString(CultureInfo.InvariantCulture));
            rows.Add("ABS DELTA RANK: " + (latest.AbsoluteDeltaRank.HasValue
                ? latest.AbsoluteDeltaRank.Value.ToString(CultureInfo.InvariantCulture) + "/" + latest.VolumeRankPopulation.ToString(CultureInfo.InvariantCulture)
                : "unavailable"));
            rows.Add("VISITS / REVISITS: "
                     + latest.VisitCount.ToString(CultureInfo.InvariantCulture) + "/"
                     + latest.RevisitCount.ToString(CultureInfo.InvariantCulture));
        }

        var ep = a.ActiveEpisodeSnapshots.FirstOrDefault();
        if (ep is not null)
        {
            rows.Add("EPISODE CLUSTER RAW: " + (ep.DataQuality == ClusterRawDataQuality.Complete ? "READY" : "PARTIAL"));
            rows.Add("EPISODE PRICE LEVELS: " + ep.PriceLevelCount.ToString(CultureInfo.InvariantCulture));
            rows.Add("EPISODE MAX VOLUME TICK: " + (ep.MaximumVolumePriceTick?.ToString(CultureInfo.InvariantCulture) ?? "unavailable"));
            rows.Add("EPISODE MAX ABS DELTA TICK: " + (ep.MaximumAbsoluteDeltaPriceTick?.ToString(CultureInfo.InvariantCulture) ?? "unavailable"));
            rows.Add("EPISODE REVISITED LEVELS: " + ep.RevisitedLevelCount.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static string FormatOptionalVolume(decimal? v) =>
        v.HasValue ? v.Value.ToString(CultureInfo.InvariantCulture) : "unavailable";

    private static string FormatOptionalRatio(decimal? r) =>
        r.HasValue ? r.Value.ToString(CultureInfo.InvariantCulture) : "unavailable";

    private static void AppendOrderflowCoreRows(List<string> rows, ExecutedOrderflowSetSnapshot set)
    {
        var a = set.CurrentAuction;
        rows.Add("ORDERFLOW COVERAGE: " + (a?.CoverageMode.ToString().ToUpperInvariant() ?? "UNKNOWN"));
        rows.Add("EXECUTED VOLUME: " + (a?.ExecutedVolume.ToString(CultureInfo.InvariantCulture) ?? "0"));
        rows.Add("TRADES: " + (a?.TradeCount.ToString(CultureInfo.InvariantCulture) ?? "0"));
        if (a is null)
            return;

        var complete = a.AggressorClassificationStatus == AggressorClassificationStatus.Complete;
        rows.Add("ASK VOLUME: " + (complete || a.AskVolume > 0m
            ? a.AskVolume.ToString(CultureInfo.InvariantCulture)
            : (a.AggressorClassificationStatus == AggressorClassificationStatus.Unavailable ? "unavailable" : a.AskVolume.ToString(CultureInfo.InvariantCulture))));
        rows.Add("BID VOLUME: " + (complete || a.BidVolume > 0m
            ? a.BidVolume.ToString(CultureInfo.InvariantCulture)
            : (a.AggressorClassificationStatus == AggressorClassificationStatus.Unavailable ? "unavailable" : a.BidVolume.ToString(CultureInfo.InvariantCulture))));
        rows.Add("UNKNOWN AGGRESSOR VOLUME: " + a.UnknownAggressorVolume.ToString(CultureInfo.InvariantCulture));
        rows.Add((complete ? "DELTA: " : "CLASSIFIED DELTA: ") + a.ClassifiedDelta.ToString(CultureInfo.InvariantCulture)
                 + (complete ? " (COMPLETE)" : ""));
        rows.Add("CLASSIFIED CVD: " + a.ClassifiedCvd.ToString(CultureInfo.InvariantCulture));
        rows.Add("AGGRESSOR COVERAGE: " + FormatRatio(a.AggressorCoverageRatio));

        var ep = set.ActiveEpisodeAggregates.FirstOrDefault();
        if (ep is not null)
        {
            rows.Add("EPISODE ORDERFLOW: " + (ep.DataQuality == OrderflowDataQuality.Complete ? "READY" : "PARTIAL"));
            rows.Add("EPISODE REF: " + ep.ReferenceRole + " / " + Truncate(ep.ReferenceId, 48));
            rows.Add("EPISODE EXECUTED VOLUME: " + ep.ExecutedVolume.ToString(CultureInfo.InvariantCulture));
            rows.Add("EPISODE TRADES: " + ep.TradeCount.ToString(CultureInfo.InvariantCulture));
            rows.Add("EPISODE ASK/BID/UNKNOWN: "
                     + ep.AskVolume.ToString(CultureInfo.InvariantCulture) + "/"
                     + ep.BidVolume.ToString(CultureInfo.InvariantCulture) + "/"
                     + ep.UnknownAggressorVolume.ToString(CultureInfo.InvariantCulture));
            rows.Add("EPISODE CLASSIFIED DELTA: " + ep.ClassifiedDelta.ToString(CultureInfo.InvariantCulture));
            rows.Add("EPISODE PRICE PROGRESS: " + (ep.NetPriceProgressTicks?.ToString(CultureInfo.InvariantCulture) ?? "unavailable") + " ticks");
        }
    }

    private static void AppendEvidenceCoreRows(List<string> rows, AcceptanceReentryEvidenceSetSnapshot set)
    {
        rows.Add("ACTIVE EVIDENCE SETS: " + set.ActiveEvidence.Count.ToString(CultureInfo.InvariantCulture));
        var latest = set.LatestUpdatedEvidence;
        rows.Add("LATEST ACCEPTANCE OBS: " + (latest is null ? "NONE" : latest.AcceptanceObservationState.ToString().ToUpperInvariant()));
        rows.Add("LATEST REENTRY OBS: " + (latest is null ? "NONE" : latest.ReentryObservationState.ToString().ToUpperInvariant()));
        if (latest is not null)
        {
            rows.Add("EVIDENCE REF: " + latest.ReferenceType + " @ " + latest.ReferencePrice.ToString(CultureInfo.InvariantCulture));
            rows.Add("EVIDENCE ROLE: " + FormatEpisodeRole(latest.ReferenceRole));
            if (latest.ReferenceRole == ReferenceInteractionRole.Centerline)
            {
                rows.Add("ACCEPTANCE OBS: NOT APPLICABLE");
                rows.Add("REENTRY OBS: NOT APPLICABLE");
            }
            else
            {
                rows.Add("ACCEPTANCE OBS: " + latest.AcceptanceObservationState.ToString().ToUpperInvariant());
                rows.Add("REENTRY OBS: " + latest.ReentryObservationState.ToString().ToUpperInvariant());
                rows.Add("OUTSIDE TIME RATIO: " + FormatRatio(latest.Acceptance.OutsideTimeRatio));
                rows.Add("OUTSIDE VOLUME RATIO: " + FormatRatio(latest.Acceptance.OutsideVolumeRatio));
                rows.Add("OUTSIDE TRADE RATIO: " + FormatRatio(latest.Acceptance.OutsideTradeCountRatio));
                rows.Add("LOCAL POC DISPLACEMENT: " + (latest.Acceptance.LocalPocDisplacementTicks?.ToString(CultureInfo.InvariantCulture) ?? "unavailable") + " ticks");
                rows.Add("GEOMETRIC REENTRY: " + (latest.Reentry.GeometricReentryObserved ? "YES" : "NO"));
                rows.Add("TIME MAINTAINED INSIDE: " + (latest.Reentry.TimeMaintainedInside > TimeSpan.Zero
                    ? latest.Reentry.TimeMaintainedInside.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture) + "s"
                    : "unavailable"));
            }
        }
    }

    private static string FormatRatio(decimal? ratio) =>
        ratio is null ? "unavailable" : ratio.Value.ToString("0.####", CultureInfo.InvariantCulture);

    private static string FormatEpisodeRole(ReferenceInteractionRole role) => role switch
    {
        ReferenceInteractionRole.UpperBoundary => "UPPER_BOUNDARY",
        ReferenceInteractionRole.LowerBoundary => "LOWER_BOUNDARY",
        ReferenceInteractionRole.Centerline => "CENTERLINE",
        _ => "UNSUPPORTED"
    };

    private static string Truncate(string value, int max)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= max)
            return value;
        return value[..max] + "…";
    }

    private static string FormatEvidence(CompositeEvidenceState state) => state switch
    {
        CompositeEvidenceState.NotCalibrated => "NOT CALIBRATED",
        CompositeEvidenceState.NotEvaluated => "NOT EVALUATED",
        CompositeEvidenceState.InsufficientData => "SHADOW INSUFFICIENT",
        CompositeEvidenceState.MergeEvidencePresent => "SHADOW MERGE",
        CompositeEvidenceState.SeparationEvidencePresent => "SHADOW SEPARATION",
        CompositeEvidenceState.Conflicted => "SHADOW CONFLICTED",
        CompositeEvidenceState.Invalid => "INVALID",
        _ => state.ToString().ToUpperInvariant()
    };

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

    public static IReadOnlyList<string> BuildSignalMaturityLines(
        SignalMaturitySetSnapshot? set, bool showDiagnostics)
    {
        if (set is null) return Array.Empty<string>();
        var rows = new List<string>();

        switch (set.ModuleState)
        {
            case MaturityModuleState.Disabled:
                rows.Add("MATURITY: DISABLED");
                return rows;
            case MaturityModuleState.AwaitingThesis:
                rows.Add("MATURITY: AWAITING THESIS");
                rows.Add("MATURITY POLICY: " + set.PolicyVersion);
                rows.Add("MATURITY LEVEL: NOT CALIBRATED");
                return rows;
            case MaturityModuleState.Invalid:
                rows.Add("MATURITY: INVALID");
                return rows;
        }

        var state = set.ModuleState == MaturityModuleState.Ready ? "READY" : "PARTIAL";
        rows.Add("MATURITY: " + state + " (" + set.ActiveCandidates.Count + " SCOPE)");
        rows.Add("MATURITY POLICY: " + set.PolicyVersion);
        rows.Add("MATURITY LEVEL: NOT CALIBRATED");
        rows.Add("CANDIDATES: " + set.CandidateCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (set.FastShadowOnly)
            rows.Add("FAST MODE: SHADOW ONLY");

        var latest = set.LatestUpdated;
        if (latest is not null)
        {
            rows.Add("MATURITY FAMILY: " + latest.ThesisFamily);
            rows.Add("LIFECYCLE: " + latest.LifecycleState.ToString().ToUpperInvariant());
            rows.Add("EXPECTED BEHAVIOR: " + latest.ExpectedBehavior.ToString().ToUpperInvariant());
            rows.Add("EXPECTED BEHAVIOR DEADLINE: NOT CALIBRATED");
            rows.Add("RETEST: " + latest.RetestObservation.ToString().ToUpperInvariant());
            rows.Add("LOCATION: " + latest.ObservedLocation.ToString().ToUpperInvariant());
            rows.Add("LOCATION GATE: " + latest.LocationGate.ToString().ToUpperInvariant());
            rows.Add("TARGET SPACE VETO: NOT AVAILABLE");
        }

        if (showDiagnostics && latest is not null)
        {
            rows.Add("MATURITY ID: " + Truncate(latest.SnapshotId, 56));
            rows.Add("MATURITY THESIS: " + Truncate(latest.ThesisId, 48));
            rows.Add("MATURITY STATE VER: " + latest.StateVersion.ToString(System.Globalization.CultureInfo.InvariantCulture));
            rows.Add("MATURITY EVENT REV: " + latest.EventRevision.ToString(System.Globalization.CultureInfo.InvariantCulture));
            rows.Add("MATURITY BLOCKERS: " + latest.BlockingReasons.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        return rows;
    }

    /// <summary>
    /// Old-value reclaim rows (Phase 2G). Always states NOT CALIBRATED for the
    /// held-vs-failed verdict — the row exists so the operator can see the axis is
    /// being measured, not concluded.
    /// </summary>
    public static IReadOnlyList<string> BuildOldValueReclaimLines(
        OldValueReclaimObservation? reclaim, bool showDiagnostics)
    {
        if (reclaim is null) return Array.Empty<string>();
        var rows = new List<string>
        {
            "OLD VALUE RECLAIM: " + reclaim.State.ToString().ToUpperInvariant()
        };

        if (reclaim.State == OldValueReclaimState.AttemptedOutcomeNotCalibrated)
            rows.Add("RECLAIM OUTCOME: NOT CALIBRATED");

        if (!showDiagnostics) return rows;

        rows.Add("RECLAIM ATTEMPTS: " + reclaim.AttemptCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
        rows.Add("RECLAIM DWELL INSIDE: "
                 + (reclaim.TimeMaintainedInside.HasValue
                     ? reclaim.TimeMaintainedInside.Value.TotalSeconds.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + "s"
                     : "unavailable"));
        rows.Add("RECLAIM MAX DEPTH: "
                 + (reclaim.MaximumDistanceReturnedInsideTicks.HasValue
                     ? reclaim.MaximumDistanceReturnedInsideTicks.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) + " ticks"
                     : "unavailable"));
        rows.Add("RECLAIM LOCAL VALUE REBUILD: "
                 + (reclaim.LocalValueRebuildInside.HasValue
                     ? (reclaim.LocalValueRebuildInside.Value ? "yes" : "no")
                     : "unavailable"));
        rows.Add("RECLAIM SINCE INSIDE: "
                 + (reclaim.ElapsedSinceLastInsideEvent.HasValue
                     ? reclaim.ElapsedSinceLastInsideEvent.Value.TotalSeconds.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + "s"
                     : "unavailable"));
        rows.Add("RECLAIM WINDOW: NOT CALIBRATED");
        return rows;
    }

    public static IReadOnlyList<string> BuildThesisContractLines(
        ThesisContractSetSnapshot? set, bool showDiagnostics)
    {
        if (set is null) return Array.Empty<string>();
        var rows = new List<string>();

        switch (set.ModuleState)
        {
            case ThesisContractModuleState.Disabled:
                rows.Add("CONTRACT: DISABLED");
                return rows;
            case ThesisContractModuleState.AwaitingMaturity:
                rows.Add("CONTRACT: AWAITING MATURITY");
                rows.Add("CONTRACT POLICY: " + set.PolicyVersion);
                rows.Add("CONTRACT STATE: NOT CALIBRATED");
                return rows;
            case ThesisContractModuleState.Invalid:
                rows.Add("CONTRACT: INVALID");
                return rows;
        }

        var state = set.ModuleState == ThesisContractModuleState.Ready ? "READY" : "PARTIAL";
        rows.Add("CONTRACT: " + state + " (" + set.DeclaredCount + " DECLARED)");
        rows.Add("CONTRACT POLICY: " + set.PolicyVersion);
        rows.Add("CONTRACT STATE: NOT CALIBRATED");
        if (!set.ProtectiveStopAuthorized)
            rows.Add("PROTECTIVE STOP: NOT AUTHORIZED");

        var latest = set.LatestUpdated;
        if (latest is not null)
        {
            rows.Add("CONTRACT FAMILY: " + latest.Family.ToString().ToUpperInvariant());
            rows.Add("EXPECTED BEHAVIOR: " + latest.ExpectedBehavior.ToString().ToUpperInvariant());
            rows.Add("SOURCE OF MOVE: " + latest.SourceOfMove.ToString().ToUpperInvariant());
            rows.Add("HORIZONS DECLARED: " + latest.Horizons.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)
                     + "/" + ThesisContractPolicyConfig.RequiredHorizonRoles.ToString(System.Globalization.CultureInfo.InvariantCulture));
            rows.Add("INVALIDATION DIMS: " + latest.Invalidations.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)
                     + "/" + ThesisContractPolicyConfig.RequiredInvalidationDimensions.ToString(System.Globalization.CultureInfo.InvariantCulture)
                     + " NOT CALIBRATED");
            rows.Add("THESIS EXPIRY: NOT CALIBRATED");
            rows.Add("MISSING EVIDENCE: " + latest.MissingEvidence.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        if (showDiagnostics && latest is not null)
        {
            rows.Add("CONTRACT ID: " + Truncate(latest.ContractId, 56));
            rows.Add("CONTRACT STATE VER: " + latest.StateVersion.ToString(System.Globalization.CultureInfo.InvariantCulture));
            rows.Add("CONTRACT EVENT REV: " + latest.EventRevision.ToString(System.Globalization.CultureInfo.InvariantCulture));
            foreach (var inv in latest.Invalidations)
                rows.Add("INVALIDATION " + inv.Dimension.ToString().ToUpperInvariant()
                         + ": " + inv.State.ToString().ToUpperInvariant());
            rows.Add("CONSISTENCY GATE: " + (latest.ConsistencyGate.AllSatisfied ? "SATISFIED" : "NOT SATISFIED"));
        }

        return rows;
    }

    /// <summary>
    /// Path rows (Phase 3E). Shows what lies ahead and how much room is left, and
    /// distinguishes "no room" from "cannot measure" — the G-LOC-002 distinction.
    /// </summary>
    public static IReadOnlyList<string> BuildPlarLines(
        PlarSetSnapshot? set, bool showDiagnostics)
    {
        if (set is null) return Array.Empty<string>();
        var rows = new List<string>();

        switch (set.ModuleState)
        {
            case PlarModuleState.Disabled:
                rows.Add("PATH: DISABLED");
                return rows;
            case PlarModuleState.AwaitingReferences:
                rows.Add("PATH: AWAITING REFERENCES");
                rows.Add("PATH POLICY: " + set.PolicyVersion);
                rows.Add("TARGET SPACE: NOT MEASURABLE");
                return rows;
            case PlarModuleState.Invalid:
                rows.Add("PATH: INVALID");
                return rows;
        }

        rows.Add("PATH: READY (" + set.ReferenceCount.ToString(System.Globalization.CultureInfo.InvariantCulture) + " REFS)");
        rows.Add("PATH POLICY: " + set.PolicyVersion);
        rows.Add("BARRIER PERMEABILITY: NOT CALIBRATED");

        AppendPathSide(rows, "UP", set.UpPath, showDiagnostics);
        AppendPathSide(rows, "DOWN", set.DownPath, showDiagnostics);

        return rows;
    }

    private static void AppendPathSide(
        List<string> rows, string label, AuctionPathSnapshot? path, bool showDiagnostics)
    {
        if (path is null) return;

        rows.Add(label + " TARGET SPACE: " + FormatTargetSpace(path));

        if (path.NearestBarrier is { } b)
            rows.Add(label + " NEXT BARRIER: " + b.ReferenceType.ToString().ToUpperInvariant()
                     + " @ " + b.DistanceTicks.ToString(System.Globalization.CultureInfo.InvariantCulture) + " ticks");
        if (path.NearestTarget is { } t)
            rows.Add(label + " NEXT TARGET: " + t.ReferenceType.ToString().ToUpperInvariant()
                     + " @ " + t.DistanceTicks.ToString(System.Globalization.CultureInfo.InvariantCulture) + " ticks");

        if (!showDiagnostics) return;

        rows.Add(label + " CORRIDOR: " + path.Corridor.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)
                 + "/" + PlarPolicyConfig.CorridorBarrierCapacity.ToString(System.Globalization.CultureInfo.InvariantCulture));
        foreach (var o in path.Corridor)
            rows.Add(label + " BARRIER " + (o.CorridorIndex + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + ": "
                     + o.ReferenceType.ToString().ToUpperInvariant()
                     + " @ " + o.DistanceTicks.ToString(System.Globalization.CultureInfo.InvariantCulture) + " ticks"
                     + " [" + o.Role.ToString().ToUpperInvariant() + "]");
        if (path.FinalTarget is { } f)
            rows.Add(label + " FINAL TARGET: " + f.ReferenceType.ToString().ToUpperInvariant()
                     + " @ " + f.DistanceTicks.ToString(System.Globalization.CultureInfo.InvariantCulture) + " ticks");
    }

    /// <summary>
    /// "unavailable" and "0 ticks" mean different things and must never render alike:
    /// one is unmeasurable, the other is a measured hard veto.
    /// </summary>
    private static string FormatTargetSpace(AuctionPathSnapshot path) => path.TargetSpaceAvailability switch
    {
        TargetSpaceAvailability.Available =>
            (path.RemainingTargetSpaceTicks?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "?") + " ticks",
        TargetSpaceAvailability.NoTargetAhead => "NONE AHEAD (VETO)",
        _ => "NOT MEASURABLE"
    };

    /// <summary>
    /// Price memory rows (Phase 1I). Reports how often references have been tested.
    /// It never states that a reference has weakened or strengthened - G-REF-001.
    /// </summary>
    public static IReadOnlyList<string> BuildPriceMemoryLines(
        PriceMemorySetSnapshot? set, bool showDiagnostics)
    {
        if (set is null) return Array.Empty<string>();
        var rows = new List<string>();

        switch (set.ModuleState)
        {
            case MemoryModuleState.Disabled:
                rows.Add("MEMORY: DISABLED");
                return rows;
            case MemoryModuleState.AwaitingEpisodes:
                rows.Add("MEMORY: AWAITING EPISODES");
                rows.Add("MEMORY POLICY: " + set.PolicyVersion);
                return rows;
            case MemoryModuleState.Invalid:
                rows.Add("MEMORY: INVALID");
                return rows;
        }

        var state = set.ModuleState == MemoryModuleState.Ready ? "READY" : "PARTIAL";
        rows.Add("MEMORY: " + state + " (" + set.TrackedReferenceCount.ToString(System.Globalization.CultureInfo.InvariantCulture) + " REFS)");
        rows.Add("MEMORY POLICY: " + set.PolicyVersion);
        rows.Add("TESTS OBSERVED: " + set.TotalTestsObserved.ToString(System.Globalization.CultureInfo.InvariantCulture)
                 + " (" + set.RetestedReferenceCount.ToString(System.Globalization.CultureInfo.InvariantCulture) + " RETESTED)");
        rows.Add("REFERENCE STRENGTH: NOT CALIBRATED");
        rows.Add("LIQUIDITY REPLENISHMENT: UNOBSERVABLE (MBO BLOCKED)");

        var latest = set.MostRecentlyTested;
        if (latest is not null)
        {
            rows.Add("LAST TESTED REF: " + Truncate(latest.ReferenceId, 44));
            rows.Add("LAST REF TESTS: " + latest.TestCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
                     + " (" + latest.RetestCount.ToString(System.Globalization.CultureInfo.InvariantCulture) + " RETESTS)");
        }

        if (!showDiagnostics) return rows;

        rows.Add("MEMORY SINCE: " + set.MemoryStartedAtUtc.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) + " UTC");
        rows.Add("MEMORY WINDOW: LIVE ONLY (PRE-START TESTS UNKNOWN)");
        if (latest is not null)
        {
            rows.Add("LAST REF CLOSED TESTS: " + latest.ClosedTestCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
            foreach (var r in latest.Records.TakeLast(3))
                rows.Add("  TEST " + Truncate(r.EpisodeId, 28) + ": "
                         + r.Outcome.ToString().ToUpperInvariant()
                         + " ATT " + r.AttemptCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        return rows;
    }

    /// <summary>
    /// Imbalance rows (Phase 2H, KDK Ch 25). Ratios are shown with the volume they
    /// came from, because a huge ratio on tiny volume is meaningless. No verdict is
    /// emitted: both the ratio rule and the minimum-volume rule are calibrated.
    /// </summary>
    public static IReadOnlyList<string> BuildImbalanceLines(
        ImbalanceSetSnapshot? set, bool showDiagnostics)
    {
        if (set is null) return Array.Empty<string>();
        var rows = new List<string>();

        switch (set.ModuleState)
        {
            case ImbalanceModuleState.Disabled:
                rows.Add("IMBALANCE: DISABLED");
                return rows;
            case ImbalanceModuleState.AwaitingCluster:
                rows.Add("IMBALANCE: AWAITING CLUSTER");
                rows.Add("IMBALANCE RULE: NOT CALIBRATED");
                return rows;
            case ImbalanceModuleState.Invalid:
                rows.Add("IMBALANCE: INVALID");
                return rows;
        }

        var state = set.ModuleState == ImbalanceModuleState.Ready ? "READY" : "PARTIAL";
        rows.Add("IMBALANCE: " + state + " (" + set.Levels.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) + " LEVELS)");
        rows.Add("IMBALANCE POLICY: " + set.PolicyVersion);
        rows.Add("IMBALANCE RULE: NOT CALIBRATED");
        rows.Add("MINIMUM VOLUME RULE: NOT CALIBRATED");
        rows.Add("STACKED RULE: NOT CALIBRATED");
        rows.Add("IMBALANCE LOCATION: " + set.LocationContext.ToString().ToUpperInvariant());
        rows.Add("MAX SAME-SIDE RUN: " + set.MaximumConsecutiveDominanceTicks.ToString(System.Globalization.CultureInfo.InvariantCulture) + " TICKS");
        if (set.LevelsWithoutRatioCount > 0)
            rows.Add("LEVELS WITHOUT RATIO: " + set.LevelsWithoutRatioCount.ToString(System.Globalization.CultureInfo.InvariantCulture));

        if (!showDiagnostics) return rows;

        rows.Add("IMBALANCE NOTE: EXECUTION ASYMMETRY, NOT ACCEPTANCE");
        foreach (var l in set.Levels.Take(3))
            rows.Add("  LVL " + l.DecimalPrice.ToString(System.Globalization.CultureInfo.InvariantCulture)
                     + ": " + l.RawDominantSide.ToString().ToUpperInvariant()
                     + " VOL " + l.ClassifiedVolume.ToString(System.Globalization.CultureInfo.InvariantCulture)
                     + " UNK " + l.UnknownAggressorVolume.ToString(System.Globalization.CultureInfo.InvariantCulture)
                     + " [" + l.Qualification.ToString().ToUpperInvariant() + "]");

        return rows;
    }
}
