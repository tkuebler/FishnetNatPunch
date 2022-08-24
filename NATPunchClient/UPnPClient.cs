using System.Net;
using System.Runtime.CompilerServices;
using LiteNetLib;
using LiteNetLib.Utils;
using Mono.Nat;

namespace FNNP
{
    public class UPnPClient
    {
        private readonly string? _ipRouter;
        public const string? DefaultRouter = "192.168.1.1";
        private readonly Protocol _ipProtocol;

        #region  Constructors
        public UPnPClient(string? ipRouter, Protocol ipProtocol)
        {
            _ipRouter = (ipRouter == null) ? DefaultRouter : ipRouter;
            _ipProtocol = ipProtocol;
        }
        public UPnPClient() : this(DefaultRouter, Protocol.Tcp) {}
        public UPnPClient(Protocol ipProtocol) : this(DefaultRouter, ipProtocol){}
        public UPnPClient(string? ipServer) : this(ipServer, Protocol.Tcp) {}
        #endregion
        #region functions
       

        private TaskCompletionSource<object> _completionSource = new TaskCompletionSource<object>();

        public async Task TryToUPnP() // TODO: rename and support PMP
        {
            await TryToUPnP(_ipRouter,Protocol.Tcp, NatProtocol.Upnp);
        }
        public async Task TryToUPnP(Protocol ipProtocol)
        {
            await TryToUPnP(_ipRouter, ipProtocol, NatProtocol.Upnp);
        }
        public async Task TryToUPnP(Protocol ipProtocol, NatProtocol natProtocol)
        {
            await TryToUPnP(_ipRouter, ipProtocol, natProtocol);
        }
        public async Task TryToUPnP(string? routerIp, Protocol ipProtocol, NatProtocol natProtocol) 
        {
            NatUtility.DeviceFound += DeviceFound;
            // TODO: support device discovery
            if (routerIp == null)
                routerIp = DefaultRouter;
            Console.WriteLine($"DefaultRouter:{DefaultRouter}");
            Task runSearch = Task.Factory.StartNew(() => 
                {
                    NatUtility.Search(System.Net.
                        IPAddress.Parse(routerIp), 
                        natProtocol);
                });
            runSearch.Wait();
            await _completionSource.Task; 
            NatUtility.StopDiscovery();
        }
        #endregion
        #region Util
        public async Task<Mapping> MapPort(INatDevice device, Protocol ipProtocol)
        {
            // Try to create a new port map:
            var mapping = new Mapping(ipProtocol, 56001, 56011);
            Mapping result = await device.CreatePortMapAsync(mapping);
            
            Console.WriteLine("Create Mapping: protocol={0}, public={1}, private={2}", mapping.Protocol,
                mapping.PublicPort,
                mapping.PrivatePort);
            
            return result;
        }

        public async Task<Mapping> GetPortMapping(INatDevice device, Mapping mapping)
        {
            // Try to retrieve confirmation on the port map we just created:
            try
            {
                Mapping m = await device.GetSpecificMappingAsync(Protocol.Udp, mapping.PublicPort);
                Console.WriteLine("Specific Mapping: protocol={0}, public={1}, private={2}", m.Protocol,
                    m.PublicPort,
                    m.PrivatePort);
                return m;
            }
            catch
            {
                Console.WriteLine("Couldn't get specific mapping");
            }
            return null;
        }
        public async Task<Mapping[]> GetPortMappings(INatDevice device)
        {
            // Try retrieving all port maps:
            try
            {
                var mappings = await device.GetAllMappingsAsync();
                if (mappings.Length == 0)
                    Console.WriteLine("No existing uPnP mappings found.");
                foreach (Mapping mp in mappings)
                    Console.WriteLine("Existing Mappings: protocol={0}, public={1}, private={2}", mp.Protocol,
                        mp.PublicPort, mp.PrivatePort);
                return mappings;
            }
            catch
            {
                Console.WriteLine("Couldn't get all mappings");
            }

            return null;
        }
        public async Task DeletePortMapping(INatDevice device, Mapping mapping)
        {
            // Try deleting the port we opened before:
            try
            {
                await device.DeletePortMapAsync(mapping);
                Console.WriteLine("Deleting Mapping: protocol={0}, public={1}, private={2}", mapping.Protocol,
                    mapping.PublicPort, mapping.PrivatePort);
            }
            catch
            {
                Console.WriteLine("Couldn't delete specific mapping");
            }
        }
        #endregion
        #region event methods
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
                Mapping mapping = await MapPort(device, _ipProtocol);

                Mapping mappingVerify = await GetPortMapping(device, mapping);

                Mapping[] maps = await GetPortMappings(device);

                await DeletePortMapping(device, mapping);

                mappingVerify = await GetPortMapping(device, mapping);

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
        #endregion
    }
}
