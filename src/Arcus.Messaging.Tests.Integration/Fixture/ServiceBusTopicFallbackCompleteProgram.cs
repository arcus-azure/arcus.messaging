using Arcus.EventGrid.Publishing;
using Arcus.Messaging.Pumps.Abstractions.Extensions;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Arcus.Messaging.Tests.Workers.ServiceBus
{
    public class ServiceBusTopicFallbackCompleteProgram
    {
        public static void main(string[] args)
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
                .ConfigureLogging(loggingBuilder => loggingBuilder.SetMinimumLevel(LogLevel.Trace).AddConsole(options => options.IncludeScopes = true))
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient(svc =>
                    {
                        var configuration = svc.GetRequiredService<IConfiguration>();
                        var eventGridTopic = configuration.GetValue<string>("EVENTGRID_TOPIC_URI");
                        var eventGridKey = configuration.GetValue<string>("EVENTGRID_AUTH_KEY");

                        return EventGridPublisherBuilder
                               .ForTopic(eventGridTopic)
                               .UsingAuthenticationKey(eventGridKey)
                               .Build();
                    });
                    services.AddServiceBusTopicMessagePump("Test-Receive-All-Topic-Only", configuration => configuration["ARCUS_SERVICEBUS_CONNECTIONSTRING"], options => options.AutoComplete = false)
                            .WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>((AzureServiceBusMessageContext context) => false)
                            .WithServiceBusFallbackMessageHandler<OrdersFallbackCompleteMessageHandler>();

                    services.AddTcpHealthProbes("ARCUS_HEALTH_PORT");
                });
    }
}