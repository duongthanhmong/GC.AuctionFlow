using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Xunit;
using Xunit.Abstractions;

namespace GC.AuctionFlow.Tests.Unit.Probe;

/// <summary>
/// Lists the parts of the ATAS indicator surface that govern what data the platform
/// delivers.
///
/// Reads metadata rather than loading the assembly: `ATAS.Indicators` cannot be loaded in
/// the test host, which is why every reflection-based check in this suite works this way.
///
/// The question it exists to answer: adding this indicator makes ATAS deliver a large
/// trade backlog through the real-time callbacks — one session carried 42,616 replayed
/// trades, and the chart aggregated them into a single bar 47.7 points tall. If the
/// platform exposes a way to decline that delivery, it is on this surface.
/// </summary>
public sealed class AtasSurfaceProbeTests
{
    private readonly ITestOutputHelper _out;

    public AtasSurfaceProbeTests(ITestOutputHelper output) => _out = output;

    [Fact]
    public void A01_Report_members_that_govern_data_delivery()
    {
        var candidates = new[]
        {
            @"C:\Program Files (x86)\ATAS Platform\ATAS.Indicators.dll",
            @"C:\Program Files\ATAS Platform\ATAS.Indicators.dll",
        };

        var path = candidates.FirstOrDefault(File.Exists);
        if (path is null) { _out.WriteLine("ATAS.Indicators.dll not found"); return; }

        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        var md = pe.GetMetadataReader();

        foreach (var handle in md.TypeDefinitions)
        {
            var type = md.GetTypeDefinition(handle);
            var name = md.GetString(type.Name);
            if (name is not ("Indicator" or "ExtendedIndicator" or "ChartObject")) continue;

            _out.WriteLine("=== " + md.GetString(type.Namespace) + "." + name + " ===");

            foreach (var ph in type.GetProperties())
            {
                var p = md.GetPropertyDefinition(ph);
                var pn = md.GetString(p.Name);
                if (System.Text.RegularExpressions.Regex.IsMatch(
                        pn, "Trade|Histor|Subscri|Depth|Market|Cumul|Load|Calc|Series|Bars"))
                    _out.WriteLine("   prop   " + pn);
            }

            foreach (var mh in type.GetMethods())
            {
                var m = md.GetMethodDefinition(mh);
                var mn = md.GetString(m.Name);
                if (System.Text.RegularExpressions.Regex.IsMatch(
                        mn, "^(get_|set_)?(Subscribe|Unsubscribe|Request|Load|Refresh|Reset)"))
                    _out.WriteLine("   method " + mn);
            }
        }
    }
}
