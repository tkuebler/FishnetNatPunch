using System;
using LiteNetLib;
using System.Net;
using System.Runtime.InteropServices;
using LiteNetLib.Utils;

namespace FNNP{

    public class SessionToken
    {
        public readonly string Token;
        public SessionToken()
        {
            Token = Generate();
        }
        public static string Generate()
        {
            string gameToken = Guid.NewGuid().ToString().GetHashCode().ToString();
            return gameToken;
        }
    }
public class NATPunchClient
{
    
    private const int DefaultServerPort = 61111; // 50010 or 61111 (raknet)
    private const string DefaultServerAddr = "voneggut.worldsalad.games"; // supposedly free server at natpunch.jenkinssoftware.com
    private static readonly TimeSpan KickTime = new TimeSpan(0, 0, 6);
    
    static void Main(string[] args)
    {
        string GameToken = "default";
        bool IsServer = false;
        int ServerPort = DefaultServerPort;
        string ServerAddr = DefaultServerAddr;
        
        int junk;
        // ugly, but whatever, I'll make it pretty later.  TODO: refactor this to a method to clean the flow
        Console.WriteLine("NATPunchClient <gameToken> <server|client> <serverPort> <serverAddress>");
        if(args.Length > 0)
        GameToken = args[0];
        if (args.Length > 1)
            IsServer = args[1].Equals("server") ? true : false;
        if (args.Length > 2)
            ServerPort = args != null && (int.TryParse(args[2], out junk)) ? junk : DefaultServerPort;
        if(args.Length > 3)
            ServerAddr = (args[3] != null) ? args[3] : DefaultServerAddr;
        
        Console.WriteLine("Client for game '{3}' (Gameserver:{0}) checking Facilitator: {1}:{2}",IsServer, ServerAddr, ServerPort, GameToken);

        EventBasedNetListener _clientListener = new EventBasedNetListener();
        
        _clientListener.PeerConnectedEvent += peer =>
        {
            Console.WriteLine("PeerConnected: " + peer.EndPoint.Address + ":" + peer.EndPoint.Port);
            NetDataWriter writer = new NetDataWriter();
            writer.Put("Hello?");
            writer.Put("Are we good?");
            peer.Send(writer, DeliveryMethod.ReliableOrdered);
        };

        _clientListener.NetworkReceiveEvent += (peer, reader, channelNumber, deliveryMethod) =>
        {
            Console.WriteLine("We got: {0} from {1}", reader.GetString(100 /* max length of string */), peer.EndPoint.ToString());
            reader.Recycle();
        };
        
        _clientListener.ConnectionRequestEvent += request =>
        {
            Console.WriteLine("connection request from {0}:{1}", request.RemoteEndPoint.Address, request.RemoteEndPoint.Port);
            request.AcceptIfKey(PunchUtils.ConnectToken);
        };

        _clientListener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
        {
            Console.WriteLine($"Peer {peer.EndPoint.ToString()} Disconnected: " + disconnectInfo.Reason);
            if (disconnectInfo.AdditionalData.AvailableBytes > 0)
            {
                Console.WriteLine("Disconnect data: " + disconnectInfo.AdditionalData.GetInt());
            }
        };
        _clientListener.DeliveryEvent += (peer, data) =>
        {
            Console.WriteLine($"{peer.EndPoint.ToString()} delievery event with: " + data.ToString());
        };
        
        NetManager _client = new NetManager(_clientListener)
        {
            //IPv6Mode = IPv6Mode.DualMode,
            NatPunchEnabled = true
        };
        EventBasedNatPunchListener natPunchListener = new EventBasedNatPunchListener();
        natPunchListener.NatIntroductionSuccess += (point, addrType, token) =>
        {
            var peer = _client.Connect(point,PunchUtils.ConnectToken);
            
            Console.WriteLine($"NatIntroductionSuccess {addrType} - Connecting to game {PunchUtils.SplitToken(token).gameToken} isServer({PunchUtils.SplitToken(token).isServer}): {point.Address}:{point.Port}, type: {addrType}, connection created: {peer != null}");
            // TODO: Pass traffic to verify (ICE)
            if (peer != null)
            {
                Console.WriteLine($"Nat Punched for: {peer.EndPoint.Address}:{peer.EndPoint.Port}. {peer.ConnectionState} from {_client.LocalPort.ToString()}");
                if (IsServer)
                    _client.NatPunchModule.Init(natPunchListener);
            }
            else
            {
                Console.WriteLine($"Error with Nat Introduction with  peer {peer.EndPoint.Address}:{peer.EndPoint.Port} from {_client.LocalPort.ToString()}. connected peers for this client: {_client.ConnectedPeerList.Count}");
            }
        };
        _client.NatPunchModule.Init(natPunchListener);
        _client.Start();
        Console.WriteLine("sending NatIntroductionRequest....");
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

                if (key == ConsoleKey.A) // TODO: instead of this capture keystrokes and send once punch has been made...
                {
                    Console.WriteLine("Client stopped");
                    _client.DisconnectPeer(_client.FirstPeer, new byte[] { 1, 2, 3, 4 });
                    _client.Stop();
                }
            }
            DateTime nowTime = DateTime.UtcNow;

            _client.NatPunchModule.PollEvents();
            _client.PollEvents();
        }
    }
}
}
