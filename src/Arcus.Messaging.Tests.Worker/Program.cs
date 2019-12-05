using Arcus.Messaging.Health.Tcp;
using Arcus.Messaging.Tests.Worker.MessageHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Arcus.Messaging.Tests.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args)
                .Build()
                .Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(configuration =>
                {
                    configuration.AddCommandLine(args);
                    configuration.AddEnvironmentVariables();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // TODO: Provide capability to configure connection string via IConfiguration & secret
                    services.AddServiceBusMessageHandler<OrdersMessagePump>();
                    services.AddHostedService<TcpHealthListener>();
                    services.AddHealthChecks();
                });
    }
}
