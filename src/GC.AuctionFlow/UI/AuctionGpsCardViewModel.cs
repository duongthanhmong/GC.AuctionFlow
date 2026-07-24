using System.Globalization;
using GC.AuctionFlow.Composite;
using GC.AuctionFlow.Core;
using GC.AuctionFlow.Directional;
using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Evidence;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Reference;
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
}
