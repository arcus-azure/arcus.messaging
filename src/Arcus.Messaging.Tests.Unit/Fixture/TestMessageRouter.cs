using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Unit.Fixture
{
    /// <summary>
    /// Represents an <see cref="MessageRouter"/> model.
    /// </summary>
    public class TestMessageRouter : MessageRouter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestMessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IMessageHandler{TMessage,TMessageContext}"/> instances.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the routing of the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        public TestMessageRouter(IServiceProvider serviceProvider, ILogger logger) 
            : base(serviceProvider, logger)
        {
        }
    }
}
