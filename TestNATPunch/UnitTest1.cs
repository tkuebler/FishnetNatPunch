using System.Diagnostics;
using System.Runtime.CompilerServices;
using FNNP;
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
        UPnPClient upnp = new UPnPClient();
        string publicIP = upnp.GetPublicIP();
        
        Task tryit = upnp.TryToUPnP();
        tryit.Wait();
        Assert.Pass();
    }

}
