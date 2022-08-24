
using System.Net;
using LiteNetLib;
using LiteNetLib.Utils;

namespace FNNP
{

    public class Utilities
    {
        private const string PublicIpApi = "https://api.my-ip.io/ip";

        public struct IPServiceDef
        {
            public int port = 6080;
            public string addr = "localhost";
            public string key = "key";
            public IPServiceDef()
            {
            }
        }

        public static string GetPublicIp()
        {
            return GetPublicIp_API(PublicIpApi);
        }

        public static string GetPublicIp_Facillitator(IPServiceDef ipService)
        {
            string publicIp = "";
            bool connected = true;
            EventBasedNetListener listener = new EventBasedNetListener();
            NetManager client = new NetManager(listener);
            client.Start();
            
            listener.PeerConnectedEvent += (peer) =>
            {
                NetDataWriter writer = new NetDataWriter();                 // Create writer class
                writer.Put("get");                                // Put some string
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
                Console.WriteLine("connected to: " + peer.EndPoint);
            } ;
            listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) =>
            {
                publicIp = dataReader.GetString(100 /* max length of string */);
                Console.WriteLine($"We got: {publicIp}");
                client.DisconnectPeer(fromPeer);
                dataReader.Recycle();
            };
            listener.NetworkErrorEvent += (endpoint, error) =>
            {
                Console.WriteLine("NetError: " + endpoint + " - " + error);
                connected = false;
            };
            NetPeer server = client.Connect(ipService.addr, ipService.port, ipService.key);
            Console.WriteLine("Trying to connect to: " +server.EndPoint + " with key " + ipService.key);
            Console.WriteLine("Server connection state is: " + server.ConnectionState);
            while ((server.ConnectionState & ConnectionState.Disconnected) == 0)
            {
                client.PollEvents();
                Thread.Sleep(15);
            }
            client.Stop();
            return publicIp;
        }
        public static string GetPublicIp_API(string apiUrl)
        {
            WebClient client = new WebClient();
            // Add a user agent header in case the
            // requested URI contains a query.

            client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");

            Stream data = client.OpenRead(apiUrl);
            StreamReader reader = new StreamReader(data);
            string s = reader.ReadToEnd();
            Console.WriteLine(s);
            data.Close();
            reader.Close();
            return s;
        }
    }
}