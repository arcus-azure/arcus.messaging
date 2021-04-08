using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Unit.Fixture
{
    /// <summary>
    /// Represents an <see cref="AzureServiceBusMessageRouter"/> instance for test purposes.
    /// </summary>
    public class TestAzureServiceBusMessageRouter : AzureServiceBusMessageRouter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IMessageHandler{TMessage}"/> instances.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the routing of the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        public TestAzureServiceBusMessageRouter(IServiceProvider serviceProvider, ILogger logger) 
            : base(serviceProvider, logger)
        {
        }
    }
}
