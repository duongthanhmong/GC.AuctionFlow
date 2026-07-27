using GC.AuctionFlow.Runtime;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Runtime;

/// <summary>
/// The schedule itself, exercised as an object rather than as source text.
///
/// This is the part of the indicator's orchestration that could be lifted out of the
/// ATAS-derived type, so it is the part that gets real tests. Every integration defect
/// found during live acceptance was an ordering or reachability failure in the module
/// chain, and each one is now a construction-time error here.
/// </summary>
public sealed class ModuleDriveScheduleTests
{
    private static ModuleDriveStep Step(
        string name, Action? bar = null, Action? publish = null, params string[] deps) =>
        new(name, bar ?? (() => { }), publish, deps);

    // ========== A: a mis-declared chain does not construct ==========

    [Fact]
    public void A01_Driving_a_module_twice_in_one_pass_is_rejected()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new ModuleDriveSchedule(new[] { Step("Plar"), Step("Plar") }));
        Assert.Contains("driven twice", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A module that reads an input driven after it sees last pass's value. That is the
    /// silent-wrong-answer case: nothing throws, nothing reads NOT AVAILABLE, and the
    /// number on the card is simply one pass stale.
    /// </summary>
    [Fact]
    public void A02_Reading_an_input_driven_later_is_rejected()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new ModuleDriveSchedule(new[]
            {
                Step("SignalMaturity", deps: new[] { "FarThesis" }),
                Step("FarThesis"),
            }));
        Assert.Contains("not driven before it", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A03_Reading_an_input_that_is_never_driven_is_rejected() =>
        Assert.Throws<ArgumentException>(() =>
            new ModuleDriveSchedule(new[] { Step("ThesisContract", deps: new[] { "Nonexistent" }) }));

    [Fact]
    public void A04_Depending_on_itself_is_rejected()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new ModuleDriveSchedule(new[] { Step("Plar", deps: new[] { "Plar" }) }));
        Assert.Contains("depends on itself", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A05_A_correctly_ordered_chain_constructs()
    {
        var schedule = new ModuleDriveSchedule(new[]
        {
            Step("FarThesis"),
            Step("AacThesis"),
            Step("SignalMaturity", deps: new[] { "FarThesis", "AacThesis" }),
        });

        Assert.Equal(new[] { "FarThesis", "AacThesis", "SignalMaturity" }, schedule.Modules);
    }

    [Fact]
    public void A06_A_step_must_have_a_bar_drive() =>
        Assert.Throws<ArgumentNullException>(() => new ModuleDriveStep("Plar", null!));

    // ========== B: both passes drive every module ==========

    [Fact]
    public void B01_RunBar_drives_every_module_in_declared_order()
    {
        var order = new List<string>();
        var schedule = new ModuleDriveSchedule(new[]
        {
            Step("A", () => order.Add("A")),
            Step("B", () => order.Add("B"), deps: new[] { "A" }),
            Step("C", () => order.Add("C"), deps: new[] { "B" }),
        });

        schedule.RunBar();
        Assert.Equal(new[] { "A", "B", "C" }, order);
    }

    /// <summary>
    /// The defect this whole type exists to prevent. Trade Facilitation had a bar drive
    /// and no publish drive, so on the five publish paths that are not the bar chain it
    /// was never touched — and the last publish wins. It read AwaitingEfficiency for a
    /// full session while throwing nothing.
    /// </summary>
    [Fact]
    public void B02_RunPublish_drives_every_module_even_without_an_explicit_publish_drive()
    {
        var driven = new List<string>();
        var schedule = new ModuleDriveSchedule(new[]
        {
            Step("TradeFacilitation", () => driven.Add("TradeFacilitation")),
            Step("Plar", () => driven.Add("Plar-bar"), () => driven.Add("Plar-publish")),
        });

        schedule.RunPublish();
        Assert.Equal(new[] { "TradeFacilitation", "Plar-publish" }, driven);
    }

    [Fact]
    public void B03_No_step_can_be_left_without_a_publish_drive()
    {
        var schedule = new ModuleDriveSchedule(new[] { Step("Plar"), Step("Imbalance") });
        Assert.All(schedule.Steps, s => Assert.NotNull(s.OnPublish));
    }

    [Fact]
    public void B04_Publish_drive_defaults_to_the_bar_drive_and_says_so()
    {
        Action bar = () => { };
        Assert.True(new ModuleDriveStep("Plar", bar).PublishSharesBarDrive);
        Assert.False(new ModuleDriveStep("Plar", bar, () => { }).PublishSharesBarDrive);
    }

    // ========== C: one module failing does not decapitate the chain ==========

    /// <summary>
    /// Downstream modules degrade honestly on their own — they read a null input and say
    /// so. Skipping them outright would instead leave them stale, which reads as a live
    /// value and is the harder failure to notice.
    /// </summary>
    [Fact]
    public void C01_A_throwing_module_does_not_stop_the_ones_after_it()
    {
        var driven = new List<string>();
        var schedule = new ModuleDriveSchedule(new[]
        {
            Step("Composite", () => driven.Add("Composite")),
            Step("Episodes", () => throw new InvalidOperationException("boom")),
            Step("Maturity", () => driven.Add("Maturity")),
        });

        schedule.RunBar();
        Assert.Equal(new[] { "Composite", "Maturity" }, driven);
    }

    [Fact]
    public void C02_A_throwing_module_is_reported_under_its_own_name()
    {
        var faults = new List<string>();
        var schedule = new ModuleDriveSchedule(
            new[] { Step("Episodes", () => throw new InvalidOperationException("boom")) },
            (module, ex) => faults.Add(module + ": " + ex.Message));

        schedule.RunPublish();
        Assert.Equal(new[] { "Episodes: boom" }, faults);
    }

    [Fact]
    public void C03_A_chain_with_no_fault_sink_still_survives_a_throw()
    {
        var schedule = new ModuleDriveSchedule(
            new[] { Step("Episodes", () => throw new InvalidOperationException("boom")) });

        schedule.RunBar();
        schedule.RunPublish();
    }
}
