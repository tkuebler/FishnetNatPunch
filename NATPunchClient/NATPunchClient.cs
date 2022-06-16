using LiteNetLib;
using System.Net;

namespace FNNP{

public class NATPunchClient
{
    private const int ServerPort = 50010;
    private const string ConnectionKey = "test_key";
    private static readonly TimeSpan KickTime = new TimeSpan(0, 0, 6);
    
    static void Main(string[] args)
    {

        EventBasedNetListener _clientListener = new EventBasedNetListener();
        _clientListener.PeerConnectedEvent += peer =>
        {
            Console.WriteLine("PeerConnected: " + peer.EndPoint);
        };

        _clientListener.ConnectionRequestEvent += request =>
        {
            request.AcceptIfKey(ConnectionKey);
        };

        _clientListener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
        {
            Console.WriteLine("PeerDisconnected: " + disconnectInfo.Reason);
            if (disconnectInfo.AdditionalData.AvailableBytes > 0)
            {
                Console.WriteLine("Disconnect data: " + disconnectInfo.AdditionalData.GetInt());
            }
        };
        
        NetManager _client = new NetManager(_clientListener)
        {
            IPv6Mode = IPv6Mode.DualMode,
            NatPunchEnabled = true
        };
        
        EventBasedNatPunchListener natPunchListener = new EventBasedNatPunchListener();
        natPunchListener.NatIntroductionSuccess += (point, addrType, token) =>
        {
            var peer = _client.Connect(point, ConnectionKey);
            Console.WriteLine($"NatIntroductionSuccess C1. Connecting to C2: {point}, type: {addrType}, connection created: {peer != null}");
        };
        
        _client.NatPunchModule.Init(natPunchListener);
        _client.Start();
        _client.NatPunchModule.SendNatIntroduceRequest("localhost", ServerPort, "token1");

        Console.WriteLine("Press ESC to quit");

        while (true)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.Escape)
                {
                    break;
                }

                if (key == ConsoleKey.A)
                {
                    Console.WriteLine("Client stopped");
                    _client.DisconnectPeer(_client.FirstPeer, new byte[] { 1, 2, 3, 4 });
                    _client.Stop();
                }
            }
            DateTime nowTime = DateTime.UtcNow;

            _client.NatPunchModule.PollEvents();
        }
    }
}
}
