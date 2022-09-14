using System;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;

namespace Arcus.Messaging.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents the model that exposes the available <see cref="IMessageHandler{TMessage}"/>s, <see cref="IFallbackMessageHandler"/>s,
    /// and possible additional configurations that can be configured with the current state.
    /// </summary>
    public class MessageHandlerCollection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHandlerCollection" /> class.
        /// </summary>
        /// <param name="services">The current available collection services to register the message handling logic into.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        public MessageHandlerCollection(IServiceCollection services)
        {
            Guard.NotNull(services, nameof(services), "Requires a collection of services to register the message handling logic into");
            Services = services;
        }

        /// <summary>
        /// Gets or sets the unique message pump ID to identify the message pump throughout the application.
        /// This ID can be used to get a reference of the previously registered message pump for which a message handler is registered.
        /// </summary>
        public string JobId { get; set; }
        
        /// <summary>
        /// Gets the current available collection of services to register the message handling logic into.
        /// </summary>
        public IServiceCollection Services { get; }
    }
}
