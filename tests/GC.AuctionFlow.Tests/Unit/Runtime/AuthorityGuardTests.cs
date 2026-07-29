using System.Linq;
using GC.AuctionFlow.Resolution;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Runtime;

/// <summary>
/// B0-S2 R5 static authority guards, proven against the COMPILED IL of GC.AuctionFlow.dll via
/// <see cref="MetadataAuthorityGraph"/>. Ownership is decided by resolved namespace/type identity
/// with exact namespace boundaries — never by substring — so a neutrally-named field, a generic
/// carrier, a delegate, an overload, or a signature parameter cannot smuggle Options or
/// Effort/Result authority past the guard.
///
/// A01 PERMANENT_CORE_AUTHORITY_GUARD
///     Options types never reach the AMT/OF acceptance/resolution process closures.
/// A02 PHASE_BOUND_GOVERNANCE_LOCK_PENDING_PHASE_D_REPLACEMENT
///     Uncalibrated Effort/Result never reaches entry/risk/cfd/participation before a Phase-D policy
///     — proven over the decision closures AND the exact reviewed sink contracts.
/// A03 TEMPORARY_PRE_B5_LOCK_PENDING_B5_REPLACEMENT
///     AuctionResolution has no unreviewed Effort/Result dependency; B5 atomically replaces only A03.
/// </summary>
public sealed class AuthorityGuardTests
{
    private const string Indicator = "GC.AuctionFlow.Atas.GcAuctionFlowIndicator";

    // Proof-harness hook: analyse a mutated GC.AuctionFlow.dll when the harness points at one,
    // otherwise the real loaded assembly. Only affects which file the metadata reader opens.
    private static string Asm =>
        Environment.GetEnvironmentVariable("GCAE_AUTHORITY_TARGET_DLL") is { Length: > 0 } p
            ? p
            : typeof(AuctionResolutionHost).Assembly.Location;

    // R5-4 exact owner predicates: namespace boundaries + explicit reviewed identities, no substring.
    private static bool IsOptions(MetadataAuthorityGraph.Named n) =>
        n.Namespace == "GC.AuctionFlow.OptionFlow"
        || n.Namespace.StartsWith("GC.AuctionFlow.OptionFlow.", StringComparison.Ordinal)
        || n.FullName == "GC.AuctionFlow.Atas.OptionFlowOverlayRenderer";

    private static bool IsEffortResult(MetadataAuthorityGraph.Named n) =>
        n.Namespace == "GC.AuctionFlow.EffortResult"
        || n.Namespace.StartsWith("GC.AuctionFlow.EffortResult.", StringComparison.Ordinal)
        || n.Namespace == "GC.AuctionFlow.Efficiency"
        || n.Namespace.StartsWith("GC.AuctionFlow.Efficiency.", StringComparison.Ordinal);

    private static readonly string[] AmtOfProcessMethods =
    {
        "ProcessAcceptanceReentryEvidence", "ProcessAcceptanceReentryResolution",
        "ProcessAuctionEfficiencyEvidence", "ProcessEffortResult", "ProcessTradeFacilitation",
        "ProcessFarThesis", "ProcessAacThesis", "ProcessSignalMaturity", "ProcessThesisContract",
    };

    private static readonly string[] DecisionOrchestratorMethods =
    {
        "ProcessExecutionReadiness", "ProcessProfileBar", "ProcessHistoricalScanner",
    };

    // R5-2 reviewed decision/execution sinks: their decoded contracts (params + return) are checked
    // regardless of reachability, so a forbidden snapshot in a signature fails even when unused.
    private static readonly (string type, string method)[] DecisionSinks =
    {
        ("GC.AuctionFlow.Entry.EntryPolicyHost", "Rebuild"),
        ("GC.AuctionFlow.Execution.RiskHost", "Rebuild"),
        ("GC.AuctionFlow.Execution.CfdMappingHost", "Rebuild"),
        ("GC.AuctionFlow.Research.ParticipationRegimeHost", "Rebuild"),
    };

    [Fact]
    public void A01_amt_of_process_il_closures_have_no_optionflow_authority()
    {
        var findings = MetadataAuthorityGraph.ReachableForbidden(Asm, Indicator, AmtOfProcessMethods, IsOptions);
        Assert.True(findings.Count == 0,
            "Options authority reached an AMT/OF process closure: " +
            string.Join("; ", findings.Select(f => f.Method + " -> " + f.ReferencedType + " (" + f.Reason + ")")));
    }

    [Fact]
    public void A02_effort_result_does_not_reach_pre_phase_d_decision_authority()
    {
        var findings = MetadataAuthorityGraph.ReachableForbidden(
            Asm, Indicator, DecisionOrchestratorMethods, IsEffortResult, DecisionSinks);
        Assert.True(findings.Count == 0,
            "Effort/Result reached pre-Phase-D decision authority: " +
            string.Join("; ", findings.Select(f => f.Method + " -> " + f.ReferencedType + " (" + f.Reason + ")")));
    }

    [Fact]
    public void A03_no_unreviewed_effort_result_adjudication_dependency_pre_b5()
    {
        var findings = MetadataAuthorityGraph.ReachableForbidden(
            Asm, Indicator, new[] { "ProcessAcceptanceReentryResolution" }, IsEffortResult);
        Assert.True(findings.Count == 0,
            "Unreviewed Effort/Result adjudication dependency present (B5 replaces A03): " +
            string.Join("; ", findings.Select(f => f.Method + " -> " + f.ReferencedType + " (" + f.Reason + ")")));
    }
}
