using System.Net;
using System.Net.Sockets;
using PrinterInstall.Core.Remote;

namespace PrinterInstall.Core.Tests.Remote;

public class SmbShareConnectionAliasTests
{
    [Fact]
    public void IpAddress_UsesReverseDnsNames()
    {
        var names = SmbShareConnection.GetAlternateServerNames("10.1.152.20", _ => new IPHostEntry
        {
            HostName = "LBR-123.example.test",
            AddressList = [IPAddress.Parse("10.1.152.20")]
        });

        Assert.Equal(["LBR-123.example.test", "LBR-123.example.test.", "LBR-123"], names);
    }

    [Fact]
    public void IpAddress_WithoutReverseDns_HasNoAlternates()
    {
        var names = SmbShareConnection.GetAlternateServerNames("10.1.152.20", _ => throw new SocketException());

        Assert.Empty(names);
    }

    [Fact]
    public void HostName_UsesTrailingDotFqdnAndIpv4()
    {
        var names = SmbShareConnection.GetAlternateServerNames("lbr-123", _ => new IPHostEntry
        {
            HostName = "lbr-123.example.test",
            AddressList = [IPAddress.Parse("fe80::1"), IPAddress.Parse("10.1.152.20")]
        });

        Assert.Equal(["lbr-123.", "lbr-123.example.test", "10.1.152.20"], names);
    }

    [Fact]
    public void HostName_WithoutDns_StillOffersTrailingDot()
    {
        var names = SmbShareConnection.GetAlternateServerNames("lbr-123", _ => throw new SocketException());

        Assert.Equal(["lbr-123."], names);
    }

    [Fact]
    public void ResolveServerName_WithoutAlias_ReturnsTrimmedHost()
    {
        Assert.Equal("never-aliased-host", SmbShareConnection.ResolveServerName("  never-aliased-host "));
    }
}
