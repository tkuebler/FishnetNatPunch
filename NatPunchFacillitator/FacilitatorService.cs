using System;
using System.Collections.Generic;
using LiteNetLib;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
// based on : https://github.com/RevenantX/LiteNetLib/blob/master/LibSample/HolePunchServerTest.cs
// src: https://github.com/tkuebler/FishnetNatPunch
// new .net hosting/daemon/options extension: https://www.atmosera.com/blog/creating-a-daemon-with-net-core-part-2/

namespace FNNP
{
    public class WaitPeer
    {
        public static int ClientIdCounter = 0;
        public IPEndPoint InternalAddr { get; }
        public IPEndPoint ExternalAddr { get; }
        public bool IsGameServer { get; }
        public String GameToken { get; }
        public int ClientId { get; }
        public DateTime RefreshTime { get; private set; }

        public void Refresh()
        {
            RefreshTime = DateTime.UtcNow;
        }
        public static int NextClientId()
        {
            ++ClientIdCounter; // TODO: roll over at max int?
            return ClientIdCounter;
        }
        public WaitPeer(IPEndPoint internalAddr, IPEndPoint externalAddr, bool isGameServer, String gameToken)
        {
            Refresh();
            InternalAddr = internalAddr;
            ExternalAddr = externalAddr;
            IsGameServer = isGameServer;
            GameToken = gameToken;
            ClientId = NextClientId();
        }
    }

    // Lazy/quick way to not touch the LiteLibNet Punchthrough module but add some stuff to the request
    public struct TokenData
    {
        public const string ConnectToken = "FNNP";
        public bool isServer;
        public string gameToken;
        public TokenData(bool serv, string token)
        {
            isServer = serv;
            gameToken = token;
        }

        public TokenData(string joinedToken)
        {
            (isServer, gameToken) = ParseToken(joinedToken);
        }
        public static string CreateToken(bool isServer, string gameToken)
        {
            return isServer.ToString() + "::" + gameToken;
        }
        public static (bool, string) ParseToken(string token)
        {
            var tmp = token.Split("::");
            return (Boolean.Parse(tmp[0]), tmp[1]);
        }
    }
    
    public class PunchListener : INatPunchListener
    {
        private NetManager _puncher;
        public readonly Dictionary<string, List<WaitPeer>> _waitingPeers = new Dictionary<string, List<WaitPeer>>();
        public readonly Dictionary<string, WaitPeer> _waitingServers = new Dictionary<string, WaitPeer>();
        public readonly List<string> _peersToRemove = new List<string>();
        public PunchListener(NetManager puncher)
        {
            _puncher = puncher;
        }

        void INatPunchListener.OnNatIntroductionRequest(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint,
            string token)
        {
            TokenData tokenData = new TokenData(token);

            if (tokenData.isServer)
            {
                if (_waitingServers.ContainsKey(tokenData.gameToken)) // refresh if valid, ignore if not
                {
                    WaitPeer _server = _waitingServers[tokenData.gameToken];
                    if (localEndPoint == _server.InternalAddr
                        && remoteEndPoint == _server.ExternalAddr
                        && tokenData.gameToken == _server.GameToken)
                    {
                        _server.Refresh();
                    }
                    else
                    {
                        Console.Write("ERROR: duplicate game key from different server ip, ignoring.");
                        return;
                    }
                }
                else // add to server list
                {
                    WaitPeer _peer = new WaitPeer(localEndPoint, remoteEndPoint, tokenData.isServer,
                        tokenData.gameToken);
                    _waitingServers[tokenData.gameToken] = _peer;
                    Console.WriteLine("Added waiting server ({0}) i {1} - o {2}", _peer.ClientId, _peer.InternalAddr,
                        _peer.ExternalAddr);
                }

                // drain queue, your server has arrived
                if (_waitingPeers.TryGetValue(tokenData.gameToken, out var gamePeers)) // check for waiting peers
                {
                    List<WaitPeer> removePeers = new List<WaitPeer>();
                    foreach (var waitPeer in gamePeers) // processes waiting queue
                    {
                        if (waitPeer.InternalAddr.Equals(localEndPoint) &&
                            waitPeer.ExternalAddr.Equals(remoteEndPoint) &&
                            waitPeer.GameToken.Equals(tokenData.gameToken))
                        {
                            waitPeer.Refresh();
                            return; // Repeat introduction request does a keep-alive
                        }

                        if (_waitingServers.ContainsKey(tokenData
                                .gameToken)) // connect the server with the incoming request without queing
                        {
                            Console.WriteLine("Wait peer found, sending introduction...");
                            //found in list - introduce client and host to eachother
                            Console.WriteLine(
                                "{4}: queued client{5} - i({0}) e({1})\n to server:  i({2}) e({3})",
                                waitPeer.InternalAddr,
                                waitPeer.ExternalAddr,
                                _waitingServers[tokenData.gameToken].InternalAddr,
                                _waitingServers[tokenData.gameToken].ExternalAddr,
                                waitPeer.GameToken,
                                waitPeer.ClientId);

                            _puncher.NatPunchModule.NatIntroduce(
                                _waitingServers[tokenData.gameToken].InternalAddr, // waiting game server
                                _waitingServers[tokenData.gameToken].ExternalAddr, // waiting game server
                                waitPeer.InternalAddr, // queued client
                                waitPeer.ExternalAddr, // queued client
                                token // request token
                            );


                            //Clear dictionary of waiting client, because it's been introduced to the server
                            Console.WriteLine("Removing client {6} from {5} wait queue",
                                waitPeer.InternalAddr,
                                waitPeer.ExternalAddr,
                                localEndPoint,
                                remoteEndPoint,
                                waitPeer.IsGameServer,
                                waitPeer.GameToken,
                                waitPeer.ClientId);

                            // add to the remove list the thing we just introduced to the server
                            removePeers.Add(waitPeer);
                        }
                    }

                    // remove list of peers to remove from queue TODO: use the proper existing data structure
                    foreach (var removePeer in removePeers)
                    {
                        _waitingPeers[removePeer.GameToken].Remove(removePeer);
                    }
                }
            }
            else // is a client not a server/host
            {
                if (_waitingServers.ContainsKey(tokenData.gameToken))
                {
                    WaitPeer _server = _waitingServers[tokenData.gameToken];
                    Console.WriteLine("There is a server waiting for incoming client request...");
                    Console.WriteLine(
                        "{4}: direct client{5} - i({0}) e({1})\n to server:  i({2}) e({3})",
                        localEndPoint,
                        remoteEndPoint,
                        _server.InternalAddr,
                        _server.ExternalAddr,
                        tokenData.gameToken,
                        WaitPeer.NextClientId());

                    _puncher.NatPunchModule.NatIntroduce(
                        _server.InternalAddr, // waiting server
                        _server.ExternalAddr, // waiting server
                        localEndPoint, // incoming client
                        remoteEndPoint, // incoming client
                        token // request token
                    );
                }
                else // Create a waiting peer for this game token that has no host so when one appears it will get processed
                {
                    // create a blank wait list if this is a new game token
                    if (!_waitingPeers.ContainsKey(tokenData.gameToken))
                        _waitingPeers[tokenData.gameToken] = new List<WaitPeer>();

                    WaitPeer _peer = new WaitPeer(localEndPoint, remoteEndPoint, tokenData.isServer,
                        tokenData.gameToken);
                    _waitingPeers[tokenData.gameToken].Add(_peer);
                    Console.WriteLine(
                        "No server for " + tokenData.gameToken + ". Wait peer({2}) created.  i({0}) e({1})",
                        localEndPoint,
                        remoteEndPoint, _peer.ClientId, _peer.IsGameServer);
                    // we'll catch this the flip side?
                }
            }
        }
        void INatPunchListener.OnNatIntroductionSuccess(IPEndPoint targetEndPoint, NatAddressType type,
            string token)
        {
            //Ignore we are server
        }
    }

    public class FacillitatorService : IHostedService, IDisposable
    {
        private readonly Dictionary<string, WaitPeer> _waitingPeers = new Dictionary<string, WaitPeer>();
        private static readonly TimeSpan KickTime = new TimeSpan(0, 0, 60);
        
        public const int DefaultServerPort = 61111;
        public const string DefaultServerAddr = "localhost";
        //static void Main(string[] args)

        public Task StartAsync(CancellationToken cancellationToken)
        {
            int ServerPort = DefaultServerPort;
            string ServerAddr = DefaultServerAddr;
            Console.WriteLine("NatPunchFacillitator <serverPort> <serverAddress>");
            Console.WriteLine("https://github.com/tkuebler/FishnetNatPunch");
            int junk;
            
            // TODO: encorporate the new config extensions that part the command line for you here
            // ugly, but whatever, I'll make it pretty later
            // if (args.Length > 0)
            //     ServerPort = args != null && (int.TryParse(args[0], out junk)) ? junk : DefaultServerPort;
            // if (args.Length > 1)
            //     ServerAddr = (args[1] != null) ? args[1] : DefaultServerAddr;

            Console.WriteLine("=== UDP NAT HolePunch Facillitator v0.1 alpha running on " + ServerAddr + ":" +
                              ServerPort + " ===");


            EventBasedNetListener clientListener = new EventBasedNetListener();

            clientListener.PeerConnectedEvent += peer => { Console.WriteLine("PeerConnected: " + peer.EndPoint); };

            clientListener.ConnectionRequestEvent += request =>
            {
                request.AcceptIfKey(TokenData.ConnectToken);
                Console.WriteLine($"Conrequest {request.RemoteEndPoint.ToString()}");
            };

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
                //IPv6Mode = IPv6Mode.DualMode,
                NatPunchEnabled = true
            };
            ;

            _puncher.Start(ServerPort);
            PunchListener punchListener = new PunchListener(_puncher);
            _puncher.NatPunchModule.Init(punchListener);

            // keep going until ESCAPE is pressed
            Console.WriteLine("Press ESC to quit.   Press 'I' for server info");

            // TODO: refactor around the aynch task
            while (true)
            {
                // TODO: move this to the daemon wrapper
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.Escape)
                    {
                        break;
                    }

                    if (key == ConsoleKey.I)
                    {
                        Console.WriteLine($"Info: {_puncher.Statistics}");
                    }
                }

                DateTime nowTime = DateTime.UtcNow;

                _puncher.NatPunchModule.PollEvents();
                _puncher.PollEvents();

                //check old peers TODO: add servers too
                foreach (var waitingPeers in punchListener._waitingPeers)
                {
                    foreach (var waitPeer in waitingPeers.Value)
                    {
                        if (nowTime - waitPeer.RefreshTime > KickTime)
                        {
                            punchListener._peersToRemove.Add(waitingPeers.Key);
                        }
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
            // TODO: refactor
            //_puncher.Stop();
            return Task.CompletedTask;
        }
    

    // public Task StartAsync(CancellationToken cancellationToken)
        // {
        //     throw new NotImplementedException();
        // }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            //throw new NotImplementedException();
            Console.WriteLine("StopAsync");
            return Task.CompletedTask;
        }
        public void Dispose()
        {
            //throw new NotImplementedException();
            Console.WriteLine("Dispose");
        }
    }
}
