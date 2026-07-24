using GC.AuctionFlow.Composite;
using GC.AuctionFlow.Profile;

namespace GC.AuctionFlow.Reference;

/// <summary>
/// Structural Reference host. Consumes Primary + Confirmed Composite snapshots only.
/// No Episode/Acceptance. No Preview Composite contribution. No I/O.
/// </summary>
public sealed class StructuralReferenceHost
{
    private StructuralReferenceRegistry _registry;
    private ReferencePolicyConfig _policy;
    private decimal _tickSize;
    private string _contractIdentity = "Unknown";
    private string _contractEpoch = "Unknown";
    private string _timestampPolicyVersion;
    private StructuralReferenceSetSnapshot? _published;
    private ReferenceInputFingerprint? _lastFingerprint;

    public StructuralReferenceHost(
        decimal tickSize,
        string contractIdentity,
        string contractEpoch,
        string? timestampPolicyVersion = null,
        ReferencePolicyConfig? policy = null)
    {
        if (tickSize <= 0m) throw new ArgumentOutOfRangeException(nameof(tickSize));
        _tickSize = tickSize;
        _contractIdentity = string.IsNullOrWhiteSpace(contractIdentity) ? "Unknown" : contractIdentity;
        _contractEpoch = string.IsNullOrWhiteSpace(contractEpoch) ? "Unknown" : contractEpoch;
        _timestampPolicyVersion = string.IsNullOrWhiteSpace(timestampPolicyVersion)
            ? AtasTimestampNormalizer.PolicyVersion
            : timestampPolicyVersion;
        _policy = policy ?? new ReferencePolicyConfig(enabled: false);
        _registry = new StructuralReferenceRegistry(_tickSize, _contractEpoch, _timestampPolicyVersion);
    }

    public StructuralReferenceSetSnapshot? Current => _published;
    public ReferencePolicyConfig Policy => _policy;
    public ReferenceInputFingerprint? LastAppliedFingerprint => _lastFingerprint;

    public void Configure(
        decimal tickSize,
        string contractIdentity,
        string contractEpoch,
        ReferencePolicyConfig policy,
        string? timestampPolicyVersion = null)
    {
        if (tickSize <= 0m) throw new ArgumentOutOfRangeException(nameof(tickSize));
        if (policy is null) throw new ArgumentNullException(nameof(policy));

        _tickSize = tickSize;
        _contractIdentity = string.IsNullOrWhiteSpace(contractIdentity) ? "Unknown" : contractIdentity;
        _contractEpoch = string.IsNullOrWhiteSpace(contractEpoch) ? "Unknown" : contractEpoch;
        _timestampPolicyVersion = string.IsNullOrWhiteSpace(timestampPolicyVersion)
            ? AtasTimestampNormalizer.PolicyVersion
            : timestampPolicyVersion;
        _policy = policy;
        _registry.Configure(_tickSize, _contractEpoch, _timestampPolicyVersion);

        if (!_policy.Enabled)
        {
            _registry.Reset();
            _published = DisabledSnapshot(DateTime.UtcNow);
            _lastFingerprint = null;
        }
    }

    public void Reset()
    {
        _registry.Reset();
        _published = null;
        _lastFingerprint = null;
    }

    public StructuralReferenceSetSnapshot Rebuild(
        PrimaryProfileSetSnapshot? profiles,
        CompositeSetSnapshot? composite,
        DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        var fingerprint = ReferenceInputFingerprint.Build(
            _policy.Enabled,
            profiles,
            composite,
            _tickSize,
            _contractEpoch,
            _timestampPolicyVersion,
            _policy.Version);

        if (!_policy.Enabled)
        {
            _registry.Reset();
            _published = DisabledSnapshot(now);
            _lastFingerprint = fingerprint;
            return _published;
        }

        try
        {
            var extraction = StructuralReferenceExtractor.Extract(profiles, composite, enabled: true);
            if (extraction.SuggestedState == StructuralReferenceModuleState.AwaitingPrimary)
            {
                _registry.Reset();
                _published = new StructuralReferenceSetSnapshot(
                    StructuralReferenceModuleState.AwaitingPrimary,
                    _policy.Version,
                    Array.Empty<StructuralReferenceSnapshot>(),
                    Array.Empty<StructuralReferenceSnapshot>(),
                    Array.Empty<StructuralReferenceSnapshot>(),
                    Array.Empty<ReferenceConfluenceGroup>(),
                    null,
                    fingerprint.ToString(),
                    _registry.Revision,
                    extraction.KnownLimitations,
                    extraction.UnavailableVolumeReasons,
                    now);
                _lastFingerprint = fingerprint;
                return _published;
            }

            if (extraction.SuggestedState == StructuralReferenceModuleState.Invalid)
            {
                _published = new StructuralReferenceSetSnapshot(
                    StructuralReferenceModuleState.Invalid,
                    _policy.Version,
                    Array.Empty<StructuralReferenceSnapshot>(),
                    Array.Empty<StructuralReferenceSnapshot>(),
                    _registry.RetiredOrExpired.ToArray(),
                    Array.Empty<ReferenceConfluenceGroup>(),
                    null,
                    fingerprint.ToString(),
                    _registry.Revision,
                    extraction.KnownLimitations,
                    extraction.UnavailableVolumeReasons,
                    now);
                _lastFingerprint = fingerprint;
                return _published;
            }

            var (confirmed, developing) = _registry.Reconcile(
                _contractIdentity,
                extraction.Candidates,
                now);

            var active = confirmed.Concat(developing).ToArray();
            var confluence = ReferenceConfluenceBuilder.Build(active);
            var nearest = NearestReferenceBuilder.Build(
                active,
                extraction.CurrentPrice,
                new PriceGrid(_tickSize));

            var state = extraction.SuggestedState;
            if (state == StructuralReferenceModuleState.Ready
                && extraction.UnavailableVolumeReasons.Count > 0)
                state = StructuralReferenceModuleState.Partial;

            _published = new StructuralReferenceSetSnapshot(
                state,
                _policy.Version,
                confirmed,
                developing,
                _registry.RetiredOrExpired.ToArray(),
                confluence,
                nearest,
                fingerprint.ToString(),
                _registry.Revision,
                extraction.KnownLimitations,
                extraction.UnavailableVolumeReasons,
                now);
            _lastFingerprint = fingerprint;
            return _published;
        }
        catch (InvalidOperationException ex)
        {
            _published = new StructuralReferenceSetSnapshot(
                StructuralReferenceModuleState.Invalid,
                _policy.Version,
                Array.Empty<StructuralReferenceSnapshot>(),
                Array.Empty<StructuralReferenceSnapshot>(),
                _registry.RetiredOrExpired.ToArray(),
                Array.Empty<ReferenceConfluenceGroup>(),
                null,
                fingerprint.ToString(),
                _registry.Revision,
                new[] { ex.Message },
                Array.Empty<string>(),
                now);
            _lastFingerprint = fingerprint;
            return _published;
        }
    }

    private StructuralReferenceSetSnapshot DisabledSnapshot(DateTime now) =>
        new(
            StructuralReferenceModuleState.Disabled,
            _policy.Version,
            Array.Empty<StructuralReferenceSnapshot>(),
            Array.Empty<StructuralReferenceSnapshot>(),
            Array.Empty<StructuralReferenceSnapshot>(),
            Array.Empty<ReferenceConfluenceGroup>(),
            null,
            "disabled",
            _registry.Revision,
            Array.Empty<string>(),
            Array.Empty<string>(),
            now);
}
