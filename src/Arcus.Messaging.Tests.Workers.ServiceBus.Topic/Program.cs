using Arcus.EventGrid.Publishing;
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
                })
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
                    services.AddServiceBusTopicMessagePump<OrdersMessagePump>(
                        subscriptionPrefix: "Receive-All", 
                        configuration => configuration["ARCUS_SERVICEBUS_CONNECTIONSTRING"],
                        options => options.IncludeTopicSubscription = true);
                    services.AddTcpHealthProbes("ARCUS_HEALTH_PORT");
                });
    }
}
