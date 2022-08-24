using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FNNP;

public class RemoteIPService : BackgroundService, IDisposable
{
    public readonly int IPServicePort;
    public readonly string ServerAddr;
    private const int DefaultServerPort = 6080;
    private const string DefaultServerAddr = "localhost";
    private readonly ILogger<RemoteIPService> _logger;
    private readonly IOptions<FacillitatorConfig> _config;
    
    public RemoteIPService(ILogger<RemoteIPService> logger, IOptions<FacillitatorConfig> config)
    {
        _logger = logger;
        _config = config;
        IPServicePort = (config.Value.IPServicePort != 0) ? config.Value.IPServicePort : DefaultServerPort;
        ServerAddr = (config.Value.ServerAddress != null) ? config.Value.ServerAddress : DefaultServerAddr;
    }
    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Task.Run(async () =>
            await ServiceListener(cancellationToken), cancellationToken);
        return Task.CompletedTask;
    }
    public Task ServiceListener(CancellationToken cancellationToken)
    {
        EventBasedNetListener listener = new EventBasedNetListener();
        
        listener.ConnectionRequestEvent += request =>
        {
            Console.WriteLine("connection from: " + request.RemoteEndPoint);
            request.AcceptIfKey("test");
        };
        listener.PeerConnectedEvent += peer =>
        {
            Console.WriteLine("Request from: " + peer.EndPoint);
            NetDataWriter writer = new NetDataWriter();                 // Create writer class
            writer.Put(peer.EndPoint);                                // Put some string
            peer.Send(writer, DeliveryMethod.ReliableOrdered);             // Send with reliability
        };
        NetManager ipGuy = new NetManager(listener);
        ipGuy.Start(IPServicePort);
        
        Console.WriteLine("=== RemoteIPService started at " + ServerAddr + ":" + IPServicePort + " ===");
        
        while (true)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.Escape)
                {
                    break;
                }
            }
            ipGuy.PollEvents();
            Thread.Sleep(10);
        }
        ipGuy.Stop();
        return Task.CompletedTask;
    }
    public Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("=== RemoteIPService stopped ===");
        return Task.CompletedTask;
    }
    public void Dispose()
    {
        Console.WriteLine("Dispose of RemoteIPService");
    }
}
