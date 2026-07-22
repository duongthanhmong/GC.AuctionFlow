using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using GC.AuctionFlow.Probe;
using Xunit;

namespace GC.AuctionFlow.Tests.Unit;

/// <summary>P0-06C: prove GCAE source has no chart series write that can receive MBO/DOM prices.</summary>
public sealed class ChartSideEffectAuditTests
{
    [Fact]
    public void GcAuctionFlowIndicator_source_has_no_indexer_or_DataSeries_assignment()
    {
        var root = FindRepoRoot();
        var path = Path.Combine(root, "src", "GC.AuctionFlow", "Atas", "GcAuctionFlowIndicator.cs");
        Assert.True(File.Exists(path), path);
        var src = File.ReadAllText(path);

        Assert.DoesNotMatch(new Regex(@"this\s*\[\s*\w+\s*\]\s*=", RegexOptions.Multiline), src);
        Assert.DoesNotContain("DataSeries.Add", src, StringComparison.Ordinal);
        Assert.DoesNotContain("ValueDataSeries", src, StringComparison.Ordinal);
        Assert.DoesNotContain("CandleDataSeries", src, StringComparison.Ordinal);
        Assert.DoesNotContain("AddSeries", src, StringComparison.Ordinal);
        // P0-08A: OnRender + EnableCustomDrawing allowed for Auction GPS Card overlay only.
        Assert.Contains("OnRender", src, StringComparison.Ordinal);
        Assert.Contains("EnableCustomDrawing = true", src, StringComparison.Ordinal);
        Assert.Contains("DenyToChangePanel = true", src, StringComparison.Ordinal);
        Assert.DoesNotMatch(new Regex(@"^\s*this\s*\[\s*bar\s*\]\s*=", RegexOptions.Multiline), src);
    }

    [Fact]
    public void Probe_and_mapper_source_have_no_chart_series_references()
    {
        var root = FindRepoRoot();
        var probeDir = Path.Combine(root, "src", "GC.AuctionFlow", "Probe");
        foreach (var file in Directory.GetFiles(probeDir, "*.cs"))
        {
            var src = File.ReadAllText(file);
            Assert.DoesNotContain("ValueDataSeries", src, StringComparison.Ordinal);
            Assert.DoesNotContain("CandleDataSeries", src, StringComparison.Ordinal);
            Assert.DoesNotContain("DataSeries", src, StringComparison.Ordinal);
            Assert.DoesNotMatch(new Regex(@"this\s*\["), src);
        }
    }

    [Fact]
    public void Compiled_OnCalculate_does_not_call_set_Item()
    {
        var asmPath = typeof(MboLifecycleProbe).Assembly.Location;
        using var fs = File.OpenRead(asmPath);
        using var pe = new PEReader(fs);
        var md = pe.GetMetadataReader();
        var type = md.TypeDefinitions
            .Select(t => md.GetTypeDefinition(t))
            .First(t => md.GetString(t.Name) == "GcAuctionFlowIndicator");

        MethodDefinitionHandle? calc = null;
        foreach (var mh in type.GetMethods())
        {
            var m = md.GetMethodDefinition(mh);
            if (md.GetString(m.Name) == "OnCalculate")
                calc = mh;
        }

        Assert.True(calc.HasValue);
        var method = md.GetMethodDefinition(calc.Value);
        var body = pe.GetMethodBody(method.RelativeVirtualAddress);
        var il = body.GetILBytes();
        Assert.NotNull(il);

        for (var i = 0; i < il!.Length; i++)
        {
            if (il[i] is 0x28 or 0x6F)
            {
                if (i + 4 >= il.Length) break;
                var token = BitConverter.ToInt32(il, i + 1);
                var handle = MetadataTokens.EntityHandle(token);
                string? name = null;
                if (handle.Kind == HandleKind.MemberReference)
                    name = md.GetString(md.GetMemberReference((MemberReferenceHandle)handle).Name);
                else if (handle.Kind == HandleKind.MethodDefinition)
                    name = md.GetString(md.GetMethodDefinition((MethodDefinitionHandle)handle).Name);

                Assert.NotEqual("set_Item", name);
                Assert.NotEqual("set_Item", name?.Replace(".", ""));
                i += 4;
            }
        }
    }

    [Fact]
    public void MboObservation_is_immutable_primitive_record_without_ATAS_types()
    {
        var t = typeof(MboObservation);
        Assert.True(t.IsSealed);
        foreach (var p in t.GetProperties())
        {
            Assert.False(p.CanWrite);
            var name = p.PropertyType.FullName ?? "";
            Assert.DoesNotContain("ATAS.", name, StringComparison.Ordinal);
            Assert.DoesNotContain("DataSeries", name, StringComparison.Ordinal);
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "GC.AuctionFlow.sln"))
                || File.Exists(Path.Combine(dir.FullName, "src", "GC.AuctionFlow", "GC.AuctionFlow.csproj")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repo root not found from " + AppContext.BaseDirectory);
    }
}
