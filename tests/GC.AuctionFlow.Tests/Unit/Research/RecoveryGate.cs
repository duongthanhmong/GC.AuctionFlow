using System.Reflection;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Research;

/// <summary>
/// Holds a research host's recovery load incomplete, so a test can fold while recovery is
/// **provably** unfinished.
///
/// Both `HistoricalScannerHost.Rebuild` and `EffortResultResearchCollector.Rebuild` choose their
/// branch on one expression:
///
/// <code>
/// _loading ??= _store.LoadAsync();
/// if (_loadMerged == 0 &amp;&amp; _loading.IsCompletedSuccessfully) { …merge recovered ids… }
/// </code>
///
/// Nothing public decides that. The recovery-first branch can be reached deterministically by
/// ordering after the host's own load through the store's FIFO queue (`D03C`, `C03C`), but the
/// fold-first branch is the *absence* of a completion, and there is no way to withhold one from
/// the outside.
///
/// **Why reflection rather than a seam in the product.** `_loading ??=` leaves a non-null value
/// alone, so a task the test owns replaces the store round-trip entirely and the host takes the
/// fold-first branch by construction. Doing it this way means the production classes are
/// byte-identical with or without these tests: no new member, no widened accessibility, no
/// `InternalsVisibleTo` (the assembly has none today), and nothing that behaves differently when
/// the gate is absent. A seam — even an internal one — would be a permanent hole in the product
/// that exists only because a test needed it.
///
/// **Why this cannot rot silently.** <see cref="Install{T}"/> fails the test if the field is
/// missing or has changed type. A rename turns these tests red rather than turning them into
/// tests of nothing.
/// </summary>
internal static class RecoveryGate
{
    /// <summary>
    /// Replaces <paramref name="host"/>'s recovery load with a task the caller controls, and
    /// returns its completion source. Until the caller completes it, every `Rebuild` folds with
    /// recovery incomplete; completing it makes the next `Rebuild` merge.
    /// </summary>
    internal static TaskCompletionSource<T> Install<T>(object host)
    {
        var field = host.GetType().GetField(
            "_loading", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.True(
            field is not null,
            $"{host.GetType().Name} no longer has a private `_loading` field. The fold-before-"
            + "recovery tests control the branch through it; without it they would silently stop "
            + "controlling anything. Rewrite them against the new shape rather than deleting them.");

        Assert.True(
            field!.FieldType == typeof(Task<T>),
            $"{host.GetType().Name}._loading is {field.FieldType}, expected {typeof(Task<T>)}. "
            + "The recovery load changed shape; the fold-order tests must be rewritten.");

        var gate = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        field.SetValue(host, gate.Task);
        return gate;
    }
}
