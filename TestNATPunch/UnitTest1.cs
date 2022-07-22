using System.Diagnostics;
using FNNP;
using NUnit.Framework.Internal;

namespace TestNATPunch;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        UPnPClient upnp = new UPnPClient();
        string publicIP = upnp.GetPublicIP();
        
        Task<bool> success = upnp.TryToUPnP();
        Console.WriteLine($"got {success.Result}");
        Assert.Pass();
    }
}
