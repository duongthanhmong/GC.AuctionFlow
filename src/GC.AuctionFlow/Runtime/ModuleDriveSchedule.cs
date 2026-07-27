namespace GC.AuctionFlow.Runtime;

/// <summary>
/// One module's place in the drive chain.
///
/// A module is driven on two kinds of occasion: a bar closing, and a snapshot being
/// published. Those are different code paths with different frequencies — publishes
/// outnumber bar closes by orders of magnitude on an active tape — but the module list
/// and its order are the same on both. Declaring the step once, with a delegate per
/// occasion, is what stops the two paths from drifting apart.
/// </summary>
public sealed class ModuleDriveStep
{
    /// <param name="module">
    /// Fault-ledger key. Must match the string the module's own handler passes to
    /// <c>RecordFault</c>, or a failure reports under a name nothing else uses.
    /// </param>
    /// <param name="onBar">Drive performed when a bar closes.</param>
    /// <param name="onPublish">
    /// Drive performed on a publish that is not a bar close. Defaults to
    /// <paramref name="onBar"/>. Supply a cheaper variant only when the module would
    /// otherwise do real work on every trade callback — and never one that skips the
    /// rebuild merely because a snapshot already exists, which freezes the module at
    /// whatever state it first reached.
    /// </param>
    /// <param name="dependsOn">Modules whose output this one reads.</param>
    public ModuleDriveStep(
        string module,
        Action onBar,
        Action? onPublish = null,
        params string[] dependsOn)
    {
        if (string.IsNullOrWhiteSpace(module))
            throw new ArgumentException("module name is required", nameof(module));

        Module = module;
        OnBar = onBar ?? throw new ArgumentNullException(nameof(onBar));
        OnPublish = onPublish ?? onBar;
        DependsOn = dependsOn ?? Array.Empty<string>();
    }

    public string Module { get; }

    public Action OnBar { get; }

    /// <summary>Never null. A module with no publish drive is a module that reads stale.</summary>
    public Action OnPublish { get; }

    public IReadOnlyList<string> DependsOn { get; }

    /// <summary>True when this module does the same work on both occasions.</summary>
    public bool PublishSharesBarDrive => ReferenceEquals(OnPublish, OnBar);
}

/// <summary>
/// The module chain, declared once and executed on every path that needs it.
///
/// Before this existed the order lived in two hand-maintained call lists — one in the
/// bar handler, one in the publisher — and they had already diverged: Resolution and
/// Trade Facilitation sat at different points in each. Every integration defect found
/// during live acceptance was a symptom of that split. A module absent from one list
/// simply read NOT AVAILABLE for the whole session while throwing nothing, which is the
/// hardest kind of failure to see.
///
/// Construction validates the chain and throws, because a mis-declared chain is a
/// programming error a test catches long before a session does.
/// </summary>
public sealed class ModuleDriveSchedule
{
    private readonly IReadOnlyList<ModuleDriveStep> _steps;

    public ModuleDriveSchedule(IReadOnlyList<ModuleDriveStep> steps, Action<string, Exception>? onFault = null)
    {
        ArgumentNullException.ThrowIfNull(steps);
        Validate(steps);

        _steps = steps;
        OnFault = onFault;
        Modules = steps.Select(s => s.Module).ToArray();
    }

    public IReadOnlyList<ModuleDriveStep> Steps => _steps;

    public IReadOnlyList<string> Modules { get; }

    private Action<string, Exception>? OnFault { get; }

    /// <summary>Drive every module for a closing bar.</summary>
    public void RunBar() => Run(bar: true);

    /// <summary>Drive every module for a publish.</summary>
    public void RunPublish() => Run(bar: false);

    private void Run(bool bar)
    {
        // Contained per step. One module throwing must not decapitate the rest of the
        // chain — the modules downstream of it will degrade honestly on their own.
        foreach (var step in _steps)
        {
            try
            {
                (bar ? step.OnBar : step.OnPublish)();
            }
            catch (Exception ex)
            {
                OnFault?.Invoke(step.Module, ex);
            }
        }
    }

    private static void Validate(IReadOnlyList<ModuleDriveStep> steps)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var step in steps)
        {
            if (step is null)
                throw new ArgumentException("schedule contains a null step", nameof(steps));

            if (!seen.Add(step.Module))
                throw new ArgumentException(
                    "module driven twice in one pass: " + step.Module, nameof(steps));

            foreach (var dependency in step.DependsOn)
            {
                if (string.Equals(dependency, step.Module, StringComparison.Ordinal))
                    throw new ArgumentException(
                        "module depends on itself: " + step.Module, nameof(steps));

                // Declared later, or not at all — either way this module reads its input
                // one pass stale, which is the silent-wrong-answer case.
                if (!seen.Contains(dependency))
                    throw new ArgumentException(
                        step.Module + " reads " + dependency + ", which is not driven before it",
                        nameof(steps));
            }
        }
    }
}
