using System.Collections.Generic;
using Arcus.EventGrid.Publishing;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Arcus.Messaging.Tests.Workers.ServiceBus.Topic
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
                    configuration.AddInMemoryCollection(new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("ARCUS_HEALTH_PORT", "5000"),
                        new KeyValuePair<string, string>("ARCUS_SERVICEBUS_CONNECTIONSTRING", "Endpoint=sb://arcus-messaging-dev-we-integration-tests.servicebus.windows.net/;SharedAccessKeyName=ManageSendListen;SharedAccessKey=ATIsPYMm9rUa8OgnhdHTD1dyL06coFApzNTlpqa2rxI=;EntityPath=order-topic")
                    });
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogging();
                    services.AddTransient(svc =>
                    {
                        var configuration = svc.GetRequiredService<IConfiguration>();
                        var eventGridTopic =
                            "https://arcus-event-grid-dev-we-integration-tests-cncf-ce.westeurope-1.eventgrid.azure.net/api/events";
                        var eventGridKey = "M1C7nia9SXWroDFyzl0dOMRL+1G2cI9D1+PCXkMsrd8=";

                        return EventGridPublisherBuilder
                            .ForTopic(eventGridTopic)
                            .UsingAuthenticationKey(eventGridKey)
                            .Build();
                    });
                    services.AddServiceBusTopicMessagePump("Receive-All", configuration => configuration["ARCUS_SERVICEBUS_CONNECTIONSTRING"])
                            .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();

                    services.AddTcpHealthProbes("ARCUS_HEALTH_PORT");
                });
    }
}
