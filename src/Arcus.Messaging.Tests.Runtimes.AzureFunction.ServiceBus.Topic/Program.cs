using Arcus.EventGrid.Publishing;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Azure;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(builder =>
    {
        builder.Services.AddAzureClients(clients =>
        {
            var eventGridTopic = Environment.GetEnvironmentVariable("ARCUS_EVENTGRID_TOPIC_URI");
            var eventGridKey = Environment.GetEnvironmentVariable("ARCUS_EVENTGRID_AUTH_KEY");
            clients.AddEventGridPublisherClient(new Uri(eventGridTopic), new AzureKeyCredential(eventGridKey));
        });
            
        builder.Services.AddServiceBusMessageRouting()
                        .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();
    })
    .Build();

host.Run();
