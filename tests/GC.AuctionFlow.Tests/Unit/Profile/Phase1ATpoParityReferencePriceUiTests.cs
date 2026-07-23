using System.Globalization;
using GC.AuctionFlow.Profile;
using GC.AuctionFlow.UI;
using GC.AuctionFlow.Core;
using GC.AuctionFlow.Probe;
using GC.AuctionFlow.Runtime;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit.Profile;

public sealed class Phase1ATpoParityReferencePriceUiTests
{
    private static readonly PrimaryAuctionClockConfig Cfg = new();
    private static readonly PriceGrid Grid = new(0.1m);

    private static DateTimeOffset EtToUtc(int y, int m, int d, int hh, int mm, int ss = 0)
    {
        var local = new DateTime(y, m, d, hh, mm, ss, DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, AuctionTimezoneResolver.Resolve()));
    }

    private static ProfileBarObservation Bar(
        int idx, DateTimeOffset start, DateTimeOffset end, decimal o, decimal h, decimal l, decimal c) =>
        new(idx, start, end, o, h, l, c, 1m,
            Array.Empty<PriceVolumeObservation>(), PriceVolumeCapability.Unavailable, true, true, 1);

    [Fact]
    public void Disabled_setting_ignores_numeric_value()
    {
        var r = TpoParityReferencePriceResolver.Resolve(enabled: false, rawPrice: 4130.2m, tickSize: 0.1m);
        Assert.False(r.IsActive);
        Assert.Null(r.NormalizedPrice);
        Assert.Equal(TpoParityReferencePriceResolver.RejectDisabled, r.Reason);

        var host = new PrimaryProfileHost(0.1m, Cfg)
        {
            EnableTpoParityReferencePrice = false,
            TpoParityReferencePrice = 4130.2m
        };
        var start = EtToUtc(2026, 7, 22, 8, 25, 0);
        host.UpsertBar(Bar(0, start, start.AddMinutes(5), 4124.8m, 4130.2m, 4124.8m, 4125.0m), 0, start.AddMinutes(10));
        var d = host.Current!.CurrentAuction!.TpoProfile!.ParityDiagnostic!;
        Assert.Null(d.ReferenceTarget);
        Assert.Null(d.OperatorReferencePrice);
        Assert.Null(d.ReferencePriceRejectReason);
        Assert.DoesNotContain(d.ToGpsDiagnosticRows(), l => l.StartsWith("REFERENCE PRICE:", StringComparison.Ordinal));
        Assert.DoesNotContain(d.ToGpsDiagnosticRows(), l => l.StartsWith("REFERENCE COUNT:", StringComparison.Ordinal));
    }

    [Fact]
    public void Enabled_valid_4130_2_produces_reference_diagnostics()
    {
        var host = new PrimaryProfileHost(0.1m, Cfg)
        {
            EnableTpoParityReferencePrice = true,
            TpoParityReferencePrice = 4130.2m
        };
        var start = EtToUtc(2026, 7, 22, 8, 25, 0);
        host.UpsertBar(Bar(0, start, start.AddMinutes(5), 4124.8m, 4130.2m, 4124.8m, 4125.0m), 0, start.AddMinutes(10));
        var d = host.Current!.CurrentAuction!.TpoProfile!.ParityDiagnostic!;
        Assert.NotNull(d.ReferenceTarget);
        Assert.Equal(4130.2m, d.OperatorReferencePrice);
        Assert.Equal(4130.2m, d.ReferenceTarget!.Price);
        Assert.Null(d.ReferencePriceRejectReason);
        var rows = d.ToGpsDiagnosticRows();
        Assert.Contains(rows, l => l == "REFERENCE PRICE: 4130.2");
        Assert.Contains(rows, l => l.StartsWith("REFERENCE COUNT:", StringComparison.Ordinal));
        Assert.Contains(rows, l => l.StartsWith("REFERENCE RANK:", StringComparison.Ordinal));
        Assert.Contains(rows, l => l.StartsWith("REFERENCE BELOW MAX BY:", StringComparison.Ordinal));
        Assert.Contains(rows, l => l.StartsWith("REFERENCE PERIODS:", StringComparison.Ordinal));
    }

    [Fact]
    public void Nonpositive_value_rejected()
    {
        Assert.Equal(
            TpoParityReferencePriceResolver.RejectNonPositive,
            TpoParityReferencePriceResolver.Resolve(true, 0m, 0.1m).Reason);
        Assert.Equal(
            TpoParityReferencePriceResolver.RejectNonPositive,
            TpoParityReferencePriceResolver.Resolve(true, -1m, 0.1m).Reason);

        var host = new PrimaryProfileHost(0.1m, Cfg)
        {
            EnableTpoParityReferencePrice = true,
            TpoParityReferencePrice = 0m
        };
        var start = EtToUtc(2026, 7, 22, 8, 25, 0);
        host.UpsertBar(Bar(0, start, start.AddMinutes(5), 100m, 100.1m, 100.0m, 100.0m), 0, start.AddMinutes(10));
        var d = host.Current!.CurrentAuction!.TpoProfile!.ParityDiagnostic!;
        Assert.Null(d.ReferenceTarget);
        Assert.Equal(TpoParityReferencePriceResolver.RejectNonPositive, d.ReferencePriceRejectReason);
        Assert.Contains(d.ToGpsDiagnosticRows(), l => l == "REFERENCE PRICE: INVALID");
        Assert.Contains(d.ToGpsDiagnosticRows(), l => l == "REFERENCE REJECT: NONPOSITIVE");
    }

    [Fact]
    public void Off_tick_value_rejected()
    {
        var r = TpoParityReferencePriceResolver.Resolve(true, 4130.25m, 0.1m);
        Assert.False(r.IsActive);
        Assert.Equal(TpoParityReferencePriceResolver.RejectOffTick, r.Reason);

        var host = new PrimaryProfileHost(0.1m, Cfg)
        {
            EnableTpoParityReferencePrice = true,
            TpoParityReferencePrice = 4130.25m
        };
        var start = EtToUtc(2026, 7, 22, 8, 25, 0);
        host.UpsertBar(Bar(0, start, start.AddMinutes(5), 100m, 100.1m, 100.0m, 100.0m), 0, start.AddMinutes(10));
        var d = host.Current!.CurrentAuction!.TpoProfile!.ParityDiagnostic!;
        Assert.Null(d.ReferenceTarget);
        Assert.Equal(TpoParityReferencePriceResolver.RejectOffTick, d.ReferencePriceRejectReason);
    }

    [Fact]
    public void Locale_ui_persistence_does_not_alter_internal_decimal_value()
    {
        var prior = CultureInfo.CurrentCulture;
        try
        {
            // ATAS Property Grid may display 4130,2 under vi-VN, but the stored property is decimal 4130.2m.
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("vi-VN");
            decimal storedFromUi = 4130.2m;
            Assert.Equal("4130,2", storedFromUi.ToString("0.0", CultureInfo.CurrentCulture));
            Assert.Equal("4130.2", storedFromUi.ToString("0.0", CultureInfo.InvariantCulture));

            var r = TpoParityReferencePriceResolver.Resolve(true, storedFromUi, 0.1m);
            Assert.True(r.IsActive);
            Assert.Equal(4130.2m, r.NormalizedPrice);
            Assert.Equal(Grid.ToTickIndex(4130.2m), Grid.ToTickIndex(r.NormalizedPrice!.Value));
        }
        finally
        {
            CultureInfo.CurrentCulture = prior;
        }
    }

    [Fact]
    public void Reference_setting_does_not_change_tpo_poc_or_va()
    {
        var start = EtToUtc(2026, 7, 22, 8, 20, 0);
        var bars = new[]
        {
            Bar(0, start, start.AddMinutes(5), 100m, 100.5m, 100.0m, 100.0m),
            Bar(1, start.AddMinutes(30), start.AddMinutes(35), 100m, 100.2m, 100.0m, 100.0m)
        };
        var eval = start.AddMinutes(40);

        var off = new PrimaryProfileHost(0.1m, Cfg)
        {
            EnableTpoParityReferencePrice = false,
            TpoParityReferencePrice = 4130.2m
        };
        foreach (var b in bars)
            off.UpsertBar(b, 1, eval);

        var on = new PrimaryProfileHost(0.1m, Cfg)
        {
            EnableTpoParityReferencePrice = true,
            TpoParityReferencePrice = 4130.2m
        };
        foreach (var b in bars)
            on.UpsertBar(b, 1, eval);

        var a = off.Current!.CurrentAuction!.TpoProfile!;
        var bSnap = on.Current!.CurrentAuction!.TpoProfile!;
        Assert.Equal(a.TpoPoc, bSnap.TpoPoc);
        Assert.Equal(a.TpoVal, bSnap.TpoVal);
        Assert.Equal(a.TpoVah, bSnap.TpoVah);
        Assert.Equal(
            a.PriceLevelTpoCounts.OrderBy(kv => kv.Key).Select(kv => (kv.Key, kv.Value)),
            bSnap.PriceLevelTpoCounts.OrderBy(kv => kv.Key).Select(kv => (kv.Key, kv.Value)));
        Assert.Null(a.ParityDiagnostic!.ReferenceTarget);
        Assert.NotNull(bSnap.ParityDiagnostic!.ReferenceTarget);
    }

    [Fact]
    public void Indicator_source_uses_bool_plus_non_nullable_decimal()
    {
        var root = FindRepoRoot();
        var src = File.ReadAllText(Path.Combine(root, "src", "GC.AuctionFlow", "Atas", "GcAuctionFlowIndicator.cs"));
        Assert.Contains("EnableTpoParityReferencePrice = false", src, StringComparison.Ordinal);
        Assert.Contains("TpoParityReferencePrice = 0m", src, StringComparison.Ordinal);
        Assert.Contains("public bool EnableTpoParityReferencePrice", src, StringComparison.Ordinal);
        Assert.Contains("public decimal TpoParityReferencePrice", src, StringComparison.Ordinal);
        Assert.DoesNotContain("decimal? TpoParityReferencePrice", src, StringComparison.Ordinal);
        // UI order: Enable Diagnostics, Enable Reference, Reference Price
        var iDiag = src.IndexOf("Enable TPO Parity Diagnostics", StringComparison.Ordinal);
        var iEnRef = src.IndexOf("Enable TPO Parity Reference Price", StringComparison.Ordinal);
        var iPrice = src.LastIndexOf("[DisplayName(\"TPO Parity Reference Price\")]", StringComparison.Ordinal);
        Assert.True(iDiag > 0 && iEnRef > iDiag && iPrice > iEnRef);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "GC.AuctionFlow.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
