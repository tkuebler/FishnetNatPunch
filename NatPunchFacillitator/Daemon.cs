using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FNNP
{

    public class FacillitatorConfig
    {
        public int Port { get; set; }
        public string IPAddress { get; set; }
    }

    public class Daemon
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Summoning Daemon....");
            var builder = new HostBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddEnvironmentVariables();
                    if (args != null)
                    {
                        config.AddCommandLine(args);
                    }
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions();
                    services.Configure<FacillitatorConfig>(hostContext.Configuration.GetSection("Facillitator"));
                    services.AddSingleton<IHostedService, FacillitatorService>();
                })
                .ConfigureLogging((hostingContext, logging) => { logging.AddConsole(); });
            await builder.RunConsoleAsync();
        }
    }
}
