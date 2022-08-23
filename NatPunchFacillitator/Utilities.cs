
using System.Net;
using LiteNetLib;

namespace FNNP
{

    public class Utilities
    {
        private const string PublicIpApi = "https://api.my-ip.io/ip";

        public struct IPServiceDef
        {
            public int port = 6080;
            public string addr = "localhost";
            public IPServiceDef()
            {
            }
        }

        public string GetPublicIp()
        {
            return GetPublicIp_API(PublicIpApi);
        }

        public string GetPublicIp_Facillitator(IPServiceDef ipService)
        {
            EventBasedNetListener listener = new EventBasedNetListener();
            NetManager client = new NetManager(listener);
            client.Start();
            client.Connect("localhost" /* host ip or name */, 9050 /* port */, "SomeConnectionKey" /* text key or NetDataWriter */);
            listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) =>
            {
                Console.WriteLine("We got: {0}", dataReader.GetString(100 /* max length of string */));
                dataReader.Recycle();
            };

            while (!Console.KeyAvailable)
            {
                client.PollEvents();
                Thread.Sleep(15);
            }

            client.Stop();
        }
        public string GetPublicIp_API(string apiUrl)
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