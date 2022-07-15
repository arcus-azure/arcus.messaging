using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using Microsoft.Extensions.DependencyInjection;

namespace Arcus.Messaging.Abstractions.EventHubs.MessageHandling
{
    /// <summary>
    /// Represents the model that exposes the available <see cref="IAzureEventHubsMessageHandler{TMessage}"/>s
    /// and possible additional configurations that can be configured with the current state of the Azure Service Bus instance.
    /// </summary>
    public class EventHubsMessageHandlerCollection : MessageHandlerCollection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventHubsMessageHandlerCollection" /> class.
        /// </summary>
        /// <param name="services">The current available collection services to register the Azure Service Bus message handling logic into.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        public EventHubsMessageHandlerCollection(IServiceCollection services) : base(services)
        {
        }
    }
}
