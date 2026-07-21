namespace Aos.LevelEngine.Identity;

public enum DeclaredDataSourceMode
{
    LIVE = 1,
    REPLAY = 2,
    SIM = 3
}

public enum ContractIdentitySource
{
    SECURITY_CODE,
    REPLAY_MANIFEST,
    CHART_SYMBOL_FALLBACK,
    UNAVAILABLE
}

public enum ContractIdentityConfidence
{
    HIGH,
    MEDIUM,
    LOW,
    NONE
}

public sealed class ContractIdentity
{
    public string? ContractCode { get; init; }
    public string? SecurityId { get; init; }
    public DateTime? ExpirationDate { get; init; }
    public required ContractIdentitySource Source { get; init; }
    public required ContractIdentityConfidence Confidence { get; init; }
}

public sealed class ContractIdentityResolveException : Exception
{
    public ContractIdentityResolveException(string message) : base(message) { }
}

/// <summary>
/// Inputs verified at ATAS runtime — do not invent additional chart APIs.
/// </summary>
public sealed class ContractIdentityInputs
{
    public string? SecurityCode { get; init; }
    public string? SecurityId { get; init; }
    public DateTime? SecurityExpiration { get; init; }
    public string? InstrumentInfoInstrument { get; init; }
    public string? ChartSymbolFallback { get; init; }
    public string? ReplayManifestContractCode { get; init; }
    public string? SimDeclaredContractCode { get; init; }
}

public static class ContractIdentityResolver
{
    /// <summary>
    /// Root symbols like "ES" are NOT accepted as contract identity.
    /// Specific codes: ESU6, ESM7, CLH5, …
    /// </summary>
    public static bool LooksLikeSpecificContract(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        var c = code.Trim();
        if (c.Length < 3) return false;
        // Must contain a digit (month/year code) — root "ES"/"NQ" fail
        return c.Any(char.IsDigit);
    }

    public static ContractIdentity Resolve(DeclaredDataSourceMode mode, ContractIdentityInputs inputs)
    {
        return mode switch
        {
            DeclaredDataSourceMode.LIVE => ResolveLive(inputs),
            DeclaredDataSourceMode.REPLAY => ResolveReplay(inputs),
            DeclaredDataSourceMode.SIM => ResolveSim(inputs),
            _ => throw new ContractIdentityResolveException(
                $"FAIL-CLOSED: unknown DeclaredDataSourceMode={mode}")
        };
    }

    private static ContractIdentity ResolveLive(ContractIdentityInputs inputs)
    {
        if (LooksLikeSpecificContract(inputs.SecurityCode))
        {
            return new ContractIdentity
            {
                ContractCode = inputs.SecurityCode!.Trim(),
                SecurityId = inputs.SecurityId,
                ExpirationDate = inputs.SecurityExpiration,
                Source = ContractIdentitySource.SECURITY_CODE,
                Confidence = ContractIdentityConfidence.HIGH
            };
        }

        if (LooksLikeSpecificContract(inputs.ChartSymbolFallback))
        {
            return new ContractIdentity
            {
                ContractCode = inputs.ChartSymbolFallback!.Trim(),
                Source = ContractIdentitySource.CHART_SYMBOL_FALLBACK,
                Confidence = ContractIdentityConfidence.MEDIUM
            };
        }

        // Root "ES" from InstrumentInfo → UNAVAILABLE (not accepted)
        throw new ContractIdentityResolveException(
            "FAIL-CLOSED LIVE: Contract identity UNAVAILABLE " +
            $"(Security.Code='{inputs.SecurityCode}', InstrumentInfo='{inputs.InstrumentInfoInstrument}', " +
            "root symbol not accepted).");
    }

    private static ContractIdentity ResolveReplay(ContractIdentityInputs inputs)
    {
        if (!LooksLikeSpecificContract(inputs.ReplayManifestContractCode))
            throw new ContractIdentityResolveException(
                "FAIL-CLOSED REPLAY: ContractCode required in replay manifest (broker connection not used).");

        return new ContractIdentity
        {
            ContractCode = inputs.ReplayManifestContractCode!.Trim(),
            Source = ContractIdentitySource.REPLAY_MANIFEST,
            Confidence = ContractIdentityConfidence.HIGH
        };
    }

    private static ContractIdentity ResolveSim(ContractIdentityInputs inputs)
    {
        if (!LooksLikeSpecificContract(inputs.SimDeclaredContractCode))
            throw new ContractIdentityResolveException(
                "FAIL-CLOSED SIM: ContractCode must be declared explicitly.");

        return new ContractIdentity
        {
            ContractCode = inputs.SimDeclaredContractCode!.Trim(),
            Source = ContractIdentitySource.CHART_SYMBOL_FALLBACK,
            Confidence = ContractIdentityConfidence.MEDIUM
        };
    }
}

/// <summary>
/// Mid-session contract change → close session; never continue silently.
/// </summary>
public sealed class SessionContractGuard
{
    public const string MutationEventCode = "SESSION_CONTRACT_MUTATION";

    private string? _sessionContractCode;
    private bool _sessionClosed;

    public bool IsSessionClosed => _sessionClosed;
    public string? SessionContractCode => _sessionContractCode;

    public readonly record struct CheckResult(
        bool AllowContinue,
        bool EmitMutationEvent,
        bool CloseSession,
        string? Detail);

    public CheckResult Observe(string? contractCode)
    {
        if (_sessionClosed)
            return new CheckResult(false, false, false, "Session already closed.");

        if (string.IsNullOrWhiteSpace(contractCode))
            return new CheckResult(false, false, false, "ContractCode empty.");

        var code = contractCode.Trim();
        if (_sessionContractCode is null)
        {
            _sessionContractCode = code;
            return new CheckResult(true, false, false, null);
        }

        if (string.Equals(_sessionContractCode, code, StringComparison.OrdinalIgnoreCase))
            return new CheckResult(true, false, false, null);

        _sessionClosed = true;
        return new CheckResult(
            AllowContinue: false,
            EmitMutationEvent: true,
            CloseSession: true,
            Detail: $"from={_sessionContractCode} to={code}");
    }
}
