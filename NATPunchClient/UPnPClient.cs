using System.Net;
using LiteNetLib;
using LiteNetLib.Utils;
using Open.Nat;

namespace FNNP
{

    public class UPnPClient 
    {
        private readonly string _ipServer;
        private const string DefaultServer = "https://api.my-ip.io/ip";
        public UPnPClient(string ipServer)
        {
            _ipServer = (ipServer == null) ? DefaultServer : ipServer;
        }
        public UPnPClient() : this(DefaultServer)
        {
        }
        public string GetPublicIP()
        {
            WebClient client = new WebClient();
            // Add a user agent header in case the
            // requested URI contains a query.

            client.Headers.Add ("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");

            Stream data = client.OpenRead (_ipServer);
            StreamReader reader = new StreamReader (data);
            string s = reader.ReadToEnd ();
            Console.WriteLine (s);
            data.Close ();
            reader.Close ();
            return s;
        }
        public async Task<bool> TryToUPnP()
        {
            var discoverer = new NatDiscoverer();
            var cts = new CancellationTokenSource(1000);
            var device = await discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts);

            await device.CreatePortMapAsync(new Mapping(Protocol.Tcp, 8600, 8600, "Test Mapping"));
            return true;
        }
    }
}
