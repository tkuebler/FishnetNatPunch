using System;
using System.Collections.Generic;
using LiteNetLib;
using System.Net;
using System.Threading;

// based on : https://github.com/RevenantX/LiteNetLib/blob/master/LibSample/HolePunchServerTest.cs

namespace FNNP
{
    public class WaitPeer
    {
        public IPEndPoint InternalAddr { get; }
        public IPEndPoint ExternalAddr { get; }
        public DateTime RefreshTime { get; private set; }

        public void Refresh()
        {
            RefreshTime = DateTime.UtcNow;
        }

        public WaitPeer(IPEndPoint internalAddr, IPEndPoint externalAddr)
        {
            Refresh();
            InternalAddr = internalAddr;
            ExternalAddr = externalAddr;
        }
    }

    public class PunchListener : INatPunchListener
    {
        private NetManager _puncher;
        public readonly Dictionary<string, WaitPeer> _waitingPeers = new Dictionary<string, WaitPeer>();
        public readonly List<string> _peersToRemove = new List<string>();
        public PunchListener(NetManager puncher)
        {
            _puncher = puncher;
        }
        void INatPunchListener.OnNatIntroductionRequest(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint,
            string token)
        {
            if (_waitingPeers.TryGetValue(token, out var wpeer))
            {
                if (wpeer.InternalAddr.Equals(localEndPoint) &&
                    wpeer.ExternalAddr.Equals(remoteEndPoint))
                {
                    wpeer.Refresh();
                    return;
                }

                Console.WriteLine("Wait peer found, sending introduction...");

                //found in list - introduce client and host to eachother
                Console.WriteLine(
                    "host - i({0}) e({1})\nclient - i({2}) e({3})",
                    wpeer.InternalAddr,
                    wpeer.ExternalAddr,
                    localEndPoint,
                    remoteEndPoint);

                _puncher.NatPunchModule.NatIntroduce(
                    wpeer.InternalAddr, // host internal
                    wpeer.ExternalAddr, // host external
                    localEndPoint, // client internal
                    remoteEndPoint, // client external
                    token // request token
                );

                //Clear dictionary
                _waitingPeers.Remove(token);
            }
            else
            {
                Console.WriteLine("Wait peer created. i({0}) e({1})", localEndPoint, remoteEndPoint);
                _waitingPeers[token] = new WaitPeer(localEndPoint, remoteEndPoint);
            }
        }

        void INatPunchListener.OnNatIntroductionSuccess(IPEndPoint targetEndPoint, NatAddressType type,
            string token)
        {
            //Ignore we are server
        }
    }

    public class Program
    {
        private const int ServerPort = 50010;
        private const string ConnectionKey = "test_key";
        private readonly Dictionary<string, WaitPeer> _waitingPeers = new Dictionary<string, WaitPeer>();
        private static readonly TimeSpan KickTime = new TimeSpan(0, 0, 6);

        static void Main(string[] args)
        {

            Console.WriteLine("=== HolePunch Server v0.1 alpha localhost:"+ ServerPort + " ===");
            

            EventBasedNetListener clientListener = new EventBasedNetListener();

            clientListener.PeerConnectedEvent += peer => { Console.WriteLine("PeerConnected: " + peer.EndPoint); };

            clientListener.ConnectionRequestEvent += request => { request.AcceptIfKey(ConnectionKey); };

            clientListener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
            {
                Console.WriteLine("PeerDisconnected: " + disconnectInfo.Reason);
                if (disconnectInfo.AdditionalData.AvailableBytes > 0)
                {
                    Console.WriteLine("Disconnect data: " + disconnectInfo.AdditionalData.GetInt());
                }
            };
            NetManager _puncher = new NetManager(clientListener)
            {
                IPv6Mode = IPv6Mode.DualMode,
                NatPunchEnabled = true
            };
            ;

            _puncher.Start(ServerPort);
            PunchListener punchListener = new PunchListener(_puncher);
            _puncher.NatPunchModule.Init(punchListener);

            // keep going until ESCAPE is pressed
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
                        Console.WriteLine("Server stopped");
                    }
                }

                DateTime nowTime = DateTime.UtcNow;

                _puncher.NatPunchModule.PollEvents();

                //check old peers
                foreach (var waitPeer in punchListener._waitingPeers)
                {
                    if (nowTime - waitPeer.Value.RefreshTime > KickTime)
                    {
                        punchListener._peersToRemove.Add(waitPeer.Key);
                    }
                }

                //remove
                for (int i = 0; i < punchListener._peersToRemove.Count; i++)
                {
                    Console.WriteLine("Kicking peer: " + punchListener._peersToRemove[i]);
                    punchListener._waitingPeers.Remove(punchListener._peersToRemove[i]);
                }

                punchListener._peersToRemove.Clear();

                Thread.Sleep(10);
            }

            _puncher.Stop();
        }
    }
}
