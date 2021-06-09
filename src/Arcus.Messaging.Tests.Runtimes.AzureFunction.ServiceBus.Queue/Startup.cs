using Arcus.EventGrid.Publishing;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue;
using Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue.MessageHandlers;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
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
            IConfiguration configuration = new ConfigurationBuilder()
                    .SetBasePath(builder.GetContext().ApplicationRootPath)
                    .AddEnvironmentVariables("ARCUS_")
                    .AddJsonFile("local.settings.json", optional: true)
                    .Build();

            builder.Services.AddTransient(svc =>
            {
                var eventGridTopic = configuration.GetValue<string>("EVENTGRID_TOPIC_URI");
                var eventGridKey = configuration.GetValue<string>("EVENTGRID_AUTH_KEY");

                return EventGridPublisherBuilder
                    .ForTopic(eventGridTopic)
                    .UsingAuthenticationKey(eventGridKey)
                    .Build();
            });
            
            builder.AddServiceBusMessageRouting()
                   .WithServiceBusMessageHandler<DummyServiceBusMessageHandler, Order>();
        }
    }
}
