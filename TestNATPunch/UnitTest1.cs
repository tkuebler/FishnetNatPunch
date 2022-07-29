using System.Diagnostics;
using System.Runtime.CompilerServices;
using FNNP;
using Mono.Nat;
using NUnit.Engine;
using NUnit.Framework.Internal;

namespace TestNATPunch;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task Test1()
    {
        UPnPClient upnp = new UPnPClient("192.168.1.1", Protocol.Udp);
        string publicIP = upnp.GetPublicIp();
        Assert.NotNull(publicIP);
        Task tryit = upnp.TryToUPnP();
        tryit.Wait();
        Assert.Null(tryit.Exception);
    }

}
