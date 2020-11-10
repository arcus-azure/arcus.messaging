using System;
using System.Collections.Generic;
using System.Text;
using Arcus.EventGrid.Publishing;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    public class ServiceBusQueueContextPredicateSelectionWithDeadLetterProgram
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
                .ConfigureLogging(loggingBuilder => loggingBuilder.AddConsole(options => options.IncludeScopes = true))
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddServiceBusQueueMessagePump(configuration => configuration["ARCUS_SERVICEBUS_CONNECTIONSTRING"], options => options.AutoComplete = false)
                            .WithMessageHandler<PassThruOrderMessageHandler, Order, AzureServiceBusMessageContext>(context => false)
                            .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>(context => context.Properties["Topic"].ToString() == "Customers")
                            .WithServiceBusMessageHandler<OrdersAzureServiceBusDeadLetterMessageHandler, Order>(context => context.Properties["Topic"].ToString() == "Orders");

                    services.AddTcpHealthProbes("ARCUS_HEALTH_PORT");
                });
    }
}
