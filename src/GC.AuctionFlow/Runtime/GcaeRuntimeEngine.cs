using GC.AuctionFlow.Cluster;
using GC.AuctionFlow.Composite;
using GC.AuctionFlow.Core;
using GC.AuctionFlow.DayStructure;
using GC.AuctionFlow.Directional;
using GC.AuctionFlow.Entry;
using GC.AuctionFlow.Execution;
using GC.AuctionFlow.EffortResult;
using GC.AuctionFlow.Efficiency;
using GC.AuctionFlow.Episode;
using GC.AuctionFlow.Evidence;
using GC.AuctionFlow.Facilitation;
using GC.AuctionFlow.Maturity;
using GC.AuctionFlow.Imbalance;
using GC.AuctionFlow.Memory;
using GC.AuctionFlow.Orderflow;
using GC.AuctionFlow.Participation;
using GC.AuctionFlow.Plar;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.Reference;
using GC.AuctionFlow.Resolution;
using GC.AuctionFlow.Thesis;

namespace GC.AuctionFlow.Runtime;

/// <summary>
/// Owns snapshot construction, publication, and transition diagnostics.
/// No ATAS types. No file I/O. No mutable probe/recorder/profile objects exposed.
/// </summary>
public sealed class GcaeRuntimeEngine
{
    private readonly RuntimeGateConfig _config;
    private readonly GcaeRuntimeSnapshotPublisher _publisher = new();
    private readonly RuntimeTransitionLedger _ledger = new();
    private long _publicationSequence;
    private GcaeRuntimeSnapshot? _previous;

    public GcaeRuntimeEngine(RuntimeGateConfig? config = null)
    {
        _config = config ?? new RuntimeGateConfig();
    }

    public RuntimeGateConfig Config => _config;
    public GcaeRuntimeSnapshotPublisher Publisher => _publisher;
    public RuntimeTransitionLedger Transitions => _ledger;
    public GcaeRuntimeSnapshot? Current => _publisher.Current;

    public GcaeRuntimeSnapshot Publish(
        ObservedInstrumentSnapshot? observed,
        string expectedInstrumentCode,
        DataSourceMode mode,
        DataSourceModeProvenance modeProvenance,
        DeclaredFeedProvider provider,
        FeedProviderProvenance providerProvenance,
        bool tradeObserved,
        DateTime? lastTradeCallbackUtc,
        bool rawRecorderMasterEnabled,
        bool tradeRecordingEnabled,
        bool recorderAccepting,
        bool recorderFaulted,
        bool recorderSessionPresent,
        bool indicatorDisposed,
        PrimaryProfileSetSnapshot? profiles = null,
        DateTime? timestampUtc = null,
        bool enableTpoParityDiagnostics = false,
        CompositeSetSnapshot? composite = null,
        bool showCompositeDiagnostics = false,
        StructuralReferenceSetSnapshot? structuralReferences = null,
        bool showStructuralReferenceDiagnostics = false,
        DirectionalContextSetSnapshot? directionalContext = null,
        bool showDirectionalContextDiagnostics = false,
        AuctionEpisodeSetSnapshot? auctionEpisodes = null,
        bool showAuctionEpisodeDiagnostics = false,
        AcceptanceReentryEvidenceSetSnapshot? acceptanceReentryEvidence = null,
        bool showAcceptanceReentryEvidenceDiagnostics = false,
        ExecutedOrderflowSetSnapshot? executedOrderflow = null,
        bool showExecutedOrderflowDiagnostics = false,
        ClusterRawSetSnapshot? clusterRaw = null,
        bool showClusterRawDiagnostics = false,
        AuctionEfficiencyEvidenceSetSnapshot? auctionEfficiency = null,
        bool showAuctionEfficiencyDiagnostics = false,
        AuctionResolutionSetSnapshot? auctionResolution = null,
        bool showAuctionResolutionDiagnostics = false,
        EffortResultClassificationSetSnapshot? effortResult = null,
        bool showEffortResultDiagnostics = false,
        FarThesisSetSnapshot? farThesis = null,
        bool showFarThesisDiagnostics = false,
        AacThesisSetSnapshot? aacThesis = null,
        bool showAacThesisDiagnostics = false,
        ParticipationSetSnapshot? participation = null,
        TradeFacilitationSetSnapshot? tradeFacilitation = null,
        SignalMaturitySetSnapshot? signalMaturity = null,
        bool showSignalMaturityDiagnostics = false,
        ThesisContractSetSnapshot? thesisContract = null,
        bool showThesisContractDiagnostics = false,
        PlarSetSnapshot? plar = null,
        bool showPlarDiagnostics = false,
        PriceMemorySetSnapshot? priceMemory = null,
        bool showPriceMemoryDiagnostics = false,
        ImbalanceSetSnapshot? imbalance = null,
        bool showImbalanceDiagnostics = false,
        DayStructureSnapshot? dayStructure = null,
        EntryPolicySnapshot? entryPolicy = null,
        CfdMappingSnapshot? cfdMapping = null,
        RiskSnapshot? risk = null,
        IReadOnlyList<string>? moduleFaults = null)
    {
        var now = timestampUtc ?? DateTime.UtcNow;
        var contract = ContractSnapshotBuilder.Build(observed, expectedInstrumentCode, _config, now);

        var auctionState = profiles?.CurrentAuction?.ProfileState;
        var profileCap = RuntimeCapabilitySnapshotBuilder.MapProfileState(auctionState);
        var profileExtra = new List<string>();
        if (profiles?.CurrentAuction?.VolumeProfile?.PriceVolumeCapability == PriceVolumeCapability.Unavailable)
            profileExtra.Add("PRICE_VOLUME_DATA_UNAVAILABLE");
        if (profiles is null)
            profileExtra.Add("PROFILE_SET_ABSENT");
        if (composite?.Confirmed is { } conf)
            profileExtra.Add("COMPOSITE_STATUS=" + conf.CompositeStatus);
        if (structuralReferences is not null)
            profileExtra.Add("REFERENCES_STATUS=" + structuralReferences.ModuleState);
        if (directionalContext is not null)
            profileExtra.Add("DIRECTIONAL_STATUS=" + directionalContext.Status);
        if (auctionEpisodes is not null)
            profileExtra.Add("EPISODES_STATUS=" + auctionEpisodes.ModuleState);
        if (acceptanceReentryEvidence is not null)
            profileExtra.Add("EVIDENCE_STATUS=" + acceptanceReentryEvidence.ModuleState);
        if (executedOrderflow is not null)
            profileExtra.Add("ORDERFLOW_STATUS=" + executedOrderflow.ModuleState);
        if (clusterRaw is not null)
            profileExtra.Add("CLUSTER_RAW_STATUS=" + clusterRaw.ModuleState);
        if (auctionEfficiency is not null)
            profileExtra.Add("AUCTION_EFFICIENCY_STATUS=" + auctionEfficiency.ModuleState);
        if (auctionResolution is not null)
            profileExtra.Add("RESOLUTION_STATUS=" + auctionResolution.ModuleState);
        if (effortResult is not null)
            profileExtra.Add("EFFORT_RESULT_STATUS=" + effortResult.ModuleState);
        if (farThesis is not null)
            profileExtra.Add("FAR_THESIS_STATUS=" + farThesis.ModuleState);
        if (aacThesis is not null)
            profileExtra.Add("AAC_THESIS_STATUS=" + aacThesis.ModuleState);
        if (tradeFacilitation is not null)
            profileExtra.Add("TRADE_FACILITATION_STATUS=" + tradeFacilitation.ModuleState);
        if (signalMaturity is not null)
            profileExtra.Add("SIGNAL_MATURITY_STATUS=" + signalMaturity.ModuleState);
        if (thesisContract is not null)
            profileExtra.Add("THESIS_CONTRACT_STATUS=" + thesisContract.ModuleState);
        if (plar is not null)
            profileExtra.Add("PLAR_STATUS=" + plar.ModuleState);
        if (priceMemory is not null)
            profileExtra.Add("PRICE_MEMORY_STATUS=" + priceMemory.ModuleState);
        if (imbalance is not null)
            profileExtra.Add("IMBALANCE_STATUS=" + imbalance.ModuleState);
        if (dayStructure is not null)
            profileExtra.Add("DAY_STRUCTURE_STATUS=" + dayStructure.ModuleState);
        if (entryPolicy is not null)
            profileExtra.Add("ENTRY_PLAN=" + entryPolicy.SelectedPlan);
        if (cfdMapping is not null)
            profileExtra.Add("CFD_MAP=" + cfdMapping.State);
        if (risk is not null)
            profileExtra.Add("RISK_STATE=" + risk.RiskState);

        var capability = RuntimeCapabilitySnapshotBuilder.Build(
            mode, modeProvenance, provider, providerProvenance,
            instrumentIdentityAvailable: observed is not null
                && !string.Equals(observed.IdentityKey, "Unknown", StringComparison.Ordinal),
            tradeObserved,
            lastTradeCallbackUtc,
            rawRecorderMasterEnabled,
            tradeRecordingEnabled,
            recorderAccepting,
            recorderFaulted,
            recorderSessionPresent,
            profileState: profileCap,
            extraLimitations: profileExtra);

        var gate = DataGateEngine.Evaluate(contract, capability, _config, indicatorDisposed, now);
        var seq = Interlocked.Increment(ref _publicationSequence);
        var recorderSummary = capability.RecorderState.ToString().ToUpperInvariant();

        var limitations = new List<string>();
        limitations.AddRange(contract.KnownLimitations);
        limitations.AddRange(capability.KnownLimitations);
        limitations.AddRange(gate.KnownLimitations);
        if (profiles?.KnownLimitations is not null)
            limitations.AddRange(profiles.KnownLimitations);
        if (composite?.Confirmed.KnownLimitations is not null)
            limitations.AddRange(composite.Confirmed.KnownLimitations);
        if (structuralReferences?.KnownLimitations is not null)
            limitations.AddRange(structuralReferences.KnownLimitations);
        if (directionalContext?.Limitations is not null)
            limitations.AddRange(directionalContext.Limitations);
        if (auctionEpisodes?.Limitations is not null)
            limitations.AddRange(auctionEpisodes.Limitations);
        if (acceptanceReentryEvidence?.Limitations is not null)
            limitations.AddRange(acceptanceReentryEvidence.Limitations);
        if (executedOrderflow?.Limitations is not null)
            limitations.AddRange(executedOrderflow.Limitations);
        if (clusterRaw?.Limitations is not null)
            limitations.AddRange(clusterRaw.Limitations);
        if (auctionEfficiency?.Limitations is not null)
            limitations.AddRange(auctionEfficiency.Limitations);
        if (auctionResolution?.Limitations is not null)
            limitations.AddRange(auctionResolution.Limitations);
        if (effortResult?.Limitations is not null)
            limitations.AddRange(effortResult.Limitations);
        if (farThesis?.Limitations is not null)
            limitations.AddRange(farThesis.Limitations);
        if (aacThesis?.Limitations is not null)
            limitations.AddRange(aacThesis.Limitations);
        if (tradeFacilitation?.Limitations is not null)
            limitations.AddRange(tradeFacilitation.Limitations);
        if (signalMaturity?.Limitations is not null)
            limitations.AddRange(signalMaturity.Limitations);
        if (thesisContract?.Limitations is not null)
            limitations.AddRange(thesisContract.Limitations);
        if (plar?.Limitations is not null)
            limitations.AddRange(plar.Limitations);
        if (priceMemory?.Limitations is not null)
            limitations.AddRange(priceMemory.Limitations);
        if (imbalance?.Limitations is not null)
            limitations.AddRange(imbalance.Limitations);
        if (dayStructure?.Limitations is not null)
            limitations.AddRange(dayStructure.Limitations);
        if (entryPolicy?.Limitations is not null)
            limitations.AddRange(entryPolicy.Limitations);
        if (cfdMapping?.Limitations is not null)
            limitations.AddRange(cfdMapping.Limitations);
        if (risk?.Limitations is not null)
            limitations.AddRange(risk.Limitations);

        var resolvedParticipation = participation ?? new ParticipationSetSnapshot(
            SettlementProximityClassifier.Classify(now),
            ThinParticipationClassifier.ClassifyNotCalibrated());
        if (resolvedParticipation.KnownLimitations is not null)
            limitations.AddRange(resolvedParticipation.KnownLimitations);

        var snapshot = new GcaeRuntimeSnapshot(
            gate,
            contract,
            capability,
            ParticipationRegimePlaceholderState.NotAvailable,
            RuntimeCapabilitySnapshotBuilder.MapPlaceholder(auctionState),
            MapReferencePlaceholder(structuralReferences),
            profiles,
            recorderSummary,
            now,
            seq,
            limitations.Distinct(StringComparer.Ordinal).ToArray(),
            enableTpoParityDiagnostics,
            composite,
            showCompositeDiagnostics,
            structuralReferences,
            showStructuralReferenceDiagnostics,
            directionalContext,
            showDirectionalContextDiagnostics,
            auctionEpisodes,
            showAuctionEpisodeDiagnostics,
            acceptanceReentryEvidence,
            showAcceptanceReentryEvidenceDiagnostics,
            executedOrderflow,
            showExecutedOrderflowDiagnostics,
            clusterRaw,
            showClusterRawDiagnostics,
            auctionEfficiency,
            showAuctionEfficiencyDiagnostics,
            auctionResolution,
            showAuctionResolutionDiagnostics,
            effortResult,
            showEffortResultDiagnostics,
            farThesis,
            showFarThesisDiagnostics,
            aacThesis,
            showAacThesisDiagnostics,
            resolvedParticipation,
            tradeFacilitation,
            signalMaturity,
            showSignalMaturityDiagnostics,
            thesisContract,
            showThesisContractDiagnostics,
            plar,
            showPlarDiagnostics,
            priceMemory,
            showPriceMemoryDiagnostics,
            imbalance,
            showImbalanceDiagnostics,
            dayStructure,
            entryPolicy,
            cfdMapping,
            risk,
            moduleFaults);

        RecordTransitions(_previous, snapshot);
        _previous = snapshot;
        _publisher.Publish(snapshot);
        return snapshot;
    }

    private static ReferencePlaceholderState MapReferencePlaceholder(StructuralReferenceSetSnapshot? refs) =>
        refs?.ModuleState switch
        {
            null => ReferencePlaceholderState.NotAvailable,
            StructuralReferenceModuleState.Disabled => ReferencePlaceholderState.Disabled,
            StructuralReferenceModuleState.AwaitingPrimary => ReferencePlaceholderState.AwaitingPrimary,
            StructuralReferenceModuleState.Ready => ReferencePlaceholderState.Ready,
            StructuralReferenceModuleState.Partial => ReferencePlaceholderState.Partial,
            StructuralReferenceModuleState.Invalid => ReferencePlaceholderState.Invalid,
            _ => ReferencePlaceholderState.NotAvailable
        };

    public void Stop()
    {
        _publisher.Clear();
        _previous = null;
    }

    private void RecordTransitions(GcaeRuntimeSnapshot? previous, GcaeRuntimeSnapshot next)
    {
        var id = next.Contract.IdentityKey;
        var ver = next.Version;
        var ts = next.TimestampUtc;

        void Track(string component, string? oldVal, string newVal, string reason)
            => _ledger.TryAddIfChanged(component, oldVal, newVal, reason, id, ver, ts);

        Track("DataState", previous?.DataGate.DataState.ToString(), next.DataGate.DataState.ToString(), next.DataGate.PrimaryReasonCode);
        Track("InstrumentMatchState", previous?.Contract.InstrumentMatchState.ToString(), next.Contract.InstrumentMatchState.ToString(), next.DataGate.PrimaryReasonCode);
        Track("ExpirationState", previous?.Contract.ExpirationState.ToString(), next.Contract.ExpirationState.ToString(), next.DataGate.PrimaryReasonCode);
        Track("RollState", previous?.Contract.RollState.ToString(), next.Contract.RollState.ToString(), next.DataGate.PrimaryReasonCode);
        Track("TradeStreamState", previous?.Capability.TradeStreamState.ToString(), next.Capability.TradeStreamState.ToString(), next.DataGate.PrimaryReasonCode);
        Track("RecorderState", previous?.Capability.RecorderState.ToString(), next.Capability.RecorderState.ToString(), next.DataGate.PrimaryReasonCode);
        Track("ProfileState", previous?.Capability.ProfileState.ToString(), next.Capability.ProfileState.ToString(), next.DataGate.PrimaryReasonCode);
        Track(
            "AuctionId",
            previous?.Profiles?.CurrentAuction?.AuctionId,
            next.Profiles?.CurrentAuction?.AuctionId ?? "",
            next.DataGate.PrimaryReasonCode);
        Track(
            "TpoPoc",
            previous?.Profiles?.CurrentAuction?.TpoProfile?.TpoPoc?.ToString(),
            next.Profiles?.CurrentAuction?.TpoProfile?.TpoPoc?.ToString() ?? "",
            next.DataGate.PrimaryReasonCode);
        Track(
            "Vpoc",
            previous?.Profiles?.CurrentAuction?.VolumeProfile?.VolumePoc?.ToString(),
            next.Profiles?.CurrentAuction?.VolumeProfile?.VolumePoc?.ToString() ?? "",
            next.DataGate.PrimaryReasonCode);
        Track(
            "TpoValueArea",
            FormatVa(previous?.Profiles?.CurrentAuction?.TpoProfile?.TpoVal, previous?.Profiles?.CurrentAuction?.TpoProfile?.TpoVah),
            FormatVa(next.Profiles?.CurrentAuction?.TpoProfile?.TpoVal, next.Profiles?.CurrentAuction?.TpoProfile?.TpoVah),
            next.DataGate.PrimaryReasonCode);
        Track(
            "VolumeValueArea",
            FormatVa(previous?.Profiles?.CurrentAuction?.VolumeProfile?.VolumeVal, previous?.Profiles?.CurrentAuction?.VolumeProfile?.VolumeVah),
            FormatVa(next.Profiles?.CurrentAuction?.VolumeProfile?.VolumeVal, next.Profiles?.CurrentAuction?.VolumeProfile?.VolumeVah),
            next.DataGate.PrimaryReasonCode);
        Track(
            "PriceVolumeCapability",
            previous?.Profiles?.CurrentAuction?.VolumeProfile?.PriceVolumeCapability.ToString(),
            next.Profiles?.CurrentAuction?.VolumeProfile?.PriceVolumeCapability.ToString() ?? "",
            next.DataGate.PrimaryReasonCode);
        Track(
            "ProviderModeGate",
            previous is null ? null : $"{previous.Capability.DataSourceMode}/{previous.Capability.FeedProvider}",
            $"{next.Capability.DataSourceMode}/{next.Capability.FeedProvider}",
            next.DataGate.PrimaryReasonCode);
        Track(
            "CompositeStatus",
            previous?.Composite?.Confirmed.CompositeStatus.ToString(),
            next.Composite?.Confirmed.CompositeStatus.ToString() ?? "",
            next.DataGate.PrimaryReasonCode);
        Track(
            "CompositeId",
            previous?.Composite?.Confirmed.CompositeId,
            next.Composite?.Confirmed.CompositeId ?? "",
            next.DataGate.PrimaryReasonCode);
        Track(
            "CompositeEvidence",
            previous?.Composite?.Confirmed.EvidenceState.ToString(),
            next.Composite?.Confirmed.EvidenceState.ToString() ?? "",
            next.DataGate.PrimaryReasonCode);
        Track(
            "ReferenceModuleState",
            previous?.StructuralReferences?.ModuleState.ToString() ?? previous?.Reference.ToString(),
            next.StructuralReferences?.ModuleState.ToString() ?? next.Reference.ToString(),
            next.DataGate.PrimaryReasonCode);
        Track(
            "ReferenceConfirmedCount",
            previous?.StructuralReferences?.ConfirmedReferences.Count.ToString(),
            next.StructuralReferences?.ConfirmedReferences.Count.ToString() ?? "",
            next.DataGate.PrimaryReasonCode);
        Track(
            "ReferenceDevelopingCount",
            previous?.StructuralReferences?.DevelopingReferences.Count.ToString(),
            next.StructuralReferences?.DevelopingReferences.Count.ToString() ?? "",
            next.DataGate.PrimaryReasonCode);
        Track(
            "DirectionalModuleState",
            previous?.DirectionalContext?.Status.ToString(),
            next.DirectionalContext?.Status.ToString() ?? "",
            next.DataGate.PrimaryReasonCode);
        Track(
            "DirectionalStructuralState",
            previous?.DirectionalContext?.StructuralContext.State.ToString(),
            next.DirectionalContext?.StructuralContext.State.ToString() ?? "",
            next.DataGate.PrimaryReasonCode);
        Track(
            "DirectionalTacticalState",
            previous?.DirectionalContext?.TacticalContext.State.ToString(),
            next.DirectionalContext?.TacticalContext.State.ToString() ?? "",
            next.DataGate.PrimaryReasonCode);
        Track(
            "EpisodeModuleState",
            previous?.AuctionEpisodes?.ModuleState.ToString(),
            next.AuctionEpisodes?.ModuleState.ToString() ?? "",
            next.DataGate.PrimaryReasonCode);
        Track(
            "EpisodeActiveCount",
            previous?.AuctionEpisodes?.ActiveEpisodes.Count.ToString(),
            next.AuctionEpisodes?.ActiveEpisodes.Count.ToString() ?? "",
            next.DataGate.PrimaryReasonCode);
        Track(
            "AuctionEfficiencyState",
            previous?.AuctionEfficiency?.ModuleState.ToString(),
            next.AuctionEfficiency?.ModuleState.ToString() ?? "",
            next.DataGate.PrimaryReasonCode);
    }

    private static string FormatVa(decimal? val, decimal? vah) =>
        val is null && vah is null ? "" : $"{val}/{vah}";
}
