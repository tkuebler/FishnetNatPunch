using FNNP;
using Mono.Nat;

namespace TestNATPunch;

public class Tests
{
    private UPnPClient upnp;
    [SetUp]
    public void Setup()
    {
        upnp = new UPnPClient("192.168.1.1", Protocol.Udp);
    }

    [Test]
    public async Task TestGetPublicIp()
    {
        
        string publicIP = upnp.GetPublicIp();
        Assert.NotNull(publicIP);
    }
    [Test]
    public async Task TestTryToUPnP() {
        Task tryit = upnp.TryToUPnP();
        tryit.Wait();
        Assert.Null(tryit.Exception);
    }
    
}
