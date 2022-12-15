using System;
using Arcus.EventGrid.Publishing;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Azure;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue
{
    public class Startup : FunctionsStartup
    {
        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="builder">The instance to build the registered services inside the functions app.</param>
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddAzureClients(clients =>
            {
                var eventGridTopic = Environment.GetEnvironmentVariable("ARCUS_EVENTGRID_TOPIC_URI");
                var eventGridKey = Environment.GetEnvironmentVariable("ARCUS_EVENTGRID_AUTH_KEY");
                clients.AddEventGridPublisherClient(new Uri(eventGridTopic), new AzureKeyCredential(eventGridKey));
            });
            
            builder.AddServiceBusMessageRouting()
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();
        }
    }
}
