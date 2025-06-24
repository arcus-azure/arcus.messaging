using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using Microsoft.Extensions.DependencyInjection;

namespace Arcus.Messaging.Abstractions.ServiceBus.MessageHandling
{
    /// <summary>
    /// Represents the model that exposes the available <see cref="IAzureServiceBusMessageHandler{TMessage}"/>s
    /// and possible additional configurations that can be configured with the current state of the Azure Service Bus instance.
    /// </summary>
    public class ServiceBusMessageHandlerCollection : MessageHandlerCollection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusMessageHandlerCollection" /> class.
        /// </summary>
        /// <param name="services">The current available collection services to register the Azure Service Bus message handling logic into.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        public ServiceBusMessageHandlerCollection(IServiceCollection services) : base(services)
        {
        }
    }
}
