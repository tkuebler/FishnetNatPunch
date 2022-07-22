using System.Net;
using LiteNetLib;
using LiteNetLib.Utils;
using Mono.Nat;

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

            client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");

            Stream data = client.OpenRead(_ipServer);
            StreamReader reader = new StreamReader(data);
            string s = reader.ReadToEnd();
            Console.WriteLine(s);
            data.Close();
            reader.Close();
            return s;
        }

        private TaskCompletionSource<object> _completionSource = new TaskCompletionSource<object>();
        public async Task TryToUPnP() // TODO: make async
        {
            NatUtility.DeviceFound += DeviceFound;
            Task runSearch = Task.Factory.StartNew(() => 
                {
                    NatUtility.Search(System.Net.
                        IPAddress.Parse("192.168.1.1"), 
                        NatProtocol.Upnp);
                });
            runSearch.Wait();
            await _completionSource.Task; 
            NatUtility.StopDiscovery();
        }

        readonly SemaphoreSlim locker = new SemaphoreSlim(1, 1);
        public async void DeviceFound(object sender, DeviceEventArgs args)
        {
            await locker.WaitAsync();
            try
            {
                INatDevice device = args.Device;

                // Only interact with one device at a time. Some devices support both
                // upnp and nat-pmp.

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Device found: {0}", device.NatProtocol);
                Console.ResetColor();
                Console.WriteLine("Type: {0}", device.GetType().Name);

                Console.WriteLine("IP: {0}", await device.GetExternalIPAsync());

                Console.WriteLine("---");

                //return;

                /******************************************/
                /*         Advanced test suite.           */
                /******************************************/

                // Try to create a new port map:
                var mapping = new Mapping(Protocol.Tcp, 56001, 56011);
                await device.CreatePortMapAsync(mapping);
                Console.WriteLine("Create Mapping: protocol={0}, public={1}, private={2}", mapping.Protocol,
                    mapping.PublicPort,
                    mapping.PrivatePort);

                // Try to retrieve confirmation on the port map we just created:
                try
                {
                    Mapping m = await device.GetSpecificMappingAsync(Protocol.Tcp, mapping.PublicPort);
                    Console.WriteLine("Specific Mapping: protocol={0}, public={1}, private={2}", m.Protocol,
                        m.PublicPort,
                        m.PrivatePort);
                }
                catch
                {
                    Console.WriteLine("Couldn't get specific mapping");
                }

                // Try retrieving all port maps:
                try
                {
                    var mappings = await device.GetAllMappingsAsync();
                    if (mappings.Length == 0)
                        Console.WriteLine("No existing uPnP mappings found.");
                    foreach (Mapping mp in mappings)
                        Console.WriteLine("Existing Mappings: protocol={0}, public={1}, private={2}", mp.Protocol,
                            mp.PublicPort, mp.PrivatePort);
                }
                catch
                {
                    Console.WriteLine("Couldn't get all mappings");
                }

                // Try deleting the port we opened before:
                try
                {
                    //await device.DeletePortMapAsync(mapping);
                    Console.WriteLine("Deleting Mapping: protocol={0}, public={1}, private={2}", mapping.Protocol,
                        mapping.PublicPort, mapping.PrivatePort);
                }
                catch
                {
                    Console.WriteLine("Couldn't delete specific mapping");
                }

                // Try retrieving all port maps:
                try
                {
                    var mappings = await device.GetAllMappingsAsync();
                    if (mappings.Length == 0)
                        Console.WriteLine("No existing uPnP mappings found.");
                    foreach (Mapping mp in mappings)
                        Console.WriteLine("Existing Mapping: protocol={0}, public={1}, private={2}", mp.Protocol,
                            mp.PublicPort, mp.PrivatePort);
                }
                catch
                {
                    Console.WriteLine("Couldn't get all mappings");

                }

                Console.WriteLine("External IP: {0}", await device.GetExternalIPAsync());
                Console.WriteLine("Done...");
            }
            catch
            {
                Console.WriteLine("unknown error..." );
            }
            finally
            {
                locker.Release();
            }
            _completionSource.SetResult(null);
        }
    }
}
