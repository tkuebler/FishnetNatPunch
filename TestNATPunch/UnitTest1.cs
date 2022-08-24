using FNNP;
using Mono.Nat;

namespace TestNATPunch
{
    public class Tests
    {
        private UPnPClient upnp;
        [SetUp]
        public void Setup()
        {
            var router = TestContext.Parameters.Get("DefaultRouter", UPnPClient.DefaultRouter);
            upnp = new UPnPClient(router, Protocol.Udp);
        }

        [Test]
        public async Task TestGetPublicIp()
        {

            string publicIP = Utilities.GetPublicIp();
            Assert.NotNull(publicIP);
            string publicIp = Utilities.GetPublicIp_Facillitator(
                new Utilities.IPServiceDef() );
            Assert.IsNotEmpty(publicIp);
        }
        [Test]
        public async Task TestGetPublicIp_Facill()
        {
            string publicIp = Utilities.GetPublicIp_Facillitator(
                new Utilities.IPServiceDef() { addr = "192.168.1.64", port = 6080, key = "test" });
            Assert.IsNotEmpty(publicIp);
        }
        [Test]
        public async Task TestTryToUPnP()
        {
            Task tryit = upnp.TryToUPnP();
            tryit.Wait();
            Assert.Null(tryit.Exception);
        }
    }
}
