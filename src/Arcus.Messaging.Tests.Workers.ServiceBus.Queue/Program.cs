using System;
using System.Collections.Generic;
using Arcus.EventGrid.Publishing;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
                    configuration.AddInMemoryCollection(new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("ARCUS_HEALTH_PORT", 5000.ToString())
                    });
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Trace));
                    //services.AddTransient(svc =>
                    //{
                    //    var configuration = svc.GetRequiredService<IConfiguration>();
                    //    var eventGridTopic = configuration.GetValue<string>("EVENTGRID_TOPIC_URI");
                    //    var eventGridKey = configuration.GetValue<string>("EVENTGRID_AUTH_KEY");

                    //    return EventGridPublisherBuilder
                    //        .ForTopic(eventGridTopic)
                    //        .UsingAuthenticationKey(eventGridKey)
                    //        .Build();
                    //});
                    //services.AddServiceBusQueueMessagePump(configuration => configuration["ARCUS_SERVICEBUS_CONNECTIONSTRING"])
                    //        .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();

                    int index = -1;
                    services.AddTcpHealthProbes(
                        "ARCUS_HEALTH_PORT", 
                        builder => builder.AddCheck("sample", () => ++index % 5 == 0 ? HealthCheckResult.Unhealthy() : HealthCheckResult.Healthy()),
                        options => options.RejectTcpConnectionWhenUnhealthy = true,
                        options =>
                        {
                            options.Delay = TimeSpan.Zero;
                            options.Period = TimeSpan.FromSeconds(5);
                        });
                });
    }
}