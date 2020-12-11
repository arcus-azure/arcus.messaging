using System;
using Arcus.Messaging.Abstractions;
using GuardNet;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Pumps.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents how incoming messages can be routed through registered <see cref="IMessageHandler{TMessage}"/> instances.
    /// </summary>
    public class MessageRouter : MessageRouter<MessageContext>, IMessageRouter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IMessageHandler{TMessage,TMessageContext}"/> instances.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the routing of the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        public MessageRouter(IServiceProvider serviceProvider, ILogger<MessageRouter<MessageContext>> logger) 
            : base(serviceProvider, logger)
        {
            Guard.NotNull(serviceProvider, nameof(serviceProvider), "Requires a service provider to retrieve the registered message handlers");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IMessageHandler{TMessage,TMessageContext}"/> instances.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the routing of the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        public MessageRouter(IServiceProvider serviceProvider, ILogger logger) 
            : base(serviceProvider, logger)
        {
            Guard.NotNull(serviceProvider, nameof(serviceProvider), "Requires a service provider to retrieve the registered message handlers");
        }
    }
}