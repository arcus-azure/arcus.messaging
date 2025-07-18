using System;
using Microsoft.Extensions.DependencyInjection;

namespace Arcus.Messaging.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents the model that exposes the available <see cref="IMessageHandler{TMessage,TMessageContext}"/>s
    /// and possible additional configurations that can be configured with the current state.
    /// </summary>
    public abstract class MessageHandlerCollection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHandlerCollection" /> class.
        /// </summary>
        /// <param name="services">The current available collection services to register the message handling logic into.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        protected MessageHandlerCollection(IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            Services = services;
        }

        /// <summary>
        /// Gets or sets the unique ID to identify the job that runs a message pump during the lifetime of the application.
        /// This ID can be used to get a reference of the previously registered message pump while registering message handlers and other functionality related to the message pump.
        /// </summary>
        public string JobId { get; set; }

        /// <summary>
        /// Gets the current available collection of services to register the message handling logic into.
        /// </summary>
        public IServiceCollection Services { get; }
    }
}
