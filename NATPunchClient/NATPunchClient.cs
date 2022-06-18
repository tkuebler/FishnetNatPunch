using System;
using LiteNetLib;
using System.Net;

namespace FNNP{

    public static class SessionToken
    {
        public static string Generate()
        {
            string gameToken = Guid.NewGuid().ToString().GetHashCode().ToString();
            return gameToken;
        }
    }
public class NATPunchClient
{
    
    private const int DefaultServerPort = 61111; // 50010 or 61111 (raknet)
    private const string DefaultServerAddr = "localhost"; // supposedly free server at natpunch.jenkinssoftware.com
    private static readonly TimeSpan KickTime = new TimeSpan(0, 0, 6);
    
    static void Main(string[] args)
    {
        string GameToken = "default";
        bool IsServer = false;
        int ServerPort = DefaultServerPort;
        string ServerAddr = DefaultServerAddr;
        int junk;
        // ugly, but whatever
        Console.WriteLine("NATPunchClient <gameToken> <server|client> <serverPort> <serverAddress>");
        Console.WriteLine(args);
        if(args.Length > 0)
        GameToken = args[0];
        if (args.Length > 1)
            IsServer = args[1].Equals("server") ? true : false;
        if (args.Length > 2)
            ServerPort = args != null && (int.TryParse(args[2], out junk)) ? junk : DefaultServerPort;
        if(args.Length > 3)
            ServerAddr = (args[3] != null) ? args[3] : DefaultServerAddr;
        
        EventBasedNetListener _clientListener = new EventBasedNetListener();
        
        _clientListener.PeerConnectedEvent += peer =>
        {
            Console.WriteLine("PeerConnected: " + peer.EndPoint);
        };

        _clientListener.ConnectionRequestEvent += request =>
        {
            request.AcceptIfKey(PunchUtils.ConnectToken);
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
            var peer = _client.Connect(point, PunchUtils.ConnectToken);
            Console.WriteLine($"NatIntroductionSuccess C1. Connecting to C2: {point}, type: {addrType}, connection created: {peer != null}");
        };
        
        _client.NatPunchModule.Init(natPunchListener);
        _client.Start();
        
        _client.NatPunchModule.SendNatIntroduceRequest(ServerAddr, ServerPort, PunchUtils.MakeToken(IsServer, GameToken));

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
