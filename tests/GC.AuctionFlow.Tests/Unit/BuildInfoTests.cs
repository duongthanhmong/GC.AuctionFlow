using GC.AuctionFlow.Core;

namespace GC.AuctionFlow.Tests.Unit;

public sealed class BuildInfoTests
{
    [Fact]
    public void Production_assembly_name_is_GC_AuctionFlow()
    {
        var name = typeof(BuildInfo).Assembly.GetName().Name;
        Assert.Equal(BuildInfo.AssemblyName, name);
        Assert.Equal("GC.AuctionFlow", name);
    }

    [Fact]
    public void Phase_is_P0_06()
    {
        Assert.Equal("P0-06", BuildInfo.Phase);
        Assert.Equal("net10.0-windows", BuildInfo.TargetFrameworkMoniker);
        Assert.Equal("GC AuctionFlow Engine", BuildInfo.VisibleIndicatorName);
        Assert.Equal(CapabilitySchemaVersions.ProbeVersionPlaceholder, "0.0.6");
        Assert.Equal(CapabilitySchemaVersions.SchemaVersion, "1.0.0");
        Assert.Equal("0.0.6+P0-06", BuildInfo.Version);
    }
}
