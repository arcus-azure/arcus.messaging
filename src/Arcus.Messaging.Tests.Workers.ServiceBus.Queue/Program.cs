using System;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Azure;
using Microsoft.Extensions.Azure;
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
                    services.AddLogging();
                    services.AddAzureClients(clients =>
                    {
                        var topicEndpoint = hostContext.Configuration.GetValue<string>("EVENTGRID_TOPIC_URI");
                        var authenticationKey = hostContext.Configuration.GetValue<string>("EVENTGRID_AUTH_KEY");
                        clients.AddEventGridPublisherClient(new Uri(topicEndpoint), new AzureKeyCredential(authenticationKey));
                    });

                    services.AddServiceBusQueueMessagePump(configuration => configuration["ARCUS_SERVICEBUS_CONNECTIONSTRING"])
                            .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();

                    services.AddTcpHealthProbes("ARCUS_HEALTH_PORT", builder => builder.AddCheck("sample", () => HealthCheckResult.Healthy()));
                });
    }
}