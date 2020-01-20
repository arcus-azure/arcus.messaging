using Arcus.Messaging.Health.Tcp;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Arcus.Messaging.Tests.Workers.ServiceBus.Queue
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
                    services.AddServiceBusQueueMessagePump<OrdersMessagePump>(configuration => configuration["ARCUS_SERVICEBUS_CONNECTIONSTRING"]);
                    services.AddTcpHealthProbes("ARCUS_HEALTH_PORT", builder => builder.AddCheck("sample", () => HealthCheckResult.Healthy()));
                });
    }
}